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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;

using Gtk;

namespace SparkleShare {

    public static class UserInterfaceHelpers {

        public static Gdk.Pixbuf GetIcon (string name, int size)
        {
            IconTheme icon_theme = new IconTheme ();
			icon_theme.AppendSearchPath (Path.Combine (UserInterface.AssetsPath, "icons"));
			
            foreach (string search_path in IconTheme.Default.SearchPath)
               icon_theme.AppendSearchPath (search_path);	

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
            string image_path = Path.Combine (UserInterface.AssetsPath, "pixmaps", name);
            return new Image (image_path);
        }


        // Converts a Gdk RGB color to a hex value.
        // Example: from "rgb:0,0,0" to "#000000"
        public static string ColorToHex (Gdk.Color color)
        {
            return string.Format ("#{0:X2}{1:X2}{2:X2}",
                (int) Math.Truncate (color.Red   / 256.00),
                (int) Math.Truncate (color.Green / 256.00),
                (int) Math.Truncate (color.Blue  / 256.00));
        }

        
        public static Gdk.Color RGBAToColor (Gdk.RGBA rgba) {
            return new Gdk.Color () {
                Red   = (ushort) (rgba.Red * 65535),
                Green = (ushort) (rgba.Green * 65535),
                Blue  = (ushort)(rgba.Blue * 65535)
            };
        }


        public static string RGBAToHex (Gdk.RGBA rgba)
        {
            return ColorToHex (RGBAToColor (rgba));
        }


        public static Gdk.Color MixColors (Gdk.Color first_color, Gdk.Color second_color, double ratio)
        {
            return new Gdk.Color (
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Red * (1.0 - ratio) +
                    second_color.Red * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Green * (1.0 - ratio) +
                    second_color.Green * ratio))) / 65535),
                Convert.ToByte ((255 * (Math.Min (65535, first_color.Blue * (1.0 - ratio) +
                    second_color.Blue * ratio))) / 65535)
            );
        }
    }
}
