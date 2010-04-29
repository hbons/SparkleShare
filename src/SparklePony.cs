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

//   Dependencies: 
//             git
//             mono-core
//             gtk-sharp2
//             gtk-sharp2-devel
//             notify-sharp
//             notify-sharp-devel
//             dbus-sharp
//
//   Compile: 
//             gmcs -pkg:gtk-sharp-2.0 -pkg:notify-sharp SparklePony.cs
//
//   Run:
//             mono SparklePony.exe

using Gtk;
using System;
using System.IO;
using System.Diagnostics;
using System.Timers;
using Notifications;
using System.Text.RegularExpressions;

public class SparklePony {

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
		SparklePonyUI SparklePonyUI = new SparklePonyUI (HideUI);
		SparklePonyUI.StartMonitoring ();
		Gtk.Application.Run ();
	}

	public static void ShowHelp () {
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
	public string FoldersPath;
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
		FoldersPath = UserHome + "/Collaboration";
		if (!Directory.Exists (FoldersPath)) {
			Directory.CreateDirectory (FoldersPath);
			Console.WriteLine ("Created '" + FoldersPath + "'");
		}

		// Get all the folders in ~/Collaboration
		string [] Folders = Directory.GetDirectories (FoldersPath);
		Repositories = new Repository [Folders.Length];

		int i = 0;
		foreach (string Folder in Folders) {
			Repositories [i] = new Repository (Folder);
			i++;
		}

		AutoFetcher AutoFetcher = new AutoFetcher (Repositories);

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
		// Activate += delegate (object o, EventArgs args) { SetSyncingState (); };
	}

	public void SetIdleState () {
		IconName = "folder-remote";
	}

	public void SetSyncingState () {
		IconName = "view-refresh"; // Massively abusing this icon here :)
	}

}

public class AutoFetcher : Timer {

	public AutoFetcher (Repository [] Repositories) : base () {

		// Fetch changes every 30 seconds
		Interval = 45000; // Keep high for now
		Elapsed += delegate (object o, ElapsedEventArgs args) { 
			foreach (Repository Repository in Repositories) {
				Stop ();
				if (!Repository.MonitorOnly)
					Repository.Fetch ();
				Start ();
			}
		};

		Start();

	}

}

public class Repository {

	private Process Process;
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

		// Get the repository's path, example: "/home/user/Collaboration/repo"
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

		BufferTimer = new Timer ();

		// Add everything that changed since SparklePony was stopped
		Add ();

	}

	public void OnFileActivity (object o, FileSystemEventArgs args) {
       WatcherChangeTypes wct = args.ChangeType;
		 if (!ShouldIgnore (args.Name)) {
	      Console.WriteLine("[Event] " + wct.ToString() + " '" + args.Name + "'");
			StartBufferTimer ();
		}
	}

	public void StartBufferTimer () {

		int Interval = 5000;
		if (!BufferTimer.Enabled) {	

			// Delay for a few seconds to see if more files change
			BufferTimer.Interval = Interval; 
			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				Console.WriteLine ("[Buffer] Done waiting.");
				Add ();
			};
			Console.WriteLine ("[Buffer] Waiting for more changes...");
			BufferTimer.Start();
		} else {

			// Extend the delay when something changes
			BufferTimer.Close ();
			BufferTimer = new Timer ();
			BufferTimer.Interval = Interval;
			BufferTimer.Elapsed += delegate (object o, ElapsedEventArgs args) {
				Console.WriteLine ("[Buffer] Done waiting.");
				Add ();
			};
			BufferTimer.Start();
			Console.WriteLine ("[Buffer] Waiting for more changes...");
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
		Console.WriteLine ("[Git] Staging changes...");
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
		Console.WriteLine ("[Commit] " + Message);
		Console.WriteLine ("[Git] Commiting changes...");
		Process.StartInfo.Arguments = "commit -m '" + Message + "'";
		Process.Start();
	}

	public void Fetch () {
		// TODO: change status icon to sync
		Console.WriteLine ("[Git] Fetching changes...");
		Process.StartInfo.Arguments = "fetch";
		Process.Start();
		Merge ();
	}

	public void Fetch (object o, ElapsedEventArgs args) {
		// TODO: What happens when network disconnects during a fetch
		// TODO: change status icon to sync
		Console.WriteLine ("[Git] Fetching changes...");
		Process.StartInfo.Arguments = "fetch";
		Process.Start();
		Process.WaitForExit ();
		Merge ();
	}

	public void Merge () {
		Watcher.EnableRaisingEvents = false;
		Console.WriteLine ("[Git] Merging fetched changes...");
		Process.StartInfo.Arguments = "merge origin/master";
		Process.Start();
		Process.WaitForExit ();
		string Output = Process.StandardOutput.ReadToEnd().Trim ();

		// Show notification if there are updates
		if (!Output.Equals ("Already up-to-date.")) {
			Process.StartInfo.Arguments = "log --pretty=oneline -1";
			Process.Start();
			string LastCommitMessage = Process.StandardOutput.ReadToEnd().Trim ().Substring (41);
			ShowNotification (LastCommitMessage, "");
		}

		Watcher.EnableRaisingEvents = true;
		// TODO: change status icon to normal
	}

	public void Push () {
		// TODO: What happens when network disconnects during a push
		Console.WriteLine ("[Git] Pushing changes...");
		Process.StartInfo.Arguments = "push";
		Process.Start();
		Process.WaitForExit ();
	}

	// Ignore folders, dotfiles, swap files and the like.
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
				if (FilesAdded > 1)
					return "In '" + Name + "', " + UserName + " added '" + 
							  Line.Replace ("#\tnew file:", "").Trim () + "' and " + (FilesAdded - 1) + " more.";
				else
					return "In '" + Name + "', " + UserName + " added '" + 
							  Line.Replace ("#\tnew file:", "").Trim () + "'.";
				DoneAddCommit = true;
			}

			if (Line.IndexOf ("modified:") > -1 && !DoneEditCommit) {
				if (FilesEdited > 1)
					return "In '" + Name + "', " + UserName + " edited '" + 
							  Line.Replace ("#\tmodified:", "").Trim () + "' and " + (FilesEdited - 1) + " more.";
				else
					return "In '" + Name + "', " + UserName + " edited '" + 
							  Line.Replace ("#\tmodified:", "").Trim () + "'.";
				DoneEditCommit = true;
			}

			if (Line.IndexOf ("renamed:") > -1 && !DoneRenameCommit) {
				if (FilesRenamed > 1)
					return "In '" + Name + "', " + UserName + " renamed '" + 
							  Line.Replace ("#\trenamed:", "").Trim ().Replace (" -> ", "' to '") + "' and " + (FilesDeleted - 1) + " more.";
				else
					return "In '" + Name + "', " + UserName + " renamed '" + 
							  Line.Replace ("#\trenamed:", "").Trim ().Replace (" -> ", "' to '") + "'.";
				DoneDeleteCommit = true;
			}

			if (Line.IndexOf ("deleted:") > -1 && !DoneDeleteCommit) {
				if (FilesDeleted > 1)
					return "In '" + Name + "', " + UserName + " deleted '" + 
							  Line.Replace ("#\tdeleted:", "").Trim () + "' and " + (FilesDeleted - 1) + " more.";
				else
					return "In '" + Name + "', " + UserName + " deleted '" + 
							  Line.Replace ("#\tdeleted:", "").Trim () + "'.";
				DoneDeleteCommit = true;
			}

		}

		return "";

	}

	// TODO: To UI
	public string [] GetPeopleList () {
		return null;
	}

	// Can potentially be moved from this class as well
	public void ShowNotification (string Title, string SubText) {
		Notification Notification = new Notification (Title, SubText);
		Notification.Urgency = Urgency.Low;
		Notification.Timeout = 3500;

		// Add a button to open the folder the changed file resides in
//		Notification.AddAction(File, "Open Folder", 
	//	                       delegate (object o, ActionArgs args) {
		//                       	Process.StartInfo.FileName = "nautilus";
		  //                     	Process.StartInfo.Arguments = "'" + Directory.GetParent (RepoPath + "/" + File) + "'";
		    //                   	Process.Start();									     
		      //                 } );
		Notification.Show ();
	}

}

public class SparklePonyWindow : Window {

	private bool Visibility;

	public SparklePonyWindow (Repository [] Repositories) : base ("Collaboration Folders")  {

		Visibility = false;
		SetSizeRequest (640, 480);
 		SetPosition (WindowPosition.Center);
		BorderWidth = 6;
		IconName = "folder-remote";

		ListStore FoldersStore = new ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string));
		string RemoteFolderIcon = "/usr/share/icons/gnome/16x16/places/folder.png";

		TreeIter Iter2;

		foreach (Repository Repository in Repositories) {
			Iter2 = FoldersStore.Prepend ();
			FoldersStore.SetValue (Iter2, 1, null);
			FoldersStore.SetValue (Iter2, 1, Repository.Name);
		}

		TreeView FoldersView = new TreeView (FoldersStore); 
		FoldersView.AppendColumn ("", new CellRendererPixbuf () , "pixbuf", 0);  
		FoldersView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);

		HBox AddRemoveButtons = new HBox ();
		Button AddButton = new Button ("Add...");
		AddRemoveButtons.PackStart (AddButton, true, true, 0);
		Button RemoveButton = new Button ("Remove");
		AddRemoveButtons.PackStart (RemoveButton, false, false, 0);



		ListStore LogStore = new ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string));



		Process Process = new Process();
		Process.EnableRaisingEvents = false; 
		Process.StartInfo.RedirectStandardOutput = true;
		Process.StartInfo.UseShellExecute = false;
		// TODO: fix hard coding, system independant
		Process.StartInfo.WorkingDirectory = Environment.GetEnvironmentVariable("HOME") + "/Collaboration/Deal";
		Process.StartInfo.FileName = "git";
		Process.StartInfo.Arguments = "log --pretty=oneline -20";
		Process.Start();
		string Output = Process.StandardOutput.ReadToEnd().Trim ();


		Gdk.Pixbuf Icon = new Gdk.Pixbuf ("/usr/share/icons/hicolor/16x16/status/document-edited.png");
		Gdk.Pixbuf Icon2 = new Gdk.Pixbuf ("/usr/share/icons/hicolor/16x16/status/document-added.png");
		Gdk.Pixbuf Icon3 = new Gdk.Pixbuf ("/usr/share/icons/hicolor/16x16/status/document-removed.png");
		Gdk.Pixbuf Icon4 = new Gdk.Pixbuf ("/usr/share/icons/hicolor/16x16/status/document-moved.png");

		TreeIter Iter;
		foreach (string Message in Regex.Split (Output, "\n")) {
			Iter = LogStore.Prepend ();
			LogStore.SetValue (Iter, 0, Icon);
			LogStore.SetValue (Iter, 1, "In 'GNOME3', Hylke Bons edited 'mockup.svg'");
			LogStore.SetValue (Iter, 2, "32 minutes ago ");
		}

		TreeView LogView = new TreeView (LogStore); 
		LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
		LogView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
		LogView.AppendColumn ("", new Gtk.CellRendererText (), "text", 2);

		TreeViewColumn [] Columns = LogView.Columns;
		Columns [0].MinWidth = 32;
		Columns [1].Expand = true;

		ScrolledWindow ScrolledWindow = new ScrolledWindow ();
		ScrolledWindow.AddWithViewport (LogView);
		ScrolledWindow.BorderWidth = 12;

		VBox LayoutVerticalLeft = new VBox (false, 0);
		LayoutVerticalLeft.PackStart (FoldersView, true, true, 0);
		LayoutVerticalLeft.PackStart (AddRemoveButtons, false, false, 0);

		LayoutVerticalLeft.BorderWidth = 12;

		VBox LayoutVerticalRight = new VBox ();

		// TODO: Fix this, it's hardcoded

		Label Label1 = new Label ("Remote URL:");
		Label1.UseMarkup = true;
		Label1.SetAlignment (0, 0);

		Label Label2 = new Label ("<b>ssh://git@github.com/hbons/Deal.git</b>");
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


		LayoutVerticalRight.PackStart (Table, false, false, 24);
	

		HBox LayoutHorizontal = new HBox (false, 12);
		LayoutHorizontal.PackStart (LayoutVerticalLeft, false, false, 0);
		LayoutHorizontal.PackStart (LayoutVerticalRight, true, true, 0);

		Notebook Notebook = new Notebook ();
		Notebook.BorderWidth = 6;
		Notebook.AppendPage (LayoutHorizontal, new Label ("Folders"));
		Notebook.AppendPage (ScrolledWindow, new Label ("Events"));




		HButtonBox DialogButtons = new HButtonBox ();
		DialogButtons.BorderWidth = 6;

		Button QuitButton = new Button ("Quit Service");
		QuitButton.Clicked += Quit;
		DialogButtons.Add (QuitButton);

		Button CloseButton = new Button (Stock.Close);
		CloseButton.Clicked += delegate (object o, EventArgs args) { HideAll (); Visibility = false; };
		DialogButtons.Add (CloseButton);

		VBox LayoutVertical = new VBox (false, 0);
		LayoutVertical.PackStart (Notebook, true, true, 0);
		LayoutVertical.PackStart (DialogButtons, false, false, 0);
		Add (LayoutVertical);

	}

	// General options: [X] Run at startup. [X]

	public void Quit (object o, EventArgs args) {
		File.Delete ("/tmp/sparklepony/sparklepony.pid");
		Application.Quit ();
	}

	public void CreateRepoList() {

	}

	public void UpdateRepoList() {

	}

	public void CreateEventLog() {

	}

	public void UpdateEventLog() {

	}

	public void ToggleVisibility() {
		if (Visibility) {
			if (HasFocus)
				HideAll ();
		} else {
			ShowAll ();
		}
	}

}
