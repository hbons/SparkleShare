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

    public enum PageType {
        Setup,
        Add,
        Syncing,
        Error,
        Finished,
        Tutorial
    }


    public class SparkleSetupController {

        public event ChangePageEventHandler ChangePageEvent;
        public delegate void ChangePageEventHandler (PageType page);
        
        public event UpdateProgressBarEventHandler UpdateProgressBarEvent;
        public delegate void UpdateProgressBarEventHandler (double percentage);


        public int TutorialPageNumber {
            get {
                return this.tutorial_page_number;
            }
        }

        public string PreviousUrl {
            get {
                return this.previous_url;
            }
        }

        public string PreviousServer {
            get {
                return this.previous_server;
            }
        }

        public string PreviousFolder {
            get {
                return this.previous_folder;
            }
        }

        public string SyncingFolder {
            get {
                return this.syncing_folder;
            }
        }

        public PageType PreviousPage {
            get {
                return this.previous_page;
            }
        }


        private string previous_server   = "";
        private string previous_folder   = "";
        private string previous_url      = "";
        private string syncing_folder    = "";
        private int tutorial_page_number = 1;
        private PageType previous_page;


        public SparkleSetupController ()
        {
            ChangePageEvent += delegate (PageType page) {
                this.previous_page = page;
            };
        }


        public void ShowAddPage ()
        {
            this.tutorial_page_number = 1;
           if (ChangePageEvent != null)
               ChangePageEvent (PageType.Tutorial);
        }


        public void ShowSetupPage ()
        {
           if (ChangePageEvent != null)
               ChangePageEvent (PageType.Setup);
        }


        public void SetupPageCompleted (string full_name, string email)
        {
            Program.Controller.UserName  = full_name;
            Program.Controller.UserEmail = email;

            Program.Controller.GenerateKeyPair ();
            Program.Controller.UpdateState ();

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial);
        }


        public void TutorialPageCompleted ()
        {
            if (this.tutorial_page_number == 4) {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Add);

            } else {
                this.tutorial_page_number++;

                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Tutorial);
            }
        }


        public void TutorialSkipped ()
        {
            this.tutorial_page_number = 4;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial);
        }


        public void AddPageCompleted (string server, string folder_name)
        {
            this.syncing_folder = Path.GetFileNameWithoutExtension (folder_name);
            this.previous_server = server;
            this.previous_folder = folder_name;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing);

            Program.Controller.FolderFetched += delegate {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Finished);

                this.syncing_folder = "";
            };

            Program.Controller.FolderFetchError += delegate (string remote_url) {
                this.previous_url = remote_url;

                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Error);

                this.syncing_folder = "";
            };
            
            Program.Controller.FolderFetching += delegate (double percentage) {
                if (UpdateProgressBarEvent != null)
                    UpdateProgressBarEvent (percentage);
            };

            Program.Controller.FetchFolder (server, folder_name);
        }


        public void ErrorPageCompleted ()
        {
            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Add);
        }


        public void SyncingCancelled ()
        {
            Program.Controller.StopFetcher ();

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Add);
        }


        public void FinishedPageCompleted ()
        {
            this.previous_server = "";
            this.previous_folder = "";
            Program.Controller.UpdateState ();
        }
    }
}
