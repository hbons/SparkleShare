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
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace SparkleShare {

	public class SparkleStatusIcon : StatusIcon {

		private Timer Timer;
		private int SyncingState;

		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}

		public EventHandler CreateWindowDelegate (SparkleRepo SparkleRepo) {
			return delegate { 
				SparkleWindow SparkleWindow = new SparkleWindow (SparkleRepo);
				SparkleWindow.ShowAll ();
			};
		}

		public SparkleStatusIcon () : base ()  {

			Timer = new Timer ();
			Activate += ShowMenu;

			//  0 = Everything up to date
			//  1 = Syncing in progress
			// -1 = Error syncing
			SyncingState = 0;

			SetIdleState ();

		}

		public void ShowMenu (object o, EventArgs Args) {

				Menu Menu = new Menu ();

				string StateText = "";
				switch (SyncingState) {
					case -1:
						StateText = _("Error syncing");
						break;
					case 0:
						StateText = _("Everything is up to date");
						break;
					case 1:
						StateText = _("Syncing…");
						break;
				}

				MenuItem StatusMenuItem = new MenuItem (StateText);
				StatusMenuItem.Sensitive = false;

				Menu.Add (StatusMenuItem);
				Menu.Add (new SeparatorMenuItem ());

				Action FolderAction = new Action ("", "SparkleShare");
				FolderAction.IconName = "folder-sparkleshare";
				FolderAction.IsImportant = true;
				FolderAction.Activated += delegate {
					Process Process = new Process ();
					switch (SparklePlatform.Name) {
						case "GNOME":
							Process.StartInfo.FileName = "xdg-open";
							break;
						case "OSX":
							Process.StartInfo.FileName = "open";
							break;
					}
					Process.StartInfo.Arguments = SparklePaths.SparklePath;
					Process.Start ();
				};
				Menu.Add (FolderAction.CreateMenuItem ());

				Action [] FolderItems =
					new Action [SparkleShare.Repositories.Length];
				
				int i = 0;
				foreach (SparkleRepo SparkleRepo in SparkleShare.Repositories) {
					FolderItems [i] = new Action ("", SparkleRepo.Name);
					FolderItems [i].IconName = "folder";
					FolderItems [i].IsImportant = true;
					FolderItems [i].Activated += CreateWindowDelegate (SparkleRepo);
					Menu.Add (FolderItems [i].CreateMenuItem ());
					i++;
				}
				
				MenuItem AddItem = new MenuItem (_("Add a Folder…"));
				AddItem.Activated += delegate {
					SparkleDialog SparkleDialog = new SparkleDialog ("");
					SparkleDialog.ShowAll ();
				};
				Menu.Add (AddItem);
				Menu.Add (new SeparatorMenuItem ());

				CheckMenuItem NotifyCheckMenuItem =
					new CheckMenuItem (_("Show Notifications"));
				Menu.Add (NotifyCheckMenuItem);
				Menu.Add (new SeparatorMenuItem ());

				string NotifyChangesFileName =
					SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
						                         "sparkleshare.notify");
					                                     
				if (System.IO.File.Exists (NotifyChangesFileName))
					NotifyCheckMenuItem.Active = true;
				
				NotifyCheckMenuItem.Toggled += delegate {
					if (System.IO.File.Exists (NotifyChangesFileName)) {
						File.Delete (NotifyChangesFileName);
					} else {
						System.IO.File.Create (NotifyChangesFileName);
					}
				};

				MenuItem AboutItem = new MenuItem (_("Visit Website"));
				AboutItem.Activated += delegate {
					Process Process = new Process ();
					switch (SparklePlatform.Name) {
						case "GNOME":
							Process.StartInfo.FileName = "xdg-open";
							break;
						case "OSX":
							Process.StartInfo.FileName = "open";
							break;						
					}
					Process.StartInfo.Arguments = "http://www.sparkleshare.org/";
					Process.Start ();
				};
				Menu.Add (AboutItem);

				Menu.Add (new SeparatorMenuItem ());
				MenuItem QuitItem = new MenuItem (_("Quit"));
				QuitItem.Activated += Quit;
				Menu.Add (QuitItem);
				Menu.ShowAll ();
				Menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);
		}

		public void SetIdleState () {
			Timer.Stop ();
			Pixbuf = SparkleHelpers.GetIcon ("folder-sparkleshare", 24);
			SyncingState = 0;
		}

		// Changes the status icon to the syncing animation
		// TODO: There are UI freezes when switching back and forth
		// bewteen syncing and idle state
		public void SetSyncingState () {

			SyncingState = 1;

			int CycleDuration = 250;
			int CurrentStep = 0;
			int Size = 24;			

			Gdk.Pixbuf SpinnerGallery =
				SparkleHelpers.GetIcon ("process-syncing-sparkleshare", Size);

			int FramesInWidth = SpinnerGallery.Width / Size;
			int FramesInHeight = SpinnerGallery.Height / Size;
			int NumSteps = FramesInWidth * FramesInHeight;
			Gdk.Pixbuf [] Images = new Gdk.Pixbuf [NumSteps - 1];

			int i = 0;
			for (int y = 0; y < FramesInHeight; y++) {
				for (int x = 0; x < FramesInWidth; x++) {
					if (!(y == 0 && x == 0)) {
						Images [i] = new Gdk.Pixbuf (SpinnerGallery,
						                             x * Size, y * Size, Size, Size);
						i++;
					}
				}
			}

			Timer = new Timer ();
			Timer.Interval = CycleDuration / NumSteps;
			Timer.Elapsed += delegate {
				if (CurrentStep < NumSteps)
					CurrentStep++;
				else
					CurrentStep = 0;
				Pixbuf = Images [CurrentStep];
			};
			Timer.Start ();

		}

		// Changes the status icon to the error icon
		public void SetErrorState () {
			IconName = "folder-sync-error";
			SyncingState = -1;
		}

		public void SetPosition (Menu menu, out int x, out int y,
		                         out bool push_in) {

			PositionMenu (menu, out x, out y, out push_in, Handle);

		}

		// Quits the program
		public void Quit (object o, EventArgs args) {
			System.IO.File.Delete
				(SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath +
                                 "sparkleshare.pid"));
			Application.Quit ();
		}

	}

}
