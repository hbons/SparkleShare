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


using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace SparkleLib {

	// A persistent connection to the server that
	// listens for change notifications
	public class SparkleListener {

		// FIXME: The IrcClient is a public property because
		// extending it causes crashes
		public IrcClient Client;
		private Thread Thread;
		public readonly string Server;
		public readonly string FallbackServer;
		public readonly string Channel;
		public readonly string Nick;


		public SparkleListener (string server, string folder_name, string user_email)
		{

			// This is SparkleShare's centralized notification service.
			// Don't worry, we only use this server as a backup if you 
			// don't have your own. All data needed to connect is hashed and
			// we don't store any personal information ever.
			Server = "204.62.14.135";

			if (!user_email.Equals ("") && user_email != null)
				Nick = GetSHA1 (folder_name + user_email + "sparkles");
			else
				Nick = GetSHA1 (DateTime.Now.ToString () + "sparkles");
			
			Nick    = "s" + Nick.Substring (0, 7);
			Channel = "#" + GetSHA1 (server + folder_name + "sparkles");

			Client = new IrcClient () {
				PingTimeout          = 180,
				PingInterval         = 90
			};

		}


		// Starts a new thread and listens to the channel
		public void Listen ()
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
		
		
		public void Announce (string message)
		{
				
			Client.SendMessage (SendType.Message, Channel, message);
			
		}


		// Frees all resources for this Listener
		public void Dispose ()
		{

			Thread.Abort ();
			Thread.Join ();

		}

		
		// Creates an SHA-1 hash of input
		private static string GetSHA1 (string s)
		{
			SHA1 sha1 = new SHA1CryptoServiceProvider ();
			Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
			Byte[] encoded_bytes = sha1.ComputeHash (bytes);
			return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
		}
		
	}

}
