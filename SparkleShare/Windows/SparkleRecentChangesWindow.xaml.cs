using System.Windows;

namespace SparkleShare
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;

    /// <summary>
    /// Logics for the recent-changes-window.
    /// </summary>
    public partial class SparkleRecentChangesWindow : Window
    {
        public SparkleEventLogController Controller = new SparkleEventLogController ();

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(int feature, [MarshalAs(UnmanagedType.U4)] int flags, bool enable);

        /// <summary>
        /// Initializes a new instance of the <see cref="SparkleRecentChangesWindow"/> class.
        /// </summary>
        public SparkleRecentChangesWindow ()
        {
            this.InitializeComponent ();

            // Hide the minimize and maximize buttons.
            this.SourceInitialized += (sender, args) => this.HideMinimizeAndMaximizeButtons();
            // Set some window-properties from code.
            this.Background = new SolidColorBrush (Color.FromRgb(240, 240, 240));
            this.AllowsTransparency = false;
            this.Icon = SparkleUIHelpers.GetImageSource ("sparkleshare-app", "ico");
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Write images to temp-folder.
            this.WriteOutImages ();

            // Set values from controller to ui.
            this.label_Size.Content = "Size: " + Controller.Size;
            this.label_History.Content = "History: " + Controller.HistorySize;

            this.webbrowser.ObjectForScripting = new SparkleScriptingObject ();
            // Disable annoying IE clicking sound
            CoInternetSetFeatureEnabled (21, 0x00000002, true);

            // Tell controller on closing event.
            this.Closing += this.OnClosing;

            // Show the window on controllers request.
            this.Controller.ShowWindowEvent += () => this.Dispatcher.BeginInvoke (
                (Action)(() => {
                    this.Show ();
                    this.Activate ();
                    this.BringIntoView ();
                }));

            // Hide the window on controllers request.
            // Also hide the webbrowser-element and show the spinner.
            this.Controller.HideWindowEvent += () => this.Dispatcher.BeginInvoke (
                (Action)(() => {
                    this.Hide ();
                    this.spinner.Visibility = Visibility.Visible;
                    this.webbrowser.Visibility = Visibility.Collapsed;
                }));

            // Update labels on controllers request.
            this.Controller.UpdateSizeInfoEvent += (size, history_size) => this.Dispatcher.BeginInvoke (
                    (Action)(() => {
                        this.label_Size.Content = "Size: " + size;
                        this.label_History.Content = "History: " + history_size;
                    }));

            // Update the combobox-elements.
            this.Controller.UpdateChooserEvent += folders => this.Dispatcher.BeginInvoke (
                (Action)(() => this.UpdateChooser (folders)));

            // Update the enabled-state of the combobox.
            this.Controller.UpdateChooserEnablementEvent += enabled => this.Dispatcher.BeginInvoke (
                (Action)(() => this.combobox.IsEnabled = enabled));

            // Update the content of the webbrowser.
            this.Controller.UpdateContentEvent += html => this.Dispatcher.BeginInvoke ((Action)(() => {
                this.UpdateContent (html);

                this.spinner.Visibility = Visibility.Collapsed;
                this.webbrowser.Visibility = Visibility.Visible;
            }));

            // Show the spinner if the content is loading.
            this.Controller.ContentLoadingEvent += () => this.Dispatcher.BeginInvoke (
                (Action)(() => {
                    this.spinner.Visibility = Visibility.Visible;
                    this.spinner.Start ();
                    this.webbrowser.Visibility = Visibility.Collapsed;
                }));

            // Show the save-file-dialog on controllers request.
            this.Controller.ShowSaveDialogEvent +=
                (file_name, target_folder_path) => this.Dispatcher.BeginInvoke (
                    (Action)(() => {
                        SaveFileDialog dialog = new SaveFileDialog ()
                                                    {
                                                        FileName = file_name,
                                                        InitialDirectory = target_folder_path,
                                                        Title = "Restore from History",
                                                        DefaultExt = "." + Path.GetExtension (file_name),
                                                        Filter = "All Files|*.*"
                                                    };

                        bool? result = dialog.ShowDialog (this);

                        if (result == true) this.Controller.SaveDialogCompleted (dialog.FileName);
                        else this.Controller.SaveDialogCancelled ();
                    }));
        }

        /// <summary>
        /// Called when [closing].
        /// Suppress the closing and asks controller to hide this window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="cancel_event_args">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        private void OnClosing (object sender, CancelEventArgs cancel_event_args)
        {
            this.Controller.WindowClosed ();
            cancel_event_args.Cancel = true;
        }

        /// <summary>
        /// Updates the content of the webbrowser.
        /// </summary>
        /// <param name="html">The HTML.</param>
        private void UpdateContent (string html)
        {
            string pixmaps_path = Path.Combine (SparkleLib.SparkleConfig.DefaultConfig.TmpPath, "Pixmaps");
            pixmaps_path = pixmaps_path.Replace ("\\", "/");

            html = html.Replace("<a href=", "<a class='windows' href=");
            html = html.Replace("<!-- $body-font-family -->", "'Segoe UI', sans-serif");
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

            this.webbrowser.ObjectForScripting = new SparkleScriptingObject ();
            this.webbrowser.NavigateToString (html);
        }

        /// <summary>
        /// Updates the combobox-items.
        /// </summary>
        /// <param name="folders">The folders.</param>
        public void UpdateChooser (string [] folders)
        {
            if (folders == null) {
                folders = Controller.Folders;
            }

            this.combobox.Items.Clear ();
            this.combobox.Items.Add (new ComboBoxItem () { Content = "Summary" });
            this.combobox.Items.Add(new Separator());
            this.combobox.SelectedItem = combobox.Items[0];

            int row = 2;
            foreach (string folder in folders)
            {
                this.combobox.Items.Add(new ComboBoxItem() { Content = folder } );

                if (folder.Equals (Controller.SelectedFolder)) {
                    this.combobox.SelectedItem = this.combobox.Items [row];
                }

                row++;
            }

            this.combobox.SelectionChanged += delegate {
                Dispatcher.BeginInvoke((Action)delegate {
                    int index = this.combobox.SelectedIndex;

                    if (index == 0) {
                        Controller.SelectedFolder = null;
                    } else {
                        Controller.SelectedFolder = (string)((ComboBoxItem)this.combobox.Items[index]).Content;
                    }
                });
            };
        }

        /// <summary>
        /// Writes the images from the pixel-map to the temp-folder.
        /// </summary>
        private void WriteOutImages ()
        {
            string tmp_path = SparkleLib.SparkleConfig.DefaultConfig.TmpPath;
            string pixmaps_path = Path.Combine(tmp_path, "Pixmaps");

            if (!Directory.Exists(pixmaps_path))
            {
                Directory.CreateDirectory(pixmaps_path);

                File.SetAttributes(tmp_path, File.GetAttributes(tmp_path) | FileAttributes.Hidden);
            }

            BitmapSource image = SparkleUIHelpers.GetImageSource("user-icon-default");
            string file_path = Path.Combine(pixmaps_path, "user-icon-default.png");

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
                file_path = Path.Combine(pixmaps_path, "document-" + action + "-12.png");

                using (FileStream stream = new FileStream(file_path, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                }
            }
        }
    }
}
