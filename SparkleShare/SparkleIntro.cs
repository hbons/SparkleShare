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
using SparkleLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SparkleShare {

	public class SparkleIntro : Window {

		private Entry NameEntry;
		private Entry EmailEntry;
		private SparkleEntry ServerEntry;
		private SparkleEntry FolderEntry;
		private Button NextButton;
		private Button AddButton;
		private bool StepTwoOnly;
		private string SecondaryTextColor;

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleIntro () : base ("")
		{

			BorderWidth    = 0;
			IconName       = "folder-sparkleshare";
			Resizable      = true;
			WindowPosition = WindowPosition.Center;

			StepTwoOnly = false;

			SetDefaultSize (640, 480);

			Window window = new Window ("");
			SecondaryTextColor = GdkColorToHex (window.Style.Foreground (StateType.Insensitive));

			ShowStepOne ();

		}


		private void ShowStepOne ()
		{

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);

					VBox layout_vertical = new VBox (false, 0) {
						BorderWidth = 30
					};

						Label header = new Label ("<span size='x-large'><b>" +
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

						Table table = new Table (4, 2, true) {
							RowSpacing = 6
						};

							UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);			

							Label name_label = new Label ("<b>" + _("Full Name:") + "</b>") {
								UseMarkup = true,
								Xalign    = 0
							};

							NameEntry = new Entry (unix_user_info.RealName);
							NameEntry.Changed += delegate {
								CheckStepOneFields ();
							};


							EmailEntry = new Entry (GetUserEmail ());
							EmailEntry.Changed += delegate {
								CheckStepOneFields ();
							};

							Label email_label = new Label ("<b>" + _("Email:") + "</b>") {
								UseMarkup = true,
								Xalign    = 0
							};


						table.Attach (name_label, 0, 1, 0, 1);
						table.Attach (NameEntry, 1, 2, 0, 1);
						table.Attach (email_label, 0, 1, 1, 2);
						table.Attach (EmailEntry, 1, 2, 1, 2);
				
						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End
						};

							NextButton = new Button (_("Next")) {
								Sensitive = false
							};
			
							NextButton.Clicked += delegate (object o, EventArgs args) {

								NextButton.Remove (NextButton.Child);
								NextButton.Add (new Label (_("Configuring…")));

								NextButton.Sensitive = false;
								table.Sensitive       = false;

								NextButton.ShowAll ();

								Configure ();
								ShowStepTwo ();

							};
			
						controls.Add (NextButton);

					layout_vertical.PackStart (header, false, false, 0);
					layout_vertical.PackStart (information, false, false, 21);
					layout_vertical.PackStart (new Label (""), false, false, 0);
					layout_vertical.PackStart (table, false, false, 0);
//					layout_vertical.PackStart (check_button, false, false, 0);


				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			CheckStepOneFields ();

			ShowAll ();
		
		}


		public void ShowStepTwo (bool step_two_only)
		{

			StepTwoOnly = step_two_only;
			ShowStepTwo ();

		}


		public void ShowStepTwo ()
		{

			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
					VBox layout_vertical = new VBox (false, 0) {
						BorderWidth = 30
					};
			
						Label header = new Label ("<span size='x-large'><b>" +
								                        _("Where is your remote folder?") +
								                        "</b></span>") {
							UseMarkup = true,
							Xalign = 0
						};

						Table table = new Table (7, 2, false) {
							RowSpacing = 12
						};

							HBox layout_server = new HBox (true, 0);

								ServerEntry = new SparkleEntry () {
									ExampleText = _("ssh://address-to-my-server/")
								};
								
								ServerEntry.Changed += CheckStepTwoFields;

								RadioButton radio_button = new RadioButton ("<b>" + _("On my own server:") + "</b>");

							layout_server.Add (radio_button);
							layout_server.Add (ServerEntry);
							
							string github_text = "<b>" + "Github" + "</b>\n" +
								  "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
								_("Free hosting for Free and Open Source Software projects.") + "\n" + 
								_("Also has paid accounts for extra private space and bandwidth.") +
								  "</span>";

							RadioButton radio_button_github = new RadioButton (radio_button, github_text);

							(radio_button_github.Child as Label).UseMarkup = true;
							(radio_button_github.Child as Label).Wrap      = true;

							string gnome_text = "<b>" + _("The GNOME Project") + "</b>\n" +
								  "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
								_("GNOME is an easy to understand interface to your computer.") + "\n" +
								_("Select this option if you’re a developer or designer working on GNOME.") +
								  "</span>";

							RadioButton radio_button_gnome = new RadioButton (radio_button, gnome_text);

							(radio_button_gnome.Child as Label).UseMarkup = true;
							(radio_button_gnome.Child as Label).Wrap      = true;

							string gitorious_text = "<b>" + _("Gitorious") + "</b>\n" +
								  "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
								_("Completely Free as in Freedom infrastructure.") + "\n" +
								_("Free accounts for Free and Open Source projects.") +
								  "</span>";
							RadioButton radio_button_gitorious = new RadioButton (radio_button, gitorious_text) {
								Xalign = 0
							};
							
							(radio_button_gitorious.Child as Label).UseMarkup = true;
							(radio_button_gitorious.Child as Label).Wrap      = true;

							radio_button_github.Toggled += delegate {

								if (radio_button_github.Active)
									FolderEntry.ExampleText = "Username/Folder";

							};

							radio_button_gitorious.Toggled += delegate {

								if (radio_button_gitorious.Active)
									FolderEntry.ExampleText = "Project/Folder";

							};

							radio_button_gnome.Toggled += delegate {

								if (radio_button_gnome.Active)
									FolderEntry.ExampleText = "Project";

							};


							radio_button.Toggled += delegate {

								if (radio_button.Active) {

									FolderEntry.ExampleText = "Folder";
									ServerEntry.Sensitive   = true;
									CheckStepTwoFields ();

								} else {

									ServerEntry.Sensitive = false;
									CheckStepTwoFields ();

								}

								ShowAll ();

							};

						table.Attach (layout_server,          0, 2, 1, 2);
						table.Attach (radio_button_github,    0, 2, 2, 3);
						table.Attach (radio_button_gitorious, 0, 2, 3, 4);
						table.Attach (radio_button_gnome,     0, 2, 4, 5);

						HBox layout_folder = new HBox (true, 0);

							FolderEntry = new SparkleEntry () {
								ExampleText = "Folder"
							};
							
							FolderEntry.Changed += CheckStepTwoFields;

							Label folder_label = new Label ("<b>" + _("Folder Name:") + "</b>") {
								UseMarkup = true,
								Xalign    = 1
							};

						(radio_button.Child as Label).UseMarkup = true;

						layout_folder.PackStart (folder_label, true, true, 12);
						layout_folder.PackStart (FolderEntry, true, true, 0);


						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End,
							Spacing     = 6
						};

							AddButton = new Button (_("Sync"));
			
							AddButton.Clicked += delegate {

								string name = FolderEntry.Text;

								// Remove the starting slash if there is one
								if (name.StartsWith ("/"))
									name = name.Substring (1);

								string server = "";

								if (radio_button.Active) {

									server = SparkleToGitUrl (ServerEntry.Text);

									// Remove the trailing slash if there is one
									if (server.EndsWith ("/"))
										server = server.Trim ("/".ToCharArray ());

								}

								if (radio_button_gitorious.Active)
									server = "ssh://git@gitorious.org";

								if (radio_button_github.Active)
									server = "ssh://git@github.com";

								if (radio_button_gnome.Active)
									server = "ssh://git@gnome.org";

								if (!name.EndsWith (".git"))
									name += ".git";

								string canonical_name = System.IO.Path.GetFileNameWithoutExtension (name);
								FolderEntry.Text = canonical_name;

								string url  = server + "/" + name;
								string tmp_folder = SparkleHelpers.CombineMore (SparklePaths.SparkleTmpPath,
									canonical_name);

								SparkleFetcher fetcher = new SparkleFetcher (url, tmp_folder);

								Console.WriteLine (url);

								fetcher.CloningStarted += delegate {

									SparkleHelpers.DebugInfo ("Git", "[" + canonical_name + "] Cloning Repository");

								};


								fetcher.CloningFinished += delegate {

									SparkleHelpers.DebugInfo ("Git", "[" + canonical_name + "] Repository cloned");

									ClearAttributes (tmp_folder);

									try {

										bool folder_exists = Directory.Exists (
											SparkleHelpers.CombineMore (SparklePaths.SparklePath, canonical_name));

										int i = 1;
										while (folder_exists) {

											i++;
											folder_exists = Directory.Exists (
												SparkleHelpers.CombineMore (SparklePaths.SparklePath,
													canonical_name + " (" + i + ")"));

										}

										string target_folder_name = canonical_name;

										if (i > 1)
											target_folder_name += " (" + i + ")";

										Directory.Move (tmp_folder,
											SparkleHelpers.CombineMore (SparklePaths.SparklePath, target_folder_name));

									} catch (Exception e) {

										SparkleHelpers.DebugInfo ("Git",
											"[" + name + "] Error moving folder: " + e.Message);

									}

									Application.Invoke (delegate { ShowFinishedStep (); });

								};


								fetcher.CloningFailed += delegate {

									SparkleHelpers.DebugInfo ("Git", "[" + canonical_name + "] Cloning failed");

									if (Directory.Exists (tmp_folder)) {

										ClearAttributes (tmp_folder);
										Directory.Delete (tmp_folder, true);

										SparkleHelpers.DebugInfo ("Config",
											"[" + name + "] Deleted temporary directory");

									}

									Application.Invoke (delegate { ShowErrorStep (); });

								};

								ShowStepTwoAndAHalf ();
								fetcher.Clone ();

							};

							Button skip_button = new Button (_("Skip"));

							skip_button.Clicked += delegate {
								ShowStepThree ();
							};


						if (!StepTwoOnly)
							controls.Add (skip_button);

						controls.Add (AddButton);

					layout_vertical.PackStart (header, false, false, 0);
					layout_vertical.PackStart (new Label (""), false, false, 3);
					layout_vertical.PackStart (table, false, false, 0);
					layout_vertical.PackStart (layout_folder, false, false, 6);

				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			CheckStepTwoFields ();

			ShowAll ();
		
		}


		private void ShowErrorStep ()
		{

			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
					VBox layout_vertical = new VBox (false, 0) {
						BorderWidth = 30
					};
			
						Label header = new Label ("<span size='x-large'><b>" +
								                _("Something went wrong…") +
								                  "</b></span>\n") {
							UseMarkup = true,
							Xalign = 0
						};
				
						Label information = new Label ("<span fgcolor='" + SecondaryTextColor + "'>" +
						                             _("Hey, it's an Alpha!") +
						                               "</span>") {
							Xalign = 0,
							Wrap   = true,
							UseMarkup = true
						};

				
						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End
						};

							Button try_again_button = new Button (_("Try again…")) {
								Sensitive = true
							};
			
							try_again_button.Clicked += delegate (object o, EventArgs args) {

								ShowStepTwo ();

							};
			
						controls.Add (try_again_button);

					layout_vertical.PackStart (header, false, false, 0);
					layout_vertical.PackStart (information, false, false, 0);

				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			ShowAll ();
		
		}


		private void ShowFinishedStep ()
		{

			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
					VBox layout_vertical = new VBox (false, 0) {
						BorderWidth = 30
					};
			
						Label header = new Label ("<span size='x-large'><b>" +
								                _("Folder synced successfully!") +
								                  "</b></span>") {
							UseMarkup = true,
							Xalign = 0
						};
				
						Label information = new Label ("<span fgcolor='" + SecondaryTextColor + "'>" +
						                             _("Buy a lottery ticket!") +
						                               "</span>") {
							Xalign = 0,
							Wrap   = true,
							UseMarkup = true
						};

				
						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End
						};

							Button finish_button = new Button (_("Finish"));
			
							finish_button.Clicked += delegate (object o, EventArgs args) {

								if(SparkleShare.SparkleUI != null)
									SparkleShare.SparkleUI.UpdateRepositories ();

								Destroy ();

							};
			
						controls.Add (finish_button);

					layout_vertical.PackStart (header, false, false, 0);
					layout_vertical.PackStart (information, false, false, 0);

				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			ShowAll ();
		
		}


		private void ShowStepTwoAndAHalf ()
		{

			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
					VBox layout_vertical = new VBox (false, 0) {
						BorderWidth = 30
					};

						Label header = new Label ("<span size='x-large'><b>" +
								                        String.Format (_("Syncing folder ‘{0}’…"), FolderEntry.Text) +
								                        "</b></span>") {
							UseMarkup = true,
							Xalign    = 0,
							Wrap      = true
						};

						Label information = new Label ("<span fgcolor='" + SecondaryTextColor + "'>" + 
						                             _("This may take a while.\n") +
						                             _("You sure it’s not coffee o-clock?") +
						                               "</span>") {
							UseMarkup = true,
							Xalign = 0
						};

						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End,
							Spacing     = 6
						};

							Button button = new Button () {
								Sensitive = false
							};
			
							if (StepTwoOnly) {

								button.Label = _("Finish");
								button.Clicked += delegate {
									Destroy ();
								};

							} else {

								button.Label = _("Next");
								button.Clicked += delegate {
									ShowStepThree ();
								};

							}

						controls.Add (button);

						SparkleSpinner spinner = new SparkleSpinner (22);

					Table table = new Table (2, 2, false) {
						RowSpacing    = 12,
						ColumnSpacing = 9
					};

					HBox box = new HBox (false, 0);

					table.Attach (spinner,      0, 1, 0, 1);
					table.Attach (header, 1, 2, 0, 1);
					table.Attach (information,  1, 2, 1, 2);

					box.PackStart (table, false, false, 0);

					layout_vertical.PackStart (box, false, false, 0);

				wrapper.PackStart (layout_vertical, true, true, 0);
				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			CheckStepTwoFields ();

			ShowAll ();

		}


		private void ShowStepThree ()
		{

			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image (SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
					"side-splash.png"));

				VBox wrapper = new VBox (false, 0);
			
				VBox layout_vertical = new VBox (false, 0) {
					BorderWidth = 30
				};

				Label header = new Label ("<span size='x-large'><b>" +
					                            _("SparkleShare is ready to go!") +
					                            "</b></span>") {
					UseMarkup = true,
					Xalign = 0
				};

				Label information = new Label (_("Now you can start accepting invitations from others. " + "\n" +
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

				layout_vertical.PackStart (header, false, false, 0);
				layout_vertical.PackStart (information, false, false, 21);
				layout_vertical.PackStart (link_wrapper, false, false, 0);

				HButtonBox controls = new HButtonBox () {
					Layout      = ButtonBoxStyle.End,
					BorderWidth = 12
				};

					Button finish_button = new Button (_("Finish"));

					finish_button.Clicked += delegate (object o, EventArgs args) {

						if (SparkleUI.NotificationIcon == null)
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
		private void CheckStepOneFields ()
		{

			if (NameEntry.Text.Length > 0 &&
			    IsValidEmail (EmailEntry.Text)) {

				NextButton.Sensitive = true;

			} else {

				NextButton.Sensitive = false;
				
			}

		}


		// Enables the Add button when the fields are
		// filled in correctly
		public void CheckStepTwoFields (object o, EventArgs args)
		{

			CheckStepTwoFields ();

		}


		// Enables the Add button when the fields are
		// filled in correctly
		public void CheckStepTwoFields ()
		{

			AddButton.Sensitive = false;
			bool IsFolder = !FolderEntry.Text.Trim ().Equals ("");

			if (ServerEntry.Sensitive == true) {
			
				if (IsGitUrl (ServerEntry.Text) && IsFolder)
					AddButton.Sensitive = true;

			} else if (IsFolder) {

					AddButton.Sensitive = true;

			}

		}


		// Configures SparkleShare with the user's information
		private void Configure ()
		{

			string config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, "config");

			TextWriter writer = new StreamWriter (config_file_path);
			writer.WriteLine ("[user]\n" +
			                  "\tname  = " + NameEntry.Text + "\n" +
			                  "\temail = " + EmailEntry.Text);
			writer.Close ();

			SparkleHelpers.DebugInfo ("Config", "Created '" + config_file_path + "'");

			GenerateKeyPair ();

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


		// Checks if a url is a valid git url
		private static bool IsGitUrl (string url)
		{
			
			return Regex.Match (url, @"ssh://(.)+").Success;

		}


		// Recursively sets access rights of a folder to 'Normal'
		private void ClearAttributes (string path)
		{

			if (Directory.Exists (path)) {

				string [] folders = Directory .GetDirectories (path);

				foreach (string folder in folders)
					ClearAttributes (folder);

				string [] files = Directory .GetFiles(path);

				foreach (string file in files)
					File.SetAttributes (file, FileAttributes.Normal);

			}

		}


		// Converts a Gdk RGB color to a hex value.
		// Example: from "rgb:0,0,0" to "#000000"
		public string GdkColorToHex (Gdk.Color color)
		{

			return String.Format ("#{0:X2}{1:X2}{2:X2}",
				(int) Math.Truncate (color.Red   / 256.00),
				(int) Math.Truncate (color.Green / 256.00),
				(int) Math.Truncate (color.Blue  / 256.00));

		}


		// Convert the more human readable sparkle:// url to something Git can use.
		// Example: sparkle://gitorious.org/sparkleshare ssh://git@gitorious.org/sparkleshare
		private static string SparkleToGitUrl (string url)
		{

			if (url.StartsWith ("sparkle://"))
				url = url.Replace ("sparkle://", "ssh://git@");

			return url;
		
		}

	}

}
