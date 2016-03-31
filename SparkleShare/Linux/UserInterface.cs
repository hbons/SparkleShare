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

using GLib;
using Gtk;

using Sparkles;

namespace SparkleShare {

    public class UserInterface {

        public static string AssetsPath = Defines.INSTALL_DIR;

        public SparkleStatusIcon StatusIcon;
        public SparkleEventLog EventLog;
        public SparkleBubbles Bubbles;
        public SparkleSetup Setup;
        public SparkleAbout About;
        public SparkleNote Note;

        public readonly string SecondaryTextColor;
        public readonly string SecondaryTextColorSelected;

        Gtk.Application application;


        public UserInterface ()
        {
            application = new Gtk.Application ("org.sparkleshare.SparkleShare", 0);

            application.Register (null);
            application.Activated += ApplicationActivatedDelegate;

            Gdk.Color color = SparkleUIHelpers.RGBAToColor (new Label().StyleContext.GetColor (StateFlags.Insensitive));
            SecondaryTextColor = SparkleUIHelpers.ColorToHex (color);
                    
            color = SparkleUIHelpers.MixColors (
                SparkleUIHelpers.RGBAToColor (new TreeView ().StyleContext.GetColor (StateFlags.Selected)),
                SparkleUIHelpers.RGBAToColor (new TreeView ().StyleContext.GetBackgroundColor (StateFlags.Selected)),
                0.39);
    
            SecondaryTextColorSelected = SparkleUIHelpers.ColorToHex (color);
        }


        public void Run ()
        {   
            (application as GLib.Application).Run (null, null);
        }


        void ApplicationActivatedDelegate (object sender, EventArgs args)
        {
            if (application.Windows.Length > 0) {
                bool has_visible_windows = false;

                foreach (Window window in application.Windows) {
                    if (window.Visible) {
                        window.Present ();
                        has_visible_windows = true;
                    }
                }

                if (!has_visible_windows)
                    Program.Controller.HandleReopen ();

            } else {
                Setup      = new SparkleSetup ();
                EventLog   = new SparkleEventLog ();
                About      = new SparkleAbout ();
                Bubbles    = new SparkleBubbles ();
                StatusIcon = new SparkleStatusIcon ();
                Note       = new SparkleNote ();

                Setup.Application    = application;
                EventLog.Application = application;
                About.Application    = application;

                Program.Controller.UIHasLoaded ();
            }
        }
    }
}
