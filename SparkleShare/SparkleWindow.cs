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
using Mono.Unix;
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace SparkleShare {

	public class SparkleWindow : Window {

		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}

		private SparkleRepo SparkleRepo;
		private HBox LayoutHorizontal;
		private ScrolledWindow LogScrolledWindow;
		private ScrolledWindow PeopleScrolledWindow;

		public SparkleWindow (SparkleRepo Repo) : base ("")  {

			SparkleRepo = Repo;
			CreateWindow ();	

		}

		public void CreateWindow () {
		
			SetSizeRequest (900, 480);
	 		SetPosition (WindowPosition.Center);
			BorderWidth = 6;
			Title = _("Happenings in ‘" + SparkleRepo.Name + "’");
			IconName = "folder-sparkleshare";

			VBox LayoutVertical = new VBox (false, 0);

				LayoutHorizontal = new HBox (true, 6);
				LayoutHorizontal.PackStart (CreatePeopleList (""));
				LayoutHorizontal.PackStart (CreateEventLog (""));

				LayoutVertical.PackStart (LayoutHorizontal, true, true, 6);

					HButtonBox DialogButtons = new HButtonBox ();
					DialogButtons.Layout = ButtonBoxStyle.Edge;
					DialogButtons.BorderWidth = 6;

						Button CloseButton = new Button (Stock.Close);
						CloseButton.Clicked += delegate (object o, EventArgs args) {
							Destroy ();
						};

						Button PreferencesButton = new Button (Stock.Preferences);
						PreferencesButton.Clicked += delegate (object o, EventArgs args) {
							SparklePreferencesDialog SparklePreferencesDialog =
								new SparklePreferencesDialog (this, SparkleRepo);
								SparklePreferencesDialog.ShowAll ();
						};

					DialogButtons.Add (PreferencesButton);
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

		public void UpdateEventLog (string SelectedEmail) {
			LayoutHorizontal.Remove (LogScrolledWindow);
			LogScrolledWindow = CreateEventLog (SelectedEmail);
			LayoutHorizontal.Add (LogScrolledWindow);
			ShowAll ();
		}

		public void UpdatePeopleList (string SelectedEmail) {
			LayoutHorizontal.Remove (PeopleScrolledWindow);
			PeopleScrolledWindow = CreatePeopleList (SelectedEmail);
			LayoutHorizontal.Add (PeopleScrolledWindow);
			LayoutHorizontal.ReorderChild (PeopleScrolledWindow, 0);
			ShowAll ();
		}

		public ScrolledWindow CreateEventLog(string SelectedEmail) {

			ListStore LogStore = new ListStore (typeof (Gdk.Pixbuf),
				                                 typeof (string),
				                                 typeof (string),
				                                 typeof (string));

			Process Process = new Process();
			Process.EnableRaisingEvents = false; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.FileName = "git";

			string Output = "";

			Process.StartInfo.WorkingDirectory = SparkleRepo.LocalPath;
			// We're using the snowman here to separate messages :)
			Process.StartInfo.Arguments =
				"log --format=\"%at☃%an %s☃%cr☃%ae\" -25";
			Process.Start();

			Output += "\n" + Process.StandardOutput.ReadToEnd().Trim ();

			Output = Output.TrimStart ("\n".ToCharArray ());
			string [] Lines = Regex.Split (Output, "\n");

			// Sort by time and get the last 25
			Array.Sort (Lines);
			Array.Reverse (Lines);
			string [] LastTwentyFive = new string [25];
			Array.Copy (Lines, 0, LastTwentyFive, 0, 25);

			TreeIter Iter;
			foreach (string Line in LastTwentyFive) {

				if (Line.Contains (SelectedEmail)) {

					// Look for the snowman!
					string [] Parts = Regex.Split (Line, "☃");
					string Message = Parts [1];
					string TimeAgo = Parts [2];
					string UserEmail = Parts [3];

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
					// We're not showing e-mail, it's only 
					// there for lookup purposes
					LogStore.SetValue (Iter, 3, UserEmail);
					Console.WriteLine (UserEmail);

				}

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

			LogView.CursorChanged += delegate(object o, EventArgs args) {
			TreeModel model;
				if (LogView.Selection.GetSelected (out model, out Iter)) {
					UpdatePeopleList ((string) LogStore.GetValue (Iter, 3));
				}
			};


			LogScrolledWindow = new ScrolledWindow ();
			LogScrolledWindow.AddWithViewport (LogView);

			return LogScrolledWindow;

		}

		// Creates a visual list of people working in the repo
		public ScrolledWindow CreatePeopleList (string SelectedEmail) {

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
				                                    typeof (string),
				                                    typeof (string));

			int i = 0;
			TreeIter Iter;
			TreePath TreePath = new TreePath ();
			foreach (string Line in Lines) {

				// Only add name if it isn't there already
				if (Array.IndexOf (People, Line) == -1) {

					People [i]      = Line;
					string [] Parts = Regex.Split (Line, "☃");

					string UserName  = Parts [0];
					string UserEmail = Parts [1];

					// Do something special if the person is you
					if (UserEmail.Equals (SparkleRepo.UserEmail))
						UserName += _(" (that’s you!)");

					// Actually add to the list
					Iter = PeopleStore.Prepend ();
					PeopleStore.SetValue (Iter, 0,
					                      SparkleHelpers.GetAvatar (UserEmail , 32));
					PeopleStore.SetValue (Iter, 1,
					                      "<b>" + UserName + "</b>\n" +
					                      "<span font_size=\"smaller\">" +
					                      UserEmail + "</span>");
					PeopleStore.SetValue (Iter, 2, UserEmail);

					if (UserEmail.Equals (SelectedEmail)) {
						TreePath = PeopleStore.GetPath (Iter);
						Console.WriteLine (TreePath.Indices [0]);
					}

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
			PeopleView.SelectionMode = SelectionMode.Single;

			// TODO: doesn't work. Always seems to select the
			// first row :(
			PeopleView.SelectPath (TreePath);
			
			PeopleView.SelectionChanged += delegate (object o, EventArgs args) {
				if (PeopleView.SelectedItems.Length > 0) {
					PeopleStore.GetIter (out Iter, PeopleView.SelectedItems [0]);
					UpdateEventLog ((string) PeopleStore.GetValue (Iter, 2));
				} else UpdateEventLog ("");
			};
			
			PeopleScrolledWindow = new ScrolledWindow ();
			PeopleScrolledWindow.AddWithViewport (PeopleView);

			return PeopleScrolledWindow;

		}

	}

}
