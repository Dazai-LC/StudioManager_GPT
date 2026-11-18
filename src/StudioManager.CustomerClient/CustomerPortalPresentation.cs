using System.Drawing.Drawing2D;
using StudioManager.CustomerDesktop;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;

namespace StudioManager.CustomerClient;

internal sealed partial class IdentityForm
{
    private readonly Panel _sidebar = new() { Dock = DockStyle.Fill, BackColor = Theme.Sidebar };
    private readonly FlowLayoutPanel _navigation = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
        WrapContents = false, AutoScroll = true, Padding = new Padding(10, 8, 10, 8), BackColor = Theme.Sidebar };
    private readonly Label _accountSummary = new() { Dock = DockStyle.Bottom, AutoSize = true, MinimumSize = new Size(0, 64), Padding = new Padding(16, 8, 12, 8),
        Font = Theme.Font(8.5f), ForeColor = Theme.SidebarText, Text = "Không gian khách hàng", UseMnemonic = false };
    private readonly PageHeader _header = new("Chào mừng đến Atelier Lumière", "Cổng khách hàng · StudioManager");
    private readonly Dictionary<string, AtelierNavButton> _navButtons = new();
    private readonly Panel _signInPane = new() { Dock = DockStyle.Fill }, _registrationPane = new() { Dock = DockStyle.Fill };
    private readonly Label _authPurpose = AtelierUi.Copy("Đăng nhập bằng tài khoản khách hàng của bạn.");
    private readonly ToolTip _statusTip = new();
    private ModernButton? _logout;
    private ModernButton? _registrationSwitch;
    private Panel? _identifierField;

    private void BuildShell()
    {
        AtelierUi.HideTabHeaders(_tabs);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        var brand = new Panel { Dock = DockStyle.Top, Height = 116 };
        brand.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            VectorIcons.Draw(e.Graphics, IconKind.Aperture, new RectangleF(18, 23, 28, 28), Theme.Primitives.Sage200, 1.9f);
            using var name = Theme.Font(11, FontStyle.Bold); using var detail = Theme.MonoFont(7.5f, FontStyle.Bold);
            using var foreground = new SolidBrush(Theme.SidebarText);
            e.Graphics.DrawString("ATELIER LUMIÈRE", name, foreground, 53, 25);
            using var muted = new SolidBrush(Theme.SidebarMuted);
            e.Graphics.DrawString("STUDIOMANAGER · KHÁCH HÀNG", detail, muted, 18, 68);
        };
        _sidebar.Controls.Add(_navigation); _sidebar.Controls.Add(_accountSummary); _sidebar.Controls.Add(brand);
        root.Controls.Add(_sidebar, 0, 0); root.SetRowSpan(_sidebar, 3);
        _header.Padding = new Padding(20, 10, 16, 8); _header.Dock = DockStyle.Fill;
        root.Controls.Add(_header, 1, 0); root.Controls.Add(_tabs, 1, 1);
        _status.Dock = DockStyle.Fill; _status.Padding = new Padding(20, 10, 20, 8); _status.Font = Theme.Font(9.2f);
        _status.BackColor = Theme.SurfaceWarm; _status.ForeColor = Theme.TextSecondary;
        _status.Text = "Sẵn sàng. Đăng nhập hoặc tạo tài khoản để sử dụng dịch vụ studio.";
        _status.TextChanged += (_, _) => _statusTip.SetToolTip(_status, _status.Text);
        root.Controls.Add(_status, 1, 2); Controls.Add(root);
        _sidebar.SizeChanged += (_, _) => _accountSummary.MaximumSize = new Size(Math.Max(140, _sidebar.ClientSize.Width), 0);
        // Preserve PageHeader rendering while allowing the customer shell's title/subtitle to grow with fonts.
        var title = _header.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<Label>()).Single(label => label.Dock == DockStyle.Top);
        var subtitle = _header.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<Label>()).Single(label => label.Dock == DockStyle.Fill);
        var sizing = false;
        void FitHeader()
        {
            if (sizing || _closed) return;
            sizing = true;
            try
            {
                var width = Math.Max(200, title.Parent!.ClientSize.Width);
                var titleHeight = Math.Max(30, TextRenderer.MeasureText(title.Text, title.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height + 4);
                var subtitleHeight = TextRenderer.MeasureText(subtitle.Text, subtitle.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height + 4;
                title.AutoEllipsis = subtitle.AutoEllipsis = false; title.Height = titleHeight;
                root.RowStyles[0].Height = Math.Max(82, _header.Padding.Vertical + titleHeight + subtitleHeight + 8);
            }
            finally { sizing = false; }
        }
        title.FontChanged += (_, _) => FitHeader(); subtitle.FontChanged += (_, _) => FitHeader();
        title.TextChanged += (_, _) => FitHeader(); subtitle.TextChanged += (_, _) => FitHeader();
        _header.SizeChanged += (_, _) => FitHeader();
        AtelierUi.StyleGrid(_grid, false);
    }

    private void BuildLogin()
    {
        _login.Controls.Add(_registrationPane); _login.Controls.Add(_signInPane);
        var login = Stack(_signInPane);
        login.Controls.Add(AtelierUi.Copy("Chào mừng bạn trở lại", true)); login.Controls.Add(_authPurpose);
        _identifier.MaxLength = 254; _identifier.PlaceholderText = "Email đăng nhập"; _password.PlaceholderText = "Mật khẩu";
        _identifierField = Field(login, "Email đăng nhập", _identifier); Field(login, "Mật khẩu", _password);
        Button(login, "Đăng nhập", LoginAsync, true);
        _registrationSwitch = Button(login, "Tạo tài khoản khách hàng", () => { ShowAuth(true); return Task.CompletedTask; });
        login.Controls.Add(AtelierUi.Copy("Khôi phục truy cập: liên hệ studio để được hỗ trợ. Phiên bản này chưa có xác minh email hoặc OTP."));

        var register = Stack(_registrationPane);
        register.Controls.Add(AtelierUi.Copy("Tài khoản của riêng bạn", true));
        register.Controls.Add(AtelierUi.Copy("Đăng ký để quản lý thông tin liên hệ, gửi yêu cầu đặt lịch và xem biên nhận thuộc quyền của bạn."));
        Field(register, "Họ tên", _regName); Field(register, "Số điện thoại", _regPhone);
        Field(register, "Email đăng nhập", _regEmail); Field(register, "Địa chỉ liên hệ", _regAddress);
        Field(register, "Mật khẩu mới", _regPassword); Field(register, "Xác nhận mật khẩu", _regConfirm);
        register.Controls.Add(AtelierUi.Copy("Mật khẩu từ 8 ký tự, có chữ hoa, chữ thường và chữ số. Email không tự liên kết với hồ sơ lịch sử."));
        Button(register, "Đăng ký", RegisterAsync, true);
        Button(register, "Đã có tài khoản · Đăng nhập", () => { ShowAuth(false); return Task.CompletedTask; });
        _password.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await Execute(LoginAsync); } };
    }

    private void ShowAuth(bool registration, bool staff = false)
    {
        _staff.Checked = staff; _signInPane.Visible = !registration; _registrationPane.Visible = registration;
        if (_registrationSwitch is not null) _registrationSwitch.Visible = !staff;
        if (_identifierField is not null) _identifierField.Controls.OfType<Label>().Single().Text = staff ? "Tên đăng nhập nhân sự" : "Email đăng nhập";
        (registration ? _registrationPane : _signInPane).BringToFront(); _tabs.SelectedTab = _login;
        _header.HeaderTitle = staff ? "Nhân sự · Duyệt liên kết hồ sơ" : registration ? "Đăng ký khách hàng" : "Chào mừng đến Atelier Lumière";
        _header.HeaderSubtitle = staff ? "Chỉ đối chiếu liên kết · Duyệt đặt lịch tại ứng dụng nhân sự" : "Không gian khách hàng · Tài khoản thật, dữ liệu của bạn";
        _authPurpose.Text = staff ? "Dùng tài khoản nhân sự để đối chiếu và duyệt liên kết hồ sơ. Các tác vụ đặt lịch, tài chính và quản trị nằm trong ứng dụng nhân sự."
            : "Đăng nhập bằng email và mật khẩu khách hàng để tiếp tục.";
        _identifier.PlaceholderText = staff ? "Tên đăng nhập nội bộ" : "Email đăng nhập";
        SelectNavigation(staff ? "staff-link" : registration ? "register" : "login");
    }

    private void RefreshNavigation(bool? staff)
    {
        foreach (var control in _navigation.Controls.Cast<Control>().ToArray()) control.Dispose();
        _navButtons.Clear();
        void Add(string key, string text, IconKind icon, Action action)
        {
            var button = new AtelierNavButton(text, icon) { Width = 220, Height = 48, Margin = new Padding(0, 3, 0, 3) };
            button.Click += (_, _) => action(); _navigation.Controls.Add(button); _navButtons.Add(key, button);
        }
        if (!staff.HasValue)
        {
            Add("login", "Đăng nhập", IconKind.Key, () => ShowAuth(false));
            Add("register", "Đăng ký khách hàng", IconKind.Plus, () => ShowAuth(true));
            Add("staff-link", "Nhân sự · Liên kết hồ sơ", IconKind.Shield, () => ShowAuth(false, true));
            ShowAuth(false, _staff.Checked);
        }
        else if (staff.Value)
        {
            Add("links", "Duyệt liên kết hồ sơ", IconKind.UserCheck, () => { _tabs.SelectedTab = _reviews; SelectNavigation("links"); });
            _header.HeaderTitle = "Duyệt liên kết hồ sơ"; _header.HeaderSubtitle = "Nhân sự · Đối chiếu trực tiếp, không tự ghép email/số điện thoại";
            SelectNavigation("links");
        }
        else
        {
            Add("profile", "Hồ sơ của tôi", IconKind.RoleCustomer, () =>
            {
                _tabs.SelectedTab = _profile; SelectNavigation("profile");
                _header.HeaderTitle = "Hồ sơ của tôi"; _header.HeaderSubtitle = "Thông tin liên hệ, bảo mật và liên kết hồ sơ studio";
            });
            Add("Catalog", "Gói chụp & dịch vụ", IconKind.Camera, () => _ = Execute(() => OpenBookingSection(CustomerWorkspaceSection.Catalog)));
            Add("Compose", "Đặt lịch & yêu cầu", IconKind.Calendar, () => _ = Execute(() => OpenBookingSection(CustomerWorkspaceSection.Compose)));
            Add("Requests", "Yêu cầu của tôi", IconKind.History, () => _ = Execute(() => OpenBookingSection(CustomerWorkspaceSection.Requests)));
            Add("Payments", "Số dư & biên nhận", IconKind.Wallet, () => _ = Execute(() => OpenBookingSection(CustomerWorkspaceSection.Payments)));
            _header.HeaderTitle = "Hồ sơ của tôi"; _header.HeaderSubtitle = "Thông tin liên hệ, bảo mật và liên kết hồ sơ studio";
            SelectNavigation("profile");
        }
        if (_logout is not null) _logout.Visible = staff.HasValue;
    }

    private async Task OpenBookingSection(CustomerWorkspaceSection section)
    {
        _tabs.SelectedTab = _booking;
        var refresh = _customerBookingLoaded;
        if (!_customerBookingLoaded) { _customerBookingLoaded = true; await _customerBooking.InitializeAsync(); }
        await _customerBooking.NavigateCustomerAsync(section, refresh);
        MarkBookingSection(section);
    }
    private void MarkBookingSection(CustomerWorkspaceSection section)
    {
        SelectNavigation(section.ToString());
        _header.HeaderTitle = section switch { CustomerWorkspaceSection.Catalog => "Gói chụp & dịch vụ", CustomerWorkspaceSection.Compose => "Đặt lịch & yêu cầu",
            CustomerWorkspaceSection.Payments => "Số dư, thanh toán & biên nhận", _ => "Yêu cầu của tôi" };
        _header.HeaderSubtitle = section == CustomerWorkspaceSection.Payments
            ? "Chọn lịch đã duyệt → Số dư / thanh toán / biên nhận. Chỉ hiển thị khoản thu đã lưu."
            : "Yêu cầu chờ duyệt chưa phải lịch chính thức · Nhân viên studio sẽ xác nhận";
    }
    private void SelectNavigation(string key)
    {
        foreach (var (name, button) in _navButtons) button.IsSelected = name == key;
    }
}
