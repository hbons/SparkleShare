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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace SparkleShare {

    [ContentProperty("Text")]
    [DefaultEvent("MouseDoubleClick")]
    public class SparkleNotifyIcon : UIElement, IAddChild {

        [DllImport("user32.dll", EntryPoint = "DestroyIcon")]
        static extern bool DestroyIcon(IntPtr h_icon);

        public Drawing.Bitmap Icon {
            set {
                NotifyIcon.Icon = GetIconFromBitmap(value);
            }
        }

        public string Text {
            get {
                return (string) GetValue(TextProperty);
            }
            set {
                var text = value;

                if(!string.IsNullOrEmpty(HeaderText))
                    text = HeaderText + "\n" + text;

                SetValue(TextProperty, text);
            }
        }

        public ContextMenu ContextMenu {
            get;
            set;
        }

        public string HeaderText {
            get;
            set;
        }

        private Forms.NotifyIcon NotifyIcon {
            get;
            set;
        }

        public readonly RoutedEvent MouseClickEvent = EventManager.RegisterRoutedEvent(
            "MouseClick", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(SparkleNotifyIcon));

        public readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
            "MouseDoubleClick", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(SparkleNotifyIcon));

        public readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(SparkleNotifyIcon), new PropertyMetadata(OnTextChanged));

        public SparkleNotifyIcon() {
            VisibilityProperty.OverrideMetadata(typeof(SparkleNotifyIcon), new PropertyMetadata(OnVisibilityChanged));

            NotifyIcon = new Forms.NotifyIcon {
                Text = Text,
                Visible = true,
                ContextMenu = new Forms.ContextMenu()
            };
            NotifyIcon.MouseDown += OnMouseDown;
            NotifyIcon.MouseUp += OnMouseUp;
            NotifyIcon.MouseClick += OnMouseClick;
            NotifyIcon.MouseDoubleClick += OnMouseDoubleClick;
        }

        public void ShowBalloonTip(string title, string subtext, string image_path) {
            // TODO:
            // - Use the image pointed to by image_path
            // - Find a way to use the prettier (Win7?) balloons
            NotifyIcon.ShowBalloonTip(5 * 1000, title, subtext, Forms.ToolTipIcon.Info);
        }

        public void Dispose() {
            NotifyIcon.Dispose();
        }


        void IAddChild.AddChild(object value) {
            throw new InvalidOperationException();
        }

        void IAddChild.AddText(string text) {
            if(text == null)
                throw new ArgumentNullException();

            Text = text;
        }

        private static MouseButtonEventArgs CreateMouseButtonEventArgs(RoutedEvent handler, Forms.MouseButtons button) {
            MouseButton mouse_button;

            if(button == Forms.MouseButtons.Left) {
                mouse_button = MouseButton.Left;

            } else if(button == Forms.MouseButtons.Right) {
                mouse_button = MouseButton.Right;

            } else if(button == Forms.MouseButtons.Middle) {
                mouse_button = MouseButton.Middle;

            } else if(button == Forms.MouseButtons.XButton1) {
                mouse_button = MouseButton.XButton1;

            } else if(button == Forms.MouseButtons.XButton2) {
                mouse_button = MouseButton.XButton2;

            } else {
                throw new InvalidOperationException();
            }

            return new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, mouse_button) {
                RoutedEvent = handler
            };
        }

        private void OnVisibilityChanged(DependencyObject target, DependencyPropertyChangedEventArgs args) {
            SparkleNotifyIcon control = (SparkleNotifyIcon) target;
            control.NotifyIcon.Visible = (control.Visibility == Visibility.Visible);
        }

        private void OnMouseDown(object sender, Forms.MouseEventArgs args) {
            RaiseEvent(CreateMouseButtonEventArgs(MouseDownEvent, args.Button));
        }

        private void OnMouseClick(object sender, Forms.MouseEventArgs args) {
            RaiseEvent(CreateMouseButtonEventArgs(MouseClickEvent, args.Button));
        }

        private void OnMouseDoubleClick(object sender, Forms.MouseEventArgs args) {
            RaiseEvent(CreateMouseButtonEventArgs(MouseDoubleClickEvent, args.Button));
        }

        private void OnMouseUp(object sender, Forms.MouseEventArgs args) {

            if(args.Button == Forms.MouseButtons.Right) {

                ContextMenu.IsOpen = true;
                ContextMenu.StaysOpen = false;
            }

            RaiseEvent(CreateMouseButtonEventArgs(MouseUpEvent, args.Button));
        }

        private static void OnTextChanged(DependencyObject target, DependencyPropertyChangedEventArgs args) {
            SparkleNotifyIcon control = (SparkleNotifyIcon) target;
            control.NotifyIcon.Text = control.Text;
        }


        private static Drawing.Icon GetIconFromBitmap(Drawing.Bitmap bitmap) {
            IntPtr unmanaged_icon = bitmap.GetHicon();
            Drawing.Icon icon = (Drawing.Icon) Drawing.Icon.FromHandle(unmanaged_icon).Clone();
            DestroyIcon(unmanaged_icon);

            return icon;
        }
    }
}
