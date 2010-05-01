//   SparklePony 0.0.8

//   SparklePony, an instant update workflow to Git.
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
using SparklePonyHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

// This is SparklePony!
public class SparklePony {

	public static SparklePonyUI SparklePonyUI;

	public static void Main (string [] args) {

		// Check if git is installed
		Process Process = new Process();
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;
		Process.StartInfo.FileName = "git";
		Process.Start();
		if (Process.StandardOutput.ReadToEnd().IndexOf ("version") == -1) {
			Console.WriteLine ("Git wasn't found.");
			Console.WriteLine ("You can get it from http://git-scm.com/.");
			Environment.Exit (0);
		}

		// Don't allow running as root
		Process.StartInfo.FileName = "whoami";
		Process.Start();
		if (Process.StandardOutput.ReadToEnd().Trim ().Equals ("root")) {
			Console.WriteLine ("Sorry, you can't run SparklePony as root.");
			Console.WriteLine ("Things will go utterly wrong."); 
			Environment.Exit (0);
		}

		// Parse the command line arguments
		bool HideUI = false;
		if (args.Length > 0) {
			foreach (string Argument in args) {
				if (Argument.Equals ("--disable-gui") || Argument.Equals ("-d"))
					HideUI = true;
				if (Argument.Equals ("--help") || Argument.Equals ("-h")) {
					ShowHelp ();
				}
			}
		}

		Gtk.Application.Init ();

		SparklePonyUI = new SparklePonyUI (HideUI);
		SparklePonyUI.StartMonitoring ();

		Gtk.Application.Run ();

	}

	public static void ShowHelp () {
		Console.WriteLine ("SparklePony Copyright (C) 2010 Hylke Bons");
		Console.WriteLine ("");
		Console.WriteLine ("This program comes with ABSOLUTELY NO WARRANTY.");
		Console.WriteLine ("This is free software, and you are welcome to redistribute it ");
		Console.WriteLine ("under certain conditions. Please read the GNU GPLv3 for details.");
		Console.WriteLine ("");
		Console.WriteLine ("SparklePony syncs the ~/SparklePony folder with remote repositories.");
		Console.WriteLine ("");
		Console.WriteLine ("Usage: sparklepony [start|stop|restart] [OPTION]...");
		Console.WriteLine ("Sync SparklePony folder with remote repositories.");
		Console.WriteLine ("");
		Console.WriteLine ("Arguments:");
		Console.WriteLine ("\t -d, --disable-gui\tDon't show the notification icon.");
		Console.WriteLine ("\t -h, --help\t\tDisplay this help text.");
		Console.WriteLine ("");
		Environment.Exit (0);
	}

}

// Holds the status icon, window and repository list
public class SparklePonyUI {

	public SparklePonyWindow SparklePonyWindow;
	public SparklePonyStatusIcon SparklePonyStatusIcon;
	public Repository [] Repositories;

	public SparklePonyUI (bool HideUI) {

		Process Process = new Process();
		Process.EnableRaisingEvents = false;
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;

		// Get home folder, example: "/home/user/" 
		string UserHome = Environment.GetEnvironmentVariable("HOME") + "/";

		// Create 'SparklePony' folder in the user's home folder
		string ReposPath = UserHome + "SparklePony";
		if (!Directory.Exists (ReposPath)) {
			Directory.CreateDirectory (ReposPath);
			Console.WriteLine ("[Config] Created '" + ReposPath + "'");

			Process.StartInfo.FileName = "gvfs-set-attribute";
			Process.StartInfo.Arguments = ReposPath + " metadata::custom-icon " +
			                              "file:///usr/share/icons/hicolor/" +
			                              "48x48/places/folder-sparklepony.png";
			Process.Start();

		}

		// Create place to store configuration user's home folder
		string ConfigPath = UserHome + ".config/sparklepony/";
		if (!Directory.Exists (ConfigPath)) {

			Directory.CreateDirectory (ConfigPath);
			Console.WriteLine ("[Config] Created '" + ConfigPath + "'");

			// Create a first run file to show the intro message
			File.Create (ConfigPath + "firstrun");
			Console.WriteLine ("[Config] Created '" + ConfigPath + "firstrun'");

			// Create a place to store the avatars
			Directory.CreateDirectory (ConfigPath + "avatars/");
			Console.WriteLine ("[Config] Created '" + ConfigPath + "avatars'");

			Directory.CreateDirectory (ConfigPath + "avatars/24x24/");
			Console.WriteLine ("[Config] Created '" + ConfigPath + 
			                   "avatars/24x24/'");

			Directory.CreateDirectory (ConfigPath + "avatars/48x48/");
			Console.WriteLine ("[Config] Created '" + ConfigPath + 
			                   "avatars/48x48/'");
		}

		// Get all the repos in ~/SparklePony
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
			SparklePonyWindow = new SparklePonyWindow (Repositories);
			SparklePonyWindow.DeleteEvent += CloseSparklePonyWindow;

			// Create the status icon
			SparklePonyStatusIcon = new SparklePonyStatusIcon ();
			SparklePonyStatusIcon.Activate += delegate { 
				SparklePonyWindow.ToggleVisibility ();
			};

		}

	}

	// Closes the window
	public void CloseSparklePonyWindow (object o, DeleteEventArgs args) {
		SparklePonyWindow = new SparklePonyWindow (Repositories);
		SparklePonyWindow.DeleteEvent += CloseSparklePonyWindow;
	}

	public void StartMonitoring () {	}
	public void StopMonitoring () { }

}

public class SparklePonyStatusIcon : StatusIcon {

	public SparklePonyStatusIcon () : base ()  {

		IconName = "folder-sparklepony";

		string UserHome = Environment.GetEnvironmentVariable("HOME") + "/";
		string FirstRunFile = UserHome + ".config/sparklepony/firstrun";

		// Show a notification on the first run
		if (File.Exists (FirstRunFile)) {

			Notification Notification;
			Notification = new Notification ("Welcome to SparklePony!",
				                               "Click here to add some folders.");

			Notification.Urgency = Urgency.Normal;
			Notification.Timeout = 7500;
			Notification.Show ();

			File.Delete (FirstRunFile);
			Console.WriteLine ("[Config] Deleted '" + FirstRunFile + "'");
		}

	}

	public void SetIdleState () {
		IconName = "folder-sparklepony";
	}

	public void SetSyncingState () {
		IconName = "view-refresh"; // Massively abusing this icon here :)
	}

}


// Repository class holds repository information and timers
public class Repository {

	private Process Process;
	private Timer FetchTimer;
	private Timer BufferTimer;
	private FileSystemWatcher Watcher;

	public string Name;
	public string Domain;
	public string RepoPath;
	public string RemoteOriginUrl;
	public string CurrentHash;

	public string UserEmail;
	public string UserName;
	public bool MonitorOnly;

	public Repository (string Path) {

		MonitorOnly = false;

		Process = new Process();
		Process.EnableRaisingEvents = false; 
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;

		// Get the repository's path, example: "/home/user/SparklePony/repo/"
		RepoPath = Path;
		Process.StartInfo.WorkingDirectory = RepoPath + "/";

		// Get user.name, example: "User Name"
		UserName = "Anonymous";
		Process.StartInfo.FileName = "git";
		Process.StartInfo.Arguments = "config --get user.name";
		Process.Start();
		UserName = Process.StandardOutput.ReadToEnd().Trim ();

		// Get user.email, example: "user@github.com"
		UserEmail = "not.set@git-scm.com";
		Process.StartInfo.FileName = "git";
		Process.StartInfo.Arguments = "config --get user.email";
		Process.Start();
		UserEmail = Process.StandardOutput.ReadToEnd().Trim ();

		// Get remote.origin.url, example: "ssh://git@github.com/user/repo"
		Process.StartInfo.FileName = "git";
		Process.StartInfo.Arguments = "config --get remote.origin.url";
		Process.Start();
		RemoteOriginUrl = Process.StandardOutput.ReadToEnd().Trim ();

		// Get the repository name, example: "Project"

		string s = RepoPath.TrimEnd ( "/".ToCharArray ());
		Name = RepoPath.Substring (s.LastIndexOf ("/") + 1);

		// Get the domain, example: "github.com" 
		Domain = RemoteOriginUrl; 
		Domain = Domain.Substring (Domain.IndexOf ("@") + 1);
		if (Domain.IndexOf (":") > -1)
			Domain = Domain.Substring (0, Domain.IndexOf (":"));
		else
			Domain = Domain.Substring (0, Domain.IndexOf ("/"));

		// Get hash of the current commit
		Process.StartInfo.FileName = "git";
		Process.StartInfo.Arguments = "rev-list --max-count=1 HEAD";
		Process.Start();
		CurrentHash = Process.StandardOutput.ReadToEnd().Trim ();

		// Watch the repository's folder
		Watcher = new FileSystemWatcher (RepoPath);
		Watcher.IncludeSubdirectories = true;
		Watcher.EnableRaisingEvents = true;
		Watcher.Filter = "*";
		Watcher.Changed += new FileSystemEventHandler(OnFileActivity);
		Watcher.Created += new FileSystemEventHandler(OnFileActivity);
		Watcher.Deleted += new FileSystemEventHandler(OnFileActivity);

		// Fetch remote changes every 20 seconds
		FetchTimer = new Timer ();
		FetchTimer.Interval = 20000;
		FetchTimer.Elapsed += delegate { 
			Fetch ();
			
		};

		FetchTimer.Start();
		BufferTimer = new Timer ();

		// Add everything that changed 
		// since SparklePony was stopped
		Add ();


	}

	// Starts a time buffer when something changes
	public void OnFileActivity (object o, FileSystemEventArgs args) {
       WatcherChangeTypes wct = args.ChangeType;
		 if (!ShouldIgnore (args.Name) && !MonitorOnly) {
	      Console.WriteLine("[Event][" + Name + "] " + wct.ToString() + 
			                  " '" + args.Name + "'");
			StartBufferTimer ();
		}
	}

	// A buffer that will fetch changes after 
	// file activity has settles down
	public void StartBufferTimer () {

		int Interval = 2000;
		if (!BufferTimer.Enabled) {	

			// Delay for a few seconds to see if more files change
			BufferTimer.Interval = Interval; 
			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				Console.WriteLine ("[Buffer][" + Name + "] Done waiting.");
				Add ();
			};
			Console.WriteLine ("[Buffer][" + Name + "] " + 
			                   "Waiting for more changes...");

			BufferTimer.Start();
		} else {

			// Extend the delay when something changes
			BufferTimer.Close ();
			BufferTimer = new Timer ();
			BufferTimer.Interval = Interval;
			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				Console.WriteLine ("[Buffer][" + Name + "] Done waiting.");
				Add ();
			};

			BufferTimer.Start();
			Console.WriteLine ("[Buffer][" + Name + "] " + 
			                   "Waiting for more changes...");

		}

	}

	// Clones a remote repo
	public void Clone () {
		Process.StartInfo.Arguments = "clone " + RemoteOriginUrl;
		Process.Start();

		// Add a gitignore file
      TextWriter Writer = new StreamWriter(RepoPath + ".gitignore");
      Writer.WriteLine("*~"); // Ignore gedit swap files
      Writer.WriteLine(".*.sw?"); // Ignore vi swap files
      Writer.Close();
	}

	// Stages the made changes
	public void Add () {
		BufferTimer.Stop ();
		Console.WriteLine ("[Git][" + Name + "] Staging changes...");
		Process.StartInfo.Arguments = "add --all";
		Process.Start();

		string Message = FormatCommitMessage ();
		if (!Message.Equals ("")) {
			Commit (Message);
			Fetch ();
			Push ();
		}
	}

	// Commits the made changes
	public void Commit (string Message) {
		Console.WriteLine ("[Commit][" + Name + "] " + Message);
		Console.WriteLine ("[Git][" + Name + "] Commiting changes...");
		Process.StartInfo.Arguments = "commit -m \"" + Message + "\"";
		Process.Start();
		ShowEventNotification (UserName + " " + Message, 
		                       SparklePonyHelpers.SparklePonyHelpers.GetAvatarFileName (UserEmail, 48), true);
	}

	// Fetches changes from the remote repo	
	public void Fetch () {
		// TODO: change status icon to sync
		FetchTimer.Stop ();
		Console.WriteLine ("[Git][" + Name + "] Fetching changes...");
		Process.StartInfo.Arguments = "fetch";
		Process.Start();
		Process.WaitForExit ();
		Merge ();
		FetchTimer.Start ();
	}

	// Merges the fetched changes
	public void Merge () {
		Watcher.EnableRaisingEvents = false;
		Console.WriteLine ("[Git][" + Name + "] Merging fetched changes...");
		Process.StartInfo.Arguments = "merge origin/master";
		Process.Start();
		Process.WaitForExit ();
		string Output = Process.StandardOutput.ReadToEnd().Trim ();

		// Show notification if there are updates
		if (!Output.Equals ("Already up-to-date.")) {

			// Get the last commit message
			Process.StartInfo.Arguments = "log --format=\"%ae\" -1";
			Process.Start();
			string LastCommitEmail = Process.StandardOutput.ReadToEnd().Trim ();

			// Get the last commit message
			Process.StartInfo.Arguments = "log --format=\"%s\" -1";
			Process.Start();
			string LastCommitMessage = Process.StandardOutput.ReadToEnd().Trim ();

			// Get the last commiter
			Process.StartInfo.Arguments = "log --format=\"%an\" -1";
			Process.Start();
			string LastCommitUserName = Process.StandardOutput.ReadToEnd().Trim ();

			ShowEventNotification (LastCommitUserName + " " + LastCommitMessage, 
		                       SparklePonyHelpers.SparklePonyHelpers.GetAvatarFileName (LastCommitEmail, 48), true);

		}

		Watcher.EnableRaisingEvents = true;
		// TODO: change status icon to normal
	}

	// Pushes the changes to the remote repo
	public void Push () {
		// TODO: What happens when network disconnects during a push
		Console.WriteLine ("[Git][" + Name + "] Pushing changes...");
		Process.StartInfo.Arguments = "push";
		Process.Start();
		Process.WaitForExit ();
	}

	// Ignores Repos, dotfiles, swap files and the like.
	public bool ShouldIgnore (string FileName) {
		if (FileName.Substring (0, 1).Equals (".") ||
			 FileName.Contains (".lock") ||
			 FileName.Contains (".git") ||
			 FileName.Contains ("/.") ||
			 Directory.Exists (RepoPath + FileName))
			return true; // Yes, ignore it.
		else if (FileName.Length > 3 &&
		         FileName.Substring (FileName.Length - 4).Equals (".swp"))
			return true;
		else return false;
	}

	// Creates a pretty commit message based on what has changed
	public string FormatCommitMessage () {

		bool DoneAddCommit = false;
		bool DoneEditCommit = false;
		bool DoneRenameCommit = false;
		bool DoneDeleteCommit = false;
		int FilesAdded = 0;
		int FilesEdited = 0;
		int FilesRenamed = 0;
		int FilesDeleted = 0;

		Process.StartInfo.Arguments = "status";
		Process.Start();
		string Output = Process.StandardOutput.ReadToEnd();

		foreach (string Line in Regex.Split (Output, "\n")) {
			if (Line.IndexOf ("new file:") > -1)
				FilesAdded++;
			if (Line.IndexOf ("modified:") > -1)
				FilesEdited++;
			if (Line.IndexOf ("renamed:") > -1)
				FilesRenamed++;
			if (Line.IndexOf ("deleted:") > -1)
				FilesDeleted++;
		}

		foreach (string Line in Regex.Split (Output, "\n")) {

			// Format message for when files are added,
			// example: "added 'file' and 3 more."
			if (Line.IndexOf ("new file:") > -1 && !DoneAddCommit) {
				DoneAddCommit = true;
				if (FilesAdded > 1)
					return "added ‘" + 
							  Line.Replace ("#\tnew file:", "").Trim () + 
					        "’ and " + (FilesAdded - 1) + " more.";
				else
					return "added ‘" + 
							  Line.Replace ("#\tnew file:", "").Trim () + "’.";
			}

			// Format message for when files are edited,
			// example: "edited 'file'."
			if (Line.IndexOf ("modified:") > -1 && !DoneEditCommit) {
				DoneEditCommit = true;
				if (FilesEdited > 1)
					return "edited ‘" + 
							  Line.Replace ("#\tmodified:", "").Trim () + 
					        "’ and " + (FilesEdited - 1) + " more.";
				else
					return "edited ‘" + 
							  Line.Replace ("#\tmodified:", "").Trim () + "’.";
			}

			// Format message for when files are edited,
			// example: "deleted 'file'."
			if (Line.IndexOf ("deleted:") > -1 && !DoneDeleteCommit) {
				DoneDeleteCommit = true;
				if (FilesDeleted > 1)
					return "deleted ‘" + 
							  Line.Replace ("#\tdeleted:", "").Trim () + 
					        "’ and " + (FilesDeleted - 1) + " more.";
				else
					return "deleted ‘" + 
							  Line.Replace ("#\tdeleted:", "").Trim () + "’.";
			}

			// Format message for when files are renamed,
			// example: "renamed 'file' to 'new name'."
			if (Line.IndexOf ("renamed:") > -1 && !DoneRenameCommit) {
				DoneDeleteCommit = true;
				if (FilesRenamed > 1)
					return "renamed ‘" + 
							  Line.Replace ("#\trenamed:", "").Trim ().Replace
					        (" -> ", "’ to ‘") + "’ and " + (FilesDeleted - 1) + 
					        " more.";
				else
					return "renamed ‘" + 
							  Line.Replace ("#\trenamed:", "").Trim ().Replace
					        (" -> ", "’ to ‘") + "’.";
			}

		}

		// Nothing happened:
		return "";

	}

	// Shows a notification with text and image
	public void ShowEventNotification (string Title, 
	                                   string IconFileName, 
	                                   bool ShowButtons) {

		Notification Notification = new Notification (Title, " ");
		Notification.Urgency = Urgency.Low;
		Notification.Timeout = 4500;
		Notification.Icon = new Gdk.Pixbuf (IconFileName);

		// Add a button to open the folder where the changed file is
		if (ShowButtons)
			Notification.AddAction ("", "Open Folder", 
				                    delegate (object o, ActionArgs args) {
					                    	Process.StartInfo.FileName = "xdg-open";
			  	                     	Process.StartInfo.Arguments = RepoPath;
				 	                   	Process.Start();
			  	                     	Process.StartInfo.FileName = "git";
				                    } );
		Notification.Show ();
	}

}

public class SparklePonyWindow : Window {

	private bool Visibility;
	private VBox LayoutVerticalLeft;
	private VBox LayoutVerticalRight;
	private TreeView ReposView;
	private ListStore ReposStore;
	private Repository [] Repositories;

	public SparklePonyWindow (Repository [] R) : base ("SparklePony")  {

		Repositories = R;

		Visibility = false;
		SetSizeRequest (720, 540);
 		SetPosition (WindowPosition.Center);
		BorderWidth = 6;
		IconName = "folder-sparklepony";

			VBox LayoutVertical = new VBox (false, 0);

				Notebook Notebook = new Notebook ();
				Notebook.BorderWidth = 6;

					HBox LayoutHorizontal = new HBox (false, 0);

						ReposStore = new ListStore (typeof (Gdk.Pixbuf), 
						                            typeof (string));

						LayoutVerticalLeft = CreateReposList ();
						LayoutVerticalLeft.BorderWidth = 12;

						LayoutVerticalRight = CreateDetailsView ();


		Label PeopleLabel = new Label ("<span font_size='large'><b>People</b></span>");
		PeopleLabel.UseMarkup = true;
		PeopleLabel.SetAlignment (0, 0);

		LayoutVerticalRight.PackStart (PeopleLabel, false, false, 0);

		LayoutVerticalRight.PackStart (CreatePeopleList (Repositories [0]), true, true, 12);

					LayoutHorizontal.PackStart (LayoutVerticalLeft, false, false, 0);
					LayoutHorizontal.PackStart (LayoutVerticalRight, true, true, 12);

				Notebook.AppendPage (LayoutHorizontal, new Label ("Folders"));
				Notebook.AppendPage (CreateEventLog (), new Label ("Events"));

			LayoutVertical.PackStart (Notebook, true, true, 0);

				HButtonBox DialogButtons = new HButtonBox ();
				DialogButtons.BorderWidth = 6;

					Button QuitServiceButton = new Button ("Quit Service");
					QuitServiceButton.Clicked += Quit;

					Button CloseButton = new Button (Stock.Close);
					CloseButton.Clicked += delegate (object o, EventArgs args) {
						Visibility = false;
						HideAll ();
					};

				DialogButtons.Add (QuitServiceButton);
				DialogButtons.Add (CloseButton);

			LayoutVertical.PackStart (DialogButtons, false, false, 0);

		Add (LayoutVertical);

	}

	// Creates a visual list of repositories
	public VBox CreateReposList() {

		string RemoteFolderIcon = "/usr/share/icons/gnome/24x24/places/folder.png";
		TreeIter ReposIter;
		foreach (Repository Repository in Repositories) {
			ReposIter = ReposStore.Prepend ();
			ReposStore.SetValue (ReposIter, 0, new Gdk.Pixbuf (RemoteFolderIcon));
			ReposStore.SetValue (ReposIter, 1, Repository.Name + "     \n" + 
			                                   Repository.Domain + "     ");
		}


		ScrolledWindow ScrolledWindow = new ScrolledWindow ();

		ReposView = new TreeView (ReposStore); 
		ReposView.AppendColumn ("", new CellRendererPixbuf () , "pixbuf", 0);  
		ReposView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
		TreeViewColumn [] ReposViewColumns = ReposView.Columns;
		ReposViewColumns [0].MinWidth = 34;

		ReposStore.IterNthChild (out ReposIter, 1);

		ReposView.ActivateRow (ReposStore.GetPath (ReposIter),
		                       ReposViewColumns [1]);


		HBox AddRemoveButtons = new HBox (false, 6);
		Button AddButton = new Button ("Add...");
		AddRemoveButtons.PackStart (AddButton, true, true, 0);

		Image RemoveImage = new Image ("/usr/share/icons/gnome/16x16/actions/list-remove.png");
		Button RemoveButton = new Button ();
		RemoveButton.Image = RemoveImage;
		AddRemoveButtons.PackStart (RemoveButton, false, false, 0);

		ScrolledWindow.AddWithViewport (ReposView);
		ScrolledWindow.WidthRequest = 200;
		VBox VBox = new VBox (false, 6);
		VBox.PackStart (ScrolledWindow, true, true, 0);
		VBox.PackStart (AddRemoveButtons, false, false, 0);

		return VBox;

	}

	// Creates the detailed view
	public VBox CreateDetailsView () {

		Label Label1 = new Label ("Remote URL:   ");
		Label1.UseMarkup = true;
		Label1.SetAlignment (0, 0);

		Label Label2 = new Label ("<b>ssh://git@github.com/hbons/Dedsfdsfsal.git</b>");
		Label2.UseMarkup = true;
		Label2.SetAlignment (0, 0);

		Label Label5 = new Label ("Path:");
		Label5.UseMarkup = true;
		Label5.SetAlignment (0, 0);

		Label Label6 = new Label ("<b>~/SparklePony/Deal</b>   ");
		Label6.UseMarkup = true;
		Label6.SetAlignment (0, 0);

		Button NotificationsCheckButton = 
			new CheckButton ("Notify me when something changes");

		Button ChangesCheckButton = 
			new CheckButton ("Synchronize my changes");

		Table Table = new Table(7, 2, false);
		Table.RowSpacing = 6;

		Table.Attach(Label1, 0, 1, 1, 2);
		Table.Attach(Label2, 1, 2, 1, 2);
		Table.Attach(Label5, 0, 1, 2, 3);
		Table.Attach(Label6, 1, 2, 2, 3);
		Table.Attach(NotificationsCheckButton, 0, 2, 4, 5);
		Table.Attach(ChangesCheckButton, 0, 2, 5, 6);

		VBox VBox = new VBox (false, 0);
		VBox.PackStart (Table, false, false, 12);

		return VBox;

	}

	public void UpdateRepoList() {

	}

	public ScrolledWindow CreateEventLog() {

		ListStore LogStore = new ListStore (typeof (Gdk.Pixbuf),
		                                    typeof (string),
		                                    typeof (string));

		Process Process = new Process();
		Process.EnableRaisingEvents = false; 
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;
		Process.StartInfo.FileName = "git";

		string Output = "";
		foreach (Repository Repository in Repositories) {

			// We're using the snowman here to separate messages :)
			Process.StartInfo.Arguments =
				"log --format=\"%at☃In ‘" + Repository.Name + "’, %an %s☃%cr\" -25";

			Process.StartInfo.WorkingDirectory = Repository.RepoPath;
			Process.Start();
			Output += "\n" + Process.StandardOutput.ReadToEnd().Trim ();
		}

		Output = Output.TrimStart ("\n".ToCharArray ());
		string [] Lines = Regex.Split (Output, "\n");

		// Sort by time and get the last 25
		Array.Sort (Lines);
		Array.Reverse (Lines);
		string [] LastTwentyFive = new string [25];
		Array.Copy (Lines, 0, LastTwentyFive, 0, 25);

		TreeIter Iter;
		foreach (string Line in LastTwentyFive) {

			// Look for the snowman!
			string [] Parts = Regex.Split (Line, "☃");
			string Message = Parts [1];
			string TimeAgo = Parts [2];

			string IconFile =
				"/usr/share/icons/hicolor/16x16/status/document-edited.png";		

			if (Message.IndexOf (" added ‘") > -1)
				IconFile = 
					"/usr/share/icons/hicolor/16x16/status/document-added.png";

			if (Message.IndexOf (" deleted ‘") > -1)
				IconFile = 
					"/usr/share/icons/hicolor/16x16/status/document-removed.png";

			if (Message.IndexOf (" moved ‘") > -1 || 
			    Message.IndexOf (" renamed ‘") > -1)

				IconFile =
					"/usr/share/icons/hicolor/16x16/status/document-moved.png";

			Iter = LogStore.Append ();
			LogStore.SetValue (Iter, 0, new Gdk.Pixbuf (IconFile));
			LogStore.SetValue (Iter, 1, Message);
			// TODO: right align time
			LogStore.SetValue (Iter, 2, "  " + TimeAgo);

		}

		TreeView LogView = new TreeView (LogStore); 
		LogView.HeadersVisible = false;

		LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
		LogView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
		LogView.AppendColumn ("", new Gtk.CellRendererText (), "text", 2);

		TreeViewColumn [] Columns = LogView.Columns;
		Columns [0].MinWidth = 32;
		Columns [1].Expand = true;
		Columns [1].MaxWidth = 150;

		ScrolledWindow ScrolledWindow = new ScrolledWindow ();
		ScrolledWindow.AddWithViewport (LogView);
		ScrolledWindow.BorderWidth = 6;

		return ScrolledWindow;

	}

	public void UpdateEventLog() {

	}

	// Creates a visual list of people working in the repo
	public ScrolledWindow CreatePeopleList (Repository Repository) {

		Process Process = new Process ();
		Process.EnableRaisingEvents = false; 
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;

		// Get a log of commits, example: "Hylke Bons☃added 'file'."
		Process.StartInfo.FileName = "git";
		Process.StartInfo.Arguments = "log --format=\"%an☃%ae\" -50";
		Process.StartInfo.WorkingDirectory = Repository.RepoPath;
		Process.Start();


		string Output = Process.StandardOutput.ReadToEnd().Trim ();
      string [] People = new string [50];
		string [] Lines = Regex.Split (Output, "\n");

		ListStore PeopleStore = new ListStore (typeof (Gdk.Pixbuf),
		                                       typeof (string),
		                                       typeof (string));

		TreeIter PeopleIter;
		int i = 0;
		foreach (string Line in Lines) {

			// Only add name if it isn't there already
			if (Array.IndexOf (People, Line) == -1) {

				People [i]      = Line;
				string [] Parts = Regex.Split (Line, "☃");

				string UserName  = Parts [0];
				string UserEmail = Parts [1];

				// Do something special if the person is you
				if (UserName.Equals (Repository.UserName))
					UserName += " (that’s you)";

				// Actually add to the list
				PeopleIter = PeopleStore.Prepend ();
				PeopleStore.SetValue (PeopleIter, 0, new Gdk.Pixbuf (SparklePonyHelpers.SparklePonyHelpers.GetAvatarFileName (UserEmail, 24)));
				PeopleStore.SetValue (PeopleIter, 1, UserName + "  ");
				PeopleStore.SetValue (PeopleIter, 2, UserEmail + "  ");

				// Let's try to get the person's gravatar for next time
				string AvatarsDirSmall =
					Environment.GetEnvironmentVariable("HOME") + 
					"/.config/sparklepony/avatars/24x24/";

				WebClient WebClient1 = new WebClient ();
				Uri UriSmall = new Uri ("http://www.gravatar.com/avatar/" + SparklePonyHelpers.SparklePonyHelpers.GetMD5 (UserEmail) + ".jpg?s=24");
				WebClient1.DownloadFileAsync (UriSmall,  AvatarsDirSmall + UserEmail);

				string AvatarsDirLarge =
					Environment.GetEnvironmentVariable("HOME") + 
					"/.config/sparklepony/avatars/48x48/";

				WebClient WebClient2 = new WebClient ();
				Uri UriLarge = new Uri ("http://www.gravatar.com/avatar/" + SparklePonyHelpers.SparklePonyHelpers.GetMD5 (UserEmail) + ".jpg?s=48");
				WebClient2.DownloadFileAsync (UriLarge,  AvatarsDirLarge + UserEmail);

			}
			i++;
		}

		TreeView PeopleView = new TreeView (PeopleStore); 
		PeopleView.AppendColumn ("", new CellRendererPixbuf () , "pixbuf", 0);  
		PeopleView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
		PeopleView.AppendColumn ("", new Gtk.CellRendererText (), "text", 2);
		TreeViewColumn [] PeopleViewColumns = PeopleView.Columns;
		PeopleViewColumns [0].MinWidth = 32;
		PeopleViewColumns [1].Expand = true;

		PeopleView.HeadersVisible = false;

		ScrolledWindow ScrolledWindow = new ScrolledWindow ();
		ScrolledWindow.AddWithViewport (PeopleView);

		return ScrolledWindow;

	}

	public void UpdatePeopleList () {

	}

	public void ToggleVisibility() {
		Present ();
		if (Visibility) {
			if (HasFocus)
				HideAll ();
		} else {
			ShowAll ();
		}
	}

	public void Quit (object o, EventArgs args) {
		File.Delete ("/tmp/sparklepony/sparklepony.pid");
		Application.Quit ();
	}

}

namespace SparklePonyHelpers {


	// Helper that returns a user's avatar
	class SparklePonyHelpers {

		public static string GetAvatarFileName (string Email, int Size) {

			string GravatarFile = Environment.GetEnvironmentVariable("HOME") + 
				                   "/.config/sparklepony/avatars/" + 
			                      Size + "x" + Size + 
				                   "/" + Email;

			if (File.Exists (GravatarFile))
				return GravatarFile;

			else {

				string FallbackFileName = "/usr/share/icons/hicolor/" + 
				                          Size + "x" + Size + 
				                          "/status/avatar-default.png";

				if (File.Exists (FallbackFileName))
					return FallbackFileName;
				else
					return "/usr/share/icons/hicolor/16x16/status/avatar-default.png";
			}

		}

		// Helper that creates an MD5 hash
		public static string GetMD5 (string s) {

		  MD5 md5 = new MD5CryptoServiceProvider ();
		  Byte[] Bytes = ASCIIEncoding.Default.GetBytes (s);
		  Byte[] EncodedBytes = md5.ComputeHash (Bytes);

		  return BitConverter.ToString(EncodedBytes).ToLower ().Replace ("-", "");

		}

	}


}
