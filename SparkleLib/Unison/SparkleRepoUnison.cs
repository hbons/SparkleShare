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

    public class SparkleRepoUnison : SparkleRepoBase {

        public SparkleRepoUnison (string path, SparkleBackend backend) :
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
		
		private string ListUnisonChanges ()
		{
			//set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
			Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
			
			SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "sparkleshare");

            unison.Start ();
			
			//need to wait until local appears - this won't work
			while (!unison.StandardOutput.ReadToEnd().Contains("local") && !unison.StandardOutput.ReadToEnd().Contains("Nothing") );
			
			unison.StandardInput.WriteLine("L"); //list changes (low verbosity)
			//what happens if there are no changes but it still presses L and q? -- does it break?
			unison.StandardInput.Flush();
			//unison.StandardInput.Close();
			unison.StandardInput.WriteLine("q"); //quit unison
			unison.StandardInput.Flush();
			unison.StandardInput.Close();
            unison.WaitForExit ();

            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());
			
			string remote_revision = unison.StandardOutput.ReadToEnd ().TrimEnd ();
			
			return remote_revision;
		}

		private bool CheckForChangesBothWays ()
		{			
            string remote_revision = ListUnisonChanges ();
			
			if (!remote_revision.Contains ("Nothing to do: replicas have not changed since last sync.")) {
                SparkleHelpers.DebugInfo ("Unison", "[" + Name + "] Remote changes found. (" + remote_revision + ")");
                return true;
            } else {
                return false;
            }
		}
		
		
        public override bool CheckForRemoteChanges ()
        {
			return CheckForChangesBothWays ();
        }
		
		
		private bool SyncBothWays ()
		{
			string remote_revision = ListUnisonChanges ();
			
			//check for conflicts before syncing
			if (remote_revision.Contains ("<-?->"))
			    ResolveConflicts (remote_revision);
			
			//set UNISON=./.sparkleshare to store archive files locally and reference profiles locally
			Environment.SetEnvironmentVariable("UNISON", "./.sparkleshare");
			
			//sync both folders now!
			//doesn't ask any questions - just syncs - needs conflicts to have been eliminated by the previous step
            SparkleUnison unison = new SparkleUnison (LocalPath,
                "-ui text " +
                "-batch " +
                "sparkleshare");

            unison.Start ();
            unison.WaitForExit ();

            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());

            if (unison.ExitCode != 0)
                return false;
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
			//unison output lines look like this when they are conflicts:
			//changed  <-?-> changed -- changed on the server and locally
			//deleted  <-?-> changed -- deleted locally, changed on server
			//changed  <-?-> deleted -- changed locally, deleted on server
			//new file <-?-> new file -- new file on the server and a new file locally
			
            //split lines and trim space
            string [] lines = remote_revision.Split ("\n".ToCharArray ());
			foreach (string line in lines) 
			{
				//check to see if the line describes a conflict
				if ( line.Contains ("<-?->") && !line.Contains ("[]") )
				{
					string conflict = line.Trim ();
					string conflicting_path = ""; 
					
					//the end of the line contains the relative path/file name
					//how to deal with spaces in filename?
				
					//when conflicts are identified copy/rename the neccesary files
					
		            // Append a timestamp to local version
		            string timestamp            = DateTime.Now.ToString ("HH:mm MMM d");
		            string their_path           = conflicting_path + " (" + SparkleConfig.DefaultConfig.UserName + ", " + timestamp + ")";
		            string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
		            string abs_their_path       = Path.Combine (LocalPath, their_path);
		
		            //File.Move (abs_conflicting_path, abs_their_path);
				}
			}
		}
		

        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
            //parse the unison log (.sparkleshare/log)
			var l = new List<SparkleChangeSet> ();
            l.Add (new SparkleChangeSet () { UserName = "test", UserEmail = "test", Revision = "test", Timestamp = DateTime.Now });
            return l;
        }


        public override void CreateInitialChangeSet ()
        {
			//?
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