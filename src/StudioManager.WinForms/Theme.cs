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

public enum IconKind
{
    None,
    Aperture,
    Dashboard,
    Calendar,
    Users,
    UserCheck,
    Layers,
    Sparkles,
    Home,
    Camera,
    ChartBar,
    Wallet,
    Shield,
    History,
    Database,
    Search,
    Filter,
    Plus,
    Edit,
    Trash,
    Check,
    Close,
    Eye,
    EyeOff,
    Lock,
    Unlock,
    Key,
    Refresh,
    Download,
    Printer,
    ArrowRight,
    Clock,
    AlertTriangle,
    Info,
    LogOut,
    Command,
    RoleAdmin,
    RoleStaff,
    RolePhotographer,
    RoleCustomer
}

/// <summary>
/// PROJECT PHOENIX — "ATELIER LUMIÈRE" THREE-LAYER DESIGN SYSTEM
/// Layer 1: Primitive Tokens (Raw Palette & Scale)
/// Layer 2: Semantic Tokens (Purpose-based surfaces, ink, accents, states)
/// Layer 3: Component Tokens & Factory Helpers
/// </summary>
public static class Theme
{
    // =========================================================================
    // LAYER 1: PRIMITIVE TOKENS (Warm Gallery Stone, Espresso Ink, Terracotta)
    // =========================================================================
    public static class Primitives
    {
        public static readonly Color Alabaster50 = Color.FromArgb(250, 248, 245);   // #FAF8F5
        public static readonly Color Alabaster100 = Color.FromArgb(244, 241, 234);  // #F4F1EA
        public static readonly Color Alabaster200 = Color.FromArgb(233, 228, 218);  // #E9E4DA
        public static readonly Color Stone300 = Color.FromArgb(214, 208, 196);      // #D6D0C4
        public static readonly Color Stone400 = Color.FromArgb(168, 160, 148);      // #A8A094
        public static readonly Color Stone500 = Color.FromArgb(124, 116, 105);      // #7C7469
        public static readonly Color Umber700 = Color.FromArgb(74, 68, 61);         // #4A443D
        public static readonly Color Espresso800 = Color.FromArgb(36, 33, 30);      // #24211E
        public static readonly Color Espresso900 = Color.FromArgb(22, 20, 18);      // #161412

        // Brand Accent: Leica Terracotta / Burnt Sienna
        public static readonly Color Terracotta50 = Color.FromArgb(253, 244, 240);  // #FDF4F0
        public static readonly Color Terracotta100 = Color.FromArgb(250, 225, 214); // #FAE1D6
        public static readonly Color Terracotta200 = Color.FromArgb(243, 193, 172); // #F3C1AC
        public static readonly Color Terracotta500 = Color.FromArgb(209, 96, 56);   // #D16038
        public static readonly Color Terracotta600 = Color.FromArgb(192, 78, 38);   // #C04E26
        public static readonly Color Terracotta700 = Color.FromArgb(161, 61, 27);   // #A13D1B
        public static readonly Color Terracotta800 = Color.FromArgb(130, 47, 20);   // #822F14

        // Semantic Accents: Botanical Sage, Champagne Ochre, Teal, Plum, Crimson
        public static readonly Color Sage50 = Color.FromArgb(238, 247, 242);
        public static readonly Color Sage200 = Color.FromArgb(183, 224, 202);
        public static readonly Color Sage600 = Color.FromArgb(43, 114, 80);
        public static readonly Color Sage700 = Color.FromArgb(31, 89, 61);

        public static readonly Color Ochre50 = Color.FromArgb(254, 249, 235);
        public static readonly Color Ochre200 = Color.FromArgb(249, 224, 162);
        public static readonly Color Ochre600 = Color.FromArgb(184, 110, 14);
        public static readonly Color Ochre700 = Color.FromArgb(148, 86, 8);

        public static readonly Color Crimson50 = Color.FromArgb(254, 242, 242);
        public static readonly Color Crimson200 = Color.FromArgb(252, 202, 202);
        public static readonly Color Crimson600 = Color.FromArgb(196, 43, 43);
        public static readonly Color Crimson700 = Color.FromArgb(163, 30, 30);

        public static readonly Color Teal50 = Color.FromArgb(238, 248, 249);
        public static readonly Color Teal200 = Color.FromArgb(176, 224, 230);
        public static readonly Color Teal600 = Color.FromArgb(22, 114, 128);

        public static readonly Color Plum50 = Color.FromArgb(246, 241, 251);
        public static readonly Color Plum200 = Color.FromArgb(216, 196, 238);
        public static readonly Color Plum600 = Color.FromArgb(108, 60, 156);

        public static readonly Color Slate50 = Color.FromArgb(241, 245, 249);
        public static readonly Color Slate200 = Color.FromArgb(203, 213, 225);
        public static readonly Color Slate600 = Color.FromArgb(71, 85, 105);
        public static readonly Color Slate700 = Color.FromArgb(51, 65, 85);
    }

    // =========================================================================
    // LAYER 2: SEMANTIC TOKENS
    // =========================================================================
    public static readonly Color Background = Primitives.Alabaster100;
    public static readonly Color BackgroundSubtle = Primitives.Alabaster200;
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceWarm = Primitives.Alabaster50;
    public static readonly Color SurfaceElevated = Color.White;
    public static readonly Color SurfaceHover = Primitives.Alabaster50;

    // Navigation Rail & Header Shell
    public static readonly Color Sidebar = Primitives.Espresso900;
    public static readonly Color SidebarCard = Primitives.Espresso800;
    public static readonly Color SidebarHover = Color.FromArgb(44, 40, 36);
    public static readonly Color SidebarActive = Primitives.Terracotta600;
    public static readonly Color SidebarActiveBg = Color.FromArgb(52, 34, 27);
    public static readonly Color SidebarText = Primitives.Alabaster200;
    public static readonly Color SidebarMuted = Primitives.Stone400;

    // Primary Brand (Terracotta Bronze)
    public static readonly Color Primary = Primitives.Terracotta600;
    public static readonly Color PrimaryHover = Primitives.Terracotta700;
    public static readonly Color PrimaryPressed = Primitives.Terracotta800;
    public static readonly Color PrimaryLight = Primitives.Terracotta50;
    public static readonly Color PrimaryLightBorder = Primitives.Terracotta200;

    // Secondary / Architectural Neutral
    public static readonly Color Secondary = Primitives.Umber700;
    public static readonly Color SecondaryHover = Primitives.Espresso800;
    public static readonly Color SecondaryLight = Primitives.Alabaster200;

    // Typography Ink
    public static readonly Color Text = Primitives.Espresso900;
    public static readonly Color TextSecondary = Primitives.Umber700;
    public static readonly Color Muted = Primitives.Stone500;
    public static readonly Color TextLight = Primitives.Stone400;

    // Hairlines & Borders
    public static readonly Color Border = Primitives.Alabaster200;
    public static readonly Color BorderSubtle = Primitives.Alabaster100;
    public static readonly Color BorderStrong = Primitives.Stone300;
    public static readonly Color BorderFocus = Primitives.Terracotta600;

    // Status & Validation
    public static readonly Color Success = Primitives.Sage600;
    public static readonly Color SuccessHover = Primitives.Sage700;
    public static readonly Color SuccessLight = Primitives.Sage50;
    public static readonly Color SuccessBorder = Primitives.Sage200;

    public static readonly Color Warning = Primitives.Ochre600;
    public static readonly Color WarningHover = Primitives.Ochre700;
    public static readonly Color WarningLight = Primitives.Ochre50;
    public static readonly Color WarningBorder = Primitives.Ochre200;

    public static readonly Color Danger = Primitives.Crimson600;
    public static readonly Color DangerHover = Primitives.Crimson700;
    public static readonly Color DangerLight = Primitives.Crimson50;
    public static readonly Color DangerBorder = Primitives.Crimson200;

    public static readonly Color Excel = Color.FromArgb(31, 94, 59);
    public static readonly Color Cyan = Primitives.Teal600;
    public static readonly Color CyanLight = Primitives.Teal50;
    public static readonly Color Purple = Primitives.Plum600;
    public static readonly Color PurpleLight = Primitives.Plum50;
    public static readonly Color Gold = Primitives.Ochre600;
    public static readonly Color GoldLight = Primitives.Ochre50;

    public static readonly Color Disabled = Primitives.Alabaster200;
    public static readonly Color DisabledText = Primitives.Stone400;

    // =========================================================================
    // TYPOGRAPHY SCALE
    // =========================================================================
    private const string FontFamilyName = "Segoe UI";
    private const string MonoFontFamilyName = "Consolas";

    public static Font Font(float size = 9.5f, FontStyle style = FontStyle.Regular) => new(FontFamilyName, size, style);
    public static Font MonoFont(float size = 9.2f, FontStyle style = FontStyle.Regular) => new(MonoFontFamilyName, size, style);
    public static Font DisplayFont => Font(21f, FontStyle.Bold);
    public static Font H1Font => Font(16.5f, FontStyle.Bold);
    public static Font H2Font => Font(13.5f, FontStyle.Bold);
    public static Font H3Font => Font(11f, FontStyle.Bold);
    public static Font SubtitleFont => Font(9.8f, FontStyle.Regular);
    public static Font BodyFont => Font(9.5f, FontStyle.Regular);
    public static Font BodyBoldFont => Font(9.5f, FontStyle.Bold);
    public static Font CaptionFont => Font(8.3f, FontStyle.Regular);
    public static Font CaptionBoldFont => Font(8.2f, FontStyle.Bold);
    public static Font SmallFont => Font(7.6f, FontStyle.Regular);

    // =========================================================================
    // GEOMETRY & GDI+ HELPERS
    // =========================================================================
    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            var empty = new GraphicsPath();
            empty.AddRectangle(new RectangleF(0, 0, 1, 1));
            return empty;
        }
        var safeRadius = Math.Max(1f, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
        var diameter = safeRadius * 2f;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Round(Control c, int radius = 8)
    {
        if (c.Width <= 1 || c.Height <= 1) return;
        using var p = RoundedRectangle(new RectangleF(0, 0, c.Width, c.Height), radius);
        c.Region = new Region(p);
    }

    // =========================================================================
    // LAYER 3: COMPONENT FACTORIES
    // =========================================================================
    public static ModernButton Button(string text, Color? color = null, IconKind icon = IconKind.None)
    {
        var activeColor = color ?? Primary;
        var variant = ButtonVariant.Primary;

        if (color.HasValue)
        {
            var argb = color.Value.ToArgb();
            if (argb == Secondary.ToArgb() || argb == Color.FromArgb(51, 65, 85).ToArgb() || argb == Color.FromArgb(100, 116, 139).ToArgb() || argb == Color.FromArgb(30, 41, 59).ToArgb())
                variant = ButtonVariant.Secondary;
            else if (argb == Danger.ToArgb() || argb == Color.FromArgb(220, 38, 38).ToArgb())
                variant = ButtonVariant.Danger;
            else if (argb == Warning.ToArgb() || argb == Color.FromArgb(217, 119, 6).ToArgb())
                variant = ButtonVariant.Warning;
            else if (argb == Success.ToArgb() || argb == Excel.ToArgb() || argb == Color.FromArgb(22, 163, 74).ToArgb())
                variant = ButtonVariant.Success;
        }

        var b = new ModernButton(activeColor, HoverFor(activeColor), PressedFor(activeColor), variant)
        {
            Text = CleanLegacyGlyphs(text),
            Icon = icon != IconKind.None ? icon : InferIconFromText(text),
            AutoSize = false,
            Height = 38,
            Font = Font(9.2f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(4),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false
        };
        b.MinimumSize = new Size(68, 36);
        b.Width = Math.Max(b.MinimumSize.Width, b.RequiredWidth);
        return b;
    }

    public static ModernButton OutlineButton(string text, Color? borderColor = null, IconKind icon = IconKind.None)
    {
        var col = borderColor ?? BorderStrong;
        var b = new ModernButton(Surface, SurfaceHover, BackgroundSubtle, ButtonVariant.Outline)
        {
            Text = CleanLegacyGlyphs(text),
            Icon = icon != IconKind.None ? icon : InferIconFromText(text),
            Height = 38,
            Font = Font(9.0f, FontStyle.Bold),
            ForeColor = TextSecondary,
            BorderColor = col,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(4),
            MinimumSize = new Size(68, 36)
        };
        b.Width = Math.Max(b.MinimumSize.Width, b.RequiredWidth);
        return b;
    }

    public static ModernButton GhostButton(string text, IconKind icon = IconKind.None)
    {
        var b = new ModernButton(Color.Transparent, BackgroundSubtle, Border, ButtonVariant.Ghost)
        {
            Text = CleanLegacyGlyphs(text),
            Icon = icon != IconKind.None ? icon : InferIconFromText(text),
            Height = 36,
            Font = Font(9.0f, FontStyle.Bold),
            ForeColor = TextSecondary,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(2),
            MinimumSize = new Size(64, 34)
        };
        b.Width = Math.Max(b.MinimumSize.Width, b.RequiredWidth);
        return b;
    }

    private static string CleanLegacyGlyphs(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var prefixes = new[] { "⇥", "↻", "⌕", "⌃", "▦", "🖨", "+  ", "+ " };
        var cleaned = text.Trim();
        foreach (var p in prefixes)
        {
            if (cleaned.StartsWith(p, StringComparison.Ordinal))
                cleaned = cleaned[p.Length..].TrimStart();
        }
        return cleaned.Replace("  →", "").Trim();
    }

    private static IconKind InferIconFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return IconKind.None;
        if (text.Contains("Tạo", StringComparison.OrdinalIgnoreCase) || text.Contains("Thêm", StringComparison.OrdinalIgnoreCase) || text.StartsWith('+'))
            return IconKind.Plus;
        if (text.Contains("Tìm", StringComparison.OrdinalIgnoreCase))
            return IconKind.Search;
        if (text.Contains("Bộ lọc", StringComparison.OrdinalIgnoreCase) || text.Contains("Thu gọn", StringComparison.OrdinalIgnoreCase))
            return IconKind.Filter;
        if (text.Contains("Làm mới", StringComparison.OrdinalIgnoreCase) || text.Contains("Thử lại", StringComparison.OrdinalIgnoreCase))
            return IconKind.Refresh;
        if (text.Contains("Xuất", StringComparison.OrdinalIgnoreCase))
            return IconKind.Download;
        if (text.Contains("In phiếu", StringComparison.OrdinalIgnoreCase))
            return IconKind.Printer;
        if (text.Contains("Hủy", StringComparison.OrdinalIgnoreCase) || text.Contains("Xóa", StringComparison.OrdinalIgnoreCase) || text.Contains("Ngừng", StringComparison.OrdinalIgnoreCase))
            return IconKind.Trash;
        if (text.Contains("Sửa", StringComparison.OrdinalIgnoreCase) || text.Contains("Chỉnh sửa", StringComparison.OrdinalIgnoreCase))
            return IconKind.Edit;
        if (text.Contains("Thu tiền", StringComparison.OrdinalIgnoreCase) || text.Contains("Hoàn tiền", StringComparison.OrdinalIgnoreCase) || text.Contains("giảm giá", StringComparison.OrdinalIgnoreCase))
            return IconKind.Wallet;
        if (text.Contains("Bước tiếp", StringComparison.OrdinalIgnoreCase) || text.Contains("Đã trả", StringComparison.OrdinalIgnoreCase) || text.Contains("Lưu", StringComparison.OrdinalIgnoreCase) || text.Contains("Xác nhận", StringComparison.OrdinalIgnoreCase))
            return IconKind.Check;
        if (text.Contains("Lịch sử", StringComparison.OrdinalIgnoreCase))
            return IconKind.History;
        if (text.Contains("Khóa", StringComparison.OrdinalIgnoreCase))
            return IconKind.Lock;
        if (text.Contains("Mở khóa", StringComparison.OrdinalIgnoreCase))
            return IconKind.Unlock;
        if (text.Contains("mật khẩu", StringComparison.OrdinalIgnoreCase))
            return IconKind.Key;
        if (text.Contains("Đăng xuất", StringComparison.OrdinalIgnoreCase))
            return IconKind.LogOut;
        if (text.Contains("Đăng nhập", StringComparison.OrdinalIgnoreCase))
            return IconKind.ArrowRight;
        return IconKind.None;
    }

    internal static Color HoverFor(Color color) => color.ToArgb() switch
    {
        var v when v == Primary.ToArgb() => PrimaryHover,
        var v when v == Secondary.ToArgb() => SecondaryHover,
        var v when v == Excel.ToArgb() => Color.FromArgb(22, 72, 44),
        var v when v == Danger.ToArgb() => DangerHover,
        var v when v == Warning.ToArgb() => WarningHover,
        var v when v == Success.ToArgb() => SuccessHover,
        _ => Darken(color, 0.88f)
    };

    internal static Color PressedFor(Color color) => Darken(HoverFor(color), 0.90f);
    private static Color Darken(Color color, float factor) => Color.FromArgb(color.A, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));

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
            Font = Font(9.3f),
            RowTemplate = { Height = 44 },
            EnableHeadersVisualStyles = false,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            GridColor = Primitives.Alabaster100
        };

        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Primitives.Alabaster50,
            ForeColor = TextSecondary,
            Font = Font(8.3f, FontStyle.Bold),
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = Primitives.Alabaster50,
            SelectionForeColor = TextSecondary,
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        g.ColumnHeadersHeight = 42;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = Text,
            SelectionBackColor = Primitives.Terracotta50,
            SelectionForeColor = Primitives.Terracotta800,
            Padding = new Padding(12, 5, 12, 5),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(253, 252, 250),
            ForeColor = Text,
            SelectionBackColor = Primitives.Terracotta50,
            SelectionForeColor = Primitives.Terracotta800
        };

        g.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                e.ToolTipText = g.Rows[e.RowIndex].Cells[e.ColumnIndex].FormattedValue?.ToString() ?? string.Empty;
        };

        return g;
    }

    public static (Color Text, Color Background, Color Border) GetStatusColors(string status)
    {
        return status switch
        {
            "Đã đặt lịch" or "DA_DAT_LICH" =>
                (Primitives.Terracotta700, Primitives.Terracotta50, Primitives.Terracotta200),
            "Đã chụp" or "DA_CHUP" =>
                (Primitives.Plum600, Primitives.Plum50, Primitives.Plum200),
            "Đang chỉnh sửa ảnh" or "DANG_CHINH_SUA" or "DANG_CHINH_SUA_ANH" =>
                (Primitives.Ochre700, Primitives.Ochre50, Primitives.Ochre200),
            "Chờ giao ảnh" or "CHO_GIAO_ANH" =>
                (Primitives.Teal600, Primitives.Teal50, Primitives.Teal200),
            "Đã giao ảnh" or "DA_GIAO_ANH" =>
                (Primitives.Sage600, Primitives.Sage50, Primitives.Sage200),
            "Hoàn thành" or "HOAN_THANH" or "HOAT_DONG" or "DANG_LAM" or "DANG_AP_DUNG" or "DANG_CUNG_CAP" or "DA_TRA" =>
                (Primitives.Sage700, Primitives.Sage50, Primitives.Sage200),
            "Đã hủy" or "DA_HUY" or "KHOA" or "NGUNG_LAM" or "NGUNG_AP_DUNG" or "NGUNG_CUNG_CAP" or "NGUNG_SU_DUNG" =>
                (Primitives.Crimson600, Primitives.Crimson50, Primitives.Crimson200),
            _ => (TextSecondary, Primitives.Alabaster100, Primitives.Stone300)
        };
    }
}

/// <summary>
/// Pure GDI+ Vector Icon System — Zero emojis, consistent 1.75px geometric stroke.
/// </summary>
public static class VectorIcons
{
    public static void Draw(Graphics g, IconKind kind, RectangleF bounds, Color color, float strokeWidth = 1.75f)
    {
        if (kind == IconKind.None || bounds.Width <= 4 || bounds.Height <= 4) return;

        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var size = Math.Min(bounds.Width, bounds.Height);
        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        var r = size * 0.42f;
        var box = new RectangleF(cx - r, cy - r, r * 2f, r * 2f);

        using var pen = new Pen(color, strokeWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        switch (kind)
        {
            case IconKind.Aperture:
                g.DrawEllipse(pen, box);
                for (var i = 0; i < 6; i++)
                {
                    var a1 = i * 60f * (float)Math.PI / 180f;
                    var a2 = (i * 60f + 75f) * (float)Math.PI / 180f;
                    var p1 = new PointF(cx + r * 0.42f * (float)Math.Cos(a1), cy + r * 0.42f * (float)Math.Sin(a1));
                    var p2 = new PointF(cx + r * 0.95f * (float)Math.Cos(a2), cy + r * 0.95f * (float)Math.Sin(a2));
                    g.DrawLine(pen, p1, p2);
                }
                break;

            case IconKind.Dashboard:
                var hw = r * 0.85f;
                g.DrawRectangle(pen, cx - r, cy - r, hw, hw * 1.15f);
                g.DrawRectangle(pen, cx + r - hw, cy - r, hw, hw * 0.7f);
                g.DrawRectangle(pen, cx - r, cy + r - hw * 0.7f, hw, hw * 0.7f);
                g.DrawRectangle(pen, cx + r - hw, cy + r - hw * 1.15f, hw, hw * 1.15f);
                break;

            case IconKind.Calendar:
                var calRect = new RectangleF(cx - r, cy - r * 0.75f, r * 2f, r * 1.75f);
                using (var p = Theme.RoundedRectangle(calRect, 2.5f)) g.DrawPath(pen, p);
                g.DrawLine(pen, calRect.Left, cy - r * 0.2f, calRect.Right, cy - r * 0.2f);
                g.DrawLine(pen, cx - r * 0.45f, cy - r, cx - r * 0.45f, cy - r * 0.5f);
                g.DrawLine(pen, cx + r * 0.45f, cy - r, cx + r * 0.45f, cy - r * 0.5f);
                break;

            case IconKind.Users:
            case IconKind.UserCheck:
                g.DrawEllipse(pen, cx - r * 0.55f, cy - r * 0.85f, r * 0.85f, r * 0.85f);
                g.DrawArc(pen, cx - r * 0.95f, cy + r * 0.15f, r * 1.65f, r * 1.1f, 195, 150);
                if (kind == IconKind.UserCheck)
                {
                    g.DrawLine(pen, cx + r * 0.35f, cy + r * 0.25f, cx + r * 0.6f, cy + r * 0.5f);
                    g.DrawLine(pen, cx + r * 0.6f, cy + r * 0.5f, cx + r * 1.0f, cy - r * 0.05f);
                }
                else
                {
                    g.DrawArc(pen, cx + r * 0.05f, cy - r * 0.75f, r * 0.7f, r * 0.7f, -60, 180);
                }
                break;

            case IconKind.Layers:
                PointF[] top = [new(cx, cy - r * 0.9f), new(cx + r, cy - r * 0.3f), new(cx, cy + r * 0.25f), new(cx - r, cy - r * 0.3f)];
                g.DrawPolygon(pen, top);
                g.DrawLines(pen, [new PointF(cx - r, cy + r * 0.15f), new PointF(cx, cy + r * 0.65f), new PointF(cx + r, cy + r * 0.15f)]);
                g.DrawLines(pen, [new PointF(cx - r, cy + r * 0.55f), new PointF(cx, cy + r * 1.0f), new PointF(cx + r, cy + r * 0.55f)]);
                break;

            case IconKind.Sparkles:
                g.DrawLine(pen, cx, cy - r, cx, cy + r);
                g.DrawLine(pen, cx - r, cy, cx + r, cy);
                g.DrawLine(pen, cx - r * 0.5f, cy - r * 0.5f, cx + r * 0.5f, cy + r * 0.5f);
                g.DrawLine(pen, cx + r * 0.5f, cy - r * 0.5f, cx - r * 0.5f, cy + r * 0.5f);
                break;

            case IconKind.Home:
                g.DrawLines(pen, [new PointF(cx - r, cy - r * 0.1f), new PointF(cx, cy - r * 0.95f), new PointF(cx + r, cy - r * 0.1f)]);
                g.DrawLines(pen, [new PointF(cx - r * 0.7f, cy - r * 0.25f), new PointF(cx - r * 0.7f, cy + r * 0.9f), new PointF(cx + r * 0.7f, cy + r * 0.9f), new PointF(cx + r * 0.7f, cy - r * 0.25f)]);
                break;

            case IconKind.Camera:
                var camBody = new RectangleF(cx - r, cy - r * 0.55f, r * 2f, r * 1.45f);
                using (var p = Theme.RoundedRectangle(camBody, 2.5f)) g.DrawPath(pen, p);
                g.DrawLines(pen, [new PointF(cx - r * 0.45f, cy - r * 0.55f), new PointF(cx - r * 0.25f, cy - r * 0.9f), new PointF(cx + r * 0.25f, cy - r * 0.9f), new PointF(cx + r * 0.45f, cy - r * 0.55f)]);
                g.DrawEllipse(pen, cx - r * 0.45f, cy - r * 0.25f, r * 0.9f, r * 0.9f);
                break;

            case IconKind.ChartBar:
                g.DrawLine(pen, cx - r, cy + r * 0.9f, cx + r, cy + r * 0.9f);
                g.DrawLine(pen, cx - r * 0.6f, cy + r * 0.9f, cx - r * 0.6f, cy + r * 0.1f);
                g.DrawLine(pen, cx, cy + r * 0.9f, cx, cy - r * 0.8f);
                g.DrawLine(pen, cx + r * 0.6f, cy + r * 0.9f, cx + r * 0.6f, cy - r * 0.35f);
                break;

            case IconKind.Wallet:
                var wRect = new RectangleF(cx - r, cy - r * 0.65f, r * 2f, r * 1.35f);
                using (var p = Theme.RoundedRectangle(wRect, 2.5f)) g.DrawPath(pen, p);
                g.DrawLine(pen, cx + r * 0.25f, cy, cx + r, cy);
                break;

            case IconKind.Shield:
                PointF[] shield =
                [
                    new(cx, cy - r),
                    new(cx + r * 0.85f, cy - r * 0.6f),
                    new(cx + r * 0.75f, cy + r * 0.25f),
                    new(cx, cy + r),
                    new(cx - r * 0.75f, cy + r * 0.25f),
                    new(cx - r * 0.85f, cy - r * 0.6f)
                ];
                g.DrawPolygon(pen, shield);
                break;

            case IconKind.History:
            case IconKind.Clock:
                g.DrawEllipse(pen, box);
                g.DrawLines(pen, [new PointF(cx, cy - r * 0.55f), new PointF(cx, cy), new PointF(cx + r * 0.45f, cy + r * 0.25f)]);
                break;

            case IconKind.Database:
                g.DrawEllipse(pen, cx - r * 0.85f, cy - r * 0.95f, r * 1.7f, r * 0.6f);
                g.DrawLine(pen, cx - r * 0.85f, cy - r * 0.65f, cx - r * 0.85f, cy + r * 0.65f);
                g.DrawLine(pen, cx + r * 0.85f, cy - r * 0.65f, cx + r * 0.85f, cy + r * 0.65f);
                g.DrawArc(pen, cx - r * 0.85f, cy - r * 0.3f, r * 1.7f, r * 0.6f, 0, 180);
                g.DrawArc(pen, cx - r * 0.85f, cy + r * 0.35f, r * 1.7f, r * 0.6f, 0, 180);
                break;

            case IconKind.Search:
                g.DrawEllipse(pen, cx - r * 0.85f, cy - r * 0.85f, r * 1.35f, r * 1.35f);
                g.DrawLine(pen, cx + r * 0.3f, cy + r * 0.3f, cx + r * 0.9f, cy + r * 0.9f);
                break;

            case IconKind.Filter:
                g.DrawLine(pen, cx - r * 0.9f, cy - r * 0.6f, cx + r * 0.9f, cy - r * 0.6f);
                g.DrawLine(pen, cx - r * 0.55f, cy, cx + r * 0.55f, cy);
                g.DrawLine(pen, cx - r * 0.25f, cy + r * 0.6f, cx + r * 0.25f, cy + r * 0.6f);
                break;

            case IconKind.Plus:
                g.DrawLine(pen, cx, cy - r * 0.8f, cx, cy + r * 0.8f);
                g.DrawLine(pen, cx - r * 0.8f, cy, cx + r * 0.8f, cy);
                break;

            case IconKind.Edit:
                PointF[] pencil = [new(cx - r * 0.8f, cy + r * 0.8f), new(cx - r * 0.35f, cy + r * 0.7f), new(cx + r * 0.75f, cy - r * 0.4f), new(cx + r * 0.4f, cy - r * 0.75f), new(cx - r * 0.7f, cy + r * 0.35f)];
                g.DrawPolygon(pen, pencil);
                break;

            case IconKind.Trash:
                g.DrawLine(pen, cx - r * 0.85f, cy - r * 0.55f, cx + r * 0.85f, cy - r * 0.55f);
                g.DrawLine(pen, cx - r * 0.3f, cy - r * 0.85f, cx + r * 0.3f, cy - r * 0.85f);
                g.DrawLines(pen, [new PointF(cx - r * 0.65f, cy - r * 0.55f), new PointF(cx - r * 0.55f, cy + r * 0.85f), new PointF(cx + r * 0.55f, cy + r * 0.85f), new PointF(cx + r * 0.65f, cy - r * 0.55f)]);
                break;

            case IconKind.Check:
                g.DrawLines(pen, [new PointF(cx - r * 0.75f, cy + r * 0.05f), new PointF(cx - r * 0.2f, cy + r * 0.6f), new PointF(cx + r * 0.8f, cy - r * 0.5f)]);
                break;

            case IconKind.Close:
                g.DrawLine(pen, cx - r * 0.65f, cy - r * 0.65f, cx + r * 0.65f, cy + r * 0.65f);
                g.DrawLine(pen, cx + r * 0.65f, cy - r * 0.65f, cx - r * 0.65f, cy + r * 0.65f);
                break;

            case IconKind.Eye:
            case IconKind.EyeOff:
                g.DrawArc(pen, cx - r, cy - r * 0.65f, r * 2f, r * 1.3f, 200, 140);
                g.DrawArc(pen, cx - r, cy - r * 0.65f, r * 2f, r * 1.3f, 20, 140);
                g.DrawEllipse(pen, cx - r * 0.3f, cy - r * 0.3f, r * 0.6f, r * 0.6f);
                if (kind == IconKind.EyeOff)
                    g.DrawLine(pen, cx - r * 0.8f, cy + r * 0.8f, cx + r * 0.8f, cy - r * 0.8f);
                break;

            case IconKind.Lock:
            case IconKind.Unlock:
                var lockBody = new RectangleF(cx - r * 0.75f, cy - r * 0.1f, r * 1.5f, r * 1.05f);
                using (var p = Theme.RoundedRectangle(lockBody, 2f)) g.DrawPath(pen, p);
                if (kind == IconKind.Lock)
                    g.DrawArc(pen, cx - r * 0.45f, cy - r * 0.85f, r * 0.9f, r * 1.1f, 180, 180);
                else
                    g.DrawArc(pen, cx - r * 0.1f, cy - r * 0.85f, r * 0.9f, r * 1.1f, 180, 180);
                break;

            case IconKind.Key:
                g.DrawEllipse(pen, cx - r * 0.9f, cy - r * 0.45f, r * 0.9f, r * 0.9f);
                g.DrawLine(pen, cx, cy, cx + r * 0.9f, cy);
                g.DrawLine(pen, cx + r * 0.55f, cy, cx + r * 0.55f, cy + r * 0.35f);
                g.DrawLine(pen, cx + r * 0.85f, cy, cx + r * 0.85f, cy + r * 0.45f);
                break;

            case IconKind.Refresh:
                g.DrawArc(pen, box, -30, 290);
                g.DrawLines(pen, [new PointF(cx + r * 0.4f, cy - r * 0.85f), new PointF(cx + r * 0.85f, cy - r * 0.45f), new PointF(cx + r * 0.35f, cy - r * 0.35f)]);
                break;

            case IconKind.Download:
                g.DrawLine(pen, cx, cy - r * 0.85f, cx, cy + r * 0.4f);
                g.DrawLines(pen, [new PointF(cx - r * 0.45f, cy), new PointF(cx, cy + r * 0.45f), new PointF(cx + r * 0.45f, cy)]);
                g.DrawLines(pen, [new PointF(cx - r * 0.85f, cy + r * 0.45f), new PointF(cx - r * 0.85f, cy + r * 0.85f), new PointF(cx + r * 0.85f, cy + r * 0.85f), new PointF(cx + r * 0.85f, cy + r * 0.45f)]);
                break;

            case IconKind.Printer:
                g.DrawRectangle(pen, cx - r * 0.55f, cy - r * 0.85f, r * 1.1f, r * 0.5f);
                g.DrawRectangle(pen, cx - r * 0.85f, cy - r * 0.35f, r * 1.7f, r * 0.8f);
                g.DrawRectangle(pen, cx - r * 0.55f, cy + r * 0.2f, r * 1.1f, r * 0.65f);
                break;

            case IconKind.ArrowRight:
                g.DrawLine(pen, cx - r * 0.8f, cy, cx + r * 0.8f, cy);
                g.DrawLines(pen, [new PointF(cx + r * 0.25f, cy - r * 0.5f), new PointF(cx + r * 0.8f, cy), new PointF(cx + r * 0.25f, cy + r * 0.5f)]);
                break;

            case IconKind.AlertTriangle:
                PointF[] tri = [new(cx, cy - r * 0.85f), new(cx + r * 0.9f, cy + r * 0.8f), new(cx - r * 0.9f, cy + r * 0.8f)];
                g.DrawPolygon(pen, tri);
                g.DrawLine(pen, cx, cy - r * 0.25f, cx, cy + r * 0.25f);
                g.DrawLine(pen, cx, cy + r * 0.52f, cx, cy + r * 0.55f);
                break;

            case IconKind.Info:
                g.DrawEllipse(pen, box);
                g.DrawLine(pen, cx, cy - r * 0.1f, cx, cy + r * 0.5f);
                g.DrawLine(pen, cx, cy - r * 0.45f, cx, cy - r * 0.42f);
                break;

            case IconKind.LogOut:
                g.DrawLines(pen, [new PointF(cx - r * 0.1f, cy - r * 0.8f), new PointF(cx - r * 0.8f, cy - r * 0.8f), new PointF(cx - r * 0.8f, cy + r * 0.8f), new PointF(cx - r * 0.1f, cy + r * 0.8f)]);
                g.DrawLine(pen, cx - r * 0.25f, cy, cx + r * 0.85f, cy);
                g.DrawLines(pen, [new PointF(cx + r * 0.4f, cy - r * 0.4f), new PointF(cx + r * 0.85f, cy), new PointF(cx + r * 0.4f, cy + r * 0.4f)]);
                break;

            case IconKind.Command:
                g.DrawLine(pen, cx - r * 0.4f, cy - r * 0.4f, cx + r * 0.4f, cy - r * 0.4f);
                g.DrawLine(pen, cx - r * 0.4f, cy + r * 0.4f, cx + r * 0.4f, cy + r * 0.4f);
                g.DrawLine(pen, cx - r * 0.4f, cy - r * 0.4f, cx - r * 0.4f, cy + r * 0.4f);
                g.DrawLine(pen, cx + r * 0.4f, cy - r * 0.4f, cx + r * 0.4f, cy + r * 0.4f);
                g.DrawEllipse(pen, cx - r * 0.85f, cy - r * 0.85f, r * 0.45f, r * 0.45f);
                g.DrawEllipse(pen, cx + r * 0.4f, cy - r * 0.85f, r * 0.45f, r * 0.45f);
                g.DrawEllipse(pen, cx - r * 0.85f, cy + r * 0.4f, r * 0.45f, r * 0.45f);
                g.DrawEllipse(pen, cx + r * 0.4f, cy + r * 0.4f, r * 0.45f, r * 0.45f);
                break;

            case IconKind.RoleAdmin:
                PointF[] adminShield =
                [
                    new(cx, cy - r * 0.96f),
                    new(cx + r * 0.86f, cy - r * 0.62f),
                    new(cx + r * 0.76f, cy + r * 0.24f),
                    new(cx, cy + r * 0.96f),
                    new(cx - r * 0.76f, cy + r * 0.24f),
                    new(cx - r * 0.86f, cy - r * 0.62f)
                ];
                g.DrawPolygon(pen, adminShield);
                g.DrawLines(pen, [new PointF(cx - r * 0.36f, cy + r * 0.02f), new PointF(cx - r * 0.1f, cy + r * 0.3f), new PointF(cx + r * 0.38f, cy - r * 0.24f)]);
                break;

            case IconKind.RoleStaff:
                var badgeCard = new RectangleF(cx - r * 0.88f, cy - r * 0.48f, r * 1.76f, r * 1.36f);
                using (var p = Theme.RoundedRectangle(badgeCard, 2.2f)) g.DrawPath(pen, p);
                g.DrawLines(pen, [new PointF(cx - r * 0.26f, cy - r * 0.48f), new PointF(cx - r * 0.18f, cy - r * 0.88f), new PointF(cx + r * 0.18f, cy - r * 0.88f), new PointF(cx + r * 0.26f, cy - r * 0.48f)]);
                g.DrawEllipse(pen, cx - r * 0.56f, cy - r * 0.18f, r * 0.46f, r * 0.46f);
                g.DrawArc(pen, cx - r * 0.7f, cy + r * 0.34f, r * 0.74f, r * 0.48f, 190, 160);
                g.DrawLine(pen, cx + r * 0.1f, cy - r * 0.04f, cx + r * 0.6f, cy - r * 0.04f);
                g.DrawLine(pen, cx + r * 0.1f, cy + r * 0.34f, cx + r * 0.52f, cy + r * 0.34f);
                break;

            case IconKind.RolePhotographer:
                var photoCamBody = new RectangleF(cx - r, cy - r * 0.48f, r * 2f, r * 1.38f);
                using (var p = Theme.RoundedRectangle(photoCamBody, 2.4f)) g.DrawPath(pen, p);
                g.DrawLines(pen, [new PointF(cx - r * 0.44f, cy - r * 0.48f), new PointF(cx - r * 0.24f, cy - r * 0.88f), new PointF(cx + r * 0.24f, cy - r * 0.88f), new PointF(cx + r * 0.44f, cy - r * 0.48f)]);
                g.DrawLine(pen, cx - r * 0.8f, cy - r * 0.66f, cx - r * 0.54f, cy - r * 0.66f);
                g.DrawEllipse(pen, cx - r * 0.44f, cy - r * 0.2f, r * 0.88f, r * 0.88f);
                g.DrawEllipse(pen, cx - r * 0.18f, cy + r * 0.06f, r * 0.36f, r * 0.36f);
                break;

            case IconKind.RoleCustomer:
                g.DrawEllipse(pen, cx - r * 0.4f, cy - r * 0.88f, r * 0.8f, r * 0.8f);
                g.DrawArc(pen, cx - r * 0.84f, cy + r * 0.1f, r * 1.68f, r * 1.15f, 190, 160);
                break;
        }

        g.Restore(state);
    }
}

/// <summary>
/// Architectural Owner-Drawn Button with GDI+ Vector Icon support.
/// </summary>
public sealed class ModernButton : Control, IButtonControl
{
    private const float CornerRadius = 7f;
    private Color _normal;
    private Color _hover;
    private Color _pressed;
    private bool _isHovering;
    private bool _isPressed;
    private bool _isDefault;
    private DialogResult _dialogResult;
    private ButtonVariant _variant = ButtonVariant.Primary;
    private IconKind _icon = IconKind.None;

    public ButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    public IconKind Icon
    {
        get => _icon;
        set { _icon = value; EnsureTextFits(); Invalidate(); }
    }

    public Color BorderColor { get; set; } = Theme.BorderStrong;
    public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
    public bool AutoEllipsis { get; set; }

    public ModernButton(Color normal, Color hover, Color pressed, ButtonVariant variant = ButtonVariant.Primary)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable | ControlStyles.StandardClick |
                 ControlStyles.SupportsTransparentBackColor, true);

        _normal = normal;
        _hover = hover;
        _pressed = pressed;
        _variant = variant;

        BackColor = normal;
        ForeColor = variant switch
        {
            ButtonVariant.Outline or ButtonVariant.Ghost => Theme.TextSecondary,
            _ => Color.White
        };

        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        TabStop = true;
    }

    public ModernButton() : this(Theme.Primary, Theme.PrimaryHover, Theme.PrimaryPressed) { }

    public int RequiredWidth
    {
        get
        {
            var iconSpace = _icon != IconKind.None ? (string.IsNullOrEmpty(Text) ? 16 : 22) : 0;
            var textWidth = string.IsNullOrEmpty(Text)
                ? 0
                : TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            return Math.Max(68, textWidth + iconSpace + 22);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var reqH = Math.Max(36, Font.Height + 14);
        return new Size(RequiredWidth, Math.Max(Height, reqH));
    }

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); EnsureTextFits(); Invalidate(); }
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); EnsureTextFits(); Invalidate(); }
    protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); EnsureTextFits(); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }

    protected override void OnBackColorChanged(EventArgs e)
    {
        if (!BackColor.IsEmpty && _variant is not ButtonVariant.Outline and not ButtonVariant.Ghost)
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
        if ((specified & BoundsSpecified.Height) != 0 && Dock is DockStyle.None or DockStyle.Top or DockStyle.Bottom)
            height = Math.Max(height, 32);
        base.SetBoundsCore(x, y, width, height, specified);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor is { A: byte.MaxValue } c ? c : Theme.Surface);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
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
            fill = _isPressed ? Theme.BackgroundSubtle : _isHovering ? Theme.SurfaceHover : Theme.Surface;
            textCol = _isHovering ? Theme.Text : ForeColor;
            borderCol = _isHovering ? Theme.Primary : BorderColor;
        }
        else if (_variant == ButtonVariant.Ghost)
        {
            fill = _isPressed ? Theme.Border : _isHovering ? Theme.BackgroundSubtle : Color.Transparent;
            textCol = _isHovering ? Theme.Primary : ForeColor;
        }
        else
        {
            fill = _isPressed ? _pressed : _isHovering ? _hover : _normal;
            textCol = ForeColor;
        }

        if (fill != Color.Transparent)
        {
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);
        }

        if (borderCol != Color.Transparent)
        {
            using var pen = new Pen(borderCol, 1.15f);
            g.DrawPath(pen, path);
        }

        if (Focused || _isDefault)
        {
            using var focusPen = new Pen(Color.FromArgb(170, Theme.Primary), 1.5f);
            g.DrawPath(focusPen, path);
        }

        var hasIcon = _icon != IconKind.None;
        const int iconSize = 15;
        const int iconGap = 6;
        var iconBlockWidth = hasIcon ? iconSize + (string.IsNullOrEmpty(Text) ? 0 : iconGap) : 0;

        Font? scaledFont = null;
        var activeFont = Font;
        var textSize = string.IsNullOrEmpty(Text)
            ? Size.Empty
            : TextRenderer.MeasureText(g, Text, activeFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        var maxAllowedContentWidth = Math.Max(16, Width - 10);
        if (!string.IsNullOrEmpty(Text) && textSize.Width + iconBlockWidth > maxAllowedContentWidth)
        {
            for (var pt = activeFont.Size - 0.4f; pt >= 7.4f; pt -= 0.4f)
            {
                scaledFont?.Dispose();
                scaledFont = new Font(activeFont.FontFamily, pt, activeFont.Style);
                textSize = TextRenderer.MeasureText(g, Text, scaledFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                if (textSize.Width + iconBlockWidth <= maxAllowedContentWidth)
                    break;
            }
            if (scaledFont is not null)
                activeFont = scaledFont;
        }

        var totalContentWidth = Math.Min(maxAllowedContentWidth, textSize.Width + iconBlockWidth);
        var startX = Math.Max(5, (Width - totalContentWidth) / 2);

        if (hasIcon)
        {
            var iconRect = new RectangleF(startX, (Height - iconSize) / 2f, iconSize, iconSize);
            VectorIcons.Draw(g, _icon, iconRect, textCol, 1.65f);
            startX += iconSize + (string.IsNullOrEmpty(Text) ? 0 : iconGap);
        }

        if (!string.IsNullOrEmpty(Text))
        {
            var textRect = new Rectangle(startX, 0, Math.Max(8, Width - startX - 4), Height);
            TextRenderer.DrawText(g, Text, activeFont, textRect, textCol,
                TextFormatFlags.SingleLine | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        scaledFont?.Dispose();
    }

    private void EnsureTextFits()
    {
        if (Dock == DockStyle.None && Width < RequiredWidth) Width = RequiredWidth;
        if (Dock == DockStyle.None && Height < 34) Height = 34;
    }

    public DialogResult DialogResult { get => _dialogResult; set => _dialogResult = value; }
    public void NotifyDefault(bool value) { _isDefault = value; Invalidate(); }
    public void PerformClick()
    {
        if (Enabled) OnClick(EventArgs.Empty);
    }
}
