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
using System.IO;
using System.Timers;

namespace SparkleShare {

	// The statusicon that stays in the
	// user's notification area
	public class SparkleStatusIcon : StatusIcon	{

		private Timer Animation;
		private Gdk.Pixbuf [] AnimationFrames;
		private int FrameNumber;
		private string StateText;
		private Menu Menu;

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleStatusIcon () : base ()
		{

			FrameNumber = 0;
			AnimationFrames = CreateAnimationFrames ();
			Animation = CreateAnimation ();

			Activate += ShowMenu;  // Primary mouse button click
			PopupMenu += ShowMenu; // Secondary mouse button click

			SetNormalState ();
			UpdateMenu ();


			SparkleShare.Controller.FolderSizeChanged += delegate {
				Application.Invoke (delegate {
					UpdateMenu ();
				});
			};
			
			SparkleShare.Controller.RepositoryListChanged += delegate {
				Application.Invoke (delegate {
					SetNormalState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnIdle += delegate {
				Application.Invoke (delegate {
					SetNormalState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnSyncing += delegate {
				Application.Invoke (delegate {
					SetAnimationState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnError += delegate {
				Application.Invoke (delegate {
					SetNormalState (true);
					UpdateMenu ();
				});
			};

		}


		// Slices up the graphic that contains the
		// animation frames.
		private Gdk.Pixbuf [] CreateAnimationFrames ()
		{

			Gdk.Pixbuf [] animation_frames = new Gdk.Pixbuf [5];
			Gdk.Pixbuf frames_pixbuf = SparkleUIHelpers.GetIcon ("process-syncing-sparkleshare", 24);
			
			for (int i = 0; i < animation_frames.Length; i++)
				animation_frames [i] = new Gdk.Pixbuf (frames_pixbuf, (i * 24), 0, 24, 24);

			return animation_frames;

		}


		// Creates the Animation that handles the syncing animation
		private Timer CreateAnimation ()
		{

			Timer Animation = new Timer () {
				Interval = 35
			};

			Animation.Elapsed += delegate {

				if (FrameNumber < AnimationFrames.Length - 1)
					FrameNumber++;
				else
					FrameNumber = 0;

				Application.Invoke (delegate {
					Pixbuf = AnimationFrames [FrameNumber];
				});

			};

			return Animation;

		}


		// Creates the menu that is popped up when the
		// user clicks the status icon
		public void UpdateMenu ()
		{

			Menu = new Menu ();

				// The menu item showing the status and size of the SparkleShare folder
				MenuItem status_menu_item = new MenuItem (StateText) {
					Sensitive = false
				};

				// A menu item that provides a link to the SparkleShare folder
				Gtk.Action folder_action = new Gtk.Action ("", "SparkleShare") {
					IconName    = "folder-sparkleshare",
					IsImportant = true
				};

				folder_action.Activated += delegate {
					SparkleShare.Controller.OpenSparkleShareFolder ();
				};

			Menu.Add (status_menu_item);
			Menu.Add (new SeparatorMenuItem ());
			Menu.Add (folder_action.CreateMenuItem ());

				if (SparkleShare.Controller.Repositories.Count > 0) {

					// Creates a menu item for each repository with a link to their logs
					foreach (SparkleRepo repo in SparkleShare.Controller.Repositories) {

						folder_action = new Gtk.Action ("", repo.Name) {
							IconName    = "folder",
							IsImportant = true
						};

						if (repo.HasUnsyncedChanges)
							folder_action.IconName = "dialog-error";

						folder_action.Activated += OpenEventLogDelegate (repo.LocalPath);

						MenuItem menu_item = (MenuItem) folder_action.CreateMenuItem ();

						if (repo.Description != null)
							menu_item.TooltipText = repo.Description;

						Menu.Add (menu_item);

					}

				} else {

					MenuItem no_folders_item = new MenuItem (_("No Remote Folders Yet")) {
						Sensitive   = false
					};

					Menu.Add (no_folders_item);

				}

				// Opens the wizard to add a new remote folder
				MenuItem sync_item = new MenuItem (_("Sync Remote Folder…"));

				sync_item.Activated += delegate {
					Application.Invoke (delegate {

						SparkleIntro intro = new SparkleIntro ();
						intro.ShowServerForm ();

					});
				};

			Menu.Add (sync_item);
			Menu.Add (new SeparatorMenuItem ());

				// A checkbutton to toggle whether or not to show notifications
				CheckMenuItem notify_item =	new CheckMenuItem (_("Show Notifications"));
								                             
				if (SparkleShare.Controller.NotificationsEnabled)
					notify_item.Active = true;

				notify_item.Toggled += delegate {
					SparkleShare.Controller.ToggleNotifications ();
				};

			Menu.Add (notify_item);
			Menu.Add (new SeparatorMenuItem ());

				// A menu item that takes the user to http://www.sparkleshare.org/
				MenuItem about_item = new MenuItem (_("Visit Website"));

				about_item.Activated += delegate {

					Process process = new Process ();
					process.StartInfo.FileName = "xdg-open";
					process.StartInfo.Arguments = "http://www.sparkleshare.org/";
					process.Start ();

				};

			Menu.Add (about_item);
			Menu.Add (new SeparatorMenuItem ());

				// A menu item that quits the application
				MenuItem quit_item = new MenuItem (_("Quit"));

				quit_item.Activated += delegate {
					SparkleShare.Controller.Quit ();
				};

			Menu.Add (quit_item);

			Menu.ShowAll ();

		}


		// A method reference that makes sure that opening the
		// event log for each repository works correctly
		private EventHandler OpenEventLogDelegate (string path)
		{

			return delegate { 

				SparkleLog log = SparkleUI.OpenLogs.Find (delegate (SparkleLog l) { return l.LocalPath.Equals (path); });

				// Check whether the log is already open, create a new one if
				//that's not the case or present it to the user if it is
				if (log == null) {

					log = new SparkleLog (path);

					log.Hidden += delegate {

					SparkleUI.OpenLogs.Remove (log);
					log.Destroy ();

					};

					SparkleUI.OpenLogs.Add (log);

				}

				log.ShowAll ();
				log.Present ();

			};

		}


		// Makes the menu visible
		private void ShowMenu (object o, EventArgs args)
		{

			Menu.Popup (null, null, SetPosition, 0, Global.CurrentEventTime);

		}


		// Makes sure the menu pops up in the right position
		private void SetPosition (Menu menu, out int x, out int y, out bool push_in)
		{

			PositionMenu (menu, out x, out y, out push_in, Handle);

		}


		// The state when there's nothing going on
		private void SetNormalState ()
		{

			SetNormalState (false);

		}


		// The state when there's nothing going on
		private void SetNormalState (bool error)
		{

			Animation.Stop ();

			if (SparkleShare.Controller.Repositories.Count == 0) {

				StateText = _("No folders yet");
				Pixbuf = AnimationFrames [0];						

			} else {
			
				if (error) {

					StateText = _("Not everything is synced");
					Pixbuf = SparkleUIHelpers.GetIcon ("sparkleshare-syncing-error", 24);

				} else {

					StateText = _("Up to date") + "  (" + SparkleShare.Controller.FolderSize + ")";
					Pixbuf = AnimationFrames [0];

				}

			}

		}


		// The state when animating
		private void SetAnimationState ()
		{

			StateText = _("Syncing…");

			if (!Animation.Enabled)
				Animation.Start ();

		}

	}

}
