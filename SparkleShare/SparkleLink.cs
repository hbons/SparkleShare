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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using System.IO;
using System.Diagnostics;
using System; //dfsf
namespace SparkleShare {

	// A clickable link that corresponds to a local file	
	public class SparkleLink : EventBox {

		public SparkleLink (string title, string url) : base ()
		{

			Label label = new Label (title) {
				Ellipsize = Pango.EllipsizeMode.Middle,
				UseMarkup = true,
				Xalign    = 0
			};

			Add (label);

			Gdk.Color color = new Gdk.Color ();

			// Only make links for files that exist
			if (!url.StartsWith ("http://") && !File.Exists (url)) {

				// Use Tango Aluminium for the links
				Gdk.Color.Parse ("#2e3436", ref color);
				label.ModifyFg (StateType.Normal, color);
				return;

			}

			// Use Tango Sky Blue for the links
			Gdk.Color.Parse ("#3465a4", ref color);
			label.ModifyFg (StateType.Normal, color);

			// Open the URL when it is clicked
			ButtonReleaseEvent += delegate {

				Process process = new Process ();
				process.StartInfo.FileName  = "gnome-open";
				process.StartInfo.Arguments = url.Replace (" ", "\\ "); // Escape space-characters
				process.Start ();

			};
 
			// Add underline when hovering the link with the cursor
			EnterNotifyEvent += delegate {

				label.Markup = "<u>" + title + "</u>";
				ShowAll ();
				Realize ();
				GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Hand2);

			};

			// Remove underline when leaving the link with the cursor
			LeaveNotifyEvent += delegate {

				label.Markup = title;
				ShowAll ();
				Realize ();
				GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Arrow);

			};

		}

	}

}
