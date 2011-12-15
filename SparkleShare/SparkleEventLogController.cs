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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

using SparkleLib;

namespace SparkleShare {

    public class SparkleEventLogController {

        public event UpdateContentEventEventHandler UpdateContentEvent;
        public delegate void UpdateContentEventEventHandler (string html);

        public event UpdateChooserEventHandler UpdateChooserEvent;
        public delegate void UpdateChooserEventHandler (string [] folders);

        public event UpdateSizeInfoEventHandler UpdateSizeInfoEvent;
        public delegate void UpdateSizeInfoEventHandler (string size, string history_size);

        public event ContentLoadingEventHandler ContentLoadingEvent;
        public delegate void ContentLoadingEventHandler ();

        private string selected_folder;


        public string SelectedFolder {
            get {
                return this.selected_folder;
            }

            set {
                this.selected_folder = value;

                if (ContentLoadingEvent != null)
                    ContentLoadingEvent ();

                Stopwatch watch = new Stopwatch ();
                watch.Start ();

                Thread thread = new Thread (new ThreadStart (delegate {
                    string html = HTML;
                    watch.Stop ();

                    // A short delay is less annoying than
                    // a flashing window
                    if (watch.ElapsedMilliseconds < 500)
                        Thread.Sleep (500 - (int) watch.ElapsedMilliseconds);

                    if (UpdateContentEvent != null)
                        UpdateContentEvent (html);
    
                    if (UpdateSizeInfoEvent != null)
                        UpdateSizeInfoEvent (Size, HistorySize);
                }));

                thread.Start ();
            }
        }

        public string HTML {
            get {
                List<SparkleChangeSet> change_sets = Program.Controller.GetLog (this.selected_folder);
                return Program.Controller.GetHTMLLog (change_sets);
            }
        }

        public string [] Folders {
            get {
                return Program.Controller.Folders.ToArray ();
            }
        }

        public string Size {
            get {
                double size = 0;

                foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                    if (this.selected_folder == null)
                        size += repo.Size;
                    else if (this.selected_folder.Equals (repo.Name))
                        return Program.Controller.FormatSize (repo.Size);
                }

                return Program.Controller.FormatSize (size);
            }
        }

        public string HistorySize {
            get {
                double size = 0;

                foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                    if (this.selected_folder == null)
                        size += repo.HistorySize;
                    else if (this.selected_folder.Equals (repo.Name))
                        return Program.Controller.FormatSize (repo.HistorySize);
                }

                return Program.Controller.FormatSize (size);
            }
        }


        public SparkleEventLogController ()
        {
            Program.Controller.AvatarFetched += delegate {
                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML);
            };

            Program.Controller.OnIdle += delegate {
                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML);

                if (UpdateSizeInfoEvent != null)
                    UpdateSizeInfoEvent (Size, HistorySize);
            };

            Program.Controller.FolderListChanged += delegate {
                if (this.selected_folder != null &&
                    !Program.Controller.Folders.Contains (this.selected_folder)) {

                    this.selected_folder = null;
                }

                if (UpdateChooserEvent != null)
                    UpdateChooserEvent (Folders);

                if (UpdateContentEvent != null)
                    UpdateContentEvent (HTML);

                if (UpdateSizeInfoEvent != null)
                    UpdateSizeInfoEvent (Size, HistorySize);
            };
        }


        public static void LinkClicked (string url)
        {
            if (url.StartsWith (Path.VolumeSeparatorChar.ToString ())) {
                Program.Controller.OpenFile (url);

            } else {
                Regex regex = new Regex (@"(.+)~(.+)~(.+)");
                Match match = regex.Match (url);

                if (match.Success) {
                    string folder_name = match.Groups [1].Value;
                    string revision    = match.Groups [2].Value;
                    string note        = match.Groups [3].Value;

                    Thread thread = new Thread (new ThreadStart (delegate {
                        Program.Controller.AddNoteToFolder (folder_name, revision, note);
                    }));

                    thread.Start ();
                }
            }
        }
    }
}
