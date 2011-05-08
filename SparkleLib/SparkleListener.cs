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

    public enum NotificationServerType
    {
        Own,
        Central
    }


    // A persistent connection to the server that
    // listens for change notifications
    public class SparkleListener {

        private Thread thread;

        // FIXME: The IrcClient is a public property because
        // extending it causes crashes
        public IrcClient Client;
        public readonly string Server;
        public readonly string FallbackServer;
        public readonly string Channel;
        public readonly string Nick;


        public SparkleListener (string server, string folder_name,
                                string user_email, NotificationServerType type)
        {
            if (type == NotificationServerType.Own) {
                Server = server;
            } else {

                // This is SparkleShare's centralized notification service.
                // Don't worry, we only use this server as a backup if you
                // don't have your own. All data needed to connect is hashed and
                // we don't store any personal information ever.
                Server = "204.62.14.135";
            }

            if (!String.IsNullOrEmpty (user_email))
                Nick = SHA1 (folder_name + user_email + "sparkles");
            else
                Nick = SHA1 (DateTime.Now.ToString () + "sparkles");

            Nick    = "s" + Nick.Substring (0, 7);
            Channel = "#" + SHA1 (server + folder_name + "sparkles");

            Client = new IrcClient () {
                PingTimeout          = 180,
                PingInterval         = 90
            };
        }


        // Starts a new thread and listens to the channel
        public void Listen ()
        {
            this.thread = new Thread (
                new ThreadStart (delegate {
                    try {

                        // Connect, login, and join the channel
                        Client.Connect (new string [] {Server}, 6667);
                        Client.Login (Nick, Nick);
                        Client.RfcJoin (Channel);

                        // List to the channel, this blocks the thread
                        Client.Listen ();
                        Client.Disconnect ();
                    } catch (Meebey.SmartIrc4net.ConnectionException e) {
                        Console.WriteLine ("Could not connect: " + e.Message);
                    }
                })
            );

            this.thread.Start ();
        }


        public void Announce (string message)
        {
            Client.SendMessage (SendType.Message, Channel, message);
        }


        // Frees all resources for this Listener
        public void Dispose ()
        {
            this.thread.Abort ();
            this.thread.Join ();
        }


        // Creates an SHA-1 hash of input
        private string SHA1 (string s)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = sha1.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
        }

    }

}
