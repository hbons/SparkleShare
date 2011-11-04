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
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using System.Linq;

namespace SparkleLib {

    public class SparkleAnnouncement {

        public readonly string FolderIdentifier;
        public readonly string Message;


        public SparkleAnnouncement (string folder_identifier, string message)
        {
            FolderIdentifier = folder_identifier;
            Message          = message;
        }
    }


    public static class SparkleListenerFactory {

        private static List<SparkleListenerBase> listeners = new List<SparkleListenerBase> ();

        public static SparkleListenerBase CreateListener (string folder_name, string folder_identifier)
        {
            string uri = SparkleConfig.DefaultConfig.GetFolderOptionalAttribute (
                folder_name, "announcements_url");

            if (uri == null) {
                // This is SparkleShare's centralized notification service.
                // Don't worry, we only use this server as a backup if you
                // don't have your own. All data needed to connect is hashed and
                // we don't store any personal information ever

                uri = "tcp://204.62.14.135:1986"; // TODO: announcements.sparkleshare.org
            }

            Uri announce_uri = new Uri (uri);

            // We use only one listener per server to keep
            // the number of connections as low as possible
            foreach (SparkleListenerBase listener in listeners) {
                if (listener.Server.Equals (announce_uri)) {
                    SparkleHelpers.DebugInfo ("ListenerFactory",
                        "Refered to existing listener for " + announce_uri);

                    listener.AlsoListenTo (folder_identifier);
                    return (SparkleListenerBase) listener;
                }
            }

            // Create a new listener with the appropriate
            // type if one doesn't exist yet for that server
            switch (announce_uri.Scheme) {
            case "tcp":
                listeners.Add (new SparkleListenerTcp (announce_uri, folder_identifier));
                break;
            case "irc":
                listeners.Add (new SparkleListenerIrc (announce_uri, folder_identifier));
                break;
            default:
                listeners.Add (new SparkleListenerTcp (announce_uri, folder_identifier));
                break;
            }
            
            SparkleHelpers.DebugInfo ("ListenerFactory", "Issued new listener for " + announce_uri);
            return (SparkleListenerBase) listeners [listeners.Count - 1];
        }
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
        public event AnnouncementEventHandler Announcement;
        public delegate void AnnouncementEventHandler (SparkleAnnouncement announcement);


        public abstract void Connect ();
        public abstract void Announce (SparkleAnnouncement announcent);
        public abstract void AlsoListenTo (string folder_identifier);
        public abstract bool IsConnected { get; }


        protected List<string> channels                = new List<string> ();
        protected Hashtable last_announce              = new Hashtable ();
        protected List<SparkleAnnouncement> queue_up   = new List<SparkleAnnouncement> ();
        protected List<SparkleAnnouncement> queue_down = new List<SparkleAnnouncement> ();
        protected bool is_connecting;
        protected Uri server;
        protected Timer reconnect_timer = new Timer { Interval = 60 * 1000, Enabled = true };

        public SparkleListenerBase (Uri server, string folder_identifier)
        {
            this.server = server;

            this.reconnect_timer.Elapsed += delegate {
                if (!IsConnected && !this.is_connecting)
                    Reconnect ();
            };

            this.reconnect_timer.Start ();
        }


        public void AnnounceBase (SparkleAnnouncement announcement)
        {
            if (IsConnected) {
                SparkleHelpers.DebugInfo ("Listener",
                    "Announcing to " + announcement.FolderIdentifier + " on " + this.server);

                Announce (announcement);

            } else {
                SparkleHelpers.DebugInfo ("Listener", "Not connected to " + this.server + ". Queuing message");
                this.queue_up.Add (announcement);
            }
        }


        public string NextQueueDownMessage (string folder_identifier)
        {
            foreach (SparkleAnnouncement announcement in this.queue_down.GetRange (0, this.queue_down.Count)) {
                if (announcement.FolderIdentifier.Equals (folder_identifier)) {
                    string message = announcement.Message;
                    this.queue_down.Remove (announcement);
                    return message;
                }
            }

            return null;
        }


        public void Reconnect ()
        {
            SparkleHelpers.DebugInfo ("Listener", "Trying to reconnect to " + this.server);
            Connect ();
        }


        public void OnConnected ()
        {
            SparkleHelpers.DebugInfo ("Listener", "Connected to " + Server);

            if (Connected != null)
                Connected ();

            if (this.queue_up.Count > 0) {
                SparkleHelpers.DebugInfo ("Listener", "Delivering " + this.queue_up.Count + " queued messages...");

                foreach (SparkleAnnouncement announcement in this.queue_up.GetRange(0, this.queue_up.Count)) {
                    AnnounceBase (announcement);
                    this.queue_up.Remove (announcement);
                }
            }
        }


        public void OnDisconnected ()
        {
            SparkleHelpers.DebugInfo ("Listener", "Disonnected from " + Server);

            if (Disconnected != null)
                Disconnected ();
        }


        public void OnAnnouncement (SparkleAnnouncement announcement)
        {
            SparkleHelpers.DebugInfo ("Listener", "Got message " + announcement.Message + " from " + announcement.FolderIdentifier + " on " + this.server);

            if (this.last_announce.ContainsKey (announcement.FolderIdentifier) ){
                SparkleHelpers.DebugInfo ("Listener", "Received previous message from " + announcement.FolderIdentifier + " on " + this.server);
                if (this.last_announce[announcement.FolderIdentifier].Equals(announcement.Message)) {
                    SparkleHelpers.DebugInfo ("Listener", "Ignoring already processed announcment " + announcement.Message + " from " + announcement.FolderIdentifier + " on " + this.server);
                    return;
                }
            }

            SparkleHelpers.DebugInfo ("Listener", "Processing message " + announcement.Message + " from " + announcement.FolderIdentifier + " on " + this.server);
            if (this.last_announce.ContainsKey (announcement.FolderIdentifier) )
                this.last_announce.Remove (announcement.FolderIdentifier);
                
            this.last_announce.Add (announcement.FolderIdentifier, announcement.Message);
            this.queue_down.Add (announcement);

            if (Announcement != null)
                Announcement (announcement);
        }


        public virtual void Dispose ()
        {
            this.reconnect_timer.Dispose ();
        }


        public Uri Server {
            get {
                return this.server;
            }
        }


        public bool IsConnecting {
            get {
               return this.is_connecting;
            }
        }
    }
}
