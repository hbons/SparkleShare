//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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
using System.IO;
using System.Threading;

using Sparkles;

namespace SparkleShare {

    public partial class PageController {

        void LoadPresets ()
        {
            int local_presets_count = 0;
            string local_presets_path = Preset.LocalPresetsPath;

            if (Directory.Exists (local_presets_path))
                // Local presets go first...
                foreach (string xml_file_path in Directory.GetFiles (local_presets_path, "*.xml")) {
                    Presets.Add (new Preset (xml_file_path));
                    local_presets_count++;
                }

            // ...system presets after that...
            if (Directory.Exists (SparkleShare.Controller.PresetsPath)) {
                foreach (string xml_file_path in Directory.GetFiles (SparkleShare.Controller.PresetsPath, "*.xml")) {
                    // ...and "Own server" at the very top
                    if (xml_file_path.EndsWith ("own-server.xml"))
                        Presets.Insert (0, new Preset (xml_file_path));
                    else
                        Presets.Add (new Preset (xml_file_path));
                }
            }

            SelectedPreset = Presets [0];
        }


        public int SelectedPresetIndex {
            get {
                return Presets.IndexOf (SelectedPreset);
            }
        }


        public void SelectedPresetChanged (int preset_index)
        {
            SelectedPreset = Presets [preset_index];
        }


        public void HostPageCompleted ()
        {
            ChangePageEvent (PageType.Address);

            string host = SelectedPreset.Address;

            if (string.IsNullOrEmpty (host)) {
                if (string.IsNullOrEmpty (SelectedPreset.Host)) {
                    new Thread (() => {
                        Thread.Sleep (page_delay);
                        AddressPagePublicKeyEvent (false, "", SelectedPreset.KeyEntryHint);

                    }).Start ();

                    return;
                }

                host = "ssh://" + SelectedPreset.Host;
            }

            new Thread (() => {
                bool authenticated = true;
                string auth_status = "✓ Authenticated";

                try {
                    authenticated = SSHFetcher.CanAuthenticateTo (
                        new Uri (host), SparkleShare.Controller.UserAuthenticationInfo);

                } catch (NetworkException) {
                    authenticated = false;
                }

                if (authenticated) {
                    if (!string.IsNullOrEmpty (SelectedPreset.Address))
                        auth_status += string.Format (" to {0}", new Uri (host).Host);

                } else {
                    string public_key = "<tt>" + SparkleShare.Controller.UserAuthenticationInfo.PublicKey.Substring (0, 40) + "…</tt>";
                    auth_status = public_key;
                }

                AddressPagePublicKeyEvent (authenticated, auth_status, SelectedPreset.KeyEntryHint);

            }).Start ();
        }
    }
}
