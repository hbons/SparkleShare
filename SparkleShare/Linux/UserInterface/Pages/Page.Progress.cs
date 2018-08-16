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

using Sparkles; // TODO: remove
using Gtk;

namespace SparkleShare {

    public class ProgressPage : Page {

        Image image;

        Label status_label;
        Label status_details_label;

        ProgressBar progress_bar;
        Label progress_label;


        public ProgressPage (PageType page_type, PageController controller) : base (page_type, controller)
        {
            Header = String.Format ("Getting files from {0}", Controller.SelectedPreset.Name);
            Description = "Depending on its size, it may be a good time to make some tea.";

            Controller.ProgressPageBarEvent += ProgressPageBarEventHandler;
        }


        public override void Dispose ()
        {
            Controller.ProgressPageBarEvent -= ProgressPageBarEventHandler;
            // TODO: State handling
        }


        public override object Render ()
        {
            // image = new Image ();

            // Progress bar
            progress_bar = new ProgressBar ();
            progress_bar.Fraction = Controller.ProgressBarPercentage / 100;

            progress_label = new Label () {
                Markup = string.Format ("Getting files from <b>{0}</b>", Controller.FetchAddress.Authority),
                Xalign  = 0
            };


            Buttons = ProgressButtons ();


// UrgencyHint = true; TODO
            //VBox wrapper = new VBox (false, 0);

/* TODO


            // Buttons
            Button show_files_button = new Button ("Show Files");
            Button finish_button     = new Button ("Finish");

            show_files_button.Clicked += delegate { Controller.ShowFilesClicked (); };
            finish_button.Clicked += delegate { Controller.FinishPageCompleted (); };

            Buttons = new Button [] { show_files_button, finish_button };

            if (warnings.Length > 0) {
                Image warning_image = new Image (UserInterfaceHelpers.GetIcon ("dialog-information", 24));

                Label warning_label = new Label (warnings [0]) {
                    Xalign = 0,
                    Wrap   = true
                };

                HBox warning_layout = new HBox (false, 0);
                warning_layout.PackStart (warning_image, false, false, 15);
                warning_layout.PackStart (warning_label, true, true, 0);

                VBox wrapper = new VBox (false, 0);
                wrapper.PackStart (warning_layout, false, false, 0);

                Add (wrapper);

            } else {
                Add (null);
            }
*/
            image = new Image (Controller.SelectedPreset.LogoPath);

            image = new Image ("dialog-error-symbolic", (IconSize) 128);

            status_label = new Label () { Markup = "<b>" + Header + "</b>" };
            status_details_label = new Label ("error goes here");

            var status_layout = new VBox (false, 0);
            status_layout.PackStart (image, false, false, 12);
            status_layout.PackStart (status_label, false, false, 6);
            status_layout.PackStart (status_details_label, false, false, 0);

            // Layout
            VBox layout = new VBox (false, 0) { BorderWidth = 48 };
            layout.PackStart (status_layout, true, true, 0);
            layout.PackStart (progress_bar, false, false, 24);
            layout.PackStart (progress_label, false, false, 0);

            return layout;
        }


        void ProgressPageBarEventHandler (bool? success,
            string status, string status_details,
            double progress, string progress_details)
        {
            Application.Invoke (delegate {
                status_label.Markup = "<b>" + status + "</b>";
                status_details_label.Text = status_details;

                progress_bar.Sensitive = success.GetValueOrDefault ();
                progress_bar.Fraction = progress / 100;
                progress_label.Text = progress_details;
            });
        }


        Button [] ProgressButtons ()
        {
            var cancel_button = new Button ("Cancel");
            cancel_button.Clicked += delegate { Controller.CancelClicked (RequestedType); };

            var back_button = new Button ("Back") { Sensitive = false };
            var done_button = new Button ("Done") { Sensitive = false };

            return new Button [] { cancel_button, null, back_button, done_button };
        }


        Button [] ErrorButtons ()
        {
            var cancel_button = new Button ("Cancel");
            cancel_button.Clicked += delegate { Controller.CancelClicked (RequestedType); };

            var back_button = new Button ("Back");
            back_button.Clicked += delegate { Controller.BackClicked (RequestedType); };

            var retry_button = new Button ("Retry");
            retry_button.Clicked += delegate {
            //     TODO
            };

            return new Button [] { cancel_button, null, back_button, retry_button };
        }


        Button [] SuccessButtons ()
        {
            var open_files_button = new Button ("Open Files");
            open_files_button.Clicked += delegate {
                Controller.ShowFilesClicked (); // TODO
            };

            var done_button = new Button ("Done");
            done_button.Clicked += delegate {
                 Controller.ProgressPageCompleted ();
            };

            return new Button [] { open_files_button, done_button };
        }
    }
}
