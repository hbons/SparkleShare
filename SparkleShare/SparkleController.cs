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


using Mono.Unix;
using SparkleLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;

using System.Security.Cryptography;
using System.Text;

namespace SparkleShare {

	public abstract class SparkleController {

		public List <SparkleRepo> Repositories;
		public string FolderSize;
		public bool FirstRun;
		
		public readonly string SparklePath;


		public event OnQuitWhileSyncingEventHandler OnQuitWhileSyncing;
		public delegate void OnQuitWhileSyncingEventHandler ();

		public event FolderFetchedEventHandler FolderFetched;
		public delegate void FolderFetchedEventHandler ();
		
		public event FolderFetchErrorEventHandler FolderFetchError;
		public delegate void FolderFetchErrorEventHandler ();
		
		public event FolderListChangedEventHandler FolderListChanged;
		public delegate void FolderListChangedEventHandler ();

		public event FolderSizeChangedEventHandler FolderSizeChanged;
		public delegate void FolderSizeChangedEventHandler (string folder_size);
		
		public event AvatarFetchedEventHandler AvatarFetched;
		public delegate void AvatarFetchedEventHandler ();

		public event OnIdleEventHandler OnIdle;
		public delegate void OnIdleEventHandler ();

		public event OnSyncingEventHandler OnSyncing;
		public delegate void OnSyncingEventHandler ();

		public event OnErrorEventHandler OnError;
		public delegate void OnErrorEventHandler ();

		public event OnInvitationEventHandler OnInvitation;
		public delegate void OnInvitationEventHandler (string server, string folder, string token);

		public event ConflictNotificationRaisedEventHandler ConflictNotificationRaised;
		public delegate void ConflictNotificationRaisedEventHandler ();

		public event NotificationRaisedEventHandler NotificationRaised;
		public delegate void NotificationRaisedEventHandler (SparkleCommit commit, string repository_path);


		public SparkleController ()
		{

			InstallLauncher ();
			EnableSystemAutostart ();

			// Create the SparkleShare folder and add it to the bookmarks
			if (CreateSparkleShareFolder ())
				AddToBookmarks ();

			FolderSize = GetFolderSize ();
			
			
			SparklePath = SparklePaths.SparklePath;
			
			string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			// Show the introduction screen if SparkleShare isn't configured
			if (!File.Exists (global_config_file_path)) {

				FirstRun = true;

			} else {

				FirstRun = false;
				AddKey ();

			}
			
			
			// Watch the SparkleShare folder
			FileSystemWatcher watcher = new FileSystemWatcher (SparklePaths.SparklePath) {
				IncludeSubdirectories = false,
				EnableRaisingEvents   = true,
				Filter                = "*"
			};

			// Remove the repository when a delete event occurs
			watcher.Deleted += delegate (object o, FileSystemEventArgs args) {

				RemoveRepository (args.FullPath);

			};

			// Add the repository when a create event occurs
			watcher.Created += delegate (object o, FileSystemEventArgs args) {
				
				// Handle invitations when the user saves an
				// invitation into the SparkleShare folder
				if (args.Name.EndsWith (".sparkle") && !FirstRun) {

					XmlDocument xml_doc = new XmlDocument (); 
					xml_doc.Load (args.Name);

					string server = xml_doc.GetElementsByTagName ("server") [0].InnerText;
					string folder = xml_doc.GetElementsByTagName ("folder") [0].InnerText;
					string token  = xml_doc.GetElementsByTagName ("token") [0].InnerText;
			
					// TODO: this is broken :\
					if (OnInvitation != null)
						OnInvitation (server, folder, token);

				} else if (Directory.Exists (Path.Combine (args.FullPath, ".git"))) {

					AddRepository (args.FullPath);

				}

			};


			CreateConfigurationFolders ();

			Thread thread = new Thread (
				new ThreadStart (PopulateRepositories)
			);

			thread.Start ();

		}


		// Uploads the user's public key to the server
		public bool AcceptInvitation (string server, string folder, string token)
		{
			
			// The location of the user's public key for SparkleShare
			string public_key_file_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".ssh",
				"sparkleshare." + UserEmail + ".key.pub");

			if (!File.Exists (public_key_file_path))
				return false;

			StreamReader reader = new StreamReader (public_key_file_path);
			string public_key = reader.ReadToEnd ();
			reader.Close ();

			string url = "https://" + server + "/?folder=" + folder +
			             "&token=" + token + "&pubkey=" + public_key;

			SparkleHelpers.DebugInfo ("WebRequest", url);

			HttpWebRequest request   = (HttpWebRequest) WebRequest.Create (url);
			HttpWebResponse response = (HttpWebResponse) request.GetResponse();

			if (response.StatusCode == HttpStatusCode.OK) {
				
				response.Close ();
				return true;
				
			} else {
				
				response.Close ();
				return false;
				
			}
			
		}


		public List <string> Folders
		{
			
			get	{
			
				List <string> folders = new List <string> ();
				
				foreach (SparkleRepo repo in Repositories)
					folders.Add (repo.LocalPath);

				return folders;
				
			}
			
		}
		
		
		public List <SparkleCommit> GetLog (string name)
		{
			
			string path = Path.Combine (SparklePaths.SparklePath, name);
			int log_size = 30;
			
			List <SparkleCommit> list = new List <SparkleCommit> ();
			
			foreach (SparkleRepo repo in Repositories) {
			
				if (repo.LocalPath.Equals (path)) {
				
					foreach (SparkleCommit commit in repo.GetCommits (log_size))
						list.Add (commit);
					
					return list;
					
				}
				
			}
			
			return null;
			
		}
		
		
		public abstract string EventLogHTML { get; }
		public abstract string DayEntryHTML { get; }
		public abstract string EventEntryHTML { get; }
		
		
		public string GetHTMLLog (string name)
		{
		
			List <SparkleCommit> commits     = GetLog (name);
			List <ActivityDay> activity_days = new List <ActivityDay> ();

			foreach (SparkleCommit commit in commits) {

				GetAvatar (commit.UserEmail, 36);

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


			string event_log_html   = EventLogHTML;
			string day_entry_html   = DayEntryHTML;
			string event_entry_html = EventEntryHTML;
			
			
			string event_log = "";

			foreach (ActivityDay activity_day in activity_days) {

				string event_entries = "";

				foreach (SparkleCommit change_set in activity_day) {

					string event_entry = "<dl>";

					if (change_set.Edited.Count > 0) {

						event_entry += "<dt>Edited</dt>";

						foreach (string file_path in change_set.Edited) {
							
							string absolute_file_path = SparkleHelpers.CombineMore (SparklePaths.SparklePath,
								name, file_path);
							
							if (File.Exists (absolute_file_path)) {

								event_entry += "<dd><a href='" + absolute_file_path + "'>" + file_path + "</a></dd>";

							} else {

								event_entry += "<dd>" + file_path + "</dd>";

							}

						}

					}


					if (change_set.Added.Count > 0) {

						event_entry += "<dt>Added</dt>";

						foreach (string file_path in change_set.Added) {
							string absolute_file_path = SparkleHelpers.CombineMore (SparklePaths.SparklePath,
								name, file_path);
							
							if (File.Exists (absolute_file_path)) {

								event_entry += "<dd><a href='" + absolute_file_path + "'>" + file_path + "</a></dd>";
								
							} else {

								event_entry += "<dd>" + file_path + "</dd>";

							}

						}

					}

					if (change_set.Deleted.Count > 0) {

						event_entry += "<dt>Deleted</dt>";

						foreach (string file_path in change_set.Deleted) {

							string absolute_file_path = SparkleHelpers.CombineMore (SparklePaths.SparklePath,
								name, file_path);
							
							if (File.Exists (absolute_file_path)) {

								event_entry += "<dd><a href='" + absolute_file_path + "'>" + file_path + "</a></dd>";
								
							} else {

								event_entry += "<dd>" + file_path + "</dd>";

							}

						}

					}

					event_entry += "</dl>";
					
					event_entries += event_entry_html.Replace ("<!-- $event-entry-content -->", event_entry)
						.Replace ("<!-- $event-user-name -->", change_set.UserName)
						.Replace ("<!-- $event-avatar-url -->", "file://" + GetAvatar (change_set.UserEmail, 36) )
						.Replace ("<!-- $event-time -->", change_set.DateTime.ToString ("H:mm"));
					
				}
				
				
				string day_entry = "";

				DateTime today     = DateTime.Now;
				DateTime yesterday = DateTime.Now.AddDays (-1);

				if (today.Day   == activity_day.DateTime.Day &&
				    today.Month == activity_day.DateTime.Month && 
				    today.Year  == activity_day.DateTime.Year) {

					day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->", "<b>Today</b>");

				} else if (yesterday.Day   == activity_day.DateTime.Day &&
				           yesterday.Month == activity_day.DateTime.Month &&
				           yesterday.Year  == activity_day.DateTime.Year) {

					day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->", "<b>Yesterday</b>");

				} else {

					day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
						"<b>" + activity_day.DateTime.ToString ("ddd MMM d, yyyy") + "</b>");

				}

				event_log += day_entry.Replace ("<!-- $day-entry-content -->", event_entries);


			}
			
			
			string html = event_log_html.Replace ("<!-- $event-log-content -->", event_log);

			return html;
			
		}
		
		
		// Creates a folder in the user's home folder to store configuration
		private void CreateConfigurationFolders ()
		{

			if (!Directory.Exists (SparklePaths.SparkleTmpPath))
				Directory.CreateDirectory (SparklePaths.SparkleTmpPath);

			string config_path     = SparklePaths.SparkleConfigPath;
			string local_icon_path = SparklePaths.SparkleLocalIconPath;

			if (!Directory.Exists (config_path)) {

				// Create a folder to store settings
				Directory.CreateDirectory (config_path);
				SparkleHelpers.DebugInfo ("Config", "Created '" + config_path + "'");

				// Create a folder to store the avatars
				Directory.CreateDirectory (local_icon_path);
				SparkleHelpers.DebugInfo ("Config", "Created '" + local_icon_path + "'");

				string notify_setting_file = SparkleHelpers.CombineMore (config_path, "sparkleshare.notify");

				// Enable notifications by default				
				if (!File.Exists (notify_setting_file))
					File.Create (notify_setting_file);

			}

		}


		// Creates a .desktop entry in autostart folder to
		// start SparkleShare automatically at login
		public abstract void EnableSystemAutostart ();

		// Installs a launcher so the user can launch SparkleShare
		// from the Internet category if needed
		public abstract void InstallLauncher ();

		// Adds the SparkleShare folder to the user's
		// list of bookmarked places
		public abstract void AddToBookmarks ();

		// Creates the SparkleShare folder in the user's home folder
		public abstract bool CreateSparkleShareFolder ();

		// Opens the SparkleShare folder or an (optional) subfolder
		public abstract void OpenSparkleShareFolder (string subfolder);


		// Fires events for the current syncing state
		private void UpdateState ()
		{

			foreach (SparkleRepo repo in Repositories) {

				if (repo.IsSyncing || repo.IsBuffering) {

					if (OnSyncing != null)
						OnSyncing ();

					return;

				} else if (repo.HasUnsyncedChanges) {
	
					if (OnError != null)
						OnError ();

					return;

				}
	
			}


			if (OnIdle != null)
				OnIdle ();


			FolderSize = GetFolderSize ();

			if (FolderSizeChanged != null)
				FolderSizeChanged (FolderSize);

		}


		// Adds a repository to the list of repositories
		private void AddRepository (string folder_path)
		{
		
			// Check if the folder is a Git repository
			if (!Directory.Exists (SparkleHelpers.CombineMore (folder_path, ".git")))
				return;

			SparkleRepo repo = new SparkleRepo (folder_path);

			repo.NewCommit += delegate (SparkleCommit commit, string repository_path) {

				if (NotificationsEnabled && NotificationRaised != null)
					NotificationRaised (commit, repository_path);

			};

			repo.FetchingStarted += delegate {
				UpdateState ();
			};

			repo.FetchingFinished += delegate {
				UpdateState ();
			};

			repo.FetchingFailed += delegate {
				UpdateState ();
			};

			repo.ChangesDetected += delegate {
				UpdateState ();
			};

			repo.PushingStarted += delegate {
				UpdateState ();
			};

			repo.PushingFinished += delegate {
				UpdateState ();
			};

			repo.CommitEndedUpEmpty += delegate {
				UpdateState ();
			};

			repo.PushingFailed += delegate {
				UpdateState ();
			};

			repo.ConflictDetected += delegate {
				if (ConflictNotificationRaised != null)
					ConflictNotificationRaised ();
			};

			Repositories.Add (repo);


			if (FolderListChanged != null)
				FolderListChanged ();

			
			FolderSize = GetFolderSize ();

			if (FolderSizeChanged != null)
				FolderSizeChanged (FolderSize);

		}


		// Removes a repository from the list of repositories and
		// updates the statusicon menu
		private void RemoveRepository (string folder_path)
		{

			string folder_name = Path.GetFileName (folder_path);

			for (int i = 0; i < Repositories.Count; i++) {

				SparkleRepo repo = Repositories [i];

				if (repo.Name.Equals (folder_name)) {

					Repositories.Remove (repo);
					repo.Dispose ();
					repo = null;
					break;

				}

			}


			if (FolderListChanged != null)
				FolderListChanged ();

			
			FolderSize = GetFolderSize ();

			if (FolderSizeChanged != null)
				FolderSizeChanged (FolderSize);

		}


		// Updates the list of repositories with all the
		// folders in the SparkleShare folder
		private void PopulateRepositories ()
		{

			Repositories = new List <SparkleRepo> ();

			foreach (string folder_path in Directory.GetDirectories (SparklePaths.SparklePath))
				AddRepository (folder_path);

			if (FolderListChanged != null)
				FolderListChanged ();

		}


		public bool NotificationsEnabled {

			get {

				string notify_setting_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
					"sparkleshare.notify");

				return File.Exists (notify_setting_file_path);

			}

		} 


		public void ToggleNotifications () {
		
			string notify_setting_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath,
				"sparkleshare.notify");
					                                 
			if (File.Exists (notify_setting_file_path))
				File.Delete (notify_setting_file_path);
			else
				File.Create (notify_setting_file_path);

		}


		private string GetFolderSize ()
		{

			double folder_size = CalculateFolderSize (new DirectoryInfo (SparklePaths.SparklePath));
			return FormatFolderSize (folder_size);

		}


		// Recursively gets a folder's size in bytes
		private double CalculateFolderSize (DirectoryInfo parent)
		{

			if (!Directory.Exists (parent.ToString ()))
				return 0;

			double size = 0;

			// Ignore the temporary 'rebase-apply' and '.tmp' directories. This prevents potential
			// crashes when files are being queried whilst the files have already been deleted.
			if (parent.Name.Equals ("rebase-apply") ||
			    parent.Name.Equals (".tmp"))
				return 0;

			foreach (FileInfo file in parent.GetFiles()) {

				if (!file.Exists)
					return 0;

				size += file.Length;

			}

			foreach (DirectoryInfo directory in parent.GetDirectories())
				size += CalculateFolderSize (directory);

		    return size;
    
		}


		// Format a file size nicely with small caps.
		// Example: 1048576 becomes "1 ᴍʙ"
        private string FormatFolderSize (double byte_count)
        {

			if (byte_count >= 1099511627776)

				return String.Format ("{0:##.##}  ᴛʙ", Math.Round (byte_count / 1099511627776, 1));

			else if (byte_count >= 1073741824)

				return String.Format ("{0:##.##} ɢʙ", Math.Round (byte_count / 1073741824, 1));

            else if (byte_count >= 1048576)

				return String.Format ("{0:##.##} ᴍʙ", Math.Round (byte_count / 1048576, 1));

			else if (byte_count >= 1024)

				return String.Format ("{0:##.##} ᴋʙ", Math.Round (byte_count / 1024, 1));

			else

				return byte_count.ToString () + " bytes";

        }

		public void OpenSparkleShareFolder ()
		{
		
			OpenSparkleShareFolder ("");
			
		}

		
		// Adds the user's SparkleShare key to the ssh-agent,
		// so all activity is done with this key
		public void AddKey ()
		{

			string keys_path = SparklePaths.SparkleKeysPath;
			string key_file_name = "sparkleshare." + UserEmail + ".key";

			Process process = new Process ();
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.FileName               = "ssh-add";
			process.StartInfo.Arguments              = Path.Combine (keys_path, key_file_name);
			process.Start ();

		}


		// Looks up the user's name from the global configuration
		public string UserName
		{

			get {

				string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");
	
				if (!File.Exists (global_config_file_path))
				    return "";
				
				StreamReader reader = new StreamReader (global_config_file_path);
				string global_config_file = reader.ReadToEnd ();
				reader.Close ();
				
				Regex regex = new Regex (@"name.+= (.+)");
				Match match = regex.Match (global_config_file);
	
				if (match.Success)
					return match.Groups [1].Value;
				else
					return "";
			
			}

			set {
				
				WriteUserInfo (value, UserEmail);
					
			}

		}


		// Looks up the user's email from the global configuration
		public string UserEmail
		{
					
			get { 
						
				string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");
	
				// Look in the global config file first
				if (File.Exists (global_config_file_path)) {
	
					StreamReader reader = new StreamReader (global_config_file_path);
					string global_config_file = reader.ReadToEnd ();
					reader.Close ();
					
					Regex regex = new Regex (@"email.+= (.+)");
					Match match = regex.Match (global_config_file);
	
					if (match.Success)
						return match.Groups [1].Value;
					else
						return "";
	
				} else { // Secondly, look at the user's private key file name
	
					string keys_path = SparklePaths.SparkleKeysPath;
	
					if (!Directory.Exists (keys_path))
						return "";
	
					foreach (string file_path in Directory.GetFiles (keys_path)) {
	
						string file_name = System.IO.Path.GetFileName (file_path);
	
						if (file_name.StartsWith ("sparkleshare.") && file_name.EndsWith (".key")) {
									
							Regex regex = new Regex (@"sparkleshare\.(.+)\.key");
							Match match = regex.Match (file_name);
	
							if (match.Success)
								return match.Groups [1].Value;
							else
								return "";
		
						}
	
					}
	
					return "";
					
				}

			}
					
			set {
			
				WriteUserInfo (UserName, value);
						
			}
						
		}
		
		
		private void WriteUserInfo (string user_name, string user_email)
		{
			
			string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			// Write the user's information to a text file
			TextWriter writer = new StreamWriter (global_config_file_path);
			writer.WriteLine ("[user]\n" +
			                  "\tname  = " + user_name + "\n" +
			                  "\temail = " + user_email);
			writer.Close ();

			SparkleHelpers.DebugInfo ("Config", "Updated '" + global_config_file_path + "'");
			
		}


		// Generates and installs an RSA keypair to identify this system
		public void GenerateKeyPair ()
		{

			string keys_path     = SparklePaths.SparkleKeysPath;
			string key_file_name = "sparkleshare." + UserEmail + ".key";
			string key_file_path = Path.Combine (keys_path, key_file_name);


			if (File.Exists (key_file_path)) {

				SparkleHelpers.DebugInfo ("Config", "Key already exists ('" + key_file_name + "'), " +
				                          "leaving it untouched");
				return;

			}

			
			if (!Directory.Exists (keys_path))
				Directory.CreateDirectory (keys_path);

			if (!File.Exists (key_file_name)) {

				Process process = new Process () {
					EnableRaisingEvents = true
				};
				
				process.StartInfo.WorkingDirectory = keys_path;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.FileName = "ssh-keygen";
				
				// -t is the crypto type
				// -P is the password (none)
				// -f is the file name to store the private key in
				process.StartInfo.Arguments = "-t rsa -P \"\" -f " + key_file_name;

				process.Start ();

				process.Exited += delegate {

					SparkleHelpers.DebugInfo ("Config", "Created private key '" + key_file_name + "'");
					SparkleHelpers.DebugInfo ("Config", "Created public key  '" + key_file_name + ".pub'");

				};

			}

		}

		
		private void DisableHostKeyCheckingForHost (string host)
		{

			string ssh_config_file_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".ssh", "config");
			string ssh_config = "Host " + host + "\n\tStrictHostKeyChecking no";

			if (File.Exists (ssh_config_file_path)) {

				TextWriter writer = File.AppendText (ssh_config_file_path);
				writer.WriteLine ("\n" + ssh_config);
				writer.Close ();

			} else {

				TextWriter writer = new StreamWriter (ssh_config_file_path);
				writer.WriteLine (ssh_config);
				writer.Close ();

			}

		}
		

		private void EnableHostKeyCheckingForHost (string host)
		{

			string ssh_config_file_path = SparkleHelpers.CombineMore
				(SparklePaths.HomePath, ".ssh", "config");
			
			string ssh_config = "Host " + host + "\n" +
			                    "\tStrictHostKeyChecking no";

			if (File.Exists (ssh_config_file_path)) {

				StreamReader reader = new StreamReader (ssh_config_file_path);
				string current_ssh_config = reader.ReadToEnd ();
				reader.Close ();
  				
   				current_ssh_config = current_ssh_config.Remove (current_ssh_config.IndexOf (ssh_config),
   					ssh_config.Length);

				if (current_ssh_config.Trim ().Equals ("")) {
	
					File.Delete (ssh_config_file_path);
				
				} else {
	
					TextWriter writer = new StreamWriter (ssh_config_file_path);
					writer.WriteLine (current_ssh_config);
					writer.Close ();
					
				}

			}

		}


		public void FetchFolder (string url, string name)
		{

			SparkleHelpers.DebugInfo ("Controller", "Formed URL: " + url);


			string host = url.Substring (url.IndexOf ("@") + 1);

			if (host.Contains (":"))
				host = host.Substring (0, host.IndexOf (":"));
			else
				host = host.Substring (0, host.IndexOf ("/"));


			DisableHostKeyCheckingForHost (host);
			

			// Strip the '.git' from the name
			string canonical_name = System.IO.Path.GetFileNameWithoutExtension (name);
			string tmp_folder = SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, canonical_name);

			SparkleFetcher fetcher = new SparkleFetcher (url, tmp_folder);


			bool folder_exists = Directory.Exists (
				SparkleHelpers.CombineMore (SparklePaths.SparklePath, canonical_name));

			// Add a numbered suffix to the nameif a folder with the same name
			// already exists. Example: "Folder (2)"
			int i = 1;
			while (folder_exists) {

				i++;
				folder_exists = Directory.Exists (
					SparkleHelpers.CombineMore (SparklePaths.SparklePath, canonical_name + " (" + i + ")"));

			}

			string target_folder_name = canonical_name;

			if (i > 1)
				target_folder_name += " (" + i + ")";


			fetcher.CloningFinished += delegate {

				EnableHostKeyCheckingForHost (host);

				SparkleHelpers.ClearAttributes (tmp_folder);

				try {

					string target_folder_path = SparkleHelpers.CombineMore (SparklePaths.SparklePath,
						target_folder_name);

					Directory.Move (tmp_folder, target_folder_path);

				} catch (Exception e) {

					SparkleHelpers.DebugInfo ("Controller", "Error moving folder: " + e.Message);
 
				}

				
				if (FolderFetched != null)
					FolderFetched ();
				
				if (FolderListChanged != null)
					FolderListChanged ();		

			};


			fetcher.CloningFailed += delegate {

				EnableHostKeyCheckingForHost (host);

				if (Directory.Exists (tmp_folder)) {

					SparkleHelpers.ClearAttributes (tmp_folder);
					Directory.Delete (tmp_folder, true);

					SparkleHelpers.DebugInfo ("Config",
						"Deleted temporary directory: " + tmp_folder);

				}


				if (FolderFetchError != null)
					FolderFetchError ();

			};


			fetcher.Start ();

		}
		
		
		// Gets the avatar for a specific email address and size
		public string GetAvatar (string email, int size)
		{

			string avatar_path = SparkleHelpers.CombineMore (SparklePaths.SparkleLocalIconPath,
				size + "x" + size, "status");

			if (!Directory.Exists (avatar_path)) {

				Directory.CreateDirectory (avatar_path);
				SparkleHelpers.DebugInfo ("Config", "Created '" + avatar_path + "'");

			}

			string avatar_file_path = SparkleHelpers.CombineMore (avatar_path, "avatar-" + email);

			if (File.Exists (avatar_file_path)) {

				return avatar_file_path;

			} else {

				// Let's try to get the person's gravatar for next time
				WebClient web_client = new WebClient ();
				Uri uri = new Uri ("http://www.gravatar.com/avatar/" + GetMD5 (email) +
					".jpg?s=" + size + "&d=404");

				string tmp_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, email + size);

				if (!File.Exists (tmp_file_path)) {

					web_client.DownloadFileAsync (uri, tmp_file_path);

					web_client.DownloadFileCompleted += delegate {

						if (File.Exists (avatar_file_path))
							File.Delete (avatar_file_path);

						FileInfo tmp_file_info = new FileInfo (tmp_file_path);

						if (tmp_file_info.Length > 255)
							File.Move (tmp_file_path, avatar_file_path);
						
						if (AvatarFetched != null)
							AvatarFetched ();

					};

				}

				// Fall back to a generic icon if there is no gravatar
				if (File.Exists (avatar_file_path))
					return avatar_file_path;
				else
					return null;

			}

		}
		
		
		// Creates an MD5 hash of input
		public static string GetMD5 (string s)
		{
			MD5 md5 = new MD5CryptoServiceProvider ();
			Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
			Byte[] encodedBytes = md5.ComputeHash (bytes);
			return BitConverter.ToString (encodedBytes).ToLower ().Replace ("-", "");
		}
		

		// Checks whether there are any folders syncing and
		// quits if safe
		public void TryQuit ()
		{

			foreach (SparkleRepo repo in Repositories) {	

				if (repo.IsSyncing) {
				
					if (OnQuitWhileSyncing != null)
						OnQuitWhileSyncing ();
					
					return;
	
				}
			
			}
			
			Quit ();
			
		}


		public void Quit ()
		{

			foreach (SparkleRepo repo in Repositories)
				repo.Dispose ();

			string pid_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, "sparkleshare.pid");
			
			// Remove the process ID file
			if (File.Exists (pid_file_path))
				File.Delete (pid_file_path);

			Environment.Exit (0);

		}


		// Checks to see if an email address is valid
		public bool IsValidEmail (string email)
		{

			Regex regex = new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.IgnoreCase);
			return regex.IsMatch (email);

		}

	
	}


	public class ChangeSet : SparkleCommit {

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