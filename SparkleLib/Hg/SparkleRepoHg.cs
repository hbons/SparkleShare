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

namespace SparkleLib {

    public class SparkleRepoHg : SparkleRepoBase {
		
        private string exlude_rules_file_path;
        private string ExclusionBlock = "#Temporary Exclusions";

        public SparkleRepoHg (string path, SparkleBackend backend) :
            base (path, backend) { 
            // Set exclude file path
            exlude_rules_file_path = SparkleHelpers.CombineMore (
                 LocalPath, ".hg", "hgignore");
		}


        public override string Identifier {
            get {
                SparkleHg hg = new SparkleHg (LocalPath, "log -r : --limit 1 --template \"{node}\"");
                hg.Start ();
                hg.WaitForExit ();

                return hg.StandardOutput.ReadToEnd ();
            }
        }


        public override string CurrentRevision {
            get {
                SparkleHg hg = new SparkleHg (LocalPath, "log --limit 1 --template \"{node}\"");
                hg.Start ();
                hg.WaitForExit ();

		string hash = hg.StandardOutput.ReadToEnd ().Trim ();
                if (hash.Length > 0)
                    return hash;
                else
                    return null;
            }
        }


        public override bool CheckForRemoteChanges ()
        {
            return true; // Mercurial doesn't have a way to check for the remote hash
        }


        public override bool SyncUp ()
        {
            Add ();

            string message = FormatCommitMessage ();
            Commit (message);

            SparkleHg hg = new SparkleHg (LocalPath, "push");

            hg.Start ();
            hg.WaitForExit ();

            if (hg.ExitCode == 0) {
                return true;
            } else {
                return false;
            }
        }


        public override bool SyncDown ()
        {
            SparkleHg hg = new SparkleHg (LocalPath, "pull");

            hg.Start ();
            hg.WaitForExit ();

            if (hg.ExitCode == 0) {
                Merge ();
                return true;
            } else {
                return false;
            }
        }


        public override bool AnyDifferences {
            get {
                SparkleHg hg = new SparkleHg (LocalPath, "status");
                hg.Start ();
                hg.WaitForExit ();

                string output = hg.StandardOutput.ReadToEnd ().TrimEnd ();
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
                    ".hg", "has_unsynced_changes");

                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".hg", "has_unsynced_changes");

                if (value) {
                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path);
                } else {
                    File.Delete (unsynced_file_path);
                }
            }
        }


        // Stages the made changes
        private void Add ()
        {
            SparkleHg hg = new SparkleHg (LocalPath, "addremove --quiet");
            hg.Start ();
            hg.WaitForExit ();

            SparkleHelpers.DebugInfo ("Hg", "[" + Name + "] Changes staged");
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

        // Commits the made changes
        private void Commit (string message)
        {
            if (!AnyDifferences)
                return;

            SparkleHg hg = new SparkleHg (LocalPath, "commit -m '" + message + "'");
            hg.Start ();
            hg.WaitForExit ();

            SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message);
        }


        // Merges the fetched changes
        private void Merge ()
        {
            DisableWatching ();

            if (AnyDifferences) {
                Add ();

                string commit_message = FormatCommitMessage ();
                Commit (commit_message);
            }

            SparkleHg hg = new SparkleHg (LocalPath, "update");

            hg.Start ();
            hg.WaitForExit ();

            EnableWatching ();
        }


        // Returns a list of the latest change sets
        public override List<SparkleChangeSet> GetChangeSets (int count)
        {
            if (count < 1)
                count = 30;

            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();

            SparkleHg hg_log = new SparkleHg (LocalPath, "log --limit " + count + " --style changelog --verbose --stat");
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            hg_log.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = hg_log.StandardOutput.ReadToEnd ();
            hg_log.WaitForExit ();

            string [] lines       = output.Split ("\n".ToCharArray ());
            List <string> entries = new List <string> ();

            int j = 0;
            string entry = "", last_entry = "";
            foreach (string line in lines) {
                if (line.StartsWith ("2") && line.EndsWith (")") && j > 0) {
                    entries.Add (entry);
                    entry = "";
                }

                entry += line + "\n";
                j++;

                last_entry = entry;
            }

            entries.Add (last_entry);

            Regex regex = new Regex (@"([0-9]{4})-([0-9]{2})-([0-9]{2}).*([0-9]{2}):([0-9]{2}).*.([0-9]{4})" +
                                      "(.+)<(.+)>.*.([a-z0-9]{12})", RegexOptions.Compiled);

            foreach (string log_entry in entries) {

                bool is_merge_commit = false;

                Match match = regex.Match (log_entry);

                if (!match.Success)
                    continue;

                SparkleChangeSet change_set = new SparkleChangeSet () {
                    Revision  = match.Groups [9].Value,
                    IsMagical   = is_merge_commit
                };

                change_set.User.Name  = match.Groups [7].Value.Trim ();
                change_set.User.Email = match.Groups [8].Value;


                change_set.Timestamp = new DateTime (int.Parse (match.Groups [1].Value),
                    int.Parse (match.Groups [2].Value), int.Parse (match.Groups [3].Value),
                    int.Parse (match.Groups [4].Value), int.Parse (match.Groups [5].Value), 0);

                string [] entry_lines = log_entry.Split ("\n".ToCharArray ());

                foreach (string entry_line in entry_lines) {
                    if (!entry_line.StartsWith ("\t* "))
                        continue;

                    if (entry_line.EndsWith ("new file.")) {
                        string files = entry_line.Substring (3, entry_line.Length - 13);
                        string [] added_files = files.Split (",".ToCharArray ());

                        foreach (string added_file in added_files) {
                            string file = added_file.TrimEnd (": ".ToCharArray ());
                            change_set.Added.Add (file);
                        }

                    } else if (entry_line.EndsWith ("deleted file.")) {
                        string files = entry_line.Substring (3, entry_line.Length - 17);
                        string [] deleted_files = files.Split (",".ToCharArray ());

                        foreach (string deleted_file in deleted_files) {
                            string file = deleted_file.TrimEnd (": ".ToCharArray ());
                            change_set.Deleted.Add (file);
                        }

                    } else if (!"".Equals (entry_line.Trim ())){
                        string files = entry_line.Substring (3);
                        files = files.TrimEnd (":".ToCharArray());
                        string [] edited_files = files.Split (",".ToCharArray ());

                        foreach (string edited_file in edited_files) {
                            if (!change_set.Added.Contains (edited_file) &&
                                !change_set.Deleted.Contains (edited_file)) {

                                change_set.Edited.Add (edited_file);
                            }
                        }
                    }
                }

                change_sets.Add (change_set);
            }

            return change_sets;
        }


        // Creates a pretty commit message based on what has changed
        private string FormatCommitMessage () // TODO
        {
            return "SparkleShare Hg";
        }


        public override void CreateInitialChangeSet ()
        {
            base.CreateInitialChangeSet ();
            Add ();

            string message = FormatCommitMessage ();
            Commit (message);
        }


        public override bool UsesNotificationCenter
        {
            get {
                string file_path = SparkleHelpers.CombineMore (LocalPath, ".hg", "disable_notification_center");
                return !File.Exists (file_path);
            }
        }
    }
}
