//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shell;

using Drawing = System.Drawing;
using Imaging = System.Windows.Interop.Imaging;
using WPF = System.Windows.Controls;

namespace SparkleShare {

    public class SparkleSetup : SparkleSetupWindow {
    
        public SparkleSetupController Controller = new SparkleSetupController ();
        
        
        public SparkleSetup ()
        {
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
            
            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
                Dispatcher.BeginInvoke ((Action) delegate {
                    Reset ();
                    
                    switch (type) {
                    case PageType.Setup: {
                        Header      = "Welcome to SparkleShare!";
                        Description  = "First off, what's your name and email?\nThis information is only visible to team members.";
                        
                        TextBlock name_label = new TextBlock () {
                            Text = "Full Name:",
                            Width = 150,
                            TextAlignment = TextAlignment.Right,
                            FontWeight = FontWeights.Bold
                        };
                        
                        string name = System.Security.Principal.WindowsIdentity.GetCurrent ().Name;
                        name = name.Split ("\\".ToCharArray ()) [1];

                        TextBox name_box = new TextBox () {
                            Text  = name,
                            Width = 175
                        };
                        
                        TextBlock email_label = new TextBlock () {
                            Text    = "Email:",
                            Width = 150,
                            TextAlignment = TextAlignment.Right,
                            FontWeight = FontWeights.Bold
                        };
                        
                        TextBox email_box = new TextBox () {
                            Width = 175
                        };
                                        
                        
                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
                        
                        Button continue_button = new Button () {
                            Content = "Continue",
                            IsEnabled = false
                        };
                        
                        
                        ContentCanvas.Children.Add (name_label);
                        Canvas.SetLeft (name_label, 180);
                        Canvas.SetTop (name_label, 200 + 3);
                        
                        ContentCanvas.Children.Add (name_box);
                        Canvas.SetLeft (name_box, 340);
                        Canvas.SetTop (name_box, 200);
                        
                        ContentCanvas.Children.Add (email_label);
                        Canvas.SetLeft (email_label, 180);
                        Canvas.SetTop (email_label, 230 + 3);
                        
                        ContentCanvas.Children.Add (email_box);
                        Canvas.SetLeft (email_box, 340);
                        Canvas.SetTop (email_box, 230);
                        
                        Buttons.Add (cancel_button);
                        Buttons.Add (continue_button);
                        
                        name_box.Focus ();
                        name_box.Select (name_box.Text.Length, 0);
                        
                        Controller.UpdateSetupContinueButtonEvent += delegate (bool enabled) {
                            Dispatcher.BeginInvoke ((Action) delegate {
                                continue_button.IsEnabled = enabled;
                            });
                        };
                        
                        name_box.TextChanged += delegate {
                            Controller.CheckSetupPage (name_box.Text, email_box.Text);
                        };
                        
                        email_box.TextChanged += delegate {
                            Controller.CheckSetupPage (name_box.Text, email_box.Text);
                        };
                        
                        cancel_button.Click += delegate {
                            Dispatcher.BeginInvoke ((Action) delegate {
                                Program.UI.StatusIcon.Dispose ();    
                                Controller.SetupPageCancelled ();
                            });
                        };
                        
                        continue_button.Click += delegate {
                            Controller.SetupPageCompleted (name_box.Text, email_box.Text);
                        };
                        
                        Controller.CheckSetupPage (name_box.Text, email_box.Text);
                        
                        break;
                    }

                    case PageType.Invite: {
                        Header      = "You've received an invite!";
                           Description = "Do you want to add this project to SparkleShare?";
        
                        
                        TextBlock address_label = new TextBlock () {
                            Text = "Address:",
                            Width = 150,
                            TextAlignment = TextAlignment.Right
                        };
                                                    
                        TextBlock address_value = new TextBlock () {
                            Text  = Controller.PendingInvite.Address,
                            Width = 175,
                            FontWeight = FontWeights.Bold
                        };
                        
                        
                        TextBlock path_label = new TextBlock () {
                            Text  = "Remote Path:",
                            Width = 150,
                            TextAlignment = TextAlignment.Right
                        };
                        
                        TextBlock path_value = new TextBlock () {
                            Width = 175,
                            Text = Controller.PendingInvite.RemotePath,
                            FontWeight = FontWeights.Bold
                        };
                        
                        
                        
                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
                        
                        Button add_button = new Button () {
                            Content = "Add"
                        };


                        ContentCanvas.Children.Add (address_label);
                        Canvas.SetLeft (address_label, 180);
                        Canvas.SetTop (address_label, 200);
                        
                        ContentCanvas.Children.Add (address_value);
                        Canvas.SetLeft (address_value, 340);
                        Canvas.SetTop (address_value, 200);
                        
                        ContentCanvas.Children.Add (path_label);
                        Canvas.SetLeft (path_label, 180);
                        Canvas.SetTop (path_label, 225);
                        
                        ContentCanvas.Children.Add (path_value);
                        Canvas.SetLeft (path_value, 340);
                        Canvas.SetTop (path_value, 225);
                        
                        Buttons.Add (add_button);
                        Buttons.Add (cancel_button);
                        
                        
                        cancel_button.Click += delegate {
                            Controller.PageCancelled ();
                        };
                        
                        add_button.Click += delegate {
                            Controller.InvitePageCompleted ();
                        };
                        
                        break;
                    }
                        
                    case PageType.Add: {
                        Header = "Where's your project hosted?";
                        

                        ListView list_view = new ListView () {
                            Width  = 419,
                            Height = 195,
                            SelectionMode = SelectionMode.Single
                        };
                        
                        GridView grid_view = new GridView () {
                            AllowsColumnReorder = false
                        };
                        
                        grid_view.Columns.Add (new GridViewColumn ());
                    
                        string xaml =    
                            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" +
                            "  xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
                            "  <Grid>" +
                            "    <StackPanel Orientation=\"Horizontal\">" +
                            "      <Image Margin=\"5,0,0,0\" Source=\"{Binding Image}\" Height=\"24\" Width=\"24\"/>" +
                            "      <StackPanel>" +
                            "        <TextBlock Padding=\"10,4,0,0\" FontWeight=\"Bold\" Text=\"{Binding Name}\">" +
                            "        </TextBlock>" +
                            "        <TextBlock Padding=\"10,0,0,4\" Opacity=\"0.5\" Text=\"{Binding Description}\">" +
                            "        </TextBlock>" +
                            "      </StackPanel>" +
                            "    </StackPanel>" +
                            "  </Grid>" +
                            "</DataTemplate>";

                        grid_view.Columns [0].CellTemplate = (DataTemplate) XamlReader.Parse (xaml);

                        Style header_style = new Style(typeof (GridViewColumnHeader));
                        header_style.Setters.Add (new Setter (GridViewColumnHeader.VisibilityProperty, Visibility.Collapsed));
                        grid_view.ColumnHeaderContainerStyle = header_style;
                        
                        foreach (SparklePlugin plugin in Controller.Plugins) {
                            // FIXME: images are blurry
                            BitmapFrame image = BitmapFrame.Create (
                                new Uri (plugin.ImagePath)
                            );
                            
                            list_view.Items.Add (
                                new {
                                    Name        = plugin.Name,
                                    Description = plugin.Description,
                                    Image       = image
                                }
                            );
                        }        
                        
                        list_view.View          = grid_view;
                        list_view.SelectedIndex = Controller.SelectedPluginIndex;
                        
                        TextBlock address_label = new TextBlock () {
                            Text       = "Address:",
                            FontWeight = FontWeights.Bold
                        };
                                                    
                        TextBox address_box = new TextBox () {
                            Width = 200,
                            Text  = Controller.PreviousAddress,
                            IsEnabled = (Controller.SelectedPlugin.Address == null)
                        };
                        
                        TextBlock address_help_label = new TextBlock () {
                            Text       = Controller.SelectedPlugin.AddressExample,
                            FontSize   = 11,
                            Foreground = new SolidColorBrush (Color.FromRgb (128, 128, 128))
                        };
                        
                        TextBlock path_label = new TextBlock () {
                            Text       = "Remote Path:",
                            FontWeight = FontWeights.Bold,
                            Width      = 200
                        };
                                                    
                        TextBox path_box = new TextBox () {
                            Width = 200,
                            Text  = Controller.PreviousPath,
                            IsEnabled = (Controller.SelectedPlugin.Path == null)
                        };
                        
                        TextBlock path_help_label = new TextBlock () {
                            Text       = Controller.SelectedPlugin.PathExample,
                            FontSize   = 11,
                            Width      = 200,
                            Foreground = new SolidColorBrush (Color.FromRgb (128, 128, 128))
                        };
                        
                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
                        
                        Button add_button = new Button () {
                            Content = "Add"
                        };

                        CheckBox history_check_box = new CheckBox ()
                        {
                            Content   = "Fetch prior revisions",
                            IsChecked = Controller.FetchPriorHistory
                        };
                        
                        history_check_box.Click += delegate {
                            Controller.HistoryItemChanged (history_check_box.IsChecked.Value);
                        };
                        
                        ContentCanvas.Children.Add (history_check_box);
                        Canvas.SetLeft (history_check_box, 185);
                        Canvas.SetBottom (history_check_box, 12);
                        
                        ContentCanvas.Children.Add (list_view);
                        Canvas.SetTop (list_view, 70);
                        Canvas.SetLeft (list_view, 185);
                        
                        ContentCanvas.Children.Add (address_label);
                        Canvas.SetTop (address_label, 285);
                        Canvas.SetLeft (address_label, 185);
                        
                        ContentCanvas.Children.Add (address_box);
                        Canvas.SetTop (address_box, 305);
                        Canvas.SetLeft (address_box, 185);
                        
                        ContentCanvas.Children.Add (address_help_label);
                        Canvas.SetTop (address_help_label, 330);
                        Canvas.SetLeft (address_help_label, 185);
                        
                        ContentCanvas.Children.Add (path_label);
                        Canvas.SetTop (path_label, 285);
                        Canvas.SetRight (path_label, 30);
                        
                        ContentCanvas.Children.Add (path_box);
                        Canvas.SetTop (path_box, 305);
                        Canvas.SetRight (path_box, 30);
                        
                        ContentCanvas.Children.Add (path_help_label);
                        Canvas.SetTop (path_help_label, 330);
                        Canvas.SetRight (path_help_label, 30);
                        
                        TaskbarItemInfo.ProgressValue = 0.0;
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                        
                        Buttons.Add (add_button);
                        Buttons.Add (cancel_button);
                        
                        address_box.Focus ();
                        address_box.Select (address_box.Text.Length, 0);
                        

                        Controller.ChangeAddressFieldEvent += delegate (string text,
                            string example_text, FieldState state) {

                            Dispatcher.BeginInvoke ((Action) delegate {
                                address_box.Text        = text;
                                address_box.IsEnabled   = (state == FieldState.Enabled);
                                address_help_label.Text = example_text;
                            });
                        };

                        Controller.ChangePathFieldEvent += delegate (string text,
                            string example_text, FieldState state) {

                            Dispatcher.BeginInvoke ((Action) delegate {
                                path_box.Text        = text;
                                path_box.IsEnabled   = (state == FieldState.Enabled);
                                path_help_label.Text = example_text;
                            });
                        };
                        
                        Controller.UpdateAddProjectButtonEvent += delegate (bool button_enabled) {
                            Dispatcher.BeginInvoke ((Action) delegate {
                                add_button.IsEnabled = button_enabled;
                            });
                        };
                        
                        list_view.SelectionChanged += delegate {
                            Controller.SelectedPluginChanged (list_view.SelectedIndex);
                        };
                        
                        list_view.KeyDown += delegate {
                            Controller.SelectedPluginChanged (list_view.SelectedIndex);
                        };

                        Controller.CheckAddPage (address_box.Text, path_box.Text, list_view.SelectedIndex);
                        
                        address_box.TextChanged += delegate {
                            Controller.CheckAddPage (address_box.Text, path_box.Text, list_view.SelectedIndex);
                        };
                        
                        path_box.TextChanged += delegate {
                            Controller.CheckAddPage (address_box.Text, path_box.Text, list_view.SelectedIndex);
                        };
                        
                        cancel_button.Click += delegate {
                            Controller.PageCancelled ();
                        };

                        add_button.Click += delegate {
                            Controller.AddPageCompleted (address_box.Text, path_box.Text);
                        };
                                          
                        break;
                    }
                        
                        
                    case PageType.Syncing: {
                        Header      = "Adding project ‘" + Controller.SyncingFolder + "’…";
                        Description = "This may take a while for large projects.\nIsn't it coffee-o'clock?";

                        Button finish_button = new Button () {
                            Content   = "Finish",
                            IsEnabled = false
                        };

                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };

                        ProgressBar progress_bar = new ProgressBar () {
                            Width  = 414,
                            Height = 15,
                            Value  = Controller.ProgressBarPercentage
                        };
                        
                        
                        ContentCanvas.Children.Add (progress_bar);
                        Canvas.SetLeft (progress_bar, 185);
                        Canvas.SetTop (progress_bar, 150);
                        
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        
                        Buttons.Add (cancel_button);
                        Buttons.Add(finish_button);
                                                   
                         
                        Controller.UpdateProgressBarEvent += delegate (double percentage) {
                            Dispatcher.BeginInvoke ((Action) delegate {
                                progress_bar.Value = percentage;    
                                TaskbarItemInfo.ProgressValue = percentage / 100;
                            });
                        };
    
                        cancel_button.Click += delegate {
                            Controller.SyncingCancelled ();
                        };
                        
                        break;
                    }
                        
                        
                    case PageType.Error: {
                        Header      = "Oops! Something went wrong…";
                        Description = "Please check the following:";

                        TextBlock help_block = new TextBlock () {
                            TextWrapping = TextWrapping.Wrap,
                            Width        = 310    
                        };

                        TextBlock bullets_block = new TextBlock () {
                            Text = "•\n\n\n•"
                        };

                        help_block.Inlines.Add (new Bold (new Run (Controller.PreviousUrl)));
                        help_block.Inlines.Add (" is the address we've compiled. Does this look alright?\n\n");
                        help_block.Inlines.Add ("Is this computer's Client ID known by the host??");

                        if (warnings.Length > 0) {
                            bullets_block.Text += "\n\n•";
                            help_block.Inlines.Add ("\n\nHere's the raw error message:");

                            foreach (string warning in warnings) {
                                help_block.Inlines.Add ("\n");
                                help_block.Inlines.Add (new Bold (new Run (warning)));
                            }
                        }

                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
                        
                        Button try_again_button = new Button () {
                            Content = "Try again…"
                        };

                        
                        ContentCanvas.Children.Add (bullets_block);
                        Canvas.SetLeft (bullets_block, 195);
                        Canvas.SetTop (bullets_block, 100);
                        
                        ContentCanvas.Children.Add (help_block);
                        Canvas.SetLeft (help_block, 210);
                        Canvas.SetTop (help_block, 100);
                        
                        TaskbarItemInfo.ProgressValue = 1.0;
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                        
                        Buttons.Add (try_again_button);
                        Buttons.Add (cancel_button);
    
                        
                        cancel_button.Click += delegate {
                            Controller.PageCancelled ();
                        };
                        
                        try_again_button.Click += delegate {
                            Controller.ErrorPageCompleted ();
                        };
                        
                        break;
                    }    

                    case PageType.CryptoSetup: {
                        Header      = "Set up file encryption";
                        Description = "Please a provide a strong password that you don't use elsewhere below:";

                        TextBlock password_label = new TextBlock () {
                            Text       = "Password:",
                            FontWeight = FontWeights.Bold
                        };
                        
                        PasswordBox password_box = new PasswordBox () {
                            Width = 200
                        };
                        
                        TextBox visible_password_box = new TextBox () {
                            Width = 200,
                            Visibility = Visibility.Hidden
                        };
                        
                        CheckBox show_password_checkbox = new CheckBox () {
                            Content = "Show password",
                            IsChecked = false
                        };
                        
                        TextBlock info_label = new TextBlock () {
                            Text         = "This password can't be changed later, and your files can't be recovered if it's forgotten.",
                            TextWrapping = TextWrapping.Wrap,
                            Width        = 315
                        };

                        Image warning_image = new Image () {
                            Source = Imaging.CreateBitmapSourceFromHIcon (Drawing.SystemIcons.Information.Handle,
                                Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions ())
                        };

                        show_password_checkbox.Checked += delegate {
                            visible_password_box.Text       = password_box.Password;
                            visible_password_box.Visibility = Visibility.Visible;
                            password_box.Visibility         = Visibility.Hidden;
                        };
                        
                        show_password_checkbox.Unchecked += delegate {
                            password_box.Password           = visible_password_box.Text;
                            password_box.Visibility         = Visibility.Visible;
                            visible_password_box.Visibility = Visibility.Hidden;
                        };
                        
                        password_box.PasswordChanged += delegate {
                            Controller.CheckCryptoSetupPage (password_box.Password);
                        };
                        
                        visible_password_box.TextChanged += delegate {
                            Controller.CheckCryptoSetupPage (visible_password_box.Text);
                        };
                        
                        Button continue_button = new Button () {
                            Content = "Continue",
                            IsEnabled = false
                        };
                        
                        continue_button.Click += delegate {
                            if (show_password_checkbox.IsChecked == true)
                                Controller.CryptoSetupPageCompleted (visible_password_box.Text);
                            else
                                Controller.CryptoSetupPageCompleted (password_box.Password);
                        };
                        
                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
                        
                        cancel_button.Click += delegate {
                            Controller.CryptoPageCancelled ();
                        };
                        
                        Controller.UpdateCryptoSetupContinueButtonEvent += delegate (bool button_enabled) {
                            Dispatcher.BeginInvoke ((Action) delegate {
                                continue_button.IsEnabled = button_enabled;
                            });
                        };
                        
                        ContentCanvas.Children.Add (password_label);
                        Canvas.SetLeft (password_label, 270);
                        Canvas.SetTop (password_label, 180);
                        
                        ContentCanvas.Children.Add (password_box);
                        Canvas.SetLeft (password_box, 335);
                        Canvas.SetTop (password_box, 180);
                        
                        ContentCanvas.Children.Add (visible_password_box);
                        Canvas.SetLeft (visible_password_box, 335);
                        Canvas.SetTop (visible_password_box, 180);
                        
                        ContentCanvas.Children.Add (show_password_checkbox);
                        Canvas.SetLeft (show_password_checkbox, 338);
                        Canvas.SetTop (show_password_checkbox, 208);

                        ContentCanvas.Children.Add (info_label);
                        Canvas.SetLeft (info_label, 240);
                        Canvas.SetTop (info_label, 300);
                                                                         
                        ContentCanvas.Children.Add (warning_image);
                        Canvas.SetLeft (warning_image, 193);
                        Canvas.SetTop (warning_image, 300);
                    
                        Buttons.Add (continue_button);
                        Buttons.Add (cancel_button);
                        
                        break;
                    }
                        
                    case PageType.CryptoPassword: {
                        
                        Header      = "This project contains encrypted files";
                        Description = "Please enter the password to see their contents.";
                        
                        TextBlock password_label = new TextBlock () {
                            Text       = "Password:",
                            FontWeight = FontWeights.Bold
                        };
                        
                        PasswordBox password_box = new PasswordBox () {
                            Width = 200
                        };
                        
                        TextBox visible_password_box = new TextBox () {
                            Width = 200,
                            Visibility = Visibility.Hidden
                        };
                        
                        CheckBox show_password_checkbox = new CheckBox () {
                            Content = "Show password",
                            IsChecked = false
                        };
                        
                        show_password_checkbox.Checked += delegate {
                            visible_password_box.Text = password_box.Password;
                            visible_password_box.Visibility = Visibility.Visible;
                            password_box.Visibility = Visibility.Hidden;
                        };
                        
                        show_password_checkbox.Unchecked += delegate {
                            password_box.Password = visible_password_box.Text;
                            password_box.Visibility = Visibility.Visible;
                            visible_password_box.Visibility = Visibility.Hidden;
                        };
                        
                        password_box.PasswordChanged += delegate {
                            Controller.CheckCryptoPasswordPage (password_box.Password);
                        };
                        
                        visible_password_box.TextChanged += delegate {
                            Controller.CheckCryptoPasswordPage (visible_password_box.Text);
                        };
                        
                        Button continue_button = new Button () {
                            Content = "Continue",
                            IsEnabled = false
                        };
                        
                        continue_button.Click += delegate {
                            if (show_password_checkbox.IsChecked == true)
                                Controller.CryptoPasswordPageCompleted (visible_password_box.Text);
                            else
                                Controller.CryptoPasswordPageCompleted (password_box.Password);
                        };
                        
                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
                        
                        cancel_button.Click += delegate {
                            Controller.CryptoPageCancelled ();
                        };
                        
                        Controller.UpdateCryptoPasswordContinueButtonEvent += delegate (bool button_enabled) {
                            Dispatcher.BeginInvoke ((Action) delegate {
                                continue_button.IsEnabled = button_enabled;
                            });
                        };
                        
                        ContentCanvas.Children.Add (password_label);
                        Canvas.SetLeft (password_label, 270);
                        Canvas.SetTop (password_label, 180);
                        
                        ContentCanvas.Children.Add (password_box);
                        Canvas.SetLeft (password_box, 335);
                        Canvas.SetTop (password_box, 180);
                        
                        ContentCanvas.Children.Add (visible_password_box);
                        Canvas.SetLeft (visible_password_box, 335);
                        Canvas.SetTop (visible_password_box, 180);
                        
                        ContentCanvas.Children.Add (show_password_checkbox);
                        Canvas.SetLeft (show_password_checkbox, 338);
                        Canvas.SetTop (show_password_checkbox, 208);
                        
                        Buttons.Add (continue_button);
                        Buttons.Add (cancel_button);
                        
                        break;
                    }

                    case PageType.Finished: {
                        Header      = "Your shared project is ready!";
                        Description = "You can find the files in your SparkleShare folder.";
                        
                        
                        Button finish_button = new Button () {
                            Content = "Finish"
                        };
    
                        Button show_files_button = new Button () {
                            Content = "Show files…"
                        };

                        if (warnings.Length > 0) {
                            Image warning_image = new Image () {
                                Source = Imaging.CreateBitmapSourceFromHIcon (Drawing.SystemIcons.Information.Handle,
                                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions ())
                            };
                            
                            TextBlock warning_block = new TextBlock () {
                                Text         = warnings [0],
                                Width        = 310,
                                TextWrapping = TextWrapping.Wrap
                            };
                                                                                
                            ContentCanvas.Children.Add (warning_image);
                            Canvas.SetLeft (warning_image, 193);
                            Canvas.SetTop (warning_image, 100);
                                                                                
                            ContentCanvas.Children.Add (warning_block);
                            Canvas.SetLeft (warning_block, 240);
                            Canvas.SetTop (warning_block, 100);
                        }
                        
                        TaskbarItemInfo.ProgressValue = 0.0;
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

                        Buttons.Add (show_files_button);
                        Buttons.Add (finish_button);
                        

                        finish_button.Click += delegate {
                            Controller.FinishPageCompleted ();
                        };

                        show_files_button.Click += delegate {
                            Controller.ShowFilesClicked ();
                        };

                        break;
                    }
                        
                    case PageType.Tutorial: {
                        switch (Controller.TutorialPageNumber) {
                            case 1: {
                                Header      = "What's happening next?";
                                Description = "SparkleShare creates a special folder on your computer " +
                                    "that will keep track of your projects.";
    
                            
                                WPF.Image slide_image = new WPF.Image () {
                                    Width  = 324,
                                    Height = 200
                                };
                            
                                slide_image.Source = SparkleUIHelpers.GetImageSource ("tutorial-slide-1");
                            
                                Button skip_tutorial_button = new Button () {
                                    Content = "Skip tutorial"
                                };
                                
                                Button continue_button = new Button () {
                                    Content = "Continue"
                                };
                            
                            
                                ContentCanvas.Children.Add (slide_image);
                                Canvas.SetLeft (slide_image, 228);
                                Canvas.SetTop (slide_image, 130);
                            
                                Buttons.Add (skip_tutorial_button);
                                Buttons.Add (continue_button);
                               
                                
                                skip_tutorial_button.Click += delegate {
                                    Controller.TutorialSkipped ();
                                };
                                
                                continue_button.Click += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
                            
                                break;
                            }
                        
                            case 2: {
                                Header      = "Sharing files with others";
                                Description = "All files added to your project folders are synced automatically with " +
                                    "the host and your team members.";
    
                            
                                Button continue_button = new Button () {
                                    Content = "Continue"
                                };
                                
                                WPF.Image slide_image = new WPF.Image () {
                                    Width  = 324,
                                    Height = 200
                                };
                            
                                slide_image.Source = SparkleUIHelpers.GetImageSource ("tutorial-slide-2");
                            
                            
                                ContentCanvas.Children.Add (slide_image);
                                Canvas.SetLeft (slide_image, 228);
                                Canvas.SetTop (slide_image, 130);
                            
                                Buttons.Add (continue_button);
                            
                                
                                continue_button.Click += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
                            
                                break;
                            }
                            
                            case 3: {
                                Header      = "The status icon helps you";
                                Description = "It shows the syncing progress, provides easy access to " +
                                    "your projects and lets you view recent changes.";
    
                                Button continue_button = new Button () {
                                    Content = "Continue"
                                };
                                
                                WPF.Image slide_image = new WPF.Image () {
                                    Width  = 324,
                                    Height = 200
                                };
                            
                                slide_image.Source = SparkleUIHelpers.GetImageSource ("tutorial-slide-3");
                            
                            
                                ContentCanvas.Children.Add (slide_image);
                                Canvas.SetLeft (slide_image, 228);
                                Canvas.SetTop (slide_image, 130);
                            
                                Buttons.Add (continue_button);                                
                            
                            
                                continue_button.Click += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
                            
                                break;
                            }
                            
                            case 4: {
                                Header      = "Here's your unique client ID";
                                Description = "You'll need it whenever you want to link this computer to a host. " +
                                    "You can also find it in the status icon menu.";
                            

                                TextBox link_code_text_box = new TextBox () {
                                    Text         = Program.Controller.CurrentUser.PublicKey,
                                    Width        = 250,
                                    MaxLines     = 1,
                                    TextWrapping = TextWrapping.NoWrap,
                                    IsEnabled    = false
                                };

                                Button copy_button = new Button () {
                                    Content = "Copy",
                                    Width   = 60
                                };

                                Button finish_button = new Button () {
                                    Content = "Finish"
                                };
                                
                                CheckBox check_box = new CheckBox () {
                                    Content   = "Add SparkleShare to startup items",
                                    IsChecked = true
                                };
                            
                            
                                ContentCanvas.Children.Add (link_code_text_box);
                                Canvas.SetLeft (link_code_text_box, 235);
                                Canvas.SetTop (link_code_text_box, 190);

                                ContentCanvas.Children.Add (copy_button);
                                Canvas.SetLeft (copy_button, 490);
                                Canvas.SetTop (copy_button, 190);

                                ContentCanvas.Children.Add (check_box);
                                Canvas.SetLeft (check_box, 185);
                                Canvas.SetBottom (check_box, 12);

                                Buttons.Add (finish_button);
                                
                                
                                check_box.Click += delegate { Controller.StartupItemChanged (check_box.IsChecked.Value); };
                                finish_button.Click += delegate { Controller.TutorialPageCompleted (); };
                                copy_button.Click += delegate { Controller.CopyToClipboardClicked(); };
    
                                break;
                            }
                        }

                        break;
                    }
                    }
                    
                    ShowAll ();
                });        
            };
        }
    }
}
