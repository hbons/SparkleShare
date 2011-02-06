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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Mono.Unix;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SparkleShare {

	public class SparkleIntro : SparkleWindow {

		private Entry NameEntry;
		private Entry EmailEntry;
		private SparkleEntry ServerEntry;
		private SparkleEntry FolderEntry;
		private Button NextButton;
		private Button SyncButton;
		private bool ServerFormOnly;
		private string SecondaryTextColor;


		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleIntro () : base ()
		{

			ServerFormOnly = false;
			SecondaryTextColor = SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive));

		}

		
		public void ShowAccountForm ()
		{

			Reset ();

			VBox layout_vertical = new VBox (false, 0);
			
				DeleteEvent += PreventClose;

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
						CheckAccountForm ();
					};


					EmailEntry = new Entry (SparkleShare.Controller.UserEmail);
					EmailEntry.Changed += delegate {
						CheckAccountForm ();
					};

					Label email_label = new Label ("<b>" + _("Email:") + "</b>") {
						UseMarkup = true,
						Xalign    = 0
					};


				table.Attach (name_label, 0, 1, 0, 1);
				table.Attach (NameEntry, 1, 2, 0, 1);
				table.Attach (email_label, 0, 1, 1, 2);
				table.Attach (EmailEntry, 1, 2, 1, 2);
		
					NextButton = new Button (_("Next")) {
						Sensitive = false
					};
	
					NextButton.Clicked += delegate (object o, EventArgs args) {

						NextButton.Remove (NextButton.Child);
						NextButton.Add (new Label (_("Configuring…")));

						NextButton.Sensitive = false;
						table.Sensitive       = false;

						NextButton.ShowAll ();
			
						SparkleShare.Controller.UserName  = NameEntry.Text;
						SparkleShare.Controller.UserEmail = EmailEntry.Text;

						SparkleShare.Controller.GenerateKeyPair ();
						SparkleShare.Controller.AddKey ();
				
						SparkleShare.Controller.FirstRun = false;
				
						DeleteEvent += PreventClose;
						ShowServerForm ();

					};
	
				AddButton (NextButton);

			layout_vertical.PackStart (header, false, false, 0);
			layout_vertical.PackStart (information, false, false, 21);
			layout_vertical.PackStart (new Label (""), false, false, 0);
			layout_vertical.PackStart (table, false, false, 0);

			Add (layout_vertical);

			CheckAccountForm ();

			ShowAll ();
		
		}


		private void PreventClose (object o, DeleteEventArgs e)
		{

			// Cancel closing when the "Close"
			// button of the window is pressed
			e.RetVal = true;

		}


		public void ShowServerForm (bool server_form_only)
		{

			ServerFormOnly = server_form_only;
			ShowServerForm ();

		}


		public void ShowServerForm ()
		{

			Reset ();

			VBox layout_vertical = new VBox (false, 0);

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
							ExampleText = _("address-to-server.com")
						};
						
						ServerEntry.Changed += CheckServerForm;

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
							FolderEntry.ExampleText = _("Username/Folder");

					};

					radio_button_gitorious.Toggled += delegate {

						if (radio_button_gitorious.Active)
							FolderEntry.ExampleText = _("Project/Folder");

					};

					radio_button_gnome.Toggled += delegate {

						if (radio_button_gnome.Active)
							FolderEntry.ExampleText = _("Project");

					};


					radio_button.Toggled += delegate {

						if (radio_button.Active) {

							FolderEntry.ExampleText = _("Folder");
							ServerEntry.Sensitive   = true;
							CheckServerForm ();

						} else {

							ServerEntry.Sensitive = false;
							CheckServerForm ();

						}

						ShowAll ();

					};

				table.Attach (layout_server,          0, 2, 1, 2);
				table.Attach (radio_button_github,    0, 2, 2, 3);
				table.Attach (radio_button_gitorious, 0, 2, 3, 4);
				table.Attach (radio_button_gnome,     0, 2, 4, 5);

				HBox layout_folder = new HBox (true, 0);

					FolderEntry = new SparkleEntry () {
						ExampleText = _("Folder")
					};
					
					FolderEntry.Changed += CheckServerForm;

					Label folder_label = new Label ("<b>" + _("Folder Name:") + "</b>") {
						UseMarkup = true,
						Xalign    = 1
					};

				(radio_button.Child as Label).UseMarkup = true;

				layout_folder.PackStart (folder_label, true, true, 12);
				layout_folder.PackStart (FolderEntry, true, true, 0);

					SyncButton = new Button (_("Sync"));
	
					SyncButton.Clicked += delegate {

						string name = FolderEntry.Text;

						// Remove the starting slash if there is one
						if (name.StartsWith ("/"))
							name = name.Substring (1);

						string server = ServerEntry.Text;

						if (name.EndsWith ("/"))
							name = name.TrimEnd ("/".ToCharArray ());

						if (name.StartsWith ("/"))
							name = name.TrimStart ("/".ToCharArray ());

						if (server.StartsWith ("ssh://"))
							server = server.Substring (6);

						if (radio_button.Active) {

							// Use the default user 'git' if no username is specified
							if (!server.Contains ("@"))
								server = "git@" + server;

							// Prepend the Secure Shell protocol when it isn't specified
							if (!server.StartsWith ("ssh://"))
								server = "ssh://" + server;

							// Remove the trailing slash if there is one
							if (server.EndsWith ("/"))
								server = server.TrimEnd ("/".ToCharArray ());

						}

						if (radio_button_gitorious.Active) {

							server = "ssh://git@gitorious.org";

							if (!name.EndsWith (".git")) {

								if (!name.Contains ("/"))
									name = name + "/" + name;

								name += ".git";

							}

						}

						if (radio_button_github.Active)
							server = "ssh://git@github.com";

						if (radio_button_gnome.Active)
							server = "ssh://git@gnome.org/git/";

						string url  = server + "/" + name;
						Console.WriteLine ("View", "[" + name + "] Formed URL: " + url);

						string canonical_name = System.IO.Path.GetFileNameWithoutExtension (name);

				
						DeleteEvent += PreventClose;
						ShowSyncingPage (canonical_name);

				
						SparkleShare.Controller.FolderFetched += delegate {
		
							DeleteEvent -= PreventClose;
					
							Application.Invoke (delegate {
								ShowSuccessPage (name);
							});
					
						};
				
						SparkleShare.Controller.FolderFetchError += delegate {
				
							DeleteEvent -= PreventClose;
				
							Application.Invoke (delegate { ShowErrorPage (); });	
				
						};
		
				
						SparkleShare.Controller.FetchFolder (url, name);
		
					};


				if (ServerFormOnly) {

					Button cancel_button = new Button (_("Cancel"));

					cancel_button.Clicked += delegate {
						Destroy ();
					};

					AddButton (cancel_button);


				} else {

					Button skip_button = new Button (_("Skip"));

					skip_button.Clicked += delegate {
						ShowCompletedPage ();
					};

					AddButton (skip_button);

				}

				AddButton (SyncButton);

			layout_vertical.PackStart (header, false, false, 0);
			layout_vertical.PackStart (new Label (""), false, false, 3);
			layout_vertical.PackStart (table, false, false, 0);
			layout_vertical.PackStart (layout_folder, false, false, 6);

			Add (layout_vertical);

			CheckServerForm ();

			ShowAll ();
		
		}


		public void ShowInvitationPage (string server, string folder, string token)
		{

			VBox layout_vertical = new VBox (false, 0);

				Label header = new Label ("<span size='x-large'><b>" +
						                _("Invitation received!") +
						                  "</b></span>") {
					UseMarkup = true,
					Xalign = 0
				};

				Label information = new Label (_("You've received an invitation to join a shared folder.\n" +
						                         "We're ready to hook you up immediately if you wish.")) {
					Xalign = 0,
					Wrap   = true
				};

				Label question = new Label (_("Do you accept this invitation?")) {
					Xalign = 0,
					Wrap   = true
				};

				Table table = new Table (2, 2, false) {
					RowSpacing = 6
				};

					Label server_label = new Label (_("Server Address:")) {
						Xalign    = 0
					};

					Label server_text = new Label ("<b>" + server + "</b>") {
						UseMarkup = true,
						Xalign    = 0
					};

					Label folder_label = new Label (_("Folder Name:")) {
						Xalign    = 0
					};

					Label folder_text = new Label ("<b>" + folder + "</b>") {
						UseMarkup = true,
						Xalign    = 0
					};

				table.Attach (folder_label, 0, 1, 0, 1);
				table.Attach (folder_text, 1, 2, 0, 1);
				table.Attach (server_label, 0, 1, 1, 2);
				table.Attach (server_text, 1, 2, 1, 2);

				Button reject_button = new Button (_("Reject"));
				Button accept_button = new Button (_("Accept and Sync"));

					reject_button.Clicked += delegate {

						Destroy ();

					};

					accept_button.Clicked += delegate {

						string url  = "ssh://git@" + server + "/" + folder;		
				
						SparkleShare.Controller.FolderFetched += delegate {
		
							Application.Invoke (delegate {
								ShowSuccessPage (folder);
							});
					
						};
				
						SparkleShare.Controller.FolderFetchError += delegate {
							
							Application.Invoke (delegate { ShowErrorPage (); });
				
						};
		
				
						SparkleShare.Controller.FetchFolder (url, folder);

					};

				AddButton (reject_button);
				AddButton (accept_button);

			layout_vertical.PackStart (header, false, false, 0);
			layout_vertical.PackStart (information, false, false, 21);
			layout_vertical.PackStart (new Label (""), false, false, 0);
			layout_vertical.PackStart (table, false, false, 0);
			layout_vertical.PackStart (new Label (""), false, false, 0);
			layout_vertical.PackStart (question, false, false, 21);

			Add (layout_vertical);

			ShowAll ();

		}
		
		
		// The page shown when syncing has failed
		private void ShowErrorPage ()
		{

			Reset ();
			
			VBox layout_vertical = new VBox (false, 0);
	
				Label header = new Label ("<span size='x-large'><b>" +
						                _("Something went wrong…") +
						                  "</b></span>\n") {
					UseMarkup = true,
					Xalign = 0
				};

					Button try_again_button = new Button (_("Try Again")) {
						Sensitive = true
					};
	
					try_again_button.Clicked += delegate (object o, EventArgs args) {
						ShowServerForm ();
					};
	
				AddButton (try_again_button);

			layout_vertical.PackStart (header, false, false, 0);

			Add (layout_vertical);

			ShowAll ();

		}


		// The page shown when syncing has succeeded
		private void ShowSuccessPage (string folder_name)
		{

			Reset ();

				VBox layout_vertical = new VBox (false, 0);

					Label header = new Label ("<span size='x-large'><b>" +
								            _("Folder synced successfully!") +
								              "</b></span>") {
						UseMarkup = true,
						Xalign = 0
					};
		
					Label information = new Label (
						String.Format(_("Now you can access the synced files from ‘{0}’ in your SparkleShare folder."),
							folder_name)) {
						Xalign = 0,
						Wrap   = true,
						UseMarkup = true
					};

						// A button that opens the synced folder
						Button open_folder_button = new Button (_("Open Folder"));

						open_folder_button.Clicked += delegate {

							SparkleShare.Controller.OpenSparkleShareFolder (System.IO.Path.GetFileNameWithoutExtension(folder_name));

						};

						Button finish_button = new Button (_("Finish"));
	
						finish_button.Clicked += delegate (object o, EventArgs args) {
							Destroy ();
						};
	
					AddButton (open_folder_button);
					AddButton (finish_button);

				layout_vertical.PackStart (header, false, false, 0);
				layout_vertical.PackStart (information, false, false, 21);

			Add (layout_vertical);

			ShowAll ();
		
		}

		private ProgressBar ProgressBar;
		// The page shown whilst syncing
		private void ShowSyncingPage (string name)
		{

			Reset ();

				ProgressBar = new ProgressBar () {
					Fraction = 0
				};

				VBox layout_vertical = new VBox (false, 0);

					Label header = new Label ("<span size='x-large'><b>" +
							                        String.Format (_("Syncing folder ‘{0}’…"), name) +
							                        "</b></span>") {
						UseMarkup = true,
						Xalign    = 0,
						Wrap      = true
					};

					Label information = new Label (_("This may take a while.\n") +
					                               _("You sure it’s not coffee o-clock?")) {
						UseMarkup = true,
						Xalign = 0
					};


						Button button = new Button () {
							Sensitive = false,
							Label = _("Finish")
						};
		
						button.Clicked += delegate {
							Destroy ();
						};

					AddButton (button);

					SparkleSpinner spinner = new SparkleSpinner (22);

				Table table = new Table (3, 2, false) {
					RowSpacing    = 12,
					ColumnSpacing = 9
				};

				HBox box = new HBox (false, 0);

				table.Attach (spinner,      0, 1, 0, 1);
				table.Attach (header, 1, 2, 0, 1);
				table.Attach (information,  1, 2, 1, 2);
				table.Attach (ProgressBar,  2, 3, 0, 2);

				box.PackStart (table, false, false, 0);

				layout_vertical.PackStart (box, false, false, 0);

			Add (layout_vertical);

			ShowAll ();

		}


		// The page shown when the setup has been completed
		private void ShowCompletedPage ()
		{

			Reset ();

				TextBuffer pubkeyBuffer = new TextBuffer(new TextTagTable ());

				while (null == SparkleShare.Controller.PublicKey)
				{
					// ugly fix to avoid race condition on ssh keyfiles this shouldn't be a problem, since
					// the keys usually gets generated in background while the user is configuring his own
					// repository.
				}

				pubkeyBuffer.Text = SparkleShare.Controller.PublicKey;


				VBox layout_vertical = new VBox (false, 0);

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


				Label pubkeyLabel = new Label(_("This is your <b>public</b> SSH key generated by SparkleShare!" + "\n" +
												"Copy and paste it into your dashboard (in Gitorious and Github)"))
				{
					UseMarkup = true,
					Wrap      = false,
					Xalign    = 0
				};

				TextView pubkeyView = new TextView (pubkeyBuffer)
				{
					Editable  = false,
					CursorVisible = false,
					WrapMode = WrapMode.Char,
					LeftMargin = 2,
					RightMargin = 2
				};


				HBox link_wrapper = new HBox (false, 0);
				LinkButton link = new LinkButton ("http://www.sparkleshare.org/",
					_("Learn how to host your own SparkleServer"));

				link_wrapper.PackStart (link, false, false, 0);

				layout_vertical.PackStart (header, false, false, 0);
				layout_vertical.PackStart (information, false, false, 21);
				layout_vertical.PackStart (pubkeyLabel, false, false, 10);
				layout_vertical.PackStart (pubkeyView, false, false, 2);
				layout_vertical.PackStart (link_wrapper, false, false, 0);

					Button finish_button = new Button (_("Finish"));

					finish_button.Clicked += delegate (object o, EventArgs args) {

						Destroy ();

					};

				AddButton (finish_button);

			Add (layout_vertical);

			ShowAll ();
		
		}


		// Enables or disables the 'Next' button depending on the 
		// entries filled in by the user
		private void CheckAccountForm ()
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
		public void CheckServerForm (object o, EventArgs args)
		{

			CheckServerForm ();

		}


		// Enables the Add button when the fields are
		// filled in correctly
		public void CheckServerForm ()
		{

			SyncButton.Sensitive = false;

			if (FolderEntry.ExampleTextActive ||
			    (ServerEntry.Sensitive && ServerEntry.ExampleTextActive))
				return;

			bool IsFolder = !FolderEntry.Text.Trim ().Equals ("");
			bool IsServer = !ServerEntry.Text.Trim ().Equals ("");

			if (ServerEntry.Sensitive == true) {
			
				if (IsServer && IsFolder)
					SyncButton.Sensitive = true;

			} else if (IsFolder) {

					SyncButton.Sensitive = true;

			}

		}


		// Checks to see if an email address is valid
		private bool IsValidEmail (string email)
		{

			Regex regex = new Regex (@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.IgnoreCase);
			return regex.IsMatch (email);

		}

	}

}
