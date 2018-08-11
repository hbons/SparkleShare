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
using Gtk;

namespace SparkleShare {

    public class FinishedPage : Page {

        public FinishedPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header      = "Your shared project is ready!";
            Description = "You can find the files in your SparkleShare folder.";


            // Buttons
            Button show_files_button = new Button ("Show Files");
            Button finish_button     = new Button ("Finish");

            show_files_button.Clicked += delegate { Controller.ShowFilesClicked (); };
            finish_button.Clicked += delegate { Controller.FinishPageCompleted (); };

            Buttons = new Button [] { show_files_button, finish_button };
        }


        public override object Render ()
        {
            // UrgencyHint = true; TODO
            VBox wrapper = new VBox (false, 0);

/* TODO
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

            return wrapper;
        }
    }
}
