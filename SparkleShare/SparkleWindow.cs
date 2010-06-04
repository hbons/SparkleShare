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
		private VBox LayoutHorizontal;
		private ScrolledWindow LogScrolledWindow;
		private ScrolledWindow PeopleScrolledWindow;
		private string SelectedEmail;

		public SparkleWindow (SparkleRepo Repo) : base ("")  {

			SparkleRepo = Repo;
			SelectedEmail = "";

			SetSizeRequest (720, 540);
	 		SetPosition (WindowPosition.Center);
			BorderWidth = 6;
			Title = _("‘" + SparkleRepo.Name + "’ on " + 
			          SparkleRepo.RemoteOriginUrl
			          .TrimEnd (("/" + SparkleRepo.Name + ".git").ToCharArray ()));
			IconName = "folder";

			VBox LayoutVertical = new VBox (false, 0);

				LayoutHorizontal = new VBox (false, 6);
				LayoutHorizontal.BorderWidth = 6;
				LayoutHorizontal.PackStart (CreatePeopleList (), false, false, 0);
				LayoutHorizontal.PackStart (CreateEventLog (), true, true, 0);

				LayoutVertical.PackStart (LayoutHorizontal, true, true, 0);

					HButtonBox DialogButtons = new HButtonBox ();
					DialogButtons.Layout = ButtonBoxStyle.End;
					DialogButtons.BorderWidth = 6;

						Button CloseButton = new Button (Stock.Close);
						CloseButton.Clicked += delegate (object o, EventArgs args) {
							Destroy ();
						};

					DialogButtons.Add (CloseButton);

				LayoutVertical.PackStart (DialogButtons, false, false, 0);

			Add (LayoutVertical);		
		
		}

		public void UpdateEventLog () {
			LayoutHorizontal.Remove (LogScrolledWindow);
			LogScrolledWindow = CreateEventLog ();
			LayoutHorizontal.PackStart (LogScrolledWindow, true, true, 0);
			ShowAll ();
		}

		public void UpdatePeopleList () {
			LayoutHorizontal.Remove (PeopleScrolledWindow);
			PeopleScrolledWindow = CreatePeopleList ();
			LayoutHorizontal.PackStart (PeopleScrolledWindow, false, false, 0);
			LayoutHorizontal.ReorderChild (PeopleScrolledWindow, 0);
			ShowAll ();
		}

		public ScrolledWindow CreateEventLog () {

			ListStore LogStore = new ListStore (typeof (Gdk.Pixbuf),
				                                 typeof (string),
				                                 typeof (string),
				                                 typeof (string),
				                                 typeof (string));

			Process Process = new Process ();
			Process.EnableRaisingEvents = true; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.FileName = "git";

			string Output = "";

			Process.StartInfo.WorkingDirectory = SparkleRepo.LocalPath;
			// We're using the snowman here to separate messages :)
			Process.StartInfo.Arguments =
				"log --format=\"%at☃%s☃%an☃%cr☃%ae\" -25";
			Process.Start ();

			Output += "\n" + Process.StandardOutput.ReadToEnd ().Trim ();

			Output = Output.TrimStart ("\n".ToCharArray ());
			string [] Lines = Regex.Split (Output, "\n");

			// Sort by time and get the last 25
			Array.Sort (Lines);
			Array.Reverse (Lines);

			TreeIter Iter;
			for (int i = 0; i < 25 && i < Lines.Length; i++) {

				string Line = Lines [i];
				if (Line.Contains (SelectedEmail)) {

					// Look for the snowman!
					string [] Parts = Regex.Split (Line, "☃");
					string Message = Parts [1];
					string UserName = Parts [2];
					string TimeAgo = Parts [3];
					string UserEmail = Parts [4];

					string IconFile = "document-edited";		

					if (Message.IndexOf ("added ‘") > -1)
						IconFile = "document-added";

					if (Message.IndexOf ("deleted ‘") > -1)
						IconFile = "document-removed";

					if (Message.IndexOf ("moved ‘") > -1 || 
						Message.IndexOf ("renamed ‘") > -1)
						IconFile = "document-moved";

					Gdk.Pixbuf ChangeIcon = SparkleHelpers.GetIcon (IconFile, 16);
					Iter = LogStore.Append ();
					LogStore.SetValue (Iter, 0, ChangeIcon);
					LogStore.SetValue (Iter, 1,
					                   Message.Replace ("/", " → "));

					if (SparkleRepo.UserEmail.Equals (UserEmail))
						LogStore.SetValue (Iter, 2, "You");
					else
						LogStore.SetValue (Iter, 2, UserName);

					LogStore.SetValue (Iter, 3, TimeAgo);

					// We're not showing e-mail, it's only 
					// there for lookup purposes
					LogStore.SetValue (Iter, 4, UserEmail);

				}

			}

			TreeView LogView = new TreeView (LogStore); 
			LogView.HeadersVisible = false;
			
			CellRendererText TextCellRight = new Gtk.CellRendererText ();
			TextCellRight.Xalign = 1;

			CellRendererText TextCellMiddle = new Gtk.CellRendererText ();
			TextCellMiddle.Ellipsize = Pango.EllipsizeMode.End;

			LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			LogView.AppendColumn ("", TextCellMiddle, "text", 1);
			LogView.AppendColumn ("", TextCellMiddle, "text", 2);
			LogView.AppendColumn ("", TextCellRight, "text", 3);

			TreeViewColumn [] Columns = LogView.Columns;
			Columns [0].MinWidth = 28;
			Columns [1].Expand = true;
			Columns [2].Expand = true;
			Columns [1].MinWidth = 350;

			LogView.CursorChanged += delegate (object o, EventArgs args) {
			TreeModel Model;
				if (LogView.Selection.GetSelected (out Model, out Iter)) {
					SelectedEmail = (string) Model.GetValue (Iter, 4);
					UpdatePeopleList ();
				}
			};

			LogScrolledWindow = new ScrolledWindow ();
			LogScrolledWindow.AddWithViewport (LogView);

			return LogScrolledWindow;

		}

		// Creates a visual list of people working in the repo
		public ScrolledWindow CreatePeopleList () {

			Process Process = new Process ();
			Process.EnableRaisingEvents = true; 
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;

			// Get a log of commits, example: "Hylke Bons☃added 'file'."
			Process.StartInfo.FileName = "git";
			Process.StartInfo.Arguments = "log --format=\"%an☃%ae\" -50";
			Process.StartInfo.WorkingDirectory = SparkleRepo.LocalPath;
			Process.Start ();

			string Output = Process.StandardOutput.ReadToEnd ().Trim ();
			string [] People = new string [50];
			string [] Lines = Regex.Split (Output, "\n");

			ListStore PeopleStore = new ListStore (typeof (Gdk.Pixbuf),
				                                    typeof (string),
				                                    typeof (string));


			int i = 0;
			TreeIter Iter;
			TreePath TreePath;
			foreach (string Line in Lines) {

				// Only add name if it isn't there already
				if (Array.IndexOf (People, Line) == -1) {

					People [i]      = Line;
					string [] Parts = Regex.Split (Line, "☃");

					string UserName  = Parts [0];
					string UserEmail = Parts [1];

					// Do something special if the person is you
					if (UserEmail.Equals (SparkleRepo.UserEmail))
						UserEmail = _("That’s you!");

					// Actually add to the list
					Iter = PeopleStore.Prepend ();
					PeopleStore.SetValue (Iter, 0,
					                      SparkleHelpers.GetAvatar (UserEmail , 32));
					PeopleStore.SetValue (Iter, 1,
					                      "<b>" + UserName + "</b>\n" +
					                      "<span font_size=\"smaller\">" +
					                      UserEmail + "</span>");
					PeopleStore.SetValue (Iter, 2, UserEmail);

				}

				i++;

			}

			IconView PeopleView = new IconView (PeopleStore); 
			PeopleView.PixbufColumn = 0;
			PeopleView.MarkupColumn = 1;
			PeopleView.Columns = 3;
			PeopleView.Spacing = 6;
			PeopleView.ItemWidth = 210;
			PeopleView.Orientation = Orientation.Horizontal;
			PeopleView.SelectionMode = SelectionMode.Single;

			// Compose an e-mail when an item is activated
			PeopleView.ItemActivated +=
				delegate (object o, ItemActivatedArgs Args) {
					switch (SparklePlatform.Name) {
						case "GNOME":
							Process.StartInfo.FileName = "xdg-open";
							break;
						case "OSX":
							Process.StartInfo.FileName = "open";
							break;						
					}
					Process.StartInfo.Arguments = "mailto:" + SelectedEmail;
					Process.Start ();
			};

			// Select the person matching with the committer event list
			i = 0;
			foreach (object [] Row in PeopleStore) {
				string UserEmail = (string) Row [2];
				if (UserEmail.Equals (SelectedEmail)) {
					TreePath = new TreePath (new int [1] {i});
					PeopleView.SelectPath (TreePath);
				}
				i++;
			}

			// Update the event log when a person is clicked
			PeopleView.SelectionChanged += delegate (object o, EventArgs args) {
				if (PeopleView.SelectedItems.Length > 0) {
					PeopleStore.GetIter (out Iter, PeopleView.SelectedItems [0]);
					string NewSelectedEmail = (string) PeopleStore.GetValue (Iter, 2);
					if (NewSelectedEmail.Equals (SelectedEmail)) {
						SelectedEmail = "";
						PeopleView.UnselectAll ();
					} else
						SelectedEmail = NewSelectedEmail;
				} else SelectedEmail = "";
				UpdateEventLog ();
			};
			
			PeopleScrolledWindow = new ScrolledWindow ();
			PeopleScrolledWindow.AddWithViewport (PeopleView);
			PeopleScrolledWindow.HeightRequest = 200;

			return PeopleScrolledWindow;

		}

	}

}
