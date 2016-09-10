//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//   Portions Copyright (C) 2016 Paul Hammant <paul@hammant.org>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as
//   published by the Free Software Foundation, either version 3 of the
//   License, or (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sparkles.Subversion {

    public class SubversionFetcher : SSHFetcher {

        SubversionCommand svn_checkout;
        SSHAuthenticationInfo auth_info;

        protected override bool IsFetchedRepoEmpty {
            get {
                var svn_rev_parse = new SubversionCommand (TargetFolder, "ls -v -r");
                svn_rev_parse.StartAndWaitForExit ();

                return (svn_rev_parse.ExitCode != 0);
            }
        }


        public SubversionFetcher (SparkleFetcherInfo fetcher_info, SSHAuthenticationInfo auth_info) : base (fetcher_info)
        {

            this.auth_info = auth_info;
            var uri_builder = new UriBuilder (RemoteUrl);

            uri_builder.Scheme = "svn";

            RemoteUrl = uri_builder.Uri;

            AvailableStorageTypes.Add (
                new StorageTypeInfo (StorageType.Encrypted, "Encrypted Storage",
                    "Trade off efficiency for privacy;\nencrypts before storing files on the host"));
        }


        public override bool Fetch ()
        {
            if (!base.Fetch ())
                return false;

            StorageType? storage_type = StorageType.Plain;

            if (storage_type == null)
                return false;

            FetchedRepoStorageType = (StorageType) storage_type;

            string svn_checkout_command = "checkout";


            svn_checkout = new SubversionCommand (Configuration.DefaultConfiguration.TmpPath,
                string.Format ("{0} \"{1}\" \"{2}\"", svn_checkout_command, RemoteUrl, TargetFolder),
                auth_info);

            svn_checkout.StartInfo.RedirectStandardError = true;
            svn_checkout.Start ();

            StreamReader output_stream = svn_checkout.StandardError;

            if (FetchedRepoStorageType == StorageType.LargeFiles)
                output_stream = svn_checkout.StandardOutput;

            double percentage = 0;
            double speed = 0;
            string information = "";

            while (!output_stream.EndOfStream) {
                string line = output_stream.ReadLine ();

                OnProgressChanged (percentage, speed, information);
            }

            svn_checkout.WaitForExit ();

            if (svn_checkout.ExitCode != 0)
                return false;

            Thread.Sleep (500);
            OnProgressChanged (100, 0, "");
            Thread.Sleep (500);

            return true;
        }


        public override void Stop ()
        {
            try {
                if (svn_checkout != null && !svn_checkout.HasExited) {
                    svn_checkout.Kill ();
                    svn_checkout.Dispose ();
                }

            } catch (Exception e) {
                Logger.LogInfo ("Fetcher", "Failed to dispose properly", e);
            }

            if (Directory.Exists (TargetFolder)) {
                try {
                    Directory.Delete (TargetFolder, true /* Recursive */ );
                    Logger.LogInfo ("Fetcher", "Deleted '" + TargetFolder + "'");

                } catch (Exception e) {
                    Logger.LogInfo ("Fetcher", "Failed to delete '" + TargetFolder + "'", e);
                }
            }
        }


        public override string Complete (StorageType selected_storage_type)
        {
            string identifier = base.Complete (selected_storage_type);
            string identifier_path = Path.Combine (TargetFolder, ".sparkleshare");

            File.WriteAllText (identifier_path, identifier);

            var svn_add    = new SubversionCommand (TargetFolder, "add .sparkleshare");
            var svn_commit = new SubversionCommand (TargetFolder, "commit -m \"Initial commit by SparkleShare\"");

            // We can't do the "commit --all" shortcut because it doesn't add untracked files
            svn_add.StartAndWaitForExit ();
            svn_commit.StartAndWaitForExit ();


            File.SetAttributes (identifier_path, FileAttributes.Hidden);
            return identifier;
        }


        public override void EnableFetchedRepoCrypto (string password)
        {
        }


        public override bool IsFetchedRepoPasswordCorrect (string password)
        {
            return true;
        }


        public override string FormatName ()
        {
            string name = Path.GetFileName (RemoteUrl.AbsolutePath);
            name = name.ReplaceUnderscoreWithSpace ();

            if (name.EndsWith (".svn"))
                name = name.Replace (".svn", "");

            return name;
        }


    }
}
