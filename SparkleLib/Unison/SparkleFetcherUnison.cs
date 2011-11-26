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
using System.Security.Cryptography;
using System.Text;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public class SparkleFetcherUnison : SparkleFetcherBase {

        public SparkleFetcherUnison (string server, string remote_folder, string target_folder) :
            base (server, remote_folder, target_folder) 
        {
            //format remote and local destination URLs and paths here
            server = server.TrimEnd ("/".ToCharArray ());

            if (server.StartsWith ("ssh://"))
                server          = server.Substring (6);

            if (!server.Contains ("@"))
                server          = "git@" + server;

            server              = "ssh://" + server;            
            remote_folder       = remote_folder.Trim ("/".ToCharArray ()); 
			
			//could use ssh server "echo $HOME" to resolve ~
			//would need a SparkleSSH thing in addtion to SparkleUnison
			
			//if(remote_folder.Contains ("~"))
            //    base.remote_url = server + "/" + remote_folder;
			//else
				base.remote_url = server + "//" + remote_folder;
				
            base.target_folder = target_folder;
        }


        public override bool Fetch ()
        {    
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //might want to also set UNISONLOCALHOSTNAME to something unique, currently defaults to computer hostname
            
            //create a rootalias so unison archives are valid after moving from .tmp to ~/Sparkleshare/...
            string actual_folder = base.target_folder.Replace(".tmp/", "");
            string actual_root   = "//" + System.Environment.MachineName + "/" + actual_folder;
            string temp_root     = "//" + System.Environment.MachineName + "/" + base.target_folder;
            string rootalias     = "'" + temp_root + " -> " + actual_root + "' ";
            
            //fetch remote repo with unison
            SparkleUnison unison = new SparkleUnison (SparkleConfig.DefaultConfig.TmpPath,
                "-auto " +
                "-batch " +
                "-confirmbigdel=false " +
                "-ui text " +
                "-log " +
                "-terse " +
                "-dumbtty " +
                "-times " +
                "-rootalias " + rootalias +
                "-logfile templog " + 
                "-ignorearchives " +
                "-force "      + "\"" + base.remote_url     + "\" " +    //protect the server copy
                "-noupdate "   + "\"" + base.remote_url     + "\" " +
                "-nocreation " + "\"" + base.remote_url     + "\" " +
                "-nodeletion " + "\"" + base.remote_url     + "\" " +
                                 "\"" + base.target_folder  + "\" " +    //root1: localhost
                                 "\"" + base.remote_url     + "\"" );    //root2: remote server

            unison.Start ();
            unison.WaitForExit ();

            string remote_revision = unison.StandardOutput.ReadToEnd ().TrimEnd ();
            
            SparkleHelpers.DebugInfo ("Unison", remote_revision);
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + unison.ExitCode.ToString ());
           
            if (unison.ExitCode != 0) {
                return false;
            } else {    
                InstallConfiguration ();
                CreateID ();
                CreateChangeLog ();
                InstallUnisonBaseProfile ();
                InstallUnisonDryRunProfile ();
                InstallUnisonSyncProfile ();
                InstallUnisonSyncLogFileProfile ();
                InstallUnisonGrabRemoteFileProfile ();
                InstallUnisonTransmitLocalFileProfile ();
                return true;
            }
        }


        // Install the user's name and email and some config into
        // the newly cloned repository
        private void InstallConfiguration ()
        {                  
            //move the .sparkleshare folder into the repo after fetching
            string dotfolder_old_path = SparkleHelpers.CombineMore (SparkleConfig.DefaultConfig.TmpPath, ".sparkleshare");
            string dotfolder_new_path = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare");
            Directory.Move(dotfolder_old_path, dotfolder_new_path);    
            
            SparkleHelpers.DebugInfo ("Unison", "Moved .sparkleshare folder to: " + dotfolder_new_path);
            
            //move the templog file to from .tmp to log in.sparkleshare
            string log_file_old_path = SparkleHelpers.CombineMore (SparkleConfig.DefaultConfig.TmpPath, "templog");
            string log_file_new_path = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "log");
            File.Move (log_file_old_path, log_file_new_path);  
            
            SparkleHelpers.DebugInfo ("Unison", "Moved log to: " + log_file_new_path);

            //create the config file
            string config_file_path = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "config");

            string config = ""; //empty for now

            // Write the config to the file
            TextWriter writer = new StreamWriter (config_file_path);
            writer.WriteLine (config);
            writer.Close ();    
            
            SparkleHelpers.DebugInfo ("Unison", "Created config file: " + config_file_path);
        }

        
        //install unison profile
        //need to specify the needed options, the local and server address, the logfile location
        //need to run export UNISON=./.sparkleshare/ on the client so unison looks for the profiles in the .sparkleshare directory
        private void InstallUnisonBaseProfile ()
        {
            // Write the base unison sparkleshare profile to the file (sparkleshare.prf)
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "sparkleshare.prf");     
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("root = ."); //root1: local folder
            writer.WriteLine ("root = " + base.remote_url); //root2: remote server -- PROBLEM WITH SPACES IN THE PATH, quotes are broken!!, try single quotes
            writer.WriteLine ("log = true");
            writer.WriteLine ("batch = true");
            writer.WriteLine ("dumbtty = true");
            writer.WriteLine ("auto = true");
            writer.WriteLine ("terse = true");
            writer.WriteLine ("times = true");
            writer.WriteLine ("logfile = .sparkleshare/log");
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
            writer.WriteLine ("ignore = Path */.svn/*");
            
            // Sparkleshare
            writer.WriteLine ("ignore = Path .sparkleshare"); //don't sync this since it has the archive file and the unison log in it
            writer.Close ();
            
            SparkleHelpers.DebugInfo ("Unison", "Added unison profile to: " + unison_profile);
        }
        
            
        private void InstallUnisonDryRunProfile ()
        {
            //create profile: dryrun.prf -- does nothing, just lists the changes that need to be made
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "dryrun.prf");
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("include sparkleshare");
            writer.WriteLine ("nodeletion = .");
            writer.WriteLine ("nodeletion = " + base.remote_url);
            writer.WriteLine ("noupdate = .");
            writer.WriteLine ("noupdate = " + base.remote_url);
            writer.WriteLine ("nocreation = .");
            writer.WriteLine ("nocreation = " + base.remote_url);
            writer.WriteLine ("ignore = Name .changelog");
            writer.Close ();
            
            SparkleHelpers.DebugInfo ("Unison", "Added unison profile to: " + unison_profile);
        }

        
        private void InstallUnisonSyncProfile ()
        {
            //create profile: sync.prf -- runs automatic unison batch sync
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "sync.prf");
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("include sparkleshare");
            writer.WriteLine ("confirmbigdel = false");
            writer.WriteLine ("ignore = Name .changelog");
            writer.Close ();
            
            SparkleHelpers.DebugInfo ("Unison", "Added unison profile to: " + unison_profile);
        }
        
        
        private void InstallUnisonGrabRemoteFileProfile ()
        {
            //create profile: grab.prf -- used to grab a specific remote file
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "grab.prf");
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("include sparkleshare");
            writer.WriteLine ("confirmbigdel = false");
            writer.WriteLine ("nodeletion = " + base.remote_url);;
            writer.WriteLine ("noupdate = " + base.remote_url);
            writer.WriteLine ("nocreation = " + base.remote_url);
            writer.WriteLine ("force = " + base.remote_url);
            writer.Close ();
            
            SparkleHelpers.DebugInfo ("Unison", "Added unison profile to: " + unison_profile);
        }
        
        
        private void InstallUnisonTransmitLocalFileProfile ()
        {
            //create profile: transmit.prf -- runs automatic unison batch sync
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "transmit.prf");
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("include sparkleshare");
            writer.WriteLine ("confirmbigdel = false");
            writer.WriteLine ("nodeletion = .");
            writer.WriteLine ("noupdate = .");
            writer.WriteLine ("nocreation = .");
            writer.WriteLine ("force = .");
            writer.Close ();
            
            SparkleHelpers.DebugInfo ("Unison", "Added unison profile to: " + unison_profile);
        }
        
        private void InstallUnisonSyncLogFileProfile ()
        {
            //create profile: logging.prf -- merges local log with log file on server (both sides can be modified)
            //requires: cat, sort and uniq
            string unison_profile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", "logging.prf");
            TextWriter writer = new StreamWriter (unison_profile);
            writer.WriteLine ("include sparkleshare");
            writer.WriteLine ("confirmbigdel = false");
            writer.WriteLine ("confirmmerge = false");
            writer.WriteLine ("path = .changelog");
            writer.WriteLine ("merge = Path .changelog -> cat CURRENT1 CURRENT2 | sort | uniq > NEW");
            writer.Close ();
            
            SparkleHelpers.DebugInfo ("Unison", "Added unison profile to: " + unison_profile);
        }
        
        
        private void CreateChangeLog ()
        {
            //create the changelog file
            string changelog_file = SparkleHelpers.CombineMore (base.target_folder, ".changelog");
            
            //check if file exists already
            if( !File.Exists (changelog_file) )
            {                
                //create the changelog file
                TextWriter writer = new StreamWriter (changelog_file);
                writer.Close ();

                SparkleHelpers.DebugInfo ("Unison", "Created changelog: " + changelog_file);
            }
            //changelog exists
            else
            {
                SparkleHelpers.DebugInfo ("Unison", "Downloaded changelog: " + changelog_file);
            }
        }
             
        
        private void CreateID ()
        {
            //idfile
            string IDfile = SparkleHelpers.CombineMore (base.target_folder, ".unisonID");
            
            //check if file exists already
            if( !File.Exists (IDfile) )
            {                
                //creates a unique identifier based on the remote_url and the UTC date/time
                string identifier = base.remote_url + DateTime.UtcNow.ToString();
                string IDhash = GetSHA1 (identifier);
            
                TextWriter writer = new StreamWriter (IDfile);
                writer.WriteLine (IDhash);
                writer.Close ();                
            
                SparkleHelpers.DebugInfo ("Unison", "Added ID file to: " + IDfile);
                
                //might want to manually use unison to push the ID to the server so that the entry doesn't got logged upon the first sync
                //mostly cosmetic.
            }
            else
            {
                SparkleHelpers.DebugInfo ("Unison", "IDfile exists: " + IDfile);
            }
            //backup ID file to .sparkleshare - just incase someone deleted it
            string backupIDfile = SparkleHelpers.CombineMore (base.target_folder, ".sparkleshare", ".unisonID");        
            File.Copy(IDfile, backupIDfile);
            SparkleHelpers.DebugInfo ("Unison", "Copied ID file to: " + backupIDfile);
        }
        
                
        private string GetSHA1 (string s)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = sha1.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");  
        }
        
        
    }

    
    public class SparkleUnison : Process {

        public SparkleUnison (string path, string args) : base ()
        {
            EnableRaisingEvents              = true;
            StartInfo.FileName               = "/usr/local/bin/unison"; //needs to reference multiple paths, how does this work?
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