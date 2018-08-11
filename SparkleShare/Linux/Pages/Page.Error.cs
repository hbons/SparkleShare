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

    public class ErrorPage : Page {

        public ErrorPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header = "Oops! Something went wrong" + "…";
            Description = "";


            // Buttons
            Button cancel_button = new Button ("Cancel");
            Button try_again_button = new Button ("Retry") { Sensitive = true };

            cancel_button.Clicked += delegate { Controller.PageCancelled (); };
            try_again_button.Clicked += delegate { Controller.ErrorPageCompleted (); };

            Buttons = new Button [] { cancel_button, try_again_button };
        }


        public override object Render ()
        {
            Image list_point_one   = new Image (UserInterfaceHelpers.GetIcon ("list-point", 16));
            Label label_one = new Label () {
                Markup = "<b>" + Controller.PreviousUrl + "</b> is the address we’ve compiled. " +
                "Does this look alright?",
                Wrap   = true,
                Xalign = 0
            };

            HBox point_one = new HBox (false, 0);
            point_one.PackStart (list_point_one, false, false, 0);
            point_one.PackStart (label_one, true, true, 12);


            Image list_point_two   = new Image (UserInterfaceHelpers.GetIcon ("list-point", 16));
            Label label_two = new Label () {
                Text   = "Is this computer’s Client ID known by the host?",
                Wrap   = true,
                Xalign = 0
            };

            HBox point_two = new HBox (false, 0);
            point_two.PackStart (list_point_two, false, false, 0);
            point_two.PackStart (label_two, true, true, 12);


            // Layout
            VBox wrapper = new VBox (false, 0);
            wrapper.PackStart (new Label ("Please check the following:") { Xalign = 0 }, false, false, 6);
            wrapper.PackStart (point_one, false, false, 12);
            wrapper.PackStart (new Label (""), true, true, 0);
            wrapper.PackStart (point_two, false, false, 12);
            wrapper.PackStart (new Label (""), true, true, 0);

/* TODO
            if (warnings.Length > 0) {
                string warnings_markup = "";

                foreach (string warning in warnings)
                    warnings_markup += "\n<b>" + warning + "</b>";

                Label label_three = new Label () {
                    Markup = "Here’s the raw error message:" + warnings_markup,
                    Wrap   = true,
                    Xalign = 0
                };

                Image list_point_three = new Image (UserInterfaceHelpers.GetIcon ("list-point", 16));
                HBox point_three = new HBox (false, 0);
                point_three.PackStart (list_point_three, false, false, 0);
                point_three.PackStart (label_three, true, true, 12);
                wrapper.PackStart (point_three, false, false, 12);
            }
*/
            return wrapper;
        }
    }
}
