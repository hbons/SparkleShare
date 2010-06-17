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

namespace SparkleShare {

	// This is SparkleShare!
	public class SparkleShare {

		// Short alias for the translations
		public static string _ (string s) {
			return Catalog.GetString (s);
		}
		
		public static SparkleRepo [] Repositories;
		public static SparkleUI SparkleUI;

		public static void Main (string [] args) {

			// Use translations
			Catalog.Init (Defines.GETTEXT_PACKAGE, Defines.LOCALE_DIR);

			// Check if git is installed
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
				Console.WriteLine (_("Things will go utterly wrong.")); 
				Environment.Exit (0);
			}

			// Parse the command line arguments
			bool HideUI = false;
			if (args.Length > 0) {
				foreach (string Argument in args) {
					if (Argument.Equals ("--disable-gui") || Argument.Equals ("-d"))
						HideUI = true;
					if (Argument.Equals ("--help") || Argument.Equals ("-h")) {
						ShowHelp ();
					}
				}
			}

			Gtk.Application.Init ();

			SparkleUI = new SparkleUI (HideUI);

			// The main loop
			Gtk.Application.Run ();

		}

		// Prints the help output
		public static void ShowHelp () {
			Console.WriteLine (_("SparkleShare Copyright (C) 2010 Hylke Bons"));
			Console.WriteLine (" ");
			Console.WriteLine (_("This program comes with ABSOLUTELY NO WARRANTY."));
			Console.WriteLine (_("This is free software, and you are welcome to redistribute it "));
			Console.WriteLine (_("under certain conditions. Please read the GNU GPLv3 for details."));
			Console.WriteLine (" ");
			Console.WriteLine (_("SparkleShare syncs the ~/SparkleShare folder with remote repositories."));
			Console.WriteLine (" ");
			Console.WriteLine (_("Usage: sparkleshare [start|stop|restart] [OPTION]..."));
			Console.WriteLine (_("Sync SparkleShare folder with remote repositories."));
			Console.WriteLine (" ");
			Console.WriteLine (_("Arguments:"));
			Console.WriteLine (_("\t -d, --disable-gui\tDon't show the notification icon."));
			Console.WriteLine (_("\t -h, --help\t\tDisplay this help text."));
			Console.WriteLine (" ");
			Environment.Exit (0);
		}

	}
	
}
