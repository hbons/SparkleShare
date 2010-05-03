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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleShare {

	// Holds the status icon, window and repository list
	public class SparkleShareUI {

		public SparkleShareWindow SparkleShareWindow;
		public SparkleShareStatusIcon SparkleShareStatusIcon;
		public Repository [] Repositories;

		public SparkleShareUI (bool HideUI) {

			Process Process = new Process();
			Process.EnableRaisingEvents = false;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			// Get home folder, example: "/home/user/" 
			string UserHome = Environment.GetEnvironmentVariable("HOME") + "/";

			// Create 'SparkleShare' folder in the user's home folder
			string ReposPath = UserHome + "SparkleShare";
			if (!Directory.Exists (ReposPath)) {
				Directory.CreateDirectory (ReposPath);
				Console.WriteLine ("[Config] Created '" + ReposPath + "'");

				Process.StartInfo.FileName = "gvfs-set-attribute";
				Process.StartInfo.Arguments = ReposPath + " metadata::custom-icon " +
					                          "file://usr/share/icons/hicolor/48x48/places/folder-sparkleshare";
				Process.Start();

			}

			// Create place to store configuration user's home folder
			string ConfigPath = UserHome + ".config/sparkleshare/";
			if (!Directory.Exists (ConfigPath)) {

				Directory.CreateDirectory (ConfigPath);
				Console.WriteLine ("[Config] Created '" + ConfigPath + "'");

				// Create a first run file to show the intro message
				File.Create (ConfigPath + "firstrun");
				Console.WriteLine ("[Config] Created '" + ConfigPath + "firstrun'");

				// Create a place to store the avatars
				Directory.CreateDirectory (ConfigPath + "avatars/");
				Console.WriteLine ("[Config] Created '" + ConfigPath + "avatars'");

			}

			// Get all the repos in ~/SparkleShare
			string [] Repos = Directory.GetDirectories (ReposPath);
			Repositories = new Repository [Repos.Length];

			int i = 0;
			foreach (string Folder in Repos) {
				Repositories [i] = new Repository (Folder);
				i++;
			}

			// Don't create the window and status 
			// icon when --disable-gui was given
			if (!HideUI) {

				// Create the window
				SparkleShareWindow = new SparkleShareWindow (Repositories);
				SparkleShareWindow.DeleteEvent += CloseSparkleShareWindow;

				// Create the status icon
				SparkleShareStatusIcon = new SparkleShareStatusIcon ();
				SparkleShareStatusIcon.Activate += delegate { 
					SparkleShareWindow.ToggleVisibility ();
				};

			}

		}

		// Closes the window
		public void CloseSparkleShareWindow (object o, DeleteEventArgs args) {
			SparkleShareWindow = new SparkleShareWindow (Repositories);
			SparkleShareWindow.DeleteEvent += CloseSparkleShareWindow;
		}

		public void StartMonitoring () {	}
		public void StopMonitoring () { }

	}

}
