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
using Sparkles;

namespace SparkleShare {

    public class UserInterface {

        public static string AssetsPath = InstallationInfo.Directory;

        public StatusIcon StatusIcon;
        public EventLog EventLog;
        public Bubbles Bubbles;
        public Setup Setup;
        public About About;
        public Note Note;

        public readonly string SecondaryTextColor;
        public readonly string SecondaryTextColorSelected;

        Application application;


        public UserInterface ()
        {
            application = new Application ("org.sparkleshare.SparkleShare", 0);

            application.Register (null);
            application.Activated += ApplicationActivatedDelegate;

            Gdk.Color color = UserInterfaceHelpers.RGBAToColor (new Label().StyleContext.GetColor (StateFlags.Insensitive));
            SecondaryTextColor = UserInterfaceHelpers.ColorToHex (color);

            var tree_view = new TreeView ();

            color = UserInterfaceHelpers.MixColors (
                UserInterfaceHelpers.RGBAToColor (tree_view.StyleContext.GetColor (StateFlags.Selected)),
                UserInterfaceHelpers.RGBAToColor (tree_view.StyleContext.GetBackgroundColor (StateFlags.Selected)),
                0.39);
    
            SecondaryTextColorSelected = UserInterfaceHelpers.ColorToHex (color);
        }


        public void Run ()
        {   
            (application as GLib.Application).Run ("org.sparkleshare.SparkleShare", new string [0]);
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
                    SparkleShare.Controller.HandleReopen ();

                return;
            }

            Setup      = new Setup ();
            EventLog   = new EventLog ();
            About      = new About ();
            Bubbles    = new Bubbles ();
            StatusIcon = new StatusIcon ();
            Note       = new Note ();

            Setup.Application    = application;
            EventLog.Application = application;
            About.Application    = application;

            SparkleShare.Controller.UIHasLoaded ();
        }
    }
}
