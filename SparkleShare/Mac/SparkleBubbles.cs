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
using System.IO;

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.Growl;

namespace SparkleShare {
    
    public class SparkleBubbles : NSObject {

        public SparkleBubblesController Controller = new SparkleBubblesController ();


        public SparkleBubbles ()
        {
            Controller.ShowBubbleEvent += delegate (string title, string subtext, string image_path) {
                InvokeOnMainThread (delegate {
                    if (NSApplication.SharedApplication.DockTile.BadgeLabel == null) {
                        NSApplication.SharedApplication.DockTile.BadgeLabel = "1";

                    } else {
                        int events = int.Parse (NSApplication.SharedApplication.DockTile.BadgeLabel);
                        NSApplication.SharedApplication.DockTile.BadgeLabel = (events + 1).ToString ();
                    }

                    if (image_path != null) {
                        NSData image_data = NSData.FromFile (image_path);
                        GrowlApplicationBridge.Notify (title, subtext, "Event", image_data, 0, false, new NSString (""));

                    } else {
                        GrowlApplicationBridge.Notify (title, subtext, "Event", null, 0, false, new NSString (""));
                    }
                });
            };
        }
    }


    public class SparkleGrowlDelegate : GrowlDelegate {

        [Export("growlNotificationWasClicked")]
        public override void GrowlNotificationWasClicked (NSObject o)
        {
            NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            Program.UI.Bubbles.Controller.BubbleClicked ();
        }


        [Export("registrationDictionaryForGrowl")]
        public override NSDictionary RegistrationDictionaryForGrowl ()
        {
            string path = NSBundle.MainBundle.PathForResource ("Growl", "plist");
            return NSDictionary.FromFile (path);
        }
    }
}
