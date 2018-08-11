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

    public class CryptoSetupPage : Page {

        Button continue_button;


        public CryptoSetupPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            if (RequestedType == PageType.CryptoSetup) {
                Header = string.Format ("Encryption password for ‘{0}’", Controller.SyncingFolder);
                Description = "Please a provide a strong password that you don’t use elsewhere.";

                Controller.UpdateCryptoSetupContinueButtonEvent += UpdateCryptoSetupContinueButtonEventHandler;

            } else {
                Header = string.Format ("‘{0}’ contains encrypted files", Controller.SyncingFolder);
                Description = "Please enter the password to see their contents.";

                Controller.UpdateCryptoPasswordContinueButtonEvent += UpdateCryptoPasswordContinueButtonEventHandler;
            }
        }


        public override object Render ()
        {
            // Password entry
            Label password_label = new Label ("<b>" + "Password" + "</b>") {
                UseMarkup = true,
                Xalign    = 1
            };

            Entry password_entry = new Entry () {
                Xalign = 0,
                Visibility = false,
                ActivatesDefault = true
            };

            password_entry.Changed += delegate {
                if (RequestedType == PageType.CryptoSetup)
                    Controller.CheckCryptoSetupPage (password_entry.Text);
                else
                    Controller.CheckCryptoPasswordPage (password_entry.Text);
            };

            password_entry.GrabFocus ();


            // Checkbox
            CheckButton show_password_check_button = new CheckButton ("Make visible") {
                Active = false,
                Xalign = 0,
            };

            show_password_check_button.Toggled += delegate {
                password_entry.Visibility = !password_entry.Visibility;
            };


            // Buttons
            Button cancel_button = new Button ("Cancel");
            cancel_button.Clicked += delegate { Controller.CryptoPageCancelled (); };

            continue_button = new Button ("Continue") { Sensitive = false };
            continue_button.Clicked +=  delegate {
                if (RequestedType == PageType.CryptoSetup)
                    Controller.CryptoSetupPageCompleted (password_entry.Text);
                else
                    Controller.CryptoPasswordPageCompleted (password_entry.Text);
            };

            Buttons = new Button [] { cancel_button, continue_button };


            // Layout
            Table table = new Table (2, 3, true) {
                RowSpacing    = 6,
                ColumnSpacing = 6
            };

            table.Attach (password_label, 0, 1, 0, 1);
            table.Attach (password_entry, 1, 2, 0, 1);

            table.Attach (show_password_check_button, 1, 2, 1, 2);

            VBox wrapper = new VBox (false, 9);
            wrapper.PackStart (table, true, false, 0);

            if (RequestedType == PageType.CryptoSetup)
                wrapper.PackStart (RenderWarning (), false, false, 0);

            return wrapper;
        }



        VBox RenderWarning ()
        {
            var image = new Image (
                UserInterfaceHelpers.GetIcon ("dialog-information", 24));

            Label label = new Label () {
                Xalign = 0,
                Wrap = true,
                Text = "This password can’t be changed later, and your files can’t be recovered if it’s forgotten."
            };


            // Layout
            HBox layout = new HBox (false, 0);
            layout.PackStart (image, false, false, 15);
            layout.PackStart (label, true, true, 0);

            VBox wrapper = new VBox (false, 0);
            wrapper.PackStart (layout, false, false, 15);

            return wrapper;
        }

        void UpdateCryptoSetupContinueButtonEventHandler (bool button_enabled) {
            Application.Invoke (delegate { continue_button.Sensitive = button_enabled; });
        }


        void UpdateCryptoPasswordContinueButtonEventHandler (bool button_enabled) {
            Application.Invoke (delegate { continue_button.Sensitive = button_enabled; });
        }


        public override void Dispose ()
        {
            Controller.UpdateCryptoSetupContinueButtonEvent -= UpdateCryptoSetupContinueButtonEventHandler;
            Controller.UpdateCryptoPasswordContinueButtonEvent -= UpdateCryptoPasswordContinueButtonEventHandler;
        }
    }


    public class CryptoPasswordPage : CryptoSetupPage {

        public CryptoPasswordPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
        }
    }
}
