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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using Mono.Unix;

namespace SparkleShare {

	public class SparkleWindow : NSWindow {
		
		private NSImage SideSplash;
		private NSImageView SideSplashView;

		public List <NSButton> Buttons;
		public string Header;
		public string Description;
		
		private NSTextField HeaderTextField;
		private NSTextField DescriptionTextField;

		
		public SparkleWindow () : base ()
		{

			SetFrame (new RectangleF (0, 0, 640, 480), true);
			
			Center ();
			
			StyleMask = (NSWindowStyle.Closable |
			             NSWindowStyle.Miniaturizable |
			             NSWindowStyle.Titled);

			MaxSize     = new SizeF (640, 480);
			MinSize     = new SizeF (640, 480);
			HasShadow   = true;	
			BackingType = NSBackingStore.Buffered;

			
			string side_splash_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
				"Pixmaps", "side-splash.png");

			SideSplash = new NSImage (side_splash_path) {
				Size = new SizeF (150, 480)
			};

			SideSplashView = new NSImageView () {
				Image = SideSplash,
				Frame = new RectangleF (0, 0, 150, 480)
			};


			Buttons = new List <NSButton> ();

			
			HeaderTextField = new NSTextField (new RectangleF (200, Frame.Height - 100, 350, 48)) {
				BackgroundColor = NSColor.WindowBackground,
				Bordered    = false,
				Editable    = false,
				Font        = NSFontManager.SharedFontManager.FontWithFamily
					("Lucida Grande", NSFontTraitMask.Bold, 0, 18)
			};
			
			DescriptionTextField = new NSTextField (new RectangleF (200, Frame.Height - 155 , 350, 64)) {
				BackgroundColor = NSColor.WindowBackground,
				Bordered        = false,
				Editable        = false
			};

			
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			MakeKeyAndOrderFront (this);

		}
		
		
		public void Reset () {
		
			ContentView.Subviews = new NSView [0];
			Buttons = new List <NSButton> ();
			
			Header      = "";
			Description = "";
			
		}


		public void ShowAll () {
			
			HeaderTextField.StringValue      = Header;
			DescriptionTextField.StringValue = Description;
			
			ContentView.AddSubview (HeaderTextField);
			ContentView.AddSubview (DescriptionTextField);
			
			ContentView.AddSubview (SideSplashView);
			
			int i = 0;
			
			if (Buttons.Count > 0) {

				DefaultButtonCell = Buttons [0].Cell;
				
				foreach (NSButton button in Buttons) {
					
					button.BezelStyle = NSBezelStyle.Rounded;
					button.Frame = new RectangleF (Frame.Width - 20 - (120 * (i + 1)) - (4 * i), 12, 120, 31);		
					ContentView.AddSubview (button);
					
					i++;
				
				}
			
			}
	
		}
		
	}
		
}
