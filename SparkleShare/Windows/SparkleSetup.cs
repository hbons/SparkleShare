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
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms.Integration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Data;
using System.Media;
using WPF = System.Windows.Controls;

namespace SparkleShare {

    public class SparkleSetup : SparkleSetupWindow {
	
		public SparkleSetupController Controller = new SparkleSetupController ();
		
		
		public SparkleSetup ()
		{
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
			
            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
				Dispatcher.Invoke ((Action) delegate {
					Reset ();
					
					switch (type) {
                    case PageType.Setup: {
						Header      = "Welcome to SparkleShare!";
			       		Description = "Before we get started, what's your name and email?\n" +
			            	"Don't worry, this information will only visible to any team members.";
		
						
			            TextBlock name_label = new TextBlock () {
			                Text = "Full Name:",
							Width = 150,
							TextAlignment = TextAlignment.Right,
							FontWeight = FontWeights.Bold
			            };
													
						TextBox name_box = new TextBox () {
							Text  = Controller.GuessedUserName,
							Width = 175
						};
						
						
			            TextBlock email_label = new TextBlock () {
			                Text    = "Email:",
							Width = 150,
							TextAlignment = TextAlignment.Right,
							FontWeight = FontWeights.Bold
			            };
						
						TextBox email_box = new TextBox () {
							Width = 175,
							Text = Controller.GuessedUserEmail
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
						
						Buttons.Add (continue_button);
						Buttons.Add (cancel_button);
						
						
						Controller.UpdateSetupContinueButtonEvent += delegate (bool enabled) {
						Dispatcher.Invoke ((Action) delegate {
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
							Dispatcher.Invoke ((Action) delegate {
								SparkleUI.StatusIcon.Dispose ();	
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
						
						GridView grid_view = new GridView ();
						
				        grid_view.Columns.Add (
							new GridViewColumn {
								DisplayMemberBinding = new Binding ("Text")
							}
						);
						
						// TODO: 
						// - Disable column headers
						// - Add plugin images
						// - Nicer markup: <b>Name</b>\n<small>Description</small>
						foreach (SparklePlugin plugin in Controller.Plugins) {
							list_view.Items.Add (
								new {
									Text = plugin.Name + "\n" + plugin.Description
							});
						}		
						
				        list_view.View          = grid_view;
						list_view.SelectedIndex = Controller.SelectedPluginIndex;
						
						
			            TextBlock address_label = new TextBlock () {
			                Text       = "Address:",
							FontWeight = FontWeights.Bold
			            };
													
						TextBox address_box = new TextBox () {
							Width = 200,
							Text  = Controller.PreviousAddress
						};
						
						TextBlock address_help_label = new TextBlock () {
			                Text       = "",
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
							Text  = Controller.PreviousPath
						};
						
						TextBlock path_help_label = new TextBlock () {
			                Text       = "",
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
						

						Buttons.Add (add_button);
						Buttons.Add (cancel_button);
						
						
                        Controller.ChangeAddressFieldEvent += delegate (string text,
                            string example_text, FieldState state) {

                            Dispatcher.Invoke ((Action) delegate {
                                address_box.Text        = text;
                                address_box.IsEnabled   = (state == FieldState.Enabled);
                                address_help_label.Text = example_text;
                            });
                        };


                        Controller.ChangePathFieldEvent += delegate (string text,
                            string example_text, FieldState state) {

                            Dispatcher.Invoke ((Action) delegate {
                                path_box.Text        = text;
                                path_box.IsEnabled   = (state == FieldState.Enabled);
                                path_help_label.Text = example_text;
                            });
                        };
						
					  	Controller.UpdateAddProjectButtonEvent += delegate (bool button_enabled) {
                            Dispatcher.Invoke ((Action) delegate {
                                add_button.IsEnabled = button_enabled;
                            });
                        };
						
						list_view.SelectionChanged += delegate {
							Controller.SelectedPluginChanged (list_view.SelectedIndex);
						};
						
						list_view.KeyDown += delegate {
							Controller.SelectedPluginChanged (list_view.SelectedIndex);
						};
						
						address_box.TextChanged += delegate {
							Controller.CheckAddPage (
								address_box.Text,
								path_box.Text,
								list_view.SelectedIndex
	                        );
						};
						
						path_box.TextChanged += delegate {
							Controller.CheckAddPage (
								address_box.Text,
								path_box.Text,
								list_view.SelectedIndex
	                        );
						};
						
						cancel_button.Click += delegate {
							Controller.PageCancelled ();
						};

                        add_button.Click += delegate {
                            Controller.AddPageCompleted (address_box.Text, path_box.Text);
                        };
						
						Controller.CheckAddPage (
							address_box.Text,
							path_box.Text,
							list_view.SelectedIndex
                        );
						
						break;
					}
						
						
					case PageType.Syncing: {
						Header = "Adding project ‘" + Controller.SyncingFolder + "’…";
                            Description = "This may take a while.\n" +
                                          "Are you sure it’s not coffee o'clock?";
 
						
						Button finish_button = new Button () {
                            Content   = "Finish",
							IsEnabled = false
                        };
    
                        Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
						
						ProgressBar progress_bar = new ProgressBar () {
							Width = 414,
							Height = 15,
							Value = 1
						};
						
						
						ContentCanvas.Children.Add (progress_bar);
						Canvas.SetLeft (progress_bar, 185);
						Canvas.SetTop (progress_bar, 150);
						
						Buttons.Add (finish_button);
						Buttons.Add (cancel_button);
						
                                                                                                    
                        Controller.UpdateProgressBarEvent += delegate (double percentage) {
                            Dispatcher.Invoke ((Action) delegate {
                                progress_bar.Value = percentage;
                            });
                        };
    
                        cancel_button.Click += delegate {
                            Controller.SyncingCancelled ();
                        };
						
						break;
					}
						
						
					case PageType.Error: {
						Header      = "Something went wrong…";
                        Description = "Please check the following:";
 
						// TODO: Bullet points
						
						
						Button cancel_button = new Button () {
                            Content = "Cancel"
                        };
						
						Button try_again_button = new Button () {
                            Content = "Try again…"
                        };
						
    					
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
						
					case PageType.Finished: {
						Header = "Project ‘" + Path.GetFileName (Controller.PreviousPath) +
							"’ succesfully added!";
                            
						Description = "Access the files from your SparkleShare folder.";
						
						
						// TODO: warnings
						
						Button finish_button = new Button () {
                            Content = "Finish"
                        };
    
                        Button open_folder_button = new Button () {
                            Content = "Open folder"
                        };
	
						
						Buttons.Add (finish_button);
						Buttons.Add (open_folder_button);
						
						
						finish_button.Click += delegate {
							Controller.FinishPageCompleted ();
						};

                        open_folder_button.Click += delegate {
                            Controller.OpenFolderClicked ();
                        };
						
						
						SystemSounds.Exclamation.Play ();
						// TODO: Catch attention without having to raise the window
						
						break;
					}
						
					case PageType.Tutorial: {
						
						switch (Controller.TutorialPageNumber) {
                            case 1: {
                                Header      = "What's happening next?";
                                Description = "SparkleShare creates a special folder on your computer " +
                                    "that will keep track of your projects.";
    
							
								WPF.Image slide_image = new WPF.Image () {
									Width  = 350,
									Height = 200
								};
							
								slide_image.Source = SparkleUIHelpers.GetImageSource ("tutorial-slide-1-windows");
							
							
								ContentCanvas.Children.Add (slide_image);
								Canvas.SetLeft (slide_image, 215);
								Canvas.SetTop (slide_image, 130);
							
                                Button skip_tutorial_button = new Button () {
                                    Content = "Skip tutorial"
                                };
    
    
                                Button continue_button = new Button () {
                                    Content = "Continue"
                                };
							
                            	// TODO: Add slides    
							
                                Buttons.Add (continue_button);
                                Buttons.Add (skip_tutorial_button);
								
								
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
    						
								Buttons.Add (continue_button);                        
								
								continue_button.Click += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
							
                                break;
                        	}
							
							case 3: {
								Header      = "The status icon is here to help";
                                Description = "It shows the syncing progress, provides easy access to " +
                                    "your projects and let's you view recent changes.";
    
							
                                Button continue_button = new Button () {
                                    Content = "Continue"
                                };
								
								WPF.Image slide_image = new WPF.Image () {
									Width  = 350,
									Height = 200
								};
							
								slide_image.Source = SparkleUIHelpers.GetImageSource ("tutorial-slide-3-windows");
							
							
								ContentCanvas.Children.Add (slide_image);
								Canvas.SetLeft (slide_image, 215);
								Canvas.SetTop (slide_image, 130);
							
								Buttons.Add (continue_button);                                
							
							
								continue_button.Click += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
							
                                break;
							}
							
							case 4: {
                                Header      = "Adding projects to SparkleShare";
                                Description = "You can do this through the status icon menu, or by clicking " +
                                    "magic buttons on webpages that look like this:";
							
							
								Button finish_button = new Button () {
                                    Content = "Finish"
								};
								
								WPF.Image slide_image = new WPF.Image () {
									Width  = 350,
									Height = 64
								};
								
								slide_image.Source = SparkleUIHelpers.GetImageSource ("tutorial-slide-4");
    
								CheckBox check_box = new CheckBox () {
									Content   = "Add SparkleShare to startup items",
									IsChecked = true
								};
							
							
								ContentCanvas.Children.Add (slide_image);
								Canvas.SetLeft (slide_image, 215);
								Canvas.SetTop (slide_image, 130);

								ContentCanvas.Children.Add (check_box);
								Canvas.SetLeft (check_box, 185);
								Canvas.SetBottom (check_box, 12);
								
                                Buttons.Add (finish_button);
								
								
								check_box.Click += delegate {
									Controller.StartupItemChanged (check_box.IsChecked.Value);
								};
							
								finish_button.Click += delegate {
									Controller.TutorialPageCompleted ();
								};
    
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
