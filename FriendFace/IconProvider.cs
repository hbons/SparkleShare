//   IconProvider is a base class that can be extended to pull buddy
//   icons from sources so that they can be used by FaceCollection.
//
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

namespace FriendFace {

	public class IconProvider {
	
		public string Identifier;
		public string ServiceName;

		private string TargetFolderPath;
		private string TargetFilePath;

		public IconProvider (string identifier)
		{

			Identifier = identifier;

			string XDG_CACHE_HOME = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");

			if (XDG_CACHE_HOME != null)
				SetTargetFolderPath (CombineMore (XDG_CACHE_HOME, "friendface"));
			else
				SetTargetFolderPath (CombineMore (System.IO.Path.DirectorySeparatorChar.ToString (),
					"tmp", "friendface"));

			string file_name = "avatar-default-" + Identifier;
			TargetFilePath = CombineMore (TargetFolderPath, file_name);

		}


		public void RetrieveIcon ()
		{

			if (File.Exists (TargetFilePath)) {

				FileInfo file_info = new FileInfo (TargetFilePath);

				// Delete the icon if it turns out to be empty
				if (file_info.Length == 0) {
					File.Delete (TargetFilePath);
					Console.WriteLine ("Deleted: " + TargetFilePath);
				}

			}

		}


		public void SetTargetFolderPath (string path)
		{

			TargetFolderPath = path;

			if (!Directory.Exists (TargetFolderPath))
				Directory.CreateDirectory (TargetFolderPath);

		}


		public string GetTargetFolderPath ()
		{
			return TargetFolderPath;
		}


		public void SetTargetFilePath (string path)
		{
			TargetFilePath = path;
		}


		public string GetTargetFilePath ()
		{
			return TargetFilePath;
		}


		// Makes it possible to combine more than two paths at once
		public string CombineMore (params string [] parts)
		{
			string new_path = "";
			foreach (string part in parts)
				new_path = Path.Combine (new_path, part);
			return new_path;
		}

	}

}
