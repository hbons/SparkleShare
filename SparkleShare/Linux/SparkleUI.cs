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

using GLib;
using Gtk;
using SparkleLib;

namespace SparkleShare {

    public class SparkleUI {

        public static string AssetsPath = Defines.INSTALL_DIR;

        public SparkleStatusIcon StatusIcon;
        public SparkleEventLog EventLog;
        public SparkleBubbles Bubbles;
        public SparkleSetup Setup;
        public SparkleAbout About;

        private Gtk.Application application;


        public SparkleUI ()
        {
            this.application = new Gtk.Application ("org.sparkleshare.sparkleshare", 0);

            this.application.Register (null);
            this.application.Activated += ApplicationActivatedDelegate;
        }


        public void Run ()
        {   
            (this.application as GLib.Application).Run (0, null);
        }


        private void ApplicationActivatedDelegate (object sender, EventArgs args)
        {
            if (this.application.Windows.Length > 0) {
                foreach (Window window in this.application.Windows) {
                    if (window.Visible)
                        window.Present ();
                }

            } else {
                Setup      = new SparkleSetup ();
                EventLog   = new SparkleEventLog ();
                About      = new SparkleAbout ();
                Bubbles    = new SparkleBubbles ();
                StatusIcon = new SparkleStatusIcon ();

                Setup.Application    = this.application;
                EventLog.Application = this.application;
                About.Application    = this.application;

                Program.Controller.UIHasLoaded ();
            }
        }
    }
}
