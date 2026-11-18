using System.Drawing.Drawing2D;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;

namespace StudioManager.CustomerDesktop;

/// <summary>Customer-only presentation components. Layout/paint only: no session, API or data access.</summary>
internal static class CustomerUi
{
    internal static int S(Control control, int value) => (int)Math.Round(value * Math.Max(96, control.DeviceDpi) / 96.0);

    internal static GraphicsPath Round(Rectangle rect, int radius)
    {
        var d = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }

    internal static Label Text(string text, float size = 9.5f, bool bold = false, Color? color = null, int top = 0, int bottom = 4) => new()
    {
        Text = text, AutoSize = true, UseMnemonic = false, BackColor = Color.Empty,
        Font = Theme.Font(size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color ?? Theme.TextSecondary,
        Margin = new Padding(0, top, 0, bottom)
    };

    internal static (string Text, Color Fore, Color Back) StateStyle(string state) => state switch
    {
        "PENDING" => ("Chờ nhân viên duyệt", Theme.Warning, Theme.WarningLight),
        "APPROVED" => ("Đã duyệt · có lịch chính thức", Theme.Success, Theme.SuccessLight),
        "REJECTED" => ("Không được duyệt", Theme.Danger, Theme.DangerLight),
        "CANCELLED" => ("Đã rút yêu cầu", Theme.Muted, Theme.BackgroundSubtle),
        _ => (state, Theme.Muted, Theme.BackgroundSubtle)
    };

    internal static Chip StateChip(string state)
    {
        var (text, fore, back) = StateStyle(state); return new Chip(text, fore, back);
    }

    internal static FlowLayoutPanel Row(bool wrap = true) => new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = wrap, FlowDirection = FlowDirection.LeftToRight,
        BackColor = Color.Empty, Margin = new Padding(0, 4, 0, 4), Padding = Padding.Empty
    };
}

/// <summary>Rounded status pill.</summary>
internal sealed class Chip : Control
{
    private Color _fore, _back;
    public Chip(string text, Color fore, Color back)
    {
        Text = text; _fore = fore; _back = back; Font = Theme.Font(8.5f, FontStyle.Bold); Margin = new Padding(0, 2, 8, 2);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Empty; Size = GetPreferredSize(Size.Empty);
    }
    public void UpdateStyle(string text, Color fore, Color back)
    {
        Text = text; _fore = fore; _back = back; Size = GetPreferredSize(Size.Empty); Invalidate();
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        var size = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        return new Size(size.Width + 24, size.Height + 10);
    }
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Size = GetPreferredSize(Size.Empty); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = CustomerUi.Round(new Rectangle(0, 0, Width - 1, Height - 1), Math.Max(6, Height / 2 - 1)))
        using (var fill = new SolidBrush(_back)) e.Graphics.FillPath(fill, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, _fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}

/// <summary>Surface card whose height follows its wrapped content for the width it is given.</summary>
internal class Card : CardPanel
{
    private bool _fitting;
    private int _lastFitWidth = -1;
    private int _lastFitChildCount = -1;
    public FlowLayoutPanel Body { get; }
    public Card(bool dark = false)
    {
        if (dark)
        {
            BackColor = Theme.Primitives.Espresso800;
            BorderColor = Theme.Primitives.Stone500;
            AccentStripeColor = Theme.Primitives.Terracotta500;
            AccentStripeThickness = 3;
            AccentStripeLeft = true;
        }
        else
        {
            BackColor = Theme.Surface;
            BorderColor = Theme.Border;
        }
        CornerRadius = 12; Padding = new Padding(20); Margin = new Padding(0, 0, 0, 14);
        Body = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Empty, Margin = Padding.Empty };
        Controls.Add(Body);
    }
    public void Add(Control control) { Body.Controls.Add(control); FitContent(true); }
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); FitContent(false); }
    public void FitContent(bool force = false)
    {
        if (_fitting || Width < 60 || IsDisposed) return;
        var width = Width - Padding.Horizontal;
        if (!force && width == _lastFitWidth && Body.Controls.Count == _lastFitChildCount) return;
        _fitting = true;
        try
        {
            Body.Location = new Point(Padding.Left, Padding.Top); Body.Width = width;
            foreach (Control child in Body.Controls)
            {
                var available = Math.Max(40, width - child.Margin.Horizontal);
                if (child is Label { AutoSize: true } label) label.MaximumSize = new Size(available, 0);
                else if (child is Label fixedLabel) fixedLabel.Width = available;
                else if (child is FlowLayoutPanel row) { row.MaximumSize = new Size(available, 0); }
                else if (child.Tag as string == "fill") child.Width = available;
            }
            Body.Height = Body.GetPreferredSize(new Size(width, 0)).Height;
            var height = Body.Height + Padding.Vertical;
            if (Height != height) Height = height;
            _lastFitWidth = width;
            _lastFitChildCount = Body.Controls.Count;
        }
        finally { _fitting = false; }
    }
}

/// <summary>Scrolling vertical page whose children always match the visible width (no horizontal overflow).</summary>
internal sealed class PageFlow : FlowLayoutPanel
{
    private bool _sizing;
    public PageFlow()
    {
        Dock = DockStyle.Fill; AutoScroll = true; WrapContents = false; FlowDirection = FlowDirection.TopDown;
        BackColor = Theme.Background; Padding = new Padding(24, 18, 24, 18); DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }
    public void Clear()
    {
        SuspendLayout();
        foreach (var child in Controls.Cast<Control>().ToArray()) { Controls.Remove(child); child.Dispose(); }
        ResumeLayout(true);
    }
    protected override void OnLayout(LayoutEventArgs levent)
    {
        if (!_sizing)
        {
            _sizing = true;
            try
            {
                var clientW = ClientSize.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4;
                var maxW = Math.Min(clientW, 1080);
                var targetW = Math.Max(240, maxW);
                var leftMargin = Math.Max(0, (clientW - targetW) / 2);
                foreach (Control child in Controls)
                {
                    var available = Math.Max(120, targetW - child.Margin.Horizontal);
                    if (child.Width != available) child.Width = available;
                    if (child.Margin.Left != leftMargin)
                        child.Margin = new Padding(leftMargin, child.Margin.Top, child.Margin.Right, child.Margin.Bottom);

                    if (child is Label label) label.MaximumSize = new Size(available, 0);
                    else if (child is FlowLayoutPanel flow)
                    {
                        flow.MaximumSize = new Size(available, 0);
                        var prefH = flow.GetPreferredSize(new Size(available, 0)).Height;
                        if (flow.Height != prefH) flow.Height = prefH;
                    }
                    else if (child is Card card) card.FitContent(false);
                }
            }
            finally { _sizing = false; }
        }
        base.OnLayout(levent);
    }
}

/// <summary>Cover-cropped photograph with an elegant fallback when no approved asset exists.</summary>
internal sealed class CoverPanel : Control
{
    private readonly Image? _image;
    private readonly string _caption;
    public CoverPanel(Image? image, string caption)
    {
        _image = image; _caption = caption; Margin = new Padding(0, 0, 0, 12);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Tag = "fill"; BackColor = Color.Empty;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 2 || Height <= 2) return;
        var g = e.Graphics; g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CustomerUi.Round(bounds, 8);
        g.SetClip(path);
        if (_image is not null)
        {
            var scale = Math.Max((float)Width / _image.Width, (float)Height / _image.Height);
            var w = Width / scale; var h = Height / scale;
            g.DrawImage(_image, new Rectangle(0, 0, Width, Height), new RectangleF((_image.Width - w) / 2, (_image.Height - h) / 2, w, h), GraphicsUnit.Pixel);
        }
        else
        {
            using var brush = new LinearGradientBrush(bounds, Theme.Primitives.Espresso800, Theme.Primitives.Umber700, 35f);
            g.FillRectangle(brush, bounds);
            var size = Math.Min(Height / 3, 44);
            VectorIcons.Draw(g, IconKind.Aperture, new RectangleF((Width - size) / 2f, Height / 2f - size, size, size), Theme.Primitives.Stone300, 1.8f);
            using var font = Theme.Font(8.5f); using var fore = new SolidBrush(Theme.Primitives.Stone300);
            using var format = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var fallbackText = string.IsNullOrWhiteSpace(_caption) ? "Ảnh đang được cập nhật" : $"Chưa có ảnh riêng · {_caption}";
            g.DrawString(fallbackText, font, fore, new RectangleF(8, Height / 2f + 8, Width - 16, 24), format);
        }
        g.ResetClip();
        using var pen = new Pen(Theme.Border); g.DrawPath(pen, path);
    }
}

/// <summary>Booking step indicator.</summary>
internal sealed class StepBar : Control
{
    private readonly string[] _labels;
    private int _step;
    public StepBar(params string[] labels)
    {
        _labels = labels; Margin = new Padding(0, 0, 0, 12); Tag = "fill"; Height = 74;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Empty;
    }
    public int Step { get => _step; set { _step = value; Invalidate(); } }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var radius = CustomerUi.S(this, 14); var slot = (float)Width / _labels.Length;
        using var labelFont = Theme.Font(8.6f, FontStyle.Bold); using var numberFont = Theme.Font(9f, FontStyle.Bold);
        for (var i = 0; i < _labels.Length; i++)
        {
            var cx = slot * i + slot / 2; var cy = radius + 4;
            if (i > 0)
            {
                using var line = new Pen(i <= _step ? Theme.Primary : Theme.BorderStrong, 2);
                g.DrawLine(line, slot * (i - 1) + slot / 2 + radius + 4, cy, cx - radius - 4, cy);
            }
            var done = i < _step || _step >= _labels.Length; var active = i == _step;
            using (var fill = new SolidBrush(done ? Theme.Success : active ? Theme.Primary : Theme.Surface))
                g.FillEllipse(fill, cx - radius, cy - radius, radius * 2, radius * 2);
            if (!done && !active) { using var pen = new Pen(Theme.BorderStrong, 1.5f); g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2); }
            TextRenderer.DrawText(g, done ? "✓" : (i + 1).ToString(), numberFont, new Rectangle((int)(cx - radius), (int)(cy - radius), radius * 2, radius * 2),
                done || active ? Color.White : Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, _labels[i], labelFont, new Rectangle((int)(slot * i), (int)(cy + radius + 4), (int)slot, 22),
                active ? Theme.Text : Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }
}

/// <summary>Cinematic full-window photographic background for authentication.</summary>
internal sealed class AuthBackdrop : Panel
{
    private static Image? _sharedImage;
    private static readonly object _imageLock = new();
    private Control? _activeCard;

    public AuthBackdrop()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        DoubleBuffered = true;
        BackColor = Theme.Primitives.Espresso900;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    private static Image? GetBackdropImage()
    {
        if (_sharedImage is not null) return _sharedImage;
        lock (_imageLock)
        {
            if (_sharedImage is not null) return _sharedImage;
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "MediaDefaults", "packages", "pkg_GC000001_cover_ai.jpg");
                if (File.Exists(path))
                {
                    using var stream = File.OpenRead(path);
                    _sharedImage = Image.FromStream(stream);
                }
            }
            catch { _sharedImage = null; }
            return _sharedImage;
        }
    }

    public void SetActiveCard(Control card)
    {
        _activeCard = card;
        LayoutCards();
    }

    public void LayoutCards()
    {
        if (IsDisposed) return;
        SuspendLayout();
        try
        {
            foreach (Control card in Controls)
            {
                if (card is not CardPanel) continue;
                var flow = card.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                if (flow is null) continue;

                var innerWidth = card.Width - card.Padding.Horizontal;
                foreach (Control c in flow.Controls)
                {
                    var avail = innerWidth - c.Margin.Horizontal;
                    if (c is Label l) l.MaximumSize = new Size(avail, 0);
                    else if (c is Panel or ModernButton) c.Width = avail;
                }
                var contentH = flow.GetPreferredSize(new Size(innerWidth, 0)).Height + card.Padding.Vertical + 14;
                card.Height = contentH;

                int x;
                if (Width >= 1060)
                {
                    x = Math.Min(Width - card.Width - CustomerUi.S(this, 50), Math.Max(Width * 50 / 100, Width - card.Width - CustomerUi.S(this, 70)));
                }
                else
                {
                    x = Math.Max(20, (Width - card.Width) / 2);
                }

                var y = Math.Max(24, (Height - card.Height) / 2);
                card.Location = new Point(x, y);
            }

            if (_activeCard is not null)
            {
                var bottom = _activeCard.Bottom + 30;
                AutoScrollMinSize = new Size(0, bottom > Height ? bottom : 0);
            }
        }
        finally
        {
            ResumeLayout(true);
            Invalidate();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutCards();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 2 || Height <= 2) return;
        var g = e.Graphics;
        var bounds = ClientRectangle;
        var image = GetBackdropImage();

        if (image is not null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scale = Math.Max((float)Width / image.Width, (float)Height / image.Height);
            var viewW = Width / scale;
            var viewH = Height / scale;
            var srcX = (image.Width - viewW) / 2f;
            var srcY = (image.Height - viewH) / 2f;
            g.DrawImage(image, bounds, new RectangleF(srcX, srcY, viewW, viewH), GraphicsUnit.Pixel);

            using (var scrim = new SolidBrush(Color.FromArgb(85, 22, 20, 18)))
                g.FillRectangle(scrim, bounds);

            using (var hGrad = new LinearGradientBrush(bounds, Color.FromArgb(140, 22, 20, 18), Color.FromArgb(60, 22, 20, 18), 0f))
                g.FillRectangle(hGrad, bounds);

            using (var vGrad = new LinearGradientBrush(bounds, Color.FromArgb(70, 22, 20, 18), Color.FromArgb(150, 22, 20, 18), 90f))
                g.FillRectangle(vGrad, bounds);
        }
        else
        {
            using var flat = new SolidBrush(Theme.Primitives.Espresso900);
            g.FillRectangle(flat, bounds);
        }

        if (_activeCard is not null && _activeCard.Visible)
        {
            var cb = _activeCard.Bounds;
            for (var i = 1; i <= 6; i++)
            {
                var sRect = new Rectangle(cb.X - i, cb.Y - i / 2 + 4, cb.Width + i * 2, cb.Height + i * 2);
                using var sPath = CustomerUi.Round(sRect, 14 + i);
                using var sBrush = new SolidBrush(Color.FromArgb(14 - i * 2, 0, 0, 0));
                g.FillPath(sBrush, sPath);
            }
        }

        if (Width >= 880)
        {
            var padLeft = CustomerUi.S(this, 44);
            var maxTextW = Math.Min(CustomerUi.S(this, 460), Width - (_activeCard?.Width ?? 460) - CustomerUi.S(this, 120));
            if (maxTextW >= 220)
            {
                using var brandFont = Theme.Font(9.5f, FontStyle.Bold);
                using var headFont = Theme.Font(22f, FontStyle.Bold);
                using var bodyFont = Theme.Font(10.5f);

                var eyebrowRect = new Rectangle(padLeft, CustomerUi.S(this, 48), maxTextW, 26);
                TextRenderer.DrawText(g, "ATELIER LUMIÈRE", brandFont, eyebrowRect, Theme.Primitives.Terracotta200, TextFormatFlags.NoPadding);

                var midY = Height / 2 - CustomerUi.S(this, 110);
                if (midY < CustomerUi.S(this, 90)) midY = CustomerUi.S(this, 90);

                var headRect = new Rectangle(padLeft, midY, maxTextW, CustomerUi.S(this, 120));
                TextRenderer.DrawText(g, "Khoảnh khắc của bạn,\nđược kể bằng ánh sáng.", headFont, headRect, Theme.Primitives.Alabaster50,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                var bodyRect = new Rectangle(padLeft, midY + CustomerUi.S(this, 126), maxTextW, CustomerUi.S(this, 80));
                TextRenderer.DrawText(g, "Không gian khách hàng · Chọn gói chụp, gửi yêu cầu đặt lịch và theo dõi hành trình cùng studio.", bodyFont, bodyRect,
                    Theme.Primitives.Alabaster200, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            }
        }
    }
}
