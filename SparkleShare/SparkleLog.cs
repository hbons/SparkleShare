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

using Gtk;
using Mono.Unix;
using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SparkleShare {

	public class SparkleLog : Window {

		private string LocalPath;
		private VBox LayoutVertical;
		private ScrolledWindow ScrolledWindow;


		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleLog (string path) : base ("")
		{

			LocalPath = path;
			
			string name = System.IO.Path.GetFileName (LocalPath);
			SetSizeRequest (540, 640);

	 		SetPosition (WindowPosition.Center);
			BorderWidth = 12;
			
			// TRANSLATORS: {0} is a folder name, and {1} is a server address
			Title = String.Format(_("Recent Events in ‘{0}’"), name);
			IconName = "folder-sparkleshare";

			LayoutVertical = new VBox (false, 12);

			LayoutVertical.PackStart (CreateEventLog (), true, true, 0);

				HButtonBox dialog_buttons = new HButtonBox {
					Layout = ButtonBoxStyle.Edge,
					BorderWidth = 0
				};

					Button open_folder_button = new Button (_("_Open Folder")) {
						UseUnderline = true
					};

					open_folder_button.Clicked += delegate (object o, EventArgs args) {

						Process process = new Process ();
						process.StartInfo.FileName  = "xdg-open";
						process.StartInfo.Arguments = LocalPath.Replace (" ", "\\ "); // Escape space-characters
						process.Start ();

						Destroy ();

					};

					Button close_button = new Button (Stock.Close);

					close_button.Clicked += delegate (object o, EventArgs args) {
						Destroy ();
					};

				dialog_buttons.Add (open_folder_button);
				dialog_buttons.Add (close_button);

			LayoutVertical.PackStart (dialog_buttons, false, false, 0);

			Add (LayoutVertical);		

		}


		public void UpdateEventLog ()
		{

			LayoutVertical.Remove (ScrolledWindow);
			ScrolledWindow = CreateEventLog ();
			LayoutVertical.PackStart (ScrolledWindow, true, true, 0);
			LayoutVertical.ReorderChild (ScrolledWindow, 0);
			ShowAll ();

		}


		private ScrolledWindow CreateEventLog ()
		{

			int number_of_events = 40;

			Process process = new Process () {
				EnableRaisingEvents = true
			};

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WorkingDirectory = LocalPath;
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "log --format=\"%at☃%an☃%ae☃%s☃%H\" -" + number_of_events;

			process.Start ();

			string output = process.StandardOutput.ReadToEnd ().Trim ();

			output = output.TrimStart ("\n".ToCharArray ());
			string [] lines = Regex.Split (output, "\n");
			int linesLength = lines.Length;
			if (output == "")
				linesLength = 0;

			// Sort by time and get the last 25
			Array.Sort (lines);
			Array.Reverse (lines);

			List <ActivityDay> activity_days = new List <ActivityDay> ();

			for (int i = 0; i < number_of_events && i < linesLength; i++) {

					string line = lines [i];

					// Look for the snowman!
					string [] parts = Regex.Split (line, "☃");

					int unix_timestamp     = int.Parse (parts [0]);
					string user_name  = parts [1];
					string user_email = parts [2];
					string message    = parts [3];
					string hash       = parts [4];

					DateTime date_time = UnixTimestampToDateTime (unix_timestamp);

					message = message.Replace ("\n", " ");

					ChangeSet change_set = new ChangeSet (user_name, user_email, message, date_time, hash);

					process.StartInfo.Arguments = "show " + hash + " --name-status";
					process.Start ();


					output = process.StandardOutput.ReadToEnd ().Trim ();

					output = output.TrimStart ("\n".ToCharArray ());
					string [] file_lines = Regex.Split (output, "\n");

					foreach (string file_line in file_lines) {

						string file_path = "";

						if (file_line.Length > 1)
							file_path = file_line.Substring (2);

						if (file_line.StartsWith ("M\t"))
							change_set.Edited.Add (file_path);

						if (file_line.StartsWith ("A\t"))
							change_set.Added.Add (file_path);

						if (file_line.StartsWith ("D\t"))
							change_set.Deleted.Add (file_path);
			
					}


					bool change_set_inserted = false;
					foreach (ActivityDay stored_activity_day in activity_days) {

						if (stored_activity_day.DateTime.Year  == change_set.DateTime.Year &&
						    stored_activity_day.DateTime.Month == change_set.DateTime.Month &&
						    stored_activity_day.DateTime.Day   == change_set.DateTime.Day) {

						    stored_activity_day.Add (change_set);
						    change_set_inserted = true;
						    break;

						}

					}
					
					if (!change_set_inserted) {

							ActivityDay activity_day = new ActivityDay (change_set.DateTime);
							activity_day.Add (change_set);
							activity_days.Add (activity_day);
						
					}

			}

			VBox layout_vertical = new VBox (false, 0);

			TreeView tree_view = new TreeView ();
			Gdk.Color background_color = tree_view.Style.Base (StateType.Normal);

			foreach (ActivityDay activity_day in activity_days) {

				Label date_label = new Label ("") {
					UseMarkup = true,
					Xpad = 9,
					Ypad = 9
				};

					DateTime today = DateTime.Now;
					DateTime yesterday = DateTime.Now.AddDays (-1);

					if (today.Day   == activity_day.DateTime.Day &&
					    today.Month == activity_day.DateTime.Month && 
					    today.Year  == activity_day.DateTime.Year) {

						date_label.Markup = "<b>Today</b>";

					} else if (yesterday.Day   == activity_day.DateTime.Day &&
					           yesterday.Month == activity_day.DateTime.Month && 
					           yesterday.Year  == activity_day.DateTime.Year) {

						date_label.Markup = "<b>Yesterday</b>";

					} else {
	
						date_label.Markup = "<b>" + activity_day.DateTime.ToString ("ddd MMM d, yyyy") + "</b>";

					}

				layout_vertical.PackStart (date_label, true, true, 0);

				Gdk.Color color = Style.Foreground (StateType.Insensitive);
				string secondary_text_color = GdkColorToHex (color);

				foreach (ChangeSet change_set in activity_day) {

					VBox log_entry = new VBox (false, 0);
					VBox deleted_files = new VBox (false, 0);
					VBox edited_files  = new VBox (false, 0);
					VBox added_files   = new VBox (false, 0);

					foreach (string file_path in change_set.Edited) {

						SparkleLink link = new SparkleLink (file_path,
							SparkleHelpers.CombineMore (LocalPath, file_path));

						link.ModifyBg (StateType.Normal, background_color);

						link.ButtonPressEvent += delegate {
							Destroy ();
						};

						edited_files.PackStart (link, false, false, 0);

					}


					foreach (string file_path in change_set.Added) {

						SparkleLink link = new SparkleLink (file_path,
							SparkleHelpers.CombineMore (LocalPath, file_path));

						link.ModifyBg (StateType.Normal, background_color);

						link.ButtonPressEvent += delegate {
							Destroy ();
						};

						added_files.PackStart (link, false, false, 0);

					}


					foreach (string file_path in change_set.Deleted) {

						SparkleLink link = new SparkleLink (file_path,
							SparkleHelpers.CombineMore (LocalPath, file_path));

						link.ModifyBg (StateType.Normal, background_color);

						link.ButtonPressEvent += delegate {
							Destroy ();
						};

						deleted_files.PackStart (link, false, false, 0);

					}


					log_entry.PackStart (new Label ("<b>" + change_set.UserName + "</b>\n" +
					                     "<span fgcolor='" + secondary_text_color +"'><small>" +
					                     "at " + change_set.DateTime.ToString ("HH:mm") +
					                     "</small></span>") { UseMarkup = true, Xalign = 0 });
					                   
					if (edited_files.Children.Length > 0) {

						Label edited_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                "Edited" +
						                                "</small></span>") {
							UseMarkup=true,
							Xalign = 0
						};

						log_entry.PackStart (edited_label, false, false, 0);
						log_entry.PackStart (edited_files, false, false, 0);

					}

					if (added_files.Children.Length > 0) {

						Label added_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                "Added" +
						                                "</small></span>") {
							UseMarkup=true,
							Xalign = 0
						};

						log_entry.PackStart (added_label, false, false, 0);
						log_entry.PackStart (added_files, false, false, 0);

					}

					if (deleted_files.Children.Length > 0) {

						Label deleted_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                "Edited" +
						                                "</small></span>") {
							UseMarkup=true,
							Xalign = 0
						};

						log_entry.PackStart (deleted_label, false, false, 0);
						log_entry.PackStart (deleted_files, false, false, 0);

					}

					HBox hbox = new HBox (false, 0);

					Image avatar = new Image (SparkleHelpers.GetAvatar (change_set.UserEmail, 32)) {
						Yalign = 0
					};

					hbox.PackStart (avatar, false, false, 18);

						VBox vbox = new VBox (false, 0);
						vbox.PackStart (log_entry, true, true, 0);

					hbox.PackStart (vbox, true, true, 0);
					hbox.PackStart (new Label (""), false, false, 12);
					
					layout_vertical.PackStart (hbox, true, true, 24);

				}

			}

			ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.ShadowType = ShadowType.None;

				EventBox wrapper = new EventBox ();
				wrapper.ModifyBg (StateType.Normal, background_color);
				wrapper.Add (layout_vertical);

			ScrolledWindow.AddWithViewport (wrapper);

			return ScrolledWindow;

		}


		// Converts a UNIX timestamp to a more usable time object
		public DateTime UnixTimestampToDateTime (int timestamp)
		{
			DateTime unix_epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0);
			return unix_epoch.AddSeconds (timestamp);
		}


		// Converts a Gdk RGB color to a hex value.
		// Example: from "rgb:0,0,0" to "#000000"
		public string GdkColorToHex (Gdk.Color color)
		{

			return String.Format ("#{0:X2}{1:X2}{2:X2}",
				(int) Math.Truncate (color.Red   / 256.00),
				(int) Math.Truncate (color.Green / 256.00),
				(int) Math.Truncate (color.Blue  / 256.00));

		}

	}

	
	public class ActivityDay : List <ChangeSet>
	{

		public DateTime DateTime;

		public ActivityDay (DateTime date_time)
		{
			DateTime = date_time;
			DateTime = new DateTime (DateTime.Year, DateTime.Month, DateTime.Day);
		}

	}

	
	public class ChangeSet
	{
	
		public string UserName;
		public string UserEmail;
		public string Message;
		public List <string> Added;
		public List <string> Deleted;
		public List <string> Edited;
		public DateTime DateTime;
		public string Hash;
	
		public ChangeSet (string user_name, string user_email, string message, DateTime date_time, string hash)
		{

			UserName  = user_name;
			UserEmail = user_email;
			Message   = message;
			DateTime  = date_time;
			Hash      = hash;

			Edited  = new List <string> ();
			Added   = new List <string> ();
			Deleted = new List <string> ();

		}
	
	}

}
