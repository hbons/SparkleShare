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

using System.Windows.Forms;
using System.Drawing;


namespace SparkleShare {

    public partial class SparkleSetup : Form {

        public SparkleSetupController Controller = new SparkleSetupController ();

        // Short alias for the translations
        public static string _ (string s)
        {
            return s;
        }


        public SparkleSetup () 
        {
            InitializeComponent ();

            pictureBox.Image = Icons.side_splash;
            this.ClientSize = new Size (this.ClientSize.Width, Icons.side_splash.Size.Height);
            this.Icon = Icons.sparkleshare;

            Controller.ChangePageEvent += delegate (PageType type) {
                tabControl.SafeInvoke ((Action)delegate {
                    switch (type) {
                        case PageType.Add:
                            tabControl.SelectedIndex = 1;
                            if (!string.IsNullOrEmpty (Controller.PreviousServer))
                                ServerEntry.Text = Controller.PreviousServer;
                            else
                                ServerEntry.Text = "";
                            FolderEntry.Text = "";
                            radio_button_own_server.Checked = true;
                            //CheckAddPage (null, null);
                            Show ();
                            break;
                        case PageType.Error:
                            tabControl.SelectedIndex = 3;
                            Show ();
                            break;
                        case PageType.Finished:
                            tabControl.SelectedIndex = 4;
                            Show ();
                            break;
                        case PageType.Setup:
                            tabControl.SelectedIndex = 0;
                            NameEntry.Text = Program.Controller.UserName;
                            Show ();
                            break;
                        case PageType.Syncing:
                            tabControl.SelectedIndex = 2;
                            Show ();
                            break;
                    }
                });
            };
        }

        private void SparkleSetup_FormClosing (object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall
                    && e.CloseReason != CloseReason.TaskManagerClosing
                    && e.CloseReason != CloseReason.WindowsShutDown) {
                e.Cancel = true;
                this.Hide ();
            }
        }

        private void buttonCancel_Click (object sender, EventArgs e)
        {
            this.Hide ();
        }

        private void buttonSync_Click (object sender, EventArgs e)
        {
            string server = ServerEntry.Text;
            string folder_name = FolderEntry.Text;

            if (radio_button_gitorious.Checked)
                server = "gitorious.org";

            if (radio_button_github.Checked)
                server = "github.com";

            if (radio_button_gnome.Checked)
                server = "gnome.org";

            Controller.AddPageCompleted (server, folder_name);
        }

        private void CheckAddPage (object sender, EventArgs e)
        {
            buttonSync.Enabled = false;

            if (radio_button_own_server.Checked)
                FolderEntry.ExampleText = _ ("Folder");
            else if (radio_button_github.Checked)
                FolderEntry.ExampleText = _ ("Username/Folder");
            else if (radio_button_gitorious.Checked)
                FolderEntry.ExampleText = _ ("Project/Folder");
            else if(radio_button_gnome.Checked)
                FolderEntry.ExampleText = _ ("Project");

            // Enables or disables the 'Next' button depending on the
            // entries filled in by the user
            buttonSync.Enabled = false;
            if (!String.IsNullOrEmpty (FolderEntry.Text)) {
                if (!radio_button_own_server.Checked || !String.IsNullOrEmpty (ServerEntry.Text))
                    buttonSync.Enabled = true;
            }
        }

        private void radio_button_own_server_CheckedChanged (object sender, EventArgs e)
        {
            ServerEntry.Enabled = radio_button_own_server.Checked;
            CheckAddPage (sender,e);
        }

        private void buttonFinish_Click (object sender, EventArgs e)
        {
            this.Hide ();
        }

        private void buttonTryAgain_Click (object sender, EventArgs e)
        {
            Controller.ErrorPageCompleted ();
        }

        private void buttonFinished_Click (object sender, EventArgs e)
        {
            this.Hide ();
        }

        private void buttonOpenFolder_Click (object sender, EventArgs e)
        {
            Program.Controller.OpenSparkleShareFolder (Controller.SyncingFolder);
        }

        private void buttonNext_Click (object sender, EventArgs e)
        {
            string full_name = NameEntry.Text;
            string email = EmailEntry.Text;

            Controller.SetupPageCompleted (full_name, email);
        }

        private void CheckSetupPage (object sender, EventArgs e)
        {
            // Enables or disables the 'Next' button depending on the
            // entries filled in by the user
            if (!String.IsNullOrEmpty(NameEntry.Text) &&
                Program.Controller.IsValidEmail (EmailEntry.Text)) {

                buttonNext.Enabled = true;
            } else {
                buttonNext.Enabled = false;
            }
        }

        private void showInfo (string text) {
            pictureBox.Visible = false;
            panel_info.Visible = true;
            label_info.Text = text;
        }

        private void hideInfo () {
            pictureBox.Visible = true;
            panel_info.Visible = false;
        }

        private void radio_button_own_server_MouseEnter (object sender, EventArgs e) {
            showInfo ("To use your own server you need to blabla");
        }

        private void radio_button_github_MouseEnter (object sender, EventArgs e) {
            showInfo ("To use your own server you need to blabla");
        }

        private void radio_button_gitorious_MouseEnter (object sender, EventArgs e) {
            showInfo ("awdaw");
        }

        private void radio_button_gnome_MouseEnter (object sender, EventArgs e) {
            showInfo ("Gnome");
        }

        private void panel_server_selection_MouseLeave (object sender, EventArgs e) {
            hideInfo ();
        }

        private void FolderEntry_MouseEnter (object sender, EventArgs e) {
            showInfo ("Type in the folder");
        }

        private void FolderEntry_MouseLeave (object sender, EventArgs e) {
            hideInfo ();
        }
    }
}
