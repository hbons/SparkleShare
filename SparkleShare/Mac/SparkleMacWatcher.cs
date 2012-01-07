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
using System.IO;
using System.Threading;
using System.Timers;

namespace SparkleShare {

    public class SparkleMacWatcher {

        public delegate void ChangedEventHandler (string path);
        public event ChangedEventHandler Changed;

        private FileSystemInfo last_changed;
        private Thread thread;
        private int poll_count = 0;


        public SparkleMacWatcher (string path)
        {
            this.thread = new Thread (new ThreadStart (delegate {
                DateTime timestamp;
                DirectoryInfo parent = new DirectoryInfo (path);
                this.last_changed = new DirectoryInfo (path);

                while (true) {
                    timestamp = this.last_changed.LastWriteTime;
                    GetLastChange (parent);

                    if (DateTime.Compare (this.last_changed.LastWriteTime, timestamp) != 0) {
                        string relative_path = this.last_changed.FullName.Substring (path.Length + 1);

                        if (Changed != null)
                            Changed (relative_path);
                    }

                    Thread.Sleep (7500);
                    this.poll_count++;
                }
            }));

            this.thread.Start ();
        }


        private void GetLastChange (DirectoryInfo parent)
        {
            try {
                if (DateTime.Compare (parent.LastWriteTime, this.last_changed.LastWriteTime) > 0)
                    this.last_changed = parent;

                foreach (DirectoryInfo info in parent.GetDirectories ()) {
                    if (!info.FullName.Contains ("/.")) {
                        if (DateTime.Compare (info.LastWriteTime, this.last_changed.LastWriteTime) > 0)
                            this.last_changed = info;
    
                        GetLastChange (info);
                    }
                }

                if (this.poll_count >= 8) {
                    foreach (FileInfo info in parent.GetFiles ()) {
                        if (!info.FullName.Contains ("/.")) {
                            if (DateTime.Compare (info.LastWriteTime, this.last_changed.LastWriteTime) > 0)
                                this.last_changed = info;
                        }
                    }

                    this.poll_count = 0;
                }

            } catch (Exception) {
                // Don't care...
            }
        }


        public void Dispose ()
        {
            this.thread.Join ();
            this.thread.Abort ();
        }
    }
}
