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
using System.IO;

using SparkleLib;

namespace SparkleShare {

    public static class SparkleKeys {

        public static string [] GenerateKeyPair (string output_path, string key_name)
        {
            key_name += ".key";
            string key_file_path = Path.Combine (output_path, key_name);

            if (File.Exists (key_file_path)) {
                SparkleLogger.LogInfo ("Auth", "A key pair exists ('" + key_name + "'), leaving it untouched");
                return new string [] { key_file_path, key_file_path + ".pub" };

            } else {
                if (!Directory.Exists (output_path))
                    Directory.CreateDirectory (output_path);
            }

            Process process = new Process ();

            process.StartInfo.FileName         = "ssh-keygen";
            process.StartInfo.WorkingDirectory = output_path;
            process.StartInfo.UseShellExecute  = false;
            process.StartInfo.CreateNoWindow   = true;

            string computer_name = System.Net.Dns.GetHostName ();

            if (computer_name.EndsWith (".local"))
                computer_name = computer_name.Substring (0, computer_name.Length - 6);

            process.StartInfo.Arguments = "-t rsa " + // crypto type
                "-P \"\"" /* password (none) */ + " " +
                "-C \"" + computer_name + "\"" /* key comment */ + " " +
                "-f \"" + key_name + "\"" /* file name */;

            process.Start ();
            process.WaitForExit ();

            if (process.ExitCode == 0)
                SparkleLogger.LogInfo ("Auth", "Created keypair '" + key_file_path + "'");
            else
                SparkleLogger.LogInfo ("Auth", "Could not create key pair '" + key_file_path + "'");

            return new string [] { key_file_path, key_file_path + ".pub" };
        }


        public static void ImportPrivateKey (string key_file_path)
        {
            Process process = new Process ();

            process.StartInfo.FileName              = "ssh-add";
            process.StartInfo.Arguments             = "\"" + key_file_path + "\"";
            process.StartInfo.UseShellExecute       = false;
            process.StartInfo.CreateNoWindow        = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start ();
            process.WaitForExit ();

            if (process.ExitCode == 0)
                SparkleLogger.LogInfo ("Auth", "Imported key '" + key_file_path + "'");
            else
                SparkleLogger.LogInfo ("Auth", "Could not import key '" + key_file_path + "'");
        }


        public static void ListPrivateKeys ()
        {
            Process process = new Process ();

            process.StartInfo.FileName               = "ssh-add";
            process.StartInfo.Arguments              = "-l";
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;

            process.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string keys_in_use = process.StandardOutput.ReadToEnd ();
            process.WaitForExit ();

            SparkleLogger.LogInfo ("Auth", "The following keys may be used: " +
                Environment.NewLine + keys_in_use.Trim ());
        }
    }
}
