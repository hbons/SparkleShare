namespace SparkleShare {
    partial class SparkleSetup {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (SparkleSetup));
            this.pictureBox = new System.Windows.Forms.PictureBox ();
            this.tabControl = new SparkleShare.controls.TablessControl ();
            this.setupPage = new System.Windows.Forms.TabPage ();
            this.groupBox1 = new System.Windows.Forms.GroupBox ();
            this.EmailEntry = new System.Windows.Forms.TextBox ();
            this.NameEntry = new System.Windows.Forms.TextBox ();
            this.label13 = new System.Windows.Forms.Label ();
            this.label12 = new System.Windows.Forms.Label ();
            this.label11 = new System.Windows.Forms.Label ();
            this.label10 = new System.Windows.Forms.Label ();
            this.buttonNext = new System.Windows.Forms.Button ();
            this.addPage = new System.Windows.Forms.TabPage ();
            this.FolderEntry = new SparkleShare.controls.ExampleTextBox ();
            this.ServerEntry = new SparkleShare.controls.ExampleTextBox ();
            this.label5 = new System.Windows.Forms.Label ();
            this.label14 = new System.Windows.Forms.Label ();
            this.panel_server_selection = new System.Windows.Forms.Panel ();
            this.buttonSync = new System.Windows.Forms.Button ();
            this.buttonCancel = new System.Windows.Forms.Button ();
            this.label4 = new System.Windows.Forms.Label ();
            this.syncingPage = new System.Windows.Forms.TabPage ();
            this.syncingProgressBar = new System.Windows.Forms.ProgressBar ();
            this.buttonFinish = new System.Windows.Forms.Button ();
            this.label7 = new System.Windows.Forms.Label ();
            this.label6 = new System.Windows.Forms.Label ();
            this.errorPage = new System.Windows.Forms.TabPage ();
            this.buttonTryAgain = new System.Windows.Forms.Button ();
            this.label3 = new System.Windows.Forms.Label ();
            this.label2 = new System.Windows.Forms.Label ();
            this.label1 = new System.Windows.Forms.Label ();
            this.finishedPage = new System.Windows.Forms.TabPage ();
            this.label9 = new System.Windows.Forms.Label ();
            this.label8 = new System.Windows.Forms.Label ();
            this.buttonFinished = new System.Windows.Forms.Button ();
            this.buttonOpenFolder = new System.Windows.Forms.Button ();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit ();
            this.tabControl.SuspendLayout ();
            this.setupPage.SuspendLayout ();
            this.groupBox1.SuspendLayout ();
            this.addPage.SuspendLayout ();
            this.syncingPage.SuspendLayout ();
            this.errorPage.SuspendLayout ();
            this.finishedPage.SuspendLayout ();
            this.SuspendLayout ();
            // 
            // pictureBox
            // 
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.pictureBox.Location = new System.Drawing.Point (0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size (150, 396);
            this.pictureBox.TabIndex = 1;
            this.pictureBox.TabStop = false;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add (this.setupPage);
            this.tabControl.Controls.Add (this.addPage);
            this.tabControl.Controls.Add (this.syncingPage);
            this.tabControl.Controls.Add (this.errorPage);
            this.tabControl.Controls.Add (this.finishedPage);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Right;
            this.tabControl.Location = new System.Drawing.Point (150, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size (514, 396);
            this.tabControl.TabIndex = 0;
            this.tabControl.TabStop = false;
            // 
            // setupPage
            // 
            this.setupPage.Controls.Add (this.groupBox1);
            this.setupPage.Controls.Add (this.label11);
            this.setupPage.Controls.Add (this.label10);
            this.setupPage.Controls.Add (this.buttonNext);
            this.setupPage.Location = new System.Drawing.Point (4, 22);
            this.setupPage.Name = "setupPage";
            this.setupPage.Padding = new System.Windows.Forms.Padding (3);
            this.setupPage.Size = new System.Drawing.Size (506, 370);
            this.setupPage.TabIndex = 0;
            this.setupPage.Text = "Setup";
            this.setupPage.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add (this.EmailEntry);
            this.groupBox1.Controls.Add (this.NameEntry);
            this.groupBox1.Controls.Add (this.label13);
            this.groupBox1.Controls.Add (this.label12);
            this.groupBox1.Location = new System.Drawing.Point (14, 119);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size (471, 102);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Information about you";
            // 
            // EmailEntry
            // 
            this.EmailEntry.Location = new System.Drawing.Point (150, 54);
            this.EmailEntry.Name = "EmailEntry";
            this.EmailEntry.Size = new System.Drawing.Size (275, 20);
            this.EmailEntry.TabIndex = 10;
            this.EmailEntry.TextChanged += new System.EventHandler (this.CheckSetupPage);
            // 
            // NameEntry
            // 
            this.NameEntry.Location = new System.Drawing.Point (150, 28);
            this.NameEntry.Name = "NameEntry";
            this.NameEntry.Size = new System.Drawing.Size (275, 20);
            this.NameEntry.TabIndex = 9;
            this.NameEntry.TextChanged += new System.EventHandler (this.CheckSetupPage);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point (15, 57);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size (41, 13);
            this.label13.TabIndex = 8;
            this.label13.Text = "Email:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point (15, 31);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size (65, 13);
            this.label12.TabIndex = 7;
            this.label12.Text = "Full name:";
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point (11, 44);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size (307, 62);
            this.label11.TabIndex = 2;
            this.label11.Text = "Before we can create a SparkleShare folder on this computer, we need a few bits o" +
                "f information from you.\r\n";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label10.Location = new System.Drawing.Point (8, 18);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size (222, 20);
            this.label10.TabIndex = 1;
            this.label10.Text = "Welcome to SparkleShare!";
            // 
            // buttonNext
            // 
            this.buttonNext.Enabled = false;
            this.buttonNext.Location = new System.Drawing.Point (410, 316);
            this.buttonNext.Name = "buttonNext";
            this.buttonNext.Size = new System.Drawing.Size (75, 23);
            this.buttonNext.TabIndex = 0;
            this.buttonNext.Text = "Next";
            this.buttonNext.UseVisualStyleBackColor = true;
            this.buttonNext.Click += new System.EventHandler (this.buttonNext_Click);
            // 
            // addPage
            // 
            this.addPage.Controls.Add (this.FolderEntry);
            this.addPage.Controls.Add (this.ServerEntry);
            this.addPage.Controls.Add (this.label5);
            this.addPage.Controls.Add (this.label14);
            this.addPage.Controls.Add (this.panel_server_selection);
            this.addPage.Controls.Add (this.buttonSync);
            this.addPage.Controls.Add (this.buttonCancel);
            this.addPage.Controls.Add (this.label4);
            this.addPage.Location = new System.Drawing.Point (4, 22);
            this.addPage.Name = "addPage";
            this.addPage.Padding = new System.Windows.Forms.Padding (3);
            this.addPage.Size = new System.Drawing.Size (506, 370);
            this.addPage.TabIndex = 1;
            this.addPage.Text = "Add";
            this.addPage.UseVisualStyleBackColor = true;
            // 
            // FolderEntry
            // 
            this.FolderEntry.ExampleText = "";
            this.FolderEntry.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.FolderEntry.Location = new System.Drawing.Point (201, 290);
            this.FolderEntry.Name = "FolderEntry";
            this.FolderEntry.Size = new System.Drawing.Size (168, 20);
            this.FolderEntry.TabIndex = 17;
            this.FolderEntry.TextChanged += new System.EventHandler (this.CheckAddPage);
            // 
            // ServerEntry
            // 
            this.ServerEntry.ExampleText = "";
            this.ServerEntry.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.ServerEntry.Location = new System.Drawing.Point (12, 290);
            this.ServerEntry.Name = "ServerEntry";
            this.ServerEntry.Size = new System.Drawing.Size (168, 20);
            this.ServerEntry.TabIndex = 16;
            this.ServerEntry.TextChanged += new System.EventHandler (this.CheckAddPage);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point (197, 267);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size (113, 20);
            this.label5.TabIndex = 15;
            this.label5.Text = "Remote path";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point (8, 267);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size (75, 20);
            this.label14.TabIndex = 14;
            this.label14.Text = "Address";
            // 
            // panel_server_selection
            // 
            this.panel_server_selection.Location = new System.Drawing.Point (12, 54);
            this.panel_server_selection.Name = "panel_server_selection";
            this.panel_server_selection.Size = new System.Drawing.Size (479, 189);
            this.panel_server_selection.TabIndex = 12;
            // 
            // buttonSync
            // 
            this.buttonSync.Enabled = false;
            this.buttonSync.Location = new System.Drawing.Point (416, 339);
            this.buttonSync.Name = "buttonSync";
            this.buttonSync.Size = new System.Drawing.Size (75, 23);
            this.buttonSync.TabIndex = 9;
            this.buttonSync.Text = "Sync";
            this.buttonSync.UseVisualStyleBackColor = true;
            this.buttonSync.Click += new System.EventHandler (this.buttonSync_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point (335, 339);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size (75, 23);
            this.buttonCancel.TabIndex = 8;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler (this.buttonCancel_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point (8, 18);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size (68, 20);
            this.label4.TabIndex = 0;
            this.label4.Text = "Header";
            // 
            // syncingPage
            // 
            this.syncingPage.Controls.Add (this.syncingProgressBar);
            this.syncingPage.Controls.Add (this.buttonFinish);
            this.syncingPage.Controls.Add (this.label7);
            this.syncingPage.Controls.Add (this.label6);
            this.syncingPage.Location = new System.Drawing.Point (4, 22);
            this.syncingPage.Name = "syncingPage";
            this.syncingPage.Size = new System.Drawing.Size (506, 370);
            this.syncingPage.TabIndex = 2;
            this.syncingPage.Text = "Syncing";
            this.syncingPage.UseVisualStyleBackColor = true;
            // 
            // syncingProgressBar
            // 
            this.syncingProgressBar.Location = new System.Drawing.Point (14, 109);
            this.syncingProgressBar.Name = "syncingProgressBar";
            this.syncingProgressBar.Size = new System.Drawing.Size (492, 23);
            this.syncingProgressBar.TabIndex = 3;
            // 
            // buttonFinish
            // 
            this.buttonFinish.Enabled = false;
            this.buttonFinish.Location = new System.Drawing.Point (431, 350);
            this.buttonFinish.Name = "buttonFinish";
            this.buttonFinish.Size = new System.Drawing.Size (75, 23);
            this.buttonFinish.TabIndex = 2;
            this.buttonFinish.Text = "Finish";
            this.buttonFinish.UseVisualStyleBackColor = true;
            this.buttonFinish.Click += new System.EventHandler (this.buttonFinish_Click);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point (11, 44);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size (307, 62);
            this.label7.TabIndex = 1;
            this.label7.Text = "This may take a while.\r\nAre you sure it’s not coffee o\'clock?\r\n";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label6.Location = new System.Drawing.Point (8, 18);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size (123, 20);
            this.label6.TabIndex = 0;
            this.label6.Text = "Syncing folder";
            // 
            // errorPage
            // 
            this.errorPage.Controls.Add (this.buttonTryAgain);
            this.errorPage.Controls.Add (this.label3);
            this.errorPage.Controls.Add (this.label2);
            this.errorPage.Controls.Add (this.label1);
            this.errorPage.Location = new System.Drawing.Point (4, 22);
            this.errorPage.Name = "errorPage";
            this.errorPage.Size = new System.Drawing.Size (506, 370);
            this.errorPage.TabIndex = 3;
            this.errorPage.Text = "Error";
            this.errorPage.UseVisualStyleBackColor = true;
            // 
            // buttonTryAgain
            // 
            this.buttonTryAgain.Location = new System.Drawing.Point (380, 319);
            this.buttonTryAgain.Name = "buttonTryAgain";
            this.buttonTryAgain.Size = new System.Drawing.Size (75, 23);
            this.buttonTryAgain.TabIndex = 3;
            this.buttonTryAgain.Text = "Try Again";
            this.buttonTryAgain.UseVisualStyleBackColor = true;
            this.buttonTryAgain.Click += new System.EventHandler (this.buttonTryAgain_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point (11, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size (307, 129);
            this.label3.TabIndex = 2;
            this.label3.Text = resources.GetString ("label3.Text");
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point (11, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size (307, 62);
            this.label2.TabIndex = 1;
            this.label2.Text = "We don\'t know exactly what the problem is, but we can try to help you pinpoint it" +
                ".";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point (8, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size (191, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Something went wrong";
            // 
            // finishedPage
            // 
            this.finishedPage.Controls.Add (this.label9);
            this.finishedPage.Controls.Add (this.label8);
            this.finishedPage.Controls.Add (this.buttonFinished);
            this.finishedPage.Controls.Add (this.buttonOpenFolder);
            this.finishedPage.Location = new System.Drawing.Point (4, 22);
            this.finishedPage.Name = "finishedPage";
            this.finishedPage.Size = new System.Drawing.Size (506, 370);
            this.finishedPage.TabIndex = 4;
            this.finishedPage.Text = "Finished";
            this.finishedPage.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point (11, 44);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size (307, 62);
            this.label9.TabIndex = 3;
            this.label9.Text = "Access the synced files from your SparkleShare folder.";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font ("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.label8.Location = new System.Drawing.Point (8, 18);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size (228, 20);
            this.label8.TabIndex = 2;
            this.label8.Text = "Folder synced successfully!";
            // 
            // buttonFinished
            // 
            this.buttonFinished.Location = new System.Drawing.Point (399, 318);
            this.buttonFinished.Name = "buttonFinished";
            this.buttonFinished.Size = new System.Drawing.Size (75, 23);
            this.buttonFinished.TabIndex = 1;
            this.buttonFinished.Text = "Finish";
            this.buttonFinished.UseVisualStyleBackColor = true;
            this.buttonFinished.Click += new System.EventHandler (this.buttonFinished_Click);
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.Location = new System.Drawing.Point (318, 318);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Size = new System.Drawing.Size (75, 23);
            this.buttonOpenFolder.TabIndex = 0;
            this.buttonOpenFolder.Text = "Open Folder";
            this.buttonOpenFolder.UseVisualStyleBackColor = true;
            this.buttonOpenFolder.Click += new System.EventHandler (this.buttonOpenFolder_Click);
            // 
            // SparkleSetup
            // 
            this.ClientSize = new System.Drawing.Size (664, 396);
            this.Controls.Add (this.tabControl);
            this.Controls.Add (this.pictureBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "SparkleSetup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SparkleShare Setup";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler (this.SparkleSetup_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit ();
            this.tabControl.ResumeLayout (false);
            this.setupPage.ResumeLayout (false);
            this.setupPage.PerformLayout ();
            this.groupBox1.ResumeLayout (false);
            this.groupBox1.PerformLayout ();
            this.addPage.ResumeLayout (false);
            this.addPage.PerformLayout ();
            this.syncingPage.ResumeLayout (false);
            this.syncingPage.PerformLayout ();
            this.errorPage.ResumeLayout (false);
            this.errorPage.PerformLayout ();
            this.finishedPage.ResumeLayout (false);
            this.finishedPage.PerformLayout ();
            this.ResumeLayout (false);

        }

        #endregion

        private SparkleShare.controls.TablessControl tabControl;
        private System.Windows.Forms.TabPage setupPage;
        private System.Windows.Forms.TabPage addPage;
        private System.Windows.Forms.TabPage syncingPage;
        private System.Windows.Forms.TabPage errorPage;
        private System.Windows.Forms.TabPage finishedPage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonSync;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonFinish;
        private System.Windows.Forms.ProgressBar syncingProgressBar;
        private System.Windows.Forms.Button buttonTryAgain;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button buttonFinished;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button buttonNext;
        private System.Windows.Forms.Panel panel_server_selection;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox EmailEntry;
        private System.Windows.Forms.TextBox NameEntry;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label14;
        private SparkleShare.controls.ExampleTextBox FolderEntry;
        private SparkleShare.controls.ExampleTextBox ServerEntry;




    }
}
