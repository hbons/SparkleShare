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
using System.Threading;

#if __MonoCS__
using Gtk;
#else
using System.Windows.Forms;
#endif
using SparkleLib;

namespace SparkleShare {

    public class SparkleUI {
        
        public static SparkleStatusIcon StatusIcon;
        public static SparkleEventLog EventLog;
        public static SparkleSetup Setup;
        public static SparkleAbout About;


        // Short alias for the translations
        public static string _(string s)
        {
            return s;
        }


        public SparkleUI ()
        {
            // Initialize the application
#if __MonoCS__
            Application.Init ();

            GLib.ExceptionManager.UnhandledException += delegate (GLib.UnhandledExceptionArgs exArgs) {
                Exception UnhandledException = (Exception)exArgs.ExceptionObject;
                string ExceptionMessage = UnhandledException.Message.ToString ();
                MessageDialog ExceptionDialog = new MessageDialog (null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                                 "Unhandled Exception!\n" + UnhandledException.GetType ().ToString ());
                ExceptionDialog.Title = "ERROR";

                while (UnhandledException != null) {
                    Console.WriteLine ("\n\n"
                                    + "Unhandled exception\n"
                                    + "-------------------\n"
                                    + UnhandledException.Message + "\n\n"
                                    + UnhandledException.StackTrace);
                    UnhandledException = UnhandledException.InnerException;
                }

                ExceptionDialog.Run ();
                ExceptionDialog.Destroy ();

            };
#endif
            // Create the statusicon
            StatusIcon = new SparkleStatusIcon ();
            
            if (Program.Controller.FirstRun) {
                Setup = new SparkleSetup ();
                Setup.Controller.ShowSetupPage ();
            }
            
            Program.Controller.OnQuitWhileSyncing += delegate {
                // TODO: Pop up a warning when quitting whilst syncing
            };
        }

        // Runs the application
        public void Run ()
        {
            Application.Run ();
        }
    }
}
