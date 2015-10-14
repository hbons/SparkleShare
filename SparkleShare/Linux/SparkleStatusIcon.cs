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
        private SparkleMenuItem [] state_menu_items;

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

                    if (Controller.Projects.Length == this.state_menu_items.Length) {
                        for (int i = 0; i < Controller.Projects.Length; i++) {
                            (this.state_menu_items [i].Child as Label).Text = Controller.Projects [i].StatusMessage;
                            this.state_menu_items [i].ShowAll ();
                        }
                    }
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

            
            this.state_menu_items = new SparkleMenuItem [Controller.Projects.Length];

            if (Controller.Projects.Length > 0) {
                int i = 0;
                foreach (ProjectInfo project in Controller.Projects) {
                    SparkleMenuItem item = new SparkleMenuItem (project.Name);

                    Gdk.Pixbuf folder_icon;
                    folder_icon = IconTheme.Default.LoadIcon ("folder", 16, IconLookupFlags.GenericFallback);

                    item.Submenu = new Menu ();

                    this.state_menu_items [i] = new SparkleMenuItem (project.StatusMessage) { Sensitive = false };

                    (item.Submenu as Menu).Add (this.state_menu_items [i]);
                    (item.Submenu as Menu).Add (new SeparatorMenuItem ());

                    if (project.IsPaused) {
                        MenuItem resume_item;

                        if (project.UnsyncedChangesInfo.Count > 0) {
                            string icons_path = new string [] {
                                SparkleUI.AssetsPath, "icons", "hicolor", "12x12", "status"}.Combine ();

                            foreach (KeyValuePair<string, string> pair in project.UnsyncedChangesInfo) {
                                string icon_path = new string [] {
                                    icons_path, pair.Value.Replace ("-12", "")}.Combine ();

                                (item.Submenu as Menu).Add (new SparkleMenuItem (pair.Key) {
                                    Image     = new Image (icon_path),
                                    Sensitive = false
                                });
                            }

                            if (!string.IsNullOrEmpty (project.MoreUnsyncedChanges)) {
                                (item.Submenu as Menu).Add (new MenuItem (project.MoreUnsyncedChanges) {
                                    Sensitive = false
                                });
                            }
                            
                            (item.Submenu as Menu).Add (new SeparatorMenuItem ());
                            resume_item = new MenuItem ("Sync and Resume…"); 
                            
                        } else {
                            resume_item = new MenuItem ("Resume");
                        }
                        
                        resume_item.Activated += Controller.ResumeDelegate (project.Name);
                        (item.Submenu as Menu).Add (resume_item);
                        
                    } else {
                        if (Controller.Projects [i].HasError) {
                            folder_icon = IconTheme.Default.LoadIcon ("dialog-warning", 16, IconLookupFlags.GenericFallback);
                            
                            MenuItem try_again_item = new MenuItem ("Try Again");
                            try_again_item.Activated += Controller.TryAgainDelegate (project.Name);
                            (item.Submenu as Menu).Add (try_again_item);

                        } else {
                            MenuItem pause_item = new MenuItem ("Pause");
                            pause_item.Activated += Controller.PauseDelegate (project.Name);
                            (item.Submenu as Menu).Add (pause_item);
                        }
                    }

                    (item.Child as Label).UseUnderline = false;
                    item.Image = new Image (folder_icon);
                    this.menu.Add (item);

                    i++;
                };
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
