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
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace SparkleShare {

    public class SparkleAbout : Window {

        public SparkleAboutController Controller = new SparkleAboutController ();

        private Label version;
        private Label copyright;
        private Label updates;


        // Short alias for the translations
        public static string _(string s)
        {
            return Program._(s);
        }


        public SparkleAbout ()
        {
            Title = "About SparkleShare";
//            Icon = Icons.sparkleshare;

            ResizeMode = ResizeMode.NoResize;
			
			
			//BackgroundImage = Icons.about;
            Height = 300;
			Width = 600;
			//Closing
			// += Close;
			
            CreateAbout ();
			

            Controller.ShowWindowEvent += delegate {
               Dispatcher.Invoke ((Action) delegate {
                    Show ();
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
            this.version = new Label () {
                
                
                Content    = "version " + Controller.RunningVersion
            };
			

            this.updates = new Label () {
                Content   = "Checking for updates..."
            };
			

			
            this.copyright = new Label () {
                Content   = "Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others.\n" +
                    "SparkleShare is Free and Open Source Software. You are free to use, modify, " +
                    "and redistribute it under the GNU General Public License version 3 or later."
            };

			Button b = new Button ();
			b.Content = "FFF";
	
			Canvas canvas = new Canvas ();
			canvas.Children.Add(this.version);
			Canvas.SetLeft(this.version, 302);
			Canvas.SetTop(this.version, 102);
			
			
			canvas.Children.Add(this.updates);
			Canvas.SetLeft(this.updates, 302);
			Canvas.SetTop(this.updates, 122);
			
			canvas.Children.Add(this.copyright);
			Canvas.SetLeft(this.copyright, 302);
			Canvas.SetTop(this.copyright, 142);
			
			
			Content = canvas;
			//AddChild(this.version);
			//AddChild(this.updates);
			//AddChild(this.copyright);
			Show ();
        }


        
		/*

        private void Close (object sender, Clos)
        {
            if (args.CloseReason != CloseReason.ApplicationExitCall &&
                args.CloseReason != CloseReason.TaskManagerClosing  &&
                args.CloseReason != CloseReason.WindowsShutDown) {

                Controller.WindowClosed ();
                args.Cancel = true;
            }
        }*/
    }
}
