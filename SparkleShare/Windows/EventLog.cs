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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Win32;
using System.Windows.Data;

namespace SparkleShare
{
    public class EventLog : Window {

        public EventLogController Controller = new EventLogController ();

        private Label label_Size;
        private Label label_History;

        private WebBrowser webbrowser;

        private Spinner spinner;

        private ComboBox combobox;

        private Grid grid_Base;

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(int feature, [MarshalAs(UnmanagedType.U4)] int flags, bool enable);

        public EventLog()
        {
            CreateEventLog();

            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            AllowsTransparency = false;
            Icon = SparkleUIHelpers.GetImageSource("sparkleshare-app", "ico");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            WriteOutImages();

            this.label_Size.Content = "Size: " + Controller.Size;
            this.label_History.Content = "History: " + Controller.HistorySize;

            this.webbrowser.ObjectForScripting = new SparkleScriptingObject();

            // Disable annoying IE clicking sound
            CoInternetSetFeatureEnabled(21, 0x00000002, true);

            Closing += this.OnClosing;

            Controller.ShowWindowEvent += delegate {
                Dispatcher.BeginInvoke((Action)(() => {
                    Show();
                    Activate();
                    BringIntoView();
                }));
            };

            Controller.HideWindowEvent += delegate {
                Dispatcher.BeginInvoke((Action)(() => {
                    Hide();
                    this.spinner.Visibility = Visibility.Visible;
                    this.webbrowser.Visibility = Visibility.Collapsed;
                }));
            };

            Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
                Dispatcher.BeginInvoke((Action)(() => {
                    this.label_Size.Content = "Size: " + size;
                    this.label_History.Content = "History: " + history_size;
                }));
            };

            Controller.UpdateChooserEvent += delegate (string[] folders) {
                Dispatcher.BeginInvoke((Action)(() =>
                  UpdateChooser(folders))
                );
            };

            Controller.UpdateChooserEnablementEvent += delegate (bool enabled) {
                Dispatcher.BeginInvoke((Action)(() =>
                  this.combobox.IsEnabled = enabled
                ));
            };

            Controller.UpdateContentEvent += delegate (string html) {
                Dispatcher.BeginInvoke((Action)(() => {
                    UpdateContent(html);

                    this.spinner.Visibility = Visibility.Collapsed;
                    this.webbrowser.Visibility = Visibility.Visible;
                }));
            };

            Controller.ContentLoadingEvent += () => this.Dispatcher.BeginInvoke(
                (Action)(() => {
                    this.spinner.Visibility = Visibility.Visible;
                    this.spinner.Start();
                    this.webbrowser.Visibility = Visibility.Collapsed;
                }));

            Controller.ShowSaveDialogEvent += delegate (string file_name, string target_folder_path) {
                Dispatcher.BeginInvoke((Action)(() => {
                    SaveFileDialog dialog = new SaveFileDialog()
                    {
                        FileName = file_name,
                        InitialDirectory = target_folder_path,
                        Title = "Restore from History",
                        DefaultExt = "." + System.IO.Path.GetExtension(file_name),
                        Filter = "All Files|*.*"
                    };

                    bool? result = dialog.ShowDialog(this);

                    if (result == true)
                        Controller.SaveDialogCompleted(dialog.FileName);
                    else
                        Controller.SaveDialogCancelled();
                }));
            };
        }


        private void CreateEventLog () {
            grid_Base = new Grid { Background = Brushes.White };

            label_Size = new Label {
                Content = "Size: ?",
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(20, 0, 0, 0),
                FontWeight = FontWeights.Bold
            };

            label_History = new Label {
                Content = "History: ?",
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(100, 0, 0, 0),
                FontWeight = FontWeights.Bold
            };

            combobox = new ComboBox {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                MinWidth = 120
            };

            spinner = new Spinner { Name = "spinner" };
            webbrowser = new WebBrowser { Name = "webbrowser" };

            Border border = new Border {
                VerticalAlignment = VerticalAlignment.Top,
                Height = 35,
                Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 223, 223, 223)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            Grid borderGrid = new Grid();

            borderGrid.Children.Add(this.label_Size);
            borderGrid.Children.Add(this.label_History);
            borderGrid.Children.Add(this.combobox);

            border.Child = borderGrid;

            Grid browserGrid = new Grid { Margin = new Thickness(0, 35, 0, 0) };

            browserGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            browserGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            browserGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            browserGrid.Children.Add(this.spinner);
            browserGrid.Children.Add(this.webbrowser);

            Rectangle sizingControlHeight = new Rectangle {
                Name = "sizingControlHeight",
                Visibility = Visibility.Hidden
            };

            Grid.SetColumn(sizingControlHeight, 1);

            Rectangle sizingControlWidth = new Rectangle {
                Name = "sizingControlHeight",
                Visibility = Visibility.Hidden
            };

            Grid.SetColumn(sizingControlWidth, 0);
            Grid.SetRow(sizingControlWidth, 0);

            browserGrid.Children.Add(sizingControlHeight);
            browserGrid.Children.Add(sizingControlWidth);

            webbrowser.SetBinding(HeightProperty, new Binding("ActualHeightProperty") { ElementName = sizingControlHeight.Name });
            webbrowser.SetBinding(WidthProperty, new Binding("ActualWidthProperty") { ElementName = sizingControlWidth.Name });

            grid_Base.Children.Add(border);
        }

        private void OnClosing(object sender, CancelEventArgs cancel_event_args)
        {
            Controller.WindowClosed();
            cancel_event_args.Cancel = true;
        }


        private void UpdateContent(string html)
        {
            string pixmaps_path = System.IO.Path.Combine(Sparkles.Configuration.DefaultConfiguration.TmpPath, "Images");
            pixmaps_path = pixmaps_path.Replace("\\", "/");

            html = html.Replace("<a href=", "<a class='windows' href=");
            html = html.Replace("<!-- $body-font-family -->", "Segoe UI");
            html = html.Replace("<!-- $day-entry-header-font-size -->", "13px");
            html = html.Replace("<!-- $body-font-size -->", "12px");
            html = html.Replace("<!-- $secondary-font-color -->", "#bbb");
            html = html.Replace("<!-- $small-color -->", "#ddd");
            html = html.Replace("<!-- $small-font-size -->", "90%");
            html = html.Replace("<!-- $day-entry-header-background-color -->", "#f5f5f5");
            html = html.Replace("<!-- $a-color -->", "#0085cf");
            html = html.Replace("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace("<!-- $pixmaps-path -->", pixmaps_path);
            html = html.Replace("<!-- $document-added-background-image -->", pixmaps_path + "/document-added-12.png");
            html = html.Replace("<!-- $document-edited-background-image -->", pixmaps_path + "/document-edited-12.png");
            html = html.Replace("<!-- $document-deleted-background-image -->", pixmaps_path + "/document-deleted-12.png");
            html = html.Replace("<!-- $document-moved-background-image -->", pixmaps_path + "/document-moved-12.png");

            this.spinner.Stop();

            this.webbrowser.ObjectForScripting = new SparkleScriptingObject();
            this.webbrowser.NavigateToString(html);
        }


        public void UpdateChooser(string[] folders)
        {
            if (folders == null)
            {
                folders = Controller.Folders;
            }

            this.combobox.Items.Clear();
            this.combobox.Items.Add(new ComboBoxItem() { Content = "Summary" });
            this.combobox.Items.Add(new Separator());
            this.combobox.SelectedItem = combobox.Items[0];

            int row = 2;
            foreach (string folder in folders)
            {
                this.combobox.Items.Add(new ComboBoxItem() { Content = folder });

                if (folder.Equals(Controller.SelectedFolder))
                    this.combobox.SelectedItem = this.combobox.Items[row];

                row++;
            }

            this.combobox.SelectionChanged += delegate {
                Dispatcher.BeginInvoke((Action)delegate {
                    int index = this.combobox.SelectedIndex;

                    if (index == 0)
                        Controller.SelectedFolder = null;
                    else
                        Controller.SelectedFolder = (string)((ComboBoxItem)this.combobox.Items[index]).Content;
                });
            };
        }


        private void WriteOutImages()
        {
            string tmp_path = Sparkles.Configuration.DefaultConfiguration.TmpPath;
            string pixmaps_path = System.IO.Path.Combine(tmp_path, "Images");

            if (!Directory.Exists(pixmaps_path))
            {
                Directory.CreateDirectory(pixmaps_path);

                File.SetAttributes(tmp_path, File.GetAttributes(tmp_path) | FileAttributes.Hidden);
            }

            BitmapSource image = SparkleUIHelpers.GetImageSource("user-icon-default");
            string file_path = System.IO.Path.Combine(pixmaps_path, "user-icon-default.png");

            using (FileStream stream = new FileStream(file_path, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }

            string[] actions = new string[] { "added", "deleted", "edited", "moved" };

            foreach (string action in actions)
            {
                image = SparkleUIHelpers.GetImageSource("document-" + action + "-12");
                file_path = System.IO.Path.Combine(pixmaps_path, "document-" + action + "-12.png");

                using (FileStream stream = new FileStream(file_path, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                }
            }
        }
    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class SparkleScriptingObject
    {
        public void LinkClicked(string url)
        {
            SparkleShare.UI.EventLog.Controller.LinkClicked(url);
        }
    }
}