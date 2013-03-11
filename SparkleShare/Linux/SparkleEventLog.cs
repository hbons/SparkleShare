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
using System.Threading;

using Gtk;
using Mono.Unix;
using WebKit;

using IO = System.IO;

namespace SparkleShare {

    public class SparkleEventLog : Window {

        public SparkleEventLogController Controller = new SparkleEventLogController ();

        private Label size_label;
        private Label history_label;
        private HBox layout_horizontal;
        private ComboBox combo_box;
        private HBox combo_box_wrapper;
        private EventBox content_wrapper;
        private ScrolledWindow scrolled_window;
        private WebView web_view;
        private SparkleSpinner spinner;


        public SparkleEventLog () : base ("")
        {
            SetSizeRequest (480, (int) (Gdk.Screen.Default.Height * 0.8));

            int x = (int) (Gdk.Screen.Default.Width * 0.61);
            int y = (int) (Gdk.Screen.Default.Height * 0.5 - (HeightRequest * 0.5));
            
            Move (x, y);

            Resizable   = true;
            BorderWidth = 0;

            Title = "Recent Changes";
            IconName = "folder-sparkleshare";

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

            this.size_label = new Label () {
                Markup = "<b>Size:</b> …",
                Xalign = 0
            };
            
            this.history_label = new Label () {
                Markup = "<b>History:</b> …",
                Xalign = 0
            };
            
            HBox layout_sizes = new HBox (false, 12);
            layout_sizes.Add (this.size_label);
            layout_sizes.Add (this.history_label);

            VBox layout_vertical = new VBox (false, 0);
            this.spinner         = new SparkleSpinner (22);
            this.content_wrapper = new EventBox ();
            this.scrolled_window = new ScrolledWindow ();

            Gdk.Color white = new Gdk.Color();
            Gdk.Color.Parse ("white", ref white);

            this.content_wrapper.ModifyBg (StateType.Normal, white);

            this.web_view = new WebView () {
                Editable = false
            };


            this.web_view.NavigationRequested += WebViewNavigationRequested;

            this.scrolled_window.Add (this.web_view);
            this.content_wrapper.Add (this.spinner);

            this.spinner.Start ();

            this.layout_horizontal = new HBox (true, 0);
            this.layout_horizontal.PackStart (layout_sizes, true, true, 12);

            layout_vertical.PackStart (this.layout_horizontal, false, false, 0);
            layout_vertical.PackStart (this.content_wrapper, true, true, 0);

            Add (layout_vertical);


            Controller.HideWindowEvent += delegate {
                Application.Invoke (delegate {
                    HideAll ();
                    
                    if (this.content_wrapper.Child != null)
                        this.content_wrapper.Remove (this.content_wrapper.Child);
                });
            };

            Controller.ShowWindowEvent += delegate {
                Application.Invoke (delegate {
                    ShowAll ();
                    Present ();
                });
            };
			
			Controller.ShowSaveDialogEvent += delegate (string file_name, string target_folder_path) {
                Application.Invoke (delegate {
                    FileChooserDialog dialog = new FileChooserDialog ("Restore from History",
						this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Ok);
					
					dialog.CurrentName = file_name;
					dialog.SetCurrentFolder (target_folder_path);
					
					if (dialog.Run () == (int) ResponseType.Ok)
						Controller.SaveDialogCompleted (dialog.Filename);
					else
						Controller.SaveDialogCancelled ();
					
					dialog.Destroy ();
                });
            };

            Controller.UpdateChooserEvent += delegate (string [] folders) {
                Application.Invoke (delegate {
                    UpdateChooser (folders);
                });
            };
			
			Controller.UpdateChooserEnablementEvent += delegate (bool enabled) {
                Application.Invoke (delegate {
                    this.combo_box.Sensitive = enabled;
                });
            };

            Controller.UpdateContentEvent += delegate (string html) {
                 Application.Invoke (delegate {
                    UpdateContent (html);
                });
            };

            Controller.ContentLoadingEvent += delegate {
                Application.Invoke (delegate {
                    if (this.content_wrapper.Child != null)
                        this.content_wrapper.Remove (this.content_wrapper.Child);

                    this.content_wrapper.Add (this.spinner);
                    this.spinner.Start ();
                    this.content_wrapper.ShowAll ();
                });
            };

            Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
                Application.Invoke (delegate {
                    this.size_label.Markup    = "<b>Size:</b> " + size;
                    this.history_label.Markup = "<b>History:</b> " + history_size;

                    this.size_label.ShowAll ();
                    this.history_label.ShowAll ();
                });
            };
        }
        
        
        private void WebViewNavigationRequested (object o, WebKit.NavigationRequestedArgs args) {
            Controller.LinkClicked (args.Request.Uri);

            // Don't follow HREFs (as this would cause a page refresh)
            if (!args.Request.Uri.Equals ("file:"))
                args.RetVal = 1;
        }


        public void UpdateChooser (string [] folders)
        {
            if (folders == null)
                folders = Controller.Folders;

            if (this.combo_box_wrapper != null && this.combo_box_wrapper.Parent != null) {
                this.layout_horizontal.Remove (this.combo_box_wrapper);
                this.combo_box_wrapper.Remove (this.combo_box);
            }

            this.combo_box_wrapper = new HBox (false, 0);
            this.combo_box = new ComboBox ();

            CellRendererText cell = new CellRendererText();
            this.combo_box.PackStart (cell, false);
            this.combo_box.AddAttribute (cell, "text", 0);

            ListStore store = new ListStore (typeof (string));

            store.AppendValues ("Summary");
            store.AppendValues ("---");
            
            this.combo_box.Model  = store;
            this.combo_box.Active = 0;
            
            int row = 2;
               foreach (string folder in folders) {
                store.AppendValues (folder);
                
                if (folder.Equals (Controller.SelectedFolder))
                    this.combo_box.Active = row;
                
                row++;
            }

            this.combo_box.RowSeparatorFunc = delegate (TreeModel model, TreeIter iter) {
                string item = (string) this.combo_box.Model.GetValue (iter, 0);
                return (item == "---");
            };

            this.combo_box.Changed += delegate {
                TreeIter iter;
                this.combo_box.GetActiveIter (out iter);
                string selection = (string) this.combo_box.Model.GetValue (iter, 0);
                TreePath path    = this.combo_box.Model.GetPath (iter);

                if (path.Indices [0] == 0)
                    Controller.SelectedFolder = null;
                else
                    Controller.SelectedFolder = selection;
            };

            this.combo_box_wrapper.PackStart (new Label (" "), false, false, 9);
            this.combo_box_wrapper.PackStart (this.combo_box, true, true, 0);

            this.layout_horizontal.BorderWidth = 9;
            this.layout_horizontal.PackStart (this.combo_box_wrapper, true, true, 0);
            this.layout_horizontal.ShowAll ();
        }


        public void UpdateContent (string html)
        {
            string pixmaps_path = IO.Path.Combine (SparkleUI.AssetsPath, "pixmaps");
            string icons_path   = new string [] {SparkleUI.AssetsPath, "icons", "hicolor", "12x12", "status"}.Combine ();

            html = html.Replace ("<!-- $body-font-size -->", (double) (Style.FontDescription.Size / 1024 + 3) + "px");
            html = html.Replace ("<!-- $day-entry-header-font-size -->", (Style.FontDescription.Size / 1024 + 3) + "px");
            html = html.Replace ("<!-- $a-color -->", "#0085cf");
            html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace ("<!-- $body-font-family -->", "\"" + Style.FontDescription.Family + "\"");
            html = html.Replace ("<!-- $body-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Normal)));
            html = html.Replace ("<!-- $body-background-color -->", SparkleUIHelpers.GdkColorToHex (new TreeView ().Style.Base (StateType.Normal)));
            html = html.Replace ("<!-- $day-entry-header-background-color -->", SparkleUIHelpers.GdkColorToHex (Style.Background (StateType.Normal)));
            html = html.Replace ("<!-- $secondary-font-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));
            html = html.Replace ("<!-- $small-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));
            html = html.Replace ("<!-- $small-font-size -->", "85%");
            html = html.Replace ("<!-- $pixmaps-path -->", pixmaps_path);
			html = html.Replace ("<!-- $document-added-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-added.png"));
            html = html.Replace ("<!-- $document-edited-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-edited.png"));
            html = html.Replace ("<!-- $document-deleted-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-deleted.png"));
            html = html.Replace ("<!-- $document-moved-background-image -->", "file://" + IO.Path.Combine (icons_path, "document-moved.png"));
                    
            this.spinner.Stop ();

            this.web_view.NavigationRequested -= WebViewNavigationRequested;
            this.web_view.LoadHtmlString (html, "file://");
            this.web_view.NavigationRequested += WebViewNavigationRequested;

            this.content_wrapper.Remove (this.content_wrapper.Child);
            this.content_wrapper.Add (this.scrolled_window);
            this.content_wrapper.ShowAll ();
        }
    }
}
