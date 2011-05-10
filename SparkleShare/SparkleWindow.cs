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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
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

			Title          = "SparkleShare Setup";
			BorderWidth    = 0;
			IconName       = "folder-sparkleshare";
			Resizable      = false;
			WindowPosition = WindowPosition.Center;

			DeleteEvent += delegate (object o, DeleteEventArgs args) {

				args.RetVal = true;
				Close ();

			};

			HBox = new HBox (false, 6);

				VBox = new VBox (false, 0);

					Wrapper = new VBox (false, 0) {
						BorderWidth = 30
					};

					Buttons = CreateButtonBox ();

				VBox.PackStart (Wrapper, false, true, 0);
				VBox.PackStart (new Alignment(0,0,0,0),true,true,0);
				VBox.PackStart (Buttons, false, true, 0);

				EventBox box = new EventBox ();
				Gdk.Color bg_color = new Gdk.Color ();
				Gdk.Color.Parse ("#2e3336", ref bg_color);
				box.ModifyBg (StateType.Normal, bg_color);

					string image_path = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare",
						"pixmaps", "side-splash.png");

					Image side_splash = new Image (image_path) {
						Yalign = 1
					};

				box.Add (side_splash);

			HBox.PackStart (box, false, false, 0);
			HBox.PackStart (VBox, true, true, 0);

			base.Add (HBox);
			//base.SetSizeRequest (680, 140);	
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
		//	ShowAll ();

		}


		public void Reset ()
		{

			if (Wrapper.Children.Length > 0)
				Wrapper.Remove (Wrapper.Children [0]);

			ClearButtons();
			ShowAll ();

		}
		public void ClearButtons()
		{
			foreach (Button button in Buttons)
				Buttons.Remove (button);
		}
		new public void ShowAll ()
		{
		
			Present ();
			base.ShowAll ();
		
		}

		public void Close ()
		{

			HideAll ();

		}

	}

}
