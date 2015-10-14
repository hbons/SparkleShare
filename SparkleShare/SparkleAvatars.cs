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
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using SparkleLib;

namespace SparkleShare
{
    public static class SparkleAvatars
    {
        private static List<string> skipped_avatars = new List<string> ();


        public static string GetAvatar (string email, int size, string target_path)
        {
            #if __MonoCS__
            ServicePointManager.ServerCertificateValidationCallback = GetAvatarValidationCallBack;
            #endif

            email = email.ToLower ();
            
            if (skipped_avatars.Contains (email))
                return null;
            
            string avatars_path = new string [] { Path.GetDirectoryName (target_path),
                "avatars", size + "x" + size }.Combine ();

            // Search avatars by file name, ignore extension
            // Delete files over a day old
            // Return first matching file
            if (Directory.Exists (avatars_path)) {
                foreach (string file_path in Directory.GetFiles (avatars_path, email.MD5 () + "*")) {
                    if (new FileInfo (file_path).LastWriteTime < DateTime.Now.AddDays (-1))
                        File.Delete (file_path);
                    else
                        return file_path;
                }
            }

            string avatar_file_path;

            try {
                avatar_file_path = Path.Combine (avatars_path, email.MD5 ());

            } catch (InvalidOperationException e) {
                SparkleLogger.LogInfo ("Avatars", "Error fetching avatar for " + email, e);
                return null;
            }
            
            WebClient client = new WebClient ();
            string url =  "https://gravatar.com/avatar/" + email.MD5 () + ".png?s=" + size + "&d=404";
            
            try {
                byte [] buffer = client.DownloadData (url);

                if (client.ResponseHeaders ["content-type"].Equals (MediaTypeNames.Image.Jpeg, StringComparison.InvariantCultureIgnoreCase)) {
                    avatar_file_path += ".jpg";

                } else if (client.ResponseHeaders ["content-type"].Equals (MediaTypeNames.Image.Gif, StringComparison.InvariantCultureIgnoreCase)) {
                    avatar_file_path += ".gif";
                
                } else {
                    avatar_file_path += ".png";
                }
                
                if (buffer.Length > 255) {
                    if (!Directory.Exists (avatars_path)) {
                        Directory.CreateDirectory (avatars_path);
                        SparkleLogger.LogInfo ("Avatars", "Created '" + avatars_path + "'");
                    }
                    
                    File.WriteAllBytes (avatar_file_path, buffer);
                    SparkleLogger.LogInfo ("Avatars", "Fetched " + size + "x" + size + " avatar for " + email);
                    
                    return avatar_file_path;
                    
                } else {
                    return null;
                }
                
            } catch (Exception e) {
                SparkleLogger.LogInfo ("Avatars", "Error fetching avatar for " + email, e);
                skipped_avatars.Add (email);
                
                return null;
            }
        }


        private static bool GetAvatarValidationCallBack (Object sender,
            X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            X509Certificate2 certificate2 = new X509Certificate2 (certificate.GetRawCertData ());
            
            // On some systems (mostly Linux) we can't assume the needed certificates are
            // available, so we have to check the certificate's SHA-1 fingerprint manually.
            //
			// Obtained from https://www.gravatar.com/ on May 8 2014 and expires on Oct 14 2015.
			string gravatar_cert_fingerprint = "DB3053FD4950A211F8827D7B201E6CA004EBB73F";

            if (!certificate2.Thumbprint.Equals (gravatar_cert_fingerprint)) {
                SparkleLogger.LogInfo ("Avatars", "Invalid certificate for https://www.gravatar.com/");
                return false;
            }
            
            return true;
        }
    }
}
