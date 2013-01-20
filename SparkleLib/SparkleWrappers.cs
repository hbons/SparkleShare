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
        public string Revision;
        public DateTime Timestamp;
        public DateTime FirstTimestamp;
        public Uri RemoteUrl;

        public List<SparkleChange> Changes = new List<SparkleChange> ();

        public string ToMessage ()
        {
            string message = "added ‘{0}’";
            
            switch (Changes [0].Type) {
            case SparkleChangeType.Edited:  message = "edited ‘{0}’"; break;
            case SparkleChangeType.Deleted: message = "deleted ‘{0}’"; break;
            case SparkleChangeType.Moved:   message = "moved ‘{0}’"; break;
            }

            if (Changes.Count > 0)
                return string.Format (message, Changes [0].Path);
            else
                return "did something magical";
        }
    }


    public class SparkleChange {

        public SparkleChangeType Type;
        public DateTime Timestamp;
        public bool IsFolder = false;
        
        public string Path;
        public string MovedToPath;
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


    public class SparkleAnnouncement {

        public readonly string FolderIdentifier;
        public readonly string Message;


        public SparkleAnnouncement (string folder_identifier, string message)
        {
            FolderIdentifier = folder_identifier;
            Message          = message;
        }
    }
}
