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
using System.Reflection;
using System.Runtime.InteropServices;

namespace SparkleLib {

    public static class SparkleBackend {

        public static string Version {
            get {
                string version = "" + Assembly.GetExecutingAssembly ().GetName ().Version;
                return version.Substring (0, version.Length - 2);
            }
        }


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


        [DllImport ("libc")]
        private static extern int uname (IntPtr buf);
    }
}
