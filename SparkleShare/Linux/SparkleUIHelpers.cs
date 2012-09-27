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

using Gtk;

namespace SparkleShare {

    public static class SparkleUIHelpers {

        // Looks up an icon from the system's theme
        public static Gdk.Pixbuf GetIcon (string name, int size)
        {
            IconTheme icon_theme = new IconTheme ();
			
//          foreach (string search_path in IconTheme.Default.SearchPath)
//              icon_theme.AppendSearchPath (search_path);	

            // FIXME: Temporary workaround for a bug in IconTheme.SearchPath in Gtk# on 64-bit systems
            // https://github.com/mono/gtk-sharp/commit/9c54fd5ae77f63d11fdc6873a3cb90691990e37f
            icon_theme.AppendSearchPath ("/usr/share/icons");
            icon_theme.AppendSearchPath ("/usr/local/share/icons");
            icon_theme.AppendSearchPath ("/opt/local/share/icons");

            icon_theme.AppendSearchPath (Path.Combine (SparkleUI.AssetsPath, "icons"));

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
            string image_path = new string [] { SparkleUI.AssetsPath, "pixmaps", name }.Combine ();
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
