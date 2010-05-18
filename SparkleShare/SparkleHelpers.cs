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
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SparkleShare {
	
	public static class SparkleHelpers {

		public static Gdk.Pixbuf GetAvatar (string Email, int Size) {

			string AvatarPath = Path.Combine (SparklePaths.SparkleAvatarPath, 
			                    Size + "x" + Size);

			if (!Directory.Exists (AvatarPath)) {
				Directory.CreateDirectory (AvatarPath);
				Console.WriteLine ("[Config] Created '" + AvatarPath + "'");
			}
			
			string AvatarFilePath = AvatarPath + Email;

			if (File.Exists (AvatarFilePath))
				return new Gdk.Pixbuf (AvatarFilePath);
			else {

				// Let's try to get the person's gravatar for next time
				WebClient WebClient = new WebClient ();
				Uri GravatarUri = new Uri ("http://www.gravatar.com/avatar/" + 
				                   GetMD5 (Email) + ".jpg?s=" + Size + "&d=404");

				string TmpFile = SparklePaths.SparkleTmpPath + Email + Size;

				if (!File.Exists (TmpFile)) {

					WebClient.DownloadFileAsync (GravatarUri, TmpFile);
					WebClient.DownloadFileCompleted += delegate {
						File.Delete (AvatarPath + Email);
						FileInfo TmpFileInfo = new FileInfo (TmpFile);
						if (TmpFileInfo.Length > 255)
							File.Move (TmpFile, AvatarPath + Email);
					};

				}

				if (File.Exists (AvatarPath + Email))
					return new Gdk.Pixbuf (AvatarPath + Email);
				else
					return GetIcon ("avatar-default", Size);

			}

		}

		// Creates an MD5 hash
		public static string GetMD5 (string s) {
		  MD5 md5 = new MD5CryptoServiceProvider ();
		  Byte[] Bytes = ASCIIEncoding.Default.GetBytes (s);
		  Byte[] EncodedBytes = md5.ComputeHash (Bytes);
		  return BitConverter.ToString(EncodedBytes).ToLower ().Replace ("-", "");
		}
		
		// Makes it possible to combine more than
		// two paths at once.
		public static string CombineMore (params string [] Parts) {
			string NewPath = " ";
			foreach (string Part in Parts)
				NewPath = Path.Combine (NewPath, Part);
			return NewPath;
		}

		public static IconTheme SparkleTheme = new IconTheme ();

		// Looks up an icon from the system's theme
		public static Gdk.Pixbuf GetIcon (string Name, int Size) {
			SparkleTheme.AppendSearchPath (SparklePaths.SparkleInstallPath);
			return SparkleTheme.LoadIcon (Name, Size,
			                              IconLookupFlags.GenericFallback);
		}

		public static bool IsGitUrl (string Url) {
			return Regex.Match (Url, @"[a-z]+://(.)+").Success;
		}

	}

}
