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
using System.Xml;

namespace SparkleLib {

    public class SparkleConfig : XmlDocument {

        public static SparkleConfig DefaultConfig;
        public static bool DebugMode = true;

        public string FullPath;
        public string TmpPath;
        public string LogFilePath;


        public string HomePath {
            get {
                if (SparkleBackend.Platform == PlatformID.Win32NT)
                    return Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
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
            LogFilePath = Path.Combine (config_path, "debug_log.txt");

            if (File.Exists (LogFilePath)) {
                try {
                    File.Delete (LogFilePath);

                } catch (Exception) {
                    // Don't delete the debug.log if, for example, 'tail' is reading it
                }
            }

            if (!Directory.Exists (config_path))
                Directory.CreateDirectory (config_path);

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
                Directory.CreateDirectory (TmpPath);
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
                XmlNode name_node  = SelectSingleNode ("/sparkleshare/user/name/text()");
                XmlNode email_node = SelectSingleNode ("/sparkleshare/user/email/text()");
                string user_name   = name_node.Value;
                string user_email  = email_node.Value;

                SparkleUser user = new SparkleUser (user_name, user_email);

                string [] private_key_file_paths = Directory.GetFiles (Path.GetDirectoryName (FullPath), "*.key");
                
                if (private_key_file_paths.Length > 0) {
                    user.PrivateKey         = File.ReadAllText (private_key_file_paths [0]);
                    user.PrivateKeyFilePath = private_key_file_paths [0];

                    user.PublicKey         = File.ReadAllText (private_key_file_paths [0] + ".pub");
                    user.PublicKeyFilePath = private_key_file_paths [0] + ".pub";
                }

                return user;
            }

            set {
                SparkleUser user = (SparkleUser) value;

                XmlNode name_node    = SelectSingleNode ("/sparkleshare/user/name/text()");
                XmlNode email_node   = SelectSingleNode ("/sparkleshare/user/email/text()");
                name_node.InnerText  = user.Name;
                email_node.InnerText = user.Email;

                Save ();
            }
        }


        public List<string> Folders {
            get {
                List<string> folders = new List<string> ();

                foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder"))
                    folders.Add (node_folder ["name"].InnerText);

                folders.Sort ();

                return folders;
            }
        }


        public void AddFolder (string name, string identifier, string url, string backend)
        {
            XmlNode node_name       = CreateElement ("name");
            XmlNode node_identifier = CreateElement ("identifier");
            XmlNode node_url        = CreateElement ("url");
            XmlNode node_backend    = CreateElement ("backend");

            node_name.InnerText       = name;
            node_identifier.InnerText = identifier;
            node_url.InnerText        = url;
            node_backend.InnerText    = backend;

            XmlNode node_folder = CreateNode (XmlNodeType.Element, "folder", null);

            node_folder.AppendChild (node_name);
            node_folder.AppendChild (node_identifier);
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


        public void RenameFolder (string identifier, string name)
        {
            XmlNode node_folder = SelectSingleNode (
                string.Format ("/sparkleshare/folder[identifier=\"{0}\"]", identifier));

            node_folder ["name"].InnerText = name;
            Save ();
        }


        public string GetBackendForFolder (string name)
        {
            return GetFolderValue (name, "backend");
        }


        public string GetIdentifierForFolder (string name)
        {
            return GetFolderValue (name, "identifier");
        }


        public string GetUrlForFolder (string name)
        {
            return GetFolderValue (name, "url");
        }


        public bool IdentifierExists (string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException ();

            foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder")) {
                XmlElement folder_id = node_folder ["identifier"];

                if (folder_id != null && identifier.Equals (folder_id.InnerText))
                    return true;
            }

            return false;
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

            Save ();
            SparkleLogger.LogInfo ("Config", "Updated option " + name + ":" + content);
        }


        private XmlNode GetFolder (string name)
        {
            return SelectSingleNode (string.Format ("/sparkleshare/folder[name=\"{0}\"]", name));
        }
        
        
        private string GetFolderValue (string name, string key)
        {
            XmlNode folder = GetFolder(name);
            
            if ((folder != null) && (folder [key] != null))
                return folder [key].InnerText;
            else
                return null;
        }


        private void Save ()
        {
            if (!File.Exists (FullPath))
                throw new FileNotFoundException (FullPath + " does not exist");

            Save (FullPath);
            SparkleLogger.LogInfo ("Config", "Wrote to '" + FullPath + "'");
        }
    }
}
