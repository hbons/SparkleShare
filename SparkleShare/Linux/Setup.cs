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

    public class Setup : SetupWindow {

        public SetupController Controller = new SetupController ();
        Page active_page;


        public Setup ()
        {
            Controller.HideWindowEvent += () => {
                Application.Invoke (delegate {
                    Hide ();

                    if (active_page != null) {
                        active_page.Dispose ();
                        active_page = null;
                    }
                });
            };

            Controller.ShowWindowEvent += () => {
                Application.Invoke (delegate {
                    ShowAll ();
                    Present ();
                });
            };

            Controller.ChangePageEvent += (PageType type, string [] warnings) => {
                Application.Invoke (delegate {
                    Reset ();
                    ShowPage (type, warnings);
                    ShowAll ();
                });
            };
        }


        public void ShowPage (PageType page_type, string [] warnings)
        {
            System.Type t = System.Type.GetType ("SparkleShare." + page_type + "Page", throwOnError: true);
            Page page = (Page) Activator.CreateInstance (t, new object [] { page_type, Controller });

            Header = page.Header;
            Description = page.Description;

            Add ((Widget) page.Render ());
            AddOption ((Widget) page.OptionArea);

            foreach (Button button in page.Buttons)
                AddButton (button);
                    
            active_page = page;
        }
    }
}
