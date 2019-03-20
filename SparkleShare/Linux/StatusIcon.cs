//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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
using System.IO;

using Sparkles;
using Gtk;

#if HAVE_APP_INDICATOR
using AppIndicator3;
#endif

namespace SparkleShare {

    public class StatusIcon {

        public StatusIconController Controller = new StatusIconController ();
        public static bool use_appindicator = true;

        Gtk.StatusIcon status_icon;

        Menu menu;
        MenuItem recent_events_item;
        MenuItem quit_item;
        MenuItem state_item;
        SparkleMenuItem [] state_menu_items;

        #if HAVE_APP_INDICATOR
        Indicator indicator;
        #endif


        public StatusIcon ()
        {
            if (use_appindicator) {
                #if HAVE_APP_INDICATOR
                indicator = new Indicator ("sparkleshare", "sparkleshare", (int) IndicatorCategory.ApplicationStatus) {
                    IconName = UserInterface.APP_ID + "-symbolic",
                    Status   = (int) IndicatorStatus.Active
                };
                #endif

            } else {
                this.status_icon = new Gtk.StatusIcon { IconName = UserInterface.APP_ID };
                this.status_icon.Activate  += ShowMenu; // Primary mouse button click
                this.status_icon.PopupMenu += ShowMenu; // Secondary mouse button click
            }

            CreateMenu ();

            Controller.UpdateIconEvent += delegate (IconState state) {
                Application.Invoke (delegate {
                    string icon_name = "org.sparkleshare.SparkleShare";

                    if (state == IconState.SyncingUp)
                        icon_name += "-syncing-up";
                    else if (state == IconState.SyncingDown)
                        icon_name += "-syncing-down";
                    else if (state == IconState.Syncing)
                        icon_name += "-syncing";
                    else if (state == IconState.Error)
                        icon_name += "-syncing-error";

                    if (use_appindicator) {
                        icon_name += "-symbolic";

                        #if HAVE_APP_INDICATOR
                        indicator.IconName = icon_name;

                        // Hack to force update the status icon
                        indicator.Status = (int) IndicatorStatus.Attention;
                        indicator.Status = (int) IndicatorStatus.Active;
                        #endif

                    } else {
                        this.status_icon.IconName = icon_name;
                    }
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
            folder_item.Image = new Image (UserInterfaceHelpers.GetIcon (UserInterface.APP_ID, 16));
            folder_item.Submenu = new Menu ();

            this.menu.Add (this.state_item);
            this.menu.Add (new SeparatorMenuItem ());                
            this.menu.Add (folder_item);

            
            this.state_menu_items = new SparkleMenuItem [Controller.Projects.Length];

            if (Controller.Projects.Length > 0) {
                int i = 0;
                foreach (ProjectInfo project in Controller.Projects) {
                    SparkleMenuItem item = new SparkleMenuItem (project.Name);
                    Gdk.Pixbuf folder_icon = UserInterfaceHelpers.GetIcon ("folder", 16);

                    item.Submenu = new Menu ();

                    this.state_menu_items [i] = new SparkleMenuItem (project.StatusMessage) { Sensitive = false };

                    (item.Submenu as Menu).Add (this.state_menu_items [i]);

                    if (!use_appindicator)
                        (item.Submenu as Menu).Add (new SeparatorMenuItem ());

                    if (project.IsPaused) {
                        MenuItem resume_item;

                        if (project.UnsyncedChangesInfo.Count > 0) {
                            string icons_path = Path.Combine (UserInterface.AssetsPath, "icons", "hicolor", "12x12", "status");

                            foreach (KeyValuePair<string, string> pair in project.UnsyncedChangesInfo) {

                                var change_item = new SparkleMenuItem (pair.Key) {
                                    Sensitive = false
                                };

                                if (!use_appindicator) {
                                    string icon_path = Path.Combine (icons_path, pair.Value.Replace ("-12", ""));
                                    change_item.Image = new Image (icon_path);
                                }

                                (item.Submenu as Menu).Add (change_item);
                            }

                            if (!string.IsNullOrEmpty (project.MoreUnsyncedChanges)) {
                                (item.Submenu as Menu).Add (new MenuItem (project.MoreUnsyncedChanges) {
                                    Sensitive = false
                                });
                            }

                            if (!use_appindicator)
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
                            
                            MenuItem try_again_item = new MenuItem ("Retry Sync");
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
                    (folder_item.Submenu as Menu).Add (item);

                    i++;
                };
            }

            this.recent_events_item = new MenuItem ("Recent Changes…");
            this.recent_events_item.Sensitive = Controller.RecentEventsItemEnabled;

            if (!use_appindicator)
                (folder_item.Submenu as Menu).Add (new SeparatorMenuItem ());

            (folder_item.Submenu as Menu).Add (this.recent_events_item);


            this.quit_item    = new MenuItem ("Quit") { Sensitive = Controller.QuitItemEnabled };
            MenuItem add_item = new MenuItem ("Sync Remote Project…");

            MenuItem link_code_item = new MenuItem ("Computer ID");

            if (Controller.LinkCodeItemEnabled) {
                link_code_item.Submenu = new Menu ();
                
                string link_code = SparkleShare.Controller.UserAuthenticationInfo.PublicKey.Substring (0, 20) + "...";
                MenuItem code_item = new MenuItem (link_code) { Sensitive = false };
                
                MenuItem copy_item = new MenuItem ("Copy to Clipboard");
                copy_item.Activated += delegate { Controller.CopyToClipboardClicked (); };
                
                (link_code_item.Submenu as Menu).Add (code_item);

                if (!use_appindicator)
                    (link_code_item.Submenu as Menu).Add (new SeparatorMenuItem ());

                (link_code_item.Submenu as Menu).Add (copy_item);
            }

            MenuItem about_item = new MenuItem ("About SparkleShare");
            
            about_item.Activated              += delegate { Controller.AboutClicked (); };
            add_item.Activated                += delegate { Controller.AddHostedProjectClicked (); };
            this.recent_events_item.Activated += delegate { Controller.RecentEventsClicked (); };
            this.quit_item.Activated          += delegate { Controller.QuitClicked (); };

            this.menu.Add (new SeparatorMenuItem ());
            this.menu.Add (add_item);
            this.menu.Add (link_code_item);
            this.menu.Add (new SeparatorMenuItem ());            
            this.menu.Add (about_item);
            this.menu.Add (new SeparatorMenuItem ());            
            this.menu.Add (this.quit_item);
            this.menu.ShowAll ();

            if (use_appindicator) {
                #if HAVE_APP_INDICATOR
                indicator.Menu = this.menu;
                #endif
            }
        }
		

        // Makes the menu visible
        void ShowMenu (object o, EventArgs args)
        {
            this.menu.Popup (null, null, SetPosition, 0, Gtk.Global.CurrentEventTime);
        }


        // Makes sure the menu pops up in the right position
        void SetPosition (Menu menu, out int x, out int y, out bool push_in)
        {
            Gtk.StatusIcon.PositionMenu (menu, out x, out y, out push_in, this.status_icon.Handle);
        }
    }

    
    public class SparkleMenuItem : ImageMenuItem {

        public SparkleMenuItem (string text) : base (text)
        {
            SetProperty ("always-show-image", new GLib.Value (true));
        }
    }
}
