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
using System.Text;

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
                ShowBubble (change_set.User.Name, change_set.ToMessage (),
                    Program.Controller.GetAvatar (change_set.User.Email, 48));
            };
        }


        public void ShowBubble (string title, string subtext, string image_path)
        {
            byte [] title_bytes   = Encoding.Default.GetBytes (title);
            byte [] subtext_bytes = Encoding.Default.GetBytes (subtext);
            title                 = Encoding.UTF8.GetString (title_bytes);
            subtext               = Encoding.UTF8.GetString (subtext_bytes);

            ShowBubbleEvent (title, subtext, image_path);
        }


        public void BubbleClicked ()
        {
            Program.Controller.ShowEventLogWindow ();
        }
    }
}
