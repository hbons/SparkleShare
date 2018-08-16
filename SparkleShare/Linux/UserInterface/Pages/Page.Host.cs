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

    public class HostPage : Page {

        SparkleTreeView tree_view;
        TreeViewColumn service_column;


        public HostPage (PageType page_type, PageController controller) : base (page_type, controller)
        {
            Header = "Where’s your project hosted?";
            Description = "";
        }


        public override object Render ()
        {
            // Host selection
            ListStore store = new ListStore (typeof (string), typeof (Gdk.Pixbuf), typeof (string), typeof (Preset));

            tree_view = new SparkleTreeView (store) {
                HeadersVisible = false,
                SearchColumn = -1,
                EnableSearch = false
            };

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
            tree_view.AppendColumn (service_column);


            // Fill the list
            foreach (Preset preset in Controller.Presets) {
                store.AppendValues ("", new Gdk.Pixbuf (preset.IconPath),
                    string.Format ("<span><b>{0}</b>\n<span size='small' fgcolor='{1}'>{2}</span></span>",
                        preset.Name, SparkleShare.UI.SecondaryTextColor, preset.Description),
                    preset);
            }


            tree_view.Model.Foreach (new TreeModelForeachFunc (TreeModelForeachFuncHandler));

            TreeSelection default_selection = tree_view.Selection;
            TreePath default_path = new TreePath ("" + Controller.SelectedPresetIndex);
            default_selection.SelectPath (default_path);

            tree_view.ScrollToCell (new TreePath ("" + Controller.SelectedPresetIndex), null, true, 0, 0);

            ScrolledWindow scrolled_window = new ScrolledWindow () { ShadowType = ShadowType.In };
            scrolled_window.SetPolicy (PolicyType.Never, PolicyType.Automatic);
            scrolled_window.Add (tree_view);


            // Buttons
            Button cancel_button = new Button ("Cancel");
            Button continue_button = new Button ("Continue");

            cancel_button.Clicked += delegate { Controller.CancelClicked (RequestedType); };
            continue_button.Clicked += delegate { Controller.HostPageCompleted (); };

            Button back_button = new Button ("Back");
            back_button.Clicked += delegate { Controller.BackClicked (RequestedType); };

            Buttons = new Button [] { cancel_button, null, back_button, continue_button };


            // Layout
            VBox layout_vertical = new VBox (false, 16);
            layout_vertical.PackStart (scrolled_window, true, true, 0);

            return layout_vertical;
        }


        void RenderServiceColumn (TreeViewColumn column, CellRenderer cell, ITreeModel model, TreeIter iter)
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
                address.Equals (Controller.FetchAddress.Host)) { // TODO Check selection

                tree_view.SetCursor (path, service_column, false);
                //Preset preset = (Preset) model.GetValue (iter, 2);

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
