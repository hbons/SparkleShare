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

			Activate += delegate {

				Menu Menu = new Menu();

				MenuItem StatusItem = new MenuItem (_("Everything is up to date"));
				StatusItem.Sensitive = false;
				Menu.Add (StatusItem);
				Menu.Add (new SeparatorMenuItem ());

				MenuItem OpenFolderItem = new MenuItem (_("SparkleShare Folder"));
				OpenFolderItem.Activated += delegate {
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
					Process.Start();
				};
				Menu.Add (OpenFolderItem);

				Action [] FolderItems =
					new Action [SparkleShare.Repositories.Length];
				
				int i = 0;
				foreach (SparkleRepo SparkleRepo in SparkleShare.Repositories) {
					FolderItems [i] = new Action("", SparkleRepo.Name);
					FolderItems [i].IconName = "folder";
					FolderItems [i].IsImportant = true;
					FolderItems [i].Activated += CreateWindowDelegate (SparkleRepo);
					Menu.Add (FolderItems [i].CreateMenuItem ());
					i++;
				}
				
				MenuItem AddItem = new MenuItem (_("Add a Folder…"));
				AddItem.Activated += delegate {
					SparkleDialog SparkleDialog = new SparkleDialog ();
					SparkleDialog.ShowAll ();
				};
				Menu.Add (AddItem);
				Menu.Add (new SeparatorMenuItem ());

				CheckMenuItem NotifyCheckMenuItem =
					new CheckMenuItem (_("Show notifications"));
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

				MenuItem AboutItem = new MenuItem (_("About SparkleShare"));
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
				MenuItem QuitItem = new MenuItem ("Quit");
				QuitItem.Activated += delegate { Environment.Exit (0); };
				Menu.Add (QuitItem);
				Menu.ShowAll ();
				Menu.Popup ();
				// TODO: Make sure the menu never overlaps the status icon
			};

			SetIdleState ();

		}

		public void SetIdleState () {
			Timer.Stop ();
			Pixbuf = SparkleHelpers.GetIcon ("folder-sparkleshare", 24);
		}

		public void SetSyncingState () {

			int CycleDuration = 250;
			int CurrentStep = 0;
			int Size = 24;			

			Gdk.Pixbuf SpinnerGallery = SparkleHelpers.GetIcon ("process-syncing-sparkleshare",
			                                                    Size);

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

		public void SetErrorState () {
//			IconName = "folder-sync-error";
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
