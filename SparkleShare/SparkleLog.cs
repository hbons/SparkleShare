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

		public readonly string LocalPath;
		private VBox LayoutVertical;
		private ScrolledWindow ScrolledWindow;
		private MenuBar MenuBar;

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
			BorderWidth = 0;
			
			// TRANSLATORS: {0} is a folder name, and {1} is a server address
			Title = String.Format(_("Recent Events in ‘{0}’"), name);
			IconName = "folder-sparkleshare";

			DeleteEvent += delegate {
				Close ();
			};
			

			// Adds a hidden menubar that contains to enable keyboard
			// shortcuts to close the log
			MenuBar = new MenuBar ();

				MenuItem file_item = new MenuItem ("File");

				    Menu file_menu = new Menu ();

						MenuItem close_1 = new MenuItem ("Close1");
						MenuItem close_2 = new MenuItem ("Close2");
		
						// adds specific Ctrl+W and Esc key accelerators to Log Window
						AccelGroup accel_group = new AccelGroup ();
						AddAccelGroup (accel_group);

						// Close on Esc
						close_1.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.W, Gdk.ModifierType.ControlMask,
							AccelFlags.Visible));

						close_1.Activated += delegate { Close (); };

						// Close on Ctrl+W
						close_2.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.Escape, Gdk.ModifierType.None,
							AccelFlags.Visible));
						close_2.Activated += delegate { Close (); };

					file_menu.Append (close_1);
					file_menu.Append (close_2);

				file_item.Submenu = file_menu;

			MenuBar.Append (file_item);

			// Hacky way to hide the menubar, but the accellerators
			// will simply be disabled when using Hide ()
			MenuBar.HeightRequest = 1;
			MenuBar.ModifyBg (StateType.Normal, Style.Background (StateType.Normal));
			
			LayoutVertical = new VBox (false, 0);

			LayoutVertical.PackStart (CreateEventLog (), true, true, 0);

				HButtonBox dialog_buttons = new HButtonBox {
					Layout = ButtonBoxStyle.Edge,
					BorderWidth = 12
				};

					Button open_folder_button = new Button (_("_Open Folder")) {
						UseUnderline = true
					};
 
					open_folder_button.Clicked += delegate (object o, EventArgs args) {

						Process process = new Process ();
						process.StartInfo.FileName  = Defines.OPEN_COMMAND;
						process.StartInfo.Arguments = LocalPath.Replace (" ", "\\ "); // Escape space-characters
						process.Start ();

						Close ();

					};

					Button close_button = new Button (Stock.Close);

					close_button.Clicked += delegate {
						Close ();
					};

				dialog_buttons.Add (open_folder_button);
				dialog_buttons.Add (close_button);

			// We have to hide the menubar somewhere...
			LayoutVertical.PackStart (MenuBar, false, false, 0);
			LayoutVertical.PackStart (dialog_buttons, false, false, 0);

			Add (LayoutVertical);

		}


		public void Close ()
		{

			foreach (SparkleRepo repo in SparkleShare.Controller.Repositories) {

				if (repo.LocalPath.Equals (LocalPath)) {

					// Remove the eventhooks
					repo.NewCommit -= UpdateEventLog;
					repo.PushingFinished -= UpdateEventLog;
					repo.PushingFailed -= UpdateEventLog;
					repo.FetchingFinished -= UpdateEventLog;
					repo.FetchingFailed -= UpdateEventLog;

				}

			}

			HideAll ();

		}


		public void UpdateEventLog (object o, EventArgs args)
		{

			Application.Invoke (delegate {

				LayoutVertical.Remove (ScrolledWindow);
				ScrolledWindow = CreateEventLog ();
				LayoutVertical.PackStart (ScrolledWindow, true, true, 0);
				LayoutVertical.ReorderChild (ScrolledWindow, 0);
				ShowAll ();

			});

		}


		private ScrolledWindow CreateEventLog ()
		{

			List <SparkleCommit> commits = new List <SparkleCommit> ();

			foreach (SparkleRepo repo in SparkleShare.Controller.Repositories) {

				// Get commits from the repository
				if (repo.LocalPath.Equals (LocalPath)) {

					commits = repo.GetCommits (25);

					// Update the log when there are new remote changes
					repo.NewCommit += UpdateEventLog;

					// Update the log when changes are being sent
					repo.PushingFinished += UpdateEventLog;
					repo.PushingFailed += UpdateEventLog;

					repo.FetchingFinished += UpdateEventLog;
					repo.FetchingFailed += UpdateEventLog;

					break;

				}

			}


			List <ActivityDay> activity_days = new List <ActivityDay> ();

			foreach (SparkleCommit commit in commits) {

				bool commit_inserted = false;
				foreach (ActivityDay stored_activity_day in activity_days) {

					if (stored_activity_day.DateTime.Year  == commit.DateTime.Year &&
					    stored_activity_day.DateTime.Month == commit.DateTime.Month &&
					    stored_activity_day.DateTime.Day   == commit.DateTime.Day) {

					    stored_activity_day.Add (commit);
					    commit_inserted = true;
					    break;

					}

				}
				
				if (!commit_inserted) {

						ActivityDay activity_day = new ActivityDay (commit.DateTime);
						activity_day.Add (commit);
						activity_days.Add (activity_day);
					
				}

			}

			VBox layout_vertical = new VBox (false, 0);

			if (SparkleShare.Controller.Repositories.Find (
					delegate (SparkleRepo r)
						{ return r.LocalPath.Equals (LocalPath) && r.HasUnsyncedChanges; }
				) != null) {

				string title = _("This folder has unsynced changes");
				string text  = _("We will sync these once we’re connected again");

				SparkleInfobar infobar = new SparkleInfobar ("dialog-error", title, text);

				layout_vertical.PackStart (infobar, false, false, 0);

			} else {

				if (SparkleShare.Controller.Repositories.Find (
					delegate (SparkleRepo r)
						{ return r.LocalPath.Equals (LocalPath) && r.HasUnsyncedChanges; }
					) != null) {

						string title = _("Could not sync with the remote folder");
						string text  = _("Is the you and the server online?");

						SparkleInfobar infobar = new SparkleInfobar ("dialog-error", title, text);

						layout_vertical.PackStart (infobar, false, false, 0);

				}

			}

			TreeView tree_view = new TreeView ();
			Gdk.Color background_color = tree_view.Style.Base (StateType.Normal);

			foreach (ActivityDay activity_day in activity_days) {

				EventBox box = new EventBox ();

				Label date_label = new Label ("") {
					UseMarkup = true,
					Xalign = 0,
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

				box.Add (date_label);
				layout_vertical.PackStart (box, false, false, 0);

				Gdk.Color color = Style.Foreground (StateType.Insensitive);
				string secondary_text_color = SparkleUIHelpers.GdkColorToHex (color);

				foreach (SparkleCommit change_set in activity_day) {

					VBox log_entry     = new VBox (false, 0);
					VBox deleted_files = new VBox (false, 0);
					VBox edited_files  = new VBox (false, 0);
					VBox added_files   = new VBox (false, 0);
					VBox moved_files   = new VBox (false, 0);


					foreach (string file_path in change_set.Edited) {

						SparkleLink link = new SparkleLink (file_path,
							SparkleHelpers.CombineMore (LocalPath, file_path));

						link.ModifyBg (StateType.Normal, background_color);

						edited_files.PackStart (link, false, false, 0);

					}

					foreach (string file_path in change_set.Added) {

						SparkleLink link = new SparkleLink (file_path,
							SparkleHelpers.CombineMore (LocalPath, file_path));

						link.ModifyBg (StateType.Normal, background_color);

						added_files.PackStart (link, false, false, 0);

					}


					foreach (string file_path in change_set.Deleted) {

						SparkleLink link = new SparkleLink (file_path,
							SparkleHelpers.CombineMore (LocalPath, file_path));

						link.ModifyBg (StateType.Normal, background_color);

						deleted_files.PackStart (link, false, false, 0);

					}

					for (int i = 0; i < change_set.MovedFrom.Count; i++) {

						SparkleLink from_link = new SparkleLink (change_set.MovedFrom [i],
							SparkleHelpers.CombineMore (LocalPath, change_set.MovedFrom [i]));

						from_link.ModifyBg (StateType.Normal, background_color);

						SparkleLink to_link = new SparkleLink (change_set.MovedTo [i],
							SparkleHelpers.CombineMore (LocalPath, change_set.MovedTo [i]));

						to_link.ModifyBg (StateType.Normal, background_color);

						Label to_label = new Label ("<span fgcolor='" + secondary_text_color +"'>" +
						                            "<small>to</small></span> ") {
					    	UseMarkup = true,
					    	Xalign = 0
					    };

						HBox link_wrapper = new HBox (false, 0);
						link_wrapper.PackStart (to_label, false, false, 0);
						link_wrapper.PackStart (to_link, true, true, 0);

						moved_files.PackStart (from_link, false, false, 0);
						moved_files.PackStart (link_wrapper, false, false, 0);

						if (change_set.MovedFrom.Count > 1)
							moved_files.PackStart (new Label (""), false, false, 0);

					}

					Label change_set_info = new Label ("<b>" + change_set.UserName + "</b>\n" +
					                                   "<span fgcolor='" + secondary_text_color +"'><small>" +
					                                   "at " + change_set.DateTime.ToString ("H:mm") +
					                                   "</small></span>") {
						UseMarkup = true,
						Xalign = 0
					};

					log_entry.PackStart (change_set_info, false, false, 0);
					                   
					if (edited_files.Children.Length > 0) {

						Label edited_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                _("Edited") +
						                                "</small></span>") {
							UseMarkup = true,
							Xalign = 0
						};

						log_entry.PackStart (edited_label, false, false, 0);
						log_entry.PackStart (edited_files, false, false, 0);

					}

					if (added_files.Children.Length > 0) {

						Label added_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                _("Added") +
						                                "</small></span>") {
							UseMarkup = true,
							Xalign = 0
						};

						log_entry.PackStart (added_label, false, false, 0);
						log_entry.PackStart (added_files, false, false, 0);

					}

					if (deleted_files.Children.Length > 0) {

						Label deleted_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                _("Deleted") +
						                                "</small></span>") {
							UseMarkup = true,
							Xalign = 0
						};

						log_entry.PackStart (deleted_label, false, false, 0);
						log_entry.PackStart (deleted_files, false, false, 0);

					}

					if (moved_files.Children.Length > 0) {

						Label moved_label = new Label ("\n<span fgcolor='" + secondary_text_color +"'><small>" +
						                                 _("Moved") +
						                                 "</small></span>") {
							UseMarkup = true,
							Xalign = 0
						};

						log_entry.PackStart (moved_label, false, false, 0);
						log_entry.PackStart (moved_files, false, false, 0);

					}

					HBox hbox = new HBox (false, 0);

					Image avatar = new Image (SparkleUIHelpers.GetAvatar (change_set.UserEmail, 32)) {
						Yalign = 0
					};

					hbox.PackStart (avatar, false, false, 18);

						VBox vbox = new VBox (false, 0);
						vbox.PackStart (log_entry, false, false, 0);

					hbox.PackStart (vbox, true, true, 0);
					hbox.PackStart (new Label (""), false, false, 12);

					layout_vertical.PackStart (hbox, false, false, 18);

				}

				layout_vertical.PackStart (new Label (""), false, false, 3);

			}

			ScrolledWindow = new ScrolledWindow ();

				EventBox wrapper = new EventBox ();
				wrapper.ModifyBg (StateType.Normal, background_color);
				wrapper.Add (layout_vertical);

			ScrolledWindow.AddWithViewport (wrapper);
			(ScrolledWindow.Child as Viewport).ShadowType = ShadowType.None;

			return ScrolledWindow;

		}

	}


	// All commits that happened on a day	
	public class ActivityDay : List <SparkleCommit>
	{

		public DateTime DateTime;

		public ActivityDay (DateTime date_time)
		{

			DateTime = date_time;
			DateTime = new DateTime (DateTime.Year, DateTime.Month, DateTime.Day);

		}

	}

}
