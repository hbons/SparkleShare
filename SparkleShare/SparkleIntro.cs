//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;
using Notifications;

namespace SparkleShare {

    public class SparkleIntro : SparkleWindow {

        private Entry NameEntry;
        private Entry EmailEntry;
        private SparkleEntry ServerEntry;
        private SparkleEntry FolderEntry;
        private String strServerEntry = "";
        private String strFolderEntry = "";
        private Button NextButton;
        private Button SyncButton;
        private bool ServerFormOnly;
        private string SecondaryTextColor;
        private ProgressBar progress_bar = new ProgressBar () { PulseStep = 0.01 };
        private Timer progress_bar_pulse_timer = new Timer () { Interval = 25, Enabled = true };


        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleIntro () : base ()
        {
            ServerFormOnly = false;
            SecondaryTextColor = SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive));
        }

        
        public void ShowAccountForm ()
        {
            Reset ();

            VBox layout_vertical = new VBox (false, 0);
            
                Deletable = false;

                Label header = new Label ("<span size='large'><b>" +
                                        _("Welcome to SparkleShare!") +
                                          "</b></span>") {
                    UseMarkup = true,
                    Xalign = 0
                };

                Label information = new Label (_("Before we can create a SparkleShare folder on this " +
                                                 "computer, we need a few bits of information from you.")) {
                    Xalign = 0,
                    Wrap   = true
                };

                Table table = new Table (4, 2, true) {
                    RowSpacing = 6
                };

                    Label name_label = new Label ("<b>" + _("Full Name:") + "</b>") {
                        UseMarkup = true,
                        Xalign    = 0
                    };

                    NameEntry = new Entry (SparkleShare.Controller.UserName);
                    NameEntry.Changed += delegate {
                        CheckAccountForm ();
                    };

                    EmailEntry = new Entry ();
                    EmailEntry.Changed += delegate {
                        CheckAccountForm ();
                    };

                    Label email_label = new Label ("<b>" + _("Email:") + "</b>") {
                        UseMarkup = true,
                        Xalign    = 0
                    };


                table.Attach (name_label, 0, 1, 0, 1);
                table.Attach (NameEntry, 1, 2, 0, 1);
                table.Attach (email_label, 0, 1, 1, 2);
                table.Attach (EmailEntry, 1, 2, 1, 2);
        
                    NextButton = new Button (_("Next")) {
                        Sensitive = false
                    };
    
                    NextButton.Clicked += delegate (object o, EventArgs args) {
                        NextButton.Remove (NextButton.Child);
                        NextButton.Add (new Label (_("Configuring…")));

                        NextButton.Sensitive = false;
                        table.Sensitive      = false;

                        NextButton.ShowAll ();
            
                        SparkleShare.Controller.UserName  = NameEntry.Text;
                        SparkleShare.Controller.UserEmail = EmailEntry.Text;

                        SparkleShare.Controller.GenerateKeyPair ();
                        SparkleShare.Controller.AddKey ();
                
                        SparkleUI.StatusIcon.CreateMenu ();

                        Deletable = true;
                        ShowServerForm ();
                    };
    
                AddButton (NextButton);

            layout_vertical.PackStart (header, false, false, 0);
            layout_vertical.PackStart (information, false, false, 21);
            layout_vertical.PackStart (new Label (""), false, false, 0);
            layout_vertical.PackStart (table, false, false, 0);

            Add (layout_vertical);
            CheckAccountForm ();
            ShowAll ();
        }


        public void ShowServerForm (bool server_form_only)
        {
            ServerFormOnly = server_form_only;
            ShowServerForm ();
        }


        public void ShowServerForm ()
        {
            Reset ();

            VBox layout_vertical = new VBox (false, 0);

                Label header = new Label ("<span size='large'><b>" +
                                                _("Where is your remote folder?") +
                                                "</b></span>") {
                    UseMarkup = true,
                    Xalign = 0
                };

                Table table = new Table (7, 2, false) {
                    RowSpacing = 12
                };

                    HBox layout_server = new HBox (true, 0);

                        ServerEntry = new SparkleEntry () { };
                        ServerEntry.Completion = new EntryCompletion();
                        ServerEntry.Completion.Model = ServerEntryCompletion();
                        ServerEntry.Completion.TextColumn = 0;

                        if (0 < strServerEntry.Trim().Length) {
                            ServerEntry.Text = strServerEntry;
                            ServerEntry.ExampleTextActive = false;
                        } else
                            ServerEntry.ExampleText = _("user@address-to-server.com");
                        
                        ServerEntry.Changed += CheckServerForm;

                        RadioButton radio_button = new RadioButton ("<b>" + _("On my own server:") + "</b>");

                    layout_server.Add (radio_button);
                    layout_server.Add (ServerEntry);
                    
                    string github_text = "<b>" + "Github" + "</b>\n" +
                          "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
                        _("Free hosting for Free and Open Source Software projects.") + "\n" + 
                        _("Also has paid accounts for extra private space and bandwidth.") +
                          "</span>";

                    RadioButton radio_button_github = new RadioButton (radio_button, github_text);

                    (radio_button_github.Child as Label).UseMarkup = true;
                    (radio_button_github.Child as Label).Wrap      = true;

                    string gnome_text = "<b>" + _("The GNOME Project") + "</b>\n" +
                          "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
                        _("GNOME is an easy to understand interface to your computer.") + "\n" +
                        _("Select this option if you’re a developer or designer working on GNOME.") +
                          "</span>";

                    RadioButton radio_button_gnome = new RadioButton (radio_button, gnome_text);

                    (radio_button_gnome.Child as Label).UseMarkup = true;
                    (radio_button_gnome.Child as Label).Wrap      = true;

                    string gitorious_text = "<b>" + _("Gitorious") + "</b>\n" +
                          "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
                        _("Completely Free as in Freedom infrastructure.") + "\n" +
                        _("Free accounts for Free and Open Source projects.") +
                          "</span>";
                    RadioButton radio_button_gitorious = new RadioButton (radio_button, gitorious_text) {
                        Xalign = 0
                    };

                    (radio_button_gitorious.Child as Label).UseMarkup = true;
                    (radio_button_gitorious.Child as Label).Wrap      = true;

                    radio_button_github.Toggled += delegate {
                        if (radio_button_github.Active)
                            FolderEntry.ExampleText = _("Username/Folder");
                    };

                    radio_button_gitorious.Toggled += delegate {
                        if (radio_button_gitorious.Active)
                            FolderEntry.ExampleText = _("Project/Folder");
                    };

                    radio_button_gnome.Toggled += delegate {
                        if (radio_button_gnome.Active)
                            FolderEntry.ExampleText = _("Project");
                    };

                    radio_button.Toggled += delegate {
                        if (radio_button.Active) {
                            FolderEntry.ExampleText = _("Folder");
                            ServerEntry.Sensitive   = true;
                            CheckServerForm ();
                        } else {
                            ServerEntry.Sensitive = false;
                            CheckServerForm ();
                        }

                        ShowAll ();
                    };

                table.Attach (layout_server,          0, 2, 1, 2);
                table.Attach (radio_button_github,    0, 2, 2, 3);
                table.Attach (radio_button_gitorious, 0, 2, 3, 4);
                table.Attach (radio_button_gnome,     0, 2, 4, 5);

                HBox layout_folder = new HBox (true, 0);

                    FolderEntry = new SparkleEntry () { };
                    FolderEntry.Completion = new EntryCompletion();
                    FolderEntry.Completion.Model = FolderEntryCompletion();
                    FolderEntry.Completion.TextColumn = 0;

                    if (0 < strFolderEntry.Trim().Length) {
                        FolderEntry.Text = strFolderEntry;
                        FolderEntry.ExampleTextActive = false;
                    } else
                        FolderEntry.ExampleText = _("Folder");
                    
                    FolderEntry.Changed += CheckServerForm;

                    Label folder_label = new Label (_("Folder Name:")) {
                        UseMarkup = true,
                        Xalign    = 1
                    };

                (radio_button.Child as Label).UseMarkup = true;

                layout_folder.PackStart (folder_label, true, true, 12);
                layout_folder.PackStart (FolderEntry, true, true, 0);

                    SyncButton = new Button (_("Sync"));
    
                    SyncButton.Clicked += delegate {
                        string folder_name    = FolderEntry.Text;
                        string server         = ServerEntry.Text;
                        string canonical_name = System.IO.Path.GetFileNameWithoutExtension (folder_name);

                        strServerEntry = ServerEntry.Text;
                        strFolderEntry = FolderEntry.Text;

                        if (radio_button_gitorious.Active)
                            server = "gitorious.org";

                        if (radio_button_github.Active)
                            server = "github.com";

                        if (radio_button_gnome.Active)
                            server = "gnome.org";

                        Application.Invoke (delegate {
                            Deletable = false;
                            ShowSyncingPage (canonical_name);
                        });
                
                        SparkleShare.Controller.FolderFetched += delegate {
                            Application.Invoke (delegate {
                                this.progress_bar_pulse_timer.Stop ();
                                Deletable = true;
                                UrgencyHint = true;
                                ShowSuccessPage (canonical_name);
                            });
                        };
                
                        SparkleShare.Controller.FolderFetchError += delegate {
                            Application.Invoke (delegate {
                                this.progress_bar_pulse_timer.Stop ();
                                Deletable = true;
                                ShowErrorPage ();
                            });
                        };

                        SparkleShare.Controller.FetchFolder (server, folder_name);
                    };


                if (ServerFormOnly) {
                    Button cancel_button = new Button (_("Cancel"));

                    cancel_button.Clicked += delegate {
                        Close ();
                    };

                    AddButton (cancel_button);
                } else {
                    Button skip_button = new Button (_("Skip"));

                    skip_button.Clicked += delegate {
                        ShowCompletedPage ();
                    };

                    AddButton (skip_button);
                }

            AddButton (SyncButton);

            layout_vertical.PackStart (header, false, false, 0);
            layout_vertical.PackStart (new Label (""), false, false, 3);
            layout_vertical.PackStart (table, false, false, 0);
            layout_vertical.PackStart (layout_folder, false, false, 6);

            Add (layout_vertical);
            CheckServerForm ();
            ShowAll ();
        }


        public void ShowInvitationPage (string server, string folder, string token)
        {
            VBox layout_vertical = new VBox (false, 0);

                Label header = new Label ("<span size='large'><b>" +
                                        _("Invitation received!") +
                                          "</b></span>") {
                    UseMarkup = true,
                    Xalign = 0
                };

                Label information = new Label (_("You've received an invitation to join a shared folder.\n" +
                                                 "We're ready to hook you up immediately if you wish.")) {
                    Xalign = 0,
                    Wrap   = true
                };

                Label question = new Label (_("Do you accept this invitation?")) {
                    Xalign = 0,
                    Wrap   = true
                };

                Table table = new Table (2, 2, false) {
                    RowSpacing = 6
                };

                    Label server_label = new Label (_("Server Address:")) {
                        Xalign    = 0
                    };

                    Label server_text = new Label ("<b>" + server + "</b>") {
                        UseMarkup = true,
                        Xalign    = 0
                    };

                    Label folder_label = new Label (_("Folder Name:")) {
                        Xalign    = 0
                    };

                    Label folder_text = new Label ("<b>" + folder + "</b>") {
                        UseMarkup = true,
                        Xalign    = 0
                    };

                table.Attach (folder_label, 0, 1, 0, 1);
                table.Attach (folder_text, 1, 2, 0, 1);
                table.Attach (server_label, 0, 1, 1, 2);
                table.Attach (server_text, 1, 2, 1, 2);

                Button reject_button = new Button (_("Reject"));
                Button accept_button = new Button (_("Accept and Sync"));

                    reject_button.Clicked += delegate {
                        Close ();
                    };

                    accept_button.Clicked += delegate {
                        string url  = "ssh://git@" + server + "/" + folder;        
                
                        SparkleShare.Controller.FolderFetched += delegate {
                            Application.Invoke (delegate {
                                this.progress_bar_pulse_timer.Stop ();
                                ShowSuccessPage (folder);
                            });
                        };
                
                        SparkleShare.Controller.FolderFetchError += delegate {
                            Application.Invoke (delegate {
                                this.progress_bar_pulse_timer.Stop ();
                                ShowErrorPage ();
                            });
                        };
        
                
                        SparkleShare.Controller.FetchFolder (url, folder);
                    };

                AddButton (reject_button);
                AddButton (accept_button);

            layout_vertical.PackStart (header, false, false, 0);
            layout_vertical.PackStart (information, false, false, 21);
            layout_vertical.PackStart (new Label (""), false, false, 0);
            layout_vertical.PackStart (table, false, false, 0);
            layout_vertical.PackStart (new Label (""), false, false, 0);
            layout_vertical.PackStart (question, false, false, 21);

            Add (layout_vertical);
            ShowAll ();
        }
        
        
        // The page shown when syncing has failed
        private void ShowErrorPage ()
        {
            Reset ();

                VBox layout_vertical = new VBox (false, 0);
        
                    Label header = new Label ("<span size='large'><b>" +
                                            _("Something went wrong…") +
                                              "</b></span>\n") {
                        UseMarkup = true,
                        Xalign = 0
                    };
    
                        Button try_again_button = new Button (_("Try Again")) {
                            Sensitive = true
                        };
        
                        try_again_button.Clicked += delegate (object o, EventArgs args) {
                            ShowServerForm ();
                        };
        
                    AddButton (try_again_button);
    
                layout_vertical.PackStart (header, false, false, 0);

            Add (layout_vertical);
            ShowAll ();
        }


        // The page shown when syncing has succeeded
        private void ShowSuccessPage (string folder_name)
        {
            Reset ();

                UrgencyHint = true;

                if (!HasToplevelFocus) {
                    string title   = String.Format (_("‘{0}’ has been successfully added"), folder_name);
                    string subtext = _("");

                    new SparkleBubble (title, subtext).Show ();
                }

                VBox layout_vertical = new VBox (false, 0);

                    Label header = new Label ("<span size='large'><b>" +
                                            _("Folder synced successfully!") +
                                              "</b></span>") {
                        UseMarkup = true,
                        Xalign = 0
                    };
        
                    Label information = new Label (
                        String.Format (_("Now you can access the synced files from ‘{0}’ in your SparkleShare folder."),
                            folder_name)) {
                        Xalign = 0,
                        Wrap   = true,
                        UseMarkup = true
                    };

                        // A button that opens the synced folder
                        Button open_folder_button = new Button (_("Open Folder"));

                        open_folder_button.Clicked += delegate {
                            SparkleShare.Controller.OpenSparkleShareFolder (folder_name);
                        };

                        Button finish_button = new Button (_("Finish"));
    
                        finish_button.Clicked += delegate (object o, EventArgs args) {
                            Close ();
                        };
    
                    AddButton (open_folder_button);
                    AddButton (finish_button);

                layout_vertical.PackStart (header, false, false, 0);
                layout_vertical.PackStart (information, false, false, 21);

            Add (layout_vertical);
            ShowAll ();
        }


        // The page shown whilst syncing
        private void ShowSyncingPage (string name)
        {
            Reset ();

                VBox layout_vertical = new VBox (false, 0);

                    Label header = new Label ("<span size='large'><b>" +
                                                    String.Format (_("Syncing folder ‘{0}’…"), name) +
                                                    "</b></span>") {
                        UseMarkup = true,
                        Xalign    = 0,
                        Wrap      = true
                    };

                    Label information = new Label (_("This may take a while.\n") +
                                                   _("Are you sure it’s not coffee o'clock?")) {
                        UseMarkup = true,
                        Xalign = 0
                    };

                        Button button = new Button () {
                            Sensitive = false,
                            Label = _("Finish")
                        };
        
                        button.Clicked += delegate {
                            Close ();
                        };

                    AddButton (button);

                layout_vertical.PackStart (header, false, false, 0);
                layout_vertical.PackStart (information, false, false, 21);

                this.progress_bar_pulse_timer.Elapsed += delegate {
                    Application.Invoke (delegate {
                        progress_bar.Pulse ();
                    });
                };

                if (this.progress_bar.Parent != null)
                    layout_vertical.Reparent(this.progress_bar);

                layout_vertical.PackStart (this.progress_bar, false, false, 54);

            Add (layout_vertical);
            ShowAll ();
        }


        // The page shown when the setup has been completed
        private void ShowCompletedPage ()
        {
            Reset ();

                VBox layout_vertical = new VBox (false, 0);

                Label header = new Label ("<span size='large'><b>" +
                                                _("SparkleShare is ready to go!") +
                                                "</b></span>") {
                    UseMarkup = true,
                    Xalign = 0
                };

                Label information = new Label (_("Now you can start accepting invitations from others. " + "\n" +
                                                 "Just click on invitations you get by email and " +
                                                 "we will take care of the rest.")) {
                    UseMarkup = true,
                    Wrap      = true,
                    Xalign    = 0
                };


                HBox link_wrapper = new HBox (false, 0);
                LinkButton link = new LinkButton ("http://www.sparkleshare.org/",
                    _("Learn how to host your own SparkleServer"));

                link_wrapper.PackStart (link, false, false, 0);

                layout_vertical.PackStart (header, false, false, 0);
                layout_vertical.PackStart (information, false, false, 21);
                layout_vertical.PackStart (link_wrapper, false, false, 0);

                    Button finish_button = new Button (_("Finish"));

                    finish_button.Clicked += delegate (object o, EventArgs args) {
                        Close ();
                    };

                AddButton (finish_button);

            Add (layout_vertical);
            ShowAll ();
        }


        // Enables or disables the 'Next' button depending on the 
        // entries filled in by the user
        private void CheckAccountForm ()
        {
            if (NameEntry.Text.Length > 0 &&
                IsValidEmail (EmailEntry.Text)) {

                NextButton.Sensitive = true;
            } else {
                NextButton.Sensitive = false;
            }
        }


        // Enables the Add button when the fields are
        // filled in correctly
        public void CheckServerForm (object o, EventArgs args)
        {
            CheckServerForm ();
        }


        // Enables the Add button when the fields are
        // filled in correctly
        public void CheckServerForm ()
        {
            SyncButton.Sensitive = false;

            if (FolderEntry.ExampleTextActive ||
                (ServerEntry.Sensitive && ServerEntry.ExampleTextActive))
                return;

            bool IsFolder = !FolderEntry.Text.Trim ().Equals ("");
            bool IsServer = !ServerEntry.Text.Trim ().Equals ("");

            if (ServerEntry.Sensitive == true) {
                if (IsServer && IsFolder)
                    SyncButton.Sensitive = true;
            } else if (IsFolder) {
                    SyncButton.Sensitive = true;
            }
        }


        TreeModel ServerEntryCompletion ()
        {
            ListStore store = new ListStore (typeof (string));
            List<string> Urls = SparkleLib.SparkleConfig.DefaultConfig.GetUrls();

            store.AppendValues ("user@localhost");
            store.AppendValues ("user@example.com");
            foreach (string url in Urls) {
                store.AppendValues (url);
            }

            return store;
        }


        TreeModel FolderEntryCompletion ()
        {
            ListStore store = new ListStore (typeof (string));

            store.AppendValues ("~/test.git");
            foreach (string folder in SparkleLib.SparkleConfig.DefaultConfig.Folders) {
                store.AppendValues (folder);
            }

            return store;
        }
        
        
        // Checks to see if an email address is valid
        private bool IsValidEmail (string email)
        {
            Regex regex = new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.IgnoreCase);
            return regex.IsMatch (email);
        }
    }
}
