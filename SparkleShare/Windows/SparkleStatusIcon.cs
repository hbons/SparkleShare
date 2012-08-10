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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace SparkleShare {

    public class SparkleStatusIcon : Control {
        
        public SparkleStatusIconController Controller = new SparkleStatusIconController();

        private Drawing.Bitmap [] animation_frames;
        private Drawing.Bitmap error_icon;

        private ContextMenu context_menu;


        private SparkleMenuItem log_item;
        private SparkleMenuItem state_item;
        private SparkleMenuItem exit_item;
        
        private SparkleNotifyIcon notify_icon = new SparkleNotifyIcon ();

        
       
        public SparkleStatusIcon ()
		{
			CreateAnimationFrames ();

			this.notify_icon.Icon = animation_frames [0];
            this.notify_icon.HeaderText = "SparkleShare";

            CreateMenu ();
         
			
			Controller.UpdateIconEvent += delegate (int icon_frame) {
				Dispatcher.BeginInvoke ((Action) delegate {
					if (icon_frame > -1)
						this.notify_icon.Icon = animation_frames [icon_frame];
					else
						this.notify_icon.Icon = this.error_icon;
				});				
			};
			
			Controller.UpdateStatusItemEvent += delegate (string state_text) {
				Dispatcher.BeginInvoke ((Action) delegate {
					this.state_item.Header = state_text;
					this.state_item.UpdateLayout ();
					this.notify_icon.HeaderText = "SparkleShare\n" + state_text;
				});
			};
			
			Controller.UpdateMenuEvent += delegate (IconState state) {
				Dispatcher.BeginInvoke ((Action) delegate {
					CreateMenu ();     
				});
			};
            
            Controller.UpdateQuitItemEvent += delegate (bool item_enabled) {
                  Dispatcher.BeginInvoke ((Action) delegate {
                    this.exit_item.IsEnabled = item_enabled;
                    this.exit_item.UpdateLayout ();
                });
            };

            Controller.UpdateOpenRecentEventsItemEvent += delegate (bool item_enabled) {
                  Dispatcher.BeginInvoke ((Action) delegate {
                    this.log_item.IsEnabled = item_enabled;
                    this.log_item.UpdateLayout ();
                });
            };
        }


        public void CreateMenu ()
        {
            this.context_menu = new ContextMenu ();

            this.state_item = new SparkleMenuItem () {
                Header    = Controller.StateText,
                IsEnabled = false
            };
			
			this.notify_icon.HeaderText = "SparkleShare\n" + Controller.StateText;
            
            Image folder_image = new Image () {
            	Source = SparkleUIHelpers.GetImageSource ("sparkleshare-folder"),
                Width  = 16,
            	Height = 16
			};
            
            SparkleMenuItem folder_item = new SparkleMenuItem () {
                Header = "SparkleShare",
                Icon   = folder_image
            };
        
                folder_item.Click += delegate {
                    Controller.SparkleShareClicked ();
                };
            
            SparkleMenuItem add_item = new SparkleMenuItem () {
                Header = "Add hosted project…"
            };
            
                add_item.Click += delegate {
                    Controller.AddHostedProjectClicked ();
                };
            
            this.log_item = new SparkleMenuItem () {
                Header    = "Recent changes…",
                IsEnabled = Controller.OpenRecentEventsItemEnabled
            };
            
                this.log_item.Click += delegate {
                    Controller.OpenRecentEventsClicked ();
                };
            
            SparkleMenuItem notify_item = new SparkleMenuItem () {
				Header = "Notifications"
			};

                CheckBox notify_check_box = new CheckBox () {
                    Margin    = new Thickness (6, 0, 0, 0),
                    IsChecked = (Controller.Folders.Length > 0 && Program.Controller.NotificationsEnabled)
                };

                notify_item.Icon = notify_check_box;

				notify_check_box.Click += delegate {
					this.context_menu.IsOpen = false;
					Program.Controller.ToggleNotifications ();
					notify_check_box.IsChecked = Program.Controller.NotificationsEnabled;
				};

                notify_item.Click += delegate {
                    Program.Controller.ToggleNotifications ();
                    notify_check_box.IsChecked = Program.Controller.NotificationsEnabled;
                };
            
            SparkleMenuItem about_item = new SparkleMenuItem () {
                Header = "About SparkleShare"
            };
            
                about_item.Click += delegate {
                     Controller.AboutClicked ();
                };
            
            this.exit_item = new SparkleMenuItem () {
                Header = "Exit"
            };
            
                this.exit_item.Click += delegate {
                    this.notify_icon.Dispose ();
                    Controller.QuitClicked ();
                };
            
            
            this.context_menu.Items.Add (this.state_item);
            this.context_menu.Items.Add (new Separator ());
			this.context_menu.Items.Add (folder_item);

            if (Controller.Folders.Length > 0) {
                foreach (string folder_name in Controller.Folders) {     
                    SparkleMenuItem subfolder_item = new SparkleMenuItem () {
                        Header = folder_name
                    };
                    
                    subfolder_item.Click += OpenFolderDelegate (folder_name);
                    
					Image subfolder_image = new Image () {
		            	Source = SparkleUIHelpers.GetImageSource ("folder"),
		                Width  = 16,
		            	Height = 16
					};
					
                    if (Program.Controller.UnsyncedFolders.Contains (folder_name)) {
                    	subfolder_item.Icon = new Image () {
							Source = (BitmapSource) Imaging.CreateBitmapSourceFromHIcon (
								System.Drawing.SystemIcons.Exclamation.Handle, 
								Int32Rect.Empty,
								BitmapSizeOptions.FromWidthAndHeight (16,16)
							)
						};  
						
					} else {
                    	subfolder_item.Icon = subfolder_image;
					}
					
					this.context_menu.Items.Add (subfolder_item);
				}
				
				SparkleMenuItem more_item = new SparkleMenuItem () {
                    Header = "More projects"
                };
				
                foreach (string folder_name in Controller.OverflowFolders) {     
                    SparkleMenuItem subfolder_item = new SparkleMenuItem () {
                        Header = folder_name
                    };
                    
                    subfolder_item.Click += OpenFolderDelegate (folder_name);
                    
					Image subfolder_image = new Image () {
		            	Source = SparkleUIHelpers.GetImageSource ("folder"),
		                Width  = 16,
		            	Height = 16
					};
					
					if (Program.Controller.UnsyncedFolders.Contains (folder_name)) {
                    	subfolder_item.Icon = new Image () {
							Source = (BitmapSource) Imaging.CreateBitmapSourceFromHIcon (
								System.Drawing.SystemIcons.Exclamation.Handle, 
								Int32Rect.Empty,
								BitmapSizeOptions.FromWidthAndHeight (16,16)
							)
						};  
						
					} else {
                        subfolder_item.Icon = subfolder_image;
					}
					
					more_item.Items.Add (subfolder_item);
                }
				
				if (more_item.Items.Count > 0) {
                    this.context_menu.Items.Add (new Separator ());
                    this.context_menu.Items.Add (more_item);
				}

            }
            
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (add_item);
            this.context_menu.Items.Add (this.log_item);
			this.context_menu.Items.Add (new Separator ());
			this.context_menu.Items.Add (notify_item);
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (about_item);
            this.context_menu.Items.Add (new Separator ());
            this.context_menu.Items.Add (this.exit_item);
			
			this.notify_icon.ContextMenu = this.context_menu;
        }

        
        public void ShowBalloon (string title, string subtext, string image_path)
        {
            this.notify_icon.ShowBalloonTip (title, subtext, image_path);
        }
        

        public void Dispose ()
        {
            this.notify_icon.Dispose ();
        }
		
		
		private void CreateAnimationFrames ()
        {
            this.animation_frames = new Drawing.Bitmap [] {
	            SparkleUIHelpers.GetBitmap ("process-syncing-i"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-ii"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-iii"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-iiii"),
	            SparkleUIHelpers.GetBitmap ("process-syncing-iiiii")
			};
			
			this.error_icon = SparkleUIHelpers.GetBitmap ("process-syncing-error");
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
