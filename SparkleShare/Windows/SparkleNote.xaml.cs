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


using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;

using Sparkles;

namespace SparkleShare {

    public partial class SparkleNote : Window {

        public SparkleNoteController Controller = new SparkleNoteController ();

        private readonly string default_text = "Anything to add?";

        public SparkleNote()
        {
            InitializeComponent();

            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            AllowsTransparency = false;
            Icon = SparkleUIHelpers.GetImageSource("sparkleshare-app", "ico");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Closing += this.OnClosing;

            Controller.ShowWindowEvent += delegate {
                Dispatcher.BeginInvoke((Action)(() => {
                    Show();
                    Activate();
                    CreateNote();
                    BringIntoView();
                }));
            };

            Controller.HideWindowEvent += delegate {
				Dispatcher.BeginInvoke ((Action) (() => {
					Hide ();
                    this.balloon_text_field.Clear();
				}));
			};

            this.cancel_button.Click += delegate {
                Dispatcher.BeginInvoke ((Action) (() => {
                    Controller.CancelClicked ();
                }));
            };

            this.sync_button.Click += delegate {
                Dispatcher.BeginInvoke ((Action) (() => {
                    string note = this.balloon_text_field.Text;

                    if (note.Equals (default_text, StringComparison.InvariantCultureIgnoreCase)) {
                        note = String.Empty;
                    }

                    Controller.SyncClicked (note);
                }));
            };

            this.balloon_text_field.GotFocus += OnTextBoxGotFocus;
            this.balloon_text_field.LostFocus += OnTextBoxLostFocus;

            CreateNote();
        }

        private void CreateNote()
        {
            ImageSource avatar = SparkleUIHelpers.GetImageSource("user-icon-default");

            if (File.Exists (Controller.AvatarFilePath)) {
                avatar = SparkleUIHelpers.GetImage (Controller.AvatarFilePath);
            }

            this.user_image.ImageSource = avatar;
            this.Title = Controller.CurrentProject ?? "Add Note";
            this.user_name_text_block.Text = SparkleShare.Controller.CurrentUser.Name;
            this.user_email_text_field.Text = SparkleShare.Controller.CurrentUser.Email;
            this.balloon_text_field.Text = default_text;

            ElementHost.EnableModelessKeyboardInterop (this);
        }

        private void OnClosing (object sender, CancelEventArgs cancel_event_args)
        {
            Controller.WindowClosed ();
            cancel_event_args.Cancel = true;
        }

        private void OnTextBoxGotFocus (object sender, RoutedEventArgs e)
        {
            if (this.balloon_text_field.Text.Equals (default_text, StringComparison.InvariantCultureIgnoreCase)) {
                this.balloon_text_field.Text = string.Empty;
            }
        }

        private void OnTextBoxLostFocus (object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty (this.balloon_text_field.Text)) {
                this.balloon_text_field.Text = default_text;
            }
        }
    }
}