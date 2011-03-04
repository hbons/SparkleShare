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


using GitSharp;
using GitSharp.Commands;
using GitSharp.Core.Transport;
using Meebey.SmartIrc4net;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleLib {

	public class SparkleRepo : Repository {

		private Timer RemoteTimer;
		private Timer LocalTimer;
		private FileSystemWatcher Watcher;
		private System.Object ChangeLock;
		private int FetchRequests;
		private SparkleListener Listener;
		private bool HasChanged;
		private List <double> SizeBuffer;


		/// <summary>
		/// The folder name the repository resides in locally
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// The folder name the repository resides in remotely
		/// </summary>
		public readonly string RemoteName;

		/// <summary>
		/// The domain the remote repository is on
		/// </summary>
		public readonly string Domain;

		/// <summary>
		/// The repository's description
		/// </summary>
		public readonly string Description;

		/// <summary>
		/// The path where the repository resides locally
		/// </summary>
		public readonly string LocalPath;

		/// <summary>
		/// The raw url used to sync with.
		/// </summary>
		public readonly string RemoteOriginUrl;

		private string _CurrentHash;
		private bool _IsSyncing;
		private bool _IsBuffering;
		private bool _IsPolling;
		private bool _IsFetching;
		private bool _IsPushing;
		private bool _HasUnsyncedChanges;
		private bool _ServerOnline;


		/// <summary>
		/// The hash of the last commit done in the repository
		/// </summary>
		public string CurrentHash {
			get {
				return _CurrentHash;
			}
		}

		/// <summary>
		/// The name of the user
		/// </summary>
		public readonly string UserName;


		/// <summary>
		/// The name of the user
		/// </summary>
		public readonly string UserEmail;


		/// <summary>
		/// Indicates whether the repository is currently waiting for local changes to settle
		/// </summary>
		public bool IsBuffering {
			get {
				return _IsBuffering;
			}
		}


		/// <summary>
		/// Indicates whether the repository is currently pushing changes
		/// </summary>
		public bool IsPushing {
			get {
				return _IsPushing;
			}
		}


		/// <summary>
		/// Indicates whether the repository has fallen back to polling the remote repository,
		/// instead of receiving instant notifications
		/// </summary>
		public bool IsPolling {
			get {
				return _IsPolling;
			}
		}


		/// <summary>
		/// Indicates whether the repository is currently fetching and/or pushing changes
		/// </summary>
		public bool IsSyncing {
			get {
				return _IsSyncing;
			}
		}


		/// <summary>
		/// Indicates whether the repository is currently fetching remote changes
		/// </summary>
		public bool IsFetching {
			get {
				return _IsFetching;
			}
		}


		/// <summary>
		/// Indicates whether the repository has local changes that aren't pushed remotely yet
		/// </summary>
		public bool HasUnsyncedChanges {
			get {
				return _HasUnsyncedChanges;
			}
		}


		/// <summary>
		/// Indicates whether the remote repository is online, 
		/// this is based on the result of the Fetch method
		/// </summary>
		public bool ServerOnline {
			get {
				return _ServerOnline;
			}
		}


		/// <event cref="Added">
		/// Raised when local files have been added to the repository's staging area
		/// </event>
		public delegate void AddedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="Commited">
		/// Raised when local files have been added to the repository's index
		/// </event>
		public delegate void CommitedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="PushingStarted">
		/// Raised when the repository has started pushing changes
		/// </event>
		public delegate void PushingStartedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="PushingFinished">
		/// Raised when the repository has finished pushing changes
		/// </event>
		public delegate void PushingFinishedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="PushingFailed">
		/// Raised when pushing changes has failed
		/// </event>
		public delegate void PushingFailedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="FetchingStarted">
		/// Raised when when the repository has started fetching remote changes
		/// </event>
		public delegate void FetchingStartedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="FetchingFinished">
		/// Raised when when the repository has finished fetching remote changes
		/// </event>
		public delegate void FetchingFinishedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="FetchingFailed">
		/// Raised when when fetching from the remote repository has failed
		/// </event>
		public delegate void FetchingFailedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="NewCommit">
		/// Raised when the repository has received one or multiple new remote commits
		/// </event>
		public delegate void NewCommitEventHandler (SparkleCommit commit, string repository_path);

		/// <event cref="ConflictDetected">
		/// Raised when the newly fetched commits are conflicting with local changes
		/// </event>
		public delegate void ConflictDetectedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="ChangesDetected">
		/// Raised when local files have changed in the repository's folder
		/// </event>
		public delegate void ChangesDetectedEventHandler (object o, SparkleEventArgs args);

		/// <event cref="CommitEndedUpEmpty">
		/// Raised when there were changes made to local files, but the net result after changes have settled 
		/// ended up the same as before the changes were made.
		/// </event>
		public delegate void CommitEndedUpEmptyEventHandler (object o, SparkleEventArgs args);

		public event AddedEventHandler Added; 
		public event CommitedEventHandler Commited; 
		public event PushingStartedEventHandler PushingStarted;
		public event PushingFinishedEventHandler PushingFinished;
		public event PushingFailedEventHandler PushingFailed;
		public event FetchingStartedEventHandler FetchingStarted;
		public event FetchingFinishedEventHandler FetchingFinished;
		public event FetchingFailedEventHandler FetchingFailed;
		public event NewCommitEventHandler NewCommit;
		public event ConflictDetectedEventHandler ConflictDetected;
		public event ChangesDetectedEventHandler ChangesDetected;
		public event CommitEndedUpEmptyEventHandler CommitEndedUpEmpty;


		public SparkleRepo (string path) : base (path)
		{
			
			LocalPath       = path;
			Name            = Path.GetFileName (LocalPath);

			RemoteName      = Path.GetFileNameWithoutExtension (RemoteOriginUrl);
			RemoteOriginUrl = Config ["remote.origin.url"];
			Domain          = GetDomain (RemoteOriginUrl);
			Description     = GetDescription ();
			UserName        = Config ["user.name"];
			UserEmail       = Config ["user.email"];

			if (Head.CurrentCommit == null)
				_CurrentHash = null;
			else
				_CurrentHash = Head.CurrentCommit.Hash;

			_IsSyncing     = false;
			_IsBuffering   = false;
			_IsPolling     = true;
			_IsFetching    = false;
			_IsPushing     = false;
			_ServerOnline  = true;
			
			HasChanged     = false;
			ChangeLock     = new Object ();
			FetchRequests  = 0;
			

			string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
				".git", "has_unsynced_changes");

			if (File.Exists (unsynced_file_path))
				_HasUnsyncedChanges = true;
			else
				_HasUnsyncedChanges = false;


			if (_CurrentHash == null)
				CreateInitialCommit ();


			// Watch the repository's folder
			Watcher = new FileSystemWatcher (LocalPath) {
				IncludeSubdirectories = true,
				EnableRaisingEvents   = true,
				Filter                = "*"
			};

			Watcher.Changed += new FileSystemEventHandler (OnFileActivity);
			Watcher.Created += new FileSystemEventHandler (OnFileActivity);
			Watcher.Deleted += new FileSystemEventHandler (OnFileActivity);
			Watcher.Renamed += new RenamedEventHandler (OnFileActivity);


			// Listen to the irc channel on the server...
			Listener = new SparkleListener (Domain, "#" + RemoteName, UserEmail);

			// ...fetch remote changes every 60 seconds if that fails
			RemoteTimer = new Timer () {
				Interval = 60000
			};

		
			RemoteTimer.Elapsed += delegate { 
				
				if (_IsPolling) {
					
					CheckForRemoteChanges ();
					
					if (!Listener.Client.IsConnected) {
					
						SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Trying to reconnect...");
						Listener.Listen ();
	
					}
					
				}

				if (_HasUnsyncedChanges)
					Push ();

			};

			// Stop polling when the connection to the irc channel is succesful
			Listener.Client.OnConnected += delegate {
				
				_IsPolling = false;
				
				// Check for changes manually one more time
				CheckForRemoteChanges ();

				// Push changes that were made since the last disconnect
				if (_HasUnsyncedChanges)
					Push ();

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Connected. Now listening... (" + Listener.Server + ")");

			};

			// Start polling when the connection to the irc channel is lost
			Listener.Client.OnConnectionError += delegate {

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Lost connection. Falling back to polling...");
				_IsPolling = true;

			};
			
			// Start polling when the connection to the irc channel is lost
			Listener.Client.OnDisconnected += delegate {

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Lost connection. Falling back to polling...");
				_IsPolling = true;

			};

			// Fetch changes when there is a message in the irc channel
			Listener.Client.OnChannelMessage += delegate (object o, IrcEventArgs args) {

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Was notified of a remote change.");
				string message = args.Data.Message.Trim ();
				
				if (!message.Equals (_CurrentHash) && message.Length == 40) {

					FetchRequests++;

					if (!_IsFetching) {

						while (FetchRequests > 0) {

							Fetch ();
							FetchRequests--;

						}
						
						Watcher.EnableRaisingEvents = false;
						Rebase ();
						Watcher.EnableRaisingEvents = true;

					}

				} else {
					
					// Not really needed as we won't be notified about our own messages
					SparkleHelpers.DebugInfo ("Irc",
						"[" + Name + "] False alarm, already up to date. (" + _CurrentHash + ")");

				}

			};

			// Start listening
			Listener.Listen ();
			

			SizeBuffer = new List <double> ();

			// Keep a timer that checks if there are changes and
			// whether they have settled
			LocalTimer = new Timer () {
				Interval = 250
			};

			LocalTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				CheckForChanges ();
			};


			RemoteTimer.Start ();
			LocalTimer.Start ();

			// Add everything that changed 
			// since SparkleShare was stopped
			AddCommitAndPush ();

			if (_CurrentHash == null)
				_CurrentHash = Head.CurrentCommit.Hash;

		}


		private void CheckForRemoteChanges ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Checking for remote changes...");
			SparkleGit git = new SparkleGit (LocalPath, "ls-remote origin master");
		
			git.Exited += delegate {
			
				if (git.ExitCode != 0)
					return;

				string remote_hash = git.StandardOutput.ReadToEnd ();

				if (!remote_hash.StartsWith (_CurrentHash)) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found.");
					Fetch ();
					
					Watcher.EnableRaisingEvents = false;
					Rebase ();
					Watcher.EnableRaisingEvents = true;

				}

			};
				

			git.Start ();
			git.WaitForExit ();

		}


		private void CheckForChanges ()
		{

			lock (ChangeLock) {

				if (HasChanged) {
					
					if (SizeBuffer.Count >= 4)
						SizeBuffer.RemoveAt (0);
						
					DirectoryInfo dir_info = new DirectoryInfo (LocalPath);
					SizeBuffer.Add (CalculateFolderSize (dir_info));

					if (SizeBuffer [0].Equals (SizeBuffer [1]) &&
					    SizeBuffer [1].Equals (SizeBuffer [2]) &&
					    SizeBuffer [2].Equals (SizeBuffer [3])) {

						SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled.");

						_IsBuffering = false;
						HasChanged   = false;
						
						while (AnyDifferences) {
							
							Watcher.EnableRaisingEvents = false;
							AddCommitAndPush ();
							Watcher.EnableRaisingEvents = true;
						
						}

					}

				}

			}

		}


		// Starts a timer when something changes
		private void OnFileActivity (object o, FileSystemEventArgs fse_args)
		{

			WatcherChangeTypes wct = fse_args.ChangeType;
			
			int number_of_changes = Status.Untracked.Count +
				                    Status.Missing.Count +
				                    Status.Modified.Count;
			
			if (number_of_changes > 0) {

				_IsBuffering = true;

				// Only fire the event if the timer has been stopped.
				// This prevents multiple events from being raised whilst "buffering".
				if (!HasChanged) {

					SparkleEventArgs args = new SparkleEventArgs ("ChangesDetected");

					if (ChangesDetected != null)
					    ChangesDetected (this, args);

				}

				SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + fse_args.Name + "'");
				SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes found, checking if settled.");
				
				RemoteTimer.Stop ();

				lock (ChangeLock) {

					HasChanged = true;

				}

			}

		}


		// When there are changes we generally want to Add, Commit and Push,
		// so this method does them all with appropriate timers, etc. switched off
		public void AddCommitAndPush ()
		{

			try {

				LocalTimer.Stop ();
				RemoteTimer.Stop ();
	
				if (AnyDifferences) {
					
					Add ();
					
					string message = FormatCommitMessage ();
					Commit (message);

					Push ();

				} else {

					SparkleEventArgs args = new SparkleEventArgs ("CommitEndedUpEmpty");

					if (CommitEndedUpEmpty != null)
					    CommitEndedUpEmpty (this, args); 

				}

			} finally {

				RemoteTimer.Start ();
				LocalTimer.Start ();

			}

		}
		
		
		public bool AnyDifferences {
		
			get {
			
				SparkleGit git = new SparkleGit (LocalPath, "status --porcelain");
				git.Start ();
				git.WaitForExit ();
				
				string output = git.StandardOutput.ReadToEnd ().Trim ();
				
				if (output.Length > 0)
					return true;
				else
					return false;
				
			}
			
		}


		// Stages the made changes
		private void Add ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Staging changes...");

			// FIXME: this GitSharp method seems to block...
			// Index.AddAll ();

			SparkleGit git = new SparkleGit (LocalPath, "add --all");
			git.Start ();
			git.WaitForExit ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes staged.");

			SparkleEventArgs args = new SparkleEventArgs ("Added");

			if (Added != null)
	            Added (this, args); 

		}


		// Removes unneeded objects
		private void CollectGarbage ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Collecting garbage...");

			SparkleGit git = new SparkleGit (LocalPath, "gc");
			git.Start ();
			git.WaitForExit ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Garbage collected..");

		}


		// Commits the made changes
		new public void Commit (string message)
		{

			if (!AnyDifferences)
				return;

			base.Commit (message);
			_CurrentHash = Head.CurrentCommit.Hash;
			
			SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message + " (" + _CurrentHash);

			SparkleEventArgs args = new SparkleEventArgs ("Commited") {
				Message = message
			};

			if (Commited != null)
	            Commited (this, args);
			
			// Collect garbage pseudo-randomly
			if (DateTime.Now.Second % 10 == 0)
				CollectGarbage ();

		}


		// Fetches changes from the remote repository
		public void Fetch ()
		{

			_IsSyncing  = true;
			_IsFetching = true;

			RemoteTimer.Stop ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Fetching changes...");

			SparkleGit git = new SparkleGit (LocalPath, "fetch -v origin master");

			SparkleEventArgs args;
			args = new SparkleEventArgs ("FetchingStarted");

			if (FetchingStarted != null)
		        FetchingStarted (this, args); 


			git.Exited += delegate {

				SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes fetched.");

				_IsSyncing  = false;
				_IsFetching = false;

				_CurrentHash = Head.CurrentCommit.Hash;

				if (git.ExitCode != 0) {

					_ServerOnline = false;
					
					args = new SparkleEventArgs ("FetchingFailed");
					
					if (FetchingFailed != null)
						FetchingFailed (this, args); 

				} else {

					_ServerOnline = true;
					
					args = new SparkleEventArgs ("FetchingFinished");

					if (FetchingFinished != null)
						FetchingFinished (this, args);

				}

				RemoteTimer.Start ();

			};


			git.Start ();
			git.WaitForExit ();

		}


		// Merges the fetched changes
		public void Rebase ()
		{
			
			if (AnyDifferences) {
				
				Add ();
				
				string commit_message = FormatCommitMessage ();
				Commit (commit_message);

			}

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Rebasing changes...");
			SparkleGit git = new SparkleGit (LocalPath, "rebase -v FETCH_HEAD");

			git.Exited += delegate {

				if (Status.MergeConflict.Count > 0) {
					
					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict detected...");

					foreach (string problem_file_name in Status.MergeConflict) {
					
						SparkleGit git_ours = new SparkleGit (LocalPath,
					    	"checkout --ours " + problem_file_name);
						git_ours.Start ();
						git_ours.WaitForExit ();
	
						string timestamp = DateTime.Now.ToString ("H:mm d MMM");
	
						string new_file_name = problem_file_name + " (" + UserName  + ", " + timestamp + ")";
						File.Move (problem_file_name, new_file_name);
								           
						SparkleGit git_theirs = new SparkleGit (LocalPath,
					    	"checkout --theirs " + problem_file_name);
						git_theirs.Start ();
						git_theirs.WaitForExit ();
					
						SparkleEventArgs args = new SparkleEventArgs ("ConflictDetected");
	
						if (ConflictDetected != null)
							ConflictDetected (this, args);
	
					}
	
					Add ();
						
					SparkleGit git_continue = new SparkleGit (LocalPath, "rebase --continue");
					git_continue.Start ();
					git_continue.WaitForExit ();

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Conflict resolved.");
	
					Push ();
						              
				}
				
			};


			git.Start ();
			git.WaitForExit ();

			_CurrentHash = Head.CurrentCommit.Hash;

			if (NewCommit != null)
				NewCommit (GetCommits (1) [0], LocalPath);
				
			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes rebased.");

		}


		// Pushes the changes to the remote repo
		public void Push ()
		{

			_IsSyncing = true;
			_IsPushing = true;
			
			SparkleGit git = new SparkleGit (LocalPath, "push origin master");

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing changes...");

			SparkleEventArgs args = new SparkleEventArgs ("PushingStarted");
			
			if (PushingStarted != null)
	            PushingStarted (this, args); 

	
			git.Exited += delegate {

				_IsSyncing = false;
				_IsPushing = false;

				if (git.ExitCode != 0) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing failed.");

					string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
						".git", "has_unsynced_changes");

					if (!File.Exists (unsynced_file_path))
						File.Create (unsynced_file_path);

					_HasUnsyncedChanges = true;

					args = new SparkleEventArgs ("PushingFailed");

					if (PushingFailed != null)
					    PushingFailed (this, args);
					
					CheckForChanges ();

				} else {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes pushed.");

					args = new SparkleEventArgs ("PushingFinished");

					string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
						".git", "has_unsynced_changes");

					if (File.Exists (unsynced_file_path))
						File.Delete (unsynced_file_path);

					_HasUnsyncedChanges = false;

					if (PushingFinished != null)
					    PushingFinished (this, args); 
					
					if (!_IsPolling)
						Listener.Announce (_CurrentHash);

				}

			};

			
			git.Start ();
			git.WaitForExit ();

		}


		// Gets the domain name of a given URL
		private string GetDomain (string url)
		{

			if (url.Equals (""))
				return null;

			string domain = url.Substring (url.IndexOf ("@") + 1);

			if (domain.Contains (":"))
				domain = domain.Substring (0, domain.IndexOf (":"));
			else
				domain = domain.Substring (0, domain.IndexOf ("/"));

			return domain;

		}


		// Gets the repository's description
		private string GetDescription ()
		{

			string description_file_path = SparkleHelpers.CombineMore (Directory, "description");

			if (!File.Exists (description_file_path))
				return null;

			StreamReader reader = new StreamReader (description_file_path);
			string description = reader.ReadToEnd ();
			reader.Close ();

			if (description.StartsWith ("Unnamed"))
				description = null;

			return description;

		}
		

		// Recursively gets a folder's size in bytes
		private double CalculateFolderSize (DirectoryInfo parent)
		{

			if (!System.IO.Directory.Exists (parent.ToString ()))
				return 0;

			double size = 0;

			// Ignore the temporary 'rebase-apply' directory. This prevents potential
			// crashes when files are being queried whilst the files have already been deleted.
			if (parent.Name.Equals ("rebase-apply"))
				return 0;

			foreach (FileInfo file in parent.GetFiles()) {

				if (!file.Exists)
					return 0;

				size += file.Length;

			}

			foreach (DirectoryInfo directory in parent.GetDirectories())
				size += CalculateFolderSize (directory);

		    return size;
    
		}


		// Create a first commit in case the user has cloned
		// an empty repository
		private void CreateInitialCommit ()
		{

			TextWriter writer = new StreamWriter (Path.Combine (LocalPath, "SparkleShare.txt"));
			writer.WriteLine (":)");
			writer.Close ();

		}


		// Returns a list of latest commits
		public List <SparkleCommit> GetCommits (int count)
		{
			
			if (count < 1)
				count = 30;
			
			List <SparkleCommit> commits = new List <SparkleCommit> ();

			SparkleGit git_log = new SparkleGit (LocalPath, "log -" + count + " --raw  --date=iso");
			git_log.Start ();
			
			// Reading the standard output HAS to go before
			// WaitForExit, or it will hang forever on output > 4096 bytes
			string output = git_log.StandardOutput.ReadToEnd ();
			git_log.WaitForExit ();

			string [] lines = output.Split ("\n".ToCharArray ());
						
			List <string> entries = new List <string> ();

			int j = 0;
			string entry = "", last_entry = "";
			foreach (string line in lines) {

				if (line.StartsWith ("commit") && j > 0) {
					
					entries.Add (entry);
					entry = "";
					
				} 
				
				entry += line + "\n";
				j++;
				
				last_entry = entry;

			}
			
			entries.Add (last_entry);

			
			foreach (string log_entry in entries) {
				
				Regex regex;
				bool is_merge_commit = false;
				
				if (log_entry.Contains ("\nMerge: ")) {
				
					regex = new Regex (@"commit ([a-z0-9]{40})\n" +
					                          "Merge: .+ .+\n" +
						                      "Author: (.+) <(.+)>\n" +
						                      "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
						                      "([0-9]{2}):([0-9]{2}):([0-9]{2}) \\+([0-9]{4})\n" +
						                      "*");
					
					is_merge_commit = true;

				} else {

					regex = new Regex (@"commit ([a-z0-9]{40})\n" +
					                          "Author: (.+) <(.+)>\n" +
					                          "Date:   ([0-9]{4})-([0-9]{2})-([0-9]{2}) " +
					                          "([0-9]{2}):([0-9]{2}):([0-9]{2}) \\+([0-9]{4})\n" +
					                          "*");

				}
				
				Match match = regex.Match (log_entry);

				if (match.Success) {

					SparkleCommit commit = new SparkleCommit ();
					
					commit.Hash        = match.Groups [1].Value;
					commit.UserName    = match.Groups [2].Value;
					commit.UserEmail   = match.Groups [3].Value;
					commit.IsMerge = is_merge_commit;

					commit.DateTime = new DateTime (int.Parse (match.Groups [4].Value),
						int.Parse (match.Groups [5].Value), int.Parse (match.Groups [6].Value),
					    int.Parse (match.Groups [7].Value), int.Parse (match.Groups [8].Value),
					    int.Parse (match.Groups [9].Value));
					                    
					string [] entry_lines = log_entry.Split ("\n".ToCharArray ());
					
					if (entry_lines.Length > 60) {
						
						commit.IsFileDump = true;
						
					} else {
									
						foreach (string entry_line in entry_lines) {
	
							if (entry_line.StartsWith (":")) {
															
								string change_type = entry_line [37].ToString ();
								string file_path   = entry_line.Substring (39);
								
								if (change_type.Equals ("A")) {
									
									commit.Added.Add (file_path);
									
								} else if (change_type.Equals ("M")) {
								
									commit.Edited.Add (file_path);
									
								} else if (change_type.Equals ("D")) {
									
									commit.Deleted.Add (file_path);
									
								}
								
							}
								
						}
						
					}
	
					commits.Add (commit);
					
				}	
				
			}

			return commits;

		}


		// Creates a pretty commit message based on what has changed
		private string FormatCommitMessage ()
		{

			// RepositoryStatus contains the following properties (all HashSet <string>)
			// 
			// * Added         ---> added and staged
			// * MergeConflict --->
			// * Missing       ---> removed but not staged
			// * Modified      ---> modified but not staged
			// * Removed       ---> removed and staged
			// * Staged        ---> modified and staged
			// * Untracked     ---> added but not staged
			//
			// Because we create the commit message, we only need to consider the staged changes

			RepositoryStatus status = Index.Status;

			string file_name = "";
			string message = null;

			if (status.Added.Count > 0) {

				foreach (string added in status.Added) {
					file_name = added;
					break;
				}

				message = "+ ‘" + file_name + "’";

			}

			if (status.Staged.Count > 0) {

				foreach (string modified in status.Staged) {
					file_name = modified;
					break;
			}

				message = "/ ‘" + file_name + "’";

			}

			if (status.Removed.Count > 0) {

				foreach (string removed in status.Removed) {
					file_name = removed;
					break;
			}

				message = "- ‘" + file_name + "’";

			}

			int changes_count = (status.Added.Count +
								 status.Staged.Count +
			                     status.Removed.Count);

			if (changes_count > 1)
				message += " + " + (changes_count - 1);

			return message;

		}


		// Disposes all resourses of this object
		new public void Dispose ()
		{

			RemoteTimer.Dispose ();
			LocalTimer.Dispose ();
			Listener.Dispose ();

			base.Dispose ();

		}

	}

}
