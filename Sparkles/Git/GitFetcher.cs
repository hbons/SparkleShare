//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as
//   published by the Free Software Foundation, either version 3 of the
//   License, or (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sparkles.Git {

    public class GitFetcher : SSHFetcher {

        GitCommand git_clone;
        SSHAuthenticationInfo auth_info;

        string password_salt = Path.GetRandomFileName ().SHA256 ().Substring (0, 16);


        protected override bool IsFetchedRepoEmpty {
            get {
                var git_rev_parse = new GitCommand (TargetFolder, "rev-parse HEAD");
                git_rev_parse.StartAndWaitForExit ();

                return (git_rev_parse.ExitCode != 0);
            }
        }


        public GitFetcher (SparkleFetcherInfo fetcher_info, SSHAuthenticationInfo auth_info) : base (fetcher_info)
        {
            this.auth_info = auth_info;
            var uri_builder = new UriBuilder (RemoteUrl);

            if (!RemoteUrl.Scheme.Equals ("ssh") && !RemoteUrl.Scheme.Equals ("git"))
                uri_builder.Scheme = "ssh";

            if (RemoteUrl.Host.Equals ("github.com") ||
                RemoteUrl.Host.Equals ("gitlab.com")) {

                AvailableStorageTypes.Add (
                    new StorageTypeInfo (StorageType.LargeFiles, "Large File Storage",
                        "Trade off versioning to save space;\nkeeps file history on the host only"));

                uri_builder.Scheme   = "ssh";
                uri_builder.UserName = "git";

                if (!RemoteUrl.AbsolutePath.EndsWith (".git"))
                    uri_builder.Path += ".git";

            } else if (string.IsNullOrEmpty (RemoteUrl.UserInfo)) {
                uri_builder.UserName = "storage";
            }

            RemoteUrl = uri_builder.Uri;

            AvailableStorageTypes.Add (
                new StorageTypeInfo (StorageType.Encrypted, "Encrypted Storage",
                    "Trade off efficiency for privacy;\nencrypts before storing files on the host"));
        }


        public override bool Fetch ()
        {
            if (!base.Fetch ())
                return false;

            StorageType? storage_type = DetermineStorageType ();

            if (storage_type == null)
                return false;

            FetchedRepoStorageType = (StorageType) storage_type;

            string git_clone_command = "clone --progress --no-checkout";

            if (!FetchPriorHistory)
                git_clone_command += " --depth=1";

            if (storage_type == StorageType.LargeFiles)
                git_clone_command = "lfs clone --progress --no-checkout";

            git_clone = new GitCommand (Configuration.DefaultConfiguration.TmpPath,
                string.Format ("{0} \"{1}\" \"{2}\"", git_clone_command, RemoteUrl, TargetFolder),
                auth_info);

            git_clone.StartInfo.RedirectStandardError = true;
            git_clone.Start ();

            StreamReader output_stream = git_clone.StandardError;

            if (FetchedRepoStorageType == StorageType.LargeFiles)
                output_stream = git_clone.StandardOutput;

            double percentage = 0;
            double speed = 0;
            string information = "";

            while (!output_stream.EndOfStream) {
                string line = output_stream.ReadLine ();

                ErrorStatus error = GitCommand.ParseProgress (line, out percentage, out speed, out information);

                if (error != ErrorStatus.None) {
                    IsActive = false;
                    git_clone.Kill ();
                    git_clone.Dispose ();

                    return false;
                }

                OnProgressChanged (percentage, speed, information);
            }

            git_clone.WaitForExit ();

            if (git_clone.ExitCode != 0)
                return false;

            Thread.Sleep (500);
            OnProgressChanged (100, 0, "");
            Thread.Sleep (500);

            return true;
        }


        public override void Stop ()
        {
            try {
                if (git_clone != null && !git_clone.HasExited) {
                    git_clone.Kill ();
                    git_clone.Dispose ();
                }

            } catch (Exception e) {
                Logger.LogInfo ("Fetcher", "Failed to dispose properly", e);
            }

            if (Directory.Exists (TargetFolder)) {
                try {
                    Directory.Delete (TargetFolder, true /* Recursive */ );
                    Logger.LogInfo ("Fetcher", "Deleted '" + TargetFolder + "'");

                } catch (Exception e) {
                    Logger.LogInfo ("Fetcher", "Failed to delete '" + TargetFolder + "'", e);
                }
            }
        }


        public override string Complete (StorageType selected_storage_type)
        {
            string identifier = base.Complete (selected_storage_type);
            string identifier_path = Path.Combine (TargetFolder, ".sparkleshare");

            InstallConfiguration ();
            InstallGitLFS ();

            InstallAttributeRules ();
            InstallExcludeRules ();

            if (IsFetchedRepoEmpty) {
                File.WriteAllText (identifier_path, identifier);

                var git_add    = new GitCommand (TargetFolder, "add .sparkleshare");
                var git_commit = new GitCommand (TargetFolder, "commit --message=\"Initial commit by SparkleShare\"");

                // We can't do the "commit --all" shortcut because it doesn't add untracked files
                git_add.StartAndWaitForExit ();
                git_commit.StartAndWaitForExit ();

                // These branches will be pushed  later by "git push --all"
                if (selected_storage_type == StorageType.LargeFiles) {
                    var git_branch = new GitCommand (TargetFolder, "branch x-sparkleshare-lfs", auth_info);
                    git_branch.StartAndWaitForExit ();
                }

                if (selected_storage_type == StorageType.Encrypted) {
                    var git_branch = new GitCommand (TargetFolder,
                        string.Format ("branch x-sparkleshare-encrypted-{0}", password_salt), auth_info);

                    git_branch.StartAndWaitForExit ();
                }

            } else {
                if (File.Exists (identifier_path))
                    identifier = File.ReadAllText (identifier_path).Trim ();

                string branch = "HEAD";
                string prefered_branch = "SparkleShare";

                // Prefer the "SparkleShare" branch if it exists
                var git_show_ref = new GitCommand (TargetFolder,
                   "show-ref --verify --quiet refs/heads/" + prefered_branch);

                git_show_ref.StartAndWaitForExit ();

                if (git_show_ref.ExitCode == 0)
                    branch = prefered_branch;

                var git_checkout = new GitCommand (TargetFolder, string.Format ("checkout --quiet --force {0}", branch));
                git_checkout.StartAndWaitForExit ();
            }

            // git-lfs may leave junk behind
            string git_lfs_tmp_path = Path.Combine (Configuration.DefaultConfiguration.TmpPath, "lfs");

            if (Directory.Exists (git_lfs_tmp_path))
                Directory.Delete (git_lfs_tmp_path, true);

            File.SetAttributes (identifier_path, FileAttributes.Hidden);
            return identifier;
        }


        public override void EnableFetchedRepoCrypto (string password)
        {
            string password_file = ".git/info/encryption_password";
            var git_config_required = new GitCommand (TargetFolder, "config filter.encryption.required true");

            var git_config_smudge = new GitCommand (TargetFolder, "config filter.encryption.smudge " +
                string.Format ("\"openssl enc -d -aes-256-cbc -base64 -S {0} -pass file:{1}\"", password_salt, password_file));

            var git_config_clean = new GitCommand (TargetFolder, "config filter.encryption.clean " +
                string.Format ("\"openssl enc -e -aes-256-cbc -base64 -S {0} -pass file:{1}\"", password_salt, password_file));

            git_config_required.StartAndWaitForExit ();
            git_config_smudge.StartAndWaitForExit ();
            git_config_clean.StartAndWaitForExit ();

            // Store the password, TODO: 600 permissions
            string password_file_path = Path.Combine (TargetFolder, ".git", "info", "encryption_password");
            File.WriteAllText (password_file_path, password.SHA256 (password_salt));
        }


        public override bool IsFetchedRepoPasswordCorrect (string password)
        {
            string password_check_file_path = Path.Combine (TargetFolder, ".sparkleshare");

            if (!File.Exists (password_check_file_path)) {
                var git_show = new GitCommand (TargetFolder, "show HEAD:.sparkleshare");
                string output = git_show.StartAndReadStandardOutput ();

                if (git_show.ExitCode == 0)
                    File.WriteAllText (password_check_file_path, output);
                else
                    return false;
            }

            string args = string.Format ("enc -d -aes-256-cbc -base64 -S {0} -pass pass:{1} -in \"{2}\"",
                password_salt, password.SHA256 (password_salt), password_check_file_path);

            var process = new Command ("openssl", args);

            process.StartInfo.WorkingDirectory = TargetFolder;
            process.StartAndWaitForExit ();

            if (process.ExitCode == 0) {
                File.Delete (password_check_file_path);
                return true;
            }

            return false;
        }


        public override string FormatName ()
        {
            string name = Path.GetFileName (RemoteUrl.AbsolutePath);
            name = name.ReplaceUnderscoreWithSpace ();

            if (name.EndsWith (".git"))
                name = name.Replace (".git", "");

            return name;
        }


        StorageType? DetermineStorageType ()
        {
            var git_ls_remote = new GitCommand (Configuration.DefaultConfiguration.TmpPath,
                string.Format ("ls-remote --heads \"{0}\"", RemoteUrl), auth_info);

            string output = git_ls_remote.StartAndReadStandardOutput ();

            if (git_ls_remote.ExitCode != 0)
                return null;

            if (string.IsNullOrWhiteSpace (output))
                return StorageType.Unknown;

            foreach (string line in output.Split ("\n".ToCharArray ())) {
                string [] line_parts = line.Split ('/');
                string branch = line_parts [line_parts.Length - 1];

                if (branch == "x-sparkleshare-lfs")
                    return StorageType.LargeFiles;

                string encrypted_storage_prefix = "x-sparkleshare-encrypted-";

                if (branch.StartsWith (encrypted_storage_prefix)) {
                    password_salt = branch.Replace (encrypted_storage_prefix, "");
                    return StorageType.Encrypted;
                }
            }

            return StorageType.Plain;
        }


        void InstallConfiguration ()
        {
            string [] settings = {
                "core.autocrlf input",
                "core.quotepath false", // Don't quote "unusual" characters in path names
                "core.ignorecase false", // Be case sensitive explicitly to work on Mac
                "core.filemode false", // Ignore permission changes
                "core.precomposeunicode true", // Use the same Unicode form on all filesystems
                "core.safecrlf false",
                "core.excludesfile \"\"",
                "core.packedGitLimit 128m", // Some memory limiting options
                "core.packedGitWindowSize 128m",
                "pack.deltaCacheSize 128m",
                "pack.packSizeLimit 128m",
                "pack.windowMemory 128m",
                "push.default matching"
            };

            if (InstallationInfo.OperatingSystem == OS.Windows)
                settings [0] = "core.autocrlf true";

            foreach (string setting in settings) {
                var git_config = new GitCommand (TargetFolder, "config " + setting);
                git_config.StartAndWaitForExit ();
            }
        }


        void InstallExcludeRules ()
        {
            string git_info_path = Path.Combine (TargetFolder, ".git", "info");

            if (!Directory.Exists (git_info_path))
                Directory.CreateDirectory (git_info_path);

            string exclude_rules = string.Join (Environment.NewLine, ExcludeRules);
            string exclude_rules_file_path = Path.Combine (git_info_path, "exclude");

            File.WriteAllText (exclude_rules_file_path, exclude_rules);
        }


        void InstallAttributeRules ()
        {
            string git_attributes_file_path = Path.Combine (TargetFolder, ".git", "info", "attributes");
            Directory.CreateDirectory (Path.GetDirectoryName (git_attributes_file_path));

            if (FetchedRepoStorageType == StorageType.LargeFiles) {
                File.WriteAllText (git_attributes_file_path, "* filter=lfs diff=lfs merge=lfs -text");
                return;
            }

            if (FetchedRepoStorageType == StorageType.Encrypted) {
                File.WriteAllText (git_attributes_file_path, "* filter=encryption -diff -delta merge=binary");
                return;
            }

            TextWriter writer = new StreamWriter (git_attributes_file_path);

            // Treat all files as binary as we always want to keep both file versions on a conflict
            writer.WriteLine ("* merge=binary");

            // Compile a list of files we don't want Git to compress. Not compressing
            // already compressed files decreases memory usage and increases speed
            string [] extensions = {
                "jpg", "jpeg", "png", "tiff", "gif", // Images
                "flac", "mp3", "ogg", "oga", // Audio
                "avi", "mov", "mpg", "mpeg", "mkv", "ogv", "ogx", "webm", // Video
                "zip", "gz", "bz", "bz2", "rpm", "deb", "tgz", "rar", "ace", "7z", "pak", "tc", "iso", ".dmg" // Archives
            };

            foreach (string extension in extensions) {
                writer.WriteLine ("*." + extension + " -delta merge=binary");
                writer.WriteLine ("*." + extension.ToUpper () + " -delta merge=binary");
            }

            writer.Close ();
        }


        void InstallGitLFS ()
        {
			var git_config_required = new GitCommand (TargetFolder, "config filter.lfs.required true");

            string GIT_SSH_COMMAND = GitCommand.FormatGitSSHCommand (auth_info);
            string smudge_command;
            string clean_command;

            if (InstallationInfo.OperatingSystem == OS.Mac) {
                smudge_command = "env GIT_SSH_COMMAND='" + GIT_SSH_COMMAND + "' " + 
                    Path.Combine (Configuration.DefaultConfiguration.BinPath, "git-lfs") + " smudge %f";

                clean_command = Path.Combine (Configuration.DefaultConfiguration.BinPath, "git-lfs") + " clean %f";

            } else {
				smudge_command = "env GIT_SSH_COMMAND='" + GIT_SSH_COMMAND + "' git-lfs smudge %f";
                clean_command = "git-lfs clean %f";
            }

            var git_config_smudge = new GitCommand (TargetFolder,
                string.Format ("config filter.lfs.smudge \"{0}\"", smudge_command));

            var git_config_clean = new GitCommand (TargetFolder,
                string.Format ("config filter.lfs.clean '{0}'", clean_command));
            
            git_config_required.StartAndWaitForExit ();
            git_config_clean.StartAndWaitForExit ();
            git_config_smudge.StartAndWaitForExit ();
        }
    }
}
