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
using Mono.Unix;
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleShare {

	// Holds repository information and timers
	public class SparkleRepo
	{

		private Process Process;
		private Timer FetchTimer;
		private Timer BufferTimer;
		private FileSystemWatcher Watcher;
		private bool HasChanged = false;
		private DateTime LastChange;
		private System.Object ChangeLock = new System.Object();

		public string Name;
		public string Domain;
		public string LocalPath;
		public string RemoteOriginUrl;
		public string CurrentHash;
		public string UserEmail;
		public string UserName;


		public delegate void AddedEventHandler (object o, SparkleEventArgs args);
		public event AddedEventHandler Added; 

		public delegate void CommitedEventHandler (object o, SparkleEventArgs args);
		public event CommitedEventHandler Commited; 

		public delegate void PushedEventHandler (object o, SparkleEventArgs args);
		public event PushedEventHandler Pushed;

		public delegate void FetchingStartedEventHandler (object o, SparkleEventArgs args);
		public event FetchingStartedEventHandler FetchingStarted;

		public delegate void FetchingFinishedEventHandler (object o, SparkleEventArgs args);
		public event FetchingFinishedEventHandler FetchingFinished;

		public delegate void NewCommitEventHandler (object o, SparkleEventArgs args);
		public event NewCommitEventHandler NewCommit;


		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		public SparkleRepo (string path)
		{

			LocalPath = path;
			Name = Path.GetFileName (LocalPath);

			Process = new Process ();
			Process.EnableRaisingEvents = true;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.FileName = "git";
			Process.StartInfo.WorkingDirectory = LocalPath;

			UserName        = GetUserName ();
			UserEmail       = GetUserEmail ();
			RemoteOriginUrl = GetRemoteOriginUrl ();
			CurrentHash     = GetCurrentHash ();
			Domain          = GetDomain (RemoteOriginUrl);


			// Watch the repository's folder
			Watcher = new FileSystemWatcher (LocalPath) {
				IncludeSubdirectories = true,
				EnableRaisingEvents   = true,
				Filter                = "*"
			};

			Watcher.Changed += new FileSystemEventHandler (OnFileActivity);
			Watcher.Created += new FileSystemEventHandler (OnFileActivity);
			Watcher.Deleted += new FileSystemEventHandler (OnFileActivity);


			// Fetch remote changes every 20 seconds
			FetchTimer = new Timer () {
				Interval = 20000
			};

			FetchTimer.Elapsed += delegate { 
				Fetch ();
			};


			// Keep a buffer that checks if there are changes and
			// whether they have settled
			BufferTimer = new Timer () {
				Interval = 4000
			};

			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				CheckForChanges ();
			};


			FetchTimer.Start ();
			BufferTimer.Start ();


			// Add everything that changed 
			// since SparkleShare was stopped
			AddCommitAndPush ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Idling...");

		}


		private void CheckForChanges ()
		{

			SparkleHelpers.DebugInfo ("Buffer", "[" + Name + "] Checking for changes.");

			lock (ChangeLock) {

				if (HasChanged) {

					SparkleHelpers.DebugInfo ("Buffer", "[" + Name + "] Changes found, checking if settled.");

					DateTime now     = DateTime.UtcNow;
					TimeSpan changed = new TimeSpan (now.Ticks - LastChange.Ticks);

					if (changed.TotalMilliseconds > 5000) {
						HasChanged = false;
						SparkleHelpers.DebugInfo ("Buffer", "[" + Name + "] Changes have settled, adding files...");
						AddCommitAndPush ();
					}

				}

			}

		}


		// Starts a time buffer when something changes
		private void OnFileActivity (object o, FileSystemEventArgs args)
		{

		   WatcherChangeTypes wct = args.ChangeType;

			if (!ShouldIgnore (args.Name)) {

				SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + args.Name + "'");

				FetchTimer.Stop ();

				lock (ChangeLock) {
					LastChange = DateTime.UtcNow;
					HasChanged = true;
				}

			}

		}


		// When there are changes we generally want to Add, Commit and Push
		// so this method does them all with appropriate timers, etc. switched off
		public void AddCommitAndPush ()
		{

			try {

				BufferTimer.Stop ();
				FetchTimer.Stop ();
	
				Add ();
				string message = FormatCommitMessage ();

				if (!message.Equals ("")) {
					Commit (message);
					Fetch ();
					Push ();
					CheckForUnicorns (message);
				}

			} finally {

				FetchTimer.Start ();
				BufferTimer.Start ();

			}

		}


		// Stages the made changes
		private void Add ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Staging changes...");

			Process.StartInfo.Arguments = "add --all";
			Process.Start ();
			Process.WaitForExit ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes staged.");

			SparkleEventArgs args = new SparkleEventArgs ("Added");

			if (Added != null)
	            Added (this, args); 

		}


		// Commits the made changes
		public void Commit (string Message)
		{

			SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + Message);

			Process.StartInfo.Arguments = "commit -m \"" + Message + "\"";
			Process.Start ();
			Process.WaitForExit ();

			SparkleEventArgs args = new SparkleEventArgs ("Commited");

			if (Commited != null)
	            Commited (this, args); 

		}


		// Fetches changes from the remote repo	
		public void Fetch ()
		{

			try {

				FetchTimer.Stop ();

				SparkleEventArgs args;
				args = new SparkleEventArgs ("FetchingStarted");

				if (FetchingStarted != null)
			        FetchingStarted (this, args); 

				SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Fetching changes...");

				Process.StartInfo.Arguments = "fetch -v";
				Process.Start ();
				Process.WaitForExit ();

				string Output = Process.StandardOutput.ReadToEnd ().Trim (); // TODO: This doesn't work :(

				SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes fetched.");

				args = new SparkleEventArgs ("FetchingFinished");

				if (FetchingFinished != null)
			        FetchingFinished (this, args); 

				// Rebase if there are changes
				if (!Output.Contains ("up to date"))
					Rebase ();

			} finally {

				FetchTimer.Start ();

			}

		}


		// Merges the fetched changes
		public void Rebase ()
		{

			Watcher.EnableRaisingEvents = false;

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Rebasing changes...");

			Process.StartInfo.Arguments = "rebase -v master";
			Process.WaitForExit ();
			Process.Start ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes rebased.");

			string output = Process.StandardOutput.ReadToEnd ().Trim ();

			// Show notification if there are updates
			if (!output.Contains ("up to date")) {

				if (output.Contains ("Failed to merge")) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Resolving conflict...");

					Process.StartInfo.Arguments = "status";
					Process.WaitForExit ();
					Process.Start ();
					output = Process.StandardOutput.ReadToEnd ().Trim ();
					string [] lines = Regex.Split (output, "\n");

					foreach (string line in lines) {

						if (line.Contains ("needs merge")) {

							string problem_file_name = line.Substring (line.IndexOf (": needs merge"));

							Process.StartInfo.Arguments = "checkout --ours " + problem_file_name;
							Process.WaitForExit ();
							Process.Start ();
							
							string TimeStamp = DateTime.Now.ToString ("H:mm d MMM yyyy");

							File.Move (problem_file_name,
								problem_file_name + " (" + UserName  + ", " + TimeStamp + ")");
							           
							Process.StartInfo.Arguments
								= "checkout --theirs " + problem_file_name;
							Process.WaitForExit ();
							Process.Start ();

							string conflict_title   = "A mid-air collision happened!\n";
							string conflict_subtext = "Don't worry, SparkleShare made\na copy of each conflicting file.";

//							SparkleBubble ConflictBubble =
	//							new  SparkleBubble(_(ConflictTitle), _(ConflictSubtext));

		//					ConflictBubble.Show ();

						}

					}

					Add ();
					
					Process.StartInfo.Arguments = "rebase --continue";
					Process.WaitForExit ();
					Process.Start ();

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict resolved.");

					Push ();
					Fetch ();
			
				}

				// Get the last committer e-mail
				Process.StartInfo.Arguments = "log --format=\"%ae\" -1";
				Process.Start ();
				string LastCommitEmail = Process.StandardOutput.ReadToEnd ().Trim ();

				// Get the last commit message
				Process.StartInfo.Arguments = "log --format=\"%s\" -1";
				Process.Start ();
				string LastCommitMessage = Process.StandardOutput.ReadToEnd ().Trim ();

				// Get the last commiter
				Process.StartInfo.Arguments = "log --format=\"%an\" -1";
				Process.Start ();
				string LastCommitUserName = Process.StandardOutput.ReadToEnd ().Trim ();

				string NotifySettingFile = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
					"sparkleshare.notify");

				if (File.Exists (NotifySettingFile)) {

					SparkleHelpers.DebugInfo ("Notification", "[" + Name + "] Showing message...");

					SparkleEventArgs args = new SparkleEventArgs ("NewCommit");
					
					if (NewCommit != null)
				        NewCommit (this, args);

//					SparkleBubble StuffChangedBubble = new SparkleBubble (LastCommitUserName, LastCommitMessage);
	//				StuffChangedBubble.Icon = SparkleHelpers.GetAvatar (LastCommitEmail, 32);

						// Add a button to open the folder where the changed file is
		/*				StuffChangedBubble.AddAction ("", _("Open Folder"),
	  						delegate {
								switch (SparklePlatform.Name) {
									case "GNOME":
								        Process.StartInfo.FileName = "xdg-open";
										break;
									case "OSX":
								        Process.StartInfo.FileName = "open";
										break;
								}
								Process.StartInfo.Arguments = LocalPath;
								Process.Start ();
				  		      	Process.StartInfo.FileName = "git";
					    	} );

					StuffChangedBubble.Show ();
*/
				}
						              
			}

			Watcher.EnableRaisingEvents = true;
			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Idling...");

		}


		// Pushes the changes to the remote repo
		public void Push ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing changes...");

			Process.StartInfo.Arguments = "push";
			Process.Start ();
			Process.WaitForExit ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes pushed.");

			SparkleEventArgs args = new SparkleEventArgs ("Pushed");

			if (Pushed != null)
	            Pushed (this, args); 

//			SparkleUI.NotificationIcon.SetIdleState ();

		}


		// Ignores repos, dotfiles, swap files and the like.
		private bool ShouldIgnore (string file_name) {

			if (file_name.Substring (0, 1).Equals (".") ||
			    file_name.Contains (".lock") ||
			    file_name.Contains (".git") ||
			    file_name.Contains ("/.") ||
			    Directory.Exists (LocalPath + file_name)) {

				return true; // Yes, ignore it.

			} else if (file_name.Length > 3 &&
			           file_name.Substring (file_name.Length - 4).Equals (".swp")) {

				return true; // Yes, ignore it.

			} else {

				return false;
				
			}

		}


		public string GetDomain (string url)
		{

			string domain;

			domain = url.Substring (RemoteOriginUrl.IndexOf ("@") + 1);
			if (domain.IndexOf (":") > -1)
				domain = domain.Substring (0, domain.IndexOf (":"));
			else
				domain = domain.Substring (0, domain.IndexOf ("/"));

			return domain;

		}


		// Gets hash of the current commit
		public string GetCurrentHash ()
		{

			string current_hash;

			Process process = new Process ();
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "rev-list --max-count=1 HEAD";
			process.Start ();

			current_hash = process.StandardOutput.ReadToEnd ().Trim ();
			
			return current_hash;

		}


		// Gets the user's name, example: "User Name"
		public string GetUserName ()
		{

			string user_name;

			Process process = new Process ();
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "config --get user.name";
			process.Start ();

			user_name = process.StandardOutput.ReadToEnd ().Trim ();

			if (user_name.Equals ("")) {

				UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);

				if (unix_user_info.RealName.Equals (""))
					user_name = "???";
				else
					user_name = unix_user_info.RealName;

			}

			return user_name;

		}


		// Gets the user's email, example: "person@gnome.org"
		public string GetUserEmail ()
		{

			string user_email;

			Process process = new Process ();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "config --get user.email";
			process.Start ();
			user_email = process.StandardOutput.ReadToEnd ().Trim ();

			return user_email;

		}


		// Gets the url of the remote repo, example: "ssh://git@git.gnome.org/project"
		public string GetRemoteOriginUrl ()
		{

				string remote_origin_url;

				Process process = new Process ();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.FileName = "git";
				process.StartInfo.Arguments = "config --get remote.origin.url";
				process.Start ();

				remote_origin_url = process.StandardOutput.ReadToEnd ().Trim ();

				return remote_origin_url;

		}


		// Creates a pretty commit message based on what has changed
		private string FormatCommitMessage ()
		{

			bool DoneAddCommit = false;
			bool DoneEditCommit = false;
			bool DoneRenameCommit = false;
			bool DoneDeleteCommit = false;
			int FilesAdded = 0;
			int FilesEdited = 0;
			int FilesRenamed = 0;
			int FilesDeleted = 0;

			Process.StartInfo.Arguments = "status";
			Process.Start ();
			string Output = Process.StandardOutput.ReadToEnd ();

			foreach (string line in Regex.Split (Output, "\n")) {
				if (line.IndexOf ("new file:") > -1)
					FilesAdded++;
				if (line.IndexOf ("modified:") > -1)
					FilesEdited++;
				if (line.IndexOf ("renamed:") > -1)
					FilesRenamed++;
				if (line.IndexOf ("deleted:") > -1)
					FilesDeleted++;
			}

			foreach (string line in Regex.Split (Output, "\n")) {

				// Format message for when files are added,
				// example: "added 'file' and 3 more."
				if (line.IndexOf ("new file:") > -1 && !DoneAddCommit) {
					DoneAddCommit = true;
					if (FilesAdded > 1)
						return "added ‘" + 
							line.Replace ("#\tnew file:", "").Trim () + 
							"’\nand " + (FilesAdded - 1) + " more.";
					else
						return "added ‘" + 
							line.Replace ("#\tnew file:", "").Trim () + "’.";
				}

				// Format message for when files are edited,
				// example: "edited 'file'."
				if (line.IndexOf ("modified:") > -1 && !DoneEditCommit) {
					DoneEditCommit = true;
					if (FilesEdited > 1)
						return "edited ‘" + 
							line.Replace ("#\tmodified:", "").Trim () + 
							"’\nand " + (FilesEdited - 1) + " more.";
					else
						return "edited ‘" + 
							line.Replace ("#\tmodified:", "").Trim () + "’.";
				}

				// Format message for when files are edited,
				// example: "deleted 'file'."
				if (line.IndexOf ("deleted:") > -1 && !DoneDeleteCommit) {
					DoneDeleteCommit = true;
					if (FilesDeleted > 1)
						return "deleted ‘" + 
							line.Replace ("#\tdeleted:", "").Trim () + 
							"’\nand " + (FilesDeleted - 1) + " more.";
					else
						return "deleted ‘" + 
							line.Replace ("#\tdeleted:", "").Trim () + "’.";
				}

				// Format message for when files are renamed,
				// example: "renamed 'file' to 'new name'."
				if (line.IndexOf ("renamed:") > -1 && !DoneRenameCommit) {
					DoneDeleteCommit = true;
					if (FilesRenamed > 1)
						return "renamed ‘" + 
							line.Replace ("#\trenamed:", "").Trim ().Replace
							(" -> ", "’ to ‘") + "’ and " + (FilesDeleted - 1) + 
							" more.";
					else
						return "renamed ‘" + 
							line.Replace ("#\trenamed:", "").Trim ().Replace
							(" -> ", "’ to ‘") + "’.";
				}

			}

			// Nothing happened:
			return "";

		}


		// Checks for unicorns
		public static void CheckForUnicorns (string s) {

			s = s.ToLower ();
			if (s.Contains ("unicorn") && (s.Contains (".png") || s.Contains (".jpg"))) {
				string title   = _("Hold your ponies!");
				string subtext = _("SparkleShare is known to be insanely fast with \n" +
				                  "pictures of unicorns. Please make sure your internets\n" +
				                  "are upgraded to the latest version to avoid problems.");
//				SparkleBubble unicorn_bubble = new SparkleBubble (title, subtext);
	//			unicorn_bubble.Show ();
			}

		}

	}


	public class SparkleEventArgs : System.EventArgs {
        
	    public string Message;

	    public SparkleEventArgs (string s)
    	{
	        Message = s;
	    }

	}

}




