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
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public abstract class SparkleFetcherBase {

        public delegate void StartedEventHandler ();
        public delegate void FinishedEventHandler ();
        public delegate void FailedEventHandler ();

        public event StartedEventHandler Started;
        public event FinishedEventHandler Finished;
        public event FailedEventHandler Failed;

        protected string target_folder;
        protected string remote_url;
        private Thread thread;


        public SparkleFetcherBase (string remote_url, string target_folder)
        {
            this.target_folder = target_folder;
            this.remote_url    = remote_url;
        }


        // Clones the remote repository
        public void Start ()
        {
            SparkleHelpers.DebugInfo ("Fetcher", "[" + this.target_folder + "] Fetching folder...");

            if (Started != null)
                Started ();

            if (Directory.Exists (this.target_folder))
                Directory.Delete (this.target_folder, true);

            this.thread = new Thread (new ThreadStart (delegate {
                if (Fetch ()) {
                    SparkleHelpers.DebugInfo ("Fetcher", "[" + this.target_folder + "] Fetching finished");

                    if (Finished != null)
                        Finished ();
                } else {
                    SparkleHelpers.DebugInfo ("Fetcher", "[" + this.target_folder + "] Fetching failed");

                    if (Failed != null)
                        Failed ();
                }
            }));

            this.thread.Start ();
        }


        public void Dispose ()
        {
            this.thread.Abort ();
            this.thread.Join ();
        }


        public abstract bool Fetch ();
    }
}
