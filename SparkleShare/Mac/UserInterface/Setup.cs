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
using MonoMac.WebKit;

using Sparkles;

namespace SparkleShare {

    public class Setup : SetupWindow {

        public SetupController Controller = new SetupController ();

        private NSButton ContinueButton, AddButton, TryAgainButton, CancelButton, FinishButton, ShowFilesButton;

        private NSTextField FullNameTextField, FullNameLabel, EmailLabel, EmailTextField;

        private NSTextField AddressTextField, AddressLabel, AddressHelpLabel;
        private NSTextField PathLabel, PathTextField, PathHelpLabel;

        private NSTextField ProgressLabel, PasswordTextField, VisiblePasswordTextField, PasswordLabel, WarningTextField;

        private NSImage WarningImage;
        private NSImageView WarningImageView;

        private NSButton HistoryCheckButton, ShowPasswordCheckButton;
        private NSProgressIndicator ProgressIndicator;
        private NSTableColumn IconColumn, DescriptionColumn;
        private NSTableView TableView;
        private NSScrollView ScrollView;
        private SparkleDataSource DataSource;

        private NSButtonCell ButtonCellProto;
        private NSMatrix Matrix;
        List<NSTextField> storage_type_descriptions;


        public Setup () : base ()
        {
            Controller.HideWindowEvent += delegate {
                SparkleShare.Controller.Invoke (() => PerformClose (this));
            };

            Controller.ShowWindowEvent += delegate {
                SparkleShare.Controller.Invoke (() => OrderFrontRegardless ());
            };

            Controller.ChangePageEvent += delegate (PageType type, string [] warnings) {
                SparkleShare.Controller.Invoke (() => {
                    Reset ();
                    ShowPage (type, warnings);
                    ShowAll ();
                });
            };

        }


        public void ShowPage (PageType type, string [] warnings)
        {
            if (type == PageType.Setup) {
                Header      = "Welcome to SparkleShare!";
                Description = "First off, what’s your name and email?\n(visible only to team members)";

                FullNameLabel       = new SparkleLabel ("Full Name:", NSTextAlignment.Right);
                FullNameLabel.Frame = new RectangleF (165, Frame.Height - 234, 160, 17);

                FullNameTextField = new NSTextField () {
                    Frame       = new RectangleF (330, Frame.Height - 238, 196, 22),
                    StringValue = UnixUserInfo.GetRealUser ().RealName,
                    Delegate    = new SparkleTextFieldDelegate ()
                };

                EmailLabel       = new SparkleLabel ("Email:", NSTextAlignment.Right);
                EmailLabel.Frame = new RectangleF (165, Frame.Height - 264, 160, 17);
                    
                EmailTextField = new NSTextField () {
                    Frame       = new RectangleF (330, Frame.Height - 268, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate ()
                };

                CancelButton = new NSButton () { Title = "Cancel" };

                ContinueButton = new NSButton () {
                    Title    = "Continue",
                    Enabled  = false
                };


                (FullNameTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    Controller.CheckSetupPage (FullNameTextField.StringValue, EmailTextField.StringValue);
                };

                (EmailTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    Controller.CheckSetupPage (FullNameTextField.StringValue, EmailTextField.StringValue);
                };

                ContinueButton.Activated += delegate {
                    string full_name = FullNameTextField.StringValue.Trim ();
                    string email     = EmailTextField.StringValue.Trim ();

                    Controller.SetupPageCompleted (full_name, email);
                };

                CancelButton.Activated += delegate { Controller.SetupPageCancelled (); };

                Controller.UpdateSetupContinueButtonEvent += delegate (bool button_enabled) {
                    SparkleShare.Controller.Invoke (() => {
                        ContinueButton.Enabled = button_enabled;
                    });
                };


                ContentView.AddSubview (FullNameLabel);
                ContentView.AddSubview (FullNameTextField);
                ContentView.AddSubview (EmailLabel);
                ContentView.AddSubview (EmailTextField);

                Buttons.Add (ContinueButton);
                Buttons.Add (CancelButton);

                Controller.CheckSetupPage (FullNameTextField.StringValue, EmailTextField.StringValue);

                if (FullNameTextField.StringValue.Equals (""))
                    MakeFirstResponder ((NSResponder) FullNameTextField);
                else
                    MakeFirstResponder ((NSResponder) EmailTextField);
            }

            if (type == PageType.Invite) {
                Header      = "You’ve received an invite!";
                Description = "Do you want to add this project to SparkleShare?";

                AddressLabel       = new SparkleLabel ("Address:", NSTextAlignment.Right);
                AddressLabel.Frame = new RectangleF (165, Frame.Height - 238, 160, 17);
                AddressLabel.Font  = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize);
     
                AddressTextField = new SparkleLabel (Controller.PendingInvite.Address, NSTextAlignment.Left) {
                    Frame = new RectangleF (330, Frame.Height - 240, 260, 17)
                };

                PathLabel       = new SparkleLabel ("Remote Path:", NSTextAlignment.Right);
                PathLabel.Frame = new RectangleF (165, Frame.Height - 262, 160, 17);
                PathLabel.Font  = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize);


                PathTextField = new SparkleLabel (Controller.PendingInvite.RemotePath, NSTextAlignment.Left) {
                    Frame = new RectangleF (330, Frame.Height - 264, 260, 17)
                };

                CancelButton = new NSButton () { Title = "Cancel" };
                AddButton = new NSButton () { Title = "Add" };


                CancelButton.Activated += delegate { Controller.PageCancelled (); };
                AddButton.Activated += delegate { Controller.InvitePageCompleted (); };


                ContentView.AddSubview (AddressLabel);
                ContentView.AddSubview (PathLabel);
                ContentView.AddSubview (AddressTextField);
                ContentView.AddSubview (PathTextField);

                Buttons.Add (AddButton);
                Buttons.Add (CancelButton);
            }

            if (type == PageType.Add) {
                Header      = "Where’s your project hosted?";
                Description = "";

                AddressLabel = new SparkleLabel ("Address:", NSTextAlignment.Left) {
                    Frame = new RectangleF (190, Frame.Height - 308, 160, 17),
                    Font  = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize)
                };

                AddressTextField = new NSTextField () {
                    Frame       = new RectangleF (190, Frame.Height - 336, 196, 22),
                    Enabled     = (Controller.SelectedPreset.Address == null),
                    Delegate    = new SparkleTextFieldDelegate (),
                    StringValue = "" + Controller.PreviousAddress
                };

                AddressTextField.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;

                PathLabel = new SparkleLabel ("Remote Path:", NSTextAlignment.Left) {
                    Frame = new RectangleF (190 + 196 + 16, Frame.Height - 308, 160, 17),
                    Font  = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize)
                };

                PathTextField = new NSTextField () {
                    Frame       = new RectangleF (190 + 196 + 16, Frame.Height - 336, 196, 22),
                    Enabled     = (Controller.SelectedPreset.Path == null),
                    Delegate    = new SparkleTextFieldDelegate (),
                    StringValue = "" + Controller.PreviousPath
                };

                PathTextField.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;

                PathHelpLabel = new SparkleLabel (Controller.SelectedPreset.PathExample, NSTextAlignment.Left) {
                    TextColor = NSColor.DisabledControlText,
                    Frame     = new RectangleF (190 + 196 + 16, Frame.Height - 358, 204, 19)
                };

                AddressHelpLabel = new SparkleLabel (Controller.SelectedPreset.AddressExample, NSTextAlignment.Left) {
                    TextColor = NSColor.DisabledControlText,
                    Frame     = new RectangleF (190, Frame.Height - 358, 204, 19)
                };

                if (TableView == null || TableView.RowCount != Controller.Presets.Count) {
                    TableView = new NSTableView () {
                        Frame            = new RectangleF (0, 0, 0, 0),
                        RowHeight        = 38,
                        IntercellSpacing = new SizeF (8, 12),
                        HeaderView       = null,
                        Delegate         = new SparkleTableViewDelegate ()
                    };

                    ScrollView = new NSScrollView () {
                        Frame               = new RectangleF (190, Frame.Height - 280, 408, 185),
                        DocumentView        = TableView,
                        HasVerticalScroller = true,
                        BorderType          = NSBorderType.BezelBorder
                    };

                    IconColumn = new NSTableColumn () {
                        Width         = 36,
                        HeaderToolTip = "Icon",
                        DataCell      = new NSImageCell () { ImageAlignment = NSImageAlignment.Right }
                    };

                    DescriptionColumn = new NSTableColumn () {
                        Width         = 350,
                        HeaderToolTip = "Description",
                        Editable      = false
                    };

                    DescriptionColumn.DataCell.Font = NSFontManager.SharedFontManager.FontWithFamily (
                        UserInterface.FontName, NSFontTraitMask.Condensed, 0, 11);

                    TableView.AddColumn (IconColumn);
                    TableView.AddColumn (DescriptionColumn);

                    // Hi-res display support was added after Snow Leopard
                    if (Environment.OSVersion.Version.Major < 11)
                        DataSource = new SparkleDataSource (1, Controller.Presets);
                    else
                        DataSource = new SparkleDataSource (BackingScaleFactor, Controller.Presets);

                    TableView.DataSource = DataSource;
                    TableView.ReloadData ();
                    
                    (TableView.Delegate as SparkleTableViewDelegate).SelectionChanged += delegate {
                        Controller.SelectedPresetChanged (TableView.SelectedRow);
                        Controller.CheckAddPage (AddressTextField.StringValue, PathTextField.StringValue, TableView.SelectedRow);
                    };
                }
                
                TableView.SelectRow (Controller.SelectedPresetIndex, false);
                TableView.ScrollRowToVisible (Controller.SelectedPresetIndex);
                MakeFirstResponder ((NSResponder) TableView);

                HistoryCheckButton = new NSButton () {
                    Frame = new RectangleF (190, Frame.Height - 400, 300, 18),
                    Title = "Fetch prior revisions"
                };

                if (Controller.FetchPriorHistory)
                    HistoryCheckButton.State = NSCellStateValue.On;

                HistoryCheckButton.SetButtonType (NSButtonType.Switch);

                AddButton = new NSButton () {
                    Title = "Add",
                    Enabled = false
                };

                CancelButton = new NSButton () { Title = "Cancel" };


                Controller.ChangeAddressFieldEvent += delegate (string text, string example_text, FieldState state) {
                    SparkleShare.Controller.Invoke (() => {
                        AddressTextField.StringValue = text;
                        AddressTextField.Enabled     = (state == FieldState.Enabled);
                        AddressHelpLabel.StringValue = example_text;
                    });
                };

                Controller.ChangePathFieldEvent += delegate (string text, string example_text, FieldState state) {
                    SparkleShare.Controller.Invoke (() => {
                        PathTextField.StringValue = text;
                        PathTextField.Enabled     = (state == FieldState.Enabled);
                        PathHelpLabel.StringValue = example_text;
                    });
                };


                (AddressTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    Controller.CheckAddPage (AddressTextField.StringValue, PathTextField.StringValue, TableView.SelectedRow);
                };

                 (PathTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    Controller.CheckAddPage (AddressTextField.StringValue, PathTextField.StringValue, TableView.SelectedRow);
                };


                HistoryCheckButton.Activated += delegate {
                    Controller.HistoryItemChanged (HistoryCheckButton.State == NSCellStateValue.On);
                };

                AddButton.Activated += delegate {
                    Controller.AddPageCompleted (AddressTextField.StringValue, PathTextField.StringValue);
                };

                CancelButton.Activated += delegate { Controller.PageCancelled (); };

                Controller.UpdateAddProjectButtonEvent += delegate (bool button_enabled) {
                    SparkleShare.Controller.Invoke (() => {
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
                ContentView.AddSubview (HistoryCheckButton);

                Buttons.Add (AddButton);
                Buttons.Add (CancelButton);

                Controller.CheckAddPage (AddressTextField.StringValue, PathTextField.StringValue, TableView.SelectedRow);
            }

            if (type == PageType.Syncing) {
                Header      = "Adding project ‘" + Controller.SyncingFolder + "’…";
                Description = "This may take a while for large projects.\nIsn’t it coffee-o’clock?";

                ProgressIndicator = new NSProgressIndicator () {
                    Frame         = new RectangleF (190, Frame.Height - 200, 640 - 150 - 80, 20),
                    Style         = NSProgressIndicatorStyle.Bar,
                    MinValue      = 0.0,
                    MaxValue      = 100.0,
                    Indeterminate = false,
                    DoubleValue   = Controller.ProgressBarPercentage
                };

                ProgressIndicator.StartAnimation (this);

                CancelButton = new NSButton () { Title = "Cancel" };

                FinishButton = new NSButton () {
                    Title = "Finish",
                    Enabled = false
                };

                ProgressLabel       = new SparkleLabel ("Preparing to fetch files…", NSTextAlignment.Right);
                ProgressLabel.Frame = new RectangleF (Frame.Width - 40 - 250, 185, 250, 25);


                Controller.UpdateProgressBarEvent += delegate (double percentage, string speed) {
                    SparkleShare.Controller.Invoke (() => {
                        ProgressIndicator.DoubleValue = percentage;
                        ProgressLabel.StringValue     = speed;
                    });
                };


                CancelButton.Activated += delegate { Controller.SyncingCancelled (); };


                ContentView.AddSubview (ProgressLabel);
                ContentView.AddSubview (ProgressIndicator);

                Buttons.Add (FinishButton);
                Buttons.Add (CancelButton);
            }

            if (type == PageType.Error) {
                Header      = "Oops! Something went wrong…";
                Description = "Please check the following:";

                // Displaying marked up text with Cocoa is
                // a pain, so we just use a webview instead
                WebView web_view = new WebView ();
                web_view.Frame   = new RectangleF (190, Frame.Height - 525, 375, 400);

                string html = "<style>" +
                    "* {" +
                    "  font-family: '" + UserInterface.FontName + "';" +
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
                    "  word-wrap: break-word;" +
                    "}" +
                    "ul {" +
                    "  padding-left: 24px;" +
                    "}" +
                    "</style>" +
                    "<ul>" +
                    "  <li><b>" + Controller.PreviousUrl + "</b> is the address we’ve compiled. Does this look alright?</li>" +
                    "  <li>Is this computer’s Client ID known by the host?</li>" +
                    "</ul>";

                if (warnings.Length > 0) {
                    string warnings_markup = "";

                    foreach (string warning in warnings)
                        warnings_markup += "<br><b>" + warning + "</b>";

                    html = html.Replace ("</ul>", "<li>Here’s the raw error message: " + warnings_markup + "</li></ul>");
                }

                web_view.MainFrame.LoadHtmlString (html, new NSUrl (""));
                web_view.DrawsBackground = false;

                CancelButton = new NSButton () { Title = "Cancel" };
                TryAgainButton = new NSButton () { Title = "Retry" };


                CancelButton.Activated += delegate { Controller.PageCancelled (); };
                TryAgainButton.Activated += delegate { Controller.ErrorPageCompleted (); };


                ContentView.AddSubview (web_view);

                Buttons.Add (TryAgainButton);
                Buttons.Add (CancelButton);
            }

            if (type == PageType.StorageSetup) {
                Header = string.Format ("Storage type for ‘{0}’", Controller.SyncingFolder);
                Description = "What type of storage would you like to use?";


                storage_type_descriptions = new List<NSTextField> ();

                ButtonCellProto = new NSButtonCell ();
                ButtonCellProto.SetButtonType (NSButtonType.Radio);
                ButtonCellProto.Font = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize);

                Matrix = new NSMatrix (new RectangleF (202, Frame.Height - 256 - 128, 256, 256), NSMatrixMode.Radio,
                    ButtonCellProto, SparkleShare.Controller.FetcherAvailableStorageTypes.Count, 1);

                Matrix.CellSize = new SizeF (256, 36);
                Matrix.IntercellSpacing = new SizeF (32, 32);

                int i = 0;
                foreach (StorageTypeInfo storage_type in SparkleShare.Controller.FetcherAvailableStorageTypes) {
                    Matrix.Cells [i].Title = " " + storage_type.Name;

                    NSTextField storage_type_description = new SparkleLabel (storage_type.Description, NSTextAlignment.Left) {
                        TextColor = NSColor.DisabledControlText,
                        Frame = new RectangleF (223, Frame.Height - 190 - (68 * i), 256, 32)
                    };

                    storage_type_descriptions.Add (storage_type_description);
                    ContentView.AddSubview (storage_type_description);

                    i++;
                }

                ContentView.AddSubview (Matrix);


                CancelButton = new NSButton () { Title = "Cancel" };
                ContinueButton = new NSButton () { Title = "Continue" };

                ContinueButton.Activated += delegate {
                    StorageTypeInfo selected_storage_type = SparkleShare.Controller.FetcherAvailableStorageTypes [Matrix.SelectedRow];
                    Controller.StoragePageCompleted (selected_storage_type.Type);
                };

                CancelButton.Activated += delegate { Controller.SyncingCancelled (); };

                Buttons.Add (ContinueButton);
                Buttons.Add (CancelButton);


                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }

            if (type == PageType.CryptoSetup || type == PageType.CryptoPassword) {
                if (type == PageType.CryptoSetup) {
                    Header      = "Set up file encryption";
                    Description = "Please a provide a strong password that you don’t use elsewhere.";
                
                } else {
                    Header      = "This project contains encrypted files";
                    Description = "Please enter the password to see their contents.";
                }

                int extra_pos_y = 0;

                if (type == PageType.CryptoPassword)
                    extra_pos_y = 20;
  
                PasswordLabel = new SparkleLabel ("Password:", NSTextAlignment.Right) {
                    Frame           = new RectangleF (155, Frame.Height - 202 - extra_pos_y, 160, 17),
                    Font            = NSFont.FromFontName (UserInterface.FontName + " Bold", NSFont.SystemFontSize)
                };

                PasswordTextField = new NSSecureTextField () {
                    Frame       = new RectangleF (320, Frame.Height - 208 - extra_pos_y, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate ()
                };

                VisiblePasswordTextField = new NSTextField () {
                    Frame       = new RectangleF (320, Frame.Height - 208 - extra_pos_y, 196, 22),
                    Delegate    = new SparkleTextFieldDelegate ()
                };

                ShowPasswordCheckButton = new NSButton () {
                    Frame = new RectangleF (318, Frame.Height - 235 - extra_pos_y, 300, 18),
                    Title = "Show password",
                    State = NSCellStateValue.Off
                };

                ShowPasswordCheckButton.SetButtonType (NSButtonType.Switch);

                WarningImage = NSImage.ImageNamed ("NSInfo");
                WarningImage.Size = new SizeF (24, 24);

                WarningImageView = new NSImageView () {
                    Image = WarningImage,
                    Frame = new RectangleF (200, Frame.Height - 320, 24, 24)
                };

                WarningTextField = new SparkleLabel ("This password can’t be changed later, and your files can’t be recovered if it’s forgotten.", NSTextAlignment.Left) {
                    Frame = new RectangleF (235, Frame.Height - 390, 325, 100),
                };

                CancelButton = new NSButton () { Title = "Cancel" };

                ContinueButton = new NSButton () {
                    Title    = "Continue",
                    Enabled  = false
                };


                Controller.UpdateCryptoPasswordContinueButtonEvent += delegate (bool button_enabled) {
                    SparkleShare.Controller.Invoke (() => { ContinueButton.Enabled = button_enabled; });
                };

                Controller.UpdateCryptoSetupContinueButtonEvent += delegate (bool button_enabled) {
                    SparkleShare.Controller.Invoke (() => { ContinueButton.Enabled = button_enabled; });
                };
                
                ShowPasswordCheckButton.Activated += delegate {
                    if (PasswordTextField.Superview == ContentView) {
                        PasswordTextField.RemoveFromSuperview ();
                        ContentView.AddSubview (VisiblePasswordTextField);

                    } else {
                        VisiblePasswordTextField.RemoveFromSuperview ();
                        ContentView.AddSubview (PasswordTextField);
                    }
                };

                (PasswordTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    VisiblePasswordTextField.StringValue = PasswordTextField.StringValue;

                    if (type == PageType.CryptoSetup)
                        Controller.CheckCryptoSetupPage (PasswordTextField.StringValue);
                    else
                        Controller.CheckCryptoPasswordPage (PasswordTextField.StringValue);
                };

                (VisiblePasswordTextField.Delegate as SparkleTextFieldDelegate).StringValueChanged += delegate {
                    PasswordTextField.StringValue = VisiblePasswordTextField.StringValue;
                    
                    if (type == PageType.CryptoSetup)
                        Controller.CheckCryptoSetupPage (PasswordTextField.StringValue);
                    else
                        Controller.CheckCryptoPasswordPage (PasswordTextField.StringValue);
                };

                ContinueButton.Activated += delegate {
                    if (type == PageType.CryptoSetup)
                        Controller.CryptoSetupPageCompleted (PasswordTextField.StringValue);
                    else
                        Controller.CryptoPasswordPageCompleted (PasswordTextField.StringValue);
                };

                CancelButton.Activated += delegate { Controller.CryptoPageCancelled (); };


                ContentView.AddSubview (PasswordLabel);
                ContentView.AddSubview (PasswordTextField);
                ContentView.AddSubview (ShowPasswordCheckButton);

                if (type == PageType.CryptoSetup) {
                    ContentView.AddSubview (WarningImageView);
                    ContentView.AddSubview (WarningTextField);
                }

                Buttons.Add (ContinueButton);
                Buttons.Add (CancelButton);

                MakeFirstResponder ((NSResponder) PasswordTextField);
                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }


            if (type == PageType.Finished) {
                Header      = "Your shared project is ready!";
                Description = "You can find the files in your SparkleShare folder.";

                if (warnings.Length > 0) {
                    WarningImage = NSImage.ImageNamed ("NSInfo");
                    WarningImage.Size = new SizeF (24, 24);

                    WarningImageView = new NSImageView () {
                        Image = WarningImage,
                        Frame = new RectangleF (200, Frame.Height - 175, 24, 24)
                    };

                    WarningTextField = new SparkleLabel (warnings [0], NSTextAlignment.Left);
                    WarningTextField.Frame       = new RectangleF (235, Frame.Height - 245, 325, 100);

                    ContentView.AddSubview (WarningImageView);
                    ContentView.AddSubview (WarningTextField);
                }

                ShowFilesButton = new NSButton () { Title = "Show Files" };
                FinishButton    = new NSButton () { Title = "Finish" };


                ShowFilesButton.Activated += delegate { Controller.ShowFilesClicked (); };
                FinishButton.Activated += delegate { Controller.FinishPageCompleted (); };


                Buttons.Add (FinishButton);
                Buttons.Add (ShowFilesButton);

                NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
            }
        }
    }


    [Register("SparkleDataSource")]
    public class SparkleDataSource : NSTableViewDataSource {

        public List<object> Items;
        public NSAttributedString [] Cells, SelectedCells;

        int backing_scale_factor;

        public SparkleDataSource (float backing_scale_factor, List<Preset> presets)
        {
            Items         = new List <object> ();
            Cells         = new NSAttributedString [presets.Count];
            SelectedCells = new NSAttributedString [presets.Count];

            this.backing_scale_factor = (int) backing_scale_factor;

            int i = 0;
            foreach (Preset preset in presets) {
                Items.Add (preset);

                NSTextFieldCell cell = new NSTextFieldCell ();

                NSData name_data = NSData.FromString ("<font face='" + UserInterface.FontName + "'><b>" + preset.Name + "</b></font>");

                NSDictionary name_dictionary       = new NSDictionary();
                NSAttributedString name_attributes = new NSAttributedString (
                    name_data, new NSUrl ("file://"), out name_dictionary);

                NSData description_data = NSData.FromString (
                    "<small><font style='line-height: 150%' color='#aaa' face='" + UserInterface.FontName + "'>" + preset.Description + "</font></small>");

                NSDictionary description_dictionary       = new NSDictionary();
                NSAttributedString description_attributes = new NSAttributedString (
                    description_data, new NSUrl ("file://"), out description_dictionary);

                NSMutableAttributedString mutable_attributes = new NSMutableAttributedString (name_attributes);
                mutable_attributes.Append (new NSAttributedString ("\n"));
                mutable_attributes.Append (description_attributes);

                cell.AttributedStringValue = mutable_attributes;
                Cells [i] = (NSAttributedString) cell.ObjectValue;

                NSTextFieldCell selected_cell = new NSTextFieldCell ();

                NSData selected_name_data = NSData.FromString (
                    "<font color='white' face='" + UserInterface.FontName +"'><b>" + preset.Name + "</b></font>");

                NSDictionary selected_name_dictionary = new NSDictionary ();
                NSAttributedString selected_name_attributes = new NSAttributedString (
                    selected_name_data, new NSUrl ("file://"), out selected_name_dictionary);

                NSData selected_description_data = NSData.FromString (
                    "<small><font style='line-height: 150%' color='#9bbaeb' face='" + UserInterface.FontName + "'>" +
                    preset.Description + "</font></small>");

                NSDictionary selected_description_dictionary       = new NSDictionary ();
                NSAttributedString selected_description_attributes = new NSAttributedString (
                    selected_description_data, new NSUrl ("file://"), out selected_description_dictionary);

                NSMutableAttributedString selected_mutable_attributes =
                    new NSMutableAttributedString (selected_name_attributes);

                selected_mutable_attributes.Append (new NSAttributedString ("\n"));
                selected_mutable_attributes.Append (selected_description_attributes);

                selected_cell.AttributedStringValue = selected_mutable_attributes;
                SelectedCells [i] = (NSAttributedString) selected_cell.ObjectValue;

                i++;
            }
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
            if (table_column.HeaderToolTip.Equals ("Description")) {
                if (table_view.SelectedRow == row_index &&
                    SparkleShare.UI.Setup.IsKeyWindow &&
                    SparkleShare.UI.Setup.FirstResponder == table_view) {

                    return SelectedCells [row_index];
                }

                return Cells [row_index];
            }

            string image_path = (Items [row_index] as Preset).ImagePath;

            if (backing_scale_factor >= 2) {
                string hi_path = String.Format ("{0}@{1}x{2}",
                    Path.Combine (Path.GetDirectoryName (image_path), Path.GetFileNameWithoutExtension (image_path)),
                    backing_scale_factor, Path.GetExtension (image_path)
                );

                if (File.Exists (hi_path))
                    image_path = hi_path;
            }

            return new NSImage (image_path) { Size = new SizeF (24, 24) };
        }
    }


    public class SparkleTextFieldDelegate : NSTextFieldDelegate {
        
        public event Action StringValueChanged = delegate { };

        public override void Changed (NSNotification notification)
        {
                StringValueChanged ();
        }
    }


    public class SparkleTableViewDelegate : NSTableViewDelegate {

        public event Action SelectionChanged = delegate { };

        public override void SelectionDidChange (NSNotification notification)
        {
            SelectionChanged ();
        }
    }


    public class SparkleLabel : NSTextField {

        public SparkleLabel (string label, NSTextAlignment alignment)
        {
            if (!string.IsNullOrEmpty (label))
                StringValue = label;

            Alignment       = alignment;
            BackgroundColor = NSColor.WindowBackground;
            Bordered        = false;
            Editable        = false;
        }
    }
}
