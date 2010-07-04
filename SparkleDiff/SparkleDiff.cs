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
using System.Diagnostics;
using System.IO;

namespace SparkleShare {

	public class SparkleDiff
	{

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		public static void Main (string [] args)
		{

			Catalog.Init (Defines.GETTEXT_PACKAGE, Defines.LOCALE_DIR);

			// Check whether git is installed
			Process Process = new Process ();
			Process.StartInfo.FileName = "git";
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.UseShellExecute = false;
			Process.Start ();

			if (Process.StandardOutput.ReadToEnd ().IndexOf ("version") == -1) {
				Console.WriteLine (_("Git wasn't found."));
				Console.WriteLine (_("You can get Git from http://git-scm.com/."));
				Environment.Exit (0);
			}

			// Don't allow running as root
			UnixUserInfo UnixUserInfo =	new UnixUserInfo (UnixEnvironment.UserName);
			if (UnixUserInfo.UserId == 0) {
				Console.WriteLine (_("Sorry, you can't run SparkleShare with these permissions."));
				Console.WriteLine (_("Things would go utterly wrong.")); 
				Environment.Exit (0);
			}

			if (args.Length > 0) {
				if (args [0].Equals ("--help") || args [0].Equals ("-h")) {
					ShowHelp ();
					Environment.Exit (0);
				}

				string file_path = args [0];

				if (File.Exists (file_path)) {

					Gtk.Application.Init ();

					SparkleDiffWindow sparkle_diff_window;
					sparkle_diff_window = new SparkleDiffWindow (file_path);
					sparkle_diff_window.ShowAll ();

					// The main loop
					Gtk.Application.Run ();

				} else {

					Console.WriteLine ("SparkleDiff: " + file_path + ": No such file or directory.");
					Environment.Exit (0);

				}
				
			}

		}

		// Prints the help output
		public static void ShowHelp ()
		{
			Console.WriteLine (_("SparkleDiff Copyright (C) 2010 Hylke Bons"));
			Console.WriteLine (" ");
			Console.WriteLine (_("This program comes with ABSOLUTELY NO WARRANTY."));
			Console.WriteLine (_("This is free software, and you are welcome to redistribute it "));
			Console.WriteLine (_("under certain conditions. Please read the GNU GPLv3 for details."));
			Console.WriteLine (" ");
			Console.WriteLine (_("SparkleDiff let's you compare revisions of an image file side by side."));
			Console.WriteLine (" ");
			Console.WriteLine (_("Usage: sparklediff [FILE]"));
			Console.WriteLine (_("Open an image file to show its revisions"));
			Console.WriteLine (" ");
			Console.WriteLine (_("Arguments:"));
			Console.WriteLine (_("\t -h, --help\t\tDisplay this help text."));
			Console.WriteLine (" ");
		}

	}

}
