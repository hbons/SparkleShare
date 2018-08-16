//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using Gtk;

namespace SparkleShare {

    public class SetupWindow : Window {

        public const int Spacing = 18;

        public string Header;
        public string Description;

        EventBox content_area;
        HBox buttons;



        public SetupWindow () : base ("SparkleShare")
        {
            IconName       = "org.sparkleshare.SparkleShare";
            Resizable      = false;
            WindowPosition = WindowPosition.Center;
            Deletable      = false;
            TypeHint       = Gdk.WindowTypeHint.Dialog;

            SetSizeRequest (720, 540);
            DeleteEvent += delegate (object sender, DeleteEventArgs args) { args.RetVal = true; };

            VBox layout_vertical = new VBox (false, 0);
            layout_vertical.BorderWidth = Spacing;
            layout_vertical.Spacing = Spacing;

            this.content_area = new EventBox ();

            this.buttons = new HBox () {
                BorderWidth = 0,
                Homogeneous = false,
                Spacing     = 6
            };

            layout_vertical.PackStart (this.content_area, true, true, 0);
            layout_vertical.PackStart (this.buttons, false, false, 0);

            base.Add (layout_vertical);
        }


        public void Reset ()
        {
            Header      = "";
            Description = "";

            if (this.content_area.Children.Length > 0)
                this.content_area.Remove (this.content_area.Children [0]);

            foreach (Widget button in this.buttons)
                this.buttons.Remove (button);
        }


        public void AddButtons (object [] buttons)
        {
            if (!Array.Exists (buttons, button => button == null))
                this.buttons.PackStart (new Label (""), true, true, 0);

            foreach (Button button in buttons) {
                if (button == null) {
                    this.buttons.PackStart (new Label (""), true, true, 0);

                } else {
                    button.WidthRequest = 100;
                    this.buttons.PackStart (button, false, false, 0);
                }
            }

            var default_button = (Button) buttons [buttons.Length - 1];
            default_button.CanDefault = true;

            default_button.StyleContext.AddClass ("suggested-action");
            Default = default_button;
        }


        public void Add (object widget)
        {
            if (widget != null)
                this.content_area.Add ((Widget) widget);
        }


        new public void ShowAll ()
        {
            Title = Header;
            base.ShowAll ();
        }
    }
}
