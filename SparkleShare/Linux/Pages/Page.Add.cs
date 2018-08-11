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

using Sparkles;

namespace SparkleShare {

    public class AddPage : Page {

        TreeView tree_view;
        TreeViewColumn service_column;

        Entry address_entry;
        Label address_example;

        Entry path_entry;
        Label path_example;


        public Add (SetupController controller) : base (controller)
        {
            Header = "Where’s your project hosted?";
            Description = "";

            Controller.ChangeAddressFieldEvent += ChangeAddressFieldEventHandler;
            Controller.ChangePathFieldEvent += ChangePathFieldEventHandler;
            Controller.UpdateAddProjectButtonEvent += UpdateAddProjectButtonEventHandler;
        }


        public void Dispose ()
        {
            Controller.ChangeAddressFieldEvent -= ChangeAddressFieldEventHandler;
            Controller.ChangePathFieldEvent -= ChangePathFieldEventHandler;
            Controller.UpdateAddProjectButtonEvent -= UpdateAddProjectButtonEventHandler;
        }


        public override object Render ()
        {
            HBox layout_fields  = new HBox (true, 32);
            VBox layout_address = new VBox (true, 0);
            VBox layout_path    = new VBox (true, 0);


            // Host selection
            ListStore store = new ListStore (typeof (string), typeof (Gdk.Pixbuf), typeof (string), typeof (Preset));

            tree_view = new SparkleTreeView (store) {
                HeadersVisible = false,
                SearchColumn = -1,
                EnableSearch = false
            };

            tree_view.ButtonReleaseEvent += delegate { path_entry.GrabFocus (); };
            tree_view.CursorChanged += delegate { Controller.SelectedPresetChanged (tree_view.SelectedRow); };


            // Padding column
            tree_view.AppendColumn ("Padding", new Gtk.CellRendererText (), "text", 0);
            tree_view.Columns [0].Cells [0].Xpad = 8;

            // Icon column
            tree_view.AppendColumn ("Icon", new Gtk.CellRendererPixbuf (), "pixbuf", 1);
            tree_view.Columns [1].Cells [0].Xpad = 6;

            // Service column
            service_column = new TreeViewColumn () { Title = "Service" };
            CellRendererText service_cell = new CellRendererText () { Ypad = 12 };
            service_column.PackStart (service_cell, true);
            service_column.SetCellDataFunc (service_cell, new TreeCellDataFunc (RenderServiceColumn));

            foreach (Preset preset in Controller.Presets) {
                store.AppendValues ("", new Gdk.Pixbuf (preset.ImagePath),
                    "<span><b>" + preset.Name + "</b>\n" +
                    "<span size=\"small\" fgcolor=\"" + SparkleShare.UI.SecondaryTextColor + "\">" + preset.Description + "</span>" +
                    "</span>", preset);
            }

            tree_view.AppendColumn (service_column);

            ScrolledWindow scrolled_window = new ScrolledWindow () { ShadowType = ShadowType.In };
            scrolled_window.SetPolicy (PolicyType.Never, PolicyType.Automatic);
            scrolled_window.Add (tree_view);

            tree_view.Model.Foreach (new TreeModelForeachFunc (TreeModelForeachFuncHandler));

            TreeSelection default_selection = tree_view.Selection;
            TreePath default_path = new TreePath ("" + Controller.SelectedPresetIndex);
            default_selection.SelectPath (default_path);

            tree_view.ScrollToCell (new TreePath ("" + Controller.SelectedPresetIndex), null, true, 0, 0);


            // Entries
            address_entry = new Entry () {
                Text = Controller.PreviousAddress,
                Sensitive = (Controller.SelectedPreset.Address == null),
                ActivatesDefault = true
            };

            path_entry = new Entry () {
                Text = Controller.PreviousPath,
                Sensitive = (Controller.SelectedPreset.Path == null),
                ActivatesDefault = true
            };

            address_example = new Label () {
                Xalign = 0,
                UseMarkup = true,
                Markup = "<span size=\"small\" fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + Controller.SelectedPreset.AddressExample + "</span>"
            };

            path_example = new Label () {
                Xalign = 0,
                UseMarkup = true,
                Markup = "<span size=\"small\" fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + Controller.SelectedPreset.PathExample + "</span>"
            };

            layout_address.PackStart (new Label () {
                    Markup = "<b>" + "Address" + "</b>",
                    Xalign = 0
                }, true, true, 0);

            layout_address.PackStart (address_entry, false, false, 0);
            layout_address.PackStart (address_example, false, false, 0);

            address_entry.Changed += delegate {
                Controller.CheckAddPage (address_entry.Text, path_entry.Text, tree_view.SelectedRow);
            };

            path_entry.Changed += delegate {
                Controller.CheckAddPage (address_entry.Text, path_entry.Text, tree_view.SelectedRow);
            };

            layout_path.PackStart (new Label () {
                Markup = "<b>" + "Remote Path" + "</b>",
                Xalign = 0
            }, true, true, 0);

            layout_path.PackStart (path_entry, false, false, 0);
            layout_path.PackStart (path_example, false, false, 0);

            layout_fields.PackStart (layout_address, true, true, 0);
            layout_fields.PackStart (layout_path, true, true, 0);


            if (string.IsNullOrEmpty (path_entry.Text)) {
                address_entry.GrabFocus ();
                address_entry.Position = -1;

            } else {
                path_entry.GrabFocus ();
                path_entry.Position = -1;
            }


            // Extra option area
            CheckButton check_button = new CheckButton ("Fetch prior revisions") { Active = false };
            check_button.Toggled += delegate { Controller.HistoryItemChanged (check_button.Active); };

            AddOption (check_button);


            // Buttons
            Button cancel_button = new Button ("Cancel");
            Button add_button = new Button ("Add") { Sensitive = false };

            cancel_button.Clicked += delegate { Controller.PageCancelled (); };
            add_button.Clicked += delegate { Controller.AddPageCompleted (address_entry.Text, path_entry.Text); };

            AddButton (cancel_button);
            AddButton (add_button);


            Controller.HistoryItemChanged (check_button.Active);
            Controller.CheckAddPage (address_entry.Text, path_entry.Text, 1);


            // Finish layout
            VBox layout_vertical = new VBox (false, 16);
            layout_vertical.PackStart (scrolled_window, true, true, 0);
            layout_vertical.PackStart (layout_fields, false, false, 0);

            return layout_vertical;
        }


        void ChangeAddressFieldEventHandler (string text, string example_text, FieldState state)
        {
            Application.Invoke (delegate {
                address_entry.Text = text;
                address_entry.Sensitive = (state == FieldState.Enabled);

                address_example.Markup =  "<span size=\"small\" fgcolor=\"" +
                    SparkleShare.UI.SecondaryTextColor + "\">" + example_text + "</span>";
            });
        }


        void ChangePathFieldEventHandler (string text, string example_text, FieldState state)
        {
            Application.Invoke (delegate {
                path_entry.Text = text;
                path_entry.Sensitive = (state == FieldState.Enabled);

                path_example.Markup = "<span size=\"small\" fgcolor=\""
                    + SparkleShare.UI.SecondaryTextColor + "\">" + example_text + "</span>";
            });
        }


        void UpdateAddProjectButtonEventHandler (bool button_enabled)
        {
            Application.Invoke (delegate { add_button.Sensitive = button_enabled; });
        }


        void RenderServiceColumn (TreeViewColumn column, CellRenderer cell,
            ITreeModel model, TreeIter iter)
        {
            string markup = (string) model.GetValue (iter, 2);
            TreeSelection selection = (column.TreeView as TreeView).Selection;

            if (selection.IterIsSelected (iter))
                markup = markup.Replace (SparkleShare.UI.SecondaryTextColor, SparkleShare.UI.SecondaryTextColorSelected);
            else
                markup = markup.Replace (SparkleShare.UI.SecondaryTextColorSelected, SparkleShare.UI.SecondaryTextColor);

            (cell as CellRendererText).Markup = markup;
        }


        bool TreeModelForeachFuncHandler (ITreeModel model, TreePath path, TreeIter iter)
        {
            string address;

            try {
                address = (model.GetValue (iter, 2) as Preset).Address;

            } catch (NullReferenceException) {
                address = "";
            }

            if (!string.IsNullOrEmpty (address) &&
                address.Equals (Controller.PreviousAddress)) {

                tree_view.SetCursor (path, service_column, false);
                Preset preset = (Preset) model.GetValue (iter, 2);

                if (preset.Address != null)
                    address_entry.Sensitive = false;

                if (preset.Path != null)
                    path_entry.Sensitive = false;

                return true;

            } else {
                return false;
            }
        }


        class SparkleTreeView : TreeView {

            public int SelectedRow
            {
                get {
                    TreeIter iter;
                    ITreeModel model;

                    Selection.GetSelected (out model, out iter);
                    return int.Parse (model.GetPath (iter).ToString ());
                }
            }


            public SparkleTreeView (ListStore store) : base (store)
            {
            }
        }
    }
}
