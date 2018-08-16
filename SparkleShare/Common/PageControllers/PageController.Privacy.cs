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

namespace SparkleShare {

    public partial class PageController {

        public void PrivacyPageCompleted (bool notification_service, bool crash_reports, bool gravatars)
        {
            SparkleShare.Controller.Config.SetConfigOption ("notification_service", notification_service.ToString ());
            SparkleShare.Controller.Config.SetConfigOption ("crash_reports", crash_reports.ToString ());
            SparkleShare.Controller.Config.SetConfigOption ("gravatars", gravatars.ToString ());

            ChangePageEvent (PageType.Host);
        }
    }
}
