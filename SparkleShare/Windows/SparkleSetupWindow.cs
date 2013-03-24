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
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SparkleShare {

    public class SparkleSetupWindow : Window {
        
        public Canvas ContentCanvas  = new Canvas ();
        public List <Button> Buttons = new List <Button> ();
        public string Header;
        public string Description;
        
        private Image side_splash;
        private Rectangle bar;
        
        private Rectangle line;
        

        public SparkleSetupWindow ()
        {
            Title      = "SparkleShare Setup";
            Width      = 640;
            Height     = 440;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush (Colors.WhiteSmoke);
            Icon       = SparkleUIHelpers.GetImageSource ("sparkleshare-app", "ico");
			
			TaskbarItemInfo = new TaskbarItemInfo () {
				Description = "SparkleShare"
			};
            
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content               = ContentCanvas;

            // Remove the close button
            Closing += Close;
            SourceInitialized += delegate {
                const int gwl_style = -16;
                const int ws_sysmenu = 0x00080000; 
                WindowInteropHelper helper = new WindowInteropHelper (this);
                int style = GetWindowLong (helper.Handle, gwl_style);
                SetWindowLong (helper.Handle, gwl_style, style & ~ws_sysmenu);
            };
            
            this.bar = new Rectangle () {
                Width  = Width,
                Height = 40,
                Fill   = new SolidColorBrush (Color.FromRgb (240, 240, 240))    
            };
            
            this.line = new Rectangle () {
                Width  = Width,
                Height = 1,
                Fill   = new SolidColorBrush (Color.FromRgb (223, 223, 223))    
            };

            this.side_splash = new Image () {
                Width  = 150,
                Height = 482
            };

            this.side_splash.Source = SparkleUIHelpers.GetImageSource ("side-splash");
            
            
            ContentCanvas.Children.Add (this.bar);
            Canvas.SetRight (bar, 0);
            Canvas.SetBottom (bar, 0);
            
            ContentCanvas.Children.Add (this.line);
            Canvas.SetRight (this.line, 0);
            Canvas.SetBottom (this.line, 40);
            
            ContentCanvas.Children.Add (this.side_splash);
            Canvas.SetLeft (this.side_splash, 0);
            Canvas.SetBottom (this.side_splash, 0);
        }
        
        
        public void Reset ()
        {
            ContentCanvas.Children.Remove (this.bar);
            
            ContentCanvas.Children.Remove (this.line);
            
            ContentCanvas.Children.Remove (this.side_splash);
            ContentCanvas = new Canvas ();
            Content       = ContentCanvas;
            
            ContentCanvas.Children.Add (this.bar);
            ContentCanvas.Children.Add (this.line);
            ContentCanvas.Children.Add (this.side_splash);
            
            Buttons       = new List <Button> ();
            Header        = "";
            Description   = "";
        }
        
        
        public void ShowAll ()
        {
            Label header_label = new Label () {
                Content    = Header,
                Foreground = new SolidColorBrush (Color.FromRgb (0, 51, 153)),
                FontSize   = 16
            };
                        
            TextBlock description_label = new TextBlock () {
                Text         = Description, 
                TextWrapping = TextWrapping.Wrap,
                Width        = 375
            };
            
            
            ContentCanvas.Children.Add (header_label);
            Canvas.SetLeft (header_label, 180);
            Canvas.SetTop (header_label, 18);    
            
            ContentCanvas.Children.Add (description_label);
            Canvas.SetLeft (description_label, 185);
            Canvas.SetTop (description_label, 60);
            
        
            if (Buttons.Count > 0) {
                Buttons [0].IsDefault = true;
				Buttons.Reverse ();
                
                int right = 9;
                
                foreach (Button button in Buttons) {
                    button.Measure (new Size (Double.PositiveInfinity, Double.PositiveInfinity));
                    Rect rect = new Rect (button.DesiredSize);
            
                    button.Width = rect.Width + 26;
                    
                    if (button.Width < 75)
                        button.Width = 75;
                    
                    ContentCanvas.Children.Add (button);
                    Canvas.SetRight (button, right);
                    Canvas.SetBottom (button, 9);
                    
                    right += (int) button.Width + 9;

					if ((button.Content as string).Equals ("Continue")) {
						Buttons [Buttons.Count - 1].IsDefault = false;
						button.IsDefault      = true;
					}
                }
            }
            
            ElementHost.EnableModelessKeyboardInterop (this);
        }
    
        
        private void Close (object sender, CancelEventArgs args)
        {
            args.Cancel = true;    
        }


        [DllImport("user32.dll")]
        private extern static int SetWindowLong (IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        private extern static int GetWindowLong (IntPtr hwnd, int index);
    }
}
