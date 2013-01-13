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
using Notifications;

namespace SparkleShare {
    
    public class SparkleBubbles {

        public SparkleBubblesController Controller = new SparkleBubblesController ();


        public SparkleBubbles ()
        {
            Controller.ShowBubbleEvent += delegate (string title, string subtext, string image_path) {
                if (!Program.Controller.NotificationsEnabled)
                    return;

                try {
                    Notification notification = new Notification () {
                        Summary = title,
                        Body    = subtext,
                        Timeout = 5 * 1000,
                        Urgency = Urgency.Low
                    };
    
                    if (image_path != null)
                        notification.Icon = new Gdk.Pixbuf (image_path);
                    else
                        notification.IconName = "folder-sparkleshare";

                    notification.Closed += delegate (object o, EventArgs args) {
                        if ((args as CloseArgs).Reason == CloseReason.User)
                            Controller.BubbleClicked ();
                    };

                    notification.Show ();

                } catch (Exception) {
                    // Ignore exceptions thrown by libnotify,
                    // they're not important enough to crash
                }
            };
        }
    }
}
