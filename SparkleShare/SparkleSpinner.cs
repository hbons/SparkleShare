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
using System.Timers;

namespace SparkleShare {
		
	// This is a close implementation of GtkSpinner
	public class SparkleSpinner : Gdk.Pixbuf {

		private int CycleDuration;
		private int NumSteps;
		private bool Active;

		private Timer Timer;
		private int CurrentStep;

		public SparkleSpinner () : base ("")  {
			Timer = new Timer ();
			CycleDuration = 1000;
			CurrentStep = 0;
			NumSteps = 20;
			Timer.Interval = CycleDuration / NumSteps;
			Timer.Elapsed += delegate { NextImage (); };
			Start ();
		}
		
		private void NextImage () {
		Console.WriteLine (CurrentStep);
			if (CurrentStep < NumSteps)
				CurrentStep++;
			else
				CurrentStep = 0;
		}
				
		public bool IsActive () {
			return Active;
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
