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
using System.Windows.Forms.Integration;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SparkleShare {

    public class SparkleSetupWindow : Window {
		
		public Canvas ContentCanvas = new Canvas ();
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
			
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Content               = ContentCanvas;
			
			Closing += Close;
			
			
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
			
			BitmapImage bitmap_image = new BitmapImage();
			
			bitmap_image.BeginInit ();
			bitmap_image.DecodePixelWidth = 150;
			
			bitmap_image.UriSource =
				new Uri (@"C:\Users\Hylke\Code\SparkleShare\data\side-splash.png");
			
			bitmap_image.EndInit ();
			this.side_splash.Source = bitmap_image;
		
			
			ContentCanvas.Children.Add (this.bar);
			Canvas.SetRight (bar, 0);
			Canvas.SetBottom (bar, 0);
			
			ContentCanvas.Children.Add (this.line);
			Canvas.SetRight (this.line, 0);
			Canvas.SetBottom (this.line, 40);
			
			ContentCanvas.Children.Add (this.side_splash);
			Canvas.SetLeft (this.side_splash, 0);
			Canvas.SetBottom (this.side_splash, 0);
			
			// TODO: enable keyboard navigation
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
                
				int right = 9;
				
                foreach (Button button in Buttons) {
					button.Measure (new Size (Double.PositiveInfinity, Double.PositiveInfinity));
					Rect rect = new Rect (button.DesiredSize);
			
					button.Width = rect.Width + 26;
					
					//if (button.Width < 60)
					//	button.Width = 60;
						
                    ContentCanvas.Children.Add (button);
					Canvas.SetRight (button, right);
					Canvas.SetBottom (button, 9);
					
					right += (int) button.Width + 9;
                }
            }
			
			ElementHost.EnableModelessKeyboardInterop (this);
			Show ();
		}
		
		
        private void Close (object sender, CancelEventArgs args)
        {
            //Controller.WindowClosed ();
            args.Cancel = true;    
        }
    }
	
	
	public class SparkleWindow : SparkleSetupWindow {
	
		public SparkleSetupController Controller = new SparkleSetupController ();
		
		
		public SparkleWindow ()
		{
			Reset ();
			
			Header      = "Welcome to SparkleShare!";
       		Description = "Before we get started, what's your name and email?\n" +
            	"Don't worry, this information will only visible to any team members.";
							
			Button continue_button = new Button () {
				Content = "Continue",
				IsEnabled = false//,
				//Width   = 75
			};
			
			
			Button cancel_button = new Button () {
				Content = "Cancel"//,
				//Width   = 75
			};
			
			Buttons.Add (continue_button);
			Buttons.Add (cancel_button);
			
			
			
			CheckBox check_box = new CheckBox () {
				Content = "Add SparkleShare to startup items",
				IsChecked = true
			};
			
			
			ContentCanvas.Children.Add (check_box);
			Canvas.SetLeft (check_box, 185);
			Canvas.SetBottom (check_box, 12);
			
				
			
            TextBlock name_label = new TextBlock () {
                Text = "Full Name:",
				Width = 150,
				TextAlignment = TextAlignment.Right,
				FontWeight = FontWeights.Bold
            };
			
			
			TextBox name = new TextBox () {
				Text  = Controller.GuessedUserName,
				Width = 175
				
			};
			
            TextBlock email_label = new TextBlock () {
                Text    = "Email:",
				Width = 150,
				TextAlignment = TextAlignment.Right,
				FontWeight = FontWeights.Bold
            };
			
			TextBox email = new TextBox () {
				Width = 175,
				Text = Controller.GuessedUserEmail
			};
			
			name.TextChanged += delegate {
				Controller.CheckSetupPage (name.Text, email.Text);
			};
			
			email.TextChanged += delegate {
				Controller.CheckSetupPage (name.Text, email.Text);
			};
			
			Controller.UpdateSetupContinueButtonEvent += delegate (bool enabled) {
				Dispatcher.Invoke ((Action) delegate {
					continue_button.IsEnabled = enabled;
				});
			};
			
			ContentCanvas.Children.Add (name);
			Canvas.SetLeft (name, 340);
			Canvas.SetTop (name, 200);
			
			
			ContentCanvas.Children.Add (name_label);
			Canvas.SetLeft (name_label, 180);
			Canvas.SetTop (name_label, 200 + 3);
			
			ContentCanvas.Children.Add (email_label);
			Canvas.SetLeft (email_label, 180);
			Canvas.SetTop (email_label, 230 + 3);
			
			
			ContentCanvas.Children.Add (email);
			Canvas.SetLeft (email, 340);
			Canvas.SetTop (email, 230);
			
			
			
				Controller.CheckSetupPage (name.Text, email.Text);
			
			ShowAll ();
		}
	}
}
