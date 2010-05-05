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
using System;
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	// Holds the status icon, window and repository list
	public class SparkleUI {

		public Repository [] Repositories;

		public SparkleWindow SparkleWindow;
		public SparkleStatusIcon SparkleStatusIcon;

		public SparkleUI (bool HideUI) {

			Process Process = new Process();
			Process.EnableRaisingEvents = false;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			// Get home folder, example: "/home/user/" 
			string UserHome = Environment.GetEnvironmentVariable("HOME") + "/";
			string ReposPath = UserHome + "SparkleShare";

			// Create 'SparkleShare' folder in the user's home folder
			// if it's not there already
			if (!Directory.Exists (ReposPath)) {
				Directory.CreateDirectory (ReposPath);
				Console.WriteLine ("[Config] Created '" + ReposPath + "'");

				Process.StartInfo.FileName = "gvfs-set-attribute";
				Process.StartInfo.Arguments = ReposPath +
				                              " metadata::custom-icon " +
					                           "file:///usr/share/icons/hicolor/" +
					                           "48x48/places/" +
					                           "folder-sparkleshare.png";
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
				SparkleWindow = new SparkleWindow (Repositories);
				SparkleWindow.DeleteEvent += CloseSparkleWindow;

				// Create the status icon
				SparkleStatusIcon = new SparkleStatusIcon ();
				SparkleStatusIcon.Activate += delegate { 
					SparkleWindow.ToggleVisibility ();
				};

			}

		}

		// Closes the window
		public void CloseSparkleWindow (object o, DeleteEventArgs args) {
			SparkleWindow = new SparkleWindow (Repositories);
			SparkleWindow.DeleteEvent += CloseSparkleWindow;
		}

		public void StartMonitoring () {	}
		public void StopMonitoring () { }

	}

}
