//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hi@planetpeanut.uk)
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

using Sparkles;
using Gtk;

namespace SparkleShare {

    public class About : Window {

        public AboutController Controller = new AboutController ();

        Label updates;


        public About () : base ("About SparkleShare")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            IconName       = "org.sparkleshare.SparkleShare";
            Resizable      = false;
            WindowPosition = WindowPosition.CenterAlways;
            TypeHint       = Gdk.WindowTypeHint.Dialog;

            SetSizeRequest (640, 260);


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
                    updates.Text = text;
                    updates.ShowAll();
                });
            };


            CreateAbout ();
        }


        void CreateAbout ()
        {
            CssProvider window_css_provider = new CssProvider ();
            Image image = UserInterfaceHelpers.GetImage ("about.png");

            window_css_provider.LoadFromData ("window, GtkWindow {" +
                "    background-image: url(\"/app/share/sparkleshare/pixmaps/about.png\");" +
                "    background-repeat: no-repeat;" +
                "    background-position: left bottom;" +
                "}");

            StyleContext.AddProvider (window_css_provider, 800);

            var layout_vertical = new VBox (false, 0);
            var links_layout = new HBox (false, 16);

            CssProvider label_css_provider = new CssProvider ();
            label_css_provider.LoadFromData ("label { color: #fff; font-size: 12px; background-color: rgba(0, 0, 0, 0); }");

            CssProvider label_highlight_css_provider = new CssProvider ();
            label_highlight_css_provider.LoadFromData ("label { color: #a8bbcf; font-size: 12px; }");

            var version = new Label {
                Text = "version " + Controller.RunningVersion,
                Xalign = 0, Xpad = 0
            };

            if (InstallationInfo.Directory.StartsWith ("/app", StringComparison.InvariantCulture))
                version.Text += " (Flatpak)";

            updates = new Label ("Checking for updates…") {
                Xalign = 0, Xpad = 0
            };

            var copyright = new Label {
                Markup = string.Format ("Copyright © 2010–{0} Hylke Bons and others", DateTime.Now.Year),
                Xalign = 0, Xpad = 0
            };

            var license = new Label ("SparkleShare is Open Source and you’re free to use,\n" +
                "change, and share it under the GNU GPLv3") {

                Xalign = 0, Xpad = 0
            };

            license.StyleContext.AddProvider (label_css_provider, 800);
            updates.StyleContext.AddProvider (label_highlight_css_provider, 800);
            version.StyleContext.AddProvider (label_css_provider, 800);
            copyright.StyleContext.AddProvider (label_css_provider, 800);

            var website_link        = new Link ("Website", Controller.WebsiteLinkAddress);
            var credits_link        = new Link ("Credits", Controller.CreditsLinkAddress);
            var report_problem_link = new Link ("Report a problem", Controller.ReportProblemLinkAddress);
            var debug_log_link      = new Link ("Debug log", Controller.DebugLogLinkAddress);

            layout_vertical.PackStart (new Label (""), true, true, 0);
            layout_vertical.PackStart (version, false, false, 0);
            layout_vertical.PackStart (updates, false, false, 0);
            layout_vertical.PackStart (copyright, false, false, 6);
            layout_vertical.PackStart (license, false, false, 6);
            layout_vertical.PackStart (links_layout, false, false, 16);

            links_layout.PackStart (website_link, false, false, 0);
            links_layout.PackStart (credits_link, false, false, 0);
            links_layout.PackStart (report_problem_link, false, false, 0);
            links_layout.PackStart (debug_log_link, false, false, 0);

            var layout_horizontal = new HBox (false, 0);
            layout_horizontal.PackStart (new Label (""), false, false, 149);
            layout_horizontal.PackStart (layout_vertical, false, false, 0);

            Add (layout_horizontal);
        }
    }
    
    
    class Link : Label {

        public Link (string label, string url)
        {
            Markup   = string.Format ("<a href=\"{0}\">{1}</a>", url, label);	
            CanFocus = false;

            CssProvider css_provider = new CssProvider ();
            css_provider.LoadFromData ("label { color: #729fcf; font-size: 12px; }");
            StyleContext.AddProvider (css_provider, 800);
        }
    }
}
