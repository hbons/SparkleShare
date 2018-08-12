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

    public class AddressPage : Page {

        Entry address_entry;
        Label address_example;

        Entry path_entry;
        Label path_example;

        Button add_button;


        public AddressPage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header = "Where’s your project hosted?";
            Description = "";

            Controller.ChangeAddressFieldEvent += ChangeAddressFieldEventHandler;
            Controller.ChangePathFieldEvent += ChangePathFieldEventHandler;
            Controller.UpdateAddProjectButtonEvent += UpdateAddProjectButtonEventHandler;
        }


        public override void Dispose ()
        {
            Controller.ChangeAddressFieldEvent -= ChangeAddressFieldEventHandler;
            Controller.ChangePathFieldEvent -= ChangePathFieldEventHandler;
            Controller.UpdateAddProjectButtonEvent -= UpdateAddProjectButtonEventHandler;
        }


        public override object Render ()
        {
            // Address entry
            address_entry = new Entry () {
                Text = Controller.SelectedPreset.Address,
                Sensitive = (Controller.SelectedPreset.Address == null),
                ActivatesDefault = true
            };

            address_example = new Label () {
                Xalign = 0,
                UseMarkup = true,
                Markup = "<span size=\"small\" fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + Controller.SelectedPreset.AddressExample + "</span>"
            };
/* TODO
            address_entry.Changed += delegate {
                Controller.CheckAddressPage (address_entry.Text, path_entry.Text, tree_view.SelectedRow);
            };
*/
            VBox layout_address = new VBox (true, 0);

            layout_address.PackStart (new Label () {
                    Markup = "<b>" + "Address" + "</b>",
                    Xalign = 0
                }, true, true, 0);

            layout_address.PackStart (address_entry, false, false, 0);
            layout_address.PackStart (address_example, false, false, 0);



            path_entry = new Entry () {
                Text = Controller.SelectedPreset.Path,
                Sensitive = (Controller.SelectedPreset.Path == null),
                ActivatesDefault = true
            };

            path_example = new Label (Controller.SelectedPreset.Path) {
                Xalign = 0,
                UseMarkup = true,
                Markup = "<span size=\"small\" fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + Controller.SelectedPreset.PathExample + "</span>"
            };
/* TODO
            path_entry.Changed += delegate {
                Controller.CheckAddressPage (address_entry.Text, path_entry.Text, tree_view.SelectedRow);
            };
*/
            VBox layout_path = new VBox (true, 0);

            layout_path.PackStart (new Label () {
                Markup = "<b>" + "Remote Path" + "</b>",
                Xalign = 0
            }, true, true, 0);

            layout_path.PackStart (path_entry, false, false, 0);
            layout_path.PackStart (path_example, false, false, 0);


            if (string.IsNullOrEmpty (path_entry.Text)) {
                address_entry.GrabFocus ();
                address_entry.Position = -1;

            } else {
                path_entry.GrabFocus ();
                path_entry.Position = -1;
            }

            // Extra option area
            CheckButton check_button = new CheckButton ("Fetch prior revisions") { Active = false };
            check_button.Toggled += delegate { Controller.HistoryItemChanged (check_button.Active); };

            Label public_key_label = new Label () {
            Markup = "<b>Public Key</b>\n" +
                SparkleShare.Controller.UserAuthenticationInfo.PublicKey.Substring (0, 48) + "...",
                UseMarkup = true,
                Xalign = 0
                 };

            VBox layout_fields  = new VBox (false, 32);
            layout_fields.PackStart (layout_address, false, false, 0);
            layout_fields.PackStart (layout_path, false, false, 0);
            layout_fields.PackStart (check_button, false, false, 0);
            layout_fields.PackStart (public_key_label, false, false, 0);


            // Buttons
            Button cancel_button = new Button ("Cancel");
            add_button = new Button ("Add") { Sensitive = false };

            cancel_button.Clicked += delegate { Controller.PageCancelled (); };
            add_button.Clicked += delegate { Controller.AddressPageCompleted ( address_entry.Text, path_entry.Text); };

            Buttons = new Button [] { cancel_button, add_button };


            // Layout
            VBox layout_vertical = new VBox (false, 16);
            layout_vertical.PackStart (layout_fields, false, false, 0);

            return layout_vertical;
        }


        void ChangeAddressFieldEventHandler (string text, string example_text, FieldState state)
        {
            Application.Invoke (delegate {
                address_entry.Text = text;
                address_entry.Sensitive = (state == FieldState.Enabled);

                address_example.Markup =  "<span size=\"small\" fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + example_text + "</span>";
            });
        }


        void ChangePathFieldEventHandler (string text, string example_text, FieldState state)
        {
            Application.Invoke (delegate {
                path_entry.Text = text;
                path_entry.Sensitive = (state == FieldState.Enabled);

                path_example.Markup = "<span size=\"small\" fgcolor=\""
                    + SparkleShare.UI.SecondaryTextColor + "\">" + example_text + "</span>";
            });
        }


        void UpdateAddProjectButtonEventHandler (bool button_enabled)
        {
            Application.Invoke (delegate { add_button.Sensitive = button_enabled; });
        }
    }
}
