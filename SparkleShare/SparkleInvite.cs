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
using System.Threading;

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
            FullAddress = new Uri (host + "/" + path);
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

            byte [] message = new byte[4096];
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

                // SparkleShare's protocol format looks like this:
                // sparkle://user@host.org/path/token
                Uri uri      = new Uri (received_message);
                string token = uri.AbsolutePath.Substring (uri.AbsolutePath.LastIndexOf ("/") + 1);
                string path  = uri.AbsolutePath.Substring (0, uri.AbsolutePath.LastIndexOf ("/"));

                if (InviteReceived != null)
                    InviteReceived (new SparkleInvite (uri.Host, path, token));
            }

            tcp_client.Close ();
        }
    }
}
