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
using System.Threading;

using System.Windows.Forms;
using System.Drawing;


namespace SparkleShare {

    public partial class SparkleSetup : Form {

        public SparkleSetupController Controller = new SparkleSetupController ();

        private TreeView treeView;

        // Short alias for the translations
        public static string _ (string s) {
            return Program._ (s);
        }


        public SparkleSetup () {
            InitializeComponent ();

            Program.TranslateWinForm (this);

            pictureBox.Image = Icons.side_splash;
            this.Icon = Icons.sparkleshare;

            Controller.HideWindowEvent += delegate
            {
                this.Hide();
            };

            Controller.ShowWindowEvent += delegate
            {
                this.Show();
            };

            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
                tabControl.SafeInvoke ((Action)delegate {
                    switch (type) {
                        case PageType.Setup:
                            tabControl.SelectedIndex = 0;
                            NameEntry.Text = Controller.GuessedUserName;
                            EmailEntry.Text = Controller.GuessedUserEmail;
                            Show();
                            Controller.CheckSetupPage(NameEntry.Text, EmailEntry.Text);
                            break;

                        case PageType.Add:
                            tabControl.SelectedIndex = 1;

                            // Add plugins to tree
                            // ===================
                            // Check whether the treeView is already created
                            // If it is dispose it and start over
                            if (treeView != null) {
                                treeView.Dispose();
                            }
                            // Set up the treeview
                            ImageList imageList = new ImageList ();
                            imageList.ImageSize = new Size (24, 24);
                            treeView = new TreeView ();
                            treeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
                            treeView.FullRowSelect = true;
                            treeView.ImageIndex = 0;
                            treeView.Indent = 35;
                            treeView.HideSelection = false;
                            treeView.ItemHeight = 40;

                            TreeNode [] nodes = new TreeNode [Controller.Plugins.Count];

                            for (int i = 0; i < Controller.Plugins.Count; i++) {
                                nodes [i] = new TreeNode (Controller.Plugins [i].Name + ";" + Controller.Plugins [i].Description);
                                nodes [i].ImageIndex = i;
                                nodes [i].SelectedImageIndex = i;
                                nodes [i].Tag = Controller.Plugins [i].Name;
                                imageList.Images.Add (Image.FromFile (Controller.Plugins [i].ImagePath));
                            }

                            treeView.Nodes.AddRange (nodes);
                            treeView.ImageList = imageList;
                            treeView.ShowLines = false;
                            treeView.ShowRootLines = false;
                            treeView.Size = new System.Drawing.Size (panel_server_selection.Size.Width,
                                panel_server_selection.Size.Height);

                            panel_server_selection.Controls.Add (treeView);
                            // Finished adding and populating tree

                            // Select first node
                            treeView.SelectedNode = treeView.Nodes[0];
                            treeView.Select();
                            Controller.SelectedPluginChanged(0);

                            treeView.AfterSelect += new TreeViewEventHandler(CheckTreeNode);

                            Show ();
                            Controller.CheckAddPage(ServerEntry.Text, FolderEntry.Text, 1);
                            break;

                        case PageType.Invite:
                            tabControl.SelectedIndex = 5;
                            InviteAddressEntry.Text = Controller.PendingInvite.Address;
                            InviteFolderEntry.Text = Controller.PendingInvite.RemotePath;
                            Show();
                            break;

                        case PageType.Syncing:
                            tabControl.SelectedIndex = 2;
                            Show();
                            break;

                        case PageType.Error:
                            tabControl.SelectedIndex = 3;
                            label3.Text = "First, have you tried turning it off and on again?\n\n" +
                                Controller.PreviousUrl + " is the address we've compiled. Does this look alright?\n\n" +
                                "The host needs to know who you are. Have you uploaded the key that sits in your SparkleShare folder?";
                            Show ();
                            break;

                        case PageType.Finished:
                            tabControl.SelectedIndex = 4;
                            Show ();
                            break;

                        case PageType.Tutorial:
                            // Do nothing in tutorial by now
                            Controller.TutorialSkipped();;
                            break;

                        default:
                            throw new NotImplementedException ("unknown PageType");
                    }
                });
            };

            Controller.UpdateSetupContinueButtonEvent += new SparkleSetupController.UpdateSetupContinueButtonEventHandler(UpdateSetupContinueButtonEvent);

            Controller.ChangeAddressFieldEvent += new SparkleSetupController.ChangeAddressFieldEventHandler(ChangeAddressFieldEvent);
            Controller.ChangePathFieldEvent += new SparkleSetupController.ChangePathFieldEventHandler(ChangePathFieldEvent);
            Controller.UpdateAddProjectButtonEvent += new SparkleSetupController.UpdateAddProjectButtonEventHandler(UpdateAddProjectButtonEvent);

            Controller.UpdateProgressBarEvent += new SparkleSetupController.UpdateProgressBarEventHandler(UpdateProgressBarEvent);
        }

        private void SparkleSetup_FormClosing (object sender, FormClosingEventArgs e) {
            if (e.CloseReason != CloseReason.ApplicationExitCall
                && e.CloseReason != CloseReason.TaskManagerClosing
                && e.CloseReason != CloseReason.WindowsShutDown) {
                e.Cancel = true;
                this.Hide ();
            }
        }

        #region Things for "Setup" page
        private void SetupNextClicked(object sender, EventArgs e)
        {
            Controller.SetupPageCompleted(NameEntry.Text, EmailEntry.Text);
        }

        private void CheckSetupPage(object sender, EventArgs e)
        {
            Controller.CheckSetupPage(NameEntry.Text, EmailEntry.Text);
        }

        void UpdateSetupContinueButtonEvent(bool button_enabled)
        {
            buttonNext.Enabled = button_enabled;
        }
        #endregion

        #region Things for "Add" page
        void ChangeAddressFieldEvent(string text, string example_text, FieldState state)
        {
            ServerEntry.Text = text;
            ServerEntry.Enabled = state == FieldState.Enabled;
            ServerEntry.ExampleText = example_text;
        }

        void ChangePathFieldEvent(string text, string example_text, FieldState state)
        {
            FolderEntry.Text = text;
            FolderEntry.Enabled = state == FieldState.Enabled;
            FolderEntry.ExampleText = example_text;
        }

        private void CheckTreeNode(object sender, EventArgs e)
        {
            Controller.SelectedPluginChanged(treeView.SelectedNode.Index);
        }

        private void CancelButtonClicked (object sender, EventArgs e) {
            Controller.PageCancelled();
        }

        private void AddButtonClicked(object sender, EventArgs e)
        {
            Controller.AddPageCompleted(ServerEntry.Text, FolderEntry.Text);
        }

        void UpdateAddProjectButtonEvent(bool button_enabled)
        {
            buttonSync.Enabled = button_enabled;
        }

        private void CheckAddPage(object sender, EventArgs e)
        {
            Controller.CheckAddPage(ServerEntry.Text, FolderEntry.Text, treeView.SelectedNode.Index);
        }
        #endregion

        #region Things for "Invite" page
        private void InviteAddButtonClicked(object sender, EventArgs e)
        {
            Controller.InvitePageCompleted();
        }

        private void InviteCancelButtonClicked(object sender, EventArgs e)
        {
            Controller.PageCancelled();
        }
        #endregion

        #region Things for "Syncing" page
        private void syncCancelClicked(object sender, EventArgs e)
        {
            Controller.SyncingCancelled();
        }

        void UpdateProgressBarEvent(double percentage)
        {
            syncingProgressBar.Value = (int)percentage;
        }
        #endregion

        #region Things for "Error" page
        private void buttonTryAgain_Click(object sender, EventArgs e)
        {
            Controller.ErrorPageCompleted();
        }
        #endregion

        #region Thigngs for "Finish" page
        private void buttonFinished_Click (object sender, EventArgs e) {
            Controller.FinishPageCompleted();
        }

        private void buttonOpenFolder_Click (object sender, EventArgs e) {
            Controller.OpenFolderClicked();
        }
        #endregion
    }
}

public class TreeView : System.Windows.Forms.TreeView {

    protected override void OnDrawNode (DrawTreeNodeEventArgs e) {
        e.Graphics.DrawString (e.Node.Text.Split (';') [0], new Font (Font.SystemFontName, 13),
            new SolidBrush (Color.Black), e.Bounds.X, e.Bounds.Y);
        e.Graphics.DrawString (e.Node.Text.Split (';') [1], new Font (Font.SystemFontName, 9),
            new SolidBrush (Color.Black), e.Bounds.X + 10, e.Bounds.Y + 15);
    }
}

public class TreeNode : System.Windows.Forms.TreeNode {
    public TreeNode (string text) {
        this.Text = text;
    }
}

