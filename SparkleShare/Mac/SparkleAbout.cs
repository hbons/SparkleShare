//   SparkleShare, a collaboration and sharing tool.
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
        private NSTextField UpdatesTextField;
        private NSTextField CreditsTextField;


        public SparkleAbout (IntPtr handle) : base (handle) { }

        public SparkleAbout () : base ()
        {
            SetFrame (new RectangleF (0, 0, 360, 288), true);
            Center ();

            Delegate    = new SparkleAboutDelegate ();
            StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
            Title       = "About SparkleShare";
            MaxSize     = new SizeF (360, 288);
            MinSize     = new SizeF (360, 288);
            HasShadow   = true;
            BackingType = NSBackingStore.Buffered;

            CreateAbout ();
            MakeKeyAndOrderFront (this);

            SparkleShare.Controller.NewVersionAvailable += delegate (string new_version) {
                InvokeOnMainThread (delegate {
                    UpdatesTextField.StringValue = "A newer version (" + new_version + ") is available!";
                    UpdatesTextField.TextColor   =
                        NSColor.FromCalibratedRgba (0.96f, 0.47f, 0.0f, 1.0f); // Tango Orange #2
                });
            };

            SparkleShare.Controller.VersionUpToDate += delegate {
                InvokeOnMainThread (delegate {
                    UpdatesTextField.StringValue = "You are running the latest version.";
                    UpdatesTextField.TextColor   =
                        NSColor.FromCalibratedRgba (0.31f, 0.60f, 0.02f, 1.0f); // Tango Chameleon #3
                });
            };

            CheckForNewVersion ();
        }


        public void CheckForNewVersion ()
        {
            SparkleShare.Controller.CheckForNewVersion ();
        }


        private void CreateAbout ()
        {
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
                StringValue     = SparkleShare.Controller.Version,
                Frame           = new RectangleF (22, Frame.Height - 94, 318, 22),
                BackgroundColor = NSColor.White,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                TextColor       = NSColor.DisabledControlText
            };

            UpdatesTextField = new NSTextField () {
                StringValue     = "Checking for updates...",
                Frame           = new RectangleF (22, Frame.Height - 222, 318, 98),
                BackgroundColor = NSColor.WindowBackground,
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
                Frame           = new RectangleF (22, Frame.Height - 250, 318, 98),
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
            ContentView.AddSubview (UpdatesTextField);
            ContentView.AddSubview (CreditsTextField);
            ContentView.AddSubview (CreditsButton);
            ContentView.AddSubview (WebsiteButton);
        }
    }


    public class SparkleAboutDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as SparkleAbout).OrderOut (this);
            return false;
        }
    }
}
