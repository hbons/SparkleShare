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
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Mono.Unix.Native;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Sparkles;
using Sparkles.Git;
using System.Linq;

namespace SparkleShare {

    public class Controller : BaseController {

        public override string PresetsPath {
            get {
                return Path.Combine (NSBundle.MainBundle.ResourcePath, "Presets");
            }
        }

        
        public Controller ()
        {
            NSApplication.Init ();

            GitCommand.GitPath  = Path.Combine (NSBundle.MainBundle.ResourcePath, "git", "libexec", "git-core", "git");
            GitCommand.ExecPath = Path.Combine (NSBundle.MainBundle.ResourcePath, "git", "libexec", "git-core");

            File.Copy (Path.Combine (GitCommand.ExecPath, "git-lfs"), Path.Combine (Config.BinPath, "git-lfs"), true);
        }

        
        public override void Initialize ()
        {
            base.Initialize ();

            BaseRepository.UseCustomWatcher = true;

            this.watcher = new SparkleMacWatcher (SparkleShare.Controller.FoldersPath);
            this.watcher.Changed += OnFilesChanged;
        }


        public override bool CreateSparkleShareFolder ()
        {
            if (!Directory.Exists (SparkleShare.Controller.FoldersPath)) {
                Directory.CreateDirectory (SparkleShare.Controller.FoldersPath);
                Syscall.chmod (SparkleShare.Controller.FoldersPath, (FilePermissions) 448); // 448 -> 700

                return true;
            }

            return false;
        }


        public override void SetFolderIcon ()
        {
            if (Environment.OSVersion.Version.Major >= 14) {
                NSWorkspace.SharedWorkspace.SetIconforFile (
                    NSImage.ImageNamed ("sparkleshare-folder-yosemite.icns"),
                    SparkleShare.Controller.FoldersPath, 0);

            } else {
                NSWorkspace.SharedWorkspace.SetIconforFile (
                    NSImage.ImageNamed ("sparkleshare-folder.icns"),
                    SparkleShare.Controller.FoldersPath, 0);
            }
        }


		public override void CreateStartupItem ()
		{
			// There aren't any bindings in MonoMac to support this yet, so
			// we call out to an applescript to do the job
			
			string args = "-e 'tell application \"System Events\" to " +
				"make login item at end with properties " +
				"{path:\"" + NSBundle.MainBundle.BundlePath + "\", hidden:false}'";
			
			var process = new Command ("osascript", args);
			process.StartAndWaitForExit ();
			
			Logger.LogInfo ("Controller", "Added " + NSBundle.MainBundle.BundlePath + " to login items");
		}
		

        public override void InstallProtocolHandler ()
        {
        }


		public override void CopyToClipboard (string text)
		{
			NSPasteboard.GeneralPasteboard.ClearContents ();
			NSPasteboard.GeneralPasteboard.SetStringForType (text, "NSStringPboardType");
		}


        public override void OpenFolder (string path)
        {
            path = Uri.UnescapeDataString (path);
            NSWorkspace.SharedWorkspace.OpenFile (path);
        }
        
        
        public override void OpenFile (string path)
        {
            path = Uri.UnescapeDataString (path);
            NSWorkspace.SharedWorkspace.OpenFile (path);
        }

        
        public override void OpenWebsite (string url)
        {
            NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
        }


        string event_log_html;
        public override string EventLogHTML
        {
            get {
                if (string.IsNullOrEmpty (this.event_log_html)) {
                    string html_file_path   = Path.Combine (NSBundle.MainBundle.ResourcePath, "HTML", "event-log.html");
                    string jquery_file_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "HTML", "jquery.js");
                    string html             = File.ReadAllText (html_file_path);
                    string jquery           = File.ReadAllText (jquery_file_path);
                    this.event_log_html     = html.Replace ("<!-- $jquery -->", jquery);
                }

                return this.event_log_html;
            }
        }


        string day_entry_html;
        public override string DayEntryHTML
        {
            get {
                if (string.IsNullOrEmpty (this.day_entry_html)) {
                    string html_file_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "HTML", "day-entry.html");
                    this.day_entry_html   = File.ReadAllText (html_file_path);
                }

                return this.day_entry_html;
            }
        }
        

        string event_entry_html;
        public override string EventEntryHTML
        {
            get {
               if (string.IsNullOrEmpty (this.event_entry_html)) {
                   string html_file_path = Path.Combine (NSBundle.MainBundle.ResourcePath, "HTML", "event-entry.html");
                   this.event_entry_html = File.ReadAllText (html_file_path);
               }

               return this.event_entry_html;
            }
        }


        // We have to use our own custom made folder watcher, as
        // System.IO.FileSystemWatcher fails watching subfolders on Mac

        SparkleMacWatcher watcher;
        delegate void FileActivityTask ();

        FileActivityTask MacFileActivityTask (BaseRepository repo, FileSystemEventArgs fse_args)
        {
            return delegate { new Thread (() => { repo.OnFileActivity (fse_args); }).Start (); };
        }

        void OnFilesChanged (List<string> changed_files_in_basedir)
        {
            var triggered_repos = new List<string> ();

            foreach (string file_path in changed_files_in_basedir) {
                string [] paths = file_path.Split (Path.DirectorySeparatorChar);

                if (paths.Length < 2)
                    continue;

                BaseRepository repo = GetRepoByName (paths [1]);

                if (repo != null && !triggered_repos.Contains (repo.Name)) {
                    FileActivityTask task = MacFileActivityTask (repo,
                        new FileSystemEventArgs (WatcherChangeTypes.Changed, file_path, "Unknown"));

                    task ();
                    triggered_repos.Add (repo.Name);
                }
            }
        }


        public delegate void Code ();
        readonly NSObject obj = new NSObject ();

        public void Invoke (Code code)
        {
            using (var a = new NSAutoreleasePool ())
            {
                obj.InvokeOnMainThread (() => code ());
            }
        }
    }
}
