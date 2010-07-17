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
using System;

namespace FriendFace
{

	public class FaceCollection : IconTheme
	{
		
		private string Path;
		
		public bool UseTwitter;
		public bool UseGravatar;
		public bool UseIdentica;
		public bool UseSystem;
		public bool UseFlickr;

		public bool UseAllServices;
		
		private string IconThemePath;

		public FaceCollection ()
		{

			Path = "";

			UseTwitter     = false;
			UseGravatar    = false;
			UseIdentica    = false;
			UseSystem      = false;
			UseFlickr      = false;
			UseAllServices = false;

		}
		
		public FaceCollection (string path)
		{
		
			UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);
			string home_path = unix_user_info.HomeDirectory;

			string IconThemePath = CombineMore (home_path, ".icons");
			SetThemePath (IconThemePath);

			if (!Directory.Exists (theme_path))
				Directory.CreateDirectory (theme_path);
				
				Directory.CreateDirectory (CombineMore (ThemePath));

			
		}
		
		public void SetThemePath (string path)
		{
			string IconThemePath = path;
		}

		public string GetThemePath ()
		{
		
		}

		
		public Gdk.Pixbuf GetFace (string identifier)
		{
			return null;
		}
		
		public bool AddFace (string identifier)
		{
			// avatar-twitter-hbons
			// 16, 24, 32, 48
			Gdk.Pixbuf gravatar_icon;
			if (UseGravatar)
				gravatar_icon = new GravatarIcon (identifier);

			return true;
			
		}
		
		public bool Refresh ()
		{
			return true;	
		}

	}

}
