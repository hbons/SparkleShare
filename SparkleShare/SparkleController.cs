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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Mono.Unix;
using SparkleLib;

namespace SparkleShare {

    public class SparkleController : SparkleControllerBase {

        public SparkleController () : base ()
        {
        }


        // Creates a .desktop entry in autostart folder to
        // start SparkleShare automatically at login
        public override void EnableSystemAutostart ()
        {
            string autostart_path = Path.Combine (Environment.GetFolderPath (
                Environment.SpecialFolder.ApplicationData), "autostart");

            string desktopfile_path = Path.Combine (autostart_path, "sparkleshare.desktop");

            if (!Directory.Exists (autostart_path))
                Directory.CreateDirectory (autostart_path);

            if (!File.Exists (desktopfile_path)) {
                TextWriter writer = new StreamWriter (desktopfile_path);
                writer.WriteLine ("[Desktop Entry]\n" +
                                  "Type=Application\n" +
                                  "Name=SparkleShare\n" +
                                  "Exec=sparkleshare start\n" +
                                  "Icon=folder-sparkleshare\n" +
                                  "Terminal=false\n" +
                                  "X-GNOME-Autostart-enabled=true\n" +
                                  "Categories=Network");
                writer.Close ();

                // Give the launcher the right permissions so it can be launched by the user
                UnixFileInfo file_info = new UnixFileInfo (desktopfile_path);
                file_info.Create (FileAccessPermissions.UserReadWriteExecute);

                SparkleHelpers.DebugInfo ("Controller", "Enabled autostart on login");
            }
        }
        

        // Installs a launcher so the user can launch SparkleShare
        // from the Internet category if needed
        public override void InstallLauncher ()
        {
            if (folder_path.Equals (SparkleConfig.DefaultConfig.TmpPath))
                return;

            string folder_name = Path.GetFileName (folder_path);
            string backend = SparkleConfig.DefaultConfig.GetBackendForFolder (folder_name);

            if (backend == null)
                return;
            
            SparkleRepoBase repo = null;

           if (backend.Equals ("Hg"))
                repo = new SparkleRepoHg (folder_path, new SparkleBackendHg ());
			
	   else if (backend.Equals ("Unison"))
                repo = new SparkleRepoUnison (folder_path, new SparkleBackendUnison ());

            string apps_path = 
                new string [] {SparkleConfig.DefaultConfig.HomePath,
                    ".local", "share", "applications"}.Combine ();


            string desktopfile_path = Path.Combine (apps_path, "sparkleshare.desktop");

            if (!File.Exists (desktopfile_path)) {
                if (!Directory.Exists (apps_path))
                    Directory.CreateDirectory (apps_path);

                TextWriter writer = new StreamWriter (desktopfile_path);
                writer.WriteLine ("[Desktop Entry]\n" +
                                  "Type=Application\n" +
                                  "Name=SparkleShare\n" +
                                  "Comment=Share documents\n" +
                                  "Exec=sparkleshare start\n" +
                                  "Icon=folder-sparkleshare\n" +
                                  "Terminal=false\n" +
                                  "Categories=Network;");
                writer.Close ();

                // Give the launcher the right permissions so it can be launched by the user
                UnixFileInfo file_info = new UnixFileInfo (desktopfile_path);
                file_info.FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute;

                SparkleHelpers.DebugInfo ("Controller", "Created '" + desktopfile_path + "'");
            }
        }


        // Adds the SparkleShare folder to the user's
        // list of bookmarked places
        public override void AddToBookmarks ()
        {
            string bookmarks_file_path   = Path.Combine (SparkleConfig.DefaultConfig.HomePath, ".gtk-bookmarks");
            string sparkleshare_bookmark = "file://" + SparkleConfig.DefaultConfig.FoldersPath + " SparkleShare";

            if (File.Exists (bookmarks_file_path)) {
                StreamReader reader = new StreamReader (bookmarks_file_path);
                string bookmarks = reader.ReadToEnd ();
                reader.Close ();

                if (!bookmarks.Contains (sparkleshare_bookmark)) {
                    TextWriter writer = File.AppendText (bookmarks_file_path);
                    writer.WriteLine ("file://" + SparkleConfig.DefaultConfig.FoldersPath + " SparkleShare");
                    writer.Close ();
                }
            } else {
                StreamWriter writer = new StreamWriter (bookmarks_file_path);
                writer.WriteLine ("file://" + SparkleConfig.DefaultConfig.FoldersPath + " SparkleShare");
                writer.Close ();
            }
        }


        // Creates the SparkleShare folder in the user's home folder
        public override bool CreateSparkleShareFolder ()
        {
            if (!Directory.Exists (SparkleConfig.DefaultConfig.FoldersPath)) {
        
                Directory.CreateDirectory (SparkleConfig.DefaultConfig.FoldersPath);
                SparkleHelpers.DebugInfo ("Controller", "Created '" + SparkleConfig.DefaultConfig.FoldersPath + "'");

                string gvfs_command_path =
                    new string [] {Path.VolumeSeparatorChar.ToString (),
                        "usr", "bin", "gvfs-set-attribute"}.Combine ();

                // Add a special icon to the SparkleShare folder
                if (File.Exists (gvfs_command_path)) {
                    Process process = new Process ();

                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute        = false;
                    process.StartInfo.FileName               = "gvfs-set-attribute";

                    // Clear the custom (legacy) icon path
                    process.StartInfo.Arguments = "-t unset " + SparkleConfig.DefaultConfig.FoldersPath + " metadata::custom-icon";
                    process.Start ();
                    process.WaitForExit ();

                    // Give the SparkleShare folder an icon name, so that it scales
                    process.StartInfo.Arguments = SparkleConfig.DefaultConfig.FoldersPath + " metadata::custom-icon-name 'folder-sparkleshare'";
                    process.Start ();
                    process.WaitForExit ();
                }

                return true;
            }

            return false;
        }
        

        public override string EventLogHTML {
            get {
                string path = new string [] {Defines.PREFIX,
                    "share", "sparkleshare", "html", "event-log.html"}.Combine ();

                string html = String.Join (Environment.NewLine, File.ReadAllLines (path));

                html = html.Replace ("<!-- $jquery-url -->", "file://" +
                  new string [] {Defines.PREFIX, "share", "sparkleshare", "html", "jquery.js"}.Combine ());
            
                return html;
            }
        }

        
        public override string DayEntryHTML {
            get {
                string path = new string [] {Defines.PREFIX,
                    "share", "sparkleshare", "html", "day-entry.html"}.Combine ();
            
                return String.Join (Environment.NewLine, File.ReadAllLines (path));
            }
        }

<<<<<<< HEAD

        public string GetAvatar (string email, int size)
        {
            string avatar_file_path = SparkleHelpers.CombineMore (
                Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath), "icons",
                size + "x" + size, "status", "avatar-" + email);

            return avatar_file_path;
        }


        public void FetchFolder (string server, string remote_folder)
        {
            server = server.Trim ();
            remote_folder = remote_folder.Trim ();

            string tmp_path = SparkleConfig.DefaultConfig.TmpPath;
            if (!Directory.Exists (tmp_path))
                Directory.CreateDirectory (tmp_path);

            // Strip the '.git' from the name
            string canonical_name = Path.GetFileNameWithoutExtension (remote_folder);
            string tmp_folder     = Path.Combine (tmp_path, canonical_name);

            SparkleFetcherBase fetcher = null;
            string backend = null;

/*            if (remote_folder.EndsWith (".hg")) {
                remote_folder = remote_folder.Substring (0, (remote_folder.Length - 3));
                fetcher       = new SparkleFetcherHg (server, remote_folder, tmp_folder);
                backend       = "Hg";

            } else if (remote_folder.EndsWith (".scp")) {
                remote_folder = remote_folder.Substring (0, (remote_folder.Length - 4));
                fetcher = new SparkleFetcherScp (server, remote_folder, tmp_folder);
                backend = "Scp";
				
		    } else if (remote_folder.EndsWith (".unison")) {
                remote_folder = remote_folder.Substring (0, (remote_folder.Length - 7));
                fetcher = new SparkleFetcherUnison (server, remote_folder, tmp_folder);
                backend = "Unison";

            } else {*/
                fetcher = new SparkleFetcherGit (server, remote_folder, tmp_folder);
                backend = "Git";
            //}

            bool target_folder_exists = Directory.Exists (
                Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, canonical_name));

            // Add a numbered suffix to the nameif a folder with the same name
            // already exists. Example: "Folder (2)"
            int i = 1;
            while (target_folder_exists) {
                i++;
                target_folder_exists = Directory.Exists (
                    Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, canonical_name + " (" + i + ")"));
            }

            string target_folder_name = canonical_name;
            if (i > 1)
                target_folder_name += " (" + i + ")";

            fetcher.Finished += delegate {

                // Needed to do the moving
                SparkleHelpers.ClearAttributes (tmp_folder);
                string target_folder_path = Path.Combine (
                    SparkleConfig.DefaultConfig.FoldersPath, target_folder_name);

                try {
                    Directory.Move (tmp_folder, target_folder_path);
                } catch (Exception e) {
                    SparkleHelpers.DebugInfo ("Controller", "Error moving folder: " + e.Message);
                }

                SparkleConfig.DefaultConfig.AddFolder (target_folder_name, fetcher.RemoteUrl, backend);
                AddRepository (target_folder_path);

                if (FolderFetched != null)
                    FolderFetched ();

                FolderSize = GetFolderSize ();

                if (FolderSizeChanged != null)
                    FolderSizeChanged (FolderSize);

                if (FolderListChanged != null)
                    FolderListChanged ();

                fetcher.Dispose ();

                if (Directory.Exists (tmp_path))
                    Directory.Delete (tmp_path, true);
            };


            fetcher.Failed += delegate {
                if (FolderFetchError != null)
                    FolderFetchError ();

                fetcher.Dispose ();

                if (Directory.Exists (tmp_path))
                    Directory.Delete (tmp_path, true);
            };


            fetcher.Start ();
        }


        // Creates an MD5 hash of input
        private string GetMD5 (string s)
        {
            MD5 md5 = new MD5CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encoded_bytes = md5.ComputeHash (bytes);
            return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
        }


        // Checks whether there are any folders syncing and
        // quits if safe
        public void TryQuit ()
        {
            foreach (SparkleRepoBase repo in Repositories) {
                if (repo.Status == SyncStatus.SyncUp   ||
                    repo.Status == SyncStatus.SyncDown ||
                    repo.IsBuffering) {

                    if (OnQuitWhileSyncing != null)
                        OnQuitWhileSyncing ();
                    
                    return;
                }
            }
=======
        
        public override string EventEntryHTML {
            get {
                string path = new string [] {Defines.PREFIX,
                    "share", "sparkleshare", "html", "event-entry.html"}.Combine ();
>>>>>>> upstream/master
            
                return String.Join (Environment.NewLine, File.ReadAllLines (path));
            }
        }

            
        public override void OpenSparkleShareFolder (string subfolder)
        {
            string folder = Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, subfolder);

            Process process = new Process ();
            process.StartInfo.FileName  = "xdg-open";
            process.StartInfo.Arguments = "\"" + folder + "\"";
            process.Start ();
        }
    }
}
