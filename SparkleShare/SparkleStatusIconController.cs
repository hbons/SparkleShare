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

namespace SparkleShare {

    public enum IconState {
        Idle,
        Syncing,
        Error
    }


    public class SparkleStatusIconController {

        public delegate void UpdateStatusLineEventHandler ();

        public event UpdateMenuEventHandler UpdateMenuEvent;
        public delegate void UpdateMenuEventHandler (IconState state);

        public IconState CurrentState = IconState.Idle;

        public string [] Folders {
            get {
                return Program.Controller.Folders.ToArray ();
            }
        }

        public string FolderSize {
            get {
                return Program.Controller.FolderSize;
            }
        }

        public SparkleStatusIconController ()
        {
            Program.Controller.FolderSizeChanged += delegate {
                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (CurrentState);
            };

            Program.Controller.FolderListChanged += delegate {
                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnIdle += delegate {
                if (CurrentState != IconState.Error)
                    CurrentState = IconState.Idle;

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnSyncing += delegate {
                CurrentState = IconState.Syncing;

                // TODO up down both

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (IconState.Syncing);
            };

            Program.Controller.OnError += delegate {
                CurrentState = IconState.Error;

                if (UpdateMenuEvent != null)
                    UpdateMenuEvent (IconState.Error);
            };
        }
    }
}
