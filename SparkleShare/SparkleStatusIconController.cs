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

        public delegate void UpdateStatusLineEventHandler ();

        public event UpdateMenuEventHandler UpdateMenuEvent;
        public delegate void UpdateMenuEventHandler (IconState state);

        public event UpdateQuitItemEventHandler UpdateQuitItemEvent;
        public delegate void UpdateQuitItemEventHandler (bool quit_item_enabled);

        public IconState CurrentState = IconState.Idle;

        public string [] Folders {
            get {
                return Program.Controller.Folders.ToArray ();
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
                    return " — " + Program.Controller.FormatSize (size);
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


        public SparkleStatusIconController ()
        {
            Program.Controller.FolderListChanged += delegate {
                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (CurrentState);
            };


            Program.Controller.OnIdle += delegate {
                if (CurrentState != IconState.Error)
                    CurrentState = IconState.Idle;

                if (UpdateQuitItemEvent != null)
                    UpdateQuitItemEvent (QuitItemEnabled);

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (CurrentState);
            };


            Program.Controller.OnSyncing += delegate {
                CurrentState = IconState.Syncing;

                if (UpdateQuitItemEvent != null)
                    UpdateQuitItemEvent (QuitItemEnabled);

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (IconState.Syncing);
            };


            Program.Controller.OnError += delegate {
                CurrentState = IconState.Error;

                if (UpdateQuitItemEvent != null)
                    UpdateQuitItemEvent (QuitItemEnabled);

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (IconState.Error);
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
            Program.Controller.ShowSetupWindow (PageType.Tutorial);
        }


        public void OpenRecentEventsClicked ()
        {
            Program.Controller.ShowEventLogWindow ();
        }


        public void AboutClicked ()
        {
            Program.Controller.ShowAboutWindow ();
        }
    }
}
