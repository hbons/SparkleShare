using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace SparkleShare
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowController mainWindowController;

		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);
			
			//			SparkleStatusIcon = new SparkleStatusIcon ();

			
									var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (32);

				statusItem.Enabled = true;

				statusItem.Image = NSImage.ImageNamed ("sparkleshare-idle.png");
				statusItem.AlternateImage = NSImage.ImageNamed ("sparkleshare-idle-focus.png");
							statusItem.Image.Size = new SizeF (16, 16);	
				statusItem.AlternateImage.Size = new SizeF (16, 16);	

			NSMenu menu = new NSMenu() {};
			menu.AddItem (new NSMenuItem () { Title="Up to date (102 MB)", Enabled = true });
			menu.AddItem (NSMenuItem.SeparatorItem);
			
			var item = new NSMenuItem () {
				Title="SparkleShare", Enabled = true,
				Action = new Selector ("ddd")
			};
			
				item.Activated += delegate {
					Console.WriteLine ("DDDD");	
				};
			
				item.Image = NSImage.ImageNamed ("NSFolder");
				item.Image.Size = new SizeF (16, 16);	

			menu.AddItem (item);

			var tmp = new NSMenuItem () {
				Title="gnome-design", Enabled = true,
				Action = new Selector ("ddd")
			};
			
				tmp.Activated += delegate {
					Console.WriteLine ("DDDD");	
				};
			
				tmp.Image = NSImage.ImageNamed ("NSFolder");
				tmp.Image.Size = new SizeF (16, 16);	

			menu.AddItem (tmp);
			menu.AddItem (NSMenuItem.SeparatorItem);

			Console.WriteLine (item.Action.Name);
			
			NSMenuItem sync_menu_item = new NSMenuItem () {
				Title = "Sync Remote Folder..."
			};

				sync_menu_item.Activated += delegate {
					Console.WriteLine ("DDDD");	
				};

			menu.AddItem (sync_menu_item);
			menu.AddItem (NSMenuItem.SeparatorItem);

			NSMenuItem notifications_menu_item = new NSMenuItem () {
				Title = "Show Notifications",
				State = NSCellStateValue.On
			};

				notifications_menu_item.Activated += delegate {
								statusItem.Image = NSImage.ImageNamed ("NSComputer");
				if (notifications_menu_item.State == NSCellStateValue.On)
					notifications_menu_item.State = NSCellStateValue.Off;
				else
					notifications_menu_item.State = NSCellStateValue.On;
				};

			menu.AddItem (notifications_menu_item);
			menu.AddItem (NSMenuItem.SeparatorItem);

			NSMenuItem about_menu_item = new NSMenuItem () {
				Title = "About"
			};

				about_menu_item.Activated += delegate {
					Console.WriteLine ("DDDD");	
					statusItem.Title = "bla";
				};

			menu.AddItem (about_menu_item);
			menu.AddItem (NSMenuItem.SeparatorItem);

			NSMenuItem quit_menu_item = new NSMenuItem () {
				Title = "Quit"
			};
				quit_menu_item.Activated += delegate {
					Console.WriteLine ("DDDD");	
					Environment.Exit (0);
				};
			
			menu.AddItem (quit_menu_item);			

			

				statusItem.Menu = menu;
				statusItem.HighlightMode = true;
		}
	}
}

