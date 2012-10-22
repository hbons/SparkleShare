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

using SparkleLib;

namespace SparkleShare {

    // This is SparkleShare!
    public class Program {

        public static SparkleController Controller;
        public static SparkleUI UI;

        private static Mutex program_mutex = new Mutex (false, "SparkleShare");
        
     
        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Main (string [] args)
        {
            if (args.Length != 0 && !args [0].Equals ("start") &&
                SparkleBackend.Platform != PlatformID.MacOSX &&
                SparkleBackend.Platform != PlatformID.Win32NT) {

                string n = Environment.NewLine;

                Console.WriteLine (n +
                    "SparkleShare is a collaboration and sharing tool that is" + n +
                    "designed to keep things simple and to stay out of your way." + n +
                    n +
                    "Version: " + SparkleLib.SparkleBackend.Version + n +
                    "Copyright (C) 2010 Hylke Bons" + n +
                    "This program comes with ABSOLUTELY NO WARRANTY." + n +
                    n +
                    "This is free software, and you are welcome to redistribute it" + n +
                    "under certain conditions. Please read the GNU GPLv3 for details." + n +
                    n +
                    "Usage: sparkleshare [start|stop|restart]");

                Environment.Exit (-1);
            }

            // Only allow one instance of SparkleShare (on Windows)
            if (!program_mutex.WaitOne (0, false)) {
                Console.WriteLine ("SparkleShare is already running.");
                Environment.Exit (-1);
            }

            try {
                Controller = new SparkleController ();
                Controller.Initialize ();

                UI = new SparkleUI ();
                UI.Run ();
            
            } catch (Exception e) {
                SparkleLogger.WriteCrashReport (e);
                Environment.Exit (-1);
            }
         
            #if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            #endif
        }
    }
}
