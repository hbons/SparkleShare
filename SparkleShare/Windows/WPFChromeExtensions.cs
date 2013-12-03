using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using System.Windows;

namespace SparkleShare
{
    /// <summary>
    /// Thanks to Matt Hamilton for this code!
    /// See http://stackoverflow.com/questions/339620/how-do-i-remove-minimize-and-maximize-from-a-resizable-window-in-wpf
    /// </summary>
    internal static class WindowExtensions {
        // from winuser.h
        private const int GWL_STYLE = -16,
                          WS_MAXIMIZEBOX = 0x10000,
                          WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll")]
        extern private static int GetWindowLong (IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        extern private static int SetWindowLong (IntPtr hwnd, int index, int value);

        internal static void HideMinimizeAndMaximizeButtons (this Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentStyle = GetWindowLong (hwnd, GWL_STYLE);

            SetWindowLong (hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }
    }
}
