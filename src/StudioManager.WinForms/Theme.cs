using System.Drawing.Drawing2D;

namespace StudioManager.WinForms;

public static class Theme
{
    // The UI reference uses a quiet neutral canvas with a single blue brand accent.
    // Keeping these values central prevents pages from drifting into unrelated styles.
    public static readonly Color Background = Color.FromArgb(241, 245, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(23, 32, 46);
    public static readonly Color SidebarHover = Color.FromArgb(38, 54, 76);
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);
    public static readonly Color PrimaryLight = Color.FromArgb(219, 234, 254);
    public static readonly Color Text = Color.FromArgb(23, 43, 77);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(203, 213, 225);
    public static readonly Color Success = Color.FromArgb(22, 163, 74);
    public static readonly Color Excel = Color.FromArgb(33, 115, 70);
    public static readonly Color Warning = Color.FromArgb(217, 119, 6);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color Cyan = Color.FromArgb(14, 116, 144);
    public static readonly Color Purple = Color.FromArgb(109, 40, 217);
    public static readonly Color Secondary = Color.FromArgb(71, 85, 105);
    public static readonly Color Disabled = Color.FromArgb(226, 232, 240);
    public static readonly Color DisabledText = Color.FromArgb(148, 163, 184);

    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
    public static void Round(Control c, int radius = 12)
    {
        if (c.Width <= 1 || c.Height <= 1) return;
        using var p = new GraphicsPath(); var r = new Rectangle(0, 0, c.Width - 1, c.Height - 1); var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
        c.Region = new Region(p);
    }
    public static Button Button(string text, Color? color = null)
    {
        var activeColor = color ?? Primary;
        // A custom-painted button avoids the aliased Region clipping that native
        // WinForms buttons show at rounded corners.  It also calculates width from
        // the label, so callers cannot accidentally cut Vietnamese button text.
        var b = new ModernButton(activeColor, HoverFor(activeColor), PressedFor(activeColor)) { Text = text, AutoSize = false, Height = 42, Font = Font(9.7f, FontStyle.Bold), Padding = new Padding(16, 0, 16, 0), Margin = new Padding(4), TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = false };
        b.MinimumSize = new Size(116, 42);
        b.Width = Math.Max(b.MinimumSize.Width, b.RequiredWidth);
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    internal static Color HoverFor(Color color) => color.ToArgb() switch
    {
        var value when value == Primary.ToArgb() => PrimaryHover,
        var value when value == Secondary.ToArgb() => Color.FromArgb(51, 65, 85),
        var value when value == Excel.ToArgb() => Color.FromArgb(24, 92, 55),
        var value when value == Danger.ToArgb() => Color.FromArgb(185, 28, 28),
        _ => Darken(color, .88f)
    };

    internal static Color PressedFor(Color color) => Darken(HoverFor(color), .9f);
    private static Color Darken(Color color, float factor) => Color.FromArgb(color.A, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));
    public static DataGridView Grid()
    {
        var g = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Surface, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, Font = Font(9.5f), RowTemplate = { Height = 40 }, EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single };
        g.ColumnHeadersDefaultCellStyle = new() { BackColor = Color.FromArgb(248, 250, 252), ForeColor = Muted, Font = Font(8.8f, FontStyle.Bold), Padding = new Padding(10, 2, 10, 2), SelectionBackColor = Color.FromArgb(248, 250, 252), SelectionForeColor = Muted };
        g.ColumnHeadersHeight = 42; g.DefaultCellStyle = new() { BackColor = Surface, ForeColor = Text, SelectionBackColor = PrimaryLight, SelectionForeColor = Text, Padding = new Padding(10, 5, 10, 5) }; g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252) }; g.GridColor = Border;
        // A narrow desktop column must not make real business data inaccessible.
        // The grid keeps its compact layout and reveals the complete value on hover.
        g.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                e.ToolTipText = g.Rows[e.RowIndex].Cells[e.ColumnIndex].FormattedValue?.ToString() ?? string.Empty;
        };
        return g;
    }
}

internal sealed class ModernButton : Button
{
    private Color _normal;
    private Color _hover;
    private Color _pressed;
    private bool _isHovering;
    private bool _isPressed;

    public ModernButton(Color normal, Color hover, Color pressed)
    {
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        BackColor = normal;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public int RequiredWidth => Math.Max(116, TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + Padding.Horizontal + 26);

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); EnsureTextFits(); }
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); EnsureTextFits(); }
    protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); EnsureTextFits(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (BackColor.IsEmpty) return;
        _normal = BackColor;
        _hover = Theme.HoverFor(_normal);
        _pressed = Theme.PressedFor(_normal);
        Invalidate();
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovering = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovering = false; _isPressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _isPressed = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _isPressed = false; Invalidate(); }

    protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
    {
        if ((specified & BoundsSpecified.Width) != 0) width = Math.Max(width, RequiredWidth);
        if ((specified & BoundsSpecified.Height) != 0) height = Math.Max(height, 42);
        base.SetBoundsCore(x, y, width, height, specified);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var bounds = new RectangleF(.5f, .5f, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        var fill = !Enabled ? Theme.Disabled : _isPressed ? _pressed : _isHovering ? _hover : _normal;
        var foreColor = Enabled ? Color.White : Theme.DisabledText;
        using var path = RoundedPath(bounds, 10f);
        using var brush = new SolidBrush(fill);
        e.Graphics.FillPath(brush, path);
        if (Focused && ShowFocusCues)
        {
            using var focusPen = new Pen(Color.FromArgb(150, Theme.Primary), 1.5f);
            e.Graphics.DrawPath(focusPen, path);
        }
        var textBounds = Rectangle.FromLTRB(Padding.Left, 0, Math.Max(Padding.Left, Width - Padding.Right), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, foreColor, TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void EnsureTextFits()
    {
        if (Width < RequiredWidth) Width = RequiredWidth;
        if (Height < 42) Height = 42;
    }

    private static GraphicsPath RoundedPath(RectangleF bounds, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
