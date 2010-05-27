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

namespace SparkleShare {

	// A dialog where the user can enter a folder
	// name and url to sync changes with
	public class SparkleDialog : Window {

		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}

		private Button AddButton;
		private ComboBoxEntry RemoteUrlCombo;
		private Table Table;

		public SparkleDialog (string Url) : base ("")  {
		
			BorderWidth = 6;
			IconName = "folder-sparkleshare";
			Modal = true;
			Resizable = false;
			SetPosition (WindowPosition.Center);
			Title = _("Add a Folder");

			VBox VBox = new VBox (false, 0);

				Label RemoteUrlLabel = new Label (_("Address:   "));
				
				RemoteUrlCombo = new ComboBoxEntry ();
				RemoteUrlCombo.Entry.Text = Url;

				ListStore Defaults = new ListStore (typeof (string),
				                                    typeof (Gdk.Pixbuf));

				RemoteUrlCombo.Entry.Completion = new EntryCompletion ();

				CellRendererPixbuf CellRendererPixbuf = new CellRendererPixbuf ();
				RemoteUrlCombo.Entry.Completion.Model = Defaults;
				RemoteUrlCombo.Entry.Completion.PackStart (CellRendererPixbuf, false);
				RemoteUrlCombo.Entry.Completion.AddAttribute (CellRendererPixbuf, "pixbuf", 1);
				RemoteUrlCombo.Entry.Completion.InlineCompletion = true;
				RemoteUrlCombo.Entry.Completion.PopupCompletion = true;;
				RemoteUrlCombo.Entry.Completion.TextColumn = 0;

				Defaults.AppendValues ("ssh://git@github.com/",
				                       SparkleHelpers.GetIcon ("github", 16));
				Defaults.AppendValues ("ssh://git@git.gnome.org/",
				                       SparkleHelpers.GetIcon ("gnome", 16));
				Defaults.AppendValues ("ssh://git@fedorahosted.org/",
				                       SparkleHelpers.GetIcon ("fedorahosted", 16));
				Defaults.AppendValues ("ssh://git@gitorious.org/",
				                       SparkleHelpers.GetIcon ("gitorious", 16));

				Label RemoteUrlExample = new Label (_("These usually look something like this:\n ") +
				                                    _("‘git://git@gnome.org/project’."));

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
				RemoteUrlCombo.WidthRequest = 320;

					AddButton.Sensitive = false;
					AddButton.Clicked += CloneRepo;

				ButtonBox.Add (CancelButton);
				ButtonBox.Add (AddButton);

				Table = new Table(3, 2, false);
				Table.RowSpacing = 6;
				Table.BorderWidth = 6;
		
				Table.Attach (RemoteUrlLabel, 0, 1, 0, 1);
				Table.Attach (RemoteUrlCombo, 1, 2, 0, 1);
				Table.Attach (RemoteUrlExample, 1, 2, 1, 2);

			VBox.PackStart (Table, false, false, 0);
			VBox.PackStart (ButtonBox, false, false, 0);

			Add (VBox);
			ShowAll ();

		}

		public void CloneRepo (object o, EventArgs args) {

			string RepoRemoteUrl = RemoteUrlCombo.Entry.Text;

			int SlashPos = RepoRemoteUrl.LastIndexOf ("/");
			int ColumnPos = RepoRemoteUrl.LastIndexOf (":");

			// Check wheter a "/" or ":" is used to separate the 
			// repo name from the domain.
			string RepoName;
			if (SlashPos > ColumnPos)
				RepoName = RepoRemoteUrl.Substring (SlashPos + 1);
			else
				RepoName = RepoRemoteUrl.Substring (ColumnPos + 1);

			Process Process = new Process();
			Process.EnableRaisingEvents = true; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.FileName = "git";
			Process.StartInfo.WorkingDirectory = SparklePaths.SparkleTmpPath;

			Process.StartInfo.Arguments =	"clone ";
			Process.StartInfo.Arguments +=
				SparkleHelpers.CombineMore (RepoRemoteUrl, RepoName).Substring (2);

			SparkleBubble SparkleBubble =
				new SparkleBubble (_("Syncing folder ‘") + RepoName + "’",
			                      _("SparkleShare will notify you when this is done."));

			Hide ();

			Process.WaitForExit ();
			Process.Start ();
			string Output = Process.StandardOutput.ReadToEnd ();

			if (Output.Contains ("fatal")) {
				Console.WriteLine ("SOMETHING WENT WRONG!!!");

				SparkleBubble =
					new SparkleBubble ("Something went wrong while syncing " +
					                   " ‘" + RepoName + "’",
				                      "Please double check your address and\n" +
				                      "network connection.");

				SparkleBubble.AddAction ("", _("Try Again…"), 
				                         delegate {
									          	Process.StartInfo.FileName = "xdg-open";
				  	                      	Process.StartInfo.Arguments =
				  	                      		SparkleHelpers.CombineMore (
				  	                      			SparklePaths.SparklePath, RepoName);
					 	                   	Process.Start();
									          } );

				SparkleDialog SparkleDialog = new SparkleDialog (RepoRemoteUrl);
				SparkleDialog.ShowAll ();
				
				Destroy ();

			}

			// Move the folder to the SparkleShare folder when done cloning
			Process.Exited += delegate {
				Directory.Move (
					SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath,
					                            RepoName),
					SparkleHelpers.CombineMore (SparklePaths.SparklePath,
					                            RepoName)
				);

				SparkleBubble =
					new SparkleBubble ("Successfully synced the folder" +
					                   " ‘" + RepoName + "’",
				                      "Now make great stuff happen!");

				SparkleBubble.AddAction ("", "Open Folder", 
				                         delegate {
									          	Process.StartInfo.FileName = "xdg-open";
				  	                      	Process.StartInfo.Arguments =
				  	                      		SparkleHelpers.CombineMore (
				  	                      			SparklePaths.SparklePath, RepoName);
					 	                   	Process.Start();
									          } );									          
				Destroy ();

			};
		
		}

		// Enables the Add button when the fields are
		// filled in correctly		
		public void CheckFields (object o, EventArgs args) {
			if (SparkleHelpers.IsGitUrl (RemoteUrlCombo.Entry.Text))
				AddButton.Sensitive = true;
			else
				AddButton.Sensitive = false;
		}

	}

}
