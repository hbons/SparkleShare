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
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	// A dialog where the user can enter a folder
	// name and url to sync changes with
	public class SparklePreferencesDialog : Window {

		private Button AddButton;
		private ComboBoxEntry RemoteUrlCombo;
		private Entry NameEntry;

		public SparklePreferencesDialog (SparkleRepo SparkleRepo) : base ("")  {
		
			BorderWidth = 6;
			IconName = "folder-sparkleshare";
			Modal = true;
			Resizable = false;
			SetPosition (WindowPosition.Center);
			Title = "Preferences";

			// Create box layout for Remote Address
			HBox RemoteUrlBox = new HBox (false, 0);

				Label Property1 = new Label ("Remote address:");
				Property1.WidthRequest = 120;
				Property1.Xalign = 0;

				Label Value1 = new Label
					("<b>" + SparkleRepo.RemoteOriginUrl + "</b>");

				Value1.UseMarkup = true;

			RemoteUrlBox.PackStart (Property1, false, false, 0);
			RemoteUrlBox.PackStart (Value1, false, false, 0);

			// Create box layout for repository path
			HBox LocalPathBox = new HBox (false, 0);

				Label Property2 = new Label ("Local path:");
				Property2.WidthRequest = 120;
				Property2.Xalign = 0;

				Label Value2 = new Label
					("<b>" + SparkleRepo.LocalPath + "</b>");

				Value2.UseMarkup = true;

			LocalPathBox.PackStart (Property2, false, false, 0);
			LocalPathBox.PackStart (Value2, false, false, 0);

			CheckButton NotifyChangesCheckButton = 
				new CheckButton ("Notify me when something changes");

			string NotifyChangesFileName =
				SparkleHelpers.CombineMore (SparkleRepo.LocalPath,
				                            ".git", "sparkleshare.notify");
			                                        
			if (File.Exists (NotifyChangesFileName))
				NotifyChangesCheckButton.Active = true;
				
			NotifyChangesCheckButton.Toggled += delegate {
				if (File.Exists (NotifyChangesFileName)) {
					SparkleRepo.NotifyChanges = false;
					File.Delete (NotifyChangesFileName);
				} else {
					SparkleRepo.NotifyChanges = true;
					File.Create (NotifyChangesFileName);
				}
			};

			CheckButton SyncChangesCheckButton = 
				new CheckButton ("Synchronize my changes");

			string SyncChangesFileName =
				SparkleHelpers.CombineMore (SparkleRepo.LocalPath,
				                            ".git", "sparkleshare.sync");

			if (File.Exists (SyncChangesFileName))
				SyncChangesCheckButton.Active = true;

			SyncChangesCheckButton.Toggled += delegate {
				if (File.Exists (SyncChangesFileName)) {
					SparkleRepo.SyncChanges = false;
					File.Delete (SyncChangesFileName);
				} else {
					SparkleRepo.SyncChanges = true;
					File.Create (SyncChangesFileName);
				}
			};


			Table Table = new Table(2, 2, true);
			Table.RowSpacing = 3;
			Table.ColumnSpacing = 12;
			Table.BorderWidth = 9;
			Table.Attach (RemoteUrlBox, 0, 1, 0, 1);
			Table.Attach (LocalPathBox, 0, 1, 1, 2);
			Table.Attach (NotifyChangesCheckButton, 1, 2, 0, 1);
			Table.Attach (SyncChangesCheckButton, 1, 2, 1, 2);

			Add (Table);

			ShowAll ();

		}

	}

}
