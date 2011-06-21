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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using SparkleLib; // Only used for SparkleChangeSet

namespace SparkleShare {

    public class SparkleEventLog : NSWindow {

        private WebView WebView;
        private NSBox Separator;
        private string HTML;
        private NSPopUpButton popup_button;
        private NSProgressIndicator ProgressIndicator;
        private List<SparkleChangeSet> change_sets;
        private string selected_log = null;


        public SparkleEventLog (IntPtr handle) : base (handle) { }

        public SparkleEventLog () : base ()
        {
            Title    = "Recent Events";
            Delegate = new SparkleEventsDelegate ();

            SetFrame (new RectangleF (0, 0, 480, 640), true);
            Center ();

            StyleMask = (NSWindowStyle.Closable |
                         NSWindowStyle.Miniaturizable |
                         NSWindowStyle.Titled);

            MaxSize     = new SizeF (480, 640);
            MinSize     = new SizeF (480, 640);
            HasShadow   = true;
            BackingType = NSBackingStore.Buffered;

            CreateEvents ();
            UpdateEvents (false);
            UpdateChooser ();

            OrderFrontRegardless ();
        }


        private void CreateEvents ()
        {
            Separator = new NSBox (new RectangleF (0, 579, 480, 1)) {
                BorderColor = NSColor.LightGray,
                BoxType = NSBoxType.NSBoxCustom
            };

            ContentView.AddSubview (Separator);

            WebView = new WebView (new RectangleF (0, 0, 480, 579), "", "") {
                PolicyDelegate = new SparkleWebPolicyDelegate ()
            };

            ProgressIndicator = new NSProgressIndicator () {
                Style = NSProgressIndicatorStyle.Spinning,
                Frame = new RectangleF (WebView.Frame.Width / 2 - 10, WebView.Frame.Height / 2 + 10, 20, 20)
            };

            ProgressIndicator.StartAnimation (this);
            Update ();
        }


        public void UpdateChooser ()
        {
            if (this.popup_button != null)
                this.popup_button.RemoveFromSuperview ();

            this.popup_button = new NSPopUpButton () {
                Frame     = new RectangleF (480 - 156 - 8, 640 - 31 - 24, 156, 26),
                PullsDown = false
            };

            this.popup_button.Cell.ControlSize = NSControlSize.Small;
            this.popup_button.Font = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Condensed, 0, NSFont.SmallSystemFontSize);

            this.popup_button.AddItem ("All Folders");
            this.popup_button.Menu.AddItem (NSMenuItem.SeparatorItem);
            this.popup_button.AddItems (SparkleShare.Controller.Folders.ToArray ());

            if (this.selected_log != null &&
                !SparkleShare.Controller.Folders.Contains (this.selected_log)) {

                this.selected_log = null;
            }

            this.popup_button.Activated += delegate {
                if (this.popup_button.IndexOfSelectedItem == 0)
                    this.selected_log = null;
                else
                    this.selected_log = this.popup_button.SelectedItem.Title;

                UpdateEvents (false);
            };

            ContentView.AddSubview (this.popup_button);
        }


        public void UpdateEvents ()
        {
            UpdateEvents (true);
        }


        public void UpdateEvents (bool silent)
        {
            if (!silent) {
                InvokeOnMainThread (delegate {
                    if (WebView.Superview == ContentView)
                        WebView.RemoveFromSuperview ();
    
                    ContentView.AddSubview (ProgressIndicator);
                });
            }

            Thread thread = new Thread (new ThreadStart (delegate {
                using (NSAutoreleasePool pool = new NSAutoreleasePool ()) {
                    Stopwatch watch = new Stopwatch ();
                    watch.Start ();
                    this.change_sets = SparkleShare.Controller.GetLog (this.selected_log);
                    GenerateHTML ();
                    watch.Stop ();

                    // A short delay is less annoying than
                    // a flashing window
                    if (watch.ElapsedMilliseconds < 500 && !silent)
                        Thread.Sleep (500 - (int) watch.ElapsedMilliseconds);

                    AddHTML ();
                }
            }));

            thread.Start ();
        }


        private void GenerateHTML ()
        {
            HTML = SparkleShare.Controller.GetHTMLLog (this.change_sets);

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


    public class SparkleEventsDelegate : NSWindowDelegate {
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as SparkleEventLog).OrderOut (this);
            return false;
        }
    }
    
    
    public class SparkleWebPolicyDelegate : WebPolicyDelegate {
        
        public override void DecidePolicyForNavigation (WebView web_view, NSDictionary action_info,
            NSUrlRequest request, WebFrame frame, NSObject decision_token)
        {
            string url = request.Url.ToString ();
            Console.WriteLine (url);
            string id = url.Substring (0, url.IndexOf ("%20"));
            string note = url.Substring (url.IndexOf ("%20") + 3);
            Console.WriteLine (id + " " + note);

            SparkleShare.Controller.Repositories [0].AddNote (id, note);
            return;


            string file_path = request.Url.ToString ();
            file_path = file_path.Replace ("%20", " ");
            
            NSWorkspace.SharedWorkspace.OpenFile (file_path);
        }
    }
}
