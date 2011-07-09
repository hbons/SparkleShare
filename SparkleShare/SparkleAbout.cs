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
using System.Diagnostics;
using System.IO;
using System.Net;

using Gtk;
using Mono.Unix;

namespace SparkleShare {

    public class SparkleAbout : Window {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private Label version;


        // Short alias for the translations
        public static string _(string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleAbout () : base ("")
        {
            DefaultSize    = new Gdk.Size (640, 280);
            BorderWidth    = 0;
            IconName       = "folder-sparkleshare";
            WindowPosition = WindowPosition.Center;
            Title          = _("About SparkleShare");
            Resizable      = false;

            CreateAbout ();

            Controller.NewVersionEvent += delegate (string new_version) {
                Application.Invoke (delegate {
                    this.version.Markup = String.Format ("<small><span fgcolor='#f57900'>{0}: {1}</span></small>", _("A newer version is available!"), new_version);
                    this.version.ShowAll ();
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                Application.Invoke (delegate {
                    this.version.Markup = String.Format ("<small><span fgcolor='#4e9a06'>{0}</span></small>", _("You are running the latest version."));
                    this.version.ShowAll ();
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                Application.Invoke (delegate {
                    this.version.Markup = String.Format ("<small><span fgcolor='#4e9a06'>{0}</span></small>", _("Checking for updates..."));
                    this.version.ShowAll ();
                });
            };
        }


        private void CreateAbout ()
        {
            Gdk.Color color = Style.Foreground (StateType.Insensitive);
            string secondary_text_color = SparkleUIHelpers.GdkColorToHex (color);


                Label header = new Label () {
                    Markup = "<span fgcolor='" + secondary_text_color + "'>version " + Controller.RunningVersion + "</span>",
                    Xalign = 0,
                    Xpad = 18,
                    Ypad = 18
                };


            this.version = new Label () {
                Markup = String.Format ("<small>{0}</small>", _("Checking for updates...")),
                Xalign = 0,
                Xpad   = 18,
                Ypad   = 22,
            };

            Label license = new Label () {
                Xalign = 0,
                Xpad   = 18,
                Ypad   = 0,
                LineWrap     = true,
                Wrap         = true,
                LineWrapMode = Pango.WrapMode.Word,

                Markup = "<small>Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others.\n" +
                         "\n" +
                         "SparkleShare is Free and Open Source Software. You are free to use, modify, " +
                         "and redistribute it under the terms of the GNU General Public License " +
                         "version 3 or later.</small>"
            };

            VBox vbox = new VBox (false, 0) {
                BorderWidth = 0
            };



            vbox.PackStart (header, true, true, 0);
            vbox.PackStart (this.version, false, false, 0);
            vbox.PackStart (license, true, true, 0);

            Add (vbox);
        }
    }
}
