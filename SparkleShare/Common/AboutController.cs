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

using Sparkles;

namespace SparkleShare {

    public class AboutController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event UpdateLabelEventDelegate UpdateLabelEvent = delegate { };
        public delegate void UpdateLabelEventDelegate (string text);

        public readonly string WebsiteLinkAddress       = "https://www.sparkleshare.org/";
        public readonly string CreditsLinkAddress       = "https://github.com/hbons/SparkleShare/blob/master/legal/Authors.txt";
        public readonly string ReportProblemLinkAddress = "https://www.github.com/hbons/SparkleShare/issues";
        public readonly string DebugLogLinkAddress      = "file://" + SparkleShare.Controller.Config.LogFilePath;

        public string RunningVersion;


        public AboutController ()
        {
            RunningVersion = InstallationInfo.Version;

            SparkleShare.Controller.ShowAboutWindowEvent += delegate {
                ShowWindowEvent ();
                new Thread (CheckForNewVersion).Start ();
            };
        }


        public void WindowClosed ()
        {
            HideWindowEvent ();
        }


        void CheckForNewVersion ()
        {
            UpdateLabelEvent ("Checking for updates…");
            Thread.Sleep (500);

            var web_client = new WebClient ();
            var uri = new Uri ("http://www.sparkleshare.org/version");

            try {
                string latest_version = web_client.DownloadString (uri);
                latest_version = latest_version.Trim ();
            
                if (new Version (latest_version) > new Version (RunningVersion))
                    UpdateLabelEvent ("An update (version " + latest_version + ") is available!");
                else
                    UpdateLabelEvent ("✓ You are running the latest version");

            } catch {
                UpdateLabelEvent ("Couldn’t check for updates\t");
            }
        }
    }
}
