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
			XmlNodeList invite_key_xml = xml_doc.GetElementsByTagName ("invitekey");

			Server    = server_xml [0].InnerText;
			Folder    = folder_xml [0].InnerText;
			InviteKey = invite_key_xml [0].InnerText;

		}


		// Uploads the user's public key to the
		// server and starts the syncing process
		public void Configure ()
		{

			// The location of the user's public key for SparkleShare
			string public_key_file_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".ssh",
				"sparkleshare." + SparkleShare.UserEmail + ".key.pub");

			if (!File.Exists (public_key_file_path))
				return;

			StreamReader reader = new StreamReader (public_key_file_path);
			string public_key = reader.ReadToEnd ();
			reader.Close ();

			string url = "http://" + Server + "/folder=" + Folder +
			                                  "&invite=" + InviteKey +
			                                  "&key="    + public_key;

			SparkleHelpers.DebugInfo ("WebRequest", url);

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (url);
			HttpWebResponse response = (HttpWebResponse) request.GetResponse();

			if (response.StatusCode == HttpStatusCode.OK)
				File.Delete (FilePath);

			response.Close ();

		}


		public void PresentInvitation ()
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

				Table table = new Table (2, 2, true) {
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
				Button accept_button = new Button (_("Accept"));

					reject_button.Clicked += delegate {

						// Delete the invitation
						File.Delete (FilePath);

						Destroy ();

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

			Present ();

		}

	}
	
}
