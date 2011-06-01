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
        public static List <SparkleLog> OpenLogs;
        public static SparkleIntro Intro;


        // Short alias for the translations
        public static string _(string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleUI ()
        {
            // Initialize the application
            Application.Init ();

            // Create the statusicon
            StatusIcon = new SparkleStatusIcon ();
            
            // Keep track of which event logs are open
            OpenLogs = new List <SparkleLog> ();

            if (SparkleShare.Controller.FirstRun) {
                Intro = new SparkleIntro ();
                Intro.ShowAccountForm ();
            }
            
            SparkleShare.Controller.OnQuitWhileSyncing += delegate {
                // TODO: Pop up a warning when quitting whilst syncing
            };

            SparkleShare.Controller.OnInvitation += delegate (string server, string folder, string token) {
                Application.Invoke (delegate {
                    SparkleIntro intro = new SparkleIntro ();
                    intro.ShowInvitationPage (server, folder, token);
                });
            };

            // Show a bubble when there are new changes
            SparkleShare.Controller.NotificationRaised += delegate (string user_name, string user_email,
                                                                    string message, string repository_path) {
                Application.Invoke (delegate {
                    foreach (SparkleLog log in OpenLogs) {
                        if (log.LocalPath.Equals (repository_path))
                                log.UpdateEventLog ();
                    }
                    
                    if (!SparkleShare.Controller.NotificationsEnabled)
                        return;

                    SparkleBubble bubble    = new SparkleBubble (user_name, message);
                    string avatar_file_path = SparkleShare.Controller.GetAvatar (user_email, 32);

                    if (avatar_file_path != null)
                        bubble.Icon = new Gdk.Pixbuf (avatar_file_path);
                    else
                        bubble.Icon = SparkleUIHelpers.GetIcon ("avatar-default", 32);

                    bubble.AddAction ("", "Show Events", delegate {
                        AddEventLog (repository_path);                
                    });

                    bubble.Show ();
                });
            };

            // Show a bubble when there was a conflict
            SparkleShare.Controller.ConflictNotificationRaised += delegate {
                Application.Invoke (delegate {
                    string title   = _("Ouch! Mid-air collision!");
                    string subtext = _("Don't worry, SparkleShare made a copy of each conflicting file.");

                    new SparkleBubble (title, subtext).Show ();
                });
            };

            SparkleShare.Controller.AvatarFetched += delegate {
                Application.Invoke (delegate {
                    foreach (SparkleLog log in OpenLogs)
                        log.UpdateEventLog ();
                });
            };

            SparkleShare.Controller.OnIdle += delegate {
                Application.Invoke (delegate {
                    foreach (SparkleLog log in OpenLogs)
                        log.UpdateEventLog ();
                });
            };
        }


        public void AddEventLog (string path)
        {
            SparkleLog log = SparkleUI.OpenLogs.Find (delegate (SparkleLog l) {
                return l.LocalPath.Equals (path);
            });

            // Check whether the log is already open, create a new one if
            // that's not the case or present it to the user if it is
            if (log == null) {
                OpenLogs.Add (new SparkleLog (path));
                OpenLogs [OpenLogs.Count - 1].ShowAll ();
                OpenLogs [OpenLogs.Count - 1].Present ();
            } else {
                log.ShowAll ();
                log.Present ();
            }
        }
        

        // Runs the application
        public void Run ()
        {
            Application.Run ();
        }
    }
}
