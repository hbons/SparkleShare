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

namespace SparkleLib {

    public class SparkleRepoRsync : SparkleRepoBase {

        public SparkleRepoRsync (string path, SparkleBackend backend) :
            base (path, backend) { }


        public override string Identifier {
            get {
                return "sparkles";
            }
        }


        public override string CurrentRevision {
            get {
                return "";
            }
        }

		//need to see if there is a way to pass the changes detected when syncing avoid checking twice
		//also the conflict management also needs to know the changes to be made
        public override bool CheckForRemoteChanges ()
        {
			remote_revision = RemoteChanges ();
			
			//counting lines won't really work since rsync doesn't know if the changes were made locally or on the server...
            if (CountLinesInString(remote_revision) > 4) {
                SparkleHelpers.DebugInfo ("Rsync", "[" + Name + "] Remote changes found. (" + remote_revision + ")");
                return true;
            } else {
                return false;
            }
        }
		
		//the option --inplace is good if your server uses block level snapshots (ex. ZFS) but it increases network throughput
		//maybe make a config file option?
		//Windows<->Solaris will want to use -A to preserve ACLs
		//--delete will delete files on the server that were deleted locally (need to make sure that the server copy wasnt modified...)
        public override bool SyncUp ()
        {
			//delete can cause problems -- see comments in the conflict code
			SparkleRsync rsync = new SparkleRsync (LocalPath,
                "--archive --itemize --compress --partial --delete --delete-during --exclude-from=.sparkleshare --log-file=.rsynclog . " + "\"" + base.remote_url + "\"");

            rsync.Start ();
            rsync.WaitForExit ();

            if (rsync.ExitCode == 0)
                return true;
            else
                return false;
        }

        public override bool SyncDown ()
        {
			SparkleRsync rsync = new SparkleRsync (LocalPath,
                "--archive --itemize --compress --partial --delete --delete-during --exclude-from=.sparkleshare --log-file=.rsynclog \"" + base.remote_url + "\" " + ".");

            rsync.Start ();
            rsync.WaitForExit ();

            if (rsync.ExitCode == 0)
                return true;
            else
                return false;
        }


        public override bool AnyDifferences {
            get {

				remote_revision = LocalChanges ();

	            if (CountLinesInString(remote_revision) > 4) 
	                return true;
	             else 
	                return false;
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

		//http://samba.anu.edu.au/ftp/rsync/rsync.html
		//see entry for -i (--itemize) this describes how rsync reports changes
        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
			//parse the rsync log here?
            var l = new List<SparkleChangeSet> ();
            l.Add (new SparkleChangeSet () { UserName = "test", UserEmail = "test", Revision = "test", Timestamp = DateTime.Now });
            return l;
        }
		
		private void ResolveConflict ()
        { 
			//some conflict resolution code in python: http://sujitpal.blogspot.com/2009/12/unison-replacement-with-rsync.htm	
			
			//local changes
			SparkleRsync rsync = new SparkleRsync (LocalPath,
            	"--archive --itemize --compress --dry-run --partial --delete --exclude-from=.sparkleshare ." + "\"" + base.remote_url + "\"");

            rsync.Start ();
            rsync.WaitForExit ();

            string local_changes = rsync.StandardOutput.ReadToEnd ().TrimEnd ();
			
			//remote changes
			SparkleRsync rsync = new SparkleRsync (LocalPath,
                "--archive --itemize --compress --dry-run --partial --delete --exclude-from=.sparkleshare \"" + base.remote_url + "\" " + ".");

            rsync.Start ();
            rsync.WaitForExit ();

            string remote_changes = rsync.StandardOutput.ReadToEnd ().TrimEnd ();
			
			//need to compare local_changes with remote_changes to check for overlapping files
			//look at each overlap and see if they are both modified or:
			//it the local version has been modified, but the server version was removed
			//and the opposite, where the local version was deleted and the server version was modified
			//for simple conflicts where the local and server version were both modified
			//(need to look at timestamps and rsync log since rsync doesn't have a record of modifications)
			//just apply a suffix (usernames + timestamps) and keep both files
			
			//**DELETED FILES** 
			//there is a complexity that arrises when a file is locally modified by one person after being deleted on the server by someone else
			//since there is no record of if the file was actually locally modified other than the timestamp
			//might need to compare the modification-timestamp of the local files to the time of last sync (from the log)
			//for files that are deleted on the server, then if they were modified copy them to the server with a suffix
			//otherwise delete them locally...
			
			//the rsync dry run will report which files are scheduled for deletion in the upcoming sync
			
			//the output from the rsync command can inform if the file was deleted
			//see: http://samba.anu.edu.au/ftp/rsync/rsync.html section on itemize changes
			
			// Append a timestamp to local version
            string timestamp = DateTime.Now.ToString ("HH:mm MMM d");
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
		
		private string RemoteChanges ()
		{
			SparkleRsync rsync = new SparkleRsync (LocalPath,
                "--archive --itemize --compress --dry-run --partial --delete --exclude-from=.sparkleshare \"" + base.remote_url + "\" " + ".");

            rsync.Start ();
            rsync.WaitForExit ();

            string remote_revision = rsync.StandardOutput.ReadToEnd ().TrimEnd ();
			
			return remote_revision ;
		}
		
		private string LocalChanges ()
		{
		  	SparkleRsync rsync = new SparkleRsync (LocalPath,
            	"--archive --itemize --compress --dry-run --partial --delete --exclude-from=.sparkleshare ." + "\"" + base.remote_url + "\"");

            rsync.Start ();
            rsync.WaitForExit ();

            string remote_revision = rsync.StandardOutput.ReadToEnd ().TrimEnd ();
			
			return remote_revision;
		}
		
		private long CountLinesInString(string s)
	    {
			long count = 1;
			int start = 0;
			while ((start = s.IndexOf('\n', start)) != -1)
			{
			    count++;
			    start++;
			}
			return count;
	    }
	}
}
