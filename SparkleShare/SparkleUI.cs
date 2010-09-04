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
using NDesk.DBus;
using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SparkleShare {

	public class SparkleUI {
		
		public static List <SparkleRepo> Repositories;
		public static SparkleStatusIcon NotificationIcon;


		// Short alias for the translations
		public static string _(string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleUI (bool HideUI)
		{

			BusG.Init ();
			Gtk.Application.Init ();

			SetProcessName ("sparkleshare");

			// The list of repositories
			Repositories = new List <SparkleRepo> ();

			EnableSystemAutostart ();
			InstallLauncher ();


			// Create the SparkleShare folder and add it to the bookmarks
			if (!Directory.Exists (SparklePaths.SparklePath)) {

				CreateSparkleShareFolder ();
				AddToBookmarks ();

			}


			// Watch the SparkleShare folder and update the repo list
			// when a deletion occurs.
			FileSystemWatcher watcher = new FileSystemWatcher (SparklePaths.SparklePath) {
				IncludeSubdirectories = false,
				EnableRaisingEvents   = true,
				Filter                = "*"
			};


			// Remove the repository when a delete event occurs
			watcher.Deleted += delegate (object o, FileSystemEventArgs args) {

				RemoveRepository (args.FullPath);

			};

			// Add the repository when a create event occurs
			watcher.Created += delegate (object o, FileSystemEventArgs args) {

				// Handle invitations when the user saves an
				// invitation into the SparkleShare folder
				if (args.Name.EndsWith ("sparkleshare.invitation")) {

					SparkleInvitation invitation;
					invitation = new SparkleInvitation (args.FullPath);

					Application.Invoke (delegate { invitation.PresentInvitation (); });

				} else if (Directory.Exists (args.FullPath)) {

					AddRepository (args.FullPath);

				}

			};


			CreateConfigurationFolders ();

			// Don't create the window and status icon when
			// the --disable-gui command line argument was given
			if (!HideUI) {

				string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

				// Show the introduction screen if SparkleShare isn't configured
				if (!File.Exists (global_config_file_path)) {

					SparkleIntro intro = new SparkleIntro ();
					intro.ShowAll ();

				} else {

					SparkleShare.UserName  = SparkleShare.GetUserName ();
					SparkleShare.UserEmail = SparkleShare.GetUserEmail ();

					SparkleShare.AddKey ();

				}

				// Create the statusicon
				NotificationIcon = new SparkleStatusIcon ();

			}

			PopulateRepositories ();

		}


		// Runs the main loop
		public void Run ()
		{

			Gtk.Application.Run ();

		}


		// Creates a folder in the user's home folder to store configuration
		public void CreateConfigurationFolders ()
		{

			if (!Directory.Exists (SparklePaths.SparkleTmpPath))
				Directory.CreateDirectory (SparklePaths.SparkleTmpPath);

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


		// Creates a .desktop entry in autostart folder to
		// start SparkleShare automatically at login
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
					                  "Categories=Network;");
					writer.Close ();

					// Give the launcher the right permissions so it can be launched by the user
					Syscall.chmod (desktopfile_path, FilePermissions.S_IRWXU);

					SparkleHelpers.DebugInfo ("Config", "Created '" + desktopfile_path + "'");

				}
		
		}


		// Adds the SparkleShare folder to the user's
		// list of bookmarked places
		public void AddToBookmarks ()
		{

			string bookmarks_file_path   = Path.Combine (SparklePaths.HomePath, ".gtk-bookmarks");
			string sparkleshare_bookmark = "file://" + SparklePaths.SparklePath + " SparkleShare";

			if (File.Exists (bookmarks_file_path)) {

				StreamReader reader = new StreamReader (bookmarks_file_path);
				string bookmarks = reader.ReadToEnd ();
				reader.Close ();

				if (!bookmarks.Contains (sparkleshare_bookmark)) {

					TextWriter writer = File.AppendText (bookmarks_file_path);
					writer.WriteLine ("file://" + SparklePaths.SparklePath + " SparkleShare");
					writer.Close ();

				}

			} else {

				StreamWriter writer = new StreamWriter (bookmarks_file_path);
				writer.WriteLine ("file://" + SparklePaths.SparklePath + " SparkleShare");
				writer.Close ();

			}

		}


		// Creates the SparkleShare folder in the user's home folder
		public void CreateSparkleShareFolder ()
		{
		
			Directory.CreateDirectory (SparklePaths.SparklePath);
			SparkleHelpers.DebugInfo ("Config", "Created '" + SparklePaths.SparklePath + "'");

			string icon_file_path = SparkleHelpers.CombineMore (Defines.PREFIX, "share", "icons", "hicolor", "48x48",
				"apps", "folder-sparkleshare.png");

			Process process = new Process ();

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			// Add a special icon to the SparkleShare folder
			process.StartInfo.FileName  = "gvfs-set-attribute";
			process.StartInfo.Arguments = SparklePaths.SparklePath + " metadata::custom-icon " +
			                              "file://" + icon_file_path;
			process.Start ();
		
		}


		// Shows a notification bubble when someone
		// made a change to the repository
		public void ShowNewCommitBubble (string author, string email, string message, string repository_name) {

			string notify_settings_file = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
				"sparkleshare.notify");

			if (File.Exists (notify_settings_file)) {

				SparkleBubble bubble = new SparkleBubble (author, message) {
					Icon = SparkleUIHelpers.GetAvatar (email, 32)				
				};

				bubble.AddAction ("", "Show Events", delegate {
				
					string path = SparkleHelpers.CombineMore (SparklePaths.SparklePath, repository_name);

					SparkleLog log = new SparkleLog (path);
					log.ShowAll ();
				
				});

				bubble.Show ();

			}

		}


		// Shows a notification bubble when there
		// was a conflict
		public void ShowConflictBubble (object o, EventArgs args)
		{

			string title   = _("Ouch! Mid-air collision!");
			string subtext = _("Don't worry, SparkleShare made a copy of each conflicting file.");

			SparkleBubble bubble = new SparkleBubble(title, subtext);
			bubble.Show ();

		}


		// Updates the statusicon to the syncing state
		public void UpdateStatusIconToSyncing (object o, EventArgs args)
		{

				NotificationIcon.SyncingReposCount++;
				NotificationIcon.ShowState ();

		}


		// Updates the statusicon to the error state
		public void UpdateStatusIconToError (object o, EventArgs args)
		{

				NotificationIcon.SyncingReposCount--;
				NotificationIcon.ShowState (true);

		}


		// Updates the syncing icon to the idle state
		public void UpdateStatusIconToIdle (object o, EventArgs args)
		{

				NotificationIcon.SyncingReposCount--;
				NotificationIcon.ShowState ();

		}


		// Adds a repository to the list of repositories and
		// updates the statusicon menu
		public void AddRepository (string folder_path)
		{
		
			// Check if the folder is a git repo
			if (!Directory.Exists (SparkleHelpers.CombineMore (folder_path, ".git")))
				return;

			SparkleRepo repo = new SparkleRepo (folder_path);

			repo.NewCommit += delegate (object o, NewCommitArgs args) {
				Application.Invoke (delegate { ShowNewCommitBubble (args.Author, args.Email, args.Message,
					args.RepositoryName); });
			};

			repo.Commited += delegate (object o, SparkleEventArgs args) {
				Application.Invoke (delegate { CheckForUnicorns (args.Message); });
			};

			repo.FetchingStarted += delegate {
				Application.Invoke (UpdateStatusIconToSyncing);
			};

			repo.FetchingFinished += delegate {
				Application.Invoke (UpdateStatusIconToIdle);
			};

			repo.ChangesDetected += delegate {
				Application.Invoke (UpdateStatusIconToSyncing);
			};

			repo.PushingFinished += delegate {
				Application.Invoke (UpdateStatusIconToIdle);
			};

			repo.CommitEndedUpEmpty += delegate {
				Application.Invoke (UpdateStatusIconToIdle);
			};

			repo.PushingFailed += delegate {
				Application.Invoke (UpdateStatusIconToError);
			};

			repo.ConflictDetected += delegate {
				Application.Invoke (ShowConflictBubble);
			};

			Repositories.Add (repo);

			if (NotificationIcon != null)
				Application.Invoke (delegate { NotificationIcon.CreateMenu (); });

		}


		// Removes a repository from the list of repositories and
		// updates the statusicon menu
		public void RemoveRepository (string folder_path)
		{

			string repo_name = Path.GetFileName (folder_path);

			foreach (SparkleRepo repo in Repositories) {

				if (repo.Name.Equals (repo_name)) {

					repo.Stop ();
					Repositories.Remove (repo);
					break;

				}

			}

			if (NotificationIcon != null)
				Application.Invoke (delegate { NotificationIcon.CreateMenu (); });

		}


		// Updates the list of repositories with all the
		// folders in the SparkleShare folder
		public void PopulateRepositories ()
		{

			Repositories = new List <SparkleRepo> ();

			foreach (string folder_path in Directory.GetDirectories (SparklePaths.SparklePath))
				AddRepository (folder_path);

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


		// Sets the unix process name to 'sparkleshare' instead of 'mono'
		private void SetProcessName (string name)
		{

			try {

				if (prctl (15, Encoding.ASCII.GetBytes (name + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) {

					throw new ApplicationException ("Error setting process name: " +
						Mono.Unix.Native.Stdlib.GetLastError ());

				}

			} catch (EntryPointNotFoundException) {

				Console.WriteLine ("SetProcessName: Entry point not found");

			}

		}


		// Strange magic needed by SetProcessName
		[DllImport ("libc")]
		private static extern int prctl (int option, byte [] arg2, IntPtr arg3,	IntPtr arg4, IntPtr arg5);

	}

}
