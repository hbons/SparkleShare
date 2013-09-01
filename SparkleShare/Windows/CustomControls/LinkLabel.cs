using System;
using System.Windows.Controls;

namespace SparkleShare.CustomControls
{
    public class LinkLabel : Label
    {
        System.Windows.Input.Cursor default_cursor;

        public LinkLabel ()
        {
            base.Foreground = System.Windows.Media.Brushes.Blue;
        }

        public string Link
        {
            get;
            set;
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(Link.ToString());
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            if (this.Parent != null && this.Parent is System.Windows.Controls.Canvas)
            {
                default_cursor = ((System.Windows.Controls.Canvas)this.Parent).Cursor;
                ((System.Windows.Controls.Canvas)this.Parent).Cursor = System.Windows.Input.Cursors.Hand;
            }
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            ((System.Windows.Controls.Canvas)this.Parent).Cursor = default_cursor;
        }
    }
}
