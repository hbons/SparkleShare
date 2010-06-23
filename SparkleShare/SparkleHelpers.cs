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
using Mono.Unix;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SparkleShare {
	
	public static class SparkleHelpers
	{

		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		// Get's the avatar for a specific email address and size
		public static Gdk.Pixbuf GetAvatar (string Email, int Size)
		{

			string AvatarPath = CombineMore (SparklePaths.SparkleAvatarPath, Size + "x" + Size);

			if (!Directory.Exists (AvatarPath)) {
				Directory.CreateDirectory (AvatarPath);
				SparkleHelpers.DebugInfo ("Config", "Created '" + AvatarPath + "'");
			}
			
			string AvatarFilePath = CombineMore (AvatarPath, Email);

			if (File.Exists (AvatarFilePath))
				return new Gdk.Pixbuf (AvatarFilePath);
			else {

				// Let's try to get the person's gravatar for next time
				WebClient WebClient = new WebClient ();
				Uri GravatarUri = new Uri ("http://www.gravatar.com/avatar/" + GetMD5 (Email) +
					".jpg?s=" + Size + "&d=404");

				string TmpFile = CombineMore (SparklePaths.SparkleTmpPath, Email + Size);

				if (!File.Exists (TmpFile)) {

					WebClient.DownloadFileAsync (GravatarUri, TmpFile);
					WebClient.DownloadFileCompleted += delegate {
						File.Delete (AvatarFilePath);
						FileInfo TmpFileInfo = new FileInfo (TmpFile);
						if (TmpFileInfo.Length > 255)
							File.Move (TmpFile, AvatarFilePath);
					};

				}

				// Fall back to a generic icon if there is no gravatar
				if (File.Exists (AvatarFilePath))
					return new Gdk.Pixbuf (AvatarFilePath);
				else
					return GetIcon ("avatar-default", Size);

			}

		}


		// Creates an MD5 hash of input
		public static string GetMD5 (string s)
		{
			MD5 md5 = new MD5CryptoServiceProvider ();
			Byte[] Bytes = ASCIIEncoding.Default.GetBytes (s);
			Byte[] EncodedBytes = md5.ComputeHash (Bytes);
			return BitConverter.ToString (EncodedBytes).ToLower ().Replace ("-", "");
		}

		// Convert the more human readable sparkle:// url to
		// something Git can use
		// Example: sparkle://gitorious.org/sparkleshare
		// to ssh://git@gitorious.org/sparkleshare
		public static string SparkleToGitUrl (string Url)
		{
			if (Url.StartsWith ("sparkle://"))
				Url = Url.Replace ("sparkle://", "ssh://git@");

			// Usually don't need the ".git" at the end.
			// It looks ugly as a folder too.
			if (Url.EndsWith (".git"))
				Url = Url.Substring (0, Url.Length - 4);

			return Url;
		}

		
		// Makes it possible to combine more than
		// two paths at once.
		public static string CombineMore (params string [] Parts)
		{
			string NewPath = " ";
			foreach (string Part in Parts)
				NewPath = Path.Combine (NewPath, Part);
			return NewPath;
		}


		// Looks up an icon from the system's theme
		public static Gdk.Pixbuf GetIcon (string Name, int Size)
		{
			IconTheme IconTheme = new IconTheme ();
			IconTheme.AppendSearchPath (CombineMore (SparklePaths.SparkleInstallPath, "icons"));
			return IconTheme.LoadIcon (Name, Size, IconLookupFlags.GenericFallback);
		}


		// Checks if a url is a valid git url
		public static bool IsGitUrl (string Url)
		{
			return Regex.Match (Url, @"(.)+(/|:)(.)+").Success;
		}


		public static bool ShowDebugInfo = true;

		// Show debug info if needed
		public static void DebugInfo (string Type, string Message)
		{
			if (ShowDebugInfo) {
				DateTime DateTime = new DateTime ();					
				string TimeStamp = DateTime.Now.ToString ("HH:mm:ss");
				Console.WriteLine ("[" + TimeStamp + "]" + "[" + Type + "] " + Message);
			}

		}


		// Formats a timestamp to a relative date compared to the current time
		// Example: "about 5 hours ago"
		public static string ToRelativeDate (DateTime date_time)
		{
			TimeSpan time_span = new TimeSpan (0);
			time_span = DateTime.Now - date_time;

			if (time_span <= TimeSpan.FromSeconds (60)) {
				return string.Format (Catalog.GetPluralString ("a second ago", "{0} seconds ago",
				                                               time_span.Seconds),
				                      time_span.Seconds);
			}

			if (time_span <= TimeSpan.FromSeconds (60)) {
				return string.Format (Catalog.GetPluralString ("a minute ago", "about {0} minutes ago",
				                                               time_span.Minutes),
				                      time_span.Minutes);
			}

			if (time_span <= TimeSpan.FromHours(24)) {
				return string.Format (Catalog.GetPluralString ("about an hour ago", "about {0} hours ago",
				                                               time_span.Hours),
				                      time_span.Hours);
			}

			if (time_span <= TimeSpan.FromDays(30)) {
				return string.Format (Catalog.GetPluralString ("yesterday", "{0} days ago",
				                                               time_span.Days),
				                      time_span.Days);
			}

			if (time_span <= TimeSpan.FromDays(365)) {
				return string.Format (Catalog.GetPluralString ("a month ago", "{0} months ago",
					                                           (int) time_span.Days / 30),
					                  (int) time_span.Days / 30);
			}

			return string.Format (Catalog.GetPluralString ("a year ago", "{0} years ago",
			                                               (int) time_span.Days / 365),
			                      (int) time_span.Days / 365);
		}

		// Checks for unicorns
		public static void CheckForUnicorns (string s) {
			s = s.ToLower ();
			if (s.Contains ("unicorn") && (s.Contains (".png") || s.Contains (".jpg"))) {
				string title   = _("Hold your ponies!");
				string subtext = _("SparkleShare is known to be insanely fast with \n" +
				                  "pictures of unicorns. Please make sure your internets\n" +
				                  "are upgraded to the latest version to avoid problems.");
				SparkleBubble unicorn_bubble = new SparkleBubble (title, subtext);
				unicorn_bubble.Show ();
			}
		}

	}

}
