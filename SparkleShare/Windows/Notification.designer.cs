namespace Notifications
{
    partial class Notification
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.pictureBox1 = new System.Windows.Forms.PictureBox ();
			this.title = new System.Windows.Forms.Label ();
			this.subtext = new System.Windows.Forms.Label ();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit ();
			this.SuspendLayout ();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Location = new System.Drawing.Point (12, 12);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size (40, 42);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// title
			// 
			this.title.AutoSize = true;
			this.title.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.title.Location = new System.Drawing.Point (58, 9);
			this.title.Name = "title";
			this.title.Size = new System.Drawing.Size (28, 13);
			this.title.TabIndex = 1;
			this.title.Text = "title";
			// 
			// subtext
			// 
			this.subtext.AutoSize = true;
			this.subtext.Location = new System.Drawing.Point (58, 22);
			this.subtext.MaximumSize = new System.Drawing.Size (171, 0);
			this.subtext.Name = "subtext";
			this.subtext.Size = new System.Drawing.Size (41, 13);
			this.subtext.TabIndex = 2;
			this.subtext.Text = "subtext";
			// 
			// Notification
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size (241, 64);
			this.ControlBox = false;
			this.Controls.Add (this.subtext);
			this.Controls.Add (this.title);
			this.Controls.Add (this.pictureBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Notification";
			this.Opacity = 0.8;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit ();
			this.ResumeLayout (false);
			this.PerformLayout ();

        }

        #endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label title;
		private System.Windows.Forms.Label subtext;

	}
}

