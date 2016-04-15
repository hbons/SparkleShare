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
using IO = System.IO;

using Gtk;
using WebKit2;

namespace SparkleShare {

    public class EventLog : Window {

        public EventLogController Controller = new EventLogController ();

        private Label size_label;
        private Label history_label;
        private EventBox content_wrapper;
        private ScrolledWindow scrolled_window;
        private VBox spinner_wrapper;
        private Spinner spinner;
        private WebView web_view;

        int pos_x, pos_y;


        public EventLog () : base ("Recent Changes")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            TypeHint = Gdk.WindowTypeHint.Dialog;
            IconName = "org.sparkleshare.SparkleShare";

            SetSizeRequest (480, 640);

            Gdk.Rectangle monitor_0_rect = Gdk.Screen.Default.GetMonitorGeometry (0);
            pos_x = (int) (monitor_0_rect.Width * 0.61);
            pos_y = (int) (monitor_0_rect.Height * 0.5 - (HeightRequest * 0.5));

            Resize (480, (int) (monitor_0_rect.Height * 0.8));

            this.size_label    = new Label () { Xalign = 0, Markup = "<b>Size:</b> …" };
            this.history_label = new Label () { Xalign = 0, Markup = "<b>History:</b> …" };

            this.size_label.SetSizeRequest (100, 24);

            HBox layout_sizes = new HBox (false, 0);
            layout_sizes.PackStart (this.size_label, false, false, 12);
            layout_sizes.PackStart (this.history_label, false, false, 0);

            VBox layout_vertical = new VBox (false, 0);
            this.spinner         = new Spinner ();
            this.spinner_wrapper = new VBox ();
            this.content_wrapper = new EventBox ();
            this.scrolled_window = new ScrolledWindow ();

            CssProvider css_provider = new CssProvider ();
            css_provider.LoadFromData ("GtkEventBox { background-color: #ffffff; }");
            this.content_wrapper.StyleContext.AddProvider (css_provider, 800);

            this.web_view = CreateWebView ();
            this.scrolled_window.Add (this.web_view);
            
            this.spinner_wrapper = new VBox (false, 0);
            this.spinner_wrapper.PackStart (new Label(""), true, true, 0);
            this.spinner_wrapper.PackStart (this.spinner, false, false, 0);
            this.spinner_wrapper.PackStart (new Label(""), true, true, 0);            
            this.spinner.SetSizeRequest (24, 24);
            this.spinner.Start ();

            this.content_wrapper.Add (this.spinner_wrapper);

            layout_vertical.PackStart (this.content_wrapper, true, true, 0);

            Add (layout_vertical);


            Controller.HideWindowEvent += delegate {
                Application.Invoke (delegate {
                    Hide ();
                    
                    if (this.content_wrapper.Child != null)
                        this.content_wrapper.Remove (this.content_wrapper.Child);
                });
            };

            Controller.ShowWindowEvent += delegate {
                Application.Invoke (delegate {
                    Move (pos_x, pos_y);
                    ShowAll ();
                    Present ();
                });
            };
			
            Controller.ShowSaveDialogEvent += delegate (string file_name, string target_folder_path) {
                Application.Invoke (delegate {
                    FileChooserDialog dialog = new FileChooserDialog ("Restore from History", this,
                        FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Ok);
					
                    dialog.CurrentName = file_name;
                    dialog.DoOverwriteConfirmation = true;
                    dialog.SetCurrentFolder (target_folder_path);

                    if (dialog.Run () == (int) ResponseType.Ok)
                        Controller.SaveDialogCompleted (dialog.Filename);
                    else
                        Controller.SaveDialogCancelled ();

                    dialog.Destroy ();
                });
            };

            Controller.UpdateContentEvent += delegate (string html) {
                 Application.Invoke (delegate { UpdateContent (html); });
            };

            Controller.ContentLoadingEvent += delegate {
                Application.Invoke (delegate {
                    if (this.content_wrapper.Child != null)
                        this.content_wrapper.Remove (this.content_wrapper.Child);

                    this.content_wrapper.Add (this.spinner_wrapper);
                    this.spinner.Start ();
                });
            };

            Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
                Application.Invoke (delegate {
                    this.size_label.Markup    = "<b>Size</b>  " + size;
                    this.history_label.Markup = "<b>History</b>  " + history_size;
                });
            };

            DeleteEvent += delegate (object o, DeleteEventArgs args) {
                Controller.WindowClosed ();
                args.RetVal = true;
            };
            
            KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
                if (args.Event.Key == Gdk.Key.Escape ||
                    (args.Event.State == Gdk.ModifierType.ControlMask && args.Event.Key == Gdk.Key.w)) {
                    
                    Controller.WindowClosed ();
                }
            };
        }


        public void UpdateContent (string html)
        {
            string pixmaps_path = IO.Path.Combine (UserInterface.AssetsPath, "pixmaps");
            string icons_path   = IO.Path.Combine (UserInterface.AssetsPath, "icons", "hicolor", "12x12", "status");

            html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace ("<!-- $a-color -->", "#0085cf");

            html = html.Replace ("<!-- $body-font-family -->", StyleContext.GetFont (StateFlags.Normal).Family);
            html = html.Replace ("<!-- $body-font-size -->", (double) (StyleContext.GetFont (StateFlags.Normal).Size / 1024 + 3) + "px");
            html = html.Replace ("<!-- $body-color -->", UserInterfaceHelpers.RGBAToHex (StyleContext.GetColor (StateFlags.Normal)));
            
			// TODO
			// html = html.Replace ("<!-- $body-background-color -->", 
            //     UserInterfaceHelpers.RGBAToHex (new TreeView ().StyleContext.GetStyleProperty ("background-color")));

            html = html.Replace ("<!-- $day-entry-header-font-size -->", (StyleContext.GetFont (StateFlags.Normal).Size / 1024 + 3) + "px");
            html = html.Replace ("<!-- $day-entry-header-background-color -->",
                UserInterfaceHelpers.RGBAToHex (StyleContext.GetBackgroundColor (StateFlags.Normal)));

            html = html.Replace ("<!-- $secondary-font-color -->", UserInterfaceHelpers.RGBAToHex (StyleContext.GetColor (StateFlags.Insensitive)));

            html = html.Replace ("<!-- $small-color -->", UserInterfaceHelpers.RGBAToHex (StyleContext.GetColor (StateFlags.Insensitive)));
            html = html.Replace ("<!-- $small-font-size -->", "90%");

            html = html.Replace ("<!-- $pixmaps-path -->", pixmaps_path);
			html = html.Replace ("<!-- $document-added-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-added.png"));
			html = html.Replace ("<!-- $document-edited-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-edited.png"));
			html = html.Replace ("<!-- $document-deleted-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-deleted.png"));
			html = html.Replace ("<!-- $document-moved-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-moved.png"));
                    
            this.spinner.Stop ();
            this.scrolled_window.Remove (this.scrolled_window.Child);
            this.web_view.Dispose ();

            this.web_view = CreateWebView ();
            this.web_view.LoadHtml (html, "file:///");
         
            this.scrolled_window.Add (this.web_view);

            this.content_wrapper.Remove (this.content_wrapper.Child);
            this.content_wrapper.Add (this.scrolled_window);
            this.scrolled_window.ShowAll ();
        }
            

        WebView CreateWebView ()
        {
            var web_view = new SparkleWebView { Editable = false };
            web_view.Settings.EnablePlugins = false;

            web_view.LinkClicked += Controller.LinkClicked;
           
            return web_view;
        }


        class SparkleWebView : WebView {

            public event LinkClickedHandler LinkClicked = delegate { };
            public delegate void LinkClickedHandler (string href);


            protected override bool OnDecidePolicy (PolicyDecision decision, PolicyDecisionType decision_type)
            {
                if (decision_type != PolicyDecisionType.NavigationAction) {
                    decision.Use ();
                    return false;
                }

                string uri = (decision as NavigationPolicyDecision).Request.Uri;

                if (uri.Equals ("file:///")) {
                    decision.Use ();
                    return false;
                }
                
                LinkClicked (uri);
                decision.Ignore ();

                return true;
            }
        }
    }
}
