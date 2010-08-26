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
using Mono.Unix;
using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleShare {

	public class SparkleWindow : Window	{

		private HBox HBox;
		private VBox VBox;
		private VBox Wrapper;
		private HButtonBox Buttons;


		public SparkleWindow () : base ("")
		{

			BorderWidth    = 0;
			IconName       = "folder-sparkleshare";
			Resizable      = true;
			WindowPosition = WindowPosition.Center;

			SetDefaultSize (640, 480);

			Buttons = CreateButtonBox ();

			HBox = new HBox (false, 6);

				string image_path = SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",	"side-splash.png");
				Image side_splash = new Image (image_path);

				VBox = new VBox (false, 0);

					Wrapper = new VBox (false, 0) {
						BorderWidth = 30
					};

				VBox.PackStart (Wrapper, true, true, 0);
				VBox.PackStart (Buttons, false, false, 0);

			HBox.PackStart (side_splash, false, false, 0);
			HBox.PackStart (VBox, true, true, 0);

			base.Add (HBox);

		}


		private HButtonBox CreateButtonBox ()
		{

			return new HButtonBox () {
				BorderWidth = 12,
				Layout      = ButtonBoxStyle.End,
				Spacing     = 6
			};

		}


		public void AddButton (Button button)
		{

			Buttons.Add (button);
			ShowAll ();

		}


		new public void Add (Widget widget)
		{

			Wrapper.PackStart (widget, true, true, 0);
			ShowAll ();

		}


		public void Reset ()
		{

			if (Wrapper.Children.Length > 0)
				Wrapper.Remove (Wrapper.Children [0]);

			foreach (Button button in Buttons)
				Buttons.Remove (button);

			ShowAll ();

		}
		
		new public void ShowAll ()
		{
		
			Present ();
			base.ShowAll ();
		
		}

	}

}
