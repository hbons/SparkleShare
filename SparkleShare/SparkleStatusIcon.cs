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
using SparkleLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace SparkleShare {

	public class SparkleStatusIcon : StatusIcon
	{

		public int SyncingReposCount;

		private Menu Menu;
		private MenuItem StatusMenuItem;
		private string StateText;

		private Timer Animation;
		private Gdk.Pixbuf [] AnimationFrames;
		private int FrameNumber;

		private double FolderSize;


		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}


		public SparkleStatusIcon () : base ()
		{

			FolderSize = GetFolderSize (new DirectoryInfo (SparklePaths.SparklePath));

			FrameNumber = 0;
			AnimationFrames = CreateAnimationFrames ();
			Animation = CreateAnimation ();

			SyncingReposCount = 0;

			StateText = "";
			StatusMenuItem = new MenuItem ();

			CreateMenu ();

			// Primary mouse button click
			Activate += ShowMenu;

			// Secondary mouse button click
			PopupMenu += ShowMenu;

			SetIdleState ();
			ShowState ();

		}


		// Slices up the graphic that contains the
		// animation frames.
		private Gdk.Pixbuf [] CreateAnimationFrames ()
		{

			Gdk.Pixbuf [] animation_frames = new Gdk.Pixbuf [5];
			Gdk.Pixbuf frames_pixbuf = SparkleUIHelpers.GetIcon ("process-syncing-sparkleshare", 24);
			
			for (int i = 0; i < animation_frames.Length; i++)
				animation_frames [i] = new Gdk.Pixbuf (frames_pixbuf, (i * 24), 0, 24, 24);

			return animation_frames;

		}


		// Creates the Animation that handles the syncing animation.
		private Timer CreateAnimation ()
		{

			Timer Animation = new Timer () {
				Interval = 35
			};

			Animation.Elapsed += delegate {

				if (FrameNumber < AnimationFrames.Length - 1)
					FrameNumber++;
				else
					FrameNumber = 0;

				Application.Invoke (delegate {

					SetPixbuf (AnimationFrames [FrameNumber]);

				});

			};

			return Animation;

		}


		// A method reference that makes sure that
		// opening the event log for each repository
		// works correctly.
		private EventHandler OpenLogDelegate (string path)
		{

			return delegate { 

				SparkleLog log = new SparkleLog (path);
				log.ShowAll ();
				log.Present ();

			};

		}


		// Recursively gets a folder's size in bytes
		private double GetFolderSize (DirectoryInfo parent)
		{

			double size = 0;

			// Ignore the temporary 'rebase-apply' directory.
			// This prevents potential crashes when files are being
			// queried whilst the files have already been deleted.
			if (parent.Name.Equals ("rebase-apply"))
				return 0;

			foreach (FileInfo file in parent.GetFiles()) {

				if (!file.Exists)
					return 0;

				size += file.Length;

			}

			foreach (DirectoryInfo directory in parent.GetDirectories())
				size += GetFolderSize (directory);

		    return size;
    
		}


		private void UpdateFolderSize ()
		{

			FolderSize = GetFolderSize (new DirectoryInfo (SparklePaths.SparklePath));

		}


		// Format a file size nicely with small caps.
		// Example: 1048576 becomes "1 ᴍʙ"
        private string FormatFileSize (double byte_count)
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


		// Creates the menu that is popped up when the
		// user clicks the statusicon
		public void CreateMenu ()
		{

				Menu = new Menu ();

					// The menu item showing the status and size of the SparkleShare folder
					StatusMenuItem = new MenuItem (StateText) {
						Sensitive = false
					};

				Menu.Add (StatusMenuItem);
				Menu.Add (new SeparatorMenuItem ());

					// A menu item that provides a link to the SparkleShare folder
					Gtk.Action folder_action = new Gtk.Action ("", "SparkleShare") {
						IconName    = "folder-sparkleshare",
						IsImportant = true
					};

					folder_action.Activated += delegate {

						Process process = new Process ();
						process.StartInfo.FileName = "xdg-open";
						process.StartInfo.Arguments = SparklePaths.SparklePath;
						process.Start ();

					};

				Menu.Add (folder_action.CreateMenuItem ());


				if (SparkleUI.Repositories.Count > 0) {

					// Creates a menu item for each repository with a link to their logs
					foreach (SparkleRepo repo in SparkleUI.Repositories) {

						folder_action = new Gtk.Action ("", repo.Name) {
							IconName    = "folder",
							IsImportant = true
						};

						folder_action.Activated += OpenLogDelegate (repo.LocalPath);

						MenuItem menu_item = (MenuItem) folder_action.CreateMenuItem ();

						if (repo.Description != null)
							menu_item.TooltipText = repo.Description;

						Menu.Add (menu_item);

					}

				} else {

					MenuItem no_folders_item = new MenuItem (_("No Shared Folders Yet")) {
						Sensitive   = false
					};

					Menu.Add (no_folders_item);

				}

				// Opens the wizard to add a new remote folder
				MenuItem add_item = new MenuItem (_("Sync Remote Folder…"));

					add_item.Activated += delegate {

						SparkleIntro intro = new SparkleIntro ();

						// Only show the server form in the wizard
						intro.ShowServerForm (true);

					};

				Menu.Add (add_item);
				Menu.Add (new SeparatorMenuItem ());

				// A checkbutton to toggle whether or not to show notifications
				CheckMenuItem notify_item =	new CheckMenuItem (_("Show Notifications"));

					// Whether notifications are shown depends existence of this file 
					string notify_setting_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
						"sparkleshare.notify");
							                                 
					if (File.Exists (notify_setting_file_path))
						notify_item.Active = true;

					notify_item.Toggled += delegate {

						if (File.Exists (notify_setting_file_path))
							File.Delete (notify_setting_file_path);
						else
							File.Create (notify_setting_file_path);
				
					};

				Menu.Add (notify_item);
				Menu.Add (new SeparatorMenuItem ());

				// A menu item that takes the use to sparkleshare.org
				MenuItem about_item = new MenuItem (_("Visit Website"));

					about_item.Activated += delegate {

						Process process = new Process ();

						process.StartInfo.FileName  = "xdg-open";
						process.StartInfo.Arguments = "http://www.sparkleshare.org/";

						process.Start ();

					};

				Menu.Add (about_item);
				Menu.Add (new SeparatorMenuItem ());

					// A menu item that quits the application
					MenuItem quit_item = new MenuItem (_("Quit"));
					quit_item.Activated += Quit;

				Menu.Add (quit_item);

		}


		private void ShowMenu (object o, EventArgs args)
		{

			Menu.ShowAll ();
			Menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);

		}


		public void ShowState ()
		{

			ShowState (false);

		}


		// Shows the state and keeps the number of syncing repositories in mind
		public void ShowState (bool error)
		{

			UpdateFolderSize ();

			// Make sure the number of syncing repositories doesn't
			// go below zero. This can happen in some circumstances.
			if (SyncingReposCount < 0)
				SyncingReposCount = 0;

			// Make sure the number of syncing repositories doesn't
			// go above the number of existing repositories. This can
			// happen in some circumstances.
			if (SyncingReposCount > SparkleUI.Repositories.Count)
				SyncingReposCount = SparkleUI.Repositories.Count;

			if (error) {

				SetErrorState ();

			} else {

				if (SyncingReposCount > 0)
					SetSyncingState ();
				else
					SetIdleState ();

			}

			// Use the new status text
			(StatusMenuItem.Children [0] as Label).Text  = StateText;
			Menu.ShowAll ();

			SparkleHelpers.DebugInfo ("Status", "Number of repos syncing: " + SyncingReposCount);

		}
		

		// Changes the state to idle for when there's no syncing going on
		private void SetIdleState ()
		{

			Animation.Stop ();

			// The first frame is the idle icon
			Application.Invoke (delegate { SetPixbuf (AnimationFrames [0]); });
			StateText = _("Up to date") + "  (" + FormatFileSize (FolderSize) + ")";

		}


		// Changes the status icon to the syncing animation
		private void SetSyncingState ()
		{

			StateText = _("Syncing…");
			Animation.Start ();

		}


		// Changes the state to indicate there was an error syncing
		public void SetErrorState ()
		{

			Animation.Stop ();
			Application.Invoke (delegate { Pixbuf = SparkleUIHelpers.GetIcon ("sparkleshare-syncing-error", 24); });
			StateText = _("Failed to sync changes");

		}


		// Updates the icon used for the statusicon
		private void SetPixbuf (Gdk.Pixbuf pixbuf)
		{

			Pixbuf = pixbuf;
		
		}


		// Makes sure the menu pops up in the right position
		private void SetPosition (Menu menu, out int x, out int y, out bool push_in)
		{

			PositionMenu (menu, out x, out y, out push_in, Handle);

		}


		// Quits the program
		private void Quit (object o, EventArgs args)
		{

			// Remove the process id file
			File.Delete (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, "sparkleshare.pid"));
			Application.Quit ();

		}

	}

}
