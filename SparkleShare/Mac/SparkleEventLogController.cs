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
using System.Diagnostics;
using System.Threading;

using SparkleLib;

namespace SparkleShare {

    public class SparkleEventLogController {

        public event UpdateContentEventEventHandler UpdateContentEvent;
        public delegate void UpdateContentEventEventHandler (string html, bool silently);
        
        public event UpdateChooserEventHandler UpdateChooserEvent;
        public delegate void UpdateChooserEventHandler (string [] folders);

        public string SelectedFolder {
            get {
                return this.selected_folder;
            }

            set {
                this.selected_folder = value;

                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML, true);
            }
        }

        public string HTML {
            get {
                Stopwatch watch = new Stopwatch ();
                watch.Start ();

                List<SparkleChangeSet> change_sets = SparkleShare.Controller.GetLog (SelectedFolder);
                string html = SparkleShare.Controller.GetHTMLLog (change_sets);

                watch.Stop ();

                // A short delay is less annoying than
                // a flashing window
                if (watch.ElapsedMilliseconds < 500 /* && !silent */)
                    Thread.Sleep (500 - (int) watch.ElapsedMilliseconds);

                return html;
            }
        }

        public string [] Folders {
            get {
                return SparkleShare.Controller.Folders.ToArray ();
            }
        }


        private string selected_folder;


        public SparkleEventLogController ()
        {
            SparkleShare.Controller.AvatarFetched += delegate {
                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML, true);
            };

            SparkleShare.Controller.OnIdle += delegate {
                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML, true);
            };

            SparkleShare.Controller.FolderListChanged += delegate {
                if (this.selected_folder != null &&
                    !SparkleShare.Controller.Folders.Contains (this.selected_folder)) {

                    this.selected_folder = null;
                }

                if (UpdateChooserEvent != null)
                    UpdateChooserEvent (Folders);

                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML, true);
            };

            SparkleShare.Controller.NotificationRaised += delegate {
                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML, true);
            };
        }
    }
}
