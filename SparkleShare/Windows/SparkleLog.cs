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

namespace SparkleShare {

    public class SparkleLog : Window {

        public readonly string LocalPath;

        private ScrolledWindow ScrolledWindow;
        private MenuBar MenuBar;
        private string LinkStatus;
        private SparkleSpinner Spinner;
        private string HTML;
        private EventBox LogContent;


        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleLog (string path) : base ("")
        {
            LocalPath = path;
            
            string name = System.IO.Path.GetFileName (LocalPath);
            SetSizeRequest (480, 640);

            Resizable = false;

            BorderWidth = 0;
            SetPosition (WindowPosition.Center);

            // Open slightly off center for each consecutive window
            if (SparkleUI.OpenLogs.Count > 0) {

                int x, y;
                GetPosition (out x, out y);
                Move (x + SparkleUI.OpenLogs.Count * 20, y + SparkleUI.OpenLogs.Count * 20);

            }
            
            // TRANSLATORS: {0} is a folder name, and {1} is a server address
            Title = String.Format(_("Events in ‘{0}’"), name);
            IconName = "folder-sparkleshare";

            DeleteEvent += Close;            

            CreateEventLog ();
            UpdateEventLog ();
        }


        private void CreateEventLog ()
        {
            LogContent           = new EventBox ();
            VBox layout_vertical = new VBox (false, 0);

                ScrolledWindow = new ScrolledWindow ();

                LogContent.Add (ScrolledWindow);

            layout_vertical.PackStart (LogContent, true, true, 0);

                HButtonBox dialog_buttons = new HButtonBox {
                    Layout = ButtonBoxStyle.Edge,
                    BorderWidth = 12
                };

                    Button open_folder_button = new Button (_("_Open Folder")) {
                        UseUnderline = true
                    };
 
                    open_folder_button.Clicked += delegate (object o, EventArgs args) {
                        SparkleShare.Controller.OpenSparkleShareFolder (LocalPath);
                    };

                    Button close_button = new Button (Stock.Close);

                    close_button.Clicked += delegate {
                        HideAll ();
                    };

                dialog_buttons.Add (open_folder_button);
                dialog_buttons.Add (close_button);

            // We have to hide the menubar somewhere...
            layout_vertical.PackStart (CreateShortcutsBar (), false, false, 0);
            layout_vertical.PackStart (dialog_buttons, false, false, 0);

            Add (layout_vertical);

            ShowAll ();
        }


        public void UpdateEventLog ()
        {
            if (HTML == null) { // TODO: there may be a race condition here
                LogContent.Remove (LogContent.Child);
                Spinner = new SparkleSpinner (22);
                LogContent.Add (Spinner);
                LogContent.ShowAll ();
            }

            Thread thread = new Thread (new ThreadStart (delegate {
                GenerateHTML ();
                AddHTML ();
            }));

            thread.Start ();
        }


        private void GenerateHTML ()
        {
            HTML = SparkleShare.Controller.GetHTMLLog (System.IO.Path.GetFileName (LocalPath));

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
        }


        private void AddHTML ()
        {
            Application.Invoke (delegate {
                Spinner.Stop ();
                LogContent.Remove (LogContent.Child);

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
