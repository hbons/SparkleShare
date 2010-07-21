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
using Mono.Unix;
using Mono.Unix.Native;
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	public class SparkleUI {
		
		private Process Process;

		// Short alias for the translations
		public static string _(string s)
		{
			return Catalog.GetString (s);
		}

		public static SparkleStatusIcon NotificationIcon;

		public SparkleUI (bool HideUI)
		{

			Process = new Process ();
			Process.EnableRaisingEvents = true;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			string SparklePath = SparklePaths.SparklePath;

			EnableSystemAutostart ();
			CreateSparkleShareFolder ();

			// Create a directory to store temporary files in
			if (!Directory.Exists (SparklePaths.SparkleTmpPath))
				Directory.CreateDirectory (SparklePaths.SparkleTmpPath);

			UpdateRepositories ();

			// Don't create the window and status 
			// icon when --disable-gui was given
			if (!HideUI) {

				SparkleIntro intro = new SparkleIntro ();
				intro.ShowAll ();

				NotificationIcon = new SparkleStatusIcon ();
				// Show a notification if there are no folders yet
				if (SparkleShare.Repositories.Length == 0) {

					SparkleBubble NoFoldersBubble;
					NoFoldersBubble = new SparkleBubble (_("Welcome to SparkleShare!"),
					                                     _("You don't have any folders set up yet."));

					NoFoldersBubble.IconName = "folder-sparkleshare";
					NoFoldersBubble.AddAction ("", _("Add a Folder…"), delegate {
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
						

			// TODO: This crashes
/*

			// Watch the SparkleShare folder and pop up the 
			// Add dialog when a new folder is created

			FileSystemWatcher Watcher = new FileSystemWatcher (SparklePaths.SparklePath);
			Watcher.IncludeSubdirectories = false;
			Watcher.EnableRaisingEvents = true;
			Watcher.Created += delegate (object o, FileSystemEventArgs args) {
			   WatcherChangeTypes wct = args.ChangeType;
				SparkleHelpers.DebugInfo ("Event",
				                          wct.ToString () + 
				                          " '" + args.Name + "'");
				SparkleDialog SparkleDialog = new SparkleDialog ();
				SparkleDialog.ShowAll ();
			};

			// When a repo folder is deleted, don't sync and update the UI
			Watcher.Deleted += delegate (object o, FileSystemEventArgs args) {
			   WatcherChangeTypes wct = args.ChangeType;
				SparkleHelpers.DebugInfo ("Event",
				                          wct.ToString () + 
				                          " '" + args.Name + "'");
				SparkleUI SparkleUI = new SparkleUI ();
				SparkleUI.ShowAll ();
			};
*/

			// Create place to store configuration user's home folder
			string ConfigPath = SparklePaths.SparkleConfigPath;
			string LocalIconPath = SparklePaths.SparkleLocalIconPath;

			if (!Directory.Exists (ConfigPath)) {

				Directory.CreateDirectory (ConfigPath);
				SparkleHelpers.DebugInfo ("Config", "Created '" + ConfigPath + "'");

				// Create a place to store the avatars
				Directory.CreateDirectory (LocalIconPath);
				SparkleHelpers.DebugInfo ("Config", "Created '" + LocalIconPath + "'");

			}
			
			string NotifySettingFile = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
				"sparkleshare.notify");

			// Enable notifications by default				
			if (!File.Exists (NotifySettingFile))
				File.Create (NotifySettingFile);

		}
		
		// Creates .desktop entry in autostart folder to
		// start SparkleShare automnatically at login
		public void EnableSystemAutostart ()
		{
		
			switch (SparklePlatform.Name) {

				case "GNOME":

					string autostart_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".config", "autostart");
					string desktopfile_path = SparkleHelpers.CombineMore (autostart_path, "sparkleshare.desktop");

					if (!File.Exists (desktopfile_path)) {

						if (!Directory.Exists (autostart_path))
							Directory.CreateDirectory (autostart_path);

						TextWriter writer = new StreamWriter (desktopfile_path);

						writer.WriteLine ("[Desktop Entry]\n" +
						                  "Type=Application\n" +
						                  "Name=SparkleShare\n" +
						                  "Exec=sparkleshare start\n" +
						                  "Icon=folder-sparkleshare\n" +
						                  "Terminal=false\n" +
						                  "X-GNOME-Autostart-enabled=true");

						writer.Close ();

						// Give the launcher the right permissions so it can be launched by the user
						Syscall.chmod (desktopfile_path, FilePermissions.S_IRWXU);

						SparkleHelpers.DebugInfo ("Config", "Created '" + desktopfile_path + "'");

					}

				break;

			}
		
		}
		

		public void AddToBookmarks ()
		{
		
			// Add the SparkleShare folder to the bookmarks
			switch (SparklePlatform.Name) {

				case "GNOME":

					string bookmarks_file_name = Path.Combine (SparklePaths.HomePath, ".gtk-bookmarks");

					if (File.Exists (bookmarks_file_name)) {
						TextWriter writer = File.AppendText (bookmarks_file_name);
						writer.WriteLine ("file://" + SparklePaths.SparklePath + " SparkleShare");
						writer.Close ();
					}

					break;

			}

		}


		// Creates the 'SparkleShare' folder in the user's home folder if
		// it's not already there
		public void CreateSparkleShareFolder ()
		{
		
			if (!Directory.Exists (SparklePaths.SparklePath)) {

				Directory.CreateDirectory (SparklePaths.SparklePath);
				SparkleHelpers.DebugInfo ("Config", "Created '" + SparklePaths.SparklePath + "'");
					
				// Add a special icon to the SparkleShare folder
				switch (SparklePlatform.Name) {

					case "GNOME":

						Process.StartInfo.FileName = "gvfs-set-attribute";
						Process.StartInfo.Arguments = SparklePaths.SparklePath + " metadata::custom-icon " +
							"file:///usr/share/icons/hicolor/48x48/places/" +
							"folder-sparkleshare.png";
						Process.Start ();

						break;

				}

				AddToBookmarks ();

			}
		
		}


		public void Test (object o, SparkleEventArgs args) {
			Console.WriteLine ("AAAAAAAAAAAAAAAAAA");
		}


		public void UpdateRepositories ()
		{

			string SparklePath = SparklePaths.SparklePath;
			// Get all the repos in ~/SparkleShare
			SparkleRepo [] TmpRepos = new SparkleRepo [Directory.GetDirectories (SparklePath).Length];
			
			int FolderCount = 0;
			foreach (string folder in Directory.GetDirectories (SparklePath)) {

				// Check if the folder is a git repo
				if (Directory.Exists (SparkleHelpers.CombineMore (folder, ".git"))) {

					TmpRepos [FolderCount] = new SparkleRepo (folder);
					FolderCount++;

					// TODO: emblems don't show up in nautilus
					// Attach emblems
					switch (SparklePlatform.Name) {
						case "GNOME":

							Process.StartInfo.FileName = "gvfs-set-attribute";
							Process.StartInfo.Arguments = "-t string \"" + folder +
								"\" metadata::emblems [synced]";
							Process.Start ();

						break;

					}

				}

			}

					SparkleRepo a = TmpRepos [0];
					a.Added += Test;


			SparkleShare.Repositories = new SparkleRepo [FolderCount];
			Array.Copy (TmpRepos, SparkleShare.Repositories, FolderCount);


	}
}	


}
