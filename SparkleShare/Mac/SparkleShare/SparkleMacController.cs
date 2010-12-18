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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using SparkleLib;
using System;
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	public class SparkleMacController : SparkleController {

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
			
			return true;
			
		}

		
		// Opens the SparkleShare folder or an (optional) subfolder
		public override void OpenSparkleShareFolder (string subfolder)
		{
		
			string folder = Path.Combine (SparklePaths.SparklePath, subfolder);

			Process process = new Process ();
			process.StartInfo.Arguments = folder.Replace (" ", "\\ "); // Escape space-characters
			process.StartInfo.FileName  = "open";
			process.Start ();
			
		}

	}

}