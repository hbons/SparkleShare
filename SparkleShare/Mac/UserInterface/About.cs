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

using MonoMac.AppKit;
using MonoMac.Foundation;

namespace SparkleShare {

    public class About : NSWindow {

        public AboutController Controller = new AboutController ();

        private NSTextField version_text_field, updates_text_field, credits_text_field;
        private SparkleLink website_link, credits_link, report_problem_link, debug_log_link;
        private NSImage about_image;
        private NSImageView about_image_view;
        private NSButton hidden_close_button;


        public About (IntPtr handle) : base (handle) { }

        public About () : base ()
        {
            SetFrame (new RectangleF (0, 0, 640, 281), true);
            Center ();

            Delegate    = new SparkleAboutDelegate ();
            StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
            Title       = "About SparkleShare";
            MaxSize     = new SizeF (640, 281);
            MinSize     = new SizeF (640, 281);
            HasShadow   = true;
            IsOpaque    = false;
            BackingType = NSBackingStore.Buffered;
            Level       = NSWindowLevel.Floating;

            this.hidden_close_button = new NSButton () {
                Frame                     = new RectangleF (0, 0, 0, 0),
                KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask,
                KeyEquivalent             = "w"
            };

            CreateAbout ();


            this.hidden_close_button.Activated += delegate { Controller.WindowClosed (); };

            Controller.HideWindowEvent += delegate {
                SparkleShare.Controller.Invoke (() => PerformClose (this));
            };

            Controller.ShowWindowEvent += delegate {
                SparkleShare.Controller.Invoke (() => OrderFrontRegardless ());
            };

            Controller.UpdateLabelEvent += delegate (string text) {
                SparkleShare.Controller.Invoke (() => { this.updates_text_field.StringValue = text; });
            };


            ContentView.AddSubview (this.hidden_close_button);
        }


        private void CreateAbout ()
        {
            this.about_image = NSImage.ImageNamed ("about");
            this.about_image.Size = new SizeF (720, 260);

            this.about_image_view = new NSImageView () {
                Image = this.about_image,
                Frame = new RectangleF (0, 0, 720, 260)
            };

            this.version_text_field = new SparkleLabel ("version " + Controller.RunningVersion, NSTextAlignment.Left) {
                DrawsBackground = false,
                Frame           = new RectangleF (295, 140, 318, 22),
                TextColor       = NSColor.White
            };

            this.updates_text_field = new SparkleLabel ("Checking for updates...", NSTextAlignment.Left) {
                DrawsBackground = false,
                Frame           = new RectangleF (295, Frame.Height - 232, 318, 98),
                TextColor       = NSColor.FromCalibratedRgba (1.0f, 1.0f, 1.0f, 0.5f)
            };

            this.credits_text_field = new SparkleLabel (
                @"Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others" +
                "\n\n" + 
                "SparkleShare is Open Source. You are free to use, modify, and redistribute it " +
                "under the GNU GPLv3", NSTextAlignment.Left) {
                
                DrawsBackground = false,
                Frame           = new RectangleF (295, Frame.Height - 260, 318, 98),
                TextColor       = NSColor.White
            };

            this.website_link       = new SparkleLink ("Website", Controller.WebsiteLinkAddress);
            this.website_link.Frame = new RectangleF (new PointF (295, 25), this.website_link.Frame.Size);
            
            this.credits_link       = new SparkleLink ("Credits", Controller.CreditsLinkAddress);
            this.credits_link.Frame = new RectangleF (
                new PointF (this.website_link.Frame.X + this.website_link.Frame.Width + 10, 25),
                this.credits_link.Frame.Size);
            
            this.report_problem_link       = new SparkleLink ("Report a problem", Controller.ReportProblemLinkAddress);
            this.report_problem_link.Frame = new RectangleF (
                new PointF (this.credits_link.Frame.X + this.credits_link.Frame.Width + 10, 25),
                this.report_problem_link.Frame.Size);
            
            this.debug_log_link       = new SparkleLink ("Debug log", Controller.DebugLogLinkAddress);
            this.debug_log_link.Frame = new RectangleF (
                new PointF (this.report_problem_link.Frame.X + this.report_problem_link.Frame.Width + 10, 25),
                this.debug_log_link.Frame.Size);

            ContentView.AddSubview (this.about_image_view);
            ContentView.AddSubview (this.version_text_field);
            ContentView.AddSubview (this.updates_text_field);
            ContentView.AddSubview (this.credits_text_field);
            ContentView.AddSubview (this.website_link);
            ContentView.AddSubview (this.credits_link);
            ContentView.AddSubview (this.report_problem_link);
            ContentView.AddSubview (this.debug_log_link);
        }


        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);
            base.OrderFrontRegardless ();
        }


        public override void PerformClose (NSObject sender)
        {
            base.OrderOut (this);
            return;
        }


        private class SparkleAboutDelegate : NSWindowDelegate {
            
            public override bool WindowShouldClose (NSObject sender)
            {
                (sender as About).Controller.WindowClosed ();
                return false;
            }
        }
        
        
        private class SparkleLink : NSTextField {
            
            private NSUrl url;
            
            
            public SparkleLink (string text, string address) : base ()
            {
                this.url = new NSUrl (address);
                
                AllowsEditingTextAttributes = true;
                BackgroundColor = NSColor.White;
                Bordered        = false;
                DrawsBackground = false;
                Editable        = false;
                Selectable      = false;
                
                NSData name_data = NSData.FromString ("<a href='" + this.url +
                    "' style='font-size: 9pt; font-family: \"Helvetica Neue\"; color: #739ECF'>" + text + "</a></font>");
                
                NSDictionary name_dictionary       = new NSDictionary();
                NSAttributedString name_attributes = new NSAttributedString (name_data, new NSUrl ("file://"), out name_dictionary);
                
                NSMutableAttributedString s = new NSMutableAttributedString ();
                s.Append (name_attributes);
                
                Cell.AttributedStringValue = s;
                SizeToFit ();
            }
            
            
            public override void MouseUp (NSEvent e)
            {
                SparkleShare.Controller.OpenWebsite (this.url.ToString ());
            }
            
            
            public override void ResetCursorRects ()
            {
                AddCursorRect (Bounds, NSCursor.PointingHandCursor);
            }
        }
    }
}
