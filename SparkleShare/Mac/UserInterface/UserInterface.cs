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

using MonoMac.AppKit;
using MonoMac.Foundation;

namespace SparkleShare {

    public class UserInterface : AppDelegate {

        public StatusIcon StatusIcon;
        public EventLog EventLog;
        public Setup Setup;
        public Bubbles Bubbles;
        public About About;
        public Note Note;


        public UserInterface ()
        {
            SparkleShare.Controller.Invoke (() => {
                NSApplication.SharedApplication.ApplicationIconImage = NSImage.ImageNamed ("sparkleshare-app.icns");
    
                Setup      = new Setup ();
                EventLog   = new EventLog ();
                About      = new About ();
                Note       = new Note ();
                Bubbles    = new Bubbles ();
                StatusIcon = new StatusIcon ();
            });

            SparkleShare.Controller.UIHasLoaded ();
        }


        public void Run ()
        {
            NSApplication.Main (SparkleShare.Arguments);
        }


        public static string FontName {
            get {
                if (Environment.OSVersion.Version.Major < 14)
                    return "Lucida Grande";

                if (Environment.OSVersion.Version.Major < 15)
                    return "Helvetica Neue";

                return "SF UI Text";
            }
        }
    }


    public partial class AppDelegate : NSApplicationDelegate {
     
        public override void WillTerminate (NSNotification notification)
        {
            SparkleShare.Controller.Quit ();
        }

        
        public override bool ApplicationShouldHandleReopen (NSApplication sender, bool has_visible_windows)
        {
            if (!has_visible_windows)
                SparkleShare.Controller.HandleReopen ();

            return true;
        }
    }
}
