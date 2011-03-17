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
using System.Threading;

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
		private SparkleSpinner Spinner;
		private string HTML;


		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleLog (string path) : base ("")
		{

			LocalPath = path;
			
			string name = System.IO.Path.GetFileName (LocalPath);
			SetSizeRequest (480, 640);

            Resizable = false;

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

			DeleteEvent += Close;			

			CreateEventLog ();

		}


		private void CreateEventLog () {

			LayoutVertical = new VBox (false, 0);

				ScrolledWindow = new ScrolledWindow ();

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

                            UpdateEventLog ();

						}

						// FIXME: webview should stay on the same page

					};

				ScrolledWindow.AddWithViewport (WebView);

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
						HideAll ();
					};

				dialog_buttons.Add (open_folder_button);
				dialog_buttons.Add (close_button);

			// We have to hide the menubar somewhere...
			LayoutVertical.PackStart (CreateShortcutsBar (), false, false, 0);
			LayoutVertical.PackStart (dialog_buttons, false, false, 0);

			Add (LayoutVertical);

		}


		public void UpdateEventLog ()
		{

			if (HTML == null) {

				LayoutVertical.Remove (ScrolledWindow);
				Spinner = new SparkleSpinner (22);
				LayoutVertical.PackStart (Spinner, true, true, 0);

			}

			Thread thread = new Thread (new ThreadStart (delegate {

				GenerateHTML ();
				AddHTML ();

			}));

			thread.Start ();

		}


		private void GenerateHTML () {

			HTML = SparkleShare.Controller.GetHTMLLog (System.IO.Path.GetFileName (LocalPath));

            HTML = HTML.Replace ("<!-- $body-font-size -->", (Style.FontDescription.Size / 1024 + 0.5) + "pt");
            HTML = HTML.Replace ("<!-- $day-entry-header-font-size -->", (Style.FontDescription.Size / 1024 + 0.6) + "pt");
            HTML = HTML.Replace ("<!-- $a-color -->", "#0085cf");
            HTML = HTML.Replace ("<!-- $a-hover-color -->", "#009ff8");
			HTML = HTML.Replace ("<!-- $body-font-family -->", "\"" + Style.FontDescription.Family + "\"");
			HTML = HTML.Replace ("<!-- $body-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Normal)));
			HTML = HTML.Replace ("<!-- $body-background-color -->", SparkleUIHelpers.GdkColorToHex (new TreeView ().Style.Base (StateType.Normal)));
			HTML = HTML.Replace ("<!-- $day-entry-header-background-color -->", SparkleUIHelpers.GdkColorToHex (Style.Background (StateType.Normal)));
			HTML = HTML.Replace ("<!-- $secondary-font-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));
			HTML = HTML.Replace ("<!-- $small-color -->", SparkleUIHelpers.GdkColorToHex (Style.Foreground (StateType.Insensitive)));	
			HTML = HTML.Replace ("<!-- $no-buddy-icon-background-image -->", "file://" +
					SparkleHelpers.CombineMore (Defines.PREFIX, "share", "sparkleshare", "icons", 
						"hicolor", "32x32", "status", "avatar-default.png"));

		}


		private void AddHTML ()
		{

			Application.Invoke (delegate {

				WebView.LoadString (HTML, null, null, "file://");

				if (Spinner.Active) {

					LayoutVertical.Remove (Spinner);
					Spinner.Stop ();

				} else {

					LayoutVertical.Remove (ScrolledWindow);

				}

				ScrolledWindow = new ScrolledWindow ();
				Viewport viewport = new Viewport ();
				WebView.Reparent (viewport);
				ScrolledWindow.Add (viewport);
				(ScrolledWindow.Child as Viewport).ShadowType = ShadowType.None;
				LayoutVertical.PackStart (ScrolledWindow, true, true, 0);
				LayoutVertical.ReorderChild (ScrolledWindow, 0);

				LayoutVertical.ShowAll ();

			});

		}


		public void Close (object o, DeleteEventArgs args)
		{

			HideAll ();
			args.RetVal = true;
			// FIXME: window positions aren't saved

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

						close_1.Activated += delegate { HideAll (); };

						// Close on Ctrl+W
						close_2.AddAccelerator ("activate", accel_group, new AccelKey (Gdk.Key.Escape, Gdk.ModifierType.None,
							AccelFlags.Visible));
						close_2.Activated += delegate { HideAll (); };

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

