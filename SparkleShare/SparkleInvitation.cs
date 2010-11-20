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
using SparkleLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml;

namespace SparkleShare {

	class SparkleInvitation : SparkleWindow {

		public string Server;
		public string Folder;
		public string InviteKey;
		public string FilePath;


		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleInvitation (string file_path)
		{

			if (!File.Exists (file_path))
				return;

			FilePath = file_path;

			XmlDocument xml_doc = new XmlDocument (); 
			xml_doc.Load (file_path);

			XmlNodeList server_xml     = xml_doc.GetElementsByTagName ("server");
			XmlNodeList folder_xml     = xml_doc.GetElementsByTagName ("folder");
			XmlNodeList invite_key_xml = xml_doc.GetElementsByTagName ("invite_key");

			Server    = server_xml [0].InnerText;
			Folder    = folder_xml [0].InnerText;
			InviteKey = invite_key_xml [0].InnerText;

		}


		// Uploads the user's public key to the
		// server and starts the syncing process
		public void Configure ()
		{

			// TODO: Move to controller
			
			// The location of the user's public key for SparkleShare
			string public_key_file_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".ssh",
				"sparkleshare." + SparkleShare.Controller.UserEmail + ".key.pub");

			if (!File.Exists (public_key_file_path))
				return;

			StreamReader reader = new StreamReader (public_key_file_path);
			string public_key = reader.ReadToEnd ();
			reader.Close ();

			string url = "http://" + Server + "/folder=" + Folder +
			                                  "&invite=" + InviteKey +
			                                  "&key="    + public_key;

			SparkleHelpers.DebugInfo ("WebRequest", url);

			HttpWebRequest request   = (HttpWebRequest) WebRequest.Create (url);
			HttpWebResponse response = (HttpWebResponse) request.GetResponse();

			if (response.StatusCode == HttpStatusCode.OK)
				File.Delete (FilePath);

			response.Close ();

		}


		new public void Present ()
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

					Label server = new Label ("<b>" + Server + "</b>") {
						UseMarkup = true,
						Xalign    = 0
					};

					Label folder_label = new Label (_("Folder Name:")) {
						Xalign    = 0
					};

					Label folder = new Label ("<b>" + Folder + "</b>") {
						UseMarkup = true,
						Xalign    = 0
					};

				table.Attach (folder_label, 0, 1, 0, 1);
				table.Attach (folder, 1, 2, 0, 1);
				table.Attach (server_label, 0, 1, 1, 2);
				table.Attach (server, 1, 2, 1, 2);

				Button reject_button = new Button (_("Reject"));
				Button accept_button = new Button (_("Accept and Sync"));

					reject_button.Clicked += delegate {

						Destroy ();

					};

					accept_button.Clicked += delegate {

						string url  = "ssh://git@" + Server + "/" + Folder;
						SparkleHelpers.DebugInfo ("Git", "[" + Folder + "] Formed URL: " + url);


				
						SparkleShare.Controller.FolderFetched += delegate {
		
							Application.Invoke (delegate {
								ShowSuccessPage (Folder);
							});
					
						};
				
						SparkleShare.Controller.FolderFetchError += delegate {
							
							Application.Invoke (delegate { ShowErrorPage (); });	
				
						};
		
				
						SparkleShare.Controller.FetchFolder (url, Folder);

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

			base.Present ();

		}


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

					Button close_button = new Button (_("Close")) {
						Sensitive = true
					};
	
					close_button.Clicked += delegate (object o, EventArgs args) {
						Destroy ();
					};
	
				AddButton (close_button);

			layout_vertical.PackStart (header, false, false, 0);

			Add (layout_vertical);

			ShowAll ();

		}


		private void ShowSuccessPage (string name)
		{

			Reset ();

					VBox layout_vertical = new VBox (false, 0);

						Label header = new Label ("<span size='x-large'><b>" +
								                _("Folder synced successfully!") +
								                  "</b></span>") {
							UseMarkup = true,
							Xalign = 0
						};
				
						Label information = new Label (String.Format(_("Now you can access the synced files from ‘{0}’ in your SparkleShare folder."),
					                                             name)) {
							Xalign = 0,
							Wrap   = true,
							UseMarkup = true
						};

							Button open_folder_button = new Button (_("Open Folder"));

							open_folder_button.Clicked += delegate (object o, EventArgs args) {

								string path = SparkleHelpers.CombineMore (SparklePaths.SparklePath, name);

								Process process = new Process ();
								process.StartInfo.FileName  = "xdg-open";
								process.StartInfo.Arguments = path.Replace (" ", "\\ "); // Escape space-characters
								process.Start ();

								Destroy ();

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

		// TODO: These pages are identical to the intro ones,
		// find a way to remove duplication
		private void ShowSyncingPage (string name)
		{

			Reset ();
			
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

			Add (layout_vertical);

			ShowAll ();

		}

	}
	
}
