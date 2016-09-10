//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//   Portions Copyright (C) 2016 Paul Hammant <paul@hammant.org>
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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sparkles.Subversion {

    public class SubversionCommand : Command {

        public static string ExecPath;


        static string svn_path;

        public static string SvnPath {
            get {
                if (svn_path == null)
                    svn_path = LocateCommand ("svn");

                return svn_path;
            }

            set {
                svn_path = value;
            }
        }


        public static string SvnVersion {
            get {
                if (SvnPath == null)
                    SvnPath = LocateCommand ("svn");

                var svn_version = new Command (SvnPath, "--version", false);

                if (ExecPath != null)
                    svn_version.SetEnvironmentVariable ("SVN_EXEC_PATH", ExecPath);

                string version = svn_version.StartAndReadStandardOutput ();
                return version.Substring(0, version.IndexOf("\n")).Replace ("svn, version ", "");
            }
        }

        public SubversionCommand (string working_dir, string args) : this (working_dir, args, null)
        {
        }


        public SubversionCommand (string working_dir, string args, SSHAuthenticationInfo auth_info) : base (SvnPath, args)
        {

            StartInfo.WorkingDirectory = working_dir;

            if (ExecPath != null)
                SetEnvironmentVariable ("SVN_EXEC_PATH", ExecPath);

			SetEnvironmentVariable ("LANG", "en_US");

        }


        static ErrorStatus FindError (string line)
        {
            ErrorStatus error = ErrorStatus.None;

            if (line.Contains ("WARNING: REMOTE HOST IDENTIFICATION HAS CHANGED!") ||
                line.Contains ("WARNING: POSSIBLE DNS SPOOFING DETECTED!")) {

                error = ErrorStatus.HostIdentityChanged;
            }

            if (line.StartsWith ("Permission denied") ||
                       line.StartsWith ("ssh_exchange_identification: Connection closed by remote host") ||
                       line.StartsWith ("The authenticity of host")) {

                error = ErrorStatus.AuthenticationFailed;
            }

            if (line.EndsWith ("does not appear to be a git repository"))
                error = ErrorStatus.NotFound;

            if (line.EndsWith ("expected old/new/ref, got 'shallow"))
                error = ErrorStatus.IncompatibleClientServer;

            if (line.StartsWith ("error: Disk space exceeded") ||
                       line.EndsWith ("No space left on device") ||
                       line.EndsWith ("file write error (Disk quota exceeded)")) {

                error = ErrorStatus.DiskSpaceExceeded;
            }

            return error;
        }
    }
}
