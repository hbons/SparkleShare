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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

using SparkleLib;

namespace SparkleShare {

    public class SparkleInvite : XmlDocument {

        public string Address { get; private set; }
        public string RemotePath { get; private set; }
        public string Fingerprint { get; private set; }
        public string AcceptUrl { get; private set; }
        public string AnnouncementsUrl { get; private set; }

        public bool IsValid {
            get {
                return (!string.IsNullOrEmpty (Address) && !string.IsNullOrEmpty (RemotePath));
            }
        }


        public SparkleInvite (string xml_file_path) : base ()
        {
            try {
                Load (xml_file_path);

            } catch (XmlException e) {
                SparkleLogger.LogInfo ("Invite", "Error parsing XML", e);
                return;
            }

            Address          = ReadField ("address");
            RemotePath       = ReadField ("remote_path");
            AcceptUrl        = ReadField ("accept_url");
            AnnouncementsUrl = ReadField ("announcements_url");
            Fingerprint      = ReadField ("fingerprint");
        }


        public bool Accept (string public_key)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            if (string.IsNullOrEmpty (AcceptUrl))
                return true;

            string post_data   = "public_key=" + public_key;
            byte [] post_bytes = Encoding.UTF8.GetBytes (post_data);

            WebRequest request    = WebRequest.Create (AcceptUrl);
            request.Method        = "POST";
            request.ContentType   = "application/x-www-form-urlencoded";
            request.ContentLength = post_bytes.Length;

            Stream data_stream = request.GetRequestStream ();
            data_stream.Write (post_bytes, 0, post_bytes.Length);
            data_stream.Close ();

            HttpWebResponse response = null;

            try {
                response = (HttpWebResponse) request.GetResponse ();
                response.Close ();

            } catch (WebException e) {
                SparkleLogger.LogInfo ("Invite", "Failed uploading public key to " + AcceptUrl + "", e);
                return false;
            }

            if (response != null && response.StatusCode == HttpStatusCode.OK) {
                SparkleLogger.LogInfo ("Invite", "Uploaded public key to " + AcceptUrl);
                return true;
            }

            return false;
        }


        private string ReadField (string name)
        {
            try {
                XmlNode node = SelectSingleNode ("/sparkleshare/invite/" + name + "/text()");
                
                if (node != null)
                    return node.Value;    
                else 
                    return "";
                
            } catch (XmlException e) {
                SparkleLogger.LogInfo ("Invite", "Error reading field '" + name + "'", e);
                return "";
            }
        }
    }
}
