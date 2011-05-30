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

		private override string CheckForChangesBothWays ()
		{
			 SparkleUnison unison = new SparkleUnison (LocalPath,
                "-auto -ui text -logfile .unisonlog . \"" + base.remote_url);

            unison.Start ();
			unison.StandardInput.Write("q"); //quit unison without making the recommended changes
			unison.StandardInput.Flush();
			unison.StandardInput.Close();
            unison.WaitForExit ();

            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());
			
			string remote_revision = unison.StandardOutput.ReadToEnd ().TrimEnd ();
			
			return remote_revision;
		}
		
		
        public override bool CheckForRemoteChanges ()
        {
			remote_revision = CheckForChangesBothWays ();
			//parse remote_revision to check for remote changes
            return true;
        }
		
		
		private override bool SyncBothWays ()
		{
           SparkleUnison unison = new SparkleUnison (LocalPath,
                "-auto -batch -ui text -logfile .unisonlog . \"" + base.remote_url);

            unison.Start ();
            unison.WaitForExit ();

            SparkleHelpers.DebugInfo ("Unison", "Exit code " + unison.ExitCode.ToString ());

            if (unison.ExitCode != 0)
                return false;
            else 
                return true;     
		}

		//not really a distiction between syncing up or down 
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
				remote_revision = CheckForChangesBothWays ();
				//parse remote_revision to check for remote changes
		        return true;
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

		private void ResolveConflict ()
		{
			remote_revision = CheckForChangesBothWays ();
			//parse remote_revision to check for conflicts
			//they are represented by <-?->
			
			//append timestamp
			string timestamp = DateTime.Now.ToString ("HH:mm MMM d");
			
			//append username to the local copy then transfer it
			SparkleConfig.DefaultConfig.UserName
		}
		

        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
            //parse the unison log (.unisonlog)
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
