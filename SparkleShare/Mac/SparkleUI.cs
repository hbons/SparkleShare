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
using System.Drawing;
using System.IO;

using Mono.Unix;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.Growl;

namespace SparkleShare {

    public class SparkleUI : AppDelegate {

        public static SparkleStatusIcon StatusIcon;
        public static SparkleEventLog EventLog;
        public static SparkleSetup Setup;
        public static SparkleBubbles Bubbles;
        public static SparkleAbout About;
		
		public static NSFont Font = NSFontManager.SharedFontManager.FontWithFamily
			("Lucida Grande", NSFontTraitMask.Condensed, 0, 13);
		
        public static NSFont BoldFont = NSFontManager.SharedFontManager.FontWithFamily
			("Lucida Grande", NSFontTraitMask.Bold, 0, 13);
		

        public SparkleUI ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                Catalog.Init ("sparkleshare",
                    Path.Combine (NSBundle.MainBundle.ResourcePath, "Translations"));

                GrowlApplicationBridge.WeakDelegate = this;
                GrowlApplicationBridge.Delegate     = new SparkleGrowlDelegate ();

                NSApplication.SharedApplication.ApplicationIconImage =
					NSImage.ImageNamed ("sparkleshare-app.icns");

                SetFolderIcon ();
    
                Setup      = new SparkleSetup ();
                EventLog   = new SparkleEventLog ();
                About      = new SparkleAbout ();
                Bubbles    = new SparkleBubbles ();
                StatusIcon = new SparkleStatusIcon ();

                Program.Controller.UIHasLoaded ();
            }
        }
    

        public void SetFolderIcon ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                string folder_icon_path = Path.Combine (
					NSBundle.MainBundle.ResourcePath,
                    "sparkleshare-mac.icns"
				);

                NSImage folder_icon = new NSImage (folder_icon_path);

                NSWorkspace.SharedWorkspace.SetIconforFile (folder_icon,
                    Program.Controller.SparklePath, 0);
            }
        }


        public void Run ()
        {
            NSApplication.Main (new string [0]);
        }


        public void UpdateDockIconVisibility ()
        {
            if (Setup.IsVisible || EventLog.IsVisible || About.IsVisible)
                ShowDockIcon ();
            else
                HideDockIcon ();
        }


        private void HideDockIcon ()
        {
            // Currently not supported, here for completeness sake (see Apple's docs)
            // NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.None;
        }


        private void ShowDockIcon ()
        {
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Regular;
        }


        [Export("registrationDictionaryForGrowl")]
        NSDictionary RegistrationDictionaryForGrowl ()
        {
            string path = NSBundle.MainBundle.PathForResource ("Growl", "plist");
            return NSDictionary.FromFile (path);
        }
    }


    public partial class AppDelegate : NSApplicationDelegate {

        public override void WillBecomeActive (NSNotification notification)
        {
            if (NSApplication.SharedApplication.DockTile.BadgeLabel != null) {
                Program.Controller.ShowEventLogWindow ();
                NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            }
        }


        public override void WillTerminate (NSNotification notification)
        {
            Program.Controller.Quit ();
        }
    }
}
