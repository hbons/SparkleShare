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

namespace SparkleShare {

    public class SparkleBubblesController {

        public event ShowBubbleEventHandler ShowBubbleEvent;
        public delegate void ShowBubbleEventHandler (string title, string subtext, string image_path);

        // Short alias for the translations
        public static string _ (string s)
        {
            string t=Program._ (s);
            return t;
        }

        public SparkleBubblesController ()
        {
            Program.Controller.ConflictNotificationRaised += delegate {
                ShowBubble (_("Ouch! Mid-air collision!"),
                            _("Don't worry, SparkleShare made a copy of each conflicting file."),
                            null);
            };

            Program.Controller.NotificationRaised += delegate (string user_name, string user_email,
                                                               string message, string folder_path) {
                ShowBubble (user_name, message,
                    Program.Controller.GetAvatar (user_email, 36));
            };
        }


        public void ShowBubble (string title, string subtext, string image_path)
        {
            if (ShowBubbleEvent != null && Program.Controller.NotificationsEnabled)
                ShowBubbleEvent (title, subtext, image_path);
        }
    }
}
