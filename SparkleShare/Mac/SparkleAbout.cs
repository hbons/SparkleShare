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

        public SparkleAboutController Controller = new SparkleAboutController ();

        private NSImage AboutImage;
        private NSImageView AboutImageView;
        private NSTextField VersionTextField;
        private NSTextField UpdatesTextField;
        private NSTextField CreditsTextField;


        public SparkleAbout (IntPtr handle) : base (handle) { }

        public SparkleAbout () : base ()
        {
            SetFrame (new RectangleF (0, 0, 640, 281), true);
            Center ();

            Delegate    = new SparkleAboutDelegate ();
            StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
            Title       = "About SparkleShare";
            MaxSize     = new SizeF (640, 281);
            MinSize     = new SizeF (640, 281);
            HasShadow   = true;
            BackingType = NSBackingStore.Buffered;

            CreateAbout ();
            OrderFrontRegardless ();
            MakeKeyAndOrderFront (this);

            Controller.NewVersionEvent += delegate (string new_version) {
                InvokeOnMainThread (delegate {
                    UpdatesTextField.StringValue = "A newer version (" + new_version + ") is available!";
                    UpdatesTextField.TextColor   =
                        NSColor.FromCalibratedRgba (0.96f, 0.47f, 0.0f, 1.0f); // Tango Orange #2
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                InvokeOnMainThread (delegate {
                    UpdatesTextField.StringValue = "You are running the latest version.";
                    UpdatesTextField.TextColor   =
                        NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f); // Tango Sky Blue #1
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                InvokeOnMainThread (delegate {
                    UpdatesTextField.StringValue = "Checking for updates...";
                    UpdatesTextField.TextColor   =
                        NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f); // Tango Sky Blue #1
                });
            };
        }


        private void CreateAbout ()
        {
            string about_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                "Pixmaps", "about.png");

            AboutImage = new NSImage (about_image_path) {
                Size = new SizeF (640, 260)
            };

            AboutImageView = new NSImageView () {
                Image = AboutImage,
                Frame = new RectangleF (0, 0, 640, 260)
            };


            VersionTextField = new NSTextField () {
                StringValue     = "version " + Controller.RunningVersion,
                Frame           = new RectangleF (295, 140, 318, 22),
                BackgroundColor = NSColor.White,
                Bordered        = false,
                Editable        = false,
                DrawsBackground = false,
                TextColor       = NSColor.White,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11)
            };

            UpdatesTextField = new NSTextField () {
                StringValue     = "Checking for updates...",
                Frame           = new RectangleF (295, Frame.Height - 232, 318, 98),
                Bordered        = false,
                Editable        = false,
                DrawsBackground = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                TextColor       =
                    NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f) // Tango Sky Blue #1
            };

            CreditsTextField = new NSTextField () {
                StringValue     = @"Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others." +
                                   "\n" +
                                   "\n" +
                                   "SparkleShare is Free and Open Source Software. You are free to use, modify, and redistribute it " +
                                   "under the GNU General Public License version 3 or later.",
                Frame           = new RectangleF (295, Frame.Height - 260, 318, 98),
                TextColor       = NSColor.White,
                DrawsBackground = false,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
            };

//            WebsiteButton.Activated += delegate {
//                NSUrl url = new NSUrl ("http://www.sparkleshare.org/");
//                NSWorkspace.SharedWorkspace.OpenUrl (url);
//            };

//            CreditsButton.Activated += delegate {
//                NSUrl url = new NSUrl ("http://www.sparkleshare.org/credits/");
//                NSWorkspace.SharedWorkspace.OpenUrl (url);
//            };

            ContentView.AddSubview (AboutImageView);

            ContentView.AddSubview (VersionTextField);
            ContentView.AddSubview (UpdatesTextField);
            ContentView.AddSubview (CreditsTextField);
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
