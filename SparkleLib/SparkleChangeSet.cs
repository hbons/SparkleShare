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

namespace SparkleLib {

    public class SparkleChangeSet {

        public string UserName;
        public string UserEmail;

        public string Folder;
        public string Revision;
        public DateTime Timestamp;
        public bool FolderSupportsNotes = false;
        public bool IsMerge             = false;

        public List<string> Added     = new List<string> ();
        public List<string> Deleted   = new List<string> ();
        public List<string> Edited    = new List<string> ();
        public List<string> MovedFrom = new List<string> ();
        public List<string> MovedTo   = new List<string> ();

        public List<SparkleNote> Notes = new List<SparkleNote> ();
    }


    public class SparkleNote {

        public string UserName;
        public string UserEmail;

        public DateTime Timestamp;
        public string Body;
    }
}
