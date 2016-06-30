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


using System.Collections.Generic;
using System.Drawing;

using MonoMac.Foundation;
using MonoMac.AppKit;

namespace SparkleShare {

    public class SetupWindow : NSWindow {

        public List <NSButton> Buttons = new List <NSButton> ();
        public string Header;
        new public string Description;

        NSImage side_splash;
        NSImageView side_splash_view;
        NSTextField header_text_field;
        NSTextField description_text_field;



        public SetupWindow ()
        {
            SetFrame (new RectangleF (0, 0, 640, 420), true);

            StyleMask   = NSWindowStyle.Titled;
            MaxSize     = new SizeF (640, 420);
            MinSize     = new SizeF (640, 420);
            HasShadow   = true;
			IsOpaque    = false;
            BackingType = NSBackingStore.Buffered;
            Level       = NSWindowLevel.Floating;

            Center ();

            this.side_splash = NSImage.ImageNamed ("side-splash");
            this.side_splash.Size = new SizeF (150, 482);

            this.side_splash_view = new NSImageView () {
                Image = this.side_splash,
                Frame = new RectangleF (0, 0, 150, 482)
            };

            this.header_text_field = new SparkleLabel ("", NSTextAlignment.Left) {
                Frame = new RectangleF (190, Frame.Height - 80, Frame.Width, 24),
                Font  = NSFontManager.SharedFontManager.FontWithFamily (
                    UserInterface.FontName, NSFontTraitMask.Bold, 0, 16)
            };

            this.description_text_field = new SparkleLabel ("", NSTextAlignment.Left) {
                Frame = new RectangleF (190, Frame.Height - 130, 640 - 240, 44)
            };

            this.header_text_field.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
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
            this.header_text_field.StringValue      = Header;
            this.description_text_field.StringValue = Description;
            
            ContentView.AddSubview (this.side_splash_view);
            ContentView.AddSubview (this.header_text_field);

            if (!string.IsNullOrEmpty (Description))
                ContentView.AddSubview (this.description_text_field);

            int i = 1;
            int x = 0;
            if (Buttons.Count > 0) {
                DefaultButtonCell = Buttons [0].Cell;
                
                foreach (NSButton button in Buttons) {
                    button.BezelStyle = NSBezelStyle.Rounded;
                    button.Frame      = new RectangleF (Frame.Width - 15 - x - (105 * i), 12, 105, 32);

                    // Make the button a bit wider if the text is likely to be longer
                    if (button.Title.Contains (" ")) {
                        button.SizeToFit ();
                        button.Frame = new RectangleF (Frame.Width - 30 - 15 - (105 * (i - 1)) - button.Frame.Width,
                            12, button.Frame.Width + 30, 32);

                        x += 22;
                    }

                    ContentView.AddSubview (button);
                    i++;
                }
            }

            RecalculateKeyViewLoop ();
        }


        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.AddWindowsItem (this, "SparkleShare Setup", false);
            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);

            base.OrderFrontRegardless ();
        }


        public override void PerformClose (NSObject sender)
        {
            OrderOut (this);
            NSApplication.SharedApplication.RemoveWindowsItem (this);

            return;
        }


        public override bool AcceptsFirstResponder ()
        {
            return true;
        }
    }
}
