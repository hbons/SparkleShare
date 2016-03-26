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
using System.Net;

using IO = System.IO;

namespace SparkleLib {

    public class SSHAuthenticationInfo : AuthenticationInfo {

        public static SSHAuthenticationInfo DefaultAuthenticationInfo;


        public string PrivateKeyFilePath;
        public string PrivateKey;

        public string PublicKeyFilePath;
        public string PublicKey;

        public string KnownHostsFilePath;


        string Path;


        public SSHAuthenticationInfo ()
        {
            string config_path = IO.Path.GetDirectoryName (SparkleConfig.DefaultConfig.FullPath);
            Path = IO.Path.Combine (config_path, "ssh");

            KnownHostsFilePath = IO.Path.Combine (Path, "known_hosts");

            if (IO.Directory.Exists (Path)) {
                ImportKeys ();

            } else {
                IO.Directory.CreateDirectory (Path);
                CreateKeyPair ();
            }
        }


        void ImportKeys ()
        {
            bool key_found = false;

            foreach (string file_path in IO.Directory.GetFiles (Path)) {
                if (file_path.EndsWith (".key")) {
                    PrivateKeyFilePath = file_path;
                    PublicKeyFilePath = file_path + ".pub";

                    key_found = true;
                    break;
                }
            }

            if (key_found) {
                PrivateKey = IO.File.ReadAllText (PrivateKeyFilePath);
                PublicKey = IO.File.ReadAllText (PublicKeyFilePath);

            } else {
                CreateKeyPair ();
                ImportKeys ();
            }
        }


        bool CreateKeyPair ()
        {
            string key_file_name = DateTime.Now.ToString ("yyyy-MM-dd_HH\\hmm") + ".key";
            string key_file_path = IO.Path.Combine (Path, key_file_name);
            string computer_name = Dns.GetHostName ();

            if (computer_name.EndsWith (".local"))
                computer_name = computer_name.Substring (0, computer_name.Length - ".local".Length);

            if (computer_name.EndsWith (".config"))
                computer_name = computer_name.Substring (0, computer_name.Length - ".config".Length);

            string arguments =
                "-t rsa "  + // Crypto type
                "-b 4096 " + // Key size
                "-P \"\" " + // No password
                "-C \"" + computer_name + " (SparkleShare)\" " + // Key comment
                "-f \"" + key_file_name + "\"";

            var process = new SparkleProcess ("ssh-keygen", arguments);
            process.StartInfo.WorkingDirectory = Path;

            process.Start ();
            process.WaitForExit ();

            if (process.ExitCode == 0) {
                PrivateKeyFilePath = key_file_path;
                PrivateKey = IO.File.ReadAllText (key_file_path);

                PublicKeyFilePath = key_file_path + ".pub";
                PublicKey = IO.File.ReadAllText (key_file_path + ".pub");

                SparkleLogger.LogInfo ("Auth", "Created key pair: " + key_file_name);
                return true;

            } else {
                SparkleLogger.LogInfo ("Auth", "Could not create key pair");
                return false;
            }
        }
    }
}
