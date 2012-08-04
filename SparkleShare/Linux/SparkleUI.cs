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
using SparkleLib;

namespace SparkleShare {

    public class SparkleUI {

        public SparkleStatusIcon StatusIcon;
        public SparkleEventLog EventLog;
        public SparkleBubbles Bubbles;
        public SparkleSetup Setup;
        public SparkleAbout About;

        public static string AssetsPath = Defines.INSTALL_DIR;


        public SparkleUI ()
        {
            Application.Init ();

            Setup      = new SparkleSetup ();
            EventLog   = new SparkleEventLog ();
            About      = new SparkleAbout ();
            Bubbles    = new SparkleBubbles ();
            StatusIcon = new SparkleStatusIcon ();
        
			Program.Controller.UIHasLoaded ();
        }


        // Runs the application
        public void Run ()
        {
            Application.Run ();
        }
    }
}
