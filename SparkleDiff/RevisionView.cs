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

namespace SparkleShare {

	// A custom widget containing an image view,
	// previous/next buttons and a combobox
	public class RevisionView : VBox
	{

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public ScrolledWindow ScrolledWindow;
		public IconView IconView;
		public int Selected;
		
		public ToggleButton ToggleButton;
		private Viewport Viewport;
		private ListStore Store;
		private Image Image;
		private int Count;
		private string SecondaryTextColor;
		private string SelectedTextColor;


		public RevisionView () : base (false, 0) 
		{

			Count = 0;
			Selected = 0;

			TreeView treeview = new TreeView ();
			SelectedTextColor  = GdkColorToHex (treeview.Style.Foreground (StateType.Selected));

			Window window = new Window ("");
			SecondaryTextColor = GdkColorToHex (window.Style.Foreground (StateType.Insensitive));

			ToggleButton = new ToggleButton ();
			ToggleButton.Clicked += ToggleView;
			ToggleButton.Relief = ReliefStyle.None;

			ScrolledWindow = new ScrolledWindow ();

			Viewport = new Viewport ();
			Viewport.Add (new Label (""));

			Store = new ListStore (typeof (Gdk.Pixbuf),
			                       typeof (string),
			                       typeof (int));

			IconView = new IconView (Store);
			IconView.SelectionChanged += ChangeSelection;
			IconView.MarkupColumn = 1;
			IconView.Margin       = 12;
			IconView.Orientation  = Orientation.Horizontal;
			IconView.PixbufColumn = 0;
			IconView.Spacing      = 12;
			
			Image = new Image ();

			ScrolledWindow.Add (Viewport);
			PackStart (ScrolledWindow, true, true, 0);

		}
		

		// Changes the selection and enforces a policy of always having something selected
		public void ChangeSelection (object o, EventArgs args)
		{

			TreeIter iter;
			Store.GetIter (out iter, new TreePath (GetSelected ().ToString ()));
			string text = (string) Store.GetValue (iter, 1);
			Store.SetValue (iter, 1, text.Replace (SelectedTextColor, SecondaryTextColor));

			if (IconView.SelectedItems.Length > 0) {

				Store.GetIter (out iter, IconView.SelectedItems [0]);
				SetSelected ((int) Store.GetValue (iter, 2));

				text = (string) Store.GetValue (iter, 1);
				text = text.Replace (SecondaryTextColor, SelectedTextColor);
				Store.SetValue (iter, 1, text);

			} else {

				IconView.SelectPath (new TreePath (GetSelected ().ToString()));

			}
			
		}
		
		
		// Converts a Gdk RGB color to a hex value.
		// Example: from "rgb:0,0,0" to "#000000"
		public string GdkColorToHex (Gdk.Color color)
		{

			return String.Format("#{0:X2}{1:X2}{2:X2}",
				(int) Math.Truncate(color.Red   / 256.00),
				(int) Math.Truncate(color.Green / 256.00),
				(int) Math.Truncate(color.Blue  / 256.00));

		}


		// Makes sure everything is in place before showing the widget
		new public void ShowAll ()
		{

			if (Children.Length == 2) {

				ToggleButton = (ToggleButton) Children [0];
				ToggleButton.Remove (ToggleButton.Child);

			} else {

				ToggleButton = new ToggleButton ();
				ToggleButton.Relief = ReliefStyle.None;
				ToggleButton.Clicked += ToggleView;
				PackStart (ToggleButton, false, false, 6);

			}
			
			HBox layout_horizontal = new HBox (false, 12);
			layout_horizontal.BorderWidth = 6;

				TreeIter iter;
				Store.GetIter (out iter, new TreePath (GetSelected ().ToString()));

				string text = (string) Store.GetValue (iter, 1);
				Gdk.Pixbuf pixbuf = (Gdk.Pixbuf) Store.GetValue (iter, 0);
				
				Label label = new Label (text.Replace (SelectedTextColor, SecondaryTextColor));
				label.UseMarkup = true;

				Arrow arrow_down = new Arrow (ArrowType.Down, ShadowType.None);

			layout_horizontal.PackStart (new Image (pixbuf), false, false, 0);
			layout_horizontal.PackStart (label, false, false, 0);
			layout_horizontal.PackStart (new Label (""), true, true, 0);
			layout_horizontal.PackStart (arrow_down, false, false, 0);

			ToggleButton.Add (layout_horizontal);
			ReorderChild (ToggleButton, 0);

			TreePath path = new TreePath (Selected.ToString());
			IconView.SelectPath (path);

			base.ShowAll ();

		}


		// Adds a revision to the combobox
		public void AddRow (Gdk.Pixbuf pixbuf, string header, string subtext)
		{

			Store.AppendValues (pixbuf, "<b>" + header + "</b>\n" +
			                            "<span fgcolor='" + SecondaryTextColor + "'>" + subtext + "</span>",
				Count);

			IconView.Model = Store;
			Count++;

		}


		// Toggles between a displayed image and a list of revisions
		public void ToggleView (object o, EventArgs args)
		{

			Viewport.Remove (Viewport.Child);

			if (ToggleButton.Active) {
			
				Viewport.Add (IconView);
				TreePath path = new TreePath (GetSelected ().ToString());

				IconView.ScrollToPath (path, (float) 0.5, (float) 0.5);
				IconView.GrabFocus ();

			} else {

				Viewport.Add (Image);

			}
			
			ShowAll ();

		}


		// Changes the image that is viewed
		public void SetImage (Image image)
		{

			Image = image;
			Viewport.Remove (Viewport.Child);
			Viewport.Add (Image);
			ToggleButton.Active = false;
			ShowAll ();

		}


		// Returns the image that is currently viewed
		public Image GetImage ()
		{
			return Image;
		}
		

		// Selects an item by number
		public bool SetSelected (int i)
		{

			if (i > -1 && i <= Count) {
				Selected = i;
				return true;
			}

			return false;

		}


		// Returns the number of the currently selected item
		public int GetSelected ()
		{
			return Selected;
		}


		// Looks up an icon from the system's theme
		public Gdk.Pixbuf GetIcon (string name, int size)
		{
			IconTheme icon_theme = new IconTheme ();
			icon_theme.AppendSearchPath (System.IO.Path.Combine ("/usr/share/sparkleshare", "icons"));
			return icon_theme.LoadIcon (name, size, IconLookupFlags.GenericFallback);
		}

	}


	// Derived class for the image view on the left
	public class LeftRevisionView : RevisionView
	{

		public LeftRevisionView () : base ()
		{

			// Select the second revision
			Selected = 1;

			// Take reading direction for time into account
			if (Direction == Gtk.TextDirection.Ltr)
				ScrolledWindow.Placement = CornerType.TopRight;
			else
				ScrolledWindow.Placement = CornerType.TopLeft;

		}
	
	}


	// Derived class for the image view on the right
	public class RightRevisionView : RevisionView
	{

		public RightRevisionView () : base ()
		{

			// Take reading direction for time into account
			if (Direction == Gtk.TextDirection.Ltr)
				ScrolledWindow.Placement = CornerType.TopLeft;
			else
				ScrolledWindow.Placement = CornerType.TopRight;

		}
	
	}

}
