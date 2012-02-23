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
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public class SparkleFetcherGut : SparkleFetcherBase {

        private SparkleGut gut;


        public SparkleFetcherGut (string server, string remote_path, string target_folder) :
            base (server, remote_path, target_folder)
        {
            if (server.EndsWith ("/"))
                server = server.Substring (0, server.Length - 1);

            // FIXME: Adding these lines makes the fetcher fail
            // if (remote_path.EndsWith ("/"))
            //     remote_path = remote_path.Substring (0, remote_path.Length - 1);

            if (!remote_path.StartsWith ("/"))
                remote_path = "/" + remote_path;


            Uri uri;

            try {
                uri = new Uri (server + remote_path);
            } catch (UriFormatException) {
                uri = new Uri ("rsync+ssh://" + server + remote_path);
            }

            TargetFolder = target_folder;
            RemoteUrl    = uri.ToString ();
        }


        public override bool Fetch ()
        {
            // place settings into the folder
            this.gut = new SparkleGut (SparkleConfig.DefaultConfig.TmpPath,
                "configure \"" + TargetFolder + "\" --url=\"" + RemoteUrl + "\"" +
                " --user=\"" + SparkleConfig.DefaultConfig.User.Name +
                " <" + SparkleConfig.DefaultConfig.User.Email + ">\"");
            this.gut.Start ();
            this.gut.StandardOutput.ReadToEnd ().TrimEnd ();
            this.gut.WaitForExit ();

            // use the previously-set settings to fetch the base data
            this.gut = new SparkleGut (TargetFolder, "fetch");
            
            this.gut.StartInfo.RedirectStandardError = true;
            this.gut.Start ();

            double percentage = 1.0;
            Regex progress_regex = new Regex (@"([0-9]+)%", RegexOptions.Compiled);

            DateTime last_change     = DateTime.Now;
            TimeSpan change_interval = new TimeSpan (0, 0, 0, 1);

            while (!this.gut.StandardError.EndOfStream) {
                string line = this.gut.StandardError.ReadLine ();
                Match match = progress_regex.Match (line);
                
                double number = 0.0;
                if (match.Success) {
                    number = double.Parse (match.Groups [1].Value);
                }
                
                if (number >= percentage) {
                    percentage = number;

                    if (DateTime.Compare (last_change, DateTime.Now.Subtract (change_interval)) < 0) {
                        base.OnProgressChanged (percentage);
                        last_change = DateTime.Now;
                    }
                }
            }
            
            this.gut.WaitForExit ();
            SparkleHelpers.DebugInfo ("Gut", "Exit code " + this.gut.ExitCode.ToString ());

            while (percentage < 100) {
                percentage += 25;

                if (percentage >= 100)
                    break;

                base.OnProgressChanged (percentage);
                Thread.Sleep (750);
            }

            base.OnProgressChanged (100);
            Thread.Sleep (1000);

            if (this.gut.ExitCode != 0) {
                return false;
            } else {
                return true;
            }
        }


        public override void Stop ()
        {
            if (this.gut != null && !this.gut.HasExited) {
                this.gut.Kill ();
                this.gut.Dispose ();
            }

            Dispose ();
        }
    }
}
