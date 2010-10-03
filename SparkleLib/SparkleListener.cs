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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SparkleLib {

	// A persistent connection to the server that
	// listens for change notifications
	public class SparkleListener
	{

		// FIXME: The IrcClient is a public property because
		// extending it causes crashes
		public IrcClient Client;
		private Thread Thread;
		public readonly string Server;
		public readonly string Channel;
		public readonly string Nick;


		public SparkleListener (string server, string channel, string nick)
		{

			Server  = server;
			Channel = channel;
			Nick    = nick.Replace ("@", "_at_").Replace (".", "_dot_");

			// Keep the nick short
			if (Nick.Length > 9)
				Nick = Nick.Substring (0, 9);

			// TODO: Remove these hardcoded values
			Channel = "#sparkletest";
			Server  = "irc.gnome.org";

			Client = new IrcClient () {
				PingTimeout          = 90,
				SocketSendTimeout    = 90,
				SocketReceiveTimeout = 90,
				AutoRetry            = true,
				AutoReconnect        = true,
				AutoRejoin           = true
			};

		}


		// Starts a new thread and listens to the channel
		public void ListenForChanges ()
		{

			Thread = new Thread (
				new ThreadStart (delegate {

					try {

						// Connect to the server
						Client.Connect (new string [] {Server}, 6667);

						// Login to the server
						Client.Login (Nick, Nick);

						// Join the channel
						Client.RfcJoin (Channel);

						Client.Listen ();

						Client.Disconnect ();

					} catch (Meebey.SmartIrc4net.ConnectionException e) {

						Console.WriteLine ("Could not connect: " + e.Message);

					}

				})
			);

			Thread.Start ();
	
		}


		// Frees all resources for this Listener
		public void Dispose ()
		{

			Thread.Abort ();
			Thread.Join ();

		}

	}

}
