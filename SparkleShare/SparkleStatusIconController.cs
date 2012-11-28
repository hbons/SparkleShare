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
using System.Threading;

using SparkleLib;

namespace SparkleShare {

    public enum IconState {
        Idle,
        SyncingUp,
        SyncingDown,
        Syncing,
        Error
    }


    public class SparkleStatusIconController {

        public event UpdateIconEventHandler UpdateIconEvent = delegate { };
        public delegate void UpdateIconEventHandler (IconState state);

        public event UpdateMenuEventHandler UpdateMenuEvent = delegate { };
        public delegate void UpdateMenuEventHandler (IconState state);

        public event UpdateStatusItemEventHandler UpdateStatusItemEvent = delegate { };
        public delegate void UpdateStatusItemEventHandler (string state_text);

        public event UpdateQuitItemEventHandler UpdateQuitItemEvent = delegate { };
        public delegate void UpdateQuitItemEventHandler (bool quit_item_enabled);

        public IconState CurrentState = IconState.Idle;
        public string StateText       = "Welcome to SparkleShare!";

        public string [] Folders;
        public string [] FolderErrors;
        

        public string FolderSize {
            get {
                double size = 0;

                foreach (SparkleRepoBase repo in Program.Controller.Repositories)
                    size += repo.Size;

                if (size == 0)
                    return "";
                else
                    return "— " + size.ToSize ();
            }
        }

        public int ProgressPercentage {
            get {
                return (int) Program.Controller.ProgressPercentage;
            }
        }

        public string ProgressSpeed {
            get {
                string progress_speed = "";

                if (Program.Controller.ProgressSpeedDown == 0 && Program.Controller.ProgressSpeedUp > 0) {
                    progress_speed = Program.Controller.ProgressSpeedUp.ToSize () + "/s ";

                } else if (Program.Controller.ProgressSpeedUp == 0 && Program.Controller.ProgressSpeedDown > 0) {
                    progress_speed = Program.Controller.ProgressSpeedDown.ToSize () + "/s ";
                        
                } else if (Program.Controller.ProgressSpeedUp   > 0 &&
                           Program.Controller.ProgressSpeedDown > 0) {

                    progress_speed = "Up: " + Program.Controller.ProgressSpeedUp.ToSize () + "/s " +
                        "Down: " + Program.Controller.ProgressSpeedDown.ToSize () + "/s";
                }

                return progress_speed;
            }
        }

        public bool RecentEventsItemEnabled {
            get {
                return (Program.Controller.Folders.Count > 0);
            }
        }

        public bool QuitItemEnabled {
            get {
                return (CurrentState == IconState.Idle || CurrentState == IconState.Error);
            }
        }


        public SparkleStatusIconController ()
        {
            UpdateFolders ();

            Program.Controller.FolderListChanged += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.Folders.Count == 0)
                        StateText = "Welcome to SparkleShare!";
                    else
                        StateText = "Projects up to date " + FolderSize;
                }

                UpdateFolders ();

                UpdateStatusItemEvent (StateText);
                UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnIdle += delegate {
                UpdateFolders ();

                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.Folders.Count == 0)
                        StateText = "Welcome to SparkleShare!";
                    else
                        StateText = "Projects up to date " + FolderSize;
                }

                UpdateIconEvent (CurrentState);
                UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnSyncing += delegate {
				int repos_syncing_up   = 0;
				int repos_syncing_down = 0;
				
				foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
					if (repo.Status == SyncStatus.SyncUp)
						repos_syncing_up++;
					
					if (repo.Status == SyncStatus.SyncDown)
						repos_syncing_down++;
				}
				
				if (repos_syncing_up > 0 &&
				    repos_syncing_down > 0) {
					
					CurrentState = IconState.Syncing;
                    StateText    = "Syncing changes…";
				
				} else if (repos_syncing_down == 0) {
					CurrentState = IconState.SyncingUp;
                    StateText    = "Sending changes…";
					
				} else {
					CurrentState = IconState.SyncingDown;
                    StateText    = "Receiving changes…";
				}

                if (ProgressPercentage > 0)
                    StateText += " " + ProgressPercentage + "%  " + ProgressSpeed;

                UpdateIconEvent (CurrentState);
                UpdateStatusItemEvent (StateText);
                UpdateQuitItemEvent (QuitItemEnabled);
            };

            Program.Controller.OnError += delegate {
                CurrentState = IconState.Error;
                StateText    = "Failed to send some changes";

                UpdateFolders ();
                
                UpdateIconEvent (CurrentState);
                UpdateMenuEvent (CurrentState);
            };
        }


        public void SubfolderClicked (string subfolder)
        {
            Program.Controller.OpenSparkleShareFolder (subfolder);
        }


        public void TryAgainClicked (string subfolder)
        {
            foreach (SparkleRepoBase repo in Program.Controller.Repositories)
                if (repo.Name.Equals (subfolder))
                    new Thread (() => repo.ForceRetry ()).Start ();
        }

        
        public EventHandler OpenFolderDelegate (string subfolder)
        {
            return delegate { SubfolderClicked (subfolder); };
        }
        
        
        public EventHandler TryAgainDelegate (string subfolder)
        {
            return delegate { TryAgainClicked (subfolder); };
        }


        public void RecentEventsClicked (object sender, EventArgs args)
        {
            new Thread (() => {
                while (!Program.Controller.RepositoriesLoaded)
                    Thread.Sleep (100);

                Program.Controller.ShowEventLogWindow ();
            
            }).Start ();
        }


        public void AddHostedProjectClicked (object sender, EventArgs args)
        {
            new Thread (() => Program.Controller.ShowSetupWindow (PageType.Add)).Start ();
        }


        public void AboutClicked (object sender, EventArgs args)
        {
            Program.Controller.ShowAboutWindow ();
        }
        
		
        public void QuitClicked (object sender, EventArgs args)
        {
            Program.Controller.Quit ();
        }


        private void UpdateFolders ()
        {
            Folders      = Program.Controller.Folders.ToArray ();
            FolderErrors = new string [Folders.Length];

            int i = 0;
            foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                FolderErrors [i] = "";
                
                if (repo.Error == ErrorStatus.HostUnreachable) {
                    FolderErrors [i] = "Can't reach the host";
                    
                } else if (repo.Error == ErrorStatus.HostIdentityChanged) {
                    FolderErrors [i] = "The host's identity has changed";
                    
                } else if (repo.Error == ErrorStatus.AuthenticationFailed) {
                    FolderErrors [i] = "Authentication failed";
                    
                } else if (repo.Error == ErrorStatus.DiskSpaceExceeded) {
                    FolderErrors [i] = "Host is out of disk space";
                    
                } else if (repo.Error == ErrorStatus.LockedFiles) {
                    FolderErrors [i] = "Some local files are locked or in use";

                } else if (repo.Error == ErrorStatus.NotFound) {
                    FolderErrors [i] = "Project doesn't exist on host";   
                }

                i++;
            }
        }
    }
}
