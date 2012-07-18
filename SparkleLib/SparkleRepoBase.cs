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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Timers = System.Timers;

namespace SparkleLib {

    public enum SyncStatus {
        Idle,
        SyncUp,
        SyncDown,
        Error
    }


    public abstract class SparkleRepoBase {

        public abstract string CurrentRevision { get; }
        public abstract double Size { get; }
        public abstract double HistorySize { get; }
        public abstract List<string> ExcludePaths { get; }
        public abstract bool HasUnsyncedChanges { get; set; }
        public abstract bool HasLocalChanges { get; }
        public abstract bool HasRemoteChanges { get; }
        public abstract bool SyncUp ();
        public abstract bool SyncDown ();
        public abstract List<SparkleChangeSet> GetChangeSets (int count);


        public delegate void SyncStatusChangedEventHandler (SyncStatus new_status);
        public event SyncStatusChangedEventHandler SyncStatusChanged;

        public delegate void ProgressChangedEventHandler (double percentage, string speed);
        public event ProgressChangedEventHandler ProgressChanged;

        public delegate void NewChangeSetEventHandler (SparkleChangeSet change_set);
        public event NewChangeSetEventHandler NewChangeSet;

        public delegate void ConflictResolvedEventHandler ();
        public event ConflictResolvedEventHandler ConflictResolved;

        public delegate void ChangesDetectedEventHandler ();
        public event ChangesDetectedEventHandler ChangesDetected;

        public readonly string LocalPath;
        public readonly string Name;
        public readonly Uri RemoteUrl;
        public List<SparkleChangeSet> ChangeSets { get; protected set; }
        public SyncStatus Status { get; private set; }
        public bool ServerOnline { get; private set; }
        public bool IsBuffering { get; private set; }
        public double ProgressPercentage { get; private set; }
        public string ProgressSpeed { get; private set; }

        public string Identifier {
            get {
                if (this.identifier != null)
                    return this.identifier;

                string id_path = Path.Combine (LocalPath, ".sparkleshare");

                if (File.Exists (id_path))
                    this.identifier = File.ReadAllText (id_path).Trim ();

                if (!string.IsNullOrEmpty (this.identifier)) {
                    return this.identifier;

                } else {
                    string config_identifier = this.local_config.GetIdentifierForFolder (Name);

                    if (!string.IsNullOrEmpty (config_identifier))
                        this.identifier = config_identifier;
                    else
                        this.identifier = SparkleFetcherBase.CreateIdentifier ();

                    File.WriteAllText (id_path, this.identifier);
                    File.SetAttributes (id_path, FileAttributes.Hidden);

                    SparkleHelpers.DebugInfo ("Local", Name + " | Assigned identifier: " + this.identifier);

                    return this.identifier;
                }
            }
        }

        public virtual string [] UnsyncedFilePaths {
            get {
                return new string [0];
            }
        }


        protected SparkleConfig local_config;


        private string identifier;
        private SparkleListenerBase listener;
        private TimeSpan poll_interval            = PollInterval.Short;
        private DateTime last_poll                = DateTime.Now;
        private DateTime progress_last_change     = DateTime.Now;
        private TimeSpan progress_change_interval = new TimeSpan (0, 0, 0, 1);


        private Timers.Timer remote_timer = new Timers.Timer () {
            Interval = 5000
        };

        private bool is_syncing {
            get {
                return (Status == SyncStatus.SyncUp || Status == SyncStatus.SyncDown || IsBuffering);
            }
        }

        private static class PollInterval {
            public static readonly TimeSpan Short = new TimeSpan (0, 0, 5, 0);
            public static readonly TimeSpan Long  = new TimeSpan (0, 0, 15, 0);
        }


        public SparkleRepoBase (string path, SparkleConfig config)
        {
            this.local_config = config;
            LocalPath         = path;
            Name              = Path.GetFileName (LocalPath);
            RemoteUrl         = new Uri (this.local_config.GetUrlForFolder (Name));
            IsBuffering       = false;
            ServerOnline      = true;
            this.identifier   = Identifier;
            ChangeSets        = GetChangeSets ();

            SyncStatusChanged += delegate (SyncStatus status) {
                Status = status;
            };

            SparkleWatcherFactory.CreateWatcher (this);
            new Thread (() => CreateListener ()).Start ();

            this.remote_timer.Elapsed += delegate {
                bool time_to_poll = (DateTime.Compare (this.last_poll,
                    DateTime.Now.Subtract (this.poll_interval)) < 0);

                if (time_to_poll && !is_syncing) {
                    this.last_poll = DateTime.Now;

                    if (HasRemoteChanges)
                        SyncDownBase ();
                }

                // In the unlikely case that we haven't synced up our
                // changes or the server was down, sync up again
                if (HasUnsyncedChanges && !is_syncing && ServerOnline)
                    SyncUpBase ();
            };
        }


        public void Initialize ()
        {
            // Sync up everything that changed
            // since we've been offline
            if (HasLocalChanges) {
                SyncUpBase ();

                while (HasUnsyncedChanges)
                    SyncUpBase ();
            }

            this.remote_timer.Start ();
        }


        public List<SparkleChangeSet> GetChangeSets () {
            return GetChangeSets (30);
        }


        public void OnFileActivity (FileSystemEventArgs args)
        {
            if (IsBuffering)
                return;

            if (ChangesDetected != null)
                ChangesDetected ();

            string relative_path = args.FullPath.Replace (LocalPath, "");

            foreach (string exclude_path in ExcludePaths) {
                if (relative_path.Contains (exclude_path))
                    return;
            }

            if (IsBuffering || !HasLocalChanges)
                return;

            SparkleHelpers.DebugInfo ("Local", Name + " | Activity detected, waiting for it to settle...");

            IsBuffering = true;
            this.remote_timer.Stop ();

            List<double> size_buffer = new List<double> ();

            do {
                if (size_buffer.Count >= 4)
                    size_buffer.RemoveAt (0);

                DirectoryInfo info = new DirectoryInfo (LocalPath);
                size_buffer.Add (CalculateSize (info));

                if (size_buffer.Count >= 4 &&
                    size_buffer [0].Equals (size_buffer [1]) &&
                    size_buffer [1].Equals (size_buffer [2]) &&
                    size_buffer [2].Equals (size_buffer [3])) {

                    SparkleHelpers.DebugInfo ("Local", Name + " | Activity has settled");
                    IsBuffering = false;

                    if (HasLocalChanges) {
                        do {
                            SyncUpBase ();

                        } while (HasLocalChanges);

                    } else {
                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Idle);
                    }

                } else {
                    Thread.Sleep (500);
                }

            } while (IsBuffering);

            this.remote_timer.Start ();
        }


        protected void OnConflictResolved ()
        {
            if (ConflictResolved != null)
                ConflictResolved ();
        }


        protected void OnProgressChanged (double progress_percentage, string progress_speed)
        {
            // Only trigger the ProgressChanged event once per second
            if (DateTime.Compare (this.progress_last_change, DateTime.Now.Subtract (this.progress_change_interval)) >= 0)
                return;

            if (ProgressChanged != null) {
                if (progress_percentage == 100.0)
                    progress_percentage = 99.0;

                ProgressPercentage        = progress_percentage;
                ProgressSpeed             = progress_speed;
                this.progress_last_change = DateTime.Now;

                ProgressChanged (progress_percentage, progress_speed);
            }
        }


        private void SyncUpBase ()
        {
            SparkleHelpers.DebugInfo ("SyncUp", Name + " | Initiated");
            HasUnsyncedChanges = true;

            this.remote_timer.Stop ();

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncUp);

            if (SyncUp ()) {
                SparkleHelpers.DebugInfo ("SyncUp", Name + " | Done");
                HasUnsyncedChanges = false;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Idle);

                this.listener.Announce (new SparkleAnnouncement (Identifier, CurrentRevision));

            } else {
                SparkleHelpers.DebugInfo ("SyncUp", Name + " | Error");
                SyncDownBase ();

                if (ServerOnline && SyncUp ()) {
                    HasUnsyncedChanges = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle);

                    this.listener.Announce (new SparkleAnnouncement (Identifier, CurrentRevision));

                } else {
                    ServerOnline = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Error);
                }
            }

            this.remote_timer.Start ();

            ProgressPercentage = 0.0;
            ProgressSpeed      = "";
        }


        private void SyncDownBase ()
        {
            SparkleHelpers.DebugInfo ("SyncDown", Name + " | Initiated");
            this.remote_timer.Stop ();

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncDown);

            string pre_sync_revision = CurrentRevision;

            if (SyncDown ()) {
                SparkleHelpers.DebugInfo ("SyncDown", Name + " | Done");
                ServerOnline = true;

                if (!pre_sync_revision.Equals (CurrentRevision)) {
                   if (ChangeSets != null &&
                       ChangeSets.Count > 0) {

                        bool emit_change_event = true;
                        foreach (SparkleChange change in ChangeSets [0].Changes)
                            if (change.Path.EndsWith (".sparkleshare"))
                                emit_change_event = false;
                        
                        if (NewChangeSet != null && emit_change_event)
                            NewChangeSet (ChangeSets [0]);
                    }
                }

                // There could be changes from a resolved
                // conflict. Tries only once, then lets
                // the timer try again periodically
                if (HasUnsyncedChanges) {
                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.SyncUp);
                    
                    SyncUp ();
                    HasUnsyncedChanges = false;
                }
                
                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Idle);

            } else {
                SparkleHelpers.DebugInfo ("SyncDown", Name + " | Error");
                ServerOnline = false;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Error);
            }

            ProgressPercentage = 0.0;
            ProgressSpeed      = "";

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.Idle);

            this.remote_timer.Start ();
        }


        private void CreateListener ()
        {
            this.listener = SparkleListenerFactory.CreateListener (Name, Identifier);

            if (this.listener.IsConnected) {
                this.poll_interval = PollInterval.Long;

                new Thread (() => {
                    if (!is_syncing && !HasLocalChanges && HasRemoteChanges)
                        SyncDownBase ();

                }).Start ();
            }

            this.listener.Connected            += ListenerConnectedDelegate;
            this.listener.Disconnected         += ListenerDisconnectedDelegate;
            this.listener.AnnouncementReceived += ListenerAnnouncementReceivedDelegate;

            // Start listening
            if (!this.listener.IsConnected && !this.listener.IsConnecting)
                this.listener.Connect ();
        }


        // Stop polling when the connection to the irc channel is succesful
        private void ListenerConnectedDelegate ()
        {
            this.poll_interval = PollInterval.Long;
            this.last_poll     = DateTime.Now;

            if (!is_syncing) {
                // Check for changes manually one more time
                if (HasRemoteChanges)
                    SyncDownBase ();

                // Push changes that were made since the last disconnect
                if (HasUnsyncedChanges)
                    SyncUpBase ();
            }
        }


        // Start polling when the connection to the channel is lost
        private void ListenerDisconnectedDelegate ()
        {
            this.poll_interval = PollInterval.Short;
            SparkleHelpers.DebugInfo (Name, "Falling back to polling");
        }


        // Fetch changes when there is an announcement
        private void ListenerAnnouncementReceivedDelegate (SparkleAnnouncement announcement)
        {
            string identifier = Identifier;

            if (announcement.FolderIdentifier.Equals (identifier) &&
                !announcement.Message.Equals (CurrentRevision)) {

                while (this.is_syncing)
                    Thread.Sleep (100);

                SparkleHelpers.DebugInfo ("Listener", "Syncing due to announcement");
                SyncDownBase ();

            } else {
                if (announcement.FolderIdentifier.Equals (identifier))
                    SparkleHelpers.DebugInfo ("Listener", "Not syncing, message is for current revision");
            }
        }


        // Recursively gets a folder's size in bytes
        private double CalculateSize (DirectoryInfo parent)
        {
            if (!Directory.Exists (parent.ToString ()))
                return 0;

            double size = 0;

            if (ExcludePaths.Contains (parent.Name))
                return 0;

            try {
                foreach (FileInfo file in parent.GetFiles ()) {
                    if (!file.Exists)
                        return 0;

                    size += file.Length;
                }

                foreach (DirectoryInfo directory in parent.GetDirectories ())
                    size += CalculateSize (directory);

            } catch (Exception) {
                return 0;
            }

            return size;
        }


        public void Dispose ()
        {
            this.remote_timer.Stop ();
            this.remote_timer.Dispose ();

            this.listener.Connected            -= ListenerConnectedDelegate;
            this.listener.Disconnected         -= ListenerDisconnectedDelegate;
            this.listener.AnnouncementReceived -= ListenerAnnouncementReceivedDelegate;

            SparkleWatcherFactory.DisposeWatcher (this);
        }
    }
}
