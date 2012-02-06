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
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
                                SocketType.Stream, ProtocolType.Tcp) {

                                ReceiveTimeout = 60 * 1000,
                                SendTimeout    = 3 * 1000
                            };

                            // Try to connect to the server
                            this.socket.Connect (Server.Host, port);

                            this.is_connecting = false;
                            this.is_connected  = true;

                            OnConnected ();

                            // Subscribe to channels of interest to us
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

                    // Wait for messages
                    while (this.is_connected) {
                        try {
                            // This blocks the thread
                            bytes_read = this.socket.Receive (bytes);

                        // We've timed out, let's ping the server to
                        // see if the connection is still up
                        } catch (SocketException e) {

                            Console.WriteLine ("1st catch block");

                            try {
                                byte [] ping_bytes =
                                    Encoding.UTF8.GetBytes ("ping");

                                Console.WriteLine ("1");

                                this.socket.Send (ping_bytes);
                                this.socket.ReceiveTimeout = 3 * 1000;

                                Console.WriteLine ("2");

                                // 10057 means "Socket is not connected"
                                if (this.socket.Receive (bytes) < 1) {
                                    Console.WriteLine ("3");
                                    throw new SocketException (10057);
                                }

                                Console.WriteLine ("4");

                            // The ping failed: disconnect completely
                            } catch (SocketException) {
                                this.is_connected  = false;
                                this.is_connecting = false;

                                this.socket.ReceiveTimeout = 60 * 1000;

                                Console.WriteLine ("2nd catch block");

                                OnDisconnected (e.Message);
                                break;
                            }
                        }

                        // Parse the received message
                        if (bytes_read > 0) {
                            string received = Encoding.UTF8.GetString (bytes);
                            string line     = received.Substring (0, received.IndexOf ("\n"));

                            if (!line.Contains ("!"))
                                continue;

                            string folder_identifier = line.Substring (0, line.IndexOf ("!"));
                            string message           = CleanMessage (line.Substring (line.IndexOf ("!") + 1));

                            if (!folder_identifier.Equals ("debug") &&
                                !String.IsNullOrEmpty (message)) {

                                // We have a message!
                                OnAnnouncement (new SparkleAnnouncement (folder_identifier, message));
                            }
                        }
                    }
                })
            );

            this.thread.Start ();
        }


        protected override void AlsoListenTo (string folder_identifier)
        {
            string to_send = "subscribe " + folder_identifier + "\n";

            try {
                lock (this.socket_lock)
                    this.socket.Send (Encoding.UTF8.GetBytes (to_send));

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
    }
}
