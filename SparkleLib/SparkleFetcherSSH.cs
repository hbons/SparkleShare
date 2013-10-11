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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace SparkleLib {

    public abstract class SparkleFetcherSSH : SparkleFetcherBase {

        public SparkleFetcherSSH (SparkleFetcherInfo info) : base (info)
        {
        }


        public override bool Fetch ()
        {
            if (RemoteUrl.Host.EndsWith (".onion")) {
                // Tor has special domain names called ".onion addresses".  They can only be
                // resolved by using a proxy via tor. While the rest of the openssh suite
                // fully supports proxying, ssh-keyscan does not, so we can't use it for .onion
                SparkleLogger.LogInfo ("Auth", "using tor .onion address skipping ssh-keyscan");

            } else if (!RemoteUrl.Scheme.StartsWith ("http")) {
                string host_key = FetchHostKey ();
                
                if (string.IsNullOrEmpty (RemoteUrl.Host) || host_key == null) {
                    SparkleLogger.LogInfo ("Auth", "Could not fetch host key");
                    this.errors.Add ("error: Could not fetch host key");
                    
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
                        SparkleLogger.LogInfo ("Auth", "Unable to derive fingerprint: ", e);
                        this.errors.Add ("error: Can't check fingerprint due to FIPS being enabled");
                        
                        return false;
                    }

                    if (host_fingerprint == null || !RequiredFingerprint.Equals (host_fingerprint)) {
                        SparkleLogger.LogInfo ("Auth", "Fingerprint doesn't match");
                        this.errors.Add ("error: Host fingerprint doesn't match");
                        
                        return false;
                    }
                    
                    warn = false;
                    SparkleLogger.LogInfo ("Auth", "Fingerprint matches");
                    
                } else {
                    SparkleLogger.LogInfo ("Auth", "Skipping fingerprint check");
                }
                
                AcceptHostKey (host_key, warn);
            }
            
            return true;
        }


        private string FetchHostKey ()
        {
            SparkleLogger.LogInfo ("Auth", "Fetching host key for " + RemoteUrl.Host);
            
            Process process = new Process ();
            process.StartInfo.FileName               = "ssh-keyscan";
            process.StartInfo.WorkingDirectory       = SparkleConfig.DefaultConfig.TmpPath;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow         = true;
            process.EnableRaisingEvents              = true;
            
            string [] key_types = {"rsa", "dsa", "ecdsa"};
            
            foreach (string key_type in key_types) {
                if (RemoteUrl.Port < 1)
                    process.StartInfo.Arguments = "-t " + key_type + " -p 22 " + RemoteUrl.Host;
                else
                    process.StartInfo.Arguments = "-t " + key_type + " -p " + RemoteUrl.Port + " " + RemoteUrl.Host;
                
                SparkleLogger.LogInfo ("Cmd", process.StartInfo.FileName + " " + process.StartInfo.Arguments);
                
                process.Start ();
                string host_key = process.StandardOutput.ReadToEnd ().Trim ();
                process.WaitForExit ();
                
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace (host_key))
                    return host_key;
            }
            
            return null;
        }
        
        
        private string DeriveFingerprint (string public_key)
        {
            try {
                MD5 md5            = new MD5CryptoServiceProvider ();
                string key         = public_key.Split (" ".ToCharArray ()) [2];
                byte [] b64_bytes  = Convert.FromBase64String (key);
                byte [] md5_bytes  = md5.ComputeHash (b64_bytes);
                string fingerprint = BitConverter.ToString (md5_bytes);
                
                return fingerprint.ToLower ().Replace ("-", ":");
                
            } catch (Exception e) {
                SparkleLogger.LogInfo ("Fetcher", "Failed creating fingerprint: " + e.Message + " " + e.StackTrace);
                return null;
            }
        }
        
        
        private void AcceptHostKey (string host_key, bool warn)
        {
            string ssh_config_path       = Path.Combine (SparkleConfig.DefaultConfig.HomePath, ".ssh");
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
                if (line.StartsWith (host + " "))
                    return;
            }
            
            if (known_hosts.EndsWith ("\n"))
                File.AppendAllText (known_hosts_file_path, host_key + "\n");
            else
                File.AppendAllText (known_hosts_file_path, "\n" + host_key + "\n");
            
            SparkleLogger.LogInfo ("Auth", "Accepted host key for " + host);
            
            if (warn)
                this.warnings.Add ("The following host key has been accepted:\n" + DeriveFingerprint (host_key));
        }
    }
}
