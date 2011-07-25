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
using System.Timers;
using System.Xml;

namespace SparkleLib {

    public enum SyncStatus {
        Idle,
        SyncUp,
        SyncDown,
        Error
    }


    public abstract class SparkleRepoBase {

        private TimeSpan short_interval = new TimeSpan (0, 0, 3, 0);
        private TimeSpan long_interval  = new TimeSpan (0, 0, 10, 0);

        private SparkleWatcher watcher;
        private TimeSpan poll_interval;
        private Timer local_timer        = new Timer () { Interval = 0.25 * 1000 };
        private Timer remote_timer       = new Timer () { Interval = 10 * 1000 };
        private DateTime last_poll       = DateTime.Now;
        private List <double> sizebuffer = new List<double> ();
        private bool has_changed         = false;
        private Object change_lock       = new Object ();

        protected SparkleListenerBase listener;
        protected SyncStatus status;
        protected bool is_buffering  = false;
        protected bool server_online = true;

        public readonly SparkleBackend Backend;
        public readonly string LocalPath;
        public readonly string Name;

        public abstract bool AnyDifferences { get; }
        public abstract string Identifier { get; }
        public abstract string CurrentRevision { get; }
        public abstract bool SyncUp ();
        public abstract bool SyncDown ();
        public abstract bool HasUnsyncedChanges { get; set; }

        public delegate void SyncStatusChangedEventHandler (SyncStatus new_status);
        public event SyncStatusChangedEventHandler SyncStatusChanged;

        public delegate void NewChangeSetEventHandler (SparkleChangeSet change_set);
        public event NewChangeSetEventHandler NewChangeSet;

        public delegate void NewNoteEventHandler (string user_name, string user_email);
        public event NewNoteEventHandler NewNote;

        public delegate void ConflictResolvedEventHandler ();
        public event ConflictResolvedEventHandler ConflictResolved;

        public delegate void ChangesDetectedEventHandler ();
        public event ChangesDetectedEventHandler ChangesDetected;


        public SparkleRepoBase (string path, SparkleBackend backend)
        {
            LocalPath          = path;
            Name               = Path.GetFileName (LocalPath);
            Backend            = backend;
            this.poll_interval = this.short_interval;

            SyncStatusChanged += delegate (SyncStatus status) {
                this.status = status;
            };

            if (CurrentRevision == null)
                CreateInitialChangeSet ();

            CreateWatcher ();
            CreateListener ();

            this.local_timer.Elapsed += delegate (object o, ElapsedEventArgs args) {
                CheckForChanges ();
            };

            this.remote_timer.Elapsed += delegate {
                bool time_to_poll = (DateTime.Compare (this.last_poll,
                    DateTime.Now.Subtract (this.poll_interval)) < 0);

                if (time_to_poll) {
                    this.last_poll = DateTime.Now;

                    if (CheckForRemoteChanges ())
                        SyncDownBase ();
                }

                // In the unlikely case that we haven't synced up our
                // changes or the server was down, sync up again
                if (HasUnsyncedChanges)
                    SyncUpBase ();
            };

            // Sync up everything that changed
            // since we've been offline
            if (AnyDifferences) {
                DisableWatching ();
                SyncUpBase ();

                while (HasUnsyncedChanges)
                    SyncUpBase ();
                EnableWatching ();
            }

            this.remote_timer.Start ();
            this.local_timer.Start ();
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


        public virtual string [] UnsyncedFilePaths {
            get {
                return new string [0];
            }
        }


        public string Domain {
            get {
                Regex regex = new Regex (@"(@|://)([a-z0-9\.-]+)(/|:)");
                Match match = regex.Match (SparkleConfig.DefaultConfig.GetUrlForFolder (Name));

                if (match.Success)
                    return match.Groups [2].Value;
                else
                    return null;
            }
        }


        protected void OnConflictResolved ()
        {
            HasUnsyncedChanges = true;

            if (ConflictResolved != null)
                ConflictResolved ();
        }


        public virtual bool CheckForRemoteChanges () // HasRemoteChanges { get; } ?
        {
            return true;
        }


        public virtual List<SparkleChangeSet> GetChangeSets (int count) {
            return null;
        }


        public virtual bool UsesNotificationCenter {
            get {
                return true;
            }
        }


        public string RemoteName {
            get {
                string url = SparkleConfig.DefaultConfig.GetUrlForFolder (Name);
                return Path.GetFileNameWithoutExtension (url);
            }
        }


        public bool IsBuffering {
            get {
                return this.is_buffering;
            }
        }


        // Disposes all resourses of this object
        public void Dispose ()
        {
            this.remote_timer.Dispose ();
            this.local_timer.Dispose ();
            this.listener.Dispose ();
        }


        private void CreateWatcher ()
        {
            this.watcher = new SparkleWatcher (LocalPath);
            this.watcher.ChangeEvent += delegate (FileSystemEventArgs args) {
                OnFileActivity (args);
            };
        }


        public void CreateListener ()
        {
            this.listener = SparkleListenerFactory.CreateListener (Name, Identifier);

            // Stop polling when the connection to the irc channel is succesful
            this.listener.Connected += delegate {
                this.poll_interval = this.long_interval;
                this.last_poll = DateTime.Now;

                // Check for changes manually one more time
                if (CheckForRemoteChanges ())
                    SyncDownBase ();

                // Push changes that were made since the last disconnect
                if (HasUnsyncedChanges)
                    SyncUpBase ();
            };

            // Start polling when the connection to the irc channel is lost
            this.listener.Disconnected += delegate {
                this.poll_interval = this.short_interval;
                SparkleHelpers.DebugInfo (Name, "Falling back to polling");
            };

            // Fetch changes when there is a message in the irc channel
            this.listener.Announcement += delegate (SparkleAnnouncement announcement) {
                string identifier = Identifier;

                if (announcement.FolderIdentifier == identifier &&
                    !announcement.Message.Equals (CurrentRevision)) {
                    if ((Status != SyncStatus.SyncUp)   &&
                        (Status != SyncStatus.SyncDown) &&
                        !this.is_buffering) {

                        while (this.listener.HasQueueDownAnnouncement (identifier))
                            SyncDownBase ();
                    }
                }
            };
            
            // Start listening
            if (!this.listener.IsConnected && !this.listener.IsConnecting) {
                this.listener.Connect ();
            }
        }


        private void CheckForChanges ()
        {
            lock (this.change_lock) {
                if (this.has_changed) {
                    if (this.sizebuffer.Count >= 4)
                        this.sizebuffer.RemoveAt (0);
                        
                    DirectoryInfo dir_info = new DirectoryInfo (LocalPath);
                     this.sizebuffer.Add (CalculateFolderSize (dir_info));

                    if (this.sizebuffer.Count >= 4 &&
                        this.sizebuffer [0].Equals (this.sizebuffer [1]) &&
                        this.sizebuffer [1].Equals (this.sizebuffer [2]) &&
                        this.sizebuffer [2].Equals (this.sizebuffer [3])) {

                        SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled.");
                        this.is_buffering = false;
                        this.has_changed  = false;
                        
                        DisableWatching ();
                        while (AnyDifferences)
                            SyncUpBase ();
                        EnableWatching ();
                    }
                }
            }
        }


        // Starts a timer when something changes
        public void OnFileActivity (FileSystemEventArgs args)
        {
            // Check the watcher for the occasions where this
            // method is called directly
            if (!this.watcher.EnableRaisingEvents)
                return;

            if (args.FullPath.Contains (Path.DirectorySeparatorChar + ".") &&
                !args.FullPath.Contains (Path.DirectorySeparatorChar + ".notes"))
                return;

            WatcherChangeTypes wct = args.ChangeType;

            if (AnyDifferences) {
                this.is_buffering = true;

                // We want to disable wathcing temporarily, but
                // not stop the local timer
                this.watcher.EnableRaisingEvents = false;

                // Only fire the event if the timer has been stopped.
                // This prevents multiple events from being raised whilst "buffering".
                if (!this.has_changed) {
                    if (ChangesDetected != null)
                        ChangesDetected ();
                }

                SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + args.Name + "'");
                SparkleHelpers.DebugInfo ("Event", "[" + Name + "] Changes found, checking if settled.");

                this.remote_timer.Stop ();

                lock (this.change_lock) {
                    this.has_changed = true;
                }
            }
        }


        public List<SparkleNote> GetNotes (string revision) {
            List<SparkleNote> notes = new List<SparkleNote> ();

            string notes_path = Path.Combine (LocalPath, ".notes");

            if (!Directory.Exists (notes_path))
                Directory.CreateDirectory (notes_path);

            Regex regex_notes = new Regex (@"<name>(.+)</name>.*" +
                                "<email>(.+)</email>.*" +
                                "<timestamp>([0-9]+)</timestamp>.*" +
                                "<body>(.+)</body>", RegexOptions.Compiled);

            foreach (string file_path in Directory.GetFiles (notes_path)) {
                if (Path.GetFileName (file_path).StartsWith (revision)) {
                    string note_xml = String.Join ("", File.ReadAllLines (file_path));

                    Match match_notes = regex_notes.Match (note_xml);

                    if (match_notes.Success) {
                        SparkleNote note = new SparkleNote () {
                            User = new SparkleUser (match_notes.Groups [1].Value,
                                match_notes.Groups [2].Value),
                            Timestamp = new DateTime (1970, 1, 1).AddSeconds (int.Parse (match_notes.Groups [3].Value)),
                            Body      = match_notes.Groups [4].Value
                        };

                        notes.Add (note);
                    }
                }
            }

            return notes;
        }


        private void SyncUpBase ()
        {
            try {
                DisableWatching ();
                this.remote_timer.Stop ();

                SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Initiated");

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.SyncUp);

                if (SyncUp ()) {
                    SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Done");

                    HasUnsyncedChanges = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle);

                    this.listener.AnnounceBase (new SparkleAnnouncement (Identifier, CurrentRevision));

                } else {
                    SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Error");

                    HasUnsyncedChanges = true;
                    SyncDownBase ();

                    if (SyncUp ()) {
                        HasUnsyncedChanges = false;

                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Idle);

                        this.listener.AnnounceBase (new SparkleAnnouncement (Identifier, CurrentRevision));

                    } else {
                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Error);
                    }
                }

            } finally {
                this.remote_timer.Start ();
                EnableWatching ();
            }
        }


        private void SyncDownBase ()
        {
            SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Initiated");
            this.remote_timer.Stop ();
            DisableWatching ();

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncDown);

            if (SyncDown ()) {
                SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Done");
                this.server_online = true;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Idle);

                SparkleChangeSet change_set = GetChangeSets (1) [0];

                bool note_added = false;
                foreach (string added in change_set.Added) {
                    if (added.Contains (".notes")) {
                        if (NewNote != null)
                            NewNote (change_set.User.Name, change_set.User.Email);

                        note_added = true;
                        break;
                    }
                }

                if (!note_added) {
                    if (NewChangeSet != null)
                        NewChangeSet (change_set);
                }

                // There could be changes from a resolved
                // conflict. Tries only once, then lets
                // the timer try again periodically
                if (HasUnsyncedChanges)
                    SyncUp ();

            } else {
                SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Error");
                this.server_online = false;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Error);
            }

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.Idle);

            this.remote_timer.Start ();
            EnableWatching ();
        }


        public void DisableWatching ()
        {
            this.watcher.EnableRaisingEvents = false;
            this.local_timer.Stop ();
        }


        public void EnableWatching ()
        {
            this.watcher.EnableRaisingEvents = true;
            this.local_timer.Start ();
        }


        // Create an initial change set when the
        // user has fetched an empty remote folder
        public virtual void CreateInitialChangeSet ()
        {
            string file_path = Path.Combine (LocalPath, "SparkleShare.txt");
            TextWriter writer = new StreamWriter (file_path);
            writer.WriteLine (":)");
            writer.Close ();
        }


        public void AddNote (string revision, string note)
        {
            string notes_path = Path.Combine (LocalPath, ".notes");

            if (!Directory.Exists (notes_path))
                Directory.CreateDirectory (notes_path);

            // Add a timestamp in seconds since unix epoch
            int timestamp = (int) (DateTime.UtcNow - new DateTime (1970, 1, 1)).TotalSeconds;

            string n = Environment.NewLine;
            note     = "<note>" + n +
                       "  <user>" +  n +
                       "    <name>" + SparkleConfig.DefaultConfig.User.Name + "</name>" + n +
                       "    <email>" + SparkleConfig.DefaultConfig.User.Email + "</email>" + n +
                       "  </user>" + n +
                       "  <timestamp>" + timestamp + "</timestamp>" + n +
                       "  <body>" + note + "</body>" + n +
                       "</note>" + n;

            string note_name = revision + SHA1 (timestamp.ToString () + note);
            string note_path = Path.Combine (notes_path, note_name);

            StreamWriter writer = new StreamWriter (note_path);
            writer.Write (note);
            writer.Close ();


            // The watcher doesn't like .*/ so we need to trigger
            // a change manually
            FileSystemEventArgs args = new FileSystemEventArgs (WatcherChangeTypes.Changed,
                notes_path, note_name);

            OnFileActivity (args);
            SparkleHelpers.DebugInfo ("Note", "Added note to " + revision);
        }


        // Recursively gets a folder's size in bytes
        private double CalculateFolderSize (DirectoryInfo parent)
        {
            if (!System.IO.Directory.Exists (parent.ToString ()))
                return 0;

            double size = 0;

            // Ignore the temporary 'rebase-apply' directory. This prevents potential
            // crashes when files are being queried whilst the files have already been deleted.
            if (parent.Name.Equals ("rebase-apply"))
                return 0;

            foreach (FileInfo file in parent.GetFiles ()) {
                if (!file.Exists)
                    return 0;

                size += file.Length;
            }

            foreach (DirectoryInfo directory in parent.GetDirectories())
                size += CalculateFolderSize (directory);

            return size;
        }


        // Creates a SHA-1 hash of input
        private string SHA1 (string s)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = sha1.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
        }
    }
}
