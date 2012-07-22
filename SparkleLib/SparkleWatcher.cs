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
using System.Threading;

using IO = System.IO;

namespace SparkleLib {

    public class SparkleWatcher : IO.FileSystemWatcher {

        public List<SparkleRepoBase> ReposToNotify = new List<SparkleRepoBase> ();


        public SparkleWatcher (SparkleRepoBase repo)
        {
            ReposToNotify.Add (repo);

            Changed += Notify;
            Created += Notify;
            Deleted += Notify;
            Renamed += Notify;

            Filter = "*";
            Path   = IO.Path.GetDirectoryName (repo.LocalPath);

            IncludeSubdirectories = true;
            EnableRaisingEvents   = true;
        }


        public void Notify (object sender, IO.FileSystemEventArgs args)
        {
            char separator       = IO.Path.DirectorySeparatorChar;
            string relative_path = args.FullPath.Substring (Path.Length);
            relative_path        = relative_path.Trim (new char [] {' ', separator});

            // Ignore changes that happened in the parent path
            if (!relative_path.Contains (separator.ToString ()))
                return;

            string repo_name = relative_path.Substring (0, relative_path.IndexOf (separator));

            foreach (SparkleRepoBase repo in ReposToNotify) {
                if (repo.Name.Equals (repo_name) && !repo.IsBuffering &&
                    (repo.Status != SyncStatus.SyncUp && repo.Status != SyncStatus.SyncDown)) {

                    new Thread (() => repo.OnFileActivity (args)).Start ();
                }
            }
        }
    }
}
