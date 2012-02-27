namespace SparkleShare {
	partial class SparkleAbout {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (SparkleAbout));
            this.version = new System.Windows.Forms.Label ();
            this.copyright = new System.Windows.Forms.Label ();
            this.emptyLabel = new System.Windows.Forms.Label ();
            this.SparkleShareVersion = new System.Windows.Forms.Label ();
            this.SuspendLayout ();
            // 
            // version
            // 
            this.version.AutoSize = true;
            this.version.BackColor = System.Drawing.Color.Transparent;
            this.version.ForeColor = System.Drawing.Color.LightGray;
            this.version.Location = new System.Drawing.Point (302, 102);
            this.version.Name = "version";
            this.version.Size = new System.Drawing.Size (34, 13);
            this.version.TabIndex = 1;
            this.version.Text = ".........";
            // 
            // copyright
            // 
            this.copyright.BackColor = System.Drawing.Color.Transparent;
            this.copyright.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.copyright.ForeColor = System.Drawing.Color.White;
            this.copyright.Location = new System.Drawing.Point (302, 135);
            this.copyright.Name = "copyright";
            this.copyright.Size = new System.Drawing.Size (298, 84);
            this.copyright.TabIndex = 2;
            this.copyright.Text = resources.GetString ("copyright.Text");
            // 
            // emptyLabel
            // 
            this.emptyLabel.AutoSize = true;
            this.emptyLabel.Location = new System.Drawing.Point (16, 89);
            this.emptyLabel.Name = "emptyLabel";
            this.emptyLabel.Size = new System.Drawing.Size (0, 13);
            this.emptyLabel.TabIndex = 6;
            // 
            // SparkleShareVersion
            // 
            this.SparkleShareVersion.AutoSize = true;
            this.SparkleShareVersion.BackColor = System.Drawing.Color.Transparent;
            this.SparkleShareVersion.ForeColor = System.Drawing.Color.White;
            this.SparkleShareVersion.Location = new System.Drawing.Point (302, 89);
            this.SparkleShareVersion.Name = "SparkleShareVersion";
            this.SparkleShareVersion.Size = new System.Drawing.Size (106, 13);
            this.SparkleShareVersion.TabIndex = 1;
            this.SparkleShareVersion.Text = "SparkleShareVersion";
            // 
            // SparkleAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size (640, 260);
            this.Controls.Add (this.SparkleShareVersion);
            this.Controls.Add (this.emptyLabel);
            this.Controls.Add (this.copyright);
            this.Controls.Add (this.version);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SparkleAbout";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About SparkleShare";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler (this.SparkleAbout_FormClosing);
            this.ResumeLayout (false);
            this.PerformLayout ();

		}

		#endregion

        private System.Windows.Forms.Label version;
        private System.Windows.Forms.Label copyright;
        private System.Windows.Forms.Label emptyLabel;
        private System.Windows.Forms.Label SparkleShareVersion;


    }
}
