using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace SparkleLib.Git
{
    class SparkleGitConfig
    {
        public readonly string MetadataPath;

        private readonly string LocalPath;

        private readonly string empty_directories_file;

        private SortedSet<string> empty_directories = new SortedSet<string>();
        private object empty_directories_lock = new object();
        

        public SparkleGitConfig(string path)
        {
            LocalPath = path;
            MetadataPath = Path.Combine(LocalPath, ".sparkleshare-git");
            empty_directories_file = Path.Combine (MetadataPath, "empty_directories");
            
            // Update repo information files
            UpgradeRepoInfo ();

            // Load repo information files
            LoadRepoInfo();
        }


        public void LoadRepoInfo()
        {
            lock (empty_directories_lock)
            {
                LinkedList<string> last_empty_directories = new LinkedList<string>(empty_directories);
                empty_directories.Clear();

                foreach (string line in File.ReadAllLines (empty_directories_file))
                    empty_directories.Add (Path.Combine (LocalPath, line));

                foreach (string last_empty_directory in last_empty_directories)
                {
                    if (! empty_directories.Contains (last_empty_directory) && Directory.Exists (last_empty_directory))
                        Directory.Delete (last_empty_directory);
                }

                CreateEmptyDirectories ();
            }
        }

        public void SaveRepoInfo()
        {
            lock (empty_directories_lock)
            {
                // Remove the empty directories that no longer exist
                LinkedList<string> deleted_empty_directories = new LinkedList<string>();
                foreach (string empty_directory in empty_directories)
                {
                    if (!Directory.Exists (empty_directory))
                        deleted_empty_directories.AddLast(empty_directory);
                }

                foreach (string empty_directory in deleted_empty_directories)
                    empty_directories.Remove(empty_directory);

                // Replace LocalPath in all empty directories, only save relative paths to file
                //   Also convert all \s to / and remove leading if needed
                List<string> empty_directories_relative = new List<string>(empty_directories);
                for (int x = 0; x < empty_directories_relative.Count; ++x)
                {
                    empty_directories_relative[x] = empty_directories_relative[x].Replace(LocalPath, "").Replace ("\\", "/");

                    if (empty_directories_relative[x][0] == '/')
                        empty_directories_relative[x] = empty_directories_relative[x].Substring(1);
                }

                
                bool isSaved = true;

                // Verify we need to write out the data
                string[] old_empty_directories = File.ReadAllLines (empty_directories_file);
                if (old_empty_directories.Length == empty_directories_relative.Count)
                {
                    for (int x = 0; x < empty_directories_relative.Count; ++x)
                    {
                        if (old_empty_directories[x] != empty_directories_relative[x])
                        {
                            isSaved = false;
                            break;
                        }
                    }
                } else {
                    isSaved = false;
                }

                // Save data if needed, ensure it gets written
                while (! isSaved)
                {
                    try {
                        // Save empty directories list
                        File.WriteAllLines (empty_directories_file, empty_directories_relative);
                        isSaved = true;
                    } catch (Exception) {
                        Thread.Yield ();
                    }
                }
            }

        }


        public void AddEmptyDirectory (string path)
        {
            lock (empty_directories_lock)
                empty_directories.Add(path);
        }

        public void RemoveEmptyDirectory (string path)
        {
            // Do not have to check for it exising since we use a sorted set
            lock (empty_directories_lock)
                empty_directories.Remove(path);
        }

        public List<SparkleChange> GetChanges (string path, string commit, string type_letter, string file_path, DateTime change_set_timestamp)
        {
            List<SparkleChange> file_changes = new List<SparkleChange> ();

            if (file_path == ".sparkleshare-git/empty_directories" && (type_letter.Equals ("A") || type_letter.Equals ("M"))) {

                // Get log to determine fill vs deletion
                SparkleGit git_log;
                if (path == null) {
                    git_log = new SparkleGit (LocalPath, "log --raw --find-renames --date=iso " +
                        "--format=medium --no-color --no-merges " + commit + "~1.." + commit);
                } else {
                    path = path.Replace ("\\", "/");
                    git_log = new SparkleGit (LocalPath, "log --raw --find-renames --date=iso " +
                        "--format=medium --no-color --no-merges " + commit + "~1.." + commit + " -- \"" + path + "\"");
                }

                string log_output   = git_log.StartAndReadStandardOutput();
                string [] log_lines = log_output.Split("\n".ToCharArray());

                // Get diff to determine what empty directories were added/deleted                
                SparkleGit git_diff = new SparkleGit (LocalPath, "diff --minimal " + commit + "~1 " + commit +
                    " -- \"" + file_path + "\"");
                
                string diff_output   = git_diff.StartAndReadStandardOutput ();
                string [] diff_lines = diff_output.Split ("\n".ToCharArray ());

                // Process through to the actual line changes
                bool within_changes = false;
                foreach (string diff_line in diff_lines) {

                    // Wait until we find a change boundary marker
                    if (diff_line.StartsWith ("@@") && diff_line.EndsWith ("@@")) {
                        within_changes = true;

                    } else if (within_changes) {
                        // Actually at a change line, require it start with +/-
                        if (diff_line.StartsWith ("+") || diff_line.StartsWith("-")) {
                            string empty_folder = diff_line.Substring (1).TrimEnd ();

                            // Remember if a file within the folder is added or removed.  This will help us determine filling the folder vs emptying the folder
                            bool file_added   = false;
                            bool file_removed = false;
                            foreach (string log_line in log_lines) {       
                                // Break out if both happened, no need to keep looking
                                if (file_added && file_removed)
                                    break;
                                
                                // Check for file being added or deleted
                                if (log_line.StartsWith (":") && log_line[37] == 'A' && log_line.Substring (39).StartsWith (empty_folder))
                                    file_added = true;
                                else if (log_line.StartsWith (":") && log_line[37] == 'D' && log_line.Substring (39).StartsWith (empty_folder))
                                    file_removed = true;
                                
                            }

                            foreach (string inner_diff_line in diff_lines) {
                                // Break out if both happened, no need to keep looking
                                if (file_added && file_removed)
                                    break;
                                
                                // Don't count our current line
                                if (inner_diff_line == diff_line)
                                    continue;

                                // Check for folder being added or deleted
                                if (inner_diff_line.StartsWith ("+") && inner_diff_line.Substring (1).StartsWith (empty_folder))
                                    file_added = true;
                                else if (inner_diff_line.StartsWith ("-") && inner_diff_line.Substring (1).StartsWith (empty_folder))
                                    file_removed = true;
                            }

                            if (diff_line.StartsWith ("+") ) {
                                // Folder was either emptied or created
                                if (! file_removed) {
                                    // Empty folder created
                                    SparkleChange add_change = new SparkleChange() {
                                        Type = SparkleChangeType.Added,
                                        Path = empty_folder,
                                        Timestamp = change_set_timestamp
                                    };
                                            
                                    file_changes.Add(add_change);
                                }
                            } else {
                                // Folder was either filled or deleted
                                if (! file_added) {
                                    // Empty folder deleted
                                    SparkleChange deleted_change = new SparkleChange() {
                                        Type = SparkleChangeType.Deleted,
                                        Path = empty_folder,
                                        Timestamp = change_set_timestamp
                                    };

                                    file_changes.Add(deleted_change);
                                }
                            }
                        }
                    }
                }
            }

            return file_changes;
        }


        private void CreateEmptyDirectories ()
        {
            lock (empty_directories_lock)
            {
                foreach (string empty_directory in empty_directories)
                {
                    if (!Directory.Exists (empty_directory))
                        Directory.CreateDirectory (empty_directory);
                }
            }
        }



        private void UpgradeRepoInfo ()
        {
            string sparkleshare_git_folder = MetadataPath;
            
            // Create information folder if it doesn't exist
            if (!Directory.Exists(sparkleshare_git_folder))
                Directory.CreateDirectory(sparkleshare_git_folder);

            // Set folder hidden if it is not
            if ((File.GetAttributes (sparkleshare_git_folder) & FileAttributes.Hidden) != FileAttributes.Hidden)
                File.SetAttributes (sparkleshare_git_folder, File.GetAttributes (sparkleshare_git_folder) | FileAttributes.Hidden);

            // Assume new format
            int current_repo_version = 0;

            // Read version from version file if exists
            string sparkleshare_git_version_file = Path.Combine(sparkleshare_git_folder, "version");
            if (File.Exists(sparkleshare_git_version_file)) {
                string version_string = File.ReadAllText (sparkleshare_git_version_file);
                
                // Try to parse the version
                try {
                    current_repo_version = int.Parse(version_string, new CultureInfo("en-US"));
                } catch {
                    File.Delete (sparkleshare_git_version_file);
                }
            }

            // Initial version of the repo information
            if (current_repo_version == 0) {
                // Create empty directories file
                string sparkleshare_git_empty_directories_file = Path.Combine(sparkleshare_git_folder, "empty_directories");
                if (!File.Exists(sparkleshare_git_empty_directories_file))
                    File.Create (sparkleshare_git_empty_directories_file);



                // Scan for .empty files, remove file and add to empty directories
                Stack<string> scan_directories = new Stack<string>();
                scan_directories.Push(LocalPath);

                
                // Loop through while we have directories to check, save emptyDirectories
                SortedSet<string> emptyDirectories = new SortedSet<string>(File.ReadAllLines(sparkleshare_git_empty_directories_file));

                while (scan_directories.Count > 0) {
                    string current_directory = scan_directories.Pop();

                    // Find subdirectories that need scanning
                    string[] scan_subdirectories = Directory.GetDirectories(current_directory);
                    foreach (string scan_subdirectory in scan_subdirectories)
                        scan_directories.Push(scan_subdirectory);
                    
                    // Find files that need scanning
                    string[] scan_files = Directory.GetFiles(current_directory);
                    foreach (string scan_file in scan_files) {
                        if (Path.GetFileName(scan_file) == ".empty") {
                            // If this is an end directory, add it to the empty directories file.
                            if (scan_subdirectories.Length == 0 && scan_files.Length == 1)
                                emptyDirectories.Add(current_directory);

                            // Remove the .empty file reguardless
                            File.Delete(scan_file);
                        }
                    }
                }

                // Write out the empty directories
                File.WriteAllLines (sparkleshare_git_empty_directories_file, emptyDirectories);

                current_repo_version = 1;
            }

            // Write out the final version we updated to
            File.WriteAllText (sparkleshare_git_version_file, current_repo_version.ToString(new CultureInfo("en-US")));
        }


    }
}
