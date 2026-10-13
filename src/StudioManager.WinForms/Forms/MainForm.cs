using StudioManager.Application.Services;
using StudioManager.Application;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Pages;

namespace StudioManager.WinForms.Forms;

public sealed class MainForm : Form
{
    private const int ExpandedWidth = 282; private const int CollapsedWidth = 76;
    private readonly AppFacade _app;
    private DashboardData? _initialDashboardData;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(28, 18, 28, 24) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = ExpandedWidth, BackColor = Theme.Sidebar, Padding = new Padding(12, 16, 12, 16) };
    private readonly FlowLayoutPanel _menu = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 8, 0, 0), BackColor = Theme.Sidebar };
    private readonly Label _logo = new() { Text = "  ▣  StudioManager\n       STUDIO OPERATIONS", Dock = DockStyle.Top, Height = 84, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft };
    // A muted slate tone distinguishes this session action from the sidebar
    // without presenting it as a destructive red operation.
    private readonly ModernButton _logout = Theme.Button("⇥  Đăng xuất", Color.FromArgb(51, 65, 85));
    private readonly Label _pageTitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, Font = Theme.Font(16, FontStyle.Bold), ForeColor = Theme.Text, Padding = new Padding(0, 0, 0, 8) };
    private readonly Label _breadcrumb = new() { Dock = DockStyle.Top, Height = 24, Text = "StudioManager", Font = Theme.Font(8.8f), ForeColor = Theme.Muted, TextAlign = ContentAlignment.BottomLeft };
    private readonly ToolTip _toolTip = new(); private readonly List<Label> _sectionLabels = []; private Button? _activeButton; private bool _collapsed;

    public MainForm(AppFacade app, DashboardData? initialDashboardData = null)
    {
        _app = app; _initialDashboardData = initialDashboardData; Text = "Studio Manager"; WindowState = FormWindowState.Maximized; MinimumSize = new(1180, 720); AutoScaleMode = AutoScaleMode.Dpi; BackColor = Theme.Background; Font = Theme.Font();
        BuildSidebar(); var top = BuildTopbar(); Controls.Add(_content); Controls.Add(top); Controls.Add(_sidebar); Shown += (_, _) => FitSidebarMenu(); ShowPage("Tổng quan", CreateDashboard(), _menu.Controls.OfType<Button>().FirstOrDefault());
    }

    private void BuildSidebar()
    {
        _logo.Font = Theme.Font(12, FontStyle.Bold);
        AddSection("Khám phá");
        AddMenu("⌂", "Tổng quan", CreateDashboard);
        AddSection("Vận hành");
        AddMenu("◷", "Lịch chụp", () => new BookingsPage(_app));
        AddMenu("♙", "Khách hàng", () => new CrudPage(_app, "Khách hàng", "KhachHang"));
        if (_app.Session!.VaiTro == VaiTro.QuanTriVien)
        {
            AddSection("Quản lý");
            AddMenu("♟", "Nhân viên", () => new CrudPage(_app, "Nhân viên", "NhanVien"));
            AddMenu("▦", "Gói chụp", () => new CrudPage(_app, "Gói chụp", "GoiChup"));
            AddMenu("◇", "Dịch vụ", () => new CrudPage(_app, "Dịch vụ bổ sung", "DichVu"));
            AddMenu("▣", "Phòng chụp", () => new CrudPage(_app, "Phòng chụp", "PhongChup"));
            AddMenu("▤", "Tài nguyên", () => new CrudPage(_app, "Tài nguyên", "TaiNguyen"));
            AddSection("Phân tích");
            AddMenu("◫", "Báo cáo", () => new ReportsPage(_app));
            AddSection("Hệ thống");
            AddMenu("◎", "Tài khoản", () => new AccountsPage(_app));
            AddMenu("≡", "Nhật ký", () => new CrudPage(_app, "Nhật ký hệ thống", "NhatKy", true));
            AddMenu("⚙", "Sao lưu", () => new BackupPage(_app));
        }
        _menu.SizeChanged += (_, _) => FitSidebarMenu();
        _sidebar.Controls.Add(_menu);
        _sidebar.Controls.Add(BuildSidebarFooter());
        _sidebar.Controls.Add(_logo);
    }

    private Control BuildSidebarFooter()
    {
        // The signed-in user is intentionally shown once in the top bar.  Keeping the
        // sidebar footer to its single logout action prevents duplicate account chrome.
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(0, 8, 0, 0), BackColor = Theme.Sidebar };
        _logout.Dock = DockStyle.Bottom; _logout.Height = 40; _logout.Width = 228; _logout.Tag = "⇥|Đăng xuất"; _logout.Click += (_, _) => Close();
        footer.Controls.Add(_logout);
        return footer;
    }

    private Control BuildTopbar()
    {
        var top = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Theme.Surface, Padding = new Padding(14, 12, 28, 10) };
        var toggle = new Button { Text = "☰", Dock = DockStyle.Left, Width = 54, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Font(15), Cursor = Cursors.Hand }; toggle.FlatAppearance.BorderSize = 0; toggle.Click += (_, _) => ToggleSidebar();
        var userPanel = new Panel { Dock = DockStyle.Right, Width = 330, Padding = new Padding(8, 8, 0, 7) };
        var avatar = new Label { Text = Initials(_app.Session!.HoTen), Dock = DockStyle.Right, Width = 46, BackColor = Theme.PrimaryLight, ForeColor = Theme.Primary, Font = Theme.Font(10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }; avatar.Resize += (_, _) => Theme.Round(avatar, 22);
        var user = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = Theme.Font(9.5f), ForeColor = Theme.Muted, Text = $"{_app.Session.HoTen}\n{(_app.Session.VaiTro == VaiTro.QuanTriVien ? "Quản trị viên" : "Nhân viên")}" };
        var pageStack = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0) };
        pageStack.Controls.Add(_pageTitle); pageStack.Controls.Add(_breadcrumb);
        userPanel.Controls.Add(user); userPanel.Controls.Add(avatar); top.Controls.Add(pageStack); top.Controls.Add(userPanel); top.Controls.Add(toggle); return top;
    }

    private void AddMenu(string icon, string title, Func<Control> page)
    {
        var b = new Button { Text = $"{icon}   {title}", Tag = $"{icon}|{title}", Width = 228, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = Theme.Sidebar, ForeColor = Color.FromArgb(203, 213, 225), Font = Theme.Font(9.7f), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0), Cursor = Cursors.Hand, Margin = new Padding(0, 1, 0, 1), UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover; b.Click += (_, _) => ShowPage(title, page(), b); _toolTip.SetToolTip(b, title); _menu.Controls.Add(b);
    }

    private void AddSection(string title)
    {
        var section = Ui.SectionLabel(title);
        _sectionLabels.Add(section);
        _menu.Controls.Add(section);
    }

    private void ToggleSidebar()
    {
        _collapsed = !_collapsed; _sidebar.SuspendLayout(); _sidebar.Width = _collapsed ? CollapsedWidth : ExpandedWidth; _logo.Text = _collapsed ? "▣" : "  ▣  StudioManager\n       STUDIO OPERATIONS"; _logo.TextAlign = _collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
        foreach (var section in _sectionLabels) section.Visible = !_collapsed;
        foreach (var b in _menu.Controls.OfType<Button>()) { var parts = (b.Tag?.ToString() ?? "|").Split('|'); b.Text = _collapsed ? parts[0] : $"{parts[0]}   {parts.ElementAtOrDefault(1)}"; b.Width = _collapsed ? 52 : 228; b.Padding = _collapsed ? Padding.Empty : new Padding(14, 0, 0, 0); b.TextAlign = _collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft; }
        _logout.Text = _collapsed ? "⇥" : "⇥  Đăng xuất"; _logout.Width = _collapsed ? 52 : 228; _sidebar.ResumeLayout(true); FitSidebarMenu();
    }

    private void FitSidebarMenu()
    {
        // A vertical scrollbar reduces the viewport width. Fixed 228px menu buttons
        // then caused an unnecessary horizontal scrollbar at compact window heights.
        // Always reserve the potential vertical-scrollbar gutter.  WinForms reports
        // VerticalScroll.Visible too late during layout, which previously let menu
        // children become one gutter wider and produced a horizontal scrollbar.
        var width = Math.Max(48, _menu.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
        foreach (var button in _menu.Controls.OfType<Button>()) button.Width = width;
        foreach (var section in _sectionLabels) section.Width = width;
        _menu.AutoScrollMinSize = Size.Empty;
        _menu.HorizontalScroll.Enabled = false;
        _menu.HorizontalScroll.Visible = false;
        if (_menu.HorizontalScroll.Value != 0) _menu.HorizontalScroll.Value = 0;
    }

    private void ShowPage(string title, Control page, Button? sender)
    {
        if (_activeButton is not null) { _activeButton.BackColor = Theme.Sidebar; _activeButton.ForeColor = Color.FromArgb(203, 213, 225); }
        _activeButton = sender; if (_activeButton is not null) { _activeButton.BackColor = Theme.Primary; _activeButton.ForeColor = Color.White; }
        _pageTitle.Text = title; _breadcrumb.Text = "StudioManager   /   " + title; _content.Controls.Clear(); page.Dock = DockStyle.Fill; _content.Controls.Add(page);
    }
    private DashboardPage CreateDashboard() { var page = new DashboardPage(_app, _initialDashboardData); _initialDashboardData = null; page.ViewBookingsRequested += (_, _) => OpenMenu("Lịch chụp"); return page; }
    private void OpenMenu(string title) { var button = _menu.Controls.OfType<Button>().FirstOrDefault(x => (x.Tag?.ToString() ?? "").EndsWith("|" + title, StringComparison.Ordinal)); button?.PerformClick(); }
    private static string Initials(string name) { var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries); return parts.Length == 0 ? "U" : string.Concat(parts.TakeLast(Math.Min(2, parts.Length)).Select(x => char.ToUpperInvariant(x[0]))); }
}
