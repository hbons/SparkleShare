//   SparkleShare, an instant update workflow to Git.
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
using System.Timers;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;
using Mono.Unix;

namespace SparkleShare {

	public class SparkleIntro : SparkleWindow {
		
		private NSButton NextButton;
		private NSButton SyncButton;
		private NSForm UserInfoForm;
		

		public SparkleIntro () : base ()
		{
			
			ShowUserForm ();
			
		}
		
		
		public void ShowUserForm ()
		{
		
			Reset ();

				Header       = "Welcome to SparkleShare!";
				Description  = "Before we can create a SparkleShare folder on this\n" +
				               "computer, we need a few bits of information from you.";
				

				UserInfoForm = new NSForm (new RectangleF (250, 190, 350, 64));
				UserInfoForm.AddEntry ("Full Name:");
				UserInfoForm.AddEntry ("Email Address:");
				UserInfoForm.CellSize = new SizeF (280, 22);
			
				string login_name = UnixUserInfo.GetLoginName ();
				string full_name  = new UnixUserInfo (login_name).RealName;
				UserInfoForm.Cells [0].StringValue = full_name;
			

				NextButton = new NSButton () {
					Title = "Next",
					Enabled = false
				};
			
				NextButton.Activated += delegate {
					
					SparkleShare.Controller.UserName  = UserInfoForm.Cells [0].StringValue.Trim ();
					SparkleShare.Controller.UserEmail = UserInfoForm.Cells [1].StringValue.Trim ();
					
					SparkleShare.Controller.GenerateKeyPair ();

					ShowServerForm ();
				
				};
			

				// TODO: Ugly hack, do properly with events
				Timer timer = new Timer () {
					Interval = 500
				};

				timer.Elapsed += delegate {
				
					InvokeOnMainThread (delegate {
				
						bool name_is_correct =
							!UserInfoForm.Cells [0].StringValue.Trim ().Equals ("");
					
						bool email_is_correct =
							!UserInfoForm.Cells [1].StringValue.Trim ().Equals ("");
	
						NextButton.Enabled = (name_is_correct && email_is_correct);
				 
					});
				
				};
			
				timer.Start ();

			ContentView.AddSubview (UserInfoForm);
			Buttons.Add (NextButton);

			ShowAll ();

		}
		

		public void ShowServerForm ()
		{
			
			Reset ();

				Header       = "Where is your remote folder?";
				Description  = "";


				SyncButton = new NSButton () {
					Title = "Sync",
					Enabled = false
				};
			

			Buttons.Add (SyncButton);

			ShowAll ();

		}

	}
		
}




			
		//	proto.SetButtonType (NSButtonType.Radio) ;
			
	//		NSButton button = new NSButton (new RectangleF (150, 0, 350, 300)) {
	//		Cell = proto,
	//			Font = NSFontManager.SharedFontManager.FontWithFamily ("Lucida Grande",
	//			                                                       NSFontTraitMask.Bold,
	//			                                                       0, 14)
	//		};
			
	//		NSMatrix matrix = new NSMatrix (new RectangleF (300, 00, 300, 300), NSMatrixMode.Radio, proto, 4, 1);
			

			
	//		matrix.Cells [0].Title = "My own server:";
	//		matrix.Cells [1].Title = "Github\nFree hosting";
	//		matrix.Cells [2].Title = "Gitorious";
	//		matrix.Cells [3].Title = "The GNOME Project";
			

