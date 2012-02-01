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
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SparkleLib {

    public class SparkleListenerTcp : SparkleListenerBase {

        private Socket socket;
        private Object socket_lock = new Object ();
        private Thread thread;
        private bool is_connected  = false;
        private bool is_connecting = false;


        public SparkleListenerTcp (Uri server, string folder_identifier) :
            base (server, folder_identifier)
        {
        }


        public override bool IsConnected {
            get {
                lock (this.socket_lock)
                    return this.is_connected;
            }
        }


        public override bool IsConnecting {
            get {
                lock (this.socket_lock)
                    return this.is_connecting;
            }
        }


        // Starts a new thread and listens to the channel
        public override void Connect ()
        {
            SparkleHelpers.DebugInfo ("ListenerTcp", "Connecting to " + Server.Host);

            this.is_connecting = true;

            this.thread = new Thread (
                new ThreadStart (delegate {
                    int port = Server.Port;

                    if (port < 0)
                        port = 1986;

                    try {
                        lock (this.socket_lock) {
                            this.socket = new Socket (AddressFamily.InterNetwork,
                                SocketType.Stream, ProtocolType.Tcp);

                            // TODO: our own time comparison to account for system sleep?
                            this.socket.ReceiveTimeout = 30 * 1000;
                            this.socket.Blocking       = true;
                            this.socket.Connect (Server.Host, port);

                            this.is_connecting = false;
                            this.is_connected  = true;

                            OnConnected ();

                            foreach (string channel in base.channels) {
                                SparkleHelpers.DebugInfo ("ListenerTcp",
                                    "Subscribing to channel " + channel);

                                byte [] subscribe_bytes =
                                    Encoding.UTF8.GetBytes ("subscribe " + channel + "\n");

                                this.socket.Send (subscribe_bytes);
                            }
                        }

                    } catch (SocketException e) {
                        this.is_connected  = false;
                        this.is_connecting = false;

                        OnDisconnected (e.Message);
                        return;
                    }


                    byte [] bytes  = new byte [4096];
                    int bytes_read = 0;

                    // List to the channels, this blocks the thread
                    while (this.socket.Connected) {
                        try {
                            bytes_read = this.socket.Receive (bytes);

                        } catch (Exception e) {
                            if (!PingHost (Server.Host)) {
                                lock (this.socket_lock) {
                                    this.socket.Close ();
                                    this.is_connected = false;

                                    OnDisconnected (e.Message);
                                }
                            }
                        }

                        if (bytes_read > 0) {
                            string received = Encoding.UTF8.GetString (bytes);
                            string line     = received.Substring (0, received.IndexOf ("\n"));

                            if (!line.Contains ("!"))
                                continue;

                            string folder_identifier = line.Substring (0, line.IndexOf ("!"));
                            string message           = CleanMessage (line.Substring (line.IndexOf ("!") + 1));

                            if (!folder_identifier.Equals ("debug") &&
                                !String.IsNullOrEmpty (message)) {

                                OnAnnouncement (new SparkleAnnouncement (folder_identifier, message));
                            }
                        }
                    }

                    OnDisconnected ("");
                })
            );

            this.thread.Start ();
        }


        protected override void AlsoListenTo (string folder_identifier)
        {
            string to_send = "subscribe " + folder_identifier + "\n";

            try {
                lock (this.socket_lock) {
                    this.socket.Send (Encoding.UTF8.GetBytes (to_send));
                }

            } catch (SocketException e) {
                this.is_connected  = false;
                this.is_connecting = false;

                OnDisconnected (e.Message);
            }
        }


        protected override void Announce (SparkleAnnouncement announcement)
        {
            string to_send = "announce " + announcement.FolderIdentifier
                + " " + announcement.Message + "\n";

            try {
                lock (this.socket_lock)
                    this.socket.Send (Encoding.UTF8.GetBytes (to_send));

            } catch (SocketException e) {
                this.is_connected  = false;
                this.is_connecting = false;

                OnDisconnected (e.Message);
            }
        }


        public override void Dispose ()
        {
            this.thread.Abort ();
            this.thread.Join ();

            base.Dispose ();
        }


        private string CleanMessage (string message)
        {
            return message.Trim ()
                .Replace ("\n", "")
                .Replace ("\0", "");
        }


        private bool PingHost (string host)
        {
            Ping ping           = new Ping ();
            PingOptions options = new PingOptions () {
                DontFragment = true
            };

            string data    = "00000000000000000000000000000000";
            byte [] buffer = Encoding.ASCII.GetBytes (data);

            PingReply reply = ping.Send (host, 15, buffer, options);

            return reply.Status == IPStatus.Success;
        }
    }
}
