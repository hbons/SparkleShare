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

using FriendFace;
using Gtk;
using Mono.Unix;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SparkleShare {

	// The main window for SparkleDiff
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

		public SparkleDiffWindow (string file_path, string [] revisions) : base ("")
		{

			string file_name = System.IO.Path.GetFileName (file_path);
			Revisions = revisions;

	 		SetPosition (WindowPosition.Center);
			BorderWidth = 12;
			IconName = "image-x-generic";

			FaceCollection face_collection = new FaceCollection ();
			face_collection.UseGravatar = true;
			
			DeleteEvent += Quit;

			Title = file_name;

			VBox layout_vertical = new VBox (false, 12);

				HBox layout_horizontal = new HBox (true, 6);

					Process process = new Process ();
					process.EnableRaisingEvents = true; 
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.UseShellExecute = false;

					process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (file_path);
					process.StartInfo.FileName = "git";
					process.StartInfo.Arguments = "log --format=\"%ct\t%an\t%ae\" " + file_name;
					process.Start ();

					ViewLeft  = new LeftRevisionView  ();
					ViewRight = new RightRevisionView ();

					string output = process.StandardOutput.ReadToEnd ();
					string [] revisions_info = Regex.Split (output.Trim (), "\n");

					int i = 0;
					foreach (string revision_info in revisions_info) {

						string [] parts = Regex.Split (revision_info.Trim (), "\t");

						int timestamp = int.Parse (parts [0]);
						string author = parts [1];
						string email  = parts [2];

						string date;
						// TRANSLATORS: This is a format specifier according to System.Globalization.DateTimeFormatInfo	
						if (i == 0)
							date = "Latest Revision";
						else
							date = String.Format (_("{0} at {1}"),
							       UnixTimestampToDateTime (timestamp).ToString (_("ddd MMM d, yyyy")),
							       UnixTimestampToDateTime (timestamp).ToString (_("H:mm")));

						face_collection.AddFace (email);

						ViewLeft.AddRow  (face_collection.GetFace (email, 32), author, date);
						ViewRight.AddRow (face_collection.GetFace (email, 32), author, date);
						
						i++;

					}


					ViewLeft.SetImage  (new RevisionImage (file_path, Revisions [1]));
					ViewRight.SetImage (new RevisionImage (file_path, Revisions [0]));
					
					ViewLeft.IconView.SelectionChanged += delegate {
					
						ViewLeft.SetImage  (new RevisionImage (file_path, Revisions [ViewLeft.GetSelected ()]));

						ViewLeft.ScrolledWindow.Hadjustment = ViewRight.ScrolledWindow.Hadjustment;
						ViewLeft.ScrolledWindow.Vadjustment = ViewRight.ScrolledWindow.Vadjustment;
						
						HookUpViews ();

					};

					ViewRight.IconView.SelectionChanged += delegate {
					
						ViewRight.SetImage  (new RevisionImage (file_path, Revisions [ViewRight.GetSelected ()]));

						ViewRight.ScrolledWindow.Hadjustment = ViewLeft.ScrolledWindow.Hadjustment;
						ViewRight.ScrolledWindow.Vadjustment = ViewLeft.ScrolledWindow.Vadjustment;
						
						HookUpViews ();

					};
					
					ViewLeft.ToggleButton.Clicked += delegate {
						if (ViewLeft.ToggleButton.Active)
							DetachViews ();
						else
							HookUpViews ();
					};
					
					ViewRight.ToggleButton.Clicked += delegate {
						if (ViewLeft.ToggleButton.Active)
							DetachViews ();
						else
							HookUpViews ();
					};

				layout_horizontal.PackStart (ViewLeft);
				layout_horizontal.PackStart (ViewRight);

				ResizeToViews ();

				// Order time view according to the user's reading direction
				if (Direction == Gtk.TextDirection.Rtl)
					layout_horizontal.ReorderChild (ViewLeft, 1);

				HookUpViews ();

				HButtonBox dialog_buttons  = new HButtonBox ();
				dialog_buttons.Layout      = ButtonBoxStyle.End;
				dialog_buttons.BorderWidth = 0;

					Button close_button = new Button (Stock.Close);
					close_button.Clicked += delegate (object o, EventArgs args) {
						Environment.Exit (0);
					};
					
				dialog_buttons.Add (close_button);

			layout_vertical.PackStart (layout_horizontal, true, true, 0);
			layout_vertical.PackStart (dialog_buttons, false, false, 0);

			Add (layout_vertical);

		}


		// Resizes the window so it will fit the content in the best possible way
		private void ResizeToViews ()
		{

			int new_width  = ViewLeft.GetImage ().Pixbuf.Width + ViewRight.GetImage ().Pixbuf.Width + 200;
			int new_height = 200;

			if (ViewLeft.GetImage ().Pixbuf.Height > ViewRight.GetImage ().Pixbuf.Height)
				new_height += ViewLeft.GetImage ().Pixbuf.Height;
			else
				new_height += ViewRight.GetImage ().Pixbuf.Height;

			if (new_width >= Screen.Width || new_height >= Screen.Height)
				Maximize ();
			else
				SetSizeRequest (new_width, new_height);
			
		}


		// Hooks up two views so their scrollbars will be kept in sync
		private void HookUpViews ()
		{

			ViewLeft.ScrolledWindow.Hadjustment.ValueChanged  += SyncViewsHorizontally;
			ViewLeft.ScrolledWindow.Vadjustment.ValueChanged  += SyncViewsVertically;
			ViewRight.ScrolledWindow.Hadjustment.ValueChanged += SyncViewsHorizontally;
			ViewRight.ScrolledWindow.Vadjustment.ValueChanged += SyncViewsVertically;
		
		}
		

		// Detach the two views from each other so they don't try to sync anymore
		private void DetachViews ()
		{

			ViewLeft.ScrolledWindow.Hadjustment.ValueChanged  -= SyncViewsHorizontally;
			ViewLeft.ScrolledWindow.Vadjustment.ValueChanged  -= SyncViewsVertically;
			ViewRight.ScrolledWindow.Hadjustment.ValueChanged -= SyncViewsHorizontally;
			ViewRight.ScrolledWindow.Vadjustment.ValueChanged -= SyncViewsVertically;
		
		}


		// Keeps the two image views in sync horizontally
		private void SyncViewsHorizontally (object o, EventArgs args)
		{

			Adjustment source_adjustment = (Adjustment) o;
			
			if (source_adjustment == ViewLeft.ScrolledWindow.Hadjustment)
				ViewRight.ScrolledWindow.Hadjustment = source_adjustment;
			else
				ViewLeft.ScrolledWindow.Hadjustment = source_adjustment;			

		}


		// Keeps the two image views in sync vertically
		private void SyncViewsVertically (object o, EventArgs args)
		{

			Adjustment source_adjustment = (Adjustment) o;

			if (source_adjustment == ViewLeft.ScrolledWindow.Vadjustment)
				ViewRight.ScrolledWindow.Vadjustment = source_adjustment;
			else
				ViewLeft.ScrolledWindow.Vadjustment = source_adjustment;			

		}


		// Converts a UNIX timestamp to a more usable time object
		public DateTime UnixTimestampToDateTime (int timestamp)
		{
			DateTime unix_epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0);
			return unix_epoch.AddSeconds (timestamp);
		}


		// Quits the program		
		private void Quit (object o, EventArgs args)
		{
			Environment.Exit (0);
		}

	}

}
