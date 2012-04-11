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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public abstract class SparkleFetcherBase {

        public delegate void StartedEventHandler ();
        public delegate void FinishedEventHandler (string [] warnings);
        public delegate void FailedEventHandler ();
        public delegate void ProgressChangedEventHandler (double percentage);

        public event StartedEventHandler Started;
        public event FinishedEventHandler Finished;
        public event FailedEventHandler Failed;
        public event ProgressChangedEventHandler ProgressChanged;

        public abstract bool Fetch ();
        public abstract void Stop ();

        public string TargetFolder;
        public Uri RemoteUrl;
        public string [] ExcludeRules;
        public string [] Warnings;
        public bool IsActive { get; private set; }

        private Thread thread;


        public SparkleFetcherBase (string server, string remote_folder,
            string target_folder, bool fetch_prior_history)
        {
            TargetFolder = target_folder;
            RemoteUrl    = new Uri (server + "/" + remote_folder);
            IsActive       = false;

            ExcludeRules = new string [] {
                // gedit and emacs
                "*~",

                // LibreOffice
                ".~lock.*",

                // Firefox and Chromium temporary download files
                "*.part",
                "*.crdownload",

                // vi(m)
                ".*.sw[a-z]",
                "*.un~",
                "*.swp",
                "*.swo",

                // KDE
                ".directory",

                // Mac OS X
                ".DS_Store",
                "Icon?",
                "._*",
                ".Spotlight-V100",
                ".Trashes",

                // Omnigraffle
                "*(Autosaved).graffle",

                // Windows
                "Thumbs.db",
                "Desktop.ini",

                // MS Office
                "~*.tmp",
                "~*.TMP",
                "*~*.tmp",
                "*~*.TMP",
                "~*.ppt",
                "~*.PPT",
                "~*.pptx",
                "~*.PPTX",
                "~*.xls",
                "~*.XLS",
                "~*.xlsx",
                "~*.XLSX",
                "~*.doc",
                "~*.DOC",
                "~*.docx",
                "~*.DOCX",

                // CVS
                "*/CVS/*",
                ".cvsignore",
                "*/.cvsignore",

                // Subversion
                "/.svn/*",
                "*/.svn/*",

                // Mercurial
                "/.hg/*",
                "*/.hg/*",
                "*/.hgignore",

                // Bazaar
                "/.bzr/*",
                "*/.bzr/*",
                "*/.bzrignore"
            };
        }


        public void Start ()
        {
            IsActive = true;
            SparkleHelpers.DebugInfo ("Fetcher", "[" + TargetFolder + "] Fetching folder: " + RemoteUrl);

            if (Started != null)
                Started ();

            if (Directory.Exists (TargetFolder))
                Directory.Delete (TargetFolder, true);

            string host = RemoteUrl.Host;

            if (String.IsNullOrEmpty (host)) {
                if (Failed != null)
                    Failed ();

                return;
            }

            string host_key = GetHostKey ();
            if (host_key != null)
                AcceptHostKey (host_key);

            Console.WriteLine (host_key);

            this.thread = new Thread (new ThreadStart (delegate {
                if (Fetch ()) {
                    Thread.Sleep (500);
                    SparkleHelpers.DebugInfo ("Fetcher", "Finished");

                    IsActive = false;

                    if (Finished != null)
                        Finished (Warnings);

                } else {
                    Thread.Sleep (500);
                    SparkleHelpers.DebugInfo ("Fetcher", "Failed");

                    IsActive = false;

                    if (Failed != null)
                        Failed ();
                }
            }));

            this.thread.Start ();
        }


        public void Dispose ()
        {
            if (this.thread != null) {
                this.thread.Abort ();
                this.thread.Join ();
            }
        }

        
        protected void OnProgressChanged (double percentage) {
            if (ProgressChanged != null)
                ProgressChanged (percentage);
        }


        private string GetHostKey ()
        {
            string host = RemoteUrl.Host;
            SparkleHelpers.DebugInfo ("Auth", "Fetching host key for " + host);

            Process process = new Process () {
                EnableRaisingEvents = true
            };

            process.StartInfo.WorkingDirectory       = SparkleConfig.DefaultConfig.TmpPath;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;

            process.StartInfo.FileName  = "ssh-keyscan";
            process.StartInfo.Arguments = "-t rsa " + host;

            process.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string host_key = process.StandardOutput.ReadToEnd ().TrimEnd ();
            process.WaitForExit ();

            if (process.ExitCode == 0)
                return host_key;
            else
                return null;
        }


        private void AcceptHostKey (string host_key)
        {
            string ssh_config_path       = Path.Combine (SparkleConfig.DefaultConfig.HomePath, ".ssh");
            string known_hosts_file_path = Path.Combine (ssh_config_path, "known_hosts");

            if (!File.Exists (known_hosts_file_path)) {
                if (!Directory.Exists (ssh_config_path))
                    Directory.CreateDirectory (ssh_config_path);

                File.Create (known_hosts_file_path).Close ();
            }

            if (File.ReadAllText (known_hosts_file_path).EndsWith ("\n"))
                File.AppendAllText (known_hosts_file_path, host_key + "\n");
            else
                File.AppendAllText (known_hosts_file_path, "\n" + host_key + "\n");

            string host = RemoteUrl.Host;
            SparkleHelpers.DebugInfo ("Auth", "Accepted host key for " + host);
        }
    }
}
