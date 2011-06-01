//   SparkleShare, a collaboration and sharing tool.
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


using Gtk;

namespace SparkleShare {

    // An infobar
    public class SparkleInfobar : EventBox {

        public SparkleInfobar (string icon_name, string title, string text)
        {
            Window window = new Window (WindowType.Popup) {
                Name = "gtk-tooltip"
            };

            window.EnsureStyle ();
            Style = window.Style;

            Label label = new Label () {
                Markup = "<b>" + title + "</b>\n" + text
            };

            HBox hbox = new HBox (false, 12) {
                BorderWidth = 12
            };

            hbox.PackStart (new Image (SparkleUIHelpers.GetIcon (icon_name, 24)),
                false, false, 0);

            hbox.PackStart (label, false, false, 0);

            Add (hbox);
        }
    }
}
