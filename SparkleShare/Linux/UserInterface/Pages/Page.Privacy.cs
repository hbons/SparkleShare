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

    public class PrivacyPage : Page {

        Switch notification_service_switch;
        Switch crash_reports_switch;
        Switch gravatars_switch;


        public PrivacyPage (PageType page_type, PageController controller) : base (page_type, controller)
        {
            Header      = "Privacy Preferences";
            Description = "";
        }


        public override void Dispose ()
        {
        }


        public override object Render ()
        {
            var intro_label = new Label () {
                Text = "SparkleShare is decentralized, so no account of any sort is created.\n" +
                    "However, some features can affect your privacy:",
                LineWrap = true,
                LineWrapMode = Pango.WrapMode.Word,
                MaxWidthChars = 48,
                Xalign = 0
            };


            HBox notification_service_layout = SwitchLayout ("Notification Service",
                "Instantly syncs when someone makes a change by sending a notification " +
                "via <a href='https://www.sparkleshare.org/'>sparkleshare.org</a>. " +
                "No personal or usage data is recorded.", recommended: true);

            notification_service_switch = (Switch) (notification_service_layout.Children [0] as Box).Children [0];


            HBox crash_reports_layout = SwitchLayout ("Crash Reports",
                "In the unlikely event of a SparkleShare crash, sends an anonymized report to " +
                "the project maintainers to help fix bugs.", recommended: false);

            crash_reports_switch = (Switch) (crash_reports_layout.Children [0] as Box).Children [0];


            HBox gravatars_layout = SwitchLayout ("Gravatars",
                "Uses <a href='https://www.gravatar.com/'>gravatar.com</a> to download " +
                "profile pictures.", recommended: false);

            gravatars_switch = (Switch) (gravatars_layout.Children [0] as Box).Children [0];


            // Tip
            var tip_label = new Label ("You will be able to change these preferences later.") { Xalign = 0 };


            // Buttons
            Button quit_button = new Button ("Quit");
            quit_button.Clicked += delegate { Controller.QuitClicked (); };

            Button back_button = new Button ("Back");
            back_button.Clicked += delegate { Controller.BackClicked (RequestedType); };

            Button continue_button = new Button ("Continue");
            continue_button.Clicked += ContinueButtonClickedHandler;

            Buttons = new Button [] { quit_button, null, back_button, continue_button };


            // Layout
            var layout = new VBox (false, 0) { MarginLeft = 32, MarginRight = 12 };
            layout.PackStart (intro_label, false, false, 32);

            layout.PackStart (notification_service_layout, false, false, 12);
            layout.PackStart (crash_reports_layout, false, false, 12);
            layout.PackStart (gravatars_layout, false, false, 12);

            layout.PackStart (tip_label, false, false, 32);

            return layout;
        }


        void ContinueButtonClickedHandler (object sender, EventArgs args)
        {
            Controller.PrivacyPageCompleted (
                notification_service: notification_service_switch.Active,
                crash_reports: crash_reports_switch.Active,
                gravatars: gravatars_switch.Active);
        }


        HBox SwitchLayout (string title, string description, bool recommended)
        {
            var light_switch = new Switch () { Active = recommended, HeightRequest = 36 };
            string recommended_text = "";

            if (recommended)
                recommended_text = " ‒ Recommended";

            var label = new Label () {
                Markup = string.Format ("<b>{0}</b>{1}\n<span fgcolor='{2}'>{3}</span>",
                    title, recommended_text, SparkleShare.UI.SecondaryTextColor, description),
                LineWrap = true,
                LineWrapMode = Pango.WrapMode.Word,
                MaxWidthChars = 48,
                Xalign = 0
            };

            var box = new VBox (false, 0);
            box.PackStart (light_switch, false, false, 6);

            var layout = new HBox (false, 0) { MarginLeft = 24 };
            layout.PackStart (box, false, false, 0);
            layout.PackStart (label, true, true, 24);

            return layout;
        }
    }
}
