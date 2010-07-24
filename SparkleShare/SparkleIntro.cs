//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Mono.Unix;
using SparkleShare;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SparkleShare {

	public class SparkleIntro : Window {

		private Entry NameEntry;
		private Entry EmailEntry;
		private Entry ServerEntry;
		private Button NextButton;

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleIntro () : base ("")
		{

			BorderWidth    = 0;
			IconName       = "folder-sparkleshare";
			Resizable      = false;
			WindowPosition = WindowPosition.Center;

			SetSizeRequest (640, 400);

			ShowStepOne ();

		}


		private void ShowStepOne ()
		{

			HBox layout_horizontal = new HBox (false, 6);

				// TODO: Fix the path
				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
					VBox layout_vertical = new VBox (false, 0) {
						BorderWidth = 30
					};
			
						Label introduction = new Label ("<span size='x-large'><b>" +
								                        _("Welcome to SparkleShare!") +
								                        "</b></span>") {
							UseMarkup = true,
							Xalign = 0
						};
				
						Label information = new Label (_("Before we can create a SparkleShare folder on this " +
								                         "computer, we need a few bits of information from you.")) {
							Xalign = 0,
							Wrap   = true
						};

						Table table = new Table (6, 2, true) {
							RowSpacing = 6
						};

							UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);			

							Label name_label = new Label ("<b>" + _("Full Name:") + "</b>") {
								UseMarkup = true,
								Xalign    = 0
							};

							NameEntry = new Entry (unix_user_info.RealName);
							NameEntry.Changed += delegate {
								CheckFields ();
							};


							EmailEntry = new Entry (GetUserEmail ());
							EmailEntry.Changed += delegate {
								CheckFields ();
							};

							Label email_label = new Label ("<b>" + _("Email:") + "</b>") {
								UseMarkup = true,
								Xalign    = 0
							};

							ServerEntry = new Entry ("ssh://gitorious.org/sparkleshare") {
								Sensitive = false
							};

							Label server_label = new Label ("<b>" + _("Folder Address:") + "</b>") {
								UseMarkup = true,
								Xalign = 0,
								Sensitive = false
							};
					
							CheckButton check_button;
							check_button = new CheckButton (_("I'm already subscribed to a " +
									                          "folder on a SparkleServer"));

							check_button.Clicked += delegate {

								if (check_button.Active) {

									server_label.Sensitive = true;
									ServerEntry.Sensitive = true;
									ServerEntry.HasFocus = true;

								} else {

									server_label.Sensitive = false;
									ServerEntry.Sensitive = false;					

								}

								ShowAll ();

							};

						table.Attach (name_label, 0, 1, 0, 1);
						table.Attach (NameEntry, 1, 2, 0, 1);
						table.Attach (email_label, 0, 1, 1, 2);
						table.Attach (EmailEntry, 1, 2, 1, 2);
						table.Attach (check_button, 0, 2, 3, 4);
						table.Attach (server_label, 0, 1, 4, 5);
						table.Attach (ServerEntry, 1, 2, 4, 5);
				
						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End
						};

							NextButton = new Button (_("Next")) {
								Sensitive = false
							};
			
							NextButton.Clicked += delegate (object o, EventArgs args) {

								NextButton.Remove (NextButton.Child);

								HBox hbox = new HBox ();

								hbox.Add (new SparkleSpinner ());
								hbox.Add (new Label (_("Configuring…")));

								NextButton.Add (hbox);

								NextButton.Sensitive = false;
								table.Sensitive       = false;

								NextButton.ShowAll ();

								Configure ();

							};
			
						controls.Add (NextButton);

					layout_vertical.PackStart (introduction, false, false, 0);
					layout_vertical.PackStart (information, false, false, 21);
					layout_vertical.PackStart (new Label (""), false, false, 0);
					layout_vertical.PackStart (table, false, false, 0);

				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			CheckFields ();

			ShowAll ();
		
		}


		private void ShowStepTwo ()
		{
		
			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
				VBox layout_vertical = new VBox (false, 0) {
					BorderWidth = 30
				};

				Label introduction = new Label ("<span size='x-large'><b>" +
					                            _("SparkleShare is ready to go!") +
					                            "</b></span>") {
					UseMarkup = true,
					Xalign = 0
				};

				Label information = new Label (_("Now you can start accepting invitations from others. " +
		                                         "Just click on invitations you get by email and " +
		                                         "we will take care of the rest.")) {
		        	UseMarkup = true,
		        	Wrap      = true,
					Xalign    = 0
				};


				HBox link_wrapper = new HBox (false, 0);
				LinkButton link = new LinkButton ("http://www.sparkleshare.org/",
					_("Learn how to host your own SparkleServer"));

				link_wrapper.PackStart (link, false, false, 0);

				layout_vertical.PackStart (introduction, false, false, 0);
				layout_vertical.PackStart (information, false, false, 21);
				layout_vertical.PackStart (link_wrapper, false, false, 0);

				HButtonBox controls = new HButtonBox () {
					Layout      = ButtonBoxStyle.End,
					BorderWidth = 12
				};

					Button finish_button = new Button (_("Finish"));

					finish_button.Clicked += delegate (object o, EventArgs args) {
						SparkleUI.NotificationIcon = new SparkleStatusIcon ();
						Destroy ();
					};

				controls.Add (finish_button);

				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, false, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			ShowAll ();
		
		}


		// Enables or disables the 'Next' button depending on the 
		// entries filled in by the user
		private void CheckFields ()
		{

			if (NameEntry.Text.Length > 0 &&
			    IsValidEmail (EmailEntry.Text)) {

				NextButton.Sensitive = true;

			} else {

				NextButton.Sensitive = false;
				
			}

		}


		// Configures SparkleShare with the user's information
		private void Configure ()
		{

			string config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, ".gitconfig");

			TextWriter writer = new StreamWriter (config_file_path);
			writer.WriteLine ("[user]\n" +
			                  "\tname  = " + NameEntry.Text + "\n" +
			                  "\temail = " + EmailEntry.Text + "\n");
			writer.Close ();

			SparkleHelpers.DebugInfo ("Config", "Created '" + config_file_path + "'");

			GenerateKeyPair ();

			ShowStepTwo ();

		}


		// Gets the email address if the user alreasy has a SparkleShare key installed
		private string GetUserEmail ()
		{

			string user_email = "";
			string keys_path = System.IO.Path.Combine (SparklePaths.HomePath, ".ssh");

			if (!Directory.Exists (keys_path))
				return "";

			foreach (string file_path in Directory.GetFiles (keys_path)) {

				string file_name = System.IO.Path.GetFileName (file_path);

				if (file_name.StartsWith ("sparkleshare.") && file_name.EndsWith (".key")) {

					user_email = file_name.Substring (file_name.IndexOf (".") + 1);
					user_email = user_email.Substring (0, user_email.LastIndexOf ("."));

					return user_email;

				}

			}
			
			return "";

		}


		// Generates and installs an RSA keypair to identify this system
		private void GenerateKeyPair ()
		{

			string user_email = EmailEntry.Text;
			string keys_path = System.IO.Path.Combine (SparklePaths.HomePath, ".ssh");
			string key_file_name = "sparkleshare." + user_email + ".key";

			Process process = new Process () {
				EnableRaisingEvents = true
			};
			
			if (!Directory.Exists (keys_path))
				Directory.CreateDirectory (keys_path);

			if (!File.Exists (key_file_name)) {

				process.StartInfo.WorkingDirectory = keys_path;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.FileName = "ssh-keygen";
				process.StartInfo.Arguments = "-t rsa -P " + user_email + " -f " + key_file_name;

				process.Start ();

				process.Exited += delegate {
					SparkleHelpers.DebugInfo ("Config", "Created key '" + key_file_name + "'");
					SparkleHelpers.DebugInfo ("Config", "Created key '" + key_file_name + ".pub'");
				};

			}

		}


		// Checks to see if an email address is valid
		private bool IsValidEmail(string email)
		{

			Regex regex = new Regex(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.IgnoreCase);
			return regex.IsMatch (email);

		}

	}

}
