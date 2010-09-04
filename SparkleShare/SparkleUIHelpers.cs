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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using SparkleLib;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SparkleShare {
	
	public static class SparkleUIHelpers
	{

		// Creates an MD5 hash of input
		public static string GetMD5 (string s)
		{
			MD5 md5 = new MD5CryptoServiceProvider ();
			Byte[] bytes = ASCIIEncoding.Default.GetBytes (s);
			Byte[] encodedBytes = md5.ComputeHash (bytes);
			return BitConverter.ToString (encodedBytes).ToLower ().Replace ("-", "");
		}


		// Gets the avatar for a specific email address and size
		public static Gdk.Pixbuf GetAvatar (string email, int size)
		{

			string avatar_path = SparkleHelpers.CombineMore (SparklePaths.SparkleLocalIconPath,
				size + "x" + size, "status");

			if (!Directory.Exists (avatar_path)) {

				Directory.CreateDirectory (avatar_path);
				SparkleHelpers.DebugInfo ("Config", "Created '" + avatar_path + "'");

			}

			string avatar_file_path = SparkleHelpers.CombineMore (avatar_path, "avatar-" + email);

			if (File.Exists (avatar_file_path)) {

				return new Gdk.Pixbuf (avatar_file_path);

			} else {

				// Let's try to get the person's gravatar for next time
				WebClient WebClient = new WebClient ();
				Uri uri = new Uri ("http://www.gravatar.com/avatar/" + GetMD5 (email) +
					".jpg?s=" + size + "&d=404");

				string tmp_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath, email + size);

				if (!File.Exists (tmp_file_path)) {

					WebClient.DownloadFileAsync (uri, tmp_file_path);

					WebClient.DownloadFileCompleted += delegate {

						File.Delete (avatar_file_path);
						FileInfo tmp_file_info = new FileInfo (tmp_file_path);

						if (tmp_file_info.Length > 255)
							File.Move (tmp_file_path, avatar_file_path);

					};

				}

				// Fall back to a generic icon if there is no gravatar
				if (File.Exists (avatar_file_path))
					return new Gdk.Pixbuf (avatar_file_path);
				else
					return GetIcon ("avatar-default", size);

			}

		}


		// Looks up an icon from the system's theme
		public static Gdk.Pixbuf GetIcon (string name, int size)
		{

			IconTheme icon_theme = new IconTheme ();
			icon_theme.AppendSearchPath (SparklePaths.SparkleIconPath);
			icon_theme.AppendSearchPath (SparklePaths.SparkleLocalIconPath);

			try {

				return icon_theme.LoadIcon (name, size, IconLookupFlags.GenericFallback);

			} catch {

				try {

					return icon_theme.LoadIcon ("gtk-missing-image", size, IconLookupFlags.GenericFallback);

				} catch {

					return null;

				}

			}

		}

	}

}
