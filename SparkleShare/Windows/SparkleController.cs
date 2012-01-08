//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using System.Reflection;
using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CefSharp;

namespace SparkleShare {

    public class SparkleController : SparkleControllerBase {
        private int sshAgentPid;

        public override string PluginsPath
        {
            get
            {
                return Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "plugins");
            }
        }


		public SparkleController () : base ()
		{
		}

		public override void Initialize ()
		{
            Settings settings = new Settings ();
            BrowserSettings browserSettings = new BrowserSettings ();

            if (!CEF.Initialize (settings, browserSettings)) {
                Console.WriteLine ("Couldn't initialise CEF");
                return;
            }

            CEF.RegisterScheme ("application", "sparkleshare", new ApplicationSchemeHandlerFactory ());
            CEF.RegisterScheme ("application", "file", new FileSchemeHandlerFactory ());
            
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);

			// Add msysgit to path, as we cannot asume it is added to the path
			// Asume it is installed in @"<exec dir>\msysgit\bin"
            string ExecutableDir = Path.GetDirectoryName(Application.ExecutablePath);
            string MSysGit = Path.Combine(ExecutableDir, "msysgit");

			string newPath = MSysGit + @"\bin" + ";"
			               + MSysGit + @"\mingw\bin" + ";"
			               + MSysGit + @"\cmd" + ";"
			               + System.Environment.ExpandEnvironmentVariables ("%PATH%");
			System.Environment.SetEnvironmentVariable ("PATH", newPath);
			System.Environment.SetEnvironmentVariable ("PLINK_PROTOCOL", "ssh");

			if (String.IsNullOrEmpty (System.Environment.GetEnvironmentVariable ("HOME")))
				System.Environment.SetEnvironmentVariable ("HOME", Environment.ExpandEnvironmentVariables ("%HOMEDRIVE%%HOMEPATH%"));

			StartSshAgent ();

			base.Initialize ();
		}

        public override string EventLogHTML
        {
            get
            {
                string html = Properties.Resources.event_log_html;

                html = html.Replace ("<!-- $jquery-url -->", "application://sparkleshare/jquery.js");

                return html;
            }
        }


        public override string DayEntryHTML
        {
            get
            {
                return Properties.Resources.day_entry_html;
            }
        }


        public override string EventEntryHTML
        {
            get
            {
                return Properties.Resources.event_entry_html;
            }
        }

        /*public override string GetAvatar (string email, int size)
        {
            if (string.IsNullOrEmpty (email)) {
                return "application://sparkleshare/avatar-default-32.png";
            }
            string avatar_file_path = SparkleHelpers.CombineMore (
                SparklePaths.SparkleLocalIconPath, size + "x" + size, "status", "avatar-" + email);

            return avatar_file_path;
        }*/


		// Creates a .desktop entry in autostart folder to
		// start SparkleShare automatically at login
		public override void EnableSystemAutostart ()
		{
		}
		

		// Installs a launcher so the user can launch SparkleShare
		// from the Internet category if needed
		public override void InstallLauncher ()
		{
		}


		// Adds the SparkleShare folder to the user's
		// list of bookmarked places
		public override void AddToBookmarks ()
		{
		}


		// Creates the SparkleShare folder in the user's home folder
		public override bool CreateSparkleShareFolder ()
		{
            if (!Directory.Exists(SparkleConfig.DefaultConfig.FoldersPath))
            {
                Directory.CreateDirectory(SparkleConfig.DefaultConfig.FoldersPath);
                Directory.CreateDirectory(SparkleConfig.DefaultConfig.TmpPath);
                SparkleHelpers.DebugInfo("Config", "Created '" + SparkleConfig.DefaultConfig.FoldersPath + "'");

				return true;
			}

			return false;
		}

        public override void OpenFile(string url)
        {
            Process process = new Process();
            process.StartInfo.Arguments = "\"" + url + "\"";
            process.StartInfo.FileName = "start";

            process.Start();
        }

		public override void OpenSparkleShareFolder (string subfolder)
		{
			Process process = new Process();
			process.StartInfo.Arguments = ",/root," + SparkleHelpers.CombineMore(SparkleConfig.DefaultConfig.FoldersPath, subfolder);
			process.StartInfo.FileName = "explorer";
			
			process.Start();
		}

        public override void Quit ()
        {
            KillSshAgent ();
            base.Quit ();
        }

		private void StartSshAgent ()
		{
		    if (String.IsNullOrEmpty (System.Environment.GetEnvironmentVariable ("SSH_AUTH_SOCK"))) {

                Process process = new Process ();
			    process.StartInfo.FileName = "ssh-agent";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;

				process.Start ();

				string Output = process.StandardOutput.ReadToEnd ();
				process.WaitForExit ();

				Match AuthSock = new Regex (@"SSH_AUTH_SOCK=([^;\n\r]*)").Match (Output);
				if (AuthSock.Success) {
					System.Environment.SetEnvironmentVariable ("SSH_AUTH_SOCK", AuthSock.Groups[1].Value);
				}

				Match AgentPid = new Regex (@"SSH_AGENT_PID=([^;\n\r]*)").Match (Output);
				if (AgentPid.Success) {
                    Int32.TryParse (AgentPid.Groups [1].Value, out sshAgentPid);
					System.Environment.SetEnvironmentVariable ("SSH_AGENT_PID", AgentPid.Groups[1].Value);
					SparkleHelpers.DebugInfo ("SSH", "ssh-agent started, PID=" + AgentPid.Groups[1].Value);
				}
				else {
					SparkleHelpers.DebugInfo ("SSH", "ssh-agent started, PID=unknown");
				}
			}
		}

		private void KillSshAgent ()
		{
            if (sshAgentPid != 0) {
                // Kill the SSH_AGENT that we started
                try {
                    Process.GetProcessById (sshAgentPid).Kill ();
                } catch (ArgumentException) {
                    SparkleHelpers.DebugInfo ("SSH", "Could not kill the ssh-agent, because the process wasn't running");
                }
            }
		}

    }

}
