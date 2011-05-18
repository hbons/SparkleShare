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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

using Meebey.SmartIrc4net;
using Mono.Unix;

namespace SparkleLib {

    public enum SyncStatus {
        Idle,
        SyncUp,
        SyncDown,
        Error
    }


    public class SparkleRepo {

        private Timer remote_timer;
        private Timer local_timer;
        private FileSystemWatcher watcher;
        private System.Object change_lock;
        private SparkleListenerBase listener;
        private List <double> sizebuffer;
        private bool has_changed;

        private string revision;
        private bool is_buffering;
        private bool is_polling;
        private bool server_online;
        private SyncStatus status;

        public readonly SparkleBackend Backend;
        public readonly string Name;
        public readonly string RemoteName;
        public readonly string Domain;
        public readonly string Description;
        public readonly string LocalPath;
        public readonly string Url;
        public readonly string UserName;
        public readonly string UserEmail;

        public string Revision {
            get {
                return this.revision;
            }
        }

        public bool IsBuffering {
            get {
                return this.is_buffering;
            }
        }

        public bool IsPolling {
            get {
                return this.is_polling;
            }
        }

        public bool HasUnsyncedChanges {
            get {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".git", "has_unsynced_changes");

                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".git", "has_unsynced_changes");

                if (value) {
                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path);
                } else {
                    File.Delete (unsynced_file_path);
                }
            }
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

        public delegate void SyncStatusChangedEventHandler (SyncStatus new_status);
        public event SyncStatusChangedEventHandler SyncStatusChanged;


        public delegate void NewChangeSetEventHandler (SparkleChangeSet change_set, string source_path);
        public delegate void ConflictResolvedEventHandler ();
        public delegate void ChangesDetectedEventHandler ();

        public event NewChangeSetEventHandler NewChangeSet;
        public event ConflictResolvedEventHandler ConflictResolved;
        public event ChangesDetectedEventHandler ChangesDetected;

        public SparkleRepo (string path, SparkleBackend backend)
        {
            LocalPath       = path;
            Name            = Path.GetFileName (LocalPath);
            Url = GetUrl ();
            RemoteName      = Path.GetFileNameWithoutExtension (Url);
            Domain          = GetDomain (Url);
            Description     = GetDescription ();
            UserName        = GetUserName ();
            UserEmail       = GetUserEmail ();
            Backend         = backend;

            this.is_buffering   = false;
            this.is_polling     = true;
            this.server_online  = true;
            this.has_changed    = false;
            this.change_lock    = new Object ();

            SyncStatusChanged += delegate (SyncStatus status) {
                this.status = status;
            };

            if (IsEmpty)
                this.revision = null;
            else
                this.revision = GetRevision ();

            if (this.revision == null)
                CreateInitialCommit ();

            // Watch the repository's folder
            this.watcher = new FileSystemWatcher (LocalPath) {
                IncludeSubdirectories = true,
                EnableRaisingEvents   = true,
                Filter                = "*"
            };

            this.watcher.Changed += new FileSystemEventHandler (OnFileActivity);
            this.watcher.Created += new FileSystemEventHandler (OnFileActivity);
            this.watcher.Deleted += new FileSystemEventHandler (OnFileActivity);
            this.watcher.Renamed += new RenamedEventHandler (OnFileActivity);

            NotificationServerType server_type;
            if (UsesNotificationCenter)
                server_type = NotificationServerType.Central;
            else
                server_type = NotificationServerType.Own;

            this.listener = new SparkleListenerIrc (Domain, Identifier, server_type);

            // ...fetch remote changes every 60 seconds if that fails
            this.remote_timer = new Timer () {
                Interval = 60000
            };

            this.remote_timer.Elapsed += delegate {
                if (this.is_polling) {
                    CheckForRemoteChanges ();
                    
                    if (!this.listener.IsConnected)
                        this.listener.Connect ();
                }

                if (HasUnsyncedChanges)
                    FetchRebaseAndPush ();
            };

            // Stop polling when the connection to the irc channel is succesful
            this.listener.Connected += delegate {
                this.is_polling = false;

                // Check for changes manually one more time
                CheckForRemoteChanges ();

                // Push changes that were made since the last disconnect
                if (HasUnsyncedChanges)
                    Push ();
            };
            
            // Start polling when the connection to the irc channel is lost
            this.listener.Disconnected += delegate {
                SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Falling back to polling");
                this.is_polling = true;
            };

            // Fetch changes when there is a message in the irc channel
            this.listener.RemoteChange += delegate (string change_id) {
                if (!change_id.Equals (this.revision) && change_id.Length == 40) {
                    if (Status != SyncStatus.SyncUp   &&
                        Status != SyncStatus.SyncDown &&
                        !this.is_buffering) {

                        while (this.listener.ChangesQueue > 0) {
                            Fetch ();
                            this.listener.DecrementChangesQueue ();
                        }

                        this.watcher.EnableRaisingEvents = false;
                        Rebase ();
                        this.watcher.EnableRaisingEvents = true;
                    }
                }
            };

            // Start listening
            this.listener.Connect ();

            this.sizebuffer = new List <double> ();

            // Keep a timer that checks if there are changes and
            // whether they have settled
            this.local_timer = new Timer () {
                Interval = 250
            };

            this.local_timer.Elapsed += delegate (object o, ElapsedEventArgs args) {
                CheckForChanges ();
            };

            this.remote_timer.Start ();
            this.local_timer.Start ();

            // Add everything that changed 
            // since SparkleShare was stopped
            AddCommitAndPush ();

            if (this.revision == null)
                this.revision = GetRevision ();
        }


        public string Identifier {
            get {

                // Because git computes a hash based on content,
                // author, and timestamp; it is unique enough to
                // use the hash of the first commit as an identifier
                // for our folder
                SparkleGit git = new SparkleGit (LocalPath, "rev-list --reverse HEAD");
                git.Start ();

                // Reading the standard output HAS to go before
                // WaitForExit, or it will hang forever on output > 4096 bytes
                string output = git.StandardOutput.ReadToEnd ();
                git.WaitForExit ();

                return output.Substring (0, 40);
            }
        }


        private void CheckForRemoteChanges ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Checking for remote changes...");
            SparkleGit git = new SparkleGit (LocalPath, "ls-remote origin master");
        
            git.Exited += delegate {
                if (git.ExitCode != 0)
                    return;

                string remote_revision = git.StandardOutput.ReadToEnd ().TrimEnd ();

                if (!remote_revision.StartsWith (this.revision)) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found. (" + remote_revision + ")");
                    Fetch ();
                    
                    this.watcher.EnableRaisingEvents = false;
                    Rebase ();
                    this.watcher.EnableRaisingEvents = true;
                }
            };

            git.Start ();
            git.WaitForExit ();
        }


        private void CheckForChanges ()
        {
            lock (this.change_lock) {
                if (this.has_changed) {
                    if ( this.sizebuffer.Count >= 4)
                         this.sizebuffer.RemoveAt (0);
                        
                    DirectoryInfo dir_info = new DirectoryInfo (LocalPath);
                     this.sizebuffer.Add (CalculateFolderSize (dir_info));

                    if ( this.sizebuffer [0].Equals (this.sizebuffer [1]) &&
                         this.sizebuffer [1].Equals (this.sizebuffer [2]) &&
                         this.sizebuffer [2].Equals (this.sizebuffer [3])) {

                        SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled.");
                        this.is_buffering = false;
                        this.has_changed  = false;
                        
                        while (AnyDifferences) {
                            this.watcher.EnableRaisingEvents = false;
                            AddCommitAndPush ();
                            this.watcher.EnableRaisingEvents = true;
                        }
                    }
                }
            }
        }


        // Starts a timer when something changes
        private void OnFileActivity (object o, FileSystemEventArgs fse_args)
        {
            if (fse_args.Name.StartsWith (".git/"))
                return;

            WatcherChangeTypes wct = fse_args.ChangeType;

            if (AnyDifferences) {
                this.is_buffering = true;

                // Only fire the event if the timer has been stopped.
                // This prevents multiple events from being raised whilst "buffering".
                if (!this.has_changed) {
                    if (ChangesDetected != null)
                        ChangesDetected ();
                }

                SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + fse_args.Name + "'");
                SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes found, checking if settled.");
                
                this.remote_timer.Stop ();

                lock (this.change_lock) {
                    this.has_changed = true;
                }
            }
        }


        // When there are changes we generally want to Add, Commit and Push,
        // so this method does them all with appropriate timers, etc. switched off
        public void AddCommitAndPush ()
        {
            try {
                this.local_timer.Stop ();
                this.remote_timer.Stop ();
    
                if (AnyDifferences) {
                    Add ();

                    string message = FormatCommitMessage ();
                    Commit (message);

                    Push ();
                } else {
                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle); // TODO: in checklocalforchanges
                }
            } finally {
                this.remote_timer.Start ();
                this.local_timer.Start ();
            }
        }


        public void FetchRebaseAndPush ()
        {
            CheckForRemoteChanges ();
            Push ();
        }

        
        private bool AnyDifferences {
            get {
                SparkleGit git = new SparkleGit (LocalPath, "status --porcelain");
                git.Start ();
                git.WaitForExit ();

                string output = git.StandardOutput.ReadToEnd ().TrimEnd ();
                string [] lines = output.Split ("\n".ToCharArray ());

                foreach (string line in lines) {
                    if (line.Length > 1 && !line [1].Equals (" "))
                        return true;
                }

                return false;
            }
        }


        private bool IsEmpty {
            get {
                SparkleGit git = new SparkleGit (LocalPath, "log -1");
                git.Start ();
                git.WaitForExit ();

                return (git.ExitCode != 0);
            }
        }


        private string GetRevision ()
        {
            // Remove stale rebase-apply files because it
            // makes the method return the wrong hashes.
            string rebase_apply_file = SparkleHelpers.CombineMore (LocalPath, ".git", "rebase-apply");
            if (File.Exists (rebase_apply_file))
                File.Delete (rebase_apply_file);

            SparkleGit git = new SparkleGit (LocalPath, "log -1 --format=%H");
            git.Start ();
            git.WaitForExit ();

            string output   = git.StandardOutput.ReadToEnd ();
            string revision = output.Trim ();

            return revision;
        }


        // Stages the made changes
        private void Add ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "add --all");
            git.Start ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes staged");
        }


        // Removes unneeded objects
        private void CollectGarbage ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "gc");
            git.Start ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Garbage collected.");
        }


        // Commits the made changes
        public void Commit (string message)
        {
            if (!AnyDifferences)
                return;

            SparkleGit git = new SparkleGit (LocalPath, "commit -m '" + message + "'");
            git.Start ();
            git.WaitForExit ();

            this.revision = GetRevision ();
            SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message + " (" + this.revision + ")");

            // Collect garbage pseudo-randomly
            if (DateTime.Now.Second % 10 == 0)
                CollectGarbage ();
        }


        // Fetches changes from the remote repository
        public void Fetch ()
        {
            this.remote_timer.Stop ();

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Fetching changes");
            SparkleGit git = new SparkleGit (LocalPath, "fetch -v origin master");

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncDown);

            git.Exited += delegate {

                this.revision = GetRevision ();

                if (git.ExitCode != 0) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes not fetched");
                    this.server_online = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Error);
                } else {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes fetched");
                    this.server_online = true;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle);
                }

                this.remote_timer.Start ();
            };

            git.Start ();
            git.WaitForExit ();
        }


        // Merges the fetched changes
        public void Rebase ()
        {
            if (AnyDifferences) {
                Add ();
                
                string commit_message = FormatCommitMessage ();
                Commit (commit_message);
            }

            SparkleGit git = new SparkleGit (LocalPath, "rebase -v FETCH_HEAD");

            git.Exited += delegate {
                if (git.ExitCode != 0) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict detected. Trying to get out...");
                    DisableWatching ();

                    while (AnyDifferences)
                        ResolveConflict ();

                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict resolved.");
                    EnableWatching ();

                    if (ConflictResolved != null)
                        ConflictResolved ();

                    Push ();
                }

                this.revision = GetRevision ();
            };

            git.Start ();
            git.WaitForExit ();

            this.revision = GetRevision ();

            if (NewChangeSet != null)
                NewChangeSet (GetChangeSets (1) [0], LocalPath);
        }


        private void ResolveConflict ()
        {
            // This is al list of conflict status codes that Git uses, their
            // meaning, and how SparkleShare should handle them.
            //
            // DD    unmerged, both deleted    -> Do nothing
            // AU    unmerged, added by us     -> Use theirs, save ours as a timestamped copy
            // UD    unmerged, deleted by them -> Use ours
            // UA    unmerged, added by them   -> Use theirs, save ours as a timestamped copy
            // DU    unmerged, deleted by us   -> Use theirs
            // AA    unmerged, both added      -> Use theirs, save ours as a timestamped copy
            // UU    unmerged, both modified   -> Use theirs, save ours as a timestamped copy
            //
            // Note that a rebase merge works by replaying each commit from the working branch on
            // top of the upstream branch. Because of this, when a merge conflict happens the
            // side reported as 'ours' is the so-far rebased series, starting with upstream,
            // and 'theirs' is the working branch. In other words, the sides are swapped.
            //
            // So: 'ours' means the 'server's version' and 'theirs' means the 'local version'

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            git_status.Start ();
            git_status.WaitForExit ();

            string output   = git_status.StandardOutput.ReadToEnd ().TrimEnd ();
            string [] lines = output.Split ("\n".ToCharArray ());

            foreach (string line in lines) {
                string conflicting_path = line.Substring (3);
                conflicting_path = conflicting_path.Trim ("\"".ToCharArray ());

                SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict type: " + line);

                // Both the local and server version have been modified
                if (line.StartsWith ("UU") || line.StartsWith ("AA") ||
                    line.StartsWith ("AU") || line.StartsWith ("UA")) {

                    // Recover local version
                    SparkleGit git_theirs = new SparkleGit (LocalPath,
                        "checkout --theirs \"" + conflicting_path + "\"");
                    git_theirs.Start ();
                    git_theirs.WaitForExit ();

                    // Append a timestamp to local version
                    string timestamp            = DateTime.Now.ToString ("HH:mm MMM d");
                    string their_path           = conflicting_path + " (" + UserName  + ", " + timestamp + ")";
                    string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
                    string abs_their_path       = Path.Combine (LocalPath, their_path);

                    File.Move (abs_conflicting_path, abs_their_path);

                    // Recover server version
                    SparkleGit git_ours = new SparkleGit (LocalPath,
                        "checkout --ours \"" + conflicting_path + "\"");
                    git_ours.Start ();
                    git_ours.WaitForExit ();

                    Add ();

                    SparkleGit git_rebase_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_rebase_continue.Start ();
                    git_rebase_continue.WaitForExit ();
                }

                // The local version has been modified, but the server version was removed
                if (line.StartsWith ("DU")) {

                    // The modified local version is already in the
                    // checkout, so it just needs to be added.
                    //
                    // We need to specifically mention the file, so
                    // we can't reuse the Add () method
                    SparkleGit git_add = new SparkleGit (LocalPath,
                        "add " + conflicting_path);
                    git_add.Start ();
                    git_add.WaitForExit ();

                    SparkleGit git_rebase_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_rebase_continue.Start ();
                    git_rebase_continue.WaitForExit ();
                }

                // The server version has been modified, but the local version was removed
                if (line.StartsWith ("UD")) {

                    // We can just skip here, the server version is
                    // already in the checkout
                    SparkleGit git_rebase_skip = new SparkleGit (LocalPath, "rebase --skip");
                    git_rebase_skip.Start ();
                    git_rebase_skip.WaitForExit ();
                }
            }
        }


        // Pushes the changes to the remote repo
        public void Push ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing changes");
            SparkleGit git = new SparkleGit (LocalPath, "push origin master");

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncUp);

            git.Exited += delegate {
                if (git.ExitCode != 0) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes not pushed");

                    HasUnsyncedChanges = true;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Error);

                    FetchRebaseAndPush ();
                } else {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes pushed");

                    HasUnsyncedChanges = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle);

                    this.listener.Announce (this.revision);
                }

            };

            git.Start ();
            git.WaitForExit ();

            // TODO put exit events here instead of in a new Exited thread, for the oter methods too
        }


        public void DisableWatching ()
        {
            this.watcher.EnableRaisingEvents = false;
        }


        public void EnableWatching ()
        {
            this.watcher.EnableRaisingEvents = true;
        }


        // Gets the domain name of a given URL
        // TODO: make this a regex
        private string GetDomain (string url)
        {
            if (url.Equals (""))
                return null;

            string domain = url.Substring (url.IndexOf ("@") + 1);

            if (domain.Contains (":"))
                domain = domain.Substring (0, domain.IndexOf (":"));
            else
                domain = domain.Substring (0, domain.IndexOf ("/"));

            return domain;
        }


        // Gets the repository's description
        private string GetDescription ()
        {
            string description_file_path = SparkleHelpers.CombineMore (LocalPath, ".git", "description");

            if (!File.Exists (description_file_path))
                return null;

            StreamReader reader = new StreamReader (description_file_path);
            string description = reader.ReadToEnd ();
            reader.Close ();

            if (description.StartsWith ("Unnamed"))
                description = null;

            return description;
        }


        private string GetUrl ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "config --get remote.origin.url");
            git.Start ();
            git.WaitForExit ();

            string output = git.StandardOutput.ReadToEnd ();
            string url    = output.Trim ();

            return url;
        }


        private string GetUserName ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "config --get user.name");
            git.Start ();
            git.WaitForExit ();

            string output = git.StandardOutput.ReadToEnd ();
            string user_name   = output.Trim ();

            return user_name;
        }


        private string GetUserEmail ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "config --get user.email");
            git.Start ();
            git.WaitForExit ();

            string output = git.StandardOutput.ReadToEnd ();
            string user_email   = output.Trim ();

            return user_email;
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

            foreach (FileInfo file in parent.GetFiles()) {
                if (!file.Exists)
                    return 0;

                size += file.Length;
            }

            foreach (DirectoryInfo directory in parent.GetDirectories())
                size += CalculateFolderSize (directory);

            return size;
        }


        // Create a first commit in case the user has cloned
        // an empty repository
        private void CreateInitialCommit ()
        {
            TextWriter writer = new StreamWriter (Path.Combine (LocalPath, "SparkleShare.txt"));
            writer.WriteLine (":)");
            writer.Close ();
        }


        // Returns a list of latest commits
        // TODO: Method needs to be made a lot faster
        public List <SparkleChangeSet> GetChangeSets (int count)
        {
            if (count < 1)
                count = 30;
            
            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();

            SparkleGit git_log = new SparkleGit (LocalPath, "log -" + count + " --raw -M --date=iso");
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            git_log.Start ();
            
            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_log.StandardOutput.ReadToEnd ();
            git_log.WaitForExit ();

            string [] lines       = output.Split ("\n".ToCharArray ());
            List <string> entries = new List <string> ();

            int j = 0;
            string entry = "", last_entry = "";
            foreach (string line in lines) {
                if (line.StartsWith ("commit") && j > 0) {
                    entries.Add (entry);
                    entry = "";
                } 
                
                entry += line + "\n";
                j++;
                
                last_entry = entry;
            }
            
            entries.Add (last_entry);

            Regex merge_regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                "Merge: .+ .+\n" +
                                "Author: (.+) <(.+)>\n" +
                                "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                "([0-9]{2}):([0-9]{2}):([0-9]{2}) .([0-9]{4})\n" +
                                "*", RegexOptions.Compiled);

            Regex non_merge_regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                "Author: (.+) <(.+)>\n" +
                                "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                "([0-9]{2}):([0-9]{2}):([0-9]{2}) .([0-9]{4})\n" +
                                "*", RegexOptions.Compiled);

            // TODO: Need to optimise for speed
            foreach (string log_entry in entries) {
                Regex regex;
                bool is_merge_commit = false;
                
                if (log_entry.Contains ("\nMerge: ")) {
                    regex = merge_regex;
                    is_merge_commit = true;
                } else {
                    regex = non_merge_regex;
                }
                
                Match match = regex.Match (log_entry);

                if (match.Success) {
                    SparkleChangeSet change_set = new SparkleChangeSet ();
                    
                    change_set.Revision  = match.Groups [1].Value;
                    change_set.UserName  = match.Groups [2].Value;
                    change_set.UserEmail = match.Groups [3].Value;
                    change_set.IsMerge   = is_merge_commit;

                    change_set.Timestamp = new DateTime (int.Parse (match.Groups [4].Value),
                        int.Parse (match.Groups [5].Value), int.Parse (match.Groups [6].Value),
                        int.Parse (match.Groups [7].Value), int.Parse (match.Groups [8].Value),
                        int.Parse (match.Groups [9].Value));
                                        
                    string [] entry_lines = log_entry.Split ("\n".ToCharArray ());
                                                        
                    foreach (string entry_line in entry_lines) {
                        if (entry_line.StartsWith (":")) {
                                                        
                            string change_type = entry_line [37].ToString ();
                            string file_path   = entry_line.Substring (39);
                            string to_file_path;
                            
                            if (change_type.Equals ("A")) {
                                change_set.Added.Add (file_path);
                            } else if (change_type.Equals ("M")) {
                                change_set.Edited.Add (file_path);
                            } else if (change_type.Equals ("D")) {
                                change_set.Deleted.Add (file_path);
                            } else if (change_type.Equals ("R")) {
                                int tab_pos  = entry_line.LastIndexOf ("\t");
                                file_path    = entry_line.Substring (42, tab_pos - 42);
                                to_file_path = entry_line.Substring (tab_pos + 1);

                                change_set.MovedFrom.Add (file_path);
                                change_set.MovedTo.Add (to_file_path);
                            }
                        }
                    }
    
                    change_sets.Add (change_set);
                }
            }

            return change_sets;
        }


        // Creates a pretty commit message based on what has changed
        private string FormatCommitMessage ()
        {
            List<string> Added    = new List<string> ();
            List<string> Modified = new List<string> ();
            List<string> Removed  = new List<string> ();
            string file_name      = "";
            string message        = "";

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            git_status.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_status.StandardOutput.ReadToEnd ().Trim ("\n".ToCharArray ());
            git_status.WaitForExit ();

            string [] lines = output.Split ("\n".ToCharArray ());
            foreach (string line in lines) {
                if (line.StartsWith ("A"))
                    Added.Add (line.Substring (3));
                else if (line.StartsWith ("M"))
                    Modified.Add (line.Substring (3));
                else if (line.StartsWith ("D"))
                    Removed.Add (line.Substring (3));
                else if (line.StartsWith ("R")) {
                    Removed.Add (line.Substring (3, (line.IndexOf (" -> ") - 3)));
                    Added.Add (line.Substring (line.IndexOf (" -> ") + 4));
                }
            }

            int count     = 0;
            int max_count = 20;

            string n = Environment.NewLine;

            foreach (string added in Added) {
                file_name = added.Trim ("\"".ToCharArray ());
                message += "+ ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "...";
            }

            foreach (string modified in Modified) {
                file_name = modified.Trim ("\"".ToCharArray ());
                message += "/ ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "...";
            }

            foreach (string removed in Removed) {
                file_name = removed.Trim ("\"".ToCharArray ());
                message += "- ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "..." + n;
            }

            return message;
        }


        public static bool IsRepo (string path)
        {
            return System.IO.Directory.Exists (Path.Combine (path, ".git"));
        }


        public bool UsesNotificationCenter
        {
            get {
                string file_path = SparkleHelpers.CombineMore (LocalPath, ".git", "disable_notification_center");
                return !File.Exists (file_path);
            }
        }


        // Disposes all resourses of this object
        public void Dispose ()
        {
            this.remote_timer.Dispose ();
            this.local_timer.Dispose ();
            this.listener.Dispose ();
        }
    }
}
