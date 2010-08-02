//   FriendFace creates an icon theme of buddy icons from the web
//   Copyright (C) 2010  Hylke Bons
//
//   This library is free software; you can redistribute it and/or
//   modify it under the terms of the GNU Lesser General Public
//   License as published by the Free Software Foundation; either
//   version 2.1 of the License, or (at your option) any later version.
//
//   This library is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//   Lesser General Public License for more details.
//
//   You should have received a copy of the GNU Lesser General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.using Gtk;

using Gtk;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FriendFace {

	public class GravatarIconProvider : IconProvider {

		public GravatarIconProvider (string email) : base (email)
		{
			ServiceName = "gravatar";
		}


		new public void RetrieveIcon ()
		{

			string target_file_path = GetTargetFilePath ();

			if (File.Exists (target_file_path))
				return;

			WebClient web_client = new WebClient ();
			Uri icon_uri = new Uri ("http://www.gravatar.com/avatar/" + MD5 (Identifier) + ".jpg?s=48&d=404");

			web_client.DownloadFileAsync (icon_uri, target_file_path);

			web_client.DownloadFileCompleted += delegate {
				base.RetrieveIcon ();					
			};

		}


		// Creates an MD5 hash of input
		public static string MD5 (string s)
		{
			MD5 md5 = new MD5CryptoServiceProvider ();
			Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
			Byte[] encoded_bytes = md5.ComputeHash (bytes);
			return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
		}

	}

}
