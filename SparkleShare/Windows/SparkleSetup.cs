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
            return s;
        }


        public SparkleSetup () {
            InitializeComponent ();

            /* Support translations for the UI */
            this.label5.Text = _ ("Remote path");
            this.label14.Text = _ ("Address");
            this.label4.Text = _ ("Where is your remote folder?");

            pictureBox.Image = Icons.side_splash;
            this.Icon = Icons.sparkleshare;

            Controller.ChangePageEvent += delegate (PageType type) {
                tabControl.SafeInvoke ((Action)delegate {
                    switch (type) {
                        case PageType.Add:
                            tabControl.SelectedIndex = 1;
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
                            treeView.AfterSelect += new TreeViewEventHandler (CheckTreeNode);
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
                            treeView.SelectedNode = treeView.Nodes [0];
                            treeView.Select ();
                            CheckAddPage (null, null);
                            CheckTreeNode (null, null);
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
                        case PageType.Tutorial:
                            if (Controller.TutorialPageNumber == 1)
                                Controller.TutorialSkipped ();
                            else
                                Controller.ShowAddPage ();
                            break;
                        default:
                            throw new NotImplementedException ("unknown PageType");
                    }
                });
            };
        }

        private void SparkleSetup_FormClosing (object sender, FormClosingEventArgs e) {
            if (e.CloseReason != CloseReason.ApplicationExitCall
                    && e.CloseReason != CloseReason.TaskManagerClosing
                    && e.CloseReason != CloseReason.WindowsShutDown) {
                e.Cancel = true;
                this.Hide ();
            }
        }

        private void buttonCancel_Click (object sender, EventArgs e) {
            this.Hide ();
        }

        private void buttonSync_Click (object sender, EventArgs e) {
            if (String.IsNullOrEmpty (Controller.Plugins [treeView.SelectedNode.Index].Address))
                Controller.AddPageCompleted (ServerEntry.Text, FolderEntry.Text);
            else
                Controller.AddPageCompleted (Controller.Plugins [treeView.SelectedNode.Index].Address,
                    FolderEntry.Text);
        }

        private void CheckTreeNode (object sender, EventArgs e) {
            // If the "own server" choice is selected, the address field is empty
            if (String.IsNullOrEmpty (Controller.Plugins [treeView.SelectedNode.Index].Address)) {
                ServerEntry.Enabled = true;
                ServerEntry.ExampleText = Controller.Plugins [treeView.SelectedNode.Index].AddressExample;
            } else {
                ServerEntry.Enabled = false;
                ServerEntry.ExampleText = Controller.Plugins [treeView.SelectedNode.Index].Address;
                ServerEntry.Enabled = false;
            }
            //Clear any previous input data so that exampletext can show
            ServerEntry.Text = "";
            FolderEntry.Text = "";
            FolderEntry.ExampleText = Controller.Plugins [treeView.SelectedNode.Index].PathExample;
            CheckAddPage (null, null);
        }

        private void CheckAddPage (object sender, EventArgs e) {
            // Enables or disables the 'Next' button depending on the
            // entries filled in by the user
            buttonSync.Enabled = false;

            if (String.IsNullOrEmpty (Controller.Plugins [treeView.SelectedNode.Index].Address)) {
                if (!String.IsNullOrEmpty (FolderEntry.Text)) {
                    if (!String.IsNullOrEmpty (ServerEntry.Text))
                        buttonSync.Enabled = true;
                }
            } else {
                if (!String.IsNullOrEmpty (FolderEntry.Text))
                    buttonSync.Enabled = true;
            }
        }

        private void buttonFinish_Click (object sender, EventArgs e) {
            this.Hide ();
        }

        private void buttonTryAgain_Click (object sender, EventArgs e) {
            Controller.ErrorPageCompleted ();
        }

        private void buttonFinished_Click (object sender, EventArgs e) {
            this.Hide ();
        }

        private void buttonOpenFolder_Click (object sender, EventArgs e) {
            Program.Controller.OpenSparkleShareFolder (Controller.SyncingFolder);
        }

        private void buttonNext_Click (object sender, EventArgs e) {
            string full_name = NameEntry.Text;
            string email = EmailEntry.Text;

            Controller.SetupPageCompleted (full_name, email);
        }

        private void CheckSetupPage (object sender, EventArgs e) {
            // Enables or disables the 'Next' button depending on the
            // entries filled in by the user
            if (!String.IsNullOrEmpty (NameEntry.Text) &&
                Program.Controller.IsValidEmail (EmailEntry.Text)) {
                buttonNext.Enabled = true;
            } else {
                buttonNext.Enabled = false;
            }
        }
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

