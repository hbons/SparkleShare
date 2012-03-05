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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SparkleShare {

    public class SparkleSetupWindow : Window {

		private Canvas canvas;
		


        public SparkleSetupWindow ()
        {
            Title      = "SparkleShare Setup";
			Width      = 640;
			Height     = 440;
			ResizeMode = ResizeMode.NoResize;
			Background = new SolidColorBrush (Colors.WhiteSmoke);
			
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			
			Closing += Close;
			
			
				Image image = new Image () {
				Width  = 150,
				Height = 482
			};
		
			BitmapImage bitmap_image = new BitmapImage();
			
			bitmap_image.BeginInit();
			// TODO: get relative reference to the image
			bitmap_image.UriSource = new Uri(@"C:\Users\Hylke\Code\SparkleShare\data\side-splash.png");
			bitmap_image.DecodePixelWidth = 150;
			bitmap_image.EndInit();
			
			image.Source  = bitmap_image;
						
			
            Label size_label = new Label () {
                Content    = "Welcome to SparkleShare!",
				
				Foreground = new SolidColorBrush (Color.FromRgb (0, 51, 153)),
				FontSize   = 16
				
            };
			
			
			
			TextBlock history_label_value = new TextBlock () {
                Text = "Before we get started, what's your name and email?\n" +
                	"Don't worry, this information will only visible to any team members.",
				TextWrapping = TextWrapping.Wrap,
				Width = 375
            };
			
						
			this.canvas = new Canvas ();
			Content = this.canvas;
			
			this.canvas.Children.Add (size_label);
			Canvas.SetLeft (size_label, 180);
			Canvas.SetTop (size_label, 18);
			
			
			
			this.canvas.Children.Add (history_label_value);
			Canvas.SetLeft (history_label_value, 185);
			Canvas.SetTop (history_label_value, 60);

			
			
			
			
            TextBlock name_label = new TextBlock () {
                Text = "Full Name:",
				Width = 150,
				TextAlignment = TextAlignment.Right,
				FontWeight = FontWeights.Bold
            };
			
			
			TextBox name = new TextBox () {
				Text  = "Hylke Bons",
				Width = 175 
			};
			
			
			
            TextBlock email_label = new TextBlock () {
                Text    = "Email:",
				Width = 150,
				TextAlignment = TextAlignment.Right,
				FontWeight = FontWeights.Bold
            };
			
			TextBox email = new TextBox () {
				Text  = "hylkebons@gmail.com",
				Width = 175
			};
			
			canvas.Children.Add (name);
			Canvas.SetLeft (name, 340);
			Canvas.SetTop (name, 200);
			
			canvas.Children.Add (name_label);
			Canvas.SetLeft (name_label, 180);
			Canvas.SetTop (name_label, 200 + 3);
			
			canvas.Children.Add (email_label);
			Canvas.SetLeft (email_label, 180);
			Canvas.SetTop (email_label, 230 + 3);
			
			
			canvas.Children.Add (email);
			Canvas.SetLeft (email, 340);
			Canvas.SetTop (email, 230);
			
			
			
			Rectangle rect = new Rectangle () {
				Width = Width,
				Height = 40,
				Fill = new SolidColorBrush (Color.FromRgb (240, 240, 240))	
			};
			
			Rectangle line = new Rectangle () {
				Width = Width,
				Height = 1,
				Fill = new SolidColorBrush (Color.FromRgb (223, 223, 223))	
			};
			
			
			
			
			canvas.Children.Add (rect);
			Canvas.SetRight (rect, 0);
			Canvas.SetBottom (rect, 0);
			
			canvas.Children.Add (line);
			Canvas.SetRight (line, 0);
			Canvas.SetBottom (line, 40);
			
			
			Button button = new Button () {
				Content = "Continue",
				Width = 75,
				IsDefault = true
			};
			
			
			canvas.Children.Add (button);
			Canvas.SetRight (button, 10);
			Canvas.SetBottom (button, 9);
			
			
			CheckBox check_box = new CheckBox () {
				Content = "Add SparkleShare to startup items",
				IsChecked = true
			};
			
			
			canvas.Children.Add (check_box);
			Canvas.SetLeft (check_box, 200);
			Canvas.SetBottom (check_box, 12);
			
			
			canvas.Children.Add (image);
			Canvas.SetLeft (image, 0);
			Canvas.SetBottom (image, 0);
			
            Show ();
        }
		
        private void Close (object sender, CancelEventArgs args)
        {
            //Controller.WindowClosed ();
            args.Cancel = true;    
        }
    }
}
