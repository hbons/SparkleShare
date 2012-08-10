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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

using Gtk;
using Mono.Unix;

namespace SparkleShare {

    public class SparkleSetupWindow : Window    {

        // TODO: caps
        private HBox HBox;
        private VBox VBox;
        private VBox Wrapper;
        private VBox OptionArea;
        private HBox Buttons;

        public string Header;
        public string Description;
        public string SecondaryTextColor;
        public string SecondaryTextColorSelected;

        public Container Content;

        public SparkleSetupWindow () : base ("")
        {
            Title          = Catalog.GetString ("SparkleShare Setup");
            BorderWidth    = 0;
            IconName       = "folder-sparkleshare";
            Resizable      = false;
            WindowPosition = WindowPosition.Center;
            Deletable      = false;

            DeleteEvent += delegate (object sender, DeleteEventArgs args) {
                args.RetVal = true;
            };

            SecondaryTextColor = SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive));
                        
            SecondaryTextColorSelected =
                SparkleUIHelpers.GdkColorToHex (
                    MixColors (
                        new TreeView ().Style.Foreground (StateType.Selected),
                        new TreeView ().Style.Background (StateType.Selected),
                        0.15
                    )
                );

            SetSizeRequest (680, 400);

            HBox = new HBox (false, 0);

                VBox = new VBox (false, 0);

                    Wrapper = new VBox (false, 0) {
                        BorderWidth = 0
                    };

                    OptionArea = new VBox (false, 0) {
                        BorderWidth = 0
                    };

                    Buttons = CreateButtonBox ();


                HBox layout_horizontal = new HBox (false , 0) {
                    BorderWidth = 0
                };

                layout_horizontal.PackStart (OptionArea, true, true, 0);
                layout_horizontal.PackStart (Buttons, false, false, 0);

                VBox.PackStart (Wrapper, true, true, 0);
                VBox.PackStart (layout_horizontal, false, false, 15);

                EventBox box = new EventBox ();
                Gdk.Color bg_color = new Gdk.Color ();
                Gdk.Color.Parse ("#000", ref bg_color);
                box.ModifyBg (StateType.Normal, bg_color);

                Image side_splash = SparkleUIHelpers.GetImage ("side-splash.png");
                side_splash.Yalign = 1;

            box.Add (side_splash);

            HBox.PackStart (box, false, false, 0);
            HBox.PackStart (VBox, true, true, 30);

            base.Add (HBox);
        }


        private HBox CreateButtonBox ()
        {
            return new HBox () {
                BorderWidth = 0,
                //Layout      = ButtonBoxStyle.End,
                Homogeneous = false,
                Spacing     = 6
            };
        }


        public void AddButton (Button button)
        {
            (button.Child as Label).Xpad = 15;
            Buttons.Add (button);
        }


        public void AddOption (Widget widget)
        {            
            OptionArea.Add (widget);
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

            Wrapper.PackStart (layout_vertical, true, true, 0);
            ShowAll ();
        }

    
        public void Reset ()
        {
            Header      = "";
            Description = "";

            if (OptionArea.Children.Length > 0)
                OptionArea.Remove (OptionArea.Children [0]);

            if (Wrapper.Children.Length > 0)
                Wrapper.Remove (Wrapper.Children [0]);

            foreach (Button button in Buttons)
                Buttons.Remove (button);

            ShowAll ();
        }
        
        
        new public void ShowAll ()
        {
            if (Buttons.Children.Length > 0) {
                Button default_button = (Button) Buttons.Children [Buttons.Children.Length - 1];
            
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
                    second_color.Red   * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Green * (1.0 - ratio) +
                    second_color.Green * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Blue * (1.0 - ratio) +
                    second_color.Blue  * ratio))) / 65535)
            );
        }
    }
}
