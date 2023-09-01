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


using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Sparkles {

    public class Command : Process {

        bool write_output;
        static string[] extended_search_path;

        public static void SetSearchPath(string[] pathes)
        {
            extended_search_path = pathes;
        }

        public static void SetSearchPath(string path)
        {
            SetSearchPath(new string[] { path});
        }

        public Command (string path, string args) : this (path, args, write_output: true)
        {
        }


        public Command (string path, string args, bool write_output)
        {
            this.write_output = write_output;

            StartInfo.FileName = path;
            StartInfo.Arguments = args;

            StartInfo.WorkingDirectory = Path.GetTempPath ();
            StartInfo.CreateNoWindow = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            StartInfo.RedirectStandardError = true;
            StartInfo.UseShellExecute = false;

            EnableRaisingEvents = true;
        }


        new public void Start ()
        {
            string folder = "";

            if (StartInfo.WorkingDirectory != Path.GetTempPath ())
                folder = Path.GetFileName (StartInfo.WorkingDirectory) + " | ";
                
            if (write_output)
                Logger.LogInfo ("Cmd", folder + Path.GetFileName (StartInfo.FileName) + " " + StartInfo.Arguments);

            try {
                base.Start ();

            } catch (Exception e) {
                Logger.LogInfo ("Cmd", "Couldn't execute command: " + e.Message);
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

            StartInfo.RedirectStandardError = false;

            return output.TrimEnd ();
        }


        public void SetEnvironmentVariable (string variable, string content)
        {
            if (StartInfo.EnvironmentVariables.ContainsKey (variable))
                StartInfo.EnvironmentVariables [variable] = content;
            else
                StartInfo.EnvironmentVariables.Add (variable, content);
        }


        protected static string LocateCommand (string name)
        {
            string[] possible_command_paths = {
                Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.Personal), "bin"),
                Path.Combine(InstallationInfo.Directory, "bin"),
                "/usr/local/bin/",
                "/usr/bin/",
                "/opt/local/bin/"
            };

            List<string> command_paths = new List<string>();            
            command_paths.AddRange(extended_search_path);
            command_paths.AddRange(possible_command_paths);

            foreach (string path in command_paths) {
                if (File.Exists(Path.Combine (path, name))) {
                    return Path.Combine (path, name);
                }
            }

            return name;
        }
    }
}
