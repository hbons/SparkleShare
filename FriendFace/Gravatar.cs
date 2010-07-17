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

namespace FriendFace {

	public class Gravatar : Gdk.Pixbuf {

		public Gravatar (string identifier)
		{

			UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);
			string home_path = unix_user_info.HomeDirectory;

			string theme_path = CombineMore (home_path, ".icons", "friendface", "48x48", "status");
			string file_name = "avatar-gravatar-" + identifier;
			string file_path = CombineMore (theme_path, file_name);

			if (!Directory.Exists (theme_path))
				Directory.CreateDirectory (theme_path);

			WebClient WebClient = new WebClient ();
			Uri icon_uri = new Uri ("http://www.gravatar.com/avatar/" + MD5 (identifier) + ".jpg?s=48&d=404");

			if (File.Exists (file_path))
				File.Delete (file_path);

			WebClient.DownloadFileAsync (icon_uri, file_path);

			WebClient.DownloadFileCompleted += delegate {

				FileInfo file_info = new FileInfo (file_path);

				if (file_info.Length < 256)
					File.Delete (file_path);

			};

		}

	}


	// Creates an MD5 hash of input
	public static string MD5 (string s)
	{
		MD5 md5 = new MD5CryptoServiceProvider ();
		Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
		Byte[] encoded_bytes = md5.ComputeHash (bytes);
		return BitConverter.ToString (encoded_bytes).ToLower ().Replace ("-", "");
	}


	// Makes it possible to combine more than
	// two paths at once
	public static string CombineMore (params string [] parts)
	{
		string new_path = "";
		foreach (string part in parts)
			new_path = Path.Combine (new_path, part);
		return new_path;
	}

}
