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

using Gtk;
using System;
using System.IO;
using System.Diagnostics;

namespace SparkleShare {

	public class SparkleFetcher {

		public delegate void CloningStartedEventHandler (object o, SparkleEventArgs args);
		public delegate void CloningFinishedEventHandler (object o, SparkleEventArgs args);
		public delegate void CloningFailedEventHandler (object o, SparkleEventArgs args);

		public event CloningStartedEventHandler CloningStarted;
		public event CloningFinishedEventHandler CloningFinished;
		public event CloningFailedEventHandler CloningFailed;

		private string Folder;
		private string RemoteOriginUrl;
		private string RepoName;

		
		public SparkleFetcher (string url, string folder)
		{

			RepoName = Path.GetDirectoryName (folder);
			Folder = folder;
			RemoteOriginUrl = url;

		}


		public void Clone ()
		{

			SparkleEventArgs args = new SparkleEventArgs ("CloningStarted");

			if (CloningStarted != null)
	            CloningStarted (this, args); 

			Process process = new Process () {	
				EnableRaisingEvents = true
			};

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "clone " + RemoteOriginUrl + " " + Folder;

			Console.WriteLine (Folder);

			Console.WriteLine (process.StartInfo.FileName + " " + process.StartInfo.Arguments);

			process.Exited += delegate {

				Console.WriteLine (process.ExitTime.ToString ());
				Console.WriteLine (process.ExitCode);

				if (process.ExitCode != 0) {

					args = new SparkleEventArgs ("CloningFailed");

					if (CloningFailed != null)
					    CloningFailed (this, args); 

				} else {

					InstallExcludeRules ();

					args = new SparkleEventArgs ("CloningFinished");

						Console.WriteLine ("FINISHED");

					if (CloningFinished != null) {
						Console.WriteLine ("EVENT FIRED");
					    CloningFinished (this, args);
					}

				}

			};

			process.Start ();

		}


		// Add a .gitignore file to the repo
		private void InstallExcludeRules ()
		{

			TextWriter writer;
			writer = new StreamWriter (SparkleHelpers.CombineMore (SparklePaths.SparklePath,
				RepoName, ".git/info/exclude"));

			writer.WriteLine ("*~"); // Ignore gedit swap files
			writer.WriteLine (".*.sw?"); // Ignore vi swap files
			writer.WriteLine (".DS_store"); // Ignore OSX's invisible directories

			writer.Close ();

		}

	}

}
