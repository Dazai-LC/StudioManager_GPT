using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms;

public enum ButtonVariant
{
    Primary,
    Secondary,
    Outline,
    Success,
    Warning,
    Danger,
    Ghost
}

public static class Theme
{
    // --- PALETTE TOKENS (Sophisticated Studio Neutral + Indigo & Amber Accents) ---
    public static readonly Color Background = Color.FromArgb(248, 250, 252);       // Slate 50
    public static readonly Color BackgroundSubtle = Color.FromArgb(241, 245, 249); // Slate 100
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceElevated = Color.White;
    public static readonly Color SurfaceHover = Color.FromArgb(248, 250, 252);

    // Sidebar - Obsidian Slate with subtle blue-gray depth
    public static readonly Color Sidebar = Color.FromArgb(15, 23, 42);             // Slate 900
    public static readonly Color SidebarCard = Color.FromArgb(24, 34, 58);
    public static readonly Color SidebarHover = Color.FromArgb(30, 41, 59);        // Slate 800
    public static readonly Color SidebarActive = Color.FromArgb(37, 99, 235);      // Blue 600
    public static readonly Color SidebarActiveBg = Color.FromArgb(30, 58, 110);
    public static readonly Color SidebarText = Color.FromArgb(203, 213, 225);      // Slate 300
    public static readonly Color SidebarMuted = Color.FromArgb(148, 163, 184);     // Slate 400

    // Primary Brand - Studio Sapphire / Royal Indigo
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);            // Blue 600
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);       // Blue 700
    public static readonly Color PrimaryPressed = Color.FromArgb(30, 64, 175);     // Blue 800
    public static readonly Color PrimaryLight = Color.FromArgb(239, 246, 255);     // Blue 50
    public static readonly Color PrimaryLightBorder = Color.FromArgb(191, 219, 254);// Blue 200

    // Secondary / Neutral Accents
    public static readonly Color Secondary = Color.FromArgb(71, 85, 105);          // Slate 600
    public static readonly Color SecondaryHover = Color.FromArgb(51, 65, 85);
    public static readonly Color SecondaryLight = Color.FromArgb(241, 245, 249);

    // Typography Colors
    public static readonly Color Text = Color.FromArgb(15, 23, 42);                // Slate 900 (High contrast)
    public static readonly Color TextSecondary = Color.FromArgb(51, 65, 85);       // Slate 700
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);            // Slate 500
    public static readonly Color TextLight = Color.FromArgb(148, 163, 184);        // Slate 400

    // Borders & Dividers
    public static readonly Color Border = Color.FromArgb(226, 232, 240);           // Slate 200
    public static readonly Color BorderSubtle = Color.FromArgb(241, 245, 249);
    public static readonly Color BorderStrong = Color.FromArgb(203, 213, 225);     // Slate 300
    public static readonly Color BorderFocus = Color.FromArgb(37, 99, 235);

    // Functional & Semantic States
    public static readonly Color Success = Color.FromArgb(16, 185, 129);          // Emerald 500
    public static readonly Color SuccessHover = Color.FromArgb(5, 150, 105);
    public static readonly Color SuccessLight = Color.FromArgb(236, 253, 245);
    public static readonly Color SuccessBorder = Color.FromArgb(167, 243, 208);

    public static readonly Color Warning = Color.FromArgb(245, 158, 11);          // Amber 500
    public static readonly Color WarningHover = Color.FromArgb(217, 119, 6);
    public static readonly Color WarningLight = Color.FromArgb(254, 252, 232);
    public static readonly Color WarningBorder = Color.FromArgb(254, 240, 138);

    public static readonly Color Danger = Color.FromArgb(239, 68, 68);             // Red 500
    public static readonly Color DangerHover = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerLight = Color.FromArgb(254, 242, 242);
    public static readonly Color DangerBorder = Color.FromArgb(254, 202, 202);

    public static readonly Color Excel = Color.FromArgb(22, 101, 52);              // Forest Green
    public static readonly Color Cyan = Color.FromArgb(6, 182, 212);              // Cyan 500
    public static readonly Color CyanLight = Color.FromArgb(236, 254, 255);
    public static readonly Color Purple = Color.FromArgb(139, 92, 246);           // Purple 500
    public static readonly Color PurpleLight = Color.FromArgb(245, 243, 255);
    public static readonly Color Gold = Color.FromArgb(217, 119, 6);              // Warm Studio Gold
    public static readonly Color GoldLight = Color.FromArgb(255, 251, 235);

    public static readonly Color Disabled = Color.FromArgb(241, 245, 249);
    public static readonly Color DisabledText = Color.FromArgb(148, 163, 184);

    // --- TYPOGRAPHY SCALE ---
    private const string FontFamilyName = "Segoe UI";

    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new(FontFamilyName, size, style);
    public static Font DisplayFont => Font(22, FontStyle.Bold);
    public static Font H1Font => Font(18, FontStyle.Bold);
    public static Font H2Font => Font(15, FontStyle.Bold);
    public static Font H3Font => Font(12.5f, FontStyle.Bold);
    public static Font SubtitleFont => Font(10.5f, FontStyle.Regular);
    public static Font BodyFont => Font(9.5f, FontStyle.Regular);
    public static Font BodyBoldFont => Font(9.5f, FontStyle.Bold);
    public static Font CaptionFont => Font(8.5f, FontStyle.Regular);
    public static Font CaptionBoldFont => Font(8.5f, FontStyle.Bold);
    public static Font SmallFont => Font(7.5f, FontStyle.Regular);

    // --- ROUNDING & SHAPE HELPERS ---
    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var safeRadius = Math.Max(1f, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
        var diameter = safeRadius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Round(Control c, int radius = 12)
    {
        if (c.Width <= 1 || c.Height <= 1) return;
        using var p = RoundedRectangle(new RectangleF(0, 0, c.Width, c.Height), radius);
        c.Region = new Region(p);
    }

    // --- BUTTON CREATION ---
    public static ModernButton Button(string text, Color? color = null)
    {
        var activeColor = color ?? Primary;
        var variant = ButtonVariant.Primary;

        if (color.HasValue)
        {
            if (color.Value.ToArgb() == Secondary.ToArgb() || color.Value.ToArgb() == Color.FromArgb(51, 65, 85).ToArgb() || color.Value.ToArgb() == Color.FromArgb(100, 116, 139).ToArgb())
                variant = ButtonVariant.Secondary;
            else if (color.Value.ToArgb() == Danger.ToArgb() || color.Value.ToArgb() == Color.FromArgb(220, 38, 38).ToArgb())
                variant = ButtonVariant.Danger;
            else if (color.Value.ToArgb() == Warning.ToArgb() || color.Value.ToArgb() == Color.FromArgb(217, 119, 6).ToArgb())
                variant = ButtonVariant.Warning;
            else if (color.Value.ToArgb() == Success.ToArgb() || color.Value.ToArgb() == Excel.ToArgb() || color.Value.ToArgb() == Color.FromArgb(22, 163, 74).ToArgb())
                variant = ButtonVariant.Success;
        }

        var b = new ModernButton(activeColor, HoverFor(activeColor), PressedFor(activeColor), variant)
        {
            Text = text,
            AutoSize = false,
            Height = 40,
            Font = Font(9.5f, FontStyle.Bold),
            Padding = new Padding(16, 0, 16, 0),
            Margin = new Padding(4),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false
        };
        b.MinimumSize = new Size(110, 40);
        b.Width = Math.Max(b.MinimumSize.Width, b.RequiredWidth);
        return b;
    }

    public static ModernButton OutlineButton(string text, Color? borderColor = null)
    {
        var col = borderColor ?? BorderStrong;
        var b = new ModernButton(Color.Transparent, Color.FromArgb(241, 245, 249), Color.FromArgb(226, 232, 240), ButtonVariant.Outline)
        {
            Text = text,
            Height = 40,
            Font = Font(9.5f, FontStyle.Bold),
            ForeColor = TextSecondary,
            BorderColor = col,
            Padding = new Padding(14, 0, 14, 0),
            Margin = new Padding(4),
            MinimumSize = new Size(100, 40)
        };
        b.Width = Math.Max(b.MinimumSize.Width, b.RequiredWidth);
        return b;
    }

    internal static Color HoverFor(Color color) => color.ToArgb() switch
    {
        var v when v == Primary.ToArgb() => PrimaryHover,
        var v when v == Secondary.ToArgb() => SecondaryHover,
        var v when v == Excel.ToArgb() => Color.FromArgb(19, 84, 43),
        var v when v == Danger.ToArgb() => DangerHover,
        var v when v == Warning.ToArgb() => WarningHover,
        var v when v == Success.ToArgb() => SuccessHover,
        _ => Darken(color, 0.88f)
    };

    internal static Color PressedFor(Color color) => Darken(HoverFor(color), 0.90f);
    private static Color Darken(Color color, float factor) => Color.FromArgb(color.A, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));

    // --- GRID CREATION & STYLING ---
    public static DataGridView Grid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Surface,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = Font(9.5f),
            RowTemplate = { Height = 44 },
            EnableHeadersVisualStyles = false,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            GridColor = Color.FromArgb(241, 245, 249)
        };

        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Secondary,
            Font = Font(9.0f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = Color.FromArgb(248, 250, 252),
            SelectionForeColor = Secondary,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        g.ColumnHeadersHeight = 44;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = Text,
            SelectionBackColor = Color.FromArgb(239, 246, 255), // Gentle light blue selection
            SelectionForeColor = PrimaryHover,
            Padding = new Padding(12, 6, 12, 6),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(252, 253, 255),
            ForeColor = Text,
            SelectionBackColor = Color.FromArgb(239, 246, 255),
            SelectionForeColor = PrimaryHover
        };

        // Tooltip on hover for truncated content
        g.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                e.ToolTipText = g.Rows[e.RowIndex].Cells[e.ColumnIndex].FormattedValue?.ToString() ?? string.Empty;
        };

        return g;
    }

    // --- STATUS COLOR MAPPINGS ---
    public static (Color Text, Color Background, Color Border) GetStatusColors(string status)
    {
        return status switch
        {
            "Đã đặt lịch" or "DA_DAT_LICH" => (Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255), Color.FromArgb(191, 219, 254)),
            "Đã chụp" or "DA_CHUP" => (Color.FromArgb(124, 58, 237), Color.FromArgb(245, 243, 255), Color.FromArgb(221, 214, 254)),
            "Đang chỉnh sửa ảnh" or "DANG_CHINH_SUA" or "DANG_CHINH_SUA_ANH" => (Color.FromArgb(217, 119, 6), Color.FromArgb(254, 252, 232), Color.FromArgb(254, 240, 138)),
            "Chờ giao ảnh" or "CHO_GIAO_ANH" => (Color.FromArgb(14, 116, 144), Color.FromArgb(236, 254, 255), Color.FromArgb(165, 243, 252)),
            "Đã giao ảnh" or "DA_GIAO_ANH" => (Color.FromArgb(13, 148, 136), Color.FromArgb(240, 253, 250), Color.FromArgb(153, 246, 228)),
            "Hoàn thành" or "HOAN_THANH" or "HOAT_DONG" or "DANG_LAM" or "DANG_AP_DUNG" or "DANG_CUNG_CAP" => (Color.FromArgb(16, 185, 129), Color.FromArgb(236, 253, 245), Color.FromArgb(167, 243, 208)),
            "Đã hủy" or "DA_HUY" or "KHOA" or "NGUNG_LAM" or "NGUNG_AP_DUNG" or "NGUNG_CUNG_CAP" or "NGUNG_SU_DUNG" => (Color.FromArgb(239, 68, 68), Color.FromArgb(254, 242, 242), Color.FromArgb(254, 202, 202)),
            _ => (Secondary, Color.FromArgb(241, 245, 249), Color.FromArgb(226, 232, 240))
        };
    }
}

// Fully owner-drawn modern button with anti-aliasing, rounded corners, and variants
public sealed class ModernButton : Control, IButtonControl
{
    private const float CornerRadius = 9f;
    private Color _normal;
    private Color _hover;
    private Color _pressed;
    private bool _isHovering;
    private bool _isPressed;
    private bool _isDefault;
    private DialogResult _dialogResult;
    private ButtonVariant _variant = ButtonVariant.Primary;

    public ButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    public Color BorderColor { get; set; } = Theme.BorderStrong;
    public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
    public bool AutoEllipsis { get; set; }

    public ModernButton(Color normal, Color hover, Color pressed, ButtonVariant variant = ButtonVariant.Primary)
    {
        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        _variant = variant;

        BackColor = normal;
        ForeColor = variant switch
        {
            ButtonVariant.Secondary => Color.White,
            ButtonVariant.Outline or ButtonVariant.Ghost => Theme.TextSecondary,
            _ => Color.White
        };

        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable | ControlStyles.StandardClick, true);
    }

    public ModernButton() : this(Theme.Primary, Theme.PrimaryHover, Theme.PrimaryPressed) { }

    public int RequiredWidth => Math.Max(100, TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + Padding.Horizontal + 28);

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); EnsureTextFits(); }
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); EnsureTextFits(); }
    protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); EnsureTextFits(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
    protected override void OnBackColorChanged(EventArgs e)
    {
        if (!BackColor.IsEmpty)
        {
            _normal = BackColor;
            _hover = Theme.HoverFor(_normal);
            _pressed = Theme.PressedFor(_normal);
        }
        base.OnBackColorChanged(e);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovering = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovering = false; _isPressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _isPressed = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _isPressed = false; Invalidate(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Enabled && e.KeyCode is Keys.Space or Keys.Enter) { _isPressed = true; Invalidate(); e.Handled = true; }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (Enabled && e.KeyCode is Keys.Space or Keys.Enter)
        {
            _isPressed = false;
            Invalidate();
            PerformClick();
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
    {
        if ((specified & BoundsSpecified.Width) != 0) width = Math.Max(width, RequiredWidth);
        if ((specified & BoundsSpecified.Height) != 0) height = Math.Max(height, 38);
        base.SetBoundsCore(x, y, width, height, specified);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, CornerRadius);

        Color fill;
        Color textCol;
        Color borderCol = Color.Transparent;

        if (!Enabled)
        {
            fill = Theme.Disabled;
            textCol = Theme.DisabledText;
            if (_variant == ButtonVariant.Outline) borderCol = Theme.Border;
        }
        else if (_variant == ButtonVariant.Outline)
        {
            fill = _isPressed ? Color.FromArgb(226, 232, 240) : _isHovering ? Color.FromArgb(248, 250, 252) : Color.Transparent;
            textCol = _isHovering ? Theme.Text : ForeColor;
            borderCol = _isHovering ? Theme.BorderStrong : BorderColor;
        }
        else if (_variant == ButtonVariant.Ghost)
        {
            fill = _isPressed ? Color.FromArgb(226, 232, 240) : _isHovering ? Color.FromArgb(241, 245, 249) : Color.Transparent;
            textCol = _isHovering ? Theme.Primary : ForeColor;
        }
        else
        {
            fill = _isPressed ? _pressed : _isHovering ? _hover : _normal;
            textCol = ForeColor;
        }

        // Fill background
        if (fill != Color.Transparent)
        {
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);
        }

        // Draw border if outline or focused
        if (borderCol != Color.Transparent)
        {
            using var pen = new Pen(borderCol, 1.2f);
            g.DrawPath(pen, path);
        }

        if (Focused || _isDefault)
        {
            using var focusPen = new Pen(Color.FromArgb(160, Theme.Primary), 1.5f);
            g.DrawPath(focusPen, path);
        }

        // Draw label
        var textBounds = Rectangle.FromLTRB(Padding.Left, 0, Math.Max(Padding.Left, Width - Padding.Right), Height);
        TextRenderer.DrawText(g, Text, Font, textBounds, textCol,
            TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void EnsureTextFits()
    {
        if (Width < RequiredWidth) Width = RequiredWidth;
        if (Height < 38) Height = 38;
    }

    public DialogResult DialogResult { get => _dialogResult; set => _dialogResult = value; }
    public void NotifyDefault(bool value) { _isDefault = value; Invalidate(); }
    public void PerformClick()
    {
        if (Enabled) OnClick(EventArgs.Empty);
    }
}
