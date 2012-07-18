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
        None,
        Setup,
        Add,
        Invite,
        Syncing,
        Error,
        Finished,
        Tutorial,
        CryptoSetup,
        CryptoPassword
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

        public event UpdateCryptoSetupContinueButtonEventHandler UpdateCryptoSetupContinueButtonEvent;
        public delegate void UpdateCryptoSetupContinueButtonEventHandler (bool button_enabled);

        public event UpdateCryptoPasswordContinueButtonEventHandler UpdateCryptoPasswordContinueButtonEvent;
        public delegate void UpdateCryptoPasswordContinueButtonEventHandler (bool button_enabled);

        public event UpdateAddProjectButtonEventHandler UpdateAddProjectButtonEvent;
        public delegate void UpdateAddProjectButtonEventHandler (bool button_enabled);

        public event ChangeAddressFieldEventHandler ChangeAddressFieldEvent;
        public delegate void ChangeAddressFieldEventHandler (string text, string example_text, FieldState state);

        public event ChangePathFieldEventHandler ChangePathFieldEvent;
        public delegate void ChangePathFieldEventHandler (string text, string example_text, FieldState state);

        public readonly List<SparklePlugin> Plugins = new List<SparklePlugin> ();
        public SparklePlugin SelectedPlugin;

        public bool WindowIsOpen { get; private set; }
        public SparkleInvite PendingInvite { get; private set; }
        public int TutorialPageNumber { get; private set; }
        public string PreviousUrl { get; private set; }
        public string PreviousAddress { get; private set; }
        public string PreviousPath { get; private set; }
        public string SyncingFolder { get; private set; }
        public double ProgressBarPercentage  { get; private set; }


        public int SelectedPluginIndex {
            get {
                return Plugins.IndexOf (SelectedPlugin);
            }
        }

        public string GuessedUserName {
            get {
                return Program.Controller.CurrentUser.Name;
            }
        }

        public string GuessedUserEmail {
            get {
                if (Program.Controller.CurrentUser.Email.Equals ("Unknown"))
                    return "";
                else
                    return Program.Controller.CurrentUser.Email;
            }
        }

        public bool FetchPriorHistory {
            get {
                return this.fetch_prior_history;
            }
        }

        private PageType current_page;
        private string saved_address     = "";
        private string saved_remote_path = "";
        private bool create_startup_item = true;
        private bool fetch_prior_history = false;


        public SparkleSetupController ()
        {
            ChangePageEvent += delegate (PageType page_type, string [] warnings) {
                this.current_page = page_type;
            };

            TutorialPageNumber = 0;
            PreviousAddress    = "";
            PreviousPath       = "";
            PreviousUrl        = "";
            SyncingFolder      = "";

            string local_plugins_path = SparklePlugin.LocalPluginsPath;
            int local_plugins_count   = 0;

            // Import all of the plugins
            if (Directory.Exists (local_plugins_path))
                // Local plugins go first...
                foreach (string xml_file_path in Directory.GetFiles (local_plugins_path, "*.xml")) {
                    Plugins.Add (new SparklePlugin (xml_file_path));
                    local_plugins_count++;
                }

            // ...system plugins after that...
            if (Directory.Exists (Program.Controller.PluginsPath)) {
                foreach (string xml_file_path in Directory.GetFiles (Program.Controller.PluginsPath, "*.xml")) {
                    // ...and "Own server" at the very top
                    if (xml_file_path.EndsWith ("own-server.xml")) {
                        Plugins.Insert (0, new SparklePlugin (xml_file_path));

                    } else if (xml_file_path.EndsWith ("ssnet.xml")) {
                        // Plugins.Insert ((local_plugins_count + 1), new SparklePlugin (xml_file_path)); TODO: Skip this plugin for now

                    } else {
                        Plugins.Add (new SparklePlugin (xml_file_path));
                    }
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
                if (page_type == PageType.CryptoSetup || page_type == PageType.CryptoPassword) {
                    if (ChangePageEvent != null)
                        ChangePageEvent (page_type, null);
                    
                    return;
                }

                if (PendingInvite != null) {
                    WindowIsOpen = true;

                    if (ShowWindowEvent != null)
                        ShowWindowEvent ();

                    return;
                }

                if (this.current_page == PageType.Syncing ||
                    this.current_page == PageType.Finished ||
                    this.current_page == PageType.CryptoSetup ||
                    this.current_page == PageType.CryptoPassword) {

                    if (ShowWindowEvent != null)
                        ShowWindowEvent ();

                    return;
                }

                if (page_type == PageType.Add) {
                    if (WindowIsOpen) {
                        if (this.current_page == PageType.Error ||
                            this.current_page == PageType.Finished ||
                            this.current_page == PageType.None) {
                            
                            if (ChangePageEvent != null)
                                ChangePageEvent (PageType.Add, null);
                        }

                        if (ShowWindowEvent != null)
                            ShowWindowEvent ();

                    } else if (!Program.Controller.FirstRun && TutorialPageNumber == 0) {
                        if (ChangePageEvent != null)
                            ChangePageEvent (PageType.Add, null);

                        WindowIsOpen = true;

                        if (ShowWindowEvent != null)
                            ShowWindowEvent ();
                    }

                    return;
                }

                WindowIsOpen = true;

                if (ChangePageEvent != null)
                    ChangePageEvent (page_type, null);

                if (ShowWindowEvent != null)
                    ShowWindowEvent ();
            };
        }


        public void PageCancelled ()
        {
            PendingInvite   = null;
            SelectedPlugin  = Plugins [0];
            PreviousAddress = "";
            PreviousPath    = "";
            PreviousUrl     = "";

            this.fetch_prior_history = false;

            WindowIsOpen = false;

            if (HideWindowEvent != null)
                HideWindowEvent ();
        }


        public void CheckSetupPage (string full_name, string email)
        {
            full_name = full_name.Trim ();
            email     = email.Trim ();

            bool fields_valid = (!string.IsNullOrEmpty (full_name) && IsValidEmail (email));

            if (UpdateSetupContinueButtonEvent != null)
                UpdateSetupContinueButtonEvent (fields_valid);
        }

        
        public void SetupPageCancelled ()
        {
            Program.Controller.Quit ();
        }
        
        
        public void SetupPageCompleted (string full_name, string email)
        {
            Program.Controller.CurrentUser = new SparkleUser (full_name, email);

            new Thread (() => {
                string keys_path     = Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
                string key_file_name = DateTime.Now.ToString ("YYYY-MM-dd HH\\hmm");

                string [] key_pair = SparkleKeys.GenerateKeyPair (keys_path, key_file_name);
                SparkleKeys.ImportPrivateKey (key_pair [0]);

                string link_code_file_path = Path.Combine (Program.Controller.FoldersPath,
                    Program.Controller.CurrentUser.Name + "'s link code.txt");

                // Create an easily accessible copy of the public
                // key in the user's SparkleShare folder
                File.Copy (key_pair [1], link_code_file_path, true);

            }).Start ();

            TutorialPageNumber = 1;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void TutorialSkipped ()
        {
            TutorialPageNumber = 4;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Tutorial, null);
        }


        public void HistoryItemChanged (bool fetch_prior_history)
        {
            this.fetch_prior_history = fetch_prior_history;
        }


        public void TutorialPageCompleted ()
        {
            TutorialPageNumber++;

            if (TutorialPageNumber == 5) {
                TutorialPageNumber = 0;

                WindowIsOpen = false;

                if (HideWindowEvent != null)
                    HideWindowEvent ();

                if (this.create_startup_item)
                    Program.Controller.CreateStartupItem ();

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
                    ChangeAddressFieldEvent (this.saved_address, SelectedPlugin.AddressExample, FieldState.Enabled);

            } else {
                if (ChangeAddressFieldEvent != null)
                    ChangeAddressFieldEvent (this.saved_address, "", FieldState.Enabled);
            }

            if (SelectedPlugin.Path != null) {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (SelectedPlugin.Path, "", FieldState.Disabled);

            } else if (SelectedPlugin.PathExample != null) {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (this.saved_remote_path, SelectedPlugin.PathExample, FieldState.Enabled);

            } else {
                if (ChangePathFieldEvent != null)
                    ChangePathFieldEvent (this.saved_remote_path, "", FieldState.Enabled);
            }
        }


        public void StartupItemChanged (bool create_startup_item)
        {
            this.create_startup_item = create_startup_item;
        }


        public void CheckAddPage (string address, string remote_path, int selected_plugin)
        {
            address     = address.Trim ();
            remote_path = remote_path.Trim ();

            if (selected_plugin == 0)
                this.saved_address = address;

            this.saved_remote_path = remote_path;

            bool fields_valid = (!string.IsNullOrEmpty (address) &&
                                 !string.IsNullOrEmpty (remote_path) &&
                                 !remote_path.Contains ("\""));

            if (UpdateAddProjectButtonEvent != null)
                UpdateAddProjectButtonEvent (fields_valid);
        }


        public void AddPageCompleted (string address, string remote_path)
        {
            SyncingFolder         = Path.GetFileNameWithoutExtension (remote_path);
            ProgressBarPercentage = 1.0;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing, null);

            address     = address.Trim ();
            remote_path = remote_path.Trim ();
            remote_path = remote_path.TrimEnd ("/".ToCharArray ());

            if (SelectedPlugin.PathUsesLowerCase)
                remote_path = remote_path.ToLower ();

            PreviousAddress = address;
            PreviousPath    = remote_path;

            Program.Controller.FolderFetched    += AddPageFetchedDelegate;
            Program.Controller.FolderFetchError += AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   += SyncingPageFetchingDelegate;

            new Thread (() => {
                Program.Controller.StartFetcher (address, SelectedPlugin.Fingerprint, remote_path,
                    SelectedPlugin.AnnouncementsUrl, this.fetch_prior_history);

            }).Start ();
        }

        // The following private methods are
        // delegates used by the previous method

        private void AddPageFetchedDelegate (string remote_url, string [] warnings)
        {
            SyncingFolder = "";

            // Create a local plugin for succesfully added projects, so
            // so the user can easily use the same host again
            if (SelectedPluginIndex == 0) {
                SparklePlugin new_plugin;
                Uri uri = new Uri (remote_url);

                try {
                    string address = remote_url.Replace (uri.AbsolutePath, "");
    
                    new_plugin = SparklePlugin.Create (
                        uri.Host, address, address, "", "", "/path/to/project");
    
                    if (new_plugin != null) {
                        Plugins.Insert (1, new_plugin);
                        SparkleHelpers.DebugInfo ("Controller", "Added plugin for " + uri.Host);
                    }

                } catch {
                    SparkleHelpers.DebugInfo ("Controller", "Failed adding plugin for " + uri.Host);
                }
            }

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Finished, warnings);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void AddPageFetchErrorDelegate (string remote_url, string [] errors)
        {
            SyncingFolder = "";
            PreviousUrl   = remote_url;
           
            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Error, errors);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void SyncingPageFetchingDelegate (double percentage)
        {
            ProgressBarPercentage = percentage;

            if (UpdateProgressBarEvent != null)
                UpdateProgressBarEvent (ProgressBarPercentage);
        }


        public void InvitePageCompleted ()
        {
            SyncingFolder   = Path.GetFileNameWithoutExtension (PendingInvite.RemotePath);
            PreviousAddress = PendingInvite.Address;
            PreviousPath    = PendingInvite.RemotePath;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing, null);

            if (!PendingInvite.Accept ()) {
                if (ChangePageEvent != null)
                    ChangePageEvent (PageType.Error, null);

                return;
            }

            Program.Controller.FolderFetched    += InvitePageFetchedDelegate;
            Program.Controller.FolderFetchError += InvitePageFetchErrorDelegate;
            Program.Controller.FolderFetching   += SyncingPageFetchingDelegate;

            Program.Controller.StartFetcher (PendingInvite.Address, PendingInvite.Fingerprint,
                PendingInvite.RemotePath, PendingInvite.AnnouncementsUrl, false); // TODO: checkbox on invite page
        }

        // The following private methods are
        // delegates used by the previous method

        private void InvitePageFetchedDelegate (string remote_url, string [] warnings)
        {
            SyncingFolder   = "";
            PendingInvite = null;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Finished, warnings);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void InvitePageFetchErrorDelegate (string remote_url, string [] errors)
        {
            SyncingFolder = "";
            PreviousUrl   = remote_url;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Error, errors);

            Program.Controller.FolderFetched    -= AddPageFetchedDelegate;
            Program.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            Program.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }


        public void SyncingCancelled ()
        {
            Program.Controller.StopFetcher ();

            if (ChangePageEvent != null) {
                if (PendingInvite != null)
                    ChangePageEvent (PageType.Invite, null);
                else
                    ChangePageEvent (PageType.Add, null);
            }
        }


        public void ErrorPageCompleted ()
        {
            if (PendingInvite != null)
                ChangePageEvent (PageType.Invite, null);
            else
                ChangePageEvent (PageType.Add, null);
        }


        public void CheckCryptoSetupPage (string password)
        {
            bool valid_password = (password.Length > 0 && !password.Contains (" "));

            if (UpdateCryptoSetupContinueButtonEvent != null)
                UpdateCryptoSetupContinueButtonEvent (valid_password);
        }


        public void CheckCryptoPasswordPage (string password)
        {
            bool password_correct = Program.Controller.CheckPassword (password);

            if (UpdateCryptoPasswordContinueButtonEvent != null)
                UpdateCryptoPasswordContinueButtonEvent (password_correct);
        }


        public void CryptoPageCancelled ()
        {
            SyncingCancelled ();
        }


        public void CryptoSetupPageCompleted (string password)
        {
            CryptoPasswordPageCompleted (password);
        }


        public void CryptoPasswordPageCompleted (string password)
        {
            ProgressBarPercentage = 100.0;

            if (ChangePageEvent != null)
                ChangePageEvent (PageType.Syncing, null);

            new Thread (() => {
                Thread.Sleep (1000);
                Program.Controller.FinishFetcher (password);

            }).Start ();
        }


        public void OpenFolderClicked ()
        {
            Program.Controller.OpenSparkleShareFolder (Path.GetFileName (PreviousPath));
            FinishPageCompleted ();
        }


        public void FinishPageCompleted ()
        {
            SelectedPlugin  = Plugins [0];
            PreviousUrl     = "";
            PreviousAddress = "";
            PreviousPath    = "";
            this.fetch_prior_history = false;

            this.current_page = PageType.None;

            if (HideWindowEvent != null)
                HideWindowEvent ();
        }


        private bool IsValidEmail (string email)
        {
            return new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.IgnoreCase).IsMatch (email);
        }
    }
}
