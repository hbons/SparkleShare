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
using Forms = System.Windows.Forms;

using Microsoft.Win32;
using SparkleLib;

namespace SparkleShare {

    public class SparkleController : SparkleControllerBase {

        private int ssh_agent_pid;


        public SparkleController () : base ()
        {
        }


        public override string PluginsPath
        {
            get {
                return Path.Combine (
                    Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "plugins"
                );
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
            Environment.SetEnvironmentVariable ("PLINK_PROTOCOL", "ssh");
            Environment.SetEnvironmentVariable ("HOME", Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));

            StartSSH ();
            base.Initialize ();
        }


        public override string EventLogHTML {
            get {
                string html = SparkleUIHelpers.GetHTML ("event-log.html");
                html        = html.Replace ("<!-- $jquery -->", SparkleUIHelpers.GetHTML ("jquery.js"));
                
                return html;
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
            string startup_folder_path = Environment.GetFolderPath (
                Environment.SpecialFolder.Startup);
            
            string shortcut_path = Path.Combine (startup_folder_path, "SparkleShare.lnk");

            if (File.Exists (shortcut_path))
                File.Delete (shortcut_path);

            string shortcut_target = Forms.Application.ExecutablePath;

            Shortcut shortcut = new Shortcut ();
            shortcut.Create (shortcut_path, shortcut_target);
        }
        

        public override void InstallProtocolHandler ()
        {
            /* FIXME: Need to find a way to do this without administrator privileges (or move to the installer)
         
            // Get assembly location
            string location   = System.Reflection.Assembly.GetExecutingAssembly ().Location;
            string folder     = Path.GetDirectoryName (location);
            string invite_exe = Path.Combine (folder, "SparkleShareInviteOpener.exe");

            // Register protocol handler as explained in
            // http://msdn.microsoft.com/en-us/library/ie/aa767914(v=vs.85).aspx
            string main_key = "HKEY_CLASSES_ROOT\\sparkleshare";
            Registry.SetValue (main_key, "", "SparkleShare Invite Opener");
            Registry.SetValue (main_key, "URL Protocol", "");

            string icon_key = "HKEY_CLASSES_ROOT\\sparkleshare\\DefaultIcon";
            Registry.SetValue (icon_key, "", invite_exe + ",1");

            string action_key = "HKEY_CLASSES_ROOT\\sparkleshare\\shell\\open\\command";
            Registry.SetValue (action_key, "", "\"" + invite_exe + "\" \"%1\"");

            */
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
            if (!Directory.Exists (FoldersPath)) {
                Directory.CreateDirectory (FoldersPath);

                SparkleHelpers.DebugInfo ("Config", "Created '" + FoldersPath + "'");

                // TODO: Set a custom SparkleShare folder icon

                return true;

            } else {
                return false;
            }
        }


        public override void OpenFile (string path)
        {
            Process.Start (path);
        }


        public override void OpenFolder (string path)
        {
            Process process             = new Process ();
            process.StartInfo.FileName  = "explorer";
            process.StartInfo.Arguments = path;
            
            process.Start();
        }


        public override void Quit ()
        {
            StopSSH ();
            base.Quit ();
        }


        private void StartSSH ()
        {
            string auth_sock = Environment.GetEnvironmentVariable ("SSH_AUTH_SOCK");

            if (!string.IsNullOrEmpty (auth_sock)) {
                SparkleHelpers.DebugInfo ("Controller", "Using existing ssh-agent with PID=" + this.ssh_agent_pid);
                return;
            }

            Process process                          = new Process ();
            process.StartInfo.FileName               = "ssh-agent";
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;

            process.Start ();

            string output = process.StandardOutput.ReadToEnd ();
            process.WaitForExit ();

            Match auth_sock_match = new Regex (@"SSH_AUTH_SOCK=([^;\n\r]*)").Match (output);
            Match ssh_pid_match   = new Regex (@"SSH_AGENT_PID=([^;\n\r]*)").Match (output);

            if (auth_sock_match.Success)
                Environment.SetEnvironmentVariable ("SSH_AUTH_SOCK", auth_sock_match.Groups [1].Value);

            if (ssh_pid_match.Success) {
                string ssh_pid = ssh_pid_match.Groups [1].Value;

                Int32.TryParse (ssh_pid, out this.ssh_agent_pid);
                Environment.SetEnvironmentVariable ("SSH_AGENT_PID", ssh_pid);

                SparkleHelpers.DebugInfo ("Controller", "ssh-agent started, PID=" + ssh_pid);

            } else {
                SparkleHelpers.DebugInfo ("Controller", "ssh-agent started, PID=Unknown");
            }
        }


        private void StopSSH ()
        {
            if (this.ssh_agent_pid == 0)
                return;

            try {
                Process.GetProcessById (this.ssh_agent_pid).Kill ();

            } catch (ArgumentException e) {
                SparkleHelpers.DebugInfo ("SSH", "Could not stop ssh-agent: " + e.Message);
            }
        }
    }
}
