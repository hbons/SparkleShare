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


using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

using Gtk;
using Mono.Unix;
using Notifications;

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

		private Notebook assistant;
		
		//
		private RadioButton radio_button_server;
		private RadioButton radio_button_github;
		private RadioButton radio_button_gnome ;
		private RadioButton radio_button_gitorious;
		
		//
		private Label success_information;
		private Button open_folder_button;
		private Button success_finish_button;
				
		private Label syncing_page_header;
		
		private Button finish_button;
		
		private Button try_again_button;

		private Button reject_button;
		private Button accept_button;

		private Label server_text;
		private Label folder_text;
		
		private Table account_form_table;

		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleIntro () : base ()
		{

			ServerFormOnly = false;
			SecondaryTextColor = SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive));
			//TODO: add the assistant pages
			assistant = new Notebook();
			assistant.AppendPage( GetAccountForm(), new Label ( "GetAccountForm" ) );
			assistant.AppendPage( GetCompletedPage( ), new Label ( "GetErrorPage" ) );
			assistant.AppendPage( GetErrorPage( ), new Label ( "GetErrorPage" ) );
			assistant.AppendPage( GetInvitationPage( ), new Label ( "GetInvitationPage" ) );
			
			assistant.AppendPage( GetServerForm(), new Label ( "GetServerForm" ) );
			assistant.AppendPage( GetSuccessPage(  ), new Label ( "GetSuccessPage" ) );
			assistant.AppendPage( GetSyncingPage( ), new Label ( "GetSyncingPage" ) );
			assistant.ShowTabs = false;
			assistant.ShowBorder = false;
			Add(assistant);

			finish_button = new Button (_("Finish"));

			finish_button.Clicked += delegate (object o, EventArgs args) {
				Close ();
			};

		}

		public void ShowAccountForm ()
		{
			ClearButtons();
			
			assistant.CurrentPage = 0;
			
			NextButton = new Button (_("Next")) {
				Sensitive = false
			};

			NextButton.Clicked += delegate (object o, EventArgs args) {

				NextButton.Remove (NextButton.Child);
				NextButton.Add (new Label (_("Configuring…")));

				NextButton.Sensitive = false;
				//todo:!
				account_form_table.Sensitive       = false;

				NextButton.ShowAll ();
	
				SparkleShare.Controller.UserName  = NameEntry.Text;
				SparkleShare.Controller.UserEmail = EmailEntry.Text;

				SparkleShare.Controller.GenerateKeyPair ();
				SparkleShare.Controller.AddKey ();
		
				SparkleShare.Controller.FirstRun = false;
		
				Deletable = true;
				ShowServerForm ();

			};

			AddButton (NextButton);		
			CheckAccountForm ();
		}
		public Widget GetAccountForm ()
		{

			VBox layout_vertical = new VBox (false, 0);
			
				Deletable = false;

				Label header = new Label ("<span size='large'><b>" +
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

				account_form_table = new Table (2, 2, false) {
					RowSpacing = 6
				};

					string full_name  = new UnixUserInfo (UnixEnvironment.UserName).RealName;
                                        if (string.IsNullOrEmpty (full_name))
						full_name = "";

					Label name_label = new Label ("<b>" + _("Full Name:") + "</b>") {
						UseMarkup = true,
						Xalign    = 0
					};

					NameEntry = new Entry (full_name.TrimEnd (",".ToCharArray()));
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


				account_form_table.Attach (name_label, 0, 1, 0, 1);
				account_form_table.Attach (NameEntry, 1, 2, 0, 1);
				account_form_table.Attach (email_label, 0, 1, 1, 2);
				account_form_table.Attach (EmailEntry, 1, 2, 1, 2);

			layout_vertical.PackStart (header, false, false, 0);
			layout_vertical.PackStart (information, false, false, 21);
			layout_vertical.PackStart (new Label (""), false, false, 0);
			layout_vertical.PackStart (account_form_table, false, false, 0);

			return layout_vertical;
		
		}
		public void ShowServerForm (bool server_form_only)
		{
			ServerFormOnly = server_form_only;
			ShowServerForm ();
		}
		public void ShowServerForm ()
		{

			ClearButtons();
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

						if (radio_button_server.Active) {

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

						ShowSyncingPage (canonical_name);
				
						SparkleShare.Controller.FolderFetched += delegate {
		
							Application.Invoke (delegate {

								Deletable = true;
								ShowSuccessPage (name);
								
							});
					
						};
				
						SparkleShare.Controller.FolderFetchError += delegate {
				
							Application.Invoke (delegate {

								Deletable = true;
								ShowErrorPage ();

							});	
				
						};
		
				
						SparkleShare.Controller.FetchFolder (url, name);
		
					};



				if (ServerFormOnly) {

					Button cancel_button = new Button (_("Cancel"));

					cancel_button.Clicked += delegate {
						Close ();
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

			
			assistant.CurrentPage = 4;
			CheckServerForm ();
			
		}
		public Widget GetServerForm ()
		{

			VBox layout_vertical = new VBox (false, 0);

				Label header = new Label ("<span size='large'><b>" +
						                        _("Where is your remote folder?") +
						                        "</b></span>") {
					UseMarkup = true,
					Xalign = 0
				};

				Table table = new Table (5, 2, false) {
					RowSpacing = 12
				};

					HBox layout_server = new HBox (false, 0);

						ServerEntry = new SparkleEntry () {
							ExampleText = _("address-to-server.com")
						};
						
						ServerEntry.Changed += CheckServerForm;

						radio_button_server = new RadioButton ("<b>" + _("On my own server:") + "</b>");

					layout_server.Add (radio_button_server);
					layout_server.Add (ServerEntry);
					
					string github_text = "<b>" + "Github" + "</b>\n" +
						  "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
						_("Free hosting for Free and Open Source Software projects.") + "\n" + 
						_("Also has paid accounts for extra private space and bandwidth.") +
						  "</span>";

					radio_button_github = new RadioButton (radio_button_server, github_text);

					(radio_button_github.Child as Label).UseMarkup = true;
					(radio_button_github.Child as Label).Wrap      = true;

					string gnome_text = "<b>" + _("The GNOME Project") + "</b>\n" +
						  "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
						_("GNOME is an easy to understand interface to your computer.") + "\n" +
						_("Select this option if you’re a developer or designer working on GNOME.") +
						  "</span>";

					radio_button_gnome = new RadioButton (radio_button_server, gnome_text);

					(radio_button_gnome.Child as Label).UseMarkup = true;
					(radio_button_gnome.Child as Label).Wrap      = true;

					string gitorious_text = "<b>" + _("Gitorious") + "</b>\n" +
						  "<span fgcolor='" + SecondaryTextColor + "' size='small'>" +
						_("Completely Free as in Freedom infrastructure.") + "\n" +
						_("Free accounts for Free and Open Source projects.") +
						  "</span>";
					
					radio_button_gitorious = new RadioButton (radio_button_server, gitorious_text) {
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


					radio_button_server.Toggled += delegate {

						if (radio_button_server.Active) {

							FolderEntry.ExampleText = _("Folder");
							ServerEntry.Sensitive   = true;
							CheckServerForm ();

						} else {

							ServerEntry.Sensitive = false;
							CheckServerForm ();

						}

						ShowAll ();

					};

				table.Attach (layout_server,          0, 2, 0, 1);
				table.Attach (radio_button_github,    0, 2, 1, 2);
				table.Attach (radio_button_gitorious, 0, 2, 2, 3);
				table.Attach (radio_button_gnome,     0, 2, 3, 4);

				HBox layout_folder = new HBox (true, 0);

					FolderEntry = new SparkleEntry () {
						ExampleText = _("Folder")
					};
					
					FolderEntry.Changed += CheckServerForm;

					Label folder_label = new Label (_("Folder Name:")) {
						UseMarkup = true,
						Xalign    = 1
					};

				(radio_button_server.Child as Label).UseMarkup = true;

				layout_folder.PackStart (folder_label, true, true, 12);
				layout_folder.PackStart (FolderEntry, true, true, 0);



			layout_vertical.PackStart (header, false, false, 0);
			layout_vertical.PackStart (new Label (""), false, false, 3);
			layout_vertical.PackStart (table, false, false, 0);
			layout_vertical.PackStart (layout_folder, false, false, 6);

			
			
			return layout_vertical;
		}

		public void ShowInvitationPage (string server, string folder, string token)
		{
			ClearButtons();
			
			
		folder_text.Markup = "<b>" + folder + "</b>";
		server_text.Markup = "<b>" + server + "</b>";
			assistant.CurrentPage = 3;
			reject_button = new Button (_("Reject"));
			accept_button = new Button (_("Accept and Sync"));

				reject_button.Clicked += delegate {
					Close ();
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
		}
		public Widget GetInvitationPage ( )
		{

			VBox layout_vertical = new VBox (false, 0);

				Label header = new Label ("<span size='large'><b>" +
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

					server_text = new Label ("<b></b>") {
						UseMarkup = true,
						Xalign    = 0
					};

					Label folder_label = new Label (_("Folder Name:")) {
						Xalign    = 0
					};

					folder_text = new Label ("<b></b>") {
						UseMarkup = true,
						Xalign    = 0
					};

				table.Attach (folder_label, 0, 1, 0, 1);
				table.Attach (folder_text, 1, 2, 0, 1);
				table.Attach (server_label, 0, 1, 1, 2);
				table.Attach (server_text, 1, 2, 1, 2);


			layout_vertical.PackStart (header, false, false, 0);
			layout_vertical.PackStart (information, false, false, 21);
			layout_vertical.PackStart (new Label (""), false, false, 0);
			layout_vertical.PackStart (table, false, false, 0);
			layout_vertical.PackStart (new Label (""), false, false, 0);
			layout_vertical.PackStart (question, false, false, 21);

			return layout_vertical;
		}
		
		private void ShowErrorPage ()
		{
			ClearButtons();
			assistant.CurrentPage = 2;
    
				try_again_button = new Button (_("Try Again")) {
					Sensitive = true
				};
	
				try_again_button.Clicked += delegate (object o, EventArgs args) {
					ShowServerForm ();
				};
	
			AddButton (try_again_button);			
		}
		// The page shown when syncing has failed
		private Widget GetErrorPage ()
		{

			
			
                UrgencyHint = true;

                VBox layout_vertical = new VBox (false, 0);
    	
    				Label header = new Label ("<span size='large'><b>" +
    						                _("Something went wrong…") +
    						                  "</b></span>\n") {
    					UseMarkup = true,
    					Xalign = 0
    				};

    
    			layout_vertical.PackStart (header, false, false, 0);
			return layout_vertical;
		}

		
		private void ShowSuccessPage (string folder_name) {
			ClearButtons();
            UrgencyHint = true;
	        if (!HasToplevelFocus) {

                string title   = String.Format (_("‘{0}’ has been successfully added"), folder_name);
                string subtext = _("");

                new SparkleBubble (title, subtext).Show ();

            }
			
			assistant.CurrentPage = 5;

			success_information.Markup = String.Format (_("Now you can access the synced files from ‘{0}’ in your SparkleShare folder."),
							folder_name); 
						// A button that opens the synced folder
						open_folder_button = new Button (_("Open Folder"));

						open_folder_button.Clicked += delegate {

							SparkleShare.Controller.OpenSparkleShareFolder (System.IO.Path.GetFileNameWithoutExtension(folder_name));

						};

						success_finish_button = new Button (_("Finish"));
	
						success_finish_button.Clicked += delegate (object o, EventArgs args) {
							Close ();
						};
	
					AddButton (open_folder_button);
					AddButton (success_finish_button);		

		}
			
			// The page shown when syncing has succeeded
		private Widget GetSuccessPage ()
		{
			string folder_name = "test";
				VBox layout_vertical = new VBox (false, 0);

					Label header = new Label ("<span size='large'><b>" +
								            _("Folder synced successfully!") +
								              "</b></span>") {
						UseMarkup = true,
						Xalign = 0
					};
		
					success_information = new Label (
						String.Format (_("Now you can access the synced files from ‘{0}’ in your SparkleShare folder."),
							folder_name
			            )) {
						Xalign = 0,
						Wrap   = true,
						UseMarkup = true
					};
				layout_vertical.PackStart (header, false, false, 0);
				layout_vertical.PackStart (success_information, false, false, 21);

			return (layout_vertical);
		}
		private void ShowSyncingPage (string name)
		{
			ClearButtons();
			assistant.CurrentPage = 6;
			syncing_page_header.Markup = "<span size='large'><b>" +
							                        String.Format (_("Syncing folder ‘{0}’…"), name) +
							                        "</b></span>";
			finish_button.Sensitive = false;
			AddButton(finish_button);
		}
		
		// The page shown whilst syncing
		private Widget GetSyncingPage ()
		{
				Deletable = false;

				VBox layout_vertical = new VBox (false, 0);

					syncing_page_header = new Label ("") {
						UseMarkup = true,
						Xalign    = 0,
						Wrap      = true
					};

					Label information = new Label (_("This may take a while.\n") +
					                               _("Are you sure it’s not coffee o'clock?")) {
						UseMarkup = true,
						Xalign = 0
					};

//					SparkleSpinner spinner = new SparkleSpinner (22);

				Table table = new Table (3, 2, false) {
					RowSpacing    = 12,
					ColumnSpacing = 9
				};

				HBox box = new HBox (false, 0);

//				table.Attach (spinner, 0, 1, 0, 1);
				table.Attach (syncing_page_header, 1, 2, 0, 1);
				table.Attach (information,  1, 2, 1, 2);

				box.PackStart (table, false, false, 0);

				layout_vertical.PackStart (box, false, false, 0);

			return layout_vertical;
		}
		

		private void ShowCompletedPage ()
		{
			ClearButtons();
			assistant.CurrentPage = 1;	
			
			finish_button.Sensitive = true;
			AddButton (finish_button);			
		}
			// The page shown when the setup has been completed
		private Widget GetCompletedPage ()
		{

				VBox layout_vertical = new VBox (false, 0);

				Label header = new Label ("<span size='large'><b>" +
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

			return (layout_vertical);

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
