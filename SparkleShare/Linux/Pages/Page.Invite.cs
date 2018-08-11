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
using Gtk;

namespace SparkleShare {

    public class InvitePage : Page {

        public InvitePage (PageType page_type, SetupController controller) : base (page_type, controller)
        {
            Header = "You’ve received an invite!";
            Description = "Do you want to add this project to SparkleShare?";


            // Buttons
            Button cancel_button = new Button ("Cancel");
            Button add_button    = new Button ("Add");

            cancel_button.Clicked += delegate { Controller.PageCancelled (); };
            add_button.Clicked += delegate { Controller.InvitePageCompleted (); };

            Buttons = new Button [] {cancel_button, add_button };
        }


        public override object Render ()
        {
            // Labels
            Label address_label = new Label ("Address:") { Xalign = 1 };
            Label address_value = new Label ("<b>" + Controller.PendingInvite.Address + "</b>") {
                UseMarkup = true,
                Xalign    = 0
            };

            Label path_label = new Label ("Remote Path:") { Xalign = 1 };
            Label path_value = new Label ("<b>" + Controller.PendingInvite.RemotePath + "</b>") {
                UseMarkup = true,
                Xalign    = 0
            };


            // Layout
            Table table = new Table (2, 3, true) {
                RowSpacing    = 6,
                ColumnSpacing = 6
            };

            table.Attach (address_label, 0, 1, 0, 1);
            table.Attach (address_value, 1, 2, 0, 1);
            table.Attach (path_label, 0, 1, 1, 2);
            table.Attach (path_value, 1, 2, 1, 2);

            VBox wrapper = new VBox (false, 9);
            wrapper.PackStart (table, true, false, 0);

            return wrapper;
        }
    }
}
