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


using System.IO;

namespace Sparkles
{
    public class OpenSSLCommand : Command
    {
        public static string OpenSSLBinary = "openssl";
        public static string OpenSSLPath = Path.GetDirectoryName(LocateCommand(OpenSSLBinary)).Replace("\\", "/");

        public static string OpenSSLCommandPath
        {
            get
            {
                return LocateCommand(OpenSSLBinary).Replace("\\", "/");
            }
        }


        public OpenSSLCommand(string command, string args) :
            base(Path.Combine(OpenSSLPath, command), args)
        {
        }
        public OpenSSLCommand(string args) : base(OpenSSLCommandPath, args)
        {
        }
        public static string OpenSSLVersion 
        {
            get {
                var openssl_version = new Command (OpenSSLCommandPath, "version", false);

                string [] version = openssl_version.StartAndReadStandardOutput ().Split (' ');
                string version_string = version [0];
                if (version.Length >= 2) {
                    version_string= version [0] + " " + version [1];
                }
                return version_string;
            }
        }
    }
}
