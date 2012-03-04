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
using System.IO;
using System.Runtime.InteropServices;

using WinForms = System.Windows.Forms;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SparkleShare {

    public class SparkleStatusIcon : Control {
		
        public SparkleStatusIconController Controller = new SparkleStatusIconController();

        private WinForms.Timer Animation;
        private Bitmap [] AnimationFrames;
        private int FrameNumber;
        private string StateText;
		private ContextMenu context_menu;
		private MenuItem exit_item;
		
        private WinForms.NotifyIcon notify_icon = new WinForms.NotifyIcon () {
            Text = "SparkleShare",
            Icon = Icons.sparkleshare,
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

			this.notify_icon.MouseClick +=new WinForms.MouseEventHandler(ShowMenu);	
			
            CreateMenu ();
            SetNormalState ();
            
			
			//TODO quit item event
			
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

		
        private void ShowMenu (object sender, WinForms.MouseEventArgs e)
		{
			this.context_menu.Placement        = PlacementMode.Mouse;
 	        this.context_menu.IsOpen           = true;
		}
		
        [DllImport("user32.dll", EntryPoint = "DestroyIcon")]
        static extern bool DestroyIcon(IntPtr hIcon);


        // Slices up the graphic that contains the
        // animation frames.
        private Bitmap [] CreateAnimationFrames ()
        {
            Bitmap [] animation_frames = new Bitmap [5];
            animation_frames [0] = Icons.process_syncing_sparkleshare_i_24;
            animation_frames [1] = Icons.process_syncing_sparkleshare_ii_24;
            animation_frames [2] = Icons.process_syncing_sparkleshare_iii_24;
            animation_frames [3] = Icons.process_syncing_sparkleshare_iiii_24;
            animation_frames [4] = Icons.process_syncing_sparkleshare_iiiii_24;

            return animation_frames;
        }


        // Creates the Animation that handles the syncing animation
        private WinForms.Timer CreateAnimation ()
        {
            FrameNumber = 0;

            WinForms.Timer Animation = new WinForms.Timer () {
                Interval = 35
            };

            Animation.Tick += delegate {
                if (FrameNumber < AnimationFrames.Length - 1)
                    FrameNumber++;
                else
                    FrameNumber = 0;

                Dispatcher.Invoke ((Action) delegate {
                    this.notify_icon.Icon = GetIconFromBitmap (AnimationFrames [FrameNumber]);
                });
            };

            return Animation;
        }


        public void CreateMenu ()
        {
			this.context_menu = new ContextMenu ();

			MenuItem status_item = new MenuItem () {
				Header    = StateText,
				IsEnabled = false
			};
			
			MenuItem folder_item = new MenuItem () {
				Header = " SparkleShare"//,
				//Icon   = Icons.sparkleshare
			};
		
				folder_item.Click += delegate {
					Controller.SparkleShareClicked ();
				};
			
			MenuItem add_item = new MenuItem () {
				Header    = " Add Hosted Project…",
				IsEnabled = (!Program.Controller.FirstRun)
			};
			
				add_item.Click += delegate {
					Controller.AddHostedProjectClicked ();
				};
			
			MenuItem log_item = new MenuItem () {
				Header    = " View Recent Changes…",
				IsEnabled = (Program.Controller.Folders.Count > 0)
			};
			
				log_item.Click += delegate {
					Controller.OpenRecentEventsClicked ();
				};
			
			MenuItem notify_item = new MenuItem ();

	            if (Program.Controller.NotificationsEnabled)
	                notify_item = new MenuItem () { Header = " Turn Notifications Off" };
	            else
	                notify_item = new MenuItem () { Header = " Turn Notifications On" };
	
	            notify_item.Click += delegate {
	                Program.Controller.ToggleNotifications ();
	                CreateMenu ();
	            };
			
			MenuItem about_item = new MenuItem () {
				Header = " About SparkleShare"
			};
			
				about_item.Click += delegate {
					 Controller.AboutClicked ();
				};
			
			exit_item = new MenuItem () {
				Header = " Exit"
			};
			
				this.exit_item.Click += delegate {
					this.notify_icon.Dispose ();
			 		Program.Controller.Quit ();	
				};
			
			
			this.context_menu.Items.Add (status_item);
			this.context_menu.Items.Add (new Separator ());
			this.context_menu.Items.Add (folder_item);

            if (Program.Controller.Folders.Count > 0) {
                foreach (string folder_name in Program.Controller.Folders) {     
					MenuItem subfolder_item = new MenuItem () {
						Header = " " + folder_name
					};
					
					subfolder_item.Click += OpenFolderDelegate (folder_name);
					/*
                    if (Program.Controller.UnsyncedFolders.Contains (folder_name))
                        subfolder_item.Icon = Icons.dialog_error_16;
                    else
                        subfolder_item.Icon = Icons.sparkleshare_windows_status;
					 */
                    this.context_menu.Items.Add (subfolder_item);
                }

            } else {
                MenuItem no_folders_item = new MenuItem () {
					Header    = " No projects yet",
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


        public void ShowBalloon (string title, string subtext, string image_path)
        {
            // TODO: Use the image pointed to by image_path

            this.notify_icon.BalloonTipText = title;
            this.notify_icon.BalloonTipText = subtext;
            this.notify_icon.BalloonTipIcon = WinForms.ToolTipIcon.None;

            this.notify_icon.ShowBalloonTip (5 * 1000);
        }


        public void UpdateMenu ()
        {
			(this.context_menu.Items [0] as MenuItem).Header = StateText;
			(this.context_menu.Items [0] as MenuItem).UpdateLayout ();
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
                StateText = _(" Welcome to SparkleShare!");

                Dispatcher.Invoke ((Action)delegate {
                    this.notify_icon.Icon = GetIconFromBitmap (AnimationFrames [0]);
                });

            } else {
                if (error) {
                    StateText = _(" Not everything is synced");

                    Dispatcher.Invoke ((Action) delegate {
                        this.notify_icon.Icon = GetIconFromBitmap (Icons.sparkleshare_syncing_error_24);
                    });
                } else {
                    StateText = _(" Files up to date") + Controller.FolderSize;
                    Dispatcher.Invoke ((Action)delegate {
                        this.notify_icon.Icon = GetIconFromBitmap (AnimationFrames [0]);
                    });
                }
            }
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
    }

// TODO: remove
    public static class ControlExtention {

        public static void SafeInvoke (this WinForms.Control ui_element,
            Action updater, bool force_synchronous)
        {
            if (ui_element == null)
                return;

            if (ui_element.InvokeRequired) {
                if (force_synchronous) {
                    ui_element.Invoke ((Action) delegate {
                        SafeInvoke (ui_element, updater, force_synchronous);
                    });

                } else {
                    ui_element.BeginInvoke ((Action) delegate {
                        SafeInvoke (ui_element, updater, force_synchronous);
                    });
                }

            } else {
                if (ui_element.IsDisposed)
                    throw new ObjectDisposedException ("Control is already disposed.");

                updater ();
            }
        }


        public static void SafeInvoke (this WinForms.Control ui_element, Action updater)
        {
            ui_element.SafeInvoke (updater, false);
        }
    }
}
