using System.Drawing.Drawing2D;

namespace AlignTaiko.Gui
{
    internal sealed class DashedDropPanel : Panel
    {
        public Color BorderColor { get; set; } = Color.FromArgb(58, 57, 70);   // default border
        public Color HoverBorderColor { get; set; } = Color.FromArgb(167, 166, 217); // accent soft
        public float BorderWidth { get; set; } = 1.5f;
        public int CornerRadius { get; set; } = 8;
        public float DashLength { get; set; } = 4f;
        public float DashGap { get; set; } = 3f;

        private bool _hover;

        public DashedDropPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            // Drop hint label etc. are children; receive hover when mouse enters them too.
            this.MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            this.MouseLeave += (_, _) => { _hover = false; Invalidate(); };
            this.ControlAdded += (_, e) =>
            {
                // Forward hover from children
                e.Control.MouseEnter += (_, _) => { _hover = true; Invalidate(); };
                e.Control.MouseLeave += (_, _) =>
                {
                    // When moving between child and panel, Leave fires; check cursor actually left bounds.
                    var pt = PointToClient(Cursor.Position);
                    _hover = ClientRectangle.Contains(pt);
                    Invalidate();
                };
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var active = _dragActive || _hover;
            var color = active ? HoverBorderColor : BorderColor;
            using var pen = new Pen(color, BorderWidth)
            {
                DashStyle = DashStyle.Custom,
                DashPattern = new[] { DashLength, DashGap },
                Alignment = PenAlignment.Inset
            };

            var rect = ClientRectangle;
            rect.Inflate(-(int)Math.Ceiling(BorderWidth), -(int)Math.Ceiling(BorderWidth));

            using var path = RoundedRect(rect, CornerRadius);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            int d = radius * 2;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public bool IsDragActive
        {
            get => _dragActive;
            set { _dragActive = value; Invalidate(); }
        }
        private bool _dragActive;
    }
}
