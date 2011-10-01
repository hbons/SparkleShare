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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;

namespace SparkleLib {

    public static class SparklePaths {

        public static string HomePath             = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
        public static string SparklePath          = Path.Combine (HomePath ,"SparkleShare");
        public static string SparkleTmpPath       = Path.Combine (SparklePath, ".tmp");
        public static string SparkleConfigPath    = Path.Combine (Environment.GetFolderPath (
                                                        Environment.SpecialFolder.ApplicationData), "sparkleshare");
        public static string SparkleLocalIconPath = Path.Combine (SparkleConfigPath, "icons");

        public static string SparkleInstallPath   = Path.Combine (Defines.PREFIX, "sparkleshare");
        public static string SparkleIconPath      = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare", "icons");

    }
}

