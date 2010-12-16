using System;
using System.Drawing;
using System.Timers;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using MonoMac.Growl;

namespace SparkleShare
{
	class MainClass
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			NSApplication.SharedApplication.applicationIconImage = NSImage.ImageNamed ("sparkleshare.icns");
			NSApplication.Main (args);
		}
	}
	
	

	public partial class AppDelegate : NSApplicationDelegate
	{
	
		//MainWindowController mainWindowController;
		NSStatusItem StatusItem;
		
		NSMenu Menu;
		NSMenuItem FolderMenuItem;
		NSMenuItem [] FolderMenuItems;
		NSMenuItem SyncMenuItem;
		NSMenuItem NotificationsMenuItem;
		NSMenuItem AboutMenuItem;
		NSMenuItem QuitMenuItem;

	
		NSWindow window;
		NSButton button;
		NSButton button2;
		
		WebView web_view;
		NSDockTile tile;
		int i = 0;
		
		
	/*	public override NSMenu ApplicationDockMenu (NSApplication app)
		{
			
			return (NSMenu) Menu;	
			
		}
	*/	
		public AppDelegate ()
		{
		}


	

		public override void FinishedLaunching (NSObject notification)
		{
			
	/*	tile = NSApplication.SharedApplication.DockTile;
			tile.BadgeLabel = "!";
tile.Display ();		
	*/		
			//	mainWindowController = new MainWindowController ();
			//	mainWindowController.Window.MakeKeyAndOrderFront (this);
			
			//			SparkleStatusIcon = new SparkleStatusIcon ();

			//		SparkleRepo repo = new SparkleRepo ("/Users/hbons/SparkleShare/SparkleShare-Test");

			StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
			
			StatusItem.Enabled             = true;
			StatusItem.Image               = NSImage.ImageNamed ("sparkleshare-idle.png");
			StatusItem.AlternateImage      = NSImage.ImageNamed ("sparkleshare-idle-focus.png");
			StatusItem.Image.Size          = new SizeF (13, 13);	
			StatusItem.AlternateImage.Size = new SizeF (13, 13);	
			StatusItem.HighlightMode = true;

			Menu = new NSMenu ();
	
			
			Menu.AddItem (new NSMenuItem () { Title="Up to date (102 ᴍʙ)", Enabled = true });			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			Timer timer = new Timer () {
				Interval = 500
			};
			
			
				
			FolderMenuItem = new NSMenuItem () {
				Title="SparkleShare", Enabled = true,
				Action = new Selector ("ddd")
			};

			
						timer.Elapsed += delegate {
			FolderMenuItem.InvokeOnMainThread (delegate {
					
					if (i == 0){
					StatusItem.Image = NSImage.ImageNamed ("sparkleshare-idle-focus.png");
						i = 1;
					}else{
					StatusItem.Image = NSImage.ImageNamed ("sparkleshare-idle.png");	
					i = 0;	
					}
					
					/*FolderMenuItem.Title+="Z";Menu.Update ();*/});	
			};
			
			timer.Start ();
				FolderMenuItem.Activated += delegate {
					Console.WriteLine ("DDDD");	
				};
			
				FolderMenuItem.Image = NSImage.ImageNamed ("NSFolder");
				FolderMenuItem.Image.Size = new SizeF (16, 16);	

			Menu.AddItem (FolderMenuItem);
			
			FolderMenuItems = new NSMenuItem [2] {
				new NSMenuItem () { Title = "gnome-design (2)" },
				new NSMenuItem () { Title = "tango-icons" }	
			};
			
			foreach (NSMenuItem item in FolderMenuItems) {
				
				item.Activated += delegate {
					
					
					
					
		button = new NSButton (new RectangleF (16, 12, 120, 31)) {
			Title = "Open Folder",
			BezelStyle = NSBezelStyle.Rounded
					
		};
					
				button2 = new NSButton (new RectangleF (480 - 120 - 16, 12, 120, 31)) {
			Title = "Close",
			BezelStyle = NSBezelStyle.Rounded
					
		};

					
	bool minimizeBox = true;
					bool maximizeBox = false;
NSWindowStyle style = (NSWindowStyle)(1 | (1 << 1) | (minimizeBox ? 4 : 1) | (maximizeBox ? 8 : 1));
					
					
window = new NSWindow (new RectangleF (0, 0, 480, 640),
					        style, 0, false);
	
					
					
					
		web_view = new WebView (new RectangleF (0, 12 + 31 + 16, 480, 640 - (12 + 31 + 16)), "", "");			
					web_view.MainFrameUrl = "http://www.google.nl/";
					
					
		window.ContentView.AddSubview (button);
		window.ContentView.AddSubview (button2);
				window.ContentView.AddSubview (web_view);
					
		window.MaxSize = new SizeF (480, 640);
		window.MinSize = new SizeF (480, 640);
					
					window.Title = "Recent Events in 'gnome-design'";

		window.HasShadow = true;	
					//window.DefaultButtonCell = button2.Cell;
		window.BackingType = NSBackingStore.Buffered;

					
					NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
					
	window.MakeKeyAndOrderFront (this);
					window.Center ();
					
					
					
					
					
				};
				
				item.Image = NSImage.ImageNamed ("NSFolder");
				Menu.AddItem (item);	
			};
		
                                                                           
			
				
			
			

			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			SyncMenuItem = new NSMenuItem () {
				Title = "Add Remote Folder..."
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

			
	//		Menu.AddItem (NSMenuItem.SeparatorItem);

			
			QuitMenuItem = new NSMenuItem () {
				Title = "Quit"
			};
	
				QuitMenuItem.Activated += delegate {
					Environment.Exit (0);
				};
			
			//Menu.AddItem (QuitMenuItem);								 

			StatusItem.Menu = Menu;
			
			
			
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			
			
		}
		
		
		
		
	}
	
}

