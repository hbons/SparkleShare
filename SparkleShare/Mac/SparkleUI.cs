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

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.Growl;

namespace SparkleShare {

	public partial class AppDelegate : NSApplicationDelegate {

        public override void WillBecomeActive (NSNotification notification)
        {
            NSApplication.SharedApplication.DockTile.BadgeLabel = null;
        }

        public override void OrderFrontStandardAboutPanel (NSObject sender)
        {
            // FIXME: Doesn't work
            new SparkleAbout ();
        }

        public override void WillTerminate (NSNotification notification)
        {
            SparkleShare.Controller.Quit ();
        }
	}


	public class SparkleUI : AppDelegate {

		public static SparkleStatusIcon StatusIcon;
		public static SparkleEventLog EventLog;
		public static SparkleIntro Intro;
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

            using (NSAutoreleasePool pool = new NSAutoreleasePool ()) {

                // Needed for Growl
                GrowlApplicationBridge.WeakDelegate = this;

                NSApplication.SharedApplication.ApplicationIconImage
                    = NSImage.ImageNamed ("sparkleshare.icns");

                SetFolderIcon ();

                if (!SparkleShare.Controller.BackendIsPresent) {
                    this.alert = new SparkleAlert ();
                    this.alert.RunModal ();
                    return;
                }
    
                Font = NSFontManager.SharedFontManager.FontWithFamily
                    ("Lucida Grande", NSFontTraitMask.Condensed, 0, 13);

                StatusIcon = new SparkleStatusIcon ();
            }

            SparkleShare.Controller.NotificationRaised += delegate (string user_name, string user_email,
                                                                    string message, string repository_path) {
				InvokeOnMainThread (delegate {
                    if (EventLog != null)
                        EventLog.UpdateEvents ();

                    if (SparkleShare.Controller.NotificationsEnabled) {
                        if (NSApplication.SharedApplication.DockTile.BadgeLabel == null)
                            NSApplication.SharedApplication.DockTile.BadgeLabel = "1";
                        else
    					    NSApplication.SharedApplication.DockTile.BadgeLabel =
                                (int.Parse (NSApplication.SharedApplication.DockTile.BadgeLabel) + 1).ToString ();

                        if (GrowlApplicationBridge.IsGrowlRunning ()) {
                            SparkleBubble bubble = new SparkleBubble (user_name, message) {
                                ImagePath = SparkleShare.Controller.GetAvatar (user_email, 36)
                            };

                            bubble.Show ();

                        } else {
        					NSApplication.SharedApplication.RequestUserAttention
        						(NSRequestUserAttentionType.InformationalRequest);
                        }
                    }
				});
			};
			

            SparkleShare.Controller.ConflictNotificationRaised += delegate {
                    string title   = "Ouch! Mid-air collision!";
                    string subtext = "Don't worry, SparkleShare made a copy of each conflicting file.";

                    new SparkleBubble (title, subtext).Show ();
            };


			SparkleShare.Controller.AvatarFetched += delegate {
				InvokeOnMainThread (delegate {
					if (EventLog != null)
                        EventLog.UpdateEvents ();
				});
			};
			

            SparkleShare.Controller.OnIdle += delegate {
                InvokeOnMainThread (delegate {
                    if (EventLog != null)
                        EventLog.UpdateEvents ();
                });
            };


            SparkleShare.Controller.FolderListChanged += delegate {
                InvokeOnMainThread (delegate {
                    if (EventLog != null) {
                        EventLog.UpdateChooser ();
                        EventLog.UpdateEvents ();
                    }
                });
            };


			if (SparkleShare.Controller.FirstRun) {
				Intro = new SparkleIntro ();
				Intro.ShowAccountForm ();
			}
		}
	

		public void SetFolderIcon ()
		{
			string folder_icon_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
				"sparkleshare-mac.icns");

			NSImage folder_icon = new NSImage (folder_icon_path);
						
			NSWorkspace.SharedWorkspace.SetIconforFile (folder_icon,
				SparkleShare.Controller.SparklePath, 0);
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
}
