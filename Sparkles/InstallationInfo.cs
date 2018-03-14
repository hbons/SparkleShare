//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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
        macOS,
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
                    operating_system = OS.macOS;

                } else if (output.Contains ("Ubuntu")) {
                    operating_system = OS.Ubuntu;

                } else {
                    operating_system = OS.GNOME;
                }

                return operating_system;
            }
        }


        public static string OperatingSystemVersion {
            get {
                if (OperatingSystem == OS.macOS) {
                    var uname = new Command ("sw_vers", "-productVersion", false);
                    string output = uname.StartAndReadStandardOutput ();
                    string version = output;

                    // Parse the version number between the periods (e.g. "10.12.1" -> 12)
                    output = output.Substring (output.IndexOf (".") + 1);
                    output = output.Substring (0, output.LastIndexOf ("."));

                    string release = "Unreleased Version";

                    switch (int.Parse (output)) {
                    case 7: release = "Lion"; break;
                    case 8: release = "Mountain Lion"; break;
                    case 9: release = "Mavericks"; break;
                    case 10: release = "Yosemite"; break;
                    case 11: release = "El Capitan"; break;
                    case 12: release = "Sierra"; break;
                    case 13: release = "High Sierra"; break;
                    }

                    return string.Format ("{0} ({1})", version, release);
                }

                string os_version = Environment.OSVersion.ToString ();
                return string.Format ("({0})", os_version.Replace ("Unix", "Linux"));
            }
        }


        public static string Version {
            get {
                string version = "" + Assembly.GetExecutingAssembly ().GetName ().Version;
                return version.Substring (0, version.Length - 2);
            }
        }


        public static bool IsFlatpak {
            get {
                return Directory.StartsWith ("/app", StringComparison.InvariantCulture);
            }
        }
    }
}
