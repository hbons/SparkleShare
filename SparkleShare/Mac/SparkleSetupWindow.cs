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

    public class SparkleSetupWindow : NSWindow {

        public List <NSButton> Buttons;
        public string Header;
        public string Description;

        private NSImage SideSplash;
        private NSImageView SideSplashView;
        private NSTextField HeaderTextField;
        private NSTextField DescriptionTextField;


        public SparkleSetupWindow () : base ()
        {
            SetFrame (new RectangleF (0, 0, 640, 420), true);

            StyleMask   = NSWindowStyle.Titled;
            MaxSize     = new SizeF (640, 420);
            MinSize     = new SizeF (640, 420);
            HasShadow   = true;
            BackingType = NSBackingStore.Buffered;

            Center ();

            string side_splash_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                "Pixmaps", "side-splash.png");

            SideSplash = new NSImage (side_splash_path) {
                Size = new SizeF (150, 482)
            };

            SideSplashView = new NSImageView () {
                Image = SideSplash,
                Frame = new RectangleF (0, 0, 150, 482)
            };

            Buttons = new List <NSButton> ();

            HeaderTextField = new NSTextField () {
                Frame           = new RectangleF (190, Frame.Height - 100, Frame.Width, 48),
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Font            = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Bold, 0, 15)
            };
            
            DescriptionTextField = new NSTextField () {
                Frame           = new RectangleF (190, Frame.Height - 130, 640 - 240, 44),
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Font            = SparkleUI.Font
            };

            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);
            
            OrderFrontRegardless ();
            Program.UI.UpdateDockIconVisibility ();
        }

        
        public void Reset ()
        {
            ContentView.Subviews = new NSView [0];
            Buttons              = new List <NSButton> ();
            Header               = "";
            Description          = "";
        }


        public void ShowAll ()
        {
            HeaderTextField.StringValue      = Header;
            DescriptionTextField.StringValue = Description;
            
            ContentView.AddSubview (HeaderTextField);

            if (!Description.Equals (""))
                ContentView.AddSubview (DescriptionTextField);
            
            ContentView.AddSubview (SideSplashView);
            
            int i = 1;
            if (Buttons.Count > 0) {
                DefaultButtonCell = Buttons [0].Cell;
                
                foreach (NSButton button in Buttons) {
                    button.BezelStyle = NSBezelStyle.Rounded;
                    button.Frame = new RectangleF (Frame.Width - 15 - (105 * i), 12, 105, 32);

                    // Make the button a bit wider if the text is
                    // likely to be longer
                    if (button.Title.Contains (" "))
                        button.Frame = new RectangleF (Frame.Width - 30 - (105 * i), 12, 120, 32);
                    
                    button.Font = SparkleUI.Font;
                    ContentView.AddSubview (button);
                    i++;
                }
            }

            RecalculateKeyViewLoop ();
        }


        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.AddWindowsItem (this, "SparkleShare Setup", false);
            base.OrderFrontRegardless ();
        }


        public override void PerformClose (NSObject sender)
        {
            base.OrderOut (this);
            NSApplication.SharedApplication.RemoveWindowsItem (this);
            Program.UI.UpdateDockIconVisibility ();

            return;
        }


        public override bool AcceptsFirstResponder ()
        {
            return true;
        }
    }
}
