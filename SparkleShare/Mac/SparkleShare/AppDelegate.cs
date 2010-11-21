using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using SparkleLib;

namespace SparkleShare
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		
		MainWindowController mainWindowController;
		NSStatusItem StatusItem;
		
		NSMenu Menu;
		NSMenuItem FolderMenuItem;
		NSMenuItem [] FolderMenuItems;
		NSMenuItem SyncMenuItem;
		NSMenuItem NotificationsMenuItem;
		NSMenuItem AboutMenuItem;
		NSMenuItem QuitMenuItem;

			
		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{

			//	mainWindowController = new MainWindowController ();
			//	mainWindowController.Window.MakeKeyAndOrderFront (this);
			
			//			SparkleStatusIcon = new SparkleStatusIcon ();

			//		SparkleRepo repo = new SparkleRepo ("/Users/hbons/SparkleShare/SparkleShare-Test");

			StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (32);
			
			StatusItem.Enabled             = true;
			StatusItem.Image               = NSImage.ImageNamed ("sparkleshare-idle.png");
			StatusItem.AlternateImage      = NSImage.ImageNamed ("sparkleshare-idle-focus.png");
			StatusItem.Image.Size          = new SizeF (13, 13);	
			StatusItem.AlternateImage.Size = new SizeF (13, 13);	
			StatusItem.HighlightMode = true;

			Menu = new NSMenu ();
	
			
			Menu.AddItem (new NSMenuItem () { Title="Up to date (102 ᴍʙ)", Enabled = true });			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			FolderMenuItem = new NSMenuItem () {
				Title="SparkleShare", Enabled = true,
				Action = new Selector ("ddd")
			};
			
				FolderMenuItem.Activated += delegate {
					Console.WriteLine ("DDDD");	
				};
			
				FolderMenuItem.Image = NSImage.ImageNamed ("NSFolder");
				FolderMenuItem.Image.Size = new SizeF (16, 16);	

			Menu.AddItem (FolderMenuItem);
			
			FolderMenuItems = new NSMenuItem [2] {
				new NSMenuItem () { Title = "gnome-design" },
				new NSMenuItem () { Title = "tango-icons" }	
			};
			
			foreach (NSMenuItem item in FolderMenuItems) {
				
				item.Activated += delegate {
						
				};
				
				item.Image = NSImage.ImageNamed ("NSFolder");
				Menu.AddItem (item);	
			};
		

			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			SyncMenuItem = new NSMenuItem () {
				Title = "Sync Remote Folder..."
			};
			
				SyncMenuItem.Activated += delegate {
				
				};
			
			Menu.AddItem (SyncMenuItem);

			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			

			NotificationsMenuItem = new NSMenuItem () {
				Title = "Show Notifications",
				State = NSCellStateValue.On
			};

				NotificationsMenuItem.Activated += delegate {
					
					//StatusItem.Image = NSImage.ImageNamed ("NSComputer");
				if (NotificationsMenuItem.State == NSCellStateValue.On)
	
					NotificationsMenuItem.State = NSCellStateValue.Off;
				
				else

					NotificationsMenuItem.State = NSCellStateValue.On;

				};

			Menu.AddItem (NotificationsMenuItem);
			
			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			AboutMenuItem = new NSMenuItem () {
				Title = "About"
			};

				AboutMenuItem.Activated += delegate {
	
				};

			Menu.AddItem (AboutMenuItem);

			
			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			QuitMenuItem = new NSMenuItem () {
				Title = "Quit"
			};
	
				QuitMenuItem.Activated += delegate {
					Environment.Exit (0);
				};
			
			Menu.AddItem (QuitMenuItem);									 

			StatusItem.Menu = Menu;

		}
	}
}

