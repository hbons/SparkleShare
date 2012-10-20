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

        private Timer timer;


        public SparkleSpinner (int size) : base ()
        {           
            Width  = size;
            Height = size;

            int current_frame            = 0;
            BitmapSource spinner_gallery = SparkleUIHelpers.GetImageSource ("process-working-22");
            int frames_in_width          = spinner_gallery.PixelWidth / size;
            int frames_in_height         = spinner_gallery.PixelHeight / size;
            int frame_count              = (frames_in_width * frames_in_height) - 1;
            Image [] frames              = new Image [frame_count];

            int i = 0;
            for (int y = 0; y < frames_in_height; y++) {
                for (int x = 0; x < frames_in_width; x++) {
                    if (!(y == 0 && x == 0)) {
                        CroppedBitmap crop = new CroppedBitmap (spinner_gallery, 
                            new Int32Rect (size * x, size * y, size, size));
                        
                        frames [i]        = new Image ();
                        frames [i].Source = crop;
                        i++;
                    }
                }
            }

            this.timer = new Timer () {
                Interval = 400 / frame_count
            };

            this.timer.Elapsed += delegate {
                Dispatcher.BeginInvoke ((Action) delegate {
                    if (current_frame < frame_count - 1)
                        current_frame++;
                    else
                        current_frame = 0;
                    
                    Source = frames [current_frame].Source;
                });
            };
        }
        
        
        public void Start ()
        {
            this.timer.Start ();
        }


        public void Stop ()
        {
            this.timer.Stop ();
        }
    }
}
