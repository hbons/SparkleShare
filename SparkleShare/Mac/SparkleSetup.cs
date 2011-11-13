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
using System.Drawing;
using System.IO;
using System.Timers;

using System.Collections.Generic;

using Mono.Unix;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

    public class SparkleSetup : SparkleSetupWindow {

        public SparkleSetupController Controller = new SparkleSetupController ();

        private NSButton ContinueButton;
        private NSButton SyncButton;
        private NSButton TryAgainButton;
        private NSButton CancelButton;
        private NSButton SkipTutorialButton;
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
        private NSTextField PathTextField;
        private NSTextField PathLabel;
        private NSTextField PathHelpLabel;
        private NSTextField AddProjectTextField;
        private Timer timer;
        private NSTableView TableView;
        private NSScrollView ScrollView;
        private NSTableColumn IconColumn;
        private NSTableColumn DescriptionColumn;
        private SparkleDataSource DataSource;


        public SparkleSetup () : base ()
        {
            Controller.ChangePageEvent += delegate (PageType type) {
                InvokeOnMainThread (delegate {
                    Reset ();

                    switch (type) {
                    case PageType.Setup: {

                        Header       = "Welcome to SparkleShare!";
                        Description  = "Before we can create a SparkleShare folder on this " +
                                       "computer, we need some information from you.";


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
                            Frame           = new RectangleF (330, Frame.Height - 238, 196, 22),
                            StringValue     = Controller.GuessedUserName
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
                            Frame           = new RectangleF (330, Frame.Height - 268, 196, 22),
                            StringValue     = Controller.GuessedUserEmail
                        };

                        // TODO: Ugly hack, do properly with events
                        timer = new Timer () {
                            Interval = 50
                        };

                        ContinueButton = new NSButton () {
                            Title    = "Continue",
                            Enabled  = false
                        };

                        ContinueButton.Activated += delegate {
                            timer.Stop ();
                            timer = null;

                            string full_name = FullNameTextField.StringValue.Trim ();
                            string email     = EmailTextField.StringValue.Trim ();

                            Controller.SetupPageCompleted (full_name, email);
                        };

                        timer.Elapsed += delegate {
                            InvokeOnMainThread (delegate {
                                bool name_is_valid = !FullNameTextField.StringValue.Trim ().Equals ("");

                                bool email_is_valid = Program.Controller.IsValidEmail (
                                    EmailTextField.StringValue.Trim ());

                                ContinueButton.Enabled = (name_is_valid && email_is_valid);
                            });
                        };

                        timer.Start ();

                        ContentView.AddSubview (FullNameLabel);
                        ContentView.AddSubview (FullNameTextField);
                        ContentView.AddSubview (EmailLabel);
                        ContentView.AddSubview (EmailTextField);

                        Buttons.Add (ContinueButton);

                        break;
                    }

                    case PageType.Add: {

                        Header       = "Where's your project hosted?";
                        Description  = "";

                        AddressLabel = new NSTextField () {
                            Alignment       = NSTextAlignment.Left,
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            Editable        = false,
                            Frame           = new RectangleF (190, Frame.Height - 308, 160, 17),
                            StringValue     = "Address:",
                            Font            = SparkleUI.Font
                        };

                        AddressTextField = new NSTextField () {
                            Frame       = new RectangleF (190, Frame.Height - 336, 196, 22),
                            Font        = SparkleUI.Font,
                            StringValue = Controller.PreviousAddress,
                            Enabled     = (Controller.SelectedPlugin.Address == null)
                        };


                        PathLabel = new NSTextField () {
                            Alignment       = NSTextAlignment.Left,
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            Editable        = false,
                            Frame           = new RectangleF (190 + 196 + 16, Frame.Height - 308, 160, 17),
                            StringValue     = "Remote Path:",
                            Font            = SparkleUI.Font
                        };

                        PathTextField = new NSTextField () {
                            Frame           = new RectangleF (190 + 196 + 16, Frame.Height - 336, 196, 22),
                            StringValue     = Controller.PreviousPath,
                            Enabled         = (Controller.SelectedPlugin.Path == null),
                            Bezeled = true
                        };


                        AddressTextField.Cell.LineBreakMode    = NSLineBreakMode.TruncatingTail;
                        PathTextField.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;


                        PathHelpLabel = new NSTextField () {
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            TextColor       = NSColor.DisabledControlText,
                            Editable        = false,
                            Frame           = new RectangleF (190 + 196 + 16, Frame.Height - 355, 204, 17),
                            StringValue     = "e.g. ‘rupert/website-design’",
                            Font            = NSFontManager.SharedFontManager.FontWithFamily
                                                  ("Lucida Grande", NSFontTraitMask.Condensed, 0, 11)
                        };


                        TableView = new NSTableView () {
                            Frame            = new RectangleF (0, 0, 0, 0),
                            RowHeight        = 30,
                            IntercellSpacing = new SizeF (0, 12),
                            HeaderView       = null
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
                            });
                        };


                        Controller.ChangePathFieldEvent += delegate (string text,
                            string example_text, FieldState state) {

                            InvokeOnMainThread (delegate {
                                PathTextField.StringValue = text;
                                PathTextField.Enabled     = (state == FieldState.Enabled);

                                if (!string.IsNullOrEmpty (example_text))
                                    PathHelpLabel.StringValue = "e.g. " + example_text;
                            });
                        };

                        TableView.SelectRow (Controller.SelectedPluginIndex, false);


                        timer = new Timer () {
                            Interval = 50
                        };

                        // TODO: Use an event
                        timer.Elapsed += delegate {
                            if (TableView.SelectedRow != Controller.SelectedPluginIndex)
                                Controller.SelectedPluginChanged (TableView.SelectedRow);

                            InvokeOnMainThread (delegate {
                                // TODO: Move checking logic to controller
                                if (!string.IsNullOrWhiteSpace (AddressTextField.StringValue) &&
                                    !string.IsNullOrWhiteSpace (PathTextField.StringValue)) {

                                    SyncButton.Enabled = true;

                                } else {
                                    SyncButton.Enabled = false;
                                }
                            });
                        };

                        timer.Start ();


                        ContentView.AddSubview (ScrollView);
                        ContentView.AddSubview (AddressLabel);
                        ContentView.AddSubview (AddressTextField);
                        ContentView.AddSubview (PathLabel);
                        ContentView.AddSubview (PathTextField);
                        ContentView.AddSubview (PathHelpLabel);

                        SyncButton = new NSButton () {
                            Title = "Add",
                            Enabled = false
                        };

                            SyncButton.Activated += delegate {
                                timer.Stop ();
                                timer = null;

                                Controller.AddPageCompleted (
                                    AddressTextField.StringValue,
                                    PathTextField.StringValue
                                );
                            };

                        Buttons.Add (SyncButton);

                            CancelButton = new NSButton () {
                                Title = "Cancel"
                            };

                            CancelButton.Activated += delegate {
                                InvokeOnMainThread (delegate {
                                    PerformClose (this);
                                });
                            };

                        Buttons.Add (CancelButton);

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

                        Buttons.Add (TryAgainButton);

                        break;
                    }

                    case PageType.Finished: {

                        Header      = "Project succesfully added!";
                        Description = "Now you can access the files from " +
                                      "‘" + Controller.SyncingFolder + "’ in " +
                                      "your SparkleShare folder.";

                        FinishButton = new NSButton () {
                            Title = "Finish"
                        };

                        FinishButton.Activated += delegate {
                            InvokeOnMainThread (delegate {
                                Controller.FinishedPageCompleted ();
                                PerformClose (this);
                            });
                        };

                        OpenFolderButton = new NSButton () {
                            Title = "Open Folder"
                        };

                        OpenFolderButton.Activated += delegate {
                            Program.Controller.OpenSparkleShareFolder (Controller.SyncingFolder);
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
                            Description = "SparkleShare creates a special folder in your personal folder " +
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
                                "Pixmaps", "tutorial-slide-1.png");

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
                            Description = "All files added to your project folders are synced with the host " +
                                "automatically, as well as with your collaborators.";

                            ContinueButton = new NSButton () {
                                Title = "Continue"
                            };

                            ContinueButton.Activated += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                                "Pixmaps", "tutorial-slide-2.png");

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
                            Description = "It shows the syncing process status, " +
                                "and contains links to your projects and the event log.";

                            ContinueButton = new NSButton () {
                                Title = "Continue"
                            };

                            ContinueButton.Activated += delegate {
                                Controller.TutorialPageCompleted ();
                            };

                            string slide_image_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
                                "Pixmaps", "tutorial-slide-3.png");

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
                            Description = "Just click this button when you see it on the web, and " +
                                "the project will be automatically added:";

                            AddProjectTextField = new NSTextField () {
                                Frame           = new RectangleF (190, Frame.Height - 290, 640 - 240, 44),
                                BackgroundColor = NSColor.WindowBackground,
                                Bordered        = false,
                                Editable        = false,
                                Font            = SparkleUI.Font,
                                StringValue     = "…or select ‘Add Hosted Project…’ from the status icon menu " +
                                "to add one by hand."
                            };

                            FinishButton = new NSButton () {
                                Title = "Finish"
                            };

                            FinishButton.Activated += delegate {
                                InvokeOnMainThread (delegate {
                                    PerformClose (this);
                                });
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
                            ContentView.AddSubview (AddProjectTextField);
                            Buttons.Add (FinishButton);

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
}
