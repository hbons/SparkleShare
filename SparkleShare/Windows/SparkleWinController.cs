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

using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace SparkleShare {

	public class SparkleWinController : SparkleController {

		public SparkleWinController () : base ()
		{
		}

		public override void Initialize ()
		{
			// Add msysgit to path, as we cannot asume it is added to the path
			// Asume it is installed in @"C:\msysgit\bin" for now
			string MSysGit=@"C:\msysgit";

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

		public override string EventLogHTML { get { return "";  } }
		public override string DayEntryHTML { get { return ""; } }
		public override string EventEntryHTML { get { return ""; } }

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
			if (!Directory.Exists (SparklePaths.SparklePath)) {

				Directory.CreateDirectory (SparklePaths.SparklePath);
				SparkleHelpers.DebugInfo ("Config", "Created '" + SparklePaths.SparklePath + "'");

				return true;

			}

			return false;
		}

		public override void OpenSparkleShareFolder (string subfolder)
		{
			Process process = new Process();
			process.StartInfo.Arguments = ",/root," + SparkleHelpers.CombineMore(SparklePaths.SparklePath, subfolder);
			process.StartInfo.FileName = "explorer";
			
			process.Start();
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
					System.Environment.SetEnvironmentVariable ("SSH_AGENT_PID", AgentPid.Groups[1].Value);
					SparkleHelpers.DebugInfo ("SSH", "ssh-agent started, PID=" + AgentPid.Groups[1].Value);
				}
				else {
					SparkleHelpers.DebugInfo ("SSH", "ssh-agent started, PID=unknown");
				}
			}
		}


	}

}
