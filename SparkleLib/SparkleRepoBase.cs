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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

namespace SparkleLib {

    public enum SyncStatus {
        Idle,
        SyncUp,
        SyncDown,
        Error
    }


    public abstract class SparkleRepoBase {
        
        private string identifier;
        private TimeSpan short_interval = new TimeSpan (0, 0, 5, 0);
        private TimeSpan long_interval  = new TimeSpan (0, 0, 15, 0);
        private TimeSpan poll_interval;
        private SparkleWatcher watcher;
        private SparkleListenerBase listener;
        private System.Timers.Timer remote_timer = new System.Timers.Timer () { Interval = 5 * 1000 };
        private DateTime last_poll               = DateTime.Now;
        private Object change_lock               = new Object ();
        private double progress_percentage       = 0.0;
        private string progress_speed            = "";
        private bool is_buffering                = false;
        private bool server_online               = true;
        private SyncStatus status;


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

        public abstract string ComputeIdentifier ();
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

        public List<SparkleChangeSet> GetChangeSets () {
            return GetChangeSets (30);
        }

        public bool ServerOnline {
            get {
                return this.server_online;
            }
        }

        public SyncStatus Status {
            get {
                return this.status;
            }
        }

        public double ProgressPercentage {
            get {
                return this.progress_percentage;
            }
        }

        public string ProgressSpeed {
            get {
                return this.progress_speed;
            }
        }

        public virtual string [] UnsyncedFilePaths {
            get {
                return new string [0];
            }
        }

        public bool IsSyncing {
            get {
                return (Status == SyncStatus.SyncUp   ||
                        Status == SyncStatus.SyncDown ||
                        this.is_buffering);
            }
        }

        public bool IsBuffering {
            get {
                return this.is_buffering;
            }
        }
        
        public string Identifier {
            get {
                if (this.identifier != null)
                    return this.identifier;

                string id_path = Path.Combine (LocalPath, ".sparkleshare");
                
                if (File.Exists (id_path))
                    this.identifier = File.ReadAllText (id_path).Trim ();

                if (this.identifier != null && this.identifier.Length > 0) {
                    return this.identifier;

                } else {
                    this.identifier = ComputeIdentifier ();
                    File.WriteAllText (id_path, this.identifier);
                    File.SetAttributes (id_path, FileAttributes.Hidden);

                    return this.identifier;
                }
            }
        }


        public SparkleRepoBase (string path)
        {
            LocalPath = path;
            Name      = Path.GetFileName (LocalPath);
            RemoteUrl = new Uri (SparkleConfig.DefaultConfig.GetUrlForFolder (Name));

            this.poll_interval = this.short_interval;

            SyncStatusChanged += delegate (SyncStatus status) {
                this.status = status;
            };

            this.identifier = Identifier;

            if (CurrentRevision == null)
                CreateInitialChangeSet ();

            ChangeSets = GetChangeSets ();
            this.watcher = CreateWatcher ();

            new Thread (
                new ThreadStart (delegate {
                    CreateListener ();
                })
            ).Start ();

            this.remote_timer.Elapsed += delegate {
                bool time_to_poll = (DateTime.Compare (this.last_poll,
                    DateTime.Now.Subtract (this.poll_interval)) < 0);

                if (time_to_poll) {
                    this.last_poll = DateTime.Now;

                    if (HasRemoteChanges)
                        SyncDownBase ();
                }

                // In the unlikely case that we haven't synced up our
                // changes or the server was down, sync up again
                if (HasUnsyncedChanges && !IsSyncing && this.server_online)
                    SyncUpBase ();
            };
        }


        public void Initialize ()
        {
            // Sync up everything that changed
            // since we've been offline
            if (HasLocalChanges) {
                this.watcher.Disable ();
                SyncUpBase ();

                while (HasUnsyncedChanges)
                    SyncUpBase ();

                this.watcher.Enable ();
            }

            this.remote_timer.Start ();
        }


        protected void OnConflictResolved ()
        {
            if (ConflictResolved != null)
                ConflictResolved ();
        }


        // Disposes all resourses of this object
        public void Dispose ()
        {
            this.remote_timer.Dispose ();
            this.listener.Dispose ();
        }


        private SparkleWatcher CreateWatcher ()
        {
            SparkleWatcher watcher = new SparkleWatcher (LocalPath);

            watcher.ChangeEvent += delegate (FileSystemEventArgs args) {
                OnFileActivity (args);
            };

            return watcher;
        }


        // Starts a timer when something changes
        public void OnFileActivity (FileSystemEventArgs args)
        {
            // Check the watcher for the occasions where this
            // method is called directly
            if (!this.watcher.EnableRaisingEvents || this.is_buffering)
                return;

            lock (this.change_lock) {
                string relative_path = args.FullPath.Replace (LocalPath, "");

                foreach (string exclude_path in ExcludePaths) {
                    if (relative_path.Contains (exclude_path))
                        return;
                }

                if (!this.is_buffering && HasLocalChanges) {
                    this.is_buffering = true;
                    this.watcher.Disable ();
                    this.remote_timer.Stop ();

                    SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Activity detected, waiting for it to settle...");

                    if (ChangesDetected != null)
                        ChangesDetected ();

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

                            SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Activity has settled");
                            this.is_buffering = false;

                            this.watcher.Disable ();
                            while (HasLocalChanges)
                                SyncUpBase ();
                            this.watcher.Enable ();

                        } else {
                            Thread.Sleep (500);
                        }

                    } while (this.is_buffering);
                }
            }
        }


        private void SyncUpBase ()
        {
            try {
                this.watcher.Disable ();
                this.remote_timer.Stop ();

                SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Initiated");

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.SyncUp);

                if (SyncUp ()) {
                    SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Done");

                    HasUnsyncedChanges = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle);

                    this.listener.Announce (new SparkleAnnouncement (Identifier, CurrentRevision));

                } else {
                    SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Error");

                    HasUnsyncedChanges = true;
                    SyncDownBase ();
                    this.watcher.Disable ();

                    if (this.server_online && SyncUp ()) {
                        HasUnsyncedChanges = false;

                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Idle);

                        this.listener.Announce (new SparkleAnnouncement (Identifier, CurrentRevision));

                    } else {
                        this.server_online = false;

                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Error);
                    }
                }

            } finally {
                this.remote_timer.Start ();
                this.watcher.Enable ();

                this.progress_percentage = 0.0;
                this.progress_speed      = "";
            }
        }


        private void SyncDownBase ()
        {
            SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Initiated");
            this.remote_timer.Stop ();
            this.watcher.Disable ();

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncDown);

            string pre_sync_revision = CurrentRevision;

            if (SyncDown ()) {
                SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Done");
                this.server_online = true;

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
                SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Error");
                this.server_online = false;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Error);
            }

            this.progress_percentage = 0.0;
            this.progress_speed      = "";

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.Idle);

            this.remote_timer.Start ();
            this.watcher.Enable ();
        }


        private void CreateListener ()
        {
            this.listener = SparkleListenerFactory.CreateListener (Name, Identifier);

            if (this.listener.IsConnected) {
                this.poll_interval = this.long_interval;

                new Thread (
                    new ThreadStart (delegate {
                        if (!IsSyncing && HasRemoteChanges)
                            SyncDownBase ();
                    })
                ).Start ();
            }

            // Stop polling when the connection to the irc channel is succesful
            this.listener.Connected += delegate {
                this.poll_interval = this.long_interval;
                this.last_poll = DateTime.Now;

                if (!IsSyncing) {

                    // Check for changes manually one more time
                    if (HasRemoteChanges)
                        SyncDownBase ();

                    // Push changes that were made since the last disconnect
                    if (HasUnsyncedChanges)
                        SyncUpBase ();
                }
            };

            // Start polling when the connection to the channel is lost
            this.listener.Disconnected += delegate {
                this.poll_interval = this.short_interval;
                SparkleHelpers.DebugInfo (Name, "Falling back to polling");
            };

            // Fetch changes when there is a message in the irc channel
            this.listener.Received += delegate (SparkleAnnouncement announcement) {
                string identifier = Identifier;

                if (announcement.FolderIdentifier.Equals (identifier) &&
                    !announcement.Message.Equals (CurrentRevision)) {

                    while (this.IsSyncing)
                        System.Threading.Thread.Sleep (100);

                    SparkleHelpers.DebugInfo ("Listener", "Syncing due to announcement");
                    SyncDownBase ();

                } else {
                    if (announcement.FolderIdentifier.Equals (identifier))
                        SparkleHelpers.DebugInfo ("Listener", "Not syncing, message is for current revision");
                }
            };

            // Start listening
            if (!this.listener.IsConnected && !this.listener.IsConnecting)
                this.listener.Connect ();
        }


        private DateTime progress_last_change     = DateTime.Now;
        private TimeSpan progress_change_interval = new TimeSpan (0, 0, 0, 1);

        protected void OnProgressChanged (double progress_percentage, string progress_speed)
        {
            if (DateTime.Compare (this.progress_last_change,
                    DateTime.Now.Subtract (this.progress_change_interval)) < 0) {

                if (ProgressChanged != null) {
                    if (progress_percentage == 100.0)
                        progress_percentage = 99.0;

                    this.progress_percentage  = progress_percentage;
                    this.progress_speed       = progress_speed;
                    this.progress_last_change = DateTime.Now;

                    ProgressChanged (progress_percentage, progress_speed);
                }
            }
        }


        // Create an initial change set when the
        // user has fetched an empty remote folder
        public virtual void CreateInitialChangeSet ()
        {
            string file_path = Path.Combine (LocalPath, "SparkleShare.txt");
            string n         = Environment.NewLine;

            File.WriteAllText (file_path,
                "Congratulations, you've successfully created a SparkleShare repository!" + n +
                "" + n +
                "Any files you add or change in this folder will be automatically synced to " + n +
                RemoteUrl + " and everyone connected to it." + n +
                "" + n +
                "SparkleShare is a Free and Open Source software program that helps people " + n +
                "collaborate and share files. If you like what we do, please consider a small " + n +
                "donation to support the project: http://sparkleshare.org/support-us/" + n +
                "" + n +
                "Have fun! :)" + n
            );
        }


        // Creates a SHA-1 hash of input
        private string SHA1 (string s)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = sha1.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
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
    }
}
