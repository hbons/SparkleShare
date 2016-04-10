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
using System.Reflection;

namespace Sparkles {
    
    public enum OS {
        Unknown,
        Mac,
        Windows,
        Ubuntu,
        GNOME
    }


    public partial class InstallationInfo {

        static OS operating_system = OS.Unknown;

        public static OS OperatingSystem {
            get {
                if (operating_system != OS.Unknown)
                    return operating_system;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                    operating_system = OS.Windows;
                    return operating_system;
                }

                var uname = new Command ("uname", "-a", false);
                string output = uname.StartAndReadStandardOutput ();

                // Environment.OSVersion.Platform.PlatformID.MacOSX is broken in Mono
                // for historical reasons, so check manually
                if (output.StartsWith ("Darwin", StringComparison.InvariantCulture)) {
                    operating_system = OS.Mac;

                } else if (output.Contains ("Ubuntu")) {
                    operating_system = OS.Ubuntu;

                } else {
                    operating_system = OS.GNOME;
                }

                return operating_system;
            }
        }


        public static string Version {
            get {
                string version = "" + Assembly.GetExecutingAssembly ().GetName ().Version;
                return version.Substring (0, version.Length - 2);
            }
        }
    }
}
