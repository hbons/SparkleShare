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

	public class SparkleWindow : Window
	{

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		private SparkleRepo SparkleRepo;
		private VBox LayoutVertical;
		private ScrolledWindow LogScrolledWindow;
		private string SelectedEmail;

		public SparkleWindow (SparkleRepo Repo) : base ("")
		{

			SparkleRepo = Repo;
			SelectedEmail = "";
			SetSizeRequest (640, 480);
	 		SetPosition (WindowPosition.Center);
			BorderWidth = 12;
			Title = String.Format(_("‘{0}’ on {1}"), SparkleRepo.Name,
				SparkleRepo.RemoteOriginUrl.TrimEnd (("/" + SparkleRepo.Name + ".git").ToCharArray ()));
			IconName = "folder";

			LayoutVertical = new VBox (false, 12);

			LayoutVertical.PackStart (CreateEventLog (), true, true, 0);

				HButtonBox DialogButtons = new HButtonBox ();
				DialogButtons.Layout = ButtonBoxStyle.Edge;
				DialogButtons.BorderWidth = 0;

					Button OpenFolderButton = new Button (_("Open Folder"));
					OpenFolderButton.Clicked += delegate (object o, EventArgs args) {
						Process Process = new Process ();
						Process.StartInfo.FileName = "xdg-open";
						Process.StartInfo.Arguments =
							SparkleHelpers.CombineMore (SparklePaths.SparklePath, SparkleRepo.Name);
						Process.Start ();
						Destroy ();
					};

					Button CloseButton = new Button (Stock.Close);
					CloseButton.Clicked += delegate (object o, EventArgs args) {
						Destroy ();
					};

				DialogButtons.Add (OpenFolderButton);
				DialogButtons.Add (CloseButton);

			LayoutVertical.PackStart (DialogButtons, false, false, 0);

			Add (LayoutVertical);		
		
		}


		public void UpdateEventLog ()
		{

			LayoutVertical.Remove (LogScrolledWindow);
			LogScrolledWindow = CreateEventLog ();
			LayoutVertical.PackStart (LogScrolledWindow, true, true, 0);
			ShowAll ();

		}


		public ScrolledWindow CreateEventLog ()
		{

			ListStore LogStore = new ListStore (typeof (Gdk.Pixbuf),
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
			Process.StartInfo.Arguments = "log --format=\"%at☃%s☃%an☃%cr☃%ae\" -25";
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
					string Message = Parts [1].Replace ("\n", " ");;
					string UserName = Parts [2];
					string TimeAgo = Parts [3];
					string UserEmail = Parts [4];

					Iter = LogStore.Append ();

					LogStore.SetValue (Iter, 0, SparkleHelpers.GetAvatar (UserEmail, 24));

					if (SparkleRepo.UserEmail.Equals (UserEmail))
						LogStore.SetValue (Iter, 1, "<b>You</b>\n" + Message.Replace ("/", " → "));
					else
						LogStore.SetValue (Iter, 1, "<b>" + UserName + "</b>\n" + Message.Replace ("/", " → "));					
					
					LogStore.SetValue (Iter, 2, TimeAgo + "  ");

					// We're not showing email, it's only 
					// there for lookup purposes
					LogStore.SetValue (Iter, 3, UserEmail);

				}

			}

			TreeView LogView = new TreeView (LogStore); 
			LogView.HeadersVisible = false;

			CellRendererText TextCellRight = new Gtk.CellRendererText ();
			TextCellRight.Xalign = 1;

			LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);

			CellRendererText CellRendererMarkup = new CellRendererText ();
			TreeViewColumn ColumnMarkup = new TreeViewColumn ();
			ColumnMarkup.PackStart (CellRendererMarkup, true);
			LogView.AppendColumn (ColumnMarkup);

			ColumnMarkup.SetCellDataFunc (CellRendererMarkup, new Gtk.TreeCellDataFunc (RenderRow));

			LogView.AppendColumn (ColumnMarkup);
			LogView.AppendColumn ("", TextCellRight, "text", 2);

			TreeViewColumn [] Columns = LogView.Columns;
			Columns [0].MinWidth = 42;
			Columns [1].Expand = true;
			Columns [2].Expand = true;
			Columns [1].MinWidth = 350;
			Columns [2].MinWidth = 50;

			Columns [2].Spacing = 200;
			LogView.CursorChanged += delegate (object o, EventArgs args) {
			TreeModel Model;
				if (LogView.Selection.GetSelected (out Model, out Iter)) {
					SelectedEmail = (string) Model.GetValue (Iter, 3);
				}
			};

			// Compose an e-mail when a row is activated
			LogView.RowActivated +=
				delegate (object o, RowActivatedArgs Args) {
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

			LogScrolledWindow = new ScrolledWindow ();
			LogScrolledWindow.AddWithViewport (LogView);

			return LogScrolledWindow;

		}

		// Renders a row with custom markup
		private void RenderRow (TreeViewColumn Column, CellRenderer Cell, TreeModel Model, TreeIter Iter)
		{

			string Item = (string) Model.GetValue (Iter, 1);
			(Cell as CellRendererText).Markup  = Item;

		}

	}

}
