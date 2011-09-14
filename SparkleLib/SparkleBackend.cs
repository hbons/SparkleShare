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
using System.Runtime.InteropServices;

namespace SparkleLib {

    public class SparkleBackend {

        public static SparkleBackend DefaultBackend = new SparkleBackendGit ();

        public string Name;
        public string Path;


        public SparkleBackend (string name, string [] paths)
        {
            Name = name;
            Path = "git";

            foreach (string path in paths) {
                if (File.Exists (path)) {
                    Path = path;
                    break;
                }
            }
        }


        public bool IsPresent {
            get {
               return (Path != null);
            }
        }


        public bool IsUsablePath (string path)
        {
            return (path.Length > 0);
        }


        public static string Version {
            get {
                return Defines.VERSION;
            }
        }


        // Strange magic needed by Platform ()
        [DllImport ("libc")]
        static extern int uname (IntPtr buf);


        // This fixes the PlatformID enumeration for MacOSX in Environment.OSVersion.Platform,
        // which is intentionally broken in Mono for historical reasons
        public static PlatformID Platform {
            get {
                IntPtr buf = IntPtr.Zero;

                try {
                    buf = Marshal.AllocHGlobal (8192);

                    if (uname (buf) == 0 && Marshal.PtrToStringAnsi (buf) == "Darwin")
                        return PlatformID.MacOSX;

                } catch {
                } finally {
                    if (buf != IntPtr.Zero)
                        Marshal.FreeHGlobal (buf);
                }

                return Environment.OSVersion.Platform;
            }
        }
    }


    public class SparkleBackendGit : SparkleBackend {

        private static string name     = "Git";
        private static string [] paths = new string [] {
            "/opt/local/bin/git",
            "/usr/bin/git",
            "/usr/local/bin/git",
            "/usr/local/git/bin/git"
        };

        public SparkleBackendGit () : base (name, paths) { }

    }


    public class SparkleBackendHg : SparkleBackend {

        private static string name     = "Hg";
        private static string [] paths = new string [] {
            "/opt/local/bin/hg",
            "/usr/bin/hg"
        };

        public SparkleBackendHg () : base (name, paths) { }

    }


    public class SparkleBackendScp : SparkleBackend {

        private static string name     = "Scp";
        private static string [] paths = new string [] {
            "/usr/bin/scp"
        };

        public SparkleBackendScp () : base (name, paths) { }

    }
}
