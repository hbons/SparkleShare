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
using System.Text.RegularExpressions;
using SparkleLib;

namespace SparkleLib.Git {

    public class SparkleRepo : SparkleRepoBase {

		private bool author_set = false;


        public SparkleRepo (string path) : base (path)
        {
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
                string file_path = new string [] {LocalPath, ".git", "repo_size"}.Combine ();

                try {
                    return double.Parse (File.ReadAllText (file_path));

                } catch {
                    return 0;
                }
            }
        }


        public override double HistorySize {
            get {
                string file_path = new string [] {LocalPath, ".git", "repo_history_size"}.Combine ();

                try {
                    return double.Parse (File.ReadAllText (file_path));

                } catch {
                    return 0;
                }
            }
        }


        private void UpdateSizes ()
        {
            double size = CalculateSizes (
                new DirectoryInfo (LocalPath));

            double history_size = CalculateSizes (
                new DirectoryInfo (Path.Combine (LocalPath, ".git")));

            string size_file_path = new string [] {LocalPath, ".git", "repo_size"}.Combine ();
            string history_size_file_path = new string [] {LocalPath, ".git", "repo_history_size"}.Combine ();

            File.WriteAllText (size_file_path, size.ToString ());
            File.WriteAllText (history_size_file_path, history_size.ToString ());
        }


        public override void CreateInitialChangeSet ()
        {
            base.CreateInitialChangeSet ();
            SyncUp (); // FIXME: Weird freeze happens when base class handles this
        }


        public override string [] UnsyncedFilePaths {
            get {
                List<string> file_paths = new List<string> ();

                SparkleGit git = new SparkleGit (LocalPath, "status --porcelain");
                git.Start ();

                // Reading the standard output HAS to go before
                // WaitForExit, or it will hang forever on output > 4096 bytes
                string output = git.StandardOutput.ReadToEnd ().TrimEnd ();
                git.WaitForExit ();

                string [] lines = output.Split ("\n".ToCharArray ());
                foreach (string line in lines) {
                    if (line [1].ToString ().Equals ("M") ||
                        line [1].ToString ().Equals ("?") ||
                        line [1].ToString ().Equals ("A")) {

                        string path = line.Substring (3);
                        path        = path.Trim ("\"".ToCharArray ());
                        file_paths.Add (path);
                    }
                }

                return file_paths.ToArray ();
            }
        }


        public override string CurrentRevision {
            get {
                // Remove stale rebase-apply files because it
                // makes the method return the wrong hashes.
                string rebase_apply_file = SparkleHelpers.CombineMore (LocalPath, ".git", "rebase-apply");

                if (File.Exists (rebase_apply_file))
                    File.Delete (rebase_apply_file);

                SparkleGit git = new SparkleGit (LocalPath, "rev-parse HEAD");
                git.Start ();
                
                string output = git.StandardOutput.ReadToEnd ();
                git.WaitForExit ();

                if (git.ExitCode == 0) {
                    return output.TrimEnd ();

                } else {
                    return null;
                }
            }
        }


        public override bool HasRemoteChanges {
            get {
                SparkleHelpers.DebugInfo ("Git", Name + " | Checking for remote changes...");

                string current_revision = CurrentRevision;
                SparkleGit git = new SparkleGit (LocalPath, "ls-remote --exit-code \"" + RemoteUrl + "\" master");
    
                git.Start ();
                git.WaitForExit ();
    
                if (git.ExitCode != 0)
                    return false;
    
                string remote_revision = git.StandardOutput.ReadToEnd ().Substring (0, 40);
    
                if (!remote_revision.StartsWith (current_revision)) {
                    SparkleHelpers.DebugInfo ("Git",
                        Name + " | Remote changes detected (local: " +
                        current_revision + ", remote: " + remote_revision + ")");

                    return true;

                } else {
                    SparkleHelpers.DebugInfo ("Git",
                        Name + " | No remote changes detected (local+remote: " + current_revision + ")");

                    return false;
                }
            }
        }


        public override bool SyncUp ()
        {
            if (HasLocalChanges) {
                Add ();

                string message = FormatCommitMessage ();
                Commit (message);
            }

            SparkleGit git = new SparkleGit (LocalPath,
                "push --progress " + // Redirects progress stats to standarderror
                "\"" + RemoteUrl + "\" master");

            git.StartInfo.RedirectStandardError = true;
            git.Start ();

            double percentage = 1.0;
            Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);

            while (!git.StandardError.EndOfStream) {
                string line   = git.StandardError.ReadLine ();
                Match match   = progress_regex.Match (line);
                string speed  = "";
                double number = 0.0;

                if (match.Success) {
                    number = double.Parse (match.Groups [1].Value);

                    // The pushing progress consists of two stages: the "Compressing
                    // objects" stage which we count as 20% of the total progress, and
                    // the "Writing objects" stage which we count as the last 80%
                    if (line.StartsWith ("Compressing")) {
                        // "Compressing objects" stage
                        number = (number / 100 * 20);

                    } else {
                        if (line.StartsWith ("ERROR: QUOTA EXCEEDED")) {
                            int quota_limit = int.Parse (line.Substring (21).Trim ());
                            throw new QuotaExceededException ("Quota exceeded", quota_limit);
                        }


                        // "Writing objects" stage
                        number = (number / 100 * 80 + 20);

                        if (line.Contains ("|")) {
                            speed = line.Substring (line.IndexOf ("|") + 1).Trim ();
                            speed = speed.Replace (", done.", "").Trim ();
                            speed = speed.Replace ("i", "");
                            speed = speed.Replace ("KB/s", "ᴋʙ/s");
                            speed = speed.Replace ("MB/s", "ᴍʙ/s");
                        }
                    }

                } else {
                    SparkleHelpers.DebugInfo ("Git", Name + " | " + line);
                }

                if (number >= percentage) {
                    percentage = number;
                    base.OnProgressChanged (percentage, speed);
                }
            }

            git.WaitForExit ();

            UpdateSizes ();
            ChangeSets = GetChangeSets ();

            if (git.ExitCode == 0)
                return true;
            else
                return false;
        }


        public override bool SyncDown ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "fetch --progress \"" + RemoteUrl + "\" master");

            git.StartInfo.RedirectStandardError = true;
            git.Start ();

            double percentage = 1.0;
            Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);

            while (!git.StandardError.EndOfStream) {
                string line   = git.StandardError.ReadLine ();
                Match match   = progress_regex.Match (line);
                string speed  = "";
                double number = 0.0;

                if (match.Success) {
                    number = double.Parse (match.Groups [1].Value);

                    // The fetching progress consists of two stages: the "Compressing
                    // objects" stage which we count as 20% of the total progress, and
                    // the "Receiving objects" stage which we count as the last 80%
                    if (line.StartsWith ("Compressing")) {
                        // "Compressing objects" stage
                        number = (number / 100 * 20);

                    } else {
                        // "Writing objects" stage
                        number = (number / 100 * 80 + 20);

                        if (line.Contains ("|")) {
                            speed = line.Substring (line.IndexOf ("|") + 1).Trim ();
                            speed = speed.Replace (", done.", "").Trim ();
                            speed = speed.Replace ("i", "");
                            speed = speed.Replace ("KB/s", "ᴋʙ/s");
                            speed = speed.Replace ("MB/s", "ᴍʙ/s");
                        }
                    }

                } else {
                    SparkleHelpers.DebugInfo ("Git", Name + " | " + line);
                }
                

                if (number >= percentage) {
                    percentage = number;
                    base.OnProgressChanged (percentage, speed);
                }
            }

            git.WaitForExit ();

            UpdateSizes ();

            if (git.ExitCode == 0) {
                Rebase ();
                
				File.SetAttributes (
					Path.Combine (LocalPath, ".sparkleshare"),
					FileAttributes.Hidden
				);

                ChangeSets = GetChangeSets ();

				return true;

            } else {
                ChangeSets = GetChangeSets ();
                return false;
            }
        }


        public override bool HasLocalChanges {
            get {
                PrepareDirectories (LocalPath);

                SparkleGit git = new SparkleGit (LocalPath, "status --porcelain");
                git.Start ();

                // Reading the standard output HAS to go before
                // WaitForExit, or it will hang forever on output > 4096 bytes
                string output = git.StandardOutput.ReadToEnd ().TrimEnd ();
                git.WaitForExit ();

                string [] lines = output.Split ("\n".ToCharArray ());

                foreach (string line in lines) {
                    if (line.Trim ().Length > 0)
                        return true;
                }

                return false;
            }
        }


        public override bool HasUnsyncedChanges {
            get {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".git", "has_unsynced_changes");

                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".git", "has_unsynced_changes");

                if (value) {
                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path).Close ();

                } else {
                    File.Delete (unsynced_file_path);
                }
            }
        }


        // Stages the made changes
        private void Add ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "add --all");
            git.Start ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Git", Name + " | Changes staged");
        }


        // Commits the made changes
        private void Commit (string message)
		{
			SparkleGit git;

			if (!this.author_set) {
	            git = new SparkleGit (LocalPath,
	                "config user.name \"" + SparkleConfig.DefaultConfig.User.Name + "\"");

				git.Start ();
				git.WaitForExit ();

	            git = new SparkleGit (LocalPath,
	                "config user.email \"" + SparkleConfig.DefaultConfig.User.Email + "\"");

				git.Start ();
				git.WaitForExit ();

				this.author_set = true;
			}

            git = new SparkleGit (LocalPath,
                "commit -m \"" + message + "\" " +
                "--author=\"" + SparkleConfig.DefaultConfig.User.Name +
                " <" + SparkleConfig.DefaultConfig.User.Email + ">\"");

            git.Start ();
            git.StandardOutput.ReadToEnd ();
            git.WaitForExit ();
        }


        // Merges the fetched changes
        private void Rebase ()
        {
            if (HasLocalChanges) {
                Add ();

                string commit_message = FormatCommitMessage ();
                Commit (commit_message);
            }

            SparkleGit git = new SparkleGit (LocalPath, "rebase FETCH_HEAD");
            git.StartInfo.RedirectStandardOutput = false;

            git.Start ();
            git.WaitForExit ();

            if (git.ExitCode != 0) {
                SparkleHelpers.DebugInfo ("Git", Name + " | Conflict detected, trying to get out...");

                while (HasLocalChanges)
                    ResolveConflict ();

                SparkleHelpers.DebugInfo ("Git", Name + " | Conflict resolved");
                OnConflictResolved ();
            }
        }


        private void ResolveConflict ()
        {
            // This is a list of conflict status codes that Git uses, their
            // meaning, and how SparkleShare should handle them.
            //
            // DD    unmerged, both deleted    -> Do nothing
            // AU    unmerged, added by us     -> Use theirs, save ours as a timestamped copy
            // UD    unmerged, deleted by them -> Use ours
            // UA    unmerged, added by them   -> Use theirs, save ours as a timestamped copy
            // DU    unmerged, deleted by us   -> Use theirs
            // AA    unmerged, both added      -> Use theirs, save ours as a timestamped copy
            // UU    unmerged, both modified   -> Use theirs, save ours as a timestamped copy
            // ??    unmerged, new files       -> Stage the new files
            //
            // Note that a rebase merge works by replaying each commit from the working branch on
            // top of the upstream branch. Because of this, when a merge conflict happens the
            // side reported as 'ours' is the so-far rebased series, starting with upstream,
            // and 'theirs' is the working branch. In other words, the sides are swapped.
            //
            // So: 'ours' means the 'server's version' and 'theirs' means the 'local version'

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            git_status.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_status.StandardOutput.ReadToEnd ().TrimEnd ();
            git_status.WaitForExit ();

            string [] lines = output.Split ("\n".ToCharArray ());

            foreach (string line in lines) {
                string conflicting_path = line.Substring (3);
                conflicting_path = conflicting_path.Trim ("\"".ToCharArray ());

                SparkleHelpers.DebugInfo ("Git", Name + " | Conflict type: " + line);

                // Ignore conflicts in the .sparkleshare file and use the local version
                if (conflicting_path.EndsWith (".sparkleshare") ||
                    conflicting_path.EndsWith (".empty")) {

                    // Recover local version
                    SparkleGit git_theirs = new SparkleGit (LocalPath,
                        "checkout --theirs \"" + conflicting_path + "\"");

                    git_theirs.Start ();
                    git_theirs.WaitForExit ();

                    File.SetAttributes (Path.Combine (LocalPath, conflicting_path), FileAttributes.Hidden);

                    SparkleGit git_rebase_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_rebase_continue.Start ();
                    git_rebase_continue.WaitForExit ();

                    continue;
                }

                // Both the local and server version have been modified
                if (line.StartsWith ("UU") || line.StartsWith ("AA") ||
                    line.StartsWith ("AU") || line.StartsWith ("UA")) {

                    // Recover local version
                    SparkleGit git_theirs = new SparkleGit (LocalPath,
                        "checkout --theirs \"" + conflicting_path + "\"");

                    git_theirs.Start ();
                    git_theirs.WaitForExit ();

                    // Append a timestamp to local version.
                    // Windows doesn't allow colons in the file name, so
                    // we use "h" between the hours and minutes instead.
                    string timestamp  = DateTime.Now.ToString ("MMM d H\\hmm");
                    string their_path = Path.GetFileNameWithoutExtension (conflicting_path) +
                        " (" + SparkleConfig.DefaultConfig.User.Name + ", " + timestamp + ")" +
                        Path.GetExtension (conflicting_path);
                    
                    string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
                    string abs_their_path       = Path.Combine (LocalPath, their_path);

                    File.Move (abs_conflicting_path, abs_their_path);

                    // Recover server version
                    SparkleGit git_ours = new SparkleGit (LocalPath,
                        "checkout --ours \"" + conflicting_path + "\"");

                    git_ours.Start ();
                    git_ours.WaitForExit ();

                    Add ();

                    SparkleGit git_rebase_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_rebase_continue.Start ();
                    git_rebase_continue.WaitForExit ();

                // The local version has been modified, but the server version was removed
                } else if (line.StartsWith ("DU")) {

                    // The modified local version is already in the
                    // checkout, so it just needs to be added.
                    //
                    // We need to specifically mention the file, so
                    // we can't reuse the Add () method
                    SparkleGit git_add = new SparkleGit (LocalPath,
                        "add \"" + conflicting_path + "\"");

                    git_add.Start ();
                    git_add.WaitForExit ();

                    SparkleGit git_rebase_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_rebase_continue.Start ();
                    git_rebase_continue.WaitForExit ();

                // The server version has been modified, but the local version was removed
                } else if (line.StartsWith ("UD")) {

                    // We can just skip here, the server version is
                    // already in the checkout
                    SparkleGit git_rebase_skip = new SparkleGit (LocalPath, "rebase --skip");
                    git_rebase_skip.Start ();
                    git_rebase_skip.WaitForExit ();

                // New local files
                } else {
                    Add ();

                    SparkleGit git_rebase_continue = new SparkleGit (LocalPath, "rebase --continue");
                    git_rebase_continue.Start ();
                    git_rebase_continue.WaitForExit ();
                }
            }
        }


        // Returns a list of the latest change sets
        public override List<SparkleChangeSet> GetChangeSets (int count)
        {
            if (count < 1)
                count = 30;

            count = 150;

            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();

            SparkleGit git_log = new SparkleGit (LocalPath,
                "log -" + count + " --raw -M --date=iso --format=medium --no-color --no-merges");

            git_log.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_log.StandardOutput.ReadToEnd ();
            git_log.WaitForExit ();

            string [] lines       = output.Split ("\n".ToCharArray ());
            List <string> entries = new List <string> ();

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

                // Only parse 250 files to prevent memory issues
                if (line_number < 254) {
                    entry += line + "\n";
                    line_number++;
                }

                last_entry = entry;
            }

            entries.Add (last_entry);

            Regex regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                "Author: (.+) <(.+)>\n" +
                "*" +
                "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                "([0-9]{2}):([0-9]{2}):([0-9]{2}) (.[0-9]{4})\n" +
                "*", RegexOptions.Compiled);

            foreach (string log_entry in entries) {
                Match match = regex.Match (log_entry);

                if (match.Success) {
                    SparkleChangeSet change_set = new SparkleChangeSet ();

                    change_set.Folder    = new SparkleFolder (Name);
                    change_set.Revision  = match.Groups [1].Value;
                    change_set.User      = new SparkleUser (match.Groups [2].Value, match.Groups [3].Value);
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

                    foreach (string entry_line in entry_lines) {
                        if (entry_line.StartsWith (":")) {

                            string change_type = entry_line [37].ToString ();
                            string file_path   = entry_line.Substring (39);
                            string to_file_path;

                            if (file_path.EndsWith (".empty"))
                                file_path = file_path.Substring (0,
                                    file_path.Length - ".empty".Length);

                            if (file_path.Equals (".sparkleshare"))
                                continue;

                            if (change_type.Equals ("A")) {
                                change_set.Changes.Add (
                                    new SparkleChange () {
                                        Path      = file_path,
                                        Timestamp = change_set.Timestamp,
                                        Type      = SparkleChangeType.Added
                                    }
                                );

                            } else if (change_type.Equals ("M")) {
                                change_set.Changes.Add (
                                    new SparkleChange () {
                                        Path      = file_path,
                                        Timestamp = change_set.Timestamp,
                                        Type      = SparkleChangeType.Edited
                                    }
                                );

                            } else if (change_type.Equals ("D")) {
                                change_set.Changes.Add (
                                    new SparkleChange () {
                                        Path      = file_path,
                                        Timestamp = change_set.Timestamp,
                                        Type      = SparkleChangeType.Deleted
                                    }
                                );

                            } else if (change_type.Equals ("R")) {
                                int tab_pos  = entry_line.LastIndexOf ("\t");
                                file_path    = entry_line.Substring (42, tab_pos - 42);
                                to_file_path = entry_line.Substring (tab_pos + 1);

                                if (file_path.EndsWith (".empty"))
                                    file_path = file_path.Substring (0, file_path.Length - 6);

                                if (to_file_path.EndsWith (".empty"))
                                    to_file_path = to_file_path.Substring (0, to_file_path.Length - 6);

                                change_set.Changes.Add (
                                    new SparkleChange () {
                                        Path        = file_path,
                                        MovedToPath = to_file_path,
                                        Timestamp   = change_set.Timestamp,
                                        Type        = SparkleChangeType.Moved
                                    }
                                );
                            }
                        }
                    }

                    if (change_set.Changes.Count > 0) {
                        if (change_sets.Count > 0) {
                            SparkleChangeSet last_change_set = change_sets [change_sets.Count - 1];

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
                            change_sets.Add (change_set);
                        }
                    }
                }
            }

            return change_sets;
        }


        // Git doesn't track empty directories, so this method
        // fills them all with a hidden empty file.
        //
        // It also prevents git repositories from becoming
        // git submodules by renaming the .git/HEAD file
        private void PrepareDirectories (string path)
        {
            try {
                foreach (string child_path in Directory.GetDirectories (path)) {
                    if (SparkleHelpers.IsSymlink (child_path))
                        continue;

                    if (child_path.EndsWith (".git")) {
                        if (child_path.Equals (Path.Combine (LocalPath, ".git")))
                            continue;
    
                        string HEAD_file_path = Path.Combine (child_path, "HEAD");
    
                        if (File.Exists (HEAD_file_path)) {
                            File.Move (HEAD_file_path, HEAD_file_path + ".backup");
                            SparkleHelpers.DebugInfo ("Git", Name + " | Renamed " + HEAD_file_path);
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
                            SparkleHelpers.DebugInfo ("Git", Name + " | Failed adding empty folder " + path);
                        }
                    }
                }

            } catch (IOException e) {
                SparkleHelpers.DebugInfo ("Git", "Failed preparing directory: " + e.Message);
            }
        }


        // Creates a pretty commit message based on what has changed
        private string FormatCommitMessage ()
        {
            List<string> Added    = new List<string> ();
            List<string> Modified = new List<string> ();
            List<string> Removed  = new List<string> ();
            string file_name      = "";
            string message        = "";

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            git_status.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_status.StandardOutput.ReadToEnd ().Trim ("\n".ToCharArray ());
            git_status.WaitForExit ();

            string [] lines = output.Split ("\n".ToCharArray ());
            foreach (string line in lines) {
                if (line.StartsWith ("A"))
                    Added.Add (line.Substring (3));
                else if (line.StartsWith ("M"))
                    Modified.Add (line.Substring (3));
                else if (line.StartsWith ("D"))
                    Removed.Add (line.Substring (3));
                else if (line.StartsWith ("R")) {
                    Removed.Add (line.Substring (3, (line.IndexOf (" -> ") - 3)));
                    Added.Add (line.Substring (line.IndexOf (" -> ") + 4));
                }
            }

            int count     = 0;
            int max_count = 20;

            string n = Environment.NewLine;

            foreach (string added in Added) {
                file_name = added.Trim ("\"".ToCharArray ());

                if (file_name.EndsWith (".empty"))
                    file_name = file_name.Substring (0, file_name.Length - 6);

                message += "+ ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "...";
            }

            foreach (string modified in Modified) {
                file_name = modified.Trim ("\"".ToCharArray ());

                if (file_name.EndsWith (".empty"))
                    continue;

                message += "/ ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "...";
            }

            foreach (string removed in Removed) {
                file_name = removed.Trim ("\"".ToCharArray ());

                if (file_name.EndsWith (".empty"))
                    file_name = file_name.Substring (0, file_name.Length - 6);

                message += "- ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "..." + n;
            }

            message = message.Replace ("\"", "");
            return message.TrimEnd ();
        }


        // Recursively gets a folder's size in bytes
        private double CalculateSizes (DirectoryInfo parent)
        {
            if (!Directory.Exists (parent.FullName))
                return 0;

            if (parent.Name.Equals ("rebase-apply"))
                return 0;

            double size = 0;

            try {
                foreach (FileInfo file in parent.GetFiles ()) {
                    if (!file.Exists)
                        return 0;

                    if (file.Name.Equals (".empty"))
                        File.SetAttributes (file.FullName, FileAttributes.Hidden);

                    size += file.Length;
                }

            } catch (Exception e) {
                SparkleHelpers.DebugInfo ("Local", "Error calculating size: " + e.Message);
                return 0;
            }


            try {
                foreach (DirectoryInfo directory in parent.GetDirectories ())
                    size += CalculateSizes (directory);

            } catch (Exception e) {
                SparkleHelpers.DebugInfo ("Local", "Error calculating size: " + e.Message);
                return 0;
            }

            return size;
        }
    }
}
