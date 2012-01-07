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

using Gtk;
using SparkleLib;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SparkleShare {

    public static class SparkleUIHelpers {

        // Creates an MD5 hash of input
        public static string GetMD5 (string s)
        {
            MD5 md5 = new MD5CryptoServiceProvider ();
            Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
            Byte[] encodedBytes = md5.ComputeHash (bytes);
            return BitConverter.ToString (encodedBytes).ToLower ().Replace ("-", "");
        }


        // Looks up an icon from the system's theme
        public static Gdk.Pixbuf GetIcon (string name, int size)
        {
            IconTheme icon_theme = new IconTheme ();

            icon_theme.AppendSearchPath (
                Path.Combine (SparkleUI.AssetsPath, "icons")
            );

            icon_theme.AppendSearchPath (
                Path.Combine (SparkleConfig.ConfigPath, "icons")
            );

            try {
                return icon_theme.LoadIcon (name, size, IconLookupFlags.GenericFallback);

            } catch {
                try {
                    return icon_theme.LoadIcon ("gtk-missing-image", size, IconLookupFlags.GenericFallback);

                } catch {
                    return null;
                }
            }
        }


        public static Image GetImage (string name)
        {
            string image_path = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare",
                "pixmaps", name);

            return new Image (image_path);
        }


        // Converts a Gdk RGB color to a hex value.
        // Example: from "rgb:0,0,0" to "#000000"
        public static string GdkColorToHex (Gdk.Color color)
        {
            return String.Format ("#{0:X2}{1:X2}{2:X2}",
                (int) Math.Truncate (color.Red   / 256.00),
                (int) Math.Truncate (color.Green / 256.00),
                (int) Math.Truncate (color.Blue  / 256.00));
        }
    }
}
