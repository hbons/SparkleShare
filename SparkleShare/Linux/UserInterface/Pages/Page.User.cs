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
using Mono.Unix;

namespace SparkleShare {

    public class UserPage : Page {

        Entry name_entry;
        Entry email_entry;

        Button continue_button;


        public UserPage (PageType page_type, PageController controller) : base (page_type, controller)
        {
            Header = "Welcome to SparkleShare";
            Description = "Hello. Let's get you set up.";

            Controller.PageCanContinueEvent += UserPageCanContinueEventHandler;
        }


        public override void Dispose ()
        {
            Controller.PageCanContinueEvent -= UserPageCanContinueEventHandler;
        }


        public override object Render ()
        {
            // Name
            Label name_label = new Label () {
                Markup = "<b>Name</b>",
                Xalign = 0
            };

            name_entry = new Entry ();
            name_entry.Changed += delegate { Controller.CheckUserPage (name_entry.Text, email_entry.Text); };

// TODO: fill name and email when Back was pressed on privacy page

/* TODO broken
            try {
                UnixUserInfo user_info = UnixUserInfo.GetRealUser ();

                if (user_info != null && user_info.RealName != null) {
                    // Some systems append a series of "," for some reason
                    string name = "" + user_info.RealName;
                    name_entry.Text = name;
                }
            } catch (Exception) {
                // No username, not a big deal
            }
*/

            // Email
            Label email_label = new Label () {
                Markup = "<b>Email</b>",
                Xalign = 0
            };

            email_entry = new Entry () { ActivatesDefault = true };
            email_entry.Changed += delegate { Controller.CheckUserPage (name_entry.Text, email_entry.Text); };


            var data_hint = new DescriptionLabel (
                "The following information is used to mark your changes in a project's history. " +
                "It won't be sent anywhere, but will be visible to those working on the same project.") {

               LineWrap = true,
                LineWrapMode = Pango.WrapMode.Word,
                MaxWidthChars = 48

                };


            // Buttons
            Button quit_button = new Button ("Quit");
            quit_button.Clicked += delegate { Controller.QuitClicked (); };

            continue_button = new Button ("Continue") { Sensitive = false };
            continue_button.Clicked += delegate { Controller.UserPageCompleted (name_entry.Text, email_entry.Text); };

            Buttons = new Button [] { quit_button, continue_button };


            if (name_entry.Text.Equals (""))
                name_entry.GrabFocus ();
            else
                email_entry.GrabFocus ();

             Controller.CheckUserPage (name_entry.Text, email_entry.Text);


            // Layout
            VBox layout = new VBox (false, 9) { BorderWidth = 64 };
            layout.PackStart (new Label (Description) { Xalign = 0 }, false, false, 0);
            layout.PackStart (data_hint, false, false, 0);

            VBox layout_fields = new VBox (false, 0) { BorderWidth = 32 };

            VBox layout_name = new VBox (false, 6);
            layout_name.PackStart (name_label, false, false, 0);
            layout_name.PackStart (name_entry, false, false, 0);

            VBox layout_email = new VBox (false, 6);
            layout_email.PackStart (email_label, false, false, 0);
            layout_email.PackStart (email_entry, false, false, 0);

            layout_fields.PackStart (layout_name, false, false, 12);
            layout_fields.PackStart (layout_email, false, false, 12);

            layout.PackStart (layout_fields, false, false, 0);

            return layout;
        }


        void UserPageCanContinueEventHandler (PageType page_type, bool can_continue)
        {
            if (page_type == RequestedType)
                Application.Invoke (delegate { continue_button.Sensitive = can_continue; });
        }
    }
}
