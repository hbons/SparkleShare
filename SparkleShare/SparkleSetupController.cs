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
using System.Text.RegularExpressions;

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

    public enum FieldState {
        Enabled,
        Disabled
    }


    public class SparkleSetupController {

        public event ChangePageEventHandler ChangePageEvent;
        public delegate void ChangePageEventHandler (PageType page, string [] warnings);
        
        public event UpdateProgressBarEventHandler UpdateProgressBarEvent;
        public delegate void UpdateProgressBarEventHandler (double percentage);

        public event UpdateSetupContinueButtonEventHandler UpdateSetupContinueButtonEvent;
        public delegate void UpdateSetupContinueButtonEventHandler (bool button_enabled);

        public event UpdateAddProjectButtonEventHandler UpdateAddProjectButtonEvent;
        public delegate void UpdateAddProjectButtonEventHandler (bool button_enabled);

        public event ChangeAddressFieldEventHandler ChangeAddressFieldEvent;
        public delegate void ChangeAddressFieldEventHandler (string text,
            string example_text, FieldState state);

        public event ChangePathFieldEventHandler ChangePathFieldEvent;
        public delegate void ChangePathFieldEventHandler (string text,
            string example_text, FieldState state);

        public readonly List<SparklePlugin> Plugins = new List<SparklePlugin> ();
        public SparklePlugin SelectedPlugin;


        public int SelectedPluginIndex {
            get {
                return Plugins.IndexOf (SelectedPlugin);
            }
        }

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

        public string PreviousAddress {
            get {
                return this.previous_address;
            }
        }

        public string PreviousPath {
            get {
                return this.previous_path;
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


        private string previous_address  = "";
        private string previous_path     = "";
        private string previous_url      = "";
        private string syncing_folder    = "";
        private int tutorial_page_number = 1;
        private PageType previous_page;


        public SparkleSetupController ()
        {
            string local_plugins_path = SparkleHelpers.CombineMore (
                Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
                "sparkleshare", "plugins");

            if (Directory.Exists (local_plugins_path))
                foreach (string xml_file_path in Directory.GetFiles (local_plugins_path, "*.xml"))
                    Plugins.Add (new SparklePlugin (xml_file_path));

            if (Directory.Exists (Program.Controller.PluginsPath)) {
                foreach (string xml_file_path in Directory.GetFiles (Program.Controller.PluginsPath, "*.xml")) {
                    if (xml_file_path.EndsWith ("own-server.xml"))
                        Plugins.Insert (0, new SparklePlugin (xml_file_path));
                    else
                        Plugins.Add (new SparklePlugin (xml_file_path));
                }
            }

            SelectedPlugin = Plugins [0];

            ChangePageEvent += delegate (PageType page, string [] warning) {
                this.previous_page = page;
            };
        }


        public void ShowSetupPage ()
        {
           if (ChangePageEvent != null)
               ChangePageEvent (PageType.Setup, null);
        }


        public void CheckSetupPage (string full_name, string email)
        {
            full_name = full_name.Trim ();
            email     = email.Trim ();

            bool fields_valid = full_name != null && full_name.Trim().Length > 0 &&
                IsValidEmail (email);

            if (UpdateSetupContinueButtonEvent != null)
                UpdateSetupContinueButtonEvent (fields_valid);
        }


        public void SetupPageCompleted (string full_name, string email)
        {
            Program.Controller.UserName  = full_name;
            Program.Controller.UserEmail = email;

            Program.Controller.GenerateKeyPair ();
            Program.Controller.UpdateState ();

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void TutorialPageCompleted ()
        {
            this.tutorial_page_number++;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void TutorialSkipped ()
        {
            this.tutorial_page_number = 4;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void ShowAddPage ()
        {
            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Add, null);

            SelectedPluginChanged (SelectedPluginIndex);
        }


        public void CheckAddPage (string address, string remote_path, int selected_plugin)
        {
            if (SelectedPluginIndex != selected_plugin)
                SelectedPluginChanged (selected_plugin);

            address     = address.Trim ();
            remote_path = remote_path.Trim ();

            bool fields_valid = address != null && address.Trim().Length > 0 &&
                remote_path != null && remote_path.Trim().Length > 0;

            if (UpdateAddProjectButtonEvent != null)
                UpdateAddProjectButtonEvent (fields_valid);
        }


        public void AddPageCompleted (string address, string path)
        {
            this.syncing_folder   = Path.GetFileNameWithoutExtension (path);
            this.previous_address = address;
            this.previous_path    = path;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing, null);

            // TODO: Remove events afterwards

            Program.Controller.FolderFetched += delegate (string [] warnings) {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Finished, warnings);

                this.previous_address = "";
                this.syncing_folder   = "";
                this.previous_url     = "";
                SelectedPlugin        = Plugins [0];
            };

            Program.Controller.FolderFetchError += delegate (string remote_url) {
                this.previous_url = remote_url;

                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Error, null);

                this.syncing_folder = "";
            };
            
            Program.Controller.FolderFetching += delegate (double percentage) {
                if (UpdateProgressBarEvent != null)
                    UpdateProgressBarEvent (percentage);
            };

            Program.Controller.FetchFolder (address, path);
        }


        public void ErrorPageCompleted ()
        {
            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Add, null);
        }


        public void SyncingCancelled ()
        {
            Program.Controller.StopFetcher ();

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Add, null);
        }


        // TODO: public void WindowClosed () { }


        public void FinishedPageCompleted ()
        {
            this.previous_address = "";
            this.previous_path    = "";
            Program.Controller.UpdateState ();
        }


        public void SelectedPluginChanged (int plugin_index)
        {
            SelectedPlugin = Plugins [plugin_index];

            if (SelectedPlugin.Address != null) {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent (SelectedPlugin.Address, "", FieldState.Disabled);

            } else if (SelectedPlugin.AddressExample != null) {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent ("", SelectedPlugin.AddressExample, FieldState.Enabled);
            } else {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent ("", "", FieldState.Enabled);
            }

            if (SelectedPlugin.Path != null) {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (SelectedPlugin.Path, "", FieldState.Disabled);

            } else if (SelectedPlugin.PathExample != null) {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent ("", SelectedPlugin.PathExample, FieldState.Enabled);

            } else {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent ("", "", FieldState.Enabled);
            }
        }


        private bool IsValidEmail (string email)
        {
            Regex regex = new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$",
                RegexOptions.IgnoreCase);

            return regex.IsMatch (email);
        }
    }
}
