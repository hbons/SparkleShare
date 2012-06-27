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

        public static SparkleConfig DefaultConfig = new SparkleConfig (default_config_path, "config.xml");
        public static bool DebugMode = true;

        public string FullPath;
        public string TmpPath;
        public string LogFilePath;


        public string HomePath {
            get {
                if (GetConfigOption ("home_path") != null)
                    return GetConfigOption ("home_path");
                else if (SparkleHelpers.IsWindows)
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
            try {
                Environment.SpecialFolder folder =
                    (Environment.SpecialFolder) Enum.Parse(
                                typeof(Environment.SpecialFolder), "UserProfile");

                string old_path = Path.Combine (
                    Environment.GetFolderPath (Environment.SpecialFolder.Personal), "SparkleShare");

                if (Directory.Exists (old_path) &&
                    Environment.OSVersion.Platform == PlatformID.Win32NT) {

                    string new_path = Path.Combine (Environment.GetFolderPath (folder), "SparkleShare");
                    Directory.Move (old_path, new_path);

                    Console.WriteLine ("Migrated SparkleShare folder to %USERPROFILE%");
                }

            } catch (Exception e) {
                Console.WriteLine ("Failed to migrate: " + e.Message);
                // TODO: Remove this block when most people have migrated to the new path
            }

            FullPath    = Path.Combine (config_path, config_file_name);
            LogFilePath = Path.Combine (config_path, "debug-log.txt");

            if (File.Exists (LogFilePath)) {
                try {
                    File.Delete (LogFilePath);

                } catch (Exception) {
                    // Don't delete the debug.log if, for example, 'tail' is reading it
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
            }
        }


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


        private XmlNode GetFolder (string name)
        {
            return SelectSingleNode (string.Format ("/sparkleshare/folder[name=\"{0}\"]", name));
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
    }


    public class ConfigFileNotFoundException : Exception {

        public ConfigFileNotFoundException (string message) : base (message)
        {
        }
    }
}

