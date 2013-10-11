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
using AppIndicator3;
#endif

namespace SparkleShare {

    public class SparkleStatusIcon {

        public SparkleStatusIconController Controller = new SparkleStatusIconController ();

        private Menu menu;
        private MenuItem recent_events_item;
        private MenuItem quit_item;
        private MenuItem state_item;

        #if HAVE_APP_INDICATOR
        private Indicator indicator;
        #else
        private StatusIcon status_icon;
        #endif


        public SparkleStatusIcon ()
        {
            #if HAVE_APP_INDICATOR
            this.indicator = new Indicator ("sparkleshare", "sparkleshare", (int) IndicatorCategory.ApplicationStatus);
            this.indicator.IconName = "process-syncing-idle";
            this.indicator.Status   = (int) IndicatorStatus.Active;
            #else
			this.status_icon          = new StatusIcon ();
            this.status_icon.IconName = "sparkleshare";

            this.status_icon.Activate  += ShowMenu; // Primary mouse button click
            this.status_icon.PopupMenu += ShowMenu; // Secondary mouse button click
            #endif

            CreateMenu ();

            Controller.UpdateIconEvent += delegate (IconState state) {
                Application.Invoke (delegate {
                    #if HAVE_APP_INDICATOR
                    string icon_name = "process-syncing-idle";
                    #else
                    string icon_name = "sparkleshare";
                    #endif

                    if (state == IconState.SyncingUp)
                        icon_name = "process-syncing-up";
                    else if (state == IconState.SyncingDown)
                        icon_name = "process-syncing-down";
                    else if (state == IconState.Syncing)
                        icon_name = "process-syncing";
                    else if (state == IconState.Error)
                        icon_name = "process-syncing-error";

                    #if HAVE_APP_INDICATOR
                    this.indicator.IconName = icon_name;

                    // Force update of the status icon
                    this.indicator.Status = (int) IndicatorStatus.Attention;
                    this.indicator.Status = (int) IndicatorStatus.Active;
                    #else
                    this.status_icon.IconName = icon_name;
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
                Application.Invoke (delegate { CreateMenu (); });
            };
        }


        public void CreateMenu ()
        {
            this.menu       = new Menu ();
            this.state_item = new MenuItem (Controller.StateText) { Sensitive = false };

            ImageMenuItem folder_item = new SparkleMenuItem ("SparkleShare");
            folder_item.Image = new Image (SparkleUIHelpers.GetIcon ("sparkleshare", 16));

            this.menu.Add (this.state_item);
            this.menu.Add (new SeparatorMenuItem ());                
            this.menu.Add (folder_item);

            if (Program.Controller.Folders.Count > 0) {
                int i = 0;
                foreach (string folder_name in Controller.Folders) {
                    ImageMenuItem item = new SparkleMenuItem (folder_name);
                    Gdk.Pixbuf folder_icon;

                    if (!string.IsNullOrEmpty (Controller.FolderErrors [i])) {
                        folder_icon = IconTheme.Default.LoadIcon ("dialog-warning", 16, IconLookupFlags.GenericFallback);
                        item.Submenu = new Menu ();
                            
                        MenuItem error_item = new MenuItem (Controller.FolderErrors [i]) { Sensitive = false };
                        MenuItem try_again_item = new MenuItem ("Try Again");
                        try_again_item.Activated += Controller.TryAgainDelegate (folder_name);

                        (item.Submenu as Menu).Add (error_item);
                        (item.Submenu as Menu).Add (new SeparatorMenuItem ());
                        (item.Submenu as Menu).Add (try_again_item);

                    } else {
                        folder_icon = IconTheme.Default.LoadIcon ("folder", 16, IconLookupFlags.GenericFallback);
                        item.Activated += Controller.OpenFolderDelegate (folder_name);
                    }

                    (item.Child as Label).UseUnderline = false;
                    item.Image = new Image (folder_icon);
                    this.menu.Add (item);
                    
                    i++;
                }
            }

			this.recent_events_item = new MenuItem ("Recent Changes…");
			this.recent_events_item.Sensitive = Controller.RecentEventsItemEnabled;
            this.quit_item    = new MenuItem ("Quit") { Sensitive = Controller.QuitItemEnabled };
            MenuItem add_item = new MenuItem ("Add Hosted Project…");

            #if HAVE_APP_INDICATOR
            MenuItem notify_item;
                                                             
            if (Program.Controller.NotificationsEnabled)
                notify_item = new MenuItem ("Turn Notifications Off");
            else
                notify_item = new MenuItem ("Turn Notifications On");

            notify_item.Activated += delegate {
                Program.Controller.ToggleNotifications ();

				Application.Invoke (delegate {				
				    if (Program.Controller.NotificationsEnabled)
                    	(notify_item.Child as Label).Text = "Turn Notifications Off";
                	else
                    	(notify_item.Child as Label).Text = "Turn Notifications On";
				});
            };
            #endif

            MenuItem link_code_item = new MenuItem ("Client ID");
            
            if (Controller.LinkCodeItemEnabled) {
                link_code_item.Submenu = new Menu ();
                
                string link_code = Program.Controller.CurrentUser.PublicKey.Substring (0, 20) + "...";
                MenuItem code_item = new MenuItem (link_code) { Sensitive = false };
                
                MenuItem copy_item = new MenuItem ("Copy to Clipboard");
                copy_item.Activated += delegate { Controller.CopyToClipboardClicked (); };
                
                (link_code_item.Submenu as Menu).Add (code_item);
                (link_code_item.Submenu as Menu).Add (new SeparatorMenuItem ());
                (link_code_item.Submenu as Menu).Add (copy_item);
            }

            MenuItem about_item = new MenuItem ("About SparkleShare");
            
            about_item.Activated              += delegate { Controller.AboutClicked (); };
            add_item.Activated                += delegate { Controller.AddHostedProjectClicked (); };
			this.recent_events_item.Activated += delegate { Controller.RecentEventsClicked (); };
            this.quit_item.Activated          += delegate { Controller.QuitClicked (); };

            folder_item.Submenu = new Menu ();
			(folder_item.Submenu as Menu).Add (this.recent_events_item);
            (folder_item.Submenu as Menu).Add (add_item);
            #if HAVE_APP_INDICATOR
            (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());
            (folder_item.Submenu as Menu).Add (notify_item);
            #endif
            (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());
            (folder_item.Submenu as Menu).Add (link_code_item);
            (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());
            (folder_item.Submenu as Menu).Add (about_item);

            this.menu.Add (new SeparatorMenuItem ());            
            this.menu.Add (this.quit_item);
            this.menu.ShowAll ();

            #if HAVE_APP_INDICATOR
            this.indicator.Menu = this.menu;
            #endif
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
