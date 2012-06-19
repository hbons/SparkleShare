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
using SparkleLib;

namespace SparkleShare {

    public class SparkleBubblesController {

        public event ShowBubbleEventHandler ShowBubbleEvent;
        public delegate void ShowBubbleEventHandler (string title, string subtext, string image_path);


        public SparkleBubblesController ()
        {
            Program.Controller.AlertNotificationRaised += delegate (string title, string message) {
                ShowBubble (title, message, null);
            };

            Program.Controller.NotificationRaised += delegate (SparkleChangeSet change_set) {
                if (Program.Controller.NotificationsEnabled)
                    ShowBubble (change_set.User.Name, FormatMessage (change_set),
                        Program.Controller.GetAvatar (change_set.User.Email, 48));
            };
        }


        public void ShowBubble (string title, string subtext, string image_path)
        {
            if (ShowBubbleEvent != null)
                ShowBubbleEvent (title, subtext, image_path);
        }


        public void BubbleClicked ()
        {
            Program.Controller.ShowEventLogWindow ();
        }


        private string FormatMessage (SparkleChangeSet change_set)
        {
            string message = "";

            if (change_set.Changes [0].Type == SparkleChangeType.Deleted)
                message = string.Format ("moved ‘{0}’", change_set.Changes [0].Path);

            if (change_set.Changes [0].Type == SparkleChangeType.Moved)
                message = string.Format ("moved ‘{0}’", change_set.Changes [0].Path);

            if (change_set.Changes [0].Type == SparkleChangeType.Added)
                message = string.Format ("added ‘{0}’", change_set.Changes [0].Path);

            if (change_set.Changes [0].Type == SparkleChangeType.Edited)
                message = string.Format ("moved ‘{0}’", change_set.Changes [0].Path);

            if (change_set.Changes.Count > 0) {
                string msg = string.Format ("and {0} more", change_set.Changes.Count);
                message    = message + " " + string.Format (msg, change_set.Changes.Count);

            } else {
                message = "did something magical";
            }

            return message;
        }
    }
}
