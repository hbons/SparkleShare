//   MarkLogic SparkleShare backend extension
//   Adam Fowler <adam.fowler@marklogic.com>
//   Copyright 2013 MarkLogic Corporation

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using SparkleLib;
using MarkLogicLib;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Logging;

namespace SparkleLib.Marklogic {

    // Sets up a fetcher that can get remote folders
    public class SparkleFetcher : SparkleFetcherBase {

        public SparkleFetcher (SparkleFetcherInfo info) : base (info)
        {
            
            
            // configure service stack logger
            LogManager.LogFactory = new SparkleShareLogFactory ();

            UseSSHKeys = false;
            ParseRemoteUrl = false;
        }


        public override bool Fetch ()
        {
            SparkleLogger.LogInfo ("Fetcher", "Starting fetch for: " + RemoteUrl.AbsoluteUri);

            // replace RemoteUrl ssh+marklogic with http // TODO do this in the general SparkleShare library
            
            string remote_path  = OriginalFetcherInfo.RemotePath.Trim ("/".ToCharArray ());
            string address      = OriginalFetcherInfo.Address;
            
            if (address.EndsWith ("/"))
                address = address.Substring (0, address.Length - 1);
            
            if (!remote_path.StartsWith ("/"))
                remote_path = "/" + remote_path;
            
            if (address.StartsWith ("ssh+marklogic"))
                address = "http" + address.Substring(13);

            
            RemoteUrl = new Uri (address + remote_path);
            SparkleLogger.LogInfo ("Fetcher", "URI now: " + RemoteUrl.AbsoluteUri);
            
            Connection connection = new Connection ();
            Options opts = new Options ();
            opts.setConnectionString (RemoteUrl.AbsoluteUri);
            connection.configure (opts);
            SparkleLogger.LogInfo ("Fetcher", "MarkLogic Connection configured. Syncing for first time. Values: " + connection.options.ToString());
            SparkleLogger.LogInfo ("Fetcher", "MarkLogic Connection string: " + connection.options.getConnectionString());


            bool result = SparkleLib.Marklogic.SparkleRepo.doSyncDown (connection,"0",TargetFolder);
            base.OnProgressChanged (100);
            SparkleLogger.LogInfo ("Fetcher", "Completed fetch");
            return result;
        }


        public override bool IsFetchedRepoEmpty {
            get {
                return true; // TODO dont hardcode this
            }
        }


        public override void EnableFetchedRepoCrypto (string password)
        {
            // do nothing? - on URL scheme
        }


        public override bool IsFetchedRepoPasswordCorrect (string password)
        {
            // execute dummy file get? - may need to do this if there is internal SparkleRepo logic for it
            return true; // TODO verify this logic
        }


        public override void Stop ()
        {
            try {
                // TODO update for ML - do nothing for now. May need to do this on async connection to kill in flight initial fetch

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Fetcher", "Failed to dispose properly", e);
            }
        }


        public override void Complete ()
        {
            // TODO update for ML - do nothing for now

            base.Complete ();
        }

        /*
        // do we really need this? NO
        private void InstallConfiguration ()
        {
            string [] settings = new string [] {
                "ml.somesetting false" // settings go here. Pass to connection later
            };

            foreach (string setting in settings) {
                //SparkleGit git_config = new SparkleGit (TargetFolder, "config " + setting);
                //git_config.StartAndWaitForExit ();
            }

        }
        */


    }
}
