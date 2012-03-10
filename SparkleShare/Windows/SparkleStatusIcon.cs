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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Forms = System.Windows.Forms;

namespace SparkleShare {

    public class SparkleStatusIcon : Control {
        
        public SparkleStatusIconController Controller = new SparkleStatusIconController();

        private Forms.Timer Animation;
        private Bitmap [] AnimationFrames;
        private Bitmap ErrorIcon;
        private int FrameNumber;
        private string StateText;
        private ContextMenu context_menu;
        private SparkleMenuItem status_item;
        private SparkleMenuItem exit_item;
        
        private SparkleNotifyIcon notify_icon = new SparkleNotifyIcon ();

        
        // Short alias for the translations
        public static string _ (string s)
        {
            return Program._ (s);
        }
        
        
        public SparkleStatusIcon ()
		{
            AnimationFrames = CreateAnimationFrames ();
            Animation       = CreateAnimation ();
			ErrorIcon       = SparkleUIHelpers.GetBitmap ("sparkleshare-syncing-error-windows");

			this.notify_icon.Icon = AnimationFrames [0];
            this.notify_icon.HeaderText = "SparkleShare";
			
            if (Controller.Folders.Length == 0)
                StateText = _("Welcome to SparkleShare!");
            else
                this.notify_icon.Text = StateText = _("Files up to date") + Controller.FolderSize;

            CreateMenu ();
            
            
            Controller.UpdateQuitItemEvent += delegate (bool enable) {
                  Dispatcher.Invoke ((Action) delegate {
                    this.exit_item.IsEnabled = enable;
                    this.exit_item.UpdateLayout ();
                });
            };

            
            Controller.UpdateMenuEvent += delegate (IconState state) {
                Dispatcher.Invoke ((Action) delegate {
                        switch (state) {
                        case IconState.Idle: {
    
                            Animation.Stop ();
                        
                            if (Controller.Folders.Length == 0)
                                this.notify_icon.Text = StateText = "Welcome to SparkleShare!";
                            else
                                this.notify_icon.Text = StateText = "Files up to date" + Controller.FolderSize;
    
                        
                            this.status_item.Header = StateText;
                            this.notify_icon.Icon = AnimationFrames [0];
                        
                            CreateMenu ();
    
                            break;
                        }
                        
                        case IconState.Syncing: {
    
                            this.notify_icon.Text = StateText = "Syncing… " +
                            	Controller.ProgressPercentage + "%  " +
                                Controller.ProgressSpeed;

                            this.status_item.Header = StateText;
    
                            if (!Animation.Enabled)
                                Animation.Start ();
    
                            break;
                        }
    
                        case IconState.Error: {

                            Animation.Stop ();
    
                            this.notify_icon.Text = StateText = _("Not everything is synced");
                            this.status_item.Header = StateText;
                            CreateMenu ();

                            this.notify_icon.Icon = ErrorIcon;
                            
                            break;
                        }
                        }
                    
                        this.status_item.UpdateLayout ();
                    });
            };
        }

        
        private Bitmap [] CreateAnimationFrames ()
        {
            return new Bitmap [] {
	            SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-i"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-ii"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-iii"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-iiii"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-iiiii")
			};
        }


        // Creates the Animation that handles the syncing animation
        private Forms.Timer CreateAnimation ()
        {
            FrameNumber = 0;

            Forms.Timer Animation = new Forms.Timer () {
                Interval = 35
            };

            Animation.Tick += delegate {
                if (FrameNumber < AnimationFrames.Length - 1)
                    FrameNumber++;
                else
                    FrameNumber = 0;

                Dispatcher.Invoke ((Action) delegate {
                    this.notify_icon.Icon = AnimationFrames [FrameNumber];
                });
            };

            return Animation;
        }


        public void CreateMenu ()
        {
            this.context_menu = new ContextMenu ();

            status_item = new SparkleMenuItem () {
                Header    = StateText,
                IsEnabled = false
            };
            
            System.Windows.Controls.Image i = new System.Windows.Controls.Image();
            i.Source = SparkleUIHelpers.GetImageSource ("folder-sparkleshare-windows-16");
                i.Width = 16;
            i.Height = 16;
            
            SparkleMenuItem folder_item = new SparkleMenuItem () {
                Header = "SparkleShare",
                Icon   = i
            };
        
                folder_item.Click += delegate {
                    Controller.SparkleShareClicked ();
                };
            
            SparkleMenuItem add_item = new SparkleMenuItem () {
                Header    = "Add Hosted Project…",
                IsEnabled = (!Program.Controller.FirstRun)
            };
            
                add_item.Click += delegate {
                    Controller.AddHostedProjectClicked ();
                };
            
            SparkleMenuItem log_item = new SparkleMenuItem () {
                Header    = "View Recent Changes…",
                IsEnabled = (Program.Controller.Folders.Count > 0)
            };
            
                log_item.Click += delegate {
                    Controller.OpenRecentEventsClicked ();
                };
            
            SparkleMenuItem notify_item = new SparkleMenuItem ();

                if (Program.Controller.NotificationsEnabled)
                    notify_item = new SparkleMenuItem () { Header = "Turn Notifications Off" };
                else
                    notify_item = new SparkleMenuItem () { Header = "Turn Notifications On" };
    
                notify_item.Click += delegate {
                    Program.Controller.ToggleNotifications ();
                    CreateMenu ();
                };
            
            SparkleMenuItem about_item = new SparkleMenuItem () {
                Header = "About SparkleShare"
            };
            
                about_item.Click += delegate {
                     Controller.AboutClicked ();
                };
            
            exit_item = new SparkleMenuItem () {
                Header = "Exit"
            };
            
                this.exit_item.Click += delegate {
                    this.notify_icon.Dispose ();
                    Controller.QuitClicked ();
                };
            
            
            this.context_menu.Items.Add (status_item);
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (folder_item);

            if (Program.Controller.Folders.Count > 0) {
                foreach (string folder_name in Program.Controller.Folders) {     
                    SparkleMenuItem subfolder_item = new SparkleMenuItem () {
                        Header = folder_name
                    };
                    
                    subfolder_item.Click += OpenFolderDelegate (folder_name);
                    
                    System.Windows.Controls.Image i2 = new System.Windows.Controls.Image();
            i2.Source = SparkleUIHelpers.GetImageSource ("folder-windows-16");
                i2.Width = 16;
            i2.Height = 16;
                    subfolder_item.Icon = i2;
                    /* TODO
                    if (Program.Controller.UnsyncedFolders.Contains (folder_name))
                        subfolder_item.Icon = Icons.dialog_error_16;
                    else
                        subfolder_item.Icon = Icons.sparkleshare_windows_status;
                     */
                    
                    this.context_menu.Items.Add (subfolder_item);
                }

            } else {
                SparkleMenuItem no_folders_item = new SparkleMenuItem () {
                    Header    = "No projects yet",
                    IsEnabled = false
                };
               
                this.context_menu.Items.Add (no_folders_item);
            }
            
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (add_item);
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (log_item);
            this.context_menu.Items.Add (notify_item);
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (about_item);
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (this.exit_item);
			
			this.notify_icon.ContextMenu = this.context_menu;
        }

        
        public void ShowBalloon (string title, string subtext, string image_path)
        {
            // TODO:
            // - Use the image pointed to by image_path
            // - Find a way to use the prettier (Win7?) balloons
            //this.notify_icon.ShowBalloonTip (5 * 1000, title, subtext, Forms.ToolTipIcon.Info);
        }
        

        public void Dispose ()
        {
            this.notify_icon.Dispose ();
        }


        // A method reference that makes sure that opening the
        // event log for each repository works correctly
        private RoutedEventHandler OpenFolderDelegate (string folder_name)
        {
            return delegate {
                Controller.SubfolderClicked (folder_name);
            };
        }
    }
    
    
    public class SparkleMenuItem : MenuItem {
        
        public SparkleMenuItem () : base ()
        {
            Padding = new Thickness (6, 3, 4, 0);
        }
    }
}
