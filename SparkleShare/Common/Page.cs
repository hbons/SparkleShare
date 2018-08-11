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

namespace SparkleShare {

    public abstract class Page : IDisposable {

        protected SetupController Controller;
        protected PageType? RequestedType;

        public string Header;
        public string Description;

        public object OptionArea;
        public object [] Buttons;


        public Page (PageType? page_type, SetupController controller)
        {
            RequestedType = page_type;
            Controller = controller;
        }


        public abstract object Render ();
        public virtual void Dispose () {}
    }
}
