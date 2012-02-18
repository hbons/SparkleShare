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
using System.Threading;

using SparkleLib;

namespace SparkleShare {

    public enum PageType {
        Setup,
        Add,
        Invite,
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

        public event ShowWindowEventHandler ShowWindowEvent;
        public delegate void ShowWindowEventHandler ();

        public event HideWindowEventHandler HideWindowEvent;
        public delegate void HideWindowEventHandler ();

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
        public delegate void ChangePathFieldEventHandler (string text, string example_text, FieldState state);

        public readonly List<SparklePlugin> Plugins = new List<SparklePlugin> ();
        public SparklePlugin SelectedPlugin;

        public int TutorialPageNumber { get; private set; }
        public string PreviousUrl { get; private set; }
        public string PreviousAddress { get; private set; }
        public string PreviousPath { get; private set; }
        public string SyncingFolder { get; private set; }
        public SparkleInvite PendingInvite;


        public int SelectedPluginIndex {
            get {
                return Plugins.IndexOf (SelectedPlugin);
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


        public SparkleSetupController ()
        {
            TutorialPageNumber = 1;
            PreviousAddress    = "";
            PreviousPath       = "";
            PreviousUrl        = "";
            SyncingFolder      = "";

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


            Program.Controller.InviteReceived += delegate (SparkleInvite invite) {
                PendingInvite = invite;

                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Invite, null);

                if (ShowWindowEvent != null)
                    ShowWindowEvent ();
            };


            Program.Controller.ShowSetupWindowEvent += delegate (PageType page_type) {
                if (PendingInvite != null) {
                    if (ShowWindowEvent != null)
                        ShowWindowEvent ();

                    return;
                }

                if (ChangePageEvent != null)
                    ChangePageEvent (page_type, null);

                if (ShowWindowEvent != null)
                    ShowWindowEvent ();

                if (page_type == PageType.Add)
                    SelectedPluginChanged (SelectedPluginIndex);
            };
        }


        public void PageCancelled ()
        {
            PendingInvite = null;

            if (HideWindowEvent != null)
                HideWindowEvent ();
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
            Program.Controller.ImportPrivateKey ();
            Program.Controller.UpdateState ();

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void TutorialSkipped ()
        {
            TutorialPageNumber = 4;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void TutorialPageCompleted ()
        {
            TutorialPageNumber++;

            if (TutorialPageNumber == 4) {
                if (HideWindowEvent != null)
                    HideWindowEvent ();

            } else {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Tutorial, null);
            }
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
            SyncingFolder   = Path.GetFileNameWithoutExtension (path);
            PreviousAddress = address;
            PreviousPath    = path;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing, null);

            // TODO: Remove events afterwards

            Program.Controller.FolderFetched += delegate (string [] warnings) {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Finished, warnings);

                PreviousAddress = "";
                SyncingFolder   = "";
                PreviousUrl     = "";
                SelectedPlugin  = Plugins [0];
            };

            Program.Controller.FolderFetchError += delegate (string remote_url) {
                Thread.Sleep (1000);
                PreviousUrl = remote_url;

                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Error, null);

                SyncingFolder = "";
            };
            
            Program.Controller.FolderFetching += delegate (double percentage) {
                if (UpdateProgressBarEvent != null)
                    UpdateProgressBarEvent (percentage);
            };

            Program.Controller.FetchFolder (address, path);
        }


        public void InvitePageCompleted ()
        {
            SyncingFolder   = Path.GetFileNameWithoutExtension (PendingInvite.RemotePath);
            PreviousAddress = PendingInvite.Address;
            // TODO: trailing slash should work
            PreviousPath    = PendingInvite.RemotePath;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing, null);

            if (!PendingInvite.Accept ()) {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Error, null);

                return;
            }


            // TODO: Remove events afterwards

            Program.Controller.FolderFetched += delegate (string [] warnings) {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Finished, warnings);

                PreviousAddress = "";
                SyncingFolder   = "";
                PreviousUrl     = "";
                SelectedPlugin  = Plugins [0];

                PendingInvite = null;
            };

            Program.Controller.FolderFetchError += delegate (string remote_url) {
                Thread.Sleep (1000);
                PreviousUrl = remote_url;

                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Error, null);

                SyncingFolder = "";
            };

            Program.Controller.FolderFetching += delegate (double percentage) {
                if (UpdateProgressBarEvent != null)
                    UpdateProgressBarEvent (percentage);
            };

            Program.Controller.FetchFolder (PendingInvite.Address, PendingInvite.RemotePath);
        }


        public void SyncingCancelled ()
        {
            Program.Controller.StopFetcher ();

            if (ChangePageEvent == null)
                return;

            if (PendingInvite != null)
                ChangePageEvent (PageType.Invite, null);
            else
                ChangePageEvent (PageType.Add, null);
        }


        public void ErrorPageCompleted ()
        {
            if (ChangePageEvent == null)
                return;

            if (PendingInvite != null)
                ChangePageEvent (PageType.Invite, null);
            else
                ChangePageEvent (PageType.Add, null);
        }


        public void FinishPageCompleted ()
        {
            PreviousAddress = "";
            PreviousPath    = "";

            Program.Controller.UpdateState ();

            if (HideWindowEvent != null)
                HideWindowEvent ();
        }


        private bool IsValidEmail (string email)
        {
            Regex regex = new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$",
                RegexOptions.IgnoreCase);

            return regex.IsMatch (email);
        }
    }
}
