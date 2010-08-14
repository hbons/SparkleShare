//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Gtk;

namespace SparkleShare {

	public class SparkleEntry : Entry {

		public bool ExampleTextActive;
		private string pExampleText;


		public SparkleEntry ()
		{

			ExampleTextActive = true;

			FocusGrabbed += delegate {

				if (ExampleTextActive) {

					ExampleTextActive = false;
					Text = "";	
					UseNormalTextColor ();

				}
			
			};
			
			FocusOutEvent += delegate {

				if (Text.Equals ("") || Text == null) {
					ExampleTextActive = true;

				}

				if (ExampleTextActive)
					UseExampleText ();

			};

		}


		public string ExampleText
		{

			get {

				return pExampleText;

			}

			set {

				pExampleText = value;

				if (ExampleTextActive) {

					UseExampleText ();

				}

			}

		}


		private void UseExampleText ()
		{

			Text = pExampleText;
			UseSecondaryTextColor ();

		}


		private void UseSecondaryTextColor ()
		{

			ModifyText (StateType.Normal, Style.Foreground (StateType.Insensitive));

		}


		private void UseNormalTextColor ()
		{

			ModifyText (StateType.Normal, Style.Foreground (StateType.Normal));

		}

	}

}
