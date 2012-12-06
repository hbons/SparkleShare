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
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xaml;

namespace SparkleShare {

    public class SparkleAbout : Window {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private Label updates;


        public SparkleAbout ()
        {
            Title      = "About SparkleShare";
            ResizeMode = ResizeMode.NoResize;
            Height     = 288;
            Width      = 640;
            Icon       = SparkleUIHelpers.GetImageSource("sparkleshare-app", "ico");
            
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Closing += Close;

            CreateAbout ();

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
                });
            };

            Controller.UpdateLabelEvent += delegate (string text) {
                Dispatcher.BeginInvoke ((Action) delegate {
                    this.updates.Content = text;
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
                Foreground = new SolidColorBrush (Color.FromArgb (128, 255, 255, 255))
            };
            
            TextBlock credits = new TextBlock () {
                FontSize     = 11,
                Foreground   = new SolidColorBrush (Colors.White),
                Text         = "Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others.\n" +
                    "\n" +
                    "SparkleShare is Open Source software. You are free to use, modify, " +
                    "and redistribute it under the GNU General Public License version 3 or later.",
                TextWrapping = TextWrapping.Wrap,
                Width        = 318
            };
            
            SparkleLink website_link = new SparkleLink ("Website", Controller.WebsiteLinkAddress);
            SparkleLink credits_link = new SparkleLink ("Credits", Controller.CreditsLinkAddress);
            SparkleLink report_problem_link = new SparkleLink ("Report a problem", Controller.ReportProblemLinkAddress);
            SparkleLink debug_log_link = new SparkleLink ("Debug log", Controller.DebugLogLinkAddress);

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

            canvas.Children.Add (website_link);
            Canvas.SetLeft (website_link, 289);
            Canvas.SetTop (website_link, 222);   

            canvas.Children.Add (credits_link);
            Canvas.SetLeft (credits_link, 289 + website_link.ActualWidth + 60);
            Canvas.SetTop (credits_link, 222);

            canvas.Children.Add (report_problem_link);
            Canvas.SetLeft (report_problem_link, 289 + website_link.ActualWidth + credits_link.ActualWidth + 115);
            Canvas.SetTop (report_problem_link, 222);  

            canvas.Children.Add (debug_log_link);
            Canvas.SetLeft (debug_log_link, 289 + website_link.ActualWidth + credits_link.ActualWidth +
                report_problem_link.ActualWidth + 220);
            Canvas.SetTop (debug_log_link, 222);  
            
            Content = canvas;
        }
        
        
        private void Close (object sender, CancelEventArgs args)
        {
            Controller.WindowClosed ();
            args.Cancel = true;    
        }
    }


    public class SparkleLink : Label {

        public SparkleLink (string title, string url)
        {
            FontSize   = 11;
            Cursor     = Cursors.Hand;
            Foreground = new SolidColorBrush (Color.FromRgb (135, 178, 227));

            TextDecoration underline = new TextDecoration () {
                Pen              = new Pen (new SolidColorBrush (Color.FromRgb (135, 178, 227)), 1),
                PenThicknessUnit = TextDecorationUnit.FontRecommended
            };

            TextDecorationCollection collection = new TextDecorationCollection ();
            collection.Add (underline);

            TextBlock text_block = new TextBlock () {
                Text            = title,
                TextDecorations = collection
            };

            Content = text_block;

            MouseUp += delegate {
                Program.Controller.OpenWebsite (url);
            };            
        }
    }
}
