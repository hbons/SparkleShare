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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Xml;

namespace SparkleShare {

	class SparkleInvitation {

		public string Server;
		public string Repository;
		public string Key;


		public SparkleInvitation (string file_path)
		{

			if (!File.Exists (file_path))
				return;

			XmlDocument xml_doc = new XmlDocument (); 
			xml_doc.Load (file_path);

			XmlNodeList server_xml     = xml_doc.GetElementsByTagName ("server");
			XmlNodeList repository_xml = xml_doc.GetElementsByTagName ("repository");
			XmlNodeList key_xml        = xml_doc.GetElementsByTagName ("key");

			Server     = server_xml [0].InnerText;
			Repository = repository_xml [0].InnerText;
			Key        = key_xml [0].InnerText;

		}


		public void Activate ()
		{

			string url = "http://" + Server + "/repo=" + Repository + "&key=" + Key;
			Console.WriteLine (url);

		}

	}
	
}
