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
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare {

	public partial class AppDelegate : NSApplicationDelegate {
		// Workaround to be able to work with SparkleUI as the main class
	}

	
	public class SparkleUI : AppDelegate
	{
	
		public static SparkleStatusIcon StatusIcon;
		public static List <SparkleLog> OpenLogs;
		public static int NewEvents;

		
		public SparkleUI ()
		{

			NSApplication.Init ();

			NSApplication.SharedApplication.ApplicationIconImage
				= NSImage.ImageNamed ("sparkleshare.icns");

			OpenLogs   = new List <SparkleLog> ();
			StatusIcon = new SparkleStatusIcon ();
			
			NewEvents = 0;
			
			SparkleShare.Controller.NotificationRaised += delegate {
				
				InvokeOnMainThread (delegate {
				
					NewEvents++;
					NSApplication.SharedApplication.DockTile.BadgeLabel = NewEvents.ToString ();
					
				});
				
			};

		}
	
		
		public void Run ()
		{
			
			NSApplication.Main (new string [0]);
			
		}
		
	}

}
