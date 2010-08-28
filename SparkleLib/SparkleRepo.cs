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

// TODO: Repush changes when reconnected
// use hash

using Mono.Unix;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleLib {

	public class SparkleRepo {

		private Process Process;
		private Timer RemoteTimer;
		private Timer LocalTimer;
		private FileSystemWatcher Watcher;
		private bool HasChanged;
		private DateTime LastChange;
		private System.Object ChangeLock = new System.Object();
		private bool HasUnsyncedChanges;

		public string Name;
		public string Domain;
		public string Description;
		public string LocalPath;
		public string RemoteOriginUrl;
		public string CurrentHash;
		public string UserEmail;
		public string UserName;

		public delegate void AddedEventHandler (object o, SparkleEventArgs args);
		public delegate void CommitedEventHandler (object o, SparkleEventArgs args);
		public delegate void PushingStartedEventHandler (object o, SparkleEventArgs args);
		public delegate void PushingFinishedEventHandler (object o, SparkleEventArgs args);
		public delegate void PushingFailedEventHandler (object o, SparkleEventArgs args);
		public delegate void FetchingStartedEventHandler (object o, SparkleEventArgs args);
		public delegate void FetchingFinishedEventHandler (object o, SparkleEventArgs args);
		public delegate void NewCommitEventHandler (object o, NewCommitArgs args);
		public delegate void ConflictDetectedEventHandler (object o, SparkleEventArgs args);
		public delegate void ChangesDetectedEventHandler (object o, SparkleEventArgs args);

		public event AddedEventHandler Added; 
		public event CommitedEventHandler Commited; 
		public event PushingStartedEventHandler PushingStarted;
		public event PushingFinishedEventHandler PushingFinished;
		public event PushingFailedEventHandler PushingFailed;
		public event FetchingStartedEventHandler FetchingStarted;
		public event FetchingFinishedEventHandler FetchingFinished;
		public event NewCommitEventHandler NewCommit;
		public event ConflictDetectedEventHandler ConflictDetected;
		public event ChangesDetectedEventHandler ChangesDetected;


		public SparkleRepo (string path)
		{

			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);

			LocalPath = path;
			Name = Path.GetFileName (LocalPath);

			Process = new Process () {
				EnableRaisingEvents = true
			};

			Process.StartInfo.FileName = "git";
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.WorkingDirectory = LocalPath;

			UserName           = GetUserName ();
			UserEmail          = GetUserEmail ();
			RemoteOriginUrl    = GetRemoteOriginUrl ();
			CurrentHash        = GetCurrentHash ();
			Domain             = GetDomain (RemoteOriginUrl);
			Description        = GetDescription ();
			HasUnsyncedChanges = false;

			if (CurrentHash == null)
				CreateInitialCommit ();

			HasChanged = false;

			// Watch the repository's folder
			Watcher = new FileSystemWatcher (LocalPath) {
				IncludeSubdirectories = true,
				EnableRaisingEvents   = true,
				Filter                = "*"
			};

			Watcher.Changed += new FileSystemEventHandler (OnFileActivity);
			Watcher.Created += new FileSystemEventHandler (OnFileActivity);
			Watcher.Deleted += new FileSystemEventHandler (OnFileActivity);


			// Fetch remote changes every minute
			RemoteTimer = new Timer () {
				Interval = 60000
			};

			RemoteTimer.Elapsed += delegate { 
				CheckForRemoteChanges ();
				if (HasUnsyncedChanges)
					Push ();
			};


			// Keep a Local that checks if there are changes and
			// whether they have settled
			LocalTimer = new Timer () {
				Interval = 4000
			};

			LocalTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				CheckForChanges ();
			};

			RemoteTimer.Start ();
			LocalTimer.Start ();


			// Add everything that changed 
			// since SparkleShare was stopped
			AddCommitAndPush ();

			if (CurrentHash == null)
				CurrentHash = GetCurrentHash ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Idling...");

		}


		private void CheckForRemoteChanges ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Checking for remote changes...");

			Process process = new Process () {
				EnableRaisingEvents = true
			};

			process.StartInfo.FileName               = "git";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.WorkingDirectory       = LocalPath;
			process.StartInfo.Arguments = "ls-remote origin master";
			process.Start ();

			process.Exited += delegate {
			
				if (process.ExitCode != 0)
					return;

				string remote_hash = process.StandardOutput.ReadToEnd ();

				if (!remote_hash.StartsWith (CurrentHash)) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found.");
					Fetch ();

				}

			};

		}


		private void CheckForChanges ()
		{

			lock (ChangeLock) {

				if (HasChanged) {

					SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes found, checking if settled.");

					DateTime now     = DateTime.UtcNow;
					TimeSpan changed = new TimeSpan (now.Ticks - LastChange.Ticks);

					if (changed.TotalMilliseconds > 5000) {
						HasChanged = false;
						SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled, adding files...");
						AddCommitAndPush ();
					}

				}

			}

		}


		// Starts a timerwhen something changes
		private void OnFileActivity (object o, FileSystemEventArgs fse_args)
		{

			WatcherChangeTypes wct = fse_args.ChangeType;

			if (!ShouldIgnore (fse_args.Name)) {

				// Only fire the event if the timer has been stopped.
				// This prevents multiple events from being raised whilst "buffering".
				if (!HasChanged) {

					SparkleEventArgs args = new SparkleEventArgs ("ChangesDetected");
Console.WriteLine ("test");
					if (ChangesDetected != null)
					    ChangesDetected (this, args);

				}

				SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + fse_args.Name + "'");

				RemoteTimer.Stop ();

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

				LocalTimer.Stop ();
				RemoteTimer.Stop ();
	
				Add ();

				string message = FormatCommitMessage ();

				if (!message.Equals ("")) {

					Commit (message);
					CheckForRemoteChanges ();
					Push ();

				}

			} finally {

				RemoteTimer.Start ();
				LocalTimer.Start ();

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
		public void Commit (string message)
		{

			SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message);

			Process.StartInfo.Arguments = "commit -m \"" + message + "\"";
			Process.Start ();
			Process.WaitForExit ();

			SparkleEventArgs args = new SparkleEventArgs ("Commited");
			args.Message = message;

			if (Commited != null)
	            Commited (this, args);

		}


		// Fetches changes from the remote repository
		public void Fetch ()
		{

			RemoteTimer.Stop ();

			Process process = new Process () {
				EnableRaisingEvents = true
			};

			process.StartInfo.FileName               = "git";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.WorkingDirectory       = LocalPath;

			SparkleEventArgs args;
			args = new SparkleEventArgs ("FetchingStarted");

			if (FetchingStarted != null)
		        FetchingStarted (this, args); 

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Fetching changes...");

			process.StartInfo.Arguments = "fetch -v origin master";

			process.Start ();

			process.Exited += delegate {

				SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes fetched.");

				args = new SparkleEventArgs ("FetchingFinished");

				if (FetchingFinished != null)
				    FetchingFinished (this, args); 

				Rebase ();

				RemoteTimer.Start ();

				CurrentHash = GetCurrentHash ();

			};

		}


		// Merges the fetched changes
		public void Rebase ()
		{

			Add ();

			Watcher.EnableRaisingEvents = false;

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Rebasing changes...");

			Process.StartInfo.Arguments = "rebase -v FETCH_HEAD";
			Process.WaitForExit ();
			Process.Start ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes rebased.");

			string output = Process.StandardOutput.ReadToEnd ().Trim ();

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
							
							string timestamp = DateTime.Now.ToString ("H:mm d MMM yyyy");

							File.Move (problem_file_name, problem_file_name + " (" + UserName  + ", " + timestamp + ")");
							           
							Process.StartInfo.Arguments = "checkout --theirs " + problem_file_name;
							Process.WaitForExit ();
							Process.Start ();

							SparkleEventArgs args = new SparkleEventArgs ("ConflictDetected");

							if (ConflictDetected != null)
								ConflictDetected (this, args); 

						}

					}

					Add ();
					
					Process.StartInfo.Arguments = "rebase --continue";
					Process.WaitForExit ();
					Process.Start ();

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict resolved.");

					Push ();
			
				}

				// Get the last commiter
				Process.StartInfo.Arguments = "log --format=\"%an\" -1";
				Process.Start ();
				string author = Process.StandardOutput.ReadToEnd ().Trim ();

				// Get the last committer e-mail
				Process.StartInfo.Arguments = "log --format=\"%ae\" -1";
				Process.Start ();
				string email = Process.StandardOutput.ReadToEnd ().Trim ();

				// Get the last commit message
				Process.StartInfo.Arguments = "log --format=\"%s\" -1";
				Process.Start ();
				string message = Process.StandardOutput.ReadToEnd ().Trim ();

				NewCommitArgs new_commit_args = new NewCommitArgs (author, email, message, Name);

				if (NewCommit != null)
			        NewCommit (this, new_commit_args);
						              
			}

			Watcher.EnableRaisingEvents = true;
			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Idling...");

		}


		// Pushes the changes to the remote repo
		public void Push ()
		{

			SparkleEventArgs args = new SparkleEventArgs ("PushingStarted");

			if (PushingStarted != null)
	            PushingStarted (this, args); 

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing changes...");

			Process.StartInfo.Arguments = "push origin master";
			Process.Start ();
			Process.WaitForExit ();

			Process.Exited += delegate {

				if (Process.ExitCode != 0) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing failed.");

					HasUnsyncedChanges = true;

					args = new SparkleEventArgs ("PushingFailed");

					if (PushingFailed != null)
					    PushingFailed (this, args); 

				} else {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes pushed.");

					args = new SparkleEventArgs ("PushingFinished");

					HasUnsyncedChanges = false;

					if (PushingFinished != null)
					    PushingFinished (this, args); 

				}

			};

		}


		public void Stop ()
		{

			RemoteTimer.Stop ();
			LocalTimer.Stop ();

		}


		// Ignores repos, dotfiles, swap files and the like.
		private bool ShouldIgnore (string file_name)
		{

			if (file_name [0].Equals (".") ||
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


		// Gets the domain name of a given URL
		public string GetDomain (string url)
		{

			if (RemoteOriginUrl.Equals (""))
				return "";

			string domain = url.Substring (RemoteOriginUrl.IndexOf ("@") + 1);

			if (domain.IndexOf (":") > -1)
				domain = domain.Substring (0, domain.IndexOf (":"));
			else
				domain = domain.Substring (0, domain.IndexOf ("/"));

			return domain;

		}


		// Gets the repository's description
		public string GetDescription ()
		{

			string description_file_path = SparkleHelpers.CombineMore (LocalPath, ".git", "description");

			if (!File.Exists (description_file_path))
				return null;

			StreamReader reader = new StreamReader (description_file_path);
			string description = reader.ReadToEnd ();
			reader.Close ();

			if (description.StartsWith ("Unnamed"))
				description = null;

			return description;

		}


		// Gets hash of the current commit
		public string GetCurrentHash ()
		{

			Process process = new Process () {
				EnableRaisingEvents = true
			};

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.FileName               = "git";
			process.StartInfo.WorkingDirectory       = LocalPath;
			process.StartInfo.Arguments              = "rev-list --max-count=1 HEAD";

			process.Start ();
			process.WaitForExit ();

			string current_hash = process.StandardOutput.ReadToEnd ().Trim ();

			if (process.ExitCode != 0)
				return null;
			else
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
			process.StartInfo.WorkingDirectory = LocalPath;
			process.StartInfo.Arguments = "config --get user.name";
			process.Start ();

			user_name = process.StandardOutput.ReadToEnd ().Trim ();

			if (user_name.Equals ("")) {

				UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);

				if (unix_user_info.RealName.Equals (""))
					user_name = "Mysterious Stranger";
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
			process.StartInfo.WorkingDirectory = LocalPath;
			process.StartInfo.Arguments = "config --get user.email";
			process.Start ();

			user_email = process.StandardOutput.ReadToEnd ().Trim ();

			if (user_email.Equals (""))
				user_email = "Unknown Email";

			return user_email;

		}


		// Create a first commit in case the user has cloned
		// an empty repository
		private void CreateInitialCommit ()
		{

			TextWriter writer = new StreamWriter (Path.Combine (LocalPath, "SparkleShare.txt"));
			writer.WriteLine (":)");
			writer.Close ();

		}


		// Gets the url of the remote repo, example: "ssh://git@git.gnome.org/project"
		public string GetRemoteOriginUrl ()
		{

				string remote_origin_url;

				Process process = new Process ();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.FileName = "git";
				process.StartInfo.WorkingDirectory = LocalPath;
				process.StartInfo.Arguments = "config --get remote.origin.url";
				process.Start ();

				remote_origin_url = process.StandardOutput.ReadToEnd ().Trim ();

				return remote_origin_url;

		}


		// TODO: this is ugly. refactor.
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
			string output = Process.StandardOutput.ReadToEnd ();

			foreach (string line in Regex.Split (output, "\n")) {
				if (line.IndexOf ("new file:") > -1)
					FilesAdded++;
				if (line.IndexOf ("modified:") > -1)
					FilesEdited++;
				if (line.IndexOf ("renamed:") > -1)
					FilesRenamed++;
				if (line.IndexOf ("deleted:") > -1)
					FilesDeleted++;
			}

			foreach (string line in Regex.Split (output, "\n")) {

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

	}

	// Arguments for most events
	public class SparkleEventArgs : System.EventArgs {
        
	    public string Type;
	    public string Message;

	    public SparkleEventArgs (string type)
    	{

	        Type = type;

	    }

	}


	// Arguments for the NewCommit event
	public class NewCommitArgs : System.EventArgs {
        
	    public string Author;
	    public string Email;
	    public string Message;
	    public string RepositoryName;

	    public NewCommitArgs (string author, string email, string message, string repository_name)
    	{

    		Author  = author;
    		Email   = email;
	        Message = message;
	        RepositoryName = repository_name;

	    }

	}

}
