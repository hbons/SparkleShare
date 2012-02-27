//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using System;
using System.Xml;

namespace SparkleShare {

    public class SparklePlugin {

        public string Name {
            get {
                return GetValue ("info", "name");
            }
        }

        public string Description {
            get {
                return GetValue ("info", "description");
            }
        }

        public string ImagePath {
            get {
                return System.IO.Path.Combine (
                    this.plugin_directory,
                    GetValue ("info", "icon")
                );
            }
        }

        public string Backend {
            get {
                return GetValue ("info", "backend");
            }
        }

        public string AnnouncementsUrl {
            get {
                return GetValue ("info", "announcements_url");
            }
        }

        public string Address {
            get {
                return GetValue ("address", "value");
            }
        }

        public string AddressExample {
            get {
                return GetValue ("address", "example");
            }
        }

        public string Path {
            get {
                return GetValue ("path", "value");
            }
        }

        public string PathExample {
            get {
                return GetValue ("path", "example");
            }
        }


        private XmlDocument xml = new XmlDocument ();
        private string plugin_directory;

        public SparklePlugin (string plugin_path)
        {
            this.plugin_directory = System.IO.Path.GetDirectoryName (plugin_path);
            this.xml.Load (plugin_path);
        }


        private string GetValue (string a, string b)
        {
            XmlNode node = this.xml.SelectSingleNode (
                "/sparkleshare/plugin/" + a + "/" + b + "/text()");

            if (node != null)
                return node.Value;
            else
                return null;
        }
    }
}
