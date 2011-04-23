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

    public class SparkleRepo {

        private Timer RemoteTimer;
        private Timer LocalTimer;
        private FileSystemWatcher Watcher;
        private System.Object ChangeLock;
        private int FetchQueue;
        private int AnnounceQueue;
        private SparkleListener Listener;
        private List <double> SizeBuffer;
        private bool HasChanged;
        private string _CurrentHash;
        private bool _IsSyncing;
        private bool _IsBuffering;
        private bool _IsPolling;
        private bool _IsFetching;
        private bool _IsPushing;
        private bool _HasUnsyncedChanges;
        private bool _ServerOnline;

        public readonly string Name;
        public readonly string RemoteName;
        public readonly string Domain;
        public readonly string Description;
        public readonly string LocalPath;
        public readonly string RemoteOriginUrl;
        public readonly string UserName;
        public readonly string UserEmail;

        public string CurrentHash {
            get {
                return _CurrentHash;
            }
        }

        public bool IsBuffering {
            get {
                return _IsBuffering;
            }
        }

        public bool IsPushing {
            get {
                return _IsPushing;
            }
        }

        public bool IsPolling {
            get {
                return _IsPolling;
            }
        }

        public bool IsSyncing {
            get {
                return _IsSyncing;
            }
        }

        public bool IsFetching {
            get {
                return _IsFetching;
            }
        }

        public bool HasUnsyncedChanges {
            get {
                return _HasUnsyncedChanges;
            }
        }

        public bool ServerOnline {
            get {
                return _ServerOnline;
            }
        }

        public delegate void AddedEventHandler (object o, SparkleEventArgs args);
        public delegate void CommitedEventHandler (object o, SparkleEventArgs args);
        public delegate void PushingStartedEventHandler (object o, SparkleEventArgs args);
        public delegate void PushingFinishedEventHandler (object o, SparkleEventArgs args);
        public delegate void PushingFailedEventHandler (object o, SparkleEventArgs args);
        public delegate void FetchingStartedEventHandler (object o, SparkleEventArgs args);
        public delegate void FetchingFinishedEventHandler (object o, SparkleEventArgs args);
        public delegate void FetchingFailedEventHandler (object o, SparkleEventArgs args);
        public delegate void NewCommitEventHandler (SparkleCommit commit, string repository_path);
        public delegate void ConflictDetectedEventHandler (object o, SparkleEventArgs args);
        public delegate void ChangesDetectedEventHandler (object o, SparkleEventArgs args);
        public delegate void CommitEndedUpEmptyEventHandler (object o, SparkleEventArgs args);

        public event AddedEventHandler Added; 
        public event CommitedEventHandler Commited; 
        public event PushingStartedEventHandler PushingStarted;
        public event PushingFinishedEventHandler PushingFinished;
        public event PushingFailedEventHandler PushingFailed;
        public event FetchingStartedEventHandler FetchingStarted;
        public event FetchingFinishedEventHandler FetchingFinished;
        public event FetchingFailedEventHandler FetchingFailed;
        public event NewCommitEventHandler NewCommit;
        public event ConflictDetectedEventHandler ConflictDetected;
        public event ChangesDetectedEventHandler ChangesDetected;
        public event CommitEndedUpEmptyEventHandler CommitEndedUpEmpty;


        public SparkleRepo (string path)
        {
            LocalPath       = path;
            Name            = Path.GetFileName (LocalPath);
            RemoteOriginUrl = GetRemoteOriginUrl ();
            RemoteName      = Path.GetFileNameWithoutExtension (RemoteOriginUrl);
            Domain          = GetDomain (RemoteOriginUrl);
            Description     = GetDescription ();
            UserName        = GetUserName ();
            UserEmail       = GetUserEmail ();
            _IsSyncing      = false;
            _IsBuffering    = false;
            _IsPolling      = true;
            _IsFetching     = false;
            _IsPushing      = false;
            _ServerOnline   = true;
            HasChanged      = false;
            ChangeLock      = new Object ();
            FetchQueue      = 0;
            AnnounceQueue   = 0;

            if (IsEmpty)
                _CurrentHash = null;
            else
                _CurrentHash = GetCurrentHash ();

            string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                ".git", "has_unsynced_changes");

            if (File.Exists (unsynced_file_path))
                _HasUnsyncedChanges = true;
            else
                _HasUnsyncedChanges = false;

            if (_CurrentHash == null)
                CreateInitialCommit ();

            // Watch the repository's folder
            Watcher = new FileSystemWatcher (LocalPath) {
                IncludeSubdirectories = true,
                EnableRaisingEvents   = true,
                Filter                = "*"
            };

            Watcher.Changed += new FileSystemEventHandler (OnFileActivity);
            Watcher.Created += new FileSystemEventHandler (OnFileActivity);
            Watcher.Deleted += new FileSystemEventHandler (OnFileActivity);
            Watcher.Renamed += new RenamedEventHandler (OnFileActivity);

            // Listen to the irc channel on the server...
            if (UsesNotificationCenter)
                Listener = new SparkleListener (Domain, RemoteName, UserEmail, NotificationServerType.Central);
            else
                Listener = new SparkleListener (Domain, RemoteName, UserEmail, NotificationServerType.Own);

            // ...fetch remote changes every 60 seconds if that fails
            RemoteTimer = new Timer () {
                Interval = 60000
            };

            RemoteTimer.Elapsed += delegate {
                if (_IsPolling) {
                    CheckForRemoteChanges ();
                    
                    if (!Listener.Client.IsConnected) {
                        SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Trying to reconnect...");
                        Listener.Listen ();
                    }
                }

                if (_HasUnsyncedChanges)
                    FetchRebaseAndPush ();
            };

            // Stop polling when the connection to the irc channel is succesful
            Listener.Client.OnConnected += delegate {
                _IsPolling = false;

                // Check for changes manually one more time
                CheckForRemoteChanges ();

                // Push changes that were made since the last disconnect
                if (_HasUnsyncedChanges)
                    Push ();

                SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Connected. Now listening... (" + Listener.Server + ")");

                if (AnnounceQueue > 0) {
                    Listener.Announce (_CurrentHash);
                    AnnounceQueue = 0;
                    SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Queued messages delivered. (" + Listener.Server + ")");
                }
            };

            // Start polling when the connection to the irc channel is lost
            Listener.Client.OnConnectionError += delegate {
                SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Lost connection. Falling back to polling...");
                _IsPolling = true;
            };
            
            // Start polling when the connection to the irc channel is lost
            Listener.Client.OnDisconnected += delegate {
                SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Lost connection. Falling back to polling...");
                _IsPolling = true;
            };

            // Fetch changes when there is a message in the irc channel
            Listener.Client.OnChannelMessage += delegate (object o, IrcEventArgs args) {
                SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Was notified of a remote change.");
                string message = args.Data.Message.Trim ();
                
                if (!message.Equals (_CurrentHash) && message.Length == 40) {
                    FetchQueue++;

                    if (!_IsFetching) {
                        while (FetchQueue > 0) {
                            Fetch ();
                            FetchQueue--;
                        }
                        
                        Watcher.EnableRaisingEvents = false;
                        Rebase ();
                        Watcher.EnableRaisingEvents = true;
                    }
                } else {
                    // Not really needed as we won't be notified about our own messages
                    SparkleHelpers.DebugInfo ("Irc",
                        "[" + Name + "] False alarm, already up to date. (" + _CurrentHash + ")");
                }
            };

            // Start listening
            Listener.Listen ();

            SizeBuffer = new List <double> ();

            // Keep a timer that checks if there are changes and
            // whether they have settled
            LocalTimer = new Timer () {
                Interval = 250
            };

            LocalTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
                CheckForChanges ();
            };

            RemoteTimer.Start ();
            LocalTimer.Start ();

            // Add everything that changed 
            // since SparkleShare was stopped
            AddCommitAndPush ();

            if (_CurrentHash == null)
                _CurrentHash = GetCurrentHash ();
        }


        private void CheckForRemoteChanges ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Checking for remote changes...");
            SparkleGit git = new SparkleGit (LocalPath, "ls-remote origin master");
        
            git.Exited += delegate {
                if (git.ExitCode != 0)
                    return;

                string remote_hash = git.StandardOutput.ReadToEnd ();

                if (!remote_hash.StartsWith (_CurrentHash)) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found.");
                    Fetch ();
                    
                    Watcher.EnableRaisingEvents = false;
                    Rebase ();
                    Watcher.EnableRaisingEvents = true;
                }
            };

            git.Start ();
            git.WaitForExit ();
        }


        private void CheckForChanges ()
        {
            lock (ChangeLock) {
                if (HasChanged) {
                    if (SizeBuffer.Count >= 4)
                        SizeBuffer.RemoveAt (0);
                        
                    DirectoryInfo dir_info = new DirectoryInfo (LocalPath);
                    SizeBuffer.Add (CalculateFolderSize (dir_info));

                    if (SizeBuffer [0].Equals (SizeBuffer [1]) &&
                        SizeBuffer [1].Equals (SizeBuffer [2]) &&
                        SizeBuffer [2].Equals (SizeBuffer [3])) {

                        SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled.");
                        _IsBuffering = false;
                        HasChanged   = false;
                        
                        while (AnyDifferences) {
                            Watcher.EnableRaisingEvents = false;
                            AddCommitAndPush ();
                            Watcher.EnableRaisingEvents = true;
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
                _IsBuffering = true;

                // Only fire the event if the timer has been stopped.
                // This prevents multiple events from being raised whilst "buffering".
                if (!HasChanged) {
                    SparkleEventArgs args = new SparkleEventArgs ("ChangesDetected");

                    if (ChangesDetected != null)
                        ChangesDetected (this, args);
                }

                SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + fse_args.Name + "'");
                SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes found, checking if settled.");
                
                RemoteTimer.Stop ();

                lock (ChangeLock) {
                    HasChanged = true;
                }
            }
        }


        // When there are changes we generally want to Add, Commit and Push,
        // so this method does them all with appropriate timers, etc. switched off
        public void AddCommitAndPush ()
        {
            try {
                LocalTimer.Stop ();
                RemoteTimer.Stop ();
    
                if (AnyDifferences) {
                    Add ();

                    string message = FormatCommitMessage ();
                    Commit (message);

                    Push ();
                } else {
                    SparkleEventArgs args = new SparkleEventArgs ("CommitEndedUpEmpty");

                    if (CommitEndedUpEmpty != null)
                        CommitEndedUpEmpty (this, args);
                }
            } finally {

                RemoteTimer.Start ();
                LocalTimer.Start ();

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
                    if (line.Length > 1 &&!line [1].Equals (" "))
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


        private string GetCurrentHash ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "log -1 --format=%H");
            git.Start ();
            git.WaitForExit ();

            string output = git.StandardOutput.ReadToEnd ();
            string hash   = output.Trim ();

            return hash;
        }


        // Stages the made changes
        private void Add ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Staging changes...");

            SparkleGit git = new SparkleGit (LocalPath, "add --all");
            git.Start ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes staged.");
            SparkleEventArgs args = new SparkleEventArgs ("Added");

            if (Added != null)
                Added (this, args);
        }


        // Removes unneeded objects
        private void CollectGarbage ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Collecting garbage...");

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

            _CurrentHash = GetCurrentHash ();
            SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message + " (" + _CurrentHash + ")");

            SparkleEventArgs args = new SparkleEventArgs ("Commited") {
                Message = message
            };

            if (Commited != null)
                Commited (this, args);
            
            // Collect garbage pseudo-randomly
            if (DateTime.Now.Second % 10 == 0)
                CollectGarbage ();
        }


        // Fetches changes from the remote repository
        public void Fetch ()
        {
            _IsSyncing  = true;
            _IsFetching = true;

            RemoteTimer.Stop ();

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Fetching changes...");
            SparkleGit git = new SparkleGit (LocalPath, "fetch -v origin master");

            SparkleEventArgs args = new SparkleEventArgs ("FetchingStarted");

            if (FetchingStarted != null)
                FetchingStarted (this, args); 

            git.Exited += delegate {
                SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes fetched.");

                _IsSyncing   = false;
                _IsFetching  = false;
                _CurrentHash = GetCurrentHash ();

                if (git.ExitCode != 0) {
                    _ServerOnline = false;

                    args = new SparkleEventArgs ("FetchingFailed");
                    
                    if (FetchingFailed != null)
                        FetchingFailed (this, args);
                } else {
                    _ServerOnline = true;
                    
                    args = new SparkleEventArgs ("FetchingFinished");

                    if (FetchingFinished != null)
                        FetchingFinished (this, args);
                }

                RemoteTimer.Start ();
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

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Rebasing changes...");
            SparkleGit git = new SparkleGit (LocalPath, "rebase -v FETCH_HEAD");

            git.Exited += delegate {
/*                if (Status.MergeConflict.Count > 0) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict detected...");

                    foreach (string problem_file_name in Status.MergeConflict) {
                        SparkleGit git_ours = new SparkleGit (LocalPath,
                            "checkout --ours " + problem_file_name);
                        git_ours.Start ();
                        git_ours.WaitForExit ();
    
                        string timestamp     = DateTime.Now.ToString ("H:mm d MMM");
                        string new_file_name = problem_file_name + " (" + UserName  + ", " + timestamp + ")";

                        File.Move (problem_file_name, new_file_name);
                                           
                        SparkleGit git_theirs = new SparkleGit (LocalPath,
                            "checkout --theirs " + problem_file_name);
                        git_theirs.Start ();
                        git_theirs.WaitForExit ();
                    
                        SparkleEventArgs args = new SparkleEventArgs ("ConflictDetected");
    
                        if (ConflictDetected != null)
                            ConflictDetected (this, args);
                    }

                    Add ();

                    SparkleGit git_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_continue.Start ();
                    git_continue.WaitForExit ();

                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict resolved.");
*/
                    _CurrentHash = GetCurrentHash ();
//                    Push ();
//                }
            };

            git.Start ();
            git.WaitForExit ();

            _CurrentHash = GetCurrentHash ();

            if (NewCommit != null)
                NewCommit (GetCommits (1) [0], LocalPath);
                
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes rebased.");
        }


        // Pushes the changes to the remote repo
        public void Push ()
        {
            _IsSyncing = true;
            _IsPushing = true;

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing changes...");
            SparkleGit git = new SparkleGit (LocalPath, "push origin master");

            SparkleEventArgs args = new SparkleEventArgs ("PushingStarted");
            
            if (PushingStarted != null)
                PushingStarted (this, args);
    
            git.Exited += delegate {
                _IsSyncing = false;
                _IsPushing = false;

                if (git.ExitCode != 0) {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing failed.");

                    string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
                        ".git", "has_unsynced_changes");

                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path);

                    _HasUnsyncedChanges = true;

                    args = new SparkleEventArgs ("PushingFailed");

                    if (PushingFailed != null)
                        PushingFailed (this, args);

                    FetchRebaseAndPush ();
                } else {
                    SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes pushed.");
                    args = new SparkleEventArgs ("PushingFinished");

                    string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
                        ".git", "has_unsynced_changes");

                    if (File.Exists (unsynced_file_path))
                        File.Delete (unsynced_file_path);

                    _HasUnsyncedChanges = false;

                    if (PushingFinished != null)
                        PushingFinished (this, args); 

                    if (Listener.Client.IsConnected) {
                        Listener.Announce (_CurrentHash);
                    } else {
                        AnnounceQueue++;
                        SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Could not deliver notification, added it to the queue");
                     }
                }

            };

            git.Start ();
            git.WaitForExit ();
        }


        // Gets the domain name of a given URL
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


        private string GetRemoteOriginUrl ()
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
        public List <SparkleCommit> GetCommits (int count)
        {
            if (count < 1)
                count = 30;
            
            List <SparkleCommit> commits = new List <SparkleCommit> ();

            SparkleGit git_log = new SparkleGit (LocalPath, "log -" + count + " --raw  --date=iso");
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

            // TODO: Need to optimise for speed
            foreach (string log_entry in entries) {
                Regex regex;
                bool is_merge_commit = false;
                
                if (log_entry.Contains ("\nMerge: ")) {
                    regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                        "Merge: .+ .+\n" +
                                        "Author: (.+) <(.+)>\n" +
                                        "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                        "([0-9]{2}):([0-9]{2}):([0-9]{2}) .([0-9]{4})\n" +
                                        "*");
                    
                    is_merge_commit = true;
                } else {
                    regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                        "Author: (.+) <(.+)>\n" +
                                        "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                        "([0-9]{2}):([0-9]{2}):([0-9]{2}) .([0-9]{4})\n" +
                                        "*");
                }
                
                Match match = regex.Match (log_entry);

                if (match.Success) {
                    SparkleCommit commit = new SparkleCommit ();
                    
                    commit.Hash      = match.Groups [1].Value;
                    commit.UserName  = match.Groups [2].Value;
                    commit.UserEmail = match.Groups [3].Value;
                    commit.IsMerge   = is_merge_commit;

                    commit.DateTime = new DateTime (int.Parse (match.Groups [4].Value),
                        int.Parse (match.Groups [5].Value), int.Parse (match.Groups [6].Value),
                        int.Parse (match.Groups [7].Value), int.Parse (match.Groups [8].Value),
                        int.Parse (match.Groups [9].Value));
                                        
                    string [] entry_lines = log_entry.Split ("\n".ToCharArray ());
                                                        
                    foreach (string entry_line in entry_lines) {
                        if (entry_line.StartsWith (":")) {
                                                        
                            string change_type = entry_line [37].ToString ();
                            string file_path   = entry_line.Substring (39);
                            
                            if (change_type.Equals ("A")) {
                                commit.Added.Add (file_path);
                            } else if (change_type.Equals ("M")) {
                                commit.Edited.Add (file_path);
                            } else if (change_type.Equals ("D")) {
                                commit.Deleted.Add (file_path);
                            }
                        }
                    }
    
                    commits.Add (commit);
                }
            }

            return commits;
        }


        // Creates a pretty commit message based on what has changed
        private string FormatCommitMessage ()
        {
            List<string> Added    = new List<string> ();
            List<string> Modified = new List<string> ();
            List<string> Removed  = new List<string> ();
            string file_name      = "";
            string message        = null;

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            git_status.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_status.StandardOutput.ReadToEnd ().Trim ("\n".ToCharArray ());
            git_status.WaitForExit ();

            string [] lines = output.Split ("\n".ToCharArray ());
            foreach (string line in lines) {
                if (line.StartsWith ("A"))
                    Added.Add (line.Substring (2));
                else if (line.StartsWith ("M"))
                    Modified.Add (line.Substring (2));
                else if (line.StartsWith ("D"))
                    Removed.Add (line.Substring (2));
            }

            if (Added.Count > 0) {
                foreach (string added in Added) {
                    file_name = added;
                    break;
                }

                message = "+ ‘" + file_name + "’";
            }

            if (Modified.Count > 0) {
                foreach (string modified in Modified) {
                    file_name = modified;
                    break;
                }

                message = "/ ‘" + file_name + "’";
            }

            if (Removed.Count > 0) {
                foreach (string removed in Removed) {
                    file_name = removed;
                    break;
                }

                message = "- ‘" + file_name + "’";
            }

            int changes_count = (Added.Count +
                                 Modified.Count +
                                 Removed.Count);

            if (changes_count > 1)
                message += " + " + (changes_count - 1);

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
            RemoteTimer.Dispose ();
            LocalTimer.Dispose ();
            Listener.Dispose ();
        }
    }
}
