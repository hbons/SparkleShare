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
        private readonly string LocalPath;

        private readonly string empty_directories_file;

        private SortedSet<string> empty_directories = new SortedSet<string>();
        private bool empty_directories_changed = false;
        private object empty_directories_lock = new object();
        

        public SparkleGitConfig(string path)
        {
            LocalPath = path;
            empty_directories_file = Path.Combine (LocalPath, ".sparkleshare-git", "empty_directories");
            
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
                List<string> empty_directories_relative = new List<string>(empty_directories);
                for (int x = 0; x < empty_directories_relative.Count; ++x)
                {
                    empty_directories_relative[x] = empty_directories_relative[x].Replace(LocalPath, "");

                    if (empty_directories_relative[x][0] == Path.DirectorySeparatorChar || 
                        empty_directories_relative[x][0] == Path.AltDirectorySeparatorChar)
                    {
                        empty_directories_relative[x] = empty_directories_relative[x].Substring(1);
                    }
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
                        Thread.Sleep(0);
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
            string sparkleshare_git_folder = Path.Combine(LocalPath, ".sparkleshare-git");
            
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
