//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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
using System.IO;
using System.Reflection;

using Gtk;
using Sparkles;

namespace SparkleShare
{
    public class UserInterface
    {
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
            string gtk_version = string.Format ("{0}.{1}.{2}", Global.MajorVersion, Global.MinorVersion, Global.MicroVersion);
            Logger.LogInfo ("Environment", "GTK+ " + gtk_version);

            application = new Application ("org.sparkleshare.SparkleShare", GLib.ApplicationFlags.None);

            application.Register (null);
            application.Activated += ApplicationActivatedDelegate;


            //if (IconTheme.Default != null)
                IconTheme.Default.AppendSearchPath (Path.Combine (UserInterface.AssetsPath, "icons"));

            var label = new Label ();
            Gdk.Color color = UserInterfaceHelpers.RGBAToColor (label.StyleContext.GetColor (StateFlags.Insensitive));
            SecondaryTextColor = UserInterfaceHelpers.ColorToHex (color);

            var tree_view = new TreeView ();

            color = UserInterfaceHelpers.MixColors (
                UserInterfaceHelpers.RGBAToColor (tree_view.StyleContext.GetColor (StateFlags.Selected)),
                UserInterfaceHelpers.RGBAToColor (tree_view.StyleContext.GetBackgroundColor (StateFlags.Selected)),
                0.39);

            SecondaryTextColorSelected = UserInterfaceHelpers.ColorToHex (color);
        }


        public void Run (string [] args)
        {
            ParseArgs (args);

            MethodInfo [] methods = typeof (GLib.Application).GetMethods (BindingFlags.Instance | BindingFlags.Public);
            ParameterInfo [] run_parameters = new ParameterInfo [0];
            MethodInfo run_method = methods [0];

            foreach (MethodInfo method in methods) {
                if (method.Name == "Run") {
                    run_parameters = method.GetParameters ();

                    if (run_parameters.Length == 2) {
                        run_method = method;
                        break;
                    }
                }
            }

            // Use the right Run method arguments depending on the installed GTK bindings
            if (run_parameters [0].ParameterType == typeof (System.Int32) &&
                run_parameters [1].ParameterType == typeof (System.String)) {

                run_method.Invoke ((application as GLib.Application), new object [] { 0, null });

            } else {
                run_method.Invoke ((application as GLib.Application), new object [] { "org.sparkleshare.SparkleShare", new string [0] });
            }
        }


        void ParseArgs (string [] args)
        {
            if (args.Length > 0)
                Logger.LogInfo ("Environment", "Arguments: " + string.Join (" ", args));

            if (Array.IndexOf (args, "--status-icon=gtk") > -1)
                StatusIcon.use_appindicator = false;

            #if HAVE_APPINDICATOR
            if (Array.IndexOf (args, "--status-icon=appindicator") > -1)
                StatusIcon.use_appindicator = true;
            #endif

            if (StatusIcon.use_appindicator)
                Logger.LogInfo ("Environment", "Status Icon: AppIndicator");
            else
                Logger.LogInfo ("Environment", "Status Icon: GtkStatusIcon");
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
            Note.Application     = application;

            SparkleShare.Controller.UIHasLoaded ();
        }
    }
}
