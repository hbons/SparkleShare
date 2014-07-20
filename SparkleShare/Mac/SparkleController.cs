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
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;

using Mono.Unix.Native;
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
            NSApplication.Init ();

            // Let's use the bundled git first
            SparkleLib.Git.SparkleGit.GitPath  = Path.Combine (NSBundle.MainBundle.ResourcePath, "git", "libexec", "git-core", "git");
            SparkleLib.Git.SparkleGit.ExecPath = Path.Combine (NSBundle.MainBundle.ResourcePath, "git", "libexec", "git-core");
        }

        
        public override void Initialize ()
        {
            base.Initialize ();

            SparkleRepoBase.UseCustomWatcher = true;
            this.watcher = new SparkleMacWatcher (Program.Controller.FoldersPath);

            this.watcher.Changed += delegate (string path) {
                FileSystemEventArgs fse_args = new FileSystemEventArgs (WatcherChangeTypes.Changed, path, "Unknown_File");
                FileActivityTask [] tasks = new FileActivityTask [Repositories.Length];

                // FIXME: There are cases where the wrong repo is triggered, so
                // we trigger all of them for now. Causes only slightly more overhead
                int i = 0;
                foreach (SparkleRepoBase repo in Repositories) {
                    tasks [i] = MacActivityTask (repo, fse_args);
                    tasks [i] ();
                    i++;
                }
            };

        }


        private delegate void FileActivityTask ();

        private FileActivityTask MacActivityTask (SparkleRepoBase repo, FileSystemEventArgs fse_args) {
            return delegate { new Thread (() => { repo.OnFileActivity (fse_args); }).Start (); };
        }


        public override void CreateStartupItem ()
        {
            // There aren't any bindings in MonoMac to support this yet, so
            // we call out to an applescript to do the job
            Process process = new Process ();
            process.StartInfo.FileName        = "osascript";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments       = "-e 'tell application \"System Events\" to " +
                "make login item at end with properties {path:\"" + NSBundle.MainBundle.BundlePath + "\", hidden:false}'";

            process.Start ();
            process.WaitForExit ();

            SparkleLogger.LogInfo ("Controller", "Added " + NSBundle.MainBundle.BundlePath + " to login items");
        }


        public override void InstallProtocolHandler ()
        {
             // We ship SparkleShareInviteHandler.app in the bundle
        }


        public override void AddToBookmarks ()
        {
            // TODO
        }


        public override bool CreateSparkleShareFolder ()
        {
            if (!Directory.Exists (Program.Controller.FoldersPath)) {
                Directory.CreateDirectory (Program.Controller.FoldersPath);

                NSWorkspace.SharedWorkspace.SetIconforFile (NSImage.ImageNamed ("sparkleshare-folder.icns"),
                    Program.Controller.FoldersPath, 0);

                Syscall.chmod (Program.Controller.FoldersPath, (FilePermissions) 448); // 448 -> 700

                return true;
            }

            return false;
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


        public override void CopyToClipboard (string text)
        {
            NSPasteboard.GeneralPasteboard.ClearContents ();
            NSPasteboard.GeneralPasteboard.SetStringForType (text, "NSStringPboardType");
        }


        private string event_log_html;
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


        private string day_entry_html;
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
        

        private string event_entry_html;
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


        public delegate void Code ();
        private NSObject obj = new NSObject ();

        public void Invoke (Code code)
        {
            using (var a = new NSAutoreleasePool ())
            {
                obj.InvokeOnMainThread (() => code ());
            }
        }
    }
}
