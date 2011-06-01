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
using System.Xml;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public class SparkleFetcherHg : SparkleFetcherBase {

        public SparkleFetcherHg (string server, string remote_folder, string target_folder) :
            base (server, remote_folder, target_folder) { }


        public override bool Fetch ()
        {
            SparkleHg hg = new SparkleHg (SparklePaths.SparkleTmpPath,
                "clone \"" + base.remote_url + "\" " + "\"" + base.target_folder + "\"");

            hg.Start ();
            hg.WaitForExit ();

            SparkleHelpers.DebugInfo ("Hg", "Exit code " + hg.ExitCode.ToString ());

            if (hg.ExitCode != 0) {
                return false;
            } else {
                InstallConfiguration ();
                InstallExcludeRules ();
                return true;
            }
        }


        // Install the user's name and email and some config into
        // the newly cloned repository
        private void InstallConfiguration ()
        {
            string global_config_file_path = Path.Combine (SparklePaths.SparkleConfigPath, "config.xml");

            if (!File.Exists (global_config_file_path))
                return;

            string repo_config_file_path = SparkleHelpers.CombineMore (base.target_folder, ".hg", "hgrc");
            string config = String.Join (Environment.NewLine, File.ReadAllLines (repo_config_file_path));

            // Add user info
            string n        = Environment.NewLine;
            XmlDocument xml = new XmlDocument();
            xml.Load (global_config_file_path);

            XmlNode node_name  = xml.SelectSingleNode ("//user/name/text()");
            XmlNode node_email = xml.SelectSingleNode ("//user/email/text()");

             // TODO this ignore duplicate names (FolderName (2))
            string ignore_file_path = base.target_folder.Replace (SparklePaths.SparkleTmpPath,
                SparklePaths.SparklePath);

            ignore_file_path = SparkleHelpers.CombineMore (ignore_file_path, ".hg", "hgignore");

            config += n +
                      "[ui]" + n +
                      "username = " + node_name.Value + " <" + node_email.Value + ">" + n +
                      "ignore = " + ignore_file_path + n;

            // Write the config to the file
            TextWriter writer = new StreamWriter (repo_config_file_path);
            writer.WriteLine (config);
            writer.Close ();

            string style_file_path = SparkleHelpers.CombineMore (base.target_folder, ".hg", "log.style");

            string style = "changeset = \"{file_mods}{file_adds}{file_dels}\"" + n +
                           "file_add  = \"A {file_add}\\n\"" + n +
                           "file_mod  = \"M {file_mod}\\n\"" + n +
                           "file_del  = \"D {file_del}\\n\"" + n;

            writer = new StreamWriter (style_file_path);
            writer.WriteLine (style);
            writer.Close ();

            SparkleHelpers.DebugInfo ("Config", "Added configuration to '" + repo_config_file_path + "'");
        }


        // Add a .gitignore file to the repo
        private void InstallExcludeRules ()
        {
            string exlude_rules_file_path = SparkleHelpers.CombineMore (
                this.target_folder, ".hg", "hgignore");

            TextWriter writer = new StreamWriter (exlude_rules_file_path);

                 writer.WriteLine ("syntax: glob");

                // gedit and emacs
                writer.WriteLine ("*~");

                // vi(m)
                writer.WriteLine (".*.sw[a-z]");
                writer.WriteLine ("*.un~");
                writer.WriteLine ("*.swp");
                writer.WriteLine ("*.swo");
                
                // KDE
                writer.WriteLine (".directory");
    
                // Mac OSX
                writer.WriteLine (".DS_Store");
                writer.WriteLine ("Icon?");
                writer.WriteLine ("._*");
                writer.WriteLine (".Spotlight-V100");
                writer.WriteLine (".Trashes");

                // Mac OSX
                writer.WriteLine ("*(Autosaved).graffle");
            
                // Windows
                writer.WriteLine ("Thumbs.db");
                writer.WriteLine ("Desktop.ini");

                // CVS
                writer.WriteLine ("*/CVS/*");
                writer.WriteLine (".cvsignore");
                writer.WriteLine ("*/.cvsignore");
                
                // Subversion
                writer.WriteLine ("/.svn/*");
                writer.WriteLine ("*/.svn/*");

            writer.Close ();
        }
    }


    public class SparkleHg : Process {

        public SparkleHg (string path, string args) : base ()
        {
            EnableRaisingEvents              = true;
            StartInfo.FileName               = "/opt/local/bin/hg";
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
