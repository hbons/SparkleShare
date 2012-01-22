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
using System.Diagnostics;

namespace SparkleLib {

    public class SparkleGit : Process {

        public static string ExecPath = null;


        public SparkleGit (string path, string args) : base ()
        {
            EnableRaisingEvents              = true;
            StartInfo.FileName               = SparkleBackend.DefaultBackend.Path;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.WorkingDirectory       = path;

            if (!string.IsNullOrEmpty (ExecPath))
                StartInfo.Arguments = "--exec-path=\"" + ExecPath + "\" " + args;
            else
                StartInfo.Arguments = args;
        }


        new public void Start ()
        {
            SparkleHelpers.DebugInfo ("Cmd", "git " + StartInfo.Arguments);
            base.Start ();
        }
    }
}
