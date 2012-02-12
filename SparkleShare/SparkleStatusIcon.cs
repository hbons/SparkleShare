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

#if HAVE_APP_INDICATOR
using AppIndicator;
#endif
using Gtk;
using Mono.Unix;

namespace SparkleShare {

    // The statusicon that stays in the
    // user's notification area
    public class SparkleStatusIcon {

        public SparkleStatusIconController Controller = new SparkleStatusIconController ();

        private Timer animation;
        private Gdk.Pixbuf [] animation_frames;
        private int frame_number;
        private string state_text;
        private Menu menu;
        private MenuItem quit_item;

        #if HAVE_APP_INDICATOR
        private ApplicationIndicator indicator;
        #else
        private StatusIcon status_icon;
        #endif
        
        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleStatusIcon ()
        {
            CreateAnimationFrames ();
            CreateAnimation ();

            #if HAVE_APP_INDICATOR
            this.indicator = new ApplicationIndicator ("sparkleshare",
                "process-syncing-sparkleshare-i", Category.ApplicationStatus) {

                Status = Status.Attention
            };
            #else
            this.status_icon = new StatusIcon ();

            this.status_icon.Activate += ShowMenu; // Primary mouse button click
            this.status_icon.PopupMenu += ShowMenu; // Secondary mouse button click
            this.status_icon.Pixbuf = this.animation_frames [0];
            #endif

            if (Controller.Folders.Length == 0)
                this.state_text = _("Welcome to SparkleShare!");
            else
                this.state_text = _("Up to date") + Controller.FolderSize;

            CreateMenu ();


            Controller.UpdateQuitItemEvent += delegate (bool quit_item_enabled) {
                Application.Invoke (delegate {
                    if (this.quit_item != null) {
                        this.quit_item.Sensitive = quit_item_enabled;
                        this.menu.ShowAll ();
                    }
                });
            };

            Controller.UpdateMenuEvent += delegate (IconState state) {
                Application.Invoke (delegate {
                    switch (state) {
                    case IconState.Idle:

                        this.animation.Stop ();

                        if (Controller.Folders.Length == 0)
                            this.state_text = _("Welcome to SparkleShare!");
                        else
                            this.state_text = _("Up to date") + Controller.FolderSize;

                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-sparkleshare-i";
                        #else
                        this.status_icon.Pixbuf = this.animation_frames [0];
                        #endif

                        UpdateStateText ();
                        CreateMenu ();

                        break;

                    case IconState.Syncing:

                        this.state_text = _("Syncing… ") +
                                    Controller.ProgressPercentage + "%  " +
                                    Controller.ProgressSpeed;

                        UpdateStateText ();

                        if (!this.animation.Enabled)
                            this.animation.Start ();

                        break;

                    case IconState.Error:

                        this.animation.Stop ();

                        this.state_text = _("Not everything is synced");
                        UpdateStateText ();
                        CreateMenu ();

                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "sparkleshare-syncing-error";
                        #else
                        this.status_icon.Pixbuf = SparkleUIHelpers.GetIcon ("sparkleshare-syncing-error", 24);
                        #endif

                        break;
                    }

                    this.menu.ShowAll ();
                });
            };
        }


        // Slices up the graphic that contains the
        // animation frames.
        private void CreateAnimationFrames ()
        {
            this.animation_frames    = new Gdk.Pixbuf [5];
            Gdk.Pixbuf frames_pixbuf = SparkleUIHelpers.GetIcon ("process-syncing-sparkleshare", 24);
            
            for (int i = 0; i < this.animation_frames.Length; i++)
                animation_frames [i] = new Gdk.Pixbuf (frames_pixbuf, (i * 24), 0, 24, 24);
        }


        // Creates the animation that handles the syncing animation
        private void CreateAnimation ()
        {
            this.frame_number = 0;

            this.animation = new Timer () {
                Interval = 35
            };

            this.animation.Elapsed += delegate {
                if (this.frame_number < this.animation_frames.Length - 1)
                    this.frame_number++;
                else
                    this.frame_number = 0;

                string icon_name = "process-syncing-sparkleshare-"; 
                for (int i = 0; i <= this.frame_number; i++)
                    icon_name += "i";

                Application.Invoke (delegate {
                    #if HAVE_APP_INDICATOR
                    this.indicator.IconName = icon_name;
                    #else
                    this.status_icon.Pixbuf = this.animation_frames [this.frame_number];
                    #endif
                });
            };
        }


        // Creates the menu that is popped up when the
        // user clicks the status icon
        public void CreateMenu ()
        {
            this.menu = new Menu ();

                // The menu item showing the status and size of the SparkleShare folder
                MenuItem status_menu_item = new MenuItem (this.state_text) {
                    Sensitive = false
                };

            this.menu.Add (status_menu_item);
            this.menu.Add (new SeparatorMenuItem ());

                ImageMenuItem folder_item = new SparkleMenuItem ("SparkleShare"){
                    Image = new Image (SparkleUIHelpers.GetIcon ("folder-sparkleshare", 16))
                };

                folder_item.Activated += delegate {
                    Program.Controller.OpenSparkleShareFolder ();
                };
                
            this.menu.Add (folder_item);

                if (Program.Controller.Folders.Count > 0) {
            
                    // Creates a menu item for each repository with a link to their logs
                    foreach (string folder_name in Program.Controller.Folders) {
                        Gdk.Pixbuf folder_icon;

                        if (Program.Controller.UnsyncedFolders.Contains (folder_name)) {
                            folder_icon = IconTheme.Default.LoadIcon ("dialog-error", 16,
                                IconLookupFlags.GenericFallback);

                        } else {
                            folder_icon = IconTheme.Default.LoadIcon ("folder", 16,
                                IconLookupFlags.GenericFallback);
                        }

                        ImageMenuItem subfolder_item = new SparkleMenuItem (folder_name) {
                            Image = new Image (folder_icon)
                        };

                        subfolder_item.Activated += OpenFolderDelegate (folder_name);
                        this.menu.Add (subfolder_item);
                    }

                } else {
                    MenuItem no_folders_item = new MenuItem (_("No projects yet")) {
                        Sensitive   = false
                    };

                    this.menu.Add (no_folders_item);
                }

                this.menu.Add (new SeparatorMenuItem ());

                // Opens the wizard to add a new remote folder
                MenuItem sync_item = new MenuItem (_("Add Hosted Project…"));
            
                if (Program.Controller.FirstRun)
                    sync_item.Sensitive = false;

                sync_item.Activated += delegate {
                    Application.Invoke (delegate {

                        if (SparkleUI.Setup == null) {
                            SparkleUI.Setup = new SparkleSetup ();
                            SparkleUI.Setup.Controller.ShowAddPage ();
                        }
        
                        if (!SparkleUI.Setup.Visible)
                            SparkleUI.Setup.Controller.ShowAddPage ();

                        //SparkleUI.Intro.ShowAll ();
                        //SparkleUI.Intro.Present ();
                    });
                };

            this.menu.Add (sync_item);
            this.menu.Add (new SeparatorMenuItem ());

            MenuItem recent_events_item = new MenuItem (_("Open Recent Events"));

                recent_events_item.Sensitive = (Controller.Folders.Length > 0);

                recent_events_item.Activated += delegate {
                    Application.Invoke (delegate {
                        if (SparkleUI.EventLog == null)
                            SparkleUI.EventLog = new SparkleEventLog ();

                        SparkleUI.EventLog.ShowAll ();
                        SparkleUI.EventLog.Present ();
                    });
                };

            this.menu.Add (recent_events_item);

            MenuItem notify_item;
                                                             
                if (Program.Controller.NotificationsEnabled)
                    notify_item = new MenuItem (_("Turn Notifications Off"));
                else
                    notify_item = new MenuItem (_("Turn Notifications On"));

                notify_item.Activated += delegate {
                    Program.Controller.ToggleNotifications ();
                    CreateMenu ();
                };

            this.menu.Add (notify_item);
            this.menu.Add (new SeparatorMenuItem ());

                // A menu item that takes the user to http://www.sparkleshare.org/
                MenuItem about_item = new MenuItem (_("About SparkleShare"));

                about_item.Activated += delegate {
                    Application.Invoke (delegate {
                        if (SparkleUI.About == null)
                            SparkleUI.About = new SparkleAbout ();

                        SparkleUI.About.ShowAll ();
                        SparkleUI.About.Present ();
                    });
                };

            this.menu.Add (about_item);
            this.menu.Add (new SeparatorMenuItem ());

                // A menu item that quits the application
                this.quit_item = new MenuItem (_("Quit")) {
                    Sensitive = Controller.QuitItemEnabled
                };

                this.quit_item.Activated += delegate {
                    Program.Controller.Quit ();
                };

            this.menu.Add (this.quit_item);
            this.menu.ShowAll ();

            #if HAVE_APP_INDICATOR
            this.indicator.Menu = this.menu;
            #endif
        }


        // A method reference that makes sure that opening the
        // event log for each repository works correctly
        private EventHandler OpenFolderDelegate (string name)
        {
            return delegate {
                Program.Controller.OpenSparkleShareFolder (name);
            };
        }


        public void UpdateStateText ()
        {
            ((this.menu.Children [0] as MenuItem).Child as Label).Text = this.state_text;
            this.menu.ShowAll ();
        }

        #if !HAVE_APP_INDICATOR
        // Makes the menu visible
        private void ShowMenu (object o, EventArgs args)
        {
            this.menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);
        }


        // Makes sure the menu pops up in the right position
        private void SetPosition (Menu menu, out int x, out int y, out bool push_in)
        {
            StatusIcon.PositionMenu (menu, out x, out y, out push_in, this.status_icon.Handle);
        }
        #endif
    }

    
    public class SparkleMenuItem : ImageMenuItem {

        public SparkleMenuItem (string text) : base (text)
        {
            SetProperty ("always-show-image", new GLib.Value (true));
        }
    }
}
