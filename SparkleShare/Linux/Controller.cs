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

using Gtk;
using Mono.Unix.Native;

using Sparkles;

namespace SparkleShare {

    public class Controller : BaseController {

        public Controller ()
        {
        }


        public override string PresetsPath {
            get {
                return Path.Combine (InstallationInfo.Directory, "presets");
            }
        }


        // Creates a .desktop entry in autostart folder to
        // start SparkleShare automatically at login
        public override void CreateStartupItem ()
        {
            string autostart_path      = Path.Combine (Config.HomePath, ".config", "autostart");
            string autostart_file_path = Path.Combine (autostart_path, "org.sparkleshare.SparkleShare.Autostart.desktop");

            if (File.Exists (autostart_file_path))
                return;
 
            if (!Directory.Exists (autostart_path))
                Directory.CreateDirectory (autostart_path);

            string autostart_exec = "sparkleshare";

            if (InstallationInfo.Directory.StartsWith ("/app/"))
                autostart_exec = "xdg-app run org.sparkleshare.SparkleShare";

			// TODO: Ship as .desktop file and copy in place
            try {
                File.WriteAllText (autostart_file_path,
                    "[Desktop Entry]\n" +
                    "Name=SparkleShare\n" +
                    "Type=Application\n" +
                    "Exec=" + autostart_exec + "\n" +
                    "Icon=org.sparkleshare.SparkleShare\n" +
                    "Terminal=false\n" +
                    "X-GNOME-Autostart-enabled=true\n");

                Logger.LogInfo ("Controller", "Added SparkleShare to startup items");

            } catch (Exception e) {
                Logger.LogInfo ("Controller", "Failed to add SparkleShare to startup items", e);
            }
        }


        // Creates the SparkleShare folder in the user's home folder
        public override bool CreateSparkleShareFolder ()
        {
            if (!Directory.Exists (Configuration.DefaultConfig.FoldersPath)) {
                Directory.CreateDirectory (Configuration.DefaultConfig.FoldersPath);
                Syscall.chmod (Configuration.DefaultConfig.FoldersPath, (FilePermissions) 448); // 448 -> 700

                return true;
            }

            return false;
        }
        

        public override string EventLogHTML {
            get {
                string html_path = Path.Combine (InstallationInfo.Directory, "html", "event-log.html");
				string jquery_file_path = Path.Combine (InstallationInfo.Directory, "html", "jquery.js");

                string html   = File.ReadAllText (html_path);
                string jquery = File.ReadAllText (jquery_file_path);

                return html.Replace ("<!-- $jquery -->", jquery);
            }
        }

        
        public override string DayEntryHTML {
            get {
                string path = Path.Combine (InstallationInfo.Directory, "html", "day-entry.html");
                return File.ReadAllText (path);
            }
        }

        
        public override string EventEntryHTML {
            get {
                string path = Path.Combine (InstallationInfo.Directory, "html", "event-entry.html");
                return File.ReadAllText (path);
            }
        }


        public override void CopyToClipboard (string text)
        {
            Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
            clipboard.Text      = text;
        }


        public override void SetFolderIcon ()
        {
            var process = new Command ("gvfs-set-attribute", Configuration.DefaultConfig.FoldersPath + " " +
                "metadata::custom-icon-name org.sparkleshare.SparkleShare");

            process.StartAndWaitForExit ();
        }


        public override void OpenFolder (string path)
        {
            OpenFile (path);
        }


        public override void OpenFile (string path)
        {
			new Command ("xdg-open", "\"" + path + "\"").Start ();
        }


        public override void InstallProtocolHandler ()
        {
        }
    }
}
