//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as 
//   published by the Free Software Foundation, either version 3 of the 
//   License, or (at your option) any later version.
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

namespace Sparkles {

    public enum DisconnectReason {
        None,
        TimeOut,
        SystemSleep
    }


    // A persistent connection to the server that
    // listens for change notifications
    public abstract class BaseListener {

        public event Action Connected = delegate { };
        
        public event DisconnectedEventHandler Disconnected = delegate { };
        public delegate void DisconnectedEventHandler (DisconnectReason reason);

        public event AnnouncementReceivedEventHandler AnnouncementReceived = delegate { };
        public delegate void AnnouncementReceivedEventHandler (Announcement announcement);

        public readonly Uri Server;

        public abstract void Connect ();
        public abstract bool IsConnected { get; }
        public abstract bool IsConnecting { get; }


        protected abstract void AnnounceInternal (Announcement announcent);
        protected abstract void AlsoListenToInternal (string folder_identifier);

        protected List<string> channels = new List<string> ();


        private int max_recent_announcements = 10;

        private Dictionary<string, List<Announcement>> recent_announcements =
            new Dictionary<string, List<Announcement>> ();

        private Dictionary<string, Announcement> queue_up   = new Dictionary<string, Announcement> ();

        private Timer reconnect_timer = new Timer {
            Interval = 60 * 1000,
            Enabled = true
        };


        public BaseListener (Uri server, string folder_identifier)
        {
            Server = server;
            this.channels.Add (folder_identifier);

            this.reconnect_timer.Elapsed += OnTimerElapsed;
            this.reconnect_timer.Start ();
        }

        private void OnTimerElapsed(object sender, EventArgs args)
        {
            if (!IsConnected && !IsConnecting)
                Reconnect ();
        }

        public void Announce (Announcement announcement)
        {
            if (!IsRecentAnnouncement (announcement)) {
                if (IsConnected) {
                    Logger.LogInfo ("Listener", "Announcing message " + announcement.Message +
                        " to " + announcement.FolderIdentifier + " on " + Server);

                    AnnounceInternal (announcement);
                    AddRecentAnnouncement (announcement);

                } else {
                    Logger.LogInfo ("Listener", "Can't send message to " + Server + ". Queuing message");
                    this.queue_up [announcement.FolderIdentifier] = announcement;
                }

            } else {
                Logger.LogInfo ("Listener", "Already processed message " + announcement.Message +
                    " to " + announcement.FolderIdentifier + " from " + Server);
            }
        }


        public void AlsoListenTo (string channel)
        {
            if (!this.channels.Contains (channel))
                this.channels.Add (channel);

            if (IsConnected) {
                Logger.LogInfo ("Listener", "Subscribing to channel " + channel + " on " + Server);
                AlsoListenToInternal (channel);
            }
        }


        public void Reconnect ()
        {
            Logger.LogInfo ("Listener", "Trying to reconnect to " + Server);
            Connect ();
        }


        public void OnConnected ()
        {
            foreach (string channel in this.channels.GetRange (0, this.channels.Count)) {
                Logger.LogInfo ("Listener", "Subscribing to channel " + channel + " on " + Server);
                AlsoListenToInternal (channel);
            }

            Logger.LogInfo ("Listener", "Listening for announcements on " + Server);
            Connected ();

            if (this.queue_up.Count > 0) {
                Logger.LogInfo ("Listener", "Delivering " + this.queue_up.Count + " queued messages...");

                foreach (KeyValuePair<string, Announcement> item in this.queue_up) {
                    Announcement announcement = item.Value;
                    Announce (announcement);
                }
            }
        }


        public void OnDisconnected (DisconnectReason reason, string message)
        {
            Logger.LogInfo ("Listener", "Disconnected from " + Server + ": " + message);
            Disconnected (reason);
        }


        public void OnAnnouncement (Announcement announcement)
        {
            Logger.LogInfo ("Listener", "Got message " + announcement.Message + " from " +
                announcement.FolderIdentifier + " on " + Server);

            if (IsRecentAnnouncement (announcement))
                return;

            AddRecentAnnouncement (announcement);
            AnnouncementReceived (announcement);
        }


        public virtual void Dispose ()
        {
            if (this.reconnect_timer != null) {
                this.reconnect_timer.Stop ();

                this.reconnect_timer.Elapsed -= OnTimerElapsed;
                this.reconnect_timer.Dispose ();

                this.reconnect_timer = null;
            }
        }


        private bool IsRecentAnnouncement (Announcement announcement)
        {
            if (!this.recent_announcements.ContainsKey (announcement.FolderIdentifier)) {
                return false;

            } else {
                foreach (Announcement recent_announcement in GetRecentAnnouncements (announcement.FolderIdentifier)) {
                    if (recent_announcement.Message.Equals (announcement.Message))
                        return true;
                }

                return false;
            }
        }


        private List<Announcement> GetRecentAnnouncements (string folder_identifier)
        {
            if (!this.recent_announcements.ContainsKey (folder_identifier))
                this.recent_announcements [folder_identifier] = new List<Announcement> ();

            return this.recent_announcements [folder_identifier];
        }


        private void AddRecentAnnouncement (Announcement announcement)
        {
            List<Announcement> recent_announcements =
                GetRecentAnnouncements (announcement.FolderIdentifier);

            if (!IsRecentAnnouncement (announcement))
                recent_announcements.Add (announcement);

            if (recent_announcements.Count > this.max_recent_announcements)
                recent_announcements.RemoveRange (0, recent_announcements.Count - this.max_recent_announcements);
        }
    }
}
