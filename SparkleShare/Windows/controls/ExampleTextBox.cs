using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SparkleShare.controls {
    public class ExampleTextBox : TextBox {

        private bool ExampleTextActive = true;
        private bool OnTextChangedActive = true;
        private bool _focused = false;

        private string _ExampleText = "";
        public string ExampleText
        {
            get { return _ExampleText; }
            set
            {
                _ExampleText = value;
                if (ExampleTextActive)
                    ActivateExampleText ();
            }
        }

        public override string Text
        {
            get
            {
                if (ExampleTextActive)
                    return "";
                return base.Text;
            }
            set
            {
                if (String.IsNullOrEmpty (value)) {
                    ActivateExampleText ();
                } else {
                    ExampleTextActive = false;
                    ForeColor = System.Drawing.SystemColors.WindowText;
                    base.Text = value;
                }
            }
        }

        private void ActivateExampleText ()
        {
            OnTextChangedActive = false;
            base.Text = ExampleText;
            OnTextChangedActive = true;
            ExampleTextActive = true;
            ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
        }

        protected override void OnTextChanged (EventArgs e)
        {
            if (!OnTextChangedActive)
                return;

            bool Empty = String.IsNullOrEmpty (base.Text);
            if (Empty) {
                ActivateExampleText ();
                SelectAll ();
            } else if (ExampleTextActive) {
                ExampleTextActive = false;
                ForeColor = System.Drawing.SystemColors.WindowText;
            }
            base.OnTextChanged (e);
        }

        protected override void OnEnter (EventArgs e)
        {
            base.OnEnter (e);
            if (ExampleTextActive && MouseButtons == MouseButtons.None) {
                SelectAll ();
                _focused = true;
            }
        }

        protected override void OnLeave (EventArgs e)
        {
            base.OnLeave (e);
            _focused = false;
        }

        protected override void OnMouseUp (MouseEventArgs mevent)
        {
            base.OnMouseUp (mevent);
            if (!_focused) {
                if (ExampleTextActive && SelectionLength == 0)
                    SelectAll ();
                _focused = true;
            }
        }
    }
}
