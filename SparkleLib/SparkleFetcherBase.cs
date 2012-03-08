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
        public string RemoteUrl;
        public string [] ExcludeRules;
        public string [] Warnings;
        public bool IsActive { get; private set; }

        private Thread thread;


        public SparkleFetcherBase (string server, string remote_folder, string target_folder)
        {
            TargetFolder = target_folder;
            RemoteUrl    = server + "/" + remote_folder;
            IsActive       = false;

            ExcludeRules = new string [] {
                // gedit and emacs
                "*~",

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


        // Clones the remote repository
        public void Start ()
        {
            IsActive = true;
            SparkleHelpers.DebugInfo ("Fetcher", "[" + TargetFolder + "] Fetching folder: " + RemoteUrl);

            if (Started != null)
                Started ();

            if (Directory.Exists (TargetFolder))
                Directory.Delete (TargetFolder, true);

            string host = GetHost (RemoteUrl);

            if (String.IsNullOrEmpty (host)) {
                if (Failed != null)
                    Failed ();

                return;
            }

            DisableHostKeyCheckingForHost (host);

            this.thread = new Thread (new ThreadStart (delegate {
                if (Fetch ()) {
                    Thread.Sleep (500);
                    SparkleHelpers.DebugInfo ("Fetcher", "Finished");

                    EnableHostKeyCheckingForHost (host);
                    IsActive = false;

                    if (Finished != null)
                        Finished (Warnings);

                } else {
                    Thread.Sleep (500);
                    SparkleHelpers.DebugInfo ("Fetcher", "Failed");

                    EnableHostKeyCheckingForHost (host);
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
    
        
        private void DisableHostKeyCheckingForHost (string host)
        {
            string path = SparkleConfig.DefaultConfig.HomePath;

            if (!(SparkleBackend.Platform == PlatformID.Unix ||
                  SparkleBackend.Platform == PlatformID.MacOSX)) {

                path = Environment.ExpandEnvironmentVariables ("%HOMEDRIVE%%HOMEPATH%");
            }

            string ssh_config_path      = Path.Combine (path, ".ssh");
            string ssh_config_file_path = SparkleHelpers.CombineMore (path, ".ssh", "config");
            string ssh_config           = "\n# <SparkleShare>" +
                                          "\nHost " + host +
                                          "\n\tStrictHostKeyChecking no" +
                                          "\n# </SparkleShare>";

            if (!Directory.Exists (ssh_config_path))
                Directory.CreateDirectory (ssh_config_path);

            if (File.Exists (ssh_config_file_path)) {
                TextWriter writer = File.AppendText (ssh_config_file_path);
                writer.Write (ssh_config);
                writer.Close ();

            } else {
                File.WriteAllText (ssh_config_file_path, ssh_config);
            }

            Chmod644 (ssh_config_file_path);
            SparkleHelpers.DebugInfo ("Fetcher", "Disabled host key checking for " + host);
        }
        

        private void EnableHostKeyCheckingForHost (string host)
        {
            string path = SparkleConfig.DefaultConfig.HomePath;

            if (SparkleBackend.Platform != PlatformID.Unix &&
                SparkleBackend.Platform != PlatformID.MacOSX) {

                path = Environment.ExpandEnvironmentVariables ("%HOMEDRIVE%%HOMEPATH%");
            }

            string ssh_config_file_path = SparkleHelpers.CombineMore (path, ".ssh", "config");

            if (File.Exists (ssh_config_file_path)) {
                string current_ssh_config = File.ReadAllText (ssh_config_file_path);

                current_ssh_config = current_ssh_config.Trim ();
                string [] lines = current_ssh_config.Split ('\n');
                string new_ssh_config = "";
                bool in_sparkleshare_section = false;

                foreach (string line in lines) {
                    if (line.StartsWith ("# <SparkleShare>")) {
                        in_sparkleshare_section = true;
                        continue;
                    }

                    if (line.StartsWith ("# </SparkleShare>")) {
                        in_sparkleshare_section = false;
                        continue;
                    }

                    if (in_sparkleshare_section)
                        continue;

                    new_ssh_config += line + "\n"; // do not use Environment.NewLine because file is in unix format
                }

                if (string.IsNullOrEmpty (new_ssh_config.Trim ())) {
                    File.Delete (ssh_config_file_path);

                } else {
                    File.WriteAllText (ssh_config_file_path, new_ssh_config.Trim ());
                    Chmod644 (ssh_config_file_path);
                }
            }

            SparkleHelpers.DebugInfo ("Fetcher", "Enabled host key checking for " + host);
        }


        private string GetHost (string url)
        {
            Regex regex = new Regex (@"(@|://)([a-z0-9\.-]+)(/|:)");
            Match match = regex.Match (url);

            if (match.Success)
                return match.Groups [2].Value;
            else
                return null;
        }
        
        
        private void Chmod644 (string file_path)
        {
            // Hack to be able to set the permissions on a file
            // that OpenSSH still likes without resorting to Mono.Unix
            FileInfo file_info   = new FileInfo (file_path);
            file_info.Attributes = FileAttributes.ReadOnly;
            file_info.Attributes = FileAttributes.Normal;
        }
    }
}
