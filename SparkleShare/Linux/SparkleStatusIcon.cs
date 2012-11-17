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

using Gtk;
#if HAVE_APP_INDICATOR
using AppIndicator;
#endif

namespace SparkleShare {

    public class SparkleStatusIcon {

        public SparkleStatusIconController Controller = new SparkleStatusIconController ();

        private Menu menu;
        private MenuItem recent_events_item;
        private MenuItem quit_item;
        private MenuItem state_item;

        #if HAVE_APP_INDICATOR
        private ApplicationIndicator indicator;
        #else
        private StatusIcon status_icon;

        private Gdk.Pixbuf syncing_idle_image  = SparkleUIHelpers.GetIcon ("sparkleshare", 24);
        private Gdk.Pixbuf syncing_up_image    = SparkleUIHelpers.GetIcon ("process-syncing-up", 24);
        private Gdk.Pixbuf syncing_down_image  = SparkleUIHelpers.GetIcon ("process-syncing-down", 24);
        private Gdk.Pixbuf syncing_image       = SparkleUIHelpers.GetIcon ("process-syncing", 24);
        private Gdk.Pixbuf syncing_error_image = SparkleUIHelpers.GetIcon ("process-syncing-error", 24);
        #endif


        public SparkleStatusIcon ()
        {
            #if HAVE_APP_INDICATOR
            this.indicator = new ApplicationIndicator ("sparkleshare", "sparkleshare", Category.ApplicationStatus);
            this.indicator.IconName = "process-syncing-idle";
            this.indicator.Status   = Status.Active;
            #else
			this.status_icon        = new StatusIcon ();
            this.status_icon.Pixbuf = this.syncing_idle_image;

            this.status_icon.Activate  += ShowMenu; // Primary mouse button click
            this.status_icon.PopupMenu += ShowMenu; // Secondary mouse button click
            #endif

            CreateMenu ();

            Controller.UpdateIconEvent += delegate (IconState state) {
                Application.Invoke (delegate {
                    switch (state) {
                    case IconState.Idle: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-idle";
                        #else
                        this.status_icon.Pixbuf = this.syncing_idle_image;
                        #endif
                        break;
                    }
                    case IconState.SyncingUp: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-up";
                        #else
                        this.status_icon.Pixbuf = this.syncing_up_image;
                        #endif
                        break;
                    }
                    case IconState.SyncingDown: {                   
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-down";
                        #else
                        this.status_icon.Pixbuf = this.syncing_down_image;
                        #endif
                        break;
                    }
                    case IconState.Syncing: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing";
                        #else
                        this.status_icon.Pixbuf = this.syncing_image;
                        #endif
                        break;
                    }
                    case IconState.Error: {
                        #if HAVE_APP_INDICATOR
                        this.indicator.IconName = "process-syncing-error";
                        #else
                        this.status_icon.Pixbuf = this.syncing_error_image;
                        #endif
                        break;
                    }
                    }

                    #if HAVE_APP_INDICATOR
                    // Force update of the status icon
                    this.indicator.Status = Status.Attention;
                    this.indicator.Status = Status.Active;
                    #endif
                });
            };

            Controller.UpdateStatusItemEvent += delegate (string state_text) {
                Application.Invoke (delegate {
                    (this.state_item.Child as Label).Text = state_text;
                    this.state_item.ShowAll ();
                });
            };

            Controller.UpdateQuitItemEvent += delegate (bool item_enabled) {
                Application.Invoke (delegate {
                    this.quit_item.Sensitive = item_enabled;
                    this.quit_item.ShowAll ();
                });
            };

            Controller.UpdateMenuEvent += delegate (IconState state) {
                Application.Invoke (delegate {
                    CreateMenu ();
                });
            };
        }


        public void CreateMenu ()
        {
            this.menu = new Menu ();

                this.state_item = new MenuItem (Controller.StateText) {
                    Sensitive = false
                };

            this.menu.Add (this.state_item);
            this.menu.Add (new SeparatorMenuItem ());

                ImageMenuItem folder_item = new SparkleMenuItem ("SparkleShare"){
                    Image = new Image (SparkleUIHelpers.GetIcon ("sparkleshare", 16))
                };

                folder_item.Activated += delegate {
                    Controller.SparkleShareClicked ();
                };
                
            this.menu.Add (folder_item);

                if (Program.Controller.Folders.Count > 0) {
                    int i = 0;
                    foreach (string folder_name in Controller.Folders) {
                        ImageMenuItem item = new SparkleMenuItem (folder_name);
                        Gdk.Pixbuf folder_icon;

                        if (!string.IsNullOrEmpty (Controller.FolderErrors [i])) {
                            folder_icon = IconTheme.Default.LoadIcon ("dialog-warning", 16,
                                IconLookupFlags.GenericFallback);

                            item.Submenu = new Menu ();
                                
                            MenuItem error_item = new MenuItem (Controller.FolderErrors [i]) {
                                Sensitive = false
                            };
                            
                            (item.Submenu as Menu).Add (error_item);

                        } else {
                            folder_icon = IconTheme.Default.LoadIcon ("folder", 16,
                                IconLookupFlags.GenericFallback);
                        }

                        item.Image = new Image (folder_icon);
                        (item.Child as Label).UseUnderline = false;

                        item.Activated += OpenFolderDelegate (folder_name);
                        this.menu.Add (item);

                        i++;
                    }

                    Menu submenu = new Menu ();
                }

                this.menu.Add (new SeparatorMenuItem ());
			
			this.recent_events_item = new MenuItem ("Recent Changes…");
			
				this.recent_events_item.Sensitive = Controller.RecentEventsItemEnabled;
				
				this.recent_events_item.Activated += delegate {
					Controller.RecentEventsClicked ();
				};
			
			this.menu.Add (this.recent_events_item);


                MenuItem sync_item = new MenuItem ("Add Hosted Project…");

                sync_item.Activated += delegate {
                    Controller.AddHostedProjectClicked ();
                };

            this.menu.Add (sync_item);
            this.menu.Add (new SeparatorMenuItem ());

            
            MenuItem notify_item;
                                                             
                if (Program.Controller.NotificationsEnabled)
                    notify_item = new MenuItem ("Turn Notifications Off");
                else
                    notify_item = new MenuItem ("Turn Notifications On");

                notify_item.Activated += delegate {
					Application.Invoke (delegate {
	                    Program.Controller.ToggleNotifications ();
					
					    if (Program.Controller.NotificationsEnabled)
	                    	(notify_item.Child as Label).Text = "Turn Notifications Off";
	                	else
	                    	(notify_item.Child as Label).Text = "Turn Notifications On";
					});
                };

            this.menu.Add (notify_item);
            this.menu.Add (new SeparatorMenuItem ());

                MenuItem about_item = new MenuItem ("About SparkleShare");

                about_item.Activated += delegate {
                    Controller.AboutClicked ();
                };

            this.menu.Add (about_item);
            this.menu.Add (new SeparatorMenuItem ());

                this.quit_item = new MenuItem ("Quit") {
                    Sensitive = Controller.QuitItemEnabled
                };

                this.quit_item.Activated += delegate {
                    Controller.QuitClicked ();
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
                Controller.SubfolderClicked (name);
            };
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
