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
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

#if __MonoCS__
using Mono.Unix;
#endif

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public abstract class SparkleFetcherBase {

        public delegate void StartedEventHandler ();
        public delegate void FinishedEventHandler ();
        public delegate void FailedEventHandler ();
        public delegate void ProgressChangedEventHandler (double percentage);

        public event StartedEventHandler Started;
        public event FinishedEventHandler Finished;
        public event FailedEventHandler Failed;
        public event ProgressChangedEventHandler ProgressChanged;

        protected string target_folder;
        protected string remote_url;

        private Thread thread;

        
        public SparkleFetcherBase (string server, string remote_folder, string target_folder)
        {
            this.target_folder = target_folder;
            this.remote_url    = server + "/" + remote_folder;
        }


        public abstract bool Fetch ();


        // Clones the remote repository
        public void Start ()
        {
            SparkleHelpers.DebugInfo ("Fetcher", "[" + this.target_folder + "] Fetching folder: " + this.remote_url);

            if (Started != null)
                Started ();

            if (Directory.Exists (this.target_folder))
                Directory.Delete (this.target_folder, true);

            string host = GetHost (this.remote_url);

            if (String.IsNullOrEmpty (host)) {
                if (Failed != null)
                    Failed ();

                return;
            }

            DisableHostKeyCheckingForHost (host);

            this.thread = new Thread (new ThreadStart (delegate {
                if (Fetch ()) {
                    SparkleHelpers.DebugInfo ("Fetcher", "Finished");

                    EnableHostKeyCheckingForHost (host);

                    if (Finished != null)
                        Finished ();

                } else {
                    SparkleHelpers.DebugInfo ("Fetcher", "Failed");

                    EnableHostKeyCheckingForHost (host);

                    if (Failed != null)
                        Failed ();
                }
            }));

            this.thread.Start ();
        }


        public virtual void Stop ()
        {
            this.thread.Abort ();
            this.thread.Join ();
        }


        public string RemoteUrl {
            get {
                return this.remote_url;
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
            string ssh_config           = Environment.NewLine + "# <SparkleShare>" +
                                          Environment.NewLine + "Host " + host +
                                          Environment.NewLine + "\tStrictHostKeyChecking no" +
                                          Environment.NewLine + "# </SparkleShare>";

            if (!Directory.Exists (ssh_config_path))
                Directory.CreateDirectory (ssh_config_path);

            if (File.Exists (ssh_config_file_path)) {
                TextWriter writer = File.AppendText (ssh_config_file_path);
                writer.WriteLine (ssh_config);
                writer.Close ();

            } else {
                File.WriteAllText (ssh_config_file_path, ssh_config);
            }

#if __MonoCS__
            UnixFileSystemInfo file_info = new UnixFileInfo (ssh_config_file_path);
                file_info.FileAccessPermissions = (FileAccessPermissions.UserRead |
                                               FileAccessPermissions.UserWrite);
#endif

            SparkleHelpers.DebugInfo ("Fetcher", "Disabled host key checking " + host);
        }
        

        private void EnableHostKeyCheckingForHost (string host)
        {
            string path = SparkleConfig.DefaultConfig.HomePath;

            if (!(SparkleBackend.Platform == PlatformID.Unix ||
                  SparkleBackend.Platform == PlatformID.MacOSX)) {

                path = Environment.ExpandEnvironmentVariables ("%HOMEDRIVE%%HOMEPATH%");
            }

            string ssh_config_file_path = SparkleHelpers.CombineMore (path, ".ssh", "config");

            if (File.Exists (ssh_config_file_path)) {
                string current_ssh_config = File.ReadAllText (ssh_config_file_path);

                current_ssh_config = current_ssh_config.Trim ();
                string [] lines = current_ssh_config.Split (Environment.NewLine.ToCharArray ());
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

                    new_ssh_config += line + Environment.NewLine;
                }

                if (string.IsNullOrWhiteSpace (new_ssh_config)) {
                    File.Delete (ssh_config_file_path);

                } else {
                    File.WriteAllText (ssh_config_file_path, new_ssh_config.Trim ());

#if __MonoCS__
                        UnixFileSystemInfo file_info = new UnixFileInfo (ssh_config_file_path);
                        file_info.FileAccessPermissions = (FileAccessPermissions.UserRead |
                                                           FileAccessPermissions.UserWrite);
#endif
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
    }
}
