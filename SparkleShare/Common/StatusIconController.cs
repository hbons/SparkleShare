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
using System.Threading;
using Timers = System.Timers;

using Sparkles;

namespace SparkleShare {

    public enum IconState {
        Idle,
        SyncingUp,
        SyncingDown,
        Syncing,
        Error
    }


    public class ProjectInfo {

        readonly BaseRepository repo;

        public string Name { get { return repo.Name; }}
        public string Path { get { return repo.LocalPath; }}

        public bool IsPaused { get { return repo.Status == SyncStatus.Paused; }}
        public bool HasError { get { return repo.Status == SyncStatus.Error; }}


        public string StatusMessage {
            get {
                string status_message = "Waiting to sync";
                
                if (!repo.LastSync.Equals (DateTime.MinValue))
                    status_message = string.Format ("✓ Synced – Last change {0}", repo.LastSync.ToPrettyDate ());

                if (repo.Status == SyncStatus.SyncUp)
                    status_message = "Sending… " + (int) repo.ProgressPercentage + "%";
            
                if (repo.Status == SyncStatus.SyncDown)
                    status_message = "Receiving… " + (int) repo.ProgressPercentage + "%";

                if (!string.IsNullOrWhiteSpace (repo.ProgressInformation))
                    status_message += " – " + SparkleShare.Controller.ProgressInformation;

                if (repo.Status == SyncStatus.SyncUp || repo.Status == SyncStatus.SyncDown) {
                    if (repo.ProgressSpeed > 0)
                        status_message += " " + repo.ProgressSpeed.ToSize () + "/s";
                }

                if (IsPaused)
                    return "Syncing Paused";

                if (HasError) {
                    switch (repo.Error) {
                    case ErrorStatus.HostUnreachable: return "Can’t reach the host";
                    case ErrorStatus.HostIdentityChanged: return "The host’s identity has changed";
                    case ErrorStatus.AuthenticationFailed: return "Authentication failed";
                    case ErrorStatus.DiskSpaceExceeded: return "Host is out of disk space";
                    case ErrorStatus.UnreadableFiles: return "Some local files are unreadable or in use";
                    case ErrorStatus.NotFound: return "Project doesn’t exist on host";
                    case ErrorStatus.IncompatibleClientServer: return "Incompatible client/server versions";
                    }
                }

                return status_message;
            }
        }


        public string MoreUnsyncedChanges = "";

        public Dictionary<string, string> UnsyncedChangesInfo {
            get { 
                var changes_info = new Dictionary<string, string> ();
            
                int changes_count = 0;
                foreach (Change change in repo.UnsyncedChanges) {
                    changes_count++;

                    if (changes_count > 10)
                        continue;

                    switch (change.Type) {
                    case ChangeType.Added:   changes_info [change.Path] = "document-added-12.png"; break;
                    case ChangeType.Edited:  changes_info [change.Path] = "document-edited-12.png"; break;
                    case ChangeType.Deleted: changes_info [change.Path] = "document-deleted-12.png"; break;
                    case ChangeType.Moved:   changes_info [change.MovedToPath] = "document-moved-12.png"; break;
                    }
                }

                if (changes_count > 10)
                    MoreUnsyncedChanges = string.Format ("and {0} more", changes_count - 10);

                return changes_info;
            }
        }


        public ProjectInfo (BaseRepository repo)
        {
            this.repo = repo;
        }
    }


    public class StatusIconController {

        public event UpdateIconEventHandler UpdateIconEvent = delegate { };
        public delegate void UpdateIconEventHandler (IconState state);

        public event UpdateMenuEventHandler UpdateMenuEvent = delegate { };
        public delegate void UpdateMenuEventHandler (IconState state);

        public event UpdateStatusItemEventHandler UpdateStatusItemEvent = delegate { };
        public delegate void UpdateStatusItemEventHandler (string state_text);

        public event UpdateQuitItemEventHandler UpdateQuitItemEvent = delegate { };
        public delegate void UpdateQuitItemEventHandler (bool quit_item_enabled);

        public IconState CurrentState = IconState.Idle;
        public string StateText = "Welcome to SparkleShare!";

        public ProjectInfo [] Projects = new ProjectInfo [0];


        public bool RecentEventsItemEnabled {
            get {
                return (SparkleShare.Controller.Repositories.Length > 0);
            }
        }

        public bool LinkCodeItemEnabled {
            get {
                return !string.IsNullOrEmpty (SparkleShare.Controller.UserAuthenticationInfo.PublicKey);
            }
        }

        public bool QuitItemEnabled {
            get {
                return (CurrentState == IconState.Idle || CurrentState == IconState.Error);
            }
        }


        public StatusIconController ()
        {
            UpdateFolders ();

            SparkleShare.Controller.FolderListChanged += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    UpdateStateText ();
                }

                UpdateFolders ();

                UpdateStatusItemEvent (StateText);
                UpdateMenuEvent (CurrentState);
            };

            SparkleShare.Controller.OnIdle += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;
                    UpdateStateText ();
                }

                UpdateFolders ();

                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText);
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateMenuEvent (CurrentState);
            };

            SparkleShare.Controller.OnSyncing += delegate {
                int repos_syncing_up = 0;
                int repos_syncing_down = 0;

                foreach (BaseRepository repo in SparkleShare.Controller.Repositories) {
                    if (repo.Status == SyncStatus.SyncUp)
                        repos_syncing_up++;

                    if (repo.Status == SyncStatus.SyncDown)
                        repos_syncing_down++;
                }

                if (repos_syncing_up > 0 &&
                    repos_syncing_down > 0) {

                    CurrentState = IconState.Syncing;
                    StateText    = "Syncing…";

                } else if (repos_syncing_down == 0) {
                    CurrentState = IconState.SyncingUp;
                    StateText    = "Sending…";

                } else {
                    CurrentState = IconState.SyncingDown;
                    StateText    = "Receiving…";
                }

                int progress_percentage = (int) SparkleShare.Controller.ProgressPercentage;
                string progress_speed = "";

                if (SparkleShare.Controller.ProgressSpeedUp > 0.0 && SparkleShare.Controller.ProgressSpeedDown > 0.0) {
                    progress_speed = "Up: " + SparkleShare.Controller.ProgressSpeedUp.ToSize () + "/s " +
                        "Down: " + SparkleShare.Controller.ProgressSpeedDown.ToSize () + "/s";
                }

                if (SparkleShare.Controller.ProgressSpeedUp > 0.0)
                    progress_speed = SparkleShare.Controller.ProgressSpeedUp.ToSize () + "/s ";

                if (SparkleShare.Controller.ProgressSpeedDown > 0.0)
                    progress_speed = SparkleShare.Controller.ProgressSpeedDown.ToSize () + "/s ";

                if (progress_percentage > 0)
                    StateText += string.Format (" {0}% {1}", progress_percentage, progress_speed);

                if (!string.IsNullOrEmpty (SparkleShare.Controller.ProgressInformation))
                    StateText += " – " + SparkleShare.Controller.ProgressInformation;

                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText);
                UpdateQuitItemEvent (QuitItemEnabled);
            };

            SparkleShare.Controller.OnError += delegate {
                CurrentState = IconState.Error;
                StateText = "Not everything synced";

                UpdateFolders ();
                
                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText);
                UpdateQuitItemEvent (QuitItemEnabled);
                UpdateMenuEvent (CurrentState);
            };


            // FIXME: Work around a race condition causing
            // the icon to not always show the right state
            var timer = new Timers.Timer { Interval = 30 * 1000 };

            timer.Elapsed += delegate {
                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText);
            };

            timer.Start ();
        }


        private string UpdateStateText ()
        {
            if (Projects.Length == 0)
                return StateText = "Welcome to SparkleShare!";
            else
                return StateText = "✓ Synced " + GetPausedCount ();
        }


        private string GetPausedCount ()
        {
            int paused_projects = 0;
            
            foreach (ProjectInfo project in Projects)
                if (project.IsPaused)
                    paused_projects++;

            if (paused_projects > 0) 
                return string.Format ("— {0} paused", paused_projects);
            else
                return "";
        }


        // Main menu items
        public void RecentEventsClicked ()
        {
            new Thread (() => {
                while (!SparkleShare.Controller.RepositoriesLoaded)
                    Thread.Sleep (100);

                SparkleShare.Controller.ShowEventLogWindow ();
            
            }).Start ();
        }

        public void AddHostedProjectClicked ()
        {
            new Thread (() => SparkleShare.Controller.ShowSetupWindow (PageType.Add)).Start ();
        }

        public void CopyToClipboardClicked ()
        {
            SparkleShare.Controller.CopyToClipboard (SparkleShare.Controller.UserAuthenticationInfo.PublicKey);
        }

        public void AboutClicked ()
        {
            SparkleShare.Controller.ShowAboutWindow ();
        }

        public void QuitClicked ()
        {
            SparkleShare.Controller.Quit ();
        }


        // Project items
        public void ProjectClicked (string project)
        {
            SparkleShare.Controller.OpenSparkleShareFolder (project);
        }

        public void PauseClicked (string project)
        {
            SparkleShare.Controller.GetRepoByName (project).Pause ();
            UpdateStateText ();
            UpdateMenuEvent (CurrentState);
        }

        public void ResumeClicked (string project)
        {
            if (SparkleShare.Controller.GetRepoByName (project).UnsyncedChanges.Count > 0) {
                SparkleShare.Controller.ShowNoteWindow (project);
            
            } else {
              new Thread (() => {
                    SparkleShare.Controller.GetRepoByName (project).Resume ("");
                    
                    UpdateStateText ();
                    UpdateMenuEvent (CurrentState);

                }).Start ();
            }
        }

        public void TryAgainClicked (string project)
        {
            new Thread (() => SparkleShare.Controller.GetRepoByName (project).ForceRetry ()).Start ();
        }


        // Helper delegates
        public EventHandler OpenFolderDelegate (string project)
        {
            return delegate { ProjectClicked (project); };
        }
        
        public EventHandler TryAgainDelegate (string project)
        {
            return delegate { TryAgainClicked (project); };
        }
        
        public EventHandler PauseDelegate (string project)
        {
            return delegate { PauseClicked (project); };
        }
        
        public EventHandler ResumeDelegate (string project)
        {
            return delegate { ResumeClicked (project); };
        }


        readonly object projects_lock = new object ();

        void UpdateFolders ()
        {
            var projects = new List<ProjectInfo> ();

            lock (projects_lock) {
                foreach (BaseRepository repo in SparkleShare.Controller.Repositories)
                    projects.Add (new ProjectInfo (repo));
            }

            Projects = projects.ToArray ();
        }
    }
}
