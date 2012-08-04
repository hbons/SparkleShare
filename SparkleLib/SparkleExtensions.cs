//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SparkleLib {

    public static class Extensions {

        public static string Combine (this string [] parts)
        {
            string new_path = "";

            foreach (string part in parts)
                new_path = Path.Combine (new_path, part);

            return new_path;
        }

        
        public static string SHA1 (this string s)
        {
            SHA1 sha1          = new SHA1CryptoServiceProvider ();
            byte [] bytes      = ASCIIEncoding.Default.GetBytes (s);
            byte [] sha1_bytes = sha1.ComputeHash (bytes);

            return BitConverter.ToString (sha1_bytes).ToLower ().Replace ("-", "");
        }


        public static string MD5 (this string s)
        {
            MD5 md5           = new MD5CryptoServiceProvider ();
            byte [] bytes     = ASCIIEncoding.Default.GetBytes (s);
            byte [] md5_bytes = md5.ComputeHash (bytes);

            return BitConverter.ToString (md5_bytes).ToLower ().Replace ("-", "");
        }
    }
}
