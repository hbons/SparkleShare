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

	public class SparkleController : SparkleControllerBase {

        public override string PluginsPath {
            get {
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Plugins");
            }
        }

        // We have to use our own custom made folder watcher, as
        // System.IO.FileSystemWatcher fails watching subfolders on Mac
        private SparkleMacWatcher watcher;

        
        public SparkleController () : base ()
        {
            using (var a = new NSAutoreleasePool ())
            {
                string content_path = Directory.GetParent (System.AppDomain.CurrentDomain.BaseDirectory).ToString ();
    
                string app_path   = Directory.GetParent (content_path).ToString ();
                string growl_path = Path.Combine (app_path, "Frameworks", "Growl.framework", "Growl");
    
                // Needed for Growl
                Dlfcn.dlopen (growl_path, 0);
                NSApplication.Init ();
            }


            // Let's use the bundled git first
            SparkleLib.Git.SparkleGit.GitPath =
                Path.Combine (NSBundle.MainBundle.ResourcePath, "git", "libexec", "git-core", "git");

            SparkleLib.Git.SparkleGit.ExecPath =
                Path.Combine (NSBundle.MainBundle.ResourcePath, "git", "libexec", "git-core");
        }


        public override void Initialize ()
        {
            base.Initialize ();

            this.watcher.Changed += delegate (string path) {
                string full_path = Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, path + "/something");

                FileSystemEventArgs event_args = new FileSystemEventArgs (WatcherChangeTypes.Changed,
                    Path.GetDirectoryName (full_path), Path.GetFileName (full_path));

                SparkleWatcherFactory.TriggerWatcherManually (event_args);
            };
        }


		public override void CreateStartupItem ()
		{
            // There aren't any bindings in MonoMac to support this yet, so
            // we call out to an applescript to do the job
            Process process = new Process ();
            process.EnableRaisingEvents              = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.FileName               = "osascript";
            process.StartInfo.CreateNoWindow         = true;

            string app_path = Path.GetDirectoryName (NSBundle.MainBundle.ResourcePath);
            app_path        = Path.GetDirectoryName (app_path);

            process.StartInfo.Arguments = "-e 'tell application \"System Events\" to " +
                "make login item at end with properties {path:\"" + app_path + "\", hidden:false}'";

            process.Exited += delegate {
                SparkleHelpers.DebugInfo ("Controller", "Added " + app_path + " to login items");
            };

            try {
                process.Start ();

            } catch (Exception e) {
                SparkleHelpers.DebugInfo ("Controller", "Failed adding " + app_path + " to login items: " + e.Message);
            }
		}


        public override void InstallProtocolHandler ()
        {
             // We ship SparkleShareInviteHandler.app in the bundle
        }


		// Adds the SparkleShare folder to the user's
		// list of bookmarked places
		public override void AddToBookmarks ()
        {/*
            NSMutableDictionary sidebar_plist = NSMutableDictionary.FromDictionary (
                NSUserDefaults.StandardUserDefaults.PersistentDomainForName ("com.apple.sidebarlists"));

            // Go through the sidebar categories
            foreach (NSString sidebar_category in sidebar_plist.Keys) {

                // Find the favorites
                if (sidebar_category.ToString ().Equals ("favorites")) {

                    // Get the favorites
                    NSMutableDictionary favorites = NSMutableDictionary.FromDictionary(
                        (NSDictionary) sidebar_plist.ValueForKey (sidebar_category));

                    // Go through the favorites
                    foreach (NSString favorite in favorites.Keys) {

                        // Find the custom favorites
                        if (favorite.ToString ().Equals ("VolumesList")) {

                            // Get the custom favorites
                            NSMutableArray custom_favorites = (NSMutableArray) favorites.ValueForKey (favorite);

                            NSMutableDictionary properties = new NSMutableDictionary ();
                            properties.SetValueForKey (new NSString ("1935819892"), new NSString ("com.apple.LSSharedFileList.TemplateSystemSelector"));

                            NSMutableDictionary new_favorite = new NSMutableDictionary ();
                            new_favorite.SetValueForKey (new NSString ("SparkleShare"),  new NSString ("Name"));

                            new_favorite.SetValueForKey (NSData.FromString ("ImgR SYSL fldr"),  new NSString ("Icon"));

                            new_favorite.SetValueForKey (NSData.FromString (SparkleConfig.DefaultConfig.FoldersPath),
                                new NSString ("Alias"));

                            new_favorite.SetValueForKey (properties, new NSString ("CustomItemProperties"));

                            // Add to the favorites
                            custom_favorites.Add (new_favorite);
                            favorites.SetValueForKey ((NSArray) custom_favorites, new NSString (favorite.ToString ()));
                            sidebar_plist.SetValueForKey (favorites, new NSString (sidebar_category.ToString ()));
                        }
                    }

                }
            }

            NSUserDefaults.StandardUserDefaults.SetPersistentDomain (sidebar_plist, "com.apple.sidebarlists");*/
		}


		public override bool CreateSparkleShareFolder ()
		{
            this.watcher = new SparkleMacWatcher (Program.Controller.FoldersPath);

            if (!Directory.Exists (Program.Controller.FoldersPath)) {
                Directory.CreateDirectory (Program.Controller.FoldersPath);
                return true;

            } else {
                return false;
            }
		}


		public override void OpenFolder (string path)
		{
			NSWorkspace.SharedWorkspace.OpenFile (path);
		}
		

        public override void OpenFile (string path)
        {
            path = Uri.UnescapeDataString (path);
            NSWorkspace.SharedWorkspace.OpenFile (path);
        }


		public override string EventLogHTML
		{
			get {
                using (var a = new NSAutoreleasePool ()) {
    				string resource_path = NSBundle.MainBundle.ResourcePath;
    				string html_path     = Path.Combine (resource_path, "HTML", "event-log.html");
    				string html          = File.ReadAllText (html_path);
    
                    string jquery_file_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                        "HTML", "jquery.js");
    
                    string jquery = File.ReadAllText (jquery_file_path);
                    html          = html.Replace ("<!-- $jquery -->", jquery);
    
                    return html;
                }
			}
		}

		
		public override string DayEntryHTML
		{
			get {
                using (var a = new NSAutoreleasePool ())
                {
    				string resource_path = NSBundle.MainBundle.ResourcePath;
    				string html_path     = Path.Combine (resource_path, "HTML", "day-entry.html");
    				
    				StreamReader reader = new StreamReader (html_path);
    				string html = reader.ReadToEnd ();
    				reader.Close ();
    				
    				return html;
                }
			}
		}
		
	
		public override string EventEntryHTML
		{
			get {
                using (var a = new NSAutoreleasePool ()) {
    				string resource_path = NSBundle.MainBundle.ResourcePath;
    				string html_path     = Path.Combine (resource_path, "HTML", "event-entry.html");
    				
    				StreamReader reader = new StreamReader (html_path);
    				string html = reader.ReadToEnd ();
    				reader.Close ();
    				
    				return html;
                }
			}
		}
	}
}
