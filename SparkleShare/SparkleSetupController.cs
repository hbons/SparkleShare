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
using System.IO;

using SparkleLib;

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

        public event ChangeAddressFieldEventHandler ChangeAddressFieldEvent;
        public delegate void ChangeAddressFieldEventHandler (string text,
            string example_text, FieldState state);

        public event ChangePathFieldEventHandler ChangePathFieldEvent;
        public delegate void ChangePathFieldEventHandler (string text,
            string example_text, FieldState state);

        public event SelectListPluginEventHandler SelectListPluginEvent;
        public delegate void SelectListPluginEventHandler (int index);

        public readonly List<SparklePlugin> Plugins = new List<SparklePlugin> ();
        public SparklePlugin SelectedPlugin;


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

        public string GuessedUserName {
            get {
                return Program.Controller.UserName;
            }
        }

        public string GuessedUserEmail {
            get {
                if (Program.Controller.UserEmail.Equals ("Unknown"))
                    return "";
                else
                    return Program.Controller.UserEmail;
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
            string local_plugins_path = SparkleHelpers.CombineMore (
                Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
                "sparkleshare", "plugins");

            string plugins_path = SparkleHelpers.CombineMore (
                Defines.DATAROOTDIR, "sparkleshare", "plugins");

            if (Directory.Exists (local_plugins_path))
                foreach (string xml_file_path in Directory.GetFiles (local_plugins_path, "*.xml"))
                    Plugins.Add (new SparklePlugin (xml_file_path));

            if (Directory.Exists (plugins_path)) {
                foreach (string xml_file_path in Directory.GetFiles (plugins_path, "*.xml")) {
                    if (xml_file_path.EndsWith ("own-server.xml"))
                        Plugins.Insert (0, new SparklePlugin (xml_file_path));
                    else
                        Plugins.Add (new SparklePlugin (xml_file_path));
                }
            }

            ChangePageEvent += delegate (PageType page) {
                this.previous_page = page;
            };
        }


        public void ShowAddPage ()
        {
            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Add);

            int index;
            if (SelectedPlugin == null)
                index = 0;
            else
                index = Plugins.IndexOf (SelectedPlugin);

            if (SelectListPluginEvent != null)
                SelectListPluginEvent (index);

            SelectedPluginChanged (index);
            SelectedPlugin = null;
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
            this.tutorial_page_number++;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial);
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


        public void SelectedPluginChanged (int plugin_index)
        {
            SelectedPlugin = Plugins [plugin_index];

            if (SelectedPlugin.Address != null) {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent (SelectedPlugin.Address, null, FieldState.Disabled);

            } else if (SelectedPlugin.AddressExample != null) {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent (PreviousServer, SelectedPlugin.AddressExample, FieldState.Enabled);
            } else {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent (PreviousServer, SelectedPlugin.AddressExample, FieldState.Enabled);
            }

            if (SelectedPlugin.Path != null) {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (SelectedPlugin.Path, null, FieldState.Disabled);

            } else if (SelectedPlugin.PathExample != null) {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (PreviousFolder, SelectedPlugin.PathExample, FieldState.Enabled);

            } else {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (PreviousFolder, SelectedPlugin.PathExample, FieldState.Enabled);
            }

            // TODO: previous server/folder doesn't work yet

            /*
            if (!string.IsNullOrEmpty (PreviousServer) && SelectedPlugin.Address == null) {
                if (ChangeAddressFieldEvent != null) {
                    ChangeAddressFieldEvent (this.previous_server,
                        SelectedPlugin.AddressExample, FieldState.Enabled);
                }
            }

            if (!string.IsNullOrEmpty (PreviousFolder) && SelectedPlugin.Path == null) {
                if (ChangePathFieldEvent != null) {
                    ChangeAddressFieldEvent (this.previous_folder,
                        SelectedPlugin.PathExample, FieldState.Enabled);
                }
            }
            */
        }
    }


    public enum FieldState {
        Enabled,
        Disabled
    }
}
