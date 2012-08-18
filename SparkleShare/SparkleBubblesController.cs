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

        public event ShowBubbleEventHandler ShowBubbleEvent = delegate { };
        public delegate void ShowBubbleEventHandler (string title, string subtext, string image_path);


        public SparkleBubblesController ()
        {
            Program.Controller.AlertNotificationRaised += delegate (string title, string message) {
                ShowBubble (title, message, null);
            };

            Program.Controller.NotificationRaised += delegate (SparkleChangeSet change_set) {
                ShowBubble (change_set.User.Name, FormatMessage (change_set),
                    Program.Controller.GetAvatar (change_set.User.Email, 48));
            };
        }


        public void ShowBubble (string title, string subtext, string image_path)
        {
            ShowBubbleEvent (title, subtext, image_path);
        }


        public void BubbleClicked ()
        {
            Program.Controller.ShowEventLogWindow ();
        }


        private string FormatMessage (SparkleChangeSet change_set)
        {
            string message = "added ‘{0}’";

            switch (change_set.Changes [0].Type) {
                case SparkleChangeType.Edited:  message = "edited ‘{0}’"; break;
                case SparkleChangeType.Deleted: message = "deleted ‘{0}’"; break;
                case SparkleChangeType.Moved:   message = "moved ‘{0}’"; break;
            }

            if (change_set.Changes.Count == 1) {
                return message = string.Format (message, change_set.Changes [0].Path);

            } else if (change_set.Changes.Count > 1) {
				message = string.Format (message, change_set.Changes [0].Path);
                return string.Format (message + " and {0} more", change_set.Changes.Count - 1);

            } else {
                return "did something magical";
            }
        }
    }
}
