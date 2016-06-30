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

    public class Note : NSWindow {

        public NoteController Controller = new NoteController ();

        private NSImage user_image, balloon_image;
        private NSImageView user_image_view, balloon_image_view;
        private NSButton hidden_close_button, cancel_button, sync_button;
        private NSBox cover;
        private NSTextField user_name_text_field, user_email_text_field, balloon_text_field;


        public Note (IntPtr handle) : base (handle) { }

        public Note () : base ()
        {
            SetFrame (new RectangleF (0, 0, 480, 240), true);
            Center ();

            Delegate    = new SparkleNoteDelegate ();
            StyleMask   = (NSWindowStyle.Closable | NSWindowStyle.Titled);
            Title       = "Add Note";
            MaxSize     = new SizeF (480, 240);
            MinSize     = new SizeF (480, 240);
            HasShadow   = true;
            IsOpaque    = false;
            BackingType = NSBackingStore.Buffered;
            Level       = NSWindowLevel.Floating;

            this.hidden_close_button = new NSButton () {
                Frame                     = new RectangleF (0, 0, 0, 0),
                KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask,
                KeyEquivalent             = "w"
            };

            CreateNote ();


            this.hidden_close_button.Activated += delegate { Controller.WindowClosed (); };

            Controller.HideWindowEvent += delegate {
                SparkleShare.Controller.Invoke (() => PerformClose (this));
            };

            Controller.ShowWindowEvent += delegate {
                SparkleShare.Controller.Invoke (() => OrderFrontRegardless ());
                CreateNote ();
            };

            Controller.UpdateTitleEvent += delegate (string title) {
                SparkleShare.Controller.Invoke (() => { Title = title; });
            };


            ContentView.AddSubview (this.hidden_close_button);
        }


        private void CreateNote ()
        {
            this.cover = new NSBox () {
                Frame = new RectangleF (
                    new PointF (-1, 58),
                    new SizeF (Frame.Width + 2, this.ContentView.Frame.Height + 1)),
                FillColor = NSColor.FromCalibratedRgba (0.77f, 0.77f, 0.75f, 1.0f),
                BorderColor = NSColor.LightGray,
                BoxType = NSBoxType.NSBoxCustom
            };


            this.user_name_text_field = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.FromCalibratedRgba (0.77f, 0.77f, 0.75f, 1.0f),
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (85, ContentView.Frame.Height - 41),
                    new SizeF (320, 22)),
                StringValue     = SparkleShare.Controller.CurrentUser.Name,
                Font            = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize)
            };
            
            this.user_email_text_field = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.FromCalibratedRgba (0.77f, 0.77f, 0.75f, 1.0f),
                TextColor       = NSColor.DisabledControlText,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (85, ContentView.Frame.Height - 60),
                    new SizeF (320, 20)),
                StringValue     = SparkleShare.Controller.CurrentUser.Email,
            };

            
            this.balloon_text_field = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.White,
                Bordered        = false,
                Editable        = true,
                Frame           = new RectangleF (
                    new PointF (30, ContentView.Frame.Height - 137),
                    new SizeF (418, 48))
            };

            (this.balloon_text_field.Cell as NSTextFieldCell).PlaceholderString  = "Anything to add?";
            (this.balloon_text_field.Cell as NSTextFieldCell).LineBreakMode      = NSLineBreakMode.ByWordWrapping;
            (this.balloon_text_field.Cell as NSTextFieldCell).UsesSingleLineMode = false;

            this.balloon_text_field.Cell.FocusRingType = NSFocusRingType.None;

            
            this.cancel_button = new NSButton () {
                Title = "Cancel",
                BezelStyle = NSBezelStyle.Rounded,
                Frame      = new RectangleF (Frame.Width - 15 - 105 * 2, 12, 105, 32),
            };

            this.sync_button = new NSButton () {
                Title = "Sync",
                BezelStyle = NSBezelStyle.Rounded,
                Frame      = new RectangleF (Frame.Width - 15 - 105, 12, 105, 32),
            };

            this.cancel_button.Activated += delegate { Controller.CancelClicked (); };
            this.sync_button.Activated += delegate { Controller.SyncClicked (this.balloon_text_field.StringValue); };

            DefaultButtonCell = this.sync_button.Cell;


            if (BackingScaleFactor >= 2)
                this.balloon_image = NSImage.ImageNamed ("text-balloon@2x");
            else
                this.balloon_image = NSImage.ImageNamed ("text-balloon");

            this.balloon_image.Size = new SizeF (438, 72);
            this.balloon_image_view = new NSImageView () { 
                Image = this.balloon_image,
                Frame = new RectangleF (21, ContentView.Frame.Height - 145, 438, 72)
            };


            if (!string.IsNullOrEmpty (Controller.AvatarFilePath))
                this.user_image = new NSImage (Controller.AvatarFilePath);
            else
                this.user_image = NSImage.ImageNamed ("user-icon-default");

            this.user_image.Size = new SizeF (48, 48);
            this.user_image_view = new NSImageView () {
                Image = this.user_image,
                Frame = new RectangleF (21, ContentView.Frame.Height - 65, 48, 48)
            };

            this.user_image_view.WantsLayer          = true;
            this.user_image_view.Layer.CornerRadius  = 5.0f;
            this.user_image_view.Layer.MasksToBounds = true;


            ContentView.AddSubview (this.cover);
            ContentView.AddSubview (this.cancel_button);
            ContentView.AddSubview (this.sync_button);
            ContentView.AddSubview (this.user_name_text_field);
            ContentView.AddSubview (this.user_email_text_field);

            ContentView.AddSubview (this.user_image_view);
            ContentView.AddSubview (this.balloon_image_view);
            ContentView.AddSubview (this.balloon_text_field);

            MakeFirstResponder ((NSResponder) this.balloon_text_field);
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


        public override bool AcceptsFirstResponder ()
        {
            return true;
        }


        class SparkleNoteDelegate : NSWindowDelegate {
            
            public override bool WindowShouldClose (NSObject sender)
            {
                (sender as Note).Controller.WindowClosed ();
                return false;
            }
        }
    }
}
