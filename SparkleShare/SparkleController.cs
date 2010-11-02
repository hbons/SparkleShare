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

using Mono.Unix;
using Mono.Unix.Native;
using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SparkleShare {

	public class SparkleController {

		public List <SparkleRepo> Repositories;
		public string FolderSize;


		public event RepositoryListChangedEventHandler RepositoryListChanged;
		public delegate void RepositoryListChangedEventHandler ();

		public event FolderSizeChangedEventHandler FolderSizeChanged;
		public delegate void FolderSizeChangedEventHandler (string folder_size);

		public event OnIdleEventHandler OnIdle;
		public delegate void OnIdleEventHandler ();

		public event OnSyncingEventHandler OnSyncing;
		public delegate void OnSyncingEventHandler ();

		public event OnErrorEventHandler OnError;
		public delegate void OnErrorEventHandler ();

		public event OnFirstRunEventHandler OnFirstRun;
		public delegate void OnFirstRunEventHandler ();

		public event OnInvitationEventHandler OnInvitation;
		public delegate void OnInvitationEventHandler (string invitation_file_path);

		public event ConflictNotificationRaisedEventHandler ConflictNotificationRaised;
		public delegate void ConflictNotificationRaisedEventHandler ();

		public event NotificationRaisedEventHandler NotificationRaised;
		public delegate void NotificationRaisedEventHandler (SparkleCommit commit, string repository_path);


		public SparkleController ()
		{

			SetProcessName ("sparkleshare");

			InstallLauncher ();
			EnableSystemAutostart ();

			// Create the SparkleShare folder and add it to the bookmarks
			if (CreateSparkleShareFolder ())
				AddToBookmarks ();

			FolderSize = GetFolderSize ();

			// Watch the SparkleShare folder
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
				if (args.Name.EndsWith (".invitation")) {

					if (OnInvitation != null)
						OnInvitation (args.FullPath);

				} else if (Directory.Exists (Path.Combine (args.FullPath, ".git"))) {

					AddRepository (args.FullPath);

				}

			};


			CreateConfigurationFolders ();

			string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			// Show the introduction screen if SparkleShare isn't configured
			if (!File.Exists (global_config_file_path)) {

				if (OnFirstRun != null)
					OnFirstRun ();

			} else {

				SparkleShare.UserName  = SparkleShare.GetUserName ();
				SparkleShare.UserEmail = SparkleShare.GetUserEmail ();

				SparkleShare.AddKey ();

			}

			Thread thread = new Thread (
				new ThreadStart (PopulateRepositories)
			);

			thread.Start ();

		}


		// Creates a folder in the user's home folder to store configuration
		private void CreateConfigurationFolders ()
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
		private void EnableSystemAutostart ()
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
		private void InstallLauncher ()
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
		private void AddToBookmarks ()
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
		private bool CreateSparkleShareFolder ()
		{

			if (!Directory.Exists (SparklePaths.SparklePath)) {
		
				Directory.CreateDirectory (SparklePaths.SparklePath);
				SparkleHelpers.DebugInfo ("Config", "Created '" + SparklePaths.SparklePath + "'");

				string icon_file_path = SparkleHelpers.CombineMore (Defines.PREFIX, "share", "icons", "hicolor",
					"48x48", "apps", "folder-sparkleshare.png");

				string gvfs_command_path = SparkleHelpers.CombineMore (Path.VolumeSeparatorChar.ToString (),
					"usr", "bin", "gvfs-set-attribute");

				// Add a special icon to the SparkleShare folder
				if (File.Exists (gvfs_command_path)) {

					Process process = new Process ();

					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.UseShellExecute = false;

					process.StartInfo.FileName  = "gvfs-set-attribute";
					process.StartInfo.Arguments = SparklePaths.SparklePath + " metadata::custom-icon " +
							                      "file://" + icon_file_path;
					process.Start ();

				}

				return true;

			}

			return false;

		}


		// Fires events for the current syncing state
		private void UpdateState ()
		{

			foreach (SparkleRepo repo in Repositories) {

				if (repo.IsSyncing || repo.IsBuffering) {

					if (OnSyncing != null)
						OnSyncing ();

					return;

				} else if (repo.HasUnsyncedChanges) {
	
					if (OnError != null)
						OnError ();

					return;

				}
	
			}


			if (OnIdle != null)
				OnIdle ();


			FolderSize = GetFolderSize ();

			if (FolderSizeChanged != null)
				FolderSizeChanged (FolderSize);

		}


		// Adds a repository to the list of repositories
		private void AddRepository (string folder_path)
		{
		
			// Check if the folder is a Git repository
			if (!Directory.Exists (SparkleHelpers.CombineMore (folder_path, ".git")))
				return;

			SparkleRepo repo = new SparkleRepo (folder_path);

			repo.NewCommit += delegate (SparkleCommit commit, string repository_path) {

				if (NotificationsEnabled && NotificationRaised != null)
					NotificationRaised (commit, repository_path);

			};

			repo.FetchingStarted += delegate {
				UpdateState ();
			};

			repo.FetchingFinished += delegate {
				UpdateState ();
			};

			repo.FetchingFailed += delegate {
				UpdateState ();
			};

			repo.ChangesDetected += delegate {
				UpdateState ();
			};

			repo.PushingStarted += delegate {
				UpdateState ();
			};

			repo.PushingFinished += delegate {
				UpdateState ();
			};

			repo.CommitEndedUpEmpty += delegate {
				UpdateState ();
			};

			repo.PushingFailed += delegate {
				UpdateState ();
			};

			repo.ConflictDetected += delegate {
				if (ConflictNotificationRaised != null)
					ConflictNotificationRaised ();
			};

			Repositories.Add (repo);


			if (RepositoryListChanged != null)
				RepositoryListChanged ();

		}


		// Removes a repository from the list of repositories and
		// updates the statusicon menu
		private void RemoveRepository (string folder_path)
		{

			string repo_name = Path.GetFileName (folder_path);

			for (int i = 0; i < Repositories.Count; i++) {

				SparkleRepo repo = Repositories [i];

				if (repo.Name.Equals (repo_name)) {

					Repositories.Remove (repo);
					repo.Dispose ();
					repo = null;
					break;

				}

			}


			if (RepositoryListChanged != null)
				RepositoryListChanged ();

		}


		// Updates the list of repositories with all the
		// folders in the SparkleShare folder
		private void PopulateRepositories ()
		{

			Repositories = new List <SparkleRepo> ();

			foreach (string folder_path in Directory.GetDirectories (SparklePaths.SparklePath))
				AddRepository (folder_path);

			if (RepositoryListChanged != null)
				RepositoryListChanged ();

		}


		public bool NotificationsEnabled {

			get {

				string notify_setting_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
					"sparkleshare.notify");

				return File.Exists (notify_setting_file_path);

			}

		} 


		public void ToggleNotifications () {
		
			string notify_setting_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
				"sparkleshare.notify");
					                                 
			if (File.Exists (notify_setting_file_path))
				File.Delete (notify_setting_file_path);
			else
				File.Create (notify_setting_file_path);

		}


		private string GetFolderSize ()
		{

			double folder_size = CalculateFolderSize (new DirectoryInfo (SparklePaths.SparklePath));
			return FormatFolderSize (folder_size);

		}


		// Recursively gets a folder's size in bytes
		private double CalculateFolderSize (DirectoryInfo parent)
		{

			if (!Directory.Exists (parent.ToString ()))
				return 0;

			double size = 0;

			// Ignore the temporary 'rebase-apply' and '.tmp' directories. This prevents potential
			// crashes when files are being queried whilst the files have already been deleted.
			if (parent.Name.Equals ("rebase-apply") ||
			    parent.Name.Equals (".tmp"))
				return 0;

			foreach (FileInfo file in parent.GetFiles()) {

				if (!file.Exists)
					return 0;

				size += file.Length;

			}

			foreach (DirectoryInfo directory in parent.GetDirectories())
				size += CalculateFolderSize (directory);

		    return size;
    
		}


		// Format a file size nicely with small caps.
		// Example: 1048576 becomes "1 ᴍʙ"
        private string FormatFolderSize (double byte_count)
        {

			if (byte_count >= 1099511627776)

				return String.Format ("{0:##.##}  ᴛʙ", Math.Round (byte_count / 1099511627776, 1));

			else if (byte_count >= 1073741824)

				return String.Format ("{0:##.##} ɢʙ", Math.Round (byte_count / 1073741824, 1));

            else if (byte_count >= 1048576)

				return String.Format ("{0:##.##} ᴍʙ", Math.Round (byte_count / 1048576, 1));

			else if (byte_count >= 1024)

				return String.Format ("{0:##.##} ᴋʙ", Math.Round (byte_count / 1024, 1));

			else

				return byte_count.ToString () + " bytes";

        }


		public void OpenSparkleShareFolder ()
		{

			Process process = new Process ();
			process.StartInfo.Arguments = SparklePaths.SparklePath;

			string open_command_path = SparkleHelpers.CombineMore (Path.VolumeSeparatorChar.ToString (),
				"usr", "bin");

			if (File.Exists (Path.Combine (open_command_path, "xdg-open"))) {

				process.StartInfo.FileName = "xdg-open";

			} else if (File.Exists (Path.Combine (open_command_path, "gnome-open"))) {

				process.StartInfo.FileName = "gnome-open";

			} else if (File.Exists (Path.Combine (open_command_path, "open"))) {

				process.StartInfo.FileName = "open";

			} else {

				return;

			}

			process.Start ();

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

		// Quits the program
		public void Quit ()
		{

			foreach (SparkleRepo repo in Repositories)
				repo.Dispose ();

			// Remove the process ID file
			File.Delete (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, "sparkleshare.pid"));

			Environment.Exit (0);

		}

	}

}
