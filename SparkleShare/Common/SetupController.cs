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

using Sparkles;

namespace SparkleShare {

    public enum PageType {
        None,
        Setup,
        Add,
        Invite,
        Syncing,
        Error,
        Finished,
        StorageSetup,
        CryptoSetup,
        CryptoPassword
    }

    public enum FieldState {
        Enabled,
        Disabled
    }


    public class SetupController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event ChangePageEventHandler ChangePageEvent = delegate { };
        public delegate void ChangePageEventHandler (PageType page, string [] warnings);
        
        public event UpdateProgressBarEventHandler UpdateProgressBarEvent = delegate { };
        public delegate void UpdateProgressBarEventHandler (double percentage, string information);

        public event UpdateSetupContinueButtonEventHandler UpdateSetupContinueButtonEvent = delegate { };
        public delegate void UpdateSetupContinueButtonEventHandler (bool button_enabled);

        public event UpdateCryptoSetupContinueButtonEventHandler UpdateCryptoSetupContinueButtonEvent = delegate { };
        public delegate void UpdateCryptoSetupContinueButtonEventHandler (bool button_enabled);

        public event UpdateCryptoPasswordContinueButtonEventHandler UpdateCryptoPasswordContinueButtonEvent = delegate { };
        public delegate void UpdateCryptoPasswordContinueButtonEventHandler (bool button_enabled);

        public event UpdateAddProjectButtonEventHandler UpdateAddProjectButtonEvent = delegate { };
        public delegate void UpdateAddProjectButtonEventHandler (bool button_enabled);

        public event ChangeAddressFieldEventHandler ChangeAddressFieldEvent = delegate { };
        public delegate void ChangeAddressFieldEventHandler (string text, string example_text, FieldState state);

        public event ChangePathFieldEventHandler ChangePathFieldEvent = delegate { };
        public delegate void ChangePathFieldEventHandler (string text, string example_text, FieldState state);

        public readonly List<Preset> Presets = new List<Preset> ();
        public Preset SelectedPreset;

        public bool WindowIsOpen { get; private set; }
        public SparkleInvite PendingInvite { get; private set; }
        public string PreviousUrl { get; private set; }
        public string PreviousAddress { get; private set; }
        public string PreviousPath { get; private set; }
        public string SyncingFolder { get; private set; }
        public double ProgressBarPercentage  { get; private set; }


        public int SelectedPresetIndex {
            get {
                return Presets.IndexOf (SelectedPreset);
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
        private bool fetch_prior_history = false;


        public SetupController ()
        {
            ChangePageEvent += delegate (PageType page_type, string [] warnings) {
                this.current_page = page_type;
            };

            PreviousAddress    = "";
            PreviousPath       = "";
            PreviousUrl        = "";
            SyncingFolder      = "";

            string local_presets_path = Preset.LocalPresetsPath;
            int local_presets_count   = 0;

            // Import all of the presets
            if (Directory.Exists (local_presets_path))
                // Local presets go first...
                foreach (string xml_file_path in Directory.GetFiles (local_presets_path, "*.xml")) {
                    Presets.Add (new Preset (xml_file_path));
                    local_presets_count++;
                }

            // ...system presets after that...
            if (Directory.Exists (SparkleShare.Controller.PresetsPath)) {
                foreach (string xml_file_path in Directory.GetFiles (SparkleShare.Controller.PresetsPath, "*.xml")) {
                    // ...and "Own server" at the very top
                    if (xml_file_path.EndsWith ("own-server.xml"))
                        Presets.Insert (0, new Preset (xml_file_path));
                    else
                        Presets.Add (new Preset (xml_file_path));
                }
            }

            SelectedPreset = Presets [0];

            SparkleShare.Controller.InviteReceived += delegate (SparkleInvite invite) {
                PendingInvite = invite;

                ChangePageEvent (PageType.Invite, null);
                ShowWindowEvent ();
            };

            SparkleShare.Controller.ShowSetupWindowEvent += delegate (PageType page_type) {
                if (page_type == PageType.StorageSetup ||
                    page_type == PageType.CryptoSetup ||
                    page_type == PageType.CryptoPassword) {

                    ChangePageEvent (page_type, null);
                    return;
                }

                if (PendingInvite != null) {
                    WindowIsOpen = true;
                    ShowWindowEvent ();
                    return;
                }

                if (this.current_page == PageType.Syncing ||
                    this.current_page == PageType.Finished ||
                    this.current_page == PageType.CryptoSetup ||
                    this.current_page == PageType.CryptoPassword) {

                    ShowWindowEvent ();
                    return;
                }

                if (page_type == PageType.Add) {
                    if (WindowIsOpen) {
                        if (this.current_page == PageType.Error ||
                            this.current_page == PageType.Finished ||
                            this.current_page == PageType.None) {

                            ChangePageEvent (PageType.Add, null);
                        }

                    } else if (!SparkleShare.Controller.FirstRun) {
                        WindowIsOpen = true;
                        ChangePageEvent (PageType.Add, null);
                    }

                    ShowWindowEvent ();
                    return;
                }

                WindowIsOpen = true;
                ChangePageEvent (page_type, null);
                ShowWindowEvent ();
            };
        }


        public void PageCancelled ()
        {
            PendingInvite   = null;
            SelectedPreset  = Presets [0];

            PreviousAddress = "";
            PreviousPath    = "";
            PreviousUrl     = "";

            this.saved_address     = "";
            this.saved_remote_path = "";
            this.fetch_prior_history = false;

            WindowIsOpen = false;
            HideWindowEvent ();
        }


        public void CheckSetupPage (string full_name, string email)
        {
            full_name = full_name.Trim ();
            email     = email.Trim ();

            bool fields_valid = (!string.IsNullOrEmpty (full_name) && IsValidEmail (email));
            UpdateSetupContinueButtonEvent (fields_valid);
        }

        
        public void SetupPageCancelled ()
        {
            SparkleShare.Controller.Quit ();
        }
        
        
        public void SetupPageCompleted (string full_name, string email)
        {
            SparkleShare.Controller.CurrentUser = new User (full_name, email);
            new Thread (() => SparkleShare.Controller.CreateStartupItem ()).Start ();

            ChangePageEvent (PageType.Add, null);
        }

      
        public void HistoryItemChanged (bool fetch_prior_history)
        {
            this.fetch_prior_history = fetch_prior_history;
        }


        public void SelectedPresetChanged (int preset_index)
        {
            SelectedPreset = Presets [preset_index];

            if (SelectedPreset.Address != null) {
                ChangeAddressFieldEvent (SelectedPreset.Address, "", FieldState.Disabled);

            } else if (SelectedPreset.AddressExample != null) {
                ChangeAddressFieldEvent (this.saved_address, SelectedPreset.AddressExample, FieldState.Enabled);

            } else {
                ChangeAddressFieldEvent (this.saved_address, "", FieldState.Enabled);
            }

            if (SelectedPreset.Path != null) {
                ChangePathFieldEvent (SelectedPreset.Path, "", FieldState.Disabled);

            } else if (SelectedPreset.PathExample != null) {
                ChangePathFieldEvent (this.saved_remote_path, SelectedPreset.PathExample, FieldState.Enabled);

            } else {
                ChangePathFieldEvent (this.saved_remote_path, "", FieldState.Enabled);
            }
        }


        public void CheckAddPage (string address, string remote_path, int selected_preset)
        {
            address     = address.Trim ();
            remote_path = remote_path.Trim ();

            if (selected_preset == 0)
                this.saved_address = address;

            this.saved_remote_path = remote_path;

            bool fields_valid = (!string.IsNullOrEmpty (address) &&
                !string.IsNullOrEmpty (remote_path) && !remote_path.Contains ("\""));

            UpdateAddProjectButtonEvent (fields_valid);
        }


        public void AddPageCompleted (string address, string remote_path)
        {
            SyncingFolder = Path.GetFileName (remote_path);

            if (remote_path.EndsWith (".git"))
                SyncingFolder = remote_path.Substring (0, remote_path.Length - 4);

            SyncingFolder = SyncingFolder.ReplaceUnderscoreWithSpace ();
            ProgressBarPercentage = 1.0;

            ChangePageEvent (PageType.Syncing, null);

            address     = Uri.EscapeUriString (address.Trim ());
            remote_path = remote_path.Trim ();
            remote_path = remote_path.TrimEnd ("/".ToCharArray ());

            if (SelectedPreset.PathUsesLowerCase)
                remote_path = remote_path.ToLower ();

            PreviousAddress = address;
            PreviousPath    = remote_path;

            SparkleShare.Controller.FolderFetched    += AddPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError += AddPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching   += SyncingPageFetchingDelegate;

            SparkleFetcherInfo info = new SparkleFetcherInfo {
                Address           = address,
                Fingerprint       = SelectedPreset.Fingerprint,
                RemotePath        = remote_path,
                FetchPriorHistory = this.fetch_prior_history,
                AnnouncementsUrl  = SelectedPreset.AnnouncementsUrl,
                Backend           = SelectedPreset.Backend 
            };

            new Thread (() => { SparkleShare.Controller.StartFetcher (info); }).Start ();
        }

        // The following private methods are
        // delegates used by the previous method

        private void AddPageFetchedDelegate (string remote_url, string [] warnings)
        {
            SyncingFolder = "";

            // Create a local preset for succesfully added projects, so
            // so the user can easily use the same host again
            if (SelectedPresetIndex == 0) {
                Preset new_preset;
                Uri uri = new Uri (remote_url);

                try {
                    string address = remote_url.Replace (uri.AbsolutePath, "");
                    new_preset = Preset.Create (uri.Host, address, address, "", "", "/path/to/project");
    
                    if (new_preset != null) {
                        Presets.Insert (1, new_preset);
                        Logger.LogInfo ("Controller", "Added preset for " + uri.Host);
                    }

                } catch {
                    Logger.LogInfo ("Controller", "Failed adding preset for " + uri.Host);
                }
            }

            ChangePageEvent (PageType.Finished, warnings);

            SparkleShare.Controller.FolderFetched    -= AddPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void AddPageFetchErrorDelegate (string remote_url, string [] errors)
        {
            SyncingFolder = "";
            PreviousUrl   = remote_url;

            ChangePageEvent (PageType.Error, errors);

            SparkleShare.Controller.FolderFetched    -= AddPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void SyncingPageFetchingDelegate (double percentage, double speed ,string information)
        {
            ProgressBarPercentage = percentage;

            if (speed > 0)
                information = speed.ToSize () + " – " + information;

            UpdateProgressBarEvent (ProgressBarPercentage, information);
        }


        public void InvitePageCompleted ()
        {
            SyncingFolder = Path.GetFileName (PendingInvite.RemotePath);

            if (PendingInvite.RemotePath.EndsWith (".git"))
                SyncingFolder = PendingInvite.RemotePath.Substring (0, PendingInvite.RemotePath.Length - 4);

			SyncingFolder   = SyncingFolder.ReplaceUnderscoreWithSpace ();
            PreviousAddress = PendingInvite.Address;
            PreviousPath    = PendingInvite.RemotePath;

            ChangePageEvent (PageType.Syncing, null);

            new Thread (() => {
                if (!PendingInvite.Accept (SparkleShare.Controller.UserAuthenticationInfo.PublicKey)) {
                    PreviousUrl = PendingInvite.Address + PendingInvite.RemotePath.TrimStart ("/".ToCharArray ());
                    ChangePageEvent (PageType.Error, new string [] { "error: Failed to upload the public key" });
                    return;
                }

                SparkleShare.Controller.FolderFetched    += InvitePageFetchedDelegate;
                SparkleShare.Controller.FolderFetchError += InvitePageFetchErrorDelegate;
                SparkleShare.Controller.FolderFetching   += SyncingPageFetchingDelegate;

                SparkleFetcherInfo info = new SparkleFetcherInfo {
                    Address           = PendingInvite.Address,
                    Fingerprint       = PendingInvite.Fingerprint,
                    RemotePath        = PendingInvite.RemotePath,
                    FetchPriorHistory = false, // TODO: checkbox on invite page
                    AnnouncementsUrl  = PendingInvite.AnnouncementsUrl
                };

                SparkleShare.Controller.StartFetcher (info);

            }).Start ();
        }

        // The following private methods are
        // delegates used by the previous method

        private void InvitePageFetchedDelegate (string remote_url, string [] warnings)
        {
            SyncingFolder   = "";
            PendingInvite = null;

            ChangePageEvent (PageType.Finished, warnings);

            SparkleShare.Controller.FolderFetched    -= AddPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }

        private void InvitePageFetchErrorDelegate (string remote_url, string [] errors)
        {
            SyncingFolder = "";
            PreviousUrl   = remote_url;

            ChangePageEvent (PageType.Error, errors);

            SparkleShare.Controller.FolderFetched    -= AddPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= AddPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching   -= SyncingPageFetchingDelegate;
        }


        public void SyncingCancelled ()
        {
            SparkleShare.Controller.StopFetcher ();

            if (PendingInvite != null)
                ChangePageEvent (PageType.Invite, null);
            else
                ChangePageEvent (PageType.Add, null);
        }


        public void ErrorPageCompleted ()
        {
            if (PendingInvite != null)
                ChangePageEvent (PageType.Invite, null);
            else
                ChangePageEvent (PageType.Add, null);
        }


        public void StoragePageCompleted (StorageType storage_type)
        {
            if (storage_type == StorageType.Encrypted) {
                ChangePageEvent (PageType.CryptoSetup, null);
                return;
            }

            ProgressBarPercentage = 100.0;
            ChangePageEvent (PageType.Syncing, null);

            new Thread (() => {
                Thread.Sleep (1000);
                SparkleShare.Controller.FinishFetcher (storage_type);

            }).Start ();
        }


        public void CheckCryptoSetupPage (string password)
        {
            new Thread (() => {
                bool is_valid_password = (password.Length > 0 && !password.StartsWith (" ") && !password.EndsWith (" "));
                UpdateCryptoSetupContinueButtonEvent (is_valid_password);
            }).Start ();
        }


        public void CheckCryptoPasswordPage (string password)
        {
            bool is_password_correct = SparkleShare.Controller.CheckPassword (password);
            UpdateCryptoPasswordContinueButtonEvent (is_password_correct);
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
            ChangePageEvent (PageType.Syncing, null);

            new Thread (() => {
                Thread.Sleep (1000);
                SparkleShare.Controller.FinishFetcher (StorageType.Encrypted, password);

            }).Start ();
        }


        public void CopyToClipboardClicked ()
        {
            SparkleShare.Controller.CopyToClipboard (SparkleShare.Controller.UserAuthenticationInfo.PublicKey);
        }


        public void ShowFilesClicked ()
        {
            string folder_name = Path.GetFileName (PreviousPath);
            folder_name = folder_name.ReplaceUnderscoreWithSpace ();

            // TODO: Open SparkleShare/$HOST
            SparkleShare.Controller.OpenSparkleShareFolder (folder_name);
            FinishPageCompleted ();
        }


        public void FinishPageCompleted ()
        {
            SelectedPreset  = Presets [0];
            PreviousUrl     = "";
            PreviousAddress = "";
            PreviousPath    = "";

            this.fetch_prior_history = false;
            this.saved_address     = "";
            this.saved_remote_path = "";
            this.current_page = PageType.None;

            WindowIsOpen = false;
            HideWindowEvent ();
        }


        private bool IsValidEmail (string email)
        {
            return new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]+$", RegexOptions.IgnoreCase).IsMatch (email);
        }
    }
}
