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

		public SparkleRepo [] Repositories;

		public SparkleWindow SparkleWindow;
		public SparkleStatusIcon SparkleStatusIcon;

		public SparkleUI (bool HideUI) {

			Process Process = new Process();
			Process.EnableRaisingEvents = false;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			string SparklePath = SparklePaths.SparklePath;

			// Create 'SparkleShare' folder in the user's home folder
			// if it's not there already
			if (!Directory.Exists (SparklePath)) {
				Directory.CreateDirectory (SparklePath);
				Console.WriteLine ("[Config] Created '" + SparklePath + "'");

				// Linux/GNOME specific
				Process.StartInfo.FileName = "gvfs-set-attribute";
				Process.StartInfo.Arguments = SparklePath +
				                              " metadata::custom-icon " +
					                           "file:///usr/share/icons/hicolor/" +
					                           "48x48/places/" +
					                           "folder-sparkleshare.png";
				Process.Start();
				
			}

			// Create place to store configuration user's home folder
			string ConfigPath = SparklePaths.SparkleConfigPath;
			string AvatarPath = SparklePaths.SparkleAvatarPath;
			if (!Directory.Exists (ConfigPath)) {

				Directory.CreateDirectory (ConfigPath);
				Console.WriteLine ("[Config] Created '" + ConfigPath + "'");

				// Create a place to store the avatars
				Directory.CreateDirectory (AvatarPath);
				Console.WriteLine ("[Config] Created '" + AvatarPath + "avatars'");

			}

			// Get all the repos in ~/SparkleShare
			string [] Repos = Directory.GetDirectories (SparklePath);
			Repositories = new SparkleRepo [Repos.Length];

			int i = 0;
			foreach (string Folder in Repos) {
				Repositories [i] = new SparkleRepo (Folder);
				i++;

				// Linux/GNOME only: attach emblems
				Process.StartInfo.FileName = "gvfs-set-attribute";
				Console.WriteLine (Folder);
				Process.StartInfo.Arguments = " file://" + Folder + 
				                              " metadata::emblems " +
														"[synced]";
				Console.WriteLine (Process.StartInfo.Arguments);
				Process.Start();				
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
