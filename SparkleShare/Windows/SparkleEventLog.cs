//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;
using Shapes = System.Windows.Shapes;

namespace SparkleShare {

    public class SparkleEventLog : Window {

        public SparkleEventLogController Controller = new SparkleEventLogController ();

        private Canvas canvas;
        private Label size_label_value;
        private Label history_label_value;
        private ComboBox combo_box;
        private WebBrowser web_browser;
        private SparkleSpinner spinner;
        
        
        public SparkleEventLog ()
        {
            Title              = "Recent Changes";
            Height             = 640;
            Width              = 480;
            ResizeMode         = ResizeMode.NoResize; // TODO
            Background         = new SolidColorBrush (Color.FromRgb (240, 240, 240));    
            AllowsTransparency = false;
            Icon               = SparkleUIHelpers.GetImageSource ("sparkleshare-app", "ico");

            int x = (int) (SystemParameters.PrimaryScreenWidth * 0.61);
            int y = (int) (SystemParameters.PrimaryScreenHeight * 0.5 - (Height * 0.5));

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = x;
            Top = y;

            WriteOutImages ();
                        
            Label size_label = new Label () {
                Content    = "Size:",
                FontWeight = FontWeights.Bold
            };
            
            this.size_label_value = new Label () {
                Content = Controller.Size
            };
            
            size_label.Measure (new Size (Double.PositiveInfinity, Double.PositiveInfinity));
            Rect size_label_rect = new Rect (size_label.DesiredSize);
            
            Label history_label = new Label () {
                Content    = "History:",
                FontWeight = FontWeights.Bold
            };
            
            this.history_label_value = new Label () {
                Content = Controller.HistorySize,
            };
            
            history_label.Measure (new Size (Double.PositiveInfinity, Double.PositiveInfinity));
            Rect history_label_rect = new Rect (history_label.DesiredSize);
            
            Shapes.Rectangle line = new Shapes.Rectangle () {
                Width  = Width,
                Height = 1,
                Fill   = new SolidColorBrush (Color.FromRgb (223, 223, 223))    
            };
            
            Shapes.Rectangle background = new Shapes.Rectangle () {
                Width  = Width,
                Height = Height,
                Fill   = new SolidColorBrush (Color.FromRgb (250, 250, 250))    
            };
                        
            this.web_browser = new WebBrowser () {
                Width  = Width - 6,
                Height = Height - 64
            };

            this.web_browser.ObjectForScripting = new SparkleScriptingObject ();
                    

            spinner = new SparkleSpinner (22);
            
            // Disable annoying IE clicking sound
            CoInternetSetFeatureEnabled (21, 0x00000002, true);

            
            this.canvas = new Canvas ();
            Content     = this.canvas;

            this.canvas.Children.Add (size_label);
            Canvas.SetLeft (size_label, 24);
            Canvas.SetTop (size_label, 4);

            this.canvas.Children.Add (this.size_label_value);
            Canvas.SetLeft (this.size_label_value, 22 + size_label_rect.Width);
            Canvas.SetTop (this.size_label_value, 4);
            
            this.canvas.Children.Add (history_label);
            Canvas.SetLeft (history_label, 130);
            Canvas.SetTop (history_label, 4);
            
            this.canvas.Children.Add (this.history_label_value);
            Canvas.SetLeft (this.history_label_value, 130 + history_label_rect.Width);
            Canvas.SetTop (this.history_label_value, 4);
            
            this.canvas.Children.Add (background);
            Canvas.SetLeft (background, 0);
            Canvas.SetTop (background, 36);
            
            this.canvas.Children.Add (spinner);
            Canvas.SetLeft (spinner, (Width / 2) - 15);
            Canvas.SetTop (spinner, (Height / 2) - 22);
            
            this.canvas.Children.Add (line);
            Canvas.SetLeft (line, 0);
            Canvas.SetTop (line, 35);
            

            Closing += Close;
            
            Controller.ShowWindowEvent += delegate {
               Dispatcher.BeginInvoke ((Action) delegate {
                    Show ();
                    Activate ();
                    BringIntoView ();
                });
            };

            Controller.HideWindowEvent += delegate {
                Dispatcher.BeginInvoke ((Action) delegate {
                    Hide ();
                    
                    if (this.canvas.Children.Contains (this.web_browser))
                        this.canvas.Children.Remove (this.web_browser);
                });
            };
            
            Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
                Dispatcher.BeginInvoke ((Action) delegate {
                    this.size_label_value.Content = size;
                    this.size_label_value.UpdateLayout ();
                    
                    this.history_label_value.Content = history_size;
                    this.history_label_value.UpdateLayout ();
                });
            };
            
            Controller.UpdateChooserEvent += delegate (string [] folders) {
                Dispatcher.BeginInvoke ((Action) delegate {
                    UpdateChooser (folders);
                });    
            };

            Controller.UpdateChooserEnablementEvent += delegate (bool enabled) {
                Dispatcher.BeginInvoke ((Action) delegate {
					this.combo_box.IsEnabled = enabled;
                });    
            };

            Controller.UpdateContentEvent += delegate (string html) {
                Dispatcher.BeginInvoke ((Action) delegate {
                    UpdateContent (html);
                });
            };

            Controller.ContentLoadingEvent += delegate {
				Dispatcher.BeginInvoke ((Action) delegate {
	                this.spinner.Start ();
	                
	                if (this.canvas.Children.Contains (this.web_browser))
	                    this.canvas.Children.Remove (this.web_browser);
				});
            };

            Controller.ShowSaveDialogEvent += delegate (string file_name, string target_folder_path) {
				Dispatcher.BeginInvoke ((Action) delegate {
	                SaveFileDialog dialog = new SaveFileDialog () {
						FileName         = file_name,
						InitialDirectory = target_folder_path,
						Title            = "Restore from History",
						DefaultExt       = "." + Path.GetExtension (file_name),
						Filter           = "All Files|*.*"
					};

					Nullable<bool> result = dialog.ShowDialog (this);

					if (result == true)
						Controller.SaveDialogCompleted (dialog.FileName);
					else
						Controller.SaveDialogCancelled ();
				});
            };
        }

        
        public void UpdateChooser (string [] folders)
        {
            if (folders == null)
                folders = Controller.Folders;
            
            if (this.combo_box != null)
                this.canvas.Children.Remove (this.combo_box);
                
            this.combo_box = new ComboBox () {
                Width = 160    
            };
            
                ComboBoxItem item = new ComboBoxItem () {
                    Content = "Summary"
                };
            
            this.combo_box.Items.Add (item);
            this.combo_box.Items.Add (new Separator ());
            
            this.combo_box.SelectedItem = combo_box.Items [0];
            
            int row = 2;
            foreach (string folder in folders) {
                this.combo_box.Items.Add (
                    new ComboBoxItem () { Content = folder }
                );
                
                if (folder.Equals (Controller.SelectedFolder))
                    this.combo_box.SelectedItem = combo_box.Items [row];
                
                row++;
            }
            
            this.combo_box.SelectionChanged += delegate {
                Dispatcher.BeginInvoke ((Action) delegate {
                    int index = this.combo_box.SelectedIndex;
                    
                    if (index == 0)
                        Controller.SelectedFolder = null;
                    else
                        Controller.SelectedFolder = (string)
                            (this.combo_box.Items [index] as ComboBoxItem).Content;
                });
            };
            
            this.canvas.Children.Add (combo_box);
            Canvas.SetLeft (this.combo_box, Width - 24 - this.combo_box.Width);
            Canvas.SetTop (this.combo_box, 6);
        }
        
        
        public void UpdateContent (string html)
        {
            string pixmaps_path = Path.Combine (SparkleLib.SparkleConfig.DefaultConfig.TmpPath, "Pixmaps");
            pixmaps_path        = pixmaps_path.Replace ("\\", "/");
            
            html = html.Replace ("<a href=", "<a class='windows' href=");
            html = html.Replace ("<!-- $body-font-family -->", "'Segoe UI', sans-serif");
            html = html.Replace ("<!-- $day-entry-header-font-size -->", "13px");
            html = html.Replace ("<!-- $body-font-size -->", "12px");
            html = html.Replace ("<!-- $secondary-font-color -->", "#bbb");
            html = html.Replace ("<!-- $small-color -->", "#ddd");
			html = html.Replace ("<!-- $small-font-size -->", "90%");
            html = html.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
            html = html.Replace ("<!-- $a-color -->", "#0085cf");
            html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace ("<!-- $pixmaps-path -->", pixmaps_path);
            html = html.Replace ("<!-- $document-added-background-image -->", pixmaps_path + "/document-added-12.png");
            html = html.Replace ("<!-- $document-edited-background-image -->", pixmaps_path + "/document-edited-12.png");
            html = html.Replace ("<!-- $document-deleted-background-image -->", pixmaps_path + "/document-deleted-12.png");
            html = html.Replace ("<!-- $document-moved-background-image -->", pixmaps_path + "/document-moved-12.png");

            this.spinner.Stop ();
    
			this.web_browser.ObjectForScripting = new SparkleScriptingObject ();  	
			this.web_browser.NavigateToString (html);
		
            if (!this.canvas.Children.Contains (this.web_browser)) {
                this.canvas.Children.Add (this.web_browser);
                Canvas.SetLeft (this.web_browser, 0);
                Canvas.SetTop (this.web_browser, 36);
            }
        }
        
        
        private void WriteOutImages ()
        {
            string tmp_path     = SparkleLib.SparkleConfig.DefaultConfig.TmpPath;
            string pixmaps_path = Path.Combine (tmp_path, "Pixmaps");
            
            if (!Directory.Exists (pixmaps_path)) {
                Directory.CreateDirectory (pixmaps_path);
                
                File.SetAttributes (tmp_path,
                    File.GetAttributes (tmp_path) | FileAttributes.Hidden);
            }
              
            BitmapSource image = SparkleUIHelpers.GetImageSource ("user-icon-default");
            string file_path   = Path.Combine (pixmaps_path, "user-icon-default.png");
                
            using (FileStream stream = new FileStream (file_path, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder ();
                encoder.Frames.Add (BitmapFrame.Create (image));
                encoder.Save (stream);
            }
              
            string [] actions = new string [] {"added", "deleted", "edited", "moved"};
            
            foreach (string action in actions) {    
                image = SparkleUIHelpers.GetImageSource ("document-" + action + "-12");
                file_path   = Path.Combine (pixmaps_path, "document-" + action + "-12.png");
                    
                using (FileStream stream = new FileStream (file_path, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder ();
                    encoder.Frames.Add (BitmapFrame.Create (image));
                    encoder.Save (stream);
                }
            }
        }
        
        
        private void Close (object sender, CancelEventArgs args)
        {
            Controller.WindowClosed ();
            args.Cancel = true;    
        }
        

        [DllImport ("urlmon.dll")]
        [PreserveSig]
        [return:MarshalAs (UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled (int feature,
			[MarshalAs (UnmanagedType.U4)] int flags, bool enable);
    }

    
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class SparkleScriptingObject {
        
        public void LinkClicked (string url)
        {
            Program.UI.EventLog.Controller.LinkClicked (url);
        }
    }
}
