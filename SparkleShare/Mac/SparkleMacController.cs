//   SparkleShare, a collaboration and sharing tool.
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
using System.Diagnostics;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using SparkleLib;

namespace SparkleShare {

	public class SparkleMacController : SparkleController {

        // We have to use our own custom made folder watcher, as
        // System.IO.FileSystemWatcher fails watching subfolders on Mac
        private SparkleMacWatcher watcher = new SparkleMacWatcher (SparklePaths.SparklePath);

        public SparkleMacController () : base () { }

        public override void Initialize ()
        {
            base.Initialize ();

            this.watcher.Changed += delegate (string path) {
                string repo_name;

                if (path.Contains ("/"))
                    repo_name = path.Substring (0, path.IndexOf ("/"));
                else
                    repo_name = path;

                // Ignore changes in the root of each subfolder, these
                // are already handled bu the repository
                if (Path.GetFileNameWithoutExtension (path).Equals (repo_name))
                    return;

                repo_name = repo_name.Trim ("/".ToCharArray ());
                FileSystemEventArgs args = new FileSystemEventArgs (WatcherChangeTypes.Changed,
                    Path.Combine (SparklePaths.SparklePath, path), Path.GetFileName (path));

                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.Name.Equals (repo_name))
                        repo.OnFileActivity (args);
                }
            };
        }


		public override void EnableSystemAutostart ()
		{
			// N/A
		}


		public override void InstallLauncher ()
		{
			// N/A
		}

		
		// Adds the SparkleShare folder to the user's
		// list of bookmarked places
		public override void AddToBookmarks ()
		{
            // TODO: Waiting for NSMutableArray/Dictionary support
         /* NSDictionary sidebar_plist = NSUserDefaults.StandardUserDefaults.PersistentDomainForName ("com.apple.sidebarlists");

            foreach (object sidebar_item in sidebar_plist.Keys) {
                if (sidebar_item.ToString ().Equals ("useritems")) {
                    NSDictionary user_items = (NSDictionary) sidebar_plist.ValueForKey (new NSString (sidebar_item.ToString ()));

                    foreach (NSObject user_item in user_items.Keys) {
                        if (user_item.ToString ().Equals ("CustomListItems")) {
                            NSArray custom_items = (NSArray) user_items.ValueForKey (new NSString (user_item.ToString ()));

                            NSDictionary new_dictionary = new NSDictionary ();
                            new dictionary.SetValueForKey (new NSString ("Test"),  new NSString ("Name"));

                            custom_items.SetValueForKey (new_dictionary, new NSString ("Item 6"));
                            user_items.SetValueForKey (custom_items, new NSString (user_item.ToString ()));
                        }
                    }

                    sidebar_plist.SetValueForKey (user_items, new NSString (sidebar_item.ToString ()));
                }
            }

            NSUserDefaults.StandardUserDefaults.SetPersistentDomain (sidebar_plist, "com.apple.sidebarlists"); */
		}
		

		// Creates the SparkleShare folder in the user's home folder
		public override bool CreateSparkleShareFolder ()
		{
			if (!Directory.Exists (SparklePaths.SparklePath)) {
				Directory.CreateDirectory (SparklePaths.SparklePath);
				return true;

			} else {
				return false;
			}
		}

		
		// Opens the SparkleShare folder or an (optional) subfolder
		public override void OpenSparkleShareFolder (string subfolder)
		{
			string folder = Path.Combine (SparklePaths.SparklePath, subfolder);
			folder.Replace (" ", "\\ "); // Escape space-characters			
			
			NSWorkspace.SharedWorkspace.OpenFile (folder);
		}
		
		
		public override string EventLogHTML
		{
			get {
				string resource_path = NSBundle.MainBundle.ResourcePath;
				string html_path     = Path.Combine (resource_path, "HTML", "event-log.html");
				
				StreamReader reader = new StreamReader (html_path);
				string html = reader.ReadToEnd ();
				reader.Close ();

                html = html.Replace ("<!-- $jquery-url -->", "file://" +
                    Path.Combine (NSBundle.MainBundle.ResourcePath, "HTML", "jquery.js"));

                return html;
			}
		}

		
		public override string DayEntryHTML
		{
			get {
				string resource_path = NSBundle.MainBundle.ResourcePath;
				string html_path     = Path.Combine (resource_path, "HTML", "day-entry.html");
				
				StreamReader reader = new StreamReader (html_path);
				string html = reader.ReadToEnd ();
				reader.Close ();
				
				return html;
			}
		}
		
	
		public override string EventEntryHTML
		{
			get {
				string resource_path = NSBundle.MainBundle.ResourcePath;
				string html_path     = Path.Combine (resource_path, "HTML", "event-entry.html");
				
				StreamReader reader = new StreamReader (html_path);
				string html = reader.ReadToEnd ();
				reader.Close ();
				
				return html;
			}
		}


        new public void Quit ()
        {
            this.watcher.Dispose ();
            NSApplication.SharedApplication.Terminate (new NSObject ());
            base.Quit ();
        }
	}
}
