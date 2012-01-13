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

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public class SparkleFetcherGit : SparkleFetcherBase {

        private SparkleGit git;


        public SparkleFetcherGit (string server, string remote_folder, string target_folder) :
            base (server, remote_folder, target_folder)
        {
            if (server.EndsWith ("/"))
                server = server.Substring (0, server.Length - 1);

            if (!remote_folder.StartsWith ("/"))
                remote_folder = "/" + remote_folder;


            Uri uri;

            try {
                uri = new Uri (server + remote_folder);

            } catch (UriFormatException) {
                uri = new Uri ("ssh://" + server + remote_folder);
            }


            if (!uri.Scheme.Equals ("ssh") &&
                !uri.Scheme.Equals ("git")) {

                uri = new Uri ("ssh://" + server);
            }


            if (uri.Host.Equals ("gitorious.org")) {
                if (!uri.AbsolutePath.Equals ("/") &&
                    !uri.AbsolutePath.EndsWith (".git")) {

                    uri = new Uri ("ssh://git@gitorious.org" + uri.AbsolutePath + ".git");

                } else {
                    uri = new Uri ("ssh://git@gitorious.org" + uri.AbsolutePath);
                }

            } else if (uri.Host.Equals ("github.com")) {
                uri = new Uri ("ssh://git@github.com" + uri.AbsolutePath);

            } else if (uri.Host.Equals ("gnome.org")) {
                uri = new Uri ("ssh://git@gnome.org/git" + uri.AbsolutePath);

            } else {
                if (string.IsNullOrEmpty (uri.UserInfo))
                    uri = new Uri (uri.Scheme + "://git@" + uri.Host + uri.AbsolutePath);
            }


            base.target_folder = target_folder;
            base.remote_url    = uri.ToString ();
        }


        public override bool Fetch ()
        {
            this.git = new SparkleGit (SparkleConfig.DefaultConfig.TmpPath,
                "clone " +
                "--progress " + // Redirects progress stats to standarderror
                "\"" + base.remote_url + "\" " + "\"" + base.target_folder + "\"");
            
            this.git.StartInfo.RedirectStandardError = true;
            this.git.Start ();

            double percentage = 1.0;
            Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);

            DateTime last_change     = DateTime.Now;
            TimeSpan change_interval = new TimeSpan (0, 0, 0, 1);

            while (!this.git.StandardError.EndOfStream) {
                string line = this.git.StandardError.ReadLine ();
                Match match = progress_regex.Match (line);
                
                double number = 0.0;
                if (match.Success) {
                    number = double.Parse (match.Groups [1].Value);
                    
                    // The cloning progress consists of two stages: the "Compressing 
                    // objects" stage which we count as 20% of the total progress, and 
                    // the "Receiving objects" stage which we count as the last 80%
                    if (line.Contains ("|"))
                        // "Receiving objects" stage
                        number = (number / 100 * 80 + 20);
                    else
                        // "Compressing objects" stage
                        number = (number / 100 * 20);
                }
                
                if (number >= percentage) {
                    percentage = number;

                    if (DateTime.Compare (last_change, DateTime.Now.Subtract (change_interval)) < 0) {
                        base.OnProgressChanged (percentage);
                        last_change = DateTime.Now;
                    }
                }
            }
            
            this.git.WaitForExit ();
            SparkleHelpers.DebugInfo ("Git", "Exit code " + this.git.ExitCode.ToString ());


            if (this.git.ExitCode != 0) {
                return false;

            } else {
                InstallConfiguration ();
                InstallExcludeRules ();
                return true;
            }
        }


        public override string [] Warnings {
            get {
                SparkleGit git = new SparkleGit (SparkleConfig.DefaultConfig.TmpPath,
                    "config --global core.excludesfile");

                git.Start ();

                // Reading the standard output HAS to go before
                // WaitForExit, or it will hang forever on output > 4096 bytes
                string output = git.StandardOutput.ReadToEnd ().Trim ();
                git.WaitForExit ();

                if (string.IsNullOrEmpty (output)) {
                    return null;

                } else {
                    return new string [] {
                        string.Format ("You seem to have configured a system ‘gitignore’ file. " +
                                       "This may interfere with SparkleShare.\n({0})", output)
                    };
                }
            }
        }

        public override void Stop ()
        {
            if (this.git != null && !this.git.HasExited) {
                this.git.Kill ();
                this.git.Dispose ();
            }

            base.Stop ();
        }


        // Install the user's name and email and some config into
        // the newly cloned repository
        private void InstallConfiguration ()
        {
            string repo_config_file_path = SparkleHelpers.CombineMore (base.target_folder, ".git", "config");
            string config = String.Join (Environment.NewLine, File.ReadAllLines (repo_config_file_path));

            string n = Environment.NewLine;

            // Show special characters in the logs
            config = config.Replace ("[core]" + n,
                "[core]" + n + "quotepath = false" + n);

            // Be case sensitive explicitly to work on Mac
            config = config.Replace ("ignorecase = true", "ignorecase = false");

            // Ignore permission changes
            config = config.Replace ("filemode = true", "filemode = false");

            // Write the config to the file
            TextWriter writer = new StreamWriter (repo_config_file_path);
            writer.WriteLine (config);
            writer.Close ();

            SparkleHelpers.DebugInfo ("Config", "Added configuration to '" + repo_config_file_path + "'");
        }


        // Add a .gitignore file to the repo
        private void InstallExcludeRules ()
        {
            DirectoryInfo info = Directory.CreateDirectory (
                SparkleHelpers.CombineMore (this.target_folder, ".git", "info"));

            // File that lists the files we want git to ignore
            string exlude_rules_file_path = Path.Combine (info.FullName, "exclude");
            TextWriter writer = new StreamWriter (exlude_rules_file_path);

                // gedit and emacs
                writer.WriteLine ("*~");

                // Firefox and Chromium temporary download files
                writer.WriteLine ("*.part");
                writer.WriteLine ("*.crdownload");

                // vi(m)
                writer.WriteLine (".*.sw[a-z]");
                writer.WriteLine ("*.un~");
                writer.WriteLine ("*.swp");
                writer.WriteLine ("*.swo");
                
                // KDE
                writer.WriteLine (".directory");
    
                // Mac OS X
                writer.WriteLine (".DS_Store");
                writer.WriteLine ("Icon?");
                writer.WriteLine ("._*");
                writer.WriteLine (".Spotlight-V100");
                writer.WriteLine (".Trashes");

                // Omnigraffle
                writer.WriteLine ("*(Autosaved).graffle");
            
                // Windows
                writer.WriteLine ("Thumbs.db");
                writer.WriteLine ("Desktop.ini");

                // MS Office
                writer.WriteLine ("~*.tmp");
                writer.WriteLine ("~*.TMP");
                writer.WriteLine ("*~*.tmp");
                writer.WriteLine ("*~*.TMP");
                writer.WriteLine ("~*.ppt");
                writer.WriteLine ("~*.PPT");
                writer.WriteLine ("~*.pptx");
                writer.WriteLine ("~*.PPTX");
                writer.WriteLine ("~*.xls");
                writer.WriteLine ("~*.XLS");
                writer.WriteLine ("~*.xlsx");
                writer.WriteLine ("~*.XLSX");
                writer.WriteLine ("~*.doc");
                writer.WriteLine ("~*.DOC");
                writer.WriteLine ("~*.docx");
                writer.WriteLine ("~*.DOCX");

                // CVS
                writer.WriteLine ("*/CVS/*");
                writer.WriteLine (".cvsignore");
                writer.WriteLine ("*/.cvsignore");
                
                // Subversion
                writer.WriteLine ("/.svn/*");
                writer.WriteLine ("*/.svn/*");

            writer.Close ();


            // File that lists the files we want don't want git to compress.
            // Not compressing the already compressed files saves us memory
            // usage and increases speed
            string no_compression_rules_file_path = Path.Combine (info.FullName, "attributes");
            writer = new StreamWriter (no_compression_rules_file_path);

                // Images
                writer.WriteLine ("*.jpg -delta");
                writer.WriteLine ("*.jpeg -delta");
                writer.WriteLine ("*.JPG -delta");
                writer.WriteLine ("*.JPEG -delta");

                writer.WriteLine ("*.png -delta");
                writer.WriteLine ("*.PNG -delta");

                writer.WriteLine ("*.tiff -delta");
                writer.WriteLine ("*.TIFF -delta");

                // Audio
                writer.WriteLine ("*.flac -delta");
                writer.WriteLine ("*.FLAC -delta");

                writer.WriteLine ("*.mp3 -delta");
                writer.WriteLine ("*.MP3 -delta");

                writer.WriteLine ("*.ogg -delta");
                writer.WriteLine ("*.OGG -delta");

                writer.WriteLine ("*.oga -delta");
                writer.WriteLine ("*.OGA -delta");

                // Video
                writer.WriteLine ("*.avi -delta");
                writer.WriteLine ("*.AVI -delta");

                writer.WriteLine ("*.mov -delta");
                writer.WriteLine ("*.MOV -delta");

                writer.WriteLine ("*.mpg -delta");
                writer.WriteLine ("*.MPG -delta");
                writer.WriteLine ("*.mpeg -delta");
                writer.WriteLine ("*.MPEG -delta");

                writer.WriteLine ("*.mkv -delta");
                writer.WriteLine ("*.MKV -delta");

                writer.WriteLine ("*.ogv -delta");
                writer.WriteLine ("*.OGV -delta");

                writer.WriteLine ("*.ogx -delta");
                writer.WriteLine ("*.OGX -delta");

                writer.WriteLine ("*.webm -delta");
                writer.WriteLine ("*.WEBM -delta");

                // Archives
                writer.WriteLine ("*.zip -delta");
                writer.WriteLine ("*.ZIP -delta");

                writer.WriteLine ("*.gz -delta");
                writer.WriteLine ("*.GZ -delta");

                writer.WriteLine ("*.bz -delta");
                writer.WriteLine ("*.BZ -delta");

                writer.WriteLine ("*.bz2 -delta");
                writer.WriteLine ("*.BZ2 -delta");

                writer.WriteLine ("*.rpm -delta");
                writer.WriteLine ("*.RPM -delta");

                writer.WriteLine ("*.deb -delta");
                writer.WriteLine ("*.DEB -delta");

                writer.WriteLine ("*.tgz -delta");
                writer.WriteLine ("*.TGZ -delta");

                writer.WriteLine ("*.rar -delta");
                writer.WriteLine ("*.RAR -delta");

                writer.WriteLine ("*.ace -delta");
                writer.WriteLine ("*.ACE -delta");

                writer.WriteLine ("*.7z -delta");
                writer.WriteLine ("*.7Z -delta");

                writer.WriteLine ("*.pak -delta");
                writer.WriteLine ("*.PAK -delta");

                writer.WriteLine ("*.tar -delta");
                writer.WriteLine ("*.TAR -delta");

            writer.Close ();
        }
    }


    public class SparkleGit : Process {

        public SparkleGit (string path, string args) : base ()
        {
            EnableRaisingEvents              = true;
            StartInfo.FileName               = SparkleBackend.DefaultBackend.Path;
            StartInfo.Arguments              = args;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.WorkingDirectory       = path;
        }


        new public void Start ()
        {
            SparkleHelpers.DebugInfo ("Cmd", StartInfo.FileName + " " + StartInfo.Arguments);
            base.Start ();
        }
    }
}
