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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Forms = System.Windows.Forms;

namespace SparkleShare {

    public class SparkleStatusIcon : Control {
		
        public SparkleStatusIconController Controller = new SparkleStatusIconController();

        private Forms.Timer Animation;
        private Icon [] AnimationFrames;
		private Icon ErrorIcon;
        private int FrameNumber;
        private string StateText;
		private ContextMenu context_menu;
		private SparkleMenuItem exit_item;
		
        private Forms.NotifyIcon notify_icon = new Forms.NotifyIcon () {
            Text = "SparkleShare",
            Visible = true
		};
		
		
        // Short alias for the translations
        public static string _ (string s)
        {
            return Program._ (s);
        }
		
		
		public SparkleStatusIcon ()
        {
            AnimationFrames = CreateAnimationFrames ();
            Animation = CreateAnimation ();
			notify_icon.Icon = AnimationFrames [0];
			ErrorIcon = GetIconFromBitmap (SparkleUIHelpers.GetBitmap ("sparkleshare-syncing-error-windows"));

			this.notify_icon.MouseClick += delegate {
				this.context_menu.Placement = PlacementMode.Mouse;
	 	        this.context_menu.IsOpen    = true;	
			};
			
            CreateMenu ();
            SetNormalState ();
			
			
			Controller.UpdateQuitItemEvent += delegate (bool enable) {
			  	Dispatcher.Invoke ((Action) delegate {
                    this.exit_item.IsEnabled = enable;
					this.exit_item.UpdateLayout ();
                });
			};
			
            Program.Controller.FolderListChanged += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    SetNormalState ();
                    CreateMenu ();
                });
            };

            Program.Controller.OnIdle += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    SetNormalState ();
                    UpdateMenu ();
                });
            };
			
            Program.Controller.OnSyncing += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    SetAnimationState ();
                    UpdateMenu ();
                });
            };

            Program.Controller.OnError += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    SetNormalState (true);
                    UpdateMenu ();
                });
            };
        }

		

        // Slices up the graphic that contains the
        // animation frames.
        private Icon [] CreateAnimationFrames ()
        {
            Icon [] animation_frames = new Icon [5];
            animation_frames [0] = GetIconFromBitmap (SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-i"));
            animation_frames [1] = GetIconFromBitmap (SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-ii"));
            animation_frames [2] = GetIconFromBitmap (SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-iii"));
            animation_frames [3] = GetIconFromBitmap (SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-iiii"));
            animation_frames [4] = GetIconFromBitmap (SparkleUIHelpers.GetBitmap ("process-syncing-sparkleshare-windows-iiiii"));

            return animation_frames;
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

			SparkleMenuItem status_item = new SparkleMenuItem () {
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
					/*
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
		}


       

        public void UpdateMenu ()
        {
			(this.context_menu.Items [0] as SparkleMenuItem).Header = StateText;
			(this.context_menu.Items [0] as SparkleMenuItem).UpdateLayout ();
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

            if (Program.Controller.Folders.Count == 0) {
                StateText = _("Welcome to SparkleShare!");

                Dispatcher.Invoke ((Action)delegate {
                    this.notify_icon.Icon = AnimationFrames [0];
                });

            } else {
                if (error) {
                    StateText = _("Not everything is synced");

                    Dispatcher.Invoke ((Action) delegate {
                        this.notify_icon.Icon = ErrorIcon;
                    });
                } else {
                    StateText = _("Files up to date") + Controller.FolderSize;
                    Dispatcher.Invoke ((Action)delegate {
                        this.notify_icon.Icon = AnimationFrames [0];

                    });
                }
            }
        }
		
		
		public void ShowBalloon (string title, string subtext, string image_path)
        {
            // TODO:
			// - Use the image pointed to by image_path
			// - Find a way to use the prettier (Win7?) balloons
            this.notify_icon.ShowBalloonTip (5 * 1000, title, subtext, Forms.ToolTipIcon.Info);
        }
		

        public void Dispose ()
        {
            this.notify_icon.Dispose ();
        }


        // The state when animating
        private void SetAnimationState ()
        {
            StateText = _("Syncing…");

            if (!Animation.Enabled)
                Animation.Start ();
        }


        // A method reference that makes sure that opening the
        // event log for each repository works correctly
        private RoutedEventHandler OpenFolderDelegate (string folder_name)
        {
            return delegate {
                Controller.SubfolderClicked (folder_name);
            };
        }


        private Icon GetIconFromBitmap (Bitmap bitmap)
        {
            IntPtr unmanaged_icon = bitmap.GetHicon ();
            Icon icon = (Icon) Icon.FromHandle (unmanaged_icon).Clone ();
            DestroyIcon (unmanaged_icon);
			
            return icon;
        }
		
		
		[DllImport("user32.dll", EntryPoint = "DestroyIcon")]
		static extern bool DestroyIcon (IntPtr hIcon);
    }
	
	
	public class SparkleMenuItem : MenuItem {
		
		public SparkleMenuItem () : base ()
		{
			Padding = new Thickness (6, 3, 4, 0);
		}
	}
}
