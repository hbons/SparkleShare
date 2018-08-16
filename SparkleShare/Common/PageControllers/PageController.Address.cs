
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
using System.Threading;

using Sparkles;

namespace SparkleShare {

    public partial class PageController {

        public event AddressPagePublicKeyEventHandler AddressPagePublicKeyEvent = delegate { };
        public delegate void AddressPagePublicKeyEventHandler (bool has_key, string auth_status, string key_entry_hint);


        public void CheckAddressPage (string address, string remote_path)
        {
            address = address.Trim ();
            remote_path = remote_path.Trim ();

            bool fields_valid = (!string.IsNullOrEmpty (address) &&
                !string.IsNullOrEmpty (remote_path) && !remote_path.Contains ("\""));

            PageCanContinueEvent (PageType.Address, fields_valid);
        }


        public void AddressPageCompleted (string address, string remote_path)
        {
            ProgressBarPercentage = 1.0;
            ChangePageEvent (PageType.Progress);

            address = Uri.EscapeUriString (address.Trim ());
            remote_path = remote_path.Trim ();

            if (SelectedPreset.PathUsesLowerCase)
                remote_path = remote_path.ToLower ();

            FetchAddress = new Uri (address + remote_path);

            SparkleShare.Controller.FolderFetched += ProgressPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError += ProgressPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching += ProgressPageFetchingDelegate;

            var info = new FetcherInfo (FetchAddress) {
                Backend = SelectedPreset.Backend,
                Fingerprint = SelectedPreset.Fingerprint,
                AnnouncementsUrl = SelectedPreset.AnnouncementsUrl
            };

            new Thread (() => { SparkleShare.Controller.StartFetcher (info); }).Start ();
        }


        public void CopyToClipboardClicked ()
        {
            SparkleShare.Controller.CopyToClipboard (SparkleShare.Controller.UserAuthenticationInfo.PublicKey);
        }
    }
}
