//   SparkleShare, an instant update workflow to Git.
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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Meebey.SmartIrc4net;

namespace SparkleLib {

    public class SparkleListenerIrc : SparkleListenerBase {

        private Thread thread;
        private IrcClient client;
        private string nick;


        public SparkleListenerIrc (string server, string folder_identifier,
            NotificationServerType type) : base (server, folder_identifier, type)
        {
            if (type == NotificationServerType.Own) {
                base.server = server;
            } else {

                // This is SparkleShare's centralized notification service.
                // Don't worry, we only use this server as a backup if you
                // don't have your own. All data needed to connect is hashed and
                // we don't store any personal information ever
                base.server = "204.62.14.135";
            }

            // Try to get a uniqueish nickname
            this.nick = SHA1 (DateTime.Now.ToString ("ffffff") + "sparkles");

            // Most irc servers don't allow nicknames starting
            // with a number, so prefix an alphabetic character
            this.nick = "s" + this.nick.Substring (0, 7);

            // Hash and salt the folder identifier, so
            // nobody knows any possible folder details
            base.channel = "#" + SHA1 (folder_identifier + "sparkles");

            this.client = new IrcClient () {
                PingTimeout  = 180,
                PingInterval = 90
            };

            this.client.OnConnected += delegate {
                OnConnected ();
            };

            this.client.OnDisconnected += delegate {
                OnDisconnected ();
            };

            this.client.OnChannelMessage += delegate (object o, IrcEventArgs args) {
                string message = args.Data.Message.Trim ();
                OnRemoteChange (message);
            };
        }


        // Starts a new thread and listens to the channel
        public override void Connect ()
        {
            SparkleHelpers.DebugInfo ("ListenerIrc", "Connecting to " + base.channel + " on " + base.server);
        
            this.thread = new Thread (
                new ThreadStart (delegate {
                    try {

                        // Connect, login, and join the channel
                        this.client.Connect (new string [] {base.server}, 6667);
                        this.client.Login (this.nick, this.nick);
                        this.client.RfcJoin (base.channel);

                        // List to the channel, this blocks the thread
                        this.client.Listen ();

                        // Disconnect when we time out
                        this.client.Disconnect ();
                    } catch (Meebey.SmartIrc4net.ConnectionException e) {
                        SparkleHelpers.DebugInfo ("ListenerIrc", "Could not connect to " + base.channel + " on " + base.server + ": " + e.Message);
                    }
                })
            );

            this.thread.Start ();
        }


        public override void Announce (string message)
        {
            this.client.SendMessage (SendType.Message, base.channel, message);
        }


        public override bool IsConnected {
            get {
              return this.client.IsConnected;
            }
        }


        public override void Dispose ()
        {
            this.thread.Abort ();
            this.thread.Join ();
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
