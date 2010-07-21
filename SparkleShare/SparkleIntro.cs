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
using SparkleShare;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Security.Cryptography;

namespace SparkleShare {

	public class SparkleIntro : Window {

		public Entry NameEntry;
		public Entry EmailEntry;
		public Entry ServerEntry;

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


		public void ShowStepOne ()
		{

			HBox layout_horizontal = new HBox (false, 6);

				// TODO: Fix the path
				Image side_splash = new Image ("/home/hbons/github/SparkleShare/data/side-splash.png");

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

							UnixUserInfo unix_user_info = new UnixUserInfo (UnixEnvironment.UserName);			

							Label name_label = new Label ("<b>" + _("Full Name:") + "</b>") {
								UseMarkup = true,
								Xalign    = 0
							};

							NameEntry = new Entry (unix_user_info.RealName);

				
						Table table = new Table (6, 2, true) {
							RowSpacing = 6
						};

							EmailEntry = new Entry ("");
							Label email_label = new Label ("<b>" + _("Email:") + "</b>") {
								UseMarkup = true,
								Xalign    = 0
							};

							Entry server_entry = new Entry ("ssh://gitorious.org/sparkleshare") {
								Sensitive = false
							};

							Label server_label = new Label ("<b>" + _("Folder Address:") + "</b>") {
								UseMarkup = true,
								Xalign = 0,
								Sensitive = false
							};
					
							CheckButton check_button;
							check_button = new CheckButton (_("I already subscribed to an existing " +
									                          "folder on a SparkleShare server"));

							check_button.Clicked += delegate {

								if (check_button.Active) {

									server_label.Sensitive = true;
									server_entry.Sensitive = true;
									server_entry.HasFocus = true;

								} else {

									server_label.Sensitive = false;
									server_entry.Sensitive = false;					

								}

								ShowAll ();

							};

						table.Attach (name_label, 0, 1, 0, 1);
						table.Attach (NameEntry, 1, 2, 0, 1);
						table.Attach (email_label, 0, 1, 1, 2);
						table.Attach (EmailEntry, 1, 2, 1, 2);
						table.Attach (check_button, 0, 2, 3, 4);
						table.Attach (server_label, 0, 1, 4, 5);
						table.Attach (server_entry, 1, 2, 4, 5);
				
						HButtonBox controls = new HButtonBox () {
							BorderWidth = 12,
							Layout      = ButtonBoxStyle.End
						};

							Button done_button = new Button (_("Next"));
			
							done_button.Clicked += delegate (object o, EventArgs args) {

								done_button.Remove (done_button.Child);

								HBox hbox = new HBox ();

								hbox.Add (new SparkleSpinner ());
								hbox.Add (new Label (_("Configuring…")));

								done_button.Add (hbox);

								done_button.Sensitive = false;
								table.Sensitive       = false;

								done_button.ShowAll ();

								Configure ();

								ShowStepTwo ();

							};
			
						controls.Add (done_button);

					layout_vertical.PackStart (introduction, false, false, 0);
					layout_vertical.PackStart (information, false, false, 21);
					layout_vertical.PackStart (new Label (""), false, false, 0);
					layout_vertical.PackStart (table, false, false, 0);

				wrapper.PackStart (layout_vertical, true, true, 0);

				wrapper.PackStart (controls, false, true, 0);

			layout_horizontal.PackStart (side_splash, false, false, 0);
			layout_horizontal.PackStart (wrapper, true, true, 0);

			Add (layout_horizontal);

			ShowAll ();
		
		}


		public void ShowStepTwo ()
		{
		
			Remove (Child);

			HBox layout_horizontal = new HBox (false, 6);

				Image side_splash = new Image ("/home/hbons/github/SparkleShare/data/side-splash.png");

			layout_horizontal.PackStart (side_splash, false, false, 0);
			
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
					Destroy ();
				};

			controls.Add (finish_button);

			wrapper.PackStart (layout_vertical, true, true, 0);
			wrapper.PackStart (controls, false, false, 0);

			layout_horizontal.Add (wrapper);

			Add (layout_horizontal);

			ShowAll ();
		
		}


		// Configure SparkleShare with the user's information
		public void Configure ()
		{

			string config_file_path = SparkleHelpers.CombineMore (SparklePaths.SparkleConfigPath, ".gitconfig");

			TextWriter writer = new StreamWriter (config_file_path);
			writer.WriteLine ("[user]\n" +
			                  "\tname  = " + NameEntry.Text + "\n" +
			                  "\temail = " + EmailEntry.Text + "\n");
			writer.Close ();

			GenerateKeyPair ();

			SparkleHelpers.DebugInfo ("Config", "Created '" + config_file_path + "'");

		}


		// Generates an RSA keypair to identify this system
		public void GenerateKeyPair ()
		{

		}

	}

}
