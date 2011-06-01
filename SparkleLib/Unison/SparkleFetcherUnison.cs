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

        public SparkleFetcherUnison (string server, string remote_folder, string target_folder) :
            base (server, remote_folder, target_folder) 
		{
		    server = server.TrimEnd ("/".ToCharArray ());

            if (server.StartsWith ("ssh://"))
                server = server.Substring (6);

            if (!server.Contains ("@"))
                server = "git@" + server;

            server = "ssh://" + server;
			
			remote_folder = remote_folder.Trim ("/".ToCharArray ());
			
			base.target_folder = target_folder;
            
			base.remote_url    = server + "//" + remote_folder;
		}


        public override bool Fetch ()
        {
            //not sure where the log file ends up at the moment
			SparkleUnison unison = new SparkleUnison (SparklePaths.SparkleTmpPath,
                "-auto -batch -ui text \"" + base.target_folder + "\" " + "\"" + base.remote_url + "\"");

            unison.Start ();
            unison.WaitForExit ();

            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());

            if (unison.ExitCode != 0) {
                return false;
            } else {
                InstallConfiguration ();
				InstallUnisonProfile (); //ignore list included in the profile
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
        private void InstallUnisonProfile ()
        {
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "sparkleshare.prf");
            File.Create (unison_profile);

            // Write the profile to the file
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("root = ."); //local folder
			writer.WriteLine ("root = " + "\"" + base.remote_url + "\""); //remote server
			writer.WriteLine ("log = true");
			writer.WriteLine ("logfile = ./.sparklehshare/log"); //goes in the .sparkleshare directory
			writer.WriteLine ("contactquietly = true"); //supress some useless output
			writer.WriteLine ("confirmbigdel = false"); //don't confirm wiping the repo
			writer.WriteLine ("retry = 2");
			//if you have rsync installed (probably linux and mac os do then the next three can be enabled
			//faster than using unison for big files
			writer.WriteLine ("copyprog = rsync --inplace --compress");
			writer.WriteLine ("copyprogrest = rsync --partial --inplace --compress");
			writer.WriteLine ("copythreshold = 10000"); //use rsync for files larger than 10MB (10000 kB)
			
			//ignore rules added here
			// gedit and emacs
            writer.WriteLine ("ignore = Name *~");

            // vi(m)
            writer.WriteLine ("ignore = Name .*.sw[a-z]");
            writer.WriteLine ("ignore = Name *.un~");
            writer.WriteLine ("ignore = Name *.swp");
            writer.WriteLine ("ignore = Name *.swo");
            
            // KDE
            writer.WriteLine ("ignore = Name .directory");

            // Mac OSX
            writer.WriteLine ("ignore = Name .DS_Store");
            writer.WriteLine ("ignore = Name Icon?");
            writer.WriteLine ("ignore = Name ._*");
            writer.WriteLine ("ignore = Name .Spotlight-V100");
            writer.WriteLine ("ignore = Name .Trashes");

            // Mac OSX
            writer.WriteLine ("ignore = Name *(Autosaved).graffle");
        
            // Windows
            writer.WriteLine ("ignore = Name Thumbs.db");
            writer.WriteLine ("ignore = Name Desktop.ini");

            // CVS
            writer.WriteLine ("ignore = Name */CVS/*");
            writer.WriteLine ("ignore = Name .cvsignore");
            writer.WriteLine ("ignore = Path */.cvsignore");
            
            // Subversion
            writer.WriteLine ("ignore = Path /.svn/*");
            writer.WriteLine ("ignore = Path */.svn/*");
			
			// Sparkleshare
            writer.WriteLine ("ignore = Path */.sparkleshare"); //don't sync this since it has the archive file in it
            writer.Close ();
			
			SparkleHelpers.DebugInfo ("Unison Profile", "Added unison profile to '" + unison_profile + "'");	
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