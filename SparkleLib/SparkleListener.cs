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

	public class SparkleListener
	{

		public SparkleListener () {

			IrcClient irc_client = new IrcClient ();

//irc_client.OnRawMessage += HandleRawMessage;

			string [] server  = new string [] {"irc.gnome.org"};
			int port       = 6667;
			string channel = "#sparkletest";

			irc_client.OnConnected += delegate {
				Console.WriteLine ("!!!!!!!!!!11");
			};	

			irc_client.OnChannelMessage += delegate {
				Console.WriteLine ("!!!22222222222!!!!!!!11");
			};

try{
			irc_client.Connect (server, port);

} catch (Exception e) {

                                Console.WriteLine("Error occured. Lawldongs!");
                                Console.WriteLine(e);

                        }


			irc_client.Login("SmartIRC", "Stupid Bot");
            irc_client.RfcJoin(channel);

            irc_client.SendMessage(SendType.Message, channel, "HEllo");


Thread thread = new Thread(new ThreadStart(delegate {

			irc_client.Listen ();

}));
thread.Start();



		}


      void HandleRawMessage (object sender, IrcEventArgs args)

                {

                        System.Console.WriteLine(args.Data.Nick+": "+args.Data.Message);

                }

                

	}

}
