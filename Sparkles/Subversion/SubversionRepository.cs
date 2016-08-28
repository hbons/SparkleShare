//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//   Portions Copyright (C) 2016 Paul Hammant <paul@hammant.org>

//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as 
//   published by the Free Software Foundation, either version 3 of the 
//   License, or (at your option) any later version.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sparkles.Subversion {

    public class SubversionRepository : BaseRepository {

        SSHAuthenticationInfo auth_info;

        bool in_merge {
            get {
                string merge_file_path = Path.Combine (LocalPath, ".svn", "MERGE_HEAD");
                return File.Exists (merge_file_path);
            }
        }


        public SubversionRepository (string path, Configuration config, SSHAuthenticationInfo auth_info) : base (path, config)
        {
            this.auth_info = auth_info;
        }


        public override List<string> ExcludePaths {
            get {
                List<string> rules = new List<string> ();
                rules.Add (".svn");

                return rules;
            }
        }


        public override double Size {
            get {
                // TODO
                return 0;
            }
        }


        public override double HistorySize {
            get {
                // TODO
                return 0;
            }
        }


        public override string CurrentRevision {
            get {
                var svn = new SubversionCommand (LocalPath, "info --show-item revision");
                string output  = svn.StartAndReadStandardOutput ();

                if (svn.ExitCode == 0)
                    return output;

                return null;
            }
        }


        public override bool HasRemoteChanges {
            get {
                Logger.LogInfo ("Svn", Name + " | Checking for remote changes...");
                string current_revision = CurrentRevision;

                var svn = new SubversionCommand (LocalPath,
                    "info --show-item revision " + RemoteUrl, auth_info);

                string output = svn.StartAndReadStandardOutput ();

                if (svn.ExitCode != 0)
                    return false;

                string remote_revision = output;

                if (!remote_revision.Equals (current_revision)) {
                    return true;
                }

                Logger.LogInfo ("Svn", Name + " | No remote changes, local+remote: " + current_revision);
                return false;
            }
        }


        public override bool SyncUp ()
        {
            if (!Add ()) {
                Error = ErrorStatus.UnreadableFiles;
                return false;
            }

            string message = base.status_message.Replace ("\"", "\\\"");

            if (string.IsNullOrEmpty (message))
                message = FormatCommitMessage ();

            var svn_commit = new SubversionCommand (LocalPath, string.Format ("commit -m=\"" + message + "\" ", RemoteUrl), auth_info);
            svn_commit.StartInfo.RedirectStandardError = true;
            svn_commit.Start ();

            if (!ReadStream (svn_commit))
                return false;

            svn_commit.WaitForExit ();

            if (svn_commit.ExitCode == 0)
                return true;

            Error = ErrorStatus.HostUnreachable;
            return false;
        }


        public override bool SyncDown ()
        {

            var svn_up = new SubversionCommand (LocalPath, "up", auth_info);

            svn_up.StartInfo.RedirectStandardError = true;
            svn_up.Start ();

            if (!ReadStream (svn_up))
                return false;

            svn_up.WaitForExit ();

            if (svn_up.ExitCode != 0) {
                Error = ErrorStatus.HostUnreachable;
                return false;
            }

            return true;
        }


        bool ReadStream (SubversionCommand command)
        {

            StreamReader output_stream = command.StandardError;

            string information = "";

            while (!output_stream.EndOfStream) {
                string line = output_stream.ReadLine ();

                OnProgressChanged (0, 0, information);
            }

            return true;
        }


        public override bool HasLocalChanges {
            get {
                PrepareDirectories (LocalPath);

                var svn = new SubversionCommand (LocalPath, "status");
                string output  = svn.StartAndReadStandardOutput ();

                return !string.IsNullOrEmpty (output);
            }
        }


        public override bool HasUnsyncedChanges {
            // TODO
            get {
                return false;
            }
            set {
            }
        }

        // Stages the made changes
        bool Add ()
        {

            var svn = new SubversionCommand (LocalPath, "status " + LocalPath);
            string output = svn.StartAndReadStandardOutput ();
            int count = 0;
            using (StringReader reader = new StringReader (output)) {
                string line;
                while ((line = reader.ReadLine ()) != null) {
                    if (line.StartsWith ("?      ")) {
                        new SubversionCommand (LocalPath, "add " + line.Substring(7)).Start();
                        count++;
                    }
                }
            }

            return count > 0;
        }


        public override void RestoreFile (string path, string revision, string target_file_path)
        {
            if (path == null)
                throw new ArgumentNullException ("path");

            if (revision == null)
                throw new ArgumentNullException ("revision");

            Logger.LogInfo ("Svn", Name + " | Restoring \"" + path + "\" (revision " + revision + ")");

            // Restore the older file...
            var svn = new SubversionCommand (LocalPath, "checkout -r" + revision + " \"" + path + "\"");
            svn.StartAndWaitForExit ();

            string local_file_path = Path.Combine (LocalPath, path);

            // ...move it...
            try {
                File.Move (local_file_path, target_file_path);
            
            } catch {
                Logger.LogInfo ("Svn",
                    Name + " | Could not move \"" + local_file_path + "\" to \"" + target_file_path + "\"");
            }

            // ...and restore the most recent revision
            svn = new SubversionCommand (LocalPath, "checkout -r" + CurrentRevision + " \"" + path + "\"");
            svn.StartAndWaitForExit ();


            if (target_file_path.StartsWith (LocalPath))
                new Thread (() => OnFileActivity (null)).Start ();
        }


        public override List<Change> UnsyncedChanges {
          get {
              return ParseStatus ();
            }
        }


        public override List<ChangeSet> GetChangeSets ()
        {
            return GetChangeSetsInternal (null);
        }

        public override List<ChangeSet> GetChangeSets (string path)
        {
            return GetChangeSetsInternal (path);
        }   

        List<ChangeSet> GetChangeSetsInternal (string path)
        {
            var change_sets = new List <ChangeSet> ();
            SubversionCommand svn;

            svn = new SubversionCommand (LocalPath, "log -l 100");

            string output = svn.StartAndReadStandardOutput ();

            string [] lines      = output.Split ("\n".ToCharArray ());
            List<string> entries = new List <string> ();

            // Split up commit entries
            int line_number = 0;
            string entry = "";

            while (lines.Length <= line_number) {
                if (lines [line_number].StartsWith ("-------")) {
                    if (lines [line_number + 1].StartsWith ("r")) {
                        entry = lines [line_number + 1];
                        entry = entry + " <###> " + lines [line_number + 3];
                        line_number += 3;
                        entries.Add (entry);
                    }
                    line_number += 1;
                }
            }

            // Parse commit entries
            foreach (string log_entry in entries) {
                Match match = this.log_regex.Match (log_entry);

                if (!match.Success) {
                    match = this.merge_regex.Match (log_entry);

                    if (!match.Success)
                        continue;
                }

                ChangeSet change_set = new ChangeSet ();

                change_set.Folder    = new SparkleFolder (Name);
                change_set.Revision  = match.Groups [1].Value;
                change_set.User      = new User (match.Groups [2].Value, match.Groups [3].Value);
                change_set.RemoteUrl = RemoteUrl;

                change_set.Timestamp = new DateTime (int.Parse (match.Groups [4].Value),
                    int.Parse (match.Groups [5].Value), int.Parse (match.Groups [6].Value),
                    int.Parse (match.Groups [7].Value), int.Parse (match.Groups [8].Value),
                    int.Parse (match.Groups [9].Value));

                string time_zone     = match.Groups [10].Value;
                int our_offset       = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
                int their_offset     = int.Parse (time_zone.Substring (0, 3));
                change_set.Timestamp = change_set.Timestamp.AddHours (their_offset * -1);
                change_set.Timestamp = change_set.Timestamp.AddHours (our_offset);

                string [] entry_lines = log_entry.Split ("\n".ToCharArray ());

                // Parse file list. Lines containing file changes start with ":"
                foreach (string entry_line in entry_lines) {
                    // Skip lines containing backspace characters
                    if (!entry_line.StartsWith (":") || entry_line.Contains ("\\177"))
                        continue;

                    string file_path = entry_line.Substring (39);

                    if (file_path.Equals (".sparkleshare"))
                        continue;

                    string type_letter    = entry_line [37].ToString ();
                    bool change_is_folder = false;

                    if (file_path.EndsWith (".empty")) { 
                        file_path        = file_path.Substring (0, file_path.Length - ".empty".Length);
                        change_is_folder = true;
                    }

                    try {
                        file_path = EnsureSpecialCharacters (file_path);
                        
                    } catch (Exception e) {
                        Logger.LogInfo ("Local", "Error parsing file name '" + file_path + "'", e);
                        continue;
                    }

                    file_path = file_path.Replace ("\\\"", "\"");

                    Change change = new Change () {
                        Path      = file_path,
                        IsFolder  = change_is_folder,
                        Timestamp = change_set.Timestamp,
                        Type      = ChangeType.Added
                    };

                    if (type_letter.Equals ("R")) {
                        int tab_pos         = entry_line.LastIndexOf ("\t");
                        file_path           = entry_line.Substring (42, tab_pos - 42);
                        string to_file_path = entry_line.Substring (tab_pos + 1);

                        try {
                            file_path = EnsureSpecialCharacters (file_path);
                            
                        } catch (Exception e) {
                            Logger.LogInfo ("Local", "Error parsing file name '" + file_path + "'", e);
                            continue;
                        }

                        try {
                            to_file_path = EnsureSpecialCharacters (to_file_path);

                        } catch (Exception e) {
                            Logger.LogInfo ("Local", "Error parsing file name '" + to_file_path + "'", e);
                            continue;
                        }

                        file_path    = file_path.Replace ("\\\"", "\"");
                        to_file_path = to_file_path.Replace ("\\\"", "\"");

                        if (file_path.EndsWith (".empty")) {
                            file_path = file_path.Substring (0, file_path.Length - 6);
                            change_is_folder = true;
                        }

                        if (to_file_path.EndsWith (".empty")) {
                            to_file_path = to_file_path.Substring (0, to_file_path.Length - 6);
                            change_is_folder = true;
                        }
                               
                        change.Path        = file_path;
                        change.MovedToPath = to_file_path;
                        change.Type        = ChangeType.Moved;

                    } else if (type_letter.Equals ("M")) {
                        change.Type = ChangeType.Edited;

                    } else if (type_letter.Equals ("D")) {
                        change.Type = ChangeType.Deleted;
                    }

                    change_set.Changes.Add (change);
                }

                // Group commits per user, per day
                if (change_sets.Count > 0 && path == null) {
                    ChangeSet last_change_set = change_sets [change_sets.Count - 1];

                    if (change_set.Timestamp.Year  == last_change_set.Timestamp.Year &&
                        change_set.Timestamp.Month == last_change_set.Timestamp.Month &&
                        change_set.Timestamp.Day   == last_change_set.Timestamp.Day &&
                        change_set.User.Name.Equals (last_change_set.User.Name)) {

                        last_change_set.Changes.AddRange (change_set.Changes);

                        if (DateTime.Compare (last_change_set.Timestamp, change_set.Timestamp) < 1) {
                            last_change_set.FirstTimestamp = last_change_set.Timestamp;
                            last_change_set.Timestamp      = change_set.Timestamp;
                            last_change_set.Revision       = change_set.Revision;

                        } else {
                            last_change_set.FirstTimestamp = change_set.Timestamp;
                        }

                    } else {
                        change_sets.Add (change_set);
                    }

                } else {
                    // Don't show removals or moves in the revision list of a file
                    if (path != null) {
                        List<Change> changes_to_skip = new List<Change> ();

                        foreach (Change change in change_set.Changes) {
                            if ((change.Type == ChangeType.Deleted || change.Type == ChangeType.Moved)
                                && change.Path.Equals (path)) {

                                changes_to_skip.Add (change);
                            }
                        }

                        foreach (Change change_to_skip in changes_to_skip)
                            change_set.Changes.Remove (change_to_skip);
                    }
                                    
                    change_sets.Add (change_set);
                }
            }

            return change_sets;
        }


        string EnsureSpecialCharacters (string path)
        {
            // The path is quoted if it contains special characters
            if (path.StartsWith ("\""))
                path = ResolveSpecialChars (path.Substring (1, path.Length - 2));

            return path;
        }


        string ResolveSpecialChars (string s)
        {
            StringBuilder builder = new StringBuilder (s.Length);
            List<byte> codes      = new List<byte> ();

            for (int i = 0; i < s.Length; i++) {
                while (s [i] == '\\' &&
                    s.Length - i > 3 &&
                    char.IsNumber (s [i + 1]) &&
                    char.IsNumber (s [i + 2]) &&
                    char.IsNumber (s [i + 3])) {

                    codes.Add (Convert.ToByte (s.Substring (i + 1, 3), 8));
                    i += 4;
                }

                if (codes.Count > 0) {
                    builder.Append (Encoding.UTF8.GetString (codes.ToArray ()));
                    codes.Clear ();
                }

                builder.Append (s [i]);
            }

            return builder.ToString ();
        }


        // Git doesn't track empty directories, so this method
        // fills them all with a hidden empty file.
        //
        // It also prevents git repositories from becoming
        // git submodules by renaming the .git/HEAD file
        void PrepareDirectories (string path)
        {
            try {
                foreach (string child_path in Directory.GetDirectories (path)) {
                    if (IsSymlink (child_path))
                        continue;

                    if (child_path.EndsWith (".svn")) {
                        if (child_path.Equals (Path.Combine (LocalPath, ".svn")))
                            continue;

                        string HEAD_file_path = Path.Combine (child_path, "HEAD");
    
                        if (File.Exists (HEAD_file_path)) {
                            File.Move (HEAD_file_path, HEAD_file_path + ".backup");
                            Logger.LogInfo ("Svn", Name + " | Renamed " + HEAD_file_path);
                        }
    
                        continue;
                    }
    
                    PrepareDirectories (child_path);
                }
    
                if (Directory.GetFiles (path).Length == 0 &&
                    Directory.GetDirectories (path).Length == 0 &&
                    !path.Equals (LocalPath)) {

                    if (!File.Exists (Path.Combine (path, ".empty"))) {
                        try {
                            File.WriteAllText (Path.Combine (path, ".empty"), "I'm a folder!");
                            File.SetAttributes (Path.Combine (path, ".empty"), FileAttributes.Hidden);

                        } catch {
                            Logger.LogInfo ("Svn", Name + " | Failed adding empty folder " + path);
                        }
                    }
                }

            } catch (IOException e) {
                Logger.LogInfo ("Svn", "Failed preparing directory", e);
            }
        }



        List<Change> ParseStatus ()
        {
            List<Change> changes = new List<Change> ();

            var svn_status = new SubversionCommand (LocalPath, "status");
            svn_status.Start ();
            
            while (!svn_status.StandardOutput.EndOfStream) {
                string line = svn_status.StandardOutput.ReadLine ();

                line        = line.Trim ();
                
                if (line.EndsWith (".empty") || line.EndsWith (".empty\""))
                    line = line.Replace (".empty", "");

                Change change;
                
                if (line.StartsWith ("R")) {
                    string path = line.Substring (3, line.IndexOf (" -> ") - 3).Trim ("\" ".ToCharArray ());
                    string moved_to_path = line.Substring (line.IndexOf (" -> ") + 4).Trim ("\" ".ToCharArray ());
                    
                    change = new Change () {
                        Type = ChangeType.Moved,
                        Path = EnsureSpecialCharacters (path),
                        MovedToPath = EnsureSpecialCharacters (moved_to_path)
                    };
                    
                } else {
                    string path = line.Substring (2).Trim ("\" ".ToCharArray ());
                    change = new Change () { Path = EnsureSpecialCharacters (path) };
                    change.Type = ChangeType.Added;

                    if (line.StartsWith ("M")) {
                        change.Type = ChangeType.Edited;
                        
                    } else if (line.StartsWith ("D")) {
                        change.Type = ChangeType.Deleted;
                    }
                }

                changes.Add (change);
            }
            
            svn_status.StandardOutput.ReadToEnd ();
            svn_status.WaitForExit ();

            return changes;
        }


        // Creates a pretty commit message based on what has changed
        string FormatCommitMessage ()
        {
            string message = "";

            foreach (Change change in ParseStatus ()) {
                if (change.Type == ChangeType.Moved) {
                    message +=  "< ‘" + EnsureSpecialCharacters (change.Path) + "’\n";
                    message +=  "> ‘" + EnsureSpecialCharacters (change.MovedToPath) + "’\n";

                } else {
                    switch (change.Type) {
                    case ChangeType.Edited:
                        message += "/";
                        break;
                    case ChangeType.Deleted:
                        message += "-";
                        break;
                    case ChangeType.Added:
                        message += "+";
                        break;
                    }

                    message += " ‘" + change.Path + "’\n";
                }
            }

            if (string.IsNullOrWhiteSpace (message))
                return null;
            else
                return message;
        }


        // Recursively gets a folder's size in bytes
        long CalculateSizes (DirectoryInfo parent)
        {
            long size = 0;

            try {
                foreach (DirectoryInfo directory in parent.GetDirectories ()) {
                    if (directory.FullName.IsSymlink () ||
                        directory.Name.Equals (".svn")) {

                        continue;
                    }

                    size += CalculateSizes (directory);
                }

            } catch (Exception e) {
                Logger.LogInfo ("Local", "Error calculating directory size", e);
            }

            try {
                foreach (FileInfo file in parent.GetFiles ()) {
                    if (file.FullName.IsSymlink ())
                        continue;
                    
                    if (file.Name.Equals (".empty"))
                        File.SetAttributes (file.FullName, FileAttributes.Hidden);
                    else
                        size += file.Length;
                }
                
            } catch (Exception e) {
                Logger.LogInfo ("Local", "Error calculating file size", e);
            }

            return size;
        }

        
        bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }


        Regex log_regex = new Regex (@"commit ([a-f0-9]{40})*\n" +
            "Author: (.+) <(.+)>\n" +
            "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
            "([0-9]{2}):([0-9]{2}):([0-9]{2}) (.[0-9]{4})\n" +
            "*", RegexOptions.Compiled);

        Regex merge_regex = new Regex (@"commit ([a-f0-9]{40})\n" +
            "Merge: [a-f0-9]{7} [a-f0-9]{7}\n" +
            "Author: (.+) <(.+)>\n" +
            "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
            "([0-9]{2}):([0-9]{2}):([0-9]{2}) (.[0-9]{4})\n" +
            "*", RegexOptions.Compiled);
    }
}
