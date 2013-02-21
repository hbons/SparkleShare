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
using SparkleLib;

namespace SparkleLib.Git {

    public abstract class SparkleProcess : Process {

        public SparkleProcess (string path, string args) : base ()
        {
            StartInfo.FileName  = path;
            StartInfo.Arguments = args;
        }


        new public void Start ()
        {
            SparkleLogger.LogInfo ("Cmd | " + System.IO.Path.GetFileName (StartInfo.WorkingDirectory),
                System.IO.Path.GetFileName (StartInfo.FileName) + " " + StartInfo.Arguments);

            try {
                base.Start ();

            } catch (Exception e) {
                SparkleLogger.LogInfo ("Cmd", "Couldn't execute command: " + e.Message);
                Environment.Exit (-1);
            }
        }


        public void StartAndWaitForExit ()
        {
            Start ();
            WaitForExit ();
        }


        public string StartAndReadStandardOutput ()
        {
            Start ();
            
            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = StandardOutput.ReadToEnd ();
            WaitForExit ();
            
            return output.TrimEnd ();
        }


        public string StartAndReadStandardError ()
        {
            StartInfo.RedirectStandardError = true;
            Start ();
            
            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = StandardError.ReadToEnd ();
            WaitForExit ();
            
            return output.TrimEnd ();
        }


        protected string LocateCommand (string name)
        {
            string [] possible_command_paths = new string [] {
                Defines.INSTALL_DIR + "/bin/" + name,
                "/usr/bin/" + name,
                "/usr/local/bin/" + name,
                "/opt/local/bin/" + name
            };

            foreach (string path in possible_command_paths) {
                if (File.Exists (path))
                    return path;
            }

            return name;
        }
    }


    public class SparkleGit : SparkleProcess {

        public static string ExecPath;
        public static string GitPath;


        public SparkleGit (string path, string args) : base (path, args)
        {
            if (string.IsNullOrEmpty (GitPath))
                GitPath = LocateCommand ("git");

            EnableRaisingEvents              = true;
            StartInfo.FileName               = GitPath;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.WorkingDirectory       = path;
            StartInfo.CreateNoWindow         = true;

            if (StartInfo.EnvironmentVariables.ContainsKey ("LANG"))
                StartInfo.EnvironmentVariables ["LANG"] = "en_US";
            else
                StartInfo.EnvironmentVariables.Add ("LANG", "en_US");

            if (string.IsNullOrEmpty (ExecPath))
                StartInfo.Arguments = args;
            else
                StartInfo.Arguments = "--exec-path=\"" + ExecPath + "\" " + args;
        }
    }


    public class SparkleGitBin : SparkleProcess {

        public static string GitBinPath;


        public SparkleGitBin (string path, string args) : base (path, args)
        {
            if (string.IsNullOrEmpty (GitBinPath))
                GitBinPath = LocateCommand ("git-bin");

            EnableRaisingEvents              = true;
            StartInfo.FileName               = GitBinPath;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.UseShellExecute        = false;
            StartInfo.WorkingDirectory       = path;
            StartInfo.CreateNoWindow         = true;
            StartInfo.Arguments              = args;
        }
    }
}
