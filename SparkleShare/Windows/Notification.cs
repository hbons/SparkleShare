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
using System.Windows.Forms;
using System.Drawing;
using System.IO;


namespace Notifications
{
    public partial class Notification : Form
    {
		private Timer animationTimer;
		private int startPosX;
		private int startPosY;

		//public new Gdk.Pixbuf Icon;

        public Notification ()
        {
			InitializeComponent ();

			TopMost = true;
			ShowInTaskbar = false;

			animationTimer = new Timer ();
			animationTimer.Interval = 50;
			animationTimer.Tick += timer_Tick;
		}

		public Notification (string title, string subtext)
			: this()
		{
			this.title.Text = title;
			this.subtext.Text = subtext;
		}

		protected override void OnLoad (EventArgs e)
		{
			// Move window out of screen
			startPosX = Screen.PrimaryScreen.WorkingArea.Width - Width;
			startPosY = Screen.PrimaryScreen.WorkingArea.Height;
			SetDesktopLocation (startPosX, startPosY);
			base.OnLoad (e);
			// Begin animation
			animationTimer.Start ();
		}

		protected override void OnShown (EventArgs e)
		{
			base.OnShown (e);

			// hacky way to move the image from a Gdk.Pixbuf to a winforms bitmap
			string Filename = Path.GetTempFileName ();
			File.Delete (Filename);

			Filename = Path.ChangeExtension (Filename, "bmp");
			if (File.Exists (Filename))
				File.Delete (Filename);
			//this.Icon.Save (Filename, "bmp");
			using (Stream s = File.OpenRead (Filename))
				pictureBox1.Image = Bitmap.FromStream (s);
			File.Delete (Filename);
		}

		void timer_Tick (object sender, EventArgs e)
		{
			startPosY -= 5;

			if (startPosY < Screen.PrimaryScreen.WorkingArea.Height - Height)
				animationTimer.Stop ();
			else
				SetDesktopLocation (startPosX, startPosY);
		}

        public void AddAction (string action, string label, ActionHandler handler)
        {
        }

        public void RemoveAction (string action)
        {
        }

        public void ClearActions ()
        {
        }

    }

	public enum Urgency : byte
	{
		Low = 0,
		Normal,
		Critical
	}

	public class ActionArgs : EventArgs
	{
		private string action;
		public string Action
		{
			get { return action; }
		}

		public ActionArgs (string action)
		{
			this.action = action;
		}
	}

	public delegate void ActionHandler (object o, ActionArgs args);

}
