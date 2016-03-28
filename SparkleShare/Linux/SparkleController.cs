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

using Gtk;
using Mono.Unix.Native;

namespace SparkleShare {

    public class SparkleController : SparkleControllerBase {

        public SparkleController ()
        {
        }


        public override string PluginsPath {
            get {
                return Path.Combine (Defines.INSTALL_DIR, "plugins");
            }
        }


        // Creates a .desktop entry in autostart folder to
        // start SparkleShare automatically at login
        public override void CreateStartupItem ()
        {
            string autostart_path = Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "autostart");

            string autostart_file_path = Path.Combine (autostart_path, "org.sparkleshare.SparkleShare.Autostart.desktop");

            if (File.Exists (autostart_file_path))
				return;
 
			if (!Directory.Exists (autostart_path))
                Directory.CreateDirectory (autostart_path);

			try {
                File.WriteAllText (autostart_file_path,
                    "[Desktop Entry]\n" +
                    "Type=Application\n" +
                    "Name=SparkleShare\n" +
                    "Exec=sparkleshare\n" +
                    "Icon=org.sparkleshare.SparkleShare\n" +
                    "Terminal=false\n" +
                    "X-GNOME-Autostart-enabled=true\n" +
                    "Categories=Network");

                SparkleLogger.LogInfo ("Controller", "Added SparkleShare to login items");

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Controller", "Failed adding SparkleShare to login items: " + e.Message);
            }
            
        }


        // Creates the SparkleShare folder in the user's home folder
        public override bool CreateSparkleShareFolder ()
        {
            if (!Directory.Exists (SparkleConfig.DefaultConfig.FoldersPath)) {
                Directory.CreateDirectory (SparkleConfig.DefaultConfig.FoldersPath);
                Syscall.chmod (SparkleConfig.DefaultConfig.FoldersPath, (FilePermissions) 448); // 448 -> 700

                SparkleLogger.LogInfo ("Controller", "Created '" + SparkleConfig.DefaultConfig.FoldersPath + "'");
                return true;
            }

            return false;
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


        public override void CopyToClipboard (string text)
        {
            Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
            clipboard.Text      = text;
        }


        public override void SetFolderIcon ()
        {
            var process = new SparkleProcess ("gvfs-set-attribute", SparkleConfig.DefaultConfig.FoldersPath + " " +
                "metadata::custom-icon-name org.sparkleshare.SparkleShare");

			process.StartAndWaitForExit ();
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


        public override void InstallProtocolHandler ()
        {
        }
    }
}
