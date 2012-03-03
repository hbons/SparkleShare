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
using System.ComponentModel;	
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace SparkleShare {

    public class SparkleAbout : Form {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private IContainer components;
        private Label version;
        private Label copyright;
        private Label updates;


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
            MaximumSize         = Size;
            MinimumSize         = Size;

            BackgroundImage = Icons.about;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;

            FormClosing += Close;

            CreateAbout ();


            Controller.ShowWindowEvent += delegate {
                Invoke ((Action) delegate {
                    Show ();
                    Activate ();
                });
            };

            Controller.HideWindowEvent += delegate {
                Invoke ((Action) delegate {
                    Hide ();
                });
            };

            Controller.NewVersionEvent += delegate (string new_version) {
                Invoke ((Action) delegate {
                    this.updates.Text = "A newer version (" + new_version + ") is available!";
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                Invoke ((Action) delegate {
                    this.updates.Text = "You are running the latest version.";
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                Invoke ((Action) delegate {
                    this.updates.Text = "Checking for updates...";
                });
            };
        }


        private void CreateAbout ()
        {
            ComponentResourceManager resources =
                new ComponentResourceManager (typeof (SparkleAbout));

            SuspendLayout ();

            this.version = new Label () {
                AutoSize  = true,
                BackColor = Color.Transparent,
                ForeColor = Color.LightGray,
                Location  = new Point (302, 102),
                Size      = new Size (34, 13),
                Text      = "version " + Controller.RunningVersion
            };

            this.updates = new Label () {
                AutoSize  = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Location  = new Point (302, 89),
                Size      = new Size (106, 13),
                Text      = "Checking for updates..."
            };

            this.copyright = new Label () {
                BackColor = Color.Transparent,
                Font      = new Font ("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte) (0))),
                ForeColor = Color.White,
                Location  = new Point (302, 135),
                Size      = new Size (298, 84),
                Text      = "Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others.\n" +
                    "SparkleShare is Free and Open Source Software. You are free to use, modify, " +
                    "and redistribute it under the GNU General Public License version 3 or later."
            };

            Controls.Add (this.version);
            Controls.Add (this.updates);
            Controls.Add (this.copyright);

            ResumeLayout (false);
            PerformLayout ();
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

                Controller.WindowClosed ();
                args.Cancel = true;
            }
        }
    }
}
