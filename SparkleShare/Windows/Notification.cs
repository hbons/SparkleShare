//   SparkleShare, an instant update workflow to Git.
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
using Gdk;

namespace Notifications
{
    public enum Urgency : byte {
        Low = 0,
        Normal,
        Critical
    }

    public class ActionArgs : EventArgs {
        private string action;
        public string Action {
          get { return action; }
        }

        public ActionArgs (string action) {
          this.action = action;
        }
    }

    public delegate void ActionHandler (object o, ActionArgs args);

    public class Notification
    {
        public Pixbuf Icon;

        public Notification ()
        {
        }

        public Notification (string title, string subtext)
        {
        }

        public void AddAction (string action, string label, ActionHandler handler)
        {
        }

        public void RemoveAction (string action)
        {
        }

        public void ClearActions ()
        {
        }

        public void Show ()
        {
        }
    }
}
