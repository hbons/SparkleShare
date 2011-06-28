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
    
    public class SparkleBadger {

        private Dictionary<string, NSImage> icons = new Dictionary<string, NSImage> ();
        private int [] sizes = new int [] {16, 32, 48, 128, 256, 512};
        private string [] paths;


        public SparkleBadger (string [] paths)
        {
            Paths = paths;
        }


        public void Badge ()
        {
            using (NSAutoreleasePool a = new NSAutoreleasePool ()) {
                foreach (string path in this.paths) {
                    string extension = Path.GetExtension (path.ToLower ());
                    NSImage new_icon = new NSImage ();

                    if (!this.icons.ContainsKey (extension)) {
                        foreach (int size in this.sizes) {
                            NSImage file_icon = NSWorkspace.SharedWorkspace.IconForFileType (extension);
                            file_icon.Size = new SizeF (size, size);

                            // TODO: replace this with the sync icon
                            NSImage overlay_icon = NSWorkspace.SharedWorkspace.IconForFileType ("sln");
                            overlay_icon.Size = new SizeF (size / 2, size / 2);

                            file_icon.LockFocus ();
                            NSGraphicsContext.CurrentContext.ImageInterpolation = NSImageInterpolation.High;
                            overlay_icon.Draw (
                                new RectangleF (0, 0, file_icon.Size.Width / 3, file_icon.Size.Width / 3),
                                new RectangleF (), NSCompositingOperation.SourceOver, 1.0f);
                            file_icon.UnlockFocus ();

                            new_icon.AddRepresentation (file_icon.Representations () [0]);
                        }


                        this.icons.Add (extension, new_icon);

                    } else {
                        new_icon = this.icons [extension];
                    }

                    NSWorkspace.SharedWorkspace.SetIconforFile (new_icon, path, 0);
                }
            }
        }


        public void Clear ()
        {
            foreach (string path in this.paths) {
                string extension = Path.GetExtension (path.ToLower ());

                NSImage original_icon = NSWorkspace.SharedWorkspace.IconForFileType (extension);
                NSWorkspace.SharedWorkspace.SetIconforFile (original_icon, path, 0);
            }
        }
    }
}
