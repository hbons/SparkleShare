//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
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
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Mono.Unix;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleSetup : SparkleSetupWindow {

        public SparkleSetupController Controller = new SparkleSetupController ();

        private NSButton ContinueButton;
        private NSButton AddButton;
        private NSButton TryAgainButton;
        private NSButton CancelButton;
        private NSButton SkipTutorialButton;
        private NSButton StartupCheckButton;
        private NSButton OpenFolderButton;
        private NSButton FinishButton;
        private NSImage SlideImage;
        private NSImageView SlideImageView;
        private NSProgressIndicator ProgressIndicator;
        private NSTextField EmailLabel;
        private NSTextField EmailTextField;
        private NSTextField FullNameTextField;
        private NSTextField FullNameLabel;
        private NSTextField AddressTextField;
        private NSTextField AddressLabel;
        private NSTextField AddressHelpLabel;
        private NSTextField PathTextField;
        private NSTextField PathLabel;
        private NSTextField PathHelpLabel;
        private NSTextField WarningTextField;
        private NSImage WarningImage;
        private NSImageView WarningImageView;
        private NSTableView TableView;
        private NSScrollView ScrollView;
        private NSTableColumn IconColumn;
        private NSTableColumn DescriptionColumn;
        private SparkleDataSource DataSource;


        public SparkleSetup () : base ()
        {
            Controller.HideWindowEvent += delegate {
                InvokeOnMainThread (delegate {
                    PerformClose (this);
                });
            };

            Controller.ShowWindowEvent += delegate {
                InvokeOnMainThread (delegate {
                    OrderFrontRegardless ();
                });
            };

            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
                using (var a = new NSAutoreleasePool ())
                {
                    InvokeOnMainThread (delegate {
                        Reset ();
    
                        switch (type) {
                        case PageType.Setup: {
    
                            Header       = "Welcome to SparkleShare!";
                            Description  = "Before we get started, what's your name and email? " +
                                "Don't worry, this information will only visible to your team members.";
    
    
                            FullNameLabel = new NSTextField () {
                                Alignment       = NSTextAlignment.Right,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (165, Frame.Height - 234, 160, 17),
                                StringValue     = "Full Name:",
                                Font            = SparkleUI.Font
                            };
    
                            FullNameTextField = new NSTextField () {
                                Frame       = new RectangleF (330, Frame.Height - 238, 196, 22),
                                StringValue = Controller.GuessedUserName,
                                Delegate    = new SparkleTextFieldDelegate ()
                            };
    
                            EmailLabel = new NSTextField () {
                                Alignment       = NSTextAlignment.Right,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (165, Frame.Height - 264, 160, 17),
                                StringValue     = "Email:",
                                Font            = SparkleUI.Font
                            };
    
                            EmailTextField = new NSTextField () {
                                Frame       = new RectangleF (330, Frame.Height - 268, 196, 22),
                                StringValue = Controller.GuessedUserEmail,
                                Delegate    = new SparkleTextFieldDelegate ()
                            };
    
    
                            (FullNameTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                                Controller.CheckSetupPage (
                                    FullNameTextField.StringValue,
                                    EmailTextField.StringValue
                                );
                            };
    
                            (EmailTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                                Controller.CheckSetupPage (
                                    FullNameTextField.StringValue,
                                    EmailTextField.StringValue
                                );
                            };
    
    
                            ContinueButton = new NSButton () {
                                Title    = "Continue",
                                Enabled  = false
                            };
    
                            ContinueButton.Activated += delegate {
                                string full_name = FullNameTextField.StringValue.Trim ();
                                string email     = EmailTextField.StringValue.Trim ();
    
                                Controller.SetupPageCompleted (full_name, email);
                            };
							
							CancelButton = new NSButton () {
                                Title = "Cancel"
                            };
    
                            CancelButton.Activated += delegate {
                                Controller.SetupPageCancelled ();
                            };
							
    
                            Controller.UpdateSetupContinueButtonEvent += delegate (bool button_enabled) {
                                InvokeOnMainThread (delegate {
                                    ContinueButton.Enabled = button_enabled;
                                });
                            };
    
    
                            ContentView.AddSubview (FullNameLabel);
                            ContentView.AddSubview (FullNameTextField);
                            ContentView.AddSubview (EmailLabel);
                            ContentView.AddSubview (EmailTextField);
    
                            Buttons.Add (ContinueButton);
							Buttons.Add (CancelButton);
    
                            Controller.CheckSetupPage (
                                FullNameTextField.StringValue,
                                EmailTextField.StringValue
                            );
    
                            break;
                        }
    
                        case PageType.Invite: {
    
                            Header      = "You've received an invite!";
                            Description = "Do you want to add this project to SparkleShare?";
    
    
                            AddressLabel = new NSTextField () {
                                Alignment       = NSTextAlignment.Right,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (165, Frame.Height - 240, 160, 17),
                                StringValue     = "Address:",
                                Font            = SparkleUI.Font
                            };
    
                            PathLabel = new NSTextField () {
                                Alignment       = NSTextAlignment.Right,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (165, Frame.Height - 264, 160, 17),
                                StringValue     = "Remote Path:",
                                Font            = SparkleUI.Font
                            };
    
                            AddressTextField = new NSTextField () {
                                Alignment       = NSTextAlignment.Left,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (330, Frame.Height - 240, 260, 17),
                                StringValue     = Controller.PendingInvite.Address,
                                Font            = SparkleUI.BoldFont
                            };
    
                            PathTextField = new NSTextField () {
                                Alignment       = NSTextAlignment.Left,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (330, Frame.Height - 264, 260, 17),
                                StringValue     = Controller.PendingInvite.RemotePath,
                                Font            = SparkleUI.BoldFont
                            };
    
    
                            ContentView.AddSubview (AddressLabel);
                            ContentView.AddSubview (PathLabel);
                            ContentView.AddSubview (AddressTextField);
                            ContentView.AddSubview (PathTextField);
    
    
                            CancelButton = new NSButton () {
                                    Title = "Cancel"
                            };
    
                                CancelButton.Activated += delegate {
                                    Controller.PageCancelled ();
                                };
    
                            AddButton = new NSButton () {
                                 Title = "Add"
                            };
    
                                AddButton.Activated += delegate {
                                    Controller.InvitePageCompleted ();
                                };
    
                            Buttons.Add (AddButton);
                            Buttons.Add (CancelButton);
    
                            break;
                        }
    
                        case PageType.Add: {
    
                            Header      = "Where's your project hosted?";
                            Description = "";
    
                            AddressLabel = new NSTextField () {
                                Alignment       = NSTextAlignment.Left,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (190, Frame.Height - 308, 160, 17),
                                StringValue     = "Address:",
                                Font            = SparkleUI.BoldFont
                            };
    
                            AddressTextField = new NSTextField () {
                                Frame       = new RectangleF (190, Frame.Height - 336, 196, 22),
                                Font        = SparkleUI.Font,
                                StringValue = Controller.PreviousAddress,
                                Enabled     = (Controller.SelectedPlugin.Address == null),
                                Delegate    = new SparkleTextFieldDelegate ()
                            };
    
    
                            PathLabel = new NSTextField () {
                                Alignment       = NSTextAlignment.Left,
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Frame           = new RectangleF (190 + 196 + 16, Frame.Height - 308, 160, 17),
                                StringValue     = "Remote Path:",
                                Font            = SparkleUI.BoldFont
                            };
    
                            PathTextField = new NSTextField () {
                                Frame           = new RectangleF (190 + 196 + 16, Frame.Height - 336, 196, 22),
                                StringValue     = Controller.PreviousPath,
                                Enabled         = (Controller.SelectedPlugin.Path == null),
                                Delegate        = new SparkleTextFieldDelegate ()
                            };
    
    
                            AddressTextField.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
                            PathTextField.Cell.LineBreakMode    = NSLineBreakMode.TruncatingTail;
    
    
                            PathHelpLabel = new NSTextField () {
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                TextColor       = NSColor.DisabledControlText,
                                Editable        = false,
                                Frame           = new RectangleF (190 + 196 + 16, Frame.Height - 355, 204, 17),
                                StringValue     = "",
                                Font            = NSFontManager.SharedFontManager.FontWithFamily
                                                      ("Lucida Grande", NSFontTraitMask.Condensed, 0, 11)
                            };
    
                            AddressHelpLabel = new NSTextField () {
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                TextColor       = NSColor.DisabledControlText,
                                Editable        = false,
                                Frame           = new RectangleF (190, Frame.Height - 355, 204, 17),
                                StringValue     = "",
                                Font            = NSFontManager.SharedFontManager.FontWithFamily
                                                      ("Lucida Grande", NSFontTraitMask.Condensed, 0, 11)
                            };
    
    
                            TableView = new NSTableView () {
                                Frame            = new RectangleF (0, 0, 0, 0),
                                RowHeight        = 30,
                                IntercellSpacing = new SizeF (0, 12),
                                HeaderView       = null,
                                Delegate         = new SparkleTableViewDelegate ()
                            };
    
                            ScrollView = new NSScrollView () {
                                Frame               = new RectangleF (190, Frame.Height - 280, 408, 175),
                                DocumentView        = TableView,
                                HasVerticalScroller = true,
                                BorderType          = NSBorderType.BezelBorder
                            };
    
                            IconColumn = new NSTableColumn (new NSImage ()) {
                                Width = 42,
                                HeaderToolTip = "Icon",
                                DataCell = new NSImageCell ()
                            };
    
                            DescriptionColumn = new NSTableColumn () {
                                Width         = 350,
                                HeaderToolTip = "Description",
                                Editable      = false
                            };
    
                            DescriptionColumn.DataCell.Font =
                                NSFontManager.SharedFontManager.FontWithFamily (
                                    "Lucida Grande", NSFontTraitMask.Condensed, 0, 11);
    
                            TableView.AddColumn (IconColumn);
                            TableView.AddColumn (DescriptionColumn);
    
                            DataSource = new SparkleDataSource ();
    
                            foreach (SparklePlugin plugin in Controller.Plugins)
                                DataSource.Items.Add (plugin);
    
                            TableView.DataSource = DataSource;
                            TableView.ReloadData ();
    
    
                            Controller.ChangeAddressFieldEvent += delegate (string text,
                                string example_text, FieldState state) {
    
                                InvokeOnMainThread (delegate {
                                    AddressTextField.StringValue = text;
                                    AddressTextField.Enabled     = (state == FieldState.Enabled);
                                    AddressHelpLabel.StringValue = example_text;
                                });
                            };
    
    
                            Controller.ChangePathFieldEvent += delegate (string text,
                                string example_text, FieldState state) {
    
                                InvokeOnMainThread (delegate {
                                    PathTextField.StringValue = text;
                                    PathTextField.Enabled     = (state == FieldState.Enabled);
                                    PathHelpLabel.StringValue = example_text;
                                });
                            };
    
    
                            TableView.SelectRow (Controller.SelectedPluginIndex, false);
    
    
                            (AddressTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                                Controller.CheckAddPage (
                                    AddressTextField.StringValue,
                                    PathTextField.StringValue,
                                    TableView.SelectedRow
                                );
                            };
    
                             (PathTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                                Controller.CheckAddPage (
                                    AddressTextField.StringValue,
                                    PathTextField.StringValue,
                                    TableView.SelectedRow
                                );
                            };
    
                            (TableView.Delegate as SparkleTableViewDelegate).SelectionChanged += delegate {
                                Controller.SelectedPluginChanged (TableView.SelectedRow);
    
                                Controller.CheckAddPage (
                                    AddressTextField.StringValue,
                                    PathTextField.StringValue,
                                    TableView.SelectedRow
                                );
                            };
    
    
                            Controller.UpdateAddProjectButtonEvent += delegate (bool button_enabled) {
                                InvokeOnMainThread (delegate {
                                    AddButton.Enabled = button_enabled;
                                });
                            };
    
    
                            ContentView.AddSubview (ScrollView);
                            ContentView.AddSubview (AddressLabel);
                            ContentView.AddSubview (AddressTextField);
                            ContentView.AddSubview (AddressHelpLabel);
                            ContentView.AddSubview (PathLabel);
                            ContentView.AddSubview (PathTextField);
                            ContentView.AddSubview (PathHelpLabel);
    
                            AddButton = new NSButton () {
                                Title = "Add",
                                Enabled = false
                            };
    
                                AddButton.Activated += delegate {
                                    Controller.AddPageCompleted (
                                        AddressTextField.StringValue,
                                        PathTextField.StringValue
                                    );
                                };
    
                            Buttons.Add (AddButton);
    
                                CancelButton = new NSButton () {
                                    Title = "Cancel"
                                };
    
                                CancelButton.Activated += delegate {
                                    Controller.PageCancelled ();
                                };
    
                            Buttons.Add (CancelButton);
    
                            Controller.CheckAddPage (
                                AddressTextField.StringValue,
                                PathTextField.StringValue,
                                TableView.SelectedRow
                            );
    
    
                            break;
                        }
    
                        case PageType.Syncing: {
    
                            Header      = "Adding project ‘" + Controller.SyncingFolder + "’…";
                            Description = "This may take a while.\n" +
                                          "Are you sure it’s not coffee o'clock?";
    
                            ProgressIndicator = new NSProgressIndicator () {
                                Frame    = new RectangleF (190, Frame.Height - 200, 640 - 150 - 80, 20),
                                Style    = NSProgressIndicatorStyle.Bar,
                                MinValue = 0.0,
                                MaxValue = 100.0,
                                Indeterminate = false,
                                DoubleValue = 1.0
                            };
    
                            ProgressIndicator.StartAnimation (this);
                                                                                                    
                            Controller.UpdateProgressBarEvent += delegate (double percentage) {
                                InvokeOnMainThread (delegate {
                                    ProgressIndicator.DoubleValue = percentage;
                                });
                            };
    
                            ContentView.AddSubview (ProgressIndicator);
    
                            FinishButton = new NSButton () {
                                Title = "Finish",
                                Enabled = false
                            };
    
                            CancelButton = new NSButton () {
                                Title = "Cancel"
                            };
    
                            CancelButton.Activated += delegate {
                                Controller.SyncingCancelled ();
                            };
    
                            Buttons.Add (FinishButton);
                            Buttons.Add (CancelButton);
    
                            break;
                        }
    
                        case PageType.Error: {
    
                            Header      = "Something went wrong…";
                            Description = "Please check the following:";
    
                            // Displaying marked up text with Cocoa is
                            // a pain, so we just use a webview instead
                            WebView web_view = new WebView ();
                            web_view.Frame = new RectangleF (190, Frame.Height - 525, 375, 400);
    
                            string html = "<style>" +
                                "* {" +
                                "  font-family: 'Lucida Grande';" +
                                "  font-size: 12px; cursor: default;" +
                                "}" +
                                "body {" +
                                "  -webkit-user-select: none;" +
                                "  margin: 0;" +
                                "  padding: 3px;" +
                                "}" +
                                "li {" +
                                "  margin-bottom: 16px;" +
                                "  margin-left: 0;" +
                                "  padding-left: 0;" +
                                "  line-height: 20px;" +
                                "}" +
                                "ul {" +
                                "  padding-left: 24px;" +
                                "}" +
                                "</style>" +
                                "<ul>" +
                                "  <li>First, have you tried turning it off and on again?</li>" +
                                "  <li><b>" + Controller.PreviousUrl + "</b> is the address we've compiled. Does this look alright?</li>" +
                                "  <li>The host needs to know who you are. Did you upload the key that's in your SparkleShare folder?</li>" +
                                "</ul>";
    
                            web_view.MainFrame.LoadHtmlString (html, new NSUrl (""));
                            web_view.DrawsBackground = false;
    
                            ContentView.AddSubview (web_view);
    
                            TryAgainButton = new NSButton () {
                                Title = "Try again…"
                            };
    
                            TryAgainButton.Activated += delegate {
                                Controller.ErrorPageCompleted ();
                            };
							
							CancelButton = new NSButton () {
                                Title = "Cancel"
                            };
    
                            CancelButton.Activated += delegate {
                                Controller.PageCancelled ();
                            };
    
                            Buttons.Add (TryAgainButton);
							Buttons.Add (CancelButton);
    
                            break;
                        }
    
                        case PageType.Finished: {
    
                            Header      = "Project ‘" + Path.GetFileName (Controller.PreviousPath) +
                                          "’ succesfully added!";
                            Description = "Access the files from your SparkleShare folder.";
    
                            if (warnings != null) {
                                WarningImage = NSImage.ImageNamed ("NSCaution");
                                WarningImage.Size = new SizeF (24, 24);
    
                                WarningImageView = new NSImageView () {
                                    Image = WarningImage,
                                    Frame = new RectangleF (190, Frame.Height - 175, 24, 24)
                                };
    
                                WarningTextField = new NSTextField () {
                                    Frame           = new RectangleF (230, Frame.Height - 245, 325, 100),
                                    StringValue     = warnings [0],
                                    BackgroundColor = NSColor.WindowBackground,
                                    Bordered        = false,
                                    Editable        = false,
                                    Font            = SparkleUI.Font
                                };
    
                                ContentView.AddSubview (WarningImageView);
                                ContentView.AddSubview (WarningTextField);
                            }
    
                            FinishButton = new NSButton () {
                                Title = "Finish"
                            };
    
                            FinishButton.Activated += delegate {
                                Controller.FinishPageCompleted ();
                            };
    
                            OpenFolderButton = new NSButton () {
                                Title = "Open Folder"
                            };
    
                            OpenFolderButton.Activated += delegate {
                                Controller.OpenFolderClicked ();
                            };
    
                            Buttons.Add (FinishButton);
                            Buttons.Add (OpenFolderButton);
    
                            NSApplication.SharedApplication.RequestUserAttention
                                (NSRequestUserAttentionType.CriticalRequest);
    
                            NSSound.FromName ("Glass").Play ();
    
                            break;
                        }
    
                        case PageType.Tutorial: {
    
                            switch (Controller.TutorialPageNumber) {
                            case 1: {
                                Header      = "What's happening next?";
                                Description = "SparkleShare creates a special folder on your computer " +
                                    "that will keep track of your projects.";
    
                                SkipTutorialButton = new NSButton () {
                                    Title = "Skip Tutorial"
                                };
    
                                SkipTutorialButton.Activated += delegate {
                                    Controller.TutorialSkipped ();
                                };
    
                                ContinueButton = new NSButton () {
                                    Title = "Continue"
                                };
    
                                ContinueButton.Activated += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
    
                                string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                                    "Pixmaps", "tutorial-slide-1-mac.png");
    
                                SlideImage = new NSImage (slide_image_path) {
                                    Size = new SizeF (350, 200)
                                };
    
                                SlideImageView = new NSImageView () {
                                    Image = SlideImage,
                                    Frame = new RectangleF (215, Frame.Height - 350, 350, 200)
                                };
    
                                ContentView.AddSubview (SlideImageView);
                                Buttons.Add (ContinueButton);
                                Buttons.Add (SkipTutorialButton);
    
                                break;
                            }
    
                            case 2: {
                                Header      = "Sharing files with others";
                                Description = "All files added to your project folders are synced automatically with " +
                                    "the host and your team members.";
    
                                ContinueButton = new NSButton () {
                                    Title = "Continue"
                                };
    
                                ContinueButton.Activated += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
    
                                string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                                    "Pixmaps", "tutorial-slide-2-mac.png");
    
                                SlideImage = new NSImage (slide_image_path) {
                                    Size = new SizeF (350, 200)
                                };
    
                                SlideImageView = new NSImageView () {
                                    Image = SlideImage,
                                    Frame = new RectangleF (215, Frame.Height - 350, 350, 200)
                                };
    
                                ContentView.AddSubview (SlideImageView);
                                Buttons.Add (ContinueButton);
    
                                break;
                            }
    
                            case 3: {
                                Header      = "The status icon is here to help";
                                Description = "It shows the syncing progress, provides easy access to " +
                                    "your projects and let's you view recent changes.";
    
                                ContinueButton = new NSButton () {
                                    Title = "Continue"
                                };
    
                                ContinueButton.Activated += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
    
                                string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                                    "Pixmaps", "tutorial-slide-3-mac.png");
    
                                SlideImage = new NSImage (slide_image_path) {
                                    Size = new SizeF (350, 200)
                                };
    
                                SlideImageView = new NSImageView () {
                                    Image = SlideImage,
                                    Frame = new RectangleF (215, Frame.Height - 350, 350, 200)
                                };
    
                                ContentView.AddSubview (SlideImageView);
                                Buttons.Add (ContinueButton);
    
                                break;
                            }
    
                            case 4: {
                                Header      = "Adding projects to SparkleShare";
                                Description = "You can do this through the status icon menu, or by clicking " +
                                    "magic buttons on webpages that look like this:";
    
    
                                StartupCheckButton = new NSButton () {
                                    Frame = new RectangleF (190, Frame.Height - 400, 300, 18),
                                    Title = "Add SparkleShare to startup items",
                                    State = NSCellStateValue.On
                                };
    
                                StartupCheckButton.SetButtonType (NSButtonType.Switch);
    
                                StartupCheckButton.Activated += delegate {
                                    Controller.StartupItemChanged (StartupCheckButton.State == NSCellStateValue.On);
                                };
    
                                FinishButton = new NSButton () {
                                    Title = "Finish"
                                };
    
                                FinishButton.Activated += delegate {
                                    Controller.TutorialPageCompleted ();
                                };
    
    
                                string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                                    "Pixmaps", "tutorial-slide-4.png");
    
                                SlideImage = new NSImage (slide_image_path) {
                                    Size = new SizeF (350, 64)
                                };
    
                                SlideImageView = new NSImageView () {
                                    Image = SlideImage,
                                    Frame = new RectangleF (215, Frame.Height - 215, 350, 64)
                                };
								
    
                                ContentView.AddSubview (SlideImageView);
                                ContentView.AddSubview (StartupCheckButton);
                                Buttons.Add (FinishButton);
    
                                break;
                            }
                            }
    
                            break;
                        }
                        }
    
                        ShowAll ();
                    });
                }
            };
        }
    }


    [Register("SparkleDataSource")]
    public class SparkleDataSource : NSTableViewDataSource {

        public List<object> Items ;


        public SparkleDataSource ()
        {
            Items = new List<object> ();
        }


        [Export("numberOfRowsInTableView:")]
        public int numberOfRowsInTableView (NSTableView table_view)
        {
            if (Items == null)
                return 0;
            else
                return Items.Count;
        }


        [Export("tableView:objectValueForTableColumn:row:")]
        public NSObject objectValueForTableColumn (NSTableView table_view,
            NSTableColumn table_column, int row_index)
        {
            // TODO: Style text nicely: "<b>Name</b>\n<grey>Description</grey>"
            if (table_column.HeaderToolTip.Equals ("Description")) {
                return new NSString (
                    (Items [row_index] as SparklePlugin).Name + "\n" +
                    (Items [row_index] as SparklePlugin).Description
                );

            } else {
                return new NSImage ((Items [row_index] as SparklePlugin).ImagePath) {
                    Size = new SizeF (24, 24)
                };
            }
        }
    }


    public class SparkleTextFieldDelegate : NSTextFieldDelegate {

        public event StringValueChangedHandler StringValueChanged;
        public delegate void StringValueChangedHandler ();


        public override void Changed (NSNotification notification)
        {
            if (StringValueChanged!= null)
                StringValueChanged ();
        }


        public override string [] GetCompletions (NSControl control, NSTextView text_view,
            string [] a, MonoMac.Foundation.NSRange range, int b)
        {
            return new string [0];
        }
    }


    public class SparkleTableViewDelegate : NSTableViewDelegate {

        public event SelectionChangedHandler SelectionChanged;
        public delegate void SelectionChangedHandler ();


        public override void SelectionDidChange (NSNotification notification)
        {
            if (SelectionChanged != null)
                SelectionChanged ();
        }
    }
}
