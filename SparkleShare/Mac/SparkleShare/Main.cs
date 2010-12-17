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
		
		SparkleLog Log;


		int i = 0;
		
		
	/*	public override NSMenu ApplicationDockMenu (NSApplication app)
		{
			
			return (NSMenu) Menu;	
			
		}
	*/	
		public AppDelegate ()
		{
		}


	SparkleStatusIcon StatusIcon;

		public override void FinishedLaunching (NSObject notification)
		{
			
			StatusIcon = new SparkleStatusIcon ();
			
	/*	tile = NSApplication.SharedApplication.DockTile;
			tile.BadgeLabel = "!";
tile.Display ();		
	*/		
			//	mainWindowController = new MainWindowController ();
			//	mainWindowController.Window.MakeKeyAndOrderFront (this);
			
			//			SparkleStatusIcon = new SparkleStatusIcon ();

			//		SparkleRepo repo = new SparkleRepo ("/Users/hbons/SparkleShare/SparkleShare-Test");

			//StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
			

				
			
			
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			
			
		}
		
		
		
		
	}
	
}

