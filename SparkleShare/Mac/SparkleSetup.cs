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
		private NSButton OpenFolderButton;
		private NSButton FinishButton;
		private NSForm UserInfoForm;
		private NSProgressIndicator ProgressIndicator;
		private NSTextField AddressTextField;
		private NSTextField FolderNameTextField;
		private NSTextField ServerTypeLabel;
		private NSTextField AddressLabel;
		private NSTextField FolderNameLabel;
		private NSTextField FolderNameHelpLabel;
		private NSButtonCell ButtonCellProto;
		private NSMatrix Matrix;
		private int ServerType;
                private Timer timer;

		
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

                        UserInfoForm = new NSForm (new RectangleF (250, 115, 350, 64));

                        UserInfoForm.AddEntry ("Full Name:");
                        UserInfoForm.AddEntry ("Email Address:");

                        UserInfoForm.CellSize                = new SizeF (280, 22);
                        UserInfoForm.IntercellSpacing        = new SizeF (4, 4);
                        UserInfoForm.Cells [0].LineBreakMode = NSLineBreakMode.TruncatingTail;
                        UserInfoForm.Cells [1].LineBreakMode = NSLineBreakMode.TruncatingTail;

                        UserInfoForm.Cells [0].StringValue   = SparkleShare.Controller.UserName;
                        UserInfoForm.Cells [1].StringValue   = SparkleShare.Controller.UserEmail;

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

                            string full_name = UserInfoForm.Cells [0].StringValue.Trim ();
                            string email = UserInfoForm.Cells [1].StringValue.Trim ();

                            Controller.SetupPageCompleted (full_name, email);
                        };

                        timer.Elapsed += delegate {
                            InvokeOnMainThread (delegate {
                                bool name_is_valid = !UserInfoForm.Cells [0].StringValue.Trim ().Equals ("");

                                bool email_is_valid = SparkleShare.Controller.IsValidEmail (
                                    UserInfoForm.Cells [1].StringValue.Trim ());

                                ContinueButton.Enabled = (name_is_valid && email_is_valid);
                            });
                        };

                        timer.Start ();

                        ContentView.AddSubview (UserInfoForm);
                        Buttons.Add (ContinueButton);

                        break;
                    }

                    case PageType.Add: {

                        Header       = "Where is your remote folder?";
                        Description  = "";

                        ServerTypeLabel  = new NSTextField () {
                            Alignment       = NSTextAlignment.Right,
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            Editable        = false,
                            Frame           = new RectangleF (150, Frame.Height - 139 , 160, 17),
                            StringValue     = "Server Type:",
                            Font            = SparkleUI.Font
                        };

                        AddressLabel = new NSTextField () {
                            Alignment       = NSTextAlignment.Right,
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            Editable        = false,
                            Frame           = new RectangleF (150, Frame.Height - 237 , 160, 17),
                            StringValue     = "Address:",
                            Font            = SparkleUI.Font
                        };

                        FolderNameLabel = new NSTextField () {
                            Alignment       = NSTextAlignment.Right,
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            Editable        = false,
                            Frame           = new RectangleF (150, Frame.Height - 264 , 160, 17),
                            StringValue     = "Folder Name:",
                            Font            = SparkleUI.Font
                        };


                        AddressTextField = new NSTextField () {
                            Frame       = new RectangleF (320, Frame.Height - 240 , 256, 22),
                            Font        = SparkleUI.Font,
                            StringValue = Controller.PreviousServer
                        };

                        AddressTextField.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;

                        FolderNameTextField = new NSTextField () {
                            Frame           = new RectangleF (320, Frame.Height - (240 + 22 + 4) , 256, 22),
                            StringValue     = Controller.PreviousFolder
                        };

                        FolderNameTextField.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;

                        FolderNameHelpLabel = new NSTextField () {
                            BackgroundColor = NSColor.WindowBackground,
                            Bordered        = false,
                            TextColor       = NSColor.DisabledControlText,
                            Editable        = false,
                            Frame           = new RectangleF (320, Frame.Height - 285 , 200, 17),
                            StringValue     = "e.g. ‘rupert/website-design’"
                        };

                        ServerType = 0;

                        ButtonCellProto = new NSButtonCell ();
                        ButtonCellProto.SetButtonType (NSButtonType.Radio) ;

                        Matrix = new NSMatrix (new RectangleF (315, 180, 256, 78),
                            NSMatrixMode.Radio, ButtonCellProto, 4, 1);

                        Matrix.CellSize = new SizeF (256, 18);

                        Matrix.Cells [0].Title = "My own server";
                        Matrix.Cells [1].Title = "Github";
                        Matrix.Cells [2].Title = "Gitorious";
                        Matrix.Cells [3].Title = "The GNOME Project";

                        foreach (NSCell cell in Matrix.Cells)
                            cell.Font = SparkleUI.Font;

                        // TODO: Ugly hack, do properly with events
                        timer = new Timer () {
                            Interval = 50
                        };

                        timer.Elapsed += delegate {
                            InvokeOnMainThread (delegate {
                                if (Matrix.SelectedRow != ServerType) {
                                    ServerType = Matrix.SelectedRow;

                                    AddressTextField.Enabled = (ServerType == 0);

                                    switch (ServerType) {
                                    case 0:
                                        AddressTextField.StringValue = "";
                                        FolderNameHelpLabel.StringValue = "e.g. ‘rupert/website-design’";
                                        break;
                                    case 1:
                                        AddressTextField.StringValue = "ssh://git@github.com/";
                                        FolderNameHelpLabel.StringValue = "e.g. ‘rupert/website-design’";
                                        break;
                                    case 2:
                                        AddressTextField.StringValue = "ssh://git@gitorious.org/";
                                        FolderNameHelpLabel.StringValue = "e.g. ‘project/website-design’";
                                        break;
                                    case 3:
                                        AddressTextField.StringValue = "ssh://git@gnome.org/git/";
                                        FolderNameHelpLabel.StringValue = "e.g. ‘gnome-icon-theme’";
                                        break;
                                    }
                                }


                                if (ServerType == 0 && !AddressTextField.StringValue.Trim ().Equals ("")
                                    && !FolderNameTextField.StringValue.Trim ().Equals ("")) {

                                    SyncButton.Enabled = true;

                                } else if (ServerType != 0 &&
                                           !FolderNameTextField.StringValue.Trim ().Equals ("")) {

                                    SyncButton.Enabled = true;

                                } else {
                                    SyncButton.Enabled = false;
                                }
                            });

                        };

                        timer.Start ();

                        ContentView.AddSubview (ServerTypeLabel);
                        ContentView.AddSubview (Matrix);

                        ContentView.AddSubview (AddressLabel);
                        ContentView.AddSubview (AddressTextField);

                        ContentView.AddSubview (FolderNameLabel);
                        ContentView.AddSubview (FolderNameTextField);
                        ContentView.AddSubview (FolderNameHelpLabel);

                        SyncButton = new NSButton () {
                            Title = "Sync",
                            Enabled = false
                        };

                            SyncButton.Activated += delegate {
                                timer.Stop ();
                                timer = null;

                                string folder_name    = FolderNameTextField.StringValue;
                                string server         = AddressTextField.StringValue;
                                Controller.AddPageCompleted (server, folder_name);
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

                        Header      = "Syncing folder ‘" + Controller.SyncingFolder + "’…";
                        Description = "This may take a while.\n" +
                                      "Are you sure it’s not coffee o'clock?";

                        ProgressIndicator = new NSProgressIndicator () {
                            Frame = new RectangleF (190, Frame.Height - 200, 640 - 150 - 80, 20),
                            Style = NSProgressIndicatorStyle.Bar
                        };

                        ProgressIndicator.StartAnimation (this);
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
                        Description = "";

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

                        Header      = "Folder synced succesfully!";
                        Description = "Now you can access the synced files from " +
                                      "‘" + Controller.SyncingFolder + "’ in " +
                                      "your SparkleShare folder.";

                        FinishButton = new NSButton () {
                            Title = "Finish"
                        };

                        FinishButton.Activated += delegate {
                            InvokeOnMainThread (delegate {
                                PerformClose (this);
                            });
                        };

                        OpenFolderButton = new NSButton () {
                            Title = "Open Folder"
                        };

                        OpenFolderButton.Activated += delegate {
                            SparkleShare.Controller.OpenSparkleShareFolder (Controller.SyncingFolder);
                        };

                        Buttons.Add (FinishButton);
                        Buttons.Add (OpenFolderButton);

                        NSApplication.SharedApplication.RequestUserAttention
                            (NSRequestUserAttentionType.CriticalRequest);

                        break;
                    }
                    }

                    ShowAll ();
                });
            };
        }
	}
}
