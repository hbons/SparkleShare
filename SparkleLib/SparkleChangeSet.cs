//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as 
//   published by the Free Software Foundation, either version 3 of the 
//   License, or (at your option) any later version.
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
using System.Collections.Generic;

namespace SparkleLib {

    public enum SparkleChangeType {
        Added,
        Edited,
        Deleted,
        Moved
    }


    public class SparkleChangeSet {

        public SparkleUser User = new SparkleUser ("Unknown", "Unknown");

        public SparkleFolder Folder;
        public Uri RemoteUrl;

        public string Revision;
        public DateTime Timestamp;
        public DateTime FirstTimestamp;
        public List<SparkleChange> Changes = new List<SparkleChange> ();
    }


    public class SparkleChange {

        public SparkleChangeType Type;
        public string Path;
        public string MovedToPath;
        public DateTime Timestamp;
    }


    public class SparkleFolder {

        public string Name;
        public Uri RemoteAddress;

        public string FullPath {
            get {
                string custom_path = SparkleConfig.DefaultConfig.GetFolderOptionalAttribute (Name, "path");

                if (custom_path != null)
                    return Path.Combine (custom_path, Name);
                else
                    return Path.Combine (SparkleConfig.DefaultConfig.FoldersPath, Name);
            }
        }


        public SparkleFolder (string name)
        {
            Name = name;
        }
    }
}
