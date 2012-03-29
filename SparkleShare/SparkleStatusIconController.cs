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
using System.IO;
using System.Timers;

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

        public event UpdateIconEventHandler UpdateIconEvent;
        public delegate void UpdateIconEventHandler (int icon_frame);

        public event UpdateMenuEventHandler UpdateMenuEvent;
        public delegate void UpdateMenuEventHandler (IconState state);

        public event UpdateStatusItemEventHandler UpdateStatusItemEvent;
        public delegate void UpdateStatusItemEventHandler (string state_text);

        public event UpdateQuitItemEventHandler UpdateQuitItemEvent;
        public delegate void UpdateQuitItemEventHandler (bool quit_item_enabled);

        public IconState CurrentState = IconState.Idle;
        public string StateText = "Welcome to SparkleShare!";


        public readonly int MenuOverFlowThreshold   = 9;
        public readonly int MinSubmenuOverflowCount = 3;


        public string [] Folders {
            get {
                int overflow_count = (Program.Controller.Folders.Count - MenuOverFlowThreshold);

                if (overflow_count >= MinSubmenuOverflowCount)
                    return Program.Controller.Folders.GetRange (0, MenuOverFlowThreshold).ToArray ();
                else
                    return Program.Controller.Folders.ToArray ();
            }
        }

        public string [] OverflowFolders {
            get {
                int overflow_count = (Program.Controller.Folders.Count - MenuOverFlowThreshold);

                if (overflow_count >= MinSubmenuOverflowCount)
                    return Program.Controller.Folders.GetRange (MenuOverFlowThreshold, overflow_count).ToArray ();
                else
                    return new string [0];
            }
        }


        public string FolderSize {
            get {
                double size = 0;

                foreach (SparkleRepoBase repo in
                         Program.Controller.Repositories.GetRange (
                             0, Program.Controller.Repositories.Count)) {

                    size += repo.Size + repo.HistorySize;
                }

                if (size == 0)
                    return "";
                else
                    return "— " + Program.Controller.FormatSize (size);
            }
        }

        public int ProgressPercentage {
            get {
                return (int) Program.Controller.ProgressPercentage;
            }
        }

        public string ProgressSpeed {
            get {
                return Program.Controller.ProgressSpeed;
            }
        }

        public bool QuitItemEnabled {
            get {
                return (CurrentState != IconState.Syncing &&
                        CurrentState != IconState.SyncingDown &&
                        CurrentState != IconState.SyncingUp);
            }
        }


        private Timer animation;
        private int animation_frame_number;


        public SparkleStatusIconController ()
        {
            InitAnimation ();

            Program.Controller.FolderListChanged += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.Folders.Count == 0)
                        StateText = "Welcome to SparkleShare!";
                    else
                        StateText = "Files up to date " + FolderSize;
                }

                if (UpdateStatusItemEvent != null)
                    UpdateStatusItemEvent (StateText);

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnIdle += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.Folders.Count == 0)
                        StateText = "Welcome to SparkleShare!";
                    else
                        StateText = "Files up to date " + FolderSize;
                }

                if (UpdateQuitItemEvent != null)
                    UpdateQuitItemEvent (QuitItemEnabled);

                if (UpdateStatusItemEvent != null)
                    UpdateStatusItemEvent (StateText);

                this.animation.Stop ();

                if (UpdateIconEvent != null)
                    UpdateIconEvent (0);
            };

            Program.Controller.OnSyncing += delegate {
				int repos_syncing_up   = 0;
				int repos_syncing_down = 0;
				
				foreach (SparkleRepoBase repo in
				         Program.Controller.Repositories.GetRange (
				         	0, Program.Controller.Repositories.Count)) {
					
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

                StateText += " " + ProgressPercentage + "%  " + ProgressSpeed;

                if (UpdateStatusItemEvent != null)
                    UpdateStatusItemEvent (StateText);

                if (UpdateQuitItemEvent != null)
                    UpdateQuitItemEvent (QuitItemEnabled);

                this.animation.Start ();
            };

            Program.Controller.OnError += delegate {
                CurrentState = IconState.Error;
                StateText    = "Failed to send some changes";

                if (UpdateQuitItemEvent != null)
                    UpdateQuitItemEvent (QuitItemEnabled);

                if (UpdateStatusItemEvent != null)
                    UpdateStatusItemEvent (StateText);

                this.animation.Stop ();

                if (UpdateIconEvent != null)
                    UpdateIconEvent (-1);
            };
        }


        public void SparkleShareClicked ()
        {
            Program.Controller.OpenSparkleShareFolder ();
        }


        public void SubfolderClicked (string subfolder)
        {
            Program.Controller.OpenSparkleShareFolder (subfolder);
        }


        public void AddHostedProjectClicked ()
        {
            Program.Controller.ShowSetupWindow (PageType.Add);
        }


        public void OpenRecentEventsClicked ()
        {
            Program.Controller.ShowEventLogWindow ();
        }


        public void AboutClicked ()
        {
            Program.Controller.ShowAboutWindow ();
        }
        
		
        public void QuitClicked ()
        {
            Program.Controller.Quit ();
        }


        private void InitAnimation ()
        {
            this.animation_frame_number = 0;

            this.animation = new Timer () {
                Interval = 40
            };

            this.animation.Elapsed += delegate {
                if (this.animation_frame_number < 4)
                    this.animation_frame_number++;
                else
                    this.animation_frame_number = 0;

                if (UpdateIconEvent != null)
                    UpdateIconEvent (this.animation_frame_number);
            };
        }
    }
}
