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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Gtk;
using Mono.Unix;
using SparkleLib;
using WebKit;

namespace SparkleShare {

	public class SparkleLog : Window {

		public readonly string LocalPath;

		private VBox LayoutVertical;
		private ScrolledWindow ScrolledWindow;
		private MenuBar MenuBar;
		private WebView WebView;
		private string LinkStatus;


		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleLog (string path) : base ("")
		{

			LocalPath = path;
			
			string name = System.IO.Path.GetFileName (LocalPath);
			SetDefaultSize (480, 640);

			BorderWidth = 0;
	 		SetPosition (WindowPosition.Center);

			// Open slightly off center for each consecutive window
			if (SparkleUI.OpenLogs.Count > 0) {

				int x, y;
				GetPosition (out x, out y);
				Move (x + SparkleUI.OpenLogs.Count * 20, y + SparkleUI.OpenLogs.Count * 20);

			}
			
			// TRANSLATORS: {0} is a folder name, and {1} is a server address
			Title = String.Format(_("Events in ‘{0}’"), name);
			IconName = "folder-sparkleshare";

			DeleteEvent += delegate {
				Close ();
			};
			
			LayoutVertical = new VBox (false, 0);

			CreateEventLog ();

			LayoutVertical.PackStart (ScrolledWindow, true, true, 0);

				UpdateEventLog ();

				HButtonBox dialog_buttons = new HButtonBox {
					Layout = ButtonBoxStyle.Edge,
					BorderWidth = 12
				};

					Button open_folder_button = new Button (_("_Open Folder")) {
						UseUnderline = true
					};
 
					open_folder_button.Clicked += delegate (object o, EventArgs args) {

						Process process = new Process ();
						process.StartInfo.FileName  = "xdg-open";
						process.StartInfo.Arguments = LocalPath.Replace (" ", "\\ "); // Escape space-characters
						process.Start ();

					};

					Button close_button = new Button (Stock.Close);

					close_button.Clicked += delegate {
						Close ();
					};

				dialog_buttons.Add (open_folder_button);
				dialog_buttons.Add (close_button);

			// We have to hide the menubar somewhere...
			LayoutVertical.PackStart (CreateShortcutsBar (), false, false, 0);
			LayoutVertical.PackStart (dialog_buttons, false, false, 0);

			Add (LayoutVertical);

		}


		public void CreateEventLog () {

			WebView = new WebView () {
				Editable = false
			};

			WebView.HoveringOverLink += delegate (object o, WebKit.HoveringOverLinkArgs args) {
				LinkStatus = args.Link;
			};

			WebView.NavigationRequested += delegate (object o, WebKit.NavigationRequestedArgs args) {

				if (args.Request.Uri == LinkStatus) {

					Process process = new Process ();
					process.StartInfo.FileName = "xdg-open";
					process.StartInfo.Arguments = args.Request.Uri.Replace (" ", "\\ "); // Escape space-characters
					process.Start ();

				}

				// TODO: Don't close window afterwards

			};

			ScrolledWindow = new ScrolledWindow ();
			ScrolledWindow.AddWithViewport (WebView);

		}


		public void UpdateEventLog ()
		{

			string html = SparkleShare.Controller.GetHTMLLog (System.IO.Path.GetFileName (LocalPath));

			html = html.Replace ("<!-- $body-font-size -->", (Style.FontDescription.Size / 1024 + 0.5) + "pt");
			html = html.Replace ("<!-- $body-font-family -->", "\"" + Style.FontDescription.Family + "\"");
			html = html.Replace ("<!-- $body-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Normal)));
			html = html.Replace ("<!-- $body-background-color -->", SparkleUIHelpers.GdkColorToHex (new TreeView ().Style.Base (StateType.Normal)));
			html = html.Replace ("<!-- $day-entry-header-background-color -->", SparkleUIHelpers.GdkColorToHex (Style.Background (StateType.Normal)));
			html = html.Replace ("<!-- $secondary-font-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));
			html = html.Replace ("<!-- $small-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));	
			html = html.Replace ("<!-- $no-buddy-icon-background-image -->", "file://" +
					SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
						"hicolor", "32x32", "status", "avatar-default.png"));

			WebView.LoadString (html, null, null, "file://");

			LayoutVertical.Remove (ScrolledWindow);
			ScrolledWindow = new ScrolledWindow ();
			Viewport viewport = new Viewport ();
			WebView.Reparent (viewport);
			ScrolledWindow.Add (viewport);
			(ScrolledWindow.Child as Viewport).ShadowType = ShadowType.None;
			LayoutVertical.PackStart (ScrolledWindow, true, true, 0);
			LayoutVertical.ReorderChild (ScrolledWindow, 0);

			ShowAll ();

		}


		public void Close ()
		{

			Destroy (); // TODO: keep logs in memory like Mac UI

		}


		private MenuBar CreateShortcutsBar () {

			// Adds a hidden menubar that contains to enable keyboard
			// shortcuts to close the log
			MenuBar = new MenuBar ();

				MenuItem file_item = new MenuItem ("File");

				    Menu file_menu = new Menu ();

						MenuItem close_1 = new MenuItem ("Close1");
						MenuItem close_2 = new MenuItem ("Close2");
		
						// adds specific Ctrl+W and Esc key accelerators to Log Window
						AccelGroup accel_group = new AccelGroup ();
						AddAccelGroup (accel_group);

						// Close on Esc
						close_1.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.W, Gdk.ModifierType.ControlMask,
							AccelFlags.Visible));

						close_1.Activated += delegate { Close (); };

						// Close on Ctrl+W
						close_2.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.Escape, Gdk.ModifierType.None,
							AccelFlags.Visible));
						close_2.Activated += delegate { Close (); };

					file_menu.Append (close_1);
					file_menu.Append (close_2);

				file_item.Submenu = file_menu;

			MenuBar.Append (file_item);

			// Hacky way to hide the menubar, but the accellerators
			// will simply be disabled when using Hide ()
			MenuBar.HeightRequest = 1;
			MenuBar.ModifyBg (StateType.Normal, Style.Background (StateType.Normal));

			return MenuBar;

		}

	}

}

