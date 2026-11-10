using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms.Controls;

/// <summary>
/// Project Phoenix — Hybrid Column & Spline Area Revenue Telemetry Chart.
/// </summary>
public sealed class RevenueBarChart : Control
{
    private IReadOnlyList<(string Label, decimal Value)> _data = [];
    private int _hoveredIndex = -1;

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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Surface;
        MinimumSize = new Size(340, 190);
        MouseMove += OnChartMouseMove;
        MouseLeave += (_, _) =>
        {
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
        };
    }

    private void OnChartMouseMove(object? sender, MouseEventArgs e)
    {
        if (_data.Count == 0) return;
        var plot = GetPlotRectangle();
        if (!plot.Contains(e.Location))
        {
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Invalidate();
            }
            return;
        }

        var slotWidth = plot.Width / (float)Math.Max(1, _data.Count);
        var nearest = (int)((e.X - plot.Left) / slotWidth);
        nearest = Math.Clamp(nearest, 0, _data.Count - 1);

        if (nearest != _hoveredIndex)
        {
            _hoveredIndex = nearest;
            Invalidate();
        }
    }

    private Rectangle GetPlotRectangle() =>
        new(58, 26, Math.Max(40, Width - 78), Math.Max(40, Height - 62));

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var plot = GetPlotRectangle();

        using var gridPen = new Pen(Theme.Primitives.Alabaster200, 1f) { DashStyle = DashStyle.Dot };
        using var baselinePen = new Pen(Theme.BorderStrong, 1f);
        using var axisFont = Theme.Font(8.0f);
        using var axisBoldFont = Theme.Font(8.0f, FontStyle.Bold);
        using var axisBrush = new SolidBrush(Theme.Muted);

        var maxVal = _data.Count > 0 ? Math.Max(1m, _data.Max(x => x.Value)) : 1000000m;
        var magnitude = (decimal)Math.Pow(10, Math.Max(1, (int)Math.Log10((double)maxVal)));
        var niceMax = Math.Ceiling(maxVal / magnitude) * magnitude;
        if (niceMax <= 0) niceMax = 1000000m;

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4;
            g.DrawLine(i == 4 ? baselinePen : gridPen, plot.Left, y, plot.Right, y);

            var val = niceMax * (4 - i) / 4;
            var valText = val >= 1_000_000 ? $"{val / 1_000_000:0.#}tr" : val >= 1_000 ? $"{val / 1_000:0}k" : $"{val:0}";
            var size = g.MeasureString(valText, axisFont);
            g.DrawString(valText, axisFont, axisBrush, plot.Left - size.Width - 8, y - size.Height / 2);
        }

        if (_data.Count == 0)
        {
            DrawEmptyState(g, "Chưa có phát sinh doanh thu trong 6 tháng gần nhất");
            return;
        }

        var slotWidth = plot.Width / (float)Math.Max(1, _data.Count);
        var barWidth = Math.Clamp(slotWidth * 0.42f, 16f, 38f);
        var points = new List<PointF>();

        for (var i = 0; i < _data.Count; i++)
        {
            var item = _data[i];
            var cx = plot.Left + slotWidth * i + slotWidth / 2f;
            var ratio = (float)(item.Value / niceMax);
            var barHeight = Math.Max(4f, ratio * plot.Height);
            var cy = plot.Bottom - ratio * plot.Height;
            points.Add(new PointF(cx, cy));

            var isHovered = i == _hoveredIndex;
            var barRect = new RectangleF(cx - barWidth / 2f, plot.Bottom - barHeight, barWidth, barHeight);

            using (var barPath = Theme.RoundedRectangle(barRect, 5f))
            {
                using var barFill = new LinearGradientBrush(
                    new RectangleF(barRect.Left, plot.Top, barRect.Width, plot.Height + 4),
                    isHovered ? Theme.Primary : Color.FromArgb(185, Theme.Primary),
                    isHovered ? Theme.PrimaryHover : Color.FromArgb(55, Theme.Primitives.Terracotta200),
                    LinearGradientMode.Vertical);
                g.FillPath(barFill, barPath);

                if (isHovered)
                {
                    using var barOutline = new Pen(Theme.PrimaryPressed, 1.2f);
                    g.DrawPath(barOutline, barPath);
                }
            }

            // X-axis month label
            var labelSize = g.MeasureString(item.Label, isHovered ? axisBoldFont : axisFont);
            using var lblBrush = new SolidBrush(isHovered ? Theme.Primary : Theme.TextSecondary);
            g.DrawString(item.Label, isHovered ? axisBoldFont : axisFont, lblBrush, cx - labelSize.Width / 2f, plot.Bottom + 8);
        }

        // Trend line overlay connecting column tops
        if (points.Count > 1)
        {
            using var trendPen = new Pen(Theme.Primitives.Espresso800, 2.0f)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLines(trendPen, points.ToArray());
        }

        // Data nodes & floating callout pill for hovered point
        for (var i = 0; i < points.Count; i++)
        {
            var pt = points[i];
            var isHovered = i == _hoveredIndex;
            var r = isHovered ? 5.5f : 3.5f;

            using var dotFill = new SolidBrush(Theme.Surface);
            using var dotBorder = new Pen(isHovered ? Theme.Primary : Theme.Primitives.Espresso800, isHovered ? 2.5f : 1.8f);
            g.FillEllipse(dotFill, pt.X - r, pt.Y - r, r * 2, r * 2);
            g.DrawEllipse(dotBorder, pt.X - r, pt.Y - r, r * 2, r * 2);

            if (isHovered)
            {
                var item = _data[i];
                var calloutText = $"{item.Label}: {item.Value:N0} đ";
                var textSize = g.MeasureString(calloutText, axisBoldFont);
                var pillW = textSize.Width + 16f;
                var pillH = 24f;
                var pillX = Math.Clamp(pt.X - pillW / 2f, plot.Left, plot.Right - pillW);
                var pillY = Math.Max(4f, pt.Y - pillH - 10f);
                var pillRect = new RectangleF(pillX, pillY, pillW, pillH);

                using var pillPath = Theme.RoundedRectangle(pillRect, 6f);
                using var pillBrush = new SolidBrush(Theme.Primitives.Espresso900);
                using var textBrush = new SolidBrush(Theme.Primitives.Alabaster50);
                g.FillPath(pillBrush, pillPath);
                g.DrawString(calloutText, axisBoldFont, textBrush, pillX + 8f, pillY + (pillH - textSize.Height) / 2f);
            }
        }
    }

    private void DrawEmptyState(Graphics g, string message)
    {
        using var font = Theme.Font(9.0f);
        using var brush = new SolidBrush(Theme.Muted);
        var size = g.MeasureString(message, font);
        g.DrawString(message, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
    }
}

/// <summary>
/// Project Phoenix — Studio Workflow Stage Funnel & Ring Hybrid Chart.
/// </summary>
public sealed class StatusDonutChart : Control
{
    private IReadOnlyDictionary<string, int> _data = new Dictionary<string, int>();

    private static readonly (string Status, Color Color)[] Palette =
    [
        ("Đã đặt lịch", Theme.Primitives.Terracotta600),
        ("Đã chụp", Theme.Primitives.Plum600),
        ("Đang chỉnh sửa ảnh", Theme.Primitives.Ochre600),
        ("Chờ giao ảnh", Theme.Primitives.Teal600),
        ("Đã giao ảnh", Theme.Primitives.Sage600),
        ("Hoàn thành", Theme.Primitives.Umber700),
        ("Đã hủy", Theme.Primitives.Crimson600)
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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Surface;
        MinimumSize = new Size(280, 190);
    }

    private static Color ColorForStatus(string status, int index)
    {
        var found = Palette.FirstOrDefault(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (found.Color != Color.Empty) return found.Color;
        var fallback = new[]
        {
            Theme.Primary, Theme.Purple, Theme.Warning, Theme.Cyan, Theme.Success, Theme.Secondary, Theme.Danger
        };
        return fallback[index % fallback.Length];
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var total = _data.Values.Sum();
        var diameter = Math.Min(132, Math.Min(Height - 36, Math.Max(90, Width / 2 - 24)));
        var donutRect = new Rectangle(14, (Height - diameter) / 2, diameter, diameter);
        const float strokeWidth = 18f;

        if (total == 0)
        {
            using var emptyPen = new Pen(Theme.Primitives.Alabaster200, strokeWidth);
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
                    g.DrawArc(pen, donutRect, start, Math.Max(2f, sweep - 2f));
                }
                start += sweep;
            }
        }

        // Center total metric inside aperture ring
        using var totalFont = Theme.Font(17f, FontStyle.Bold);
        using var captionFont = Theme.Font(7.6f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);

        var totalStr = total.ToString();
        var totalSize = g.MeasureString(totalStr, totalFont);
        g.DrawString(totalStr, totalFont, textBrush,
            donutRect.Left + (donutRect.Width - totalSize.Width) / 2f,
            donutRect.Top + donutRect.Height / 2f - 18f);

        var capStr = "TỔNG LỊCH";
        var capSize = g.MeasureString(capStr, captionFont);
        g.DrawString(capStr, captionFont, mutedBrush,
            donutRect.Left + (donutRect.Width - capSize.Width) / 2f,
            donutRect.Top + donutRect.Height / 2f + 6f);

        // Right-hand Pipeline Stage Progress Bars
        var lx = donutRect.Right + 22;
        var rightW = Math.Max(80, Width - lx - 10);
        var items = _data.OrderByDescending(x => x.Value).Take(6).ToList();
        var rowH = 28;
        var ly = Math.Max(10, (Height - items.Count * rowH) / 2);

        using var legFont = Theme.Font(8.2f, FontStyle.Regular);
        using var valFont = Theme.Font(8.2f, FontStyle.Bold);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var col = ColorForStatus(item.Key, i);
            var pct = total > 0 ? (item.Value * 100f / total) : 0f;

            // Label & Count
            var labelText = item.Key.Length > 18 ? item.Key[..16] + "…" : item.Key;
            g.DrawString(labelText, legFont, textBrush, lx, ly);

            var countText = $"{item.Value} ({pct:0}%)";
            var countSize = g.MeasureString(countText, valFont);
            g.DrawString(countText, valFont, mutedBrush, lx + rightW - countSize.Width, ly);

            // Mini stage progress track
            var trackRect = new RectangleF(lx, ly + 17, rightW, 5f);
            using (var trackPath = Theme.RoundedRectangle(trackRect, 2.5f))
            using (var trackBrush = new SolidBrush(Theme.Primitives.Alabaster200))
            {
                g.FillPath(trackBrush, trackPath);
            }

            if (pct > 0)
            {
                var fillW = Math.Max(6f, rightW * (pct / 100f));
                var fillRect = new RectangleF(lx, ly + 17, fillW, 5f);
                using var fillPath = Theme.RoundedRectangle(fillRect, 2.5f);
                using var fillBrush = new SolidBrush(col);
                g.FillPath(fillBrush, fillPath);
            }

            ly += rowH;
        }
    }
}

/// <summary>
/// Project Phoenix — Horizontal Ranked Package Revenue Chart for ReportsPage.
/// </summary>
public sealed class PackageHorizontalBarChart : Control
{
    private IReadOnlyList<(string Name, decimal Value)> _data = [];

    public IReadOnlyList<(string Name, decimal Value)> Data
    {
        get => _data;
        set
        {
            _data = value ?? [];
            Invalidate();
        }
    }

    public PackageHorizontalBarChart()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Surface;
        MinimumSize = new Size(280, 180);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (_data.Count == 0)
        {
            using var emptyFont = Theme.Font(9.2f);
            using var emptyBrush = new SolidBrush(Theme.Muted);
            const string msg = "Không có dữ liệu gói chụp trong kỳ báo cáo đã chọn";
            var sz = g.MeasureString(msg, emptyFont);
            g.DrawString(msg, emptyFont, emptyBrush, (Width - sz.Width) / 2f, (Height - sz.Height) / 2f);
            return;
        }

        var items = _data.OrderByDescending(x => x.Value).Take(7).ToList();
        var maxVal = Math.Max(1m, items.Max(x => x.Value));
        var totalVal = Math.Max(1m, _data.Sum(x => x.Value));

        using var rankFont = Theme.MonoFont(8.2f, FontStyle.Bold);
        using var nameFont = Theme.Font(9.0f, FontStyle.Bold);
        using var valFont = Theme.Font(8.6f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);

        var padX = 10f;
        var usableW = Math.Max(100f, Width - padX * 2);
        var rowH = Math.Clamp((Height - 16f) / Math.Max(1, items.Count), 38f, 52f);
        var y = 8f;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var share = (float)(item.Value * 100m / totalVal);
            var barRatio = (float)(item.Value / maxVal);

            // Rank badge
            var rankBadge = new RectangleF(padX, y + 2, 24, 18);
            using (var rPath = Theme.RoundedRectangle(rankBadge, 4f))
            using (var rFill = new SolidBrush(i == 0 ? Theme.Primary : Theme.Primitives.Alabaster200))
            using (var rText = new SolidBrush(i == 0 ? Color.White : Theme.TextSecondary))
            {
                g.FillPath(rFill, rPath);
                var rStr = $"#{i + 1}";
                var rSz = g.MeasureString(rStr, rankFont);
                g.DrawString(rStr, rankFont, rText, rankBadge.Left + (rankBadge.Width - rSz.Width) / 2f, rankBadge.Top + (rankBadge.Height - rSz.Height) / 2f);
            }

            // Package name
            var pkgName = item.Name.Length > 34 ? item.Name[..32] + "…" : item.Name;
            g.DrawString(pkgName, nameFont, textBrush, padX + 32, y + 1);

            // Revenue + Share % on the right
            var metricStr = $"{item.Value:N0} đ ({share:0.#}%)";
            var mSize = g.MeasureString(metricStr, valFont);
            g.DrawString(metricStr, valFont, i == 0 ? new SolidBrush(Theme.Primary) : mutedBrush, padX + usableW - mSize.Width, y + 2);

            // Horizontal Bar Track
            var trackRect = new RectangleF(padX, y + 23, usableW, 8f);
            using (var tPath = Theme.RoundedRectangle(trackRect, 4f))
            using (var tFill = new SolidBrush(Theme.Primitives.Alabaster100))
            {
                g.FillPath(tFill, tPath);
            }

            var fillW = Math.Max(8f, usableW * barRatio);
            var fillRect = new RectangleF(padX, y + 23, fillW, 8f);
            using (var fPath = Theme.RoundedRectangle(fillRect, 4f))
            using (var fBrush = new LinearGradientBrush(
                new RectangleF(padX, y + 23, usableW, 8f),
                i == 0 ? Theme.Primary : Theme.Primitives.Ochre600,
                i == 0 ? Theme.Primitives.Ochre600 : Theme.Primitives.Terracotta500,
                LinearGradientMode.Horizontal))
            {
                g.FillPath(fBrush, fPath);
            }

            y += rowH;
        }
    }
}

public sealed class SkeletonLoader : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 28 };
    private int _offset;

    public SkeletonLoader()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Background;
        _timer.Tick += (_, _) =>
        {
            _offset = (_offset + 18) % Math.Max(1, Width + 320);
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
        var leftColW = Math.Max(300, (int)(Width * 0.66f) - margin);
        var rightColW = Math.Max(220, Width - leftColW - margin * 3);

        // Top PulseStrip
        blocks.Add(new Rectangle(margin, 12, leftColW, 92));
        // Middle Charts
        blocks.Add(new Rectangle(margin, 116, leftColW, 230));
        // Bottom Recent Orders
        blocks.Add(new Rectangle(margin, 358, leftColW, Math.Max(140, Height - 374)));
        // Right Studio Call Sheet
        blocks.Add(new Rectangle(margin + leftColW + margin, 12, rightColW, Math.Max(300, Height - 28)));

        using var baseBrush = new SolidBrush(Theme.Primitives.Alabaster200);

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
                    Color.FromArgb(140, 255, 255, 255),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(shine, intersect);
            }
        }
    }
}
