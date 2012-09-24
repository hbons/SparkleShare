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


using System.Timers;
using Gtk;

namespace SparkleShare {
        
    // This is a close implementation of GtkSpinner
    public class SparkleSpinner : Image {

        private Timer timer;


        public SparkleSpinner (int size) : base ()
        {
            int current_frame          = 0;
            Gdk.Pixbuf spinner_gallery = SparkleUIHelpers.GetIcon ("process-working", size);
            int frames_in_width        = spinner_gallery.Width / size;
            int frames_in_height       = spinner_gallery.Height / size;
            int frame_count            = (frames_in_width * frames_in_height) - 1;
            Gdk.Pixbuf [] frames       = new Gdk.Pixbuf [frame_count];

            int i = 0;
            for (int y = 0; y < frames_in_height; y++) {
                for (int x = 0; x < frames_in_width; x++) {
                    if (!(y == 0 && x == 0)) {
                        frames [i] = new Gdk.Pixbuf (spinner_gallery, x * size, y * size, size, size);
                        i++;
                    }
                }
            }

            timer = new Timer () {
                Interval = 600 / frame_count
            };

            timer.Elapsed += delegate {
                if (current_frame < frame_count - 1)
                    current_frame++;
                else
                    current_frame = 0;
                
                Application.Invoke (delegate {
                    Pixbuf = frames [current_frame];
                });
            };
        }


        public void Start ()
        {
            timer.Start ();
        }


        public void Stop ()
        {
            timer.Stop ();
        }
    }
}
