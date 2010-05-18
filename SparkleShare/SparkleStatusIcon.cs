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

namespace SparkleShare {

	public class SparkleStatusIcon : StatusIcon {

		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}

		public SparkleStatusIcon () : base ()  {

			Activate += delegate {

				Menu Menu = new Menu();

				MenuItem StatusItem = new MenuItem (_("Everything is up to date"));
				StatusItem.Sensitive = false;
				Menu.Add (StatusItem);
				Menu.Add (new SeparatorMenuItem ());
		
				MenuItem [] FolderItems =
					new MenuItem [SparkleShare.Repositories.Length];

				// TODO: For some strange reason both entries
				// open the same repo...
				int i = 0;
				foreach (SparkleRepo SparkleRepo in SparkleShare.Repositories) {
					FolderItems [i] = new MenuItem (SparkleRepo.Name);
					FolderItems [i].Activated += delegate {
						SparkleWindow SparkleWindow = new SparkleWindow (SparkleRepo);
						SparkleWindow.ShowAll ();
					};
					Menu.Add (FolderItems [i]);
					i++;
				}
				
				MenuItem AddItem = new MenuItem (_("Add a Folder…"));
				AddItem.Activated += delegate {
					SparkleDialog SparkleDialog = new SparkleDialog ();
					SparkleDialog.ShowAll ();
				};
				Menu.Add (AddItem);
				Menu.Add (new SeparatorMenuItem ());

				MenuItem OpenFolderItem = new MenuItem (_("Open Sharing Folder"));
				OpenFolderItem.Activated += delegate {
							Process Process = new Process ();
							Process.StartInfo.FileName = "xdg-open";
							Process.StartInfo.Arguments = SparklePaths.SparklePath;
							Process.Start();
				};
				Menu.Add (OpenFolderItem);

				MenuItem AboutItem = new MenuItem (_("Visit SparkleShare Website"));
				AboutItem.Activated += delegate {
							Process Process = new Process ();
							Process.StartInfo.FileName = "xdg-open";
							Process.StartInfo.Arguments = "http://www.sparkleshare.org/";
							Process.Start();
				};
				Menu.Add (AboutItem);

				Menu.Add (new SeparatorMenuItem ());
				MenuItem QuitItem = new MenuItem ("Quit");
				QuitItem.Activated += delegate { Environment.Exit (0); };
				Menu.Add (QuitItem);
			
				Menu.ShowAll ();
				Menu.Popup ();

			};

			SetIdleState ();

		}

		public void SetIdleState () {
			IconName = "folder-sparkleshare";
		}

		public void SetSyncingState () {
//			IconName = "folder-syncing";
		}

		public void SetErrorState () {
//			IconName = "folder-sync-error";
		}

		// Quits the program
		public void Quit (object o, EventArgs args) {
			System.IO.File.Delete (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath +
			                                         "sparkleshare.pid"));
			Application.Quit ();
		}

	}

}
