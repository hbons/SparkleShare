namespace SparkleShare {
    partial class SparkleEventLog {
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
            this.WebViewPanel = new System.Windows.Forms.Panel ();
            this.panel2 = new System.Windows.Forms.Panel ();
            this.combo_box = new System.Windows.Forms.ComboBox ();
            this.panel2.SuspendLayout ();
            this.SuspendLayout ();
            // 
            // WebViewPanel
            // 
            this.WebViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WebViewPanel.Location = new System.Drawing.Point (0, 44);
            this.WebViewPanel.Name = "WebViewPanel";
            this.WebViewPanel.Size = new System.Drawing.Size (472, 569);
            this.WebViewPanel.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Controls.Add (this.combo_box);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point (0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size (472, 44);
            this.panel2.TabIndex = 0;
            // 
            // combo_box
            // 
            this.combo_box.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.combo_box.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combo_box.FormattingEnabled = true;
            this.combo_box.Location = new System.Drawing.Point (339, 12);
            this.combo_box.Name = "combo_box";
            this.combo_box.Size = new System.Drawing.Size (121, 21);
            this.combo_box.TabIndex = 0;
            this.combo_box.SelectedIndexChanged += new System.EventHandler (this.combo_box_SelectedIndexChanged);
            // 
            // SparkleEventLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size (472, 613);
            this.Controls.Add (this.WebViewPanel);
            this.Controls.Add (this.panel2);
            this.Name = "SparkleEventLog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Recent Events";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler (this.SparkleEventLog_FormClosing);
            this.panel2.ResumeLayout (false);
            this.ResumeLayout (false);

        }

        #endregion

        private System.Windows.Forms.Panel WebViewPanel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ComboBox combo_box;






    }
}
