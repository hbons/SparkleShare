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
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Meebey.SmartIrc4net;

namespace SparkleLib {

    public class SparkleListenerIrc : SparkleListenerBase {

        private Thread thread;
        private IrcClient client;
        private string nick;


        public SparkleListenerIrc (Uri server, string folder_identifier) :
            base (server, folder_identifier)
        {
            // Try to get a uniqueish nickname
            this.nick = SHA1 (DateTime.Now.ToString ("ffffff") + "sparkles");

            // Most irc servers don't allow nicknames starting
            // with a number, so prefix an alphabetic character
            this.nick = "s" + this.nick.Substring (0, 7);

            base.channels.Add ("#" + folder_identifier);

            this.client = new IrcClient () {
                PingTimeout  = 180,
                PingInterval = 60
            };

            string proxy  = Environment.GetEnvironmentVariable ("http_proxy");
            Uri proxy_uri = null;
            if (!String.IsNullOrEmpty (proxy) &&
                Uri.TryCreate (proxy, UriKind.Absolute, out proxy_uri)) {

#if __MonoCS__
                if (proxy_uri.Scheme == "http") {
                    this.client.ProxyType = ProxyType.Http;
                    this.client.ProxyHost = proxy_uri.Host;
                    this.client.ProxyPort = proxy_uri.Port;
                }
#endif
            }

            this.client.OnConnected += delegate {
                base.is_connecting = false;
                OnConnected ();
            };

            this.client.OnDisconnected += delegate {
                base.is_connecting = false;
                OnDisconnected ();
            };

            this.client.OnError += delegate {
                base.is_connecting = false;
                OnDisconnected ();
            };

            this.client.OnChannelMessage += delegate (object o, IrcEventArgs args) {
                string message = args.Data.Message.Trim ();
                string folder_id = args.Data.Channel.Substring (1); // remove the starting hash
                OnAnnouncement (new SparkleAnnouncement (folder_id, message));
            };
        }


        public override bool IsConnected {
            get {
              return this.client.IsConnected;
            }
        }


        // Starts a new thread and listens to the channel
        public override void Connect ()
        {
            SparkleHelpers.DebugInfo ("ListenerIrc", "Connecting to " + Server);

            base.is_connecting = true;

            this.thread = new Thread (
                new ThreadStart (delegate {
                    try {
                        // Connect, login, and join the channel
                        int port = base.server.Port;
                        if (port < 0) port = 6667;
                        this.client.Connect (base.server.Host, port);
                        this.client.Login (this.nick, this.nick, 8, this.nick);

                        foreach (string channel in base.channels) {
                            SparkleHelpers.DebugInfo ("ListenerIrc", "Joining channel " + channel);
                            this.client.RfcJoin (channel);
                            this.client.RfcMode (channel, "+s");
                        }

                        // List to the channel, this blocks the thread
                        this.client.Listen ();

                        // Disconnect when we time out
                        this.client.Disconnect ();

                    } catch (ConnectionException e) {
                        SparkleHelpers.DebugInfo ("ListenerIrc", "Could not connect to " + Server + ": " + e.Message);
                    }
                })
            );

            this.thread.Start ();
        }


        public override void AlsoListenTo (string folder_identifier)
        {
            string channel = "#" + folder_identifier;
            if (!base.channels.Contains (channel)) {
                base.channels.Add (channel);

                if (IsConnected) {
                    SparkleHelpers.DebugInfo ("ListenerIrc", "Joining channel " + channel);
                    this.client.RfcJoin (channel);
                    this.client.RfcMode (channel, "+s");
                }
            }
        }


        public override void Announce (SparkleAnnouncement announcement)
        {
            string channel = "#" + announcement.FolderIdentifier;
            this.client.SendMessage (SendType.Message, channel, announcement.Message);

            // Also announce to ourselves for debugging purposes
            // base.OnAnnouncement (announcement);
        }


        public override void Dispose ()
        {
            this.thread.Abort ();
            this.thread.Join ();
            base.Dispose ();
        }


        // Creates a SHA-1 hash of input
        private string SHA1 (string s)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = sha1.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
        }
    }
}
