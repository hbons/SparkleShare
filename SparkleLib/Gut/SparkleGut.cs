//   A gutted-out interface from sparkleshare to any random executable,
//   designed to make sparkleshare back-ends incredibly easy to develop
//   in any language:
//   Copyright (C) 2012  Shish <shish@shishnet.org>
//
//   Based on the default Git back-end:
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

    public class SparkleGut : Process {

        public static string Path     = null;


        public SparkleGut (string path, string args) : base ()
        {
            Path = LocateGut ();

            EnableRaisingEvents              = true;
            StartInfo.FileName               = Path;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.WorkingDirectory       = path;
            StartInfo.CreateNoWindow         = true;
			StartInfo.Arguments              = args;
        }


        new public void Start ()
        {
            SparkleHelpers.DebugInfo ("Cmd", "gut " + StartInfo.Arguments);

            try {
                base.Start ();

            } catch (Exception e) {
                SparkleHelpers.DebugInfo ("Cmd", "There's a problem running Gut: " + e.Message);
                Environment.Exit (-1);
            }
        }


        private string LocateGut ()
        {
            if (!string.IsNullOrEmpty (Path))
                return Path;

            string [] possible_gut_paths = new string [] {
                "/usr/bin/gut",
                "/usr/local/bin/gut",
                "/opt/local/bin/gut",
                "/usr/local/gut/bin/gut"
            };

            foreach (string path in possible_gut_paths)
                if (File.Exists (path))
                    return path;

            return "gut";
        }
    }
}
