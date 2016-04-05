//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using Gtk;

namespace SparkleShare {

    public class Note : Window {

        public NoteController Controller = new NoteController ();


        public Note () : base ("Add Note")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            IconName       = "org.sparkleshare.SparkleShare";
            Resizable      = false;
            WindowPosition = WindowPosition.CenterAlways;
            BorderWidth    = 16;

            SetSizeRequest (480, 120);


            DeleteEvent += delegate (object o, DeleteEventArgs args) {
                Controller.WindowClosed ();
                args.RetVal = true;
            };
            
            KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
                if (args.Event.Key == Gdk.Key.Escape ||
                    (args.Event.State == Gdk.ModifierType.ControlMask && args.Event.Key == Gdk.Key.w)) {
                    
                    Controller.WindowClosed ();
                }
            };

            Controller.HideWindowEvent += delegate {
                Application.Invoke (delegate { Hide (); });
            };

            Controller.ShowWindowEvent += delegate {
                Application.Invoke (delegate {
                    CreateNote ();
                    ShowAll ();
                    Present ();
                });
            };

            Controller.UpdateTitleEvent += delegate (string title) {
                Application.Invoke (delegate { Title = title; });
            };

            CreateNote ();
        }


        private void CreateNote ()
        {
            var user_image = new Image (Controller.AvatarFilePath);

            /* TODO: Style the entry neatly, multiple lines, and add placeholder text
            string balloon_image_path = new string [] { UserInterface.AssetsPath, "pixmaps", "text-balloon.png" }.Combine ();
            Image balloon_image = new Image (balloon_image_path);
            CssProvider balloon_css_provider = new CssProvider ();

            balloon_css_provider.LoadFromData ("GtkEntry {" +
                "background-image: url('" + balloon_image_path + "');" +
                "background-repeat: no-repeat;" +
                "background-position: left top;" +
                "}");

            balloon.StyleContext.AddProvider (balloon_css_provider, 800);
            */

            var balloon_label = new Label ("<b>Anything to add?</b>") {
                Xalign = 0,
                UseMarkup = true
            };

            var balloon = new Entry { MaxLength = 144 };


            var cancel_button = new Button ("Cancel");
            var sync_button   = new Button ("Sync"); // TODO: Make default button
			sync_button.CanDefault = true;

            cancel_button.Clicked += delegate { Controller.CancelClicked (); };
            sync_button.Clicked   += delegate { Controller.SyncClicked (balloon.Buffer.Text); };


            var layout_vertical   = new VBox (false, 16);
            var layout_horizontal = new HBox (false, 16);

            var buttons = new HBox {
                Homogeneous = false,
                Spacing     = 6
            };

            var user_label = new Label {
                Markup = "<b>" + SparkleShare.Controller.CurrentUser.Name + "</b>\n" +
                         "<span fgcolor=\"" + SparkleShare.UI.SecondaryTextColor + "\">" + SparkleShare.Controller.CurrentUser.Email +
                         "</span>"
            };


            layout_horizontal.PackStart (user_image, false, false, 0);
            layout_horizontal.PackStart (user_label, false, false, 0);

            buttons.PackStart (new Label (""), true, true, 0);
            buttons.PackStart (cancel_button, false, false, 0);
            buttons.PackStart (sync_button, false, false, 0);

            layout_vertical.PackStart (layout_horizontal, false, false, 0);
            layout_vertical.PackStart (balloon_label, false, false, 0);
            layout_vertical.PackStart (balloon, false, false, 0);
            layout_vertical.PackStart (buttons, false, false, 0);

            Default = sync_button;

            Add (layout_vertical);
        }
    }
}

