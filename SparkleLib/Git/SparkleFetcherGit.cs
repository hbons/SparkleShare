//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using SparkleLib;

namespace SparkleLib.Git {

    public class SparkleFetcher : SparkleFetcherSSH {

        private SparkleGit git;
        private bool use_git_bin;
        private string cached_salt;

        private Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);
        private Regex speed_regex    = new Regex (@"([0-9\.]+) ([KM])iB/s", RegexOptions.Compiled);


        private string crypto_salt {
            get {
                if (!string.IsNullOrEmpty (this.cached_salt))
                    return this.cached_salt;

                // Check if the repo's salt is stored in a branch...
                SparkleGit git   = new SparkleGit (TargetFolder, "ls-remote --heads");
                string branches  = git.StartAndReadStandardOutput ();
                Regex salt_regex = new Regex ("refs/heads/salt-([0-9a-f]+)");
				Match salt_match = salt_regex.Match (branches);

				if (salt_match.Success)
					this.cached_salt = salt_match.Groups [1].Value;

                // ...if not, create a new salt for the repo
                if (string.IsNullOrEmpty (this.cached_salt)) {
                    this.cached_salt      = GenerateCryptoSalt ();
                    string salt_file_path = new string [] { TargetFolder, ".git", "salt" }.Combine ();

                    // Temporarily store the salt in a file, so the Repo object can
                    // push it to a branch on the host later
                    File.WriteAllText (salt_file_path, this.cached_salt);
                }

                return this.cached_salt;
            }
        }


        public SparkleFetcher (SparkleFetcherInfo info) : base (info)
        {
            if (RemoteUrl.ToString ().StartsWith ("ssh+"))
                RemoteUrl = new Uri ("ssh" + RemoteUrl.ToString ().Substring (RemoteUrl.ToString ().IndexOf ("://")));

            Uri uri = RemoteUrl;

            if (!uri.Scheme.Equals ("ssh") && !uri.Scheme.Equals ("https") &&
                !uri.Scheme.Equals ("http") && !uri.Scheme.Equals ("git")) {

                uri = new Uri ("ssh://" + uri);
            }

            if (uri.Host.Equals ("gitorious.org") && !uri.Scheme.StartsWith ("http")) {
                if (!uri.AbsolutePath.Equals ("/") &&
                    !uri.AbsolutePath.EndsWith (".git")) {

                    uri = new Uri ("ssh://git@gitorious.org" + uri.AbsolutePath + ".git");

                } else {
                    uri = new Uri ("ssh://git@gitorious.org" + uri.AbsolutePath);
                }

            } else if (uri.Host.Equals ("github.com") && !uri.Scheme.StartsWith ("http")) {
                uri = new Uri ("ssh://git@github.com" + uri.AbsolutePath);

            } else if (uri.Host.Equals ("bitbucket.org") && !uri.Scheme.StartsWith ("http")) {
                // Nothing really

            } else {
                if (string.IsNullOrEmpty (uri.UserInfo) && !uri.Scheme.StartsWith ("http")) {
                    if (uri.Port == -1)
                        uri = new Uri (uri.Scheme + "://storage@" + uri.Host + uri.AbsolutePath);
                    else
                        uri = new Uri (uri.Scheme + "://storage@" + uri.Host + ":" + uri.Port + uri.AbsolutePath);
                }

                this.use_git_bin = false; // TODO
            }

            RemoteUrl = uri;
        }


        public override bool Fetch ()
        {
            if (!base.Fetch ())
                return false;

            if (FetchPriorHistory) {
                this.git = new SparkleGit (SparkleConfig.DefaultConfig.TmpPath,
                    "clone --progress --no-checkout \"" + RemoteUrl + "\" \"" + TargetFolder + "\"");

            } else {
                this.git = new SparkleGit (SparkleConfig.DefaultConfig.TmpPath,
                    "clone --progress --no-checkout --depth=1 \"" + RemoteUrl + "\" \"" + TargetFolder + "\"");
            }

            this.git.StartInfo.RedirectStandardError = true;
            this.git.Start ();

            double percentage = 1.0;

            DateTime last_change     = DateTime.Now;
            TimeSpan change_interval = new TimeSpan (0, 0, 0, 1);

            try {
                while (!this.git.StandardError.EndOfStream) {
                    string line = this.git.StandardError.ReadLine ();
                    Match match = this.progress_regex.Match (line);
                    
                    double number = 0.0;
                    double speed  = 0.0; 
                    if (match.Success) {
                        try {
                            number = double.Parse (match.Groups [1].Value, new CultureInfo ("en-US"));
                            
                        } catch (FormatException) {
                            SparkleLogger.LogInfo ("Git", "Error parsing progress: \"" + match.Groups [1] + "\"");
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
                            Match speed_match = this.speed_regex.Match (line);
                            
                            if (speed_match.Success) {
                                try {
                                    speed = double.Parse (speed_match.Groups [1].Value, new CultureInfo ("en-US")) * 1024;

                                } catch (FormatException) {
                                    SparkleLogger.LogInfo ("Git", "Error parsing speed: \"" + speed_match.Groups [1] + "\"");
                                }
                                
                                if (speed_match.Groups [2].Value.Equals ("M"))
                                    speed = speed * 1024;
                            }    
                        }

                    } else {
                        SparkleLogger.LogInfo ("Fetcher", line);
                        line = line.Trim (new char [] {' ', '@'});

                        if (line.StartsWith ("fatal:", StringComparison.InvariantCultureIgnoreCase) ||
                            line.StartsWith ("error:", StringComparison.InvariantCultureIgnoreCase)) {

                            base.errors.Add (line);

                        } else if (line.StartsWith ("WARNING: REMOTE HOST IDENTIFICATION HAS CHANGED!")) {
                            base.errors.Add ("warning: Remote host identification has changed!");

                        } else if (line.StartsWith ("WARNING: POSSIBLE DNS SPOOFING DETECTED!")) {
                            base.errors.Add ("warning: Possible DNS spoofing detected!");
                        }
                    }

                    if (number >= percentage) {
                        percentage = number;

                        if (DateTime.Compare (last_change, DateTime.Now.Subtract (change_interval)) < 0) {
                            base.OnProgressChanged (percentage, speed);
                            last_change = DateTime.Now;
                        }
                    }
                }
            
            } catch (Exception) {
                IsActive = false;
                return false;
            }

            this.git.WaitForExit ();

            if (this.git.ExitCode == 0) {
                while (percentage < 100) {
                    percentage += 25;

                    if (percentage >= 100)
                        break;

                    Thread.Sleep (500);
                    base.OnProgressChanged (percentage, 0);
                }

                base.OnProgressChanged (100, 0);

                InstallConfiguration ();
                InstallExcludeRules ();
                InstallAttributeRules ();

                return true;

            } else {
                return false;
            }
        }


        public override bool IsFetchedRepoEmpty {
            get {
                SparkleGit git = new SparkleGit (TargetFolder, "rev-parse HEAD");
                git.StartAndWaitForExit ();

                return (git.ExitCode != 0);
            }
        }


        public override void EnableFetchedRepoCrypto (string password)
        {
            // Define the crypto filter in the config
            string repo_config_file_path = new string [] { TargetFolder, ".git", "config" }.Combine ();
            string config = File.ReadAllText (repo_config_file_path);

            string n = Environment.NewLine;

			string salt = this.crypto_salt;

            config += "[filter \"crypto\"]" + n +
                "\tsmudge = openssl enc -d -aes-256-cbc -base64 -S " + salt + " -pass file:.git/password" + n +
                "\tclean  = openssl enc -e -aes-256-cbc -base64 -S " + salt + " -pass file:.git/password" + n;

            File.WriteAllText (repo_config_file_path, config);

            // Pass all files through the crypto filter
            string git_attributes_file_path = new string [] { TargetFolder, ".git", "info", "attributes" }.Combine ();
            File.AppendAllText (git_attributes_file_path, "\n* filter=crypto");

            // Store the password
            string password_file_path = new string [] { TargetFolder, ".git", "password" }.Combine ();
            File.WriteAllText (password_file_path, password.Trim ());
        }


        public override bool IsFetchedRepoPasswordCorrect (string password)
        {
            string password_check_file_path = Path.Combine (TargetFolder, ".sparkleshare");

            if (!File.Exists (password_check_file_path)) {
                SparkleGit git = new SparkleGit (TargetFolder, "show HEAD:.sparkleshare");
                string output = git.StartAndReadStandardOutput ();

                if (git.ExitCode == 0)
                    File.WriteAllText (password_check_file_path, output);
                else
                    return false;
            }

            Process process = new Process ();
            process.EnableRaisingEvents              = true;
            process.StartInfo.WorkingDirectory       = TargetFolder;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;

            process.StartInfo.FileName  = "openssl";
            process.StartInfo.Arguments = "enc -d -aes-256-cbc -base64 -S " + this.crypto_salt +
                " -pass pass:\"" + password + "\" -in \"" + password_check_file_path + "\"";

            SparkleLogger.LogInfo ("Cmd | " + System.IO.Path.GetFileName (process.StartInfo.WorkingDirectory),
                System.IO.Path.GetFileName (process.StartInfo.FileName) + " " + process.StartInfo.Arguments);

            process.Start ();
            process.WaitForExit ();

            if (process.ExitCode == 0) {
                File.Delete (password_check_file_path);
                return true;

            } else {
                return false;
            }
        }


        public override void Stop ()
        {
            try {
                if (this.git != null && !this.git.HasExited) {
                    this.git.Kill ();
                    this.git.Dispose ();
                }

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Fetcher", "Failed to dispose properly", e);
            }

            if (Directory.Exists (TargetFolder)) {
                try {
                    Directory.Delete (TargetFolder, true /* Recursive */ );
                    SparkleLogger.LogInfo ("Fetcher", "Deleted '" + TargetFolder + "'");
                    
                } catch (Exception e) {
                    SparkleLogger.LogInfo ("Fetcher", "Failed to delete '" + TargetFolder + "'", e);
                }
            }
        }


        public override void Complete ()
        {
            if (!IsFetchedRepoEmpty) {
                SparkleGit git = new SparkleGit (TargetFolder, "checkout --quiet HEAD");
                git.StartAndWaitForExit ();
            }

            base.Complete ();
        }


        private void InstallConfiguration ()
        {
            string [] settings = new string [] {
                "core.quotepath false", // Don't quote "unusual" characters in path names
                "core.ignorecase false", // Be case sensitive explicitly to work on Mac
                "core.filemode false", // Ignore permission changes
                "core.autocrlf false", // Don't change file line endings
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

            foreach (string setting in settings) {
                SparkleGit git_config = new SparkleGit (TargetFolder, "config " + setting);
                git_config.StartAndWaitForExit ();
            }

            if (this.use_git_bin)
                InstallGitBinConfiguration ();
        }


        public void InstallGitBinConfiguration ()
        {
            string [] settings = new string [] {
                "core.bigFileThreshold 1024g",
                "filter.bin.clean \"git bin clean %f\"",
                "filter.bin.smudge \"git bin smudge\""
            };

            foreach (string setting in settings) {
                SparkleGit git_config = new SparkleGit (TargetFolder, "config " + setting);
                git_config.StartAndWaitForExit ();
            }
        }


        // Add a .gitignore file to the repo
        private void InstallExcludeRules ()
        {
            string git_info_path = new string [] { TargetFolder, ".git", "info" }.Combine ();

            if (!Directory.Exists (git_info_path))
                Directory.CreateDirectory (git_info_path);

            string exclude_rules           = string.Join (Environment.NewLine, ExcludeRules);
            string exclude_rules_file_path = new string [] { git_info_path, "exclude" }.Combine ();

            File.WriteAllText (exclude_rules_file_path, exclude_rules);
        }


        private void InstallAttributeRules ()
        {
            string attribute_rules_file_path = new string [] { TargetFolder, ".git", "info", "attributes" }.Combine ();
            TextWriter writer                = new StreamWriter (attribute_rules_file_path);

            if (this.use_git_bin) {
                writer.WriteLine ("* filter=bin binary");

            } else {
                // Compile a list of files we don't want Git to compress.
                // Not compressing already compressed files decreases memory usage and increases speed
                string [] extensions = new string [] {
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
            }

            writer.Close ();
        }
    }
}
