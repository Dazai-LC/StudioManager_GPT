using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms.Controls;

public sealed class PageHeader : Panel
{
    public FlowLayoutPanel Actions { get; } = new()
    {
        Dock = DockStyle.Right,
        Width = 540,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        AutoScroll = false,
        Padding = new Padding(0, 6, 0, 0),
        BackColor = Color.Transparent
    };

    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;

    public PageHeader(string title, string subtitle)
    {
        Height = 68;
        Dock = DockStyle.Top;
        BackColor = Color.Transparent;
        Padding = new Padding(2, 2, 0, 8);
        AccessibleName = title;

        var copy = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 30,
            Font = Theme.Font(13.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            Font = Theme.Font(9.0f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        copy.Controls.Add(_subtitleLabel);
        copy.Controls.Add(_titleLabel);

        Controls.Add(copy);
        Controls.Add(Actions);
        Actions.SendToBack();
        copy.BringToFront();
    }

    public string HeaderTitle { get => _titleLabel.Text; set => _titleLabel.Text = value; }
    public string HeaderSubtitle { get => _subtitleLabel.Text; set => _subtitleLabel.Text = value; }
}

public class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 10;
    public Color BorderColor { get; set; } = Theme.Border;
    public bool ShowBorder { get; set; } = true;
    public Color AccentStripeColor { get; set; } = Color.Transparent;
    public int AccentStripeThickness { get; set; } = 3;
    public bool AccentStripeLeft { get; set; }

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        Padding = new Padding(16);
        Margin = Padding.Empty;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor is { A: byte.MaxValue } c ? c : Theme.Background);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, CornerRadius);

        using (var fillBrush = new SolidBrush(BackColor))
        {
            g.FillPath(fillBrush, path);
        }

        if (AccentStripeColor != Color.Transparent && AccentStripeThickness > 0)
        {
            var state = g.Save();
            g.SetClip(path);
            using var accentBrush = new SolidBrush(AccentStripeColor);
            if (AccentStripeLeft)
                g.FillRectangle(accentBrush, 0, 0, AccentStripeThickness + 1, Height);
            else
                g.FillRectangle(accentBrush, 0, 0, Width, AccentStripeThickness + 1);
            g.Restore(state);
        }

        if (ShowBorder && BorderColor != Color.Transparent)
        {
            using var borderPen = new Pen(BorderColor, 1f);
            g.DrawPath(borderPen, path);
        }
    }
}

/// <summary>
/// Studio Telemetry Tile — Pure GDI+ vector icon, architectural top accent stripe, tabular metric display.
/// </summary>
public sealed class StatCard : CardPanel
{
    private readonly Label _value;
    private readonly Label _trend;
    private readonly Label _titleLabel;
    private readonly Color _accent;
    private readonly IconKind _icon;

    public StatCard(string title, Color accent, string icon = "")
        : this(title, accent, InferIcon(title, icon))
    {
    }

    public StatCard(string title, Color accent, IconKind icon)
    {
        _accent = accent;
        _icon = icon;
        Height = 118;
        CornerRadius = 10;
        AccentStripeColor = accent;
        AccentStripeThickness = 3;
        Padding = new Padding(16, 12, 16, 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        var iconCanvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(2, 0, 0, 0)
        };
        iconCanvas.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new RectangleF(1, 1, 36, 36);
            using var path = Theme.RoundedRectangle(box, 9f);
            using var fill = new SolidBrush(Color.FromArgb(24, _accent));
            using var pen = new Pen(Color.FromArgb(55, _accent), 1f);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            VectorIcons.Draw(g, _icon, new RectangleF(9, 9, 20, 20), _accent, 1.8f);
        };

        _titleLabel = new Label
        {
            Text = title.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = Theme.Font(7.8f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _value = new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = Theme.Font(17.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _trend = new Label
        {
            Text = "Đồng bộ trực tiếp",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = Theme.Font(8.0f, FontStyle.Bold),
            ForeColor = accent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        layout.Controls.Add(_titleLabel, 0, 0);
        layout.Controls.Add(_value, 0, 1);
        layout.Controls.Add(_trend, 0, 2);
        layout.Controls.Add(iconCanvas, 1, 0);
        layout.SetRowSpan(iconCanvas, 2);

        Controls.Add(layout);
    }

    private static IconKind InferIcon(string title, string legacyGlyph)
    {
        if (title.Contains("HÔM NAY", StringComparison.OrdinalIgnoreCase) || title.Contains("LỊCH", StringComparison.OrdinalIgnoreCase))
            return IconKind.Calendar;
        if (title.Contains("SẮP TỚI", StringComparison.OrdinalIgnoreCase))
            return IconKind.Clock;
        if (title.Contains("HẬU KỲ", StringComparison.OrdinalIgnoreCase) || title.Contains("XỬ LÝ", StringComparison.OrdinalIgnoreCase))
            return IconKind.Sparkles;
        if (title.Contains("CHỜ GIAO", StringComparison.OrdinalIgnoreCase) || title.Contains("HOÀN THÀNH", StringComparison.OrdinalIgnoreCase))
            return IconKind.Check;
        if (title.Contains("CÔNG NỢ", StringComparison.OrdinalIgnoreCase) || title.Contains("THU", StringComparison.OrdinalIgnoreCase) || title.Contains("DOANH THU", StringComparison.OrdinalIgnoreCase))
            return IconKind.Wallet;
        if (title.Contains("HỦY", StringComparison.OrdinalIgnoreCase))
            return IconKind.AlertTriangle;
        return IconKind.Aperture;
    }

    public string Value { get => _value.Text; set => _value.Text = value; }
    public string Trend { get => _trend.Text; set => _trend.Text = value; }
}

/// <summary>
/// Horizontal Segmented Pill Bar for Hub Sub-Navigation & Period Presets.
/// </summary>
public sealed class SegmentedTabBar : Control
{
    public sealed record SegmentItem(string Key, string Label, IconKind Icon = IconKind.None, string? Badge = null);

    private readonly List<SegmentItem> _items = [];
    private int _selectedIndex;
    private int _hoveredIndex = -1;
    private readonly List<RectangleF> _tabRects = [];

    public event EventHandler<SegmentItem>? SegmentSelected;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = _items.Count == 0 ? -1 : Math.Clamp(value, 0, _items.Count - 1);
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            Invalidate();
        }
    }

    public SegmentItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public SegmentedTabBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Height = 40;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    public void SetItems(IEnumerable<SegmentItem> items, string? activeKey = null)
    {
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = 0;
        if (activeKey is not null)
        {
            var idx = _items.FindIndex(x => string.Equals(x.Key, activeKey, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) _selectedIndex = idx;
        }
        Invalidate();
    }

    public void SelectByKey(string key, bool raiseEvent = false)
    {
        var idx = _items.FindIndex(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        _selectedIndex = idx;
        Invalidate();
        if (raiseEvent) SegmentSelected?.Invoke(this, _items[idx]);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hit = -1;
        for (var i = 0; i < _tabRects.Count; i++)
        {
            if (_tabRects[i].Contains(e.Location))
            {
                hit = i;
                break;
            }
        }
        if (hit != _hoveredIndex)
        {
            _hoveredIndex = hit;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredIndex != -1)
        {
            _hoveredIndex = -1;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        for (var i = 0; i < _tabRects.Count; i++)
        {
            if (_tabRects[i].Contains(e.Location))
            {
                if (_selectedIndex != i)
                {
                    _selectedIndex = i;
                    Invalidate();
                    SegmentSelected?.Invoke(this, _items[i]);
                }
                else
                {
                    SegmentSelected?.Invoke(this, _items[i]);
                }
                break;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        _tabRects.Clear();
        if (_items.Count == 0) return;

        // Outer pill track
        var outerBounds = new RectangleF(0.5f, 1.5f, Math.Max(10, Width - 1), Math.Max(10, Height - 3));
        using (var trackPath = Theme.RoundedRectangle(outerBounds, 9f))
        using (var trackBrush = new SolidBrush(Theme.Primitives.Alabaster200))
        using (var trackBorder = new Pen(Theme.BorderStrong, 1f))
        {
            g.FillPath(trackBrush, trackPath);
            g.DrawPath(trackBorder, trackPath);
        }

        using var font = Theme.Font(8.8f, FontStyle.Bold);
        using var compactFont = Theme.Font(8.1f, FontStyle.Bold);
        using var badgeFont = Theme.Font(7.6f, FontStyle.Bold);

        var availableTrackWidth = Math.Max(40f, Width - 8f);
        var gap = 3f;
        var totalGaps = Math.Max(0, _items.Count - 1) * gap;

        // First pass: measure natural widths and adapt padding/font if track is tight (e.g., 1366x768 @ 150% DPI)
        var activeFont = font;
        var horizPad = 24f;
        var naturalWidths = new float[_items.Count];
        float totalNatural = 0f;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var ts = TextRenderer.MeasureText(g, item.Label, activeFont, new Size(600, 36), TextFormatFlags.NoPadding);
            var iw = item.Icon != IconKind.None ? 20f : 0f;
            var bw = !string.IsNullOrWhiteSpace(item.Badge)
                ? TextRenderer.MeasureText(g, item.Badge, badgeFont, new Size(200, 24), TextFormatFlags.NoPadding).Width + 14f
                : 0f;
            naturalWidths[i] = ts.Width + iw + bw + horizPad;
            totalNatural += naturalWidths[i];
        }

        if (totalNatural + totalGaps > availableTrackWidth)
        {
            activeFont = compactFont;
            horizPad = 16f;
            totalNatural = 0f;
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var ts = TextRenderer.MeasureText(g, item.Label, activeFont, new Size(600, 36), TextFormatFlags.NoPadding);
                var iw = item.Icon != IconKind.None ? 18f : 0f;
                var bw = !string.IsNullOrWhiteSpace(item.Badge)
                    ? TextRenderer.MeasureText(g, item.Badge, badgeFont, new Size(200, 24), TextFormatFlags.NoPadding).Width + 12f
                    : 0f;
                naturalWidths[i] = ts.Width + iw + bw + horizPad;
                totalNatural += naturalWidths[i];
            }
        }

        var scale = (totalNatural + totalGaps > availableTrackWidth && totalNatural > 1f)
            ? Math.Max(0.65f, (availableTrackWidth - totalGaps) / totalNatural)
            : 1f;

        float x = 4f;
        var tabHeight = Math.Max(10f, Height - 9f);
        const float y = 4.5f;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var textSize = TextRenderer.MeasureText(g, item.Label, activeFont, new Size(600, 36), TextFormatFlags.NoPadding);
            var badgeWidth = !string.IsNullOrWhiteSpace(item.Badge)
                ? TextRenderer.MeasureText(g, item.Badge, badgeFont, new Size(200, 24), TextFormatFlags.NoPadding).Width + 13f
                : 0f;

            var tabWidth = Math.Max(54f, naturalWidths[i] * scale);
            var rect = new RectangleF(x, y, tabWidth, tabHeight);
            _tabRects.Add(rect);

            var isSelected = i == _selectedIndex;
            var isHovered = i == _hoveredIndex;

            if (isSelected)
            {
                using var activePath = Theme.RoundedRectangle(rect, 7f);
                using var activeFill = new SolidBrush(Theme.Surface);
                using var activeBorder = new Pen(Theme.Primary, 1.2f);
                g.FillPath(activeFill, activePath);
                g.DrawPath(activeBorder, activePath);
            }
            else if (isHovered)
            {
                using var hoverPath = Theme.RoundedRectangle(rect, 7f);
                using var hoverFill = new SolidBrush(Color.FromArgb(140, Theme.SurfaceWarm));
                g.FillPath(hoverFill, hoverPath);
            }

            var fg = isSelected ? Theme.Primary : (isHovered ? Theme.Text : Theme.TextSecondary);
            var sidePad = Math.Max(6f, horizPad * 0.5f * scale);
            var cursorX = rect.Left + sidePad;

            if (item.Icon != IconKind.None)
            {
                VectorIcons.Draw(g, item.Icon, new RectangleF(cursorX, rect.Top + (rect.Height - 15f) / 2f, 15f, 15f), fg, 1.7f);
                cursorX += 19f;
            }

            var maxTextW = Math.Max(12, (int)(rect.Right - cursorX - badgeWidth - 4f));
            var textRect = new Rectangle((int)cursorX, (int)rect.Top, Math.Min(textSize.Width + 2, maxTextW), (int)rect.Height);
            TextRenderer.DrawText(g, item.Label, activeFont, textRect, fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            cursorX += textRect.Width + 4f;

            if (!string.IsNullOrWhiteSpace(item.Badge) && cursorX + badgeWidth - 6f <= rect.Right)
            {
                var badgeRect = new RectangleF(cursorX, rect.Top + (rect.Height - 18f) / 2f, badgeWidth - 6f, 18f);
                using var bPath = Theme.RoundedRectangle(badgeRect, 8f);
                using var bFill = new SolidBrush(isSelected ? Theme.PrimaryLight : Theme.Primitives.Alabaster100);
                g.FillPath(bFill, bPath);
                TextRenderer.DrawText(g, item.Badge, badgeFont, Rectangle.Round(badgeRect),
                    isSelected ? Theme.Primary : Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            x += tabWidth + gap;
        }
    }
}

/// <summary>
/// Live Key-Value Row for Master-Detail Dossier Inspector Drawers.
/// </summary>
public sealed class DossierFieldList : Panel
{
    private readonly List<(string Label, string Value, Color? ValueColor, bool Highlight)> _rows = [];

    public DossierFieldList()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    public void SetRows(IEnumerable<(string Label, string Value, Color? ValueColor, bool Highlight)> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);
        using var valFont = Theme.Font(8.9f, FontStyle.Bold);
        var rowStep = Math.Max(36, valFont.Height + 16);
        Height = Math.Max(42, _rows.Count * rowStep + 8);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var labelFont = Theme.Font(8.4f, FontStyle.Regular);
        using var valFont = Theme.Font(8.9f, FontStyle.Bold);
        using var linePen = new Pen(Theme.BorderSubtle, 1f);

        var rowH = Math.Max(34, valFont.Height + 14);
        var rowStep = rowH + 2;
        var y = 4;
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];

            if (row.Highlight)
            {
                using var hlPath = Theme.RoundedRectangle(new RectangleF(0, y + 2, Width, rowH - 4), 6f);
                using var hlBrush = new SolidBrush(Theme.PrimaryLight);
                g.FillPath(hlBrush, hlPath);
            }
            else if (i < _rows.Count - 1)
            {
                g.DrawLine(linePen, 0, y + rowH, Width, y + rowH);
            }

            var leftRect = new Rectangle(6, y, (int)(Width * 0.42f), rowH);
            var rightRect = new Rectangle((int)(Width * 0.42f) + 6, y, (int)(Width * 0.58f) - 12, rowH);

            TextRenderer.DrawText(g, row.Label, labelFont, leftRect, Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, string.IsNullOrWhiteSpace(row.Value) ? "—" : row.Value, valFont, rightRect,
                row.ValueColor ?? Theme.Text,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            y += rowStep;
        }
    }
}

public static class Ui
{
    public static SearchInput SearchBox(string placeholder = "Tìm kiếm...") =>
        new(placeholder) { Width = 300, Height = 38, Margin = new Padding(0, 1, 8, 1) };

    public static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Height = 24,
        Dock = DockStyle.Top,
        Font = Theme.Font(8.2f, FontStyle.Bold),
        ForeColor = Theme.TextSecondary,
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static Panel Field(string label, Control input, int width = 260)
    {
        var p = new Panel { Width = width, Height = 68, Margin = new Padding(0, 0, 14, 8), BackColor = Color.Transparent };
        input.Dock = DockStyle.Bottom;
        input.Height = 38;
        p.Controls.Add(input);
        p.Controls.Add(FieldLabel(label));
        return p;
    }

    public static Panel ToolbarButton(Control button)
    {
        button.Margin = Padding.Empty;
        button.Height = 38;
        button.Dock = DockStyle.Bottom;
        var slot = new Panel { Width = button.Width, Height = 64, Margin = new Padding(0, 0, 10, 8), BackColor = Color.Transparent };
        slot.Controls.Add(button);
        return slot;
    }

    public static Panel ToolbarInput(Control input)
    {
        input.Margin = Padding.Empty;
        input.Dock = DockStyle.Bottom;
        input.Height = 38;
        var slot = new Panel { Width = input.Width, Height = 64, Margin = new Padding(0, 0, 10, 8), BackColor = Color.Transparent };
        slot.Controls.Add(input);
        return slot;
    }

    public static Panel ToolbarButtonGroup(params Control[] buttons)
    {
        const int gap = 8;
        var width = buttons.Sum(x => x.Width) + Math.Max(0, buttons.Length - 1) * gap;
        var slot = new Panel { Width = width, Height = 64, Margin = new Padding(0, 0, 10, 8), BackColor = Color.Transparent };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Width = width, Height = 38, WrapContents = false, Padding = Padding.Empty, BackColor = Color.Transparent };
        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            button.Dock = DockStyle.None;
            button.Height = 38;
            button.Margin = new Padding(0, 0, index == buttons.Length - 1 ? 0 : gap, 0);
            bar.Controls.Add(button);
        }
        slot.Controls.Add(bar);
        return slot;
    }

    public static Label ToolbarCaption(string text, int width = 184) => new()
    {
        Text = text,
        AutoSize = false,
        Width = width,
        Height = 64,
        Margin = new Padding(0, 0, 10, 8),
        Font = Theme.Font(8.4f, FontStyle.Bold),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static FlowLayoutPanel Toolbar() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 2, 0, 6)
    };

    public static CardPanel SearchToolbarCard() => new()
    {
        Dock = DockStyle.Top,
        Height = 62,
        Padding = new Padding(12, 10, 12, 10),
        Margin = new Padding(0, 0, 0, 10),
        CornerRadius = 10
    };

    public static FlowLayoutPanel ActionButtonGroup() => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Height = 46,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        Padding = Padding.Empty,
        Margin = Padding.Empty,
        BackColor = Theme.Surface
    };

    public static Control SelectionActionBar(string hint, FlowLayoutPanel actions)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            Padding = new Padding(16, 10, 16, 10),
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeLeft = true,
            AccentStripeThickness = 3
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Theme.Surface };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var copy = new Label
        {
            Dock = DockStyle.Fill,
            Text = hint,
            ForeColor = Theme.Muted,
            Font = Theme.Font(8.9f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(4, 0, 12, 0)
        };

        actions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        foreach (Control action in actions.Controls) action.Margin = new Padding(4);
        actions.ControlAdded += (_, e) =>
        {
            if (e.Control is { } action) action.Margin = new Padding(4);
        };

        layout.Controls.Add(copy, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    public static Control DataHost(DataGridView grid)
    {
        var frame = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            CornerRadius = 10
        };
        grid.Margin = Padding.Empty;
        frame.Controls.Add(grid);

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = Color.Transparent
        };
        host.Controls.Add(frame);
        return host;
    }

    public static Label SectionLabel(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        AutoSize = false,
        Height = 24,
        Width = 220,
        Margin = new Padding(8, 12, 0, 4),
        Font = Theme.Font(7.6f, FontStyle.Bold),
        ForeColor = Theme.SidebarMuted,
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static void Error(IWin32Window owner, string message) =>
        MessageBox.Show(owner, message, "Không thể thực hiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Info(IWin32Window owner, string message) =>
        MessageBox.Show(owner, message, "StudioManager — Atelier Lumière", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool IsDatabaseException(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is Microsoft.Data.SqlClient.SqlException ||
                string.Equals(cur.GetType().Name, "SqlException", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public static string FormatRuntimeError(string stage, Exception ex)
    {
        if (IsDatabaseException(ex))
        {
            return $"Không thể kết nối hoặc truy vấn SQL Server tại bước [{stage}] ({ex.GetType().Name}): {ex.Message}";
        }

        return $"Lỗi tại bước [{stage}] ({ex.GetType().Name}): {ex.Message}";
    }

    public static void LogError(string stage, Exception ex)
    {
        try
        {
            var entry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] STAGE: {stage}{Environment.NewLine}" +
                $"Exception: {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}" +
                (ex.InnerException is not null
                    ? $"InnerException: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}{Environment.NewLine}"
                    : string.Empty) +
                $"StackTrace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}" +
                new string('-', 72) + Environment.NewLine;

            System.Diagnostics.Debug.WriteLine(entry);

            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(Path.Combine(logDir, "ui-runtime.log"), entry);
        }
        catch
        {
            // Never allow diagnostic logging failure to interrupt UI recovery.
        }
    }

    public static void ExportGrid(DataGridView grid, IWin32Window owner, string defaultName)
    {
        if (grid.Columns.Count == 0) { Error(owner, "Không có dữ liệu để xuất."); return; }
        var visible = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).OrderBy(x => x.DisplayIndex).ToList();
        if (visible.Count == 0) { Error(owner, "Không có cột hiển thị để xuất."); return; }
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook Unicode (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
            FilterIndex = 1,
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = defaultName + "_" + timestamp + ".xlsx"
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
        if (string.Equals(Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            ExportCsv(dialog.FileName, grid, visible);
            Info(owner, "Đã xuất CSV UTF-8 thành công.");
            return;
        }
        ExportWorkbook(dialog.FileName, grid, visible);
        Info(owner, "Đã xuất file Excel Unicode thành công.");
    }

    private static void ExportCsv(string path, DataGridView grid, IReadOnlyList<DataGridViewColumn> visible)
    {
        using var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
        var separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        writer.WriteLine("sep=" + separator);
        writer.WriteLine(string.Join(separator, visible.Select(x => Csv(x.HeaderText))));
        foreach (DataGridViewRow row in grid.Rows.Cast<DataGridViewRow>().Where(x => !x.IsNewRow))
            writer.WriteLine(string.Join(separator, visible.Select(x => Csv(row.Cells[x.Index].FormattedValue?.ToString() ?? ""))));
    }

    private static void ExportWorkbook(string path, DataGridView grid, IReadOnlyList<DataGridViewColumn> visible)
    {
        var rows = new List<IReadOnlyList<string>> { visible.Select(x => x.HeaderText).ToArray() };
        rows.AddRange(grid.Rows.Cast<DataGridViewRow>().Where(x => !x.IsNewRow)
            .Select(row => (IReadOnlyList<string>)visible.Select(column => row.Cells[column.Index].FormattedValue?.ToString() ?? string.Empty).ToArray()));

        using var archive = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
        WriteArchiveEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
        WriteArchiveEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        WriteArchiveEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Dữ liệu\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
        WriteArchiveEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
        WriteArchiveEntry(archive, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Times New Roman\"/></font><font><b/><sz val=\"11\"/><name val=\"Times New Roman\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs></styleSheet>");

        var sheet = new System.Text.StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sheet.Append("<cols>");
        for (var column = 0; column < visible.Count; column++)
        {
            var longest = rows.Max(row => row.Count > column ? row[column].Length : 0);
            sheet.Append($"<col min=\"{column + 1}\" max=\"{column + 1}\" width=\"{Math.Clamp(longest + 2, 12, 42)}\" customWidth=\"1\"/>");
        }
        sheet.Append("</cols><sheetData>");
        for (var row = 0; row < rows.Count; row++)
        {
            sheet.Append($"<row r=\"{row + 1}\">");
            for (var column = 0; column < visible.Count; column++)
                sheet.Append($"<c r=\"{ExcelColumnName(column + 1)}{row + 1}\" t=\"inlineStr\" s=\"{(row == 0 ? 1 : 0)}\"><is><t xml:space=\"preserve\">{Xml(rows[row][column])}</t></is></c>");
            sheet.Append("</row>");
        }
        sheet.Append("</sheetData><autoFilter ref=\"A1:").Append(ExcelColumnName(visible.Count)).Append(rows.Count).Append("\"/></worksheet>");
        WriteArchiveEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
    }

    private static void WriteArchiveEntry(System.IO.Compression.ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ExcelColumnName(int number)
    {
        var name = string.Empty;
        while (number > 0) { number--; name = (char)('A' + number % 26) + name; number /= 26; }
        return name;
    }

    private static string Xml(string value) => System.Security.SecurityElement.Escape(value.Replace("\0", string.Empty)) ?? string.Empty;
    private static string Csv(string value)
    {
        var trimmed = value.TrimStart();
        var safe = trimmed.Length > 0 && "=+-@".Contains(trimmed[0]) ? "'" + value : value;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}

/// <summary>
/// Pure Vector GDI+ Search Input — Zero emojis, warm Alabaster surface, Terracotta focus border.
/// </summary>
public sealed class SearchInput : UserControl
{
    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(9.4f)
    };

    private bool _clearHovered;

    public SearchInput(string placeholder)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Primitives.Alabaster50;
        Cursor = Cursors.IBeam;
        MinimumSize = new Size(180, 38);
        Height = 38;

        _input.PlaceholderText = placeholder;
        _input.TextChanged += (_, _) =>
        {
            LayoutInnerControls();
            Invalidate();
            OnTextChanged(EventArgs.Empty);
        };
        _input.KeyDown += (_, e) => OnKeyDown(e);
        _input.KeyPress += (_, e) => OnKeyPress(e);
        _input.KeyUp += (_, e) => OnKeyUp(e);
        _input.GotFocus += (_, _) => Invalidate();
        _input.LostFocus += (_, _) => Invalidate();

        Controls.Add(_input);
        Resize += (_, _) => LayoutInnerControls();
        LayoutInnerControls();
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text { get => _input.Text; set => _input.Text = value ?? string.Empty; }
    public string PlaceholderText { get => _input.PlaceholderText; set => _input.PlaceholderText = value; }
    public new event KeyEventHandler? KeyDown { add => _input.KeyDown += value; remove => _input.KeyDown -= value; }
    public new event KeyPressEventHandler? KeyPress { add => _input.KeyPress += value; remove => _input.KeyPress -= value; }
    public new event KeyEventHandler? KeyUp { add => _input.KeyUp += value; remove => _input.KeyUp -= value; }
    public void Clear() => _input.Clear();
    public override void Refresh() { _input.Refresh(); base.Refresh(); }

    private Rectangle ClearButtonBounds => new(Width - 30, (Height - 22) / 2, 22, 22);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hasText = !string.IsNullOrEmpty(_input.Text);
        var overClear = hasText && ClearButtonBounds.Contains(e.Location);
        Cursor = overClear ? Cursors.Hand : Cursors.IBeam;
        if (overClear != _clearHovered)
        {
            _clearHovered = overClear;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_clearHovered)
        {
            _clearHovered = false;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!string.IsNullOrEmpty(_input.Text) && ClearButtonBounds.Contains(e.Location))
        {
            _input.Clear();
            _input.Focus();
            return;
        }
        _input.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, 8f);
        using var fill = new SolidBrush(BackColor);
        using var pen = new Pen(_input.Focused ? Theme.Primary : Theme.BorderStrong, _input.Focused ? 1.6f : 1f);

        g.FillPath(fill, path);
        g.DrawPath(pen, path);

        // Vector Search Icon
        var iconColor = _input.Focused ? Theme.Primary : Theme.Muted;
        VectorIcons.Draw(g, IconKind.Search, new RectangleF(11, (Height - 15) / 2f, 15, 15), iconColor, 1.75f);

        // Vector Clear Button
        if (!string.IsNullOrEmpty(_input.Text))
        {
            var cb = ClearButtonBounds;
            if (_clearHovered)
            {
                using var hoverBrush = new SolidBrush(Theme.Primitives.Alabaster200);
                g.FillEllipse(hoverBrush, cb);
            }
            VectorIcons.Draw(g, IconKind.Close, new RectangleF(cb.Left + 5, cb.Top + 5, 12, 12),
                _clearHovered ? Theme.Danger : Theme.Muted, 1.75f);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) =>
        e.Graphics.Clear(Parent?.BackColor is { A: byte.MaxValue } color ? color : Theme.Background);

    private void LayoutInnerControls()
    {
        var rightGutter = !string.IsNullOrEmpty(_input.Text) ? 34 : 10;
        _input.Bounds = new Rectangle(34, Math.Max(0, (Height - _input.PreferredHeight) / 2),
            Math.Max(20, Width - 34 - rightGutter), _input.PreferredHeight);
    }
}
