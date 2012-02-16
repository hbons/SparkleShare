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
using System.Net;
using System.Xml;

using SparkleLib;

namespace SparkleShare {

    public class SparkleInvite {

        public string Address { get; private set; }
        public string RemotePath { get; private set; }
        public Uri AcceptUrl { get; private set; }

        public bool Valid {
            get {
                return (!string.IsNullOrEmpty (Address) &&
                        !string.IsNullOrEmpty (RemotePath) &&
                        !string.IsNullOrEmpty (AcceptUrl.ToString ()));
            }
        }


        public SparkleInvite (string address, string remote_path, string accept_url)
        {
            Initialize (address, remote_path, accept_url);
        }


        public SparkleInvite (string xml_file_path)
        {
            XmlDocument xml_document = new XmlDocument ();
            XmlNode node;

            string address     = "";
            string remote_path = "";
            string accept_url  = "";


            try {
                xml_document.Load (xml_file_path);

                node = xml_document.SelectSingleNode ("/sparkleshare/invite/address/text()");
                if (node != null) { address = node.Value; }

                node = xml_document.SelectSingleNode ("/sparkleshare/invite/remote_path/text()");
                if (node != null) { remote_path = node.Value; }

                node = xml_document.SelectSingleNode ("/sparkleshare/invite/accept_url/text()");
                if (node != null) { accept_url = node.Value; }

                Initialize (address, remote_path, accept_url);

            } catch (XmlException e) {
                SparkleHelpers.DebugInfo ("Invite", "Invalid XML: " + e.Message);
                return;
            }
        }


        public bool Accept ()
        {
            WebClient web_client = new WebClient ();

            try {
                web_client.DownloadData (AcceptUrl);
                SparkleHelpers.DebugInfo ("Invite", "Uploaded public key");

                return true;

            } catch (WebException e) {
                SparkleHelpers.DebugInfo ("Invite",
                    "Failed uploading public key: " + e.Message);

                return false;
            }
        }


        private void Initialize (string address, string remote_path, string accept_url)
        {/*
            if (!remote_path.StartsWith ("/"))
                remote_path = "/" + remote_path;

            if (!address.EndsWith ("/"))
                address = address + "/";
              */
            Address    = address;
            RemotePath = remote_path;
            AcceptUrl  = new Uri (accept_url);
        }
    }
}
