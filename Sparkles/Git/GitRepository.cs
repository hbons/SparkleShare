//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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

            git_config = new GitCommand (LocalPath, "config core.sshCommand " + GitCommand.FormatGitSSHCommand (auth_info));
            git_config.StartAndWaitForExit();
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

                } catch (Exception e) {
                    Logger.LogInfo ("Git", Name + " | Failed to parse " + file_path, e);
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

                } catch (Exception e) {
                    Logger.LogInfo ("Git", Name + " | Failed to parse " + file_path, e);
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
            if (InstallationInfo.OperatingSystem == OS.macOS || InstallationInfo.OperatingSystem == OS.Windows) {
                pre_push_hook_content =
                    "#!/bin/sh" + Environment.NewLine +
                    "env GIT_SSH_COMMAND='" + GitCommand.FormatGitSSHCommand (auth_info) + "' " +
                    Path.Combine (Configuration.DefaultConfiguration.BinPath, "git-lfs").Replace ("\\", "/")  + " pre-push \"$@\"";

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
                File.Create (lfs_is_behind_file_path).Close ();

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

            string user_name  = base.local_config.User.Name;
            string user_email = base.local_config.User.Email;

            if (!this.user_is_set) {
                git = new GitCommand (LocalPath, "config user.name \"" + user_name + "\"");
                git.StartAndWaitForExit ();

                git = new GitCommand (LocalPath, "config user.email \"" + user_email + "\"");
                git.StartAndWaitForExit ();

                this.user_is_set = true;
            }

            if (StorageType == StorageType.Encrypted) {
                string password_file_path = Path.Combine (LocalPath, ".git", "info", "encryption_password");
                string password = File.ReadAllText (password_file_path);

                user_name  = user_name.AESEncrypt (password);
                user_email = user_email.AESEncrypt (password);
            }

            git = new GitCommand (LocalPath,
                string.Format ("commit --all --message=\"{0}\" --author=\"{1} <{2}>\"",
                    message, user_name, user_email));

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
                string conflicting_file_path = line.Substring (3);
                conflicting_file_path        = EnsureSpecialChars (conflicting_file_path);
                conflicting_file_path        = conflicting_file_path.Trim ("\"".ToCharArray ());

                // Remove possible rename indicators
                string [] separators = {" -> \"", " -> "};
                foreach (string separator in separators) {
                    if (conflicting_file_path.Contains (separator))
                        conflicting_file_path = conflicting_file_path.Substring (conflicting_file_path.IndexOf (separator) + separator.Length);
                }

                Logger.LogInfo ("Git", Name + " | Conflict type: " + line);

                // Ignore conflicts in hidden files and use the local versions
                if (conflicting_file_path.EndsWith (".sparkleshare") || conflicting_file_path.EndsWith (".empty")) {
                    Logger.LogInfo ("Git", Name + " | Ignoring conflict in special file: " + conflicting_file_path);

                    // Recover local version
                    var git_ours = new GitCommand (LocalPath, "checkout --ours \"" + conflicting_file_path + "\"");
                    git_ours.StartAndWaitForExit ();

                    string abs_conflicting_path = Path.Combine (LocalPath, conflicting_file_path);

                    if (File.Exists (abs_conflicting_path))
                        File.SetAttributes (abs_conflicting_path, FileAttributes.Hidden);

                    continue;
                }

                Logger.LogInfo ("Git", Name + " | Resolving: " + conflicting_file_path);

                // Both the local and server version have been modified
                if (line.StartsWith ("UU") || line.StartsWith ("AA") ||
                    line.StartsWith ("AU") || line.StartsWith ("UA")) {

                    // Get the author name of the conflicting version
                    var git_log = new GitCommand (LocalPath, "log -n 1 FETCH_HEAD --pretty=format:%an " + conflicting_file_path);
                    string other_author_name = git_log.StartAndReadStandardOutput ();


                    // Generate distinguishing names for both versions of the file
                    string clue_A = string.Format (" (by {0})", base.local_config.User.Name);
                    string clue_B = string.Format (" (by {0})", other_author_name);

                    if (base.local_config.User.Name == other_author_name) {
                        clue_A = " (A)";
                        clue_B = " (B)";
                    }


                    string file_name_A = Path.GetFileNameWithoutExtension (conflicting_file_path) + clue_A + Path.GetExtension (conflicting_file_path);
                    string file_name_B = Path.GetFileNameWithoutExtension (conflicting_file_path) + clue_B + Path.GetExtension (conflicting_file_path);

                    string abs_conflicting_file_path = Path.Combine (LocalPath, conflicting_file_path);

                    string abs_file_path_A = Path.Combine (Path.GetDirectoryName (abs_conflicting_file_path), file_name_A);
                    string abs_file_path_B = Path.Combine (Path.GetDirectoryName (abs_conflicting_file_path), file_name_B);


                    // Recover local version
                    var git_checkout_A = new GitCommand (LocalPath, "checkout --ours \"" + conflicting_file_path + "\"");
                    git_checkout_A.StartAndWaitForExit ();

                    if (File.Exists (abs_conflicting_file_path) && !File.Exists (abs_file_path_A))
                        File.Move (abs_conflicting_file_path, abs_file_path_A);


                    // Recover server version
                    var git_checkout_B = new GitCommand (LocalPath, "checkout --theirs \"" + conflicting_file_path + "\"");
                    git_checkout_B.StartAndWaitForExit ();

                    if (File.Exists (abs_conflicting_file_path) && !File.Exists (abs_file_path_B))
                        File.Move (abs_conflicting_file_path, abs_file_path_B);


                    // Recover original (before both versions diverged)
                    var git_checkout = new GitCommand (LocalPath, "checkout ORIG_HEAD^ \"" + conflicting_file_path + "\"");
                    git_checkout.StartAndWaitForExit ();


                    trigger_conflict_event = true;


                // The server version has been modified, but the local version was removed
                } else if (line.StartsWith ("DU")) {

                    // The modified local version is already in the checkout, so it just needs to be added.
                    // We need to specifically mention the file, so we can't reuse the Add () method
                    var git_add = new GitCommand (LocalPath, "add \"" + conflicting_file_path + "\"");
                    git_add.StartAndWaitForExit ();


                // The local version has been modified, but the server version was removed
                } else if (line.StartsWith ("UD")) {

                    // Recover our version
                    var git_theirs = new GitCommand (LocalPath, "checkout --ours \"" + conflicting_file_path + "\"");
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

            var git = new GitCommand (LocalPath,
                "commit --message=\"Conflict resolution\" --author=\"SparkleShare <info@sparkleshare.org>\"");

            git.StartInfo.RedirectStandardOutput = false;
            git.StartAndWaitForExit ();

            HasUnsyncedChanges = true;

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

            } catch (Exception e) {
                string message = string.Format ("Failed to move \"{0}\" to \"{1}\"", local_file_path, target_file_path);
                Logger.LogInfo ("Git", Name + " | " + message, e);
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

            string log_args = "--since=1.month --name-status --date=iso --find-renames --no-merges --no-color";

            if (path == null) {
                git = new GitCommand (LocalPath, "--no-pager log " + log_args);

            } else {
                path = path.Replace ("\\", "/");
                git = new GitCommand (LocalPath, "--no-pager log " + log_args + " -- \"" + path + "\"");
            }

            string output = git.StartAndReadStandardOutput ();

            if (path == null && string.IsNullOrWhiteSpace (output)) {
                git = new GitCommand (LocalPath, "--no-pager log -n 75 " + log_args);
                output = git.StartAndReadStandardOutput ();
            }


            // Offset the output so our log_regex can be simpler
            string commit_sep = "commit ";

            if (output.StartsWith (commit_sep))
                output = output.Substring (commit_sep.Length) + "\n\n" + commit_sep;


			MatchCollection matches = this.log_regex.Matches (output);

            foreach (Match match in matches) {
                ChangeSet change_set = ParseChangeSet (match);

                if (change_set == null)
                    continue;

                int count = 0;
                foreach (string line in match.Groups ["files"].Value.Split ("\n".ToCharArray ())) {
                    if (count++ == 256)
                        break;

                    Change change = ParseChange (line);

                    if (change == null)
                        continue;

                    change.Timestamp = change_set.Timestamp;
                    change_set.Changes.Add (change);
                }


                if (path == null && change_sets.Count > 0) {
                    ChangeSet last_change_set = change_sets [change_sets.Count - 1];

					// If a change set set already exists for this user and day, group into that one
                    if (change_set.Timestamp.Year == last_change_set.Timestamp.Year &&
                        change_set.Timestamp.Month == last_change_set.Timestamp.Month &&
                        change_set.Timestamp.Day == last_change_set.Timestamp.Day &&
                        change_set.User.Name.Equals (last_change_set.User.Name)) {

                        last_change_set.Changes.AddRange (change_set.Changes);

                        if (DateTime.Compare (last_change_set.Timestamp, change_set.Timestamp) < 1) {
                            last_change_set.FirstTimestamp = last_change_set.Timestamp;
                            last_change_set.Timestamp = change_set.Timestamp;
                            last_change_set.Revision = change_set.Revision;

                        } else {
                            last_change_set.FirstTimestamp = change_set.Timestamp;
                        }

                    } else {
                        change_sets.Add (change_set);
                    }

                } else if (path != null) {
                    // Don't show removals or moves in the history list of a file
                    var changes = new Change [change_set.Changes.Count];
                    change_set.Changes.CopyTo (changes);

                    foreach (Change change in changes) {
                        if (!change.Path.Equals (path))
                            continue;

                        if (change.Type == ChangeType.Deleted || change.Type == ChangeType.Moved)
                            change_set.Changes.Remove (change);
                    }

                    change_sets.Add (change_set);

                } else {
                    change_sets.Add (change_set);
                }
            }

            return change_sets;
        }


        ChangeSet ParseChangeSet (Match match)
        {
            ChangeSet change_set = new ChangeSet ();

            // Set the name and email
            if (match.Groups ["name"].Value == "SparkleShare")
                return null;

            change_set.Folder = new SparkleFolder (Name);
            change_set.Revision = match.Groups ["commit"].Value;
            change_set.User = new User (match.Groups ["name"].Value, match.Groups ["email"].Value);
            change_set.RemoteUrl = RemoteUrl;

            if (StorageType == StorageType.Encrypted) {
                string password_file_path = Path.Combine (LocalPath, ".git", "info", "encryption_password");
                string password = File.ReadAllText (password_file_path);

                try {
                    change_set.User = new User (
                        change_set.User.Name.AESDecrypt (password),
                        change_set.User.Email.AESDecrypt (password));

                } catch (Exception e) {
                    Console.WriteLine (e.StackTrace);
                    change_set.User = new User (match.Groups ["name"].Value, match.Groups ["email"].Value);
                }
            }

            // Get the right date and time by offsetting the timezones
            change_set.Timestamp = new DateTime (
                int.Parse (match.Groups ["year"].Value), int.Parse (match.Groups ["month"].Value), int.Parse (match.Groups ["day"].Value),
                int.Parse (match.Groups ["hour"].Value), int.Parse (match.Groups ["minute"].Value), int.Parse (match.Groups ["second"].Value));

            string time_zone = match.Groups ["timezone"].Value;
            int our_offset = TimeZone.CurrentTimeZone.GetUtcOffset (DateTime.Now).Hours;
            int their_offset = int.Parse (time_zone.Substring (0, 3));

            change_set.Timestamp = change_set.Timestamp.AddHours (their_offset * -1);
            change_set.Timestamp = change_set.Timestamp.AddHours (our_offset);

            return change_set;
        }


        Change ParseChange (string line)
        {
            // Skip lines containing backspace characters or the .sparkleshare file
            if (line.Contains ("\\177") || line.Contains (".sparkleshare"))
                return null;

            // File lines start with a change type letter and then a tab character
            if (!line.StartsWith ("A\t") &&
                !line.StartsWith ("M\t") &&
                !line.StartsWith ("D\t") &&
                !line.StartsWith ("R100\t")) {

                return null;
            }

            Change change = new Change () { Type = ChangeType.Added };
            string file_path;

            int first_tab_pos = line.IndexOf ('\t');
            int last_tab_pos = line.LastIndexOf ('\t');

            if (first_tab_pos == last_tab_pos) {
                char type_letter = line [0];

                if (type_letter == 'M')
                    change.Type = ChangeType.Edited;

                if (type_letter == 'D')
                    change.Type = ChangeType.Deleted;

                file_path = line.Substring (first_tab_pos + 1);

            } else {
                change.Type = ChangeType.Moved;

                // The "to" and "from" file paths are separated by a tab
                string [] parts = line.Split ("\t".ToCharArray ());

                file_path = parts [1];
                string to_file_path = parts [2];

                to_file_path = to_file_path.Replace ("\\\"", "\"");
                change.MovedToPath = to_file_path;
            }

            file_path = file_path.Replace ("\\\"", "\"");
            change.Path = file_path;

            string empty_name = ".empty";

            // Handle .empty files as if they were folders
            if (change.Path.EndsWith (empty_name)) {
                change.Path = change.Path.Substring (0, change.Path.Length - empty_name.Length);

                if (change.Type == ChangeType.Moved)
                    change.MovedToPath = change.MovedToPath.Substring (0, change.MovedToPath.Length - empty_name.Length);

                change.IsFolder = true;
            }

            try {
                change.Path = EnsureSpecialChars (change.Path);

                if (change.Type == ChangeType.Moved)
                    change.MovedToPath = EnsureSpecialChars (change.MovedToPath);

            } catch (Exception e) {
                Logger.LogInfo ("Local", string.Format ("Error parsing line due to special character: '{0}'", line), e);
                return null;
            }

            return change;
        }


        string EnsureSpecialChars (string path)
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

                        } catch (Exception e) {
                            Logger.LogInfo ("Git", Name + " | Failed adding empty folder " + path, e);
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
                        Path = EnsureSpecialChars (path),
                        MovedToPath = EnsureSpecialChars (moved_to_path)
                    };

                } else {
                    string path = line.Substring (2).Trim ("\" ".ToCharArray ());
                    change = new Change () { Path = EnsureSpecialChars (path) };
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
                    message +=  "< ‘" + EnsureSpecialChars (change.Path) + "’\n";
                    message +=  "> ‘" + EnsureSpecialChars (change.MovedToPath) + "’\n";

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


        Regex log_regex = new Regex (
            "(?'commit'[a-f0-9]{40})\n" +
            "Author: (?'name'.+?) <(?'email'.+?)>\n" +
            "Date:   (?'year'[0-9]{4})-(?'month'[0-9]{2})-(?'day'[0-9]{2}) (?'hour'[0-9]{2}):(?'minute'[0-9]{2}):(?'second'[0-9]{2}) (?'timezone'.[0-9]{4})\n" +
            "\n" +
            "    (?'message'.+?)\n" +
            "\n" +
            "(?'files'.+?)\n\ncommit ", RegexOptions.Singleline | RegexOptions.Compiled);
    }
}
