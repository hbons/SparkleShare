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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Mono.Unix;
using System;
using System.IO;

namespace SparkleShare {
	
	public static class SparklePaths {

		private static UnixUserInfo UnixUserInfo =
			new UnixUserInfo (UnixEnvironment.UserName);

		public static string HomePath = UnixUserInfo.HomeDirectory;
			
		public static string SparklePath = Path.Combine (HomePath ,"SparkleShare");

		public static string SparkleTmpPath = Path.Combine (SparklePath, ".tmp");

		public static string SparkleConfigPath =
			SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare");
			
		public static string SparkleInstallPath =
			SparkleHelpers.CombineMore ("usr", "share", "sparkleshare",
			                            "icons", "hicolor");

		public static string SparkleAvatarPath =
			Path.Combine (SparkleConfigPath, "avatars");
			                                   
		public static string SparkleIconPath =
			SparkleHelpers.CombineMore ("usr", "share", "icons", "hicolor");

	}

}
