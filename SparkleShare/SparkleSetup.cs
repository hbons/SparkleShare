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
        private string SecondaryTextColorSelected;

        private Entry NameEntry;
        private Entry EmailEntry;
        private SparkleEntry AddressEntry;
        private SparkleEntry PathEntry;

        private Button NextButton;
        private Button SyncButton;

        private Table Table;
        private ProgressBar progress_bar = new ProgressBar ();
        

        // Short alias for the translations
        public static string _ (string s)
        {
            return Catalog.GetString (s);
        }

        public static int GetSelected (TreeView tree)
        {
            TreeIter iter;
            TreeModel model;
            tree.Selection.GetSelected(out model, out iter);
            return int.Parse (model.GetPath (iter).ToString ());
        }

        public SparkleSetup () : base ()
        {
            SecondaryTextColor         = SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive));
            SecondaryTextColorSelected =
                SparkleUIHelpers.GdkColorToHex (
                    MixColors (
                        new TreeView ().Style.Foreground (StateType.Selected),
                        new TreeView ().Style.Background (StateType.Selected),
                        0.15
                    )
                );

            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
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

                            NameEntry = new Entry (Controller.GuessedUserName);
                            NameEntry.Changed += delegate {
                                Controller.CheckSetupPage (NameEntry.Text, EmailEntry.Text);
                            };

                            EmailEntry = new Entry (Controller.GuessedUserEmail);
                            EmailEntry.Changed += delegate {
                                Controller.CheckSetupPage (NameEntry.Text, EmailEntry.Text);
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

                        Controller.CheckSetupPage (NameEntry.Text, EmailEntry.Text);

                        break;
                    } 

                    case PageType.Add: {

                        Header = _("Where's your project hosted?");

                        VBox layout_vertical = new VBox (false, 12);
                        HBox layout_fields   = new HBox (true, 12);
                        VBox layout_address  = new VBox (true, 0);
                        VBox layout_path     = new VBox (true, 0);


                        ListStore store = new ListStore (typeof (Gdk.Pixbuf),
                            typeof (string), typeof (SparklePlugin));

                        TreeView tree = new TreeView (store) { HeadersVisible = false };
                        ScrolledWindow scrolled_window = new ScrolledWindow ();
                        scrolled_window.AddWithViewport (tree);

                        // Icon column
                        tree.AppendColumn ("Icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
                        tree.Columns [0].Cells [0].Xpad = 6;

                        // Service column
                        TreeViewColumn service_column = new TreeViewColumn () { Title = "Service" };
                        CellRendererText service_cell = new CellRendererText () { Ypad = 4 };
                        service_column.PackStart (service_cell, true);
                        service_column.SetCellDataFunc (service_cell, new TreeCellDataFunc (RenderServiceColumn));

                        foreach (SparklePlugin plugin in Controller.Plugins) {
                            store.AppendValues (
                                new Gdk.Pixbuf (plugin.ImagePath),
                                "<span size=\"small\"><b>" + plugin.Name + "</b>\n" +
                                  "<span fgcolor=\"" + SecondaryTextColorSelected + "\">" +
                                  plugin.Description + "</span>" +
                                "</span>",
                                plugin);
                        }

                        tree.AppendColumn (service_column);

                        PathEntry = new SparkleEntry ();
                        AddressEntry = new SparkleEntry ();


                        // Select the first plugin by default
                        TreeSelection default_selection = tree.Selection;
                        TreePath default_path = new TreePath ("0");
                        default_selection.SelectPath (default_path);
                        Controller.SelectedPluginChanged (0);

                        Controller.ChangeAddressFieldEvent += delegate (string text,
                            string example_text, FieldState state) {
                            Console.WriteLine ("> " +  text);
                            Application.Invoke (delegate {
                                AddressEntry.Text        = text;
                                AddressEntry.Sensitive   = (state == FieldState.Enabled);

                                if (string.IsNullOrEmpty (example_text))
                                    AddressEntry.ExampleText = null;
                                else
                                    AddressEntry.ExampleText = example_text;

                                if (string.IsNullOrEmpty (text))
                                    AddressEntry.ExampleTextActive = true;
                                else
                                    AddressEntry.ExampleTextActive = false;
                            });
                        };

                        Controller.ChangePathFieldEvent += delegate (string text,
                            string example_text, FieldState state) {

                            Application.Invoke (delegate {
                                PathEntry.Text        = text;
                                PathEntry.Sensitive   = (state == FieldState.Enabled);

                                if (string.IsNullOrEmpty (example_text))
                                    PathEntry.ExampleText = null;
                                else
                                    PathEntry.ExampleText = example_text;

                                if (string.IsNullOrEmpty (text))
                                    PathEntry.ExampleTextActive = true;
                                else
                                    PathEntry.ExampleTextActive = false;
                            });
                        };

                        // Update the address field text when the selection changes
                        tree.CursorChanged += delegate (object sender, EventArgs e) {
                            Controller.SelectedPluginChanged (GetSelected(sender as TreeView));

                            // TODO: Scroll to selected row when using arrow keys
                        };

                        tree.Model.Foreach (new TreeModelForeachFunc (delegate (TreeModel model,
                            TreePath path, TreeIter iter) {

                            string address;

                            try {
                                address = (model.GetValue (iter, 2) as SparklePlugin).Address;

                            } catch (NullReferenceException) {
                                address = "";
                            }

                            if (!string.IsNullOrEmpty (address) &&
                                address.Equals (Controller.PreviousAddress)) {

                                tree.SetCursor (path, service_column, false);
                                SparklePlugin plugin = (SparklePlugin) model.GetValue (iter, 2);

                                if (plugin.Address != null) {
                                    AddressEntry.Sensitive = false;}

                                if (plugin.Path != null)
                                    PathEntry.Sensitive = false;

                                // TODO: Scroll to the selection

                                return true;
                            } else {
                                return false;
                            }
                        }));

                        AddressEntry.Completion = new EntryCompletion();
                        ListStore server_store = new ListStore (typeof (string));

                        foreach (string host in Program.Controller.PreviousHosts)
                            server_store.AppendValues (host);

                        AddressEntry.Completion.Model      = server_store;
                        AddressEntry.Completion.TextColumn = 0;

                        AddressEntry.Changed += delegate {
                            Controller.CheckAddPage (AddressEntry.Text, PathEntry.Text, GetSelected(tree));
                        };

                                layout_address.PackStart (new Label () {
                                    Markup = "<b>" + _("Address") + "</b>",
                                    Xalign = 0
                                }, true, true, 0);

                                layout_address.PackStart (AddressEntry, true, true, 0);

                                    PathEntry.Completion  = new EntryCompletion();

                                    ListStore folder_store = new ListStore (typeof (string));

                                    //foreach (string host in Program.Controller.FolderPaths)
                                    //    folder_store.AppendValues (host);

                                    PathEntry.Completion.Model      = folder_store;
                                    PathEntry.Completion.TextColumn = 0;

                                    PathEntry.Changed += delegate {
                                        Controller.CheckAddPage (AddressEntry.Text, PathEntry.Text, GetSelected(tree));
                                    };

                                layout_path.PackStart (new Label () { Markup = "<b>" + _("Remote Path") + "</b>", Xalign = 0 },
                                    true, true, 0);
                                layout_path.PackStart (PathEntry, true, true, 0);

                            layout_fields.PackStart (layout_address);
                            layout_fields.PackStart (layout_path);

                        layout_vertical.PackStart (new Label (""), false, false, 0);
                        layout_vertical.PackStart (scrolled_window, true, true, 0);
                        layout_vertical.PackStart (layout_fields, false, false, 0);

                        Add (layout_vertical);

                            // Cancel button
                            Button cancel_button = new Button (_("Cancel"));

                            cancel_button.Clicked += delegate {
                                Close ();
                            };

                            // Sync button
                            SyncButton = new Button (_("Add"));

                            SyncButton.Clicked += delegate {
                                string server         = AddressEntry.Text;
                                string folder_name    = PathEntry.Text;

                                Controller.AddPageCompleted (server, folder_name);
                            };

                        AddButton (cancel_button);
                        AddButton (SyncButton);

                        Controller.CheckAddPage (AddressEntry.Text, PathEntry.Text, GetSelected(tree));

                        break;
                    }

                    case PageType.Syncing: {

                        Header      = String.Format (_("Adding project ‘{0}’…"), Controller.SyncingFolder);
                        Description = _("This may take a while.") + Environment.NewLine +
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

                        Header      = _("Project successfully added!");
                        Description = _("Access the files from your SparkleShare folder.");

                        // A button that opens the synced folder
                        Button open_folder_button = new Button (_("Open Folder"));

                        open_folder_button.Clicked += delegate {
                            Program.Controller.OpenSparkleShareFolder (Controller.SyncingFolder);
                        };

                        Button finish_button = new Button (_("Finish"));

                        finish_button.Clicked += delegate {
                            Controller.FinishedPageCompleted ();
                            Close ();
                        };


                        if (warnings != null) {
                            Image warning_image = new Image (SparkleUIHelpers.GetIcon ("dialog-warning", 24));
                            Label warning_label = new Label (warnings [0]) {
                                Xalign = 0
                            };

                            HBox warning_layout = new HBox (false, 0);
                            warning_layout.PackStart (warning_image, false, false, 0);
                            warning_layout.PackStart (warning_label, true, true, 0);

                            Add (warning_layout);

                        } else {
                            Add (null);
                        }


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

                            Label label = new Label (_("…or select <b>‘Add Hosted Project…’</b> from the status icon menu " +
                                "to add one by hand.")) {
                                Wrap   = true,
                                Xalign = 0,
                                UseMarkup = true
                            };

                            Image slide = SparkleUIHelpers.GetImage ("tutorial-slide-4.png");

                            Button finish_button = new Button (_("Finish"));
                            finish_button.Clicked += delegate {
                                Close ();
                            };


                            VBox box = new VBox (false, 0);
                            box.Add (slide);
                            box.Add (label);

                            Add (box);
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


        private void RenderServiceColumn (TreeViewColumn column, CellRenderer cell,
            TreeModel model, TreeIter iter)
        {
            string markup           = (string) model.GetValue (iter, 1);
            TreeSelection selection = (column.TreeView as TreeView).Selection;

            if (selection.IterIsSelected (iter))
                markup = markup.Replace (SecondaryTextColor, SecondaryTextColorSelected);
            else
                markup = markup.Replace (SecondaryTextColorSelected, SecondaryTextColor);

            (cell as CellRendererText).Markup = markup;
        }


        private Gdk.Color MixColors (Gdk.Color first_color, Gdk.Color second_color, double ratio)
        {
            return new Gdk.Color (
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Red   * (1.0 - ratio) + second_color.Red   * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Green * (1.0 - ratio) + second_color.Green * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Blue  * (1.0 - ratio) + second_color.Blue  * ratio))) / 65535)
            );
        }
    }
}
