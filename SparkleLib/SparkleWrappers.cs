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
    }


    public class SparkleChange {

        public SparkleChangeType Type;
		public DateTime Timestamp;
		
        public string Path;
        public string MovedToPath;
    }


    public class SparkleFolder : IComparable<SparkleFolder> {

        public string Name { get; private set; }
        public Uri RemoteAddress;

        private string full_name = null;

        public string FullPath {
            get {
                if (full_name == null) {
                    string custom_path = SparkleConfig.DefaultConfig.GetFolderOptionalAttribute(Name, "path");

                    if (custom_path != null)
                        full_name = custom_path;
                    else
                        full_name = Path.Combine(SparkleConfig.DefaultConfig.FoldersPath, Name);
                }

                return full_name;
            }
        }


        public SparkleFolder (string name)
        {
            Name = name;
        }
        
        public int CompareTo (SparkleFolder other) {
            return Name.CompareTo (other.Name);
        }

        public bool Equals (SparkleFolder other) {
            if (ReferenceEquals (null, other)) return false;
            if (ReferenceEquals (this, other)) return true;
            return Equals (other.Name, this.Name) && Equals (other.full_name, this.full_name);
        }

        public override bool Equals (object obj) {
            if (ReferenceEquals (null, obj)) return false;
            if (ReferenceEquals (this, obj)) return true;
            if (obj.GetType () != typeof(SparkleFolder)) return false;
            return Equals ((SparkleFolder)obj);
        }

        public override int GetHashCode () {
            unchecked {
                return ((this.Name != null ? this.Name.GetHashCode () : 0) * 397) ^ (this.full_name != null ? this.full_name.GetHashCode () : 0);
            }
        }
    }
}
