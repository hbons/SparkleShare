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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Mono.Unix;
using Mono.Unix.Native;
using SparkleShare;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	public class SparkleUI {
		
		public static SparkleStatusIcon NotificationIcon;
		public static List <SparkleRepo> Repositories;

		private Process Process;


		// Short alias for the translations
		public static string _(string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleUI (bool HideUI)
		{

			Repositories = new List <SparkleRepo> ();

			Process = new Process () {
				EnableRaisingEvents = true
			};
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			EnableSystemAutostart ();
			InstallLauncher ();

			// Create the SparkleShare folder and add it to the bookmarks
			if (!Directory.Exists (SparklePaths.SparklePath)) {

				CreateSparkleShareFolder ();
				AddToBookmarks ();

			}

			// Create a directory to store temporary files in
			if (!Directory.Exists (SparklePaths.SparkleTmpPath))
				Directory.CreateDirectory (SparklePaths.SparkleTmpPath);

			CreateConfigurationFolders ();
			UpdateRepositories ();

			// Don't create the window and status 
			// icon when --disable-gui was given
			if (!HideUI) {

				// Show the intro screen if there are no folders
				if (Repositories.Count == 0) {

					SparkleIntro intro = new SparkleIntro ();
					intro.ShowAll ();

				} else {

					NotificationIcon = new SparkleStatusIcon ();

				}

			}

		}


		// Creates a folder in the user's home folder to store configuration
		public void CreateConfigurationFolders ()
		{

			string config_path     = SparklePaths.SparkleConfigPath;
			string local_icon_path = SparklePaths.SparkleLocalIconPath;

			if (!Directory.Exists (config_path)) {

				// Create a folder to store settings
				Directory.CreateDirectory (config_path);
				SparkleHelpers.DebugInfo ("Config", "Created '" + config_path + "'");

				// Create a folder to store the avatars
				Directory.CreateDirectory (local_icon_path);
				SparkleHelpers.DebugInfo ("Config", "Created '" + local_icon_path + "'");

				string notify_setting_file = SparkleHelpers.CombineMore (config_path, "sparkleshare.notify");

				// Enable notifications by default				
				if (!File.Exists (notify_setting_file))
					File.Create (notify_setting_file);

			}

		}


		// Creates .desktop entry in autostart folder to
		// start SparkleShare automnatically at login
		public void EnableSystemAutostart ()
		{
		
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
					                  "X-GNOME-Autostart-enabled=true\n" +
					                  "Categories=Network");
					writer.Close ();

					// Give the launcher the right permissions so it can be launched by the user
					Syscall.chmod (desktopfile_path, FilePermissions.S_IRWXU);

					SparkleHelpers.DebugInfo ("Config", "Created '" + desktopfile_path + "'");

				}

		}
		

		// Installs a launcher so the user can launch SparkleShare
		// from the Internet category if needed
		public void InstallLauncher ()
		{
		
			string apps_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".local", "share", "applications");
			string desktopfile_path = SparkleHelpers.CombineMore (apps_path, "sparkleshare.desktop");

			if (!File.Exists (desktopfile_path)) {

				if (!Directory.Exists (apps_path))
					Directory.CreateDirectory (apps_path);

					TextWriter writer = new StreamWriter (desktopfile_path);
					writer.WriteLine ("[Desktop Entry]\n" +
					                  "Type=Application\n" +
					                  "Name=SparkleShare\n" +
					                  "Comment=Share documents\n" +
					                  "Exec=sparkleshare start\n" +
					                  "Icon=folder-sparkleshare\n" +
					                  "Terminal=false\n" +
					                  "Categories=Network");
					writer.Close ();

					// Give the launcher the right permissions so it can be launched by the user
					Syscall.chmod (desktopfile_path, FilePermissions.S_IRWXU);

					SparkleHelpers.DebugInfo ("Config", "Created '" + desktopfile_path + "'");

				}
		
		}


		// Adds the SparkleShare folder to the user's
		// list of bookmarked folders
		public void AddToBookmarks ()
		{

			string bookmarks_file_name = Path.Combine (SparklePaths.HomePath, ".gtk-bookmarks");

			if (File.Exists (bookmarks_file_name)) {

				TextWriter writer = File.AppendText (bookmarks_file_name);
				writer.WriteLine ("file://" + SparklePaths.SparklePath + " SparkleShare");
				writer.Close ();

			}

		}


		// Creates the SparkleShare folder in the user's home folder if
		// it's not already there
		public void CreateSparkleShareFolder ()
		{
		
			Directory.CreateDirectory (SparklePaths.SparklePath);
			SparkleHelpers.DebugInfo ("Config", "Created '" + SparklePaths.SparklePath + "'");
				
			// Add a special icon to the SparkleShare folder
			Process.StartInfo.FileName = "gvfs-set-attribute";
			Process.StartInfo.Arguments = SparklePaths.SparklePath + " metadata::custom-icon " +
			                              "file:///usr/share/icons/hicolor/48x48/places/" +
			                              "folder-sparkleshare.png";
			Process.Start ();
		
		}


		// Shows a notification bubble when someone
		// made a change to the repository
		public void ShowNewCommitBubble (string author, string email, string message) {

			string notify_settings_file = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
				"sparkleshare.notify");

			if (File.Exists (notify_settings_file)) {

				SparkleBubble bubble= new SparkleBubble (author, message);
				bubble.Icon = SparkleHelpers.GetAvatar (email, 32);
				bubble.Show ();

			}

		}


		// Shows a notification bubble when there
		// was a conflict
		public void ShowConflictBubble (object o, EventArgs args) {

			string title   = _("Ouch! Mid-air collision!");
			string subtext = _("Don't worry, SparkleShare made a copy of each conflicting file.");

			SparkleBubble bubble = new SparkleBubble(title, subtext);
			bubble.Show ();

		}


		// Updates the statusicon to the syncing state
		public void UpdateStatusIconSyncing (object o, EventArgs args)
		{

				NotificationIcon.SyncingReposCount++;
				NotificationIcon.ShowState ();

		}


		// Updates the syncing icon to the idle state
		public void UpdateStatusIconIdle (object o, EventArgs args)
		{

				NotificationIcon.SyncingReposCount--;
				NotificationIcon.ShowState ();

		}


		// Updates the list of repositories with all the
		// folders in the SparkleShare folder
		public void UpdateRepositories ()
		{

			Repositories = new List <SparkleRepo> ();

			foreach (string folder in Directory.GetDirectories (SparklePaths.SparklePath)) {

				// Check if the folder is a git repo
				if (Directory.Exists (SparkleHelpers.CombineMore (folder, ".git"))) {

					SparkleRepo repo = new SparkleRepo (folder);

					repo.NewCommit += delegate (object o, NewCommitArgs args) {
						Application.Invoke (delegate { ShowNewCommitBubble (args.Author, args.Email, args.Message); });
					};

					repo.Commited += delegate (object o, SparkleEventArgs args) {
						Application.Invoke (delegate { CheckForUnicorns (args.Message); });
					};

					repo.FetchingStarted += delegate {
						Application.Invoke (UpdateStatusIconSyncing);
					};

					repo.FetchingFinished += delegate {
						Application.Invoke (UpdateStatusIconIdle);
					};

					repo.PushingStarted += delegate {
						Application.Invoke (UpdateStatusIconSyncing);
					};

					repo.PushingFinished += delegate {
						Application.Invoke (UpdateStatusIconIdle);
					};

					repo.ConflictDetected += delegate {
						Application.Invoke (ShowConflictBubble);
					};

					Repositories.Add (repo);

				}

			}

		}


		// Warns the user implicitly that unicorns are actually lethal creatures
		public static void CheckForUnicorns (string message) {

			message = message.ToLower ();

			if (message.Contains ("unicorn") && (message.Contains (".png") || message.Contains (".jpg"))) {

				string title   = _("Hold your ponies!");
				string subtext = _("SparkleShare is known to be insanely fast with " +
				                   "pictures of unicorns. Please make sure your internets " +
				                   "are upgraded to the latest version to avoid any problems.");

				SparkleBubble bubble = new SparkleBubble (title, subtext);
				bubble.Show ();

			}

		}

	}

}
