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
using System.Text.RegularExpressions;

namespace SparkleShare {

	// A dialog where the user can enter a folder
	// name and url to sync changes with
	public class SparkleDialog : Window {

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		private Button AddButton;
		private ComboBoxEntry RemoteUrlCombo;

		public SparkleDialog (string Url) : base ("")
		{
			BorderWidth = 12;
			IconName = "folder-sparkleshare";
			WidthRequest = 320;
			Title = "SparkleShare";

			SetPosition (WindowPosition.Center);

			VBox VBox = new VBox (false, 0);

				Label RemoteUrlLabel = new Label (_("Address of remote SparkleShare folder:"));
				RemoteUrlLabel.Xalign = 0;

				ListStore Defaults = new ListStore (typeof (string));

				RemoteUrlCombo = new ComboBoxEntry (Defaults, 0);

				if (Url.Equals (""))
					RemoteUrlCombo.Entry.Text = "ssh://";
				else
					RemoteUrlCombo.Entry.Text = Url;

				RemoteUrlCombo.Entry.Completion = new EntryCompletion ();
				RemoteUrlCombo.Entry.Completion.Model = Defaults;

				RemoteUrlCombo.Entry.Completion.InlineCompletion = true;
				RemoteUrlCombo.Entry.Completion.PopupCompletion = true;
				RemoteUrlCombo.Entry.Completion.TextColumn = 0;
				RemoteUrlCombo.Entry.Changed += CheckFields;

				// Add some preset addresses
				Defaults.AppendValues ("ssh://git@github.com/");
				Defaults.AppendValues ("ssh://git@git.gnome.org/");
				Defaults.AppendValues ("ssh://git@fedorahosted.org/");
				Defaults.AppendValues ("ssh://git@gitorious.org/");

				HButtonBox ButtonBox = new HButtonBox ();
				ButtonBox.Layout = ButtonBoxStyle.End;
				ButtonBox.Spacing = 6;
				ButtonBox.BorderWidth = 0;

					AddButton = new Button (_("Add Folder"));
					AddButton.Clicked += CloneRepo;
					AddButton.Sensitive = false;

					Button CancelButton = new Button (Stock.Cancel);

					CancelButton.Clicked += delegate {
						Destroy ();
					};

				ButtonBox.Add (CancelButton);
				ButtonBox.Add (AddButton);
		
			VBox.PackStart (RemoteUrlLabel, false, false, 0);
			VBox.PackStart (RemoteUrlCombo, false, false, 12);
			VBox.PackStart (ButtonBox, false, false, 0);

			Add (VBox);

			ShowAll ();

		}

		// Clones a remote repo
		public void CloneRepo (object o, EventArgs args) {

//		  SparkleUI.NotificationIcon.SetSyncingState ();

			HideAll ();

			string RepoRemoteUrl = RemoteUrlCombo.Entry.Text;
			RepoRemoteUrl = SparkleToGitUrl (RepoRemoteUrl);

			int SlashPos = RepoRemoteUrl.LastIndexOf ("/");
			int ColumnPos = RepoRemoteUrl.LastIndexOf (":");


			Process Process = new Process ();
			Process.EnableRaisingEvents = true; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			SparkleHelpers.DebugInfo ("Config", "[" + RepoName + "] Cloning repository...");

			// TODO: don't freeze the UI while cloning a repo
			// Clone into the system's temporary folder
			Process.StartInfo.FileName = "git";
			Process.StartInfo.WorkingDirectory = SparklePaths.SparkleTmpPath;
			Process.StartInfo.Arguments = String.Format ("clone {0} {1}",
				RepoRemoteUrl,
				RepoName);

			Process.Start ();
			Process.WaitForExit ();

			if (Process.ExitCode != 0) {

				// error

				try {
					Directory.Delete (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, RepoName));
				} catch (System.IO.DirectoryNotFoundException) {
					SparkleHelpers.DebugInfo ("Config", "[" + RepoName + "] Temporary directory did not exist...");
				}
			
			} else {

				SparkleHelpers.DebugInfo ("Git", "[" + RepoName + "] Repository cloned");

				Directory.Move (SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, RepoName),
					SparkleHelpers.CombineMore (SparklePaths.SparklePath, RepoName));


				// Add a .gitignore file to the repo
				TextWriter Writer = new StreamWriter (SparkleHelpers.CombineMore (SparklePaths.SparklePath, RepoName,
					".gitignore"));
				Writer.WriteLine ("*~"); // Ignore gedit swap files
				Writer.WriteLine (".*.sw?"); // Ignore vi swap files
				Writer.WriteLine (".DS_store"); // Ignore OSX's invisible directories
				Writer.Close ();

				// TODO: Install username and email from global file

				File.Create (SparkleHelpers.CombineMore (SparklePaths.SparklePath, RepoName,
					".emblems"));

				SparkleShare.SparkleUI.UpdateRepositories ();

				// Show a confirmation notification
				SparkleBubble FinishedBubble;
				FinishedBubble = new SparkleBubble (String.Format(_("Successfully synced folder ‘{0}’"), RepoName),
					   _("Now make great stuff happen!"));

				FinishedBubble.AddAction ("", _("Open Folder"),
					delegate {
						Process.StartInfo.FileName = "xdg-open";
						Process.StartInfo.Arguments = SparkleHelpers.CombineMore (SparklePaths.SparklePath, RepoName);
						Process.Start ();
					} );
				FinishedBubble.Show ();

			}

		}



		


	}

}
