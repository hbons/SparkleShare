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
using System.Timers;

using Gtk;
using Mono.Unix;

namespace SparkleShare {

    // The statusicon that stays in the
    // user's notification area
    public class SparkleStatusIcon : StatusIcon    {

        private Timer Animation;
        private Gdk.Pixbuf [] AnimationFrames;
        private int FrameNumber;
        private string StateText;
        private Menu Menu;

        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleStatusIcon () : base ()
        {
            AnimationFrames = CreateAnimationFrames ();
            Animation = CreateAnimation ();

            Activate += ShowMenu;  // Primary mouse button click
            PopupMenu += ShowMenu; // Secondary mouse button click

            SetNormalState ();
            CreateMenu ();

            SparkleShare.Controller.FolderSizeChanged += delegate {
                Application.Invoke (delegate {
                    if (!Animation.Enabled)
                        SetNormalState ();

                    UpdateMenu ();
                });
            };
            
            SparkleShare.Controller.FolderListChanged += delegate {
                Application.Invoke (delegate {
                    SetNormalState ();
                    CreateMenu ();
                });
            };

            SparkleShare.Controller.OnIdle += delegate {
                Application.Invoke (delegate {
                    SetNormalState ();
                    UpdateMenu ();
                });
            };

            SparkleShare.Controller.OnSyncing += delegate {
                Application.Invoke (delegate {
                    SetAnimationState ();
                    UpdateMenu ();
                });
            };

            SparkleShare.Controller.OnError += delegate {
                Application.Invoke (delegate {
                    SetNormalState (true);
                    UpdateMenu ();
                });
            };
        }


        // Slices up the graphic that contains the
        // animation frames.
        private Gdk.Pixbuf [] CreateAnimationFrames ()
        {
            Gdk.Pixbuf [] animation_frames = new Gdk.Pixbuf [5];
            Gdk.Pixbuf frames_pixbuf = SparkleUIHelpers.GetIcon ("process-syncing-sparkleshare", 24);
            
            for (int i = 0; i < animation_frames.Length; i++)
                animation_frames [i] = new Gdk.Pixbuf (frames_pixbuf, (i * 24), 0, 24, 24);

            return animation_frames;
        }


        // Creates the Animation that handles the syncing animation
        private Timer CreateAnimation ()
        {
            FrameNumber = 0;

            Timer Animation = new Timer () {
                Interval = 35
            };

            Animation.Elapsed += delegate {
                if (FrameNumber < AnimationFrames.Length - 1)
                    FrameNumber++;
                else
                    FrameNumber = 0;

                Application.Invoke (delegate {
                    Pixbuf = AnimationFrames [FrameNumber];
                });
            };

            return Animation;
        }


        // Creates the menu that is popped up when the
        // user clicks the status icon
        public void CreateMenu ()
        {
            Menu = new Menu ();

                // The menu item showing the status and size of the SparkleShare folder
                MenuItem status_menu_item = new MenuItem (StateText) {
                    Sensitive = false
                };

            Menu.Add (status_menu_item);
            Menu.Add (new SeparatorMenuItem ());

                ImageMenuItem folder_item = new SparkleMenuItem ("SparkleShare"){
                    Image = new Image (SparkleUIHelpers.GetIcon ("folder-sparkleshare", 16))
                };

                folder_item.Activated += delegate {
                    SparkleShare.Controller.OpenSparkleShareFolder ();
                };
                
            Menu.Add (folder_item);

                if (SparkleShare.Controller.Folders.Count > 0) {
            
                    // Creates a menu item for each repository with a link to their logs
                    foreach (string path in SparkleShare.Controller.Folders) {

                        Gdk.Pixbuf folder_icon = IconTheme.Default.LoadIcon ("folder", 16,
                            IconLookupFlags.GenericFallback);
                        
                        ImageMenuItem subfolder_item = new SparkleMenuItem (Path.GetFileName (path)) {
                            Image = new Image (folder_icon)
                        };

//                        if (repo.HasUnsyncedChanges)
//                            folder_action.IconName = "dialog-error";
                    
                        subfolder_item.Activated += OpenEventLogDelegate (path);

                        Menu.Add (subfolder_item);
                    }

                } else {
                    MenuItem no_folders_item = new MenuItem (_("No Remote Folders Yet")) {
                        Sensitive   = false
                    };

                    Menu.Add (no_folders_item);
                }

                // Opens the wizard to add a new remote folder
                MenuItem sync_item = new MenuItem (_("Add Remote Folder…"));
            
                if (SparkleShare.Controller.FirstRun)
                    sync_item.Sensitive = false;

                sync_item.Activated += delegate {
                    Application.Invoke (delegate {

                        if (SparkleUI.Intro == null) {
                            SparkleUI.Intro = new SparkleIntro ();
                            SparkleUI.Intro.ShowServerForm (true);
                        }
        
                        if (!SparkleUI.Intro.Visible)
                            SparkleUI.Intro.ShowServerForm (true);

                        SparkleUI.Intro.ShowAll ();
                        SparkleUI.Intro.Present ();
                    });
                };

            Menu.Add (sync_item);
            Menu.Add (new SeparatorMenuItem ());

                MenuItem notify_item;
                                                             
                if (SparkleShare.Controller.NotificationsEnabled)
                    notify_item = new MenuItem (_("Turn Notifications Off"));
                else
                    notify_item = new MenuItem (_("Turn Notifications On"));

                notify_item.Activated += delegate {
                    SparkleShare.Controller.ToggleNotifications ();
                    CreateMenu ();
                };

            Menu.Add (notify_item);
            Menu.Add (new SeparatorMenuItem ());

                // A menu item that takes the user to http://www.sparkleshare.org/
                MenuItem about_item = new MenuItem (_("About SparkleShare"));

                about_item.Activated += delegate {
                    SparkleAbout about = new SparkleAbout ();
                    about.ShowAll ();
                };

            Menu.Add (about_item);
            Menu.Add (new SeparatorMenuItem ());

                // A menu item that quits the application
                MenuItem quit_item = new MenuItem (_("Quit"));

                quit_item.Activated += delegate {
                    SparkleShare.Controller.Quit ();
                };

            Menu.Add (quit_item);
            Menu.ShowAll ();
        }


        // A method reference that makes sure that opening the
        // event log for each repository works correctly
        private EventHandler OpenEventLogDelegate (string path)
        {
            return delegate {
                SparkleShare.UI.AddEventLog (path);
            };
        }


        public void UpdateMenu ()
        {
            Menu.Remove (Menu.Children [0]);

                MenuItem status_menu_item = new MenuItem (StateText) {
                    Sensitive = false
                };

            Menu.Add (status_menu_item);
            Menu.ReorderChild (status_menu_item, 0);

            Menu.ShowAll ();
        }


        // Makes the menu visible
        private void ShowMenu (object o, EventArgs args)
        {
            Menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);
        }


        // Makes sure the menu pops up in the right position
        private void SetPosition (Menu menu, out int x, out int y, out bool push_in)
        {
            PositionMenu (menu, out x, out y, out push_in, Handle);
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

                Application.Invoke (delegate {
                    Pixbuf = AnimationFrames [0];
                });
            } else {
                if (error) {
                    StateText = _("Not everything is synced");

                    Application.Invoke (delegate {
                        Pixbuf = SparkleUIHelpers.GetIcon ("sparkleshare-syncing-error", 24);
                    });
                } else {
                    StateText = _("Up to date") + "  (" + SparkleShare.Controller.FolderSize + ")";
                    Application.Invoke (delegate {
                        Pixbuf = AnimationFrames [0];
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

    
    public class SparkleMenuItem : ImageMenuItem {

        public SparkleMenuItem (string text) : base (text)
        {
            SetProperty ("always-show-image", new GLib.Value (true));
        }
    }
}
