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
using System.IO;
using System.Xml;

namespace SparkleShare {

    public class SparklePlugin {

        public string Name;
        public string Description;
        public string ImagePath;
        public string Backend;

        public string Address;
        public string AddressExample;
        public string Path;
        public string PathExample;


        public SparklePlugin (string plugin_path)
        {
            string plugin_directory = System.IO.Path.GetDirectoryName (plugin_path);

            XmlDocument xml = new XmlDocument ();
            xml.Load (plugin_path);

            XmlNode node;

            node = xml.SelectSingleNode ("/sparkleshare/plugin/info/name/text()");
            if (node != null) { Name = node.Value; }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/info/description/text()");
            if (node != null) { Description = node.Value; }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/info/icon/text()");
            if (node != null) { ImagePath = System.IO.Path.Combine (plugin_directory, node.Value); }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/info/backend/text()");
            if (node != null) { Backend = node.Value; }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/address/value/text()");
            if (node != null) { Address = node.Value; }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/address/example/text()");
            if (node != null) { AddressExample = node.Value; }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/path/value/text()");
            if (node != null) { Path = node.Value; }

            node = xml.SelectSingleNode ("/sparkleshare/plugin/path/example/text()");
            if (node != null) { PathExample = node.Value; }
        }
    }
}
