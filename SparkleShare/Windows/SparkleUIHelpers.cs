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

using System.Collections.Generic;
using System.Windows.Forms;
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

        public static string ToHex (this System.Drawing.Color color)
        {
            return String.Format ("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        //http://stackoverflow.com/a/1499161/33499
        public static IEnumerable<Control> All (this Control.ControlCollection controls)
        {
            foreach (Control control in controls) {
                foreach (Control grandChild in control.Controls.All ())
                    yield return grandChild;

                yield return control;
            }
        }
    }
}
