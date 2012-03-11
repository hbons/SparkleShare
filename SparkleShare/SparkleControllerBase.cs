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

        public List<SparkleRepoBase> Repositories = new List<SparkleRepoBase> ();
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
        public delegate void FolderFetchErrorHandler (string remote_url);
        
        public event FolderFetchingHandler FolderFetching;
        public delegate void FolderFetchingHandler (double percentage);
        
        public event FolderListChangedHandler FolderListChanged;
        public delegate void FolderListChangedHandler ();

        public event AvatarFetchedHandler AvatarFetched;
        public delegate void AvatarFetchedHandler ();

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

        public event NoteNotificationRaisedEventHandler NoteNotificationRaised;
        public delegate void NoteNotificationRaisedEventHandler (SparkleUser user, string folder_name);



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

                lock (this.repo_lock) {
                    foreach (SparkleRepoBase repo in Repositories) {
                        if (repo.HasUnsyncedChanges)
                            unsynced_folders.Add (repo.Name);
                    }
                }

                return unsynced_folders;
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
        public abstract void OpenSparkleShareFolder (string subfolder);

        // Opens a file with the appropriate application
        public abstract void OpenFile (string url);


        private SparkleFetcherBase fetcher;
        private List<string> failed_avatars = new List<string> ();

        private Object avatar_lock = new Object ();
        private Object repo_lock   = new Object ();


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

            // Remove the repository when a delete event occurs
            watcher.Deleted += delegate (object o, FileSystemEventArgs args) {
                RemoveRepository (args.FullPath);
                SparkleConfig.DefaultConfig.RemoveFolder (Path.GetFileName (args.Name));

                if (FolderListChanged != null)
                    FolderListChanged ();
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

            new Thread (new ThreadStart (PopulateRepositories)).Start ();
        }
        
        
        public void UIHasLoaded ()
        {
            if (FirstRun)
                ShowSetupWindow (PageType.Setup);
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

            lock (this.repo_lock) {
                foreach (SparkleRepoBase repo in Repositories) {
                    List<SparkleChangeSet> change_sets = repo.GetChangeSets (30);
    
                    if (change_sets != null)
                        list.AddRange (change_sets);
                    else
                        SparkleHelpers.DebugInfo ("Log", "Could not create log for " + repo.Name);
                }
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

            string path = Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, name);
            int log_size = 50;

            lock (this.repo_lock) {
                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.LocalPath.Equals (path))            
                        return repo.GetChangeSets (log_size);
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
            List<string> emails = new List<string> ();

            change_sets.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            change_sets.Reverse ();

            if (change_sets.Count == 0)
                return null;

            foreach (SparkleChangeSet change_set in change_sets) {
                if (!emails.Contains (change_set.User.Email))
                    emails.Add (change_set.User.Email);

                bool change_set_inserted = false;
                foreach (ActivityDay stored_activity_day in activity_days) {
                    if (stored_activity_day.DateTime.Year  == change_set.Timestamp.Year &&
                        stored_activity_day.DateTime.Month == change_set.Timestamp.Month &&
                        stored_activity_day.DateTime.Day   == change_set.Timestamp.Day) {

                        bool squash = false;
                        foreach (SparkleChangeSet existing_set in stored_activity_day) {
                            if (change_set.User.Name.Equals (existing_set.User.Name) &&
                                change_set.User.Email.Equals (existing_set.User.Email) &&
                                change_set.Folder.Equals (existing_set.Folder)) {

                                existing_set.Added.AddRange (change_set.Added);
                                existing_set.Edited.AddRange (change_set.Edited);
                                existing_set.Deleted.AddRange (change_set.Deleted);
                                existing_set.MovedFrom.AddRange (change_set.MovedFrom);
                                existing_set.MovedTo.AddRange (change_set.MovedTo);
                                
                                existing_set.Added   = existing_set.Added.Distinct ().ToList ();
                                existing_set.Edited  = existing_set.Edited.Distinct ().ToList ();
                                existing_set.Deleted = existing_set.Deleted.Distinct ().ToList ();

                                if (DateTime.Compare (existing_set.Timestamp, change_set.Timestamp) < 1) {
                                    existing_set.FirstTimestamp = existing_set.Timestamp;
                                    existing_set.Timestamp      = change_set.Timestamp;
                                    existing_set.Revision       = change_set.Revision;

                                } else {
                                    existing_set.FirstTimestamp = change_set.Timestamp;
                                }

                                squash = true;
                            }
                        }

                        if (!squash)
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

            new Thread (new ThreadStart (delegate {
                FetchAvatars (emails, 48);
                FetchAvatars (emails, 36);
            })).Start ();

            string event_log_html   = EventLogHTML;
            string day_entry_html   = DayEntryHTML;
            string event_entry_html = EventEntryHTML;
            string event_log        = "";

            foreach (ActivityDay activity_day in activity_days) {
                string event_entries = "";

                foreach (SparkleChangeSet change_set in activity_day) {
                    string event_entry = "<dl>";

                    if (change_set.IsMagical) {
                        event_entry += "<dd>Did something magical</dd>";

                    } else {
                        if (change_set.Added.Count > 0) {
                            foreach (string file_path in change_set.Added) {
                                event_entry += "<dd class='document added'>";
								
                                event_entry += FormatBreadCrumbs (
                                    Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, change_set.Folder),
                                    file_path
                                );

                                event_entry += "</dd>";
                            }
                        }
						
                        if (change_set.Edited.Count > 0) {
                            foreach (string file_path in change_set.Edited) {
                                event_entry += "<dd class='document edited'>";

                                event_entry += FormatBreadCrumbs (
                                    Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, change_set.Folder),
                                    file_path
                                );

                                event_entry += "</dd>";
                            }
                        }
    
                        if (change_set.Deleted.Count > 0) {
                            foreach (string file_path in change_set.Deleted) {
                                event_entry += "<dd class='document deleted'>";
								
                                event_entry += FormatBreadCrumbs (
                                    Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, change_set.Folder),
                                    file_path
                                );

                                event_entry += "</dd>";
                            }
                        }

                        if (change_set.MovedFrom.Count > 0) {
                            int i = 0;
                            foreach (string file_path in change_set.MovedFrom) {
                                string to_file_path = change_set.MovedTo [i];
								event_entry += "<dd class='document moved'>";
								
                                event_entry += FormatBreadCrumbs (
                                        Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, change_set.Folder),
                                        file_path
                                );

                                event_entry += "<br>";
                                event_entry += FormatBreadCrumbs (
                                        Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, change_set.Folder),
                                        to_file_path
                                );

                                event_entry += "</dd>";

                                i++;
                            }
                        }
                    }

                    string change_set_avatar = GetAvatar (change_set.User.Email, 48);
                    
					if (File.Exists (change_set_avatar)) {
                        change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
					
				    } else {
                        change_set_avatar = "file://<!-- $pixmaps-path -->/" +
							AssignAvatar (change_set.User.Email);
					}
					
                    event_entry   += "</dl>";

                    string timestamp = change_set.Timestamp.ToString ("H:mm");

                    if (!change_set.FirstTimestamp.Equals (new DateTime ()))
                        timestamp = change_set.FirstTimestamp.ToString ("H:mm") +
                                    " – " + timestamp;

                    event_entries += event_entry_html.Replace ("<!-- $event-entry-content -->", event_entry)
                        .Replace ("<!-- $event-user-name -->", change_set.User.Name)
                        .Replace ("<!-- $event-avatar-url -->", change_set_avatar)
                        .Replace ("<!-- $event-time -->", timestamp)
                        .Replace ("<!-- $event-folder -->", change_set.Folder)
                        .Replace ("<!-- $event-url -->", change_set.Url.ToString ())
                        .Replace ("<!-- $event-revision -->", change_set.Revision)
                        .Replace ("<!-- $event-folder-color -->", AssignColor (change_set.Folder));
                }

                string day_entry   = "";
                DateTime today     = DateTime.Now;
                DateTime yesterday = DateTime.Now.AddDays (-1);

                if (today.Day   == activity_day.DateTime.Day &&
                    today.Month == activity_day.DateTime.Month && 
                    today.Year  == activity_day.DateTime.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='today' name='" + activity_day.DateTime.ToString (_("dddd, MMMM d")) + "'>"
                        + _("Today") + "</span>");

                } else if (yesterday.Day   == activity_day.DateTime.Day &&
                           yesterday.Month == activity_day.DateTime.Month &&
                           yesterday.Year  == activity_day.DateTime.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='yesterday' name='" + activity_day.DateTime.ToString (_("dddd, MMMM d")) + "'>"
                        + _("Yesterday") + "</span>");

                } else {
                    if (activity_day.DateTime.Year != DateTime.Now.Year) {

                        // TRANSLATORS: This is the date in the event logs
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.DateTime.ToString (_("dddd, MMMM d, yyyy")));

                    } else {

                        // TRANSLATORS: This is the date in the event logs, without the year
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.DateTime.ToString (_("dddd, MMMM d")));
                    }
                }

                event_log += day_entry.Replace ("<!-- $day-entry-content -->", event_entries);
            }


            int midnight = (int) (DateTime.Today.AddDays (1) - new DateTime (1970, 1, 1)).TotalSeconds;

            string html = event_log_html.Replace ("<!-- $event-log-content -->", event_log)
                .Replace ("<!-- $username -->", UserName)
                .Replace ("<!-- $user-avatar-url -->", "file://" + GetAvatar (UserEmail, 48))
                .Replace ("<!-- $midnight -->", midnight.ToString ());

            return html;
        }


        // Fires events for the current syncing state
        public void UpdateState ()
        {
            bool has_syncing_repos  = false;
            bool has_unsynced_repos = false;

            lock (this.repo_lock) {
                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.Status == SyncStatus.SyncDown ||
                        repo.Status == SyncStatus.SyncUp   ||
                        repo.IsBuffering) {
    
                        has_syncing_repos = true;
    
                    } else if (repo.HasUnsyncedChanges) {
                        has_unsynced_repos = true;
                    }
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
                    AlertNotificationRaised ("Conflict detected.",
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


            lock (this.repo_lock) {
                Repositories.Add (repo);
            }

            repo.Initialize ();
        }


        // Removes a repository from the list of repositories and
        // updates the statusicon menu
        private void RemoveRepository (string folder_path)
        {
            string folder_name = Path.GetFileName (folder_path);

            lock (this.repo_lock) {
                for (int i = 0; i < Repositories.Count; i++) {
                    SparkleRepoBase repo = Repositories [i];
    
                    if (repo.Name.Equals (folder_name)) {
                        repo.Dispose ();
                        Repositories.Remove (repo);
                        repo = null;
                        break;
                    }
                }
            }
        }


        // Updates the list of repositories with all the
        // folders in the SparkleShare folder
        private void PopulateRepositories ()
        {
            foreach (string folder_name in SparkleConfig.DefaultConfig.Folders) {
                string folder_path = new SparkleFolder (folder_name).FullPath;

                if (Directory.Exists (folder_path))
                    AddRepository (folder_path);
                else
                    SparkleConfig.DefaultConfig.RemoveFolder (folder_name);
            }

            if (FolderListChanged != null)
                FolderListChanged ();
        }


        public bool NotificationsEnabled {
            get {
                string notifications_enabled =
                    SparkleConfig.DefaultConfig.GetConfigOption ("notifications");

                if (String.IsNullOrEmpty (notifications_enabled)) {
                    SparkleConfig.DefaultConfig.SetConfigOption ("notifications", bool.TrueString);
                    return true;

                } else {
                    return notifications_enabled.Equals (bool.TrueString);
                }
            }
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
            OpenSparkleShareFolder ("");
        }

        
        // Adds the user's SparkleShare key to the ssh-agent,
        // so all activity is done with this key
        public void ImportPrivateKey ()
        {
            string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
            string key_file_name = "sparkleshare." + UserEmail + ".key";
            string key_file_path = Path.Combine (keys_path, key_file_name);

            if (!File.Exists (key_file_path)) {
                foreach (string file_name in Directory.GetFiles (keys_path)) {
                    if (file_name.StartsWith ("sparkleshare") &&
                        file_name.EndsWith (".key")) {

                        key_file_path = Path.Combine (keys_path, file_name);
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
        }


        // Looks up the user's name from the global configuration
        public string UserName
        {
            get {
                return SparkleConfig.DefaultConfig.User.Name;
            }

            set {
                SparkleConfig.DefaultConfig.User = new SparkleUser (value, UserEmail);
            }
        }


        // Looks up the user's email from the global configuration
        public string UserEmail
        {
            get {
                return SparkleConfig.DefaultConfig.User.Email;
            }
                    
            set {
                SparkleConfig.DefaultConfig.User = new SparkleUser (UserName, value);
            }
        }
        

        // Generates and installs an RSA keypair to identify this system
        public void GenerateKeyPair ()
        {
            string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
            string key_file_name = "sparkleshare." + UserEmail + ".key";
            string key_file_path = Path.Combine (keys_path, key_file_name);

            if (File.Exists (key_file_path)) {
                SparkleHelpers.DebugInfo ("Auth", "Key already exists ('" + key_file_name + "'), " +
                                          "leaving it untouched");
                return;
            }

            if (!Directory.Exists (keys_path))
                Directory.CreateDirectory (keys_path);

            if (!File.Exists (key_file_name)) {
                Process process = new Process () {
                    EnableRaisingEvents = true
                };
                
                process.StartInfo.WorkingDirectory       = keys_path;
                process.StartInfo.UseShellExecute        = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName               = "ssh-keygen";
                process.StartInfo.CreateNoWindow         = true;

                // -t is the crypto type
                // -P is the password (none)
                // -f is the file name to store the private key in
                process.StartInfo.Arguments = "-t rsa -P \"\" -f " + key_file_name;

                process.Start ();
                process.WaitForExit ();

                SparkleHelpers.DebugInfo ("Auth", "Created private key '" + key_file_name + "'");
                SparkleHelpers.DebugInfo ("Auth", "Created public key  '" + key_file_name + ".pub'");

                // Add some restrictions to what the key can
                // do when uploaded to the server
                // string public_key = File.ReadAllText (key_file_path + ".pub");
                // public_key = "no-port-forwarding,no-X11-forwarding,no-agent-forwarding,no-pty " + public_key;
                // File.WriteAllText (key_file_path + ".pub", public_key);

                // Create an easily accessible copy of the public
                // key in the user's SparkleShare folder
                File.Copy (key_file_path + ".pub",
                    Path.Combine (SparklePath, UserName + "'s key.txt"),
                    true); // Overwriting is allowed
            }
        }


        public void FetchAvatars (string email, int size)
        {
            FetchAvatars (new List<string> (new string [] { email }), size);
        }

        // Gets the avatar for a specific email address and size
        public void FetchAvatars (List<string> emails, int size)
        {
            List<string> old_avatars = new List<string> ();
            bool avatar_fetched      = false;
            string avatar_path       = new string [] {
                Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath),
                "icons", size + "x" + size, "status"}.Combine ();

            if (!Directory.Exists (avatar_path)) {
                Directory.CreateDirectory (avatar_path);
                SparkleHelpers.DebugInfo ("Avatar", "Created '" + avatar_path + "'");
            }

            foreach (string raw_email in emails) {
                // Gravatar wants lowercase emails
                string email            = raw_email.ToLower ();
                string avatar_file_path = Path.Combine (avatar_path, "avatar-" + email);

                if (File.Exists (avatar_file_path)) {
                    FileInfo avatar_info = new FileInfo (avatar_file_path);

                    // Delete avatars older than a month
                    if (avatar_info.CreationTime < DateTime.Now.AddMonths (-1)) {
                        try {
                          avatar_info.Delete ();
                          old_avatars.Add (email);

                        } catch (FileNotFoundException) {
                            if (old_avatars.Contains (email))
                                old_avatars.Remove (email);
                        }
                    }

                } else if (this.failed_avatars.Contains (email)) {
                    break;

                } else {
                  WebClient client = new WebClient ();
                  string url       =  "http://gravatar.com/avatar/" + GetMD5 (email) +
                                      ".jpg?s=" + size + "&d=404";
                  try {
                    // Fetch the avatar
                    byte [] buffer = client.DownloadData (url);

                    // Write the avatar data to a
                    // if not empty
                    if (buffer.Length > 255) {
                        avatar_fetched = true;

                        lock (this.avatar_lock)
                            File.WriteAllBytes (avatar_file_path, buffer);

                        SparkleHelpers.DebugInfo ("Avatar", "Fetched gravatar for " + email);
                    }

                  } catch (WebException e) {
                        SparkleHelpers.DebugInfo ("Avatar", "Failed fetching gravatar for " + email);

                        // Stop downloading further avatars if we have no internet access
                        if (e.Status == WebExceptionStatus.Timeout)
                            break;
                        else
                            this.failed_avatars.Add (email);
                  }
               }
            }

            // Fetch new versions of the avatars that we
            // deleted because they were too old
            if (old_avatars.Count > 0)
                FetchAvatars (old_avatars, size);

            if (AvatarFetched != null && avatar_fetched)
                AvatarFetched ();
        }


        public string GetAvatar (string email, int size)
        {
            string avatar_file_path = SparkleHelpers.CombineMore (
                Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath), "icons",
                size + "x" + size, "status", "avatar-" + email);

            if (File.Exists (avatar_file_path)) {
                return avatar_file_path;

            } else {
                FetchAvatars (email, size);

                if (File.Exists (avatar_file_path))
                    return avatar_file_path;
                else
                    return null;
            }
        }


        public void FetchFolder (string server, string remote_folder, string announcements_url)
        {
            server        = server.Trim ();
            remote_folder = remote_folder.Trim ();

            if (announcements_url != null)
                announcements_url = announcements_url.Trim ();


            string tmp_path = SparkleConfig.DefaultConfig.TmpPath;
            if (!Directory.Exists (tmp_path)) {
                Directory.CreateDirectory (tmp_path);
                File.SetAttributes (tmp_path, FileAttributes.Directory | FileAttributes.Hidden);
            }


            // Strip the '.git' from the name
            string canonical_name = Path.GetFileNameWithoutExtension (remote_folder);
            string tmp_folder     = Path.Combine (tmp_path, canonical_name);
            string backend        = Path.GetExtension (remote_folder);

            if (!string.IsNullOrEmpty (backend)) {
                backend = backend.Substring (1);

                char [] letters = backend.ToCharArray ();
                letters [0] = char.ToUpper (letters [0]);
                backend = new string (letters);

            } else {
                backend = "Git";
            }


            try {
                this.fetcher = (SparkleFetcherBase) Activator.CreateInstance (
                        Type.GetType ("SparkleLib." + backend + ".SparkleFetcher, SparkleLib." + backend),
                            server,
                            remote_folder,
                            tmp_folder
                );

            } catch {
                SparkleHelpers.DebugInfo ("Controller",
                    "Failed to load \"" + backend + "\" backend for \"" + canonical_name + "\"");

                if (FolderFetchError != null)
                    FolderFetchError (Path.Combine (server, remote_folder).Replace (@"\", "/"));

                return;
            }


            bool target_folder_exists = Directory.Exists (
                Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, canonical_name));

            // Add a numbered suffix to the nameif a folder with the same name
            // already exists. Example: "Folder (2)"
            int i = 1;
            while (target_folder_exists) {
                i++;
                target_folder_exists = Directory.Exists (
                    Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, canonical_name + " (" + i + ")"));
            }

            string target_folder_name = canonical_name;
            if (i > 1)
                target_folder_name += " (" + i + ")";

            this.fetcher.Finished += delegate (string [] warnings) {

                // Needed to do the moving
                SparkleHelpers.ClearAttributes (tmp_folder);
                string target_folder_path = Path.Combine (
                    SparkleConfig.DefaultConfig.FoldersPath, target_folder_name);

                try {
                    Directory.Move (tmp_folder, target_folder_path);

                    SparkleConfig.DefaultConfig.AddFolder (target_folder_name, this.fetcher.RemoteUrl, backend);
    
                    if (!string.IsNullOrEmpty (announcements_url)) {
                        SparkleConfig.DefaultConfig.SetFolderOptionalAttribute (target_folder_name,
                            "announcements_url", announcements_url);
                    }

                    AddRepository (target_folder_path);

                    if (FolderFetched != null)
                        FolderFetched (this.fetcher.RemoteUrl, warnings);

                    if (FolderListChanged != null)
                    FolderListChanged ();

                } catch (Exception e) {
                    SparkleHelpers.DebugInfo ("Controller", "Error moving folder: " + e.Message);
                }

                this.fetcher.Dispose ();
                this.fetcher = null;
				
				// TODO: only remove stale repos
                //if (Directory.Exists (tmp_path))
                //    Directory.Delete (tmp_path, true);
            };

            this.fetcher.Failed += delegate {
                if (FolderFetchError != null)
                    FolderFetchError (this.fetcher.RemoteUrl);

                this.fetcher.Dispose ();

                if (Directory.Exists (tmp_path))
                    Directory.Delete (tmp_path, true);

                this.fetcher = null;
            };
            
            this.fetcher.ProgressChanged += delegate (double percentage) {
                if (FolderFetching != null)
                    FolderFetching (percentage);
            };


            this.fetcher.Start ();
        }


        public void StopFetcher ()
        {
            if (fetcher != null)
                fetcher.Stop ();
        }


        // Checks whether there are any folders syncing and
        // quits if safe
        public void TryQuit ()
        {
            lock (this.repo_lock) {
                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.Status == SyncStatus.SyncUp   ||
                        repo.Status == SyncStatus.SyncDown ||
                        repo.IsBuffering) {
    
                        return;
                    }
                }
            }
            
            Quit ();
        }


        public virtual void Quit ()
        {
            lock (this.repo_lock) {
                foreach (SparkleRepoBase repo in Repositories)
                    repo.Dispose ();
            }

            Environment.Exit (0);
        }


        private string [] tango_palette = new string [] {"#eaab00", "#e37222",
            "#3892ab", "#33c2cb", "#19b271", "#9eab05", "#8599a8", "#9ca696",
            "#b88454", "#cc0033", "#8f6678", "#8c6cd0", "#796cbf", "#4060af",
            "#aa9c8f", "#818a8f"};

        private string AssignColor (string s)
        {
            string hash    = "0" + GetMD5 (s).Substring (0, 8);
            string numbers = Regex.Replace (hash, "[a-z]", "");
            int number     = int.Parse (numbers);

            return this.tango_palette [number % this.tango_palette.Length];
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
			path_root = path_root.Replace ("/",
				Path.DirectorySeparatorChar.ToString ());
			
			path = path.Replace ("/",
				Path.DirectorySeparatorChar.ToString ());
			
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
    }


    public class ChangeSet : SparkleChangeSet { }
    
    
    // All change sets that happened on a day
    public class ActivityDay : List <SparkleChangeSet>
    {
        public DateTime DateTime;

        public ActivityDay (DateTime date_time)
        {
            DateTime = date_time;
            DateTime = new DateTime (DateTime.Year, DateTime.Month, DateTime.Day);
        }
    }
}
