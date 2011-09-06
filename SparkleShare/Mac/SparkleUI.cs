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
using System.Timers;

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
        public static NSFont Font;

        private NSAlert alert;


        public SparkleUI ()
        {
            string content_path = Directory.GetParent (
                System.AppDomain.CurrentDomain.BaseDirectory).ToString ();

            string app_path     = Directory.GetParent (content_path).ToString ();
            string growl_path   = Path.Combine (app_path, "Frameworks", "Growl.framework", "Growl");

            // Needed for Growl
            Dlfcn.dlopen (growl_path, 0);
            NSApplication.Init ();

            // Use translations
            Catalog.Init ("sparkleshare",
                Path.Combine (NSBundle.MainBundle.ResourcePath, "Translations"));

            using (NSAutoreleasePool pool = new NSAutoreleasePool ()) {

                // Needed for Growl
                GrowlApplicationBridge.WeakDelegate = this;

                NSApplication.SharedApplication.ApplicationIconImage
                    = NSImage.ImageNamed ("sparkleshare.icns");

                if (!Program.Controller.BackendIsPresent) {
                    this.alert = new SparkleAlert ();
                    this.alert.RunModal ();
                    return;
                }

                SetFolderIcon ();
    
                Font = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Condensed, 0, 13);

                StatusIcon = new SparkleStatusIcon ();
                Bubbles = new SparkleBubbles ();

                if (Program.Controller.FirstRun) {
                    Setup = new SparkleSetup ();
                    Setup.Controller.ShowSetupPage ();
                }
            }
        }
    

        public void SetFolderIcon ()
        {
            string folder_icon_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                "sparkleshare-mac.icns");

            NSImage folder_icon = new NSImage (folder_icon_path);
                        
            NSWorkspace.SharedWorkspace.SetIconforFile (folder_icon,
                Program.Controller.SparklePath, 0);
        }


        public void Run ()
        {
            NSApplication.Main (new string [0]);
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
            NSApplication.SharedApplication.DockTile.BadgeLabel = null;
        }

        public override void WillTerminate (NSNotification notification)
        {
            Program.Controller.Quit ();
        }
    }
}
