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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SparkleLib;

namespace SparkleLib.Git {

    public class SparkleRepo : SparkleRepoBase {

        private bool user_is_set;
        private bool use_git_bin;
        private bool is_encrypted;

        private string cached_branch;

        private Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);
        private Regex speed_regex    = new Regex (@"([0-9\.]+) ([KM])iB/s", RegexOptions.Compiled);

        private Regex log_regex = new Regex (@"commit ([a-z0-9]{40})\n" +
                                              "Author: (.+) <(.+)>\n" +
                                              "*" +
                                              "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
                                              "([0-9]{2}):([0-9]{2}):([0-9]{2}) (.[0-9]{4})\n" +
                                              "*", RegexOptions.Compiled);

        private string branch {
            get {
                if (!string.IsNullOrEmpty (this.cached_branch)) 
                    return this.cached_branch;

                string rebase_apply_path = new string [] { LocalPath, ".git", "rebase-apply" }.Combine ();

                SparkleGit git = new SparkleGit (LocalPath, "config core.ignorecase true");
                git.StartAndWaitForExit ();

                while (Directory.Exists (rebase_apply_path) && HasLocalChanges) {
                    try {
                        ResolveConflict ();
                        
                    } catch (IOException e) {
                        SparkleLogger.LogInfo ("Git", Name + " | Failed to resolve conflict, trying again...", e);
                    }
                }

                git = new SparkleGit (LocalPath, "config core.ignorecase false");
                git.StartAndWaitForExit ();

                git = new SparkleGit (LocalPath, "rev-parse --abbrev-ref HEAD");
                this.cached_branch = git.StartAndReadStandardOutput ();

                return this.cached_branch;
            }
        }


        public SparkleRepo (string path, SparkleConfig config) : base (path, config)
        {
            SparkleGit git = new SparkleGit (LocalPath, "config core.ignorecase false");
            git.StartAndWaitForExit ();

            // Check if we should use git-bin
            git = new SparkleGit (LocalPath, "config --get filter.bin.clean");
            git.StartAndWaitForExit ();

            this.use_git_bin = (git.ExitCode == 0);

            if (this.use_git_bin)
                ConfigureGitBin ();

            git = new SparkleGit (LocalPath, "config remote.origin.url \"" + RemoteUrl + "\"");
            git.StartAndWaitForExit ();

            string password_file_path = Path.Combine (LocalPath, ".git", "password");

            if (File.Exists (password_file_path))
                this.is_encrypted = true;
        }


        private void ConfigureGitBin ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "config filter.bin.clean \"git bin clean %f\"");
            git.StartAndWaitForExit ();
            
            git = new SparkleGit (LocalPath, "config filter.bin.smudge \"git bin smudge\"");
            git.StartAndWaitForExit ();

            git = new SparkleGit (LocalPath, "config git-bin.sftpUrl \"" + RemoteUrl + "\"");
            git.StartAndWaitForExit ();
            
            git = new SparkleGit (LocalPath, "config git-bin.sftpPrivateKeyFile \"" + base.local_config.User.PrivateKeyFilePath + "\"");
            git.StartAndWaitForExit ();
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
                string file_path = new string [] { LocalPath, ".git", "repo_size" }.Combine ();

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
                string file_path = new string [] { LocalPath, ".git", "repo_history_size" }.Combine ();

                try {
                    string size = File.ReadAllText (file_path);
                    return double.Parse (size);

                } catch {
                    return 0;
                }
            }
        }


        private void UpdateSizes ()
        {
            double size         = CalculateSizes (new DirectoryInfo (LocalPath));
            double history_size = CalculateSizes (new DirectoryInfo (Path.Combine (LocalPath, ".git")));

            string size_file_path = new string [] { LocalPath, ".git", "repo_size" }.Combine ();
            string history_size_file_path = new string [] { LocalPath, ".git", "repo_history_size" }.Combine ();

            File.WriteAllText (size_file_path, size.ToString ());
            File.WriteAllText (history_size_file_path, history_size.ToString ());
        }


        public override string CurrentRevision {
            get {
                SparkleGit git = new SparkleGit (LocalPath, "rev-parse HEAD");
                string output  = git.StartAndReadStandardOutput ();

                if (git.ExitCode == 0)
                    return output;
                else
                    return null;
            }
        }


        public override bool HasRemoteChanges {
            get {
                SparkleLogger.LogInfo ("Git", Name + " | Checking for remote changes...");
                string current_revision = CurrentRevision;

                SparkleGit git = new SparkleGit (LocalPath, "ls-remote --heads --exit-code \"" + RemoteUrl + "\" " + this.branch);
                string output  = git.StartAndReadStandardOutput ();

                if (git.ExitCode != 0)
                    return false;

                string remote_revision = "" + output.Substring (0, 40);

                if (!remote_revision.Equals (current_revision)) {
                    git = new SparkleGit (LocalPath, "merge-base " + remote_revision + " master");
                    git.StartAndWaitForExit ();

                    if (git.ExitCode != 0) {
                        SparkleLogger.LogInfo ("Git", Name + " | Remote changes found, local: " +
                            current_revision + ", remote: " + remote_revision);

                        Error = ErrorStatus.None;
                        return true;
                    
                    } else {
                        SparkleLogger.LogInfo ("Git", Name + " | Remote " + remote_revision + " is already in our history");
                        return false;
                    }
                } 

                SparkleLogger.LogInfo ("Git", Name + " | No remote changes, local+remote: " + current_revision);
                return false;
            }
        }


        public override bool SyncUp ()
        {
            if (!Add ()) {
                Error = ErrorStatus.UnreadableFiles;
                return false;
            }

            string message = FormatCommitMessage ();

            if (message != null)
                Commit (message);
 
            if (this.use_git_bin) {
                SparkleGitBin git_bin = new SparkleGitBin (LocalPath, "push");
                git_bin.StartAndWaitForExit ();

                // TODO: Progress
            }

            SparkleGit git = new SparkleGit (LocalPath, "push --progress \"" + RemoteUrl + "\" " + this.branch);
            git.StartInfo.RedirectStandardError = true;
            git.Start ();

            double percentage = 1.0;

            while (!git.StandardError.EndOfStream) {
                string line   = git.StandardError.ReadLine ();
                Match match   = this.progress_regex.Match (line);
                double speed  = 0.0;
                double number = 0.0;

                if (match.Success) {
                    try {
                        number = double.Parse (match.Groups [1].Value, new CultureInfo ("en-US"));
                    
                    } catch (FormatException) {
                        SparkleLogger.LogInfo ("Git", "Error parsing progress: \"" + match.Groups [1] + "\"");
                    }

                    // The pushing progress consists of two stages: the "Compressing
                    // objects" stage which we count as 20% of the total progress, and
                    // the "Writing objects" stage which we count as the last 80%
                    if (line.StartsWith ("Compressing")) {
                        // "Compressing objects" stage
                        number = (number / 100 * 20);

                    } else {
                        // "Writing objects" stage
                        number = (number / 100 * 80 + 20);
                        Match speed_match = this.speed_regex.Match (line);

                        if (speed_match.Success) {
                            try {
                                speed = double.Parse (speed_match.Groups [1].Value, new CultureInfo ("en-US")) * 1024;
                            
                            } catch (FormatException) {
                                SparkleLogger.LogInfo ("Git", "Error parsing speed: \"" + speed_match.Groups [1] + "\"");
                            }

                            if (speed_match.Groups [2].Value.Equals ("M"))
                                speed = speed * 1024;
                        }    
                    }

                } else {
                    SparkleLogger.LogInfo ("Git", Name + " | " + line);

                    if (FindError (line))
                        return false;
                }

                if (number >= percentage) {
                    percentage = number;
                    base.OnProgressChanged (percentage, speed);
                }
            }

            git.WaitForExit ();
            UpdateSizes ();

            if (git.ExitCode == 0) {
                ClearCache ();

                string salt_file_path = new string [] { LocalPath, ".git", "salt" }.Combine ();

                // If the repo is encrypted, create a branch to 
                // store the salt in and push it to the host
                if (File.Exists (salt_file_path)) {
                    string salt = File.ReadAllText (salt_file_path).Trim ();

                    SparkleGit git_salt = new SparkleGit (LocalPath, "branch salt-" + salt);
                    git_salt.StartAndWaitForExit ();

                    git_salt = new SparkleGit (LocalPath, "push origin salt-" + salt);
                    git_salt.StartAndWaitForExit ();

                    File.Delete (salt_file_path);
                }

                return true;

            } else {
                Error = ErrorStatus.HostUnreachable;
                return false;
            }
        }


        public override bool SyncDown ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "fetch --progress \"" + RemoteUrl + "\" " + this.branch);

            git.StartInfo.RedirectStandardError = true;
            git.Start ();

            double percentage = 1.0;

            while (!git.StandardError.EndOfStream) {
                string line   = git.StandardError.ReadLine ();
                Match match   = this.progress_regex.Match (line);
                double speed  = 0.0;
                double number = 0.0;

                if (match.Success) {
                    try {
                        number = double.Parse (match.Groups [1].Value, new CultureInfo ("en-US"));   
                    
                    } catch (FormatException) {
                        SparkleLogger.LogInfo ("Git", "Error parsing progress: \"" + match.Groups [1] + "\"");
                    }

                    // The fetching progress consists of two stages: the "Compressing
                    // objects" stage which we count as 20% of the total progress, and
                    // the "Receiving objects" stage which we count as the last 80%
                    if (line.StartsWith ("Compressing")) {
                        // "Compressing objects" stage
                        number = (number / 100 * 20);

                    } else {
                        // "Writing objects" stage
                        number = (number / 100 * 80 + 20);
                        Match speed_match = this.speed_regex.Match (line);
                        
                        if (speed_match.Success) {
                            try {
                                speed = double.Parse (speed_match.Groups [1].Value, new CultureInfo ("en-US")) * 1024;
                                
                            } catch (FormatException) {
                                SparkleLogger.LogInfo ("Git", "Error parsing speed: \"" + speed_match.Groups [1] + "\"");
                            }
                            
                            if (speed_match.Groups [2].Value.Equals ("M"))
                                speed = speed * 1024;
                        }
                    }

                } else {
                    SparkleLogger.LogInfo ("Git", Name + " | " + line);

                    if (FindError (line))
                        return false;
                }
                

                if (number >= percentage) {
                    percentage = number;
                    base.OnProgressChanged (percentage, speed);
                }
            }

            git.WaitForExit ();
            UpdateSizes ();

            if (git.ExitCode == 0) {
                if (Rebase ()) {
                    ClearCache ();
                    return true;
                
                } else {
                    return false;
                }

            } else {
                Error = ErrorStatus.HostUnreachable;
                return false;
            }
        }


        public override bool HasLocalChanges {
            get {
                PrepareDirectories (LocalPath);

                SparkleGit git = new SparkleGit (LocalPath, "status --porcelain");
                string output  = git.StartAndReadStandardOutput ();

                return !string.IsNullOrEmpty (output);
            }
        }


        public override bool HasUnsyncedChanges {
            get {
                string unsynced_file_path =  new string [] { LocalPath, ".git", "has_unsynced_changes" }.Combine ();
                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = new string [] { LocalPath, ".git", "has_unsynced_changes" }.Combine ();

                if (value)
                    File.WriteAllText (unsynced_file_path, "");
                else
                    File.Delete (unsynced_file_path);
            }
        }


        // Stages the made changes
        private bool Add ()
        {
            SparkleGit git = new SparkleGit (LocalPath, "add --all");
            git.StartAndWaitForExit ();

            return (git.ExitCode == 0);
        }


        // Commits the made changes
        private void Commit (string message)
        {
            SparkleGit git;

            if (!this.user_is_set) {
                git = new SparkleGit (LocalPath, "config user.name \"" + base.local_config.User.Name + "\"");
                git.StartAndWaitForExit ();

                git = new SparkleGit (LocalPath, "config user.email \"" + base.local_config.User.Email + "\"");
                git.StartAndWaitForExit ();

                this.user_is_set = true;
            }

            git = new SparkleGit (LocalPath, "commit --all --message=\"" + message + "\" " +
                "--author=\"" + base.local_config.User.Name + " <" + base.local_config.User.Email + ">\"");

            git.StartAndReadStandardOutput ();
        }


        // Merges the fetched changes
        private bool Rebase ()
        {
            string message = FormatCommitMessage ();
            
            if (message != null) {
                Add ();
                Commit (message);
            }

            SparkleGit git;
            string rebase_apply_path = new string [] { LocalPath, ".git", "rebase-apply" }.Combine ();

            // Stop if we're already in a rebase because something went wrong
            if (Directory.Exists (rebase_apply_path)) {
                git = new SparkleGit (LocalPath, "rebase --abort");
                git.StartAndWaitForExit ();

                return false;
            }

            // Temporarily change the ignorecase setting to true to avoid
            // conflicts in file names due to letter case changes
            git = new SparkleGit (LocalPath, "config core.ignorecase true");
            git.StartAndWaitForExit ();

            git = new SparkleGit (LocalPath, "rebase FETCH_HEAD");
            git.StartInfo.RedirectStandardOutput = false;

            string error_output = git.StartAndReadStandardError ();

            if (git.ExitCode != 0) {
                // Stop when we can't rebase due to locked local files
                // error: cannot stat 'filename': Permission denied
                if (error_output.Contains ("error: cannot stat")) {
                    Error = ErrorStatus.UnreadableFiles;
                    SparkleLogger.LogInfo ("Git", Name + " | Error status changed to " + Error);

                    git = new SparkleGit (LocalPath, "rebase --abort");
                    git.StartAndWaitForExit ();

                    git = new SparkleGit (LocalPath, "config core.ignorecase false");
                    git.StartAndWaitForExit ();

                    return false;
                
                } else {
                    SparkleLogger.LogInfo ("", error_output);
                    SparkleLogger.LogInfo ("Git", Name + " | Conflict detected, trying to get out...");
                    
                    while (Directory.Exists (rebase_apply_path) && HasLocalChanges) {
                        try {
                            ResolveConflict ();

                        } catch (IOException e) {
                            SparkleLogger.LogInfo ("Git", Name + " | Failed to resolve conflict, trying again...", e);
                        }
                    }

                    SparkleLogger.LogInfo ("Git", Name + " | Conflict resolved");
                    OnConflictResolved ();
                }
            }

            git = new SparkleGit (LocalPath, "config core.ignorecase false");
            git.StartAndWaitForExit ();

            return true;
        }


        private void ResolveConflict ()
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
            //
            // Note that a rebase merge works by replaying each commit from the working branch on
            // top of the upstream branch. Because of this, when a merge conflict happens the
            // side reported as 'ours' is the so-far rebased series, starting with upstream,
            // and 'theirs' is the working branch. In other words, the sides are swapped.
            //
            // So: 'ours' means the 'server's version' and 'theirs' means the 'local version' after this comment

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            string output         = git_status.StartAndReadStandardOutput ();

            string [] lines = output.Split ("\n".ToCharArray ());
            bool changes_added = false;

            foreach (string line in lines) {
                string conflicting_path = line.Substring (3);
                conflicting_path        = EnsureSpecialCharacters (conflicting_path);
                conflicting_path        = conflicting_path.Trim ("\"".ToCharArray ());

                SparkleLogger.LogInfo ("Git", Name + " | Conflict type: " + line);

                // Ignore conflicts in the .sparkleshare file and use the local version
                if (conflicting_path.EndsWith (".sparkleshare") || conflicting_path.EndsWith (".empty")) {
                    SparkleLogger.LogInfo ("Git", Name + " | Ignoring conflict in special file: " + conflicting_path);

                    // Recover local version
                    SparkleGit git_theirs = new SparkleGit (LocalPath, "checkout --theirs \"" + conflicting_path + "\"");
                    git_theirs.StartAndWaitForExit ();

                    File.SetAttributes (Path.Combine (LocalPath, conflicting_path), FileAttributes.Hidden);
                    changes_added = true;

                    continue;
                }

                SparkleLogger.LogInfo ("Git", Name + " | Resolving: " + line);

                // Both the local and server version have been modified
                if (line.StartsWith ("UU") || line.StartsWith ("AA") ||
                    line.StartsWith ("AU") || line.StartsWith ("UA")) {

                    // Recover local version
                    SparkleGit git_theirs = new SparkleGit (LocalPath, "checkout --theirs \"" + conflicting_path + "\"");
                    git_theirs.StartAndWaitForExit ();

                    // Append a timestamp to local version.
                    // Windows doesn't allow colons in the file name, so
                    // we use "h" between the hours and minutes instead.
                    string timestamp  = DateTime.Now.ToString ("MMM d H\\hmm");
                    string their_path = Path.GetFileNameWithoutExtension (conflicting_path) +
                        " (" + base.local_config.User.Name + ", " + timestamp + ")" + Path.GetExtension (conflicting_path);

                    string abs_conflicting_path = Path.Combine (LocalPath, conflicting_path);
                    string abs_their_path       = Path.Combine (LocalPath, their_path);

                    if (File.Exists (abs_conflicting_path) && !File.Exists (abs_their_path))
                        File.Move (abs_conflicting_path, abs_their_path);

                    // Recover server version
                    SparkleGit git_ours = new SparkleGit (LocalPath, "checkout --ours \"" + conflicting_path + "\"");
                    git_ours.StartAndWaitForExit ();

                    changes_added = true;

                // The local version has been modified, but the server version was removed
                } else if (line.StartsWith ("DU")) {

                    // The modified local version is already in the checkout, so it just needs to be added.
                    // We need to specifically mention the file, so we can't reuse the Add () method
                    SparkleGit git_add = new SparkleGit (LocalPath, "add \"" + conflicting_path + "\"");
                    git_add.StartAndWaitForExit ();

                    changes_added = true;
                
                // The server version has been modified, but the local version was removed
                } else if (line.StartsWith ("UD")) {
                    
                    // Recover server version
                    SparkleGit git_theirs = new SparkleGit (LocalPath, "checkout --ours \"" + conflicting_path + "\"");
                    git_theirs.StartAndWaitForExit ();

                    changes_added = true;

                // Server and local versions were removed
                } else if (line.StartsWith ("DD")) {
                    SparkleLogger.LogInfo ("Git", Name + " | No need to resolve: " + line);

                // New local files
                } else if (line.StartsWith ("??")) {
                    SparkleLogger.LogInfo ("Git", Name + " | Found new file, no need to resolve: " + line);
                    changes_added = true;
                
                } else {
                    SparkleLogger.LogInfo ("Git", Name + " | Don't know what to do with: " + line);
                }
            }

            Add ();
            SparkleGit git;

            if (changes_added)
                git = new SparkleGit (LocalPath, "rebase --continue");
            else
                git = new SparkleGit (LocalPath, "rebase --skip");

            git.StartInfo.RedirectStandardOutput = false;
            git.StartAndWaitForExit ();
        }


        public override void RestoreFile (string path, string revision, string target_file_path)
        {
            if (path == null)
                throw new ArgumentNullException ("path");

            if (revision == null)
                throw new ArgumentNullException ("revision");

            SparkleLogger.LogInfo ("Git", Name + " | Restoring \"" + path + "\" (revision " + revision + ")");

            // git-show doesn't decrypt objects, so we can't use it to retrieve
            // files from the index. This is a suboptimal workaround but it does the job
            if (this.is_encrypted) {
                // Restore the older file...
                SparkleGit git = new SparkleGit (LocalPath, "checkout " + revision + " \"" + path + "\"");
                git.StartAndWaitForExit ();

                string local_file_path = Path.Combine (LocalPath, path);

                // ...move it...
                try {
                    File.Move (local_file_path, target_file_path);
                
                } catch {
                    SparkleLogger.LogInfo ("Git",
                        Name + " | Could not move \"" + local_file_path + "\" to \"" + target_file_path + "\"");
                }

                // ...and restore the most recent revision
                git = new SparkleGit (LocalPath, "checkout " + CurrentRevision + " \"" + path + "\"");
                git.StartAndWaitForExit ();
            
            // The correct way
            } else {
                path = path.Replace ("\"", "\\\"");

                SparkleGit git = new SparkleGit (LocalPath, "show " + revision + ":\"" + path + "\"");
                git.Start ();

                FileStream stream = File.OpenWrite (target_file_path);    
                git.StandardOutput.BaseStream.CopyTo (stream);
                stream.Close ();

                git.WaitForExit ();
            }

            if (target_file_path.StartsWith (LocalPath))
                new Thread (() => OnFileActivity (null)).Start ();
        }


        private bool FindError (string line)
        {
            Error = ErrorStatus.None;

            if (line.Contains ("WARNING: REMOTE HOST IDENTIFICATION HAS CHANGED!") ||
                line.Contains ("WARNING: POSSIBLE DNS SPOOFING DETECTED!")) {
                
                Error = ErrorStatus.HostIdentityChanged;
                
            } else if (line.StartsWith ("Permission denied") ||
                       line.StartsWith ("ssh_exchange_identification: Connection closed by remote host")) {

                Error = ErrorStatus.AuthenticationFailed;

            } else if (line.EndsWith ("does not appear to be a git repository")) {
                Error = ErrorStatus.NotFound;            
                
            } else if (line.StartsWith ("error: Disk space exceeded") ||
                       line.EndsWith ("No space left on device")) {

                Error = ErrorStatus.DiskSpaceExceeded;
            }

            if (Error != ErrorStatus.None) {
                SparkleLogger.LogInfo ("Git", Name + " | Error status changed to " + Error);
                return true;
            
            } else {
                return false;
            }
        }


        public override List<SparkleChangeSet> GetChangeSets ()
        {
            return GetChangeSetsInternal (null);
        }

        public override List<SparkleChangeSet> GetChangeSets (string path)
        {
            return GetChangeSetsInternal (path);
        }   

        private List<SparkleChangeSet> GetChangeSetsInternal (string path)
        {
            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();
            SparkleGit git;

            if (path == null) {
                git = new SparkleGit (LocalPath, "log --since=1.month --raw --find-renames --date=iso " +
                    "--format=medium --no-color --no-merges");

            } else {
                path = path.Replace ("\\", "/");

                git = new SparkleGit (LocalPath, "log --raw --find-renames --date=iso " +
                    "--format=medium --no-color --no-merges -- \"" + path + "\"");
            }

            string output = git.StartAndReadStandardOutput ();

            if (path == null && string.IsNullOrWhiteSpace (output)) {
                git = new SparkleGit (LocalPath, "log -n 75 --raw --find-renames --date=iso " +
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

                if (!match.Success)
                    continue;

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
                        SparkleLogger.LogInfo ("Local", "Error parsing file name '" + file_path + "'", e);
                        continue;
                    }

                    file_path = file_path.Replace ("\\\"", "\"");

                    SparkleChange change = new SparkleChange () {
                        Path      = file_path,
                        IsFolder  = change_is_folder,
                        Timestamp = change_set.Timestamp,
                        Type      = SparkleChangeType.Added
                    };

                    if (type_letter.Equals ("R")) {
                        int tab_pos         = entry_line.LastIndexOf ("\t");
                        file_path           = entry_line.Substring (42, tab_pos - 42);
                        string to_file_path = entry_line.Substring (tab_pos + 1);

                        try {
                            file_path = EnsureSpecialCharacters (file_path);
                            
                        } catch (Exception e) {
                            SparkleLogger.LogInfo ("Local", "Error parsing file name '" + file_path + "'", e);
                            continue;
                        }

                        try {
                            to_file_path = EnsureSpecialCharacters (to_file_path);

                        } catch (Exception e) {
                            SparkleLogger.LogInfo ("Local", "Error parsing file name '" + to_file_path + "'", e);
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
                        change.Type        = SparkleChangeType.Moved;

                    } else if (type_letter.Equals ("M")) {
                        change.Type = SparkleChangeType.Edited;

                    } else if (type_letter.Equals ("D")) {
                        change.Type = SparkleChangeType.Deleted;
                    }

                    change_set.Changes.Add (change);
                }

                // Group commits per user, per day
                if (change_sets.Count > 0 && path == null) {
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
                    // Don't show removals or moves in the revision list of a file
                    if (path != null) {
                        List<SparkleChange> changes_to_skip = new List<SparkleChange> ();

                        foreach (SparkleChange change in change_set.Changes) {
                            if ((change.Type == SparkleChangeType.Deleted || change.Type == SparkleChangeType.Moved)
                                && change.Path.Equals (path)) {

                                changes_to_skip.Add (change);
                            }
                        }

                        foreach (SparkleChange change_to_skip in changes_to_skip)
                            change_set.Changes.Remove (change_to_skip);
                    }
                                    
                    change_sets.Add (change_set);
                }
            }

            return change_sets;
        }


        private string EnsureSpecialCharacters (string path)
        {
            // The path is quoted if it contains special characters
            if (path.StartsWith ("\""))
                path = ResolveSpecialChars (path.Substring (1, path.Length - 2));

            return path;
        }


        private string ResolveSpecialChars (string s)
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


        private void ClearCache ()
        {
            if (!this.use_git_bin)
                return;

            SparkleGitBin git_bin = new SparkleGitBin (LocalPath, "clear -f");
            git_bin.StartAndWaitForExit ();
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
                    if (IsSymlink (child_path))
                        continue;

                    if (child_path.EndsWith (".git")) {
                        if (child_path.Equals (Path.Combine (LocalPath, ".git")))
                            continue;
    
                        string HEAD_file_path = Path.Combine (child_path, "HEAD");
    
                        if (File.Exists (HEAD_file_path)) {
                            File.Move (HEAD_file_path, HEAD_file_path + ".backup");
                            SparkleLogger.LogInfo ("Git", Name + " | Renamed " + HEAD_file_path);
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
                            SparkleLogger.LogInfo ("Git", Name + " | Failed adding empty folder " + path);
                        }
                    }
                }

            } catch (IOException e) {
                SparkleLogger.LogInfo ("Git", "Failed preparing directory", e);
            }
        }


        // Creates a pretty commit message based on what has changed
        private string FormatCommitMessage ()
        {
            int count      = 0;
            string message = "";

            SparkleGit git_status = new SparkleGit (LocalPath, "status --porcelain");
            git_status.Start ();

            while (!git_status.StandardOutput.EndOfStream) {
                string line = git_status.StandardOutput.ReadLine ();
                line        = line.Trim ();

                if (line.EndsWith (".empty") || line.EndsWith (".empty\""))
                    line = line.Replace (".empty", "");

                if (line.StartsWith ("R")) {
                    string path = line.Substring (3, line.IndexOf (" -> ") - 3).Trim ("\"".ToCharArray ());
                    string moved_to_path = line.Substring (line.IndexOf (" -> ") + 4).Trim ("\"".ToCharArray ());

                    message +=  "< ‘" + EnsureSpecialCharacters (path) + "’\n";
                    message +=  "> ‘" + EnsureSpecialCharacters (moved_to_path) + "’\n";

                } else {
                    if (line.StartsWith ("M")) {
                        message += "/";

                    } else if (line.StartsWith ("D")) {
                        message += "-";

                    } else {
                        message += "+";
                    }

                    string path = line.Substring (3).Trim ("\"".ToCharArray ());
                    message += " ‘" + EnsureSpecialCharacters (path) + "’\n";
                }

                count++;
                if (count == 10) {
                    message += "...\n";
                    break;
                }
            }

            git_status.StandardOutput.ReadToEnd ();
            git_status.WaitForExit ();

            if (string.IsNullOrWhiteSpace (message))
                return null;
            else
                return message;
        }


        // Recursively gets a folder's size in bytes
        private long CalculateSizes (DirectoryInfo parent)
        {
            long size = 0;

            try {
                foreach (DirectoryInfo directory in parent.GetDirectories ()) {
                    if (directory.IsSymlink () ||
                        directory.Name.Equals (".git") || 
                        directory.Name.Equals ("rebase-apply")) {

                        continue;
                    }

                    size += CalculateSizes (directory);
                }

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Local", "Error calculating directory size", e);
            }

            try {
                foreach (FileInfo file in parent.GetFiles ()) {
                    if (file.IsSymlink ())
                        continue;
                    
                    if (file.Name.Equals (".empty"))
                        File.SetAttributes (file.FullName, FileAttributes.Hidden);
                    else
                        size += file.Length;
                }
                
            } catch (Exception e) {
                SparkleLogger.LogInfo ("Local", "Error calculating file size", e);
            }

            return size;
        }

        
        private bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }
    }
}
