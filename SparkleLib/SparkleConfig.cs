//   SparkleShare, an instant update workflow to Git.
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
using System.Xml;

namespace SparkleLib {

    public class SparkleConfig : XmlDocument {

        public static SparkleConfig DefaultConfig = new SparkleConfig (
            System.IO.Path.Combine (SparklePaths.SparkleConfigPath, "config.xml"));

        public string Path;


        public SparkleConfig (string path)
        {
            Path = path;
            Load (Path);
        }


        public string UserName {
            get {
                XmlNode node = SelectSingleNode ("//user/name/text()");
                return node.Value;
            }

            set {
                XmlNode node = SelectSingleNode ("//user/name/text()");
                node.InnerText = value;

                Save (Path);
            }
        }


        public string UserEmail {
            get {
                XmlNode node = SelectSingleNode ("//user/name/email()");
                return node.Value;
            }

            set {
                XmlNode node = SelectSingleNode ("//user/name/email()");
                node.InnerText = value;

                Save (Path);
            }
        }


        public void AddFolder (string name, SparkleBackend backend)
        {
            XmlNode node_folder = CreateNode (XmlNodeType.Element, "folder", null);

            XmlNode node_name = CreateElement ("name");
            node_name.InnerText = name;

            XmlNode node_backend = CreateElement ("backend");
            node_backend.InnerText = backend.Name;

            node_folder.AppendChild (node_name);
            node_folder.AppendChild (node_backend);

            XmlNode node_root = SelectSingleNode ("/");
            node_root.AppendChild (node_folder);

            Save (Path);
        }


        public void RemoveFolder (string name)
        {
            foreach (XmlNode node_folder in SelectNodes ("//folder")) {
                if (node_folder ["name"].InnerText.Equals (name))
                    SelectSingleNode ("/").RemoveChild (node_folder);
            }

            Save (Path);
        }
    }
}
