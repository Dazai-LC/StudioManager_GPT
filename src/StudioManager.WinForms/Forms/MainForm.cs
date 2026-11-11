using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Pages;

namespace StudioManager.WinForms.Forms;

public sealed class MainForm : Form
{
    private const int RailWidth = 112;

    private readonly AppFacade _app;
    private DashboardData? _initialDashboardData;

    private readonly Panel _content = new()
    {
        BackColor = Theme.Background,
        Padding = new Padding(20, 14, 20, 18)
    };

    private readonly FlowLayoutPanel _railMenu = new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(0, 4, 0, 0),
        BackColor = Theme.Sidebar
    };

    private readonly Label _hubBreadcrumb = new()
    {
        Dock = DockStyle.Top,
        Height = 20,
        Text = "ATELIER LUMIÈRE  /  BÀN LÀM VIỆC",
        Font = Theme.MonoFont(7.6f, FontStyle.Bold),
        ForeColor = Theme.Primary,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly Label _hubTitle = new()
    {
        Dock = DockStyle.Fill,
        Text = "Bàn điều khiển trung tâm",
        Font = Theme.Font(14.5f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly SegmentedTabBar _subNav = new()
    {
        Dock = DockStyle.Fill,
        Visible = false
    };

    private readonly Panel _subNavHost = new()
    {
        Height = 50,
        Padding = new Padding(20, 6, 20, 6),
        BackColor = Theme.SurfaceWarm,
        Visible = false
    };

    private readonly ToolTip _toolTip = new();
    private readonly List<RailNavButton> _railButtons = [];
    private string _activeHubKey = "dashboard";
    private string _activeSubKey = "";

    private ObsidianRailPanel _rail = null!;
    private WorkspaceHostPanel _workspaceHost = null!;

    public MainForm(AppFacade app, DashboardData? initialDashboardData = null)
    {
        _app = app;
        _initialDashboardData = initialDashboardData;
        Text = "StudioManager — Atelier Lumière Command Suite";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1220, 740);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();
        KeyPreview = true;

        BuildObsidianRail();
        var topHeader = BuildTopCommandHeader();
        BuildSubNavigationStrip();

        _workspaceHost = new WorkspaceHostPanel(topHeader, _subNavHost, _content);

        Controls.Add(_workspaceHost);
        Controls.Add(_rail);

        KeyDown += OnMainFormKeyDown;
        Shown += (_, _) =>
        {
            Activate();
            BringToFront();
        };
        NavigateTo("dashboard");
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int clientW = ClientSize.Width;
        int clientH = ClientSize.Height;
        if (clientW <= 0 || clientH <= 0 || _rail == null || _workspaceHost == null) return;

        int railW = RailWidth;
        _rail.SetBounds(0, 0, railW, clientH);
        _workspaceHost.SetBounds(railW, 0, Math.Max(0, clientW - railW), clientH);
    }

    private void OnMainFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.K)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            OpenCommandPalette();
            return;
        }

        if (e.Alt)
        {
            switch (e.KeyCode)
            {
                case Keys.D1: NavigateTo("dashboard"); e.Handled = true; break;
                case Keys.D2: NavigateTo("bookings"); e.Handled = true; break;
                case Keys.D3: NavigateTo("catalog"); e.Handled = true; break;
                case Keys.D4 when IsAdmin: NavigateTo("reports"); e.Handled = true; break;
                case Keys.D5 when IsAdmin: NavigateTo("system"); e.Handled = true; break;
            }
        }
    }

    private bool IsAdmin => _app.Session?.VaiTro == VaiTro.QuanTriVien;

    private void BuildObsidianRail()
    {
        var brandLogo = new Panel
        {
            Height = 64,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        brandLogo.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var cx = brandLogo.Width / 2f;
            var cy = brandLogo.Height / 2f;
            var box = new RectangleF(cx - 22, cy - 22, 44, 44);
            using var path = Theme.RoundedRectangle(box, 12f);
            using var fill = new SolidBrush(Theme.SidebarCard);
            using var border = new Pen(Theme.Primary, 1.5f);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
            VectorIcons.Draw(g, IconKind.Aperture, new RectangleF(cx - 12, cy - 12, 24, 24), Theme.Primary, 1.9f);
        };
        brandLogo.Click += (_, _) => NavigateTo("dashboard");
        _toolTip.SetToolTip(brandLogo, "StudioManager — Atelier Lumière (Alt+1)");

        AddRailItem("dashboard", "Bàn điều khiển", IconKind.Dashboard, "Bàn điều khiển trung tâm (Alt+1)");
        AddRailItem("bookings", "Lịch chụp", IconKind.Calendar, "Điều phối Lịch chụp & Phòng máy (Alt+2)");
        AddRailItem("catalog", "Danh mục", IconKind.Layers, IsAdmin
            ? "Khách hàng, Nhân sự, Gói chụp, Dịch vụ, Phòng & Thiết bị (Alt+3)"
            : "Hồ sơ Khách hàng (Alt+3)");

        if (IsAdmin)
        {
            AddRailItem("reports", "Phân tích", IconKind.ChartBar, "Báo cáo Doanh thu & Hiệu suất Gói chụp (Alt+4)");
            AddRailItem("system", "Hệ thống", IconKind.Shield, "Tài khoản, Nhật ký Kiểm toán & Sao lưu (Alt+5)");
        }

        var footer = new Panel
        {
            Height = 128,
            BackColor = Color.Transparent
        };

        var passBtn = new RailNavButton("password", "Mật khẩu", IconKind.Key)
        {
            Width = RailWidth - 12,
            Height = 56,
            Location = new Point(6, 6)
        };
        passBtn.Click += (_, _) =>
        {
            if (_app.Session is null) return;
            using var dlg = new ChangePasswordForm(_app, _app.Session);
            dlg.ShowDialog(this);
        };
        _toolTip.SetToolTip(passBtn, "Đổi mật khẩu tài khoản hiện tại");

        var logoutBtn = new RailNavButton("logout", "Thoát", IconKind.LogOut, isDanger: true)
        {
            Width = RailWidth - 12,
            Height = 56,
            Location = new Point(6, 66)
        };
        logoutBtn.Click += (_, _) => Close();
        _toolTip.SetToolTip(logoutBtn, "Đăng xuất khỏi phiên làm việc");

        footer.Controls.Add(passBtn);
        footer.Controls.Add(logoutBtn);

        _rail = new ObsidianRailPanel(brandLogo, _railMenu, footer);
    }

    private void AddRailItem(string key, string shortLabel, IconKind icon, string tooltip)
    {
        var btn = new RailNavButton(key, shortLabel, icon)
        {
            Width = RailWidth - 12,
            Height = 64,
            Margin = new Padding(6, 2, 6, 4)
        };
        btn.Click += (_, _) => NavigateTo(key);
        _toolTip.SetToolTip(btn, tooltip);
        _railButtons.Add(btn);
        _railMenu.Controls.Add(btn);
    }

    private Control BuildTopCommandHeader()
    {
        var header = new TopCommandBarPanel
        {
            Height = 62,
            BackColor = Theme.Surface
        };

        var leftTitles = new Panel
        {
            BackColor = Color.Transparent
        };
        _hubBreadcrumb.Dock = DockStyle.None;
        _hubTitle.Dock = DockStyle.None;
        leftTitles.Controls.Add(_hubBreadcrumb);
        leftTitles.Controls.Add(_hubTitle);
        leftTitles.Layout += (_, _) =>
        {
            int lw = leftTitles.ClientSize.Width;
            int lh = leftTitles.ClientSize.Height;
            if (lw <= 0 || lh <= 0) return;
            int bcH = Math.Max(19, _hubBreadcrumb.PreferredHeight + 2);
            _hubBreadcrumb.SetBounds(0, 0, lw, bcH);
            _hubTitle.SetBounds(0, bcH, lw, Math.Max(26, lh - bcH));
        };

        var userPill = BuildUserBadgePill();
        var cmdButton = Theme.OutlineButton("Tìm nhanh lệnh & màn hình (Ctrl+K)", Theme.BorderStrong, IconKind.Command);
        cmdButton.Height = 40;
        cmdButton.Width = Math.Max(264, cmdButton.RequiredWidth + 6);
        cmdButton.Margin = Padding.Empty;
        cmdButton.Click += (_, _) => OpenCommandPalette();

        header.SetChildren(leftTitles, cmdButton, userPill);
        return header;
    }

    private Control BuildUserBadgePill()
    {
        var session = _app.Session;
        var fullName = session?.HoTen ?? "Nhân sự Studio";
        var isAdminRole = session?.VaiTro == VaiTro.QuanTriVien;
        var roleName = isAdminRole ? "QUẢN TRỊ VIÊN" : "NHÂN VIÊN";
        var roleColor = isAdminRole ? Theme.Primary : Theme.Primitives.Teal600;
        var initials = Initials(fullName);

        var pill = new Panel
        {
            Width = 236,
            Height = 42,
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };

        pill.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, pill.Width - 1), Math.Max(1, pill.Height - 1));
            using var path = Theme.RoundedRectangle(bounds, pill.Height / 2f);
            using var bg = new SolidBrush(Theme.SurfaceWarm);
            using var border = new Pen(Theme.BorderStrong, 1f);
            g.FillPath(bg, path);
            g.DrawPath(border, path);

            // Avatar circle on right
            const float avSize = 32f;
            var avRect = new RectangleF(pill.Width - avSize - 5f, (pill.Height - avSize) / 2f, avSize, avSize);
            using var avFill = new SolidBrush(roleColor);
            g.FillEllipse(avFill, avRect);

            using var avFont = Theme.Font(8.8f, FontStyle.Bold);
            using var whiteBrush = new SolidBrush(Color.White);
            var avSz = g.MeasureString(initials, avFont);
            g.DrawString(initials, avFont, whiteBrush, avRect.Left + (avRect.Width - avSz.Width) / 2f, avRect.Top + (avRect.Height - avSz.Height) / 2f);

            // Name & Role
            using var nameFont = Theme.Font(8.8f, FontStyle.Bold);
            using var roleFont = Theme.MonoFont(7.2f, FontStyle.Bold);
            var halfH = pill.Height / 2;
            var nameRect = new Rectangle(12, 3, Math.Max(40, pill.Width - 54), Math.Max(16, halfH - 2));
            var roleRect = new Rectangle(12, halfH, Math.Max(40, pill.Width - 54), Math.Max(14, pill.Height - halfH - 3));

            TextRenderer.DrawText(g, fullName, nameFont, nameRect, Theme.Text,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, roleName, roleFont, roleRect, roleColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        };

        return pill;
    }

    private void BuildSubNavigationStrip()
    {
        _subNavHost.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawLine(pen, 0, _subNavHost.Height - 1, _subNavHost.Width, _subNavHost.Height - 1);
        };
        _subNav.SegmentSelected += (_, seg) => OpenSubView(_activeHubKey, seg.Key);
        _subNavHost.Controls.Add(_subNav);
        _subNavHost.VisibleChanged += (_, _) =>
        {
            if (_workspaceHost != null && _workspaceHost.SubNavVisible != _subNavHost.Visible)
            {
                _workspaceHost.SubNavVisible = _subNavHost.Visible;
            }
        };
    }

    public void NavigateTo(string hubKey, string? subKey = null)
    {
        try
        {
            _activeHubKey = hubKey;
            foreach (var btn in _railButtons)
                btn.IsActive = string.Equals(btn.Key, hubKey, StringComparison.OrdinalIgnoreCase);

            switch (hubKey)
            {
                case "dashboard":
                    _subNav.Visible = false;
                    _workspaceHost.SubNavVisible = false;
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  WORKSPACE 01";
                    _hubTitle.Text = "Bàn điều khiển trung tâm";
                    MountPage(CreateDashboard());
                    break;

                case "bookings":
                    var bookingTabs = new List<SegmentedTabBar.SegmentItem>
                    {
                        new("LichChup", "Lịch chụp chính thức", IconKind.Calendar),
                        new("YeuCauDatLich", "Yêu cầu đặt lịch", IconKind.Clock),
                        new("DoiSoatThanhToan", "Đối soát & Thanh toán", IconKind.Wallet)
                    };
                    var targetBookingKey = subKey ?? (_activeSubKey is "LichChup" or "YeuCauDatLich" or "DoiSoatThanhToan" ? _activeSubKey : "LichChup");
                    _subNav.SetItems(bookingTabs, targetBookingKey);
                    _subNav.Visible = true;
                    _workspaceHost.SubNavVisible = true;
                    OpenSubView("bookings", targetBookingKey);
                    break;

                case "catalog":
                    var catalogTabs = new List<SegmentedTabBar.SegmentItem>
                    {
                        new("KhachHang", "Khách hàng", IconKind.Users)
                    };
                    if (IsAdmin)
                    {
                        catalogTabs.AddRange([
                            new("NhanVien", "Nhân sự & Nhiếp ảnh gia", IconKind.UserCheck),
                            new("GoiChup", "Gói chụp Studio", IconKind.Camera),
                            new("DichVu", "Dịch vụ bổ sung", IconKind.Sparkles),
                            new("PhongChup", "Phòng chụp", IconKind.Home),
                            new("TaiNguyen", "Thiết bị & Tài nguyên", IconKind.Layers)
                        ]);
                    }
                    var targetCatalogKey = subKey ?? (_activeSubKey is "KhachHang" or "NhanVien" or "GoiChup" or "DichVu" or "PhongChup" or "TaiNguyen" ? _activeSubKey : "KhachHang");
                    if (!IsAdmin) targetCatalogKey = "KhachHang";
                    _subNav.SetItems(catalogTabs, targetCatalogKey);
                    _subNav.Visible = true;
                    _workspaceHost.SubNavVisible = true;
                    OpenSubView("catalog", targetCatalogKey);
                    break;

                case "reports":
                    if (!IsAdmin) return;
                    _subNav.Visible = false;
                    _workspaceHost.SubNavVisible = false;
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  WORKSPACE 04";
                    _hubTitle.Text = "Phân tích Doanh thu & Hiệu suất Kinh doanh";
                    MountPage(new ReportsPage(_app));
                    break;

                case "system":
                    if (!IsAdmin) return;
                    var sysTabs = new List<SegmentedTabBar.SegmentItem>
                    {
                        new("TaiKhoan", "Tài khoản & Phân quyền", IconKind.Shield),
                        new("NhatKy", "Nhật ký Kiểm toán", IconKind.History),
                        new("SaoLuu", "Kho Sao lưu & Phục hồi SQL", IconKind.Database)
                    };
                    var targetSysKey = subKey ?? (_activeSubKey is "TaiKhoan" or "NhatKy" or "SaoLuu" ? _activeSubKey : "TaiKhoan");
                    _subNav.SetItems(sysTabs, targetSysKey);
                    _subNav.Visible = true;
                    _workspaceHost.SubNavVisible = true;
                    OpenSubView("system", targetSysKey);
                    break;
            }
            _workspaceHost.PerformLayout();
        }
        catch (Exception ex)
        {
            Ui.LogError($"MainForm.NavigateTo({hubKey}, {subKey})", ex);
            if (!IsHandleCreated)
            {
                throw;
            }
            Ui.Error(this, Ui.FormatRuntimeError($"Điều hướng phân hệ '{hubKey}'", ex));
        }
    }

    private void OpenSubView(string hubKey, string subKey)
    {
        _activeSubKey = subKey;
        if (hubKey == "bookings")
        {
            switch (subKey)
            {
                case "YeuCauDatLich":
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  ĐIỀU PHỐI VẬN HÀNH  /  YÊU CẦU ĐẶT LỊCH";
                    _hubTitle.Text = "Hộp thư Tiếp nhận Yêu cầu Đặt lịch Trực tuyến";
                    MountPage(new BookingRequestsPlaceholderPage(_app));
                    break;

                case "DoiSoatThanhToan":
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  ĐIỀU PHỐI VẬN HÀNH  /  ĐỐI SOÁT & THANH TOÁN";
                    _hubTitle.Text = "Trung tâm Đối soát & Cổng Thanh toán Số";
                    MountPage(new PaymentWorkspacePlaceholderPage(_app));
                    break;

                case "LichChup":
                default:
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  ĐIỀU PHỐI VẬN HÀNH  /  LỊCH CHỤP CHÍNH THỨC";
                    _hubTitle.Text = "Điều phối Lịch chụp & Tiến độ Hậu kỳ";
                    MountPage(new BookingsPage(_app));
                    break;
            }
        }
        else if (hubKey == "catalog")
        {
            var (title, entity) = subKey switch
            {
                "NhanVien" => ("Hồ sơ Nhân sự & Nhiếp ảnh gia", "NhanVien"),
                "GoiChup" => ("Danh mục Gói chụp Studio", "GoiChup"),
                "DichVu" => ("Danh mục Dịch vụ bổ sung", "DichVu"),
                "PhongChup" => ("Không gian Phòng chụp", "PhongChup"),
                "TaiNguyen" => ("Thiết bị, Đạo cụ & Tài nguyên", "TaiNguyen"),
                _ => ("Hồ sơ Khách hàng", "KhachHang")
            };
            _hubBreadcrumb.Text = $"ATELIER LUMIÈRE  //  DANH MỤC VẬN HÀNH  /  {title.ToUpperInvariant()}";
            _hubTitle.Text = title;
            MountPage(new CrudPage(_app, title, entity));
        }
        else if (hubKey == "system")
        {
            switch (subKey)
            {
                case "NhatKy":
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  TRUNG TÂM HỆ THỐNG  /  NHẬT KÝ KIỂM TOÁN";
                    _hubTitle.Text = "Nhật ký Kiểm toán & Truy vết Dữ liệu";
                    MountPage(new CrudPage(_app, "Nhật ký hệ thống", "NhatKy", true));
                    break;
                case "SaoLuu":
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  TRUNG TÂM HỆ THỐNG  /  KHO SAO LƯU SQL";
                    _hubTitle.Text = "Kho Sao lưu & Phục hồi Cơ sở dữ liệu";
                    MountPage(new BackupPage(_app));
                    break;
                default:
                    _hubBreadcrumb.Text = "ATELIER LUMIÈRE  //  TRUNG TÂM HỆ THỐNG  /  TÀI KHOẢN & PHÂN QUYỀN";
                    _hubTitle.Text = "Quản lý Tài khoản & Bảo mật Truy cập";
                    MountPage(new AccountsPage(_app));
                    break;
            }
        }
    }

    private void MountPage(Control page)
    {
        _content.SuspendLayout();
        _content.Controls.Clear();
        page.Dock = DockStyle.Fill;
        _content.Controls.Add(page);
        _content.ResumeLayout(true);
    }

    private DashboardPage CreateDashboard()
    {
        var page = new DashboardPage(_app, _initialDashboardData);
        _initialDashboardData = null;
        page.ViewBookingsRequested += (_, _) => NavigateTo("bookings");
        return page;
    }

    private void OpenCommandPalette()
    {
        using var palette = new QuickCommandPaletteDialog(IsAdmin);
        if (palette.ShowDialog(this) == DialogResult.OK && palette.SelectedCommand is { } cmd)
        {
            if (cmd.HubKey == "action:password")
            {
                if (_app.Session is not null)
                {
                    using var dlg = new ChangePasswordForm(_app, _app.Session);
                    dlg.ShowDialog(this);
                }
                return;
            }
            NavigateTo(cmd.HubKey, cmd.SubKey);
        }
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "U" : string.Concat(parts.TakeLast(Math.Min(2, parts.Length)).Select(x => char.ToUpperInvariant(x[0])));
    }
}

internal sealed class RailNavButton : Control
{
    public string Key { get; }
    private readonly string _label;
    private readonly IconKind _icon;
    private readonly bool _isDanger;
    private bool _isActive;
    private bool _isHovered;

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    public RailNavButton(string key, string label, IconKind icon, bool isDanger = false)
    {
        Key = key;
        _label = label;
        _icon = icon;
        _isDanger = isDanger;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        BackColor = Theme.Sidebar;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovered = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var box = new RectangleF(2, 2, Width - 4, Height - 4);
        if (_isActive || _isHovered)
        {
            using var path = Theme.RoundedRectangle(box, 10f);
            var fillCol = _isActive
                ? Theme.SidebarActiveBg
                : (_isDanger ? Color.FromArgb(55, Theme.Danger) : Theme.SidebarHover);
            using var fill = new SolidBrush(fillCol);
            g.FillPath(fill, path);

            if (_isActive)
            {
                using var border = new Pen(Color.FromArgb(120, Theme.Primary), 1f);
                g.DrawPath(border, path);

                // Left active indicator pill
                using var barBrush = new SolidBrush(Theme.Primary);
                using var barPath = Theme.RoundedRectangle(new RectangleF(0, Height * 0.22f, 3.5f, Height * 0.56f), 2f);
                g.FillPath(barBrush, barPath);
            }
        }

        var fg = _isActive
            ? Theme.Primitives.Terracotta200
            : (_isDanger && _isHovered ? Theme.Primitives.Crimson200 : (_isHovered ? Color.White : Theme.SidebarMuted));

        var iconColor = _isActive ? Theme.Primary : fg;
        var iconY = 8f;
        VectorIcons.Draw(g, _icon, new RectangleF((Width - 22f) / 2f, iconY, 22f, 22f), iconColor, _isActive ? 2.0f : 1.75f);

        const TextFormatFlags textFlags = TextFormatFlags.HorizontalCenter |
                                          TextFormatFlags.VerticalCenter |
                                          TextFormatFlags.SingleLine |
                                          TextFormatFlags.NoPadding;

        var textRect = new Rectangle(2, 34, Width - 4, 26);
        float fontSize = _isActive ? 7.6f : 7.3f;
        using (var testFont = Theme.Font(fontSize, _isActive ? FontStyle.Bold : FontStyle.Regular))
        {
            var sz = TextRenderer.MeasureText(g, _label, testFont, Size.Empty, textFlags);
            if (sz.Width > textRect.Width)
            {
                float ratio = (float)(textRect.Width - 2) / sz.Width;
                fontSize = Math.Max(5.8f, fontSize * ratio);
            }
        }

        using var font = Theme.Font(fontSize, _isActive ? FontStyle.Bold : FontStyle.Regular);
        TextRenderer.DrawText(g, _label, font, textRect, fg, textFlags);
    }
}

internal sealed class TopCommandBarPanel : Panel
{
    private Control? _leftTitles;
    private ModernButton? _cmdButton;
    private Control? _userPill;

    public TopCommandBarPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void SetChildren(Control leftTitles, ModernButton cmdButton, Control userPill)
    {
        _leftTitles = leftTitles;
        _cmdButton = cmdButton;
        _userPill = userPill;
        Controls.Add(userPill);
        Controls.Add(cmdButton);
        Controls.Add(leftTitles);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 0 || h <= 0 || _leftTitles == null || _cmdButton == null || _userPill == null) return;

        int pillW = 236;
        int pillH = 42;
        int pillX = Math.Max(20, w - 20 - pillW);
        int pillY = Math.Max(4, (h - pillH) / 2);
        _userPill.SetBounds(pillX, pillY, pillW, pillH);

        int cmdW = Math.Clamp(_cmdButton.RequiredWidth + 8, 240, Math.Max(240, Math.Min(330, pillX - 240)));
        int cmdH = 40;
        int cmdX = Math.Max(20, pillX - 12 - cmdW);
        int cmdY = Math.Max(4, (h - cmdH) / 2);
        _cmdButton.SetBounds(cmdX, cmdY, cmdW, cmdH);

        int leftW = Math.Max(100, cmdX - 36);
        int leftPadV = 8;
        _leftTitles.SetBounds(20, leftPadV, leftW, Math.Max(40, h - leftPadV * 2));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border, 1f);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }
}

internal sealed class WorkspaceHostPanel : Panel
{
    public const int HeaderHeight = 72;
    public const int SubNavHeight = 50;

    private readonly Control _header;
    private readonly Control _subNav;
    private readonly Control _content;
    private bool _subNavVisible;

    public bool SubNavVisible
    {
        get => _subNavVisible;
        set
        {
            if (_subNavVisible != value)
            {
                _subNavVisible = value;
                _subNav.Visible = value;
                PerformLayout();
                Invalidate();
            }
        }
    }

    public WorkspaceHostPanel(Control header, Control subNav, Control content)
    {
        _header = header;
        _subNav = subNav;
        _content = content;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Background;

        Controls.Add(_header);
        Controls.Add(_subNav);
        Controls.Add(_content);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 0 || h <= 0) return;

        _header.Visible = true;
        _header.SetBounds(0, 0, w, HeaderHeight);

        int subH = _subNavVisible ? SubNavHeight : 0;
        _subNav.Visible = _subNavVisible;
        _subNav.SetBounds(0, HeaderHeight, w, subH);

        int contentY = HeaderHeight + subH;
        _content.SetBounds(0, contentY, w, Math.Max(0, h - contentY));
    }
}

internal sealed class ObsidianRailPanel : Panel
{
    private readonly Control _brandLogo;
    private readonly Control _menu;
    private readonly Control _footer;

    public ObsidianRailPanel(Control brandLogo, Control menu, Control footer)
    {
        _brandLogo = brandLogo;
        _menu = menu;
        _footer = footer;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Sidebar;

        Controls.Add(_brandLogo);
        Controls.Add(_menu);
        Controls.Add(_footer);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        int w = ClientSize.Width;
        int h = ClientSize.Height;
        if (w <= 0 || h <= 0) return;

        int logoH = 64;
        int footerH = 128;

        _brandLogo.SetBounds(0, 0, w, logoH);
        _footer.SetBounds(0, Math.Max(logoH, h - footerH), w, footerH);
        int menuY = logoH;
        int menuH = Math.Max(0, h - logoH - footerH);
        _menu.SetBounds(0, menuY, w, menuH);
    }
}

internal sealed class QuickCommandPaletteDialog : Form
{
    public sealed record CommandEntry(string Title, string Category, string HubKey, string? SubKey, IconKind Icon);

    private readonly List<CommandEntry> _all = [];
    private List<CommandEntry> _filtered = [];
    private readonly SearchInput _search = new("Gõ tên màn hình, danh mục hoặc chức năng cần mở...") { Dock = DockStyle.Top, Height = 42 };
    private readonly ListBox _list = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 46,
        BackColor = Theme.Surface,
        Font = Theme.Font(9.5f)
    };

    public CommandEntry? SelectedCommand { get; private set; }

    public QuickCommandPaletteDialog(bool isAdmin)
    {
        Text = "Bảng điều hướng nhanh (Command Palette)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(580, 440);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Background;
        Font = Theme.Font();
        KeyPreview = true;

        _all.Add(new("Bàn điều khiển trung tâm (Dashboard)", "Bàn làm việc", "dashboard", null, IconKind.Dashboard));
        _all.Add(new("Lịch chụp chính thức & Tiến độ", "Lịch chụp & Vận hành", "bookings", "LichChup", IconKind.Calendar));
        _all.Add(new("Hộp thư Yêu cầu đặt lịch trực tuyến (v2.2 Demo)", "Lịch chụp & Vận hành", "bookings", "YeuCauDatLich", IconKind.Clock));
        _all.Add(new("Trung tâm Đối soát & Thanh toán (v2.2 Demo)", "Lịch chụp & Vận hành", "bookings", "DoiSoatThanhToan", IconKind.Wallet));
        _all.Add(new("Hồ sơ Khách hàng", "Khách & Danh mục", "catalog", "KhachHang", IconKind.Users));

        if (isAdmin)
        {
            _all.Add(new("Hồ sơ Nhân sự & Nhiếp ảnh gia", "Khách & Danh mục", "catalog", "NhanVien", IconKind.UserCheck));
            _all.Add(new("Danh mục Gói chụp Studio", "Khách & Danh mục", "catalog", "GoiChup", IconKind.Camera));
            _all.Add(new("Danh mục Dịch vụ bổ sung", "Khách & Danh mục", "catalog", "DichVu", IconKind.Sparkles));
            _all.Add(new("Danh mục Phòng chụp", "Khách & Danh mục", "catalog", "PhongChup", IconKind.Home));
            _all.Add(new("Thiết bị, Đạo cụ & Tài nguyên", "Khách & Danh mục", "catalog", "TaiNguyen", IconKind.Layers));
            _all.Add(new("Báo cáo Doanh thu & Hiệu suất", "Phân tích", "reports", null, IconKind.ChartBar));
            _all.Add(new("Tài khoản & Phân quyền truy cập", "Hệ thống", "system", "TaiKhoan", IconKind.Shield));
            _all.Add(new("Nhật ký Kiểm toán hệ thống", "Hệ thống", "system", "NhatKy", IconKind.History));
            _all.Add(new("Kho Sao lưu & Phục hồi SQL Server", "Hệ thống", "system", "SaoLuu", IconKind.Database));
        }
        _all.Add(new("Đổi mật khẩu bảo mật cá nhân", "Tài khoản", "action:password", null, IconKind.Key));

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12),
            Padding = new Padding(16),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 3
        };

        var hint = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = "Sử dụng phím ↑↓ để di chuyển  •  Enter để mở  •  Esc để đóng",
            Font = Theme.Font(8.0f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.BottomLeft
        };

        var spacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

        _list.DrawItem += DrawCommandItem;
        _list.DoubleClick += (_, _) => ConfirmSelection();
        _search.TextChanged += (_, _) => FilterCommands();
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down && _list.Items.Count > 0)
            {
                _list.SelectedIndex = Math.Min(_list.Items.Count - 1, _list.SelectedIndex + 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && _list.Items.Count > 0)
            {
                _list.SelectedIndex = Math.Max(0, _list.SelectedIndex - 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ConfirmSelection();
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
            }
        };

        card.Controls.Add(_list);
        card.Controls.Add(spacer);
        card.Controls.Add(_search);
        card.Controls.Add(hint);
        Controls.Add(card);

        FilterCommands();
        Shown += (_, _) => _search.Focus();
    }

    private void FilterCommands()
    {
        var q = _search.Text.Trim();
        _filtered = string.IsNullOrWhiteSpace(q)
            ? _all.ToList()
            : _all.Where(x => x.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                              x.Category.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in _filtered) _list.Items.Add(item.Title);
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        _list.EndUpdate();
    }

    private void ConfirmSelection()
    {
        if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _filtered.Count)
        {
            SelectedCommand = _filtered[_list.SelectedIndex];
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void DrawCommandItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _filtered.Count) return;
        var item = _filtered[e.Index];
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var bg = new SolidBrush(selected ? Theme.PrimaryLight : Theme.Surface);
        g.FillRectangle(bg, e.Bounds);

        if (selected)
        {
            using var bar = new SolidBrush(Theme.Primary);
            g.FillRectangle(bar, e.Bounds.Left, e.Bounds.Top + 6, 3, e.Bounds.Height - 12);
        }

        VectorIcons.Draw(g, item.Icon, new RectangleF(e.Bounds.Left + 14, e.Bounds.Top + 14, 18, 18),
            selected ? Theme.Primary : Theme.TextSecondary, 1.8f);

        using var titleFont = Theme.Font(9.4f, selected ? FontStyle.Bold : FontStyle.Regular);
        using var catFont = Theme.MonoFont(7.8f, FontStyle.Bold);

        TextRenderer.DrawText(g, item.Title, titleFont,
            new Rectangle(e.Bounds.Left + 44, e.Bounds.Top, e.Bounds.Width - 160, e.Bounds.Height),
            selected ? Theme.Primitives.Terracotta800 : Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, item.Category.ToUpperInvariant(), catFont,
            new Rectangle(e.Bounds.Right - 140, e.Bounds.Top, 128, e.Bounds.Height),
            selected ? Theme.Primary : Theme.Muted,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }
}
