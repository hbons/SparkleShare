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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Drawing = System.Drawing;

namespace SparkleShare {

    public static class SparkleUIHelpers {
        
        public static string ToHex (this Drawing.Color color)
        {
            return string.Format ("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        
        public static BitmapFrame GetImageSource (string name)
        {                                          
            Assembly assembly   = Assembly.GetExecutingAssembly ();
            Stream image_stream = assembly.GetManifestResourceStream ("SparkleShare.Pixmaps." + name + ".png");
            return BitmapFrame.Create (image_stream);
        }

        
        public static Drawing.Bitmap GetBitmap (string name)
        {                                          
            Assembly assembly   = Assembly.GetExecutingAssembly ();
            Stream image_stream = assembly.GetManifestResourceStream ("SparkleShare.Pixmaps." + name + ".png");
            return (Drawing.Bitmap) Drawing.Bitmap.FromStream (image_stream);
        }
		
		
		public static string GetHTML (string name)
        {                                          
            Assembly assembly        = Assembly.GetExecutingAssembly ();
            StreamReader html_reader = new StreamReader (
				assembly.GetManifestResourceStream ("SparkleShare.HTML." + name));
            
			return html_reader.ReadToEnd ();
        }
		
		
		public static ImageSource ToImageSource(FrameworkElement obj)
        {
            // Save current canvas transform
            Transform transform = obj.LayoutTransform;
            obj.LayoutTransform = null;
            
            // fix margin offset as well
            Thickness margin = obj.Margin;
            obj.Margin = new Thickness(0, 0,
                 margin.Right - margin.Left, margin.Bottom - margin.Top);

            // Get the size of canvas
            Size size = new Size(obj.Width, obj.Height);
            
            // force control to Update
            obj.Measure(size);
            obj.Arrange(new Rect(size));

            RenderTargetBitmap bmp = new RenderTargetBitmap(
                (int)obj.Width, (int)obj.Height, 96, 96, PixelFormats.Pbgra32);
            
            bmp.Render(obj);
			bmp.Freeze ();

            // return values as they were before
            obj.LayoutTransform = transform;
            obj.Margin = margin;
            return bmp;
        }
		
    }
}
