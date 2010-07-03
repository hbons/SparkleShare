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
using System.IO;
using System.Text.RegularExpressions;

namespace SparkleShare {

	public class SparkleDiff
	{

		public static void Main (string [] args)
		{
			Catalog.Init (Defines.GETTEXT_PACKAGE, Defines.LOCALE_DIR);

			if (args.Length > 0) {

				string file_path = args [0];

				if (File.Exists (file_path)) {

					Gtk.Application.Init ();

					SparkleDiffWindow sparkle_diff_window;
					sparkle_diff_window = new SparkleDiffWindow (file_path);
					sparkle_diff_window.ShowAll ();

					// The main loop
					Gtk.Application.Run ();

				} else {

					Console.WriteLine ("SparkleDiff: " + file_path + ": No such file or directory.");
					Environment.Exit (0);

				}
				
			}

		}

	}


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

			string file_name = System.IO.Path.GetFileName (file_path);

			SetSizeRequest (800, 540);
	 		SetPosition (WindowPosition.Center);

			BorderWidth = 12;

			DeleteEvent += Quit;

			IconName = "image-x-generic";
			// TRANSLATORS: The parameter is a filename
			Title = String.Format(_("Comparing Revisions of ‘{0}’"), file_name);
			
			Revisions = GetRevisionsForFile (file_path);

			VBox layout_vertical = new VBox (false, 12);

				HBox layout_horizontal = new HBox (false, 12);

					Process process = new Process ();
					process.EnableRaisingEvents = true; 
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.UseShellExecute = false;

					// TODO: Nice commit summary and "Current Revision"
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

					ViewLeft  = new RevisionView (revisions_info);
					ViewRight = new RevisionView (revisions_info);
					
					ViewLeft.ComboBox.Active  = 1;
					ViewRight.ComboBox.Active = 0;

					RevisionImage revision_image_left  = new RevisionImage (file_path, Revisions [1]);
					RevisionImage revision_image_right = new RevisionImage (file_path, Revisions [0]);

					ViewLeft.SetImage (revision_image_left);
					ViewRight.SetImage (revision_image_right);
					
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


		// Hooks up two views so they will be kept in sync
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
		private string [] GetRevisionsForFile (string file_path)
		{

			string file_name = System.IO.Path.GetFileName (file_path);

			Process process = new Process ();
			process.EnableRaisingEvents = true; 
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			// TODO: Nice commit summary and "Current Revision"
			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (file_path);
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "log --format=\"%H\" " + file_name;
			process.Start ();

			string output = process.StandardOutput.ReadToEnd ();

			return Regex.Split (output.Trim (), "\n");

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


	// An image grabbed from a stream generated by Git
	public class RevisionImage : Image
	{
	
		public string Revision;
		public string FilePath;
	
		public RevisionImage (string file_path, string revision) : base ()
		{
		
			Revision = revision;
			FilePath = file_path;

			Process process = new Process ();
			process.EnableRaisingEvents = true; 
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (FilePath);
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "show " + revision + ":" + System.IO.Path.GetFileName (FilePath);
			process.Start ();

			Pixbuf = new Gdk.Pixbuf ((System.IO.Stream) process.StandardOutput.BaseStream);
		
		}
	
	}


	// A custom widget containing an image view,
	// previous/next buttons and a combobox
	public class RevisionView : VBox
	{

		public ScrolledWindow ScrolledWindow;
		public ComboBox ComboBox;
		public Button ButtonPrevious;
		public Button ButtonNext;
		
		private int ValueCount;
		private Image Image;

		public RevisionView (string [] revisions) : base (false, 6) 
		{

			Image = new Image ();

			ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (Image);
			PackStart (ScrolledWindow, true, true, 0);

			HBox controls = new HBox (false, 6);
			controls.BorderWidth = 0;

				Image image_previous = new Image ();
				image_previous.IconName = "go-previous";
				ButtonPrevious = new Button (image_previous);
				ButtonPrevious.Clicked += PreviousInComboBox;

				ValueCount = 0;

				ComboBox = ComboBox.NewText ();

				foreach (string revision in revisions) {
					ComboBox.AppendText (revision);
				}

				ComboBox.Active = 0;
				
				ValueCount = revisions.Length;

				Image image_next = new Image ();
				image_next.IconName = "go-next";
				ButtonNext = new Button (image_next);
				ButtonNext.Clicked += NextInComboBox;

//			controls.PackStart (ButtonPrevious, false, false, 0);
			controls.PackStart (ComboBox, false, false, 0);
//			controls.PackStart (ButtonNext, false, false, 0);

			PackStart (controls, false, false, 0);

			UpdateControls ();

		}


		public void NextInComboBox (object o, EventArgs args) {

/*			if (ComboBox.Active > 0)
				ComboBox.Active--;

			UpdateControls ();
*/
		}
	

		public void PreviousInComboBox (object o, EventArgs args) {

/*			if (ComboBox.Active + 1 < ValueCount)
				ComboBox.Active++;

			UpdateControls ();
*/
		}


		// Changes the image that is viewed
		public void SetImage (Image image) {

			Image = image;
			Remove (ScrolledWindow);
			ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (Image);
			Add (ScrolledWindow);
			ReorderChild (ScrolledWindow, 0);
			ShowAll ();

		}


		// Updates the buttons to be disabled or enabled when needed
		public void UpdateControls () {

			// TODO: Doesn't work yet. Sleepy -.-
/*			ButtonPrevious.State = StateType.Normal;
			ButtonNext.State = StateType.Normal;

			if (ComboBox.Active == 0)
				ButtonNext.State = StateType.Insensitive;

			if (ComboBox.Active + 1 == ValueCount)
				ButtonPrevious.State = StateType.Insensitive;
*/

		}

	}

}
