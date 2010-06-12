/*
 * Copyright (c) 2006-2007 Sebastian Dröge <slomo@circular-chaos.org>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Reflection;
using System.Collections.Generic;

using GLib;
using Gdk;
using Gtk;

using NDesk.DBus;
using org.freedesktop;
using org.freedesktop.DBus;

namespace Notifications {
	public enum Urgency : byte {
		Low = 0,
		Normal,
		Critical
	}

	public class ActionArgs : EventArgs {
		private string action;
		public string Action {
			get { return action; }
		}

		public ActionArgs (string action) {
			this.action = action;
		}
	}

	public class CloseArgs : EventArgs {
		private CloseReason reason;
		public CloseReason Reason {
			get { return reason; }
		}

		public CloseArgs (CloseReason reason) {
			this.reason = reason;
		}
	}

	public delegate void ActionHandler (object o, ActionArgs args);
	public delegate void CloseHandler (object o, CloseArgs args);
	
	public class Notification {
		private struct IconData {
			public int Width;
			public int Height;
			public int Rowstride;
			public bool HasAlpha;
			public int BitsPerSample;
			public int NChannels;
			public byte[] Pixels;		
		}

		private struct ActionTuple {
			public string Label;
			public ActionHandler Handler;

			public ActionTuple (string label, ActionHandler handler) {
				Label = label;
				Handler = handler;
			}
		}

		private INotifications nf;

		private bool updates_pending = false;
		private bool shown = false;

		private string app_name;
		private uint id = 0;
		private int timeout = -1;
		private string summary = String.Empty, body = String.Empty;
		private string icon = String.Empty;
		private Gtk.Widget attach_widget = null;
		private Gtk.StatusIcon status_icon = null;
		private IDictionary <string, ActionTuple> action_map = new Dictionary<string, ActionTuple> ();
		private IDictionary <string, object> hints  = new Dictionary<string, object> ();

		public event EventHandler Closed;

		static Notification () {
			BusG.Init ();
		}
		
		public Notification () {
			nf = Global.DBusObject;

			nf.NotificationClosed += OnClosed;
			nf.ActionInvoked += OnActionInvoked;

			this.app_name = Assembly.GetCallingAssembly().GetName().Name;
		}

		public Notification (string summary, string body) : this () {
			this.summary = summary;
			this.body = body;
		}

		public Notification (string summary, string body, string icon) : this (summary, body) {
			this.icon = icon;
		}

		public Notification (string summary, string body, Pixbuf icon) : this (summary, body) {
			SetPixbufHint (icon);
		}

		public Notification (string summary, string body, Pixbuf icon, Gtk.Widget widget) : this (summary, body, icon) {
			AttachToWidget (widget);
		}
		
		public Notification (string summary, string body, string icon, Gtk.Widget widget) : this (summary, body, icon) {
			AttachToWidget (widget);
		}

		public Notification (string summary, string body, Pixbuf icon, Gtk.StatusIcon status_icon) : this (summary, body, icon) {
			AttachToStatusIcon (status_icon);
		}
		
		public Notification (string summary, string body, string icon, Gtk.StatusIcon status_icon) : this (summary, body, icon) {
			AttachToStatusIcon (status_icon);
		}


		public string Summary {
			set {
				summary = value;
				Update ();
			}
			get {
				return summary;
			}
		}

		public string Body {
			set {
				body = value;
				Update ();
			}
			get {
				return body;
			}
		}

		public int Timeout {
			set {
				timeout = value;
				Update ();
			}
			get {
				return timeout;
			}
		}

		public Urgency Urgency {
			set {
				hints["urgency"] = (byte) value;
				Update ();
			}
			get {
				return hints.ContainsKey ("urgency") ? (Urgency) hints["urgency"] : Urgency.Normal;
			}
		}

		public string Category {
			set {
				hints["category"] = value;
				Update ();
			}
			get {
				return hints.ContainsKey ("category") ? (string) hints["category"] : String.Empty;
			}

		}

		public Pixbuf Icon {
			set {
				SetPixbufHint (value);
				icon = String.Empty;
				Update ();
			}
		}

		public string IconName {
			set {
				icon = value;
				hints.Remove ("icon_data");
				Update ();
			}
		}

		public uint Id {
			get {
				return id;
			}
		}

		public Gtk.Widget AttachWidget {
			get {
				return attach_widget;
			}
			set {
				AttachToWidget (value);
			}
		}

		public Gtk.StatusIcon StatusIcon {
			get {
				return status_icon;
			}
			set {
				AttachToStatusIcon (value);
			}
		}

		private void SetPixbufHint (Pixbuf pixbuf) {
			IconData icon_data = new IconData ();
			icon_data.Width = pixbuf.Width;
			icon_data.Height = pixbuf.Height;
			icon_data.Rowstride = pixbuf.Rowstride;
			icon_data.HasAlpha = pixbuf.HasAlpha;
			icon_data.BitsPerSample = pixbuf.BitsPerSample;
			icon_data.NChannels = pixbuf.NChannels;

			int len = (icon_data.Height - 1) * icon_data.Rowstride + icon_data.Width *
				((icon_data.NChannels * icon_data.BitsPerSample + 7) / 8);
			icon_data.Pixels = new byte[len];
			System.Runtime.InteropServices.Marshal.Copy (pixbuf.Pixels, icon_data.Pixels, 0, len);

			hints["icon_data"] = icon_data;
		}

		public void AttachToWidget (Gtk.Widget widget) {
			int x, y;

			widget.GdkWindow.GetOrigin (out x, out y);

			if (widget.GetType() != typeof (Gtk.Window) || ! widget.GetType().IsSubclassOf(typeof (Gtk.Window))) {
				x += widget.Allocation.X;
				y += widget.Allocation.Y;
			}

			x += widget.Allocation.Width / 2;
			y += widget.Allocation.Height / 2;

			SetGeometryHints (widget.Screen, x, y);
			attach_widget = widget;
			status_icon = null;
		}

		public void AttachToStatusIcon (Gtk.StatusIcon status_icon) {
			Gdk.Screen screen;
			Gdk.Rectangle rect;
			Orientation orientation;
			int x, y;

			if (!status_icon.GetGeometry (out screen, out rect, out orientation)) {
				return;
			}

			x = rect.X + rect.Width / 2;
			y = rect.Y + rect.Height / 2;

			SetGeometryHints (screen, x, y);

			this.status_icon = status_icon;
			attach_widget = null;
		}

		public void SetGeometryHints (Screen screen, int x, int y) {
			hints["x"] = x;
			hints["y"] = y;
			hints["xdisplay"] = screen.MakeDisplayName ();
			Update ();
		}

		private void Update () {
			if (shown && !updates_pending) {
				updates_pending = true;
				GLib.Timeout.Add (100, delegate {
					if (updates_pending) {
						Show ();
						updates_pending = false;
					}
					return false;
				});
			}
		}
		
		public void Show () {
			string[] actions;
			lock (action_map) {
				actions = new string[action_map.Keys.Count * 2];
				int i = 0;
				foreach (KeyValuePair<string,ActionTuple> pair in action_map) {
					actions[i++] = pair.Key;
					actions[i++] = pair.Value.Label;
				}
			}
			id = nf.Notify (app_name, id, icon, summary, body, actions, hints, timeout);
			shown = true;
		}

		public void Close () {
			nf.CloseNotification (id);
			id = 0;
			shown = false;
		}

		private void OnClosed (uint id, uint reason) {
			if (this.id == id) {
				this.id = 0;
				shown = false;
				if (Closed != null) {
					Closed (this, new CloseArgs ((CloseReason) reason));
				}
			}
		}

		public void AddAction (string action, string label, ActionHandler handler) {
			if (Notifications.Global.Capabilities != null &&
			    Array.IndexOf (Notifications.Global.Capabilities, "actions") > -1) {
				lock (action_map) {
					action_map[action] = new ActionTuple (label, handler);
				}
				Update ();
			}
		}

		public void RemoveAction (string action) {
			lock (action_map) {
				action_map.Remove (action);
			}
			Update ();
		}

		public void ClearActions () {
			lock (action_map) {
				action_map.Clear ();
			}
			Update ();
		}

		private void OnActionInvoked (uint id, string action) {
			lock (action_map) {
				if (this.id == id && action_map.ContainsKey (action))
					action_map[action].Handler (this, new ActionArgs (action));
			}
		}

		public void AddHint (string name, object value) {
			hints[name] = value;
			Update ();
		}

		public void RemoveHint (string name) {
			hints.Remove (name);
			Update ();
		}
	}
}
