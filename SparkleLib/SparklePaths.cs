//   SparkleShare, an instant update workflow to Git.
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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using Mono.Unix;

namespace SparkleLib {

    public static class Backend {

        public static string Name = "Git";

        public static string Path {

            get {

                // QUICK RC1 HACK FIX
                if (File.Exists ("/usr/local/git/bin/git"))
                    return "/usr/local/git/bin/git";
                else if (File.Exists ("/usr/bin/git"))
                    return "/usr/bin/git";

                Process process                          = new Process ();
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute        = false;
                process.StartInfo.FileName               = "which";
                process.StartInfo.Arguments              = Backend.Name.ToLower ();
                process.Start ();

                string path = process.StandardOutput.ReadToEnd ();
                path = path.Trim ();

                if (!string.IsNullOrEmpty (path)) {

                    return path;

                } else {

                    Console.WriteLine ("Sorry, SparkleShare needs " + Backend.Name + " to run, but it wasn't found.");
                    return null;

                }

            }

        }


        public static bool IsPresent {

            get {

                Process process                          = new Process ();
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute        = false;
                process.StartInfo.FileName               = "which";
                process.StartInfo.Arguments              = Backend.Name.ToLower ();
                process.Start ();

                string path = process.StandardOutput.ReadToEnd ();
                path = path.Trim ();

                return !string.IsNullOrEmpty (path);

            }

        }

    }

	
	public static class SparklePaths {
		
		public static string GitPath              = Backend.Path;
		public static string HomePath             = new UnixUserInfo (UnixEnvironment.UserName).HomeDirectory;
		public static string SparklePath          = Path.Combine (HomePath ,"SparkleShare");
		public static string SparkleTmpPath       = Path.Combine (SparklePath, ".tmp");
		public static string SparkleConfigPath    = SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare");
		public static string SparkleKeysPath      = SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare");
		public static string SparkleInstallPath   = Path.Combine (Defines.PREFIX, "sparkleshare");
		public static string SparkleLocalIconPath = SparkleHelpers.CombineMore (SparkleConfigPath, "icons");
		public static string SparkleIconPath      = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare", "icons");

	}

}
