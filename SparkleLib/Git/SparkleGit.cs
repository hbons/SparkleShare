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
using System.Diagnostics;

namespace SparkleLib {

    public class SparkleGit : Process {

        public static string ExecPath = null;
        public static string Path     = null;


        public SparkleGit (string path, string args) : base ()
        {
            Path = LocateGit ();

            EnableRaisingEvents              = true;
            StartInfo.FileName               = Path;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.WorkingDirectory       = path;
            StartInfo.CreateNoWindow         = true;

            if (string.IsNullOrEmpty (ExecPath))
                StartInfo.Arguments = args;
            else
                StartInfo.Arguments = "--exec-path=\"" + ExecPath + "\" " + args;
        }


        new public void Start ()
        {
            SparkleHelpers.DebugInfo ("Cmd", "git " + StartInfo.Arguments);

            try {
                base.Start ();

            } catch (Exception e) {
                SparkleHelpers.DebugInfo ("Cmd", "There's a problem running Git: " + e.Message);
                Environment.Exit (-1);
            }
        }


        private string LocateGit ()
        {
            if (!string.IsNullOrEmpty (Path))
                return Path;

            string [] possible_git_paths = new string [] {
                "/usr/bin/git",
                "/usr/local/bin/git",
                "/opt/local/bin/git",
                "/usr/local/git/bin/git"
            };

            foreach (string path in possible_git_paths)
                if (File.Exists (path))
                    return path;

            return "git";
        }
    }
}
