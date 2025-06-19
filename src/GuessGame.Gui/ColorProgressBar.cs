using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuessGame.Gui
{
    public class ColorProgressBar : Control
    {
        private int _value;
        private int _maximum = 100;
        private Color _progressColor = Color.FromArgb(0, 120, 215);

        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(value, Maximum));
                Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(1, value);
                Invalidate();
            }
        }

        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        public ColorProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.LightGray;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var rect = ClientRectangle;
            
            // Draw background
            using (var backBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(backBrush, rect);
            }

            // Draw progress
            if (Value > 0)
            {
                var width = (int)((rect.Width * Value) / (double)Maximum);
                var progressRect = new Rectangle(rect.X, rect.Y, width, rect.Height);
                
                using (var foreBrush = new SolidBrush(ProgressColor))
                {
                    e.Graphics.FillRectangle(foreBrush, progressRect);
                }
            }
        }
    }
}
