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
using System.Diagnostics;
using System.IO;

using SparkleLib;

namespace SparkleShare {

    public class SparkleController : SparkleControllerBase {

        public SparkleController () : base ()
        {
        }


        public override string PluginsPath {
            get {
                return new string [] { Defines.INSTALL_DIR, "plugins" }.Combine ();
            }
        }


        // Creates a .desktop entry in autostart folder to
        // start SparkleShare automatically at login
        public override void CreateStartupItem ()
        {
            string autostart_path = Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "autostart");

            string desktopfile_path = Path.Combine (autostart_path, "sparkleshare.desktop");

            if (!Directory.Exists (autostart_path))
                Directory.CreateDirectory (autostart_path);

            if (!File.Exists (desktopfile_path)) {
                try {
                    File.WriteAllText (desktopfile_path,
                        "[Desktop Entry]\n" +
                        "Type=Application\n" +
                        "Name=SparkleShare\n" +
                        "Exec=sparkleshare start\n" +
                        "Icon=folder-sparkleshare\n" +
                        "Terminal=false\n" +
                        "X-GNOME-Autostart-enabled=true\n" +
                        "Categories=Network");

                    SparkleLogger.LogInfo ("Controller", "Added SparkleShare to login items");

                } catch (Exception e) {
                    SparkleLogger.LogInfo ("Controller", "Failed adding SparkleShare to login items: " + e.Message);
                }
            }
        }
        
        
        public override void InstallProtocolHandler ()
        {
            // sparkleshare-invite-opener.desktop launches the handler on newer
            // systems (like GNOME 3) that implement the last freedesktop.org specs.
            // For GNOME 2 however we need to tell gconf about the protocol manually

            try {
                // Add the handler to gconf...
                Process process = new Process ();
                process.StartInfo.FileName  = "gconftool-2";
                process.StartInfo.Arguments =
                    "-s /desktop/gnome/url-handlers/sparkleshare/command 'sparkleshare open %s' --type String";

                process.Start ();
                process.WaitForExit ();

                // ...and enable it
                process.StartInfo.Arguments = "-s /desktop/gnome/url-handlers/sparkleshare/enabled --type Boolean true";

                process.Start ();
                process.WaitForExit ();

            } catch {
                // Pity...
            }
        }


        // Adds the SparkleShare folder to the user's
        // list of bookmarked places
        public override void AddToBookmarks ()
        {
            string bookmarks_file_path   = Path.Combine (SparkleConfig.DefaultConfig.HomePath, ".gtk-bookmarks");
            string sparkleshare_bookmark = "file://" + FoldersPath + " SparkleShare";

            if (File.Exists (bookmarks_file_path)) {
                string bookmarks = File.ReadAllText (bookmarks_file_path);

                if (!bookmarks.Contains (sparkleshare_bookmark))
                    File.AppendAllText (bookmarks_file_path, "file://" + FoldersPath + " SparkleShare");

            } else {
                File.WriteAllText (bookmarks_file_path, "file://" + FoldersPath + " SparkleShare");
            }
        }


        // Creates the SparkleShare folder in the user's home folder
        public override bool CreateSparkleShareFolder ()
        {
			bool folder_created = false;
			
            if (!Directory.Exists (SparkleConfig.DefaultConfig.FoldersPath)) {
            	Directory.CreateDirectory (SparkleConfig.DefaultConfig.FoldersPath);
				SparkleLogger.LogInfo ("Controller", "Created '" + SparkleConfig.DefaultConfig.FoldersPath + "'");
				
				folder_created = true;
			}

            string gvfs_command_path = new string [] { Path.VolumeSeparatorChar.ToString (),
            	"usr", "bin", "gvfs-set-attribute" }.Combine ();
			
            // Add a special icon to the SparkleShare folder
            if (File.Exists (gvfs_command_path)) {
                Process process = new Process ();
                process.StartInfo.FileName = "gvfs-set-attribute";

                // Give the SparkleShare folder an icon name, so that it scales
                process.StartInfo.Arguments = SparkleConfig.DefaultConfig.FoldersPath +
                    " metadata::custom-icon-name 'sparkleshare'";

                process.Start ();
                process.WaitForExit ();
            }

            return folder_created;
        }
        

        public override string EventLogHTML {
            get {
                string html_path = new string [] { Defines.INSTALL_DIR, "html", "event-log.html" }.Combine ();
                string jquery_file_path = new string [] { Defines.INSTALL_DIR, "html", "jquery.js" }.Combine ();

                string html   = File.ReadAllText (html_path);
                string jquery = File.ReadAllText (jquery_file_path);

                return html.Replace ("<!-- $jquery -->", jquery);
            }
        }

        
        public override string DayEntryHTML {
            get {
                string path = new string [] { Defines.INSTALL_DIR, "html", "day-entry.html" }.Combine ();
                return File.ReadAllText (path);
            }
        }

        
        public override string EventEntryHTML {
            get {
                string path = new string [] { Defines.INSTALL_DIR, "html", "event-entry.html" }.Combine ();
                return File.ReadAllText (path);
            }
        }

            
        public override void OpenFolder (string path)
        {
            OpenFile (path);
        }


        public override void OpenFile (string path)
        {
            Process process             = new Process ();
            process.StartInfo.FileName  = "xdg-open";
            process.StartInfo.Arguments = "\"" + path + "\"";
            process.Start ();
        }


		public override void OpenWebsite (string url)
		{
			OpenFile (url);
		}
    }
}
