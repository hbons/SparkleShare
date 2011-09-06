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
using System.Text.RegularExpressions;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleEventLog : NSWindow {

        private SparkleEventLogController controller = new SparkleEventLogController ();

        private WebView web_view = new WebView (new RectangleF (0, 0, 480, 579), "", "") {
            PolicyDelegate = new SparkleWebPolicyDelegate ()
        };

        private NSBox Separator = new NSBox (new RectangleF (0, 579, 480, 1)) {
            BorderColor = NSColor.LightGray,
            BoxType = NSBoxType.NSBoxCustom
        };

        private NSPopUpButton popup_button;
        private NSProgressIndicator progress_indicator;


        public SparkleEventLog (IntPtr handle) : base (handle) { }

        // TODO: Window needs to be made resizable
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

            ContentView.AddSubview (Separator);


            this.progress_indicator = new NSProgressIndicator () {
                Style = NSProgressIndicatorStyle.Spinning,
                Frame = new RectangleF (this.web_view.Frame.Width / 2 - 10, this.web_view.Frame.Height / 2 + 10, 20, 20)
            };

            this.progress_indicator.StartAnimation (this);
            ContentView.AddSubview (this.progress_indicator);


            UpdateContent (null);
            UpdateChooser (null);
            OrderFrontRegardless ();


            // Hook up the controller events
            this.controller.UpdateChooserEvent += delegate (string [] folders) {
                InvokeOnMainThread (delegate {
                    UpdateChooser (folders);
                });
            };

            this.controller.UpdateContentEvent += delegate (string html) {
                InvokeOnMainThread (delegate {
                    UpdateContent (html);
                });
            };

            this.controller.ContentLoadingEvent += delegate {
                InvokeOnMainThread (delegate {
                    if (this.web_view.Superview == ContentView)
                        this.web_view.RemoveFromSuperview ();

                    ContentView.AddSubview (this.progress_indicator);
                });
            };
        }


        public void UpdateChooser (string [] folders)
        {
            if (folders == null)
                folders = this.controller.Folders;

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
            this.popup_button.AddItems (folders);

            this.popup_button.Activated += delegate {
                if (this.popup_button.IndexOfSelectedItem == 0)
                    this.controller.SelectedFolder = null;
                else
                    this.controller.SelectedFolder = this.popup_button.SelectedItem.Title;
            };

            ContentView.AddSubview (this.popup_button);
        }


        public void UpdateContent (string html)
        {
            using (NSAutoreleasePool pool = new NSAutoreleasePool ()) {
                Thread thread = new Thread (new ThreadStart (delegate {
                    if (html == null)
                        html = this.controller.HTML;
    
                    html = html.Replace ("<!-- $body-font-family -->", "Lucida Grande");
                    html = html.Replace ("<!-- $day-entry-header-font-size -->", "13.6px");
                    html = html.Replace ("<!-- $body-font-size -->", "13.4px");
                    html = html.Replace ("<!-- $secondary-font-color -->", "#bbb");
                    html = html.Replace ("<!-- $small-color -->", "#ddd");
                    html = html.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
                    html = html.Replace ("<!-- $a-color -->", "#0085cf");
                    html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
                    html = html.Replace ("<!-- $no-buddy-icon-background-image -->",
                                         "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "avatar-default.png"));
                    html = html.Replace ("<!-- $document-added-background-image -->",
                                         "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-added-12.png"));
                    html = html.Replace ("<!-- $document-deleted-background-image -->",
                                         "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-deleted-12.png"));
                    html = html.Replace ("<!-- $document-edited-background-image -->",
                                         "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-edited-12.png"));
                    html = html.Replace ("<!-- $document-moved-background-image -->",
                                         "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath, "Pixmaps", "document-moved-12.png"));
    
                    InvokeOnMainThread (delegate {
                        if (this.progress_indicator.Superview == ContentView)
                            this.progress_indicator.RemoveFromSuperview ();

                        this.web_view.MainFrame.LoadHtmlString (html, new NSUrl (""));
                        ContentView.AddSubview (this.web_view);
                    });
                }));

                thread.Start ();
            }
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
            
            if (url.StartsWith (Path.VolumeSeparatorChar.ToString ())) {
                string file_path = request.Url.ToString ();
                file_path = file_path.Replace ("%20", " ");

                NSWorkspace.SharedWorkspace.OpenFile (file_path);

            } else {
                Regex regex = new Regex (@"(.+)~(.+)~(.+)");
                Match match = regex.Match (url);

                if (match.Success) {
                    string folder_name = match.Groups [1].Value;
                    string revision    = match.Groups [2].Value;
                    string note        = match.Groups [3].Value;

                    Thread thread = new Thread (new ThreadStart (delegate {
                        Program.Controller.AddNoteToFolder (folder_name, revision, note);
                    }));

                    thread.Start ();
                }
            }
        }
    }
}
