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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;

using SparkleLib;

namespace SparkleShare {

    public abstract class SparkleControllerBase {

        public SparkleRepoBase [] Repositories {
            get {
                lock (this.repo_lock)
                    return this.repositories.GetRange (0, this.repositories.Count).ToArray ();
            }
        }

        public bool RepositoriesLoaded { get; private set;}

        private List<SparkleRepoBase> repositories = new List<SparkleRepoBase> ();
        public string FoldersPath { get; private set; }

        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";


        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };
        public delegate void ShowSetupWindowEventHandler (PageType page_type);

        public event Action ShowAboutWindowEvent = delegate { };
        public event Action ShowEventLogWindowEvent = delegate { };

        public event FolderFetchedEventHandler FolderFetched = delegate { };
        public delegate void FolderFetchedEventHandler (string remote_url, string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError = delegate { };
        public delegate void FolderFetchErrorHandler (string remote_url, string [] errors);
        
        public event FolderFetchingHandler FolderFetching = delegate { };
        public delegate void FolderFetchingHandler (double percentage);
        
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
            get {
                return this.config.User.Email.Equals ("Unknown");
            }
        }

        public List<string> Folders {
            get {
                List<string> folders = this.config.Folders;
                folders.Sort ();

                return folders;
            }
        }

        public List<string> UnsyncedFolders {
            get {
                List<string> unsynced_folders = new List<string> ();

                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.HasUnsyncedChanges)
                        unsynced_folders.Add (repo.Name);
                }

                return unsynced_folders;
            }
        }

        public SparkleUser CurrentUser {
            get {
                return this.config.User;
            }

            set {
                this.config.User = value;
            }
        }

        public bool NotificationsEnabled {
            get {
                string notifications_enabled = this.config.GetConfigOption ("notifications");

                if (string.IsNullOrEmpty (notifications_enabled)) {
                    this.config.SetConfigOption ("notifications", bool.TrueString);
                    return true;

                } else {
                    return notifications_enabled.Equals (bool.TrueString);
                }
            }
        }


        public abstract string EventLogHTML { get; }
        public abstract string DayEntryHTML { get; }
        public abstract string EventEntryHTML { get; }

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


        private SparkleConfig config;
        private SparkleFetcherBase fetcher;
        private FileSystemWatcher watcher;
        private Object repo_lock        = new Object ();
        private Object check_repos_lock = new Object ();


        public SparkleControllerBase ()
        {
            string app_data_path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
            string config_path   = Path.Combine (app_data_path, "sparkleshare");
            
            this.config                 = new SparkleConfig (config_path, "config.xml");
            SparkleConfig.DefaultConfig = this.config;
            FoldersPath                 = this.config.FoldersPath;
        }


        public virtual void Initialize ()
        {
            SparklePlugin.PluginsPath = PluginsPath;
            InstallProtocolHandler ();

            // Create the SparkleShare folder and add it to the bookmarks
            if (CreateSparkleShareFolder ())
                AddToBookmarks ();

            if (FirstRun) {
                this.config.SetConfigOption ("notifications", bool.TrueString);

            } else {
                string keys_path = Path.GetDirectoryName (this.config.FullPath);
                string key_file_path = "";

                foreach (string file_name in Directory.GetFiles (keys_path)) {
                    if (file_name.EndsWith (".key")) {
                        key_file_path = Path.Combine (keys_path, file_name);
                        SparkleKeys.ImportPrivateKey (key_file_path);

                        break;
                    }
                }

                if (!string.IsNullOrEmpty (key_file_path)) {
                    string public_key_file_path = key_file_path + ".pub";
                    string link_code_file_path  = Path.Combine (FoldersPath, CurrentUser.Name + "'s link code.txt");

                    // Create an easily accessible copy of the public
                    // key in the user's SparkleShare folder
                    if (File.Exists (public_key_file_path) && !File.Exists (link_code_file_path))
                        File.Copy (public_key_file_path, link_code_file_path, true);

                    CurrentUser.PublicKey = File.ReadAllText (public_key_file_path);
                }

                SparkleKeys.ListPrivateKeys ();
            }

            // Watch the SparkleShare folder
            this.watcher = new FileSystemWatcher () {
                Filter                = "*",
                IncludeSubdirectories = false,
                Path                  = FoldersPath
            };

            watcher.Deleted += OnFolderActivity;
            watcher.Created += OnFolderActivity;
            watcher.Renamed += OnFolderActivity;

            watcher.EnableRaisingEvents = true;
        }


        public void UIHasLoaded ()
        {
            if (FirstRun) {
                ShowSetupWindow (PageType.Setup);

            } else {
                new Thread (() => {
                    CheckRepositories ();
                    RepositoriesLoaded = true;
                    FolderListChanged ();

                }).Start ();
            }
        }


        private void AddRepository (string folder_path)
        {
            SparkleRepoBase repo = null;
            string folder_name   = Path.GetFileName (folder_path);
            string backend       = this.config.GetBackendForFolder (folder_name);

            try {
                repo = (SparkleRepoBase) Activator.CreateInstance (
                    Type.GetType ("SparkleLib." + backend + ".SparkleRepo, SparkleLib." + backend),
                        new object [] { folder_path, this.config }
                );

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller",
                    "Failed to load '" + backend + "' backend for '" + folder_name + "': " + e.Message);

                return;
            }

            repo.ChangesDetected += delegate {
                UpdateState ();
            };

            repo.SyncStatusChanged += delegate (SyncStatus status) {
                if (status == SyncStatus.Idle) {
                    ProgressPercentage = 0.0;
                    ProgressSpeed      = "";
                }

                UpdateState ();
            };

            repo.ProgressChanged += delegate (double percentage, string speed) {
                ProgressPercentage = percentage;
                ProgressSpeed      = speed;

                UpdateState ();
            };

            repo.NewChangeSet += delegate (SparkleChangeSet change_set) {
                if (NotificationsEnabled)
                    NotificationRaised (change_set);
            };

            repo.ConflictResolved += delegate {
                if (NotificationsEnabled)
                    AlertNotificationRaised ("Conflict detected",
                        "Don't worry, SparkleShare made a copy of each conflicting file.");
            };

            this.repositories.Add (repo);
            repo.Initialize ();
        }


        private void RemoveRepository (string folder_path)
        {
            for (int i = 0; i < this.repositories.Count; i++) {
                SparkleRepoBase repo = this.repositories [i];

                if (repo.LocalPath.Equals (folder_path)) {
                    repo.Dispose ();
                    this.repositories.Remove (repo);
                    repo = null;

                    return;
                }
            }
        }


        private void CheckRepositories ()
        {
            lock (this.check_repos_lock) {
                string path = this.config.FoldersPath;

                foreach (string folder_path in Directory.GetDirectories (path)) {
                    string folder_name = Path.GetFileName (folder_path);

                    if (folder_name.Equals (".tmp"))
                        continue;

                    if (this.config.GetIdentifierForFolder (folder_name) == null) {
                        string identifier_file_path = Path.Combine (folder_path, ".sparkleshare");

                        if (!File.Exists (identifier_file_path))
                            continue;

                        string identifier = File.ReadAllText (identifier_file_path).Trim ();

                        if (this.config.IdentifierExists (identifier)) {
                            RemoveRepository (folder_path);
                            this.config.RenameFolder (identifier, folder_name);

                            string new_folder_path = Path.Combine (path, folder_name);
                            AddRepository (new_folder_path);

                            SparkleLogger.LogInfo ("Controller",
                                "Renamed folder with identifier " + identifier + " to '" + folder_name + "'");
                        }
                    }
                }

                foreach (string folder_name in this.config.Folders) {
                    string folder_path = new SparkleFolder (folder_name).FullPath;

                    if (!Directory.Exists (folder_path)) {
                        this.config.RemoveFolder (folder_name);
                        RemoveRepository (folder_path);

                        SparkleLogger.LogInfo ("Controller",
                            "Removed folder '" + folder_name + "' from config");

                    } else {
                        AddRepository (folder_path);
                    }
                }

                FolderListChanged ();
            }
        }


        // Fires events for the current syncing state
        private void UpdateState ()
        {
            bool has_unsynced_repos = false;

            foreach (SparkleRepoBase repo in Repositories) {
                if (repo.Status == SyncStatus.SyncDown || repo.Status == SyncStatus.SyncUp || repo.IsBuffering) {
                    OnSyncing ();
                    return;

                } else if (repo.HasUnsyncedChanges) {
                    has_unsynced_repos = true;
                }
            }

            if (has_unsynced_repos)
                OnError ();
            else
                OnIdle ();
        }


        private void ClearFolderAttributes (string path)
        {
            if (!Directory.Exists (path))
                return;

            string [] folders = Directory.GetDirectories (path);

            foreach (string folder in folders)
                ClearFolderAttributes (folder);

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


        public void OnFolderActivity (object o, FileSystemEventArgs args)
        {
            if (args != null && args.FullPath.EndsWith (".xml") &&
                args.ChangeType == WatcherChangeTypes.Created) {

                HandleInvite (args);
                return;

            } else {
                if (Directory.Exists (args.FullPath) && args.ChangeType == WatcherChangeTypes.Created)
                    return;

                CheckRepositories ();
            }
        }


        public void HandleInvite (FileSystemEventArgs args)
        {
            if (this.fetcher != null &&
                this.fetcher.IsActive) {

                AlertNotificationRaised ("SparkleShare Setup seems busy", "Please wait for it to finish");

            } else {
                SparkleInvite invite = new SparkleInvite (args.FullPath);

                // It may be that the invite we received a path to isn't
                // fully downloaded yet, so we try to read it several times
                int tries = 0;
                while (!invite.IsValid) {
                    Thread.Sleep (100);
                    invite = new SparkleInvite (args.FullPath);
                    tries++;

                    if (tries > 10) {
                        AlertNotificationRaised ("Oh noes!", "This invite seems screwed up...");
                        break;
                    }
                }

                if (invite.IsValid)
                    InviteReceived (invite);

                File.Delete (args.FullPath);
            }
        }


        public void StartFetcher (string address, string required_fingerprint,
            string remote_path, string announcements_url, bool fetch_prior_history)
        {
            if (announcements_url != null)
                announcements_url = announcements_url.Trim ();

            string tmp_path = this.config.TmpPath;

			if (!Directory.Exists (tmp_path)) {
                Directory.CreateDirectory (tmp_path);
                File.SetAttributes (tmp_path, File.GetAttributes (tmp_path) | FileAttributes.Hidden);
            }

            string canonical_name = Path.GetFileNameWithoutExtension (remote_path);
            string tmp_folder     = Path.Combine (tmp_path, canonical_name);
            string backend        = SparkleFetcherBase.GetBackend (remote_path);

            try {
                this.fetcher = (SparkleFetcherBase) Activator.CreateInstance (
                    Type.GetType ("SparkleLib." + backend + ".SparkleFetcher, SparkleLib." + backend),
                        address, required_fingerprint, remote_path, tmp_folder, fetch_prior_history
                );

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller",
                    "Failed to load '" + backend + "' backend for '" + canonical_name + "' " + e.Message);

                FolderFetchError (Path.Combine (address, remote_path).Replace (@"\", "/"),
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
            
            this.fetcher.ProgressChanged += delegate (double percentage) {
                FolderFetching (percentage);
            };

            this.fetcher.Start ();
        }


        public void StopFetcher ()
        {
            this.fetcher.Stop ();

            if (Directory.Exists (this.fetcher.TargetFolder)) {
                try {
                    Directory.Delete (this.fetcher.TargetFolder, true);
                    SparkleLogger.LogInfo ("Controller", "Deleted " + this.fetcher.TargetFolder);

                } catch (Exception e) {
                    SparkleLogger.LogInfo ("Controller",
                        "Failed to delete " + this.fetcher.TargetFolder + ": " + e.Message);
                }
            }

            this.fetcher.Dispose ();
            this.fetcher = null;
        }


        public void FinishFetcher (string password)
        {
            this.fetcher.EnableFetchedRepoCrypto (password);

            this.watcher.EnableRaisingEvents = false;
            FinishFetcher ();
            this.watcher.EnableRaisingEvents = true;
        }


        public void FinishFetcher ()
        {
            this.fetcher.Complete ();
            string canonical_name = Path.GetFileNameWithoutExtension (this.fetcher.RemoteUrl.AbsolutePath);

            bool target_folder_exists = Directory.Exists (
                Path.Combine (this.config.FoldersPath, canonical_name));

            // Add a numbered suffix to the name if a folder with the same name
            // already exists. Example: "Folder (2)"
            int suffix = 1;
            while (target_folder_exists) {
                suffix++;
                target_folder_exists = Directory.Exists (
                    Path.Combine (this.config.FoldersPath, canonical_name + " (" + suffix + ")"));
            }

            string target_folder_name = canonical_name;

            if (suffix > 1)
                target_folder_name += " (" + suffix + ")";

            string target_folder_path = Path.Combine (this.config.FoldersPath, target_folder_name);

            try {
                ClearFolderAttributes (this.fetcher.TargetFolder);
                Directory.Move (this.fetcher.TargetFolder, target_folder_path);

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller", "Error moving directory: " + e.Message);
                return;
            }

            string backend = SparkleFetcherBase.GetBackend (this.fetcher.RemoteUrl.AbsolutePath);

            this.config.AddFolder (target_folder_name, this.fetcher.Identifier,
                this.fetcher.RemoteUrl.ToString (), backend);

            FolderFetched (this.fetcher.RemoteUrl.ToString (), this.fetcher.Warnings.ToArray ());

            /* TODO
            if (!string.IsNullOrEmpty (announcements_url)) {
                this.config.SetFolderOptionalAttribute (
                    target_folder_name, "announcements_url", announcements_url);
            */

            AddRepository (target_folder_path);
            FolderListChanged ();

            this.fetcher.Dispose ();
            this.fetcher = null;
        }


        public bool CheckPassword (string password)
        {
            return this.fetcher.IsFetchedRepoPasswordCorrect (password);
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
            OpenFolder (this.config.FoldersPath);
        }


        public void OpenSparkleShareFolder (string name)
        {
            OpenFolder (new SparkleFolder (name).FullPath);
        }


        public void ToggleNotifications () {
            bool notifications_enabled = this.config.GetConfigOption ("notifications").Equals (bool.TrueString);
            this.config.SetConfigOption ("notifications", (!notifications_enabled).ToString ());
        }


        private List<string> skipped_avatars = new List<string> ();

        public string GetAvatar (string email, int size)
        {
            ServicePointManager.ServerCertificateValidationCallback = GetAvatarValidationCallBack;

            string fetch_avatars_option = this.config.GetConfigOption ("fetch_avatars");

            if (fetch_avatars_option != null &&
                fetch_avatars_option.Equals (bool.FalseString)) {

                return null;
            }

            email = email.ToLower ();

            if (this.skipped_avatars.Contains (email))
                return null;

            string avatars_path = new string [] { Path.GetDirectoryName (this.config.FullPath),
                "avatars", size + "x" + size }.Combine ();

            string avatar_file_path = Path.Combine (avatars_path, email.MD5 () + ".png");

            if (File.Exists (avatar_file_path)) {
                if (new FileInfo (avatar_file_path).CreationTime < DateTime.Now.AddDays (-1))
                    File.Delete (avatar_file_path);
                else
                    return avatar_file_path;
            }

            WebClient client = new WebClient ();
            string url =  "https://gravatar.com/avatar/" + email.MD5 () + ".png?s=" + size + "&d=404";

            try {
                byte [] buffer = client.DownloadData (url);

                if (buffer.Length > 255) {
                    if (!Directory.Exists (avatars_path)) {
                        Directory.CreateDirectory (avatars_path);
                        SparkleLogger.LogInfo ("Controller", "Created '" + avatars_path + "'");
                    }

                    File.WriteAllBytes (avatar_file_path, buffer);
                    SparkleLogger.LogInfo ("Controller", "Fetched " + size + "x" + size + " avatar for " + email);

                    return avatar_file_path;

                } else {
                    return null;
                }

            } catch (WebException e) {
                SparkleLogger.LogInfo ("Controller", "Error fetching avatar for " + email + ": " + e.Message);
                skipped_avatars.Add (email);

                return null;
            }
        }


        private bool GetAvatarValidationCallBack (Object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors errors)
        {
            X509Certificate2 certificate2 = new X509Certificate2 (certificate.GetRawCertData ());

            // On some systems (mostly Linux) we can't assume the needed certificates are
            // available, so we have to check the certificate's SHA-1 fingerprint manually.
            //
            // Obtained from https://www.gravatar.com/ on Aug 18 2012 and
            // expires on Oct 24 2015.
            string gravatar_cert_fingerprint = "217ACB08C0A1ACC23A21B6ECDE82CD45E14DEC19";

            if (certificate2.Thumbprint.Equals (gravatar_cert_fingerprint)) {
                return true;
            
            } else {
                SparkleLogger.LogInfo ("Controller",
                    "Not connecting to https://www.gravatar.com/ due to invalid certificate");

                return false;
            }
        }


        public string AssignAvatar (string s)
        {
            string hash    = "0" + s.MD5 ().Substring (0, 8);
            string numbers = Regex.Replace (hash, "[a-z]", "");
            int number     = int.Parse (numbers);
            string letters = "abcdefghijklmnopqrstuvwxyz";

            return "avatar-" + letters [(number % 11)] + ".png";
        }


        // Format a file size nicely with small caps.
        // Example: 1048576 becomes "1 ᴍʙ"
        public string FormatSize (double byte_count)
        {
            if (byte_count >= 1099511627776)
                return String.Format ("{0:##.##} ᴛʙ", Math.Round (byte_count / 1099511627776, 1));
            else if (byte_count >= 1073741824)
                return String.Format ("{0:##.##} ɢʙ", Math.Round (byte_count / 1073741824, 1));
            else if (byte_count >= 1048576)
                return String.Format ("{0:##.##} ᴍʙ", Math.Round (byte_count / 1048576, 0));
            else if (byte_count >= 1024)
                return String.Format ("{0:##.##} ᴋʙ", Math.Round (byte_count / 1024, 0));
            else
                return byte_count.ToString () + " bytes";
        }


        public virtual void Quit ()
        {
            foreach (SparkleRepoBase repo in Repositories)
                repo.Dispose ();

            Environment.Exit (0);
        }
    }
}
