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
using System.IO;

using SparkleLib;
using System.Windows.Forms;
using System.Drawing;

namespace SparkleShare {

    // The statusicon that stays in the
    // user's notification area
    public class SparkleStatusIcon {

        private Timer Animation;
        private Bitmap [] AnimationFrames;
        private int FrameNumber;
        private string StateText;

        private ToolStripItem status_menu_item;
        private NotifyIcon status_icon;
        
        // Short alias for the translations
        public static string _ (string s)
        {
            return s;
        }


        public SparkleStatusIcon ()
        {
            AnimationFrames = CreateAnimationFrames ();
            Animation = CreateAnimation ();

            this.status_icon = new NotifyIcon ();
            status_icon.Text = "SparkleShare";
            status_icon.Icon = Icons.sparkleshare;
            status_icon.Visible = true;

            CreateMenu ();
            SetNormalState ();

            SparkleShare.Controller.FolderSizeChanged += delegate {
                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    if (!Animation.Enabled)
                        SetNormalState ();

                    UpdateMenu ();
                });
            };
            
            SparkleShare.Controller.FolderListChanged += delegate {
                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    SetNormalState ();
                    CreateMenu ();
                });
            };

            SparkleShare.Controller.OnIdle += delegate {
                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    SetNormalState ();
                    UpdateMenu ();
                });
            };

            SparkleShare.Controller.OnSyncing += delegate {
                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    SetAnimationState ();
                    UpdateMenu ();
                });
            };

            SparkleShare.Controller.OnError += delegate {
                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    SetNormalState (true);
                    UpdateMenu ();
                });
            };
        }


        // Slices up the graphic that contains the
        // animation frames.
        private Bitmap [] CreateAnimationFrames ()
        {
            Bitmap [] animation_frames = new Bitmap [5];
            animation_frames [0] = Icons.process_syncing_sparkleshare_i_24;
            animation_frames [1] = Icons.process_syncing_sparkleshare_ii_24;
            animation_frames [2] = Icons.process_syncing_sparkleshare_iii_24;
            animation_frames [3] = Icons.process_syncing_sparkleshare_iiii_24;
            animation_frames [4] = Icons.process_syncing_sparkleshare_iiiii_24;

            return animation_frames;
        }


        // Creates the Animation that handles the syncing animation
        private Timer CreateAnimation ()
        {
            FrameNumber = 0;

            Timer Animation = new Timer () {
                Interval = 35
            };

            Animation.Tick += delegate {
                if (FrameNumber < AnimationFrames.Length - 1)
                    FrameNumber++;
                else
                    FrameNumber = 0;

                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    this.status_icon.Icon = Icon.FromHandle (AnimationFrames [FrameNumber].GetHicon ());
                });
            };

            return Animation;
        }


        // Creates the menu that is popped up when the
        // user clicks the status icon
        public void CreateMenu ()
        {
            ContextMenuStrip Menu = new ContextMenuStrip ();

            // The menu item showing the status and size of the SparkleShare folder
            status_menu_item = new ToolStripLabel (StateText);

            Menu.Items.Add (status_menu_item);
            Menu.Items.Add (new ToolStripSeparator ());

            ToolStripMenuItem folder_item = new ToolStripMenuItem ("SparkleShare") {
                Image = Icons.folder_sparkleshare_16
            };

            folder_item.Click += delegate {
                SparkleShare.Controller.OpenSparkleShareFolder ();
            };

            Menu.Items.Add (folder_item);

            if (SparkleShare.Controller.Folders.Count > 0) {

                // Creates a menu item for each repository with a link to their logs
                foreach (string folder_name in SparkleShare.Controller.Folders) {
                    Bitmap folder_icon;

                    if (SparkleShare.Controller.UnsyncedFolders.Contains (folder_name)) {
                        folder_icon = Icons.dialog_error_16;
                    } else {
                        folder_icon = Icons.sparkleshare_windows_status;
                    }

                    ToolStripMenuItem subfolder_item = new ToolStripMenuItem (folder_name) {
                        Image = folder_icon
                    };

                    subfolder_item.Click += OpenFolderDelegate (folder_name);
                    Menu.Items.Add (subfolder_item);
                }

            } else {
                ToolStripMenuItem no_folders_item = new ToolStripMenuItem (_ ("No Remote Folders Yet")) {
                    Enabled = false
                };

                Menu.Items.Add (no_folders_item);
            }

            Menu.Items.Add (new ToolStripSeparator ());

            // Opens the wizard to add a new remote folder
            ToolStripMenuItem sync_item = new ToolStripMenuItem (_ ("Add Remote Folder…"));

            if (SparkleShare.Controller.FirstRun)
                sync_item.Enabled = false;

            sync_item.Click += delegate {

                if (SparkleUI.Setup == null) {
                    SparkleUI.Setup = new SparkleSetup ();
                    SparkleUI.Setup.Controller.ShowAddPage ();
                }

                if (!SparkleUI.Setup.Visible)
                    SparkleUI.Setup.Controller.ShowAddPage ();
            };

            Menu.Items.Add (sync_item);
            Menu.Items.Add (new ToolStripSeparator ());

            ToolStripMenuItem recent_events_item = new ToolStripMenuItem (_ ("Show Recent Events"));

            if (SparkleShare.Controller.Folders.Count < 1)
                recent_events_item.Enabled = false;

            recent_events_item.Click += delegate {
                if (SparkleUI.EventLog == null)
                    SparkleUI.EventLog = new SparkleEventLog ();

                SparkleUI.EventLog.ShowAll ();
                SparkleUI.EventLog.Present ();
            };

            Menu.Items.Add (recent_events_item);

            ToolStripMenuItem notify_item;

            if (SparkleShare.Controller.NotificationsEnabled)
                notify_item = new ToolStripMenuItem (_ ("Turn Notifications Off"));
            else
                notify_item = new ToolStripMenuItem (_ ("Turn Notifications On"));

            notify_item.Click += delegate {
                SparkleShare.Controller.ToggleNotifications ();
                CreateMenu ();
            };

            Menu.Items.Add (notify_item);
            Menu.Items.Add (new ToolStripSeparator ());

            // A menu item that takes the user to http://www.sparkleshare.org/
            ToolStripMenuItem about_item = new ToolStripMenuItem (_ ("About SparkleShare"));

            about_item.Click += delegate {
                if (SparkleUI.About == null)
                    SparkleUI.About = new SparkleAbout ();

                SparkleUI.About.Show ();
                SparkleUI.About.BringToFront ();
            };

            Menu.Items.Add (about_item);
            Menu.Items.Add (new ToolStripSeparator ());

            // A menu item that quits the application
            ToolStripMenuItem quit_item = new ToolStripMenuItem (_ ("Quit"));

            quit_item.Click += delegate {
                SparkleShare.Controller.Quit ();
            };

            Menu.Items.Add (quit_item);

            status_icon.ContextMenuStrip = Menu;
        }


        // A method reference that makes sure that opening the
        // event log for each repository works correctly
        private EventHandler OpenFolderDelegate (string name)
        {
            return delegate {
                SparkleShare.Controller.OpenSparkleShareFolder (name);
            };
        }


        public void UpdateMenu ()
        {
            status_menu_item.Text=StateText;
        }


        // The state when there's nothing going on
        private void SetNormalState ()
        {
            SetNormalState (false);
        }


        // The state when there's nothing going on
        private void SetNormalState (bool error)
        {
            Animation.Stop ();

            if (SparkleShare.Controller.Folders.Count == 0) {
                StateText = _("Welcome to SparkleShare!");

                status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                    this.status_icon.Icon = Icon.FromHandle (AnimationFrames [0].GetHicon ());
                });

            } else {
                if (error) {
                    StateText = _("Not everything is synced");

                    status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                        this.status_icon.Icon = Icon.FromHandle (Icons.sparkleshare_syncing_error_24.GetHicon ());
                    });
                } else {
                    StateText = _("Up to date") + "  (" + SparkleShare.Controller.FolderSize + ")";
                    status_icon.ContextMenuStrip.SafeInvoke ((Action)delegate {
                        this.status_icon.Icon = Icon.FromHandle (AnimationFrames [0].GetHicon ());
                    });
                }
            }
        }


        // The state when animating
        private void SetAnimationState ()
        {
            StateText = _("Syncing…");

            if (!Animation.Enabled)
                Animation.Start ();
        }
    }


    public static class ControlExtention {
        public static void SafeInvoke (this Control uiElement, Action updater, bool forceSynchronous)
        {
            if (uiElement == null) {
                throw new ArgumentNullException ("uiElement");
            }

            if (uiElement.InvokeRequired) {
                if (forceSynchronous) {
                    uiElement.Invoke ((Action)delegate { SafeInvoke (uiElement, updater, forceSynchronous); });
                } else {
                    uiElement.BeginInvoke ((Action)delegate { SafeInvoke (uiElement, updater, forceSynchronous); });
                }
            } else {
                if (uiElement.IsDisposed) {
                    throw new ObjectDisposedException ("Control is already disposed.");
                }

                updater ();
            }
        }
        public static void SafeInvoke (this Control uiElement, Action updater)
        {
            uiElement.SafeInvoke (updater, false);
        }

    }
}
