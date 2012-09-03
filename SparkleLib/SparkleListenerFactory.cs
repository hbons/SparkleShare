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
using System.Collections.Generic;

namespace SparkleLib {

    public static class SparkleListenerFactory {

        private static List<SparkleListenerBase> listeners = new List<SparkleListenerBase> ();


        public static SparkleListenerBase CreateListener (string folder_name, string folder_identifier)
        {
            // Check if the user wants to use a global custom notification service
            string uri = SparkleConfig.DefaultConfig.GetConfigOption ("announcements_url");

            // Check if the user wants a use a custom notification service for this folder
            if (string.IsNullOrEmpty (uri))
                uri = SparkleConfig.DefaultConfig.GetFolderOptionalAttribute (folder_name, "announcements_url");

            // This is SparkleShare's centralized notification service.
            // It communicates "It's time to sync!" signals between clients.
            //
            // Please see the SparkleShare wiki if you wish to run
            // your own service instead
            if (string.IsNullOrEmpty (uri))
                uri = "tcp://notifications.sparkleshare.org:443";

            Uri announce_uri = new Uri (uri);

            // Use only one listener per notification service to keep
            // the number of connections as low as possible
            foreach (SparkleListenerBase listener in listeners) {
                if (listener.Server.Equals (announce_uri)) {
                    SparkleLogger.LogInfo ("ListenerFactory", "Refered to existing listener for " + announce_uri);

                    // We already seem to have a listener for this server,
                    // refer to the existing one instead
                    listener.AlsoListenTo (folder_identifier);
                    return (SparkleListenerBase) listener;
                }
            }

            listeners.Add (new SparkleListenerTcp (announce_uri, folder_identifier));
            SparkleLogger.LogInfo ("ListenerFactory", "Issued new listener for " + announce_uri);

            return (SparkleListenerBase) listeners [listeners.Count - 1];
        }
    }
}
