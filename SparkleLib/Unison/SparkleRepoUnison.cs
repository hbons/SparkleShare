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
                        //TODO: recalculate the repoID and upload it to the server
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
                //don't check .dotfolders since the unison archive and fingerprint files will be different
                if(!d.FullName.ToString().StartsWith("."))
                {
                    string dirname = d.Name.ToString();
                    //need to include directory name not entire path as this will be different on different clients
                    files.AppendLine(dirname);
                    PopulateTree(d.FullName, files);
                }
            }
            //lastly, loop through each file in the directory
            //compiles directory names, filenames, file sizes and last write times
            foreach(FileInfo f in directory.GetFiles())
            {    
                //ignore .dotfiles, they might be different/out of sync
				if(!f.Name.ToString().StartsWith("."))
					files.AppendLine(f.Name.ToString() + " " + f.Length.ToString() + " " + f.LastWriteTimeUtc.ToString());
            }
            return files;
        }
        

        private bool CheckForChangesBothWays ()
        {            
            SparkleHelpers.DebugInfo ("Unison", "Checking for changes");
			
			Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "dryrun");

            unison.Start ();
            unison.WaitForExit ();
            
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + unison.ExitCode.ToString ());
            
            if (unison.ExitCode != 0)
                return true;
            else
                return false;
        }
        
        
        public override bool CheckForRemoteChanges ()
        {
            return CheckForChangesBothWays ();
        }
        
        
        private bool SyncBothWays ()
        {
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
                //conflicts and the resolution are logged in the conflict handling code
                if (remote_revision.Contains ("<-?->"))
                    //pass the output of the dry-run to the conflict handling code
                    ResolveConflicts (remote_revision);
                
                //probably not needed to set this again here - lets be safe though
                Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");                
                
                //sync both folders now!
                SparkleUnison unison_sync = new SparkleUnison (LocalPath,
                    "-ui text " +
                    "sync");
    
                unison_sync.Start ();
                unison_sync.WaitForExit ();
    
                SparkleHelpers.DebugInfo ("Unison", "Exit code: " + unison_sync.ExitCode.ToString ());
                
                remote_revision = unison_sync.StandardOutput.ReadToEnd ().TrimEnd ();
                
                //output the result for debug
                SparkleHelpers.DebugInfo ("Unison", "Sync Complete: " + remote_revision.ToString ());
                
                //parse the output for logging
                string [] lines = remote_revision.Split ("\n".ToCharArray ());
                foreach (string line in lines) 
                {      
                    //check for changes from local->remote server (remote->local don't need logging here)
                    if(line.ToString().Contains("---->"))
                    {
                        string linestring = line.ToString().TrimEnd();
                        string linetrim = linestring.Trim();
                        string revision = "";
                        if(linetrim.StartsWith("new file"))
                        {
                            revision = "Added";
                        }
                        else if(linetrim.StartsWith("deleted"))
                        {
                            revision = "Deleted";
                        }
                        else if(linetrim.StartsWith("new dir"))
                        {
                            revision = "New Folder";
                        }
                        else if(linetrim.StartsWith("changed"))
                        {
                            revision = "Edited";
                        }
                        else //just incase
                        {
                            revision = "Unknown";
                        }
                        WriteChangeLog(linestring.Remove(0,26).Trim(), revision);
                        //TODO: make an array of changes and then add them all at once
                        //add to the contruct here
                    }
                }
                //outside here run the new writechangelog function that will write all the changes
    
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
                        //TODO: queue up the log entries to write at once
                        //check if it was the local file that was deleted, if so log the deletion
                        if(line.Trim().StartsWith("deleted"))
                            WriteChangeLog(conflicting_path, "Deleted");
                        //otherwise the file must have been edited otherwise there wouldn't be a conflict
                        else
                            WriteChangeLog(conflicting_path, "Edited");
                        
                        Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");                        
                        
                        //just keep/upload the newer version of the deleted file (doesn't matter if its local or remote)
                        SparkleUnison unison_deletefix = new SparkleUnison (LocalPath,
                            "-ui text " +
                            "-path '" + conflicting_path + "' " +
                            "-force newer " +
                            "sync");
                            
                        unison_deletefix.Start ();
                        unison_deletefix.WaitForExit ();
                        
                        int exitcode = unison_deletefix.ExitCode;
                        
                        SparkleHelpers.DebugInfo ("Unison", "Exit code: " + exitcode.ToString ());
                        
                        //need to check that this was successful
                        
                        //check if it was the local file that was deleted, if so now its been recovered
                        //don't really know easily from who's copy, ignore that for now
                        //just reports added since there doesn't seem to be a capability for recovered
                        if(line.Trim().StartsWith("deleted"))
                            WriteChangeLog(conflicting_path, "Added");              
                    
                    }        
                    //implies that there is a conflict with 2 changed files
                    else 
                    {
                        // Append a timestamp to local version (their copy is the local copy)
                        string timestamp            = DateTime.Now.ToString ("HH:mm MMM d");
                        string username             = SparkleConfig.DefaultConfig.UserName.ToString();
                        string their_path           = conflicting_path + " (" + username + ", " + timestamp + ")";
                        string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
                        string abs_their_path       = Path.Combine (LocalPath, their_path);
                      
                        File.Move (abs_conflicting_path, abs_their_path);                        
                     
                        //upload the renamed file
                        if (UnisonTransmit (their_path) == 0)
                        {
                            //now get the server version of the conflicting file
                            UnisonGrab (conflicting_path);
                            
                            //update the log about the added timestamped/usernamed file
                            WriteChangeLog(their_path, "Added");
                        } 
                        //TODO: what to do upon failure?
                    }
                }
            }
        }

        
        private int UnisonTransmit (string path)
        {
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //transmit the file to the server
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "-path '" + path + "' " +                                        
                "transmit");

            unison.Start ();
            unison.WaitForExit ();
            
            int exitcode = unison.ExitCode;
            SparkleHelpers.DebugInfo ("Unison", "Transmitted File: " + path + " Exit code: " + exitcode.ToString());
            return exitcode;
        }
        

        private int UnisonGrab (string path)
        {
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //grab the file from the server
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "-path '" + path + "' " +                                        
                "grab");

            unison.Start ();
            unison.WaitForExit ();
            
            int exitcode = unison.ExitCode;
            SparkleHelpers.DebugInfo ("Unison", "Grabed file: " + path + " Exit code: " + exitcode.ToString());
            return exitcode;
        }
        
        private int UnisonTransmitLog ()
        {
            //set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
            Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
            
            //sync log with server
            //merges changes to .changelog with server (will also make local changes)
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " + 
                "logging");

            unison.Start ();
            unison.WaitForExit ();
            
            int exitcode = unison.ExitCode;
            SparkleHelpers.DebugInfo ("Unison", "Exit code: " + exitcode.ToString());
            return exitcode;
        }
        
        
        private int WriteChangeLog (string path, string revision)
        {
            //rewrite this to operate over a number of changes
            //foreach change in changes? (this only works for arrays)
            //probably need to implement some sort of class or something
            
            //might want to write the log server side directly via ssh and append >> to the file
            //still need to then sync the log to the local client for offline use.
            
            string changelog_file = SparkleHelpers.CombineMore (LocalPath, ".changelog");         
            string timestamp = DateTime.UtcNow.ToString(); //log written in UTC
            string username = SparkleConfig.DefaultConfig.UserName.ToString().Trim();
            string useremail = SparkleConfig.DefaultConfig.UserEmail.ToString().Trim();
            string logupdate = timestamp + ", " + username + ", " + useremail + ", " + revision + ", " + path;
            
            //grab the latest log from the server
            //might be possible to just rely on the merging code in UnisonTransmitLog() and just skip this grab
            if (UnisonGrab (".changelog") == 0)
                SparkleHelpers.DebugInfo ("Unison", "Downloaded latest log file: " + changelog_file);
            
            //TODO: fix! 
            //check that file exists, otherwise create it now
           // if (!File.Exists (changelog_file))
           // {
           //     File.Create (changelog_file);
           //     SparkleHelpers.DebugInfo ("Unison", "Created log file: " + changelog_file);
           // }
                        
            //append to the log file
            using (StreamWriter sw = File.AppendText(changelog_file)) 
            {
                sw.WriteLine (logupdate);
            }    

            SparkleHelpers.DebugInfo ("Unison", "Updated local log: " + changelog_file + ": " + logupdate);
            
            //append all the updates queued then transmit the updated log
            
            int exitcode = UnisonTransmitLog ();
            
            if(exitcode == 0)
                SparkleHelpers.DebugInfo ("Unison", "Updated server log: " + changelog_file);
            
            return exitcode;
        }
        
        
        public override List <SparkleChangeSet> GetChangeSets (int count)
        {            
            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();
            
            string changelog_file = SparkleHelpers.CombineMore (LocalPath, ".changelog");
            
            //get the latest log file from the server
            if (UnisonGrab (".changelog") == 0)
                SparkleHelpers.DebugInfo ("Unison", "Downloaded latest log file: " + changelog_file);
            
            if (File.Exists (changelog_file))
            {   
                //TODO: implement tail to only read from the end of the log:
                //http://www.codeproject.com/KB/cs/wintail.aspx
                //wait for this to be implemented in the API
                
                TextReader reader = new StreamReader (changelog_file);
                string changelog = reader.ReadToEnd().ToString().Trim();
                string [] lines = changelog.Split ("\n".ToCharArray ());
                Array.Reverse(lines);

								
				//trying to read from the end of the file
				FileStream fileStream = new FileStream(changelog_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, true);
				if(fileStream.Length - count > 0)
				{
				    fileStream.Seek(Math.Max(0, fileStream.Length - count), SeekOrigin.Begin);				
				    StreamReader fileReader = new StreamReader(fileStream);
				    string endoflog = fileReader.ToString().Trim();
					lines = endoflog.Split ("\n".ToCharArray ());
					
				}
                
				foreach (string line in lines)
                {
                   
                    string[] parts = line.Split(",".ToCharArray ()); 
                    //TODO: fix: commas in filenames will be broken..
                    //maybe make a way to join all the parts after the 4th comma?
                    
                    //foreach (string part in parts) 
                    //    SparkleHelpers.DebugInfo ("Unison", "Read log entry: " + part);
                    
                    SparkleChangeSet change_set = new SparkleChangeSet ();
                    
                    int our_offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
                    
                    try
                    {
                        change_set.Timestamp = DateTime.Parse(parts[0].Trim());    
                    }
                    //if the time string can't be interpretted just use the current time for now
                    catch (FormatException)
                    {
                        change_set.Timestamp = DateTime.UtcNow;
                    }
                    
                    //change UTC to our timezone
                    if (our_offset > 0)
                        change_set.Timestamp = change_set.Timestamp.AddHours (our_offset);
                    else
                        change_set.Timestamp = change_set.Timestamp.AddHours (our_offset * -1);
                                        
                    change_set.Revision  = parts[3].Trim();
                    change_set.UserName  = parts[1].Trim();
                    change_set.UserEmail = parts[2].Trim();
                    
                    string relativepath = parts[4].Trim();
                    string name = relativepath;
                    
                    if (!relativepath.Contains("/"))
                    {
                        change_set.Folder = Name;
                        name = relativepath;
                    }
                    else
                    {
                        string[] pathparts = relativepath.Split("/".ToCharArray ());
                        name               = pathparts[pathparts.Length - 1].ToString(); //filename
                        string pathhere    = relativepath.Remove(relativepath.Length - name.Length - 1, name.Length + 1); //relative path
                        change_set.Folder  = Name + "/" + pathhere;    //folder Name/relative path
                    }            
                    
                    if (change_set.Revision.Equals ("Added"))
                    {
                        change_set.Added.Add (name);
                    } 
                    else if (change_set.Revision.Equals ("Edited"))
                    {
                        change_set.Edited.Add (name);
                    } 
                    else if (change_set.Revision.Equals ("Deleted"))
                    {
                        change_set.Deleted.Add (name);
                    }
                    else if (change_set.Revision.Equals ("New Folder")) //not sure how this should be handled with the new nice log
                    {
                        change_set.Added.Add (name + "/"); //make it look more like a directory
                    }
                        
                    //unison doesn't recognize if files were moved it just thinks they were deleted then added somewhere else, oh well.
                    
                    change_sets.Add (change_set);
                }
            }
            else
            {
                File.Create (changelog_file);
                SparkleHelpers.DebugInfo ("Unison", "Created log file: " + changelog_file);
            }
            return change_sets;
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