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

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleAbout : NSWindow {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private NSImage about_image;
        private NSImageView about_image_view;
        private NSTextField version_text_field;
        private NSTextField updates_text_field;
        private NSTextField credits_text_field;
        private NSButton hidden_close_button;
        private SparkleLink website_link;
        private SparkleLink credits_link;
        private SparkleLink report_problem_link;


        public SparkleAbout (IntPtr handle) : base (handle) { }

        public SparkleAbout () : base ()
        {
            using (var a = new NSAutoreleasePool ())
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

                this.website_link       = new SparkleLink ("Website", new NSUrl (Controller.WebsiteLinkAddress));
                this.website_link.Frame = new RectangleF (new PointF (295, 25), this.website_link.Frame.Size);

                this.credits_link = new SparkleLink ("Credits", new NSUrl (Controller.CreditsLinkAddress));
                this.credits_link.Frame = new RectangleF (
                    new PointF (this.website_link.Frame.X + this.website_link.Frame.Width + 10, 25),
                    this.credits_link.Frame.Size);

                this.report_problem_link = new SparkleLink ("Report a problem", new NSUrl (Controller.ReportProblemLinkAddress));
                this.report_problem_link.Frame = new RectangleF (
                    new PointF (this.credits_link.Frame.X + this.credits_link.Frame.Width + 10, 25),
                    this.report_problem_link.Frame.Size);

                this.hidden_close_button = new NSButton () {
                    Frame                     = new RectangleF (0, 0, 0, 0),
                    KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask,
                    KeyEquivalent             = "w"
                };

                this.hidden_close_button.Activated += delegate {
                    Controller.WindowClosed ();
                };


                ContentView.AddSubview (this.hidden_close_button);

                CreateAbout ();

                ContentView.AddSubview (this.website_link);
                ContentView.AddSubview (this.credits_link);
                ContentView.AddSubview (this.report_problem_link);
            }

            Controller.HideWindowEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        PerformClose (this);
                    });
                }
            };

            Controller.ShowWindowEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        OrderFrontRegardless ();
                    });
                }
            };

            Controller.NewVersionEvent += delegate (string new_version) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        this.updates_text_field.StringValue = "A newer version (" + new_version + ") is available!";
                        this.updates_text_field.TextColor   = NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f);
                    });
                }
            };

            Controller.VersionUpToDateEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        this.updates_text_field.StringValue = "You are running the latest version.";
                        this.updates_text_field.TextColor   = NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f);
                    });
                }
            };

            Controller.CheckingForNewVersionEvent += delegate {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        this.updates_text_field.StringValue = "Checking for updates...";
                        this.updates_text_field.TextColor   = NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f);
                    });
                }
            };
        }


        private void CreateAbout ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                string about_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                    "Pixmaps", "about.png");

                this.about_image = new NSImage (about_image_path) {
                    Size = new SizeF (640, 260)
                };

                this.about_image_view = new NSImageView () {
                    Image = this.about_image,
                    Frame = new RectangleF (0, 0, 640, 260)
                };


                this.version_text_field = new NSTextField () {
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

                this.updates_text_field = new NSTextField () {
                    StringValue     = "Checking for updates...",
                    Frame           = new RectangleF (295, Frame.Height - 232, 318, 98),
                    Bordered        = false,
                    Editable        = false,
                    DrawsBackground = false,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily
                        ("Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                    TextColor       = NSColor.FromCalibratedRgba (0.45f, 0.62f, 0.81f, 1.0f) // Tango Sky Blue #1
                };

                this.credits_text_field = new NSTextField () {
                    StringValue     = @"Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others." +
                                       "\n" +
                                       "\n" +
                                       "SparkleShare is Open Source software. You are free to use, modify, and redistribute it " +
                                       "under the GNU General Public License version 3 or later.",
                    Frame           = new RectangleF (295, Frame.Height - 260, 318, 98),
                    TextColor       = NSColor.White,
                    DrawsBackground = false,
                    Bordered        = false,
                    Editable        = false,
                    Font            = NSFontManager.SharedFontManager.FontWithFamily (
                        "Lucida Grande", NSFontTraitMask.Unbold, 0, 11),
                };

                ContentView.AddSubview (this.about_image_view);
                ContentView.AddSubview (this.version_text_field);
                ContentView.AddSubview (this.updates_text_field);
                ContentView.AddSubview (this.credits_text_field);
            }
        }


        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();

            base.OrderFrontRegardless ();
        }


        public override void PerformClose (NSObject sender)
        {
            base.OrderOut (this);

            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();

            return;
        }
    }


    public class SparkleAboutDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as SparkleAbout).Controller.WindowClosed ();
            return false;
        }
    }


    public class SparkleLink : NSTextField {

        private NSUrl url;


        public SparkleLink (string text, NSUrl url) : base ()
        {
            this.url = url;

            AllowsEditingTextAttributes = true;
            BackgroundColor = NSColor.White;
            Bordered        = false;
            DrawsBackground = false;
            Editable        = false;
            Selectable      = false;

            NSData name_data = NSData.FromString ("<a href='" + url +
                "' style='font-size: 8pt; font-family: \"Lucida Grande\"; color: #739ECF'>" + text + "</a></font>");

            NSDictionary name_dictionary       = new NSDictionary();
            NSAttributedString name_attributes = new NSAttributedString (name_data, new NSUrl ("file://"), out name_dictionary);

            NSMutableAttributedString s = new NSMutableAttributedString ();
            s.Append (name_attributes);

            Cell.AttributedStringValue = s;

            SizeToFit ();
        }


        public override void MouseUp (NSEvent e)
        {
            NSWorkspace.SharedWorkspace.OpenUrl (url);
        }


        public override void ResetCursorRects ()
        {
            AddCursorRect (Bounds, NSCursor.PointingHandCursor);
        }
    }
}
