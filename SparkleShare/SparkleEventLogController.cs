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

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        public event Action ContentLoadingEvent = delegate { };

        public event UpdateContentEventEventHandler UpdateContentEvent = delegate { };
        public delegate void UpdateContentEventEventHandler (string html);

        public event UpdateChooserEventHandler UpdateChooserEvent = delegate { };
        public delegate void UpdateChooserEventHandler (string [] folders);

        public event UpdateSizeInfoEventHandler UpdateSizeInfoEvent = delegate { };
        public delegate void UpdateSizeInfoEventHandler (string size, string history_size);


        private string selected_folder;


        public bool WindowIsOpen { get; private set; }

        public string SelectedFolder {
            get {
                return this.selected_folder;
            }

            set {
                this.selected_folder = value;

                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");

                Stopwatch watch = new Stopwatch ();
                watch.Start ();

                new Thread (() => {
                    string html = HTML;
                    watch.Stop ();

                    // A short delay is less annoying than
                    // a flashing window
					int delay = 500;
					
                    if (watch.ElapsedMilliseconds < delay)
                        Thread.Sleep (delay - (int) watch.ElapsedMilliseconds);

                    UpdateContentEvent (html);
                    UpdateSizeInfoEvent (Size, HistorySize);

                }).Start ();
            }
        }

        public string HTML {
            get {
                List<SparkleChangeSet> change_sets = GetLog (this.selected_folder);

                string html = GetHTMLLog (change_sets);
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
                    if (this.selected_folder == null) {
                        size += repo.Size;

                    } else if (this.selected_folder.Equals (repo.Name)) {
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
                    if (this.selected_folder == null) {
                        size += repo.HistorySize;

                    } else if (this.selected_folder.Equals (repo.Name)) {
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
                if (!WindowIsOpen)
                    ContentLoadingEvent ();

                if (this.selected_folder == null) {
                    new Thread (() => {
                        UpdateChooserEvent (Folders);
                        UpdateContentEvent (HTML);

                    }).Start ();
                }

                WindowIsOpen = true;
                ShowWindowEvent ();
            };
			
            Program.Controller.OnIdle += delegate {
                UpdateContentEvent (HTML);
                UpdateSizeInfoEvent (Size, HistorySize);
            };
			
            Program.Controller.FolderListChanged += delegate {
                if (this.selected_folder != null && !Program.Controller.Folders.Contains (this.selected_folder))
                    this.selected_folder = null;

                UpdateChooserEvent (Folders);
                UpdateSizeInfoEvent (Size, HistorySize);
            };
        }


        public void WindowClosed ()
        {
            WindowIsOpen = false;
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


        private List<SparkleChangeSet> GetLog ()
        {
            List<SparkleChangeSet> list = new List<SparkleChangeSet> ();

            foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                List<SparkleChangeSet> change_sets = repo.ChangeSets;

                if (change_sets != null)
                    list.AddRange (change_sets);
                else
                    SparkleLogger.LogInfo ("Log", "Could not create log for " + repo.Name);
            }

            list.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            list.Reverse ();

            if (list.Count > 100)
                return list.GetRange (0, 100);
            else
                return list.GetRange (0, list.Count);
        }


        private List<SparkleChangeSet> GetLog (string name)
        {
            if (name == null)
                return GetLog ();

            foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                if (repo.Name.Equals (name))
                    return repo.ChangeSets;
            }

            return new List<SparkleChangeSet> ();
        }


        public string GetHTMLLog (List<SparkleChangeSet> change_sets)
        {
            List <ActivityDay> activity_days = new List <ActivityDay> ();

            change_sets.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            change_sets.Reverse ();

            if (change_sets.Count == 0)
                return "";

            foreach (SparkleChangeSet change_set in change_sets) {
                bool change_set_inserted = false;
                foreach (ActivityDay stored_activity_day in activity_days) {
                    if (stored_activity_day.Date.Year  == change_set.Timestamp.Year &&
                        stored_activity_day.Date.Month == change_set.Timestamp.Month &&
                        stored_activity_day.Date.Day   == change_set.Timestamp.Day) {

                        stored_activity_day.Add (change_set);

                        change_set_inserted = true;
                        break;
                    }
                }

                if (!change_set_inserted) {
                    ActivityDay activity_day = new ActivityDay (change_set.Timestamp);
                    activity_day.Add (change_set);
                    activity_days.Add (activity_day);
                }
            }

            string event_log_html   = Program.Controller.EventLogHTML;
            string day_entry_html   = Program.Controller.DayEntryHTML;
            string event_entry_html = Program.Controller.EventEntryHTML;
            string event_log        = "";

            foreach (ActivityDay activity_day in activity_days) {
                string event_entries = "";

                foreach (SparkleChangeSet change_set in activity_day) {
                    string event_entry = "<dl>";

                    foreach (SparkleChange change in change_set.Changes) {
                        if (change.Type != SparkleChangeType.Moved) {
                            event_entry += "<dd class='document " + change.Type.ToString ().ToLower () + "'>";
                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
                            event_entry += "</dd>";

                        } else {
                            event_entry += "<dd class='document moved'>";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
                            event_entry += "<br>";
                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.MovedToPath);
                            event_entry += "</dd>";
                        }
                    }

                    string change_set_avatar = Program.Controller.GetAvatar (change_set.User.Email, 48);

                    if (change_set_avatar != null) {
				       	change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
				
                    } else {
                        change_set_avatar = "file://<!-- $pixmaps-path -->/" +
                        	Program.Controller.AssignAvatar (change_set.User.Email);
                    }

                    event_entry += "</dl>";

                    string timestamp = change_set.Timestamp.ToString ("H:mm");

                    if (!change_set.FirstTimestamp.Equals (new DateTime ()) &&
                        !change_set.Timestamp.ToString ("H:mm").Equals (change_set.FirstTimestamp.ToString ("H:mm"))) {

                        timestamp = change_set.FirstTimestamp.ToString ("H:mm") + " – " + timestamp;
                    }

                    event_entries += event_entry_html.Replace ("<!-- $event-entry-content -->", event_entry)
                        .Replace ("<!-- $event-user-name -->", change_set.User.Name)
                        .Replace ("<!-- $event-avatar-url -->", change_set_avatar)
                        .Replace ("<!-- $event-folder -->", change_set.Folder.Name)
                        .Replace ("<!-- $event-url -->", change_set.RemoteUrl.ToString ())
                        .Replace ("<!-- $event-revision -->", change_set.Revision);
                }

                string day_entry   = "";
                DateTime today     = DateTime.Now;
                DateTime yesterday = DateTime.Now.AddDays (-1);

                if (today.Day   == activity_day.Date.Day &&
                    today.Month == activity_day.Date.Month &&
                    today.Year  == activity_day.Date.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='today' name='" +
                         activity_day.Date.ToString ("dddd, MMMM d") + "'>" + "Today" +
                        "</span>");

                } else if (yesterday.Day   == activity_day.Date.Day &&
                           yesterday.Month == activity_day.Date.Month &&
                           yesterday.Year  == activity_day.Date.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='yesterday' name='" + activity_day.Date.ToString ("dddd, MMMM d") + "'>" +
                        "Yesterday" +
                        "</span>");

                } else {
                    if (activity_day.Date.Year != DateTime.Now.Year) {
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.Date.ToString ("dddd, MMMM d, yyyy"));

                    } else {
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.Date.ToString ("dddd, MMMM d"));
                    }
                }

                event_log += day_entry.Replace ("<!-- $day-entry-content -->", event_entries);
            }

            int midnight = (int) (DateTime.Today.AddDays (1) - new DateTime (1970, 1, 1)).TotalSeconds;

            string html = event_log_html.Replace ("<!-- $event-log-content -->", event_log);
            html = html.Replace ("<!-- $midnight -->", midnight.ToString ());

            return html;
        }


        private string FormatBreadCrumbs (string path_root, string path)
        {
            path_root                = path_root.Replace ("/", Path.DirectorySeparatorChar.ToString ());
            path                     = path.Replace ("/", Path.DirectorySeparatorChar.ToString ());
            string new_path_root     = path_root;
            string [] crumbs         = path.Split (Path.DirectorySeparatorChar);
            string link              = "";
            bool previous_was_folder = false;

            int i = 0;
            foreach (string crumb in crumbs) {
                if (string.IsNullOrEmpty (crumb))
                    continue;

                string crumb_path = Path.Combine (new_path_root, crumb);

                if (Directory.Exists (crumb_path)) {
                    link += "<a href='" + crumb_path + "'>" + crumb + Path.DirectorySeparatorChar + "</a>";
                    previous_was_folder = true;

                } else if (File.Exists (crumb_path)) {
                    link += "<a href='" + crumb_path + "'>" + crumb + "</a>";
                    previous_was_folder = false;

                } else {
                    if (i > 0 && !previous_was_folder)
                        link += Path.DirectorySeparatorChar;

                    link += crumb;
                    previous_was_folder = false;
                }

                new_path_root = Path.Combine (new_path_root, crumb);
                i++;
            }

            return link;
        }


        // All change sets that happened on a day
        private class ActivityDay : List<SparkleChangeSet>
        {
            public DateTime Date;

            public ActivityDay (DateTime date_time)
            {
                Date = new DateTime (date_time.Year, date_time.Month, date_time.Day);
            }
        }
    }
}
