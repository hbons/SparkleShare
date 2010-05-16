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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleShare {

	public class SparkleWindow : Window {

		private bool Visibility;
		private VBox LayoutVerticalLeft;
		private VBox LayoutVerticalRight;
		private HBox LayoutHorizontal;

		public Notebook Notebook;
		private TreeView ReposView;
		private ListStore ReposStore;
		private SparkleRepo [] Repositories;

		public SparkleWindow (SparkleRepo [] R) : base ("SparkleShare")  {

			Repositories = R;
			
			// Show a notification if there are no folders yet
			if (Repositories.Length == 0) {

				SparkleBubble NoFoldersBubble;
				NoFoldersBubble = new SparkleBubble ("Welcome to SparkleShare!",
				                                     "You don't have any " +
						                               "folders set up yet.\n" +
						                               "Please create some in " +
						                               "the SparkleShare folder.");

				NoFoldersBubble.IconName = "folder-sparkleshare";
				NoFoldersBubble.AddAction ("", "Open Folder", 
				                           delegate {
				                           	Process Process = new Process ();
									               Process.StartInfo.FileName =
									               	"xdg-open";
					  	                     	Process.StartInfo.Arguments =
					  	                     		SparklePaths.SparklePath;
						 	                   	Process.Start();
				                           } );

			} else CreateWindow ();

		}

		public void CreateWindow () {
		
			Visibility = false;
			SetSizeRequest (720, 540);
	 		SetPosition (WindowPosition.Center);
			BorderWidth = 6;
			IconName = "folder-sparkleshare";

				VBox LayoutVertical = new VBox (false, 0);

					Notebook = new Notebook ();
					Notebook.BorderWidth = 6;

						LayoutHorizontal = new HBox (false, 0);

							ReposStore = new ListStore (typeof (Gdk.Pixbuf), 
								                         typeof (string),
								                         typeof (SparkleRepo));

							LayoutVerticalLeft = CreateReposList ();
							LayoutVerticalLeft.BorderWidth = 12;

							LayoutVerticalRight =
								CreateDetailedView (Repositories [0]);

						LayoutHorizontal.PackStart (LayoutVerticalLeft,
						                            false, false, 0);
						                            
						LayoutHorizontal.PackStart (LayoutVerticalRight,
						                            true, true, 12);

					Notebook.AppendPage (CreateEventLog (), new Label ("Events"));
					Notebook.AppendPage (LayoutHorizontal, new Label ("Folders"));

				LayoutVertical.PackStart (Notebook, true, true, 0);

					HButtonBox DialogButtons = new HButtonBox ();
					DialogButtons.Layout = ButtonBoxStyle.End;
					DialogButtons.BorderWidth = 6;

						Button CloseButton = new Button (Stock.Close);
						CloseButton.Clicked += delegate (object o, EventArgs args) {
							Visibility = false;
							HideAll ();
						};

					DialogButtons.Add (CloseButton);

				LayoutVertical.PackStart (DialogButtons, false, false, 0);


/*			Timer RedrawTimer = new Timer ();
			RedrawTimer.Interval = 5000;
			RedrawTimer.Elapsed += delegate { 

				TreeSelection Selection = ReposView.Selection;;
				TreeIter Iter = new TreeIter ();;
				Selection.GetSelected (out Iter);
				SparkleRepo SparkleRepo = (SparkleRepo)ReposStore.GetValue (Iter, 2);
				Console.WriteLine(SparkleRepo.Name);									
			
				LayoutHorizontal.Remove (LayoutVerticalRight);

				LayoutVerticalRight = CreateDetailedView (SparkleRepo);

				LayoutHorizontal.PackStart (LayoutVerticalRight, true, true, 12);
				ShowAll ();
			
			
			};

			RedrawTimer.Start();
*/
			Add (LayoutVertical);		
		
		}


		// Creates a visual list of repositories
		public VBox CreateReposList() {

			Gdk.Pixbuf FolderIcon = SparkleHelpers.GetIcon ("folder", 32);
				
			TreeIter ReposIter;
			foreach (SparkleRepo SparkleRepo in Repositories) {

				ReposIter = ReposStore.Prepend ();

				ReposStore.SetValue (ReposIter, 0, FolderIcon);

				ReposStore.SetValue (ReposIter, 1, SparkleRepo.Name + "    \n" + 
					                                SparkleRepo.Domain + "    ");

				ReposStore.SetValue (ReposIter, 2, SparkleRepo);

			}

			ScrolledWindow ScrolledWindow = new ScrolledWindow ();

			ReposView = new TreeView (ReposStore); 
			ReposView.HeadersVisible = false;

			ReposView.AppendColumn ("", new CellRendererPixbuf () , "pixbuf", 0);  
			ReposView.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);

			TreeViewColumn [] ReposViewColumns = ReposView.Columns;

			ReposViewColumns [0].MinWidth = 48;

			ReposStore.IterNthChild (out ReposIter, 0);
			ReposView.ActivateRow (ReposStore.GetPath (ReposIter),
			                       ReposViewColumns [1]);

			// Update the detailed view when something
			// gets selected in the folders list.
			ReposView.CursorChanged += delegate {

				TreeSelection Selection = ReposView.Selection;;
				TreeIter Iter = new TreeIter ();;

				Selection.GetSelected (out Iter);

				SparkleRepo SparkleRepo =
					(SparkleRepo) ReposStore.GetValue (Iter, 2);
			
				LayoutHorizontal.Remove (LayoutVerticalRight);
				LayoutVerticalRight = CreateDetailedView (SparkleRepo);
				LayoutHorizontal.PackStart (LayoutVerticalRight, true, true, 12);
				ShowAll ();

			};


			ScrolledWindow.AddWithViewport (ReposView);
			ScrolledWindow.WidthRequest = 200;
			VBox VBox = new VBox (false, 6);
			VBox.PackStart (ScrolledWindow, true, true, 0);

			return VBox;

		}

		// Creates the detailed view
		public VBox CreateDetailedView (SparkleRepo SparkleRepo) {

			// Create box layout for Remote Address
			HBox RemoteUrlBox = new HBox (false, 0);

				Label Property1 = new Label ("Remote address:");
				Property1.WidthRequest = 120;
				Property1.Xalign = 0;

				Label Value1 = new Label
					("<b>" + SparkleRepo.RemoteOriginUrl + "</b>");

				Value1.UseMarkup = true;

			RemoteUrlBox.PackStart (Property1, false, false, 0);
			RemoteUrlBox.PackStart (Value1, false, false, 0);

			// Create box layout for repository path
			HBox LocalPathBox = new HBox (false, 0);

				Label Property2 = new Label ("Local path:");
				Property2.WidthRequest = 120;
				Property2.Xalign = 0;

				Label Value2 = new Label
					("<b>" + SparkleRepo.LocalPath + "</b>");

				Value2.UseMarkup = true;

			LocalPathBox.PackStart (Property2, false, false, 0);
			LocalPathBox.PackStart (Value2, false, false, 0);

			CheckButton NotifyChangesCheckButton = 
				new CheckButton ("Notify me when something changes");

			string NotifyChangesFileName =
				SparkleHelpers.CombineMore (SparkleRepo.LocalPath,
				                            ".git", "sparkleshare.notify");
			                                        
			if (File.Exists (NotifyChangesFileName))
				NotifyChangesCheckButton.Active = true;
				
			NotifyChangesCheckButton.Toggled += delegate {
				if (File.Exists (NotifyChangesFileName)) {
					SparkleRepo.NotifyChanges = false;
					File.Delete (NotifyChangesFileName);
				} else {
					SparkleRepo.NotifyChanges = true;
					File.Create (NotifyChangesFileName);
				}
			};

			CheckButton SyncChangesCheckButton = 
				new CheckButton ("Synchronize my changes");

			string SyncChangesFileName =
				SparkleHelpers.CombineMore (SparkleRepo.LocalPath,
				                            ".git", "sparkleshare.sync");

			if (File.Exists (SyncChangesFileName))
				SyncChangesCheckButton.Active = true;

			SyncChangesCheckButton.Toggled += delegate {
				if (File.Exists (SyncChangesFileName)) {
					SparkleRepo.SyncChanges = false;
					File.Delete (SyncChangesFileName);
				} else {
					SparkleRepo.SyncChanges = true;
					File.Create (SyncChangesFileName);
				}
			};

			VBox VBox = new VBox (false, 0);

				Table Table = new Table(7, 2, false);
				Table.RowSpacing = 6;

				Table.Attach (RemoteUrlBox, 0, 2, 0, 1);
				Table.Attach (LocalPathBox, 0, 2, 1, 2);
				Table.Attach (NotifyChangesCheckButton, 0, 2, 4, 5);
				Table.Attach (SyncChangesCheckButton, 0, 2, 5, 6);

				Label PeopleLabel =
					new Label ("<span font_size='large'><b>Active users" +
						        "</b></span>");

				PeopleLabel.UseMarkup = true;
				PeopleLabel.SetAlignment (0, 0);

			VBox.PackStart (Table, false, false, 12);

			VBox.PackStart (PeopleLabel, false, false, 0);
			VBox.PackStart (CreatePeopleList (SparkleRepo ), true, true, 12);

			return VBox;

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
			foreach (SparkleRepo SparkleRepo in Repositories) {

				// We're using the snowman here to separate messages :)
				Process.StartInfo.Arguments =
					"log --format=\"%at☃In ‘" + SparkleRepo.Name + "’, %an %s☃%cr\" -25";

				Process.StartInfo.WorkingDirectory = SparkleRepo.LocalPath;
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

				string IconFile = "document-edited";		

				if (Message.IndexOf (" added ‘") > -1)
					IconFile = "document-added";

				if (Message.IndexOf (" deleted ‘") > -1)
					IconFile = "document-removed";

				if (Message.IndexOf (" moved ‘") > -1 || 
					Message.IndexOf (" renamed ‘") > -1)
					IconFile = "document-moved";

				Gdk.Pixbuf ChangeIcon = SparkleHelpers.GetIcon (IconFile, 16);
				Iter = LogStore.Append ();
				LogStore.SetValue (Iter, 0, ChangeIcon);
				LogStore.SetValue (Iter, 1, Message);
				LogStore.SetValue (Iter, 2, " " + TimeAgo);

			}

			TreeView LogView = new TreeView (LogStore); 
			LogView.HeadersVisible = false;
			

			CellRendererText TextCellRight = new Gtk.CellRendererText ();
			TextCellRight.Alignment = Pango.Alignment.Right;

			CellRendererText TextCellMiddle = new Gtk.CellRendererText ();
			TextCellMiddle.Ellipsize = Pango.EllipsizeMode.End;

			LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			LogView.AppendColumn ("", TextCellMiddle, "text", 1);
			LogView.AppendColumn ("", TextCellRight, "text", 2);

			TreeViewColumn [] Columns = LogView.Columns;
			Columns [0].MinWidth = 32;
			Columns [1].Expand = true;
			Columns [1].MaxWidth = 150;

			ScrolledWindow ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (LogView);
			ScrolledWindow.BorderWidth = 12;

			return ScrolledWindow;

		}

		// Creates a visual list of people working in the repo
		public ScrolledWindow CreatePeopleList (SparkleRepo SparkleRepo) {

			Process Process = new Process ();
			Process.EnableRaisingEvents = false; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			// Get a log of commits, example: "Hylke Bons☃added 'file'."
			Process.StartInfo.FileName = "git";
			Process.StartInfo.Arguments = "log --format=\"%an☃%ae\" -50";
			Process.StartInfo.WorkingDirectory = SparkleRepo.LocalPath;
			Process.Start();

			string Output = Process.StandardOutput.ReadToEnd().Trim ();
			string [] People = new string [50];
			string [] Lines = Regex.Split (Output, "\n");

			ListStore PeopleStore = new ListStore (typeof (Gdk.Pixbuf),
				                                   typeof (string));

			int i = 0;
			TreeIter PeopleIter;
			foreach (string Line in Lines) {

				// Only add name if it isn't there already
				if (Array.IndexOf (People, Line) == -1) {

					People [i]      = Line;
					string [] Parts = Regex.Split (Line, "☃");

					string UserName  = Parts [0];
					string UserEmail = Parts [1];

					// Do something special if the person is you
					if (UserName.Equals (SparkleRepo.UserName))
						UserName += " (that’s you!)";

					// Actually add to the list
					PeopleIter = PeopleStore.Prepend ();
					PeopleStore.SetValue (PeopleIter, 0,
					                      SparkleHelpers.GetAvatar (UserEmail , 32));
					PeopleStore.SetValue (PeopleIter, 1,
					                      "<b>" + UserName + "</b>\n" +
					                      "<span font_size=\"smaller\">" + UserEmail + "</span>");

				}

				i++;

			}

			IconView PeopleView = new IconView (PeopleStore); 
			PeopleView.PixbufColumn = 0;
			PeopleView.MarkupColumn = 1;
			PeopleView.Columns = 2;
			PeopleView.Spacing = 6;
			PeopleView.ItemWidth = 200;
			PeopleView.Orientation = Orientation.Horizontal;
			ScrolledWindow ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (PeopleView);

			return ScrolledWindow;

		}

		// Shows or hides the window
		public void ToggleVisibility() {
			if (Repositories.Length > 0) {
				Present ();
				if (Visibility) {
					if (HasFocus)
						HideAll ();
				} else {
					ShowAll ();
				}
			}
		}

	}

}
