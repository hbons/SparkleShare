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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xaml;

namespace SparkleShare {

    public class SparkleAbout : Window {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private Label updates;


        // Short alias for the translations
        public static string _(string s)
        {
            return Program._(s);
        }


        public SparkleAbout ()
        {
            Title      = "About SparkleShare";
            ResizeMode = ResizeMode.NoResize;
            Height     = 288;
            Width      = 640;
            
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            Closing += Close;

            CreateAbout ();


            Controller.ShowWindowEvent += delegate {
               Dispatcher.Invoke ((Action) delegate {
                    Show ();
                    Activate ();
                    BringIntoView ();
                });
            };

            Controller.HideWindowEvent += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    Hide ();
                });
            };

            Controller.NewVersionEvent += delegate (string new_version) {
                Dispatcher.Invoke ((Action) delegate {
                    this.updates.Content = "A newer version (" + new_version + ") is available!";
                    this.updates.UpdateLayout ();
                });
            };

            Controller.VersionUpToDateEvent += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    this.updates.Content = "You are running the latest version.";
                    this.updates.UpdateLayout ();
                });
            };

            Controller.CheckingForNewVersionEvent += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    this.updates.Content = "Checking for updates...";
                    this.updates.UpdateLayout ();
                });
            };
        }


        private void CreateAbout ()
        {
            Image image = new Image () {
                Width  = 640,
                Height = 260
            };
        
            image.Source = SparkleUIHelpers.GetImageSource ("about");
            
            
            Label version = new Label () {
                Content    = "version " + Controller.RunningVersion,
                FontSize   = 11,
                Foreground = new SolidColorBrush (Colors.White)
            };

            this.updates = new Label () {
                Content    = "Checking for updates...",
                FontSize   = 11,
                Foreground = new SolidColorBrush (Color.FromRgb (45, 62, 81)) // TODO: color looks off
            };
            
            TextBlock credits = new TextBlock () {
                FontSize     = 11,
                Foreground   = new SolidColorBrush (Colors.White),
                Text         = "Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others.\n" +
                    "\n" +
                    "SparkleShare is Free and Open Source Software. You are free to use, modify, " +
                    "and redistribute it under the GNU General Public License version 3 or later.",
                TextWrapping = TextWrapping.Wrap,
                Width        = 318
            };
            
            
            Canvas canvas = new Canvas ();
            
            canvas.Children.Add (image);
            Canvas.SetLeft (image, 0);
            Canvas.SetTop (image, 0);

            canvas.Children.Add (version);
            Canvas.SetLeft (version, 289);
            Canvas.SetTop (version, 92);
            
            canvas.Children.Add (this.updates);
            Canvas.SetLeft (this.updates, 289);
            Canvas.SetTop (this.updates, 109);
            
            canvas.Children.Add (credits);
            Canvas.SetLeft (credits, 294);
            Canvas.SetTop (credits, 142);    
            
            Content = canvas;
        }
        
        
        private void Close (object sender, CancelEventArgs args)
        {
            Controller.WindowClosed ();
            args.Cancel = true;    
        }
    }
}
