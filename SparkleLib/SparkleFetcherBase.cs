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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public abstract class SparkleFetcherBase {

        public event Action Started = delegate { };
        public event Action Failed = delegate { };

        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler (bool repo_is_encrypted, bool repo_is_empty, string [] warnings);

        public event ProgressChangedEventHandler ProgressChanged = delegate { };
        public delegate void ProgressChangedEventHandler (double percentage);


        public abstract bool Fetch ();
        public abstract void Stop ();
        public abstract bool IsFetchedRepoEmpty { get; }
        public abstract bool IsFetchedRepoPasswordCorrect (string password);
        public abstract void EnableFetchedRepoCrypto (string password);

        public Uri RemoteUrl { get; protected set; }
        public string RequiredFingerprint { get; protected set; }
        public readonly bool FetchPriorHistory = false;
        public string TargetFolder { get; protected set; }
        public bool IsActive { get; private set; }
        public string Identifier;

        public string [] Warnings {
            get {
                return this.warnings.ToArray ();
            }
        }

        public string [] Errors {
            get {
                return this.errors.ToArray ();
            }
        }

        
        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();

        protected string [] ExcludeRules = new string [] {
            "*.autosave", // Various autosaving apps
            "*~", // gedit and emacs
            ".~lock.*", // LibreOffice
            "*.part", "*.crdownload", // Firefox and Chromium temporary download files
            ".*.sw[a-z]", "*.un~", "*.swp", "*.swo", // vi(m)
            ".directory", // KDE
            ".DS_Store", "Icon\r\r", "._*", ".Spotlight-V100", ".Trashes", // Mac OS X
            "*(Autosaved).graffle", // Omnigraffle
            "Thumbs.db", "Desktop.ini", // Windows
            "~*.tmp", "~*.TMP", "*~*.tmp", "*~*.TMP", // MS Office
            "~*.ppt", "~*.PPT", "~*.pptx", "~*.PPTX",
            "~*.xls", "~*.XLS", "~*.xlsx", "~*.XLSX",
            "~*.doc", "~*.DOC", "~*.docx", "~*.DOCX",
            "*/CVS/*", ".cvsignore", "*/.cvsignore", // CVS
            "/.svn/*", "*/.svn/*", // Subversion
            "/.hg/*", "*/.hg/*", "*/.hgignore", // Mercurial
            "/.bzr/*", "*/.bzr/*", "*/.bzrignore" // Bazaar
        };


        private Thread thread;


        public SparkleFetcherBase (string server, string required_fingerprint,
            string remote_path, string target_folder, bool fetch_prior_history)
        {
            RequiredFingerprint = required_fingerprint;
            FetchPriorHistory   = fetch_prior_history;
            remote_path         = remote_path.Trim ("/".ToCharArray ());

            if (server.EndsWith ("/"))
                server = server.Substring (0, server.Length - 1);

            if (!remote_path.StartsWith ("/"))
                remote_path = "/" + remote_path;

            if (!server.Contains ("://"))
                server = "ssh://" + server;

            TargetFolder = target_folder;
            RemoteUrl    = new Uri (server + remote_path);
            IsActive     = false;
        }


        public void Start ()
        {
            IsActive = true;
            Started ();

            SparkleLogger.LogInfo ("Fetcher", TargetFolder + " | Fetching folder: " + RemoteUrl);

            if (Directory.Exists (TargetFolder))
                Directory.Delete (TargetFolder, true);

            string host     = RemoteUrl.Host;
            string host_key = GetHostKey ();

            if (string.IsNullOrEmpty (host) || host_key == null) {
                Failed ();
                return;
            }

            bool warn = true;
            if (RequiredFingerprint != null) {
                string host_fingerprint = GetFingerprint (host_key);

                if (host_fingerprint == null || !RequiredFingerprint.Equals (host_fingerprint)) {
                    SparkleLogger.LogInfo ("Auth", "Fingerprint doesn't match");

                    this.errors.Add ("error: Host fingerprint doesn't match");
                    Failed ();

                    return;
                }

                warn = false;
                SparkleLogger.LogInfo ("Auth", "Fingerprint matches");

            } else {
               SparkleLogger.LogInfo ("Auth", "Skipping fingerprint check");
            }

            AcceptHostKey (host_key, warn);

            this.thread = new Thread (() => {
                if (Fetch ()) {
                    Thread.Sleep (500);
                    SparkleLogger.LogInfo ("Fetcher", "Finished");

                    IsActive = false;

                    // TODO: Find better way to determine if folder should have crypto setup
                    bool repo_is_encrypted = RemoteUrl.ToString ().Contains ("crypto");
                    Finished (repo_is_encrypted, IsFetchedRepoEmpty, Warnings);

                } else {
                    Thread.Sleep (500);
                    SparkleLogger.LogInfo ("Fetcher", "Failed");

                    IsActive = false;
                    Failed ();
                }
            });

            this.thread.Start ();
        }


        public virtual void Complete ()
        {
            string identifier_path = Path.Combine (TargetFolder, ".sparkleshare");

            if (File.Exists (identifier_path)) {
                Identifier = File.ReadAllText (identifier_path).Trim ();

            } else {
                Identifier = CreateIdentifier ();
                File.WriteAllText (identifier_path, Identifier);
            }

            if (IsFetchedRepoEmpty)
                CreateInitialChangeSet ();
        }


        // Create an initial change set when the
        // user has fetched an empty remote folder
        public void CreateInitialChangeSet ()
        {
            string file_path = Path.Combine (TargetFolder, "SparkleShare.txt");
            string n = Environment.NewLine;

            string text = "Congratulations, you've successfully created a SparkleShare repository!" + n +
                n +
                "Any files you add or change in this folder will be automatically synced to " + n +
                RemoteUrl + " and everyone connected to it." + n +
                n +
                "SparkleShare is an Open Source software program that helps people " + n +
                "collaborate and share files. If you like what we do, please consider a small " + n +
                "donation to support the project: http://sparkleshare.org/support-us/" + n +
                n +
                "Have fun! :)" + n;

            File.WriteAllText (file_path, text);
        }


        public static string CreateIdentifier ()
        {
            string random = Path.GetRandomFileName ();
            return random.SHA1 ();
        }


        public static string GetBackend (string path)
        {
            string extension = Path.GetExtension (path);

            if (!string.IsNullOrEmpty (extension)) {
                extension       = extension.Substring (1);
                char [] letters = extension.ToCharArray ();
                letters [0]     = char.ToUpper (letters [0]);

                return new string (letters);

            } else {
                return "Git";
            }
        }


        public void Dispose ()
        {
            if (this.thread != null) {
                this.thread.Abort ();
                this.thread.Join ();
            }
        }


        protected void OnProgressChanged (double percentage) {
            ProgressChanged (percentage);
        }


        private string GetHostKey ()
        {
            string host = RemoteUrl.Host;
            SparkleLogger.LogInfo ("Auth", "Fetching host key for " + host);

            Process process = new Process () {
                EnableRaisingEvents = true
            };

            process.StartInfo.FileName               = "ssh-keyscan";
            process.StartInfo.Arguments              = "-t rsa " + host;
            process.StartInfo.WorkingDirectory       = SparkleConfig.DefaultConfig.TmpPath;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;

            process.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string host_key = process.StandardOutput.ReadToEnd ().Trim ();
            process.WaitForExit ();

            if (process.ExitCode == 0)
                return host_key;
            else
                return null;
        }


        private string GetFingerprint (string public_key)
        {
            try {
                MD5 md5            = new MD5CryptoServiceProvider ();
                string key         = public_key.Split (" ".ToCharArray ()) [2];
                byte [] b64_bytes  = Convert.FromBase64String (key);
                byte [] md5_bytes  = md5.ComputeHash (b64_bytes);
                string fingerprint = BitConverter.ToString (md5_bytes);

                return fingerprint.ToLower ().Replace ("-", ":");

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Fetcher", "Failed creating fingerprint: " + e.Message + e.StackTrace);
                return null;
            }
        }


        private void AcceptHostKey (string host_key, bool warn)
        {
            string ssh_config_path       = Path.Combine (SparkleConfig.DefaultConfig.HomePath, ".ssh");
            string known_hosts_file_path = Path.Combine (ssh_config_path, "known_hosts");

            if (!File.Exists (known_hosts_file_path)) {
                if (!Directory.Exists (ssh_config_path))
                    Directory.CreateDirectory (ssh_config_path);

                File.Create (known_hosts_file_path).Close ();
            }

            string host                 = RemoteUrl.Host;
            string known_hosts          = File.ReadAllText (known_hosts_file_path);
            string [] known_hosts_lines = File.ReadAllLines (known_hosts_file_path);

            foreach (string line in known_hosts_lines) {
                if (line.StartsWith (host + " "))
                    return;
            }

            if (known_hosts.EndsWith ("\n"))
                File.AppendAllText (known_hosts_file_path, host_key + "\n");
            else
                File.AppendAllText (known_hosts_file_path, "\n" + host_key + "\n");

            SparkleLogger.LogInfo ("Auth", "Accepted host key for " + host);

            if (warn)
                this.warnings.Add ("The following host key has been accepted:\n" + GetFingerprint (host_key));
        }
    }
}
