using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms;

public enum RoleVisualKind
{
    Admin,
    Staff,
    Photographer,
    Customer,
    Unknown
}

public sealed record RoleVisualInfo(
    RoleVisualKind Kind,
    string Code,
    string DisplayLabel,
    string TooltipDetail,
    IconKind Icon,
    Color Accent,
    Color SoftBackground,
    Color BorderColor);

public static class RoleVisualResolver
{
    public static RoleVisualInfo Resolve(VaiTro? role, ChucVu? position = null)
    {
        if (role == VaiTro.QuanTriVien)
            return ForAdmin();

        if (role == VaiTro.NhanVien)
        {
            return position == ChucVu.NhiepAnhGia
                ? ForPhotographer("Nhiếp ảnh gia", "Nhân viên chụp ảnh (tài khoản Nhân viên • chức vụ Nhiếp ảnh gia)")
                : ForStaff();
        }

        return Unknown();
    }

    public static RoleVisualInfo ResolvePosition(ChucVu? position) => position switch
    {
        ChucVu.NhiepAnhGia => ForPhotographer(),
        ChucVu.TiepNhan => ForStaff("TIEP_NHAN", "Tiếp nhận", "Nhân sự tiếp nhận khách hàng, tư vấn và điều phối lịch chụp"),
        ChucVu.Khac => ForStaff("KHAC", "Nhân viên khác", "Nhân sự hỗ trợ vận hành và hậu cần tại studio"),
        _ => Unknown()
    };

    public static RoleVisualInfo ForAdmin() => new(
        RoleVisualKind.Admin,
        "QUAN_TRI_VIEN",
        "Quản trị viên",
        "Quản trị hệ thống, tài khoản, phân quyền, danh mục và báo cáo doanh thu",
        IconKind.RoleAdmin,
        Theme.Primitives.Ochre700,
        Theme.Primitives.Ochre50,
        Theme.Primitives.Ochre200);

    public static RoleVisualInfo ForStaff(
        string code = "NHAN_VIEN",
        string displayLabel = "Nhân viên",
        string tooltipDetail = "Nhân sự vận hành studio, lịch hẹn và đối soát thanh toán") => new(
        RoleVisualKind.Staff,
        code,
        displayLabel,
        tooltipDetail,
        IconKind.RoleStaff,
        Theme.Primitives.Slate700,
        Theme.Primitives.Slate50,
        Theme.Primitives.Slate200);

    public static RoleVisualInfo ForPhotographer(
        string displayLabel = "Nhiếp ảnh gia",
        string tooltipDetail = "Nhiếp ảnh gia phụ trách buổi chụp và thiết bị phòng chụp") => new(
        RoleVisualKind.Photographer,
        "NHIEP_ANH_GIA",
        displayLabel,
        tooltipDetail,
        IconKind.RolePhotographer,
        Theme.Primitives.Terracotta600,
        Theme.Primitives.Terracotta50,
        Theme.Primitives.Terracotta200);

    public static RoleVisualInfo ForCustomer(
        string displayLabel = "Khách hàng",
        string tooltipDetail = "Khách hàng đặt lịch và sử dụng dịch vụ chụp ảnh") => new(
        RoleVisualKind.Customer,
        "KHACH_HANG",
        displayLabel,
        tooltipDetail,
        IconKind.RoleCustomer,
        Theme.Primitives.Sage700,
        Theme.Primitives.Sage50,
        Theme.Primitives.Sage200);

    public static RoleVisualInfo Unknown(string? rawLabel = null) => new(
        RoleVisualKind.Unknown,
        "CHUA_XAC_DINH",
        string.IsNullOrWhiteSpace(rawLabel) ? "Chưa phân loại" : rawLabel.Trim(),
        "Vai trò hoặc chức vụ chưa được ánh xạ",
        IconKind.Users,
        Theme.Primitives.Stone500,
        Theme.Primitives.Alabaster100,
        Theme.Primitives.Stone300);

    public static RoleVisualInfo ResolveRaw(string? rawRoleOrPosition, string? rawSecondaryPosition = null)
    {
        var primary = NormalizeToken(rawRoleOrPosition);
        var secondary = NormalizeToken(rawSecondaryPosition);

        if (primary is "QUAN_TRI_VIEN" or "QUANTRIVIEN" or "QUAN TRI VIEN" or "ADMIN" or "ADMINISTRATOR")
            return ForAdmin();

        if (primary is "NHIEP_ANH_GIA" or "NHIEPANHGIA" or "NHIEP ANH GIA" or "PHOTOGRAPHER")
            return ForPhotographer();

        if (primary is "KHACH_HANG" or "KHACHHANG" or "KHACH HANG" or "CUSTOMER")
            return ForCustomer();

        if (primary is "TIEP_NHAN" or "TIEPNHAN" or "TIEP NHAN")
            return ResolvePosition(ChucVu.TiepNhan);

        if (primary is "KHAC")
            return ResolvePosition(ChucVu.Khac);

        if (primary is "NHAN_VIEN" or "NHANVIEN" or "NHAN VIEN" or "STAFF")
        {
            if (secondary is "NHIEP_ANH_GIA" or "NHIEPANHGIA" or "NHIEP ANH GIA" or "PHOTOGRAPHER")
                return ForPhotographer("Nhiếp ảnh gia", "Nhân viên chụp ảnh (tài khoản Nhân viên • chức vụ Nhiếp ảnh gia)");

            return ForStaff();
        }

        if (secondary is "NHIEP_ANH_GIA" or "NHIEPANHGIA" or "NHIEP ANH GIA" or "PHOTOGRAPHER")
            return ForPhotographer();

        return Unknown(rawRoleOrPosition);
    }

    public static void DrawGridRolePill(Graphics g, Rectangle cellBounds, RoleVisualInfo info, bool selected, string? labelOverride = null)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var bgColor = selected ? MixColors(info.SoftBackground, Theme.PrimaryLight, 0.28f) : info.SoftBackground;
        var pillHeight = Math.Max(22, Math.Min(26, cellBounds.Height - 10));
        var pillRect = new RectangleF(
            cellBounds.X + 8,
            cellBounds.Y + (cellBounds.Height - pillHeight) / 2f,
            Math.Max(68, cellBounds.Width - 16),
            pillHeight);

        using var path = Theme.RoundedRectangle(pillRect, pillHeight / 2f);
        using var fill = new SolidBrush(bgColor);
        using var border = new Pen(info.BorderColor, 1f);
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        const float iconSize = 13f;
        var iconBounds = new RectangleF(pillRect.Left + 8f, pillRect.Top + (pillRect.Height - iconSize) / 2f, iconSize, iconSize);
        VectorIcons.Draw(g, info.Icon, iconBounds, info.Accent, 1.55f);

        var textLeft = (int)Math.Ceiling(iconBounds.Right + 5f);
        var textRect = new Rectangle(
            textLeft,
            (int)pillRect.Top,
            Math.Max(24, (int)pillRect.Right - textLeft - 8),
            (int)pillRect.Height);

        using var font = Theme.Font(8.2f, FontStyle.Bold);
        TextRenderer.DrawText(
            g,
            labelOverride ?? info.DisplayLabel,
            font,
            textRect,
            info.Accent,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        g.Restore(state);
    }

    public static void AttachRoleIconComboBox(
        ComboBox combo,
        Func<object?, RoleVisualInfo> resolver,
        Func<object?, string>? textSelector = null)
    {
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = Math.Max(combo.ItemHeight, 22);

        combo.DrawItem += (_, e) =>
        {
            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
                return;

            var g = e.Graphics;
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var isComboBoxEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
            var bgColor = isSelected && !isComboBoxEdit ? Theme.PrimaryLight : combo.BackColor;

            using var bg = new SolidBrush(bgColor);
            g.FillRectangle(bg, e.Bounds);

            if (e.Index < 0 || e.Index >= combo.Items.Count)
                return;

            var item = combo.Items[e.Index];
            var visual = resolver(item);
            var text = textSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;

            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var badgeSize = Math.Min(18f, Math.Max(14f, e.Bounds.Height - 6f));
            var badgeRect = new RectangleF(
                e.Bounds.Left + 6f,
                e.Bounds.Top + (e.Bounds.Height - badgeSize) / 2f,
                badgeSize,
                badgeSize);

            using var badgePath = Theme.RoundedRectangle(badgeRect, 5f);
            using var badgeFill = new SolidBrush(visual.SoftBackground);
            using var badgeBorder = new Pen(visual.BorderColor, 1f);
            g.FillPath(badgeFill, badgePath);
            g.DrawPath(badgeBorder, badgePath);

            var iconInset = Math.Max(2.2f, badgeSize * 0.16f);
            var iconBounds = RectangleF.Inflate(badgeRect, -iconInset, -iconInset);
            VectorIcons.Draw(g, visual.Icon, iconBounds, visual.Accent, 1.45f);

            var textLeft = (int)Math.Ceiling(badgeRect.Right + 7f);
            var textRect = new Rectangle(
                textLeft,
                e.Bounds.Top,
                Math.Max(20, e.Bounds.Right - textLeft - 4),
                e.Bounds.Height);

            var textColor = isSelected && !isComboBoxEdit ? Theme.Primary : Theme.Text;
            TextRenderer.DrawText(
                g,
                text,
                combo.Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            g.Restore(state);
        };
    }

    private static string NormalizeToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var formD = raw.Trim().Normalize(System.Text.NormalizationForm.FormD);
        var chars = new char[formD.Length];
        var len = 0;

        foreach (var ch in formD)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            chars[len++] = ch switch
            {
                'đ' or 'Đ' => 'D',
                _ => char.ToUpperInvariant(ch)
            };
        }

        return new string(chars, 0, len).Normalize(System.Text.NormalizationForm.FormC);
    }

    private static Color MixColors(Color a, Color b, float weightB)
    {
        var t = Math.Clamp(weightB, 0f, 1f);
        var inv = 1f - t;
        return Color.FromArgb(
            (int)Math.Round(a.A * inv + b.A * t),
            (int)Math.Round(a.R * inv + b.R * t),
            (int)Math.Round(a.G * inv + b.G * t),
            (int)Math.Round(a.B * inv + b.B * t));
    }
}

public sealed class RoleBadgePill : Panel
{
    private RoleVisualInfo _info = RoleVisualResolver.Unknown();
    private string? _labelOverride;
    private ToolTip? _toolTip;

    public RoleBadgePill()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Height = 28;
        Width = 156;
        Margin = Padding.Empty;
    }

    public bool AutoWidth { get; set; } = true;

    public void SetRole(RoleVisualInfo info, ToolTip? toolTip = null, string? labelOverride = null)
    {
        _info = info;
        _labelOverride = labelOverride;
        _toolTip = toolTip ?? _toolTip;

        if (_toolTip is not null)
        {
            var tip = string.IsNullOrWhiteSpace(info.TooltipDetail)
                ? info.DisplayLabel
                : $"{info.DisplayLabel}: {info.TooltipDetail}";
            _toolTip.SetToolTip(this, tip);
        }

        if (AutoWidth)
        {
            using var font = Theme.Font(8.4f, FontStyle.Bold);
            var text = _labelOverride ?? _info.DisplayLabel;
            var textSize = TextRenderer.MeasureText(text, font);
            Width = Math.Clamp(textSize.Width + 40, 108, 250);
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f));
        using var path = Theme.RoundedRectangle(bounds, Height / 2f);
        using var fill = new SolidBrush(_info.SoftBackground);
        using var border = new Pen(_info.BorderColor, 1f);
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        var iconSize = Math.Min(15f, Math.Max(12f, Height - 12f));
        var iconRect = new RectangleF(bounds.Left + 10f, bounds.Top + (bounds.Height - iconSize) / 2f, iconSize, iconSize);
        VectorIcons.Draw(g, _info.Icon, iconRect, _info.Accent, 1.55f);

        var textLeft = (int)Math.Ceiling(iconRect.Right + 6f);
        var textRect = new Rectangle(textLeft, 0, Math.Max(20, Width - textLeft - 10), Height);
        using var font = Theme.Font(8.4f, FontStyle.Bold);
        TextRenderer.DrawText(
            g,
            _labelOverride ?? _info.DisplayLabel,
            font,
            textRect,
            _info.Accent,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }
}

internal sealed class RoleProfileDetailDialog : Form
{
    private readonly ToolTip _toolTip = new();

    public RoleProfileDetailDialog(
        string windowTitle,
        string overlineText,
        string primaryTitle,
        RoleVisualInfo roleVisual,
        IReadOnlyList<(string Label, string Value, Color? ValueColor, bool Highlight)> fields)
    {
        Text = windowTitle;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 440);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = roleVisual.Accent,
            AccentStripeThickness = 4
        };

        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = overlineText,
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = primaryTitle,
            Font = Theme.Font(14f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var badgeHost = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(0, 4, 0, 4) };
        var badge = new RoleBadgePill { Height = 28, Location = new Point(0, 4) };
        badge.SetRole(roleVisual, _toolTip);
        badgeHost.Controls.Add(badge);

        var dossier = new DossierFieldList { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        dossier.SetRows(fields);

        var close = Theme.OutlineButton("Đóng", Theme.BorderStrong, IconKind.Close);
        close.Dock = DockStyle.Bottom;
        close.Height = 38;
        close.Click += (_, _) => Close();

        card.Controls.Add(dossier);
        card.Controls.Add(close);
        card.Controls.Add(badgeHost);
        card.Controls.Add(title);
        card.Controls.Add(overline);

        Controls.Add(card);
        CancelButton = close;
        AcceptButton = close;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();

        base.Dispose(disposing);
    }
}
