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
using System.Runtime.InteropServices;
using System.Text;

using SparkleLib;
using SparkleLib.Options;

namespace SparkleShare {

    // This is SparkleShare!
    public class SparkleShare {

        public static SparkleController Controller;
        public static SparkleUI UI;


        // Short alias for the translations
        public static string _ (string s)
        {
            return s;
        }
        

        public static void Main (string [] args)
        {
            // Use translations
			if ((SparkleBackend.Platform == PlatformID.Unix ||
				 SparkleBackend.Platform == PlatformID.MacOSX))
				Mono.Unix.Catalog.Init (Defines.GETTEXT_PACKAGE, Defines.LOCALE_DIR);

            // Don't allow running as root on Linux or Mac
            if ((SparkleBackend.Platform == PlatformID.Unix ||
                 SparkleBackend.Platform == PlatformID.MacOSX) &&
                new Mono.Unix.UnixUserInfo (Mono.Unix.UnixEnvironment.UserName).UserId == 0) {

                Console.WriteLine (_("Sorry, you can't run SparkleShare with these permissions."));
                Console.WriteLine (_("Things would go utterly wrong."));
                Environment.Exit (-1);
            }

            // Parse the command line options
            bool hide_ui   = false;
            bool show_help = false;

            var p = new OptionSet () {
                { "d|disable-gui", _("Don't show the notification icon"), v => hide_ui = v != null },
                { "v|version", _("Print version information"), v => { PrintVersion (); } },
                { "h|help", _("Show this help text"), v => show_help = v != null }
            };

            try {
                p.Parse (args);

            } catch (OptionException e) {
                Console.Write ("SparkleShare: ");
                Console.WriteLine (e.Message);
                Console.WriteLine ("Try `sparkleshare --help' for more information.");
            }

            if (show_help)
                ShowHelp (p);

            // Load the right controller for the OS
            string controller_name = "Lin";
            switch (SparkleBackend.Platform) {
            case PlatformID.Unix:
                SetProcessName ("sparkleshare");
                break;
            case PlatformID.MacOSX:
                controller_name = "Mac";
                break;
            case PlatformID.Win32NT:
                controller_name = "Win";
                break;
            }

            // Initialize the controller this way so that
            // there aren't any exceptions in the OS specific UI's
            Controller = (SparkleController) Activator.CreateInstance (
                Type.GetType ("SparkleShare.Sparkle" + controller_name + "Controller"));

            Controller.Initialize ();
        
            if (Controller != null && !hide_ui) {
                UI = new SparkleUI ();
                UI.Run ();
            }
        }


        // Prints the help output
        public static void ShowHelp (OptionSet option_set)
        {
            Console.WriteLine (" ");
            Console.WriteLine (_("SparkleShare, a collaboration and sharing tool."));
            Console.WriteLine (_("Copyright (C) 2010 Hylke Bons"));
            Console.WriteLine (" ");
            Console.WriteLine (_("This program comes with ABSOLUTELY NO WARRANTY."));
            Console.WriteLine (" ");
            Console.WriteLine (_("This is free software, and you are welcome to redistribute it "));
            Console.WriteLine (_("under certain conditions. Please read the GNU GPLv3 for details."));
            Console.WriteLine (" ");
            Console.WriteLine (_("SparkleShare automatically syncs Git repositories in "));
            Console.WriteLine (_("the ~/SparkleShare folder with their remote origins."));
            Console.WriteLine (" ");
            Console.WriteLine (_("Usage: sparkleshare [start|stop|restart] [OPTION]..."));
            Console.WriteLine (_("Sync SparkleShare folder with remote repositories."));
            Console.WriteLine (" ");
            Console.WriteLine (_("Arguments:"));

            option_set.WriteOptionDescriptions (Console.Out);
            Environment.Exit (0);
        }


        // Prints the version information
        public static void PrintVersion ()
        {
            Console.WriteLine (_("SparkleShare " + Defines.VERSION));
            Environment.Exit (0);
        }


        // Strange magic needed by SetProcessName ()
        [DllImport ("libc")]
        private static extern int prctl (int option, byte [] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
        
        
        // Sets the Unix process name to 'sparkleshare' instead of 'mono'
        private static void SetProcessName (string name)
        {
            try {
                if (prctl (15, Encoding.ASCII.GetBytes (name + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0)
                    throw new ApplicationException ("Error setting process name: " +
                                                    Mono.Unix.Native.Stdlib.GetLastError ());

            } catch (EntryPointNotFoundException) {
                Console.WriteLine ("SetProcessName: Entry point not found");
            }
        }
    }
}
