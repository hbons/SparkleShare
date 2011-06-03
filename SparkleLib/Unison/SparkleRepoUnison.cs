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
                string IDfile = SparkleHelpers.CombineMore (LocalPath, ".unisonID");
                
                //ID file not found
                if(!File.Exists(IDfile))
                {
                    //check for a backup in .sparkleshare
                    string backupIDfile = SparkleHelpers.CombineMore (LocalPath, ".sparkleshare", ".unisonID");    
                    if(File.Exists ( backupIDfile) )
                    {
                        File.Copy(backupIDfile, IDfile);
                        SparkleHelpers.DebugInfo ("Unison", "Recovered backup ID file: " + backupIDfile);
                    }
                    //check if there is a copy on the server
                    else if ( UnisonGrab(IDfile) == 0 )
                    {
                        SparkleHelpers.DebugInfo ("Unison", "Downloaded ID file from server: " + IDfile);
                    }
                    else
                    {
                        //should probably create the ID here just like in the fetcher
                        SparkleHelpers.DebugInfo ("Unison", "NO REPO ID FILE FOUND");
                        return "unisonsparkles";
                    }
                }            
                
                //read the repo ID from the file
                TextReader reader = new StreamReader (IDfile);
                string repoID = reader.ReadToEnd().ToString().TrimEnd();
                SparkleHelpers.DebugInfo ("Unison", "Repo ID found: " + repoID);
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
        //hashes directory tree, file sizes and Last Modification Times
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
            
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + unison.ExitCode.ToString ());
            
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
            
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + unison_dryrun.ExitCode.ToString ());
            
            //check to see if there are really changes to make
            if (unison_dryrun.ExitCode != 0) 
            {
                //check for conflicts before syncing
                //conflicts are logged in the conflict handling code
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
                
                remote_revision = unison_sync.StandardOutput.ReadToEnd ().TrimEnd ();
                string [] lines = remote_revision.Split ("\n".ToCharArray ());
                foreach (string line in lines) 
                {      
                    //check for changes from local->remote server (remote->local don't need logging here)
                    if(line.Contains("---->"))
                    {
                        string path = line.ToString().Remove(0,26).Trim();
                        string revision = "";
                        if(line.Trim().StartsWith("new file"))
                        {
                            revision = "Added";
                        }
                        else if(line.StartsWith("deleted"))
                        {
                            revision = "Deleted";
                        }
                        else if(line.StartsWith("new dir"))
                        {
                            revision = "New Folder";
                        }
                        else if(line.StartsWith("changed"))
                        {
                            revision = "Edited";
                        }
                        WriteChangeLog(path, revision);
                    }
                }
    
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
                        //check if it was the local file that was deleted, if so log the deletion
                        if(line.Trim().StartsWith("deleted"))
                            WriteChangeLog(conflicting_path, "Deleted");
                        //otherwise the file must have been edited otherwise there wouldn't be a conflict
                        else
                            WriteChangeLog(conflicting_path, "Edited");
                        
                        //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
                        Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");                        
                        
                        //just keep/upload the newer version of the deleted file (doesn't matter if its local or remote)
                        SparkleUnison unison_deletefix = new SparkleUnison (LocalPath,
                            "-ui text " +
                            "-path '" + conflicting_path + "' " +
                            "-force newer " +
                            "sync");
                            
                        unison_deletefix.Start ();
                        unison_deletefix.WaitForExit ();
                        
                        SparkleHelpers.DebugInfo ("Unison", "Exit code: " + unison_deletefix.ExitCode.ToString ());
                        
                        //check if it was the local file that was deleted, if so now its been recovered
                        //don't really know easily from who's copy, ignore that for now
                        if(line.Trim().StartsWith("deleted"))
                            WriteChangeLog(conflicting_path, "Recovered");              
                    
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
                     
                        //upload the renamed file
                        UnisonTransmit (their_path);
                        
                        //get the server version of the conflicting file
                        UnisonGrab (conflicting_path);
                        
                        //update the log about the added file
                        WriteChangeLog(their_path, "Added");
                    }
                }
            }
        }

        
        private int UnisonTransmit (string path)
        {
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //grab the conflicted file from the server
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "-path '" + path + "' " +                                        
                "transmit");

            unison.Start ();
            unison.WaitForExit ();
            
            int exitcode = unison.ExitCode;
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + exitcode.ToString());
            return exitcode;
        }
        

        private int UnisonGrab (string path)
        {
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //grab the conflicted file from the server
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "-path '" + path + "' " +                                        
                "grab");

            unison.Start ();
            unison.WaitForExit ();
            
            int exitcode = unison.ExitCode;
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + exitcode.ToString());
            return exitcode;
        }
        
        
        private int WriteChangeLog (string path, string revision)
        {
            string changelog_file = SparkleHelpers.CombineMore (LocalPath, ".changelog");         
            string timestamp = DateTime.Now.ToString ("HH:mm MMM d");
            string username = SparkleConfig.DefaultConfig.UserName.ToString().Trim();
            string useremail = SparkleConfig.DefaultConfig.UserEmail.ToString().Trim();
            string logupdate = timestamp + ", \"" + username + "\", \"" + useremail + "\", " + revision + ", \"" + path + "\"";
            
            //update the log file from the server
            if (UnisonGrab(".changelog") == 0)
                SparkleHelpers.DebugInfo ("Unison", "Downloaded latest log file: " + changelog_file);
            
            //check that file exists, otherwise create it now
            if (!File.Exists (changelog_file))
			{
                File.Create (changelog_file);
				SparkleHelpers.DebugInfo ("Unison", "Created log file: " + changelog_file);
			}
                        
            //append to the log file
            using (StreamWriter sw = File.AppendText(changelog_file)) 
            {
                sw.WriteLine (logupdate);
            }    

            SparkleHelpers.DebugInfo ("Unison", "Updated local log: " + changelog_file + ": " + logupdate);
            
            //send updated log to server
            //TODO: need to figure out if its needed to merge the log if 2 people submit at the same time...
            //what hasppens if there is a collision/conflict
            //this will overwrite the log, if someone checks out a log and then someone else uploads it will break...
            int exitcode = UnisonTransmit (".changelog");
            
            if(exitcode == 0)
                SparkleHelpers.DebugInfo ("Unison", "Updated server log: " + changelog_file);
            
            return exitcode;
        }
        
        
        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
            //TODO: read the log file here (created in WriteChangeLog)
            //careful with timezones (should be all in UTC) -> correct for user's timezone
			
			var l = new List<SparkleChangeSet> ();
			
			string changelog_file = SparkleHelpers.CombineMore (LocalPath, ".changelog");
			
			//update the log file from the server
            if (UnisonGrab(".changelog") == 0)
                SparkleHelpers.DebugInfo ("Unison", "Downloaded latest log file: " + changelog_file);
			
			if (!File.Exists (changelog_file))
			{		
				TextReader reader = new StreamReader (changelog_file);
	            string changelog = reader.ReadToEnd().ToString();
				string [] lines = changelog.Split ("\n".ToCharArray ());
                foreach (string line in lines)
				{
					l.Add (new SparkleChangeSet () { UserName = "test", UserEmail = "test", Revision = "test", Timestamp = DateTime.Now });
				}
			}
			else
			{
				File.Create (changelog_file);
				SparkleHelpers.DebugInfo ("Unison", "Created log file: " + changelog_file);
			}
            return l;
        }


        public override void CreateInitialChangeSet ()
        {
            //TODO: not sure exactly what this should be...
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