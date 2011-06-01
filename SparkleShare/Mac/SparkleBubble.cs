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
using System.IO;

using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.Growl;

namespace SparkleShare {
    
    public class SparkleBubble : NSObject {

        public string ImagePath;

        private string title;
        private string subtext;


        public SparkleBubble (string title, string subtext)
        {
            this.title   = title;
            this.subtext = subtext;
        }


        public void Show ()
        {
            InvokeOnMainThread (delegate {
                if (ImagePath != null && File.Exists (ImagePath)) {
                    NSData image_data = NSData.FromFile (ImagePath);
    
                    GrowlApplicationBridge.Notify (this.title, this.subtext,
                        "Start", image_data, 0, false, null);
    
                } else {
                    GrowlApplicationBridge.Notify (this.title, this.subtext,
                        "Start", null, 0, false, null);
                }
            });
        }
    }
}
