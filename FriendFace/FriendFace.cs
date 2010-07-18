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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Mono.Unix;
using System;
using System.IO;

namespace FriendFace {

	public class FaceCollection : IconTheme	{
		
		public bool UseFlickr;
		public bool UseGravatar;
		public bool UseIdentica;
		public bool UseTwitter;
		
		private string Path;

		public FaceCollection ()
		{

			UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);
			string default_theme_path = CombineMore (unix_user_info.HomeDirectory, ".icons");
			new FaceCollection (default_theme_path);

		}

		
		public FaceCollection (string path)
		{
			CustomTheme = "FriendFace";
			SetThemePath (path);
		}


		public void SetThemePath (string path)
		{

			Path = path;

			if (!Directory.Exists (Path))
				Directory.CreateDirectory (Path);

			AppendSearchPath (path);

			Refresh ();

		}


		public string GetThemePath ()
		{
			return Path;
		}


		public Gdk.Pixbuf GetFace (string identifier, int size)
		{
			return LoadIcon ("avatar-default-" + identifier, size, IconLookupFlags.GenericFallback);
		}


		public void Refresh ()
		{foreach (string i in SearchPath) {Console.WriteLine (i);}

			IconProvider provider = new IconProvider ("");
			string folder = provider.GetTargetFolderPath ();
			string [] files = Directory.GetFiles (folder);
			
			int [] sizes = {16, 24, 32, 48};

			foreach (string file_path in files) {

				Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (file_path);
				
				FileInfo file_info = new FileInfo (file_path);

				// Delete the icon if it turns out to be empty
				if (file_info.Length == 0) {
					File.Delete (file_path);
					Console.WriteLine ("Deleted: " + file_path);
				}

				for (int i = 0; i < 4; i++) {
				
					int size = sizes [i];

					Gdk.Pixbuf pixbuf_copy = pixbuf.Copy ();

					if (pixbuf.Width != size || pixbuf.Height != size)
						pixbuf_copy = pixbuf.ScaleSimple (size, size, Gdk.InterpType.Hyper);

					string size_folder_path = CombineMore (Path, size + "x" + size, "status");
					string size_file_path = CombineMore (size_folder_path, System.IO.Path.GetFileName (file_path));
					
					Directory.CreateDirectory (size_folder_path);

					if (File.Exists (size_file_path))
						File.Delete (size_file_path);

					pixbuf_copy.Save (size_file_path, "png");

				}
	
				File.Delete (file_path);

			}

			RescanIfNeeded ();

		}


		public bool AddFace (string identifier)
		{

			if (UseGravatar) {
				GravatarIconProvider provider = new GravatarIconProvider (identifier);
				provider.RetrieveIcon ();
			}

			return true;
	
		}


		// Makes it possible to combine more than
		// two paths at once
		private string CombineMore (params string [] parts)
		{
			string new_path = "";
			foreach (string part in parts)
				new_path = System.IO.Path.Combine (new_path, part);
			return new_path;
		}

	}

}
