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
using System.IO;
using System.Security.Cryptography;

namespace Sparkles {
    
    public abstract class SSHFetcher : BaseFetcher {

        protected SSHFetcher (SparkleFetcherInfo info) : base (info)
        {
        }


        public override bool Fetch ()
        {
            // Tor has special domain names called ".onion addresses". They can only be
            // resolved by using a proxy via tor. While the rest of the openssh suite
            // fully supports proxying, ssh-keyscan does not, so we can't use it for .onion
            if (RemoteUrl.Host.EndsWith (".onion", StringComparison.InvariantCultureIgnoreCase)) {
                Logger.LogInfo ("Auth", "using tor .onion address skipping ssh-keyscan");
                return true;
            }

            if (RemoteUrl.Scheme.StartsWith ("http", StringComparison.InvariantCultureIgnoreCase))
                return true;
            
            string host_key = FetchHostKey ();

            if (string.IsNullOrEmpty (RemoteUrl.Host) || host_key == null) {
                Logger.LogInfo ("Auth", "Could not fetch host key");
                errors.Add ("error: Could not fetch host key");

                return false;
            }

            bool warn = true;

            if (RequiredFingerprint != null) {
                string host_fingerprint;

                try {
                    host_fingerprint = DeriveFingerprint (host_key);
                
                } catch (InvalidOperationException e) {
                    // "Unapproved cryptographic algorithms" won't work when FIPS is enabled on Windows.
                    // Software like Cisco AnyConnect can demand this feature is on, so we show an error
                    Logger.LogInfo ("Auth", "Unable to derive fingerprint: ", e);
                    errors.Add ("error: Can't check fingerprint due to FIPS being enabled");
                    
                    return false;
                }

                if (host_fingerprint == null || !RequiredFingerprint.Equals (host_fingerprint)) {
                    Logger.LogInfo ("Auth", "Fingerprint doesn't match");
                    errors.Add ("error: Host fingerprint doesn't match");
                    
                    return false;
                }
                
                warn = false;
                Logger.LogInfo ("Auth", "Fingerprint matches");
                
            } else {
                Logger.LogInfo ("Auth", "Skipping fingerprint check");
            }
            
            AcceptHostKey (host_key, warn);
            
            return true;
        }


        string FetchHostKey ()
        {
            Logger.LogInfo ("Auth", string.Format ("Fetching host key for {0}", RemoteUrl.Host));
            var ssh_keyscan = new Command ("ssh-keyscan", string.Format ("-t rsa -p 22 {0}", RemoteUrl.Host));

            if (RemoteUrl.Port > 0)
                ssh_keyscan.StartInfo.Arguments = string.Format ("-t rsa -p {0} {1}", RemoteUrl.Port, RemoteUrl.Host);

            string host_key = ssh_keyscan.StartAndReadStandardOutput ();

            if (ssh_keyscan.ExitCode == 0 && !string.IsNullOrWhiteSpace (host_key))
                return host_key;

            return null;
        }

        
        string DeriveFingerprint (string public_key)
        {
            try {
                SHA256 sha256 = new SHA256CryptoServiceProvider ();
                string key = public_key.Split (" ".ToCharArray ()) [2];

                byte [] base64_bytes = Convert.FromBase64String (key);
                byte [] sha256_bytes = sha256.ComputeHash (base64_bytes);

                string fingerprint = BitConverter.ToString (sha256_bytes);
                Console.WriteLine( fingerprint.ToLower ().Replace ("-", ":"));
                return fingerprint.ToLower ().Replace ("-", ":");

            } catch (Exception e) {
                Logger.LogInfo ("Fetcher", "Failed to create fingerprint: " + e.Message + " " + e.StackTrace);
                return null;
            }
        }
        
        
        void AcceptHostKey (string host_key, bool warn)
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
            
            Logger.LogInfo ("Auth", "Accepted host key for " + host);
            
            if (warn)
                warnings.Add ("The following host key has been accepted:\n" + DeriveFingerprint (host_key));
        }
    }
}
