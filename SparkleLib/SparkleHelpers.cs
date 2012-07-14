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
using System.Security.Cryptography;
using System.Text;

namespace SparkleLib {
    
    public static class SparkleHelpers {

        private static Object debug_lock = new Object ();

        // Show debug info if needed
        public static void DebugInfo (string type, string message)
        {
            string timestamp = DateTime.Now.ToString ("HH:mm:ss");
            string line      = timestamp + " | " + type + " | " + message;

            if (SparkleConfig.DebugMode)
                Console.WriteLine (line);

            lock (debug_lock) {
                File.AppendAllText (
                    SparkleConfig.DefaultConfig.LogFilePath,
                    line + Environment.NewLine
                );
            }
        }


        // Makes it possible to combine more than
        // two paths at once
        public static string CombineMore (params string [] parts)
        {
            string new_path = "";

            foreach (string part in parts)
                new_path = Path.Combine (new_path, part);

            return new_path;
        }


        // Recursively sets access rights of a folder to 'Normal'
        public static void ClearAttributes (string path)
        {
            if (Directory.Exists (path)) {
                string [] folders = Directory .GetDirectories (path);

                foreach (string folder in folders)
                    ClearAttributes (folder);

                string [] files = Directory .GetFiles(path);

                foreach (string file in files)
                    if (!IsSymlink (file))
                        File.SetAttributes (file, FileAttributes.Normal);
            }
        }


        // Check if a file is a symbolic link
        public static bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }


        // Converts a UNIX timestamp to a more usable time object
        public static DateTime UnixTimestampToDateTime (int timestamp)
        {
            DateTime unix_epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0);
            return unix_epoch.AddSeconds (timestamp);
        }


        public static bool IsWindows {
            get {
                PlatformID platform = Environment.OSVersion.Platform;

                return (platform == PlatformID.Win32NT ||
                        platform == PlatformID.Win32S  ||
                        platform == PlatformID.Win32Windows);
            }
        }


        public static string SHA1 (string s)
        {
            SHA1 sha1         = new SHA1CryptoServiceProvider ();
            byte [] bytes     = ASCIIEncoding.Default.GetBytes (s);
            byte [] enc_bytes = sha1.ComputeHash (bytes);

            return BitConverter.ToString (enc_bytes).ToLower ().Replace ("-", "");
        }


        public static string MD5 (string s)
        {
            MD5 md5           = new MD5CryptoServiceProvider ();
            byte [] bytes     = ASCIIEncoding.Default.GetBytes (s);
            byte [] enc_bytes = md5.ComputeHash (bytes);

            return BitConverter.ToString (enc_bytes).ToLower ().Replace ("-", "");
        }
    }
}
