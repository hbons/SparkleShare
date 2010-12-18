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

	public class SparkleWindow : NSWindow {

		public readonly string LocalPath;
		
		private WebView WebView;		
		private NSButton CloseButton;
		private NSButton OpenFolderButton;

		private NSImage SideSplash;
		
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

			SideSplash = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/side-splash.png");
			SideSplash.Size = new SizeF (150, 480);
			
			
			NSText tv = new NSText (new RectangleF (200, 200, 200, 200)) {
				Value = "TEST"	
			};
			
			ContentView.AddSubview (new NSImageView (new RectangleF (0, 0, 150, 480)) { Image = SideSplash});
			ContentView.AddSubview (new NSTextField (new RectangleF (200, 100, 128, 31)) { BezelStyle = NSTextFieldBezelStyle.Rounded});
			
			
			
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			MakeKeyAndOrderFront (this);
			
		}

	}
		
}
