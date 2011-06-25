using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SparkleShare {
	public partial class SystemTrayForm : Form {

		protected override void OnLoad(EventArgs e) {
			WindowState		= FormWindowState.Minimized;
			ShowInTaskbar	= false;
			
			base.OnLoad(e);

			InitializeTrayIcon();
		}

		public void InitializeTrayIcon() {
			NotifyIcon trayIcon			= new NotifyIcon();
			trayIcon.Text				= "SparkleShare";
			trayIcon.Icon				= new Icon("sparkleshare.ico",40,40);
			trayIcon.Visible			= true;

			trayIcon.ContextMenuStrip	= GetTrayIconMenu();
		}

		public ContextMenuStrip GetTrayIconMenu() {
			ToolStripItem VersionStatusItem	= new ToolStripLabel();

			if (Updater.IsUpToDate()) {
				VersionStatusItem.Text		= "Up to date";
				VersionStatusItem.Enabled	= false;
			} else {
				VersionStatusItem.Click += UpdateApplication;
			}

			ToolStripItem AddRemoteFolderItem	= new ToolStripLabel();
			AddRemoteFolderItem.Text			= "Add remote folder";
			AddRemoteFolderItem.Click			+= AddRemoteFolder;

			ToolStripItem NotificationsItem	= new ToolStripLabel();
			NotificationsItem.Text			= "Turn off notifications";
			NotificationsItem.Click			+= ToggleNotifications;

			ToolStripItem AboutItem = new ToolStripLabel();
			AboutItem.Text			= "About SparkleShare";
			AboutItem.Click			+= DisplayAboutDialog;

			ToolStripItem ExitItem	= new ToolStripLabel();
			ExitItem.Text			= "Exit";
			ExitItem.Click			+= ExitApplication;

			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add(VersionStatusItem);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(AddRemoteFolderItem);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(NotificationsItem);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(AboutItem);
			menu.Items.Add(ExitItem);

			return menu;
		}

		public void UpdateApplication(object sender, EventArgs e) {
			throw new NotImplementedException();
		}

		public void AddRemoteFolder(object sender, EventArgs e) {
			throw new NotImplementedException();
		}

		public void ToggleNotifications(object sender, EventArgs e) {
			throw new NotImplementedException();
		}

		public void DisplayAboutDialog(object sender, EventArgs e) {
			throw new NotImplementedException();
		}

		public void ExitApplication(object sender, EventArgs e) {
			throw new NotImplementedException();
		}
	}
}