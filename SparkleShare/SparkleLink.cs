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

namespace SparkleShare {
	
	public class SparkleLink : EventBox {

		private Label Label;

		public SparkleLink (string title, string url) : base ()
		{

			Label = new Label (title) {
				Ellipsize = Pango.EllipsizeMode.Middle,
				UseMarkup = true,
				Xalign    = 0
			};

			Add (Label);

			if (!File.Exists (url))
				return;

			Gdk.Color color = new Gdk.Color ();
			Gdk.Color.Parse ("#3465a4", ref color);
			Label.ModifyFg (StateType.Normal, color);

			ButtonPressEvent += delegate {

				Process process = new Process ();
				process.StartInfo.FileName  = "xdg-open";
				process.StartInfo.Arguments = url.Replace (" ", "\\ "); // Escape space-characters
				process.Start ();

			};

			EnterNotifyEvent += delegate {

				Label.Markup = "<u>" + title + "</u>";
				ShowAll ();

			};

			LeaveNotifyEvent += delegate {

				Label.Markup = title;
				ShowAll ();

			};

		}

	}

}
