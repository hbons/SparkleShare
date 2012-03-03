//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

using System.Drawing;
using System.Windows.Forms;

namespace SparkleShare {

    public class SparkleAbout : Form {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private System.ComponentModel.IContainer components = null;
        private Label version;
        private Label copyright;
        private Label empty_label;
        private Label SparkleShareVersion;


        // Short alias for the translations
        public static string _(string s)
        {
            return Program._(s);
        }


        public SparkleAbout ()
        {
            Name = "SparkleAbout";

            Text = "About SparkleShare";
            Icon = Icons.sparkleshare;

            MaximizeBox = false;
            MinimizeBox = false;

            ClientSize          = BackgroundImage.Size;
            AutoScaleDimensions = new SizeF (6F, 13F);
            AutoScaleMode       = AutoScaleMode.Font;
            ClientSize          = new Size (640, 260);

            BackgroundImage = Icons.about;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;

            FormClosing += FormClosingEventHandler (Close);

            Program.TranslateWinForm (this);


            Controller.ShowWindowEvent += delegate {
                Invoke ((Action) delegate {
                    Show ();
                });
            };

            Controller.HideWindowEvent += delegate {
                Invoke ((Action) delegate {
                    Hide ();
                });
            };

            Controller.NewVersionEvent += delegate (string new_version) {
                Invoke ((Action) delegate {
                    this.version.Text = new_version;
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                Invoke ((Action) delegate {
                    this.version.Text = "You are running the latest version.";
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                Invoke ((Action) delegate {
                    this.version.Text = "Checking for updates...";
                });
            };


            CreateAbout ();
        }


        private void CreateAbout
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (SparkleAbout));
            this.version = new System.Windows.Forms.Label ();
            this.copyright = new System.Windows.Forms.Label ();
            this.emptyLabel = new System.Windows.Forms.Label ();
            this.SparkleShareVersion = new System.Windows.Forms.Label ();
            this.SuspendLayout ();

            this.version.AutoSize = true;
            this.version.BackColor = System.Drawing.Color.Transparent;
            this.version.ForeColor = System.Drawing.Color.LightGray;
            this.version.Location = new System.Drawing.Point (302, 102);
            this.version.Name = "version";
            this.version.Size = new System.Drawing.Size (34, 13);
            this.version.TabIndex = 1;
            this.version.Text = ".........";

            this.copyright.BackColor = System.Drawing.Color.Transparent;
            this.copyright.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.copyright.ForeColor = System.Drawing.Color.White;
            this.copyright.Location = new System.Drawing.Point (302, 135);
            this.copyright.Name = "copyright";
            this.copyright.Size = new System.Drawing.Size (298, 84);
            this.copyright.TabIndex = 2;
            this.copyright.Text = resources.GetString ("copyright.Text");

            this.emptyLabel.AutoSize = true;
            this.emptyLabel.Location = new System.Drawing.Point (16, 89);
            this.emptyLabel.Name = "emptyLabel";
            this.emptyLabel.Size = new System.Drawing.Size (0, 13);
            this.emptyLabel.TabIndex = 6;

            this.SparkleShareVersion.AutoSize = true;
            this.SparkleShareVersion.BackColor = System.Drawing.Color.Transparent;
            this.SparkleShareVersion.ForeColor = System.Drawing.Color.White;
            this.SparkleShareVersion.Location = new System.Drawing.Point (302, 89);
            this.SparkleShareVersion.Name = "SparkleShareVersion";
            this.SparkleShareVersion.Size = new System.Drawing.Size (106, 13);
            this.SparkleShareVersion.TabIndex = 1;
            this.SparkleShareVersion.Text = "SparkleShareVersion";


            this.Controls.Add (this.SparkleShareVersion);
            this.Controls.Add (this.emptyLabel);
            this.Controls.Add (this.copyright);
            this.Controls.Add (this.version);
            this.ResumeLayout (false);
            this.PerformLayout ();

            this.SparkleShareVersion.Text = Controller.RunningVersion;
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.version.Text = "";
        }


        protected override void Dispose (bool disposing) {
            if (disposing && (components != null))
                components.Dispose ();

            base.Dispose (disposing);
        }


        private void Close (object sender, FormClosingEventArgs args)
        {
            if (args.CloseReason != CloseReason.ApplicationExitCall &&
                args.CloseReason != CloseReason.TaskManagerClosing  &&
                args.CloseReason != CloseReason.WindowsShutDown) {

                args.Cancel = true;
                Controller.WindowClosed ();
            }
        }
    }
}
