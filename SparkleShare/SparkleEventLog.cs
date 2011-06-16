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
using System.Text.RegularExpressions;
using System.Threading;

using Gtk;
using Mono.Unix;
using SparkleLib;
using WebKit;

namespace SparkleShare {

    public class SparkleEventLog : Window {

        private ScrolledWindow ScrolledWindow;
        private MenuBar MenuBar;
        private WebView WebView;
        private string LinkStatus;
        private SparkleSpinner Spinner;
        private string HTML;
        private EventBox LogContent;
        private List<SparkleChangeSet> change_sets;
        private string selected_log = null;
        private ComboBox combo_box;
        private HBox layout_horizontal;


        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleEventLog () : base ("")
        {
            SetSizeRequest (480, 640);
            SetPosition (WindowPosition.Center);

            Resizable   = false;
            BorderWidth = 0;

            Title = _("Recent Events");
            IconName = "folder-sparkleshare";

            DeleteEvent += Close;

            CreateEvents ();
            UpdateEvents (false);
            UpdateChooser ();
        }


        private void CreateEvents ()
        {
            VBox layout_vertical = new VBox (false, 0);
            LogContent           = new EventBox ();

            ScrolledWindow = new ScrolledWindow ();

                WebView = new WebView () {
                    Editable = false
                };

                    WebView.HoveringOverLink += delegate (object o, WebKit.HoveringOverLinkArgs args) {
                        LinkStatus = args.Link;
                    };

                    // FIXME: Use the right event, waiting for newer webkit bindings: NavigationPolicyDecisionRequested
                    WebView.NavigationRequested += delegate (object o, WebKit.NavigationRequestedArgs args) {
                        if (args.Request.Uri == LinkStatus) {
                            Process process = new Process ();
                            process.StartInfo.FileName = "xdg-open";
                            process.StartInfo.Arguments = args.Request.Uri.Replace (" ", "\\ "); // Escape space-characters
                            process.Start ();

                            // Don't follow HREFs (as this would cause a page refresh)
                            args.RetVal = 1;
                        }
                    };

            ScrolledWindow.Add (WebView);
            LogContent.Add (ScrolledWindow);

            this.layout_horizontal = new HBox (true, 0);
            this.layout_horizontal.PackStart (new Label (""), true, true, 0);
            this.layout_horizontal.PackStart (new Label (""), true, true, 0);

            layout_vertical.PackStart (layout_horizontal, false, false, 0);
            layout_vertical.PackStart (LogContent, true, true, 0);

            // We have to hide the menubar somewhere...
            layout_vertical.PackStart (CreateShortcutsBar (), false, false, 0);

            Add (layout_vertical);
            ShowAll ();
        }


        public void UpdateChooser ()
        {
            if (this.combo_box != null && this.combo_box.Parent != null)
                this.layout_horizontal.Remove (this.combo_box);

            this.combo_box = new ComboBox ();
            this.layout_horizontal.BorderWidth = 9;

            CellRendererText cell = new CellRendererText();
            this.combo_box.PackStart (cell, false);
            this.combo_box.AddAttribute (cell, "text", 0);
            ListStore store = new ListStore (typeof (string));
            this.combo_box.Model = store;
   
            store.AppendValues (_("All Folders"));
            store.AppendValues ("---");

            foreach (string folder_name in SparkleShare.Controller.Folders)
                store.AppendValues (folder_name);

            this.combo_box.Active = 0;

            this.combo_box.RowSeparatorFunc = delegate (TreeModel model, TreeIter iter) {
                string item = (string) this.combo_box.Model.GetValue (iter, 0);
                return (item == "---");
            };

            this.combo_box.Changed += delegate {
                TreeIter iter;
                this.combo_box.GetActiveIter (out iter);

                string selection = (string) this.combo_box.Model.GetValue (iter, 0);

                if (selection.Equals (_("All Folders")))
                    this.selected_log = null;
                else
                    this.selected_log = selection;

                UpdateEvents (false);
            };

            this.layout_horizontal.PackStart (this.combo_box, true, true, 0);
            this.layout_horizontal.ShowAll ();
        }


        public void UpdateEvents ()
        {
            UpdateEvents (true);
        }


        public void UpdateEvents (bool silent)
        {
            if (!silent) {
                LogContent.Remove (LogContent.Child);
                Spinner = new SparkleSpinner (22);
                LogContent.Add (Spinner);
                LogContent.ShowAll ();
            }

            Thread thread = new Thread (new ThreadStart (delegate {
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
            }));

            thread.Start ();
        }


        private void GenerateHTML ()
        {
            HTML = SparkleShare.Controller.GetHTMLLog (this.change_sets);

            HTML = HTML.Replace ("<!-- $body-font-size -->", (double) (Style.FontDescription.Size / 1024 + 3) + "px");
            HTML = HTML.Replace ("<!-- $day-entry-header-font-size -->", (Style.FontDescription.Size / 1024 + 3) + "px");
            HTML = HTML.Replace ("<!-- $a-color -->", "#0085cf");
            HTML = HTML.Replace ("<!-- $a-hover-color -->", "#009ff8");
            HTML = HTML.Replace ("<!-- $body-font-family -->", "\"" + Style.FontDescription.Family + "\"");
            HTML = HTML.Replace ("<!-- $body-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Normal)));
            HTML = HTML.Replace ("<!-- $body-background-color -->", SparkleUIHelpers.GdkColorToHex (new TreeView ().Style.Base (StateType.Normal)));
            HTML = HTML.Replace ("<!-- $day-entry-header-background-color -->", SparkleUIHelpers.GdkColorToHex (Style.Background (StateType.Normal)));
            HTML = HTML.Replace ("<!-- $secondary-font-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));
            HTML = HTML.Replace ("<!-- $small-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));    
            HTML = HTML.Replace ("<!-- $no-buddy-icon-background-image -->", "file://" +
                    SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
                        "hicolor", "32x32", "status", "avatar-default.png"));
            HTML = HTML.Replace ("<!-- $document-added-background-image -->", "file://" +
                    SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
                        "hicolor", "12x12", "status", "document-added.png"));
            HTML = HTML.Replace ("<!-- $document-edited-background-image -->", "file://" +
                    SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
                        "hicolor", "12x12", "status", "document-edited.png"));
            HTML = HTML.Replace ("<!-- $document-deleted-background-image -->", "file://" +
                    SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
                        "hicolor", "12x12", "status", "document-deleted.png"));
            HTML = HTML.Replace ("<!-- $document-moved-background-image -->", "file://" +
                    SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
                        "hicolor", "12x12", "status", "document-moved.png"));
        }


        private void AddHTML ()
        {
            Application.Invoke (delegate {
                Spinner.Stop ();
                LogContent.Remove (LogContent.Child);

                WebView.LoadString (HTML, null, null, "file://");

                LogContent.Add (ScrolledWindow);
                LogContent.ShowAll ();
            });
        }


        public void Close (object o, DeleteEventArgs args)
        {
            HideAll ();
            args.RetVal = true;
            // TODO: window positions aren't saved
        }


        private MenuBar CreateShortcutsBar ()
        {
            // Adds a hidden menubar that contains to enable keyboard
            // shortcuts to close the log
            MenuBar = new MenuBar ();

                MenuItem file_item = new MenuItem ("File");

                    Menu file_menu = new Menu ();

                        MenuItem close_1 = new MenuItem ("Close1");
                        MenuItem close_2 = new MenuItem ("Close2");
        
                        // adds specific Ctrl+W and Esc key accelerators to Log Window
                        AccelGroup accel_group = new AccelGroup ();
                        AddAccelGroup (accel_group);

                        // Close on Esc
                        close_1.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.W, Gdk.ModifierType.ControlMask,
                            AccelFlags.Visible));

                        close_1.Activated += delegate { HideAll (); };

                        // Close on Ctrl+W
                        close_2.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.Escape, Gdk.ModifierType.None,
                            AccelFlags.Visible));
                        close_2.Activated += delegate { HideAll (); };

                    file_menu.Append (close_1);
                    file_menu.Append (close_2);

                file_item.Submenu = file_menu;

            MenuBar.Append (file_item);

            // Hacky way to hide the menubar, but the accellerators
            // will simply be disabled when using Hide ()
            MenuBar.HeightRequest = 1;
            MenuBar.ModifyBg (StateType.Normal, Style.Background (StateType.Normal));

            return MenuBar;
        }
    }
}

