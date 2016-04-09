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

namespace Sparkles {

    public class SSHAuthenticationInfo : AuthenticationInfo {

        public static SSHAuthenticationInfo DefaultAuthenticationInfo;

        public string PrivateKeyFilePath { get; private set; }
        public string PrivateKey { get; private set; }

        public string PublicKeyFilePath { get; private set; }
        public string PublicKey { get; private set; }

        public string KnownHostsFilePath { get; private set; }


        readonly string Path;


        public SSHAuthenticationInfo ()
        {
            Path = IO.Path.Combine (Configuration.DefaultConfiguration.DirectoryPath, "ssh");

            KnownHostsFilePath = IO.Path.Combine (Path, "known_hosts");
            KnownHostsFilePath = MakeWindowsDomainAccountSafe (KnownHostsFilePath);

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
                if (file_path.EndsWith (".key", StringComparison.InvariantCultureIgnoreCase)) {
                    PrivateKeyFilePath = file_path;
                    PublicKeyFilePath  = file_path + ".pub";

                    key_found = true;
                    break;
                }
            }

            if (key_found) {
                PrivateKeyFilePath = MakeWindowsDomainAccountSafe (PrivateKeyFilePath);
                PublicKeyFilePath  = MakeWindowsDomainAccountSafe (PublicKeyFilePath);

                PrivateKey = IO.File.ReadAllText (PrivateKeyFilePath);
                PublicKey  = IO.File.ReadAllText (PublicKeyFilePath);

            } else {
                CreateKeyPair ();
                ImportKeys ();
            }
        }


        bool CreateKeyPair ()
        {
            string key_file_name = DateTime.Now.ToString ("yyyy-MM-dd_HH\\hmm") + ".key";
            string computer_name = Dns.GetHostName ();

            if (computer_name.EndsWith (".local",  StringComparison.InvariantCultureIgnoreCase) ||
                computer_name.EndsWith (".config", StringComparison.InvariantCultureIgnoreCase))

                computer_name = computer_name.Substring (0,
                    computer_name.LastIndexOf (".", StringComparison.InvariantCulture));

            string arguments =
                "-t rsa "  + // Crypto type
                "-b 4096 " + // Key size
                "-P \"\" " + // No password
                "-C \"" + computer_name + " (SparkleShare)\" " + // Key comment
                "-f \"" + key_file_name + "\"";

            var ssh_keygen = new Command ("ssh-keygen", arguments);
            ssh_keygen.StartInfo.WorkingDirectory = Path;
            ssh_keygen.StartAndWaitForExit ();

            if (ssh_keygen.ExitCode == 0) {
                Logger.LogInfo ("Auth", "Created key pair: " + key_file_name);
                ImportKeys ();

                return true;
            }

            Logger.LogInfo ("Auth", "Could not create key pair");
            return false;
        }


        // Use forward slashes in paths when dealing with Windows domain accounts
        string MakeWindowsDomainAccountSafe (string path)
        {
            if (path.StartsWith ("\\\\", StringComparison.InvariantCulture))
                return path.Replace ("\\", "/");

            return path;
        }
    }
}
