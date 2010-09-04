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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SparkleLib;
using SparkleLib.Options;

namespace SparkleShare {

	// This is SparkleShare!
	public class SparkleShare {

		public static SparkleUI SparkleUI;
		public static string UserName;
		public static string UserEmail;


		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}
		

		public static void Main (string [] args)
		{

			// Use translations
			Catalog.Init (Defines.GETTEXT_PACKAGE, Defines.LOCALE_DIR);

			// Check whether git is installed
			Process process = new Process ();
			process.StartInfo.FileName = "git";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.Start ();

			if (process.StandardOutput.ReadToEnd ().IndexOf ("version") == -1) {

				Console.WriteLine (_("Git wasn't found."));
				Console.WriteLine (_("You can get Git from http://git-scm.com/."));

				Environment.Exit (0);

			}


			UnixUserInfo user_info = new UnixUserInfo (UnixEnvironment.UserName);

			// Don't allow running as root
			if (user_info.UserId == 0) {

				Console.WriteLine (_("Sorry, you can't run SparkleShare with these permissions."));
				Console.WriteLine (_("Things would go utterly wrong.")); 

				Environment.Exit (0);

			}


			bool HideUI   = false;
			bool ShowHelp = false;

			var p = new OptionSet () {
				{ "d|disable-gui", _("Don't show the notification icon"), v => HideUI = v != null },
				{ "v|version", _("Show this help text"), v => { PrintVersion (); Environment.Exit (0); } },
				{ "h|help", _("Print version information"), v=> ShowHelp = v != null }
			};

			try {

				p.Parse (args);

			} catch (OptionException e) {

				Console.Write ("SparkleShare: ");
				Console.WriteLine (e.Message);
				Console.WriteLine ("Try `sparkleshare --help' for more information.");

			}

			if (ShowHelp)
				DisplayHelp(p);

			SparkleUI = new SparkleUI (HideUI);
			SparkleUI.Run();

		}


		// Prints the help output
		public static void DisplayHelp (OptionSet p)
		{
			Console.WriteLine (" ");
			Console.WriteLine (_("SparkleShare, an instant update workflow to Git."));
			Console.WriteLine (_("Copyright (C) 2010 Hylke Bons"));
			Console.WriteLine (" ");
			Console.WriteLine (_("This program comes with ABSOLUTELY NO WARRANTY."));
			Console.WriteLine (" ");
			Console.WriteLine (_("This is free software, and you are welcome to redistribute it "));
			Console.WriteLine (_("under certain conditions. Please read the GNU GPLv3 for details."));
			Console.WriteLine (" ");
			Console.WriteLine (_("SparkleShare automatically syncs Git repositories in "));
			Console.WriteLine (_("the ~/SparkleShare folder with their remote origins."));
			Console.WriteLine (" ");
			Console.WriteLine (_("Usage: sparkleshare [start|stop|restart] [OPTION]..."));
			Console.WriteLine (_("Sync SparkleShare folder with remote repositories."));
			Console.WriteLine (" ");
			Console.WriteLine (_("Arguments:"));
			p.WriteOptionDescriptions (Console.Out);
			Environment.Exit (0);
		}


		// Prints the version information
		public static void PrintVersion ()
		{

			Console.WriteLine (_("SparkleShare " + Defines.VERSION));

		}


		// Looks up the user's name from the global configuration
		public static string GetUserName ()
		{

			string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			StreamReader reader = new StreamReader (global_config_file_path);

			// Discard the first line
			reader.ReadLine ();

			string line = reader.ReadLine ();
			reader.Close ();

			string name = line.Substring (line.IndexOf ("=") + 2);

			return name;

		}


		// Looks up the user's email from the global configuration
		public static string GetUserEmail ()
		{

			string email = "";
			string global_config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			// Look in the global config file first
			if (File.Exists (global_config_file_path)) {

				StreamReader reader = new StreamReader (global_config_file_path);

				// Discard the first two lines
				reader.ReadLine ();
				reader.ReadLine ();

				string line = reader.ReadLine ();
				reader.Close ();

				email = line.Substring (line.IndexOf ("=") + 2);

				return email;

			// Secondly, look at the user's private key file name
			} else {

				string keys_path = SparklePaths.SparkleKeysPath;

				if (!Directory.Exists (keys_path))
					return "";

				foreach (string file_path in Directory.GetFiles (keys_path)) {

					string file_name = System.IO.Path.GetFileName (file_path);

					if (file_name.StartsWith ("sparkleshare.") && file_name.EndsWith (".key")) {

						email = file_name.Substring (file_name.IndexOf (".") + 1);
						email = email.Substring (0, email.LastIndexOf ("."));

						return email;

					}

				}

				return "";

			}

		}


		// Adds the user's SparkleShare key to the ssh-agent,
		// so all activity is done with this key
		public static void AddKey ()
		{

			string keys_path = SparklePaths.SparkleKeysPath;
			string key_file_name = "sparkleshare." + UserEmail + ".key";

			Process process = new Process ();
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute        = false;
			process.StartInfo.FileName               = "ssh-add";
			process.StartInfo.Arguments              = Path.Combine (keys_path, key_file_name);
			process.Start ();

		}

	}
	
}
