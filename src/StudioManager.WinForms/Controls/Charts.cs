using System.Drawing.Drawing2D;

namespace StudioManager.WinForms.Controls;

public sealed class RevenueBarChart : Control
{
    private IReadOnlyList<(string Label, decimal Value)> _data = [];
    public IReadOnlyList<(string Label, decimal Value)> Data { get => _data; set { _data = value ?? []; Invalidate(); } }
    public RevenueBarChart() { DoubleBuffered = true; BackColor = Color.White; MinimumSize = new Size(360, 190); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var plot = new Rectangle(48, 24, Math.Max(20, Width - 70), Math.Max(20, Height - 62));
        using var gridPen = new Pen(Color.FromArgb(235, 238, 244), 1);
        for (var i = 0; i <= 4; i++) { var y = plot.Top + plot.Height * i / 4; g.DrawLine(gridPen, plot.Left, y, plot.Right, y); }
        if (_data.Count == 0) { DrawCentered(g, "Chưa có dữ liệu doanh thu", Theme.Muted); return; }
        var max = Math.Max(1m, _data.Max(x => x.Value)); var slot = plot.Width / (float)Math.Max(1, _data.Count - 1);
        var points = new List<PointF>();
        for (var i = 0; i < _data.Count; i++)
        {
            var item = _data[i]; var x = plot.Left + slot * i; var y = plot.Bottom - (float)(item.Value / max) * (plot.Height - 16); points.Add(new PointF(x, y));
            using var labelFont = Theme.Font(8.2f); using var labelBrush = new SolidBrush(Theme.Muted); var size = g.MeasureString(item.Label, labelFont); g.DrawString(item.Label, labelFont, labelBrush, x - size.Width / 2, plot.Bottom + 8);
        }
        if (points.Count > 1)
        {
            using var fillPath = new GraphicsPath(); fillPath.AddLines(points.ToArray()); fillPath.AddLine(points[^1].X, plot.Bottom, points[0].X, plot.Bottom); fillPath.CloseFigure();
            using var fill = new LinearGradientBrush(plot, Color.FromArgb(100, Theme.Primary), Color.FromArgb(3, Theme.Primary), LinearGradientMode.Vertical); g.FillPath(fill, fillPath);
            using var line = new Pen(Theme.Primary, 2.6f) { LineJoin = LineJoin.Round }; g.DrawLines(line, points.ToArray());
        }
        foreach (var point in points) { using var dot = new SolidBrush(Color.White); using var outline = new Pen(Theme.Primary, 2); g.FillEllipse(dot, point.X - 4, point.Y - 4, 8, 8); g.DrawEllipse(outline, point.X - 4, point.Y - 4, 8, 8); }
    }
    private void DrawCentered(Graphics g, string text, Color color) { using var f = Theme.Font(10); using var b = new SolidBrush(color); var s = g.MeasureString(text, f); g.DrawString(text, f, b, (Width - s.Width) / 2, (Height - s.Height) / 2); }
    private static GraphicsPath Rounded(RectangleF r, float radius) { var p = new GraphicsPath(); var d = radius * 2; p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddLine(r.Right, r.Bottom, r.X, r.Bottom); p.CloseFigure(); return p; }
}

public sealed class StatusDonutChart : Control
{
    private IReadOnlyDictionary<string, int> _data = new Dictionary<string, int>();
    private readonly Color[] _colors = [Theme.Primary, Theme.Success, Theme.Warning, Theme.Cyan, Theme.Purple, Theme.Danger, Color.FromArgb(100,116,139)];
    public IReadOnlyDictionary<string, int> Data { get => _data; set { _data = value ?? new Dictionary<string, int>(); Invalidate(); } }
    public StatusDonutChart() { DoubleBuffered = true; BackColor = Color.White; MinimumSize = new Size(290, 190); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; var total = _data.Values.Sum();
        var diameter = Math.Min(142, Math.Min(Height - 40, Width / 2)); var circle = new Rectangle(18, (Height - diameter) / 2, diameter, diameter);
        if (total == 0) { using var p = new Pen(Theme.Border, 22); g.DrawEllipse(p, circle); }
        else { var start = -90f; var i = 0; foreach (var item in _data) { var sweep = 360f * item.Value / total; using var p = new Pen(_colors[i++ % _colors.Length], 22) { StartCap = LineCap.Round, EndCap = LineCap.Round }; g.DrawArc(p, circle, start, Math.Max(1, sweep - 2)); start += sweep; } }
        using var big = Theme.Font(19, FontStyle.Bold); using var small = Theme.Font(8.5f); using var text = new SolidBrush(Theme.Text); using var muted = new SolidBrush(Theme.Muted);
        var totalText = total.ToString(); var ts = g.MeasureString(totalText, big); g.DrawString(totalText, big, text, circle.Left + (circle.Width - ts.Width) / 2, circle.Top + circle.Height / 2 - 24); var cap = g.MeasureString("Tổng lịch", small); g.DrawString("Tổng lịch", small, muted, circle.Left + (circle.Width - cap.Width) / 2, circle.Top + circle.Height / 2 + 9);
        var lx = circle.Right + 28; var ly = 22; var index = 0; foreach (var item in _data.OrderByDescending(x => x.Value).Take(5)) { using var b = new SolidBrush(_colors[index++ % _colors.Length]); g.FillEllipse(b, lx, ly + 4, 9, 9); g.DrawString(item.Key, small, muted, lx + 16, ly); g.DrawString(item.Value.ToString(), small, text, Width - 34, ly); ly += 28; }
    }
}

public sealed class SkeletonLoader : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 28 }; private int _offset;
    public SkeletonLoader() { DoubleBuffered = true; BackColor = Theme.Background; _timer.Tick += (_, _) => { _offset = (_offset + 18) % Math.Max(1, Width + 280); Invalidate(); }; }
    public void Start() { Visible = true; BringToFront(); _timer.Start(); }
    public void Stop() { _timer.Stop(); Visible = false; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var blocks = new List<Rectangle>(); var margin = 10; var cardWidth = Math.Max(150, (Width - 5 * margin) / 4);
        for (var i = 0; i < 4; i++) blocks.Add(new Rectangle(margin + i * (cardWidth + margin), 20, cardWidth, 112));
        blocks.Add(new Rectangle(margin, 155, Math.Max(260, Width * 2 / 3 - 16), 235)); blocks.Add(new Rectangle(Width * 2 / 3 + 4, 155, Math.Max(220, Width / 3 - 14), 235)); blocks.Add(new Rectangle(margin, 410, Math.Max(220, Width - 2 * margin), Math.Max(120, Height - 430)));
        foreach (var block in blocks) { using var baseBrush = new SolidBrush(Color.FromArgb(231, 235, 242)); e.Graphics.FillRectangle(baseBrush, block); var shimmer = new Rectangle(_offset - 280, block.Top, 280, block.Height); using var shine = new LinearGradientBrush(shimmer, Color.FromArgb(0,255,255,255), Color.FromArgb(150,255,255,255), LinearGradientMode.Horizontal); e.Graphics.FillRectangle(shine, Rectangle.Intersect(block, shimmer)); }
    }
}
