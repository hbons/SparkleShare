//   A gutted-out interface from sparkleshare to any random executable,
//   designed to make sparkleshare back-ends incredibly easy to develop
//   in any language:
//   Copyright (C) 2012  Shish <shish@shishnet.org>
//
//   Based on the default Git back-end:
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
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace SparkleLib {

    public class SparkleRepoGut : SparkleRepoBase {

        public SparkleRepoGut (string path) : base (path)
        {
        }


        public override string Identifier {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "identifier");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return output;
            }
        }


        public override List<string> ExcludePaths {
            get {
                List<string> rules = new List<string> ();
                rules.Add (".gut");

                return rules;
            }
        }


        public override double Size {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "size");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return double.Parse(output);
            }
        }


        public override double HistorySize {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "history-size");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return double.Parse(output);
            }
        }


        public override string [] UnsyncedFilePaths {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "unsynced-file-paths");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return output.Split ("\n".ToCharArray ());
            }
        }


        public override string CurrentRevision {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "current-revision");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return output;
            }
        }


        public override bool HasRemoteChanges
        {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "has-remote-changes");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return (output.Equals("true"));
            }
        }


        public override bool SyncUp ()
        {
            SparkleGut gut = new SparkleGut (LocalPath, "sync-up");
            gut.StartInfo.RedirectStandardError = true;
            gut.Start ();

            double percentage = 1.0;
            Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);
            Regex speed_regex = new Regex (@"([0-9]+[KMG]?B/s)", RegexOptions.Compiled);
            while (!gut.StandardError.EndOfStream) {
                string line   = gut.StandardError.ReadLine ();
                Match progress_match = progress_regex.Match (line);
                Match speed_match    = speed_regex.Match (line);
                string speed  = "";
                double number = 0.0;

                if (progress_match.Success) {
                    number = double.Parse (progress_match.Groups [1].Value);
                }

                if (speed_match.Success) {
                    speed = speed_match.Groups [1].Value;
                }

                if (number >= percentage) {
                    percentage = number;
                    base.OnProgressChanged (percentage, speed);
                }
            }

            string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
            gut.WaitForExit ();
            return (output.Equals("true"));
        }


        public override bool SyncDown ()
        {
            SparkleGut gut = new SparkleGut (LocalPath, "sync-down");
            gut.StartInfo.RedirectStandardError = true;
            gut.Start ();

            double percentage = 1.0;
            Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);
            Regex speed_regex = new Regex (@"([0-9]+[KMG]?B/s)", RegexOptions.Compiled);
            while (!gut.StandardError.EndOfStream) {
                string line   = gut.StandardError.ReadLine ();
                Match progress_match = progress_regex.Match (line);
                Match speed_match    = speed_regex.Match (line);
                string speed  = "";
                double number = 0.0;

                if (progress_match.Success) {
                    number = double.Parse (progress_match.Groups [1].Value);
                }

                if (speed_match.Success) {
                    speed = speed_match.Groups [1].Value;
                }

                if (number >= percentage) {
                    percentage = number;
                    base.OnProgressChanged (percentage, speed);
                }
            }

            string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
            gut.WaitForExit ();
            return (output.Equals("true"));
        }


        public override bool HasLocalChanges {
            get {
                SparkleGut gut = new SparkleGut (LocalPath, "has-local-changes");
                gut.Start ();
                string output = gut.StandardOutput.ReadToEnd ().TrimEnd ();
                gut.WaitForExit ();
                return (output.Equals("true"));
            }
        }


        public override bool HasUnsyncedChanges {
            get {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".gut", "has_unsynced_changes");

                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".gut", "has_unsynced_changes");

                if (value) {
                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path).Close ();

                } else {
                    File.Delete (unsynced_file_path);
                }
            }
        }


        // Returns a list of the latest change sets
        public override List <SparkleChangeSet> GetChangeSets (int count)
        {
            if (count < 1)
                count = 30;

            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();

            // Console.InputEncoding  = System.Text.Encoding.Unicode;
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            SparkleGut gut_log = new SparkleGut (LocalPath, "get-change-sets --count=" + count);
            gut_log.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = gut_log.StandardOutput.ReadToEnd ();
            string [] lines = output.Split ("\n".ToCharArray ());
            gut_log.WaitForExit ();


            SparkleChangeSet change_set = null;
            foreach (string line in lines) {
                if (line.StartsWith ("revision")) {
                    change_set = new SparkleChangeSet ();
                    change_set.Folder = Name;
                    change_set.Url = Url;
                    change_sets.Add (change_set);
                }
                if(change_set == null)
                    continue;

                string[] kv = line.Split (":".ToCharArray (), 2);
                if(kv.Length != 2)
                    continue;

                string key = kv[0];
                string val = kv[1];

                if(key.Equals("revision")) {
                    change_set.Revision = val;
                }
                if(key.Equals("user")) {
                    Regex regex = new Regex (@"(.+) <(.+)>");
                    Match match = regex.Match (val);
                    change_set.User = new SparkleUser (match.Groups [1].Value, match.Groups [2].Value);
                }
                if(key.Equals("magical")) {
                    change_set.IsMagical = val.Equals("true");
                }
                if(key.Equals("timestamp")) {
                    Regex regex = new Regex (@"([0-9]{4})-([0-9]{2})-([0-9]{2}) ([0-9]{2}):([0-9]{2}):([0-9]{2}) (.[0-9]{4})");
                    Match match = regex.Match (val);
                    change_set.Timestamp = new DateTime (int.Parse (match.Groups [1].Value),
                        int.Parse (match.Groups [2].Value), int.Parse (match.Groups [3].Value),
                        int.Parse (match.Groups [4].Value), int.Parse (match.Groups [5].Value),
                        int.Parse (match.Groups [6].Value));
                    string time_zone     = match.Groups [7].Value;
                    int our_offset       = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
                    int their_offset     = int.Parse (time_zone.Substring (0, 3));
                    change_set.Timestamp = change_set.Timestamp.AddHours (their_offset * -1);
                    change_set.Timestamp = change_set.Timestamp.AddHours (our_offset);
                }
                if(key.Equals("added")) {
                    change_set.Added.Add(val);
                }
                if(key.Equals("edited")) {
                    change_set.Edited.Add(val);
                }
                if(key.Equals("deleted")) {
                    change_set.Deleted.Add(val);
                }
            }

            return change_sets;
        }

    }
}
