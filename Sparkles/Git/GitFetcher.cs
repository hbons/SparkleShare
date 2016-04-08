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

        SSHAuthenticationInfo auth_info;
        GitCommand git_clone;

        Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);
        Regex speed_regex    = new Regex (@"([0-9\.]+) ([KM])iB/s", RegexOptions.Compiled);

        string password_salt = "662282447f6bbb8c8e15fb32dd09e3e708c32bc8";


        public override bool IsFetchedRepoEmpty {
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

                uri_builder.Scheme   = "ssh";
                uri_builder.UserName = "git";

                if (!RemoteUrl.AbsolutePath.EndsWith (".git"))
                    uri_builder.Path += ".git";

            } else if (string.IsNullOrEmpty (RemoteUrl.UserInfo)) {
                uri_builder.UserName = "storage";
            }

            RemoteUrl = uri_builder.Uri;
        }


        public override bool Fetch ()
        {
            if (!base.Fetch ())
                return false;

            if (FetchPriorHistory) {
                git_clone = new GitCommand (Configuration.DefaultConfiguration.TmpPath,
                    "clone --progress --no-checkout \"" + RemoteUrl + "\" \"" + TargetFolder + "\"", auth_info);

            } else {
                git_clone = new GitCommand (Configuration.DefaultConfiguration.TmpPath,
                    "clone --progress --no-checkout --depth=1 \"" + RemoteUrl + "\" \"" + TargetFolder + "\"", auth_info);
            }

            git_clone.StartInfo.RedirectStandardError = true;
            git_clone.Start ();

            double percentage = 1.0;

            var last_change = DateTime.Now;
            var change_interval = new TimeSpan (0, 0, 0, 1);

            try {
                while (!git_clone.StandardError.EndOfStream) {
                    string line = git_clone.StandardError.ReadLine ();
                    Match match = progress_regex.Match (line);

                    double number = 0.0;
                    double speed  = 0.0;
                    if (match.Success) {
                        try {
                            number = double.Parse (match.Groups [1].Value, new CultureInfo ("en-US"));

                        } catch (FormatException) {
                            Logger.LogInfo ("Git", "Error parsing progress: \"" + match.Groups [1] + "\"");
                        }

                        // The pushing progress consists of two stages: the "Compressing
                        // objects" stage which we count as 20% of the total progress, and
                        // the "Writing objects" stage which we count as the last 80%
                        if (line.Contains ("Compressing")) {
                            // "Compressing objects" stage
                            number = (number / 100 * 20);

                        } else {
                            // "Writing objects" stage
                            number = (number / 100 * 80 + 20);
                            Match speed_match = speed_regex.Match (line);

                            if (speed_match.Success) {
                                try {
                                    speed = double.Parse (speed_match.Groups [1].Value, new CultureInfo ("en-US")) * 1024;

                                } catch (FormatException) {
                                    Logger.LogInfo ("Git", "Error parsing speed: \"" + speed_match.Groups [1] + "\"");
                                }

                                if (speed_match.Groups [2].Value.Equals ("M"))
                                    speed = speed * 1024;
                            }
                        }

                    } else {
                        Logger.LogInfo ("Fetcher", line);
                        line = line.Trim (new char [] {' ', '@'});

                        if (line.StartsWith ("fatal:", StringComparison.InvariantCultureIgnoreCase) ||
                            line.StartsWith ("error:", StringComparison.InvariantCultureIgnoreCase)) {

                            errors.Add (line);

                        } else if (line.StartsWith ("WARNING: REMOTE HOST IDENTIFICATION HAS CHANGED!")) {
                            errors.Add ("warning: Remote host identification has changed!");

                        } else if (line.StartsWith ("WARNING: POSSIBLE DNS SPOOFING DETECTED!")) {
                            errors.Add ("warning: Possible DNS spoofing detected!");
                        }
                    }

                    if (number >= percentage) {
                        percentage = number;

                        if (DateTime.Compare (last_change, DateTime.Now.Subtract (change_interval)) < 0) {
                            OnProgressChanged (percentage, speed);
                            last_change = DateTime.Now;
                        }
                    }
                }

            } catch (Exception) {
                IsActive = false;
                return false;
            }

            git_clone.WaitForExit ();

            if (git_clone.ExitCode != 0)
                return false;

            while (percentage < 100) {
                percentage += 25;

                if (percentage >= 100)
                    break;

                Thread.Sleep (500);
                OnProgressChanged (percentage, 0);
            }

            OnProgressChanged (100, 0);

            InstallConfiguration ();
            InstallExcludeRules ();
            InstallAttributeRules ();
            InstallGitLFS ();

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


        public override void Complete ()
        {
            if (!IsFetchedRepoEmpty) {
                string branch = "HEAD";
                string prefered_branch = "SparkleShare";

                // Prefer the "SparkleShare" branch if it exists
                var git_show_ref = new GitCommand (TargetFolder, "show-ref --verify --quiet refs/heads/" + prefered_branch);
                git_show_ref.StartAndWaitForExit ();

                if (git_show_ref.ExitCode == 0)
                    branch = prefered_branch;

                var git_checkout = new GitCommand (TargetFolder, "checkout --quiet " + branch);
                git_checkout.StartAndWaitForExit ();
            }

            base.Complete ();
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

            if (InstallationInfo.Platform == PlatformID.Win32NT)
                settings [0] = "core.autocrlf true";

            foreach (string setting in settings) {
                var git_config = new GitCommand (TargetFolder, "config " + setting);
                git_config.StartAndWaitForExit ();
            }
        }


        public override void EnableFetchedRepoCrypto (string password)
        {
            var git_config_smudge = new GitCommand (TargetFolder,
                "config filter.encryption.smudge \"openssl enc -d -aes-256-cbc -base64" + " " +
                "-S " + password.SHA256 (password_salt).Substring (0, 16) + " " +
                "-pass file:.git/info/encryption_password\"");

            var git_config_clean = new GitCommand (TargetFolder,
                "config filter.encryption.clean  \"openssl enc -e -aes-256-cbc -base64" + " " +
                "-S " + password.SHA256 (password_salt).Substring (0, 16) + " " +
                "-pass file:.git/info/encryption_password\"");

            git_config_smudge.StartAndWaitForExit ();
            git_config_clean.StartAndWaitForExit ();

            // Pass all files through the encryption filter
            // TODO: diff=encryption merge=encryption -text?
            string git_attributes_file_path = Path.Combine (TargetFolder, ".git", "info", "attributes");
            File.WriteAllText (git_attributes_file_path, "* filter=encryption");

            // Store the password
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

            string args = "enc -d -aes-256-cbc -base64 -salt -pass pass:" + password.SHA256 (password_salt) + " " + 
                "-in \"" + password_check_file_path + "\"";

            var process = new Command ("openssl", args);
            process.StartInfo.WorkingDirectory = TargetFolder;

            process.StartAndWaitForExit ();

            if (process.ExitCode == 0) {
                File.Delete (password_check_file_path);
                return true;
            }

            return false;
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
            string attribute_rules_file_path = Path.Combine (TargetFolder, ".git", "info", "attributes");
            TextWriter writer = new StreamWriter (attribute_rules_file_path);

            // Compile a list of files we don't want Git to compress. Not compressing
            // already compressed files decreases memory usage and increases speed
            string [] extensions = {
                "jpg", "jpeg", "png", "tiff", "gif", // Images
                "flac", "mp3", "ogg", "oga", // Audio
                "avi", "mov", "mpg", "mpeg", "mkv", "ogv", "ogx", "webm", // Video
                "zip", "gz", "bz", "bz2", "rpm", "deb", "tgz", "rar", "ace", "7z", "pak", "tc", "iso", ".dmg" // Archives
            };

            foreach (string extension in extensions) {
                writer.WriteLine ("*." + extension + " -delta");
                writer.WriteLine ("*." + extension.ToUpper () + " -delta");
            }

            writer.WriteLine ("*.txt text");
            writer.WriteLine ("*.TXT text");
            writer.Close ();
        }


        void InstallGitLFS ()
        {
            var git_lfs = new GitCommand (TargetFolder, "lfs install --local");
            git_lfs.StartAndWaitForExit ();
        }


        void EnableGitLFS ()
        {
            string git_attributes_file_path = Path.Combine (TargetFolder, ".gitattributes");
            File.WriteAllText (git_attributes_file_path, "* filter=lfs diff=lfs merge=lfs -text");
        }
    }
}
