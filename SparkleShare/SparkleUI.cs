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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Gtk;
using Mono.Unix;
using Mono.Unix.Native;
using SparkleLib;

namespace SparkleShare {

	public class SparkleUI {
		
		public static SparkleStatusIcon StatusIcon;
		public static List <SparkleLog> OpenLogs;


		// Short alias for the translations
		public static string _(string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleUI ()
		{

			// Initialize the application
			Application.Init ();

			// Create the statusicon
			StatusIcon = new SparkleStatusIcon ();
			
			// Keep track of which event logs are open
			OpenLogs = new List <SparkleLog> ();

			if (SparkleShare.Controller.FirstRun) {

				SparkleIntro intro = new SparkleIntro ();
				intro.ShowAccountForm ();

			}
			
			SparkleShare.Controller.OnQuitWhileSyncing += delegate {
				
				// TODO: Pop up a warning when quitting whilst syncing	

			};

			SparkleShare.Controller.OnInvitation += delegate (string server, string folder, string token) {
				Application.Invoke (delegate {
					
					SparkleIntro intro = new SparkleIntro ();
					intro.ShowInvitationPage (server, folder, token);

				});
			};

			// Show a bubble when there are new changes
			SparkleShare.Controller.NotificationRaised += delegate (SparkleCommit commit, string repository_path) {

				foreach (SparkleLog log in OpenLogs)
					if (log.LocalPath.Equals (repository_path))
						log.UpdateEventLog ();

				if (!SparkleShare.Controller.NotificationsEnabled)
					return;

				string file_name = "";
				string message = null;

				if (commit.Added.Count > 0) {

					foreach (string added in commit.Added) {
						file_name = added;
						break;
					}

					message = String.Format (_("added ‘{0}’"), file_name);

				}

				if (commit.Edited.Count > 0) {

					foreach (string modified in commit.Edited) {
						file_name = modified;
						break;
					}

					message = String.Format (_("edited ‘{0}’"), file_name);

				}

				if (commit.Deleted.Count > 0) {

					foreach (string removed in commit.Deleted) {
						file_name = removed;
						break;
					}

					message = String.Format (_("deleted ‘{0}’"), file_name);

				}

				int changes_count = (commit.Added.Count +
						             commit.Edited.Count +
						             commit.Deleted.Count);

				if (changes_count > 1)
					message += " + " + (changes_count - 1);


				Application.Invoke (delegate {

					SparkleBubble bubble = new SparkleBubble (commit.UserName, message);

					string avatar_file_path = SparkleUIHelpers.GetAvatar (commit.UserEmail, 32);

					if (avatar_file_path != null)
						bubble.Icon = new Gdk.Pixbuf (avatar_file_path);
					else
						bubble.Icon = SparkleUIHelpers.GetIcon ("avatar-default", 32);

					bubble.AddAction ("", "Show Events", delegate {
				
						SparkleLog log = new SparkleLog (repository_path);
						log.ShowAll ();
				
					});

					bubble.Show ();

				});

			};


			// Show a bubble when there was a conflict
			SparkleShare.Controller.ConflictNotificationRaised += delegate {
				Application.Invoke (delegate {

					string title   = _("Ouch! Mid-air collision!");
					string subtext = _("Don't worry, SparkleShare made a copy of each conflicting file.");

					new SparkleBubble (title, subtext).Show ();

				});
			};


			SparkleShare.Controller.AvatarFetched += delegate {
			
				Application.Invoke (delegate {
					
					foreach (SparkleLog log in OpenLogs)
						log.UpdateEventLog ();
					
				});
		
			};


			SparkleShare.Controller.OnIdle += delegate {
			
				Application.Invoke (delegate {
					
					foreach (SparkleLog log in OpenLogs)
						log.UpdateEventLog ();
					
				});
		
			};

		}


		// Runs the application
		public void Run ()
		{

			Application.Run ();

		}

	}

}
