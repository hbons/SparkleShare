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
using System;

namespace SparkleShare {

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

			HBox controls = new HBox (false, 3);
			controls.BorderWidth = 0;
				
				Arrow arrow_left = new Arrow (ArrowType.Left, ShadowType.None);
				ButtonPrevious = new Button ();
				ButtonPrevious.Add (arrow_left);
				ButtonPrevious.Clicked += PreviousInComboBox;
				ButtonPrevious.ExposeEvent += EqualizeSizes;

				ValueCount = 0;

				ComboBox = ComboBox.NewText ();

				foreach (string revision in revisions) {
					ComboBox.AppendText (revision);
				}

				ComboBox.Active = 0;
				
				ValueCount = revisions.Length;

				Arrow arrow_right = new Arrow (ArrowType.Right, ShadowType.None);
				ButtonNext = new Button ();
				ButtonNext.Add (arrow_right);
				ButtonNext.Clicked += NextInComboBox;
				ButtonNext.ExposeEvent += EqualizeSizes;

			controls.PackStart (new Label (""), true, false, 0);
			controls.PackStart (ButtonPrevious, false, false, 0);
			controls.PackStart (ButtonNext, false, false, 0);
			controls.PackStart (ComboBox, false, false, 9);
			controls.PackStart (new Label (""), true, false, 0);

			PackStart (controls, false, false, 0);
			PackStart (ScrolledWindow, true, true, 0);

			Shown += delegate {
				UpdateControls ();
			};

		}


		// Equalizes the height and width of a button when exposed
		private void EqualizeSizes (object o, ExposeEventArgs args) {

			Button button = (Button) o;
			button.WidthRequest = button.Allocation.Height;

		}
		

		public void NextInComboBox (object o, EventArgs args) {

			if (ComboBox.Active - 1 >= 0)
				ComboBox.Active--;

//			UpdateControls ();

		}
	

		public void PreviousInComboBox (object o, EventArgs args) {

			if (ComboBox.Active + 1 < ValueCount)
				ComboBox.Active++;

//			UpdateControls ();

		}


		// Updates the buttons to be disabled or enabled when needed
		public void UpdateControls () {

			ButtonPrevious.State = StateType.Normal;
			ButtonNext.State     = StateType.Normal;

			// TODO: Disable Next or Previous buttons when at the first or last value of the combobox
			// I can't get this to work! >:(

			if (ComboBox.Active == ValueCount - 1) {
				ButtonPrevious.State = StateType.Insensitive;
			}

			if (ComboBox.Active == 0) {
				ButtonNext.State = StateType.Insensitive;
			}


		}


		// Changes the image that is viewed
		public void SetImage (Image image) {

			Image = image;
			Remove (ScrolledWindow);
			ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (Image);
			Add (ScrolledWindow);
			ShowAll ();

		}
		
		public Image GetImage ()
		{
			return Image;
		}

	}


	// Derived class for the image view on the left
	public class LeftRevisionView : RevisionView {
	
		public LeftRevisionView (string [] revisions) : base (revisions) {

			ComboBox.Active  = 1;

			if (Direction == Gtk.TextDirection.Ltr)
				ScrolledWindow.Placement = CornerType.TopRight;
			else
				ScrolledWindow.Placement = CornerType.TopLeft;

		}
	
	}


	// Derived class for the image view on the right
	public class RightRevisionView : RevisionView {
	
		public RightRevisionView (string [] revisions) : base (revisions) {

			ComboBox.Active  = 0;

			if (Direction == Gtk.TextDirection.Ltr)
				ScrolledWindow.Placement = CornerType.TopLeft;
			else
				ScrolledWindow.Placement = CornerType.TopRight;

		}
	
	}


}
