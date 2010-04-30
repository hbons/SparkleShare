//   SparklePony 0.0.5

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
using System;
using System.IO;
using System.Diagnostics;
using System.Timers;
using Notifications;
using System.Text.RegularExpressions;

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
			Console.WriteLine ("Git wasn't found.\nYou can get it from http://git-scm.com/.");
			Environment.Exit (0);
		}

		// Don't allow running as root
		Process.StartInfo.FileName = "whoami";
		Process.Start();
		if (Process.StandardOutput.ReadToEnd().Trim ().Equals ("root")) {
			Console.WriteLine ("Sorry, you shouldn't run SparklePony as root.\nThings will go utterly wrong.");
			Environment.Exit (0);
		}

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
		Console.WriteLine ("This program comes with ABSOLUTELY NO WARRANTY.");
		Console.WriteLine ("This is free software, and you are welcome to redistribute it ");
		Console.WriteLine ("under certain conditions. Please read the GNU GPLv3 for details.");
		Console.WriteLine ("");
		Console.WriteLine ("SparklePony syncs the ~/Collaboration folder with remote repositories.");
		Console.WriteLine ("");
		Console.WriteLine ("Usage: sparklepony [start|stop|restart] [OPTION]...");
		Console.WriteLine ("Sync Collaboration folder with remote repositories.");
		Console.WriteLine ("");
		Console.WriteLine ("Arguments:");
		Console.WriteLine ("\t -d, --disable-gui\tDon't show the notification icon.");
		Console.WriteLine ("\t -h, --help\t\tDisplay this help text.");
		Console.WriteLine ("");
		Environment.Exit (0);
	}

}

public class SparklePonyUI {

	public SparklePonyWindow SparklePonyWindow;
	public SparklePonyStatusIcon SparklePonyStatusIcon;
	public string ReposPath;
	public string UserHome;
	public Repository [] Repositories;

	public SparklePonyUI (bool HideUI) {

		Process Process = new Process();
		Process.EnableRaisingEvents = false;
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;

		// Get home folder, example: "/home/user" 
		UserHome = Environment.GetEnvironmentVariable("HOME");

		// Create 'Collaboration' folder in the user's home folder
		ReposPath = UserHome + "/Collaboration";
		if (!Directory.Exists (ReposPath)) {
			Directory.CreateDirectory (ReposPath);
			Console.WriteLine ("Created '" + ReposPath + "'");
		}

		// Get all the Repos in ~/Collaboration
		string [] Repos = Directory.GetDirectories (ReposPath);
		Repositories = new Repository [Repos.Length];

		int i = 0;
		foreach (string Folder in Repos) {
			Repositories [i] = new Repository (Folder);
			i++;
		}

		if (!HideUI) {

			// Create the window
			SparklePonyWindow = new SparklePonyWindow (Repositories);
			SparklePonyWindow.DeleteEvent += CloseSparklePonyWindow;

			// Create the status icon
			SparklePonyStatusIcon = new SparklePonyStatusIcon ();
			SparklePonyStatusIcon.Activate += delegate  { SparklePonyWindow.ToggleVisibility (); };
		}

	}

	public void CloseSparklePonyWindow (object o, DeleteEventArgs args) {
		SparklePonyWindow = new SparklePonyWindow (Repositories);
		SparklePonyWindow.DeleteEvent += CloseSparklePonyWindow;
	}

	public void StartMonitoring () {	}
	public void StopMonitoring () { }

}

public class SparklePonyStatusIcon : StatusIcon {

	public SparklePonyStatusIcon () : base ()  {
		IconName = "folder-remote";
		// TODO: Only on first run
		Notification Notification = new Notification ("Welcome to SparklePony!", "Click here to add some folders.");
		Notification.Urgency = Urgency.Normal;
		Notification.Timeout = 10000;
		Notification.Show ();
	}

	public void SetIdleState () {
		IconName = "folder-remote";
	}

	public void SetSyncingState () {
		IconName = "view-refresh"; // Massively abusing this icon here :)
	}

}


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

		// Get the repository's path, example: "/home/user/Collaboration/repo/"
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

		// Get the repository name, example: "project"
		Name = RepoPath.Substring (RepoPath.TrimEnd ( "/".ToCharArray ()).LastIndexOf ("/") + 1);

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

		// Fetch changes every 20 seconds

		FetchTimer = new Timer ();
		FetchTimer.Interval = 20000;
		FetchTimer.Elapsed += delegate { 
			Fetch ();
			
		};

		FetchTimer.Start();

		BufferTimer = new Timer ();

		// Add everything that changed since SparklePony was stopped
		Add ();

	}

	public void OnFileActivity (object o, FileSystemEventArgs args) {
       WatcherChangeTypes wct = args.ChangeType;
		 if (!ShouldIgnore (args.Name) && !MonitorOnly) {
	      Console.WriteLine("[" + Name + "][Event] " + wct.ToString() + " '" + args.Name + "'");
			StartBufferTimer ();
		}
	}

	public void StartBufferTimer () {

		int Interval = 3000;
		if (!BufferTimer.Enabled) {	

			// Delay for a few seconds to see if more files change
			BufferTimer.Interval = Interval; 
			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				Console.WriteLine ("[" + Name + "][Buffer] Done waiting.");
				Add ();
			};
			Console.WriteLine ("[" + Name + "][Buffer] Waiting for more changes...");
			BufferTimer.Start();
		} else {

			// Extend the delay when something changes
			BufferTimer.Close ();
			BufferTimer = new Timer ();
			BufferTimer.Interval = Interval;
			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				Console.WriteLine ("[" + Name + "][Buffer] Done waiting.");
				Add ();
			};
			BufferTimer.Start();
			Console.WriteLine ("[" + Name + "][Buffer] Waiting for more changes...");
		}
	}

	public void Clone () {
		Process.StartInfo.Arguments = "clone " + RemoteOriginUrl;
		Process.Start();

		// Add a gitignore file
      TextWriter Writer = new StreamWriter(RepoPath + ".gitignore");
      Writer.WriteLine("*~");
      Writer.Close();
	}

	public void Add () {
		BufferTimer.Stop ();
		Console.WriteLine ("[" + Name + "][Git] Staging changes...");
		Process.StartInfo.Arguments = "add --all";
		Process.Start();

		string Message = FormatCommitMessage ();
		if (!Message.Equals ("")) {
			Commit (Message);
			Fetch ();
			Push ();
		}
	}

	public void Commit (string Message) {
		Console.WriteLine ("[" + Name + "][Commit] " + Message);
		Console.WriteLine ("[" + Name + "][Git] Commiting changes...");
		Process.StartInfo.Arguments = "commit -m \"" + Message + "\"";
		Process.Start();
	}

	public void Fetch () {
		// TODO: change status icon to sync
		FetchTimer.Stop ();
		Console.WriteLine ("[" + Name + "][Git] Fetching changes...");
		Process.StartInfo.Arguments = "fetch";
		Process.Start();
		Process.WaitForExit ();
		Merge ();
		FetchTimer.Start ();
	}

	public void Fetch (object o, ElapsedEventArgs args) {
		// TODO: What happens when network disconnects during a fetch
		// TODO: change status icon to sync
		FetchTimer.Stop ();
		Console.WriteLine ("[" + Name + "][Git] Fetching changes...");
		Process.StartInfo.Arguments = "fetch";
		Process.Start();
		Process.WaitForExit ();
		Merge ();
		FetchTimer.Start ();

	}

	public void Merge () {
		Watcher.EnableRaisingEvents = false;
		Console.WriteLine ("[" + Name + "][Git] Merging fetched changes...");
		Process.StartInfo.Arguments = "merge origin/master";
		Process.Start();
		Process.WaitForExit ();
		string Output = Process.StandardOutput.ReadToEnd().Trim ();

		// Show notification if there are updates
		if (!Output.Equals ("Already up-to-date.")) {
			Process.StartInfo.Arguments = "log --pretty=oneline -1";
			Process.Start();
			string LastCommitMessage = Process.StandardOutput.ReadToEnd().Trim ().Substring (41);
			ShowNotification (LastCommitMessage, "", true);
		}

		Watcher.EnableRaisingEvents = true;
		// TODO: change status icon to normal
	}

	public void Push () {
		// TODO: What happens when network disconnects during a push
		Console.WriteLine ("[" + Name + "][Git] Pushing changes...");
		Process.StartInfo.Arguments = "push";
		Process.Start();
		Process.WaitForExit ();
	}

	// Ignore Repos, dotfiles, swap files and the like.
	public bool ShouldIgnore (string s) {
		if (s.Substring (0, 1).Equals (".") ||
			 s.Contains (".lock") ||
			 s.Contains (".git") ||
			 s.Contains ("/.") ||
			 Directory.Exists (Process.StartInfo.WorkingDirectory + "/" + s))
			return true; // Yes, ignore it.
		else if (s.Length > 3 && s.Substring (s.Length - 4).Equals (".swp"))
			return true;
		else return false;
	}

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

			if (Line.IndexOf ("new file:") > -1 && !DoneAddCommit) {
				DoneAddCommit = true;
				if (FilesAdded > 1)
					return UserName + " added '" + 
							  Line.Replace ("#\tnew file:", "").Trim () + "' and " + (FilesAdded - 1) + " more.";
				else
					return UserName + " added '" + 
							  Line.Replace ("#\tnew file:", "").Trim () + "'.";
			}

			if (Line.IndexOf ("modified:") > -1 && !DoneEditCommit) {
				DoneEditCommit = true;
				if (FilesEdited > 1)
					return UserName + " edited '" + 
							  Line.Replace ("#\tmodified:", "").Trim () + "' and " + (FilesEdited - 1) + " more.";
				else
					return UserName + " edited '" + 
							  Line.Replace ("#\tmodified:", "").Trim () + "'.";
			}

			if (Line.IndexOf ("renamed:") > -1 && !DoneRenameCommit) {
				DoneDeleteCommit = true;
				if (FilesRenamed > 1)
					return UserName + " renamed '" + 
							  Line.Replace ("#\trenamed:", "").Trim ().Replace (" -> ", "' to '") + "' and " + (FilesDeleted - 1) + " more.";
				else
					return UserName + " renamed '" + 
							  Line.Replace ("#\trenamed:", "").Trim ().Replace (" -> ", "' to '") + "'.";
			}

			if (Line.IndexOf ("deleted:") > -1 && !DoneDeleteCommit) {
				DoneDeleteCommit = true;
				if (FilesDeleted > 1)
					return UserName + " deleted '" + 
							  Line.Replace ("#\tdeleted:", "").Trim () + "' and " + (FilesDeleted - 1) + " more.";
				else
					return UserName + " deleted '" + 
							  Line.Replace ("#\tdeleted:", "").Trim () + "'.";
			}

		}

		return "";

	}

	public void ShowNotification (string Title, string SubText, bool ShowButtons) {

		Notification Notification = new Notification (Title, SubText);
		Notification.Urgency = Urgency.Low;
		Notification.Timeout = 4500;

		// Add a button to open the folder the changed file resides in
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

	public SparklePonyWindow (Repository [] R) : base ("Collaboration Folders")  {

		Repositories = R;

		Visibility = false;
		SetSizeRequest (640, 480);
 		SetPosition (WindowPosition.Center);
		BorderWidth = 6;
		IconName = "folder-remote";

			VBox LayoutVertical = new VBox (false, 0);

				Notebook Notebook = new Notebook ();
				Notebook.BorderWidth = 6;

					HBox LayoutHorizontal = new HBox (false, 12);

						ReposStore = new ListStore (typeof (Gdk.Pixbuf), typeof (string));
						LayoutVerticalLeft = CreateReposList ();
						LayoutVerticalLeft.BorderWidth = 12;

						LayoutVerticalRight = CreateDetailsView ();

					LayoutHorizontal.PackStart (LayoutVerticalLeft, false, false, 0);
					LayoutHorizontal.PackStart (LayoutVerticalRight, true, true, 0);

				Notebook.AppendPage (LayoutHorizontal, new Label ("Folders"));
				Notebook.AppendPage (CreateEventLog (), new Label ("Events"));

			LayoutVertical.PackStart (Notebook, true, true, 0);

				HButtonBox DialogButtons = new HButtonBox ();
				DialogButtons.BorderWidth = 6;

					Button QuitServiceButton = new Button ("Quit Service");
					QuitServiceButton.Clicked += Quit;

					Button CloseButton = new Button (Stock.Close);
					CloseButton.Clicked += delegate (object o, EventArgs args) { HideAll (); Visibility = false; };

				DialogButtons.Add (QuitServiceButton);
				DialogButtons.Add (CloseButton);

			LayoutVertical.PackStart (DialogButtons, false, false, 0);

		Add (LayoutVertical);

	}

	public VBox CreateReposList() {

		string RemoteFolderIcon = "/usr/share/icons/gnome/22x22/places/folder-remote.png";
		TreeIter ReposIter;
		foreach (Repository Repository in Repositories) {
			ReposIter = ReposStore.Prepend ();
			ReposStore.SetValue (ReposIter, 0, new Gdk.Pixbuf (RemoteFolderIcon));
			ReposStore.SetValue (ReposIter, 1, Repository.Name + "     \n" + Repository.Domain + "     ");
		}
		ReposView = new TreeView (ReposStore); 
		ReposView.AppendColumn ("", new CellRendererPixbuf () , "pixbuf", 0);  
		ReposView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
		TreeViewColumn [] ReposViewColumns = ReposView.Columns;
		ReposViewColumns [0].MinWidth = 34;

		ReposStore.IterNthChild (out ReposIter, 1);

		ReposView.ActivateRow (ReposStore.GetPath (ReposIter), ReposViewColumns [1]);


		HBox AddRemoveButtons = new HBox ();
		Button AddButton = new Button ("Add...");
		AddRemoveButtons.PackStart (AddButton, true, true, 0);

		Image RemoveImage = new Image (Stock.Remove);
		Button RemoveButton = new Button (RemoveImage);
		AddRemoveButtons.PackStart (RemoveButton, false, false, 0);

		VBox VBox = new VBox (false, 0);
		VBox.PackStart (ReposView, true, true, 0);
		VBox.PackStart (AddRemoveButtons, false, false, 0);

		return VBox;
	}

	public VBox CreateDetailsView () {

		Label Label1 = new Label ("Remote URL:");
		Label1.UseMarkup = true;
		Label1.SetAlignment (0, 0);

		Label Label2 = new Label ("<b>ssh://git@github.com/hbons/Dedsfdsfsal.git</b>");
		Label2.UseMarkup = true;
		Label2.SetAlignment (0, 0);

		Label Label5 = new Label ("Path:");
		Label5.UseMarkup = true;
		Label5.SetAlignment (0, 0);

		Label Label6 = new Label ("<b>~/Collaboration/Deal</b>");
		Label6.UseMarkup = true;
		Label6.SetAlignment (0, 0);

		Button NotificationsCheckButton = new CheckButton ("Notify me when something changes");
		Button ChangesCheckButton = new CheckButton ("Synchronize my changes");

		Table Table = new Table(6, 2, false);
		Table.RowSpacing = 6;

		Table.Attach(Label1, 0, 1, 0, 1);
		Table.Attach(Label2, 1, 2, 0, 1);
		Table.Attach(Label5, 0, 1, 1, 2);
		Table.Attach(Label6, 1, 2, 1, 2);
		Table.Attach(NotificationsCheckButton, 0, 2, 3, 4);
		Table.Attach(ChangesCheckButton, 0, 2, 4, 5);

		VBox VBox = new VBox (false, 0);
		VBox.PackStart (Table, false, false, 24);

		return VBox;

	}

	public void UpdateRepoList() {

	}

	public ScrolledWindow CreateEventLog() {

		ListStore LogStore = new ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string));

		Process Process = new Process();
		Process.EnableRaisingEvents = false; 
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;

		Process.StartInfo.FileName = "git";
		string Output = "";
		foreach (Repository Repository in Repositories) {

			// We're using the snowman here to separate messages :)
			Process.StartInfo.Arguments = "log --format=\"%at☃In '" + Repository.Name + "', %s☃%cr\" -25";
			Process.StartInfo.WorkingDirectory = Repository.RepoPath;
			Process.Start();
			Output += "\n" + Process.StandardOutput.ReadToEnd().Trim ();
		}
		string [] Lines = Regex.Split (Output.TrimStart ("\n".ToCharArray ()), "\n");

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

			string IconFile = "/usr/share/icons/hicolor/16x16/status/document-edited.png";		
			if (Message.IndexOf (" added ") > -1)
				IconFile = "/usr/share/icons/hicolor/16x16/status/document-added.png";
			if (Message.IndexOf (" deleted ") > -1)
				IconFile = "/usr/share/icons/hicolor/16x16/status/document-removed.png";
			if (Message.IndexOf (" moved ") > -1 || Message.IndexOf (" renamed ") > -1)
				IconFile = "/usr/share/icons/hicolor/16x16/status/document-moved.png";

			Iter = LogStore.Append ();
			LogStore.SetValue (Iter, 0, new Gdk.Pixbuf (IconFile));
			LogStore.SetValue (Iter, 1, Message);
			LogStore.SetValue (Iter, 2, "  " + TimeAgo);
		}

		TreeView LogView = new TreeView (LogStore); 
		LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
		LogView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
		LogView.AppendColumn ("", new Gtk.CellRendererText (), "text", 2);

		TreeViewColumn [] Columns = LogView.Columns;
		Columns [0].MinWidth = 32;
		Columns [1].Expand = true;
		Columns [1].MaxWidth = 150;

		ScrolledWindow ScrolledWindow = new ScrolledWindow ();
		ScrolledWindow.AddWithViewport (LogView);
		ScrolledWindow.BorderWidth = 12;

		return ScrolledWindow;

	}

	public void UpdateEventLog() {

	}

	public void CreatePeopleList () {

	}

	public void UpdatePeopleList () {

	}

	public void ToggleVisibility() {
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
