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


namespace SparkleShare {

    public class SparkleSetup : SparkleSetupWindow {

        public SparkleSetupController Controller = new SparkleSetupController ();

        private string SecondaryTextColor;

        private Entry NameEntry;
        private Entry EmailEntry;
        private SparkleEntry ServerEntry;
        private SparkleEntry FolderEntry;

        private Button NextButton;
        private Button SyncButton;

        private Table Table;
        private ProgressBar progress_bar = new ProgressBar ();
        

        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleSetup () : base ()
        {
            SecondaryTextColor = SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive));

            Controller.ChangePageEvent += delegate (PageType type) {
                Application.Invoke (delegate {
                    Reset ();

                    switch (type) {
                    case PageType.Setup: {

                        Header = _("Welcome to SparkleShare!");
                        Description = _("Before we can create a SparkleShare folder on this " +
                                        "computer, we need a few bits of information from you.");

                        Table = new Table (4, 2, true) {
                            RowSpacing = 6
                        };

                            Label name_label = new Label ("<b>" + _("Full Name:") + "</b>") {
                                UseMarkup = true,
                                Xalign    = 0
                            };

                            NameEntry = new Entry (Program.Controller.UserName);
                            NameEntry.Changed += delegate {
                                CheckSetupPage ();
                            };

                            EmailEntry = new Entry ();
                            EmailEntry.Changed += delegate {
                                CheckSetupPage ();
                            };

                            Label email_label = new Label ("<b>" + _("Email:") + "</b>") {
                                UseMarkup = true,
                                Xalign    = 0
                            };

                        Table.Attach (name_label, 0, 1, 0, 1);
                        Table.Attach (NameEntry, 1, 2, 0, 1);
                        Table.Attach (email_label, 0, 1, 1, 2);
                        Table.Attach (EmailEntry, 1, 2, 1, 2);

                            NextButton = new Button (_("Next")) {
                                Sensitive = false
                            };

                            NextButton.Clicked += delegate (object o, EventArgs args) {
                                string full_name = NameEntry.Text;
                                string email     = EmailEntry.Text;

                                Controller.SetupPageCompleted (full_name, email);
                            };

                        AddButton (NextButton);
                        Add (Table);

                        CheckSetupPage ();

                        break;
                    } 

                    case PageType.Add: {

                        Header = _("Where is your remote folder?");

                        Table = new Table (6, 2, false) {
                            RowSpacing = 0
                        };

                            HBox layout_server = new HBox (true, 0);

                                // Own server radiobutton
                                RadioButton radio_button = new RadioButton ("<b>" + _("On my own server:") + "</b>");
                                (radio_button.Child as Label).UseMarkup = true;

                                radio_button.Toggled += delegate {
                                    if (radio_button.Active) {
                                        FolderEntry.ExampleText = _("Folder");
                                        ServerEntry.Sensitive   = true;
                                        CheckAddPage ();
                                    } else {
                                        ServerEntry.Sensitive = false;
                                        CheckAddPage ();
                                    }

                                    ShowAll ();
                                };

                                // Own server entry
                                ServerEntry = new SparkleEntry () { };
                                ServerEntry.Completion = new EntryCompletion();

                                ListStore server_store = new ListStore (typeof (string));

                                foreach (string host in Program.Controller.PreviousHosts)
                                    server_store.AppendValues (host);

                                ServerEntry.Completion.Model = server_store;
                                ServerEntry.Completion.TextColumn = 0;

                                if (!string.IsNullOrEmpty (Controller.PreviousServer)) {
                                    ServerEntry.Text = Controller.PreviousServer;
                                    ServerEntry.ExampleTextActive = false;
                                } else {
                                    ServerEntry.ExampleText = _("address-to-server.com");
                                }

                                ServerEntry.Changed += delegate {
                                    CheckAddPage ();
                                };

                            layout_server.Add (radio_button);
                            layout_server.Add (ServerEntry);

                        Table.Attach (layout_server,          0, 2, 1, 2);

                            // Github radiobutton
                            string github_text = "<b>" + "Github" + "</b>";

                            RadioButton radio_button_github = new RadioButton (radio_button, github_text);
                            (radio_button_github.Child as Label).UseMarkup = true;
                            (radio_button_github.Child as Label).Wrap      = true;

                            radio_button_github.Toggled += delegate {
                                if (radio_button_github.Active)
                                    FolderEntry.ExampleText = _("Username/Folder");
                            };


                            // Gitorious radiobutton
                            string gitorious_text = "<b>" + _("Gitorious") + "</b>";

                            RadioButton radio_button_gitorious = new RadioButton (radio_button, gitorious_text);
                            (radio_button_gitorious.Child as Label).UseMarkup = true;
                            (radio_button_gitorious.Child as Label).Wrap      = true;

                            radio_button_gitorious.Toggled += delegate {
                                if (radio_button_gitorious.Active)
                                    FolderEntry.ExampleText = _("Project/Folder");
                            };


                            // GNOME radiobutton
                            string gnome_text = "<b>" + _("The GNOME Project") + "</b>";

                            RadioButton radio_button_gnome = new RadioButton (radio_button, gnome_text);
                            (radio_button_gnome.Child as Label).UseMarkup = true;
                            (radio_button_gnome.Child as Label).Wrap      = true;

                            radio_button_gnome.Toggled += delegate {
                                if (radio_button_gnome.Active)
                                    FolderEntry.ExampleText = _("Project");
                            };

                        Table.Attach (radio_button_github,    0, 2, 2, 3);
                        Table.Attach (radio_button_gitorious, 0, 2, 3, 4);
                        Table.Attach (radio_button_gnome,     0, 2, 4, 5);

                            // Folder label and entry
                            HBox layout_folder = new HBox (true, 0);

                                Label folder_label = new Label (_("Folder Name:")) {
                                    UseMarkup = true,
                                    Xalign    = 1
                                };

                                FolderEntry             = new SparkleEntry ();
                                FolderEntry.ExampleText = _("Folder");
                                FolderEntry.Completion = new EntryCompletion();

                                ListStore folder_store = new ListStore (typeof (string));

                                //foreach (string host in Program.Controller.FolderPaths)
                                //    folder_store.AppendValues (host);

                                FolderEntry.Completion.Model = folder_store;
                                FolderEntry.Completion.TextColumn = 0;

                                FolderEntry.Changed += delegate {
                                    CheckAddPage ();
                                };

                            layout_folder.PackStart (folder_label, true, true, 12);
                            layout_folder.PackStart (FolderEntry, true, true, 0);

                        Table.Attach (layout_folder, 0, 2, 5, 6);

                        VBox box = new VBox (false, 0);
                        box.PackStart (Table, false, false, 0);
                        Add (box);

                            // Cancel button
                            Button cancel_button = new Button (_("Cancel"));

                            cancel_button.Clicked += delegate {
                                Close ();
                            };


                            // Sync button
                            SyncButton = new Button (_("Sync"));

                            SyncButton.Clicked += delegate {
                                string server         = ServerEntry.Text;
                                string folder_name    = FolderEntry.Text;

                                if (radio_button_gitorious.Active)
                                    server = "gitorious.org";

                                if (radio_button_github.Active)
                                    server = "github.com";

                                if (radio_button_gnome.Active)
                                    server = "gnome.org";

                                Controller.AddPageCompleted (server, folder_name);
                            };

                        AddButton (cancel_button);
                        AddButton (SyncButton);

                        CheckAddPage ();

                        break;
                    }

                    case PageType.Syncing: {

                        Header      = String.Format (_("Syncing folder ‘{0}’…"), Controller.SyncingFolder);
                        Description = _("This may take a while." + Environment.NewLine) +
                                      _("Are you sure it’s not coffee o'clock?");

                        Button finish_button = new Button () {
                            Sensitive = false,
                            Label = _("Finish")
                        };

                        Button cancel_button = new Button () {
                            Label = _("Cancel")
                        };

                        cancel_button.Clicked += delegate {
                            Controller.SyncingCancelled ();
                        };

                        AddButton (cancel_button);
                        AddButton (finish_button);

                        Controller.UpdateProgressBarEvent += delegate (double percentage) {
                            Application.Invoke (delegate {
                                this.progress_bar.Fraction = percentage / 100;
                            });
                        };

                        if (this.progress_bar.Parent != null)
                           (this.progress_bar.Parent as Container).Remove (this.progress_bar);

                        VBox bar_wrapper = new VBox (false , 0);
                        bar_wrapper.PackStart (this.progress_bar, false, false, 0);

                        Add (bar_wrapper);

                        break;
                    }

                    case PageType.Error: {

                        Header      = _("Something went wrong") + "…";

						VBox points = new VBox (false, 0);
						Image list_point_one   = new Image (SparkleUIHelpers.GetIcon ("list-point", 16)) {  };
						Image list_point_two   = new Image (SparkleUIHelpers.GetIcon ("list-point", 16)) {  };
						Image list_point_three = new Image (SparkleUIHelpers.GetIcon ("list-point", 16)) {  };

                        Label label_one = new Label () {
                            Text   = "First, have you tried turning it off and on again?",
                            Wrap   = true,
                            Xalign = 0
                        };

                        Label label_two = new Label () {
                            Markup = "<b>" + Controller.PreviousUrl + "</b> is the address we've compiled. " +
                                     "Does this look alright?",
                            Wrap   = true,
                            Xalign = 0
                        };

                        Label label_three = new Label () {
                            Text   = "The host needs to know who you are. Did you upload the key that's in " +
                                     "your SparkleShare folder?",
                            Wrap   = true,
                            Xalign = 0
                        };

						
                        points.PackStart (new Label ("Please check the following:") { Xalign = 0 }, false, false, 6);

                        HBox point_one = new HBox (false, 0);
						point_one.PackStart (list_point_one, false, false, 0);
						point_one.PackStart (label_one, true, true, 12);
						points.PackStart (point_one, false, false, 12);
						
						HBox point_two = new HBox (false, 0);
						point_two.PackStart (list_point_two, false, false, 0);
						point_two.PackStart (label_two, true, true, 12);
						points.PackStart (point_two, false, false, 12);
                          
                        HBox point_three = new HBox (false, 0);
						point_three.PackStart (list_point_three, false, false, 0);
						point_three.PackStart (label_three, true, true, 12);
						points.PackStart (point_three, false, false, 12);

                        points.PackStart (new Label (""), true, true, 0);


                        Button try_again_button = new Button (_("Try Again…")) {
                            Sensitive = true
                        };

                        try_again_button.Clicked += delegate {
                            Controller.ErrorPageCompleted ();
                        };

                        AddButton (try_again_button);
                        Add (points);

                        break;
                    }

                    case PageType.Finished: {

                        UrgencyHint = true;

                        if (!HasToplevelFocus) {
                            string title   = String.Format (_("‘{0}’ has been successfully added"), Controller.SyncingFolder);
                            string subtext = "";

                            SparkleUI.Bubbles.Controller.ShowBubble (title, subtext, null);
                        }

                        Header      = _("Folder synced successfully!");
                        Description = _("Access the synced files from your SparkleShare folder.");

                        // A button that opens the synced folder
                        Button open_folder_button = new Button (_("Open Folder"));

                        open_folder_button.Clicked += delegate {
                          Program.Controller.OpenSparkleShareFolder (Controller.SyncingFolder);
                        };

                        Button finish_button = new Button (_("Finish"));

                        finish_button.Clicked += delegate {
                            Close ();
                        };

                        Add (null);

                        AddButton (open_folder_button);
                        AddButton (finish_button);

                        break;
                    }


                    case PageType.Tutorial: {

                        switch (Controller.TutorialPageNumber) {
                        case 1: {
                            Header      = _("What's happening next?");
                            Description = _("SparkleShare creates a special folder in your personal folder " +
                                "that will keep track of your projects.");

                            Button skip_tutorial_button = new Button (_("Skip Tutorial"));
                            skip_tutorial_button.Clicked += delegate {
                                Controller.TutorialSkipped ();
                            };

                            Button continue_button = new Button (_("Continue"));
                            continue_button.Clicked += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            Image slide = SparkleUIHelpers.GetImage ("tutorial-slide-1.png");

                            Add (slide);

                            AddButton (skip_tutorial_button);
                            AddButton (continue_button);

                            break;
                        }

                        case 2: {
                            Header      = _("Sharing files with others");
                            Description = _("All files added to your project folders are synced with the host " +
                                "automatically, as well as with your collaborators.");

                            Button continue_button = new Button (_("Continue"));
                            continue_button.Clicked += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            Image slide = SparkleUIHelpers.GetImage ("tutorial-slide-2.png");

                            Add (slide);
                            AddButton (continue_button);

                            break;
                        }

                        case 3: {
                            Header      = _("The status icon is here to help");
                            Description = _("It shows the syncing process status, " +
                                "and contains links to your projects and the event log.");

                            Button continue_button = new Button (_("Continue"));
                            continue_button.Clicked += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            Image slide = SparkleUIHelpers.GetImage ("tutorial-slide-3.png");

                            Add (slide);
                            AddButton (continue_button);

                            break;
                        }

                        case 4: {
                            Header      = _("Adding projects to SparkleShare");
                            Description = _("Just click this button when you see it on the web, and " +
                                "the project will be automatically added:");

                            Button continue_button = new Button (_("Continue"));
                            continue_button.Clicked += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            Label label = new Label (_("…or select <b>‘Add Project…’</b> from the status icon menu " +
                                "to add one by hand.")) {
                                Wrap   = true,
                                Xalign = 0,
                                UseMarkup = true
                            };

                            Image slide = SparkleUIHelpers.GetImage ("tutorial-slide-4.png");

                            Button add_project_button = new Button (_("Add Project…"));
                            add_project_button.Clicked += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            Button finish_button = new Button (_("Finish"));
                            finish_button.Clicked += delegate {
                                Close ();
                            };


                            VBox box = new VBox (false, 0);
                            box.Add (slide);
                            box.Add (label);

                            Add (box);

                            AddButton (add_project_button);
                            AddButton (finish_button);

                            break;
                        }
                        }

                        break;
                    }
                    }

                    ShowAll ();
                });
            };
        }


        // Enables or disables the 'Next' button depending on the
        // entries filled in by the user
        private void CheckSetupPage ()
        {
            if (NameEntry.Text.Length > 0 &&
                Program.Controller.IsValidEmail (EmailEntry.Text)) {

                NextButton.Sensitive = true;
            } else {
                NextButton.Sensitive = false;
            }
        }


        // Enables or disables the 'Next' button depending on the
        // entries filled in by the user
        public void CheckAddPage ()
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

    }
}
