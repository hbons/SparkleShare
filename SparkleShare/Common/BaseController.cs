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
using System.Linq;
using System.Threading;

using Sparkles;
using Sparkles.Git;

namespace SparkleShare {
    
    public abstract class BaseController {
        
        public BaseRepository [] Repositories {
            get {
                lock (this.repo_lock)
                    return this.repositories.GetRange (0, this.repositories.Count).ToArray ();
            }
        }
        
        
        void AddRepository (BaseRepository repo)
        {
            lock (this.repo_lock) {
                this.repositories.Add (repo);
                this.repositories.Sort ((x, y) => string.Compare (x.Name, y.Name));
            }
        }
        
        
        void RemoveRepository (BaseRepository repo)
        {
            lock (this.repo_lock)
                this.repositories.Remove (repo);
        }
        
        
        public BaseRepository GetRepoByName (string name)
        {
            lock (this.repo_lock) {
                foreach (BaseRepository repo in this.repositories)
                    if (repo.Name.Equals (name))
                        return repo;
            }
            
            return null;
        }
        
        
        public Configuration Config { get; private set; }
        public bool RepositoriesLoaded { get; private set; }
        public string FoldersPath { get; private set; }
        
        public double ProgressPercentage = 0.0;
        public double ProgressSpeedUp    = 0.0;
        public double ProgressSpeedDown  = 0.0;
        
        
        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);

        public event ShowNoteWindowEventHandler ShowNoteWindowEvent = delegate { };
        public delegate void ShowNoteWindowEventHandler (string project);

        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };
        
        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler (string remote_url, string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError = delegate { };
        public delegate void FolderFetchErrorHandler (string remote_url, string [] errors);
        
        public event FolderFetchingHandler FolderFetching = delegate { };
        public delegate void FolderFetchingHandler (double percentage, double speed);
        
        
        public event Action FolderListChanged = delegate { };
        public event Action OnIdle = delegate { };
        public event Action OnSyncing = delegate { };
        public event Action OnError = delegate { };
        
        
        public event InviteReceivedHandler InviteReceived = delegate { };
        public delegate void InviteReceivedHandler (SparkleInvite invite);
        
        public event NotificationRaisedEventHandler NotificationRaised = delegate { };
        public delegate void NotificationRaisedEventHandler (ChangeSet change_set);
        
        public event AlertNotificationRaisedEventHandler AlertNotificationRaised = delegate { };
        public delegate void AlertNotificationRaisedEventHandler (string title, string message);
        
        
        public bool FirstRun {
            get { return Config.User.Email.Equals ("Unknown"); }
        }
        
        public List<string> Folders {
            get {
                List<string> folders = Config.Folders;
                return folders;
            }
        }


        public User CurrentUser {
            get { return Config.User; }
            set { Config.User = value; }
        }

        public SSHAuthenticationInfo UserAuthenticationInfo;


        public bool NotificationsEnabled {
            get {
                string notifications_enabled = Config.GetConfigOption ("notifications");
                
                if (string.IsNullOrEmpty (notifications_enabled)) {
                    Config.SetConfigOption ("notifications", bool.TrueString);
                    return true;
                    
                } else {
                    return notifications_enabled.Equals (bool.TrueString);
                }
            }
        }
        
        public bool AvatarsEnabled {
            get {
                string fetch_avatars_option = Config.GetConfigOption ("fetch_avatars");
                
                if (fetch_avatars_option == null || fetch_avatars_option.Equals (bool.FalseString))
                    return false;
                
                return true;
            }
        }
        
        
        // Path where the plugins are kept
        public abstract string PresetsPath { get; }
        
        // Enables SparkleShare to start automatically at login
        public abstract void CreateStartupItem ();

        // Installs the sparkleshare:// protocol handler
        public abstract void InstallProtocolHandler ();

        // Installs the sparkleshare:// protocol handler
        public abstract void SetFolderIcon ();

        // Creates the SparkleShare folder in the user's home folder
        public abstract bool CreateSparkleShareFolder ();
        
        // Opens the SparkleShare folder or an (optional) subfolder
        public abstract void OpenFolder (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenFile (string path);
        
        // Opens a file with the appropriate application
        public virtual void OpenWebsite (string url) { }
        
        // Copies text to the clipboard
        public abstract void CopyToClipboard (string text);
        
        public abstract string EventLogHTML { get; }
        public abstract string DayEntryHTML { get; }
        public abstract string EventEntryHTML { get; }
        
        
        BaseFetcher fetcher;
        FileSystemWatcher watcher;
        object repo_lock = new object ();
        object check_repos_lock = new object ();
        List<BaseRepository> repositories = new List<BaseRepository> ();
        bool lost_folders_path = false;
        
        
        public BaseController ()
        {
            
            string app_data_path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
            string config_path   = Path.Combine (app_data_path, "sparkleshare");
            
            Config = new Configuration (config_path, "config.xml");
            Configuration.DefaultConfig = Config;

            UserAuthenticationInfo = new SSHAuthenticationInfo ();
            SSHAuthenticationInfo.DefaultAuthenticationInfo = UserAuthenticationInfo;

            FoldersPath = Config.FoldersPath;
        }
        
        
        public virtual void Initialize ()
        {
            Logger.LogInfo ("Environment", "SparkleShare " + InstallationInfo.Version);
            Logger.LogInfo ("Environment", "Git " + GitCommand.GitVersion);
            Logger.LogInfo ("Environment", InstallationInfo.Platform + " (" + Environment.OSVersion + ")");
            
            Preset.PresetsPath = PresetsPath;
            InstallProtocolHandler ();
            
            try {
                CreateSparkleShareFolder ();

            } catch (DirectoryNotFoundException) {
                this.lost_folders_path = true;
            }

            SetFolderIcon ();

            // Watch the SparkleShare folder
            this.watcher = new FileSystemWatcher () {
                Filter                = "*",
                IncludeSubdirectories = false,
                Path                  = FoldersPath
            };
            
            watcher.Created += OnFolderActivity;
            watcher.EnableRaisingEvents = true;
        }
        
        
        int reopen_attempt_counts = 0;
        
        public void HandleReopen ()
        {
            if (Repositories.Length > 0) {
                ShowEventLogWindow ();
                
            } else if (reopen_attempt_counts > 1) {
                AlertNotificationRaised ("Hello!", "SparkleShare sits right here, as a status icon.");
                reopen_attempt_counts = 0;
                
            } else {
                reopen_attempt_counts++;
            }
        }
        
        
        public void UIHasLoaded ()
        {
            if (this.lost_folders_path) {
                SparkleShare.UI.Bubbles.Controller.ShowBubble ("Where's your SparkleShare folder?",
                                                          "Did you put it on a detached drive?", null);
                
                Environment.Exit (-1);
            }
            
            if (FirstRun) {
                ShowSetupWindow (PageType.Setup);
                
            } else {
                new Thread (() => {
                    StartupInviteScan ();
                    CheckRepositories ();
                    RepositoriesLoaded = true;
                    UpdateState ();
                    
                }).Start ();
            }

            AlertNotificationRaised ("Hello!", "This is a test");
        }
        
        
        public void ShowSetupWindow (PageType page_type)
        {
            ShowSetupWindowEvent (page_type);
        }
        
        
        public void ShowAboutWindow ()
        {
            ShowAboutWindowEvent ();
        }


        public void ShowNoteWindow (string project)
        {
            ShowNoteWindowEvent (project);
        }
        
        
        public void ShowEventLogWindow ()
        {
            ShowEventLogWindowEvent ();
        }
        
        
        public void OpenSparkleShareFolder ()
        {
            OpenFolder (Config.FoldersPath);
        }
        
        
        public void OpenSparkleShareFolder (string name)
        {
            OpenFolder (new SparkleFolder (name).FullPath);
        }
        
        
        public void ToggleNotifications ()
        {
            bool notifications_enabled = Config.GetConfigOption ("notifications").Equals (bool.TrueString);
            Config.SetConfigOption ("notifications", (!notifications_enabled).ToString ());
        }
        
        
        void CheckRepositories ()
        {
            lock (this.check_repos_lock) {
                string path = Config.FoldersPath;
                
                // Detect any renames
                foreach (string folder_path in Directory.GetDirectories (path)) {
                    string folder_name = Path.GetFileName (folder_path);
                    
                    if (folder_name.Equals (".tmp"))
                        continue;
                    
                    if (Config.GetIdentifierForFolder (folder_name) == null) {
                        string identifier_file_path = Path.Combine (folder_path, ".sparkleshare");
                        
                        if (!File.Exists (identifier_file_path))
                            continue;
                        
                        string identifier = File.ReadAllText (identifier_file_path).Trim ();
                        
                        if (Config.IdentifierExists (identifier)) {
                            RemoveRepository (GetRepoByName (folder_name));
                            Config.RenameFolder (identifier, folder_name);
                            
                            string new_folder_path = Path.Combine (path, folder_name);
                            AddRepository (new_folder_path);
                            
                            Logger.LogInfo ("Controller",
                                                   "Renamed folder with identifier " + identifier + " to '" + folder_name + "'");
                        }
                    }
                }
                
                // Remove any deleted folders
                foreach (string folder_name in Config.Folders) {
                    string folder_path = new SparkleFolder (folder_name).FullPath;
                    
                    if (!Directory.Exists (folder_path)) {
                        Config.RemoveFolder (folder_name);
                        RemoveRepository (GetRepoByName (folder_name));
                        
                        Logger.LogInfo ("Controller", "Removed folder '" + folder_name + "' from config");
                        
                    } else {
                        AddRepository (folder_path);
                    }
                }
                
                // Remove any duplicate folders
                string previous_name = "";
                foreach (string folder_name in Config.Folders) {
                    if (!string.IsNullOrEmpty (previous_name) && folder_name.Equals (previous_name))
                        Config.RemoveFolder (folder_name);
                    else
                        previous_name = folder_name;
                }
                
                FolderListChanged ();
            }
        }
        
        
        void AddRepository (string folder_path)
        {
            BaseRepository repo = null;
            string folder_name   = Path.GetFileName (folder_path);
            string backend       = Config.GetBackendForFolder (folder_name);
            
            try {
                repo = (BaseRepository) Activator.CreateInstance (
                    Type.GetType ("Sparkles." + backend + "." + backend + "Repository, Sparkles." + backend),
                    new object [] { folder_path, Config });
                
            } catch (Exception e) {
                Logger.LogInfo ("Controller", "Failed to load backend '" + backend + "' for '" + folder_name + "': ", e);
                return;
            }
            
            repo.ChangesDetected += delegate {
                UpdateState ();
            };
            
            repo.SyncStatusChanged += delegate (SyncStatus status) {
                if (status == SyncStatus.Idle) {
                    ProgressPercentage = 0.0;
                    ProgressSpeedUp    = 0.0;
                    ProgressSpeedDown  = 0.0;
                }
                
                UpdateState ();
            };
            
            repo.ProgressChanged += delegate {
                ProgressPercentage = 0.0;
                ProgressSpeedUp    = 0.0;
                ProgressSpeedDown  = 0.0;
                
                double percentage = 0.0;
                int repo_count    = 0;
                
                foreach (BaseRepository rep in Repositories) {
                    if (rep.ProgressPercentage > 0) {
                        percentage += rep.ProgressPercentage;
                        repo_count++;
                    }
                    
                    if (rep.Status == SyncStatus.SyncUp)
                        ProgressSpeedUp += rep.ProgressSpeed;
                    
                    if (rep.Status == SyncStatus.SyncDown)
                        ProgressSpeedDown += rep.ProgressSpeed;
                }
                
                if (repo_count > 0)
                    ProgressPercentage = percentage / repo_count;
                
                UpdateState ();
            };
            
            repo.NewChangeSet += delegate (ChangeSet change_set) {
                if (AvatarsEnabled)
                    change_set.User.AvatarFilePath = Avatars.GetAvatar (change_set.User.Email, 48, Config.FullPath);
                
                NotificationRaised (change_set);
            };
            
            repo.ConflictResolved += delegate {
                AlertNotificationRaised ("Resolved a file collision",
                                         "Local and server versions were kept.");
            };
            
            AddRepository (repo);
            repo.Initialize (); 
        }


        void OnFolderActivity (object o, FileSystemEventArgs args)
        {
            if (args != null && args.FullPath.EndsWith (".xml") &&
                args.ChangeType == WatcherChangeTypes.Created) {

                HandleInvite (args);
                return;
            }
        }
        
        
        void StartupInviteScan ()
        {
            foreach (string invite in Directory.GetFiles (FoldersPath, "*.xml")) {
                HandleInvite (invite);
            }
        }
        
        
        void HandleInvite (FileSystemEventArgs args)
        {
            HandleInvite (args.FullPath);
        }
        
        
        void HandleInvite (string path)
        {
            if (this.fetcher != null &&
                this.fetcher.IsActive) {
                
                AlertNotificationRaised ("SparkleShare Setup seems busy", "Please wait for it to finish");
                
            } else {
                SparkleInvite invite = new SparkleInvite (path);
                
                // It may be that the invite we received a path to isn't
                // fully downloaded yet, so we try to read it several times
                int tries = 0;
                while (!invite.IsValid) {
                    Thread.Sleep (100);
                    invite = new SparkleInvite (path);
                    tries++;
                    
                    if (tries > 10) {
                        AlertNotificationRaised ("Oh noes!", "This invite seems screwed up...");
                        break;
                    }
                }
                
                if (invite.IsValid)
                    InviteReceived (invite);
                
                File.Delete (path);
            }
        }
        
        
        // Fires events for the current syncing state
        void UpdateState ()
        {
            bool has_unsynced_repos = false;
            bool has_syncing_repos  = false;
            
            foreach (BaseRepository repo in Repositories) {
                if (repo.Status == SyncStatus.SyncDown || repo.Status == SyncStatus.SyncUp || repo.IsBuffering) {
                    has_syncing_repos = true;
                    break;
                    
                } else if (repo.Status == SyncStatus.Idle && repo.HasUnsyncedChanges) {
                    has_unsynced_repos = true;
                }
            }
            
            if (has_syncing_repos)
                OnSyncing ();
            else if (has_unsynced_repos)
                OnError ();
            else
                OnIdle ();
        }
        
        
        public void StartFetcher (SparkleFetcherInfo info)
        {
            string tmp_path = Config.TmpPath;
            
            if (!Directory.Exists (tmp_path)) {
                Directory.CreateDirectory (tmp_path);
                File.SetAttributes (tmp_path, File.GetAttributes (tmp_path) | FileAttributes.Hidden);
            }
            
            string canonical_name = Path.GetFileName (info.RemotePath);
            string backend        = info.Backend; 
            
            if (string.IsNullOrEmpty (backend))
                backend = BaseFetcher.GetBackend (info.Address);
            
            info.TargetDirectory  = Path.Combine (tmp_path, canonical_name);
            
            try {
                this.fetcher = (BaseFetcher) Activator.CreateInstance (
                    Type.GetType ("Sparkles." + backend + "." + backend + "Fetcher, Sparkles." + backend), info);
                
            } catch (Exception e) {
                Logger.LogInfo ("Controller",
                                       "Failed to load '" + backend + "' backend for '" + canonical_name + "' " + e.Message);
                
                FolderFetchError (Path.Combine (info.Address, info.RemotePath).Replace (@"\", "/"),
                                  new string [] {"Failed to load \"" + backend + "\" backend for \"" + canonical_name + "\""});
                
                return;
            }
            
            this.fetcher.Finished += delegate (bool repo_is_encrypted, bool repo_is_empty, string [] warnings) {
                if (repo_is_encrypted && repo_is_empty) {
                    ShowSetupWindowEvent (PageType.CryptoSetup);
                    
                } else if (repo_is_encrypted) {
                    ShowSetupWindowEvent (PageType.CryptoPassword);
                    
                } else {
                    FinishFetcher ();
                }
            };
            
            this.fetcher.Failed += delegate {
                FolderFetchError (this.fetcher.RemoteUrl.ToString (), this.fetcher.Errors);
                StopFetcher ();
            };
            
            this.fetcher.ProgressChanged += delegate (double percentage, double speed) {
                FolderFetching (percentage, speed);
            };
            
            this.fetcher.Start ();
        }
        
        
        public void StopFetcher ()
        {
            this.fetcher.Stop ();
            this.fetcher.Dispose ();
            
            this.fetcher = null;
            this.watcher.EnableRaisingEvents = true;
        }
        
        
        public bool CheckPassword (string password)
        {
            return this.fetcher.IsFetchedRepoPasswordCorrect (password);
        }
        
        
        public void FinishFetcher (string password)
        {
            this.fetcher.EnableFetchedRepoCrypto (password);
            FinishFetcher ();
        }
        
        
        public void FinishFetcher ()
        {
            this.watcher.EnableRaisingEvents = false;
            
            this.fetcher.Complete ();
            string canonical_name = Path.GetFileName (this.fetcher.RemoteUrl.AbsolutePath);
            
            if (canonical_name.EndsWith (".git"))
                canonical_name = canonical_name.Replace (".git", "");
            
            canonical_name = canonical_name.Replace ("-crypto", "");
            canonical_name = canonical_name.ReplaceUnderscoreWithSpace ();
            canonical_name = canonical_name.Replace ("%20", " ");
            
            bool target_folder_exists = Directory.Exists (
                Path.Combine (Config.FoldersPath, canonical_name));
            
            // Add a numbered suffix to the name if a folder with the same name
            // already exists. Example: "Folder (2)"
            int suffix = 1;
            while (target_folder_exists) {
                suffix++;
                target_folder_exists = Directory.Exists (
                    Path.Combine (Config.FoldersPath, canonical_name + " (" + suffix + ")"));
            }
            
            string target_folder_name = canonical_name;
            
            if (suffix > 1)
                target_folder_name += " (" + suffix + ")";
            
            string target_folder_path = Path.Combine (Config.FoldersPath, target_folder_name);
            
            try {
                Directory.Move (this.fetcher.TargetFolder, target_folder_path);
                
            } catch (Exception e) {
                Logger.LogInfo ("Controller", "Error moving directory, trying again...", e);
                
                try {
                    ClearDirectoryAttributes (this.fetcher.TargetFolder);
                    Directory.Move (this.fetcher.TargetFolder, target_folder_path);
                    
                } catch (Exception x) {
                    Logger.LogInfo ("Controller", "Error moving directory", x);
                    
                    this.fetcher.Dispose ();
                    this.fetcher = null;
                    this.watcher.EnableRaisingEvents = true;
                    return;
                }
            }
            
            string backend = BaseFetcher.GetBackend (this.fetcher.RemoteUrl.ToString ());
            
            Config.AddFolder (target_folder_name, this.fetcher.Identifier,
                              this.fetcher.RemoteUrl.ToString (), backend);
            
            if (this.fetcher.OriginalFetcherInfo.AnnouncementsUrl != null) {
                Config.SetFolderOptionalAttribute (target_folder_name, "announcements_url",
                                                   this.fetcher.OriginalFetcherInfo.AnnouncementsUrl);
            }
            
            RepositoriesLoaded = true;
            FolderFetched (this.fetcher.RemoteUrl.ToString (), this.fetcher.Warnings.ToArray ());
            
            AddRepository (target_folder_path);
            FolderListChanged ();
            
            this.fetcher.Dispose ();
            this.fetcher = null;
            
            this.watcher.EnableRaisingEvents = true;
        }
        
        
        public virtual void Quit ()
        {
            foreach (BaseRepository repo in Repositories)
                repo.Dispose ();
            
            Environment.Exit (0);
        }


        void ClearDirectoryAttributes (string path)
        {
            if (!Directory.Exists (path))
                return;

            string [] folders = Directory.GetDirectories (path);

            foreach (string folder in folders)
                ClearDirectoryAttributes (folder);

            string [] files = Directory.GetFiles (path);

            foreach (string file in files)
                if (!IsSymlink (file))
                    File.SetAttributes (file, FileAttributes.Normal);
        }

        
        bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }
    }
}
