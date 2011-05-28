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

		//need to see if there is a way to pass the changes detected later to avoid checking twice
        public override bool CheckForRemoteChanges ()
        {
			SparkleRsync rsync = new SparkleRsync (LocalPath,
                "-aizvPn \"" + base.remote_url + "\" " + "\".\"");

            rsync.Start ();
            rsync.WaitForExit ();

            string remote_revision = rsync.StandardOutput.ReadToEnd ().TrimEnd ();

            if (CountLinesInString(remote_revision) > 4) {
                SparkleHelpers.DebugInfo ("Rsync", "[" + Name + "] Remote changes found. (" + remote_revision + ")");
                return true;
            } else {
                return false;
            }
        }


        public override bool SyncUp ()
        {
			SparkleRsync rsync = new SparkleRsync (LocalPath,
                "-aizvP \".\" " + "\"" + base.remote_url + "\"");

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
                "-aizvP \"" + base.remote_url + "\" " + "\".\"");

            rsync.Start ();
            rsync.WaitForExit ();

            if (rsync.ExitCode == 0)
                return true;
            else
                return false;
        }


        public override bool AnyDifferences {
            get {
                SparkleRsync rsync = new SparkleRsync (LocalPath,
                	"-aizvPn \".\" " + "\"" + base.remote_url + "\"");

	            rsync.Start ();
	            rsync.WaitForExit ();
	
	            string remote_revision = rsync.StandardOutput.ReadToEnd ().TrimEnd ();
	
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


        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
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
