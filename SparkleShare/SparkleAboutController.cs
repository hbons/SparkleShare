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
using System.Net;
using System.Threading;

namespace SparkleShare {

    public class SparkleAboutController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event UpdateLabelEventDelegate UpdateLabelEvent = delegate { };
        public delegate void UpdateLabelEventDelegate (string text);

        public readonly string WebsiteLinkAddress       = "http://www.sparkleshare.org/";
        public readonly string CreditsLinkAddress       = "http://www.github.com/hbons/SparkleShare/tree/master/legal/AUTHORS";
        public readonly string ReportProblemLinkAddress = "http://www.github.com/hbons/SparkleShare/issues";
        public readonly string DebugLogLinkAddress      = "file://" + Program.Controller.ConfigPath;

        public string RunningVersion {
            get {
                string version = SparkleLib.SparkleBackend.Version;

                if (version.EndsWith (".0"))
                    version = version.Substring (0, version.Length - 2);

                return version;
            }
        }


        public SparkleAboutController ()
        {
            Program.Controller.ShowAboutWindowEvent += delegate {
                ShowWindowEvent ();
                new Thread (() => CheckForNewVersion ()).Start ();
            };
        }


        public void WindowClosed ()
        {
            HideWindowEvent ();
        }


        private void CheckForNewVersion ()
        {
            UpdateLabelEvent ("Checking for updates...");
            Thread.Sleep (500);

            WebClient web_client = new WebClient ();
            Uri uri = new Uri ("http://www.sparkleshare.org/version");

            try {
                string latest_version = web_client.DownloadString (uri).Trim ();
            
                if (new Version (latest_version) > new Version (RunningVersion))
                    UpdateLabelEvent ("A newer version (" + latest_version + ") is available!");
                else
                    UpdateLabelEvent ("You are running the latest version.");

            } catch {
                UpdateLabelEvent ("Version check failed.");
            }
        }
    }
}
