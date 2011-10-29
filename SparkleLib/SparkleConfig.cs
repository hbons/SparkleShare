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

using Mono.Unix;

namespace SparkleLib {

    public class SparkleConfig : XmlDocument {

        public static string ConfigPath = Path.Combine (
            Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData),
                "sparkleshare");

        public static SparkleConfig DefaultConfig = new SparkleConfig (ConfigPath, "config.xml");


        public string FullPath;

        public string HomePath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
        public string TmpPath;

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
            FullPath = System.IO.Path.Combine (config_path, config_file_name);
            TmpPath  = Path.Combine (FoldersPath, ".tmp");

            if (!Directory.Exists (config_path)) {
                Directory.CreateDirectory (config_path);
                SparkleHelpers.DebugInfo ("Config", "Created \"" + config_path + "\"");
            }

            string icons_path = System.IO.Path.Combine (config_path, "icons");
            if (!Directory.Exists (icons_path)) {
                Directory.CreateDirectory (icons_path);
                SparkleHelpers.DebugInfo ("Config", "Created \"" + icons_path + "\"");
            }

            try {
              Load (FullPath);
              
            } catch (TypeInitializationException) {
                CreateInitialConfig ();

            } catch (IOException) {
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
            }
        }


        private void CreateInitialConfig ()
        {
            string user_name = "Unknown";

            if (SparkleBackend.Platform == PlatformID.Unix ||
                SparkleBackend.Platform == PlatformID.MacOSX) {

                user_name = new UnixUserInfo (UnixEnvironment.UserName).RealName;
                if (string.IsNullOrEmpty (user_name))
                    user_name = UnixEnvironment.UserName;
                else
                    user_name = user_name.TrimEnd (",".ToCharArray());

            } else {
                user_name = Environment.UserName;
            }

            if (string.IsNullOrEmpty (user_name))
                user_name = "Unknown";

            TextWriter writer = new StreamWriter (FullPath);
            string n          = Environment.NewLine;

            writer.Write ("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + n +
                          "<sparkleshare>" + n +
                          "  <user>" + n +
                          "    <name>" + user_name + "</name>" + n +
                          "    <email>Unknown</email>" + n +
                          "  </user>" + n +
                          "</sparkleshare>");
            writer.Close ();

            SparkleHelpers.DebugInfo ("Config", "Created \"" + FullPath + "\"");
        }


        public SparkleUser User {
            get {
                XmlNode name_node = SelectSingleNode ("/sparkleshare/user/name/text()");
                string name  = name_node.Value;

                XmlNode email_node = SelectSingleNode ("/sparkleshare/user/email/text()");
                string email = email_node.Value;

                return new SparkleUser (name, email);
            }

            set {
                SparkleUser user = (SparkleUser) value;

                XmlNode name_node = SelectSingleNode ("/sparkleshare/user/name/text()");
                name_node.InnerText = user.Name;

                XmlNode email_node = SelectSingleNode ("/sparkleshare/user/email/text()");
                email_node.InnerText = user.Email;

                this.Save ();
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

            this.Save ();
        }


        public void RemoveFolder (string name)
        {
            foreach (XmlNode node_folder in SelectNodes ("/sparkleshare/folder")) {
                if (node_folder ["name"].InnerText.Equals (name))
                    SelectSingleNode ("/sparkleshare").RemoveChild (node_folder);
            }

            this.Save ();
        }


        public bool FolderExists (string name)
        {
            XmlNode folder = this.GetFolder (name);
            return (folder != null);
        }


        public string GetBackendForFolder (string name)
        {
            return this.GetFolderValue (name, "backend");
        }


        public string GetUrlForFolder (string name)
        {
            return this.GetFolderValue (name, "url");
        }


        public bool SetFolderOptionalAttribute (string folder_name, string key, string value)
        {
            XmlNode folder = this.GetFolder (folder_name);

            if (folder == null)
                return false;

            if (folder [key] != null) {
                folder [key].InnerText = value;

            } else {
                XmlNode new_node = CreateElement (key);
                new_node.InnerText = value;
                folder.AppendChild (new_node);
            }

            return true;
        }


        public string GetFolderOptionalAttribute (string folder_name, string key)
        {
            XmlNode folder = this.GetFolder (folder_name);

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
                    Uri uri = new Uri (node_folder ["url"].InnerText);

                    if ("git" != uri.UserInfo && !hosts.Contains (uri.UserInfo + "@" + uri.Host))
                        hosts.Add (uri.UserInfo + "@" + uri.Host);
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
            XmlNode folder = this.GetFolder(name);
            
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
            this.Save ();
        }


        private void Save ()
        {
            if (!File.Exists (FullPath))
                throw new ConfigFileNotFoundException (FullPath + " does not exist");

            this.Save (FullPath);
            SparkleHelpers.DebugInfo ("Config", "Updated \"" + FullPath + "\"");
        }
    }


    public class ConfigFileNotFoundException : Exception {

        public ConfigFileNotFoundException (string message) :
            base (message) { }
    }
}

