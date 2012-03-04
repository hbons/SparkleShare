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
using System.Windows.Forms;

using CefSharp;
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
                    Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location),
                    "plugins"
                );
            }
        }


		public override void Initialize ()
		{
            Settings settings                = new Settings ();
            BrowserSettings browser_settings = new BrowserSettings ();

            if (!CEF.Initialize (settings, browser_settings)) {
                Console.WriteLine ("Could not initialise CEF");
                return;
            }

            CEF.RegisterScheme ("application", "sparkleshare", new ApplicationSchemeHandlerFactory ());
            CEF.RegisterScheme ("application", "file", new FileSchemeHandlerFactory ());
            
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);

			// Add msysgit to path, as we cannot asume it is added to the path
			// Asume it is installed in @"<exec dir>\msysgit\bin"
            string executable_dir = Path.GetDirectoryName (Application.ExecutablePath);
            string msysgit = Path.Combine (executable_dir, "msysgit");

			string new_PATH = msysgit + @"\bin" + ";" +
			    msysgit + @"\mingw\bin" + ";" +
			    msysgit + @"\cmd" + ";" +
			    Environment.ExpandEnvironmentVariables ("%PATH%");

			Environment.SetEnvironmentVariable ("PATH", new_PATH);
			Environment.SetEnvironmentVariable ("PLINK_PROTOCOL", "ssh");

			if (string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("HOME")))
			    Environment.SetEnvironmentVariable ("HOME",
                    Environment.ExpandEnvironmentVariables ("%HOMEDRIVE%%HOMEPATH%"));

			StartSSH ();
			base.Initialize ();
		}


        public override string EventLogHTML
        {
            get {
                string html = Properties.Resources.event_log_html;
                html = html.Replace ("<!-- $jquery-url -->", "application://sparkleshare/jquery.js");
                return html;
            }
        }


        public override string DayEntryHTML
        {
            get {
                return Properties.Resources.day_entry_html;
            }
        }


        public override string EventEntryHTML
        {
            get {
                return Properties.Resources.event_entry_html;
            }
        }


		public override void CreateStartupItem ()
		{
            // TODO
		}
		

        public override void InstallProtocolHandler()
		{
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
		}


		public override void AddToBookmarks ()
		{
            // TODO
		}


		public override bool CreateSparkleShareFolder ()
		{
            if (!Directory.Exists (SparkleConfig.DefaultConfig.FoldersPath)) {
                Directory.CreateDirectory(SparkleConfig.DefaultConfig.FoldersPath);
                Directory.CreateDirectory(SparkleConfig.DefaultConfig.TmpPath);

                SparkleHelpers.DebugInfo("Config", "Created \"" +
                    SparkleConfig.DefaultConfig.FoldersPath + "\"");

                // TODO: Set a custom SparkleShare folder icon

				return true;

            } else {
			    return false;
            }
		}


        public override void OpenFile (string url)
        {
            Process process = new Process ();
            process.StartInfo.FileName  = "start";
            process.StartInfo.Arguments = "\"" + url + "\"";

            process.Start ();
        }


		public override void OpenSparkleShareFolder (string subfolder)
		{
			Process process             = new Process ();
            process.StartInfo.FileName  = "explorer";
            process.StartInfo.Arguments = ",/root," +
                Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, subfolder);
			
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

            if (string.IsNullOrEmpty (auth_sock)) {
                Process process                          = new Process ();
			    process.StartInfo.FileName               = "ssh-agent";
				process.StartInfo.UseShellExecute        = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.CreateNoWindow         = true;

				process.Start ();

				string output = process.StandardOutput.ReadToEnd ();
				process.WaitForExit ();

				Match auth_sock_match = new Regex (@"SSH_AUTH_SOCK=([^;\n\r]*)").Match (output);

                if (auth_sock_match.Success)
					Environment.SetEnvironmentVariable ("SSH_AUTH_SOCK",
                        auth_sock_match.Groups [1].Value);

				Match ssh_pid_match =
                    new Regex (@"SSH_AGENT_PID=([^;\n\r]*)").Match (output);

                if (ssh_pid_match.Success) {
                    string ssh_pid = ssh_pid_match.Groups [1].Value;

                    Int32.TryParse (ssh_pid, out this.ssh_agent_pid);
					Environment.SetEnvironmentVariable ("SSH_AGENT_PID", ssh_pid);

                    SparkleHelpers.DebugInfo ("Controller",
                        "ssh-agent started, PID=" + ssh_pid);

                } else {
					SparkleHelpers.DebugInfo ("Controller",
                        "ssh-agent started, PID=unknown");
				}
			}
		}


		private void StopSSH ()
		{
            if (ssh_agent_pid != 0) {
                try {
                    Process.GetProcessById (this.ssh_agent_pid).Kill ();

                } catch (ArgumentException) {
                    SparkleHelpers.DebugInfo ("SSH",
                        "Could not stop SSH: the process isn't running");
                }
            }
		}
    }
}
