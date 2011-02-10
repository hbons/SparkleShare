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

		private Process Process;
		private Timer RemoteTimer;
		private Timer LocalTimer;
		private FileSystemWatcher Watcher;
		private bool HasChanged;
		private DateTime LastChange;
		private System.Object ChangeLock;
		private int FetchRequests;
		private SparkleListener Listener;

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

			LocalPath = path;
			Name = Path.GetFileName (LocalPath);

			Process = new Process () {
				EnableRaisingEvents = true
			};

			Process.StartInfo.FileName = SparklePaths.GitPath;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.WorkingDirectory = LocalPath;

			RemoteName          = Path.GetFileNameWithoutExtension (RemoteOriginUrl);
			RemoteOriginUrl     = Config ["remote.origin.url"];
			Domain              = GetDomain (RemoteOriginUrl);
			Description         = GetDescription ();

			UserName            = Config ["user.name"];
			UserEmail           = Config ["user.email"];

			if (Head.CurrentCommit == null)
				_CurrentHash    = null;
			else
				_CurrentHash    = Head.CurrentCommit.Hash;

			_IsSyncing          = false;
			_IsBuffering        = false;
			_IsPolling          = true;
			_IsFetching         = false;
			_IsPushing          = false;
			_ServerOnline       = true;

			string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
				".git", "has_unsynced_changes");

			if (File.Exists (unsynced_file_path))
				_HasUnsyncedChanges = true;
			else
				_HasUnsyncedChanges = false;


			if (_CurrentHash == null)
				CreateInitialCommit ();

			HasChanged = false;
			ChangeLock = new System.Object ();
			FetchRequests = 0;


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

			// Fetch remote changes every minute
			RemoteTimer = new Timer () {
				Interval = 60000
			};


			// Listen to the irc channel on the server
			Listener = new SparkleListener (Domain, "#" + RemoteName, UserEmail);

			RemoteTimer.Elapsed += delegate { 

				if (_IsPolling)
					CheckForRemoteChanges ();

				if (_HasUnsyncedChanges)
					Push ();

				if (!Listener.Client.IsConnected) {

					SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Trying to reconnect...");
					Listener.Client.Reconnect (true, true);

				}

			};

			// Stop polling when the connection to the irc channel is succesful
			Listener.Client.OnConnected += delegate {

				// Check for changes manually one more time
				CheckForRemoteChanges ();

				// Push changes that were made since the last disconnect
				if (_HasUnsyncedChanges)
					Push ();

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Connected. Now listening...");

				_IsPolling = false;

			};

			// Start polling when the connection to the irc channel is lost
			Listener.Client.OnDisconnected += delegate {

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Lost connection. Falling back to polling...");

				CheckForRemoteChanges ();

				_IsPolling = true;

			};

			// Fetch changes when there is a message in the irc channel
			Listener.Client.OnChannelMessage += delegate (object o, IrcEventArgs args) {

				SparkleHelpers.DebugInfo ("Irc", "[" + Name + "] Was notified of a remote change.");

				if (!args.Data.Message.Equals (_CurrentHash)) {

					FetchRequests++;

					if (!_IsFetching) {

						while (FetchRequests > 0) {

							Fetch ();
							FetchRequests--;

						}

						Rebase ();

					}

				} else {

					SparkleHelpers.DebugInfo ("Irc",
						"[" + Name + "] False alarm, already up to date. (" + _CurrentHash + ")");

				}

			};

			// Start listening
			Listener.ListenForChanges ();


			// Keep a timer that checks if there are changes and
			// whether they have settled
			LocalTimer = new Timer () {
				Interval = 4000
			};

			LocalTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				CheckForChanges ();
			};


			if (_IsPolling)
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

			Process process = new Process () {
				EnableRaisingEvents = true
			};

			process.StartInfo.FileName               = SparklePaths.GitPath;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.WorkingDirectory       = LocalPath;
			process.StartInfo.Arguments = "ls-remote origin master";

			process.Exited += delegate {
			
				if (process.ExitCode != 0)
					return;

				string remote_hash = process.StandardOutput.ReadToEnd ();

				if (!remote_hash.StartsWith (_CurrentHash)) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found.");
					Fetch ();
					Rebase ();

				}

			};

			process.Start ();

/* FIXME: LsRemoteCommand is not yet implemented by GitSharp

			LsRemoteCommand ls_remote = new LsRemoteCommand () {
				Repository = this
			};

			ls_remote.Execute ();

			using (StreamReader reader = new StreamReader (ls_remote.OutputStream.BaseStream))
			{

				string remote_hash = reader.ReadLine ());

				if (!remote_hash.StartsWith (_CurrentHash)) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Remote changes found.");

					Fetch ();
					Rebase ();

				}

			}
*/

		}


		private void CheckForChanges ()
		{

			lock (ChangeLock) {

				if (HasChanged) {

					SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes found, checking if settled.");

					DateTime now     = DateTime.UtcNow;
					TimeSpan changed = new TimeSpan (now.Ticks - LastChange.Ticks);

					if (changed.TotalMilliseconds > 5000) {

						SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled, adding files...");

						_IsBuffering = false;

						HasChanged = false;
						AddCommitAndPush ();

					}

				}

			}

		}


		// Starts a timer when something changes
		private void OnFileActivity (object o, FileSystemEventArgs fse_args)
		{

			WatcherChangeTypes wct = fse_args.ChangeType;

			if (!ShouldIgnore (fse_args.FullPath)) {

				_IsBuffering = true;

				// Only fire the event if the timer has been stopped.
				// This prevents multiple events from being raised whilst "buffering".
				if (!HasChanged) {

					SparkleEventArgs args = new SparkleEventArgs ("ChangesDetected");

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


		// When there are changes we generally want to Add, Commit and Push,
		// so this method does them all with appropriate timers, etc. switched off
		public void AddCommitAndPush ()
		{

			try {

				LocalTimer.Stop ();
				RemoteTimer.Stop ();
	
				Add ();

				string message = FormatCommitMessage ();

				if (message != null) {

					Commit (message);
					CheckForRemoteChanges ();
					Push ();

				} else {

					SparkleEventArgs args = new SparkleEventArgs ("CommitEndedUpEmpty");

					if (CommitEndedUpEmpty != null)
					    CommitEndedUpEmpty (this, args); 

				}

			} finally {

				if (_IsPolling)
					RemoteTimer.Start ();

				LocalTimer.Start ();

			}

		}


		// Stages the made changes
		private void Add ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Staging changes...");

			// FIXME: this GitSharp method seems to block...
			// Index.AddAll ();
			Process.StartInfo.Arguments = "add --all";
			Process.Start ();
			Process.WaitForExit ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes staged.");

			SparkleEventArgs args = new SparkleEventArgs ("Added");

			if (Added != null)
	            Added (this, args); 

		}


		// Removes unneeded objects
		private void CollectGarbage ()
		{

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Collecting garbage...");

			Process.StartInfo.Arguments = "gc";
			Process.Start ();
			Process.WaitForExit ();

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Garbage collected..");

		}


		// Commits the made changes
		new public void Commit (string message)
		{

			if (!Status.AnyDifferences)
				return;

			base.Commit (message);

			SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message);

			SparkleEventArgs args = new SparkleEventArgs ("Commited") {
				Message = message
			};

			if (Commited != null)
	            Commited (this, args);
			
			// Collect garbage pseudo-randomly
			if (DateTime.Now.Second == 0)
				CollectGarbage ();

		}


		// Fetches changes from the remote repository
		public void Fetch ()
		{

			_IsSyncing  = true;
			_IsFetching = true;

			RemoteTimer.Stop ();


/* FIXME: SSH transport doesn't work with GitSharp
			try {

				FetchCommand fetch_command = new FetchCommand () {
					Remote = "origin",
					Repository = this
				};

				fetch_command.Execute ();

			} catch (GitSharp.Core.Exceptions.TransportException e) {

				Console.WriteLine ("Nothing to fetch: " + e.Message);
			
			}
*/
			
			Process process = new Process () {
				EnableRaisingEvents = true
			};

			process.StartInfo.FileName               = SparklePaths.GitPath;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.WorkingDirectory       = LocalPath;

			SparkleEventArgs args;
			args = new SparkleEventArgs ("FetchingStarted");

			if (FetchingStarted != null)
		        FetchingStarted (this, args); 

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Fetching changes...");

			process.StartInfo.Arguments = "fetch -v origin master";

			process.Exited += delegate {

				SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Changes fetched.");

				args = new SparkleEventArgs ("FetchingFinished");

				_IsSyncing  = false;
				_IsFetching = false;

				if (_IsPolling)
					RemoteTimer.Start ();

				_CurrentHash = Head.CurrentCommit.Hash;

				if (process.ExitCode != 0) {

					_ServerOnline = false;

					if (FetchingFailed != null)
						FetchingFailed (this, args); 

				} else {

					_ServerOnline = true;

					if (FetchingFinished != null)
						FetchingFinished (this, args);

				}

			};

			process.Start ();
			process.WaitForExit ();

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


				List <SparkleCommit> commits = GetCommits (1);

				if (NewCommit != null)
			        NewCommit (commits [0], LocalPath);
						              
			}

			Watcher.EnableRaisingEvents = true;

		}


		// Pushes the changes to the remote repo
		public void Push ()
		{

			_IsSyncing = true;
			_IsPushing = true;

			SparkleEventArgs args = new SparkleEventArgs ("PushingStarted");

			if (PushingStarted != null)
	            PushingStarted (this, args); 

			SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing changes...");

/* FIXME: SSH transport doesn't work with GitSharp
			try {

				PushCommand push_command = new PushCommand () {
					Remote = "origin",
					Repository = this
				};

				push_command.Execute ();

			} catch (GitSharp.Core.Exceptions.TransportException e) {

				Console.WriteLine (e.Message);
			
			}
*/

			Process.StartInfo.Arguments = "push origin master";

			Process.WaitForExit ();
			
			Process.Exited += delegate {

				_IsSyncing = false;
				_IsPushing = false;

				if (Process.ExitCode != 0) {

					SparkleHelpers.DebugInfo ("Git", "[" + Name + "] Pushing failed.");

					string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath ,
						".git", "has_unsynced_changes");

					if (!File.Exists (unsynced_file_path))
						File.Create (unsynced_file_path);

					_HasUnsyncedChanges = true;

					args = new SparkleEventArgs ("PushingFailed");

					if (PushingFailed != null)
					    PushingFailed (this, args); 

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

				}

			};
			Process.Start ();

		}


		// Ignores repos, dotfiles, swap files and the like
		private bool ShouldIgnore (string file_path)
		{
			
			// TODO: check against .git/info/exclude
			if (file_path.EndsWith (".lock") ||
			    file_path.EndsWith ("~")     ||
			    file_path.Contains (".git")  ||
			    file_path.Contains ("/.")    ||
			    file_path.EndsWith (".swp")  ||
			    System.IO.Directory.Exists (Path.Combine (LocalPath, file_path))) {

				return true; // Yes, ignore it

			} else {

				return false;
				
			}

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

			if (count <= 0)
				return null;

			List <SparkleCommit> commits = new List <SparkleCommit> ();

			string commit_ref = "HEAD";

			try {

				for (int i = 0; i < count; i++) {

					Commit commit = new Commit (this, commit_ref);

					SparkleCommit sparkle_commit = new SparkleCommit ();

					sparkle_commit.UserName  = commit.Author.Name;
					sparkle_commit.UserEmail = commit.Author.EmailAddress;
					sparkle_commit.DateTime  = commit.CommitDate.DateTime;
					sparkle_commit.Hash      = commit.Hash;
					
					foreach (Change change in commit.Changes) {

						if (change.ChangeType.ToString ().Equals ("Added"))
							sparkle_commit.Added.Add (change.Path);

						if (change.ChangeType.ToString ().Equals ("Modified"))
							sparkle_commit.Edited.Add (change.Path);

						if (change.ChangeType.ToString ().Equals ("Deleted"))
							sparkle_commit.Deleted.Add (change.Path);

					}

					commits.Add (sparkle_commit);
					commit_ref += "^";

				}

			} catch (System.NullReferenceException) {

				// FIXME: Doesn't show the first commit because it throws
				// this exception before getting to it. Seems to be a bug in GitSharp

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
