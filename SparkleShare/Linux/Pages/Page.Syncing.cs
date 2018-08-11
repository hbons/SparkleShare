//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;

using Sparkles;
using Gtk;

namespace SparkleShare {

    public class SyncingPage : Page {

        ProgressBar progress_bar;
        Label progress_label;


        public SyncingPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header = String.Format ("Adding project ‘{0}’…", Controller.SyncingFolder);
            Description = "This may take a while for large projects.\nIsn’t it coffee-o’clock?";

            Controller.UpdateProgressBarEvent += UpdateProgressBarEventHandler;
        }


        public override void Dispose ()
        {
            Controller.UpdateProgressBarEvent -= UpdateProgressBarEventHandler;
        }


        public override object Render ()
        {
            // Progress bar
            progress_bar = new ProgressBar ();
            progress_bar.Fraction    = Controller.ProgressBarPercentage / 100;

            progress_label = new Label ("Preparing to fetch files…") {
                Justify = Justification.Right,
                Xalign  = 1
            };


            // Buttons
            Button cancel_button = new Button ("Cancel");
            Button finish_button = new Button ("Finish") { Sensitive = false };

            cancel_button.Clicked += delegate { Controller.SyncingCancelled (); };

            Buttons = new Button [] { cancel_button, finish_button };


            // Layout
            VBox wrapper = new VBox (false, 0);
            wrapper.PackStart (progress_bar, false, false, 21);
            wrapper.PackStart (progress_label, false, true, 0);

            return wrapper;
        }


        void UpdateProgressBarEventHandler (double percentage, string speed)
        {
            Application.Invoke (delegate {
                progress_bar.Fraction = percentage / 100;
                progress_label.Text   = speed;
            });
        }
    }
}
