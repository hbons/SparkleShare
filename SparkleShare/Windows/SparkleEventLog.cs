//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;

using SparkleLib;
using CefSharp;
using System.IO;
using System.Text;

namespace SparkleShare {

    public partial class SparkleEventLog : Form, IBeforeResourceLoad {
        private readonly CefWebBrowser _browserControl;

        private string HTML;
        private List<SparkleChangeSet> change_sets;
        private string selected_log = null;

        // Short alias for the translations
        public static string _ (string s)
        {
            return s;
        }

        public SparkleEventLog () 
        {
            InitializeComponent ();

            this.Icon = Icons.sparkleshare;

            this.change_sets = Program.Controller.GetLog (null);
            GenerateHTML ();

            _browserControl = new CefWebBrowser ("application://sparkleshare/eventlog");
            _browserControl.Dock = DockStyle.Fill;
            //_browserControl.PropertyChanged += HandleBrowserPropertyChanged;
            //_browserControl.ConsoleMessage += HandleConsoleMessage;
            _browserControl.BeforeResourceLoadHandler = this;
            WebViewPanel.Controls.Add (_browserControl);
            
            UpdateChooser ();
        }

        public void UpdateChooser ()
        {
            this.combo_box.Items.Add (_ ("All Folders"));
            this.combo_box.Items.Add ("");

            foreach (string folder_name in Program.Controller.Folders)
                this.combo_box.Items.Add (folder_name);

            this.combo_box.SelectedItem = this.combo_box.Items[0];
        }


        public void UpdateEvents ()
        {
            UpdateEvents (true);
        }


        public void UpdateEvents (bool silent)
        {
            Thread thread = new Thread (new ThreadStart (delegate {
                Stopwatch watch = new Stopwatch ();
                watch.Start ();
                this.change_sets = Program.Controller.GetLog (this.selected_log);
                GenerateHTML ();
                watch.Stop ();

                // A short delay is less annoying than
                // a flashing window
                if (watch.ElapsedMilliseconds < 500 && !silent)
                    Thread.Sleep (500 - (int) watch.ElapsedMilliseconds);

                AddHTML ();
            }));

            thread.Start ();
        }


        private void GenerateHTML ()
        {
            HTML = Program.Controller.GetHTMLLog (this.change_sets);

            if (HTML == null)
                return;

            HTML = HTML.Replace ("<!-- $body-font-size -->", this.Font.Size + "px");
            HTML = HTML.Replace ("<!-- $day-entry-header-font-size -->", this.Font.Size + "px");
            HTML = HTML.Replace ("<!-- $a-color -->", "#0085cf");
            HTML = HTML.Replace ("<!-- $a-hover-color -->", "#009ff8");
            HTML = HTML.Replace ("<!-- $body-font-family -->", "\"" + this.Font.FontFamily + "\"");
            HTML = HTML.Replace ("<!-- $body-color -->", this.ForeColor.ToHex());
            HTML = HTML.Replace ("<!-- $body-background-color -->", this.BackColor.ToHex());
            HTML = HTML.Replace ("<!-- $day-entry-header-background-color -->", this.BackColor.ToHex());
            HTML = HTML.Replace ("<!-- $secondary-font-color -->", this.ForeColor.ToHex());
            HTML = HTML.Replace ("<!-- $small-color -->", this.ForeColor.ToHex());    
            HTML = HTML.Replace ("<!-- $no-buddy-icon-background-image -->",
                "application://sparkleshare/avatar-default-32.png");
            HTML = HTML.Replace ("<!-- $document-added-background-image -->",
                "application://sparkleshare/document-added-12.png");
            HTML = HTML.Replace ("<!-- $document-edited-background-image -->",
                "application://sparkleshare/document-edited-12.png");
            HTML = HTML.Replace ("<!-- $document-deleted-background-image -->",
                "application://sparkleshare/document-deleted-12.png");
            HTML = HTML.Replace ("<!-- $document-moved-background-image -->",
                "application://sparkleshare/document-moved-12.png");

            HTML = HTML.Replace ("href='" + SparklePaths.SparklePath, "href='application://file/" + SparklePaths.SparklePath);
            HTML = HTML.Replace ("file://application://sparkleshare/", "application://sparkleshare/");
            HTML = HTML.Replace ("file://", "application://file/");
        }


        private void AddHTML ()
        {
            Invoke ((Action)delegate {
                _browserControl.Reload ();
            });
        }

        private void SparkleEventLog_FormClosing (object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide ();
        }

    
        #region IBeforeResourceLoad Members

        public void HandleBeforeResourceLoad (CefWebBrowser browserControl, IRequestResponse requestResponse)
        {
            IRequest request = requestResponse.Request;
            Console.WriteLine ("{0} {1}", request.Method, request.Url);

            if (request.Url.StartsWith ("application://sparkleshare/eventlog")) {
                Stream resourceStream;
                if (HTML != null)
                    resourceStream = new MemoryStream (Encoding.UTF8.GetPreamble ().Concat (Encoding.UTF8.GetBytes (HTML)).ToArray ());
                else
                    resourceStream = new MemoryStream ();

                requestResponse.RespondWith (resourceStream, "text/html");
            } else if (request.Url.StartsWith ("application://file/")) {
                string Filename = request.Url.Substring ("application://file/".Length);
                Filename = Uri.UnescapeDataString (Filename);
                Filename = Filename.Replace ("/", "\\");

                if (Filename.StartsWith (SparklePaths.SparklePath))
                    System.Diagnostics.Process.Start (Filename);
            }
        }

        #endregion

        private void combo_box_SelectedIndexChanged (object sender, EventArgs e)
        {
            String SelectedText = this.combo_box.SelectedItem as string;

            if (string.IsNullOrEmpty (SelectedText) || SelectedText.Equals (_ ("All Folders")))
                this.selected_log = null;
            else
                this.selected_log = SelectedText;

            UpdateEvents (false);
        }
    }
}

