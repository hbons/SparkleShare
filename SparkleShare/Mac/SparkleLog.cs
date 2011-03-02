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

using System.IO;

namespace SparkleShare {

	public class SparkleLog : NSWindow {

		public readonly string LocalPath;
		
		private WebView WebView;		
		private NSButton CloseButton;
		private NSButton OpenFolderButton;
		private NSBox Separator;

		public SparkleLog (IntPtr handle) : base (handle) { } 
		
		public SparkleLog (string path) : base ()
		{

			LocalPath = path;

			Delegate = new SparkleLogDelegate ();
			
			SetFrame (new RectangleF (0, 0, 480, 640), true);
			Center ();
			
			// Open slightly off center for each consecutive window
			if (SparkleUI.OpenLogs.Count > 0) {
			
				RectangleF offset = new RectangleF (Frame.X + (SparkleUI.OpenLogs.Count * 20),
					Frame.Y - (SparkleUI.OpenLogs.Count * 20), Frame.Width, Frame.Height);
			
				SetFrame (offset, true);

			}
			
			StyleMask = (NSWindowStyle.Closable |
			             NSWindowStyle.Miniaturizable |
			             NSWindowStyle.Titled);

			MaxSize     = new SizeF (480, 640);
			MinSize     = new SizeF (480, 640);
			HasShadow   = true;			
			BackingType = NSBackingStore.Buffered;

			CreateEventLog ();
			UpdateEventLog ();
			
			ContentView.AddSubview (WebView);
			
			OpenFolderButton = new NSButton (new RectangleF (16, 12, 120, 32)) {
				Title = "Open Folder",
				BezelStyle = NSBezelStyle.Rounded	,
				Font = SparkleUI.Font
			};

				OpenFolderButton.Activated += delegate {
					SparkleShare.Controller.OpenSparkleShareFolder (LocalPath);
				};

			ContentView.AddSubview (OpenFolderButton);


			CloseButton = new NSButton (new RectangleF (480 - 120 - 16, 12, 120, 32)) {
				Title = "Close",
				BezelStyle = NSBezelStyle.Rounded,
				Font = SparkleUI.Font
			};
					
				CloseButton.Activated += delegate {
					InvokeOnMainThread (delegate {	
						PerformClose (this);
					});
				};
								
			ContentView.AddSubview (CloseButton);
						

			string name = Path.GetFileName (LocalPath);
			Title = String.Format ("Events in ‘{0}’", name);
			
			Separator = new NSBox (new RectangleF (0, 58, 480, 1)) {
				BorderColor = NSColor.LightGray,
				BoxType = NSBoxType.NSBoxCustom
			};
			
			ContentView.AddSubview (Separator);
			
			OrderFrontRegardless ();
			
		}


		public void UpdateEventLog ()
		{

			string folder_name = Path.GetFileName (LocalPath);
			string html        = SparkleShare.Controller.GetHTMLLog (folder_name);
			
			html = html.Replace ("<!-- $body-font-family -->", "Lucida Grande");
			html = html.Replace ("<!-- $body-font-size -->", "13.4px");
			html = html.Replace ("<!-- $secondary-font-color -->", "#bbb");
			html = html.Replace ("<!-- $small-color -->", "#ddd");
			html = html.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
			html = html.Replace ("<!-- $a-color -->", "#0085cf");
			html = html.Replace ("<!-- $no-buddy-icon-background-image -->",
				"file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "avatar-default.png"));

			WebView.MainFrame.LoadHtmlString (html, new NSUrl (""));
			
			Update ();

		}


		private WebView CreateEventLog ()
		{

			WebView = new WebView (new RectangleF (0, 59, 480, 559), "", ""){
				PolicyDelegate = new SparkleWebPolicyDelegate ()
			};
			
			return WebView;

		}

	}


	public class SparkleLogDelegate : NSWindowDelegate {
		
		public override bool WindowShouldClose (NSObject sender)
		{

			(sender as SparkleLog).OrderOut (this);
			return false;
			
		}

	}
	
	
	public class SparkleWebPolicyDelegate : WebPolicyDelegate {
		
		public override void DecidePolicyForNavigation (WebView web_view, NSDictionary action_info,
			NSUrlRequest request, WebFrame frame, NSObject decision_token)
		{
		
			string file_path = request.Url.ToString ();
			file_path = file_path.Replace ("%20", " ");
			
			NSWorkspace.SharedWorkspace.OpenFile (file_path);

		}
		
	}

}