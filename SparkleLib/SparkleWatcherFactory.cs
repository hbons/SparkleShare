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

namespace SparkleLib {

    public static class SparkleWatcherFactory {

        private static List<SparkleWatcher> watchers = new List<SparkleWatcher> ();


        public static SparkleWatcher CreateWatcher (SparkleRepoBase repo_to_watch)
        {
            foreach (SparkleWatcher watcher in watchers) {
                foreach (SparkleRepoBase repo in watcher.ReposToNotify) {
                    string path_to_watch = Path.GetDirectoryName (repo_to_watch.LocalPath);

                    if (watcher.Path.Equals (path_to_watch)) {
                        watcher.ReposToNotify.Add (repo_to_watch);
                        SparkleHelpers.DebugInfo ("WatcherFactory", "Refered to existing watcher for " + path_to_watch);

                        return watcher;
                    }
                }
            }

            SparkleWatcher new_watcher = new SparkleWatcher (repo_to_watch);
            watchers.Add (new_watcher);

            SparkleHelpers.DebugInfo ("WatcherFactory", "Issued new watcher for " + repo_to_watch.Name);

            return watchers [watchers.Count - 1];
        }


        public static void TriggerWatcherManually (FileSystemEventArgs args)
        {
            foreach (SparkleWatcher watcher in watchers) {
                if (args.FullPath.StartsWith (watcher.Path)) {
                    watcher.Notify (null, args);
                    return;
                }
            }
        }


        public static void DisposeWatcher (SparkleRepoBase repo)
        {
            foreach (SparkleWatcher watcher in watchers) {
                if (watcher.ReposToNotify.Contains (repo)) {
                    watcher.ReposToNotify.Remove (repo);
                    return;
                }
            }
        }
    }
}
