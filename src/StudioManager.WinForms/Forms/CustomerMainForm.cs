using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.CustomerPages;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Forms;

/// <summary>
/// Isolated native WinForms shell for Experience A (Customer Desktop Portal Demo).
/// Completely separated from internal Staff/Admin MainForm hubs, ledger operations, and SQL mutations.
/// </summary>
public sealed class CustomerMainForm : Form, ICustomerNavigator
{
    private readonly AppFacade _app;
    private readonly CustomerSessionContext _session;
    private readonly ToolTip _toolTip = new();

    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill, BackColor = Theme.Background };
    private readonly FlowLayoutPanel _navStack = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = Color.Transparent,
        Padding = new Padding(10, 6, 10, 6)
    };
    private readonly Label _topBreadcrumbLabel = new() { Dock = DockStyle.Top, Height = 20, Margin = Padding.Empty, UseMnemonic = false, Font = Theme.MonoFont(7.6f, FontStyle.Bold), ForeColor = Theme.Primitives.Sage700, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true };
    private readonly Label _topTitleLabel = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, UseMnemonic = false, Font = Theme.Font(13.2f, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly RoleBadgePill _rolePill = new();
    private readonly Label _userSummaryLabel = new() { Dock = DockStyle.Fill, Font = Theme.Font(8.4f), ForeColor = Theme.SidebarText };

    private string _activeKey = "overview";
    private readonly Dictionary<string, NavTileButton> _navButtons = new(StringComparer.OrdinalIgnoreCase);

    public CustomerMainForm(AppFacade app, CustomerSessionContext session)
    {
        _app = app;
        _session = session;
        DesktopDemoSessionStore.EnsureSeeded();

        Text = $"Atelier Lumière — Cổng Khách hàng (Demo) • {session.Profile.FullName}";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1320, 820);
        MinimumSize = new Size(1040, 660);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Left Sidebar Rail
        var sidebar = BuildSidebarRail();
        root.SetRowSpan(sidebar, 2);

        // Top Header Bar
        var topBar = BuildTopBar();

        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(topBar, 1, 0);
        root.Controls.Add(_contentHost, 1, 1);

        Controls.Add(root);
        RefreshBadgeCounts();
        NavigateTo("overview");
    }

    private Control BuildSidebarRail()
    {
        var rail = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Sidebar };
        var sep = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Primitives.Espresso800 };

        var brand = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16, 14, 12, 10), BackColor = Theme.Sidebar };
        brand.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            VectorIcons.Draw(g, IconKind.Aperture, new RectangleF(16, 26, 24, 24), Theme.Primitives.Sage200, 1.9f);
            using var f1 = Theme.Font(10.2f, FontStyle.Bold);
            using var f2 = Theme.MonoFont(7.6f, FontStyle.Bold);
            TextRenderer.DrawText(g, "ATELIER LUMIÈRE", f1, new Rectangle(48, 19, 184, 22), Theme.Primitives.Alabaster50, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "CỔNG KHÁCH HÀNG • DEMO", f2, new Rectangle(48, 43, 184, 18), Theme.Primitives.Sage200, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        };

        void AddNav(string key, string label, IconKind icon)
        {
            var btn = new NavTileButton(label, icon)
            {
                Width = 224,
                Height = 40,
                Margin = new Padding(0, 0, 0, 4)
            };
            btn.Click += (_, _) => NavigateTo(key);
            _navButtons[key] = btn;
            _navStack.Controls.Add(btn);
        }

        AddNav("overview", "Tổng quan studio", IconKind.Dashboard);
        AddNav("packages", "Gói chụp & Concept", IconKind.Sparkles);
        AddNav("spaces", "Phòng chụp & Tài nguyên", IconKind.Home);
        AddNav("wizard", "Tạo yêu cầu đặt lịch", IconKind.Plus);
        AddNav("requests", "Yêu cầu của tôi", IconKind.History);
        AddNav("bookings", "Lịch chụp của tôi", IconKind.Calendar);
        AddNav("payments", "VietQR & Biên nhận", IconKind.Wallet);
        AddNav("albums", "Album ảnh của tôi", IconKind.Layers);
        AddNav("profile", "Hồ sơ & Liên kết", IconKind.RoleCustomer);

        // Bottom User Card
        var foot = new Panel { Dock = DockStyle.Bottom, Height = 128, BackColor = Theme.SidebarCard, Padding = new Padding(12, 10, 12, 10) };
        var roleHost = new Panel { Dock = DockStyle.Top, Height = 30 };
        _rolePill.SetRole(RoleVisualResolver.ForCustomer(), _toolTip);
        roleHost.Controls.Add(_rolePill);

        var logoutBtn = Theme.OutlineButton("Đăng xuất / Đổi cổng", Theme.BorderStrong, IconKind.LogOut);
        logoutBtn.Dock = DockStyle.Bottom;
        logoutBtn.Height = 34;
        logoutBtn.Click += (_, _) =>
        {
            DesktopDemoSessionStore.SignOutCustomer();
            DialogResult = DialogResult.OK;
            Close();
        };

        foot.Controls.Add(_userSummaryLabel);
        foot.Controls.Add(logoutBtn);
        foot.Controls.Add(roleHost);

        rail.Controls.Add(_navStack);
        rail.Controls.Add(brand);
        rail.Controls.Add(foot);
        rail.Controls.Add(sep);
        return rail;
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = Padding.Empty, Padding = new Padding(22, 8, 22, 6) };
        var line = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.BorderStrong };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 356));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var leftIdentity = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, 0, 12, 0)
        };
        leftIdentity.Controls.Add(_topTitleLabel);
        leftIdentity.Controls.Add(_topBreadcrumbLabel);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 0, 0),
            Margin = Padding.Empty
        };

        var demoPillCard = new CardPanel
        {
            Size = new Size(176, 36),
            CornerRadius = 7,
            BackColor = Theme.WarningLight,
            BorderColor = Theme.WarningBorder,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(10, 0, 0, 0)
        };
        demoPillCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "CHẾ ĐỘ DEMO CỤC BỘ",
            UseMnemonic = false,
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.WarningHover,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        });

        var quickReq = Theme.Button("Đặt lịch ngay", Theme.Primitives.Sage600, IconKind.Calendar);
        quickReq.Width = 152;
        quickReq.Height = 36;
        quickReq.Margin = Padding.Empty;
        quickReq.Click += (_, _) => NavigateTo("wizard");

        right.Controls.Add(demoPillCard);
        right.Controls.Add(quickReq);

        grid.Controls.Add(leftIdentity, 0, 0);
        grid.Controls.Add(right, 1, 0);

        bar.Controls.Add(grid);
        bar.Controls.Add(line);
        return bar;
    }

    public void RefreshBadgeCounts()
    {
        var p = _session.Profile;
        _userSummaryLabel.Text = $"{p.FullName}\r\n{p.LinkStatusHeadline}";
        Text = $"Atelier Lumière — Cổng Khách hàng (Demo) • {p.FullName}";
    }

    public void NavigateTo(string sectionKey, object? payload = null)
    {
        _activeKey = sectionKey;
        foreach (var (k, btn) in _navButtons)
            btn.IsSelected = string.Equals(k, sectionKey, StringComparison.OrdinalIgnoreCase);

        _contentHost.SuspendLayout();
        while (_contentHost.Controls.Count > 0)
        {
            var old = _contentHost.Controls[0];
            _contentHost.Controls.RemoveAt(0);
            old.Dispose();
        }

        Control nextPage = sectionKey.ToLowerInvariant() switch
        {
            "overview" => CreateNamedPage("ATELIER LUMIÈRE  //  CỔNG KHÁCH HÀNG", "Tổng quan studio", () => new CustomerDashboardPage(_session, this)),
            "packages" => CreateNamedPage("ATELIER LUMIÈRE  //  DANH MỤC DỊCH VỤ", "Gói chụp & Concept", () => new CustomerPackagesPage(_app, this, payload as string)),
            "spaces" => CreateNamedPage("ATELIER LUMIÈRE  //  KHÔNG GIAN & ĐẠO CỤ", "Phòng chụp & Tài nguyên", () => new CustomerSpacesResourcesPage(_app, this)),
            "wizard" => CreateNamedPage("ATELIER LUMIÈRE  //  ĐẶT LỊCH TRỰC TUYẾN", "Tạo yêu cầu đặt lịch (5 bước)", () => new CustomerBookingWizardPage(_session, this, payload as string, openHistory: false)),
            "requests" => CreateNamedPage("ATELIER LUMIÈRE  //  THEO DÕI YÊU CẦU", "Yêu cầu đặt lịch của tôi", () => new CustomerBookingWizardPage(_session, this, null, openHistory: true)),
            "bookings" => CreateNamedPage("ATELIER LUMIÈRE  //  LỊCH CHỤP CHÍNH THỨC", "Lịch chụp của tôi", () => new CustomerBookingsPage(_session, this)),
            "payments" => CreateNamedPage("ATELIER LUMIÈRE  //  TÀI CHÍNH & CHỨNG TỪ", "Thanh toán VietQR & Biên nhận", () => new CustomerPaymentsPage(_session, this, payload as string)),
            "albums" => CreateNamedPage("ATELIER LUMIÈRE  //  BÀN GIAO HÌNH ẢNH", "Album ảnh của tôi", () => new CustomerAlbumsPage(_session)),
            "profile" => CreateNamedPage("ATELIER LUMIÈRE  //  BẢO MẬT TÀI KHOẢN", "Hồ sơ & Liên kết", () => new CustomerProfilePage(_session, this)),
            _ => CreateNamedPage("ATELIER LUMIÈRE  //  CỔNG KHÁCH HÀNG", "Tổng quan studio", () => new CustomerDashboardPage(_session, this))
        };

        nextPage.Dock = DockStyle.Fill;
        _contentHost.Controls.Add(nextPage);
        _contentHost.ResumeLayout();
    }

    private Control CreateNamedPage(string breadcrumb, string sectionTitle, Func<Control> factory)
    {
        _topBreadcrumbLabel.Text = breadcrumb;
        _topTitleLabel.Text = sectionTitle;
        return factory();
    }

    private sealed class NavTileButton : Panel
    {
        private readonly string _text;
        private readonly IconKind _icon;
        private bool _selected;
        private bool _hover;

        public bool IsSelected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public NavTileButton(string text, IconKind icon)
        {
            _text = text;
            _icon = icon;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.Sidebar);

            var rect = new RectangleF(1, 1, Math.Max(2, Width - 2), Math.Max(2, Height - 2));
            using var path = Theme.RoundedRectangle(rect, 8f);
            if (_selected)
            {
                using var fill = new SolidBrush(Color.FromArgb(46, 72, 58));
                g.FillPath(fill, path);
                using var stripe = new SolidBrush(Theme.Primitives.Sage200);
                g.FillRectangle(stripe, 2, 8, 3, Height - 16);
            }
            else if (_hover)
            {
                using var fill = new SolidBrush(Theme.SidebarHover);
                g.FillPath(fill, path);
            }

            var iconCol = _selected ? Theme.Primitives.Sage200 : Theme.SidebarMuted;
            var txtCol = _selected ? Color.White : Theme.SidebarText;
            VectorIcons.Draw(g, _icon, new RectangleF(14, (Height - 16) / 2f, 16, 16), iconCol, 1.75f);
            using var font = Theme.Font(9.2f, _selected ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(g, _text, font, new Rectangle(38, 0, Math.Max(40, Width - 44), Height), txtCol,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }
}
