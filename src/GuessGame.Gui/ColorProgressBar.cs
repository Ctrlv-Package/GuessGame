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
            var rect = ClientRectangle;
            
            // Draw background with a border to make it visible in both light and dark modes
            e.Graphics.FillRectangle(new SolidBrush(BackColor), rect);
            using (var borderPen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);
            }
            
            if (Value > 0)
            {
                var width = (int)((Value / (double)Maximum) * rect.Width);
                var progressRect = new Rectangle(rect.X, rect.Y, width, rect.Height);
                using (var brush = new SolidBrush(ProgressColor))
                {
                    e.Graphics.FillRectangle(brush, progressRect);
                }
            }
        }
    }
}
