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
using System.IO;
using System.Threading;

using Timers = System.Timers;

namespace Sparkles {

    public enum StorageType {
        Unknown,
        Plain,
        LargeFiles,
        Encrypted
    }


    public class StorageTypeInfo {

        public readonly StorageType Type;

        public readonly string Name;
        public readonly string Description;


        public StorageTypeInfo (StorageType storage_type, string name, string description)
        {
            Type = storage_type;

            Name = name;
            Description = description;
        }
    }

    public enum SyncStatus {
        Idle,
        Paused,
        SyncUp,
        SyncDown,
        Error
    }

    public enum ErrorStatus {
        None,
        HostUnreachable,
        HostIdentityChanged,
        AuthenticationFailed,
        DiskSpaceExceeded,
        UnreadableFiles,
        NotFound,
        IncompatibleClientServer,
        Unknown
    }


    public abstract class BaseRepository {

        public abstract bool SyncUp ();
        public abstract bool SyncDown ();
        public abstract void RestoreFile (string path, string revision, string target_file_path);
        public abstract bool HasUnsyncedChanges { get; set; }
        public abstract bool HasLocalChanges { get; }
        public abstract bool HasRemoteChanges { get; }

        public abstract string CurrentRevision { get; }
        public abstract double Size { get; }
        public abstract double HistorySize { get; }

        public abstract List<string> ExcludePaths { get; }
        public abstract List<Change> UnsyncedChanges { get; }
        public abstract List<ChangeSet> GetChangeSets ();
        public abstract List<ChangeSet> GetChangeSets (string path);

        protected StorageType StorageType = StorageType.Plain;

        public static bool UseCustomWatcher = false;


        public event SyncStatusChangedEventHandler SyncStatusChanged = delegate { };
        public delegate void SyncStatusChangedEventHandler (SyncStatus new_status);

        public event ProgressChangedEventHandler ProgressChanged = delegate { };
        public delegate void ProgressChangedEventHandler ();

        public event NewChangeSetEventHandler NewChangeSet = delegate { };
        public delegate void NewChangeSetEventHandler (ChangeSet change_set);

        public event Action ConflictResolved = delegate { };
        public event Action ChangesDetected = delegate { };


        public readonly string LocalPath;
        public readonly string Name;
        public readonly Uri RemoteUrl;
        public List<ChangeSet> ChangeSets { get; set; }
        public SyncStatus Status { get; set; }
        public ErrorStatus Error { get; protected set; }
        public bool IsBuffering { get; set; }

        public double ProgressPercentage { get; private set; }
        public double ProgressSpeed { get; private set; }
        public string ProgressInformation { get; private set; }

        public DateTime LastSync {
            get {
                if (ChangeSets != null && ChangeSets.Count > 0)
                    return ChangeSets [0].Timestamp;
                else
                    return DateTime.MinValue;
            }
        }

        public virtual string Identifier {
            get {
                if (this.identifier != null)
                    return this.identifier;

                string id_path = Path.Combine (LocalPath, ".sparkleshare");

                if (File.Exists (id_path))
                    this.identifier = File.ReadAllText (id_path).Trim ();

                if (!string.IsNullOrEmpty (this.identifier)) {
                    return this.identifier;

                } else {
                    string config_identifier = this.local_config.IdentifierByName (Name);

                    if (!string.IsNullOrEmpty (config_identifier))
                        this.identifier = config_identifier;
                    else
                        this.identifier = Path.GetRandomFileName ().SHA256 ();

                    File.WriteAllText (id_path, this.identifier);
                    File.SetAttributes (id_path, FileAttributes.Hidden);

                    Logger.LogInfo ("Local", Name + " | Assigned identifier: " + this.identifier);

                    return this.identifier;
                }
            }
        }


        protected Configuration local_config;

        string identifier;
        BaseListener listener;
        Watcher watcher;
        TimeSpan poll_interval        = PollInterval.Short;
        DateTime last_poll            = DateTime.Now;
        Timers.Timer remote_timer     = new Timers.Timer () { Interval = 5000 };
        DisconnectReason last_disconnect_reason = DisconnectReason.None;

        bool is_syncing {
            get { return (Status == SyncStatus.SyncUp || Status == SyncStatus.SyncDown || IsBuffering); }
        }

        static class PollInterval {
            public static readonly TimeSpan Short = new TimeSpan (0, 0, 5, 0);
            public static readonly TimeSpan Long  = new TimeSpan (0, 0, 15, 0);
        }


        public BaseRepository (string path, Configuration config)
        {
            Logger.LogInfo (path, "Initializing...");

            Status            = SyncStatus.Idle;
            Error             = ErrorStatus.None;
            this.local_config = config;
            LocalPath         = path;
            Name              = Path.GetFileName (LocalPath);
            RemoteUrl         = new Uri (this.local_config.UrlByName (Name));
            IsBuffering       = false;
            this.identifier   = Identifier;
            ChangeSets        = GetChangeSets ();

            string storage_type = this.local_config.GetFolderOptionalAttribute (Name, "storage_type");

            if (!string.IsNullOrEmpty (storage_type))
                StorageType = (StorageType) Enum.Parse(typeof(StorageType), storage_type);

            string is_paused = this.local_config.GetFolderOptionalAttribute (Name, "paused");
            if (is_paused != null && is_paused.Equals (bool.TrueString))
                Status = SyncStatus.Paused;

            string identifier_file_path = Path.Combine (LocalPath, ".sparkleshare");
            File.SetAttributes (identifier_file_path, FileAttributes.Hidden);

            if (!UseCustomWatcher)
                this.watcher = new Watcher (LocalPath);

            new Thread (() => CreateListener ()).Start ();

            this.remote_timer.Elapsed += RemoteTimerElapsedDelegate;
        }


        void RemoteTimerElapsedDelegate (object sender, EventArgs args)
        {
            if (this.is_syncing || IsBuffering || Status == SyncStatus.Paused)
                return;
            
            int time_comparison = DateTime.Compare (this.last_poll, DateTime.Now.Subtract (this.poll_interval));
            
            if (time_comparison < 0) {
                if (HasUnsyncedChanges && !this.is_syncing)
                    SyncUpBase ();
                
                this.last_poll = DateTime.Now;
                
                if (HasRemoteChanges && !this.is_syncing)
                    SyncDownBase ();
                
                if (this.listener.IsConnected)
                    this.poll_interval = PollInterval.Long;
            }
            
            // In the unlikely case that we haven't synced up our
            // changes or the server was down, sync up again
            if (HasUnsyncedChanges && !this.is_syncing && Error == ErrorStatus.None)
                SyncUpBase ();
            
            if (Status != SyncStatus.Idle && Status != SyncStatus.Error) {
                Status = SyncStatus.Idle;
                SyncStatusChanged (Status);
            }
        }


        public void Initialize ()
        {
            // Sync up everything that changed since we've been offline
            new Thread (() => {
                if (Status != SyncStatus.Paused) {
                    if (HasRemoteChanges)
                        SyncDownBase ();

                    if (HasUnsyncedChanges || HasLocalChanges) {
                        do {
                            SyncUpBase ();

                        } while (HasLocalChanges);
                    }
                }
                
                if (!UseCustomWatcher)
                    this.watcher.ChangeEvent += OnFileActivity;

                this.remote_timer.Start ();
            
            }).Start ();
        }


        Object buffer_lock = new Object ();

        public void OnFileActivity (FileSystemEventArgs args)
        {
            if (IsBuffering || this.is_syncing)
                return;

            if (args != null) {
                foreach (string exclude_path in ExcludePaths) {
                    if (args.FullPath.Contains (Path.DirectorySeparatorChar + exclude_path))
                        return;
                }
            }
            
            if (Status == SyncStatus.Paused) {
                ChangesDetected ();
                return;
            }

            lock (this.buffer_lock) {
                if (IsBuffering || this.is_syncing || !HasLocalChanges)
                    return;

                IsBuffering = true;
            }

            ChangesDetected ();

            if (!UseCustomWatcher)
                this.watcher.Disable ();

            Logger.LogInfo ("Local", Name + " | Activity detected, waiting for it to settle...");

            List<double> size_buffer = new List<double> ();
            DirectoryInfo info = new DirectoryInfo (LocalPath);

            do {
                if (size_buffer.Count >= 4)
                    size_buffer.RemoveAt (0);

                size_buffer.Add (CalculateSize (info));

                if (size_buffer.Count >= 4 &&
                    size_buffer [0].Equals (size_buffer [1]) &&
                    size_buffer [1].Equals (size_buffer [2]) &&
                    size_buffer [2].Equals (size_buffer [3])) {

                    Logger.LogInfo ("Local", Name + " | Activity has settled");
                    IsBuffering = false;

                    bool first_sync = true;

                    if (HasLocalChanges && Status == SyncStatus.Idle) {
                        do {
                            if (!first_sync)
                                Logger.LogInfo ("Local", Name + " | More changes found");

                            SyncUpBase ();

                            if (Error == ErrorStatus.UnreadableFiles)
                                return;

                            first_sync = false;

                        } while (HasLocalChanges);
                    } 

                    if (Status != SyncStatus.Idle && Status != SyncStatus.Error) {
                        Status = SyncStatus.Idle;
                        SyncStatusChanged (Status);
                    }

                } else {
                    Thread.Sleep (500);
                }

            } while (IsBuffering);

            if (!UseCustomWatcher)
                this.watcher.Enable ();
        }


        public void ForceRetry ()
        {
            if (Error != ErrorStatus.None && !this.is_syncing)
                SyncUpBase ();
        }


        protected void OnConflictResolved ()
        {
            ConflictResolved ();
        }


        DateTime progress_last_change = DateTime.Now;

        protected void OnProgressChanged (double percentage, double speed, string information)
        {
            if (percentage < 1)
                return;

            // Only trigger the ProgressChanged event once per second
            if (DateTime.Compare (this.progress_last_change, DateTime.Now.Subtract (new TimeSpan (0, 0, 0, 1))) >= 0)
                return;

            if (percentage == 100.0)
                percentage = 99.0;

			progress_last_change = DateTime.Now;

            ProgressPercentage = percentage;
            ProgressSpeed = speed;
            ProgressInformation = information;

            ProgressChanged ();
        }


        void SyncUpBase ()
        {
            if (!UseCustomWatcher)
                this.watcher.Disable ();

            Logger.LogInfo ("SyncUp", Name + " | Initiated");
            HasUnsyncedChanges = true;

            Status = SyncStatus.SyncUp;
            SyncStatusChanged (Status);

            if (SyncUp ()) {
                Logger.LogInfo ("SyncUp", Name + " | Done");
                ChangeSets = GetChangeSets ();

                HasUnsyncedChanges = false;
                this.poll_interval = PollInterval.Long;

                this.listener.Announce (new Announcement (Identifier, CurrentRevision));

                Status = SyncStatus.Idle;
                SyncStatusChanged (Status);

            } else {
                Logger.LogInfo ("SyncUp", Name + " | Error");
                SyncDownBase ();

                if (!UseCustomWatcher)
                    this.watcher.Disable ();

                if (Error == ErrorStatus.None && SyncUp ()) {
                    HasUnsyncedChanges = false;

                    this.listener.Announce (new Announcement (Identifier, CurrentRevision));

                    Status = SyncStatus.Idle;
                    SyncStatusChanged (Status);

                } else {
                    this.poll_interval = PollInterval.Short;

                    Status = SyncStatus.Error;
                    SyncStatusChanged (Status);
                }
            }

            ProgressPercentage = 0.0;
            ProgressSpeed      = 0.0;

            if (!UseCustomWatcher)
                this.watcher.Enable ();

            this.status_message = "";
        }


        void SyncDownBase ()
        {
            if (!UseCustomWatcher)
                this.watcher.Disable ();

            Logger.LogInfo ("SyncDown", Name + " | Initiated");

            Status = SyncStatus.SyncDown;
            SyncStatusChanged (Status);

            string pre_sync_revision = CurrentRevision;

            if (SyncDown ()) {
                Error = ErrorStatus.None;

                string identifier_file_path = Path.Combine (LocalPath, ".sparkleshare");
                File.SetAttributes (identifier_file_path, FileAttributes.Hidden);

                ChangeSets = GetChangeSets ();

                if (!pre_sync_revision.Equals (CurrentRevision) &&
                    ChangeSets != null && ChangeSets.Count > 0 &&
                    !ChangeSets [0].User.Name.Equals (this.local_config.User.Name)) {

                    bool emit_change_event = true;

                    foreach (Change change in ChangeSets [0].Changes) {
                        if (change.Path.EndsWith (".sparkleshare")) {
                            emit_change_event = false;
                            break;
                        }
                    }
                    
                    if (emit_change_event)
                        NewChangeSet (ChangeSets [0]);
                }

                Logger.LogInfo ("SyncDown", Name + " | Done");

                // There could be changes from a resolved
                // conflict. Tries only once, then lets
                // the timer try again periodically
                if (HasUnsyncedChanges) {
                    Status = SyncStatus.SyncUp;
                    SyncStatusChanged (Status);
                    
                    if (SyncUp ())
                        HasUnsyncedChanges = false;
                }

                Status = SyncStatus.Idle;
                SyncStatusChanged (Status);

            } else {
                Logger.LogInfo ("SyncDown", Name + " | Error");

                ChangeSets = GetChangeSets ();

                Status = SyncStatus.Error;
                SyncStatusChanged (Status);
            }

            ProgressPercentage = 0.0;
            ProgressSpeed      = 0.0;

            Status = SyncStatus.Idle;
            SyncStatusChanged (Status);

            if (!UseCustomWatcher)
                this.watcher.Enable ();
        }


        void CreateListener ()
        {
            this.listener = ListenerFactory.CreateListener (Name, Identifier);

            if (this.listener.IsConnected)
                this.poll_interval = PollInterval.Long;

            this.listener.Connected            += ListenerConnectedDelegate;
            this.listener.Disconnected         += ListenerDisconnectedDelegate;
            this.listener.AnnouncementReceived += ListenerAnnouncementReceivedDelegate;

            if (!this.listener.IsConnected && !this.listener.IsConnecting)
                this.listener.Connect ();
        }

        
        void ListenerConnectedDelegate ()
        {
            if (this.last_disconnect_reason == DisconnectReason.SystemSleep) {
                this.last_disconnect_reason = DisconnectReason.None;

                if (HasRemoteChanges && !this.is_syncing)
                    SyncDownBase ();
            }

            this.poll_interval = PollInterval.Long;
        }


        void ListenerDisconnectedDelegate (DisconnectReason reason)
        {
            Logger.LogInfo (Name, "Falling back to regular polling");
            this.poll_interval = PollInterval.Short;

            this.last_disconnect_reason = reason;

            if (reason == DisconnectReason.SystemSleep) {
                this.remote_timer.Stop ();

                int backoff_time = 2;

                do {
                    Logger.LogInfo (Name, "Next reconnect attempt in " + backoff_time + " seconds");
                    Thread.Sleep (backoff_time * 1000);
                    this.listener.Connect ();
                    backoff_time *= 2;
                
                } while (backoff_time < 64 && !this.listener.IsConnected);

                this.remote_timer.Start ();
            }
        }


        void ListenerAnnouncementReceivedDelegate (Announcement announcement)
        {
            string identifier = Identifier;

            if (!announcement.FolderIdentifier.Equals (identifier))
                return;
                
            if (!announcement.Message.Equals (CurrentRevision)) {
                while (this.is_syncing)
                    Thread.Sleep (100);

                Logger.LogInfo (Name, "Syncing due to announcement");

                if (Status == SyncStatus.Paused)
                    Logger.LogInfo (Name, "We're paused, skipping sync");
                else
                    SyncDownBase ();
            }
        }


        // Recursively gets a folder's size in bytes
        long CalculateSize (DirectoryInfo parent)
        {
            if (ExcludePaths.Contains (parent.Name))
                return 0;

            long size = 0;

            try {
                foreach (DirectoryInfo directory in parent.GetDirectories ())
                    size += CalculateSize (directory);

                foreach (FileInfo file in parent.GetFiles ())
                    size += file.Length;

            } catch (Exception e) {
                Logger.LogInfo ("Local", "Error calculating directory size", e);
            }

            return size;
        }


        public void Pause ()
        {
            if (Status == SyncStatus.Idle) {
                this.local_config.SetFolderOptionalAttribute (Name, "paused", bool.TrueString);
                Status = SyncStatus.Paused;
            }
        }


        protected string status_message = "";

        public void Resume (string message)
        {
            this.status_message = message;

            if (Status == SyncStatus.Paused) {
                this.local_config.SetFolderOptionalAttribute (Name, "paused", bool.FalseString);
                Status = SyncStatus.Idle;

                if (HasUnsyncedChanges || HasLocalChanges) {
                    do {
                        SyncUpBase ();
                        
                    } while (HasLocalChanges);
                }
            }
        }


        public void Dispose ()
        {
            if (remote_timer != null) {
                this.remote_timer.Elapsed -= RemoteTimerElapsedDelegate;
                this.remote_timer.Stop ();
                this.remote_timer.Dispose ();
                this.remote_timer = null;
            }

            this.listener.Disconnected         -= ListenerDisconnectedDelegate;
            this.listener.AnnouncementReceived -= ListenerAnnouncementReceivedDelegate;

            this.listener.Dispose ();

            if (!UseCustomWatcher)
                this.watcher.Dispose ();
        }
    }
}
