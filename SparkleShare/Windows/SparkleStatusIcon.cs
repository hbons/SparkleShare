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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

using Drawing = System.Drawing;

namespace SparkleShare {

    public class SparkleStatusIcon : Control {

        public SparkleStatusIconController Controller = new SparkleStatusIconController();

        private readonly Drawing.Bitmap syncing_idle_image = SparkleUIHelpers.GetBitmap("process-syncing-idle");
        private readonly Drawing.Bitmap syncing_up_image = SparkleUIHelpers.GetBitmap("process-syncing-up");
        private readonly Drawing.Bitmap syncing_down_image = SparkleUIHelpers.GetBitmap("process-syncing-down");
        private readonly Drawing.Bitmap syncing_image = SparkleUIHelpers.GetBitmap("process-syncing");
        private readonly Drawing.Bitmap syncing_error_image = SparkleUIHelpers.GetBitmap("process-syncing-error");

        private ContextMenu context_menu;

        private SparkleMenuItem log_item;
        private SparkleMenuItem state_item;
        private SparkleMenuItem exit_item;
        private SparkleMenuItem[] state_menu_items;

        private readonly SparkleNotifyIcon notify_icon = new SparkleNotifyIcon();


        public SparkleStatusIcon() {
            this.notify_icon.HeaderText = "SparkleShare";
            this.notify_icon.Icon = this.syncing_idle_image;

            CreateMenu();

            Controller.UpdateIconEvent += delegate(IconState state) {
                Dispatcher.BeginInvoke(
                    (Action) delegate {
                    switch(state) {
                        case IconState.Idle: {
                                this.notify_icon.Icon = this.syncing_idle_image;
                                break;
                            }
                        case IconState.SyncingUp: {
                                this.notify_icon.Icon = this.syncing_up_image;
                                break;
                            }
                        case IconState.SyncingDown: {
                                this.notify_icon.Icon = this.syncing_down_image;
                                break;
                            }
                        case IconState.Syncing: {
                                this.notify_icon.Icon = this.syncing_image;
                                break;
                            }
                        case IconState.Error: {
                                this.notify_icon.Icon = this.syncing_error_image;
                                break;
                            }
                    }
                });
            };

            Controller.UpdateStatusItemEvent += delegate(string state_text) {
                Dispatcher.BeginInvoke(
                    (Action) delegate {
                    this.state_item.Header = state_text;
                    this.state_item.UpdateLayout();

                    if(Controller.Projects.Length == state_menu_items.Length) {
                        for(int i = 0; i < Controller.Projects.Length; i++)
                            state_menu_items[i].Header = Controller.Projects[i].StatusMessage;
                    }

                    this.notify_icon.HeaderText = "SparkleShare\n" + state_text;
                });
            };

            Controller.UpdateMenuEvent += delegate {
                Dispatcher.BeginInvoke((Action) CreateMenu);
            };

            Controller.UpdateQuitItemEvent += delegate(bool item_enabled) {
                Dispatcher.BeginInvoke((Action) delegate {
                    this.exit_item.IsEnabled = item_enabled;
                    this.exit_item.UpdateLayout();
                });
            };
        }


        public void CreateMenu() {
            this.context_menu = new ContextMenu();

            this.state_item = new SparkleMenuItem {
                Header = Controller.StateText,
                IsEnabled = false
            };

            Image folder_image = new Image {
                Source = SparkleUIHelpers.GetImageSource("sparkleshare-folder"),
                Width = 16,
                Height = 16
            };

            SparkleMenuItem folder_item = new SparkleMenuItem {
                Header = "SparkleShare",
                Icon = folder_image
            };

            SparkleMenuItem add_item = new SparkleMenuItem {
                Header = "Add hosted project…"
            };

            this.log_item = new SparkleMenuItem {
                Header = "Recent changes…",
                IsEnabled = Controller.RecentEventsItemEnabled
            };

            SparkleMenuItem link_code_item = new SparkleMenuItem {
                Header = "Client ID"
            };

            if(Controller.LinkCodeItemEnabled) {
                SparkleMenuItem code_item = new SparkleMenuItem {
                    Header = SparkleShare.Controller.CurrentUser.PublicKey.Substring(0, 20) + "..."
                };

                SparkleMenuItem copy_item = new SparkleMenuItem {
                    Header = "Copy to Clipboard"
                };
                copy_item.Click += delegate {
                    Controller.CopyToClipboardClicked();
                };

                link_code_item.Items.Add(code_item);
                link_code_item.Items.Add(new Separator());
                link_code_item.Items.Add(copy_item);
            }

            CheckBox notify_check_box = new CheckBox {
                Margin = new Thickness(6, 0, 0, 0),
                IsChecked = SparkleShare.Controller.NotificationsEnabled
            };

            SparkleMenuItem notify_item = new SparkleMenuItem {
                Header = "Notifications",
                Icon = notify_check_box
            };

            SparkleMenuItem about_item = new SparkleMenuItem {
                Header = "About SparkleShare"
            };
            this.exit_item = new SparkleMenuItem {
                Header = "Exit"
            };


            add_item.Click += delegate {
                Controller.AddHostedProjectClicked();
            };
            this.log_item.Click += delegate {
                Controller.RecentEventsClicked();
            };
            about_item.Click += delegate {
                Controller.AboutClicked();
            };

            notify_check_box.Click += delegate {
                this.context_menu.IsOpen = false;
                SparkleShare.Controller.ToggleNotifications();
                notify_check_box.IsChecked = SparkleShare.Controller.NotificationsEnabled;
            };

            notify_item.Click += delegate {
                SparkleShare.Controller.ToggleNotifications();
                notify_check_box.IsChecked = SparkleShare.Controller.NotificationsEnabled;
            };

            this.exit_item.Click += delegate {
                this.notify_icon.Dispose();
                Controller.QuitClicked();
            };


            this.context_menu.Items.Add(this.state_item);
            this.context_menu.Items.Add(new Separator());
            this.context_menu.Items.Add(folder_item);

            state_menu_items = new SparkleMenuItem[Controller.Projects.Length];

            if(Controller.Projects.Length > 0) {
                int i = 0;
                foreach(ProjectInfo project in Controller.Projects) {

                    SparkleMenuItem subfolder_item = new SparkleMenuItem {
                        Header = project.Name.Replace("_", "__"),
                        Icon = new Image {
                            Source = SparkleUIHelpers.GetImageSource("folder"),
                            Width = 16,
                            Height = 16
                        }
                    };

                    state_menu_items[i] = new SparkleMenuItem {
                        Header = project.StatusMessage,
                        IsEnabled = false
                    };

                    subfolder_item.Items.Add(state_menu_items[i]);
                    subfolder_item.Items.Add(new Separator());

                    SparkleMenuItem open_item = new SparkleMenuItem {
                        Header = "Open folder",
                        Icon = new Image
                        {
                            Source = SparkleUIHelpers.GetImageSource("folder"),
                            Width = 16,
                            Height = 16
                        }
                    };

                    open_item.Click += new RoutedEventHandler(Controller.OpenFolderDelegate(project.Name));
                    subfolder_item.Items.Add(open_item);
                    subfolder_item.Items.Add(new Separator());

                    if(project.IsPaused) {
                        SparkleMenuItem resume_item;

                        if(project.UnsyncedChangesInfo.Count > 0) {
                            foreach(KeyValuePair<string, string> pair in project.UnsyncedChangesInfo)
                                subfolder_item.Items.Add(new SparkleMenuItem {
                                    Header = pair.Key,
                                    // TODO image
                                    IsEnabled = false
                                });

                            if(!string.IsNullOrEmpty(project.MoreUnsyncedChanges)) {
                                subfolder_item.Items.Add(new SparkleMenuItem {
                                    Header = project.MoreUnsyncedChanges,
                                    IsEnabled = false
                                });
                            }

                            subfolder_item.Items.Add(new Separator());
                            resume_item = new SparkleMenuItem {
                                Header = "Sync and Resume…"
                            };

                        } else {
                            resume_item = new SparkleMenuItem {
                                Header = "Resume"
                            };
                        }

                        resume_item.Click += (sender, e) => Controller.ResumeDelegate(project.Name)(sender, e);
                        subfolder_item.Items.Add(resume_item);

                    } else {
                        if(Controller.Projects[i].HasError) {
                            subfolder_item.Icon = new Image {
                                Source = Imaging.CreateBitmapSourceFromHIcon(
                                    Drawing.SystemIcons.Exclamation.Handle, Int32Rect.Empty,
                                    BitmapSizeOptions.FromWidthAndHeight(16, 16))
                            };

                            SparkleMenuItem try_again_item = new SparkleMenuItem {
                                Header = "Retry Sync"
                            };
                            try_again_item.Click += (sender, e) => Controller.TryAgainDelegate(project.Name)(sender, e);
                            subfolder_item.Items.Add(try_again_item);

                        } else {
                            SparkleMenuItem pause_item = new SparkleMenuItem {
                                Header = "Pause"
                            };
                            pause_item.Click +=
                                (sender, e) => Controller.PauseDelegate(project.Name)(sender, e);
                            subfolder_item.Items.Add(pause_item);
                        }
                    }

                    this.context_menu.Items.Add(subfolder_item);
                    i++;
                };
            }

            folder_item.Items.Add(this.log_item);
            folder_item.Items.Add(add_item);
            folder_item.Items.Add(new Separator());
            folder_item.Items.Add(notify_item);
            folder_item.Items.Add(new Separator());
            folder_item.Items.Add(link_code_item);
            folder_item.Items.Add(new Separator());
            folder_item.Items.Add(about_item);

            this.context_menu.Items.Add(new Separator());
            this.context_menu.Items.Add(this.exit_item);

            this.notify_icon.ContextMenu = this.context_menu;
        }


        public void ShowBalloon(string title, string subtext, string image_path) {
            this.notify_icon.ShowBalloonTip(title, subtext, image_path);
        }


        public void Dispose() {
            this.notify_icon.Dispose();
        }
    }

    public class SparkleMenuItem : MenuItem {

        public SparkleMenuItem() {
            Padding = new Thickness(6, 3, 4, 0);
        }
    }
}
