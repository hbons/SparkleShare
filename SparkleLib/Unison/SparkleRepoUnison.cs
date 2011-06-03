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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace SparkleLib {

    public class SparkleRepoUnison : SparkleRepoBase {

        public SparkleRepoUnison (string path, SparkleBackend backend) :
            base (path, backend) { }


        public override string Identifier {
            get {
                //needs better error handling incase the id file was accidentally deleted
                //maybe copy it into the .sparkleshare folder too?
                string IDfile = SparkleHelpers.CombineMore (LocalPath, ".unisonID");
                
                //check for a backup in .sparkleshare if the file isn't found
                if(!File.Exists(IDfile))
                {
                    string backupIDfile = SparkleHelpers.CombineMore (LocalPath, ".sparkleshare", ".unisonID");    
                    if(File.Exists(backupIDfile))
                    {
                        File.Copy(backupIDfile, IDfile);
                        SparkleHelpers.DebugInfo ("Unison", "Recovered backup ID file: " + backupIDfile);
                    }
                    else
                    {
                        SparkleHelpers.DebugInfo ("Unison", "NO REPO ID FILE FOUND");
                        return "unisonsparkles";
                    }
                }            
                
                TextReader reader = new StreamReader (IDfile);
                string repoID = reader.ReadToEnd().ToString();
                SparkleHelpers.DebugInfo ("Unison", "Repo ID found:" + repoID);
                return repoID;
            }
        }
        
        
        private string GetSHA1 (string s)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = sha1.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");  
        }
        

        //need to make sure this actually works on two different clients
        //hashes directory tree and Last Modification Time
        public override string CurrentRevision {
            get {                
                StringBuilder sb = new StringBuilder();
                sb = PopulateTree(LocalPath, sb);                
                string treehash = GetSHA1 (sb.ToString());                
                SparkleHelpers.DebugInfo ("Unison", "Current Revision: " + treehash);                
                return treehash;    
            }
        }
        

        private StringBuilder PopulateTree(string dir, StringBuilder files) 
        {
            //get the information of the directory
            DirectoryInfo directory = new DirectoryInfo(dir);
            //loop through each subdirectory
            foreach(DirectoryInfo d in directory.GetDirectories()) 
            {
                //don't check dotfolders since the unison archive and fingerprint files will be different
                if(!d.FullName.ToString().StartsWith("."))
				{
                    string dirname = d.Name.ToString();
					files.AppendLine(dirname);
					PopulateTree(d.FullName, files);
			    }
            }
            // lastly, loop through each file in the directory
            foreach(FileInfo f in directory.GetFiles())
            {    
                string filename = f.Name.ToString();
				string lastwrite = f.LastWriteTimeUtc.ToString();    
                string size = f.Length.ToString();
                files.AppendLine(filename + " " + size + " " + lastwrite);
            }
            return files;
        }
        

        private bool CheckForChangesBothWays ()
        {            
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "dryrun");

            unison.Start ();
            unison.WaitForExit ();

            string remote_revision = unison.StandardOutput.ReadToEnd ().TrimEnd ();
            
            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());
            
            if (unison.ExitCode != 0)
            {
                SparkleHelpers.DebugInfo ("Unison", remote_revision);
                return true;
            } 
            else
                return false;
        }
        
        
        public override bool CheckForRemoteChanges ()
        {
            return CheckForChangesBothWays ();
        }
        
        
        private bool SyncBothWays ()
        {
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //check for changes
            SparkleUnison unison_dryrun = new SparkleUnison (LocalPath,
                "-ui text " +
                "dryrun");

            unison_dryrun.Start ();
            unison_dryrun.WaitForExit ();

            string remote_revision = unison_dryrun.StandardOutput.ReadToEnd ().TrimEnd ();
            
            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison_dryrun.ExitCode.ToString ());
            
            //check to see if there are really changes to make
            if (unison_dryrun.ExitCode != 0) 
            {
                //check for conflicts before syncing
                if (remote_revision.Contains ("<-?->"))
                    ResolveConflicts (remote_revision);
                
                //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
                Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");                
                
                //sync both folders now!
                SparkleUnison unison_sync = new SparkleUnison (LocalPath,
                    "-ui text " +
                    "sync");
    
                unison_sync.Start ();
                unison_sync.WaitForExit ();
    
                SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison_sync.ExitCode.ToString ());
    
                if (unison_sync.ExitCode != 0)
                    return false;
                else 
                    return true;
            }
            else
                return true;
        }

        public override bool SyncUp ()
        {
            return SyncBothWays ();
        }


        public override bool SyncDown ()
        {
            return SyncBothWays ();
        }


        public override bool AnyDifferences {
            get {
                return CheckForChangesBothWays ();
            }
        }


        public override bool HasUnsyncedChanges {
            get {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".sparkleshare", "has_unsynced_changes");

                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".sparkleshare", "has_unsynced_changes");

                if (value) {
                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path);
                } else {
                    File.Delete (unsynced_file_path);
                }
            }
        }

        private void ResolveConflicts (string remote_revision)
        {          
            //split lines
            string [] lines = remote_revision.Split ("\n".ToCharArray ());
            foreach (string line in lines) 
            {               
                //check to see if the line describes a conflict (new files, changes, deletions)
                if ( line.Contains ("<-?->") )
                {
                    SparkleHelpers.DebugInfo ("Unison", "Conflict: " + line.TrimEnd());
                    string conflicting_path = line.Remove(0,26).TrimEnd();
                    
                    //check to see if the conflict is over a deleted file
                    if ( line.Contains ("deleted") )
                    {
                        //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
                        Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");                        
                        
                        //just get the new version of the deleted file
                        SparkleUnison unison_deletefix = new SparkleUnison (LocalPath,
                            "-ui text " +
                            "-path '" + conflicting_path + "' " +
                            "-force newer " +
                            "sync");
                            
                        unison_deletefix.Start ();
                        unison_deletefix.WaitForExit ();
                        
                        SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison_deletefix.ExitCode.ToString ());
                    }
                    //implies that there is a conflict with 2 changed files
                    else 
                    {
                        // Append a timestamp to local version (their copy is the local copy)
                        string timestamp            = DateTime.Now.ToString ("HH:mm MMM d");
                        string their_path           = conflicting_path + " (" + SparkleConfig.DefaultConfig.UserName + ", " + timestamp + ")";
                        string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
                        string abs_their_path       = Path.Combine (LocalPath, their_path);
                      
                        File.Move (abs_conflicting_path, abs_their_path);
                        
                        //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
                        Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
                        
                        //send the renamed file to the server
                        SparkleUnison unison_sync = new SparkleUnison (LocalPath,
                            "-ui text " +
                            "-path '" + their_path + "' " +                                        
                            "transmit");
                            
                        unison_sync.Start ();
                        unison_sync.WaitForExit ();
                        
                        SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison_sync.ExitCode.ToString ());
                        
                        //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
                        Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
                        
                        //grab the conflicted file from the server
                        SparkleUnison unison_grab = new SparkleUnison (LocalPath,
                            "-ui text " +
                            "-path '" + conflicting_path + "' " +                                        
                            "grab");
        
                        unison_grab.Start ();
                        unison_grab.WaitForExit ();
        
                        SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison_grab.ExitCode.ToString ());
                    }
                }
            }
        }
        

        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
            //TODO: parse the unison log (.sparkleshare/log)
            var l = new List<SparkleChangeSet> ();
            l.Add (new SparkleChangeSet () { UserName = "test", UserEmail = "test", Revision = "test", Timestamp = DateTime.Now });
            return l;
        }


        public override void CreateInitialChangeSet ()
        {
            base.CreateInitialChangeSet ();
        }


        public override bool UsesNotificationCenter
        {
            get {
                string file_path = SparkleHelpers.CombineMore (LocalPath, ".sparkleshare", "disable_notification_center");
                return !File.Exists (file_path);
            }
        }
    }
}