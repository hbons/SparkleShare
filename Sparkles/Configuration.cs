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
using System.Collections.Generic;
using System.Xml;

namespace Sparkles {

    public class Configuration : XmlDocument {

        public static Configuration DefaultConfiguration;
        public static bool DebugMode = true;

        public readonly string DirectoryPath;
        public readonly string FilePath;
        public readonly string TmpPath;
        public readonly string BinPath;
        public readonly string LogFilePath;


        public string HomePath {
            get {
                if (InstallationInfo.OperatingSystem == OS.Windows)
                    return Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
                
                return Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            }
        }


        public string FoldersPath {
            get {
                if (GetConfigOption ("folders_path") != null)                      
                    return GetConfigOption ("folders_path");
                
                return Path.Combine (HomePath, "SparkleShare");
            }
        }


        public Configuration (string config_path, string config_file_name)
        {
            FilePath = Path.Combine (config_path, config_file_name);
            DirectoryPath = config_path;

			BinPath = Path.Combine (config_path, "bin");

            if (!Directory.Exists (BinPath))
                Directory.CreateDirectory (BinPath);

            string logs_path = Path.Combine (config_path, "logs");

            int i = 1;
            do {
                LogFilePath = Path.Combine (
                    logs_path, "log_" + DateTime.Now.ToString ("yyyy-MM-dd") + "." +  i + ".txt");

                i++;

            } while (File.Exists (LogFilePath));

            if (!Directory.Exists (logs_path))
                Directory.CreateDirectory (logs_path);

            // Delete logs older than a week
            foreach (FileInfo file in new DirectoryInfo (logs_path).GetFiles ("log*.txt")) {
                if (file.LastWriteTime < DateTime.Now.AddDays (-7))
                    file.Delete ();
            }

            if (!Directory.Exists (config_path))
                Directory.CreateDirectory (config_path);

            try {
                Load (FilePath);

            } catch (TypeInitializationException) {
                CreateInitialConfig ();

            } catch (FileNotFoundException) {
                CreateInitialConfig ();

            } catch (XmlException) {
                var file = new FileInfo (FilePath);

                if (file.Length == 0) {
                    File.Delete (FilePath);
                    CreateInitialConfig ();

                } else {
                    throw new XmlException (FilePath + " does not contain a valid config XML structure.");
                }

            } finally {
                Load (FilePath);
                TmpPath = Path.Combine (DirectoryPath, "tmp");
                Directory.CreateDirectory (TmpPath);
            }
        }


        void CreateInitialConfig ()
        {
            string user_name = Environment.UserName;

            if (InstallationInfo.OperatingSystem != OS.Windows) {
                if (string.IsNullOrEmpty (user_name))
                    user_name = "Unknown";
                else
                    // On Unix systems the user name may have commas appended
                    user_name = user_name.TrimEnd (',');
            }

            // TODO: Don't do this manually
            string n = Environment.NewLine;
            File.WriteAllText (FilePath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + n +
                "<sparkleshare>" + n +
                "  <user>" + n +
                "    <name>" + user_name + "</name>" + n +
                "    <email>Unknown</email>" + n +
                "  </user>" + n +
                "  <notifications>True</notifications>" + n +
                "</sparkleshare>");
        }


        public User User {
            get {
                string name  = SelectSingleNode ("/sparkleshare/user/name/text()").Value;
                string email = SelectSingleNode ("/sparkleshare/user/email/text()").Value;

                return new User (name, email);
            }

            set {
                SelectSingleNode ("/sparkleshare/user/name/text()").InnerText  = value.Name;
                SelectSingleNode ("/sparkleshare/user/email/text()").InnerText = value.Email;

                Save ();
            }
        }


        public List<string> Folders {
            get {
                var folders = new List<string> ();

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


        public void RenameFolder (string identifier, string new_name)
        {
            XmlNode node_folder = SelectSingleNode (
                string.Format ("/sparkleshare/folder[identifier=\"{0}\"]", identifier));

            node_folder ["name"].InnerText = new_name;
            Save ();
        }


        public string BackendByName (string name)
        {
            return FolderValueByKey (name, "backend");
        }


        public string IdentifierByName (string name)
        {
            return FolderValueByKey (name, "identifier");
        }


        public string UrlByName (string name)
        {
            return FolderValueByKey (name, "url");
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
            XmlNode folder = FolderByName (folder_name);

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
            XmlNode folder = FolderByName (folder_name);

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
            Logger.LogInfo ("Config", "Updated option " + name + ":" + content);
        }


        XmlNode FolderByName (string name)
        {
            return SelectSingleNode (string.Format ("/sparkleshare/folder[name=\"{0}\"]", name));
        }
        
        
        string FolderValueByKey (string name, string key)
        {
            XmlNode folder = FolderByName(name);
            
            if ((folder != null) && (folder [key] != null))
                return folder [key].InnerText;
            
            return null;
        }


        void Save ()
        {
            if (!File.Exists (FilePath))
                throw new FileNotFoundException (FilePath + " does not exist");

            Save (FilePath);
            Logger.LogInfo ("Config", "Wrote to '" + FilePath + "'");
        }
    }
}
