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

using NDesk.DBus;
using org.freedesktop;
using org.freedesktop.DBus;

namespace Notifications {
	[Interface ("org.freedesktop.Notifications")]
	internal interface INotifications : Introspectable, Properties {
		ServerInformation ServerInformation { get; }
		string[] Capabilities { get; }
		void CloseNotification (uint id);
		uint Notify (string app_name, uint id, string icon, string summary, string body,
			string[] actions, IDictionary<string, object> hints, int timeout);
		event NotificationClosedHandler NotificationClosed;
		event ActionInvokedHandler ActionInvoked;
	}

	public enum CloseReason : uint {
		Expired = 1,
		User = 2,
		API = 3,
		Reserved = 4
	}

	internal delegate void NotificationClosedHandler (uint id, uint reason);
	internal delegate void ActionInvokedHandler (uint id, string action);

	public struct ServerInformation {
		public string Name;
		public string Vendor;
		public string Version;
		public string SpecVersion;
	}

	public static class Global {
		private const string interface_name = "org.freedesktop.Notifications";
		private const string object_path = "/org/freedesktop/Notifications";

		private static INotifications dbus_object = null;
		private static object dbus_object_lock = new object ();

		internal static INotifications DBusObject {
			get {
				if (dbus_object != null)
					return dbus_object;

				lock (dbus_object_lock) {
					if (! Bus.Session.NameHasOwner (interface_name))
						Bus.Session.StartServiceByName (interface_name);

					dbus_object = Bus.Session.GetObject<INotifications>
						(interface_name, new ObjectPath (object_path));
					return dbus_object;
				}
			}
		}

		public static string[] Capabilities {
			get {
				return DBusObject.Capabilities;
			}
		}
		
		public static ServerInformation ServerInformation {
			get {
				return DBusObject.ServerInformation;
			}
		}
	}
}
