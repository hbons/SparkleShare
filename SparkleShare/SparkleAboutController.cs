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
using System.Timers;

using SparkleLib;

namespace SparkleShare {

    public class SparkleAboutController {

        public event ShowWindowEventHandler ShowWindowEvent;
        public delegate void ShowWindowEventHandler ();

        public event HideWindowEventHandler HideWindowEvent;
        public delegate void HideWindowEventHandler ();

        public event NewVersionEventHandler NewVersionEvent;
        public delegate void NewVersionEventHandler (string new_version);

        public event VersionUpToDateEventHandler VersionUpToDateEvent;
        public delegate void VersionUpToDateEventHandler ();

        public event CheckingForNewVersionEventHandler CheckingForNewVersionEvent;
        public delegate void CheckingForNewVersionEventHandler ();


        public string RunningVersion {
            get {
                return SparkleBackend.Version;
            }
        }


        public SparkleAboutController ()
        {
            Program.Controller.ShowAboutWindowEvent += delegate {
                if (ShowWindowEvent != null)
                    ShowWindowEvent ();

                CheckForNewVersion ();
            };
        }


        public void WindowClosed ()
        {
            if (HideWindowEvent != null)
                HideWindowEvent ();
        }


        public void CheckForNewVersion ()
        {
            if (CheckingForNewVersionEvent != null)
                CheckingForNewVersionEvent ();

            WebClient web_client = new WebClient ();
            Uri uri = new Uri ("http://www.sparkleshare.org/version");

            web_client.DownloadStringCompleted += delegate (object o, DownloadStringCompletedEventArgs args) {
                if (args.Error != null)
                    return;

                int running_version = int.Parse (
                    "" + RunningVersion [0] + RunningVersion [2] + RunningVersion [4]
                );

                string result = args.Result.Trim ();
                int new_version = int.Parse (
                    "" + result [0] + result [2] + result [4]
                );

                // Add a little delay, making it seems we're
                // actually doing hard work
                Thread.Sleep (2 * 1000);

                if (running_version >= new_version) {
                    if (VersionUpToDateEvent != null)
                        VersionUpToDateEvent ();

                } else {
                    if (NewVersionEvent != null)
                        NewVersionEvent (result);
                }
            };

            web_client.DownloadStringAsync (uri);
        }
    }
}
