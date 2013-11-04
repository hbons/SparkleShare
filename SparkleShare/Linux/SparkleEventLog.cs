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

using Gtk;
using WebKit;

namespace SparkleShare {

    public class SparkleEventLog : Window {

        public SparkleEventLogController Controller = new SparkleEventLogController ();

        private Label size_label;
        private Label history_label;
        private ComboBox combo_box;
        private EventBox content_wrapper;
        private HBox combo_box_wrapper;
        private HBox layout_horizontal;
        private ScrolledWindow scrolled_window;
        private VBox spinner_wrapper;
        private Spinner spinner;
        private WebView web_view;

        private int pos_x, pos_y;


        public SparkleEventLog () : base ("Recent Changes")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            Gdk.Rectangle monitor_0_rect = Gdk.Screen.Default.GetMonitorGeometry (0);
            SetSizeRequest (480, (int) (monitor_0_rect.Height * 0.8));

            IconName = "sparkleshare";
            this.pos_x = (int) (monitor_0_rect.Width * 0.61);
            this.pos_y = (int) (monitor_0_rect.Height * 0.5 - (HeightRequest * 0.5));
            
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

            this.content_wrapper.OverrideBackgroundColor (StateFlags.Normal,
                new Gdk.RGBA () { Red = 1, Green = 1, Blue=1, Alpha = 1 });

            this.web_view = new WebView () { Editable = false };
            this.web_view.NavigationRequested += WebViewNavigationRequested;

            this.scrolled_window.Add (this.web_view);
            
            this.spinner_wrapper = new VBox (false, 0);
            this.spinner_wrapper.PackStart (new Label(""), true, true, 0);
            this.spinner_wrapper.PackStart (this.spinner, false, false, 0);
            this.spinner_wrapper.PackStart (new Label(""), true, true, 0);            
            this.spinner.SetSizeRequest (24, 24);
            this.spinner.Start ();

            this.content_wrapper.Add (this.spinner_wrapper);

            this.layout_horizontal = new HBox (false, 0);
            this.layout_horizontal.PackStart (layout_sizes, true, true, 12);

            layout_vertical.PackStart (this.layout_horizontal, false, false, 0);
            layout_vertical.PackStart (new HSeparator (), false, false, 0);
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
                    Move (this.pos_x, this.pos_y);
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

            Controller.UpdateChooserEvent += delegate (string [] folders) {
                Application.Invoke (delegate { UpdateChooser (folders); });
            };
			
            Controller.UpdateChooserEnablementEvent += delegate (bool enabled) {
                Application.Invoke (delegate { this.combo_box.Sensitive = enabled; });
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

            this.combo_box.RowSeparatorFunc = delegate (ITreeModel model, TreeIter iter) {
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

            this.combo_box_wrapper.Add (this.combo_box);
            this.combo_box.GrabFocus ();

            this.layout_horizontal.BorderWidth = 6;
            this.layout_horizontal.PackStart (this.combo_box_wrapper, false, false, 0);
            this.layout_horizontal.ShowAll ();
        }


        public void UpdateContent (string html)
        {
            string pixmaps_path = new string [] {SparkleUI.AssetsPath, "pixmaps"}.Combine ();
            string icons_path   = new string [] {SparkleUI.AssetsPath, "icons", "hicolor", "12x12", "status"}.Combine ();

            html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace ("<!-- $a-color -->", "#0085cf");

            html = html.Replace ("<!-- $body-font-family -->", StyleContext.GetFont (StateFlags.Normal).Family);
            html = html.Replace ("<!-- $body-font-size -->", (double) (StyleContext.GetFont (StateFlags.Normal).Size / 1024 + 3) + "px");
            html = html.Replace ("<!-- $body-color -->", SparkleUIHelpers.RGBAToHex (StyleContext.GetColor (StateFlags.Normal)));
            html = html.Replace ("<!-- $body-background-color -->",
                SparkleUIHelpers.RGBAToHex (new TreeView ().StyleContext.GetBackgroundColor (StateFlags.Normal)));

            html = html.Replace ("<!-- $day-entry-header-font-size -->", (StyleContext.GetFont (StateFlags.Normal).Size / 1024 + 3) + "px");
            html = html.Replace ("<!-- $day-entry-header-background-color -->",
                SparkleUIHelpers.RGBAToHex (StyleContext.GetBackgroundColor (StateFlags.Normal)));

            html = html.Replace ("<!-- $secondary-font-color -->", SparkleUIHelpers.RGBAToHex (StyleContext.GetColor (StateFlags.Insensitive)));

            html = html.Replace ("<!-- $small-color -->", SparkleUIHelpers.RGBAToHex (StyleContext.GetColor (StateFlags.Insensitive)));
            html = html.Replace ("<!-- $small-font-size -->", "90%");

            html = html.Replace ("<!-- $pixmaps-path -->", pixmaps_path);
			html = html.Replace ("<!-- $document-added-background-image -->", "file://" + new string [] {icons_path, "document-added.png"}.Combine ());
            html = html.Replace ("<!-- $document-edited-background-image -->", "file://" + new string [] {icons_path, "document-edited.png"}.Combine ());
            html = html.Replace ("<!-- $document-deleted-background-image -->", "file://" + new string [] {icons_path, "document-deleted.png"}.Combine ());
            html = html.Replace ("<!-- $document-moved-background-image -->", "file://" + new string [] {icons_path, "document-moved.png"}.Combine ());
                    
            this.spinner.Stop ();
            this.scrolled_window.Remove (this.web_view);
            this.web_view.Dispose ();

            this.web_view = new WebView () { Editable = false };
            this.web_view.LoadString (html, "text/html", "UTF-8", "file://");
            this.web_view.NavigationRequested += WebViewNavigationRequested;
            this.scrolled_window.Add (this.web_view);

            this.content_wrapper.Remove (this.content_wrapper.Child);
            this.content_wrapper.Add (this.scrolled_window);
            this.scrolled_window.ShowAll ();
        }


        private void WebViewNavigationRequested (object o, WebKit.NavigationRequestedArgs args) {
            Controller.LinkClicked (args.Request.Uri);

            // Don't follow HREFs (as this would cause a page refresh)
            if (!args.Request.Uri.Equals ("file:"))
                args.RetVal = 1;
        }
    }
}

