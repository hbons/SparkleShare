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

using System;
using System.IO;
using System.Diagnostics;
using System.Timers;

namespace SparkleLib {

	// A helper class that fetches and configures 
	// a remote repository
	public class SparkleFetcher {

		public delegate void CloningStartedEventHandler (object o, SparkleEventArgs args);
		public delegate void CloningFinishedEventHandler (object o, SparkleEventArgs args);
		public delegate void CloningFailedEventHandler (object o, SparkleEventArgs args);

		public event CloningStartedEventHandler CloningStarted;
		public event CloningFinishedEventHandler CloningFinished;
		public event CloningFailedEventHandler CloningFailed;

		private string TargetFolder;
		private string RemoteOriginUrl;
		public SparkleProgress Progress;


		public SparkleFetcher (string url, string folder)
		{

			TargetFolder = folder;
			RemoteOriginUrl = url;
			Progress = new SparkleProgress ();

		}


		// Clones the remote repository
		public void Start ()
		{

			if (Directory.Exists (TargetFolder))
				Directory.Delete (TargetFolder, true);
			
			SparkleHelpers.DebugInfo ("Git", "[" + TargetFolder + "] Cloning Repository");
			
			if (CloningStarted != null)
	            CloningStarted (this, new SparkleEventArgs ("CloningStarted")); 

			Process process = new Process () {	
				EnableRaisingEvents = true
			};

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "clone --progress " +
			                              "\"" + RemoteOriginUrl + "\" " + "\"" + TargetFolder + "\"";

Timer timer = new Timer () {
Interval = 1000
};
timer.Elapsed += delegate {
Console.WriteLine ("ppppppppppppppp");
				Console.WriteLine (process.StandardError.ReadToEnd ());
				
};
			process.ErrorDataReceived += delegate (object o, DataReceivedEventArgs args) {
				Console.WriteLine (">>>>>>>>>" + args.Data);
				Progress.Speed = "";
				Progress.Fraction = 0;
			};


			process.Exited += delegate {

				SparkleHelpers.DebugInfo ("Git", "Exit code " + process.ExitCode.ToString ());

				if (process.ExitCode != 0) {
					
					SparkleHelpers.DebugInfo ("Git", "[" + TargetFolder + "] Cloning failed");

					if (CloningFailed != null)
					    CloningFailed (this, new SparkleEventArgs ("CloningFailed")); 

				} else {

					InstallUserInfo ();
					InstallExcludeRules ();
					
					SparkleHelpers.DebugInfo ("Git", "[" + TargetFolder + "] Repository cloned");

					if (CloningFinished != null)
					    CloningFinished (this, new SparkleEventArgs ("CloningFinished"));

				}

			};
timer.Start();
			process.Start ();
			process.BeginErrorReadLine ();

		}


		// Install the user's name and email into
		// the newly cloned repository
		private void InstallUserInfo ()
		{

			string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			if (File.Exists (global_config_file_path)) {

				StreamReader reader = new StreamReader (global_config_file_path);
				string user_info = reader.ReadToEnd ();
				reader.Close ();

				string repo_config_file_path = SparkleHelpers.CombineMore (TargetFolder, ".git", "config");

				TextWriter writer = File.AppendText (repo_config_file_path);
				writer.WriteLine (user_info);
				writer.Close ();

				SparkleHelpers.DebugInfo ("Config", "Added user info to '" + repo_config_file_path + "'");

			}

		}


		// Add a .gitignore file to the repo
		private void InstallExcludeRules ()
		{

			string exlude_rules_file_path = SparkleHelpers.CombineMore (TargetFolder, ".git", "info", "exclude");

			TextWriter writer = new StreamWriter (exlude_rules_file_path);

			// Ignore gedit swap files
			writer.WriteLine ("*~");

			// Ignore vi swap files
			writer.WriteLine (".*.sw?");

			// Ignore OSX's invisible directories
			writer.WriteLine (".DS_store");

			writer.Close ();

		}

	}


	public class SparkleProgress {

		public string _Speed;
		public int _Fraction;

		public delegate void ProgressChangedEventHandler ();
		public event ProgressChangedEventHandler ProgressChanged;


		public SparkleProgress ()
		{

			_Speed      = "0 B/s";
			_Fraction = 0;

		}


		public string Speed
		{

			get {

				return _Speed;

			}

			set {

				_Speed = value;

				if (ProgressChanged != null)
					ProgressChanged ();

			}

		}

		public int Fraction
		{

			get {

				return _Fraction;

			}

			set {

				_Fraction = value;

				if (ProgressChanged != null)
					ProgressChanged ();

			}

		}

	}
	
}
