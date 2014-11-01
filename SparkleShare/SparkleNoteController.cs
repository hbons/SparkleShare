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
using System.Net;
using System.Threading;

namespace SparkleShare {

    public class SparkleNoteController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event UpdateTitleEventDelegate UpdateTitleEvent = delegate { };
        public delegate void UpdateTitleEventDelegate (string title);

        public string AvatarFilePath = "";
        public string CurrentProject { get; private set; }


        public SparkleNoteController ()
        {
            Program.Controller.ShowNoteWindowEvent += delegate (string project) {
                CurrentProject = project;
                ShowWindowEvent ();
                UpdateTitleEvent (CurrentProject);
            };

            AvatarFilePath = SparkleAvatars.GetAvatar (Program.Controller.CurrentUser.Email,
                48, Program.Controller.Config.FullPath);
        }


        public void CancelClicked ()
        {
            HideWindowEvent ();
        }


        public void SyncClicked (string note)
        {
            HideWindowEvent ();
            new Thread (() => Program.Controller.GetRepoByName (CurrentProject).Resume (note)).Start ();
        }


        public void WindowClosed ()
        {
            HideWindowEvent ();
        }
    }
}
