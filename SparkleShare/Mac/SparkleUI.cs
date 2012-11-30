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

using MonoMac.Foundation;
using MonoMac.AppKit;

namespace SparkleShare {

    public class SparkleUI : AppDelegate {

        public SparkleStatusIcon StatusIcon;
        public SparkleEventLog EventLog;
        public SparkleSetup Setup;
        public SparkleBubbles Bubbles;
        public SparkleAbout About;
		
		public static NSFont Font = NSFontManager.SharedFontManager.FontWithFamily (
			"Lucida Grande", NSFontTraitMask.Condensed, 0, 13);
		
        public static NSFont BoldFont = NSFontManager.SharedFontManager.FontWithFamily (
			"Lucida Grande", NSFontTraitMask.Bold, 0, 13);
		

        public SparkleUI ()
        {
            Program.Controller.Invoke (() => {
                NSWorkspace.SharedWorkspace.SetIconforFile (
                    NSImage.ImageNamed ("sparkleshare-folder.icns"), Program.Controller.FoldersPath, 0);

                NSApplication.SharedApplication.ApplicationIconImage = NSImage.ImageNamed ("sparkleshare-app.icns");
    
                Setup      = new SparkleSetup ();
                EventLog   = new SparkleEventLog ();
                About      = new SparkleAbout ();
                Bubbles    = new SparkleBubbles ();
                StatusIcon = new SparkleStatusIcon ();
            });

            Program.Controller.UIHasLoaded ();
        }


        public void Run ()
        {
            NSApplication.Main (new string [0]);
        }


        public void UpdateDockIconVisibility ()
        {
            if (Setup.IsVisible || EventLog.IsVisible || About.IsVisible)
                NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Regular;
        }
    }


    public partial class AppDelegate : NSApplicationDelegate {
     
        public override void WillTerminate (NSNotification notification)
        {
            Program.Controller.Quit ();
        }
    }
}
