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
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleEventLog : NSWindow {

        public SparkleEventLogController Controller = new SparkleEventLogController ();
        public float TitlebarHeight;

        private WebView web_view;
        private NSBox background;
        private NSPopUpButton popup_button;
        private NSProgressIndicator progress_indicator;
        private NSTextField size_label, size_label_value, history_label, history_label_value;
        private NSButton hidden_close_button;


        public SparkleEventLog (IntPtr handle) : base (handle) { }

        public SparkleEventLog () : base ()
        {
            Title    = "Recent Changes";
            Delegate = new SparkleEventsDelegate ();

            int min_width  = 480;
            int min_height = 640;
            int height     = (int) (NSScreen.MainScreen.Frame.Height * 0.85);

            float x    = (float) (NSScreen.MainScreen.Frame.Width * 0.61);
            float y    = (float) (NSScreen.MainScreen.Frame.Height * 0.5 - (height * 0.5));

            SetFrame (
                new RectangleF (
                    new PointF (x, y),
                    new SizeF (min_width, height)),
                true);

            StyleMask = (NSWindowStyle.Closable | NSWindowStyle.Miniaturizable |
                         NSWindowStyle.Titled | NSWindowStyle.Resizable);

            MinSize        = new SizeF (min_width, min_height);
            HasShadow      = true;
            BackingType    = NSBackingStore.Buffered;
            TitlebarHeight = Frame.Height - ContentView.Frame.Height;


            this.web_view = new WebView (new RectangleF (0, 0, 481, 579), "", "") {
                Frame = new RectangleF (new PointF (0, 0),
                    new SizeF (ContentView.Frame.Width, ContentView.Frame.Height - 39))
            };


            this.hidden_close_button = new NSButton () {
                KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask,
                KeyEquivalent = "w"
            };

            this.hidden_close_button.Activated += delegate {
                Controller.WindowClosed ();
            };


            this.size_label = new NSTextField () {
                Alignment       = NSTextAlignment.Right,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (0, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)),
                StringValue     = "Size:",
                Font            = SparkleUI.BoldFont
            };

            this.size_label_value = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (60, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)),
                StringValue     = "…",
                Font            = SparkleUI.Font
            };


            this.history_label = new NSTextField () {
                Alignment       = NSTextAlignment.Right,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (130, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)),
                StringValue     = "History:",
                Font            = SparkleUI.BoldFont
            };

            this.history_label_value = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (190, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)
                ),
                StringValue     = "…",
                Font            = SparkleUI.Font
            };

            this.popup_button = new NSPopUpButton () {
                Frame = new RectangleF (
                    new PointF (ContentView.Frame.Width - 156 - 12, ContentView.Frame.Height - 33),
                    new SizeF (156, 26)),
                PullsDown = false
            };

            this.background = new NSBox () {
                Frame = new RectangleF (
                    new PointF (-1, -1),
                    new SizeF (Frame.Width + 2, this.web_view.Frame.Height + 2)),
                FillColor = NSColor.White,
                BorderColor = NSColor.LightGray,
                BoxType = NSBoxType.NSBoxCustom
            };

            this.progress_indicator = new NSProgressIndicator () {
                Frame = new RectangleF (
                    new PointF (Frame.Width / 2 - 10, this.web_view.Frame.Height / 2 + 10),
                    new SizeF (20, 20)),
                Style = NSProgressIndicatorStyle.Spinning
            };

            this.progress_indicator.StartAnimation (this);

            ContentView.AddSubview (this.size_label);
            ContentView.AddSubview (this.size_label_value);
            ContentView.AddSubview (this.history_label);
            ContentView.AddSubview (this.history_label_value);
            ContentView.AddSubview (this.popup_button);
            ContentView.AddSubview (this.progress_indicator);
            ContentView.AddSubview (this.background);
            ContentView.AddSubview (this.hidden_close_button);

            (Delegate as SparkleEventsDelegate).WindowResized += delegate (SizeF new_window_size) {
                Program.Controller.Invoke (() => Relayout (new_window_size));
            };


            // Hook up the controller events
            Controller.HideWindowEvent += delegate {
                Program.Controller.Invoke (() => {
                    this.progress_indicator.Hidden = true;
                    PerformClose (this);
                });
            };

            Controller.ShowWindowEvent += delegate {
                Program.Controller.Invoke (() => OrderFrontRegardless ());
            };
            
            Controller.UpdateChooserEvent += delegate (string [] folders) {
                Program.Controller.Invoke (() => UpdateChooser (folders));
            };

            Controller.UpdateChooserEnablementEvent += delegate (bool enabled) {
                Program.Controller.Invoke (() => { this.popup_button.Enabled = enabled; });
            };

            Controller.UpdateContentEvent += delegate (string html) {
                Program.Controller.Invoke (() => {
                    this.progress_indicator.Hidden = true;
                    UpdateContent (html);
                });
            };

            Controller.ContentLoadingEvent += delegate {
                Program.Controller.Invoke (() => {
                    this.web_view.RemoveFromSuperview ();
                    this.progress_indicator.Hidden = false;
                    this.progress_indicator.StartAnimation (this);
                });
            };
            
            Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
                Program.Controller.Invoke (() => {
                    this.size_label_value.StringValue    = size;
                    this.history_label_value.StringValue = history_size;
                });
            };

            Controller.ShowSaveDialogEvent += delegate (string file_name, string target_folder_path) {
                Program.Controller.Invoke (() => {
                    NSSavePanel panel = new NSSavePanel () {
                        DirectoryUrl         = new NSUrl (target_folder_path, true),
                        NameFieldStringValue = file_name,
                        ParentWindow         = this,
                        Title                = "Restore from History",
                        PreventsApplicationTerminationWhenModal = false
                    };

                    if ((NSPanelButtonType) panel.RunModal () == NSPanelButtonType.Ok) {
                        string target_file_path = Path.Combine (panel.DirectoryUrl.RelativePath, panel.NameFieldStringValue);
                        Controller.SaveDialogCompleted (target_file_path);
                    
                    } else {
                        Controller.SaveDialogCancelled ();
                    }
                });
            };
        }


        public void Relayout (SizeF new_window_size)
        {
            this.web_view.Frame = new RectangleF (this.web_view.Frame.Location,
                new SizeF (new_window_size.Width, new_window_size.Height - TitlebarHeight - 39));

            this.background.Frame = new RectangleF (this.background.Frame.Location,
                new SizeF (new_window_size.Width, new_window_size.Height - TitlebarHeight - 37));

            this.size_label.Frame = new RectangleF (
                new PointF (this.size_label.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.size_label.Frame.Size);

            this.size_label_value.Frame = new RectangleF (
                new PointF (this.size_label_value.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.size_label_value.Frame.Size);

            this.history_label.Frame = new RectangleF (
                new PointF (this.history_label.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.history_label.Frame.Size);

            this.history_label_value.Frame = new RectangleF (
                new PointF (this.history_label_value.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.history_label_value.Frame.Size);

            this.progress_indicator.Frame = new RectangleF (
                new PointF (new_window_size.Width / 2 - 10, this.web_view.Frame.Height / 2 + 10),
                this.progress_indicator.Frame.Size);

            this.popup_button.RemoveFromSuperview (); // Needed to prevent redraw glitches

            this.popup_button.Frame = new RectangleF (
                new PointF (new_window_size.Width - this.popup_button.Frame.Width - 12, new_window_size.Height - TitlebarHeight - 33),
                this.popup_button.Frame.Size);

            ContentView.AddSubview (this.popup_button);
        }


        public void UpdateChooser (string [] folders)
        {
            if (folders == null)
                folders = Controller.Folders;

            this.popup_button.Cell.ControlSize = NSControlSize.Small;
            this.popup_button.Font = NSFontManager.SharedFontManager.FontWithFamily (
                "Lucida Grande", NSFontTraitMask.Condensed, 0, NSFont.SmallSystemFontSize);

            this.popup_button.RemoveAllItems ();

            this.popup_button.AddItem ("Summary");
            this.popup_button.Menu.AddItem (NSMenuItem.SeparatorItem);
			
			int row = 2;
       		foreach (string folder in folders) {
                this.popup_button.AddItem (folder);
				
				if (folder.Equals (Controller.SelectedFolder))
					this.popup_button.SelectItem (row);
				
				row++;
        	}
			
            this.popup_button.AddItems (folders);

            this.popup_button.Activated += delegate {
                Program.Controller.Invoke (() => {
                    if (this.popup_button.IndexOfSelectedItem == 0)
                        Controller.SelectedFolder = null;
                    else
                        Controller.SelectedFolder = this.popup_button.SelectedItem.Title;
                });
            };
        }


        public void UpdateContent (string html)
        {
		    string pixmaps_path = "file://" + NSBundle.MainBundle.ResourcePath;
			
            html = html.Replace ("<!-- $body-font-family -->", "Lucida Grande");
            html = html.Replace ("<!-- $day-entry-header-font-size -->", "13.6px");
            html = html.Replace ("<!-- $body-font-size -->", "13.4px");
            html = html.Replace ("<!-- $secondary-font-color -->", "#bbb");
            html = html.Replace ("<!-- $small-color -->", "#ddd");
            html = html.Replace ("<!-- $small-font-size -->", "10px");
            html = html.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
            html = html.Replace ("<!-- $a-color -->", "#0085cf");
            html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace ("<!-- $pixmaps-path -->", pixmaps_path);
            html = html.Replace ("<!-- $document-added-background-image -->", pixmaps_path + "/document-added-12.png");
            html = html.Replace ("<!-- $document-deleted-background-image -->", pixmaps_path + "/document-deleted-12.png");
            html = html.Replace ("<!-- $document-edited-background-image -->", pixmaps_path + "/document-edited-12.png");
            html = html.Replace ("<!-- $document-moved-background-image -->", pixmaps_path + "/document-moved-12.png");
			
            this.web_view = new WebView (new RectangleF (0, 0, 481, 579), "", "") {
                Frame = new RectangleF (new PointF (0, 0), new SizeF (ContentView.Frame.Width, ContentView.Frame.Height - 39))
            };

            this.web_view.MainFrame.LoadHtmlString (html, new NSUrl (""));

            this.web_view.PolicyDelegate = new SparkleWebPolicyDelegate ();
            ContentView.AddSubview (this.web_view);

            (this.web_view.PolicyDelegate as SparkleWebPolicyDelegate).LinkClicked += delegate (string href) {
                if (href.StartsWith ("file:///"))
                    href = href.Substring (7);

                Controller.LinkClicked (href);
            };

            this.progress_indicator.Hidden = true;
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


    public class SparkleEventsDelegate : NSWindowDelegate {

        public event WindowResizedHandler WindowResized = delegate { };
        public delegate void WindowResizedHandler (SizeF new_window_size);

        public override SizeF WillResize (NSWindow sender, SizeF to_frame_size)
        {
            WindowResized (to_frame_size);
            return to_frame_size;
        }

        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as SparkleEventLog).Controller.WindowClosed ();
            return false;
        }
    }
    
    
    public class SparkleWebPolicyDelegate : WebPolicyDelegate {

        public event LinkClickedHandler LinkClicked = delegate { };
        public delegate void LinkClickedHandler (string href);

        public override void DecidePolicyForNavigation (WebView web_view, NSDictionary action_info,
            NSUrlRequest request, WebFrame frame, NSObject decision_token)
        {
            LinkClicked (request.Url.ToString ());
        }
    }
}
