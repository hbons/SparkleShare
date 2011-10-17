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
using System.Xml;

namespace SparkleLib {

    public class SparkleRepoGit : SparkleRepoBase {

        private string exlude_rules_file_path;
        private string ExclusionBlock = "#Temporary Exclusions";

        public SparkleRepoGit (string path, SparkleBackend backend) :
            base (path, backend) {
            // Set exclude file path
            exlude_rules_file_path = SparkleHelpers.CombineMore (
                 LocalPath, ".git", "info", "exclude");
        }


        public override string Identifier {
            get {

                // Because git computes a hash based on content,
                // author, and timestamp; it is unique enough to
                // use the hash of the first commit as an identifier
                // for our folder
                SparkleGit git = new SparkleGit (LocalPath, "rev-list --reverse HEAD");
                git.Start ();

                // Reading the standard output HAS to go before
                // WaitForExit, or it will hang forever on output > 4096 bytes
                string output = git.StandardOutput.ReadToEnd ();
                git.WaitForExit ();

                if (output.Length < 40)
                    return null;

                return output.Substring (0, 40);
            }
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
                        path = path.Trim ("\"".ToCharArray ());
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

                SparkleGit git = new SparkleGit (LocalPath, "log -1 --format=%H");
                git.Start ();
                git.WaitForExit ();

                if (git.ExitCode == 0) {
                    string output = git.StandardOutput.ReadToEnd ();
                    return output.TrimEnd ();

                } else {
                    return null;
                }
            }
        }


        public override bool CheckForRemoteChanges ()
        {
            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Checking for remote changes...");
            SparkleGit git = new SparkleGit (LocalPath, "ls-remote origin master");

            git.Start ();
            git.WaitForExit ();

            if (git.ExitCode != 0)
                return false;

            string remote_revision = git.StandardOutput.ReadToEnd ().TrimEnd ();

            if (!remote_revision.StartsWith (CurrentRevision)) {
                SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found. (" + remote_revision + ")");
                return true;

            } else {
                return false;
            }
        }


        public override bool SyncUp ()
        {
            Add ();

            string message = FormatCommitMessage ();
            Commit (message);

            SparkleGit git = new SparkleGit (LocalPath, "push origin master");
            git.Start ();
            git.StandardOutput.ReadToEnd ();
            git.WaitForExit ();

            if (git.ExitCode == 0)
                return true;
            else
                return false;
        }


        public override bool SyncDown ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "fetch -v");
            git.Start ();
            git.WaitForExit ();

            if (git.ExitCode == 0) {
                Rebase ();
                return true;

            } else {
                return false;
            }
        }


        public override bool AnyDifferences {
            get {
                FillEmptyDirectories (LocalPath);

                SparkleGit git = new SparkleGit (LocalPath, "status --porcelain");
                git.Start ();

                // Reading the standard output HAS to go before
                // WaitForExit, or it will hang forever on output > 4096 bytes
                string output = git.StandardOutput.ReadToEnd ().TrimEnd ();
                git.WaitForExit ();

                string [] lines = output.Split ("\n".ToCharArray ());

                foreach (string line in lines) {
                    if (line.Length > 1 && !line [1].Equals (" "))
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

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes staged");
        }

        // Add a new file to be ignored
        public override bool AddExclusionRule (FileSystemEventArgs args) {

            string RelativePath = SparkleHelpers.DiffPaths(args.FullPath, LocalPath);
            
            List<String> exclusions;
            try {
                exclusions = ReadExclusionRules();
            }
            catch {
                return false;
            }
            
            // Look for the local exclusions section
            bool added = false;
            for(int i = 0; i < exclusions.Count; i++) {
            	string entry = exclusions[i];
            	if(entry.Equals(ExclusionBlock)) {
                    // add a new exclusion rule containing a file path
                    exclusions.Insert(i + 1, RelativePath);
                    added = true;
                    break;
            	}
            }
            
            /*
             * For compability to existing repos:
             * Add a "#Temporary Exclusions"-Block to the
             * ignore file in order to recognize this
             * exclude rules later on
             */
            if(!added) {
                exclusions.Add(ExclusionBlock);
                exclusions.Add(RelativePath);
            }
            
            // Write exceptions list back to file
            return WriteExclusionRules(exclusions);
        }
		
        // Check whether a specific rule exists in the exclusion file
        public override bool ExclusionRuleExists(FileSystemEventArgs args) {
            string RelativePath = SparkleHelpers.DiffPaths(args.FullPath, LocalPath);
        
            List<String> exclusions;
            try {
                // Read rules from temporary block only
                exclusions = ReadExclusionRules(true);
        
                foreach(string entry in exclusions) {
                    if(entry.Equals(RelativePath)) {
                        return true;
                    }
                }
            } catch {
                SparkleHelpers.DebugInfo("Error", "Cannot determine whether an exclusion rule for " +
                                         args.FullPath + " already exists or not.");
                return false;
            }
        
            return false;
        }
        
        // Remove file from exclusion list when they are readable again
        public override bool RemoveExclusionRule(FileSystemEventArgs args) {
            string RelativePath = SparkleHelpers.DiffPaths(args.FullPath, LocalPath);
        
            List<String> exclusions;
            try {
                exclusions = ReadExclusionRules();
        
                /*
                 * Removing a rule should only apply to rules in the "Temporary Exclusion"-block.
                 * Therefore we first read until reaching the block and then remove the rule.
                 *
                 * We cannot use ReadExclusionRules(true) here since we write all lines back
                 * to the file. This would result in a crippled exclusion file.
                 */
                bool BlockReached = false;
                foreach(string entry in exclusions) {
                    if(entry.Equals(ExclusionBlock)) {
                        BlockReached = true;
                    }
        
                    // Remove this rule
                    if(BlockReached && entry.Equals(RelativePath)) {
                        exclusions.Remove(entry);
                        break;
                    }
                }
        
                return WriteExclusionRules(exclusions);
            } catch {
                SparkleHelpers.DebugInfo("Error", "Unable to remove exclusion rule for entry " + RelativePath);
                return false;
            }
        }

        // Reads the exclusion rules file into a string list
        private List<String> ReadExclusionRules() {
        
            List<String> exclusions = new List<String>();
            TextReader reader = new StreamReader (exlude_rules_file_path);;
        
            try {
                while(reader.Peek() > -1) {
                    exclusions.Add(reader.ReadLine().TrimEnd());
                }
            }
            catch (IOException e) {
                SparkleHelpers.DebugInfo("Error", "Reading from exclusion file failed: " + e.Message);
                return new List<String>();
            }
            finally {
                if(reader != null) {
                    reader.Close();
                }
            }
        
            return exclusions;
        }

        // Reads rules only from temporary exclusion block
        private List<String> ReadExclusionRules(bool TempOnly) {
            if(TempOnly) {
                bool ForceRead = false;
                List<String> exclusions = new List<String>();
                foreach(string entry in ReadExclusionRules()) {
                    if(ForceRead || entry.Equals(ExclusionBlock)) {
                        exclusions.Add(entry);
                        ForceRead = true;
                    }
                }
        
                return exclusions;
            }
        
            return ReadExclusionRules();
        }

        // Writes the exclusion rules file with a given string list
        private bool WriteExclusionRules(List<String> lines) {
        
            TextWriter writer = new StreamWriter (exlude_rules_file_path);
        
            try {
                foreach(string line in lines) {
                    writer.WriteLine(line.TrimEnd());
                }
            } catch(IOException e) {
                SparkleHelpers.DebugInfo("Error", "Writing into exclusion file failed: " + e.Message);
                return false;
            }
            finally {
                if(writer != null) {
                    writer.Close();
                }
            }
        
            return true;
        }

        // Removes unneeded objects
        private void CollectGarbage ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "gc");
            git.Start ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Garbage collected.");
        }


        // Commits the made changes
        private void Commit (string message)
        {
            SparkleGit git = new SparkleGit (LocalPath,
                "commit -m \"" + message + "\" " +
                "--author=\"" + SparkleConfig.DefaultConfig.User.Name +
                " <" + SparkleConfig.DefaultConfig.User.Email + ">\"");

            git.Start ();
            git.StandardOutput.ReadToEnd ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message);

            // Collect garbage pseudo-randomly. Turn off for
            // now: too resource heavy.
            // if (DateTime.Now.Second % 10 == 0)
            //     CollectGarbage ();
        }


        // Merges the fetched changes
        private void Rebase ()
        {
            DisableWatching ();

            if (AnyDifferences) {
                Add ();

                string commit_message = FormatCommitMessage ();
                Commit (commit_message);
            }

            SparkleGit git = new SparkleGit (LocalPath, "rebase -v FETCH_HEAD");

            git.Start ();
            git.WaitForExit ();

            if (git.ExitCode != 0) {
                SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict detected. Trying to get out...");

                while (AnyDifferences)
                    ResolveConflict ();

                SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict resolved.");
                OnConflictResolved ();
            }

            EnableWatching ();
        }


        private void ResolveConflict ()
        {
            // This is al list of conflict status codes that Git uses, their
            // meaning, and how SparkleShare should handle them.
            //
            // DD    unmerged, both deleted    -> Do nothing
            // AU    unmerged, added by us     -> Use theirs, save ours as a timestamped copy
            // UD    unmerged, deleted by them -> Use ours
            // UA    unmerged, added by them   -> Use theirs, save ours as a timestamped copy
            // DU    unmerged, deleted by us   -> Use theirs
            // AA    unmerged, both added      -> Use theirs, save ours as a timestamped copy
            // UU    unmerged, both modified   -> Use theirs, save ours as a timestamped copy
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

            if (String.IsNullOrEmpty (output)) {
                // no conflict any more.
                return;
            }

            string [] lines = output.Split ("\n".ToCharArray ());

            foreach (string line in lines) {
                string conflicting_path = line.Substring (3);
                conflicting_path = conflicting_path.Trim ("\"".ToCharArray ());

                SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict type: " + line);

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
                    string timestamp            = DateTime.Now.ToString ("HH\\hmm MMM d");
                    string their_path           = conflicting_path + " (" + SparkleConfig.DefaultConfig.User.Name + ", " + timestamp + ")";
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
                }

                // The local version has been modified, but the server version was removed
                if (line.StartsWith ("DU")) {

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
                }

                // The server version has been modified, but the local version was removed
                if (line.StartsWith ("UD")) {

                    // We can just skip here, the server version is
                    // already in the checkout
                    SparkleGit git_rebase_skip = new SparkleGit (LocalPath, "rebase --skip");
                    git_rebase_skip.Start ();
                    git_rebase_skip.WaitForExit ();
                }
            }
        }


        // Returns a list of the latest change sets
        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
            if (count < 1)
                count = 30;

            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();

            SparkleGit git_log = new SparkleGit (LocalPath, "log -" + count + " --raw -M --date=iso");
			if ((SparkleBackend.Platform == PlatformID.Unix ||
				 SparkleBackend.Platform == PlatformID.MacOSX)) {
				// this causes an IOException on windows
				Console.OutputEncoding = System.Text.Encoding.Unicode;
			}

            git_log.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = git_log.StandardOutput.ReadToEnd ();
            git_log.WaitForExit ();

            string [] lines       = output.Split ("\n".ToCharArray ());
            List <string> entries = new List <string> ();

            int j = 0;
            string entry = "", last_entry = "";
            foreach (string line in lines) {
                if (line.StartsWith ("commit") && j > 0) {
                    entries.Add (entry);
                    entry = "";
                }

                entry += line + "\n";
                j++;

                last_entry = entry;
            }

            entries.Add (last_entry);

            Regex merge_regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                "Merge: .+ .+\n" +
                                "Author: (.+) <(.+)>\n" +
                                "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                "([0-9]{2}):([0-9]{2}):([0-9]{2}) .([0-9]{4})\n" +
                                "*", RegexOptions.Compiled);

            Regex non_merge_regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                "Author: (.+) <(.+)>\n" +
                                "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                "([0-9]{2}):([0-9]{2}):([0-9]{2}) (.[0-9]{4})\n" +
                                "*", RegexOptions.Compiled);

            foreach (string log_entry in entries) {
                Regex regex;
                bool is_merge_commit = false;

                if (log_entry.Contains ("\nMerge: ")) {
                    regex = merge_regex;
                    is_merge_commit = true;
                } else {
                    regex = non_merge_regex;
                }

                Match match = regex.Match (log_entry);

                if (match.Success) {
                    SparkleChangeSet change_set = new SparkleChangeSet ();

                    change_set.Folder        = Name;
                    change_set.Revision      = match.Groups [1].Value;
                    change_set.User.Name     = match.Groups [2].Value;
                    change_set.User.Email    = match.Groups [3].Value;
                    change_set.IsMagical     = is_merge_commit;

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
                                file_path = file_path.Substring (0, file_path.Length - ".empty".Length);

                            if (change_type.Equals ("A") && !file_path.Contains (".notes")) {
                                change_set.Added.Add (file_path);

                            } else if (change_type.Equals ("M")) {
                                change_set.Edited.Add (file_path);

                            } else if (change_type.Equals ("D")) {
                                change_set.Deleted.Add (file_path);

                            } else if (change_type.Equals ("R")) {
                                int tab_pos  = entry_line.LastIndexOf ("\t");
                                file_path    = entry_line.Substring (42, tab_pos - 42);
                                to_file_path = entry_line.Substring (tab_pos + 1);

                                change_set.MovedFrom.Add (file_path);
                                change_set.MovedTo.Add (to_file_path);
                            }
                        }
                    }

                    if ((change_set.Added.Count +
                         change_set.Edited.Count +
                         change_set.Deleted.Count +
                         change_set.MovedFrom.Count) > 0) {

                        change_set.Notes.AddRange (GetNotes (change_set.Revision));
                        change_sets.Add (change_set);
                    }
                }
            }

            return change_sets;
        }


        // Git doesn't track empty directories, so this method
        // fills them all with a hidden empty file
        private void FillEmptyDirectories (string path)
        {
            foreach (string child_path in Directory.GetDirectories (path)) {
                if (child_path.EndsWith (".git") || child_path.EndsWith (".notes"))
                    continue;

                FillEmptyDirectories (child_path);
            }

            if (Directory.GetFiles (path).Length == 0)
                File.Create (Path.Combine (path, ".empty")).Close ();
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
                message += "+ ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "...";
            }

            foreach (string modified in Modified) {
                file_name = modified.Trim ("\"".ToCharArray ());
                message += "/ ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "...";
            }

            foreach (string removed in Removed) {
                file_name = removed.Trim ("\"".ToCharArray ());
                message += "- ‘" + file_name + "’" + n;

                count++;
                if (count == max_count)
                    return message + "..." + n;
            }

            message = message.Replace ("\"", "");
            return message.TrimEnd ();
        }


        public override bool UsesNotificationCenter
        {
            get {
                string file_path = SparkleHelpers.CombineMore (LocalPath, ".git", "disable_notification_center");
                return !File.Exists (file_path);
            }
        }


        public override void CreateInitialChangeSet ()
        {
            base.CreateInitialChangeSet ();
            SyncUp ();
        }
    }
}
