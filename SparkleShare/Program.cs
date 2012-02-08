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

using Mono.Unix;
using SparkleLib;

namespace SparkleShare {

    // This is SparkleShare!
    public class Program {

        public static SparkleController Controller;
        public static SparkleUI UI;


        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }
        

        public static void Main (string [] args)
        {
            // Parse the command line options
            bool show_help       = false;
            OptionSet option_set = new OptionSet () {
                { "v|version", _("Print version information"), v => { PrintVersion (); } },
                { "h|help", _("Show this help text"), v => show_help = v != null }
            };

            try {
                option_set.Parse (args);

            } catch (OptionException e) {
                Console.Write ("SparkleShare: ");
                Console.WriteLine (e.Message);
                Console.WriteLine ("Try `sparkleshare --help' for more information.");
            }

            if (show_help)
                ShowHelp (option_set);


            // Initialize the controller this way so that
            // there aren't any exceptions in the OS specific UI's
            Controller = new SparkleController ();
            Controller.Initialize ();
        
            if (Controller != null) {
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
            Console.WriteLine (_("SparkleShare is a collaboration and sharing tool that is "));
            Console.WriteLine (_("designed to keep things simple and to stay out of your way."));
            Console.WriteLine (" ");
            Console.WriteLine (_("Usage: sparkleshare [start|stop|restart|version] [OPTION]..."));
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
    }
}
