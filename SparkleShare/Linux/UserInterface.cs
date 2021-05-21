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

        public string SecondaryTextColor;
        public string SecondaryTextColorSelected;

        public static readonly string APP_ID = "org.sparkleshare.SparkleShare";
        Application application;


        public UserInterface ()
        {
            string gtk_version = string.Format ("{0}.{1}.{2}", Global.MajorVersion, Global.MinorVersion, Global.MicroVersion);
            Logger.LogInfo ("Environment", "GTK+ " + gtk_version);

            application = new Application (APP_ID, GLib.ApplicationFlags.None);
            application.Activated += ApplicationActivatedDelegate;

            if (!application.IsRemote)
                return;

            application.Register (null);
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
                run_method.Invoke ((application as GLib.Application), new object [] { APP_ID, new string [0] });
            }
        }


        void ParseArgs (string [] args)
        {
            if (args.Length > 0)
                Logger.LogInfo ("Environment", "Arguments: " + string.Join (" ", args));

            if (Array.IndexOf (args, "--status-icon=gtk") > -1)
                StatusIcon.use_appindicator = false;

            #if HAVE_APP_INDICATOR
            if (Array.IndexOf (args, "--status-icon=appindicator") > -1)
                StatusIcon.use_appindicator = true;
            #else
            if (StatusIcon.use_appindicator) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine ("error: AppIndicator not found. Install AppIndicator or run with --status-icon=gtk");
                Console.ResetColor ();

                Environment.Exit (-1);
            }
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

            if (IconTheme.Default != null)
                IconTheme.Default.AppendSearchPath (Path.Combine (UserInterface.AssetsPath, "icons"));

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

            DetectTextColors ();

            SparkleShare.Controller.UIHasLoaded ();
        }


        void DetectTextColors ()
        {
            Gdk.Color text_color = UserInterfaceHelpers.RGBAToColor (new Label ().StyleContext.GetColor (StateFlags.Insensitive));
            var tree_view_style = new TreeView ().StyleContext;

            Gdk.Color text_color_selected = UserInterfaceHelpers.MixColors (
                UserInterfaceHelpers.RGBAToColor (tree_view_style.GetColor (StateFlags.Selected)),
                UserInterfaceHelpers.RGBAToColor (tree_view_style.GetBackgroundColor (StateFlags.Selected)),
                0.2);

            SecondaryTextColor = UserInterfaceHelpers.ColorToHex (text_color);
            SecondaryTextColorSelected = UserInterfaceHelpers.ColorToHex (text_color_selected);
        }
    }
}
