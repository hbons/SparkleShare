//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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

using Sparkles;
using Gtk;

namespace SparkleShare {

    public class StoragePage : Page {

        public StoragePage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header = string.Format ("Storage type for ‘{0}’", Controller.SyncingFolder);
            Description = "What type of storage would you like to use?";
        }


        public override object Render ()
        {
            // Radio buttons
            VBox radio_buttons = new VBox (false, 0) { BorderWidth = 12 };

            foreach (StorageTypeInfo storage_type in SparkleShare.Controller.FetcherAvailableStorageTypes) {
                RadioButton radio_button = new RadioButton (null,
                    storage_type.Name + "\n" + storage_type.Description);

                (radio_button.Child as Label).Markup =
                    string.Format("<b>{0}</b>\n<span fgcolor=\"{1}\">{2}</span>",
                        storage_type.Name, SparkleShare.UI.SecondaryTextColor, storage_type.Description);

                (radio_button.Child as Label).Xpad = 9;

                radio_buttons.PackStart (radio_button, false, false, 9);
                radio_button.Group = (radio_buttons.Children [0] as RadioButton).Group;
            }


            // Buttons
            Button cancel_button = new Button ("Cancel");
            cancel_button.Clicked += delegate { Controller.SyncingCancelled (); };

            Button continue_button = new Button ("Continue");
            continue_button.Clicked += delegate {
                int checkbox_index= 0;

                foreach (RadioButton radio_button in radio_buttons.Children) {
                    if (radio_button.Active) {
                        StorageTypeInfo selected_storage_type = SparkleShare.Controller.FetcherAvailableStorageTypes [checkbox_index];
                        Controller.StoragePageCompleted (selected_storage_type.Type);
                        return;
                    }

                    checkbox_index++;
                }
            };

            Buttons = new Button [] { cancel_button, continue_button };


            // Layout
            VBox layout = new VBox (false, 0);
            layout.PackStart (new Label (""), true, true, 0);
            layout.PackStart (radio_buttons, false, false, 0);
            layout.PackStart (new Label (""), true, true, 0);

            return layout;
        }
    }
}
