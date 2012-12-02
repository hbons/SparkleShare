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

using IO = System.IO;

namespace SparkleShare {

    public class SparklePlugin : XmlDocument {

        public static string PluginsPath = "";

        public static string LocalPluginsPath = IO.Path.Combine (
            Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "sparkleshare", "plugins");

        new public string Name { get { return GetValue ("info", "name"); } }
        public string Description { get { return GetValue ("info", "description"); } }
        public string Backend { get { return GetValue ("info", "backend"); } }
        public string Fingerprint { get { return GetValue ("info", "fingerprint"); } }
        public string AnnouncementsUrl { get { return GetValue ("info", "announcements_url"); } }
        public string Address { get { return GetValue ("address", "value"); } }
        public string AddressExample { get { return GetValue ("address", "example"); } }
        public string Path { get { return GetValue ("path", "value"); } }
        public string PathExample { get { return GetValue ("path", "example"); } }

        public string ImagePath {
            get {
                string image_file_name = GetValue ("info", "icon");
                string image_path      = IO.Path.Combine (this.plugin_directory, image_file_name);

                if (IO.File.Exists (image_path))
                    return image_path;
                else
                    return IO.Path.Combine (PluginsPath, image_file_name);
            }
        }
        
        public bool PathUsesLowerCase {
            get {
                string uses_lower_case = GetValue ("path", "uses_lower_case");
                
                if (!string.IsNullOrEmpty (uses_lower_case))
                    return uses_lower_case.Equals (bool.TrueString);
                else
                    return false;
            }
        }

        private string plugin_directory;


        public SparklePlugin (string plugin_path)
        {
            this.plugin_directory = System.IO.Path.GetDirectoryName (plugin_path);
            Load (plugin_path);
        }


        public static SparklePlugin Create (string name, string description, string address_value,
            string address_example, string path_value, string path_example)
        {
            string plugin_path = System.IO.Path.Combine (LocalPluginsPath, name + ".xml");

            if (IO.File.Exists (plugin_path))
                return null;

            string plugin_xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<sparkleshare>" +
                "  <plugin>" +
                "    <info>" +
                "        <name>" + name + "</name>" +
                "        <description>" + description + "</description>" +
                "        <icon>own-server.png</icon>" +
                "    </info>" +
                "    <address>" +
                "      <value>" + address_value + "</value>" +
                "      <example>" + address_example + "</example>" +
                "    </address>" +
                "    <path>" +
                "      <value>" + path_value + "</value>" +
                "      <example>" + path_example + "</example>" +
                "    </path>" +
                "  </plugin>" +
                "</sparkleshare>";

            plugin_xml = plugin_xml.Replace ("<value></value>", "<value/>");
            plugin_xml = plugin_xml.Replace ("<example></example>", "<example/>");

            if (!IO.Directory.Exists (LocalPluginsPath))
                IO.Directory.CreateDirectory (LocalPluginsPath);

            IO.File.WriteAllText (plugin_path, plugin_xml);
            return new SparklePlugin (plugin_path);
        }


        private string GetValue (string a, string b)
        {
            XmlNode node = SelectSingleNode ("/sparkleshare/plugin/" + a + "/" + b + "/text()");

            if (node != null && !string.IsNullOrEmpty (node.Value))
                return node.Value;
            else
                return null;
        }
    }
}