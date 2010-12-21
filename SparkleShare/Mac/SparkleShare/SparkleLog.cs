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
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

	public class SparkleLog : NSWindow {

		public readonly string LocalPath;
		
		private WebView WebView;		
		private NSButton CloseButton;
		private NSButton OpenFolderButton;


		public SparkleLog (string path) : base ()
		{
			
			LocalPath = path;
	
			Delegate = new LogDelegate ();
			
			SetFrame (new RectangleF (0, 0, 480, 640), true);
			
			Center ();
			
			StyleMask = (NSWindowStyle.Closable |
			             NSWindowStyle.Miniaturizable |
			             NSWindowStyle.Titled);

			MaxSize     = new SizeF (480, 640);
			MinSize     = new SizeF (480, 640);
			HasShadow   = true;			
			BackingType = NSBackingStore.Buffered;


			ContentView.AddSubview (CreateEventLog ());
			
			OpenFolderButton = new NSButton (new RectangleF (16, 12, 120, 31)) {
				Title = "Open Folder",
				BezelStyle = NSBezelStyle.Rounded	
			};

				OpenFolderButton.Activated += delegate {
					SparkleShare.Controller.OpenSparkleShareFolder (LocalPath);
				};

			ContentView.AddSubview (OpenFolderButton);


			CloseButton = new NSButton (new RectangleF (480 - 120 - 16, 12, 120, 31)) {
				Title = "Close",
				BezelStyle = NSBezelStyle.Rounded	
			};
					
				CloseButton.Activated += delegate {
					InvokeOnMainThread (delegate {	
						PerformClose (this);
					});
				};
								
			ContentView.AddSubview (CloseButton);
						

			string name = System.IO.Path.GetFileName (LocalPath);
			Title = String.Format ("Recent Events in ‘{0}’", name);

			OrderFrontRegardless ();
			
		}


		public void UpdateEventLog ()
		{

		}


		private WebView CreateEventLog ()
		{
			
			RectangleF frame = new RectangleF (0, 12 + 31 + 16, 480, 640 - (12 + 31 + 16));
			
			WebView = new WebView (frame, "", "");			
			WebView.MainFrameUrl = "http://www.google.nl/";

			return WebView;

		}

	}
	
	
	public class LogDelegate : NSWindowDelegate {
		
		public override void WillClose (NSNotification notification)
		{
			
			Console.WriteLine ("CLOSING " + (notification.Object as SparkleLog).LocalPath);
	
			SparkleUI.OpenLogs.Remove ((SparkleLog) notification.Object);
			
		}

	}

}
