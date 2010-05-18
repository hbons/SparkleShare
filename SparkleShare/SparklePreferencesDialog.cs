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

		public SparklePreferencesDialog (SparkleWindow SparkleWindow,
		                                 SparkleRepo SparkleRepo) : base ("")  {

			BorderWidth = 12;
			IconName = "folder-sparkleshare";
			Resizable = false;
			SetPosition (WindowPosition.Center);
			Title = "Preferences";
			TransientFor = SparkleWindow;

			VBox LayoutVertical = new VBox (false, 0);

			Label InfoLabel = new Label ();
			InfoLabel.Text = "The folder" +
			                 "<b>" + SparkleRepo.LocalPath + "</b>" +
			                 "\nis linked to " +
			                 "<b>" + SparkleRepo.RemoteOriginUrl + "</b>";

			InfoLabel.Xalign = 0;
			InfoLabel.UseMarkup = true;

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

			HButtonBox DialogButtons = new HButtonBox ();
			DialogButtons.Layout = ButtonBoxStyle.End;
			DialogButtons.BorderWidth = 0;

			Button CloseButton = new Button (Stock.Close);
			CloseButton.Clicked += delegate (object o, EventArgs args) {
				Destroy ();
			};
			DialogButtons.Add (CloseButton);
			SparkleWindow.Default = CloseButton;

			LayoutVertical.PackStart (InfoLabel, false, false, 0);
			LayoutVertical.PackStart (new Label (), false, false, 0);
			LayoutVertical.PackStart (NotifyChangesCheckButton, false, false, 0);
			LayoutVertical.PackStart (SyncChangesCheckButton, false, false, 3);
			LayoutVertical.PackStart (new Label (), false, false, 0);
			LayoutVertical.PackStart (DialogButtons, false, false, 0);

			Add (LayoutVertical);

		}

	}

}
