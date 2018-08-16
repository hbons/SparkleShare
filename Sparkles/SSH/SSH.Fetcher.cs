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
using System.IO;
using System.Security.Cryptography;

namespace Sparkles {

    public abstract class SSHFetcher : BaseFetcher {

        public static string SSHKeyScan = "ssh-keyscan";
        SSHAuthenticationInfo auth_info;


        protected SSHFetcher (FetcherInfo info, SSHAuthenticationInfo auth_info) : base (info)
        {
            this.auth_info = auth_info;
        }


        public override FetchResult Fetch ()
        {
            bool host_key_missing = false;
            string host_key = FetchHostKey ();

            if (host_key == null)
                host_key_missing = true;

            if (RequiredFingerprint == null) {
                Logger.LogInfo ("Auth", "Skipping fingerprint check");  // TODO expose to UI

            } else {
                string host_fingerprint;

                try {
                    host_fingerprint = DeriveFingerprint (host_key);

                } catch {
                    return FetchResult.HostNotSupported;
                }

                if (RequiredFingerprint != host_fingerprint) {
                    Logger.LogInfo ("Auth", "Fingerprints don't match");
                    return FetchResult.HostChanged;
                }

                Logger.LogInfo ("Auth", "Fingerprints match");
            }

            bool authenticated;

            try {
                authenticated = CanAuthenticateTo (RemoteUrl, this.auth_info);

            } catch (NetworkException) {
                return FetchResult.NoNetwork;
            }

            if (host_key_missing)
                return FetchResult.HostNotSupported;

            if (!authenticated)
                return FetchResult.NotAuthenticated;

            AcceptHostKey (host_key);
            return FetchResult.Success;
        }


        string FetchHostKey ()
        {
            Logger.LogInfo ("Auth", string.Format ("Fetching host key for {0}", RemoteUrl.Host));
            var ssh_keyscan = new Command (SSHKeyScan, string.Format ("-t rsa -p 22 {0}", RemoteUrl.Host));

            if (RemoteUrl.Port > 0)
                ssh_keyscan.StartInfo.Arguments = string.Format ("-t rsa -p {0} {1}", RemoteUrl.Port, RemoteUrl.Host);

            string host_key = ssh_keyscan.StartAndReadStandardOutput ();

            if (ssh_keyscan.ExitCode == 0 && !string.IsNullOrWhiteSpace (host_key))
                return host_key;

            Logger.LogInfo ("Auth", "Could not fetch host key");
            return null;
        }


        void AcceptHostKey (string host_key)
        {
            string ssh_config_path = Path.Combine (Configuration.DefaultConfiguration.DirectoryPath, "ssh");
            string known_hosts_file_path = Path.Combine (ssh_config_path, "known_hosts");

            if (!File.Exists (known_hosts_file_path)) {
                if (!Directory.Exists (ssh_config_path))
                    Directory.CreateDirectory (ssh_config_path);

                File.Create (known_hosts_file_path).Close ();
            }

            string host                 = RemoteUrl.Host;
            string known_hosts          = File.ReadAllText (known_hosts_file_path);
            string [] known_hosts_lines = File.ReadAllLines (known_hosts_file_path);

            foreach (string line in known_hosts_lines) {
                if (line.StartsWith (host + " ", StringComparison.InvariantCulture))
                    return;
            }

            if (known_hosts.EndsWith ("\n", StringComparison.InvariantCulture))
                File.AppendAllText (known_hosts_file_path, host_key + "\n");
            else
                File.AppendAllText (known_hosts_file_path, "\n" + host_key + "\n");
        }


        string DeriveFingerprint (string public_key)
        {
            try {
                SHA256 sha256 = new SHA256CryptoServiceProvider ();
                string key = public_key.Split (" ".ToCharArray ()) [2];

                byte [] base64_bytes = Convert.FromBase64String (key);
                byte [] sha256_bytes = sha256.ComputeHash (base64_bytes);

                string fingerprint = BitConverter.ToString (sha256_bytes);
                fingerprint = fingerprint.ToLower ().Replace ("-", ":");

                return fingerprint;

            } catch (Exception e) {
                throw new FormatException ("Could not derive fingerprint from " + public_key, e);
            }
        }


        public static bool CanAuthenticateTo (Uri host, SSHAuthenticationInfo auth_info)
        {
            if (host == null || auth_info == null)
                throw new ArgumentNullException ();

            const string AUTH_CHECK = "Authentication succeeded";
            const string OFFLINE_CHECK = "Could not resolve hostname";

            string server = host.Authority;

            if (!string.IsNullOrEmpty (host.UserInfo))
                server = host.UserInfo + "@" + host.Authority;

            string [] args  = new string [] {
                "-T", // Non-interactive mode
                "-v", // Verbose output to StandardError
                "-o", "StrictHostKeyChecking=no",
                "-o", "UserKnownHostsFile=/dev/null",
                "-i", auth_info.PrivateKeyFilePath,
                 server };

            var ssh = new Command ("ssh", string.Join (" ", args));
            string output = ssh.StartAndReadStandardError ();

            if (output.Contains (OFFLINE_CHECK))
                throw new NetworkException ();

            return output.Contains (AUTH_CHECK);
        }
    }
}
