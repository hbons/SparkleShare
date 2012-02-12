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
        private Thread thread;
        private Object socket_lock  = new Object ();
        private bool is_connected   = false;
        private bool is_connecting  = false;
        private DateTime last_ping  = DateTime.Now;


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
                                ReceiveTimeout = 5 * 1000,
                                SendTimeout    = 5 * 1000
                            };

                            // Try to connect to the server
                            this.socket.Connect (Server.Host, port);

                            this.is_connecting = false;
                            this.is_connected  = true;

                            OnConnected ();

                            // Subscribe to channels of interest to us
                            foreach (string channel in base.channels)
                                AlsoListenToInternal (channel);
                        }

                    } catch (SocketException e) {
                        this.is_connected  = false;
                        this.is_connecting = false;

                        this.socket.Dispose ();

                        OnDisconnected (e.Message);
                        return;
                    }


                    byte [] bytes  = new byte [4096];
                    int bytes_read = 0;
                    this.last_ping = DateTime.Now;

                    // Wait for messages
                    while (this.is_connected) {

                        // This blocks the thread
                        int i = 0;
                        while (this.socket.Available < 1) {
                            Thread.Sleep (1000);
                            i++;

                            try {
                                // We've timed out, let's ping the server to
                                // see if the connection is still up
                                if (i == 180) {
                                    SparkleHelpers.DebugInfo ("ListenerTcp",
                                        "Pinging " + Server);

                                    byte [] ping_bytes = Encoding.UTF8.GetBytes ("ping\n");
                                    byte [] pong_bytes = new byte [4096];

                                    lock (this.socket_lock)
                                        this.socket.Send (ping_bytes);

                                    if (this.socket.Receive (pong_bytes) < 1)
                                        // 10057 means "Socket is not connected"
                                        throw new SocketException (10057);

                                    SparkleHelpers.DebugInfo ("ListenerTcp",
                                        "Received pong from " + Server);

                                    i = 0;
                                    this.last_ping = DateTime.Now;

                                } else {
                                    // Check when the last ping occured. If it's
                                    // significantly longer than our regular interval the
                                    // system likely woke up from sleep and we want to
                                    // simulate a disconnect
                                    int sleepiness = DateTime.Compare (
                                        this.last_ping.AddMilliseconds (180 * 1000 * 1.2),
                                        DateTime.Now
                                    );
    
                                    if (sleepiness <= 0) {
                                        SparkleHelpers.DebugInfo ("ListenerTcp",
                                            "System woke up from sleep");

                                        // 10057 means "Socket is not connected"
                                        throw new SocketException (10057);
                                    }
                                }

                            // The ping failed: disconnect completely
                            } catch (SocketException) {
                                this.is_connected          = false;
                                this.is_connecting         = false;;

                                this.socket.Dispose ();

                                OnDisconnected ("Ping timeout");
                                return;
                            }
                        }


                        if (this.socket.Available > 0)
                            lock (this.socket_lock)
                                bytes_read = this.socket.Receive (bytes);


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


        protected override void AlsoListenToInternal (string folder_identifier)
        {
            SparkleHelpers.DebugInfo ("ListenerTcp",
                "Subscribing to channel " + folder_identifier + " on " + Server);

            string to_send = "subscribe " + folder_identifier + "\n";

            try {
                lock (this.socket_lock)
                    this.socket.Send (Encoding.UTF8.GetBytes (to_send));

                this.last_ping = DateTime.Now;

            } catch (SocketException e) {
                this.is_connected  = false;
                this.is_connecting = false;

                OnDisconnected (e.Message);
            }
        }


        protected override void AnnounceInternal (SparkleAnnouncement announcement)
        {
            string to_send = "announce " + announcement.FolderIdentifier
                + " " + announcement.Message + "\n";

            try {
                lock (this.socket_lock)
                    this.socket.Send (Encoding.UTF8.GetBytes (to_send));

                this.last_ping = DateTime.Now;

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
