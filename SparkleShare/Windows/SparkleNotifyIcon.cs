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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace SparkleShare {

    [ContentProperty("Text")]
    [DefaultEvent("MouseDoubleClick")]
    public class SparkleNotifyIcon : FrameworkElement, IAddChild {

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string module_name);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx (int hook_id, int code, int param, IntPtr data_pointer);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SetWindowsHookEx (int hook_id, HookProc function, IntPtr instance, int thread_id);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon")]
        static extern bool DestroyIcon (IntPtr hIcon);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx (int hook_id);

        [StructLayout (LayoutKind.Sequential)]
        private struct MouseLLHook
        {
            internal int X;
            internal int Y;
            internal int MouseData;
            internal int Flags;
            internal int Time;
            internal int Info;
        }

        
        public Drawing.Bitmap Icon {
            set {
                this.notify_icon.Icon = GetIconFromBitmap (value);
            }
        }

        public string HeaderText {
            get {
                return header_text;
            }
            
            set {
                header_text = value;
            }
        }
        
        public string Text {
            get {
                return (string) GetValue (TextProperty);
            }
            
            set {
                string text = value;
                
                if (!string.IsNullOrEmpty (header_text))
                    text = header_text + "\n" + text;
                    
                SetValue (TextProperty, text);
            }
        }

        public event MouseButtonEventHandler MouseClick {
            add {
                AddHandler (MouseClickEvent, value);
            }

            remove {
                RemoveHandler (MouseClickEvent, value);
            }
        }

        public event MouseButtonEventHandler MouseDoubleClick {
            add {
                AddHandler (MouseDoubleClickEvent, value);
            }

            remove {
                RemoveHandler (MouseDoubleClickEvent, value);
            }
        }


        public readonly RoutedEvent MouseClickEvent = EventManager.RegisterRoutedEvent (
            "MouseClick", RoutingStrategy.Bubble, typeof (MouseButtonEventHandler), typeof (SparkleNotifyIcon));

        public readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
            "MouseDoubleClick",    RoutingStrategy.Bubble,    typeof (MouseButtonEventHandler), typeof (SparkleNotifyIcon));

        public readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",    typeof(string), typeof (SparkleNotifyIcon), new PropertyMetadata (OnTextChanged));

        
        private string header_text;
        private Forms.NotifyIcon notify_icon;
        private HookProc hook_proc_ref;
        private int mouse_hook_handle;
        private delegate int HookProc (int code, int param, IntPtr struct_pointer);
        
        
        public SparkleNotifyIcon ()
        {
            VisibilityProperty.OverrideMetadata (typeof (SparkleNotifyIcon),
                new PropertyMetadata (OnVisibilityChanged));
            
            this.notify_icon = new Forms.NotifyIcon () {
                Text    = Text,
                Visible = true
            };

            this.notify_icon.MouseDown        += OnMouseDown;
            this.notify_icon.MouseUp          += OnMouseUp;
            this.notify_icon.MouseClick       += OnMouseClick;
            this.notify_icon.MouseDoubleClick += OnMouseDoubleClick;

            this.hook_proc_ref = OnMouseEventProc;
        }
        
        
        public void ShowBalloonTip (string title, string subtext, string image_path)
        {
            // TODO:
            // - Use the image pointed to by image_path
            // - Find a way to use the prettier (Win7?) balloons
            this.notify_icon.ShowBalloonTip (5 * 1000, title, subtext, Forms.ToolTipIcon.Info);    
        }
        

        public void Dispose()
        {
            this.notify_icon.Dispose();
        }


        void IAddChild.AddChild (object value)
        {
            throw new InvalidOperationException ();
        }

        
        void IAddChild.AddText (string text)
        {
            if (text == null)
                throw new ArgumentNullException ();

            Text = text;
        }

    
        private static MouseButtonEventArgs CreateMouseButtonEventArgs(
            RoutedEvent handler, Forms.MouseButtons button)
        {
            MouseButton mouse_button;

            if (button == Forms.MouseButtons.Left) {
                mouse_button = MouseButton.Left;
    
            } else if (button == Forms.MouseButtons.Right) {
                mouse_button = MouseButton.Right;
                        
            } else if (button == Forms.MouseButtons.Middle) {
                mouse_button = MouseButton.Middle;
                        
            } else if (button == Forms.MouseButtons.XButton1) {
                mouse_button = MouseButton.XButton1;
                            
            } else if (button == Forms.MouseButtons.XButton2) {
                mouse_button = MouseButton.XButton2;
                            
            } else {
                throw new InvalidOperationException ();
            }

            return new MouseButtonEventArgs (InputManager.Current.PrimaryMouseDevice, 0, mouse_button) {
                RoutedEvent = handler
            };
        }


        private void ShowContextMenu ()
        {
            if (ContextMenu != null) {
                ContextMenu.Opened += OnContextMenuOpened;
                ContextMenu.Closed += OnContextMenuClosed;
                
                ContextMenu.Placement = PlacementMode.Mouse;
                ContextMenu.IsOpen    = true;
            }
        }
        

        private Rect GetContextMenuRect (ContextMenu menu)
        {
            var source = PresentationSource.FromVisual (menu);
            if (source != null) {
                Point start_point = menu.PointToScreen(new Point(0, 0));
                Point end_point = menu.PointToScreen(new Point(menu.ActualWidth, menu.ActualHeight));

                return new Rect(start_point, end_point);
            }

            return new Rect();
        }


        private Point GetHitPoint (IntPtr struct_pointer)
        {
            MouseLLHook mouse_hook = (MouseLLHook) Marshal.PtrToStructure (
                struct_pointer, typeof (MouseLLHook));
            
            return new Point (mouse_hook.X, mouse_hook.Y);
        }        
        
        
        private int OnMouseEventProc (int code, int button, IntPtr data_pointer)
        {
            int left_button_down  = 0x201;
            int right_button_down = 0x204;

            int ret;
            try {
                if (button == left_button_down || button == right_button_down)
                {
                    Rect context_menu_rect = GetContextMenuRect (ContextMenu);
                    Point hit_point = GetHitPoint (data_pointer);

					if (!context_menu_rect.Contains(hit_point)) {
						new Thread (() => {
							Thread.Sleep (750);

							Dispatcher.BeginInvoke ((Action) delegate {
								ContextMenu.IsOpen = false;
							});
						}).Start ();
					}
                }
            } 
            finally {
                ret = CallNextHookEx(this.mouse_hook_handle, code, button, data_pointer);
            }

            return ret;
        }
        
        
        private void OnContextMenuOpened (object sender, RoutedEventArgs args)
        {
            using (Process process      = Process.GetCurrentProcess ())
            using (ProcessModule module = process.MainModule)
            {
                this.mouse_hook_handle = SetWindowsHookEx (14, this.hook_proc_ref, 
                    GetModuleHandle (module.ModuleName), 0);
            }

            if (this.mouse_hook_handle == 0)            
                throw new Win32Exception (Marshal.GetLastWin32Error ());
        }


        private void OnContextMenuClosed (object sender, RoutedEventArgs args)
        {
            UnhookWindowsHookEx (this.mouse_hook_handle);

            ContextMenu.Opened -= OnContextMenuOpened;
            ContextMenu.Closed -= OnContextMenuClosed;
        }
        
        
        private void OnVisibilityChanged (DependencyObject target,
            DependencyPropertyChangedEventArgs args)
        {
            SparkleNotifyIcon control   = (SparkleNotifyIcon) target;
            control.notify_icon.Visible = (control.Visibility == Visibility.Visible);
        }
        
        
        private void OnMouseDown(object sender, Forms.MouseEventArgs args)
        {
            RaiseEvent (CreateMouseButtonEventArgs (MouseDownEvent, args.Button));
        }
        
        
        private void OnMouseClick (object sender, Forms.MouseEventArgs args)
        {
            RaiseEvent (CreateMouseButtonEventArgs (MouseClickEvent, args.Button));
        }
        
        
        private void OnMouseDoubleClick (object sender, Forms.MouseEventArgs args)
        {
            RaiseEvent (CreateMouseButtonEventArgs (MouseDoubleClickEvent, args.Button));
        }


        private void OnMouseUp (object sender, Forms.MouseEventArgs args)
        {
            if (args.Button == Forms.MouseButtons.Left ||
                args.Button == Forms.MouseButtons.Right) {
                
                ShowContextMenu ();
            }

            RaiseEvent (CreateMouseButtonEventArgs (MouseUpEvent, args.Button));
        }


        protected override void OnVisualParentChanged (DependencyObject parent)
        {
            base.OnVisualParentChanged (parent);
        }


        private static void OnTextChanged (DependencyObject target,
            DependencyPropertyChangedEventArgs args)
        {
            SparkleNotifyIcon control = (SparkleNotifyIcon) target;
            control.notify_icon.Text  = control.Text;
        }
        

        private Drawing.Icon GetIconFromBitmap (Drawing.Bitmap bitmap)
        {
            IntPtr unmanaged_icon = bitmap.GetHicon ();
            Drawing.Icon icon = (Drawing.Icon) Drawing.Icon.FromHandle (unmanaged_icon).Clone ();
            DestroyIcon (unmanaged_icon);
            
            return icon;
        }
    }
}
