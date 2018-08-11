//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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

using Gtk;
using Mono.Unix;

using Sparkles;

namespace SparkleShare {

    public class Setup : SetupWindow {

        public SetupController Controller = new SetupController ();


        public Setup ()
        {
            Controller.HideWindowEvent += delegate {
                Application.Invoke (delegate { Hide (); });
            };

            Controller.ShowWindowEvent += delegate {
                Application.Invoke (delegate {
                    ShowAll ();
                    Present ();
                });
            };
            
            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
                Application.Invoke (delegate {
                    Reset ();
                    ShowPage (type, warnings);
                    ShowAll ();
                });
            };
        }
        
        
        public void ShowPage (PageType type, string [] warnings)
        {
            if (type == PageType.Setup) {
                Header      = "Welcome to SparkleShare!";
                Description = "First off, what’s your name and email?\n(visible only to team members)";

                Table table = new Table (2, 3, true) {
                    RowSpacing    = 6,
                    ColumnSpacing = 6
                };

                Label name_label = new Label ("<b>" + "Your Name:" + "</b>") {
                    UseMarkup = true,
                    Xalign    = 1
                };

                Entry name_entry = new Entry () {
                    Xalign = 0,
                    ActivatesDefault = true
                };

                try {
                    UnixUserInfo user_info = UnixUserInfo.GetRealUser ();
                
                    if (user_info != null && user_info.RealName != null)
                        // Some systems append a series of "," for some reason
                        name_entry.Text = user_info.RealName.TrimEnd (",".ToCharArray ());

                } catch (ArgumentException) {
                    // No username, not a big deal
                }

                Entry email_entry = new Entry () {
                    Xalign = 0,
                    ActivatesDefault = true
                };
                
                Label email_label = new Label ("<b>" + "Email:" + "</b>") {
                    UseMarkup = true,
                    Xalign    = 1
                };

                table.Attach (name_label, 0, 1, 0, 1);
                table.Attach (name_entry, 1, 2, 0, 1);
                table.Attach (email_label, 0, 1, 1, 2);
                table.Attach (email_entry, 1, 2, 1, 2);
                
                VBox wrapper = new VBox (false, 9);
                wrapper.PackStart (table, true, false, 0);
                
                Button cancel_button = new Button ("Cancel");
                Button continue_button = new Button ("Continue") { Sensitive = false };


                Controller.UpdateSetupContinueButtonEvent += delegate (bool button_enabled) {
                    Application.Invoke (delegate { continue_button.Sensitive = button_enabled; });
                };

                name_entry.Changed    += delegate { Controller.CheckSetupPage (name_entry.Text, email_entry.Text); };
                email_entry.Changed   += delegate { Controller.CheckSetupPage (name_entry.Text, email_entry.Text); };
                cancel_button.Clicked += delegate { Controller.SetupPageCancelled (); };
            
                continue_button.Clicked += delegate {
                    Controller.SetupPageCompleted (name_entry.Text, email_entry.Text);
                };

               
                AddButton (cancel_button);
                AddButton (continue_button);
                Add (wrapper);

                Controller.CheckSetupPage (name_entry.Text, email_entry.Text);

                if (name_entry.Text.Equals (""))
                    name_entry.GrabFocus ();
                else
                    email_entry.GrabFocus ();
            }

            if (type == PageType.Add) {
                Add ((Widget) new AddPage (SparkleShare.UI.Setup.Controller).Render ());
            }

            if (type == PageType.Invite) {
                Header      = "You’ve received an invite!";
                Description = "Do you want to add this project to SparkleShare?";

                Table table = new Table (2, 3, true) {
                    RowSpacing    = 6,
                    ColumnSpacing = 6
                };

                Label address_label = new Label ("Address:") { Xalign = 1 };
                Label path_label = new Label ("Remote Path:") { Xalign = 1 };

                Label address_value = new Label ("<b>" + Controller.PendingInvite.Address + "</b>") {
                    UseMarkup = true,
                    Xalign    = 0
                };

                Label path_value = new Label ("<b>" + Controller.PendingInvite.RemotePath + "</b>") {
                    UseMarkup = true,
                    Xalign    = 0
                };

                table.Attach (address_label, 0, 1, 0, 1);
                table.Attach (address_value, 1, 2, 0, 1);
                table.Attach (path_label, 0, 1, 1, 2);
                table.Attach (path_value, 1, 2, 1, 2);

                VBox wrapper = new VBox (false, 9);
                wrapper.PackStart (table, true, false, 0);

                Button cancel_button = new Button ("Cancel");
                Button add_button    = new Button ("Add");


                cancel_button.Clicked += delegate { Controller.PageCancelled (); };
                add_button.Clicked += delegate { Controller.InvitePageCompleted (); };


                AddButton (cancel_button);
                AddButton (add_button);
                Add (wrapper);
            }

            if (type == PageType.Syncing) {
                Header      = String.Format ("Adding project ‘{0}’…", Controller.SyncingFolder);
                Description = "This may take a while for large projects.\nIsn’t it coffee-o’clock?";

                ProgressBar progress_bar = new ProgressBar ();
                progress_bar.Fraction    = Controller.ProgressBarPercentage / 100;

                Button cancel_button = new Button () { Label = "Cancel" };
                Button finish_button = new Button ("Finish") { Sensitive = false };
                
                Label progress_label = new Label ("Preparing to fetch files…") {
                    Justify = Justification.Right,
                    Xalign  = 1
                };
                

                Controller.UpdateProgressBarEvent += delegate (double percentage, string speed) {
                    Application.Invoke (delegate {
                        progress_bar.Fraction = percentage / 100;
                        progress_label.Text   = speed;
                    });
                };
                
                cancel_button.Clicked += delegate { Controller.SyncingCancelled (); };


                VBox bar_wrapper = new VBox (false, 0);
                bar_wrapper.PackStart (progress_bar, false, false, 21);
                bar_wrapper.PackStart (progress_label, false, true, 0);

                Add (bar_wrapper);
                AddButton (cancel_button);
                AddButton (finish_button);
            }

            if (type == PageType.Error) {
                Header = "Oops! Something went wrong" + "…";

                VBox points = new VBox (false, 0);
                Image list_point_one   = new Image (UserInterfaceHelpers.GetIcon ("list-point", 16));
                Image list_point_two   = new Image (UserInterfaceHelpers.GetIcon ("list-point", 16));
                Image list_point_three = new Image (UserInterfaceHelpers.GetIcon ("list-point", 16));

                Label label_one = new Label () {
                    Markup = "<b>" + Controller.PreviousUrl + "</b> is the address we’ve compiled. " +
                    "Does this look alright?",
                    Wrap   = true,
                    Xalign = 0
                };

                Label label_two = new Label () {
                    Text   = "Is this computer’s Client ID known by the host?",
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

                if (warnings.Length > 0) {
                    string warnings_markup = "";

                    foreach (string warning in warnings)
                        warnings_markup += "\n<b>" + warning + "</b>";

                    Label label_three = new Label () {
                        Markup = "Here’s the raw error message:" + warnings_markup,
                        Wrap   = true,
                        Xalign = 0
                    };

                    HBox point_three = new HBox (false, 0);
                    point_three.PackStart (list_point_three, false, false, 0);
                    point_three.PackStart (label_three, true, true, 12);
                    points.PackStart (point_three, false, false, 12);
                }

                points.PackStart (new Label (""), true, true, 0);

                Button cancel_button = new Button ("Cancel");
                Button try_again_button = new Button ("Retry") { Sensitive = true };


                cancel_button.Clicked += delegate { Controller.PageCancelled (); };
                try_again_button.Clicked += delegate { Controller.ErrorPageCompleted (); };

                
                AddButton (cancel_button);
                AddButton (try_again_button);
                Add (points);
            }

            if (type == PageType.StorageSetup) {
                Header = string.Format ("Storage type for ‘{0}’", Controller.SyncingFolder);
                Description = "What type of storage would you like to use?";

                VBox layout_vertical = new VBox (false, 0);
                VBox layout_radio_buttons = new VBox (false, 0) { BorderWidth = 12 };

                foreach (StorageTypeInfo storage_type in SparkleShare.Controller.FetcherAvailableStorageTypes) {
                    RadioButton radio_button = new RadioButton (null,
                        storage_type.Name + "\n" + storage_type.Description);

                    (radio_button.Child as Label).Markup = string.Format(
                        "<b>{0}</b>\n<span fgcolor=\"{1}\">{2}</span>",
                        storage_type.Name, SparkleShare.UI.SecondaryTextColor, storage_type.Description);

                    (radio_button.Child as Label).Xpad = 9;
                   
                    layout_radio_buttons.PackStart (radio_button, false, false, 9);
                    radio_button.Group = (layout_radio_buttons.Children [0] as RadioButton).Group;
                }

                layout_vertical.PackStart (new Label (""), true, true, 0);
                layout_vertical.PackStart (layout_radio_buttons, false, false, 0);
                layout_vertical.PackStart (new Label (""), true, true, 0);
                Add (layout_vertical);

                Button cancel_button = new Button ("Cancel");
                Button continue_button = new Button ("Continue");

                continue_button.Clicked += delegate {
                    int checkbox_index= 0;
                    foreach (RadioButton radio_button in layout_radio_buttons.Children) {
                        if (radio_button.Active) {
                            StorageTypeInfo selected_storage_type = SparkleShare.Controller.FetcherAvailableStorageTypes [checkbox_index];
                            Controller.StoragePageCompleted (selected_storage_type.Type);
                            return;
                        }

                        checkbox_index++;
                    }
                };

                cancel_button.Clicked += delegate {
                    Controller.SyncingCancelled ();
                };

                AddButton (cancel_button);
                AddButton (continue_button);
            }

            if (type == PageType.CryptoSetup || type == PageType.CryptoPassword) {
                if (type == PageType.CryptoSetup) {
                    Header      = string.Format ("Encryption password for ‘{0}’", Controller.SyncingFolder);
                    Description = "Please a provide a strong password that you don’t use elsewhere.";
                
                } else {
                    Header      = string.Format ("‘{0}’ contains encrypted files", Controller.SyncingFolder);
                    Description = "Please enter the password to see their contents.";
                }

                Label password_label = new Label ("<b>" + "Password" + "</b>") {
                    UseMarkup = true,
                    Xalign    = 1
                };

                Entry password_entry = new Entry () {
                    Xalign = 0,
                    Visibility = false,
                    ActivatesDefault = true
                };
                
                CheckButton show_password_check_button = new CheckButton ("Make visible") {
                    Active = false,
                    Xalign = 0,
                };

                Table table = new Table (2, 3, true) {
                    RowSpacing    = 6,
                    ColumnSpacing = 6
                };

                table.Attach (password_label, 0, 1, 0, 1);
                table.Attach (password_entry, 1, 2, 0, 1);
                
                table.Attach (show_password_check_button, 1, 2, 1, 2);
                
                VBox wrapper = new VBox (false, 9);
                wrapper.PackStart (table, true, false, 0);
   
                Image warning_image = new Image (
                    UserInterfaceHelpers.GetIcon ("dialog-information", 24));

                Label warning_label = new Label () {
                    Xalign = 0,
                    Wrap   = true,
                    Text   = "This password can’t be changed later, and your files can’t be recovered if it’s forgotten."
                };

                HBox warning_layout = new HBox (false, 0);
                warning_layout.PackStart (warning_image, false, false, 15);
                warning_layout.PackStart (warning_label, true, true, 0);
                
                VBox warning_wrapper = new VBox (false, 0);
                warning_wrapper.PackStart (warning_layout, false, false, 15);

                if (type == PageType.CryptoSetup)
                    wrapper.PackStart (warning_wrapper, false, false, 0);
                
                Button cancel_button = new Button ("Cancel");
                Button continue_button = new Button ("Continue") { Sensitive = false };
                
                
                Controller.UpdateCryptoSetupContinueButtonEvent += delegate (bool button_enabled) {
                    Application.Invoke (delegate { continue_button.Sensitive = button_enabled; });
                };
                
                Controller.UpdateCryptoPasswordContinueButtonEvent += delegate (bool button_enabled) {
                    Application.Invoke (delegate { continue_button.Sensitive = button_enabled; });
                };

                show_password_check_button.Toggled += delegate {
                    password_entry.Visibility = !password_entry.Visibility;
                };

                password_entry.Changed += delegate {
                    if (type == PageType.CryptoSetup)
                        Controller.CheckCryptoSetupPage (password_entry.Text);
                    else
                        Controller.CheckCryptoPasswordPage (password_entry.Text);
                };
                 
                cancel_button.Clicked += delegate { Controller.CryptoPageCancelled (); };
                
                continue_button.Clicked += delegate { 
                    if (type == PageType.CryptoSetup)
                        Controller.CryptoSetupPageCompleted (password_entry.Text);
                    else
                        Controller.CryptoPasswordPageCompleted (password_entry.Text);
                };
                
                
                Add (wrapper);

                AddButton (cancel_button);
                AddButton (continue_button);

                password_entry.GrabFocus ();
            }
                
            if (type == PageType.Finished) {
                Header      = "Your shared project is ready!";
                Description = "You can find the files in your SparkleShare folder.";
                
                UrgencyHint = true;

                Button show_files_button = new Button ("Show Files");
                Button finish_button     = new Button ("Finish");


                show_files_button.Clicked += delegate { Controller.ShowFilesClicked (); };
                finish_button.Clicked += delegate { Controller.FinishPageCompleted (); };


                if (warnings.Length > 0) {
                    Image warning_image = new Image (UserInterfaceHelpers.GetIcon ("dialog-information", 24));
                    
                    Label warning_label = new Label (warnings [0]) {
                        Xalign = 0,
                        Wrap   = true
                    };

                    HBox warning_layout = new HBox (false, 0);
                    warning_layout.PackStart (warning_image, false, false, 15);
                    warning_layout.PackStart (warning_label, true, true, 0);
                    
                    VBox warning_wrapper = new VBox (false, 0);
                    warning_wrapper.PackStart (warning_layout, false, false, 0);

                    Add (warning_wrapper);

                } else {
                    Add (null);
                }

                AddButton (show_files_button);
                AddButton (finish_button);
            }
        }

    
        private void RenderServiceColumn (TreeViewColumn column, CellRenderer cell,
            ITreeModel model, TreeIter iter)
        {
            string markup = (string) model.GetValue (iter, 2);
            TreeSelection selection = (column.TreeView as TreeView).Selection;

            if (selection.IterIsSelected (iter))
                markup = markup.Replace (SparkleShare.UI.SecondaryTextColor, SparkleShare.UI.SecondaryTextColorSelected);
            else
                markup = markup.Replace (SparkleShare.UI.SecondaryTextColorSelected, SparkleShare.UI.SecondaryTextColor);

            (cell as CellRendererText).Markup = markup;
        }
        
        
        private class SparkleTreeView : TreeView {

            public int SelectedRow
            {
                get {
                    TreeIter iter;
                    ITreeModel model;

                    Selection.GetSelected (out model, out iter);
                    return int.Parse (model.GetPath (iter).ToString ());
                }
            }


            public SparkleTreeView (ListStore store) : base (store)
            {
            }
        }
    }
}