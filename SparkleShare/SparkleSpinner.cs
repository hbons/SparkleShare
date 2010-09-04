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
using SparkleLib;

namespace SparkleShare {
		
	// This is a close implementation of GtkSpinner
	public class SparkleSpinner : Image
	{

		public bool Active;

		private Gdk.Pixbuf [] Images;
		private Timer Timer;
		private int CycleDuration;
		private int CurrentStep;
		private int NumSteps;
		private int Size;

		public SparkleSpinner (int size) : base ()
		{

			Size = size;

			CycleDuration = 600;
			CurrentStep = 0;

			Gdk.Pixbuf spinner_gallery = SparkleUIHelpers.GetIcon ("process-working", Size);

			int frames_in_width  = spinner_gallery.Width / Size;
			int frames_in_height = spinner_gallery.Height / Size;

			NumSteps = frames_in_width * frames_in_height;
			Images   = new Gdk.Pixbuf [NumSteps - 1];

			int i = 0;

			for (int y = 0; y < frames_in_height; y++) {

				for (int x = 0; x < frames_in_width; x++) {

					if (!(y == 0 && x == 0)) {
						
						Images [i] = new Gdk.Pixbuf (spinner_gallery, x * Size, y * Size, Size, Size);
						i++;

					}

				}

			}

			Timer = new Timer () {
				Interval = CycleDuration / NumSteps
			};

			Timer.Elapsed += delegate {
				NextImage ();
			};

			Start ();

		}

		private void NextImage ()
		{

			if (CurrentStep < NumSteps - 2)
				CurrentStep++;
			else
				CurrentStep = 0;

			Application.Invoke (delegate { SetImage (); });

		}

		private void SetImage ()
		{

			Pixbuf = Images [CurrentStep];

		}
						
		public bool IsActive ()
		{

			return Active;

		}

		public void Start ()
		{

			CurrentStep = 0;
			Active = true;
			Timer.Start ();

		}

		public void Stop ()
		{

			Active = false;
			Timer.Stop ();

		}

	}

}
