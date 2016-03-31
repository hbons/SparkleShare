//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
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

namespace Sparkles {

    public class Command : Process {

        bool write_output;


        public Command (string path, string args) : this (path, args, false)
        {
        }


        public Command (string path, string args, bool write_output)
        {
            this.write_output = write_output;

            StartInfo.FileName = path;
            StartInfo.Arguments = args;
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            StartInfo.CreateNoWindow = true;
            EnableRaisingEvents = true;
        }


        new public void Start ()
        {
            string folder = "";

            if (!string.IsNullOrEmpty (StartInfo.WorkingDirectory))
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

            return output.TrimEnd ();
        }


        protected string LocateCommand (string name)
        {
            string [] possible_command_paths = {
                Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/bin/" + name,
                InstallationInfo.Directory + "/bin/" + name,
                "/usr/local/bin/" + name,
                "/usr/bin/" + name,
                "/opt/local/bin/" + name
            };

            foreach (string path in possible_command_paths) {
                if (File.Exists (path))
                    return path;
            }

            return name;
        }
    }
}
