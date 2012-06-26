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
using System.Threading;

#if __MonoCS__
using Mono.Unix;
#endif

namespace SparkleShare {

    // This is SparkleShare!
    public class Program {

        public static SparkleController Controller;
        public static SparkleUI UI;

        private static Mutex program_mutex = new Mutex (false, "SparkleShare");
		
		
        // Short alias for the translations
        public static string _ (string s)
        {
            #if __MonoCS__
            return Catalog.GetString (s);
            #else
            return s;
            #endif
        }
        
     
        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Main (string [] args)
        {
            if (args.Length != 0 && !args [0].Equals ("start")) {
                Console.WriteLine (" ");
                Console.WriteLine ("SparkleShare is a collaboration and sharing tool that is ");
                Console.WriteLine ("designed to keep things simple and to stay out of your way.");
                Console.WriteLine (" ");
                Console.WriteLine ("Version: " + SparkleLib.Defines.VERSION);
                Console.WriteLine ("Copyright (C) 2010 Hylke Bons");
                Console.WriteLine (" ");
                Console.WriteLine ("This program comes with ABSOLUTELY NO WARRANTY.");
                Console.WriteLine (" ");
                Console.WriteLine ("This is free software, and you are welcome to redistribute it ");
                Console.WriteLine ("under certain conditions. Please read the GNU GPLv3 for details.");
                Console.WriteLine (" ");
                Console.WriteLine ("Usage: sparkleshare [start|stop|restart]");

                Environment.Exit (-1);
            }
			
			// Only allow one instance of SparkleShare (on Windows)
			if (!program_mutex.WaitOne (0, false)) {
				Console.WriteLine ("SparkleShare is already running.");
				Environment.Exit (-1);
			}

            // Initialize the controller this way so that
            // there aren't any exceptions in the OS specific UI's
            Controller = new SparkleController ();
            Controller.Initialize ();
        
            if (Controller != null) {
                UI = new SparkleUI ();
                UI.Run ();
            }
         
            #if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            #endif
        }
    }
}
