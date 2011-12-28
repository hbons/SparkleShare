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

using System.Windows.Forms;
using System.Drawing;

namespace SparkleShare {

    public partial class SparkleAbout : Form {

        public SparkleAboutController Controller = new SparkleAboutController ();

        // Short alias for the translations
        public static string _(string s)
        {
            return Program._(s);
        }


        public SparkleAbout ()
        {
            InitializeComponent ();

            this.BackgroundImage=Icons.about;
            this.ClientSize = this.BackgroundImage.Size;
            this.Icon = Icons.sparkleshare;

            this.SparkleShareVersion.Text = SparkleLib.Defines.VERSION;
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.version.Text = "";

            Program.TranslateWinForm (this);

            Controller.NewVersionEvent += delegate (string new_version) {
                this.version.Invoke((Action)delegate {
                    this.version.Text = new_version;
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                this.version.Invoke((Action)delegate {
                    this.version.Text = "You are running the latest version.";
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                this.version.Invoke((Action)delegate {
                    this.version.Text = "Checking for updates...";
                });
            };

        }

        private void SparkleAbout_FormClosing (object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall
                    && e.CloseReason != CloseReason.TaskManagerClosing
                    && e.CloseReason != CloseReason.WindowsShutDown) {
                e.Cancel = true;
                this.Hide ();
            }
        }
    }
}
