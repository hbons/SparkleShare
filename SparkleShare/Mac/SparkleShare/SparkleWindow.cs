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


			
			
			
			NSButtonCell proto = new NSButtonCell {
			Title = " Github"	
			};
			
			NSText text = new NSText (new RectangleF (150,150,350,300)) {
				Value = "DDDDDDDD"
			};
			
			proto.SetButtonType (NSButtonType.Radio) ;
			
			NSButton button = new NSButton (new RectangleF (150, 0, 350, 300)) {
			Cell = proto,
				Font = NSFontManager.SharedFontManager.FontWithFamily ("Lucida Grande",
				                                                       NSFontTraitMask.Bold,
				                                                       0, 14)
			};
			
			NSMatrix matrix = new NSMatrix (new RectangleF (300, 00, 300, 300), NSMatrixMode.Radio, proto, 4, 1);
			

			
			matrix.Cells [0].Title = "My own server:";
			matrix.Cells [1].Title = "Github\nFree hosting";
			matrix.Cells [2].Title = "Gitorious";
			matrix.Cells [3].Title = "The GNOME Project";
			
			ContentView.AddSubview (new NSImageView (new RectangleF (0, 0, 150, 480)) { Image = SideSplash});
			ContentView.AddSubview (new NSTextField (new RectangleF (200, 100, 128, 25)) { BezelStyle = NSTextFieldBezelStyle.Square, Editable=false});
			ContentView.AddSubview (button);
			ContentView.AddSubview (text);
			
			
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			MakeKeyAndOrderFront (this);
			
		}

	}
		
}
