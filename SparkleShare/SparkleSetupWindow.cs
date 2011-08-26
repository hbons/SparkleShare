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
using SparkleLib;

namespace SparkleShare {

    public class SparkleSetupWindow : Window    {

        private HBox HBox;
        private VBox VBox;
        private VBox Wrapper;
        private HButtonBox Buttons;

        public string Header;
        public string Description;

        public Container Content;

        public SparkleSetupWindow () : base ("")
        {
            Title          = Catalog.GetString ("SparkleShare Setup");
            BorderWidth    = 0;
            IconName       = "folder-sparkleshare";
            Resizable      = false;
            WindowPosition = WindowPosition.Center;
            Deletable      = false;

            SetSizeRequest (680, 440);

            DeleteEvent += delegate (object o, DeleteEventArgs args) {
                args.RetVal = true;
                Close ();
            };

            HBox = new HBox (false, 6);

                VBox = new VBox (false, 0);

                    Wrapper = new VBox (false, 0) {
                        BorderWidth = 30
                    };

                    Buttons = CreateButtonBox ();

                VBox.PackStart (Wrapper, true, true, 0);
                VBox.PackStart (Buttons, false, false, 0);

                EventBox box = new EventBox ();
                Gdk.Color bg_color = new Gdk.Color ();
                Gdk.Color.Parse ("#000", ref bg_color);
                box.ModifyBg (StateType.Normal, bg_color);

                    string image_path = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare",
                        "pixmaps", "side-splash.png");

                    Image side_splash = new Image (image_path) {
                        Yalign = 1
                    };

                box.Add (side_splash);

            HBox.PackStart (box, false, false, 0);
            HBox.PackStart (VBox, true, true, 0);

            base.Add (HBox);
        }


        private HButtonBox CreateButtonBox ()
        {
            return new HButtonBox () {
                BorderWidth = 12,
                Layout      = ButtonBoxStyle.End,
                Spacing     = 6
            };
        }


        public void AddButton (Button button)
        {
            Buttons.Add (button);
            ShowAll ();
        }


        new public void Add (Widget widget)
        {
            Label header = new Label ("<span size='large'><b>" + Header + "</b></span>") {
                UseMarkup = true,
                Xalign = 0
            };

            Label description = new Label (Description) {
                Xalign = 0,
                Wrap   = true
            };

            VBox layout_vertical = new VBox (false, 0);
            layout_vertical.PackStart (header, false, false, 0);

            if (!string.IsNullOrEmpty (Description))
                layout_vertical.PackStart (description, false, false, 21);

            if (widget != null)
                layout_vertical.PackStart (widget, true, true, 21);

            Wrapper.PackStart (layout_vertical, true, true, 0);
            ShowAll ();
        }


        public void Reset ()
        {
            Header      = "";
            Description = "";

            if (Wrapper.Children.Length > 0)
                Wrapper.Remove (Wrapper.Children [0]);

            foreach (Button button in Buttons)
                Buttons.Remove (button);

            ShowAll ();
        }
        
        new public void ShowAll ()
        {

         Present ();

            base.ShowAll ();
        }

        public void Close ()
        {
            HideAll ();
        }
    }
}
