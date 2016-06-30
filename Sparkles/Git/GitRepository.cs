//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
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

namespace Sparkles.Git {

    public class GitRepository : BaseRepository {

        SSHAuthenticationInfo auth_info;
        bool user_is_set;


        string cached_branch;

        string branch {
            get {
                if (!string.IsNullOrEmpty (this.cached_branch)) 
                    return this.cached_branch;

                var git = new GitCommand (LocalPath, "config core.ignorecase true");
                git.StartAndWaitForExit ();

                // TODO: ugly
                while (this.in_merge && HasLocalChanges) {
                    try {
                        ResolveConflict ();
                        
                    } catch (IOException e) {
                        Logger.LogInfo ("Git", Name + " | Failed to resolve conflict, trying again...", e);
                    }
                }

                git = new GitCommand (LocalPath, "config core.ignorecase false");
                git.StartAndWaitForExit ();

                git = new GitCommand (LocalPath, "rev-parse --abbrev-ref HEAD");
                this.cached_branch = git.StartAndReadStandardOutput ();

                return this.cached_branch;
            }
        }


        bool in_merge {
            get {
                string merge_file_path = Path.Combine (LocalPath, ".git", "MERGE_HEAD");
                return File.Exists (merge_file_path);
            }
        }


        public GitRepository (string path, Configuration config, SSHAuthenticationInfo auth_info) : base (path, config)
        {
            this.auth_info = auth_info;

            var git_config = new GitCommand (LocalPath, "config core.ignorecase false");
            git_config.StartAndWaitForExit ();

            git_config = new GitCommand (LocalPath, "config remote.origin.url \"" + RemoteUrl + "\"");
            git_config.StartAndWaitForExit ();
        }


        public override List<string> ExcludePaths {
            get {
                List<string> rules = new List<string> ();
                rules.Add (".git");

                return rules;
            }
        }


        public override double Size {
            get {
                string file_path = Path.Combine (LocalPath, ".git", "info", "size");

                try {
                    string size = File.ReadAllText (file_path);
                    return double.Parse (size);

                } catch {
                    return 0;
                }
            }
        }


        public override double HistorySize {
            get {
                string file_path = Path.Combine (LocalPath, ".git", "info", "history_size");

                try {
                    string size = File.ReadAllText (file_path);
                    return double.Parse (size);

                } catch {
                    return 0;
                }
            }
        }


        void UpdateSizes ()
        {
            double size         = CalculateSizes (new DirectoryInfo (LocalPath));
            double history_size = CalculateSizes (new DirectoryInfo (Path.Combine (LocalPath, ".git")));

            string size_file_path = Path.Combine (LocalPath, ".git", "info", "size");
            string history_size_file_path = Path.Combine (LocalPath, ".git", "info", "history_size");

            File.WriteAllText (size_file_path, size.ToString ());
            File.WriteAllText (history_size_file_path, history_size.ToString ());
        }


        public override string CurrentRevision {
            get {
                var git = new GitCommand (LocalPath, "rev-parse HEAD");
                string output  = git.StartAndReadStandardOutput ();

                if (git.ExitCode == 0)
                    return output;

                return null;
            }
        }


        public override bool HasRemoteChanges {
            get {
                Logger.LogInfo ("Git", Name + " | Checking for remote changes...");
                string current_revision = CurrentRevision;

                var git = new GitCommand (LocalPath,
                    "ls-remote --heads --exit-code origin " + this.branch, auth_info);

                string output = git.StartAndReadStandardOutput ();

                if (git.ExitCode != 0)
                    return false;

                string remote_revision = "" + output.Substring (0, 40);

                if (!remote_revision.Equals (current_revision)) {
                    git = new GitCommand (LocalPath, "merge-base " + remote_revision + " master");
                    git.StartAndWaitForExit ();

                    if (git.ExitCode != 0) {
                        Logger.LogInfo ("Git", Name + " | Remote changes found, local: " +
                            current_revision + ", remote: " + remote_revision);

                        Error = ErrorStatus.None;
                        return true;
                    
                    } else {
                        Logger.LogInfo ("Git", Name + " | Remote " + remote_revision + " is already in our history");
                        return false;
                    }
                }

                Logger.LogInfo ("Git", Name + " | No remote changes, local+remote: " + current_revision);
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

            if (message != null)
                Commit (message);

            string pre_push_hook_path = Path.Combine (LocalPath, ".git", "hooks", "pre-push");
            string pre_push_hook_content;

            // The pre-push hook may have been changed by Git LFS, overwrite it to use our own configuration
            if (InstallationInfo.OperatingSystem == OS.Mac) {
                pre_push_hook_content =
                    "#!/bin/sh" + Environment.NewLine +
                    "env GIT_SSH_COMMAND='" + GitCommand.FormatGitSSHCommand (auth_info) + "' " +
                    Path.Combine (Configuration.DefaultConfiguration.BinPath, "git-lfs") + " pre-push \"$@\"";

            } else {
                pre_push_hook_content =
                    "#!/bin/sh" + Environment.NewLine +
                    "env GIT_SSH_COMMAND='" + GitCommand.FormatGitSSHCommand (auth_info) + "' " +
                    "git-lfs pre-push \"$@\"";
            }

            Directory.CreateDirectory (Path.GetDirectoryName (pre_push_hook_path));
            File.WriteAllText (pre_push_hook_path, pre_push_hook_content);

            var git_push = new GitCommand (LocalPath, string.Format ("push --all --progress origin", RemoteUrl), auth_info);
            git_push.StartInfo.RedirectStandardError = true;
            git_push.Start ();

            if (!ReadStream (git_push))
                return false;

            git_push.WaitForExit ();

            UpdateSizes ();

            if (git_push.ExitCode == 0)
                return true;

            Error = ErrorStatus.HostUnreachable;
            return false;
        }


        public override bool SyncDown ()
        {
            string lfs_is_behind_file_path = Path.Combine (LocalPath, ".git", "lfs", "is_behind");

            if (StorageType == StorageType.LargeFiles)
                File.Create (lfs_is_behind_file_path);

            var git_fetch = new GitCommand (LocalPath, "fetch --progress origin " + branch, auth_info);

            git_fetch.StartInfo.RedirectStandardError = true;
            git_fetch.Start ();

            if (!ReadStream (git_fetch))
                return false;

            git_fetch.WaitForExit ();

            if (git_fetch.ExitCode != 0) {
                Error = ErrorStatus.HostUnreachable;
                return false;
            }

            if (Merge ()) {
                if (StorageType == StorageType.LargeFiles) {
                    // Pull LFS files manually to benefit from concurrency
                    var git_lfs_pull = new GitCommand (LocalPath, "lfs pull origin", auth_info);
                    git_lfs_pull.StartAndWaitForExit ();

                    if (git_lfs_pull.ExitCode != 0) {
                        Error = ErrorStatus.HostUnreachable;
                        return false;
                    }

                    if (File.Exists (lfs_is_behind_file_path))
                        File.Delete (lfs_is_behind_file_path);
                }

                UpdateSizes ();
                return true;
            }

            return false;
        }


        bool ReadStream (GitCommand command)
        {
            StreamReader output_stream = command.StandardError;

            if (StorageType == StorageType.LargeFiles)
                output_stream = command.StandardOutput;

            double percentage = 0;
            double speed = 0;
            string information = "";

            while (!output_stream.EndOfStream) {
                string line = output_stream.ReadLine ();
                ErrorStatus error = GitCommand.ParseProgress (line, out percentage, out speed, out information);

                if (error != ErrorStatus.None) {
                    Error = error;
                    information = line;

                    command.Kill ();
                    command.Dispose ();
                    Logger.LogInfo ("Git", Name + " | Error status changed to " + Error);

                    return false;
                }

                OnProgressChanged (percentage, speed, information);
            }

            return true;
        }


        public override bool HasLocalChanges {
            get {
                PrepareDirectories (LocalPath);

                var git = new GitCommand (LocalPath, "status --porcelain");
                string output  = git.StartAndReadStandardOutput ();

                return !string.IsNullOrEmpty (output);
            }
        }


        public override bool HasUnsyncedChanges {
            get {
                if (StorageType == StorageType.LargeFiles) {
                    string lfs_is_behind_file_path = Path.Combine (LocalPath, ".git", "lfs", "is_behind");

                    if (File.Exists (lfs_is_behind_file_path))
                        return true;
                }

                string unsynced_file_path =  Path.Combine (LocalPath, ".git", "has_unsynced_changes");
                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = Path.Combine (LocalPath, ".git", "has_unsynced_changes");

                if (value)
                    File.WriteAllText (unsynced_file_path, "");
                else
                    File.Delete (unsynced_file_path);
            }
        }


        // Stages the made changes
        bool Add ()
        {
            var git = new GitCommand (LocalPath, "add --all");
            git.StartAndWaitForExit ();

            return (git.ExitCode == 0);
        }


        // Commits the made changes
        void Commit (string message)
        {
            GitCommand git;

            if (!this.user_is_set) {
                git = new GitCommand (LocalPath, "config user.name \"" + base.local_config.User.Name + "\"");
                git.StartAndWaitForExit ();

                git = new GitCommand (LocalPath, "config user.email \"" + base.local_config.User.Email + "\"");
                git.StartAndWaitForExit ();

                this.user_is_set = true;
            }

            git = new GitCommand (LocalPath, "commit --all --message=\"" + message + "\" " +
                "--author=\"" + base.local_config.User.Name + " <" + base.local_config.User.Email + ">\"");

            git.StartAndReadStandardOutput ();
        }


        // Merges the fetched changes
        bool Merge ()
        {
            string message = FormatCommitMessage ();
            
            if (message != null) {
                Add ();
                Commit (message);
            }

            GitCommand git;

            // Stop if we're already in a merge because something went wrong
            if (this.in_merge) {
                 git = new GitCommand (LocalPath, "merge --abort");
                 git.StartAndWaitForExit ();
            
                 return false;
            }

            // Temporarily change the ignorecase setting to true to avoid
            // conflicts in file names due to letter case changes
            git = new GitCommand (LocalPath, "config core.ignorecase true");
            git.StartAndWaitForExit ();

            git = new GitCommand (LocalPath, "merge FETCH_HEAD");
            git.StartInfo.RedirectStandardOutput = false;

            string error_output = git.StartAndReadStandardError ();

            if (git.ExitCode != 0) {
                // Stop when we can't merge due to locked local files
                // error: cannot stat 'filename': Permission denied
                if (error_output.Contains ("error: cannot stat")) {
                    Error = ErrorStatus.UnreadableFiles;
                    Logger.LogInfo ("Git", Name + " | Error status changed to " + Error);

                    git = new GitCommand (LocalPath, "merge --abort");
                    git.StartAndWaitForExit ();

                    git = new GitCommand (LocalPath, "config core.ignorecase false");
                    git.StartAndWaitForExit ();

                    return false;
                
                } else {
                    Logger.LogInfo ("Git", error_output);
                    Logger.LogInfo ("Git", Name + " | Conflict detected, trying to get out...");
                    
                    while (this.in_merge && HasLocalChanges) {
                        try {
                            ResolveConflict ();

                        } catch (Exception e) {
                            Logger.LogInfo ("Git", Name + " | Failed to resolve conflict, trying again...", e);
                        }
                    }

                    Logger.LogInfo ("Git", Name + " | Conflict resolved");
                }
            }

            git = new GitCommand (LocalPath, "config core.ignorecase false");
            git.StartAndWaitForExit ();

            return true;
        }


        void ResolveConflict ()
        {
            // This is a list of conflict status codes that Git uses, their
            // meaning, and how SparkleShare should handle them.
            //
            // DD    unmerged, both deleted    -> Do nothing
            // AU    unmerged, added by us     -> Use server's, save ours as a timestamped copy
            // UD    unmerged, deleted by them -> Use ours
            // UA    unmerged, added by them   -> Use server's, save ours as a timestamped copy
            // DU    unmerged, deleted by us   -> Use server's
            // AA    unmerged, both added      -> Use server's, save ours as a timestamped copy
            // UU    unmerged, both modified   -> Use server's, save ours as a timestamped copy
            // ??    unmerged, new files       -> Stage the new files

            var git_status = new GitCommand (LocalPath, "status --porcelain");
            string output         = git_status.StartAndReadStandardOutput ();

            string [] lines = output.Split ("\n".ToCharArray ());
            bool trigger_conflict_event = false;

            foreach (string line in lines) {
                string conflicting_path = line.Substring (3);
                conflicting_path        = EnsureSpecialCharacters (conflicting_path);
                conflicting_path        = conflicting_path.Trim ("\"".ToCharArray ());

                // Remove possible rename indicators
                string [] separators = {" -> \"", " -> "};
                foreach (string separator in separators) {
                    if (conflicting_path.Contains (separator)) {
                        conflicting_path = conflicting_path.Substring (
                            conflicting_path.IndexOf (separator) + separator.Length);
                    }
                }

                Logger.LogInfo ("Git", Name + " | Conflict type: " + line);

                // Ignore conflicts in hidden files and use the local versions
                if (conflicting_path.EndsWith (".sparkleshare") || conflicting_path.EndsWith (".empty")) {
                    Logger.LogInfo ("Git", Name + " | Ignoring conflict in special file: " + conflicting_path);

                    // Recover local version
                    var git_ours = new GitCommand (LocalPath, "checkout --ours \"" + conflicting_path + "\"");
                    git_ours.StartAndWaitForExit ();

                    string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);

                    if (File.Exists (abs_conflicting_path))
                        File.SetAttributes (abs_conflicting_path, FileAttributes.Hidden);
            
                    continue;
                }

                Logger.LogInfo ("Git", Name + " | Resolving: " + conflicting_path);

                // Both the local and server version have been modified
                if (line.StartsWith ("UU") || line.StartsWith ("AA") ||
                    line.StartsWith ("AU") || line.StartsWith ("UA")) {

                    // Recover local version
                    var git_ours = new GitCommand (LocalPath, "checkout --ours \"" + conflicting_path + "\"");
                    git_ours.StartAndWaitForExit ();

                    // Append a timestamp to local version.
                    // Windows doesn't allow colons in the file name, so
                    // we use "h" between the hours and minutes instead.
                    string timestamp  = DateTime.Now.ToString ("MMM d H\\hmm");
                    string our_path = Path.GetFileNameWithoutExtension (conflicting_path) +
                        " (" + base.local_config.User.Name + ", " + timestamp + ")" + Path.GetExtension (conflicting_path);

                    string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
                    string abs_our_path         = Path.Combine (LocalPath, our_path);

                    if (File.Exists (abs_conflicting_path) && !File.Exists (abs_our_path))
                        File.Move (abs_conflicting_path, abs_our_path);

                    // Recover server version
                    var git_theirs = new GitCommand (LocalPath, "checkout --theirs \"" + conflicting_path + "\"");
                    git_theirs.StartAndWaitForExit ();

                    trigger_conflict_event = true;

            
                // The server version has been modified, but the local version was removed
                } else if (line.StartsWith ("DU")) {

                    // The modified local version is already in the checkout, so it just needs to be added.
                    // We need to specifically mention the file, so we can't reuse the Add () method
                    var git_add = new GitCommand (LocalPath, "add \"" + conflicting_path + "\"");
                    git_add.StartAndWaitForExit ();

                
                // The local version has been modified, but the server version was removed
                } else if (line.StartsWith ("UD")) {
                    
                    // Recover server version
                    var git_theirs = new GitCommand (LocalPath, "checkout --theirs \"" + conflicting_path + "\"");
                    git_theirs.StartAndWaitForExit ();

            
                // Server and local versions were removed
                } else if (line.StartsWith ("DD")) {
                    Logger.LogInfo ("Git", Name + " | No need to resolve: " + line);

                // New local files
                } else if (line.StartsWith ("??")) {
                    Logger.LogInfo ("Git", Name + " | Found new file, no need to resolve: " + line);
                
                } else {
                    Logger.LogInfo ("Git", Name + " | Don't know what to do with: " + line);
                }
            }

            Add ();

            var git = new GitCommand (LocalPath, "commit --message \"Conflict resolution by SparkleShare\"");
            git.StartInfo.RedirectStandardOutput = false;
            git.StartAndWaitForExit ();

            if (trigger_conflict_event)
                OnConflictResolved ();
        }


        public override void RestoreFile (string path, string revision, string target_file_path)
        {
            if (path == null)
                throw new ArgumentNullException ("path");

            if (revision == null)
                throw new ArgumentNullException ("revision");

            Logger.LogInfo ("Git", Name + " | Restoring \"" + path + "\" (revision " + revision + ")");

            // Restore the older file...
            var git = new GitCommand (LocalPath, "checkout " + revision + " \"" + path + "\"");
            git.StartAndWaitForExit ();

            string local_file_path = Path.Combine (LocalPath, path);

            // ...move it...
            try {
                File.Move (local_file_path, target_file_path);
            
            } catch {
                Logger.LogInfo ("Git",
                    Name + " | Could not move \"" + local_file_path + "\" to \"" + target_file_path + "\"");
            }

            // ...and restore the most recent revision
            git = new GitCommand (LocalPath, "checkout " + CurrentRevision + " \"" + path + "\"");
            git.StartAndWaitForExit ();


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
            GitCommand git;

            if (path == null) {
                git = new GitCommand (LocalPath, "--no-pager log --since=1.month --raw --find-renames --date=iso " +
                    "--format=medium --no-color --no-merges");

            } else {
                path = path.Replace ("\\", "/");

                git = new GitCommand (LocalPath, "--no-pager log --raw --find-renames --date=iso " +
                    "--format=medium --no-color --no-merges -- \"" + path + "\"");
            }

            string output = git.StartAndReadStandardOutput ();

            if (path == null && string.IsNullOrWhiteSpace (output)) {
                git = new GitCommand (LocalPath, "--no-pager log -n 75 --raw --find-renames --date=iso " +
                    "--format=medium --no-color --no-merges");

                output = git.StartAndReadStandardOutput ();
            }

            string [] lines      = output.Split ("\n".ToCharArray ());
            List<string> entries = new List <string> ();

            // Split up commit entries
            int line_number = 0;
            bool first_pass = true;
            string entry = "", last_entry = "";
            foreach (string line in lines) {
                if (line.StartsWith ("commit") && !first_pass) {
                    entries.Add (entry);
                    entry = "";
                    line_number = 0;

                } else {
                    first_pass = false;
                }

                // Only parse first 250 files to prevent memory issues
                if (line_number < 250) {
                    entry += line + "\n";
                    line_number++;
                }

                last_entry = entry;
            }

            entries.Add (last_entry);

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

                    if (child_path.EndsWith (".git")) {
                        if (child_path.Equals (Path.Combine (LocalPath, ".git")))
                            continue;

                        string HEAD_file_path = Path.Combine (child_path, "HEAD");
    
                        if (File.Exists (HEAD_file_path)) {
                            File.Move (HEAD_file_path, HEAD_file_path + ".backup");
                            Logger.LogInfo ("Git", Name + " | Renamed " + HEAD_file_path);
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
                            Logger.LogInfo ("Git", Name + " | Failed adding empty folder " + path);
                        }
                    }
                }

            } catch (IOException e) {
                Logger.LogInfo ("Git", "Failed preparing directory", e);
            }
        }



        List<Change> ParseStatus ()
        {
            List<Change> changes = new List<Change> ();

            var git_status = new GitCommand (LocalPath, "status --porcelain");
            git_status.Start ();
            
            while (!git_status.StandardOutput.EndOfStream) {
                string line = git_status.StandardOutput.ReadLine ();
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
            
            git_status.StandardOutput.ReadToEnd ();
            git_status.WaitForExit ();

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
                        directory.Name.Equals (".git") || 
                        directory.Name.Equals ("rebase-apply")) {

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
