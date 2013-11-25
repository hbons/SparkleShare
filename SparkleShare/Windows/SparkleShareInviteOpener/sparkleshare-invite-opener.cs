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
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using SparkleLib;

namespace SparkleShare {

    public class SparkleShare {

        public static void Main (string [] args)
        {
            new SparkleInviteOpen (args [0]);
        }
    }


    public class SparkleInviteOpen {

        public SparkleConfig Config { get; private set; }

        public SparkleInviteOpen()
        {
            string app_data_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string config_path = Path.Combine(app_data_path, "sparkleshare");

            Config = new SparkleConfig(config_path, "config.xml");
            SparkleConfig.DefaultConfig = Config;
        }

        public SparkleInviteOpen (string url) : this()
        {
            string xml = "";

            SparkleLogger.LogInfo("Invite", "Before parse: " + url);

            // Windows sometimes doesn't strip off protocol handlers            
            url = url.Replace ("sparkleshare://addProject/", "");
            url = url.Replace ("sparkleshare-unsafe://addProject/", "");

            // Outlook breaks URLs
            url = Regex.Replace (url, "(https?:)/([^/])", "$1//$2");
            WebClient web_client = new WebClient ();

            SparkleLogger.LogInfo("Invite", "Received invite url: " + url);

            try {
                xml = web_client.DownloadString (url);

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Invite", "Failed downloading invite: " + url, e);
                Environment.Exit (-1);
            }

            string file_name = DateTime.UtcNow.Millisecond.ToString () + ".xml";

            string home_path   = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
            string target_path = Path.Combine (home_path, "SparkleShare", file_name);

            if (xml.Contains ("<sparkleshare>")) {
                File.WriteAllText (target_path, xml);
                File.SetAttributes (target_path, FileAttributes.Hidden);
                SparkleLogger.LogInfo("Invite", "Downloaded invite: " + url);

                // TODO: Start SparkleShare.exe
            }
        }
    }
}
