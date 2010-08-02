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
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace SparkleShare {

	public class SparkleStatusIcon : StatusIcon
	{

		public int SyncingReposCount;

		private Timer Timer;
		private Menu Menu;
		private MenuItem StatusMenuItem;
		private string StateText;
		private Gdk.Pixbuf [] AnimationFrames;
		private int FrameNumber;


		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}


		public SparkleStatusIcon () : base ()
		{

			CreateAnimationFrames ();
			CreateTimer ();

			SyncingReposCount = 0;

			StateText = "";
			StatusMenuItem = new MenuItem ();

			CreateMenu ();
			Activate += ShowMenu;

			SetIdleState ();
			ShowState ();

		}


		private void CreateAnimationFrames ()
		{

			FrameNumber = 0;

			AnimationFrames = new Gdk.Pixbuf [5];
			Gdk.Pixbuf frames_pixbuf = SparkleHelpers.GetIcon ("process-syncing-sparkleshare", 24);
			
			for (int i = 0; i < AnimationFrames.Length; i++)
				AnimationFrames [i] = new Gdk.Pixbuf (frames_pixbuf, (i * 24), 0, 24, 24);

		}


		// Creates the timer that handles the syncing animation
		private void CreateTimer ()
		{

			Timer = new Timer () {
				Interval = 35
			};

			Timer.Elapsed += delegate {

				if (FrameNumber < AnimationFrames.Length - 1)
					FrameNumber++;
				else
					FrameNumber = 0;

				Application.Invoke (delegate { SetPixbuf (AnimationFrames [FrameNumber]); });

			};

		}


		private EventHandler CreateWindowDelegate (SparkleRepo repo)
		{

			return delegate { 

				SparkleWindow SparkleWindow = new SparkleWindow (repo);
				SparkleWindow.ShowAll ();

			};

		}


		// Creates the menu that is popped up when the
		// user clicks the statusicon
		private void CreateMenu ()
		{

				Menu = new Menu ();

					StatusMenuItem = new MenuItem (StateText) {
						Sensitive = false
					};

				Menu.Add (StatusMenuItem);

				Menu.Add (new SeparatorMenuItem ());

					// TODO: Append folder size in secondary text color
					Gtk.Action folder_action = new Gtk.Action ("", _("SparkleShare Folder")) {
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

					foreach (SparkleRepo SparkleRepo in SparkleUI.Repositories) {

						folder_action = new Gtk.Action ("", SparkleRepo.Name) {
							IconName    = "folder",
							IsImportant = true
						};

						folder_action.Activated += CreateWindowDelegate (SparkleRepo);

						Menu.Add (folder_action.CreateMenuItem ());

					}

				} else {

					MenuItem no_folders_item = new MenuItem (_("No Shared Folders Yet")) {
						Sensitive   = false
					};

					Menu.Add (no_folders_item);

				}

				MenuItem add_item = new MenuItem (_("Add Remote Folder…"));

					add_item.Activated += delegate {

						SparkleIntro intro = new SparkleIntro ();
						intro.ShowStepTwo (true);
						intro.ShowAll ();

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

				MenuItem about_item = new MenuItem (_("About"));

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

			Label label = (Label) StatusMenuItem.Children [0];
			label.Text  = StateText;

			Menu.ShowAll ();

		}


		public void ShowState ()
		{

			if (SyncingReposCount > 0)
				SetSyncingState ();
			else
				SetIdleState ();

			UpdateStatusMenuItem ();

		}
		

		// Changes the state to idle for when there's no syncing going on
		private void SetIdleState ()
		{

			Timer.Stop ();

			Pixbuf  = SparkleHelpers.GetIcon ("folder-sparkleshare", 24);
			StateText = _("All up to date");

		}


		// Changes the status icon to the syncing animation
		private void SetSyncingState ()
		{

			StateText = _("Syncing…");
			Timer.Start ();

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
