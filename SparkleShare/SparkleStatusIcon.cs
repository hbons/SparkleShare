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

		private Timer Timer;
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
			Timer = CreateTimer ();

			SyncingReposCount = 0;

			StateText = "";
			StatusMenuItem = new MenuItem ();

			CreateMenu ();

			// Primary mouse button
			Activate += ShowMenu;

			// Secondary mouse button
			PopupMenu += ShowMenu;

			SetIdleState ();
			ShowState ();

		}


		private Gdk.Pixbuf [] CreateAnimationFrames ()
		{

			Gdk.Pixbuf [] animation_frames = new Gdk.Pixbuf [5];
			Gdk.Pixbuf frames_pixbuf = SparkleUIHelpers.GetIcon ("process-syncing-sparkleshare", 24);
			
			for (int i = 0; i < animation_frames.Length; i++)
				animation_frames [i] = new Gdk.Pixbuf (frames_pixbuf, (i * 24), 0, 24, 24);

			return animation_frames;

		}


		// Creates the timer that handles the syncing animation
		private Timer CreateTimer ()
		{

			Timer timer = new Timer () {
				Interval = 35
			};

			timer.Elapsed += delegate {

				if (FrameNumber < AnimationFrames.Length - 1)
					FrameNumber++;
				else
					FrameNumber = 0;

				Application.Invoke (delegate { SetPixbuf (AnimationFrames [FrameNumber]); });

			};

			return timer;

		}


		private EventHandler CreateWindowDelegate (string path)
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

			// Ignore the temporary 'rebase-apply' directory
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

					// Creates a menu item for each repository with a link to them
					foreach (SparkleRepo repo in SparkleUI.Repositories) {

						folder_action = new Gtk.Action ("", repo.Name) {
							IconName    = "folder",
							IsImportant = true
						};

						folder_action.Activated += CreateWindowDelegate (repo.LocalPath);

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

				MenuItem add_item = new MenuItem (_("Sync Remote Folder…"));

					add_item.Activated += delegate {

						SparkleIntro intro = new SparkleIntro ();
						intro.ShowServerForm (true);

					};

				Menu.Add (add_item);
				Menu.Add (new SeparatorMenuItem ());

				CheckMenuItem notify_item =	new CheckMenuItem (_("Show Notifications"));

					string notify_setting = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
						"sparkleshare.notify");
							                                 
					if (File.Exists (notify_setting))
						notify_item.Active = true;
				
					notify_item.Toggled += delegate {

						if (File.Exists (notify_setting))
							File.Delete (notify_setting);
						else
							File.Create (notify_setting);
				
					};

				Menu.Add (notify_item);

				Menu.Add (new SeparatorMenuItem ());

				MenuItem about_item = new MenuItem (_("Visit Website"));

					about_item.Activated += delegate {

						Process process = new Process ();

						process.StartInfo.FileName  = "xdg-open";
						process.StartInfo.Arguments = "http://www.sparkleshare.org/";

						process.Start ();

					};

				Menu.Add (about_item);
				Menu.Add (new SeparatorMenuItem ());

					MenuItem quit_item = new MenuItem (_("Quit"));
					quit_item.Activated += Quit;

				Menu.Add (quit_item);

		}


		private void ShowMenu (object o, EventArgs args)
		{

			Menu.ShowAll ();
			Menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);

		}


		private void UpdateStatusMenuItem ()
		{

			(StatusMenuItem.Children [0] as Label).Text  = StateText;
			Menu.ShowAll ();

		}


		public void ShowState ()
		{

			ShowState (false);

		}


		// Shows the state and keeps the number of syncing repositories in mind
		public void ShowState (bool error)
		{

			UpdateFolderSize ();

			if (SyncingReposCount < 0)
				SyncingReposCount = 0;

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

			UpdateStatusMenuItem ();
			
			SparkleHelpers.DebugInfo ("Status", "Number of repos syncing: " + SyncingReposCount);

		}
		

		// Changes the state to idle for when there's no syncing going on
		private void SetIdleState ()
		{

			Timer.Stop ();

			Application.Invoke (delegate { SetPixbuf (AnimationFrames [0]); });
			StateText = _("Up to date") + "  (" + FormatFileSize (FolderSize) + ")";

		}


		// Changes the status icon to the syncing animation
		private void SetSyncingState ()
		{

			StateText = _("Syncing…");
			Timer.Start ();

		}


		// Changes the state to indicate there was an error syncing
		public void SetErrorState ()
		{

			Timer.Stop ();
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

			File.Delete (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, "sparkleshare.pid"));
			Application.Quit ();

		}

	}

}
