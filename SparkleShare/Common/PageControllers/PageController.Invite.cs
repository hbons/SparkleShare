//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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
using System.IO;
using System.Threading;

using Sparkles;

namespace SparkleShare {

    public partial class PageController {

        public void InvitePageCompleted ()
        {
            // TODO PreviousAddress = PendingInvite.Address;

            ChangePageEvent (PageType.Progress);

            new Thread (() => {
                if (!PendingInvite.Accept (SparkleShare.Controller.UserAuthenticationInfo.PublicKey)) {
                    FetchAddress = new Uri (PendingInvite.Address + PendingInvite.RemotePath.TrimStart ('/'));
                    // TODO ChangePageEvent (PageType.Error, new string [] { "error: Failed to upload the public key" });
                    return;
                }

                SparkleShare.Controller.FolderFetched += InvitePageFetchedDelegate;
                SparkleShare.Controller.FolderFetchError += InvitePageFetchErrorDelegate;
                SparkleShare.Controller.FolderFetching += ProgressPageFetchingDelegate;

                var info = new FetcherInfo (new Uri (Path.Combine (PendingInvite.Address, PendingInvite.RemotePath))) {
                    Fingerprint = PendingInvite.Fingerprint,
                    AnnouncementsUrl = PendingInvite.AnnouncementsUrl
                };

                SparkleShare.Controller.StartFetcher (info);

            }).Start ();
        }


        void InvitePageFetchedDelegate (string remote_url)
        {
            PendingInvite = null;

            // TODO success state + warning

            SparkleShare.Controller.FolderFetched -= ProgressPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= ProgressPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching -= ProgressPageFetchingDelegate;
        }


        void InvitePageFetchErrorDelegate (string remote_url, string error, string error_details)
        {
            FetchAddress = new Uri (remote_url);

            //TODO ChangePageEvent (PageType.Error, errors);

            SparkleShare.Controller.FolderFetched -= ProgressPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= ProgressPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching -= ProgressPageFetchingDelegate;
        }
    }
}
