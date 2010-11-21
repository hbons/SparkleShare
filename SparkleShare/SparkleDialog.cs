//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).

using Gtk;
using SparkleLib;
using Mono.Unix;
using System.Diagnostics;

namespace SparkleShare {

	public class SparkleDialog : Window	{

		// Short alias for the translations
		public static string _(string s)
		{
			return Catalog.GetString (s);
		}


		public SparkleDialog () : base ("")
		{

			BorderWidth    = 0;
			IconName       = "folder-sparkleshare";
			Resizable      = true;
			WindowPosition = WindowPosition.Center;
			Title          = "SparkleShare " + Defines.VERSION;
			Resizable = false;

			SetSizeRequest (480, 480);

			Label label = new Label () {
				Xalign = 0,
				Xpad = 12,
				Ypad = 12
			};

			Gdk.Color color = Style.Foreground (StateType.Insensitive);
			string secondary_text_color = SparkleUIHelpers.GdkColorToHex (color);

			label.Markup = "<b><span size='x-large'>SparkleShare</span></b>\n" +
			               "<span fgcolor='" + secondary_text_color + "'>version " + Defines.VERSION + "</span>\n\n" +

@"Copyright © 2010 Hylke Bons

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

Maintainer:

	Hylke Bons (hylkebons@gmail.com)

Contributors:

	Alex Hudson (home@alexhudson.com)
	Allan Day (allanpday@gmail.com)
	Andreas Nilsson (andreasn@gnome.org)
	Benjamin Podszun (benjamin.podszun@gmail.com)
	Bertrand Lorentz (bertrand.lorentz@gmail.com)
	Garrett LeSage (garrett@novell.com)
	Jakub Steiner (jimmac@redhat.com)
	Lapo Calamandrei (calamandrei@gmail.com)
	Luis Cordova (cordoval@gmail.com)
	Łukasz Jernaś (deejay1@srem.org)
	Michael Monreal (michael.monreal@gmail.com)
	Oleg Khlystov (pktfag@gmail.com)
	Paul Cutler (pcutler@gnome.org)
	Philipp Gildein (rmbl@openspeak-project.org)
	Ruben Vermeersch (rubenv@gnome.org)
	Sandy Armstrong (sanfordarmstrong@gmail.com)
	Simon Pither (simon@pither.com)
	Steven Harms (sharms@ubuntu.com)
	Vincent Untz (vuntz@gnome.org)

Thanks very much!";


/* Git# is Copyright © 2007-2009 by the Git Development Community
See source file headers for specific contributor copyrights.

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Git Development Community nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS 'AS IS' AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


SmartIrc4net - The IRC library for .NET/C#

Copyright © 2003-2005 Mirco Bauer (meebey@meebey.net)

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Lesser General Public License for more details."; */

			VBox wrapper = new VBox (false, 0);

				VBox vbox = new VBox (false, 0) {
					BorderWidth = 0
				};

					ScrolledWindow scrolled_window = new ScrolledWindow () {
						HscrollbarPolicy = PolicyType.Never,
						ShadowType = ShadowType.None,
						BorderWidth = 0
					};

					scrolled_window.AddWithViewport (label);

					(scrolled_window.Child as Viewport).ShadowType = ShadowType.None;
					(scrolled_window.Child as Viewport).ModifyBg (StateType.Normal,
						(new Entry () as Entry).Style.Base (StateType.Normal));

					HButtonBox button_bar = new HButtonBox () {
						BorderWidth = 12
					};

					Button close_button = new Button (Stock.Close);

							close_button.Clicked += delegate {
								Destroy ();
							};

						Button website_button = new Button (_("_Visit Website")) {
							UseUnderline = true
						};
			
							website_button.Clicked += delegate {

								Process process = new Process ();
								process.StartInfo.FileName = "xdg-open";
								process.StartInfo.Arguments = "http://www.sparkleshare.org/";
								process.Start ();

							};

					button_bar.Add (website_button);
					button_bar.Add (close_button);

				vbox.PackStart (scrolled_window, true, true, 0);
				vbox.PackStart (new HSeparator (), false, false, 0);
				vbox.PackStart (button_bar, false, false, 0);

					string image_path = SparkleHelpers.CombineMore (Defines.PREFIX, "share", "pixmaps",
						"sparkleshare-about.png");

				wrapper.PackStart (new Image (image_path), false, false, 0);
				wrapper.PackStart (vbox, true, true, 0);

			Add (wrapper);

		}

	}

}

