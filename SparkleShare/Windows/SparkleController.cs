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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using System.Windows;
using Forms = System.Windows.Forms;
using Microsoft.Win32;

using Sparkles;

namespace SparkleShare {

    public class SparkleController : SparkleControllerBase {

        public SparkleController ()
        {
        }


        public override string PresetsPath
        {
            get {
                return Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "plugins");
            }
        }


        public override void Initialize ()
        {
            // Add msysgit to path, as we cannot asume it is added to the path
            // Asume it is installed in @"<exec dir>\msysgit\bin"
            string executable_path = Path.GetDirectoryName (Forms.Application.ExecutablePath);
            string msysgit_path    = Path.Combine (executable_path, "msysgit");

            string new_PATH = msysgit_path + @"\bin" + ";" +
                msysgit_path + @"\mingw\bin" + ";" +
                msysgit_path + @"\cmd" + ";" +
                Environment.ExpandEnvironmentVariables ("%PATH%");

            Environment.SetEnvironmentVariable ("PATH", new_PATH);
            Environment.SetEnvironmentVariable ("HOME", Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));

            Sparkles.Git.SparkleGit.SSHPath = Path.Combine (msysgit_path, "bin", "ssh.exe");

            base.Initialize ();
        }


        public override string EventLogHTML {
            get {
                string html = SparkleUIHelpers.GetHTML ("event-log.html");
                return html.Replace ("<!-- $jquery -->", SparkleUIHelpers.GetHTML ("jquery.js"));
            }
        }


        public override string DayEntryHTML {
            get {
                return SparkleUIHelpers.GetHTML ("day-entry.html");
            }
        }


        public override string EventEntryHTML {
            get {
                return SparkleUIHelpers.GetHTML ("event-entry.html");
            }
        }


        public override void CreateStartupItem ()
        {
            string startup_folder_path = Environment.GetFolderPath (Environment.SpecialFolder.Startup);
            string shortcut_path       = Path.Combine (startup_folder_path, "SparkleShare.lnk");

            if (File.Exists (shortcut_path))
                File.Delete (shortcut_path);

            string shortcut_target = Forms.Application.ExecutablePath;

            Shortcut shortcut = new Shortcut ();
            shortcut.Create (shortcut_path, shortcut_target);
        }
        

        public override void InstallProtocolHandler ()
        {
            // We ship a separate .exe for this
        }


        public override void AddToBookmarks ()
        {
            string user_profile_path = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
            string shortcut_path     = Path.Combine (user_profile_path, "Links", "SparkleShare.lnk");

            if (File.Exists (shortcut_path))
                File.Delete (shortcut_path);

            Shortcut shortcut = new Shortcut ();
            shortcut.Create (FoldersPath, shortcut_path);
        }


        public override bool CreateSparkleShareFolder ()
        {
            if (Directory.Exists (FoldersPath))
                return false;

        	Directory.CreateDirectory (FoldersPath);

			File.SetAttributes (FoldersPath, File.GetAttributes (FoldersPath) | FileAttributes.System);
            SparkleLogger.LogInfo ("Config", "Created '" + FoldersPath + "'");

            string app_path       = Path.GetDirectoryName (Forms.Application.ExecutablePath);
            string icon_file_path = Path.Combine (app_path, "Images", "sparkleshare-folder.ico");

            if (!File.Exists (icon_file_path)) {
                string ini_file_path  = Path.Combine (FoldersPath, "desktop.ini");
                string n = Environment.NewLine;

                string ini_file = "[.ShellClassInfo]" + n +
                    "IconFile=" + icon_file_path + n +
                    "IconIndex=0" + n +
                    "InfoTip=SparkleShare";

                try {
                    File.Create (ini_file_path).Close ();
                    File.WriteAllText (ini_file_path, ini_file);
                    
                    File.SetAttributes (ini_file_path,
                        File.GetAttributes (ini_file_path) | FileAttributes.Hidden | FileAttributes.System);

                } catch (IOException e) {
                    SparkleLogger.LogInfo ("Config", "Failed setting icon for '" + FoldersPath + "': " + e.Message);
                }

                return true;
            }

            return false;
        }


        public override void OpenFile (string path)
        {
            Process.Start (path);
        }


        public override void OpenFolder (string path)
        {
            Process.Start (path);
        }


        public override void OpenWebsite (string url)
        {
            Process.Start (new ProcessStartInfo (url));
        }


        public override void CopyToClipboard (string text)
        {
            try {
                Clipboard.SetData (DataFormats.Text, text);
            
            } catch (COMException e) {
                SparkleLogger.LogInfo ("Controller", "Copy to clipboard failed", e);
            }
        }


        public override void Quit ()
        {
            base.Quit ();
        }
    }
}
