﻿﻿//   SparkleShare, an instant update workflow to Git.
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

using AppKit;
using CoreGraphics;

namespace SparkleShare {

    public class StatusIcon {

        public StatusIconController Controller = new StatusIconController ();

        private NSStatusItem status_item = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
        private NSMenu menu, submenu, link_code_submenu;
        private SparkleMenuDelegate menu_delegate;

        private NSMenuItem state_item, folder_item, add_item, about_item, recent_events_item, quit_item,
            code_item, copy_item, link_code_item;

        private NSMenuItem [] folder_menu_items, try_again_menu_items, pause_menu_items,
            resume_menu_items, state_menu_items;

        private NSImage syncing_idle_image  = NSImage.ImageNamed ("process-syncing-idle");
        private NSImage syncing_up_image    = NSImage.ImageNamed ("process-syncing-up");
        private NSImage syncing_down_image  = NSImage.ImageNamed ("process-syncing-down");
        private NSImage syncing_image       = NSImage.ImageNamed ("process-syncing");
        private NSImage syncing_error_image = NSImage.ImageNamed ("process-syncing-error");
        
        private NSImage folder_image  = NSImage.ImageNamed ("NSFolder");
        private NSImage caution_image = NSImage.ImageNamed ("NSCaution");
        private NSImage sparkleshare_image;


        public StatusIcon ()
        {
            this.status_item.HighlightMode  = true;
            this.status_item.Image          = this.syncing_idle_image;
            this.status_item.Image.Template = true;

            if (Environment.OSVersion.Version.Major >= 14)
                this.sparkleshare_image = (NSImage)NSImage.ImageNamed ("sparkleshare-folder-yosemite.icns").Copy();
            else
                this.sparkleshare_image = (NSImage)NSImage.ImageNamed ("sparkleshare-folder.icns").Copy();

            CreateMenu ();

            Controller.UpdateIconEvent += delegate (IconState state) {
                SparkleShare.Controller.Invoke (() => {
                    switch (state) {
                    case IconState.Idle: { this.status_item.Image = this.syncing_idle_image; break; }
                    case IconState.SyncingUp: { this.status_item.Image = this.syncing_up_image; break; }
                    case IconState.SyncingDown: { this.status_item.Image = this.syncing_down_image; break; }
                    case IconState.Syncing: { this.status_item.Image = this.syncing_image; break; }
                    case IconState.Error: { this.status_item.Image = this.syncing_error_image; break; }
                    }

                    this.status_item.Image.Template = true;
                });
            };
            
            Controller.UpdateStatusItemEvent += delegate (string state_text) {
                SparkleShare.Controller.Invoke (() => {
                    this.state_item.Title = state_text;

                    if (Controller.Projects.Length == this.state_menu_items.Length) {
                        for (int i = 0; i < Controller.Projects.Length; i++)
                            this.state_menu_items [i].Title = Controller.Projects [i].StatusMessage;
                    }
                });
            };

            Controller.UpdateMenuEvent += delegate {
                while (this.menu_delegate.MenuIsOpen)
                    System.Threading.Thread.Sleep (100);

                SparkleShare.Controller.Invoke (() => CreateMenu ());
            };

            Controller.UpdateQuitItemEvent += delegate (bool quit_item_enabled) {
                SparkleShare.Controller.Invoke (() => { this.quit_item.Enabled = quit_item_enabled; });
            };
        }


        public void CreateMenu ()
        {
            this.menu = new NSMenu () { AutoEnablesItems = false };

            this.state_item = new NSMenuItem () {
                Title   = Controller.StateText,
                Enabled = false
            };

            this.folder_item = new NSMenuItem () {
                Title   = "SparkleShare",
                Enabled = true
            };

            this.folder_item.Image = this.sparkleshare_image;
            this.folder_item.Image.Size = new CGSize (16, 16);

            this.add_item = new NSMenuItem () {
                Title   = "Sync Remote Project…",
                Enabled = true
            };

            this.recent_events_item = new NSMenuItem () {
                Title   = "Recent Changes…",
                Enabled = Controller.RecentEventsItemEnabled
            };

            this.link_code_item = new NSMenuItem ();
            this.link_code_item.Title = "Computer ID";

            if (Controller.LinkCodeItemEnabled) {
                this.link_code_submenu = new NSMenu ();

                this.code_item = new NSMenuItem ();
                this.code_item.Title = SparkleShare.Controller.UserAuthenticationInfo.PublicKey.Substring (0, 20) + "...";

                this.copy_item = new NSMenuItem ();
                this.copy_item.Title = "Copy to Clipboard";
                this.copy_item.Activated += delegate { Controller.CopyToClipboardClicked (); };

                this.link_code_submenu.AddItem (this.code_item);
                this.link_code_submenu.AddItem (NSMenuItem.SeparatorItem);
                this.link_code_submenu.AddItem (this.copy_item);
            
                this.link_code_item.Submenu = this.link_code_submenu;
            }

            this.about_item = new NSMenuItem () {
                Title   = "About SparkleShare",
                Enabled = true
            };

            this.quit_item = new NSMenuItem () {
                Title   = "Quit",
                Enabled = Controller.QuitItemEnabled
            };

            this.folder_menu_items    = new NSMenuItem [Controller.Projects.Length];
            this.try_again_menu_items = new NSMenuItem [Controller.Projects.Length];
            this.pause_menu_items     = new NSMenuItem [Controller.Projects.Length];
            this.resume_menu_items    = new NSMenuItem [Controller.Projects.Length];
            this.state_menu_items     = new NSMenuItem [Controller.Projects.Length];

            if (Controller.Projects.Length > 0) {
                int i = 0;
                foreach (ProjectInfo project in Controller.Projects) {
                    NSMenuItem item = new NSMenuItem () { Title = project.Name };
                    this.folder_menu_items [i] = item;

                    item.Submenu = new NSMenu ();
                    item.Image   = this.folder_image;

                    this.state_menu_items [i] = new NSMenuItem (project.StatusMessage);

                    item.Submenu.AddItem (this.state_menu_items [i]);
                    item.Submenu.AddItem (NSMenuItem.SeparatorItem);

                    if (project.IsPaused) {
                        if (project.UnsyncedChangesInfo.Count > 0) {
                            foreach (KeyValuePair<string, string> pair in project.UnsyncedChangesInfo)
                                item.Submenu.AddItem (new NSMenuItem (pair.Key) {
                                    Image = NSImage.ImageNamed (pair.Value)
                                });

                            if (!string.IsNullOrEmpty (project.MoreUnsyncedChanges))
                                item.Submenu.AddItem (new NSMenuItem (project.MoreUnsyncedChanges));

                            item.Submenu.AddItem (NSMenuItem.SeparatorItem);
                            this.resume_menu_items [i] = new NSMenuItem ("Sync and Resume…"); 
                        
                        } else {
                            this.resume_menu_items [i] = new NSMenuItem ("Resume");
                        }

                        this.resume_menu_items [i].Activated += Controller.ResumeDelegate (project.Name);
                        item.Submenu.AddItem (this.resume_menu_items [i]);
                    
                    } else {
                        if (Controller.Projects [i].HasError) {
                            item.Image = this.caution_image;
                            
                            this.try_again_menu_items [i]           = new NSMenuItem ();
                            this.try_again_menu_items [i].Title     = "Retry Sync";
                            this.try_again_menu_items [i].Activated += Controller.TryAgainDelegate (project.Name);

                            item.Submenu.AddItem (this.try_again_menu_items [i]);
                            
                        } else {
                            this.pause_menu_items [i] = new NSMenuItem ("Pause"); 
                            this.pause_menu_items [i].Activated += Controller.PauseDelegate (project.Name);

                            item.Submenu.AddItem (this.pause_menu_items [i]);
                        }
                    }

                    if (!Controller.Projects [i].HasError)
                        this.folder_menu_items [i].Activated += Controller.OpenFolderDelegate (project.Name);

                    item.Image.Size = new CGSize (16, 16);
                    i++;
                };
            }

            
            if (Controller.RecentEventsItemEnabled)
                this.recent_events_item.Activated += delegate { Controller.RecentEventsClicked (); };

            this.add_item.Activated   += delegate { Controller.AddHostedProjectClicked (); };
            this.about_item.Activated += delegate { Controller.AboutClicked (); };
            this.quit_item.Activated  += delegate { Controller.QuitClicked (); };


            this.menu.AddItem (this.state_item);
            this.menu.AddItem (NSMenuItem.SeparatorItem);
            this.menu.AddItem (this.folder_item);
            
            this.submenu = new NSMenu ();

            this.submenu.AddItem (this.recent_events_item);
            this.submenu.AddItem (this.add_item);
            this.submenu.AddItem (NSMenuItem.SeparatorItem);
            this.submenu.AddItem (link_code_item);
            this.submenu.AddItem (NSMenuItem.SeparatorItem);
            this.submenu.AddItem (this.about_item);
            
            this.folder_item.Submenu = this.submenu;

            foreach (NSMenuItem item in this.folder_menu_items)
                this.menu.AddItem (item);

            this.menu.AddItem (NSMenuItem.SeparatorItem);
            this.menu.AddItem (this.quit_item);

            this.menu_delegate    = new SparkleMenuDelegate ();
            this.menu.Delegate    = this.menu_delegate;
            this.status_item.Menu = this.menu;
        }
    

        private class SparkleMenuDelegate : NSMenuDelegate {
            
            public SparkleMenuDelegate (IntPtr handle) : base (handle) { }
            public SparkleMenuDelegate () { }

            public bool MenuIsOpen { get; private set; }

            
            public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
            {
            }


            public override void MenuWillOpen (NSMenu menu)
            {
                MenuIsOpen = true;
            }


            public override void MenuDidClose (NSMenu menu)
            {
                MenuIsOpen = false;
            }
        }
    }
}
