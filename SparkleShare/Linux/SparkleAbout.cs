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

namespace SparkleShare {

    public class SparkleAbout : Window {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private Label updates;


        public SparkleAbout () : base ("About SparkleShare")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            IconName       = "sparkleshare";
            Resizable      = false;
            WindowPosition = WindowPosition.Center;

            SetSizeRequest (600, 260);


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
                    ShowAll ();
                    Present ();
                });
            };

            Controller.UpdateLabelEvent += delegate (string text) {
                Application.Invoke (delegate {
                    this.updates.Text = text;
                    this.updates.ShowAll();
                });
            };


            CreateAbout ();
        }


        private void CreateAbout ()
        {
            Gdk.RGBA white = new Gdk.RGBA ();
            white.Parse ("#ffffff");

            Gdk.RGBA highlight = new Gdk.RGBA ();
            highlight.Parse ("#a8bbcf");

            Pango.FontDescription font = StyleContext.GetFont (StateFlags.Normal);
            font.Size = 9 * 1024;

            CssProvider css_provider = new CssProvider ();
            string image_path        = new string [] { SparkleUI.AssetsPath, "pixmaps", "about.png" }.Combine ();

            css_provider.LoadFromData ("GtkWindow {" +
                "background-image: url('" + image_path + "');" +
                "background-repeat: no-repeat;" +
                "background-position: left bottom;" +
                "}");
            
            StyleContext.AddProvider (css_provider, 800);

            VBox layout_vertical = new VBox (false, 0);            
            HBox links_layout = new HBox (false, 16);


            Label version = new Label () {
                Text   = "version " + Controller.RunningVersion,
                Xalign = 0, Xpad   = 0
            };

            version.OverrideFont (font);
            version.OverrideColor (StateFlags.Normal, white);


            this.updates = new Label ("Checking for updates…") {
                Xalign = 0, Xpad = 0
            };

            this.updates.OverrideFont (font);
            this.updates.OverrideColor (StateFlags.Normal, highlight);


            Label copyright = new Label () {
                Markup = string.Format ("Copyright © 2010–{0} Hylke Bons and others.", DateTime.Now.Year),
                Xalign = 0, Xpad   = 0
            };

            copyright.OverrideFont (font);
            copyright.OverrideColor (StateFlags.Normal, white);


            TextView license          = new TextView ();
            TextBuffer license_buffer = license.Buffer;
            license.WrapMode          = WrapMode.Word;
            license.Sensitive         = false;

            license_buffer.Text = "SparkleShare is Open Source and you’re free to use, change, " +
                "and share it under the GNU GPLv3.";

            license.OverrideBackgroundColor (StateFlags.Normal, new Gdk.RGBA () { Alpha = 0 });
            license.OverrideFont (font);
            license.OverrideColor (StateFlags.Normal, white);

            
            SparkleLink website_link        = new SparkleLink ("Website", Controller.WebsiteLinkAddress);
            SparkleLink credits_link        = new SparkleLink ("Credits", Controller.CreditsLinkAddress);
            SparkleLink report_problem_link = new SparkleLink ("Report a problem", Controller.ReportProblemLinkAddress);
            SparkleLink debug_log_link      = new SparkleLink ("Debug log", Controller.DebugLogLinkAddress);


            layout_vertical.PackStart (new Label (""), true, true, 0);            
            layout_vertical.PackStart (version, false, false, 0);
            layout_vertical.PackStart (this.updates, false, false, 0);
            layout_vertical.PackStart (copyright, false, false, 6);
            layout_vertical.PackStart (license, false, false, 6);
            layout_vertical.PackStart (links_layout, false, false, 16);

            links_layout.PackStart (website_link, false, false, 0);
            links_layout.PackStart (credits_link, false, false, 0);
            links_layout.PackStart (report_problem_link, false, false, 0);
            links_layout.PackStart (debug_log_link, false, false, 0);
            
            HBox layout_horizontal = new HBox (false, 0);
            layout_horizontal.PackStart (new Label (""), false, false, 149);
            layout_horizontal.PackStart (layout_vertical, false, false, 0);

            Add (layout_horizontal);
        }
    }
    
    
    public class SparkleLink : Label {
        
        public SparkleLink (string text, string url)
        {
            Markup = string.Format ("<a href=\"{0}\"><span fgcolor=\"#729fcf\">{1}</span></a>", url, text);	
            CanFocus = false;

            Pango.FontDescription font = StyleContext.GetFont (StateFlags.Normal);
            font.Size = 9 * 1024;

            OverrideFont (font);
        }
    }
}
