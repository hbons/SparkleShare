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

    public class SparkleSpinner : Image {

        private Image [] images;
        private Timer timer;
		private int num_steps;
        private int current_step = 0;


        public SparkleSpinner (int size) : base ()
        {			
			Width  = size;
			Height = size;

			BitmapSource spinner_gallery = SparkleUIHelpers.GetImageSource ("process-working-22");
           			
            int frames_in_width  = spinner_gallery.PixelWidth / size;
            int frames_in_height = spinner_gallery.PixelHeight / size;

            this.num_steps = (frames_in_width * frames_in_height) - 1;
            this.images    = new Image [this.num_steps];

            int i = 0;

            for (int y = 0; y < frames_in_height; y++) {
                for (int x = 0; x < frames_in_width; x++) {
                    if (!(y == 0 && x == 0)) {
						CroppedBitmap crop = new CroppedBitmap (spinner_gallery, 
                            new Int32Rect (size * x, size * y, size, size));
						
						this.images [i]        = new Image ();
						this.images [i].Source = crop;
                        i++;
                    }
                }
            }

            this.timer = new Timer () {
                Interval = 400 / this.num_steps
            };

            this.timer.Elapsed += delegate {
	            Dispatcher.BeginInvoke ((Action) delegate {
	                NextImage ();
				});
            };

            Start ();
        }
		
		
        public void Start ()
        {
            this.timer.Start ();
        }


        public void Stop ()
        {
            this.timer.Stop ();
			this.current_step = 0;
        }
		
		
        private void NextImage ()
        {
            if (this.current_step < this.num_steps - 1)
                this.current_step++;
            else
                this.current_step = 0;

			SetImage ();
        }


        private void SetImage ()
        {
            Source = this.images [this.current_step].Source;
        }
    }
}
