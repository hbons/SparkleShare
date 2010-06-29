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

			Gtk.Application.Init ();
			SparkleDiffWindow sparkle_diff_window;
			sparkle_diff_window = new SparkleDiffWindow ("/home/hbons/SparkleShare/Deal/ANDRESDIAZGeorgeWashington.jpg");
			sparkle_diff_window.ShowAll ();

			// The main loop
			Gtk.Application.Run ();

		}

	}


	public class SparkleDiffWindow : Window
	{

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		private string FilePath;
		private string FileName;

		private RevisionView ViewLeft;
		private RevisionView ViewRight;

		private string [] RevisionHashes;

		public SparkleDiffWindow (string file_path) : base ("")
		{

			FilePath = file_path;
			FileName = System.IO.Path.GetFileName (FilePath);

			SetSizeRequest (1024, 600);
	 		SetPosition (WindowPosition.Center);

			BorderWidth = 12;

			Title = String.Format(_("Comparing Versions of ‘{0}’"), System.IO.Path.GetFileName (FilePath));
			IconName = "folder-sparkleshare";
			
			GetRevisions ();

			VBox layout_vertical = new VBox (false, 12);

				HBox layout_horizontal = new HBox (false, 12);
				
				ViewLeft  = new RevisionView ();
				ViewRight = new RevisionView ();
				layout_horizontal.PackStart (ViewLeft);
				layout_horizontal.PackStart (ViewRight);

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


		private string [] GetRevisions ()
		{

			Process process = new Process ();
			process.EnableRaisingEvents = true; 
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (FilePath);
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "log --format=\"%H\" " + FileName;
			process.Start ();

			string output = process.StandardOutput.ReadToEnd ().Trim ();
			RevisionHashes = Regex.Split (output, "\n");

			return RevisionHashes;

		}


		private void SyncViewsHorizontally (object o, EventArgs args) {

			Widget [] view_left_children = ViewLeft.Children;
			ScrolledWindow left_scrolled_window = (ScrolledWindow) view_left_children [0];

			Widget [] view_right_children = ViewRight.Children;
			ScrolledWindow right_scrolled_window = (ScrolledWindow) view_right_children [0];

			Adjustment source_adjustment = (Adjustment) o;
			
			if (source_adjustment == left_scrolled_window.Hadjustment)
				right_scrolled_window.Hadjustment = source_adjustment;
			else
				left_scrolled_window.Hadjustment = source_adjustment;			

		}


		private void SyncViewsVertically (object o, EventArgs args) {

			Widget [] view_left_children = ViewLeft.Children;
			ScrolledWindow left_scrolled_window = (ScrolledWindow) view_left_children [0];

			Widget [] view_right_children = ViewRight.Children;
			ScrolledWindow right_scrolled_window = (ScrolledWindow) view_right_children [0];

			Adjustment source_adjustment = (Adjustment) o;
			
			if (source_adjustment == left_scrolled_window.Vadjustment)
				right_scrolled_window.Vadjustment = source_adjustment;
			else
				left_scrolled_window.Vadjustment = source_adjustment;			

		}

		

		private void UpdateViews (object o, EventArgs args)
		{

			HBox hbox_left = (HBox) ViewLeft.Children [1];
			ComboBox combobox_left = (ComboBox) hbox_left.Children [1];
			string version_left  = RevisionHashes [combobox_left.Active];

			HBox hbox_right = (HBox) ViewRight.Children [1];
			ComboBox combobox_right = (ComboBox) hbox_right.Children [1];
			string version_right  = RevisionHashes [combobox_right.Active];

			Process process = new Process ();
			process.EnableRaisingEvents = true; 
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (FilePath);
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "show " + version_left + ":" + FileName;
			process.Start ();

			Gdk.Pixbuf pixbuf;
			pixbuf = new Gdk.Pixbuf ( (System.IO.Stream) process.StandardOutput.BaseStream);

			ViewLeft.Remove (ViewLeft.Children [0]);
			ScrolledWindow scrolled_window = new ScrolledWindow ();
			scrolled_window.AddWithViewport (new Image (pixbuf));
			
			ViewLeft.PackStart (scrolled_window, true, true, 0);
			ViewLeft.ReorderChild (scrolled_window, 0);

			scrolled_window.Hadjustment.ValueChanged += SyncViewsHorizontally;
			scrolled_window.Vadjustment.ValueChanged += SyncViewsVertically;

			ShowAll ();

		}


		private VBox CreateRevisionView (string position)
		{


				Process process = new Process ();
				process.EnableRaisingEvents = true; 
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.UseShellExecute = false;

				process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName (FilePath);
				process.StartInfo.FileName = "git";
				process.StartInfo.Arguments = "show HEAD:" + FileName;
				process.Start ();

				Gdk.Pixbuf pixbuf;
				pixbuf = new Gdk.Pixbuf ( (System.IO.Stream) process.StandardOutput.BaseStream);


				ViewLeft.ScrolledWindow.Hadjustment.ValueChanged += SyncViewsHorizontally;
				ViewLeft.ScrolledWindow.Vadjustment.ValueChanged += SyncViewsVertically;


/*					bool current_version = true;
					foreach (string hash in RevisionHashes) {
						Console.WriteLine (hash);
						if (current_version) {
							revision_combobox.AppendText ("Current Version");
							current_version = false;
						} else {
							revision_combobox.AppendText (hash);
						}
					}
				
					if (position.Equals ("Left"))
						revision_combobox.Active = 1;
					else if (position.Equals ("Right"))
						revision_combobox.Active = 0;
						
					revision_combobox.Changed += UpdateViews;
*/return new VBox ();

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


		public RevisionView () : base (false, 6) 
		{

			Image = new Image ();

			ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (Image);
			PackStart (ScrolledWindow, true, true, 0);

			HButtonBox controls = new HButtonBox ();
			controls.Layout = ButtonBoxStyle.Start;
			controls.BorderWidth = 0;

				Image image_previous = new Image ();
				image_previous.IconName = "go-previous";
				ButtonPrevious = new Button (image_previous);

				ValueCount = 0;
				ComboBox = ComboBox.NewText ();

				Image image_next = new Image ();
				image_next.IconName = "go-next";
				ButtonNext = new Button (image_next);
//move outside this class				ButtonNext.Clicked += Next;

			controls.Add (ButtonPrevious);				
			controls.Add (ComboBox);
			controls.Add (ButtonNext);	

			PackStart (controls, false, false, 0);

		}


		// Fills the widget's combobox with entries
		public void FillComboBox (string [] values) {

			ValueCount = values.Length;
			ComboBox.Changed += Update;

		}


		// Changes the image that is viewed
		public void SetImage (Image image) {

			Image = image;
			ShowAll ();

		}


		// Updates the buttons to be disabled or enabled when needed
		public void UpdateControls () {

			if (ComboBox.Active == 0)
				ButtonPrevious.State = StateType.Insensitive;
			
			if (ComboBox.Active + 1 < ValueCount)
				ButtonNext.State = StateType.Insensitive;
				
		}


	}

}
