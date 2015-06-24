//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using System;
using Gtk;

namespace SparkleShare {

    public class SparkleNote : Window {

        public SparkleNoteController Controller = new SparkleNoteController ();


        public SparkleNote () : base ("Sync")
        {
            SetWmclass ("SparkleShare", "SparkleShare");

            IconName       = "sparkleshare";
            Resizable      = false;
            WindowPosition = WindowPosition.Center;

            SetSizeRequest (480, 240);


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

            Controller.HideWindowEvent += delegate {
                Application.Invoke (delegate { Hide (); });
            };

            Controller.ShowWindowEvent += delegate {
                Application.Invoke (delegate {
                    CreateNote ();
                    ShowAll ();
                    Present ();
                });
            };

            CreateNote ();
        }


        private void CreateNote ()
        {
            // TODO
        }
    }
}
