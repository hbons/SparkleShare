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

        
        public override string EventEntryHTML {
            get {
                string path = new string [] {Defines.PREFIX,
                    "share", "sparkleshare", "html", "event-entry.html"}.Combine ();
            
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
