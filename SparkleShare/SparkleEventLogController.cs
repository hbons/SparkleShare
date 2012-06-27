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

        public event ShowWindowEventHandler ShowWindowEvent;
        public delegate void ShowWindowEventHandler ();

        public event HideWindowEventHandler HideWindowEvent;
        public delegate void HideWindowEventHandler ();

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

                if (UpdateSizeInfoEvent != null)
                    UpdateSizeInfoEvent ("…", "…");

                Stopwatch watch = new Stopwatch ();
                watch.Start ();

                Thread thread = new Thread (new ThreadStart (delegate {
                    string html = HTML;
                    watch.Stop ();

                    // A short delay is less annoying than
                    // a flashing window
					int delay = 500;
					
                    if (watch.ElapsedMilliseconds < delay)
                        Thread.Sleep (delay - (int) watch.ElapsedMilliseconds);

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

                string html = Program.Controller.GetHTMLLog (change_sets);

                if (UpdateSizeInfoEvent != null)
                    UpdateSizeInfoEvent (Size, HistorySize);

                return html;
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
                    else if (this.selected_folder.Equals (repo.Name)) {
                        if (repo.Size == 0)
                            return "???";
                        else
                            return Program.Controller.FormatSize (repo.Size);
                    }
                }

                if (size == 0)
                    return "???";
                else
                    return Program.Controller.FormatSize (size);
            }
        }

        public string HistorySize {
            get {
                double size = 0;

                foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                    if (this.selected_folder == null)
                        size += repo.HistorySize;
                    else if (this.selected_folder.Equals (repo.Name)) {
                        if (repo.HistorySize == 0)
                            return "???";
                        else
                            return Program.Controller.FormatSize (repo.HistorySize);
                    }
                }

                if (size == 0)
                    return "???";
                else
                    return Program.Controller.FormatSize (size);
            }
        }


        public SparkleEventLogController ()
        {
            Program.Controller.ShowEventLogWindowEvent += delegate {
                if (this.selected_folder == null) {
                    new Thread (
                        new ThreadStart (delegate {
                            if (UpdateChooserEvent != null)
                                UpdateChooserEvent (Folders);

                            if (UpdateContentEvent != null)
                                UpdateContentEvent (HTML);
                        })
                    ).Start ();
                }

                if (ShowWindowEvent != null)
                    ShowWindowEvent ();
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

                if (UpdateSizeInfoEvent != null)
                    UpdateSizeInfoEvent (Size, HistorySize);
            };
        }


        public void WindowClosed ()
        {
            if (HideWindowEvent != null)
                HideWindowEvent ();
			
			this.selected_folder = null;
        }


        public void LinkClicked (string url)
        {
            url = url.Replace ("%20", " ");
        
            if (url.StartsWith (Path.VolumeSeparatorChar.ToString ()) ||
			    url.Substring (1, 1).Equals (":")) {

                Program.Controller.OpenFile (url);
            }
        }
    }
}
