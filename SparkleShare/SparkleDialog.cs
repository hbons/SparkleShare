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
	public class SparkleDialog : Window {

		private Button AddButton;
		private ComboBoxEntry RemoteUrlCombo;
		private Entry NameEntry;
		private SparkleWindow ParentSparkleWindow;

		public SparkleDialog (SparkleWindow Parent) : base ("")  {
		
			ParentSparkleWindow = Parent;
			BorderWidth = 6;
			IconName = "folder-sparkleshare";
			Modal = true;
			Resizable = false;
			SetPosition (WindowPosition.Center);
			Title = "Add Folder";
			TransientFor = ParentSparkleWindow;

			VBox VBox = new VBox (false, 0);

				Label NameLabel = new Label ("Folder Name:   ");
				NameEntry = new Entry ();
				Label NameExample = new Label ("<span size='small'><i>Example: " +
				                               "‘Project’.</i></span>");
				NameExample.UseMarkup = true;
				NameExample.SetAlignment (0, 0);
				NameLabel.Xalign = 1;
		
				Label RemoteUrlLabel = new Label ("Remote address:   ");

				string [] DefaultUrls = new string [4] { "ssh://git@github.com",
						                                   "ssh://git@git.gnome.org",
						                                   "ssh://git@fedorahosted.org",
						                                   "ssh://git@gitorious.org" };

				RemoteUrlCombo = new ComboBoxEntry (DefaultUrls);

				Label RemoteUrlExample = new Label ("<span size='small'><i>Example: " +
				                                    "‘ssh://git@github.com’.</i></span>");
				RemoteUrlExample.UseMarkup = true;
				RemoteUrlExample.SetAlignment (0, 0);
				RemoteUrlLabel.Xalign = 1;

				HButtonBox ButtonBox = new HButtonBox ();
				ButtonBox.Layout = ButtonBoxStyle.End;
				ButtonBox.Spacing = 6;
				ButtonBox.BorderWidth = 6;

					AddButton = new Button (Stock.Add);
					Button CancelButton = new Button (Stock.Cancel);

					CancelButton.Clicked += delegate {
						Destroy ();
					};

				RemoteUrlCombo.Entry.Changed += CheckFields;
				NameEntry.Changed += CheckFields;

					AddButton.Sensitive = false;
					AddButton.Clicked += CloneRepo;

				ButtonBox.Add (CancelButton);
				ButtonBox.Add (AddButton);

				Table Table = new Table(4, 2, false);
				Table.RowSpacing = 6;
				Table.BorderWidth = 6;
				Table.Attach (NameLabel, 0, 1, 0, 1);
		
				Table.Attach (NameEntry, 1, 2, 0, 1);
				Table.Attach (NameExample, 1, 2, 1, 2);
				Table.Attach (RemoteUrlLabel, 0, 1, 3, 4);
				Table.Attach (RemoteUrlCombo, 1, 2, 3, 4);
				Table.Attach (RemoteUrlExample, 1, 2, 4, 5);

			VBox.PackStart (Table, false, false, 0);
			VBox.PackStart (ButtonBox, false, false, 0);

			Add (VBox);

		}

		public void CloneRepo (object o, EventArgs args) {

			Remove (Child);
				VBox Box = new VBox (false, 24);
				SparkleSpinner Spinner = new SparkleSpinner ();
				Label Label = new Label ("Downloading files,\n" + 
				                         "this may take a while...");
				Box.PackStart (Spinner, false, false, 0);
				Box.PackStart (Label, false, false, 0);
			BorderWidth = 30;
			Add (Box);

			string RepoRemoteUrl = RemoteUrlCombo.Entry.Text;
			string RepoName = NameEntry.Text;

			Process Process = new Process();
			Process.EnableRaisingEvents = true; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.FileName = "git";
			Process.StartInfo.WorkingDirectory =
				SparklePaths.SparkleTmpPath;

			Process.StartInfo.Arguments =
				"clone " + SparkleHelpers.CombineMore (RepoRemoteUrl, RepoName);
			Console.WriteLine	(Process.StartInfo.Arguments);

//			Process.Start ();
			Process.Exited += delegate {
				Directory.Move (
					SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath,
					                            RepoName),
					SparkleHelpers.CombineMore (SparklePaths.SparklePath,
					                            RepoName)
				);
				Destroy ();
				ParentSparkleWindow.ToggleVisibility ();
				ParentSparkleWindow.Notebook.CurrentPage = 1;
			};
		
		}

		// Enables the Add button when the fields are
		// filled in correctly		
		public void CheckFields (object o, EventArgs args) {
			if (SparkleHelpers.IsGitUrl (RemoteUrlCombo.Entry.Text)
			    && NameEntry.Text.Length > 0)
				AddButton.Sensitive = true;
			else
				AddButton.Sensitive = false;
		}

	}

}
