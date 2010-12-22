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
	
	public static class SparklePaths
	{
		
		public static string GitPath = "/usr/bin/git"; // TODO: Don't hardcode this

		public static string HomePath = new UnixUserInfo (UnixEnvironment.UserName).HomeDirectory;

		public static string SparklePath = Path.Combine (HomePath ,"SparkleShare");

		public static string SparkleTmpPath = Path.Combine (SparklePath, ".tmp");

		public static string SparkleConfigPath = SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare");
		
		public static string SparkleKeysPath = SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare");

		public static string SparkleInstallPath = SparkleHelpers.CombineMore (Defines.PREFIX, "sparkleshare");

		public static string SparkleLocalIconPath = SparkleHelpers.CombineMore (SparkleConfigPath, "icons", "hicolor");

		public static string SparkleIconPath = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare",
			"icons");

		
		private static string GetGitPath ()
		{
		
			Process process = new Process ();
			
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.FileName               = "which";
			process.StartInfo.Arguments              = "git";
			process.Start ();
			
			string git_path = process.StandardOutput.ReadToEnd ().Trim ();

			if (!string.IsNullOrEmpty (git_path))
				return git_path;
			else
				return null;
		
		}

	}

}
