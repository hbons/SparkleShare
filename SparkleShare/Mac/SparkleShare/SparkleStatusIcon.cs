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
using System.Timers;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using Mono.Unix;

namespace SparkleShare {

	// The statusicon that stays in the
	// user's notification area
	public class SparkleStatusIcon : NSObject {

		private Timer Animation;
		private int FrameNumber;
		private string StateText;

		private NSStatusItem StatusItem;
		private NSMenu Menu;
		private NSMenuItem StateMenuItem;
		private NSMenuItem FolderMenuItem;
		private NSMenuItem [] FolderMenuItems;
		private NSMenuItem SyncMenuItem;
		private NSMenuItem NotificationsMenuItem;
		private NSMenuItem AboutMenuItem;

		
		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		
		public SparkleStatusIcon () : base ()
		{

			Animation = CreateAnimation ();

			StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
			StatusItem.HighlightMode = true;
			
			
			SetAnimationState ();
			CreateMenu ();
			

			
/*			SparkleShare.Controller.FolderSizeChanged += delegate {
				Application.Invoke (delegate {
					UpdateMenu ();
				});
			};
			
			SparkleShare.Controller.FolderListChanged += delegate {
				Application.Invoke (delegate {
					SetNormalState ();
					CreateMenu ();
				});
			};

			SparkleShare.Controller.OnIdle += delegate {
				Application.Invoke (delegate {
					SetNormalState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnSyncing += delegate {
				Application.Invoke (delegate {
					SetAnimationState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnError += delegate {
				Application.Invoke (delegate {
					SetNormalState (true);
					UpdateMenu ();
				});
			};
*/
		}


		// Creates the Animation that handles the syncing animation
		private Timer CreateAnimation ()
		{

			FrameNumber = 0;

			Timer Animation = new Timer () {
				Interval = 35
			};

			Animation.Elapsed += delegate {

				if (FrameNumber < 4)
					FrameNumber++;
				else
					FrameNumber = 0;

				InvokeOnMainThread (delegate {
					
					StatusItem.AlternateImage      = NSImage.ImageNamed ("idle" + FrameNumber + ".png");
					StatusItem.AlternateImage.Size = new SizeF (16, 16);
					
					StatusItem.Image      = NSImage.ImageNamed ("idle" + FrameNumber + ".png");
					StatusItem.Image.Size = new SizeF (16, 16);
					
				});

			};

			return Animation;

		}


		// Creates the menu that is popped up when the
		// user clicks the status icon
		public void CreateMenu ()
		{

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
					// SparkleShare.Controller.OpenSparkleShareFolder ();
				};
			
				FolderMenuItem.Image = NSImage.ImageNamed ("sparkleshare.icns");
				FolderMenuItem.Image.Size = new SizeF (16, 16);	
			
			Menu.AddItem (FolderMenuItem);
			

			FolderMenuItems = new NSMenuItem [2] {
				new NSMenuItem () { Title = "gnome-design" },
				new NSMenuItem () { Title = "tango-icons" }	
			};
			
			
//			if (SparkleShare.Controller.Folders.Count > 0) {
			
				
//				foreach (string path in SparkleShare.Controller.Folders) {
			
				foreach (NSMenuItem item in FolderMenuItems) {	
				
//					if (repo.HasUnsyncedChanges)
//						folder_action.IconName = "dialog-error";
						
					item.Image      = NSImage.ImageNamed ("NSFolder");
					item.Image.Size = new SizeF (16, 16);
					
					item.Activated += delegate {
						
					};
					
					item.Activated += OpenEventLogDelegate (item.Title);
					
					Menu.AddItem (item);
			
				};
		
			// } else {
		
				// TODO: No Remote Folders Yet
		
			// }
					
			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			SyncMenuItem = new NSMenuItem () {
				Title = "Add Remote Folder..."
			};
			
//				if (SparkleShare.Controller.FirstRun)
//					SyncMenuItem.Enabled = false;
			
				SyncMenuItem.Activated += delegate {
					// TODO
				};
			
			Menu.AddItem (SyncMenuItem);

			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			

			NotificationsMenuItem = new NSMenuItem () {
				Title = "Show Notifications",
				State = NSCellStateValue.On
			};
			
//				if (SparkleShare.Controller.NotificationsEnabled)
//					NotificationsMenuItem.State = NSCellStateValue.On;
//				else
//					NotificationsMenuItem.State = NSCellStateValue.On;
				NotificationsMenuItem.Activated += delegate {
//					SparkleShare.Controller.ToggleNotifications ();
				};

			Menu.AddItem (NotificationsMenuItem);
			
			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			AboutMenuItem = new NSMenuItem () {
				Title = "About"
			};

				AboutMenuItem.Activated += delegate {
					// TODO
				};

			Menu.AddItem (AboutMenuItem);
										 
			StatusItem.Menu = Menu;

		}


		// A method reference that makes sure that opening the
		// event log for each repository works correctly
		private EventHandler OpenEventLogDelegate (string path)
		{

			return delegate { 

				SparkleLog log = null; //SparkleUI.OpenLogs.Find (delegate (SparkleLog l) { return l.LocalPath.Equals (path); });

				// Check whether the log is already open, create a new one if
				//that's not the case or present it to the user if it is
				if (log == null) {

					log = new SparkleLog (path);

					/*log.Hidden += delegate {

					SparkleUI.OpenLogs.Remove (log);
					log.Destroy ();

					};

					SparkleUI.OpenLogs.Add (log);*/

				}

			};

		}


		public void UpdateMenu ()
		{

			StateMenuItem.Title = StateText;

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
			
			if (false /* SparkleShare.Controller.Folders.Count == 0 */) {

				StateText = _("Welcome to SparkleShare!");
				InvokeOnMainThread (delegate {

					StatusItem.Image      = NSImage.ImageNamed ("idle.png");
					StatusItem.Image.Size = new SizeF (16, 16);
						
					StatusItem.AlternateImage      = NSImage.ImageNamed ("idle-active.png");
					StatusItem.AlternateImage.Size = new SizeF (16, 16);
					
				});
				
			} else {
			
				if (error) {

					StateText = _("Not everything is synced");
					InvokeOnMainThread (delegate {
						//Pixbuf = SparkleUIHelpers.GetIcon ("sparkleshare-syncing-error", 24);
					});

				} else {
					
					StateText = _("Up to date") + "  ("/* + SparkleShare.Controller.FolderSize + ")" */;
					InvokeOnMainThread (delegate {
						
						StatusItem.Image      = NSImage.ImageNamed ("idle.png");
						StatusItem.Image.Size = new SizeF (16, 16);
						
						StatusItem.AlternateImage      = NSImage.ImageNamed ("idle-active.png");
						StatusItem.AlternateImage.Size = new SizeF (16, 16);
						
					});

				}

			}

		}


		// The state when animating
		private void SetAnimationState ()
		{

			StateText = _("Syncing…");

			if (!Animation.Enabled)
				Animation.Start ();

		}

	}

}


