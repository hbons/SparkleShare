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

    public class SetupPage : Page {

        Entry name_entry;
        Entry email_entry;

        Button continue_button;


        public SetupPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header      = "Welcome to SparkleShare!";
            Description = "First off, what’s your name and email?\n(visible only to team members)";

            Controller.UpdateSetupContinueButtonEvent += UpdateSetupContinueButtonEventHandler;
        }


        public override void Dispose ()
        {
            Controller.UpdateSetupContinueButtonEvent -= UpdateSetupContinueButtonEventHandler;
        }


        public override object Render ()
        {
            // Name
            Label name_label = new Label ("<b>" + "Your Name:" + "</b>") {
                UseMarkup = true,
                Xalign    = 1
            };

            name_entry = new Entry () {
                Xalign = 0,
                ActivatesDefault = true
            };

            try {
                UnixUserInfo user_info = UnixUserInfo.GetRealUser ();

                if (user_info != null && user_info.RealName != null)
                    // Some systems append a series of "," for some reason
                    name_entry.Text = user_info.RealName.TrimEnd (",".ToCharArray ());

            } catch (ArgumentException) {
                // No username, not a big deal
            }

            name_entry.Changed += delegate { Controller.CheckSetupPage (name_entry.Text, email_entry.Text); };


            // Email
            Label email_label = new Label ("<b>" + "Email:" + "</b>") {
                UseMarkup = true,
                Xalign    = 1
            };

            email_entry = new Entry () {
                Xalign = 0,
                ActivatesDefault = true
            };

            email_entry.Changed += delegate { Controller.CheckSetupPage (name_entry.Text, email_entry.Text); };


            Controller.CheckSetupPage (name_entry.Text, email_entry.Text);

            if (name_entry.Text.Equals (""))
                name_entry.GrabFocus ();
            else
                email_entry.GrabFocus ();


            // Buttons
            Button cancel_button = new Button ("Cancel");
            cancel_button.Clicked += delegate { Controller.SetupPageCancelled (); };

            continue_button = new Button ("Continue") { Sensitive = false };
            continue_button.Clicked += delegate {
                Controller.SetupPageCompleted (name_entry.Text, email_entry.Text);
            };

            Buttons = new Button [] {cancel_button, continue_button};


            // Layout
            Table table = new Table (2, 3, true) {
                RowSpacing    = 6,
                ColumnSpacing = 6
            };

            table.Attach (name_label, 0, 1, 0, 1);
            table.Attach (name_entry, 1, 2, 0, 1);
            table.Attach (email_label, 0, 1, 1, 2);
            table.Attach (email_entry, 1, 2, 1, 2);

            VBox wrapper = new VBox (false, 9);
            wrapper.PackStart (table, true, false, 0);

            return wrapper;
        }


        void UpdateSetupContinueButtonEventHandler (bool button_enabled)
        {
            Application.Invoke (delegate { continue_button.Sensitive = button_enabled; });
        }
    }
}
