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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using SparkleLib;

namespace SparkleShare {

    public abstract class SparkleControllerBase {

        public SparkleRepoBase [] Repositories {
            get {
                lock (this.repo_lock) {
                    SparkleRepoBase [] repositories =
                        this.repositories.GetRange (0, this.repositories.Count).ToArray ();

                    return repositories;
                }
            }
        }

        public bool RepositoriesLoaded { get; private set;}

        public List<SparkleRepoBase> repositories = new List<SparkleRepoBase> ();
        public readonly string SparklePath = SparkleConfig.DefaultConfig.FoldersPath;

        public double ProgressPercentage = 0.0;
        public string ProgressSpeed      = "";


        public event ShowSetupWindowEventHandler ShowSetupWindowEvent;
        public delegate void ShowSetupWindowEventHandler (PageType page_type);

        public event ShowAboutWindowEventHandler ShowAboutWindowEvent;
        public delegate void ShowAboutWindowEventHandler ();

        public event ShowEventLogWindowEventHandler ShowEventLogWindowEvent;
        public delegate void ShowEventLogWindowEventHandler ();


        public event FolderFetchedEventHandler FolderFetched;
        public delegate void FolderFetchedEventHandler (string remote_url, string [] warnings);
        
        public event FolderFetchErrorHandler FolderFetchError;
        public delegate void FolderFetchErrorHandler (string remote_url, string [] errors);
        
        public event FolderFetchingHandler FolderFetching;
        public delegate void FolderFetchingHandler (double percentage);
        
        public event FolderListChangedHandler FolderListChanged;
        public delegate void FolderListChangedHandler ();

        public event OnIdleHandler OnIdle;
        public delegate void OnIdleHandler ();

        public event OnSyncingHandler OnSyncing;
        public delegate void OnSyncingHandler ();

        public event OnErrorHandler OnError;
        public delegate void OnErrorHandler ();

        public event InviteReceivedHandler InviteReceived;
        public delegate void InviteReceivedHandler (SparkleInvite invite);

        public event NotificationRaisedEventHandler NotificationRaised;
        public delegate void NotificationRaisedEventHandler (SparkleChangeSet change_set);

        public event AlertNotificationRaisedEventHandler AlertNotificationRaised;
        public delegate void AlertNotificationRaisedEventHandler (string title, string message);


        public bool FirstRun {
            get {
                return SparkleConfig.DefaultConfig.User.Email.Equals ("Unknown");
            }
        }

        public List<string> Folders {
            get {
                List<string> folders = SparkleConfig.DefaultConfig.Folders;
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
                return SparkleConfig.DefaultConfig.User;
            }

            set {
                SparkleConfig.DefaultConfig.User = value;
            }
        }

        public bool NotificationsEnabled {
            get {
                string notifications_enabled = SparkleConfig.DefaultConfig.GetConfigOption ("notifications");

                if (string.IsNullOrEmpty (notifications_enabled)) {
                    SparkleConfig.DefaultConfig.SetConfigOption ("notifications", bool.TrueString);
                    return true;

                } else {
                    return notifications_enabled.Equals (bool.TrueString);
                }
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


        private SparkleFetcherBase fetcher;
        private Object repo_lock            = new Object ();
        private Object delete_watcher_lock  = new Object ();


        // Short alias for the translations
        public static string _ (string s)
        {
            return Program._(s);
        }


        public SparkleControllerBase ()
        {
        }


        public virtual void Initialize ()
        {
            SparklePlugin.PluginsPath = PluginsPath;
            InstallProtocolHandler ();

            // Create the SparkleShare folder and add it to the bookmarks
            if (CreateSparkleShareFolder ())
                AddToBookmarks ();

            if (FirstRun)
                SparkleConfig.DefaultConfig.SetConfigOption ("notifications", bool.TrueString);
            else
                ImportPrivateKey ();

            // Watch the SparkleShare folder
            FileSystemWatcher watcher = new FileSystemWatcher (SparkleConfig.DefaultConfig.FoldersPath) {
                IncludeSubdirectories = false,
                EnableRaisingEvents   = true,
                Filter                = "*"
            };

            watcher.Deleted += delegate (object o, FileSystemEventArgs args) {
                lock (this.delete_watcher_lock) {
                    foreach (string folder_name in SparkleConfig.DefaultConfig.Folders) {
                        string folder_path = new SparkleFolder (folder_name).FullPath;

                        if (!Directory.Exists (folder_path)) {
                            SparkleConfig.DefaultConfig.RemoveFolder (folder_name);
                            RemoveRepository (folder_path);
                        }
                    }

                    if (FolderListChanged != null)
                        FolderListChanged ();
                }
            };

            watcher.Created += delegate (object o, FileSystemEventArgs args) {
                if (!args.FullPath.EndsWith (".xml"))
                    return;

                if (this.fetcher != null &&
                    this.fetcher.IsActive) {

                    if (AlertNotificationRaised != null)
                        AlertNotificationRaised ("SparkleShare Setup seems busy",
                            "Please wait for it to finish");

                } else {
                    if (InviteReceived != null) {
                        SparkleInvite invite = new SparkleInvite (args.FullPath);

                        // It may be that the invite we received a path to isn't
                        // fully downloaded yet, so we try to read it several times
                        int tries = 0;
                        while (!invite.IsValid) {
                            Thread.Sleep (1 * 250);
                            invite = new SparkleInvite (args.FullPath);
                            tries++;
                            if (tries > 20)
                                break;
                        }

                        if (invite.IsValid) {
                            InviteReceived (invite);

                        } else {
                            invite = null;

                            if (AlertNotificationRaised != null)
                                AlertNotificationRaised ("Oh noes!",
                                    "This invite seems screwed up...");
                        }

                        File.Delete (args.FullPath);
                    }
                }
            };
        }


        public void UIHasLoaded ()
        {
            if (FirstRun)
                ShowSetupWindow (PageType.Setup);
            else
                new Thread (new ThreadStart (PopulateRepositories)).Start ();
        }


        public void ShowSetupWindow (PageType page_type)
        {
            if (ShowSetupWindowEvent != null)
                ShowSetupWindowEvent (page_type);
        }


        public void ShowAboutWindow ()
        {
            if (ShowAboutWindowEvent != null)
                ShowAboutWindowEvent ();
        }


        public void ShowEventLogWindow ()
        {
            if (ShowEventLogWindowEvent != null)
                ShowEventLogWindowEvent ();
        }


        public List<SparkleChangeSet> GetLog ()
        {
            List<SparkleChangeSet> list = new List<SparkleChangeSet> ();

            foreach (SparkleRepoBase repo in Repositories) {
                List<SparkleChangeSet> change_sets = repo.ChangeSets;

                if (change_sets != null)
                    list.AddRange (change_sets);
                else
                    SparkleHelpers.DebugInfo ("Log", "Could not create log for " + repo.Name);
            }

            list.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            list.Reverse ();

            if (list.Count > 100)
                return list.GetRange (0, 100);
            else
                return list.GetRange (0, list.Count);
        }


        public List<SparkleChangeSet> GetLog (string name)
        {
            if (name == null)
                return GetLog ();

            string path  = new SparkleFolder (name).FullPath;

            lock (this.repo_lock) {
                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.LocalPath.Equals (path))
                        return repo.ChangeSets;
                }
            }

            return null;
        }
        
        
        public abstract string EventLogHTML { get; }
        public abstract string DayEntryHTML { get; }
        public abstract string EventEntryHTML { get; }

        public string GetHTMLLog (List<SparkleChangeSet> change_sets)
        {
            List <ActivityDay> activity_days = new List <ActivityDay> ();

            change_sets.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            change_sets.Reverse ();

            if (change_sets.Count == 0)
                return null;

            foreach (SparkleChangeSet change_set in change_sets) {
                bool change_set_inserted = false;
                foreach (ActivityDay stored_activity_day in activity_days) {
                    if (stored_activity_day.Date.Year  == change_set.Timestamp.Year &&
                        stored_activity_day.Date.Month == change_set.Timestamp.Month &&
                        stored_activity_day.Date.Day   == change_set.Timestamp.Day) {

                        stored_activity_day.Add (change_set);

                        change_set_inserted = true;
                        break;
                    }
                }

                if (!change_set_inserted) {
                    ActivityDay activity_day = new ActivityDay (change_set.Timestamp);
                    activity_day.Add (change_set);
                    activity_days.Add (activity_day);
                }
            }

            string event_log_html   = EventLogHTML;
            string day_entry_html   = DayEntryHTML;
            string event_entry_html = EventEntryHTML;
            string event_log        = "";

            foreach (ActivityDay activity_day in activity_days) {
                string event_entries = "";

                foreach (SparkleChangeSet change_set in activity_day) {
                    string event_entry = "<dl>";

                    foreach (SparkleChange change in change_set.Changes) {
                        if (change.Type != SparkleChangeType.Moved) {

                            event_entry += "<dd class='document " + change.Type.ToString ().ToLower () + "'>";
                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
                            event_entry += "</dd>";

                        } else {

                    		event_entry += "<dd class='document moved'>";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
                            event_entry += "<br>";
                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.MovedToPath);
                            event_entry += "</dd>";

                        }
                    }

                    string change_set_avatar = GetAvatar (change_set.User.Email, 48);
                    
					if (change_set_avatar != null) {
                        change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
					
				    } else {
                        change_set_avatar = "file://<!-- $pixmaps-path -->/" +
							AssignAvatar (change_set.User.Email);
					}
					
                    event_entry += "</dl>";

                    string timestamp = change_set.Timestamp.ToString ("H:mm");

                    if (!change_set.FirstTimestamp.Equals (new DateTime ()) &&
                        !change_set.Timestamp.ToString ("H:mm").Equals (change_set.FirstTimestamp.ToString ("H:mm")))
                        timestamp = change_set.FirstTimestamp.ToString ("H:mm") + " – " + timestamp;

                    event_entries += event_entry_html.Replace ("<!-- $event-entry-content -->", event_entry)
                        .Replace ("<!-- $event-user-name -->", change_set.User.Name)
                        .Replace ("<!-- $event-avatar-url -->", change_set_avatar)
                        .Replace ("<!-- $event-folder -->", change_set.Folder.Name)
                        .Replace ("<!-- $event-url -->", change_set.RemoteUrl.ToString ())
                        .Replace ("<!-- $event-revision -->", change_set.Revision);
                }

                string day_entry   = "";
                DateTime today     = DateTime.Now;
                DateTime yesterday = DateTime.Now.AddDays (-1);

                if (today.Day   == activity_day.Date.Day &&
                    today.Month == activity_day.Date.Month &&
                    today.Year  == activity_day.Date.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='today' name='" + activity_day.Date.ToString (_("dddd, MMMM d")) + "'>"
                        + _("Today") + "</span>");

                } else if (yesterday.Day   == activity_day.Date.Day &&
                           yesterday.Month == activity_day.Date.Month &&
                           yesterday.Year  == activity_day.Date.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='yesterday' name='" + activity_day.Date.ToString (_("dddd, MMMM d")) + "'>"
                        + _("Yesterday") + "</span>");

                } else {
                    if (activity_day.Date.Year != DateTime.Now.Year) {

                        // TRANSLATORS: This is the date in the event logs
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.Date.ToString (_("dddd, MMMM d, yyyy")));

                    } else {

                        // TRANSLATORS: This is the date in the event logs, without the year
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.Date.ToString (_("dddd, MMMM d")));
                    }
                }

                event_log += day_entry.Replace ("<!-- $day-entry-content -->", event_entries);
            }


            int midnight = (int) (DateTime.Today.AddDays (1) - new DateTime (1970, 1, 1)).TotalSeconds;

            string html = event_log_html.Replace ("<!-- $event-log-content -->", event_log)
                .Replace ("<!-- $midnight -->", midnight.ToString ());

            return html;
        }


        // Fires events for the current syncing state
        public void UpdateState ()
        {
            bool has_syncing_repos  = false;
            bool has_unsynced_repos = false;

            foreach (SparkleRepoBase repo in Repositories) {
                if (repo.Status == SyncStatus.SyncDown ||
                    repo.Status == SyncStatus.SyncUp   ||
                    repo.IsBuffering) {

                    has_syncing_repos = true;

                } else if (repo.HasUnsyncedChanges) {
                    has_unsynced_repos = true;
                }
            }

            if (has_syncing_repos) {
                if (OnSyncing != null)
                    OnSyncing ();

            } else if (has_unsynced_repos) {
                if (OnError != null)
                    OnError ();

            } else {
                if (OnIdle != null)
                    OnIdle ();
            }
        }


        // Adds a repository to the list of repositories
        private void AddRepository (string folder_path)
        {
            SparkleRepoBase repo = null;
            string folder_name   = Path.GetFileName (folder_path);
            string backend       = SparkleConfig.DefaultConfig.GetBackendForFolder (folder_name);

            try {
                repo = (SparkleRepoBase) Activator.CreateInstance (
                    Type.GetType ("SparkleLib." + backend + ".SparkleRepo, SparkleLib." + backend),
                        folder_path
                );

            } catch {
                SparkleHelpers.DebugInfo ("Controller",
                    "Failed to load \"" + backend + "\" backend for \"" + folder_name + "\"");

                return;
            }


            repo.NewChangeSet += delegate (SparkleChangeSet change_set) {
                if (NotificationRaised != null)
                    NotificationRaised (change_set);
            };

            repo.ConflictResolved += delegate {
                if (AlertNotificationRaised != null)
                    AlertNotificationRaised ("Conflict detected",
                        "Don't worry, SparkleShare made a copy of each conflicting file.");
            };

            repo.SyncStatusChanged += delegate (SyncStatus status) {
                if (status == SyncStatus.Idle) {
                    ProgressPercentage = 0.0;
                    ProgressSpeed      = "";
                }

                if (status == SyncStatus.Idle     ||
                    status == SyncStatus.SyncUp   ||
                    status == SyncStatus.SyncDown ||
                    status == SyncStatus.Error) {

                    UpdateState ();
                }
            };

            repo.ProgressChanged += delegate (double percentage, string speed) {
                ProgressPercentage = percentage;
                ProgressSpeed      = speed;

                UpdateState ();
            };

            repo.ChangesDetected += delegate {
                UpdateState ();
            };

            this.repositories.Add (repo);
            repo.Initialize ();
        }


        // Removes a repository from the list of repositories and
        // updates the statusicon menu
        private void RemoveRepository (string folder_path)
        {
            string folder_name = Path.GetFileName (folder_path);

            for (int i = 0; i < Repositories.Length; i++) {
                SparkleRepoBase repo = Repositories [i];

                if (repo.Name.Equals (folder_name)) {
                    repo.Dispose ();

                    lock (this.repo_lock) {
                        this.repositories.Remove (repo);
                    }

                    repo = null;
                    break;
                }
            }

        }


        // Updates the list of repositories with all the
        // folders in the SparkleShare folder
        private void PopulateRepositories ()
        {
            lock (this.repo_lock) {
                foreach (string folder_name in SparkleConfig.DefaultConfig.Folders) {
                    string folder_path = new SparkleFolder (folder_name).FullPath;

                    if (Directory.Exists (folder_path))
                        AddRepository (folder_path);
                    else
                        SparkleConfig.DefaultConfig.RemoveFolder (folder_name);
                }
            }

            RepositoriesLoaded = true;

            if (FolderListChanged != null)
                FolderListChanged ();
        }


        public void ToggleNotifications () {
            bool notifications_enabled =
                SparkleConfig.DefaultConfig.GetConfigOption ("notifications")
                    .Equals (bool.TrueString);

            if (notifications_enabled)
                SparkleConfig.DefaultConfig.SetConfigOption ("notifications", bool.FalseString);
            else
                SparkleConfig.DefaultConfig.SetConfigOption ("notifications", bool.TrueString);
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


        public void OpenSparkleShareFolder ()
        {
            OpenFolder (SparkleConfig.DefaultConfig.FoldersPath);
        }


        public void OpenSparkleShareFolder (string name)
        {
            OpenFolder (new SparkleFolder (name).FullPath);
        }

        
        // Adds the user's SparkleShare key to the ssh-agent,
        // so all activity is done with this key
        public void ImportPrivateKey ()
        {
            string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
            string key_file_name = "sparkleshare." + CurrentUser.Email + ".key";
            string key_file_path = Path.Combine (keys_path, key_file_name);

            if (!File.Exists (key_file_path)) {
                foreach (string file_name in Directory.GetFiles (keys_path)) {
                    if (file_name.StartsWith ("sparkleshare") &&
                        file_name.EndsWith (".key")) {

                        key_file_path = Path.Combine (keys_path, file_name);
                        break;
                    }
                }
            }

            Process process = new Process ();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.FileName               = "ssh-add";
            process.StartInfo.Arguments              = "\"" + key_file_path + "\"";
            process.StartInfo.CreateNoWindow         = true;

            process.Start ();
            process.WaitForExit ();

            string pubkey_file_path = key_file_path + ".pub";

            // Create an easily accessible copy of the public
            // key in the user's SparkleShare folder
            if (!File.Exists (pubkey_file_path))
                File.Copy (pubkey_file_path,
                    Path.Combine (SparklePath, CurrentUser.Name + "'s key.txt"), true); // Overwriting is allowed

            ListPrivateKeys ();
        }


        private void ListPrivateKeys ()
        {
            Process process = new Process () {
                EnableRaisingEvents = true
            };

            process.StartInfo.WorkingDirectory       = SparkleConfig.DefaultConfig.TmpPath;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;

            process.StartInfo.FileName = "ssh-add";
            process.StartInfo.Arguments = "-l";

            process.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string keys_in_use = process.StandardOutput.ReadToEnd ().Trim ();
            process.WaitForExit ();

            SparkleHelpers.DebugInfo ("Auth",
                "The following keys will be available to SparkleShare: " + Environment.NewLine + keys_in_use);
        }


        // Generates and installs an RSA keypair to identify this system
        public void GenerateKeyPair ()
        {
            string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
            string key_file_name = "sparkleshare." + CurrentUser.Email + ".key";
            string key_file_path = Path.Combine (keys_path, key_file_name);

            if (File.Exists (key_file_path)) {
                SparkleHelpers.DebugInfo ("Auth", "Keypair exists ('" + key_file_name + "'), leaving it untouched");
                return;

            } else {
                if (!Directory.Exists (keys_path))
                    Directory.CreateDirectory (keys_path);
            }

            Process process = new Process () {
                EnableRaisingEvents = true
            };

            process.StartInfo.WorkingDirectory       = keys_path;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName               = "ssh-keygen";
            process.StartInfo.CreateNoWindow         = true;

            string computer_name = System.Net.Dns.GetHostName ();

            if (computer_name.EndsWith (".local"))
                computer_name = computer_name.Replace (".local", "");

            process.StartInfo.Arguments = "-t rsa " + // crypto type
                "-P \"\" " + // password (none)
                "-C \"" + computer_name + "\" " + // key comment
                "-f " + key_file_name; // file name

            process.Start ();
            process.WaitForExit ();

            if (process.ExitCode == 0)
                SparkleHelpers.DebugInfo ("Auth", "Created keypair '" + key_file_name + "'");
            else
                SparkleHelpers.DebugInfo ("Auth", "Could not create keypair '" + key_file_name + "'");

            // Create an easily accessible copy of the public
            // key in the user's SparkleShare folder
            File.Copy (key_file_path + ".pub", Path.Combine (SparklePath, CurrentUser.Name + "'s link code.txt"), true);
        }


        public void StartFetcher (string address, string required_fingerprint,
            string remote_path, string announcements_url, bool fetch_prior_history)
        {
            if (announcements_url != null)
                announcements_url = announcements_url.Trim ();

            string tmp_path = SparkleConfig.DefaultConfig.TmpPath;

			if (!Directory.Exists (tmp_path)) {
                Directory.CreateDirectory (tmp_path);
                File.SetAttributes (tmp_path,
					File.GetAttributes (tmp_path) | FileAttributes.Hidden);
            }

            string canonical_name = Path.GetFileNameWithoutExtension (remote_path);
            string tmp_folder     = Path.Combine (tmp_path, canonical_name);
            string backend        = SparkleFetcherBase.GetBackend (remote_path);

            try {
                this.fetcher = (SparkleFetcherBase) Activator.CreateInstance (
                        Type.GetType ("SparkleLib." + backend + ".SparkleFetcher, SparkleLib." + backend),
                            address,
                            required_fingerprint,
                            remote_path,
                            tmp_folder,
                            fetch_prior_history
                );

            } catch {
                SparkleHelpers.DebugInfo ("Controller",
                    "Failed to load \"" + backend + "\" backend for \"" + canonical_name + "\"");

                if (FolderFetchError != null)
                    FolderFetchError (
                        Path.Combine (address, remote_path).Replace (@"\", "/"),
                        new string [] {"Failed to load \"" + backend + "\" backend for \"" + canonical_name + "\""});

                return;
            }


            this.fetcher.Finished += delegate (bool repo_is_encrypted, bool repo_is_empty, string [] warnings) {
                if (repo_is_encrypted && repo_is_empty) {
                    if (ShowSetupWindowEvent != null)
                        ShowSetupWindowEvent (PageType.CryptoSetup);

                } else if (repo_is_encrypted) {
                    if (ShowSetupWindowEvent != null)
                        ShowSetupWindowEvent (PageType.CryptoPassword);

                } else {
                    FinishFetcher ();
                }
            };

            this.fetcher.Failed += delegate {
                if (FolderFetchError != null)
                    FolderFetchError (this.fetcher.RemoteUrl.ToString (), this.fetcher.Errors);

                StopFetcher ();
            };
            
            this.fetcher.ProgressChanged += delegate (double percentage) {
                if (FolderFetching != null)
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
                    SparkleHelpers.DebugInfo ("Controller",
                        "Deleted " + this.fetcher.TargetFolder);

                } catch (Exception e) {
                    SparkleHelpers.DebugInfo ("Controller",
                        "Failed to delete " + this.fetcher.TargetFolder + ": " + e.Message);
                }
            }

            this.fetcher.Dispose ();
            this.fetcher = null;
        }


        public void FinishFetcher (string password)
        {
            this.fetcher.EnableFetchedRepoCrypto (password);
            FinishFetcher ();
        }


        public void FinishFetcher ()
        {
            this.fetcher.Complete ();
            string canonical_name = Path.GetFileNameWithoutExtension (this.fetcher.RemoteUrl.AbsolutePath);

            bool target_folder_exists = Directory.Exists (
                Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, canonical_name));

            // Add a numbered suffix to the name if a folder with the same name
            // already exists. Example: "Folder (2)"
            int suffix = 1;
            while (target_folder_exists) {
                suffix++;
                target_folder_exists = Directory.Exists (
                    Path.Combine (
                        SparkleConfig.DefaultConfig.FoldersPath,
                        canonical_name + " (" + suffix + ")"
                    )
                );
            }

            string target_folder_name = canonical_name;

            if (suffix > 1)
                target_folder_name += " (" + suffix + ")";

            string target_folder_path = Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, target_folder_name);

            try {
                SparkleHelpers.ClearAttributes (this.fetcher.TargetFolder);
                Directory.Move (this.fetcher.TargetFolder, target_folder_path);

                string backend = SparkleFetcherBase.GetBackend (this.fetcher.RemoteUrl.AbsolutePath);
                SparkleConfig.DefaultConfig.AddFolder (target_folder_name, this.fetcher.RemoteUrl.ToString (), backend);

                if (FolderFetched != null)
                    FolderFetched (this.fetcher.RemoteUrl.ToString (), this.fetcher.Warnings.ToArray ());

                /* TODO
                if (!string.IsNullOrEmpty (announcements_url)) {
                    SparkleConfig.DefaultConfig.SetFolderOptionalAttribute (
                        target_folder_name, "announcements_url", announcements_url);
                */

                lock (this.repo_lock) {
                    AddRepository (target_folder_path);
                }

                if (FolderListChanged != null)
                    FolderListChanged ();

                this.fetcher.Dispose ();
                this.fetcher = null;

            } catch (Exception e) {
                SparkleHelpers.DebugInfo ("Controller", "Error adding folder: " + e.Message);
            }
        }


        public bool CheckPassword (string password)
        {
            return this.fetcher.IsFetchedRepoPasswordCorrect (password);
        }


        public string GetAvatar (string email, int size)
        {
            email = email.ToLower ();

            string avatars_path = new string [] { Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath),
                "icons", size + "x" + size, "status" }.Combine ();

            string avatar_file_path = Path.Combine (avatars_path, "avatar-" + email);

            if (File.Exists (avatar_file_path)) {
                if (new FileInfo (avatar_file_path).CreationTime < DateTime.Now.AddDays (-1))
                    File.Delete (avatar_file_path);
                else
                    return avatar_file_path;
            }

            WebClient client = new WebClient ();
            string url =  "http://gravatar.com/avatar/" + GetMD5 (email) + ".jpg?s=" + size + "&d=404";

            try {
                byte [] buffer = client.DownloadData (url);

                if (buffer.Length > 255) {
                    if (!Directory.Exists (avatars_path)) {
                        Directory.CreateDirectory (avatars_path);
                        SparkleHelpers.DebugInfo ("Controller", "Created '" + avatars_path + "'");
                    }

                    File.WriteAllBytes (avatar_file_path, buffer);
                    SparkleHelpers.DebugInfo ("Controller", "Fetched " + size + "x" + size + " gravatar for " + email);

                    return avatar_file_path;

                } else {
                    return null;
                }

            } catch (WebException) {
                return null;
            }
        }


        public virtual void Quit ()
        {
            foreach (SparkleRepoBase repo in Repositories)
                repo.Dispose ();

            Environment.Exit (0);
        }


        private string AssignAvatar (string s)
        {
            string hash    = "0" + GetMD5 (s).Substring (0, 8);
            string numbers = Regex.Replace (hash, "[a-z]", "");
            int number     = int.Parse (numbers);
            string letters = "abcdefghijklmnopqrstuvwxyz";

            return "avatar-" + letters [(number % 11)] + ".png";
        }


        // Creates an MD5 hash of input
        private string GetMD5 (string s)
        {
            MD5 md5 = new MD5CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = md5.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
        }


        private string FormatBreadCrumbs (string path_root, string path)
        {
			path_root = path_root.Replace ("/", Path.DirectorySeparatorChar.ToString ());
			path = path.Replace ("/", Path.DirectorySeparatorChar.ToString ());
			
            string link      = "";
            string [] crumbs = path.Split (Path.DirectorySeparatorChar);

            int i = 0;
            string new_path_root = path_root;
            bool previous_was_folder = false;
			
            foreach (string crumb in crumbs) {
                if (string.IsNullOrEmpty (crumb))
                    continue;
				
                string crumb_path = Path.Combine (new_path_root, crumb);
				
                if (Directory.Exists (crumb_path)) {
                    link += "<a href='" + crumb_path + "'>" + crumb + Path.DirectorySeparatorChar + "</a>";
                    previous_was_folder = true;

                } else if (File.Exists (crumb_path)) {
                    link += "<a href='" + crumb_path + "'>" + crumb + "</a>";
                    previous_was_folder = false;

                } else {
                    if (i > 0 && !previous_was_folder)
                        link += Path.DirectorySeparatorChar;

                    link += crumb;
                    previous_was_folder = false;
                }

                new_path_root = Path.Combine (new_path_root, crumb);
                i++;
            }

            return link;
        }

        
        // All change sets that happened on a day
        private class ActivityDay : List <SparkleChangeSet>
        {
            public DateTime Date;
    
            public ActivityDay (DateTime date_time)
            {
                Date = new DateTime (date_time.Year, date_time.Month, date_time.Day);
            }
        }
    }
}
