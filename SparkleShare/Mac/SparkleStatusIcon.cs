//   SparkleShare, an instant update workflow to Git.
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
using System.Drawing;
using System.IO;
using System.Timers;

using Mono.Unix;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace SparkleShare {

    // The statusicon that stays in the
    // user's notification area
    public class SparkleStatusIcon : NSObject {

        public SparkleStatusIconController Controller = new SparkleStatusIconController ();

        private Timer Animation;
        private int FrameNumber;
        private string StateText;

        private NSStatusItem StatusItem;
        private NSMenu Menu;
        private NSMenuItem StateMenuItem;
        private NSMenuItem FolderMenuItem;
        private NSMenuItem [] FolderMenuItems;
        private NSMenuItem SyncMenuItem;
        private NSMenuItem AboutMenuItem;
        private NSMenuItem NotificationsMenuItem;
        private NSMenuItem RecentEventsMenuItem;
        
        private delegate void Task ();
        private EventHandler [] Tasks;

        
        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }

        
        public SparkleStatusIcon () : base ()
        {
            using (var a = new NSAutoreleasePool ()) {
                Animation = CreateAnimation ();

                StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
                StatusItem.HighlightMode = true;
    
                StateText = _("Up to date") + " (" + Controller.FolderSize + ")";
                CreateMenu ();
    
                Menu.Delegate = new SparkleStatusIconMenuDelegate ();
            }

            Controller.UpdateMenuEvent += delegate (IconState state) {
                InvokeOnMainThread (delegate {
                    using (var a = new NSAutoreleasePool ()) {
                        switch (state) {
                        case IconState.Idle:
    
                            Animation.Stop ();

                            if (Controller.Folders.Length == 0)
                                StateText = _("Welcome to SparkleShare!");
                            else
                                StateText = _("Up to date") + " (" + Controller.FolderSize + ")";
    
                            StateMenuItem.Title = StateText;
                            CreateMenu ();
    
                            break;
    
                        case IconState.Syncing:
    
                            StateText = _("Syncing…");
                            StateMenuItem.Title = StateText;
    
                            if (!Animation.Enabled)
                                Animation.Start ();
    
                            break;
    
                        case IconState.Error:
    
                            StateText = _("Not everything is synced");
                            StateMenuItem.Title = StateText;
                            CreateMenu ();
    
                            InvokeOnMainThread (delegate {
                                StatusItem.Image               = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/error.png");
                                StatusItem.AlternateImage      = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/error-active.png");
                                StatusItem.Image.Size          = new SizeF (16, 16);
                                StatusItem.AlternateImage.Size = new SizeF (16, 16);
                            });
                            break;
                        }
                    }
                });
            };
        }


        public void CreateMenu ()
        {
            using (NSAutoreleasePool a = new NSAutoreleasePool ()) {
                StatusItem.Image               = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/idle0.png");
                StatusItem.AlternateImage      = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/idle0-active.png");
                StatusItem.Image.Size          = new SizeF (16, 16);
                StatusItem.AlternateImage.Size = new SizeF (16, 16);
    
                Menu = new NSMenu ();
                
                    StateMenuItem = new NSMenuItem () {
                        Title = StateText
                    };
                
                Menu.AddItem (StateMenuItem);
                Menu.AddItem (NSMenuItem.SeparatorItem);
    
                    FolderMenuItem = new NSMenuItem () {
                        Title = "SparkleShare"
                    };
    
                    FolderMenuItem.Activated += delegate {
                        Program.Controller.OpenSparkleShareFolder ();
                    };
                
                    FolderMenuItem.Image = NSImage.ImageNamed ("sparkleshare-mac");
                    FolderMenuItem.Image.Size = new SizeF (16, 16);    
                
                Menu.AddItem (FolderMenuItem);
    
                    FolderMenuItems = new NSMenuItem [Program.Controller.Folders.Count];
    
                    if (Controller.Folders.Length > 0) {
                        Tasks = new EventHandler [Program.Controller.Folders.Count];
    
                        int i = 0;
                        foreach (string folder_name in Program.Controller.Folders) {
                            NSMenuItem item = new NSMenuItem ();
    
                            item.Title = folder_name;
    
                            if (Program.Controller.UnsyncedFolders.Contains (folder_name))
                                item.Image = NSImage.ImageNamed ("NSCaution");
                            else
                                item.Image = NSImage.ImageNamed ("NSFolder");
    
                            item.Image.Size = new SizeF (16, 16);
                            Tasks [i] = OpenFolderDelegate (folder_name);
    
                            FolderMenuItems [i] = item;
                            FolderMenuItems [i].Activated += Tasks [i];
    
                            i++;
                        };
    
                    } else {
                        FolderMenuItems = new NSMenuItem [1];
    
                        FolderMenuItems [0] = new NSMenuItem () {
                            Title = "No Remote Folders Yet"
                        };
                    }
    
                foreach (NSMenuItem item in FolderMenuItems)
                    Menu.AddItem (item);
                    
                Menu.AddItem (NSMenuItem.SeparatorItem);
    
                    SyncMenuItem = new NSMenuItem () {
                        Title = "Add Remote Folder…"
                    };
                
                    if (!Program.Controller.FirstRun) {
                        SyncMenuItem.Activated += delegate {
                            InvokeOnMainThread (delegate {
                                NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
    
                                if (SparkleUI.Setup == null) {
                                    SparkleUI.Setup = new SparkleSetup ();
                                    SparkleUI.Setup.Controller.ShowAddPage ();
                                }
    
                                if (!SparkleUI.Setup.IsVisible)
                                    SparkleUI.Setup.Controller.ShowAddPage ();
    
                                SparkleUI.Setup.OrderFrontRegardless ();
                                SparkleUI.Setup.MakeKeyAndOrderFront (this);
                            });
                        };
                    }
    
                Menu.AddItem (SyncMenuItem);
                Menu.AddItem (NSMenuItem.SeparatorItem);
    
                    RecentEventsMenuItem = new NSMenuItem () {
                        Title = "Show Recent Events"
                    };
    
                    if (Controller.Folders.Length > 0) {
                        RecentEventsMenuItem.Activated += delegate {
                            InvokeOnMainThread (delegate {
                                NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
        
                                if (SparkleUI.EventLog == null)
                                    SparkleUI.EventLog = new SparkleEventLog ();
        
                                SparkleUI.EventLog.OrderFrontRegardless ();
                                SparkleUI.EventLog.MakeKeyAndOrderFront (this);
                            });
                        };
                    }
    
                Menu.AddItem (RecentEventsMenuItem);
    
                    NotificationsMenuItem = new NSMenuItem ();
    
                    if (Program.Controller.NotificationsEnabled)
                        NotificationsMenuItem.Title = "Turn Notifications Off";
                    else
                        NotificationsMenuItem.Title = "Turn Notifications On";
    
                    NotificationsMenuItem.Activated += delegate {
                        Program.Controller.ToggleNotifications ();
    
                        InvokeOnMainThread (delegate {
                            if (Program.Controller.NotificationsEnabled)
                                NotificationsMenuItem.Title = "Turn Notifications Off";
                            else
                                NotificationsMenuItem.Title = "Turn Notifications On";
                        });
                    };
    
                Menu.AddItem (NotificationsMenuItem);
                Menu.AddItem (NSMenuItem.SeparatorItem);
    
                    AboutMenuItem = new NSMenuItem () {
                        Title = "About SparkleShare"
                    };
    
                    AboutMenuItem.Activated += delegate {
                        InvokeOnMainThread (delegate {
                            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
    
                            if (SparkleUI.About == null)
                                SparkleUI.About = new SparkleAbout ();
                            else
                                 SparkleUI.About.OrderFrontRegardless ();

                        });
                    };
    
    
                Menu.AddItem (AboutMenuItem);
    
                StatusItem.Menu = Menu;
                StatusItem.Menu.Update ();
            }
        }


        // A method reference that makes sure that opening the
        // event log for each repository works correctly
        private EventHandler OpenFolderDelegate (string name)
        {
            return delegate {
                Program.Controller.OpenSparkleShareFolder (name);
            };
        }


        // Creates the Animation that handles the syncing animation
        private Timer CreateAnimation ()
        {
            FrameNumber = 0;

            Timer Animation = new Timer () {
                Interval = 40
            };

            Animation.Elapsed += delegate {
                if (FrameNumber < 4)
                    FrameNumber++;
                else
                    FrameNumber = 0;

                InvokeOnMainThread (delegate {
                    string image_path =    Path.Combine (NSBundle.MainBundle.ResourcePath,
                        "Pixmaps", "idle" + FrameNumber + ".png");

                    string alternate_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                        "Pixmaps", "idle" + FrameNumber + "-active.png");

                    StatusItem.Image               = new NSImage (image_path);
                    StatusItem.AlternateImage      = new NSImage (alternate_image_path);
                    StatusItem.Image.Size          = new SizeF (16, 16);
                    StatusItem.AlternateImage.Size = new SizeF (16, 16);
                });
            };

            return Animation;
        }

    }
    
    
    public class SparkleStatusIconMenuDelegate : NSMenuDelegate {
        
        public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item) { }
    
        public override void MenuWillOpen (NSMenu menu)
        {
            InvokeOnMainThread (delegate {
                NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            });
        }
    }
}
