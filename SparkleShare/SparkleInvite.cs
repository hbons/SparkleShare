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
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Threading;

using SparkleLib;

namespace SparkleShare {

    public class SparkleInvite {

        public readonly Uri FullAddress;
        public readonly string Token;

        public string Host {
            get {
                return FullAddress.Host;
            }
        }

        public string Path {
            get {
                return FullAddress.AbsolutePath;
            }
        }


        public SparkleInvite (string host, string path, string token)
        {
            if (path.StartsWith ("/"))
                path = path.Substring (1);

            if (!host.EndsWith ("/"))
                host = host + "/";

            FullAddress = new Uri ("ssh://" + host + path);
            Token       = token;
        }


        public SparkleInvite (string xml_file_path)
        {
            XmlDocument xml_document = new XmlDocument ();
            XmlNode node;

            string host = "", path = "", token = "";

            try {
                xml_document.Load (xml_file_path);

                node = xml_document.SelectSingleNode ("/sparkleshare/invite/host/text()");
                if (node != null) { host = node.Value; }

                node = xml_document.SelectSingleNode ("/sparkleshare/invite/path/text()");
                if (node != null) { path = node.Value; }

                node = xml_document.SelectSingleNode ("/sparkleshare/invite/token/text()");
                if (node != null) { token = node.Value; }

            } catch (XmlException e) {
                SparkleHelpers.DebugInfo ("Invite", "Invalid XML: " + e.Message);
                return;
            }


            if (path.StartsWith ("/"))
                path = path.Substring (1);

            if (!host.EndsWith ("/"))
                host = host + "/";

            FullAddress = new Uri ("ssh://" + host + path);
            Token       = token;
        }
    }


    public class SparkleInviteListener {

        public event InviteReceivedHandler InviteReceived;
        public delegate void InviteReceivedHandler (SparkleInvite invite);

        private Thread thread;
        private TcpListener tcp_listener;


        public SparkleInviteListener (int port)
        {
            this.tcp_listener = new TcpListener (IPAddress.Loopback, port);
            this.thread       = new Thread(new ThreadStart (Listen));
        }


        public void Start ()
        {
            this.thread.Start ();
        }


        private void Listen ()
        {
            this.tcp_listener.Start ();

            while (true)
            {
                // Blocks until a client connects
                TcpClient client = this.tcp_listener.AcceptTcpClient ();

                // Create a thread to handle communications
                Thread client_thread = new Thread (HandleClient);
                client_thread.Start (client);
            }
        }


        private void HandleClient (object client)
        {
            TcpClient tcp_client = (TcpClient) client;
            NetworkStream client_stream = tcp_client.GetStream ();

            byte [] message = new byte [4096];
            int bytes_read;

            while (true)
            {
                bytes_read = 0;

                try {
                  // Blocks until the client sends a message
                  bytes_read = client_stream.Read (message, 0, 4096);
    
                } catch {
                    Console.WriteLine ("Socket error...");
                }
    
                // The client has disconnected
                if (bytes_read == 0)
                    break;

                ASCIIEncoding encoding  = new ASCIIEncoding ();
                string received_message = encoding.GetString (message, 0, bytes_read);
                string invite_xml = "";

                if (received_message.StartsWith (Uri.UriSchemeHttp) ||
                    received_message.StartsWith (Uri.UriSchemeHttps)) {

                    WebClient web_client = new WebClient ();

                    try {
                        // Fetch the invite file
                        byte [] buffer = web_client.DownloadData (received_message);
                        SparkleHelpers.DebugInfo ("Invite", "Received: " + received_message);

                        invite_xml = ASCIIEncoding.ASCII.GetString (buffer);

                    } catch (WebException e) {
                        SparkleHelpers.DebugInfo ("Invite", "Failed downloading: " +
                                                            received_message + " " + e.Message);
                        continue;
                    }

                } else if (received_message.StartsWith (Uri.UriSchemeFile)) {
                    try {
                        received_message = received_message.Replace (Uri.UriSchemeFile + "://", "");
                        invite_xml = File.ReadAllText (received_message);

                    } catch {
                        SparkleHelpers.DebugInfo ("Invite", "Failed opening: " + received_message);
                        continue;
                    }

                } else {
                    SparkleHelpers.DebugInfo ("Invite",
                        "Path to invite must use either the file:// or http(s):// scheme");

                    continue;
                }

                XmlDocument xml_document = new XmlDocument ();
                XmlNode node;

                string host = "", path = "", token = "";

                try {
                    xml_document.LoadXml (invite_xml);

                    node = xml_document.SelectSingleNode ("/sparkleshare/invite/host/text()");
                    if (node != null) { host = node.Value; }

                    node = xml_document.SelectSingleNode ("/sparkleshare/invite/path/text()");
                    if (node != null) { path = node.Value; }

                    node = xml_document.SelectSingleNode ("/sparkleshare/invite/token/text()");
                    if (node != null) { token = node.Value; }

                } catch (XmlException e) {
                    SparkleHelpers.DebugInfo ("Invite", "Invalid XML: " + received_message + " " + e.Message);
                    return;
                }

                if (InviteReceived != null)
                    InviteReceived (new SparkleInvite (host, path, token));
            }

            tcp_client.Close ();
        }
    }
}
