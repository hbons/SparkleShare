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
using System.Drawing;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleAlert : NSAlert {

        public SparkleAlert () : base ()
        {
            MessageText     = "SparkleShare couldn't find Git on your system. Do you want to download it?";
            InformativeText = "Git is required to run SparkleShare.";

            Icon = NSImage.ImageNamed ("sparkleshare.icns");

            AddButton ("Download");
            AddButton ("Cancel");

            Buttons [0].Activated += delegate {
                NSUrl url = new NSUrl ("http://code.google.com/p/git-osx-installer/downloads/list");
                NSWorkspace.SharedWorkspace.OpenUrl (url);
                Environment.Exit (0);
            };

            Buttons [1].Activated += delegate {
                Environment.Exit (-1);
            };
        }
    }
}
