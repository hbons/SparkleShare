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
using System.Timers;

namespace SparkleLib {

    // A persistent connection to the server that
    // listens for change notifications
    public abstract class SparkleListenerBase {

        public event Action Connected = delegate { };
        public event Action Disconnected = delegate { };

        public event AnnouncementReceivedEventHandler AnnouncementReceived = delegate { };
        public delegate void AnnouncementReceivedEventHandler (SparkleAnnouncement announcement);

        public readonly Uri Server;

        public abstract void Connect ();
        public abstract bool IsConnected { get; }
        public abstract bool IsConnecting { get; }


        protected abstract void AnnounceInternal (SparkleAnnouncement announcent);
        protected abstract void AlsoListenToInternal (string folder_identifier);

        protected List<string> channels = new List<string> ();


        private int max_recent_announcements = 10;

        private Dictionary<string, List<SparkleAnnouncement>> recent_announcements =
            new Dictionary<string, List<SparkleAnnouncement>> ();

        private Dictionary<string, SparkleAnnouncement> queue_up   = new Dictionary<string, SparkleAnnouncement> ();

        private Timer reconnect_timer = new Timer {
            Interval = 60 * 1000,
            Enabled = true
        };


        public SparkleListenerBase (Uri server, string folder_identifier)
        {
            Server = server;
            this.channels.Add (folder_identifier);

            this.reconnect_timer.Elapsed += delegate {
                if (!IsConnected && !IsConnecting)
                    Reconnect ();
            };

            this.reconnect_timer.Start ();
        }


        public void Announce (SparkleAnnouncement announcement)
        {
            if (!IsRecentAnnouncement (announcement)) {
                if (IsConnected) {
                    SparkleLogger.LogInfo ("Listener", "Announcing message " + announcement.Message +
                        " to " + announcement.FolderIdentifier + " on " + Server);

                    AnnounceInternal (announcement);
                    AddRecentAnnouncement (announcement);

                } else {
                    SparkleLogger.LogInfo ("Listener", "Can't send message to " + Server + ". Queuing message");
                    this.queue_up [announcement.FolderIdentifier] = announcement;
                }

            } else {
                SparkleLogger.LogInfo ("Listener", "Already processed message " + announcement.Message +
                    " to " + announcement.FolderIdentifier + " from " + Server);
            }
        }


        public void AlsoListenTo (string channel)
        {
            if (!this.channels.Contains (channel))
                this.channels.Add (channel);

            if (IsConnected) {
                SparkleLogger.LogInfo ("Listener", "Subscribing to channel " + channel + " on " + Server);
                AlsoListenToInternal (channel);
            }
        }


        public void Reconnect ()
        {
            SparkleLogger.LogInfo ("Listener", "Trying to reconnect to " + Server);
            Connect ();
        }


        public void OnConnected ()
        {
            foreach (string channel in this.channels.GetRange (0, this.channels.Count)) {
                SparkleLogger.LogInfo ("Listener", "Subscribing to channel " + channel + " on " + Server);
                AlsoListenToInternal (channel);
            }

            SparkleLogger.LogInfo ("Listener", "Listening for announcements on " + Server);
            Connected ();

            if (this.queue_up.Count > 0) {
                SparkleLogger.LogInfo ("Listener", "Delivering " + this.queue_up.Count + " queued messages...");

                foreach (KeyValuePair<string, SparkleAnnouncement> item in this.queue_up) {
                    SparkleAnnouncement announcement = item.Value;
                    Announce (announcement);
                }
            }
        }


        public void OnDisconnected (string message)
        {
            SparkleLogger.LogInfo ("Listener", "Disconnected from " + Server + ": " + message);
            Disconnected ();
        }


        public void OnAnnouncement (SparkleAnnouncement announcement)
        {
            SparkleLogger.LogInfo ("Listener", "Got message " + announcement.Message + " from " +
                announcement.FolderIdentifier + " on " + Server);

            if (IsRecentAnnouncement (announcement))
                return;

            AddRecentAnnouncement (announcement);
            AnnouncementReceived (announcement);
        }


        public virtual void Dispose ()
        {
            this.reconnect_timer.Dispose ();
        }


        private bool IsRecentAnnouncement (SparkleAnnouncement announcement)
        {
            if (!this.recent_announcements.ContainsKey (announcement.FolderIdentifier)) {
                return false;

            } else {
                foreach (SparkleAnnouncement recent_announcement in GetRecentAnnouncements (announcement.FolderIdentifier)) {
                    if (recent_announcement.Message.Equals (announcement.Message))
                        return true;
                }

                return false;
            }
        }


        private List<SparkleAnnouncement> GetRecentAnnouncements (string folder_identifier)
        {
            if (!this.recent_announcements.ContainsKey (folder_identifier))
                this.recent_announcements [folder_identifier] = new List<SparkleAnnouncement> ();

            return this.recent_announcements [folder_identifier];
        }


        private void AddRecentAnnouncement (SparkleAnnouncement announcement)
        {
            List<SparkleAnnouncement> recent_announcements =
                GetRecentAnnouncements (announcement.FolderIdentifier);

            if (!IsRecentAnnouncement (announcement))
                recent_announcements.Add (announcement);

            if (recent_announcements.Count > this.max_recent_announcements)
                recent_announcements.RemoveRange (0, recent_announcements.Count - this.max_recent_announcements);
        }
    }
}
