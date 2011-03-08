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
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleAbout : NSWindow {

        private NSButton WebsiteButton;
        private NSButton CreditsButton;
        private NSBox Box;
        private NSTextField HeaderTextField;
        private NSTextField VersionTextField;
        private NSTextField CreditsTextField;


        public SparkleAbout (IntPtr handle) : base (handle) { }

        public SparkleAbout () : base ()
        {

            SetFrame (new RectangleF (0, 0, 360, 260), true);
            Center ();
            
            StyleMask = (NSWindowStyle.Closable |
                         NSWindowStyle.Titled);

            MaxSize     = new SizeF (360, 260);
            MinSize     = new SizeF (360, 260);
            HasShadow   = true;
            BackingType = NSBackingStore.Buffered;

            Box = new NSBox () {
                FillColor = NSColor.White,
                Frame = new RectangleF (-1, Frame.Height - 105, Frame.Width + 2, 105),
                BoxType = NSBoxType.NSBoxCustom
            };

            HeaderTextField = new NSTextField () {
                StringValue     = "SparkleShare",
                Frame           = new RectangleF (22, Frame.Height - 89, 318, 48),
                BackgroundColor = NSColor.White,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Condensed, 0, 24)
            };

            VersionTextField = new NSTextField () {
                StringValue     = "0.2.0",
                Frame           = new RectangleF (22, Frame.Height - 94, 318, 22),
                BackgroundColor = NSColor.White,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                TextColor       = NSColor.DisabledControlText
            };

            CreditsTextField = new NSTextField () {
                StringValue     = @"Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others" +
                                   "\n" +
                                   "\n" +
                                   "SparkleShare is Free and Open Source Software. You are free to use, modify, and redistribute it " +
                                   "under the GNU General Public License version 3 or later.",
                Frame           = new RectangleF (22, Frame.Height - 222, 318, 98),
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
            };

            WebsiteButton = new NSButton () {
                Frame = new RectangleF (12, 12, 120, 32),
                Title = "Visit Website",
                BezelStyle = NSBezelStyle.Rounded,
                Font = SparkleUI.Font
            };

            WebsiteButton.Activated += delegate {

                NSUrl url = new NSUrl ("http://www.sparkleshare.org/");
                NSWorkspace.SharedWorkspace.OpenUrl (url);

            };

            CreditsButton = new NSButton () {
                Frame = new RectangleF (Frame.Width - 12 - 120, 12, 120, 32),
                Title = "Show Credits",
                BezelStyle = NSBezelStyle.Rounded,
                Font = SparkleUI.Font
            };

            CreditsButton.Activated += delegate {

                NSUrl url = new NSUrl ("http://www.sparkleshare.org/credits/");
                NSWorkspace.SharedWorkspace.OpenUrl (url);

            };

            ContentView.AddSubview (Box);
            ContentView.AddSubview (HeaderTextField);
            ContentView.AddSubview (VersionTextField);
            ContentView.AddSubview (CreditsTextField);
            ContentView.AddSubview (CreditsButton);
            ContentView.AddSubview (WebsiteButton);

            MakeKeyAndOrderFront (this);

        }

    }

}
