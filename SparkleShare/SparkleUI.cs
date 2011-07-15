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

using Gtk;
using Mono.Unix;
using Mono.Unix.Native;
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
            return Catalog.GetString (s);
        }


        public SparkleUI ()
        {
            // Initialize the application
            Application.Init ();

            // Use translations
            Catalog.Init (Defines.GETTEXT_PACKAGE, Defines.LOCALE_DIR);

            // Create the statusicon
            StatusIcon = new SparkleStatusIcon ();
            
            if (SparkleShare.Controller.FirstRun) {
                Setup = new SparkleSetup ();
                Setup.Controller.ShowSetupPage ();
            }
            
            SparkleShare.Controller.OnQuitWhileSyncing += delegate {
                // TODO: Pop up a warning when quitting whilst syncing
            };



            // Show a bubble when there are new changes
            SparkleShare.Controller.NotificationRaised += delegate (string user_name, string user_email,
                                                                    string message, string repository_path) {
                Application.Invoke (delegate {
                    if (EventLog != null)
                        EventLog.UpdateEvents ();

                    if (!SparkleShare.Controller.NotificationsEnabled)
                        return;

                    // TODO SparkleBubble bubble    = new SparkleBubble (user_name, message);
                    //string avatar_file_path = SparkleShare.Controller.GetAvatar (user_email, 36);

                    //if (avatar_file_path != null)
                      //  bubble.Icon = new Gdk.Pixbuf (avatar_file_path);
                    //else
                      //  bubble.Icon = SparkleUIHelpers.GetIcon ("avatar-default", 36);

                    //bubble.Show ();
                });
            };

            // Show a bubble when there was a conflict
            SparkleShare.Controller.ConflictNotificationRaised += delegate {
                Application.Invoke (delegate {
                    string title   = _("Ouch! Mid-air collision!");
                    string subtext = _("Don't worry, SparkleShare made a copy of each conflicting file.");

                    // TODO new SparkleBubble (title, subtext).Show ();
                });
            };

            SparkleShare.Controller.AvatarFetched += delegate {
                Application.Invoke (delegate {
                    if (EventLog != null)
                        EventLog.UpdateEvents ();
                });
            };

            SparkleShare.Controller.OnIdle += delegate {
                Application.Invoke (delegate {
                    if (EventLog != null)
                        EventLog.UpdateEvents ();
                });
            };

            SparkleShare.Controller.FolderListChanged += delegate {
                Application.Invoke (delegate {
                    if (EventLog != null) {
                        EventLog.UpdateChooser ();
                        EventLog.UpdateEvents ();
                    }
                });
            };
        }        

        // Runs the application
        public void Run ()
        {
            Application.Run ();
        }
    }
}
