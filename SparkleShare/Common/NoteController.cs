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

using Sparkles;

namespace SparkleShare {

    public class NoteController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event UpdateTitleEventDelegate UpdateTitleEvent = delegate { };
        public delegate void UpdateTitleEventDelegate (string title);

        public readonly string AvatarFilePath = "";
        public string CurrentProject { get; private set; }


        public NoteController ()
        {
            SparkleShare.Controller.ShowNoteWindowEvent += OnNoteWindowEvent;

            if (SparkleShare.Controller.AvatarsEnabled && !SparkleShare.Controller.FirstRun)
                AvatarFilePath = Avatars.GetAvatar (SparkleShare.Controller.CurrentUser.Email,
                    48, SparkleShare.Controller.Config.DirectoryPath);
        }


        public void CancelClicked ()
        {
            HideWindowEvent ();
        }


        public void SyncClicked (string note)
        {
            HideWindowEvent ();
            new Thread (() => ResumeWithNote (note)).Start ();
        }


        public void WindowClosed ()
        {
            HideWindowEvent ();
        }


        void OnNoteWindowEvent (string project)
        {
            CurrentProject = project;

            ShowWindowEvent ();
            UpdateTitleEvent (CurrentProject);
        }


        void ResumeWithNote (string note)
        {
            BaseRepository repo = SparkleShare.Controller.GetRepoByName (CurrentProject);
            repo.Resume (note);
        }
    }
}
