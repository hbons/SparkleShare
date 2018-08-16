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

using Sparkles;

namespace SparkleShare {

    public partial class PageController {

        public event ProgressPageBarEventHandler ProgressPageBarEvent = delegate { };

        public delegate void ProgressPageBarEventHandler (bool? success,
            string status, string status_details,
            double progress, string progress_details);


        void ProgressPageFetchedDelegate (string remote_url)
        {
            // Create a local preset for succesfully added projects, so
            // so the user can easily use the same host again
            if (SelectedPresetIndex == 0) {
                Preset new_preset;
                Uri uri = new Uri (remote_url);

                try {
                    string address = remote_url.Replace (uri.AbsolutePath, "");
                    new_preset = Preset.Create (uri.Host, address, address, "", "", "/path/to/project");

                    if (new_preset != null) {
                        Presets.Insert (1, new_preset);
                        Logger.LogInfo ("Controller", "Added preset for " + uri.Host);
                    }

                } catch (Exception e) {
                    Logger.LogInfo ("Controller", "Failed adding preset for " + uri.Host, e);
                }
            }

            SparkleShare.Controller.FolderFetched -= ProgressPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= ProgressPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching -= ProgressPageFetchingDelegate;

            ProgressPageBarEvent (
                success: true,
                status: "Fetched all files from " + FetchAddress, status_details: null,
                progress: 100.0, progress_details: null);
        }


        void ProgressPageFetchErrorDelegate (string remote_url, string error, string error_details)
        {
            FetchAddress = new Uri (remote_url);

            ProgressPageBarEvent (
                success: false,
                status: error, status_details: error_details,
                progress: ProgressBarPercentage, progress_details: "Could not download files from " + remote_url);

            SparkleShare.Controller.FolderFetched -= ProgressPageFetchedDelegate;
            SparkleShare.Controller.FolderFetchError -= ProgressPageFetchErrorDelegate;
            SparkleShare.Controller.FolderFetching -= ProgressPageFetchingDelegate;
        }


        void ProgressPageFetchingDelegate (double percentage, double speed ,string information)
        {
            ProgressBarPercentage = percentage;

            if (speed > 0)
                information = speed.ToSize () + " – " + information;

            ProgressPageBarEvent (
                success: null,
                status: "Getting files", status_details: null,
                progress: ProgressBarPercentage, progress_details: information);
        }


        public void ErrorPageCompleted ()
        {
            if (PendingInvite != null)
                ChangePageEvent (PageType.Invite);
            else
                ChangePageEvent (PageType.Address);
        }


        public void ShowFilesClicked ()
        {
            string folder_name = Path.GetFileName (FetchAddress.AbsolutePath);
            folder_name = folder_name.ReplaceUnderscoreWithSpace ();

            SparkleShare.Controller.OpenSparkleShareFolder (folder_name);
            ProgressPageCompleted ();
        }
    }
}
