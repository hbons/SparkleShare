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
        DescriptionLabel address_example;

        Entry path_entry;
        DescriptionLabel path_example;

        Label public_key_label;

        Button copy_button;
        Button add_button;


        public AddressPage (PageType page_type, PageController controller) : base (page_type, controller)
        {
            Header = "Where’s your project hosted?";
            Description = "";

            Controller.PageCanContinueEvent += AddressPageCanContinueEventHandler;
            Controller.AddressPagePublicKeyEvent += AddressPagePublicKeyEventHandler;
        }


        public override void Dispose ()
        {
            Controller.PageCanContinueEvent -= AddressPageCanContinueEventHandler;
            Controller.AddressPagePublicKeyEvent -= AddressPagePublicKeyEventHandler;
        }


        public override object Render ()
        {
            // Address entry
            address_entry = new Entry () {
                Text = Controller.SelectedPreset.Address,
                ActivatesDefault = true
            };

            address_example = new DescriptionLabel (Controller.SelectedPreset.AddressExample) { Xalign = 1 };
            address_entry.Changed += delegate { Controller.CheckAddressPage (address_entry.Text, path_entry.Text); };

            VBox layout_address = new VBox (false, 6);

            layout_address.PackStart (new Label () {
                    Markup = "<b>Address</b>",
                    Xalign = 0 }, true, true, 0);

            layout_address.PackStart (address_entry, false, false, 0);
            layout_address.PackStart (address_example, false, false, 0);


            path_entry = new Entry () {
                Text = Controller.SelectedPreset.Path,
                ActivatesDefault = true
            };

            path_entry.ClipboardPasted += delegate {
                try {
                  path_entry.Text = new Uri (path_entry.Text).AbsolutePath;
                  path_entry.Position = path_entry.Text.Length;

                } catch {
                }
            };

            path_example = new DescriptionLabel (Controller.SelectedPreset.Path) { Xalign = 1 };
            path_entry.Changed += delegate { Controller.CheckAddressPage (address_entry.Text, path_entry.Text); };

            VBox layout_path = new VBox (false, 6);

            layout_path.PackStart (new Label () {
                Markup = "<b>Remote Path</b>",
                Xalign = 0 }, true, true, 0);

            layout_path.PackStart (path_entry, false, false, 0);
            layout_path.PackStart (path_example, false, false, 0);


            if (string.IsNullOrEmpty (path_entry.Text)) {
                address_entry.GrabFocus ();
                address_entry.Position = -1;

            } else {
                path_entry.GrabFocus ();
                path_entry.Position = -1;
            }


            public_key_label = new Label () {
                Markup = "Authenticating…\n",
                UseMarkup = true,
                Xalign = 0,
                Yalign = 0
            };

            copy_button = new Button ("Copy") {
                Visible = false,
                NoShowAll = true
            };

            copy_button.Clicked += delegate {
                SparkleShare.Controller.CopyToClipboard (SparkleShare.Controller.UserAuthenticationInfo.PublicKey);
            };

            var copy_layout = new HBox (false, 0);
            copy_layout.PackStart (copy_button, false, false, 0);

            VBox layout_fields = new VBox (false, 0) { BorderWidth = 24 };
            layout_fields.PackStart (layout_address, false, false, 12);
            layout_fields.PackStart (layout_path, false, false, 12);

            layout_fields.PackStart (new Label () {
                Markup = "<b>" + "Public Key" + "</b>",
                Xalign = 0,
                Yalign = 0
            }, false, false, 12);

            layout_fields.PackStart (public_key_label, false, false, 0);
            layout_fields.PackStart (copy_layout, false, false, 12);

            // Buttons
            Button cancel_button = new Button ("Cancel");
            add_button = new Button ("Add") { Sensitive = false };

            cancel_button.Clicked += delegate { Controller.CancelClicked (RequestedType); };
            add_button.Clicked += delegate { Controller.AddressPageCompleted ( address_entry.Text, path_entry.Text); };

            Button back_button = new Button ("Back");
            back_button.Clicked += delegate { Controller.BackClicked (RequestedType); };

            Buttons = new Button [] { cancel_button, null, back_button, add_button };

            var padding = new HBox  (false, 0);
            padding.PackStart (layout_fields, true, true, 128);

            // Layout
            return padding;
        }


        void AddressPagePublicKeyEventHandler (bool authenticated, string auth_status, string key_entry_hint)
        {
            Application.Invoke (delegate {
                public_key_label.Markup = auth_status;

                if (!authenticated) {
                    public_key_label.Markup = key_entry_hint + "\n" + "<span fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + auth_status + "</span>";
                    copy_button.Show ();
                }
            });
        }


        void AddressPageCanContinueEventHandler (PageType page_type, bool can_continue)
        {
            if (page_type == RequestedType)
                Application.Invoke (delegate { add_button.Sensitive = can_continue; });
        }
    }
}
