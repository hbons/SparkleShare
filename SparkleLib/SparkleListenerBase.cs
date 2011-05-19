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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Meebey.SmartIrc4net;

namespace SparkleLib {

    public enum NotificationServerType
    {
        Own,
        Central
    }


    // A persistent connection to the server that
    // listens for change notifications
    public abstract class SparkleListenerBase {

        // We've connected to the server
        public event ConnectedEventHandler Connected;
        public delegate void ConnectedEventHandler ();

        // We've disconnected from the server
        public event DisconnectedEventHandler Disconnected;
        public delegate void DisconnectedEventHandler ();

        // We've been notified about a remote
        // change by the channel
        public event RemoteChangeEventHandler RemoteChange;
        public delegate void RemoteChangeEventHandler (string change_id);

        // Starts listening for remote changes
        public abstract void Connect ();

        // Announces to the channel that
        // we've pushed changes to the server
        public abstract void Announce (string message);

        // Release all resources
        public abstract void Dispose ();

        public abstract bool IsConnected { get; }

        // Announcements that weren't sent off
        // because we were disconnected
        protected List<string> announce_queue = new List<string> ();

        // Announcements of remote changes that we've received
        public int ChangesQueue {
            get {
                return this.changes_queue;
            }
        }

        protected string server;
        protected string channel;
        protected int changes_queue = 0;

        public SparkleListenerBase (string server, string folder_identifier, NotificationServerType type) { }

        public void DecrementChangesQueue ()
        {
            this.changes_queue--;
        }

        public void OnConnected ()
        {
            if (Connected != null)
                Connected ();
        }

        public void OnDisconnected ()
        {
            if (Disconnected != null)
                Disconnected ();
        }

        public void OnRemoteChange (string change_id)
        {
            if (RemoteChange != null)
                RemoteChange (change_id);
        }
    }
}
