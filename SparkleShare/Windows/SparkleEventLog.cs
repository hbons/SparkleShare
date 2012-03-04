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

namespace SparkleShare {

    public class SparkleEventLog : Window {

        public SparkleEventLogController Controller = new SparkleEventLogController ();

		private Canvas canvas;
        private Label size_label_value;
		private Label history_label_value;
		private ComboBox combo_box;
		private WebBrowser web_browser;
		
		
        // Short alias for the translations
        public static string _(string s)
        {
            return Program._(s);
        }


        public SparkleEventLog ()
        {
            Title      = "Recent Changes";
			Height     = 640;
			Width      = 480;
			ResizeMode = ResizeMode.NoResize;
			Background = new SolidColorBrush (Colors.WhiteSmoke);	
			
			Closing += Close;
			
						
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
                Content = Controller.HistorySize
            };
			
			history_label.Measure (new Size (Double.PositiveInfinity, Double.PositiveInfinity));
			Rect history_label_rect = new Rect (history_label.DesiredSize);
			
			
			this.web_browser = new WebBrowser () {
				Width  = Width - 7,
				Height = Height - 48 - 12
			};
			
			
			this.canvas = new Canvas ();
			Content = this.canvas;
			
			this.canvas.Children.Add (size_label);
			Canvas.SetLeft (size_label, 12);
			Canvas.SetTop (size_label, 10);
			
			this.canvas.Children.Add (this.size_label_value);
			Canvas.SetLeft (this.size_label_value, 12 + size_label_rect.Width);
			Canvas.SetTop (this.size_label_value, 10);
			
			
			this.canvas.Children.Add (history_label);
			Canvas.SetLeft (history_label, 120);
			Canvas.SetTop (history_label, 10);
			
			this.canvas.Children.Add (this.history_label_value);
			Canvas.SetLeft (this.history_label_value, 120 + history_label_rect.Width);
			Canvas.SetTop (this.history_label_value, 10);
			

            Controller.ShowWindowEvent += delegate {
               Dispatcher.Invoke ((Action) delegate {
                    Show ();
					Activate ();
					BringIntoView ();
					
					UpdateContent (null);
					UpdateChooser (null);
                });
            };

            Controller.HideWindowEvent += delegate {
                Dispatcher.Invoke ((Action) delegate {
                    Hide ();
                });
            };
			
			Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
				Dispatcher.Invoke ((Action) delegate {
					this.size_label_value.Content = size;
					this.size_label_value.UpdateLayout ();
					
					this.history_label_value.Content = history_size;
					this.history_label_value.UpdateLayout ();
				});
			};
			
            Controller.UpdateChooserEvent += delegate (string [] folders) {
        		Dispatcher.Invoke ((Action) delegate {
                    UpdateChooser (folders);
                });    
            };

            Controller.UpdateContentEvent += delegate (string html) {
                Dispatcher.Invoke ((Action) delegate {
                    UpdateContent (html);
                });
            };

            Controller.ContentLoadingEvent += delegate {
                if (this.canvas.Children.Contains (this.web_browser))
					this.canvas.Children.Remove (this.web_browser);

                    //    ContentView.AddSubview (this.progress_indicator); //TODO spinner
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
					Content = "All Projects"
				};
			
			this.combo_box.Items.Add (item);
			this.combo_box.SelectedItem = combo_box.Items [0];
			this.combo_box.Items.Add (new Separator ());
			
			foreach (string folder in folders) {
				this.combo_box.Items.Add (
					new ComboBoxItem () { Content = folder }
				);
			}
			
			this.combo_box.SelectionChanged += delegate {
				Dispatcher.Invoke ((Action) delegate {
					int index = this.combo_box.SelectedIndex;
					
					if (index == 0)
						Controller.SelectedFolder = null;
					else
						Controller.SelectedFolder = (string)
							(this.combo_box.Items [index] as ComboBoxItem).Content;
				});
			};
			
			this.canvas.Children.Add (combo_box);
			Canvas.SetLeft (this.combo_box, Width - 18 - this.combo_box.Width);
			Canvas.SetTop (this.combo_box, 12);
		}
		
		
		public void UpdateContent (string html)
		{
            Thread thread = new Thread (new ThreadStart (delegate {
                if (html == null)
                    html = Controller.HTML;

                html = html.Replace ("<!-- $body-font-family -->", "sans-serif");
                html = html.Replace ("<!-- $day-entry-header-font-size -->", "13.6px");
                html = html.Replace ("<!-- $body-font-size -->", "13.4px");
                html = html.Replace ("<!-- $secondary-font-color -->", "#bbb");
                html = html.Replace ("<!-- $small-color -->", "#ddd");
                html = html.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
                html = html.Replace ("<!-- $a-color -->", "#0085cf");
                html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
             //   html = html.Replace ("<!-- $pixmaps-path -->",
               //     "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath,
                 //   "Pixmaps"));

         //       html = html.Replace ("<!-- $document-added-background-image -->",
           //         "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath,
             //       "Pixmaps", "document-added-12.png"));

              //  html = html.Replace ("<!-- $document-deleted-background-image -->",
                //    "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath,
                  //  "Pixmaps", "document-deleted-12.png"));

                //html = html.Replace ("<!-- $document-edited-background-image -->",
                  //  "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath,
                    //"Pixmaps", "document-edited-12.png"));

                //html = html.Replace ("<!-- $document-moved-background-image -->",
                  //  "file://" + Path.Combine (NSBundle.MainBundle.ResourcePath,
                    //"Pixmaps", "document-moved-12.png"));

                Dispatcher.Invoke ((Action) delegate {
                    //if (this.progress_indicator.Superview == ContentView) TODO: spinner
                       // this.progress_indicator.RemoveFromSuperview ();

					this.web_browser.NavigateToString (html);
					
					if (!this.canvas.Children.Contains (this.web_browser)) {
						this.canvas.Children.Add (this.web_browser);
						Canvas.SetLeft (this.web_browser, 0);
						Canvas.SetTop (this.web_browser, 48);
					}
                });
            }));

            thread.Start ();
		}
		
		
        private void Close (object sender, CancelEventArgs args)
        {
                Controller.WindowClosed ();
                args.Cancel = true;    
        }
    }
}
