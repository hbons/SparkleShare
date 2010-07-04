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
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SparkleShare {

	// The main window of SparkleDiff
	public class SparkleDiffWindow : Window
	{

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		private RevisionView ViewLeft;
		private RevisionView ViewRight;

		private string [] Revisions;

		public SparkleDiffWindow (string file_path) : base ("")
		{

			Revisions = GetRevisionsForFilePath (file_path);
			
			if (Revisions.Length < 2) {
				Console.WriteLine ("SparkleDiff: " + file_path + ": File has no history.");
			}

			string file_name = System.IO.Path.GetFileName (file_path);

			// TODO: Adjust the size of the window to the images
			SetSizeRequest (800, 540);
	 		SetPosition (WindowPosition.Center);

			BorderWidth = 12;

			DeleteEvent += Quit;

			IconName = "image-x-generic";

			// TRANSLATORS: The parameter is a filename
			Title = String.Format(_("Comparing Revisions of ‘{0}’"), file_name);

			VBox layout_vertical = new VBox (false, 12);

				HBox layout_horizontal = new HBox (false, 12);

					Process process = new Process ();
					process.EnableRaisingEvents = true; 
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.UseShellExecute = false;

					process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (file_path);
					process.StartInfo.FileName = "git";
					process.StartInfo.Arguments = "log --format=\"%ct\t%an\" " + file_name;
					process.Start ();

					string output = process.StandardOutput.ReadToEnd ();
					string [] revisions_info = Regex.Split (output.Trim (), "\n");

					int i = 0;
					foreach (string revision_info in revisions_info) {

						string [] parts = Regex.Split (revision_info.Trim (), "\t");

						int timestamp = int.Parse (parts [0]);
						string author = parts [1];

						if (i == 0)
							revisions_info [i] = _("Current Revision") + "\t" + author;
						else

							// TRANSLATORS: This is a format specifier according to System.Globalization.DateTimeFormatInfo
							revisions_info [i] = UnixTimestampToDateTime (timestamp).ToString (_("d MMM\tH:mm")) +
							"\t" + author;

						i++;

					}

					ViewLeft  = new LeftRevisionView  (revisions_info);
					ViewRight = new RightRevisionView (revisions_info);

					ViewLeft.SetImage  (new RevisionImage (file_path, Revisions [1]));
					ViewRight.SetImage (new RevisionImage (file_path, Revisions [0]));
					
					ViewLeft.ComboBox.Changed += delegate {

						RevisionImage revision_image;
						revision_image = new RevisionImage (file_path, Revisions [ViewLeft.ComboBox.Active]);
						ViewLeft.SetImage (revision_image);

						HookUpViews ();
						
						ViewLeft.ScrolledWindow.Hadjustment = ViewRight.ScrolledWindow.Hadjustment;
						ViewLeft.ScrolledWindow.Vadjustment = ViewRight.ScrolledWindow.Vadjustment;
						
						ViewLeft.UpdateControls ();

					};

					ViewRight.ComboBox.Changed += delegate {

						RevisionImage revision_image;
						revision_image = new RevisionImage (file_path, Revisions [ViewRight.ComboBox.Active]);
						ViewRight.SetImage (revision_image);

						HookUpViews ();

						ViewRight.ScrolledWindow.Hadjustment = ViewLeft.ScrolledWindow.Hadjustment;
						ViewRight.ScrolledWindow.Vadjustment = ViewLeft.ScrolledWindow.Vadjustment;

						ViewRight.UpdateControls ();

					};


				layout_horizontal.PackStart (ViewLeft);
				layout_horizontal.PackStart (ViewRight);

				// Order time view according to the user's reading direction
				if (Direction == Gtk.TextDirection.Rtl) // See Deejay1? I can do i18n too! :P				
					layout_horizontal.ReorderChild (ViewLeft, 1);


				HookUpViews ();

				HButtonBox dialog_buttons  = new HButtonBox ();
				dialog_buttons.Layout      = ButtonBoxStyle.End;
				dialog_buttons.BorderWidth = 0;

					Button CloseButton = new Button (Stock.Close);
					CloseButton.Clicked += delegate (object o, EventArgs args) {
						Environment.Exit (0);
					};

				dialog_buttons.Add (CloseButton);

			layout_vertical.PackStart (layout_horizontal, true, true, 0);
			layout_vertical.PackStart (dialog_buttons, false, false, 0);

			Add (layout_vertical);

		}


		// Hooks up two views so their scrollbars will be kept in sync
		private void HookUpViews () {

			ViewLeft.ScrolledWindow.Hadjustment.ValueChanged  += SyncViewsHorizontally;
			ViewLeft.ScrolledWindow.Vadjustment.ValueChanged  += SyncViewsVertically;
			ViewRight.ScrolledWindow.Hadjustment.ValueChanged += SyncViewsHorizontally;
			ViewRight.ScrolledWindow.Vadjustment.ValueChanged += SyncViewsVertically;
		
		}


		// Keeps the two image views in sync horizontally
		private void SyncViewsHorizontally (object o, EventArgs args) {

			Adjustment source_adjustment = (Adjustment) o;
			
			if (source_adjustment == ViewLeft.ScrolledWindow.Hadjustment)
				ViewRight.ScrolledWindow.Hadjustment = source_adjustment;
			else
				ViewLeft.ScrolledWindow.Hadjustment = source_adjustment;			

		}


		// Keeps the two image views in sync vertically
		private void SyncViewsVertically (object o, EventArgs args) {

			Adjustment source_adjustment = (Adjustment) o;

			if (source_adjustment == ViewLeft.ScrolledWindow.Vadjustment)
				ViewRight.ScrolledWindow.Vadjustment = source_adjustment;
			else
				ViewLeft.ScrolledWindow.Vadjustment = source_adjustment;			

		}


		// Gets a list of all earlier revisions of this file
		private string [] GetRevisionsForFilePath (string file_path)
		{

			Process process = new Process ();
			process.EnableRaisingEvents = true; 
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			process.StartInfo.WorkingDirectory = SparkleDiff.GetGitRoot (file_path);
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "log --format=\"%H\" " + SparkleDiff.GetPathFromGitRoot (file_path);

			process.Start ();

			string output = process.StandardOutput.ReadToEnd ();
			string [] revisions = Regex.Split (output.Trim (), "\n");

			return revisions;

		}


		// Converts a UNIX timestamp to a more usable time object
		public DateTime UnixTimestampToDateTime (int timestamp)
		{
			DateTime unix_epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0);
			return unix_epoch.AddSeconds (timestamp);
		}


		// Quits the program		
		private void Quit (object o, EventArgs args) {

			Environment.Exit (0);

		}

	}

}
