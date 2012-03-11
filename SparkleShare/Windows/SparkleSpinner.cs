//   SparkleShare, a collaboration and sharing tool.
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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SparkleShare {
        
	// TODO: cleanup
    // This is a close implementation of GtkSpinner
    public class SparkleSpinner : Image {

        public bool Active;

        private Image [] Images;
        private Timer Timer;
        private int CycleDuration;
        private int CurrentStep;
        private int NumSteps;
        private int Size;


        public SparkleSpinner (int size) : base ()
        {
			
			Width = size;
			Height = size;
            Size = size;

            CycleDuration = 400;
            CurrentStep = 0;

			BitmapSource spinner_gallery = SparkleUIHelpers.GetImageSource ("process-working-22");
            
			
            int frames_in_width  = spinner_gallery.PixelWidth / Size;
            int frames_in_height = spinner_gallery.PixelHeight / Size;

            NumSteps = frames_in_width * frames_in_height;
            Images   = new Image [NumSteps - 1];

            int i = 0;

            for (int y = 0; y < frames_in_height; y++) {
                for (int x = 0; x < frames_in_width; x++) {
                    if (!(y == 0 && x == 0)) {
						CroppedBitmap crop = new CroppedBitmap (
							spinner_gallery,
							new Int32Rect (x*Size, y*Size, Size, Size));
						Images [i] = new Image ();
						Images [i].Source = crop;
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

            Dispatcher.Invoke ((Action)delegate { SetImage (); });
        }


        private void SetImage ()
        {
            Source = Images [CurrentStep].Source;
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
