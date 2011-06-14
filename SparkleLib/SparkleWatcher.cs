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
using System.IO;

namespace SparkleLib {

    public class SparkleWatcher : FileSystemWatcher {

        public delegate void ChangeEventEventHandler (FileSystemEventArgs args);
        public event ChangeEventEventHandler ChangeEvent;


        public SparkleWatcher (string path) : base (path)
        {
            IncludeSubdirectories = true;
            EnableRaisingEvents   = true;
            Filter                = "*";

            Changed += delegate (object o, FileSystemEventArgs args) {
                if (ChangeEvent != null)
                    ChangeEvent (args);
            };

            Created += delegate (object o, FileSystemEventArgs args) {
                if (ChangeEvent != null)
                    ChangeEvent (args);
            };

            Deleted += delegate (object o, FileSystemEventArgs args) {
                if (ChangeEvent != null)
                    ChangeEvent (args);
            };

            Renamed += delegate (object o, RenamedEventArgs args) {
                if (ChangeEvent != null)
                    ChangeEvent (args);
            };

        }
    }
}
