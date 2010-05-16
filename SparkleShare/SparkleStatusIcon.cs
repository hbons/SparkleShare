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
using SparkleShare;
using System;

namespace SparkleShare {
	
	public class SparkleStatusIcon : StatusIcon {

		public SparkleStatusIcon () : base ()  {
Activate += delegate {


			Menu popupMenu = new Menu();

			foreach (SparkleRepo SparkleRepo in SparkleShare.Repositories) {
			ImageMenuItem Item = new ImageMenuItem (SparkleRepo.Name);
				Item.Image = new Image (SparkleHelpers.GetIcon ("folder", 16));
	
			Item.Activated += delegate { SparkleWindow SparkleWindow = new SparkleWindow (SparkleRepo);
			SparkleWindow.ShowAll ();Console.WriteLine (SparkleRepo.Name); };
				popupMenu.Add(Item);
				

		}
			ImageMenuItem menuItemQuit = new ImageMenuItem ("Quit SparkleShare");
			popupMenu.Add(menuItemQuit);
			
			
			
			
			
			// Quit the application when quit has been clicked.
			menuItemQuit.Activated += delegate { Environment.Exit(0); };
			popupMenu.ShowAll();
			popupMenu.Popup();

};
			SetIdleState ();
		}

		public void SetIdleState () {
			IconName = "folder-synced";
			Tooltip = "SparkleShare, all up to date";
		}

		public void SetSyncingState () {
//			IconName = "folder-syncing";
//			Tooltip = "SparkleShare, updating files...";
		}

		public void SetErrorState () {
//			IconName = "folder-sync-error";
//			Tooltip = "SparkleShare, something went wrong";
		}
		

		// Quits the program
		public void Quit (object o, EventArgs args) {
			System.IO.File.Delete (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath +
			                                         "sparkleshare.pid"));
			Application.Quit ();
		}

	}

}
