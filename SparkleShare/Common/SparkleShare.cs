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

using Sparkles;

namespace SparkleShare {

    public class SparkleShare {

        public static Controller Controller;
        public static UserInterface UI;
        public static string [] Arguments;

        static Mutex program_mutex = new Mutex (false, "SparkleShare");


        #if !__MonoCS__
        [STAThread]
        #endif
        public static void Main (string [] args)
        {
            Arguments = args;

            if (args.Length != 0 && (args [0].Equals ("help") || args [0].Equals ("version")) &&
                InstallationInfo.OperatingSystem != OS.Mac &&
                InstallationInfo.OperatingSystem != OS.Windows) {

                string n = Environment.NewLine;

                Console.WriteLine (n +
                    "Share and collaborate by syncing with any Git repository instantly." + n +
                    n +
                   "Version: " + InstallationInfo.Version + n +
                    "Copyright (C) 2010 Hylke Bons and others" + n +
                    "This program comes with ABSOLUTELY NO WARRANTY." + n +
                    n +
                    "This is free software, and you are welcome to redistribute it" + n +
                    "under certain conditions. Please read the GNU GPLv3 for details." + n);

                Environment.Exit (0);
            }

            #if !__MonoCS__
            // Handle Squirrel Events
            var updater = new SparkleUpdater ();
            updater.HandleEvents (args);
            #endif

            // Only allow one instance of SparkleShare (on Windows)
            if (!program_mutex.WaitOne (0, false)) {
                Console.WriteLine ("SparkleShare is already running.");
                Environment.Exit (-1);
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Controller = new Controller (Configuration.DefaultConfiguration);
            Controller.Initialize ();

            UI = new UserInterface ();

            #if !__MonoCS__
            UI.Updater = updater;
            #endif

            UI.Run ();

            #if !__MonoCS__
            // Suppress assertion messages in debug mode
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            #endif
        }


        static void OnUnhandledException (object sender, UnhandledExceptionEventArgs exception_args)
        {
            var exception = (Exception) exception_args.ExceptionObject;
            Logger.WriteCrashReport (exception);
        }
    }
}
