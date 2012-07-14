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

namespace SparkleShare {

    public class SparkleShare {

        public static void Main (string [] args) {

            new SparkleInviteOpen (args [0]);
        }
    }


    public class SparkleInviteOpen {

        public SparkleInviteOpen (string url)
        {
            string safe_url   = url.Replace ("sparkleshare://", "https://");
            string unsafe_url = url.Replace ("sparkleshare://", "http://");
            string xml        = "";

            WebClient web_client = new WebClient ();

            try {
                xml = web_client.DownloadString (safe_url);

            } catch {
                Console.WriteLine ("Failed downloading invite: " + safe_url);

                try {
                    xml = web_client.DownloadString (unsafe_url);

                } catch {
                    Console.WriteLine ("Failed downloading invite: " + unsafe_url);
                }
            }

            string home_path   = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            string target_path = Path.Combine (home_path, "SparkleShare");

            if (xml.Contains ("<sparkleshare>")) {
                File.WriteAllText (target_path, xml);
                Console.WriteLine ("Downloaded invite: " + safe_url);
            }
        }
    }
}
