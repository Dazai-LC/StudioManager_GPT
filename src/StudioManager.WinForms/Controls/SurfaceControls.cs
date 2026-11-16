using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms.Controls;

public sealed class PageHeader : Panel
{
    public FlowLayoutPanel Actions { get; } = new()
    {
        Dock = DockStyle.Right,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        AutoScroll = false,
        Padding = new Padding(8, 6, 0, 0),
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
            UseMnemonic = false,
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
            UseMnemonic = false,
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
