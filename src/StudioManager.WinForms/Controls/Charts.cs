using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms.Controls;

public sealed class RevenueBarChart : Control
{
    private IReadOnlyList<(string Label, decimal Value)> _data = [];
    private int _hoveredIndex = -1;
    private readonly ToolTip _toolTip = new();

    public IReadOnlyList<(string Label, decimal Value)> Data
    {
        get => _data;
        set
        {
            _data = value ?? [];
            _hoveredIndex = -1;
            Invalidate();
        }
    }

    public RevenueBarChart()
    {
        DoubleBuffered = true;
        BackColor = Theme.Surface;
        MinimumSize = new Size(360, 200);
        MouseMove += OnChartMouseMove;
        MouseLeave += (_, _) => { _hoveredIndex = -1; _toolTip.Hide(this); Invalidate(); };
    }

    private void OnChartMouseMove(object? sender, MouseEventArgs e)
    {
        if (_data.Count == 0) return;
        var plot = GetPlotRectangle();
        if (!plot.Contains(e.Location))
        {
            if (_hoveredIndex != -1) { _hoveredIndex = -1; _toolTip.Hide(this); Invalidate(); }
            return;
        }

        var slot = plot.Width / (float)Math.Max(1, _data.Count - 1);
        var nearest = (int)Math.Round((e.X - plot.Left) / slot);
        nearest = Math.Clamp(nearest, 0, _data.Count - 1);

        if (nearest != _hoveredIndex)
        {
            _hoveredIndex = nearest;
            var item = _data[nearest];
            _toolTip.Show($"{item.Label}: {item.Value:N0} đ", this, e.X + 10, e.Y - 25, 2500);
            Invalidate();
        }
    }

    private Rectangle GetPlotRectangle() => new(54, 28, Math.Max(20, Width - 80), Math.Max(20, Height - 68));

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var plot = GetPlotRectangle();

        // Background subtle grid lines
        using var gridPen = new Pen(Color.FromArgb(241, 245, 249), 1f);
        using var axisFont = Theme.Font(8.0f);
        using var axisBrush = new SolidBrush(Theme.Muted);

        var maxVal = _data.Count > 0 ? Math.Max(1m, _data.Max(x => x.Value)) : 1000000m;
        // Round max up to nice round number
        var magnitude = (decimal)Math.Pow(10, Math.Max(1, (int)Math.Log10((double)maxVal)));
        var niceMax = Math.Ceiling(maxVal / magnitude) * magnitude;
        if (niceMax == 0) niceMax = 1000000m;

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);

            var val = niceMax * (4 - i) / 4;
            var valText = val >= 1_000_000 ? $"{val / 1_000_000:0.#}M" : val >= 1_000 ? $"{val / 1_000:0}k" : $"{val:0}";
            var size = g.MeasureString(valText, axisFont);
            g.DrawString(valText, axisFont, axisBrush, plot.Left - size.Width - 8, y - size.Height / 2);
        }

        if (_data.Count == 0)
        {
            DrawEmptyState(g, "Chưa có dữ liệu doanh thu trong giai đoạn này");
            return;
        }

        var slot = plot.Width / (float)Math.Max(1, _data.Count - 1);
        var points = new List<PointF>();

        for (var i = 0; i < _data.Count; i++)
        {
            var item = _data[i];
            var x = plot.Left + slot * i;
            var y = plot.Bottom - (float)(item.Value / niceMax) * plot.Height;
            points.Add(new PointF(x, y));

            // Month label
            var labelSize = g.MeasureString(item.Label, axisFont);
            g.DrawString(item.Label, axisFont, axisBrush, x - labelSize.Width / 2, plot.Bottom + 10);
        }

        if (points.Count > 1)
        {
            // Gradient fill under the curve
            using var fillPath = new GraphicsPath();
            fillPath.AddLines(points.ToArray());
            fillPath.AddLine(points[^1].X, plot.Bottom, points[0].X, plot.Bottom);
            fillPath.CloseFigure();

            using var fill = new LinearGradientBrush(
                new Rectangle(plot.Left, plot.Top, plot.Width, plot.Height + 2),
                Color.FromArgb(90, Theme.Primary),
                Color.FromArgb(5, Theme.Primary),
                LinearGradientMode.Vertical);
            g.FillPath(fill, fillPath);

            // Spline/Line stroke
            using var line = new Pen(Theme.Primary, 2.8f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(line, points.ToArray());
        }

        // Draw points with hover emphasis
        for (var i = 0; i < points.Count; i++)
        {
            var pt = points[i];
            var isHovered = i == _hoveredIndex;
            var radius = isHovered ? 6f : 4f;

            if (isHovered)
            {
                // Glow outer ring
                using var glowBrush = new SolidBrush(Color.FromArgb(40, Theme.Primary));
                g.FillEllipse(glowBrush, pt.X - 10, pt.Y - 10, 20, 20);

                // Vertical indicator line
                using var vlinePen = new Pen(Color.FromArgb(160, Theme.Primary), 1f) { DashStyle = DashStyle.Dash };
                g.DrawLine(vlinePen, pt.X, plot.Top, pt.X, plot.Bottom);
            }

            using var dotBrush = new SolidBrush(Color.White);
            using var borderPen = new Pen(Theme.Primary, isHovered ? 2.5f : 2f);
            g.FillEllipse(dotBrush, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
            g.DrawEllipse(borderPen, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
        }
    }

    private void DrawEmptyState(Graphics g, string message)
    {
        using var font = Theme.Font(9.5f);
        using var brush = new SolidBrush(Theme.Muted);
        var size = g.MeasureString(message, font);
        g.DrawString(message, font, brush, (Width - size.Width) / 2, (Height - size.Height) / 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _toolTip.Dispose();
        base.Dispose(disposing);
    }
}

public sealed class StatusDonutChart : Control
{
    private IReadOnlyDictionary<string, int> _data = new Dictionary<string, int>();

    private static readonly (string Status, Color Color)[] Palette =
    [
        ("Đã đặt lịch", Color.FromArgb(37, 99, 235)),       // Blue
        ("Đã chụp", Color.FromArgb(124, 58, 237)),          // Purple
        ("Đang chỉnh sửa ảnh", Color.FromArgb(245, 158, 11)), // Amber
        ("Chờ giao ảnh", Color.FromArgb(6, 182, 212)),       // Cyan
        ("Đã giao ảnh", Color.FromArgb(16, 185, 129)),       // Emerald
        ("Hoàn thành", Color.FromArgb(100, 116, 139)),      // Slate
        ("Đã hủy", Color.FromArgb(239, 68, 68))             // Red
    ];

    public IReadOnlyDictionary<string, int> Data
    {
        get => _data;
        set
        {
            _data = value ?? new Dictionary<string, int>();
            Invalidate();
        }
    }

    public StatusDonutChart()
    {
        DoubleBuffered = true;
        BackColor = Theme.Surface;
        MinimumSize = new Size(290, 200);
    }

    private Color ColorForStatus(string status, int index)
    {
        var found = Palette.FirstOrDefault(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (found.Color != Color.Empty) return found.Color;
        var fallback = new[] { Theme.Primary, Theme.Success, Theme.Warning, Theme.Cyan, Theme.Purple, Theme.Danger, Color.FromArgb(100, 116, 139) };
        return fallback[index % fallback.Length];
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var total = _data.Values.Sum();
        var diameter = Math.Min(148, Math.Min(Height - 36, Width / 2 - 10));
        var donutRect = new Rectangle(20, (Height - diameter) / 2, diameter, diameter);
        const float strokeWidth = 24f;

        if (total == 0)
        {
            using var emptyPen = new Pen(Color.FromArgb(241, 245, 249), strokeWidth);
            g.DrawEllipse(emptyPen, donutRect);
        }
        else
        {
            var start = -90f;
            var idx = 0;
            foreach (var kvp in _data)
            {
                var sweep = 360f * kvp.Value / total;
                if (sweep > 0)
                {
                    var segColor = ColorForStatus(kvp.Key, idx++);
                    using var pen = new Pen(segColor, strokeWidth)
                    {
                        StartCap = LineCap.Flat,
                        EndCap = LineCap.Flat
                    };
                    g.DrawArc(pen, donutRect, start, Math.Max(1.5f, sweep - 1.5f));
                }
                start += sweep;
            }
        }

        // Center total metric
        using var totalFont = Theme.Font(20, FontStyle.Bold);
        using var captionFont = Theme.Font(8.0f);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);

        var totalStr = total.ToString();
        var totalSize = g.MeasureString(totalStr, totalFont);
        g.DrawString(totalStr, totalFont, textBrush,
            donutRect.Left + (donutRect.Width - totalSize.Width) / 2,
            donutRect.Top + donutRect.Height / 2 - 22);

        var capStr = "Tổng lịch";
        var capSize = g.MeasureString(capStr, captionFont);
        g.DrawString(capStr, captionFont, mutedBrush,
            donutRect.Left + (donutRect.Width - capSize.Width) / 2,
            donutRect.Top + donutRect.Height / 2 + 8);

        // Legend list on the right
        var lx = donutRect.Right + 26;
        var ly = Math.Max(16, (Height - Math.Min(5, _data.Count) * 28) / 2);
        var legendIndex = 0;

        foreach (var item in _data.OrderByDescending(x => x.Value).Take(5))
        {
            var col = ColorForStatus(item.Key, legendIndex++);
            using var pillBrush = new SolidBrush(col);
            g.FillEllipse(pillBrush, lx, ly + 5, 9, 9);

            var pct = total > 0 ? (item.Value * 100f / total) : 0;
            using var legFont = Theme.Font(8.5f);
            using var valFont = Theme.Font(8.5f, FontStyle.Bold);

            // Status label
            var labelText = item.Key;
            if (labelText.Length > 16) labelText = labelText.Substring(0, 14) + "…";
            g.DrawString(labelText, legFont, mutedBrush, lx + 16, ly);

            // Value + percentage
            var countText = $"{item.Value} ({pct:0}%)";
            var countSize = g.MeasureString(countText, valFont);
            g.DrawString(countText, valFont, textBrush, Width - countSize.Width - 14, ly);

            ly += 28;
        }
    }
}

public sealed class SkeletonLoader : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 24 };
    private int _offset;

    public SkeletonLoader()
    {
        DoubleBuffered = true;
        BackColor = Theme.Background;
        _timer.Tick += (_, _) =>
        {
            _offset = (_offset + 16) % Math.Max(1, Width + 320);
            Invalidate();
        };
    }

    public void Start()
    {
        Visible = true;
        BringToFront();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        Visible = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var blocks = new List<Rectangle>();
        const int margin = 12;
        var cardWidth = Math.Max(140, (Width - 5 * margin) / 4);

        // Header greeting bar
        blocks.Add(new Rectangle(margin, 16, Math.Min(360, Width - 2 * margin), 48));

        // 4 KPI Cards
        for (var i = 0; i < 4; i++)
            blocks.Add(new Rectangle(margin + i * (cardWidth + margin), 78, cardWidth, 120));

        // Middle analytics (2 panels)
        var leftWidth = Math.Max(260, (int)(Width * 0.58f) - margin);
        var rightWidth = Math.Max(220, Width - leftWidth - 3 * margin);
        blocks.Add(new Rectangle(margin, 214, leftWidth, 260));
        blocks.Add(new Rectangle(margin + leftWidth + margin, 214, rightWidth, 260));

        // Bottom recent data grid
        blocks.Add(new Rectangle(margin, 490, Math.Max(220, Width - 2 * margin), Math.Max(120, Height - 510)));

        using var baseBrush = new SolidBrush(Color.FromArgb(235, 239, 245));

        foreach (var block in blocks)
        {
            using var path = Theme.RoundedRectangle(new RectangleF(block.X, block.Y, block.Width, block.Height), 10);
            g.FillPath(baseBrush, path);

            var shimmer = new Rectangle(_offset - 320, block.Top, 320, block.Height);
            var intersect = Rectangle.Intersect(block, shimmer);
            if (intersect.Width > 0 && intersect.Height > 0)
            {
                using var shine = new LinearGradientBrush(
                    shimmer,
                    Color.FromArgb(0, 255, 255, 255),
                    Color.FromArgb(160, 255, 255, 255),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(shine, intersect);
            }
        }
    }
}
