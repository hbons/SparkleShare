//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
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

    public class SetupWindow : Window    {

        private EventBox content_area;
        private EventBox option_area;
        private HBox buttons;

        public string Header;
        public string Description;


        public SetupWindow () : base ("SparkleShare Setup")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

			IconName       = "org.sparkleshare.SparkleShare";
            Resizable      = false;
            WindowPosition = WindowPosition.CenterAlways;
            Deletable      = false;
            TypeHint       = Gdk.WindowTypeHint.Dialog;


            SetSizeRequest (400, 400);

            DeleteEvent += delegate (object sender, DeleteEventArgs args) { args.RetVal = true; };


                VBox layout_vertical = new VBox (false, 16);
            layout_vertical.BorderWidth = 16;

                    this.content_area    = new EventBox ();
                    this.option_area = new EventBox ();

                    this.buttons = CreateButtonBox ();

                HBox layout_actions = new HBox (false , 16);

                layout_actions.PackStart (this.option_area, true, true, 0);
                layout_actions.PackStart (this.buttons, false, false, 0);

                layout_vertical.PackStart (this.content_area, true, true, 0);
                layout_vertical.PackStart (layout_actions, false, false, 0);


            base.Add (layout_vertical);
        }


        private HBox CreateButtonBox ()
        {
            return new HBox () {
                BorderWidth = 0,
                Homogeneous = false,
                Spacing     = 6
            };
        }


        public void AddButton (Button button)
        {
            (button.Child as Label).Xpad = 15;
            this.buttons.Add (button);
        }


        public void AddOption (Widget widget)
        {            
            this.option_area.Add (widget);
        }


        new public void Add (Widget widget)
        {
            Title = Header;

            VBox layout_vertical = new VBox (false, 0);

            if (!string.IsNullOrEmpty (Description)) {
                Label description = new Label (Description) {
                    Xalign = 0,
                    LineWrap = true,
                    LineWrapMode = Pango.WrapMode.WordChar
                };
                
                layout_vertical.PackStart (description, false, false, 0);
            }

            if (widget != null)
                layout_vertical.PackStart (widget, true, true, 0);

            this.content_area.Add (layout_vertical);
        }

    
        public void Reset ()
        {
            Header      = "";
            Description = "";

            if (this.option_area.Children.Length > 0)
                this.option_area.Remove (this.option_area.Children [0]);

            if (this.content_area.Children.Length > 0)
                this.content_area.Remove (this.content_area.Children [0]);

            foreach (Button button in this.buttons)
                this.buttons.Remove (button);
        }
        
        
        new public void ShowAll ()
        {
            if (this.buttons.Children.Length > 0) {
                Button default_button = (Button) this.buttons.Children [this.buttons.Children.Length - 1];
            
                default_button.CanDefault = true;
                Default = default_button;
                default_button.StyleContext.AddClass ("suggested-action");
            }

            Present ();
            base.ShowAll ();
        }
    }
}

