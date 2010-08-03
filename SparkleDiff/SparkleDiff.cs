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
using System.Text.RegularExpressions;
using SparkleLib;

namespace SparkleShare {

	public class SparkleDiff
	{

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		// Finds out the path relative to the Git root directory
		public static string GetPathFromGitRoot (string file_path)
		{
			string git_root = GetGitRoot (file_path);
			return file_path.Substring (git_root.Length + 1);
		}


		// Finds out the path relative to the Git root directory
		public static string GetGitRoot (string file_path)
		{

			file_path = System.IO.Path.GetDirectoryName (file_path);

			while (file_path != null) {

				if (Directory.Exists (System.IO.Path.Combine (file_path, ".git")))
					return file_path;

				file_path = Directory.GetParent (file_path).FullName;

			}

			return null;
			
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

			UnixUserInfo UnixUserInfo =	new UnixUserInfo (UnixEnvironment.UserName);

			// Don't allow running as root
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

				string file_path = System.IO.Path.GetFullPath (args [0]);

				if (File.Exists (file_path)) {

					Gtk.Application.Init ();
					
					string [] revisions = GetRevisionsForFilePath (file_path);

					// Quit if the given file doesn't have any history
					if (revisions.Length < 2) {
						Console.WriteLine ("SparkleDiff: " + file_path + ": File has no history.");
						Environment.Exit (-1);
					}

					SparkleDiffWindow sparkle_diff_window;
					sparkle_diff_window = new SparkleDiffWindow (file_path, revisions);
					sparkle_diff_window.ShowAll ();

					// The main loop
					Gtk.Application.Run ();

				} else {

					Console.WriteLine ("SparkleDiff: " + file_path + ": No such file or directory.");
					Environment.Exit (-1);

				}
				
			} else {

			 	ShowHelp ();

			}

		}


		// Gets a list of all earlier revisions of this file
		public static string [] GetRevisionsForFilePath (string file_path)
		{

			Process process = new Process ();
			process.EnableRaisingEvents = true; 
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			process.StartInfo.WorkingDirectory = SparkleDiff.GetGitRoot (file_path);
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = "log --format=\"%H\" " + SparkleDiff.GetPathFromGitRoot (file_path);

			process.Start ();

			string output = process.StandardOutput.ReadToEnd ();
			string [] revisions = Regex.Split (output.Trim (), "\n");

			return revisions;

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
