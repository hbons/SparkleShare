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
using System.Timers;

namespace SparkleShare {
		
	// This is a close implementation of GtkSpinner
	public class SparkleSpinner : Gdk.Pixbuf {

		public int CycleDuration;
		public int NumSteps;
		public bool Active;

		private Timer Timer;
		private int CurrentStep;

		public SparkleSpinner () : base ("")  {
			Timer = new Timer ();
			CycleDuration = 1000;
			Timer.Interval = 50;
			Timer.Elapsed += delegate { 
				//
			};
			Start ();

		}

		public void Start () {
			Active = true;		
			Timer.Start ();
		}

		public void Stop () {
			Active = false;
			Timer.Stop ();
		}

	}

}
