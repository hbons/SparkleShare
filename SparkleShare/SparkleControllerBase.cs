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

using SparkleLib;

namespace SparkleShare {

    public abstract class SparkleControllerBase {

        public SparkleRepoBase [] Repositories {
            get {
                lock (this.repo_lock)
                    return this.repositories.GetRange (0, this.repositories.Count).ToArray ();
            }
        }


        public SparkleConfig Config { get; private set; }
        public bool RepositoriesLoaded { get; private set; }
        public string FoldersPath { get; private set; }

        public double ProgressPercentage = 0.0;
        public double ProgressSpeedUp    = 0.0;
        public double ProgressSpeedDown  = 0.0;


        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);

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
        public delegate void NotificationRaisedEventHandler (SparkleChangeSet change_set);

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

        public SparkleUser CurrentUser {
            get { return Config.User; }
            set { Config.User = value; }
        }

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
                
                if (fetch_avatars_option != null && fetch_avatars_option.Equals (bool.FalseString))
                    return false;
                
                return true;
            }
        }


        // Path where the plugins are kept
        public abstract string PluginsPath { get; }

        // Enables SparkleShare to start automatically at login
        public abstract void CreateStartupItem ();

        // Installs the sparkleshare:// protocol handler
        public abstract void InstallProtocolHandler ();

        // Adds the SparkleShare folder to the user's
        // list of bookmarked places
        public abstract void AddToBookmarks ();

        // Creates the SparkleShare folder in the user's home folder
        public abstract bool CreateSparkleShareFolder ();

        // Opens the SparkleShare folder or an (optional) subfolder
        public abstract void OpenFolder (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenFile (string path);
        
        // Opens a file with the appropriate application
        public abstract void OpenWebsite (string url);

        // Copies text to the clipboard
        public abstract void CopyToClipboard (string text);

        public abstract string EventLogHTML { get; }
        public abstract string DayEntryHTML { get; }
        public abstract string EventEntryHTML { get; }


        private SparkleFetcherBase fetcher;
        private FileSystemWatcher watcher;
        private Object repo_lock = new Object ();
        private Object check_repos_lock = new Object ();
        private List<SparkleRepoBase> repositories = new List<SparkleRepoBase> ();
        private bool lost_folders_path = false;


        public SparkleControllerBase ()
        {

            string app_data_path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
            string config_path   = Path.Combine (app_data_path, "sparkleshare");
            
            Config                      = new SparkleConfig (config_path, "config.xml");
            SparkleConfig.DefaultConfig = Config;
            FoldersPath                 = Config.FoldersPath;
        }


        public virtual void Initialize ()
        {
            SparkleLogger.LogInfo ("Environment", "SparkleShare version: " + SparkleLib.SparkleBackend.Version +
                ", Operating system: " + SparkleLib.SparkleBackend.Platform + " (" + Environment.OSVersion + ")");

            SparklePlugin.PluginsPath = PluginsPath;
            InstallProtocolHandler ();

            try {
                if (CreateSparkleShareFolder ())
                    AddToBookmarks ();

            } catch (DirectoryNotFoundException) {
                this.lost_folders_path = true;
            }

            if (FirstRun) {
                Config.SetConfigOption ("notifications", bool.TrueString);

            } else {
                string keys_path = Path.GetDirectoryName (Config.FullPath);
                string key_file_path = "";

                foreach (string file_path in Directory.GetFiles (keys_path)) {
                    string file_name = Path.GetFileName(file_path);
                    if (file_name.EndsWith (".key")) {
                        key_file_path = Path.Combine (keys_path, file_name);

                        // Replace spaces with underscores in old keys
                        if (file_name.Contains (" ")) {
                            string new_file_name = file_name.Replace (" ", "_");
                            File.Move (key_file_path, Path.Combine (keys_path, new_file_name));
                            File.Move (key_file_path + ".pub", Path.Combine (keys_path, new_file_name + ".pub"));
                            key_file_path = Path.Combine (keys_path, new_file_name);
                        }

                        SparkleKeys.ImportPrivateKey (key_file_path);

                        break;
                    }
                }

                CurrentUser.PublicKey = File.ReadAllText (key_file_path + ".pub");
                SparkleKeys.ListPrivateKeys ();
            }

            // Watch the SparkleShare folder
            this.watcher = new FileSystemWatcher () {
                Filter                = "*",
                IncludeSubdirectories = false,
                Path                  = FoldersPath
            };

            watcher.Created += OnFolderActivity;
            // FIXME watcher.Deleted += OnFolderActivity;
            // FIXME watcher.Renamed += OnFolderActivity;

            watcher.EnableRaisingEvents = true;
        }


        public void UIHasLoaded ()
        {
            if (this.lost_folders_path) {
                Program.UI.Bubbles.Controller.ShowBubble ("Where's your SparkleShare folder?",
                    "Did you put it on a detached drive?", null);
            
                Environment.Exit (-1);
            }

            if (FirstRun) {
                ShowSetupWindow (PageType.Setup);

                new Thread (() => {
                    string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
                    string key_file_name = DateTime.Now.ToString ("yyyy-MM-dd_HH\\hmm");
                    
                    string [] key_pair = SparkleKeys.GenerateKeyPair (keys_path, key_file_name);
                    SparkleKeys.ImportPrivateKey (key_pair [0]);

                    CurrentUser.PublicKey = File.ReadAllText (key_pair [1]);
                    FolderListChanged (); // FIXME: Hacky way to update status icon menu to show the key

                }).Start ();

            } else {
                new Thread (() => {
                    StartupInviteScan ();
                    CheckRepositories ();
                    RepositoriesLoaded = true;
                    UpdateState ();

                }).Start ();
            }
        }


        public void ShowSetupWindow (PageType page_type)
        {
            ShowSetupWindowEvent (page_type);
        }
        
        
        public void ShowAboutWindow ()
        {
            ShowAboutWindowEvent ();
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

        
        private void CheckRepositories ()
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
                            RemoveRepository (folder_path);
                            Config.RenameFolder (identifier, folder_name);
                            
                            string new_folder_path = Path.Combine (path, folder_name);
                            AddRepository (new_folder_path);
                            
                            SparkleLogger.LogInfo ("Controller",
                                "Renamed folder with identifier " + identifier + " to '" + folder_name + "'");
                        }
                    }
                }
                
                // Remove any deleted folders
                foreach (string folder_name in Config.Folders) {
                    string folder_path = new SparkleFolder (folder_name).FullPath;
                    
                    if (!Directory.Exists (folder_path)) {
                        Config.RemoveFolder (folder_name);
                        RemoveRepository (folder_path);
                        
                        SparkleLogger.LogInfo ("Controller", "Removed folder '" + folder_name + "' from config");
                        
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


        private void AddRepository (string folder_path)
        {
            SparkleRepoBase repo = null;
            string folder_name   = Path.GetFileName (folder_path);
            string backend       = Config.GetBackendForFolder (folder_name);

            try {
                repo = (SparkleRepoBase) Activator.CreateInstance (
                    Type.GetType ("SparkleLib." + backend + ".SparkleRepo, SparkleLib." + backend),
                    new object [] { folder_path, Config });

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller", "Failed to load backend '" + backend + "' for '" + folder_name + "': ", e);
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

                foreach (SparkleRepoBase rep in Repositories) {
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

            repo.NewChangeSet += delegate (SparkleChangeSet change_set) {
                if (AvatarsEnabled)
                    change_set.User.AvatarFilePath = SparkleAvatars.GetAvatar (change_set.User.Email, 48, Config.FullPath);

                NotificationRaised (change_set);
            };

            repo.ConflictResolved += delegate {
                AlertNotificationRaised ("Conflict happened", "Don't worry, we've made a copy of each conflicting file.");
            };

            this.repositories.Add (repo);
            this.repositories.Sort ((x, y) => string.Compare (x.Name, y.Name));
            repo.Initialize ();
        }


        private void RemoveRepository (string folder_path)
        {
            foreach (SparkleRepoBase repo in this.repositories) {
                if (repo.LocalPath.Equals (folder_path)) {
                    this.repositories.Remove (repo);
                    repo.Dispose ();
                    return;
                }
            }
        }


        private void OnFolderActivity (object o, FileSystemEventArgs args)
        {
            if (args != null && args.FullPath.EndsWith (".xml") &&
                args.ChangeType == WatcherChangeTypes.Created) {

                HandleInvite (args);
                return;

            }/* else { FIXME: on the fly folder removal doesn't always work. disabling for now
                Thread.Sleep (1000);

                if (Directory.Exists (args.FullPath) && args.ChangeType == WatcherChangeTypes.Created)
                    return;

                CheckRepositories ();
            }*/
        }


        private void StartupInviteScan ()
        {
            foreach (string invite in Directory.GetFiles (FoldersPath, "*.xml")) {
                HandleInvite (invite);
            }
        }


        private void HandleInvite (FileSystemEventArgs args)
        {
            HandleInvite (args.FullPath);
        }


        private void HandleInvite (string path)
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
        private void UpdateState ()
        {
            bool has_unsynced_repos = false;
            bool has_syncing_repos  = false;

            foreach (SparkleRepoBase repo in Repositories) {
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
                backend = SparkleFetcherBase.GetBackend (info.Address); 
 
            info.TargetDirectory  = Path.Combine (tmp_path, canonical_name);

            try {
                this.fetcher = (SparkleFetcherBase) Activator.CreateInstance (
                    Type.GetType ("SparkleLib." + backend + ".SparkleFetcher, SparkleLib." + backend), info);

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller",
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
            canonical_name = canonical_name.Replace ("_", " ");
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
                SparkleLogger.LogInfo ("Controller", "Error moving directory, trying again...", e);

                try {
                    ClearDirectoryAttributes (this.fetcher.TargetFolder);
                    Directory.Move (this.fetcher.TargetFolder, target_folder_path);

                } catch (Exception x) {
                    SparkleLogger.LogInfo ("Controller", "Error moving directory", x);
                    
                    this.fetcher.Dispose ();
                    this.fetcher = null;
                    this.watcher.EnableRaisingEvents = true;
                    return;
                }
            }

            string backend = SparkleFetcherBase.GetBackend (this.fetcher.RemoteUrl.ToString ());

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
            foreach (SparkleRepoBase repo in Repositories)
                repo.Dispose ();
            
            Environment.Exit (0);
        }


        private void ClearDirectoryAttributes (string path)
        {
            if (!Directory.Exists (path))
                return;
            
            string [] folders = Directory.GetDirectories (path);
            
            foreach (string folder in folders)
                ClearDirectoryAttributes (folder);
            
            string [] files = Directory.GetFiles(path);
            
            foreach (string file in files)
                if (!IsSymlink (file))
                    File.SetAttributes (file, FileAttributes.Normal);
        }
        
        
        private bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }
    }
}
