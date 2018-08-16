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
using System.Threading;

using Sparkles;

namespace SparkleShare {

    public partial class PageController {

        public void CheckUserPage (string name, string email)
        {
            name = name.Trim ();
            email = email.Trim ();

            bool fields_valid = (!string.IsNullOrEmpty (name) && email.Contains ("@"));
            PageCanContinueEvent (PageType.User, fields_valid);
        }


        public void UserPageCompleted (string full_name, string email)
        {
            SparkleShare.Controller.CurrentUser = new User (full_name, email);
            new Thread (() => SparkleShare.Controller.CreateStartupItem ()).Start ();

            ChangePageEvent (PageType.Privacy);
        }
    }
}
