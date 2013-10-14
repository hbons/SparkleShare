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

    public class SparkleSetupWindow : Window    {

        private EventBox content_area;
        private EventBox option_area;
        private HBox buttons;

        public string Header;
        public string Description;

        public readonly string SecondaryTextColor;
        public readonly string SecondaryTextColorSelected;


        public SparkleSetupWindow () : base ("SparkleShare Setup")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            IconName       = "sparkleshare";
            Resizable      = false;
            WindowPosition = WindowPosition.Center;
            Deletable      = false;
            TypeHint       = Gdk.WindowTypeHint.Dialog;


            SetSizeRequest (680, 400);

            DeleteEvent += delegate (object sender, DeleteEventArgs args) { args.RetVal = true; };

            Gdk.Color color = SparkleUIHelpers.RGBAToColor (StyleContext.GetColor (StateFlags.Insensitive));
            SecondaryTextColor = SparkleUIHelpers.ColorToHex (color);
                    
            color = MixColors (
                SparkleUIHelpers.RGBAToColor (new TreeView ().StyleContext.GetColor (StateFlags.Selected)),
                SparkleUIHelpers.RGBAToColor (new TreeView ().StyleContext.GetBackgroundColor (StateFlags.Selected)),
                0.39);
    
            SecondaryTextColorSelected = SparkleUIHelpers.ColorToHex (color);

            HBox layout_horizontal = new HBox (false, 0);

                VBox layout_vertical = new VBox (false, 0);

                    this.content_area    = new EventBox ();
                    this.option_area = new EventBox ();

                    this.buttons = CreateButtonBox ();

                HBox layout_actions = new HBox (false , 48);

                layout_actions.PackStart (this.option_area, true, true, 0);
                layout_actions.PackStart (this.buttons, false, false, 0);

                layout_vertical.PackStart (this.content_area, true, true, 0);
                layout_vertical.PackStart (layout_actions, false, false, 15);

                Image side_splash = SparkleUIHelpers.GetImage ("side-splash.png");
                side_splash.Yalign = 1;

            layout_horizontal.PackStart (side_splash, false, false, 0);
            layout_horizontal.PackStart (layout_vertical, true, true, 30);

            base.Add (layout_horizontal);
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
            Label header = new Label ("<span size='large'><b>" + Header + "</b></span>") {
                UseMarkup = true,
                Xalign = 0,
            };

            VBox layout_vertical = new VBox (false, 0);
            layout_vertical.PackStart (new Label (""), false, false, 6);
            layout_vertical.PackStart (header, false, false, 0);

            if (!string.IsNullOrEmpty (Description)) {
                Label description = new Label (Description) {
                    Xalign = 0,
                    LineWrap = true,
                    LineWrapMode = Pango.WrapMode.WordChar
                };
                
                layout_vertical.PackStart (description, false, false, 21);
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
            }
        
            Present ();
            base.ShowAll ();
        }
        
        
        private Gdk.Color MixColors (Gdk.Color first_color, Gdk.Color second_color, double ratio)
        {
            return new Gdk.Color (
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Red * (1.0 - ratio) +
                    second_color.Red * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Green * (1.0 - ratio) +
                    second_color.Green * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Blue * (1.0 - ratio) +
                    second_color.Blue * ratio))) / 65535)
            );
        }
    }
}
