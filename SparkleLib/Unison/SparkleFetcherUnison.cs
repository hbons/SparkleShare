//   SparkleShare, an instant update workflow to Git.
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
    public class SparkleFetcherUnison : SparkleFetcherBase {

        public SparkleFetcherUnison (string remote_url, string target_folder) :
            base (remote_url, target_folder) { }


        public override bool Fetch ()
        {
			//unison needs an extra / to specify the absolute path just like Hg
			//this can't easily be added here
            SparkleUnison unison = new SparkleUnison (SparklePaths.SparkleTmpPath,
                "-auto -batch -contactquietly -ui text \"" + base.target_folder + "\" " + "\"" + base.remote_url + "\"");

            unison.Start ();
            unison.WaitForExit ();

            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());

            if (unison.ExitCode != 0) {
                return false;
            } else {
                InstallConfiguration ();
                InstallExcludeRules ();
				InstallUnisonProfiles ();
                return true;
            }
        }


        // Install the user's name and email and some config into
        // the newly cloned repository
        private void InstallConfiguration ()
        {
            string log_file_path = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "log");
            File.Create (log_file_path);

            string config_file_path = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "config");
            File.Create (config_file_path);

            string config = "";

            // Write the config to the file
            TextWriter writer = new StreamWriter (config_file_path);
            writer.WriteLine (config);
            writer.Close ();

            SparkleHelpers.DebugInfo ("Config", "Added configuration to '" + config_file_path + "'");
        }

		
		//install unison profile
		//need to specify the needed options, the local and server address, the logfile location
		//need to run export UNISON=./.sparkleshare/ on the client so unison looks for the profiles in the .sparkleshare directory
        private void InstallUnisonProfiles ()
        {
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "sparkleshare.prf");
            File.Create (unison_profile);

            // Write the profile to the file
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("root = .");
			writer.WriteLine ("root = " + base.remote_url);
			writer.WriteLine ("log = true");
			writer.WriteLine ("logfile = .sparkleshare/log");
			writer.WriteLine ("contactquietly = true");
			writer.WriteLine ("rsrc = false");
            writer.Close ();
			
			SparkleHelpers.DebugInfo ("Unison Profile", "Added configuration to '" + unison_check_profile + "'");
		
        }		

        // Add a .gitignore file to the repo
        private void InstallExcludeRules ()
        {
            string exlude_rules_file_path = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "exclude");
            File.Create (exlude_rules_file_path);

            TextWriter writer = new StreamWriter (exlude_rules_file_path);

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

    public class SparkleUnison : Process {

        public SparkleUnison (string path, string args) : base ()
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
