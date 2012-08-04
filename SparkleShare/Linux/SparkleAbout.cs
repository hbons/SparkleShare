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


using System;

using Gtk;
using Mono.Unix;

namespace SparkleShare {

    public class SparkleAbout : Window {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private Label updates;


        public SparkleAbout () : base ("")
        {
            DeleteEvent += delegate (object o, DeleteEventArgs args) {
                Controller.WindowClosed ();
                args.RetVal = true;
            };

            DefaultSize    = new Gdk.Size (600, 260);
            Resizable      = false;
            BorderWidth    = 0;
            IconName       = "folder-sparkleshare";
            WindowPosition = WindowPosition.Center;
            Title          = "About SparkleShare";
            AppPaintable   = true;

            string image_path = new string [] { Program.UI.AssetsPath, "pixmaps", "about.png" }.Combine ();

            Realize ();
            Gdk.Pixbuf buf = new Gdk.Pixbuf (image_path);
            Gdk.Pixmap map, map2;
            buf.RenderPixmapAndMask (out map, out map2, 255);
            GdkWindow.SetBackPixmap (map, false);

            CreateAbout ();

            Controller.HideWindowEvent += delegate {
                Application.Invoke (delegate {
                    HideAll ();
                });
            };

            Controller.ShowWindowEvent += delegate {
                Application.Invoke (delegate {
                    ShowAll ();
                    Present ();
                });
            };

            Controller.NewVersionEvent += delegate (string new_version) {
                Application.Invoke (delegate {
                    this.updates.Markup = String.Format ("<span font_size='small' fgcolor='#729fcf'>{0}</span>",
                        string.Format ("A newer version ({0}) is available!", new_version));

                    this.updates.ShowAll ();
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                Application.Invoke (delegate {
                    this.updates.Markup = String.Format ("<span font_size='small' fgcolor='#729fcf'>{0}</span>",
                        "You are running the latest version.");

                    this.updates.ShowAll ();
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                Application.Invoke (delegate {
                    this.updates.Markup = String.Format ("<span font_size='small' fgcolor='#729fcf'>{0}</span>",
                        "Checking for updates...");

                    this.updates.ShowAll ();
                });
            };
        }


        private void CreateAbout ()
        {
            Label version = new Label () {
                Markup = string.Format ("<span font_size='small' fgcolor='white'>version {0}</span>",
                    Controller.RunningVersion),
                Xalign = 0,
                Xpad = 300
            };

            this.updates = new Label () {
                Markup = "<span font_size='small' fgcolor='#729fcf'>Checking for updates...</span>",
                Xalign = 0,
                Xpad = 300
            };

            Label copyright = new Label () {
                Markup = "<span font_size='small' fgcolor='white'>" +
                         "Copyright © 2010–" + DateTime.Now.Year + " " +
                         "Hylke Bons and others." +
                         "</span>",
                Xalign = 0,
                Xpad   = 300
            };

            Label license = new Label () {
                LineWrap     = true,
                LineWrapMode = Pango.WrapMode.Word,
                Markup       = "<span font_size='small' fgcolor='white'>" +
                               "SparkleShare Open Source software. You are free to use, modify, " +
                               "and redistribute it under the GNU General Public License version 3 or later." +
                               "</span>",
                WidthRequest = 330,
                Wrap         = true,
                Xalign       = 0,
                Xpad         = 300,
            };

            VBox layout_horizontal = new VBox (false, 0) {
                BorderWidth   = 0,
                HeightRequest = 260,
                WidthRequest  = 640
            };

            layout_horizontal.PackStart (new Label (""), false, false, 42);
            layout_horizontal.PackStart (version, false, false, 0);
            layout_horizontal.PackStart (this.updates, false, false, 0);
            layout_horizontal.PackStart (copyright, false, false, 9);
            layout_horizontal.PackStart (license, false, false, 0);
            layout_horizontal.PackStart (new Label (""), false, false, 0);

            Add (layout_horizontal);
        }
    }
}
