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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using SparkleLib; // Only used for SparkleChangeSet

namespace SparkleShare {

    public class SparkleLog : NSWindow {

        public readonly string LocalPath;

        private WebView WebView;
        private NSBox Separator;
        private string HTML;
        private NSPopUpButton popup_button;
        private NSProgressIndicator ProgressIndicator;
        private List<SparkleChangeSet> change_sets = SparkleShare.Controller.GetLog ();

        public SparkleLog (IntPtr handle) : base (handle) { } 
        
        public SparkleLog (string path) : base ()
        {
            LocalPath = path;

            Delegate = new SparkleLogDelegate ();

            SetFrame (new RectangleF (0, 0, 480, 640), true);
            Center ();
            
            // Open slightly off center for each consecutive window
            if (SparkleUI.OpenLogs.Count > 0) {
                RectangleF offset = new RectangleF (Frame.X + (SparkleUI.OpenLogs.Count * 20),
                    Frame.Y - (SparkleUI.OpenLogs.Count * 20), Frame.Width, Frame.Height);

                SetFrame (offset, true);
            }
            
            StyleMask = (NSWindowStyle.Closable |
                         NSWindowStyle.Miniaturizable |
                         NSWindowStyle.Titled);

            MaxSize     = new SizeF (480, 640);
            MinSize     = new SizeF (480, 640);
            HasShadow   = true;            
            BackingType = NSBackingStore.Buffered;

            CreateEventLog ();
            UpdateEventLog ();
            
            OrderFrontRegardless ();
        }


        private void CreateEventLog ()
        {
            Title = "Recent Events";

            Separator = new NSBox (new RectangleF (0, 573, 480, 1)) {
                BorderColor = NSColor.LightGray,
                BoxType = NSBoxType.NSBoxCustom
            };

            ContentView.AddSubview (Separator);

            this.popup_button = new NSPopUpButton (new RectangleF (480 - 156 - 8, 640 - 31 - 26, 156, 26), false);
            //this.popup_button.
            this.popup_button.AddItem ("All Folders");
            this.popup_button.Menu.AddItem (NSMenuItem.SeparatorItem);
            this.popup_button.AddItems (SparkleShare.Controller.Folders.ToArray ());


            this.popup_button.Activated += delegate {
                Console.WriteLine (this.popup_button.SelectedItem.Title);
            };

            ContentView.AddSubview (this.popup_button);

            ProgressIndicator = new NSProgressIndicator () {
                Style = NSProgressIndicatorStyle.Spinning,
                Frame = new RectangleF (Frame.Width / 2 - 10, Frame.Height / 2 + 10, 20, 20)
            };

            ProgressIndicator.StartAnimation (this);

            WebView = new WebView (new RectangleF (0, 0, 480, 573   ), "", ""){
                PolicyDelegate = new SparkleWebPolicyDelegate ()
            };

            Update ();
        }


        public void UpdateEventLog ()
        {
            InvokeOnMainThread (delegate {
                    if (HTML == null)
                        ContentView.AddSubview (ProgressIndicator);
            });

            Thread thread = new Thread (new ThreadStart (delegate {
                using (NSAutoreleasePool pool = new NSAutoreleasePool ()) {
                    GenerateHTML ();
                    AddHTML ();
                }
            }));

            thread.Start ();
        }


        private void GenerateHTML ()
        {
            HTML = SparkleShare.Controller.GetHTMLLog ();

            HTML = HTML.Replace ("<!-- $body-font-family -->", "Lucida Grande");
            HTML = HTML.Replace ("<!-- $day-entry-header-font-size -->", "13.6px");
            HTML = HTML.Replace ("<!-- $body-font-size -->", "13.4px");
            HTML = HTML.Replace ("<!-- $secondary-font-color -->", "#bbb");
            HTML = HTML.Replace ("<!-- $small-color -->", "#ddd");
            HTML = HTML.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
            HTML = HTML.Replace ("<!-- $a-color -->", "#0085cf");
            HTML = HTML.Replace ("<!-- $a-hover-color -->", "#009ff8");
            HTML = HTML.Replace ("<!-- $no-buddy-icon-background-image -->",
                "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "avatar-default.png"));
            HTML = HTML.Replace ("<!-- $document-added-background-image -->",
                "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-added-12.png"));
            HTML = HTML.Replace ("<!-- $document-deleted-background-image -->",
                "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-deleted-12.png"));
            HTML = HTML.Replace ("<!-- $document-edited-background-image -->",
                "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-edited-12.png"));
            HTML = HTML.Replace ("<!-- $document-moved-background-image -->",
                "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-moved-12.png"));
        }


        private void AddHTML ()
        {
            InvokeOnMainThread (delegate {
                    if (ProgressIndicator.Superview == ContentView)
                        ProgressIndicator.RemoveFromSuperview ();
        
                    WebView.MainFrame.LoadHtmlString (HTML, new NSUrl (""));
        
                    ContentView.AddSubview (WebView);
                    Update ();

            });
        }
    }


    public class SparkleLogDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as SparkleLog).OrderOut (this);
            return false;
        }
    }
    
    
    public class SparkleWebPolicyDelegate : WebPolicyDelegate {
        
        public override void DecidePolicyForNavigation (WebView web_view, NSDictionary action_info,
                                                        NSUrlRequest request, WebFrame frame, NSObject decision_token)
        {
            string file_path = request.Url.ToString ();
            file_path = file_path.Replace ("%20", " ");
            
            NSWorkspace.SharedWorkspace.OpenFile (file_path);
        }
    }
}
