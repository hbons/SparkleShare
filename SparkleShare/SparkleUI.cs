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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	public class SparkleUI {

		public static SparkleStatusIcon NotificationIcon;

		public SparkleUI (bool HideUI) {

			Process Process = new Process ();
			Process.EnableRaisingEvents = false;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			string SparklePath = SparklePaths.SparklePath;

			// Create 'SparkleShare' folder in the user's home folder
			// if it's not there already
			if (!Directory.Exists (SparklePath)) {

				Directory.CreateDirectory (SparklePath);
				Console.WriteLine ("[Config] Created '" + SparklePath + "'");
					
				// Add a special icon to the SparkleShare folder
				switch (SparklePlatform.Name) {
					case "GNOME":
						Process.StartInfo.FileName = "gvfs-set-attribute";
						Process.StartInfo.Arguments =
							SparklePath + " metadata::custom-icon " +
	                  "file:///usr/share/icons/hicolor/48x48/places/" +
	                  "folder-sparkleshare.png";
						Process.Start ();
						break;
				}

				// Add the SparkleShare folder to the bookmarks
				switch (SparklePlatform.Name) {
					case "GNOME":

						string BookmarksFileName =
							Path.Combine (SparklePaths.HomePath, ".gtk-bookmarks");

						if (File.Exists (BookmarksFileName)) {
							TextWriter TextWriter = File.AppendText (BookmarksFileName);
							TextWriter.WriteLine ("file://" + SparklePath + " SparkleShare");
							TextWriter.Close ();
						}

						break;

				}

			}

			if (!HideUI) {
				// Create the status icon
				NotificationIcon = new SparkleStatusIcon ();
			}

			// Get all the repos in ~/SparkleShare
			SparkleRepo [] TmpRepos =
				new SparkleRepo [Directory.GetDirectories (SparklePath).Length];

			int i = 0;
			foreach (string Folder in Directory.GetDirectories (SparklePath)) {

				// Check if the folder is a git repo
				if (Directory.Exists (SparkleHelpers.CombineMore (Folder,
				                                                  ".git"))) {
					TmpRepos [i] = new SparkleRepo (Folder);
					i++;

					// Attach emblems
					// TODO: emblems don't work in nautilus
					switch (SparklePlatform.Name) {
						case "GNOME":
							Process.StartInfo.FileName = "gvfs-set-attribute";
							Process.StartInfo.Arguments =
								Folder + " metadata::emblems [synced]";
							Console.WriteLine (Process.StartInfo.FileName + " " + Process.StartInfo.Arguments);
							Process.Start ();
						break;
					}

				}

			}
			
			SparkleShare.Repositories = new SparkleRepo [i];
			Array.Copy (TmpRepos, SparkleShare.Repositories, i);

			// Don't create the window and status 
			// icon when --disable-gui was given
			if (!HideUI) {
				
				// Show a notification if there are no folders yet
				if (SparkleShare.Repositories.Length == 0) {

					SparkleBubble NoFoldersBubble;
					NoFoldersBubble = new SparkleBubble ("Welcome to SparkleShare!",
					                                     "You don't have any " +
					                                     "folders set up yet.");

					NoFoldersBubble.IconName = "folder-sparkleshare";
					NoFoldersBubble.AddAction ("", "Add a Folder…", delegate {
						SparkleDialog SparkleDialog = new SparkleDialog ("");
						SparkleDialog.ShowAll ();
/*						Process.StartInfo.FileName = "xdg-open";
						Process.StartInfo.Arguments = SparklePaths.SparklePath;
						Process.Start ();
*/
					} );
					
					NoFoldersBubble.Show ();

				}

			}
			
			// TODO: When a repo folder is deleted, don't sync and update the UI
			
			// Watch the SparkleShare folder and pop up the 
			// Add dialog when a new folder is created
			// TODO: This crashes
/*			FileSystemWatcher Watcher = new FileSystemWatcher (SparklePaths.SparklePath);
			Watcher.IncludeSubdirectories = false;
			Watcher.EnableRaisingEvents = true;
			Watcher.Created += delegate (object o, FileSystemEventArgs args) {
			   WatcherChangeTypes wct = args.ChangeType;
				Console.WriteLine ("[Event][SparkleShare] " + wct.ToString () + 
				                   " '" + args.Name + "'");
				SparkleDialog SparkleDialog = new SparkleDialog ();
				SparkleDialog.ShowAll ();
			};
*/

			// Create place to store configuration user's home folder
			string ConfigPath = SparklePaths.SparkleConfigPath;
			string AvatarPath = SparklePaths.SparkleAvatarPath;

			if (!Directory.Exists (ConfigPath)) {

				Directory.CreateDirectory (ConfigPath);
				Console.WriteLine ("[Config] Created '" + ConfigPath + "'");

				// Create a place to store the avatars
				Directory.CreateDirectory (AvatarPath);
				Console.WriteLine ("[Config] Created '" + AvatarPath + "avatars'");

			}
			
			string NotifySettingFile =
				SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
					                         "sparkleshare.notify");

			// Enable notifications by default				
			if (!File.Exists (NotifySettingFile))
				File.Create (NotifySettingFile);

		}

	}

}
