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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Principal;
using System.Xml;

namespace SparkleLib {

    public class SparkleConfig : XmlDocument {

        private static string default_config_path = Path.Combine (
            Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
                "sparkleshare");

        // TODO: declare elsewhere
        public static SparkleConfig DefaultConfig = new SparkleConfig (default_config_path, "config.xml");
        public static bool DebugMode = true;


        public string FullPath;
        public string TmpPath;
        public string LogFilePath;

        public string HomePath {
            get {
                if (GetConfigOption ("home_path") != null)
                    return GetConfigOption ("home_path");
                else
                    return Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            }
        }

        public string FoldersPath {
            get {
                if (GetConfigOption ("folders_path") != null)
                    return GetConfigOption ("folders_path");
                else
                    return Path.Combine (HomePath, "SparkleShare");
            }
        }

        public SparkleConfig (string config_path, string config_file_name)
        {
            FullPath    = Path.Combine (config_path, config_file_name);
            LogFilePath = Path.Combine (config_path, "debug.log");

            if (File.Exists (LogFilePath)) {
                try {
                    File.Delete (LogFilePath);

                } catch (Exception) {
                    // Don't delete the debug.log if 'tail' is reading it
                }
            }

            if (!Directory.Exists (config_path))
                Directory.CreateDirectory (config_path);

            string icons_path = Path.Combine (config_path, "icons");
            if (!Directory.Exists (icons_path))
                Directory.CreateDirectory (icons_path);

            try {
              Load (FullPath);
              
            } catch (TypeInitializationException) {
                CreateInitialConfig ();

            } catch (FileNotFoundException) {
                CreateInitialConfig ();

            } catch (XmlException) {
            
                FileInfo file = new FileInfo (FullPath);
                
                if (file.Length == 0) {
                    File.Delete (FullPath);
                    CreateInitialConfig ();

                } else {
                    throw new XmlException (FullPath + " does not contain a valid config XML structure.");
                }

            } finally {
                Load (FullPath);
                TmpPath = Path.Combine (FoldersPath, ".tmp");
            }
        }


        private void CreateInitialConfig ()
        {
            string user_name = "Unknown";

            if (SparkleBackend.Platform == PlatformID.Unix ||
                SparkleBackend.Platform == PlatformID.MacOSX) {

                user_name = Environment.UserName;
                if (string.IsNullOrEmpty (user_name))
                    user_name = "";
                else
                    user_name = user_name.TrimEnd (",".ToCharArray ());

            } else {
                user_name = Environment.UserName;
            }

            if (string.IsNullOrEmpty (user_name))
                user_name = "Unknown";

            string n = Environment.NewLine;
            File.WriteAllText (FullPath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + n +
                "<sparkleshare>" + n +
                "  <user>" + n +
                "    <name>" + user_name + "</name>" + n +
                "    <email>Unknown</email>" + n +
                "  </user>" + n +
                "</sparkleshare>");
        }


        public SparkleUser User {
            get {
                XmlNode name_node = SelectSingleNode ("/sparkleshare/user/name/text()");
                string name  = name_node.Value;

                XmlNode email_node = SelectSingleNode ("/sparkleshare/user/email/text()");
                string email = email_node.Value;

                string pubkey_file_path = Path.Combine (
                    Path.GetDirectoryName (FullPath),
                    "sparkleshare." + email + ".key.pub"
                );

                SparkleUser user = new SparkleUser (name, email);
                
                if (File.Exists (pubkey_file_path))
                    user.PublicKey = File.ReadAllText (pubkey_file_path);

                return user;
            }

            set {
                SparkleUser user = (SparkleUser) value;

                XmlNode name_node = SelectSingleNode ("/sparkleshare/user/name/text()");
                name_node.InnerText = user.Name;

                XmlNode email_node = SelectSingleNode ("/sparkleshare/user/email/text()");
                email_node.InnerText = user.Email;

                Save ();

                // ConfigureSSH ();
            }
        }

/*
        private void ConfigureSSH ()
        {
            if (User.Email.Equals ("Unknown"))
                return;

            string path = Environment.GetFolderPath (Environment.SpecialFolder.Personal);

            if (!(SparkleBackend.Platform == PlatformID.Unix ||
                  SparkleBackend.Platform == PlatformID.MacOSX)) {

                path = Environment.ExpandEnvironmentVariables ("%HOMEDRIVE%%HOMEPATH%");
            }

            string ssh_config_path      = Path.Combine (path, ".ssh");
            string ssh_config_file_path = SparkleHelpers.CombineMore (path, ".ssh", "config");
            string ssh_config           = "IdentityFile " +
                Path.Combine (SparkleConfig.ConfigPath, "sparkleshare." + User.Email + ".key");

            if (!Directory.Exists (ssh_config_path))
                Directory.CreateDirectory (ssh_config_path);

            if (File.Exists (ssh_config_file_path)) {
                string current_config = File.ReadAllText (ssh_config_file_path);
                if (current_config.Contains (ssh_config))
                    return;

                if (current_config.EndsWith ("\n\n"))
                    ssh_config = "# SparkleShare's key\n" + ssh_config;
                else if (current_config.EndsWith ("\n"))
                    ssh_config = "\n# SparkleShare's key\n" + ssh_config;
                else
                    ssh_config = "\n\n# SparkleShare's key\n" + ssh_config;

                TextWriter writer = File.AppendText (ssh_config_file_path);
                writer.Write (ssh_config + "\n");
                writer.Close ();

            } else {
                File.WriteAllText (ssh_config_file_path, ssh_config);
            }

            Chmod644 (ssh_config_file_path);

            SparkleHelpers.DebugInfo ("Config", "Added key to " + ssh_config_file_path);
        }
*/

        public List<string> Folders {
            get {
                List<string> folders = new List<string> ();

                foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder"))
                    folders.Add (node_folder ["name"].InnerText);

                return folders;
            }
        }


        public void AddFolder (string name, string url, string backend)
        {
            XmlNode node_name = CreateElement ("name");
            node_name.InnerText = name;

            XmlNode node_url = CreateElement ("url");
            node_url.InnerText = url;

            XmlNode node_backend = CreateElement ("backend");
            node_backend.InnerText = backend;

            XmlNode node_folder = CreateNode (XmlNodeType.Element, "folder", null);
            node_folder.AppendChild (node_name);
            node_folder.AppendChild (node_url);
            node_folder.AppendChild (node_backend);

            XmlNode node_root = SelectSingleNode ("/sparkleshare");
            node_root.AppendChild (node_folder);

            Save ();
        }


        public void RemoveFolder (string name)
        {
            foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder")) {
                if (node_folder ["name"].InnerText.Equals (name))
                    SelectSingleNode ("/sparkleshare").RemoveChild (node_folder);
            }

            Save ();
        }


        public bool FolderExists (string name)
        {
            XmlNode folder = GetFolder (name);
            return (folder != null);
        }


        public string GetBackendForFolder (string name)
        {
            return GetFolderValue (name, "backend");
        }


        public string GetUrlForFolder (string name)
        {
            return GetFolderValue (name, "url");
        }


        public bool SetFolderOptionalAttribute (string folder_name, string key, string value)
        {
            XmlNode folder = GetFolder (folder_name);

            if (folder == null)
                return false;

            if (folder [key] != null) {
                folder [key].InnerText = value;

            } else {
                XmlNode new_node = CreateElement (key);
                new_node.InnerText = value;
                folder.AppendChild (new_node);
            }

            Save ();
            return true;
        }


        public string GetFolderOptionalAttribute (string folder_name, string key)
        {
            XmlNode folder = GetFolder (folder_name);

            if (folder != null) {
                if (folder [key] != null)
                    return folder [key].InnerText;
                else
                    return null;

            } else {
                return null;
            }
        }


        public List<string> Hosts {
            get {
                List<string> hosts = new List<string> ();

                foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder")) {
                    Uri uri = new Uri (node_folder ["url"].InnerText);

                    if (!hosts.Contains (uri.Host))
                        hosts.Add (uri.Host);
                }

              return hosts;
           }
        }


        public List<string> HostsWithUsername {
            get {
                List<string> hosts = new List<string> ();

                foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder")) {
                    try {
                        Uri uri = new Uri (node_folder ["url"].InnerText);

                        if (uri.UserInfo != "git" && !hosts.Contains (uri.UserInfo + "@" + uri.Host))
                            hosts.Add (uri.UserInfo + "@" + uri.Host);

                    } catch (UriFormatException) {
                        SparkleHelpers.DebugInfo ("Config",
                            "Ignoring badly formatted URI: " + node_folder ["url"].InnerText);
                    }
                }

              return hosts;
           }
        }


        private XmlNode GetFolder (string name)
        {
            return SelectSingleNode (String.Format("/sparkleshare/folder[name='{0}']", name));
        }


        private string GetFolderValue (string name, string key)
        {
            XmlNode folder = GetFolder(name);
            
            if ((folder != null) && (folder [key] != null)) {
                return folder [key].InnerText;
            }

            return null;
        }


        public string GetConfigOption (string name)
        {
            XmlNode node = SelectSingleNode ("/sparkleshare/" + name);

            if (node != null)
                return node.InnerText;
            else
                return null;
        }


        public void SetConfigOption (string name, string content)
        {
            XmlNode node = SelectSingleNode ("/sparkleshare/" + name);

            if (node != null) {
                node.InnerText = content;

            } else {
                node           = CreateElement (name);
                node.InnerText = content;

                XmlNode node_root = SelectSingleNode ("/sparkleshare");
                node_root.AppendChild (node);
            }

            SparkleHelpers.DebugInfo ("Config", "Updated " + name + ":" + content);
            Save ();
        }


        private void Save ()
        {
            if (!File.Exists (FullPath))
                throw new ConfigFileNotFoundException (FullPath + " does not exist");

            Save (FullPath);
            SparkleHelpers.DebugInfo ("Config", "Updated \"" + FullPath + "\"");
        }


        private void Chmod644 (string file_path)
        {
            // Hack to be able to set the permissions on a file
            // that OpenSSH still likes without resorting to Mono.Unix
            FileInfo file_info   = new FileInfo (file_path);
            file_info.Attributes = FileAttributes.ReadOnly;
            file_info.Attributes = FileAttributes.Normal;
        }
    }


    public class ConfigFileNotFoundException : Exception {

        public ConfigFileNotFoundException (string message) :
            base (message) { }
    }
}

