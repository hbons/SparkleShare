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
using System.Globalization;
using System.IO;
using System.Text;
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
        
        public event UpdateChooserEnablementEventHandler UpdateChooserEnablementEvent = delegate { };
        public delegate void UpdateChooserEnablementEventHandler (bool enabled);
        
        public event UpdateSizeInfoEventHandler UpdateSizeInfoEvent = delegate { };
        public delegate void UpdateSizeInfoEventHandler (string size, string history_size);
        
        public event ShowSaveDialogEventHandler ShowSaveDialogEvent = delegate { };
        public delegate void ShowSaveDialogEventHandler (string file_name, string target_folder_path);


        private string selected_folder;
        private RevisionInfo restore_revision_info;
        private bool history_view_active;


        public bool WindowIsOpen { get; private set; }

        public string SelectedFolder {
            get {
                return this.selected_folder;
            }

            set {
                this.selected_folder = value;

                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");

                new Thread (() => {
                    SparkleDelay delay = new SparkleDelay ();
                    string html = HTML;
                    delay.Stop ();

					if (!string.IsNullOrEmpty (html))
                    	UpdateContentEvent (html);

                    UpdateSizeInfoEvent (Size, HistorySize);

                }).Start ();
            }
        }

        public string HTML {
            get {
                List<SparkleChangeSet> change_sets = GetLog (this.selected_folder);
                string html = GetHTMLLog (change_sets);

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
                            return repo.Size.ToSize ();
                    }
                }

                if (size == 0)
                    return "???";
                else
                    return size.ToSize ();
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
                            return repo.HistorySize.ToSize ();
                    }
                }

                if (size == 0)
                    return "???";
                else
                    return size.ToSize ();
            }
        }


        public SparkleEventLogController ()
        {
            Program.Controller.ShowEventLogWindowEvent += delegate {
                if (!WindowIsOpen) {
                    ContentLoadingEvent ();
                    UpdateSizeInfoEvent ("…", "…");

                    if (this.selected_folder == null) {
                        new Thread (() => {
                            SparkleDelay delay = new SparkleDelay ();
                            string html = HTML;
                            delay.Stop ();

                            UpdateChooserEvent (Folders);
                            UpdateChooserEnablementEvent (true);

							if (!string.IsNullOrEmpty (html))
                            	UpdateContentEvent (html);
                            
							UpdateSizeInfoEvent (Size, HistorySize);

                        }).Start ();
                    }
                }

                WindowIsOpen = true;
                ShowWindowEvent ();
            };
			
            Program.Controller.OnIdle += delegate {
                if (this.history_view_active)
                    return;

                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");

                SparkleDelay delay = new SparkleDelay ();
                string html = HTML;
                delay.Stop ();

				if (!string.IsNullOrEmpty (html))
                	UpdateContentEvent (html);

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
            if (url.StartsWith ("about:") || string.IsNullOrEmpty (url))
                return;
				
            url = url.Replace ("%20", " ");
            
            if (url.StartsWith ("http")) {
                Program.Controller.OpenWebsite (url);
            
            } else if (url.StartsWith ("restore://") && this.restore_revision_info == null) {
                Regex regex = new Regex ("restore://(.+)/([a-f0-9]+)/(.+)/(.{3} [0-9]+ [0-9]+h[0-9]+)/(.+)");
                Match match = regex.Match (url);
                
                if (match.Success) {
                    string author_name = match.Groups [3].Value;
                    string timestamp   = match.Groups [4].Value;

                    this.restore_revision_info = new RevisionInfo () {
                        Folder   = new SparkleFolder (match.Groups [1].Value),
                        Revision = match.Groups [2].Value,
                        FilePath = match.Groups [5].Value
                    };

                    string file_name = Path.GetFileNameWithoutExtension (this.restore_revision_info.FilePath) +
                        " (" + author_name + " " + timestamp + ")" + Path.GetExtension (this.restore_revision_info.FilePath);

                    string target_folder_path = Path.Combine (this.restore_revision_info.Folder.FullPath,
                        Path.GetDirectoryName (this.restore_revision_info.FilePath));

                    ShowSaveDialogEvent (file_name, target_folder_path);
                }
                
            } else if (url.StartsWith ("back://")) {
                this.history_view_active = false;
                SelectedFolder           = this.selected_folder; // TODO: Return to the same position on the page

                UpdateChooserEnablementEvent (true);

            } else if (url.StartsWith ("history://")) {
                this.history_view_active = true;

                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");
                UpdateChooserEnablementEvent (false);

                string folder    = url.Replace ("history://", "").Split ("/".ToCharArray ()) [0];
                string file_path = url.Replace ("history://" + folder + "/", "");

                byte [] file_path_bytes = Encoding.Default.GetBytes (file_path);
                file_path = Encoding.UTF8.GetString (file_path_bytes);

                foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                    if (!repo.Name.Equals (folder))
						continue;

			        new Thread (() => {
                        SparkleDelay delay = new SparkleDelay ();
                        List<SparkleChangeSet> change_sets = repo.GetChangeSets (file_path);
                        string html = GetHistoryHTMLLog (change_sets, file_path);
                        delay.Stop ();

						if (!string.IsNullOrEmpty (html))
	                    	UpdateContentEvent (html);

                	}).Start ();

                    break;
                }

            } else {
                Program.Controller.OpenFile (url);
            }   
        }


        public void SaveDialogCompleted (string target_file_path)
        {
            foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                if (repo.Name.Equals (this.restore_revision_info.Folder.Name)) {
                    repo.RestoreFile (this.restore_revision_info.FilePath,
                        this.restore_revision_info.Revision, target_file_path);

                    break;
                }
            }

            this.restore_revision_info = null;
            Program.Controller.OpenFolder (Path.GetDirectoryName (target_file_path));
        }


        public void SaveDialogCancelled ()
        {
            this.restore_revision_info = null;
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


        public string GetHistoryHTMLLog (List<SparkleChangeSet> change_sets, string file_path)
        {
            string html = "<div class='history-header'>" +
                "<a class='windows' href='back://'>&laquo; Back</a> &nbsp;|&nbsp; ";

            if (change_sets.Count > 1)
                html += "Revisions for <b>&ldquo;";
            else
                html += "No revisions for <b>&ldquo;";
            
            html += Path.GetFileName (file_path) + "&rdquo;</b>";
			html += "</div><div class='table-wrapper'><table>";
            
            int count = 0;
            foreach (SparkleChangeSet change_set in change_sets) {
                count++;

                if (count == 1)
                    continue;
                
                string change_set_avatar = Program.Controller.GetAvatar (change_set.User.Email, 24);
                
                if (change_set_avatar != null)
                    change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
                else
                    change_set_avatar = "file://<!-- $pixmaps-path -->/user-icon-default.png";
                
                html += "<tr>" +
                            "<td class='avatar'><img src='" + change_set_avatar + "'></td>" +
                            "<td class='name'>" + change_set.User.Name + "</td>" +
                            "<td class='date'>" + 
                                change_set.Timestamp.ToString ("d MMM yyyy", CultureInfo.InvariantCulture) + 
                            "</td>" +
                            "<td class='time'>" + change_set.Timestamp.ToString ("HH:mm") + "</td>" +
                            "<td class='restore'>" +
                                "<a href='restore://" + change_set.Folder.Name + "/" + 
                                change_set.Revision + "/" + change_set.User.Name + "/" + 
                                change_set.Timestamp.ToString ("MMM d H\\hmm", CultureInfo.InvariantCulture) + "/" +
                                file_path + "'>Restore&hellip;</a>" +
                            "</td>" +
                        "</tr>";
                
                count++;
            }

            html += "</table></div>";
            html = Program.Controller.EventLogHTML.Replace ("<!-- $event-log-content -->", html);

			return html.Replace ("<!-- $midnight -->", "100000000");
        }


        public string GetHTMLLog (List<SparkleChangeSet> change_sets)
        {
            if (change_sets.Count == 0)
                return "";
			
            List <ActivityDay> activity_days = new List <ActivityDay> ();

            change_sets.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            change_sets.Reverse ();

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
                            event_entry += "<dd class='" + change.Type.ToString ().ToLower () + "'>";

                            if (!change.IsFolder) {
                                event_entry += "<small><a href=\"history://" + change_set.Folder.Name + "/" + 
                                    change.Path + "\" title=\"View revisions\">" + change.Timestamp.ToString ("HH:mm") +
                                    "</a></small> &nbsp;";

                            } else {
                                event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") + "</small> &nbsp;";
                            }

                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
                            event_entry += "</dd>";

                        } else {
                            event_entry += "<dd class='moved'>";
                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
                            event_entry += "<br>";
                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.MovedToPath);
                            event_entry += "</dd>";
                        }
                    }

                    string change_set_avatar = Program.Controller.GetAvatar (change_set.User.Email, 48);

                    if (change_set_avatar != null)
				       	change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
                    else
                        change_set_avatar = "file://<!-- $pixmaps-path -->/user-icon-default.png";

                    event_entry += "</dl>";

                    string timestamp = change_set.Timestamp.ToString ("H:mm");

                    if (!change_set.FirstTimestamp.Equals (new DateTime ()) &&
                        !change_set.Timestamp.ToString ("H:mm").Equals (change_set.FirstTimestamp.ToString ("H:mm"))) {

                        timestamp = change_set.FirstTimestamp.ToString ("H:mm") + " – " + timestamp;
                    }

                    event_entries += event_entry_html.Replace ("<!-- $event-entry-content -->", event_entry)
                        .Replace ("<!-- $event-user-name -->", change_set.User.Name)
                        .Replace ("<!-- $event-user-email -->", change_set.User.Email)
                        .Replace ("<!-- $event-avatar-url -->", change_set_avatar)
                        .Replace ("<!-- $event-url -->", change_set.RemoteUrl.ToString ())
                        .Replace ("<!-- $event-revision -->", change_set.Revision);

                    if (this.selected_folder == null) 
                        event_entries = event_entries.Replace ("<!-- $event-folder -->", " @ " + change_set.Folder.Name);
                }

                string day_entry   = "";
                DateTime today     = DateTime.Now;
                DateTime yesterday = DateTime.Now.AddDays (-1);

                if (today.Day   == activity_day.Date.Day &&
                    today.Month == activity_day.Date.Month &&
                    today.Year  == activity_day.Date.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='today' name='" +
                         activity_day.Date.ToString ("dddd, MMMM d", CultureInfo.InvariantCulture) + "'>" + "Today" +
                        "</span>");

                } else if (yesterday.Day   == activity_day.Date.Day &&
                           yesterday.Month == activity_day.Date.Month &&
                           yesterday.Year  == activity_day.Date.Year) {

                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                        "<span id='yesterday' name='" + activity_day.Date.ToString ("dddd, MMMM d", CultureInfo.InvariantCulture) + "'>" +
                        "Yesterday" +
                        "</span>");

                } else {
                    if (activity_day.Date.Year != DateTime.Now.Year) {
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.Date.ToString ("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture));

                    } else {
                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
                            activity_day.Date.ToString ("dddd, MMMM d", CultureInfo.InvariantCulture));
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
            byte [] path_root_bytes = Encoding.Default.GetBytes (path_root);
            byte [] path_bytes      = Encoding.Default.GetBytes (path);
            path_root               = Encoding.UTF8.GetString (path_root_bytes);
            path                    = Encoding.UTF8.GetString (path_bytes);

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


        private class RevisionInfo {
            public SparkleFolder Folder;
            public string FilePath;
            public string Revision;
        }


        private class SparkleDelay : Stopwatch {

            public SparkleDelay () : base ()
            {
                Start ();
            }


            new public void Stop ()
            {
                base.Stop ();

                if (ElapsedMilliseconds < 500)
                    Thread.Sleep (500 - (int) ElapsedMilliseconds);
            }
        }
    }
}
