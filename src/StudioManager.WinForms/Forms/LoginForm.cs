using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

public sealed class LoginForm : Form
{
    private readonly AppFacade _app;
    private bool _isAuthenticating;

    private readonly TextBox _username = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.2f),
        PlaceholderText = "Nhập tên tài khoản (vd: admin)"
    };

    private readonly TextBox _password = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.2f),
        PlaceholderText = "Nhập mật khẩu bảo mật",
        UseSystemPasswordChar = true
    };

    private readonly ModernButton _login = Theme.Button("Mở không gian làm việc", Theme.Primary, IconKind.ArrowRight);

    private readonly CardPanel _errorPanel = new()
    {
        Dock = DockStyle.Fill,
        Height = 46,
        Margin = new Padding(0, 4, 0, 2),
        Visible = false,
        BackColor = Theme.DangerLight,
        BorderColor = Theme.DangerBorder,
        AccentStripeColor = Theme.Danger,
        AccentStripeLeft = true,
        AccentStripeThickness = 4,
        CornerRadius = 8,
        Padding = new Padding(14, 4, 10, 4)
    };

    private readonly Label _errorLabel = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.Danger,
        BackColor = Color.Transparent,
        Font = Theme.Font(8.8f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Visible = false
    };

    public LoginForm(AppFacade app)
    {
        _app = app;
        Text = "StudioManager — Atelier Lumière Edition";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1220, 740);
        MinimumSize = new Size(1040, 660);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildTopArchitecturalRibbon(), 0, 0);
        root.Controls.Add(BuildMainSplitCanvas(), 0, 1);

        Controls.Add(root);
        AcceptButton = _login;

        _username.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoginAsync();
            }
        };

        _password.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoginAsync();
            }
        };

        _username.TextChanged += (_, _) => { if (_errorPanel.Visible) ClearError(); };
        _password.TextChanged += (_, _) => { if (_errorPanel.Visible) ClearError(); };
    }

    private static Control BuildTopArchitecturalRibbon() => AuthChromeFactory.BuildTopRibbon(
        "ATELIER LUMIÈRE  //  UNIFIED STUDIO AUTHENTICATION",
        "ĐĂNG NHẬP HỢP NHẤT  •  TỰ ĐỘNG ĐIỀU HƯỚNG THEO VAI TRÒ",
        Theme.Primitives.Sage600);

    private Control BuildMainSplitCanvas()
    {
        var canvas = new FullCanvasAuthSurface(AuthVisualAssets.LoginBackgroundAssetId, StudioBackdropMood.WorkstationOptics)
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 14, 24, 14)
        };

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        split.Controls.Add(BuildOpticalShowcasePanel(), 0, 0);
        split.Controls.Add(BuildCredentialVaultPanel(), 1, 0);
        canvas.Controls.Add(split);
        return canvas;
    }

    private static Control BuildOpticalShowcasePanel()
    {
        var hero = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(12, 16, 18, 14),
            BackColor = Color.Transparent
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // Tagline pill
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112)); // Editorial Headline
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));  // Editorial Lead
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 3 Architectural Pillar Cards
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Footer signature

        var overline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "PRO PHOTOGRAPHY OPERATIONS SYSTEM",
            ForeColor = Theme.Primitives.Terracotta200,
            Font = Theme.MonoFont(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var headline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Nghệ thuật điều phối\nkhông gian nhiếp ảnh.",
            ForeColor = Theme.Primitives.Alabaster50,
            Font = Theme.Font(19.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lead = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Kiểm soát trọn vẹn quy trình từ đặt lịch phòng máy, điều phối nhiếp ảnh gia, hậu kỳ hình ảnh đến quyết toán tài chính.",
            ForeColor = Theme.Primitives.Stone300,
            Font = Theme.Font(9.2f),
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 4, 0, 0)
        };

        layout.Resize += (_, _) =>
        {
            var w = Math.Max(200, layout.ClientSize.Width - 8);
            var hSize = TextRenderer.MeasureText(headline.Text, headline.Font, new Size(w, 240), TextFormatFlags.WordBreak);
            var lSize = TextRenderer.MeasureText(lead.Text, lead.Font, new Size(w, 200), TextFormatFlags.WordBreak);
            layout.RowStyles[1].Height = Math.Max(88, hSize.Height + 16);
            layout.RowStyles[2].Height = Math.Max(54, lSize.Height + 14);
        };

        var pillars = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 6)
        };
        pillars.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pillars.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        pillars.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        pillars.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));

        pillars.Controls.Add(CreatePillarTile(IconKind.Calendar, "Điều phối Lịch chụp & Phòng máy", "Kiểm tra xung đột khung giờ, phân bổ ekip và thiết bị theo thời gian thực."), 0, 0);
        pillars.Controls.Add(CreatePillarTile(IconKind.Layers, "Đường ống Hậu kỳ 7 Giai đoạn", "Theo dõi tiến độ từ Đã đặt lịch → Đã chụp → Chỉnh sửa → Bàn giao ảnh."), 0, 1);
        pillars.Controls.Add(CreatePillarTile(IconKind.Wallet, "Sổ cái Tài chính & Biên lai", "Quản lý tiền cọc, thu bổ sung, hoàn tiền, chiết khấu và công nợ minh bạch."), 0, 2);

        var signature = new Label
        {
            Dock = DockStyle.Fill,
            Text = "ATELIER LUMIÈRE DESIGN SYSTEM  •  NATIVE WINFORMS .NET 8",
            ForeColor = Theme.Primitives.Stone400,
            Font = Theme.MonoFont(7.8f),
            TextAlign = ContentAlignment.BottomLeft,
            AutoEllipsis = true
        };

        layout.Controls.Add(overline, 0, 0);
        layout.Controls.Add(headline, 0, 1);
        layout.Controls.Add(lead, 0, 2);
        layout.Controls.Add(pillars, 0, 3);
        layout.Controls.Add(signature, 0, 4);

        hero.Controls.Add(layout);
        return hero;
    }

    private static Control CreatePillarTile(IconKind icon, string title, string subtitle) =>
        AuthChromeFactory.CreateTranslucentPillarTile(
            icon,
            title,
            subtitle,
            Theme.Primitives.Terracotta200,
            Color.FromArgb(62, Theme.Primary));

    private Control BuildCredentialVaultPanel()
    {
        var vault = new ElevatedFrostedAuthCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 0, 0, 0),
            Padding = new Padding(34, 14, 34, 12),
            AccentStripeColor = Theme.Primary
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 0: Header badge
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 1: Title
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // 2: Subtitle
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 66)); // 3: Identifier
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 66)); // 4: Password
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // 5: Options row
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // 6: Primary CTA Button
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // 7: Inline Error Banner
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // 8: Frontend Demo / QA Customer Launcher
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 9: Footer

        _username.PlaceholderText = "Tên đăng nhập, Email hoặc SĐT (vd: admin)";

        var emblemCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        emblemCanvas.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new RectangleF(0, (emblemCanvas.Height - 40) / 2f, 40, 40);
            using var path = Theme.RoundedRectangle(box, 10f);
            using var fill = new SolidBrush(Theme.PrimaryLight);
            using var pen = new Pen(Theme.PrimaryLightBorder, 1f);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            VectorIcons.Draw(g, IconKind.Shield, new RectangleF(box.Left + 9, box.Top + 9, 22, 22), Theme.Primary, 1.9f);

            using var badgeFont = Theme.MonoFont(8.0f, FontStyle.Bold);
            var badgeRect = new Rectangle(52, 0, Math.Max(100, emblemCanvas.Width - 56), emblemCanvas.Height);
            TextRenderer.DrawText(g, "ĐĂNG NHẬP HỢP NHẤT • TỰ ĐỘNG PHÂN LUỒNG VAI TRÒ", badgeFont, badgeRect, Theme.Primary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đăng nhập Atelier Lumière",
            Font = Theme.Font(18.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var desc = new Label
        {
            Dock = DockStyle.Fill,
            UseMnemonic = false,
            Text = "Một cửa vào duy nhất: Quản trị / Nhân viên dùng tài khoản SQL nội bộ; tài khoản Khách hàng sẽ tự động mở Cổng Khách hàng khi kết nối Backend.",
            Font = Theme.Font(8.4f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var options = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var remember = new CheckBox
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            Text = "Duy trì trạng thái làm việc trên thiết bị này",
            Checked = true,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.8f),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

        var forgot = new LinkLabel
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Text = "Cần cấp lại mật khẩu?",
            TextAlign = ContentAlignment.MiddleRight,
            LinkColor = Theme.Primary,
            ActiveLinkColor = Theme.PrimaryHover,
            Cursor = Cursors.Hand,
            Font = Theme.Font(8.8f, FontStyle.Bold),
            Margin = new Padding(10, 0, 0, 0)
        };
        forgot.LinkClicked += (_, _) => AtelierNotificationService.ShowInfo(
            this,
            "• Đối với Quản trị viên & Nhân viên: Việc đặt lại mật khẩu do Quản trị viên thực hiện tại Trung tâm Hệ thống → Tài khoản & Phân quyền.\n\n" +
            "• Đối với Khách hàng: Quy trình khôi phục truy cập và đối chiếu hồ sơ cũ đang ở chế độ xem trước Frontend Demo (chưa kết nối SQL/OTP).",
            "Chính sách bảo mật & Khôi phục truy cập");

        options.Controls.Add(remember, 0, 0);
        options.Controls.Add(forgot, 1, 0);

        ConfigureLoginButton();
        _errorPanel.Controls.Add(_errorLabel);

        // Isolated Frontend Demo / QA preview launcher for Customer Desktop UX
        var demoLauncherCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 8,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 0, 2)
        };
        var demoLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "XEM TRƯỚC CỔNG KHÁCH HÀNG (FRONTEND DEMO • CHƯA NỐI SQL AUTH)",
            Font = Theme.MonoFont(7.2f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            UseMnemonic = false,
            AutoEllipsis = true
        };
        var demoBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
        var customerDemoBtn = Theme.OutlineButton("Cổng Khách hàng (Demo)", Theme.Primitives.Sage600, IconKind.RoleCustomer);
        customerDemoBtn.Height = 32;
        customerDemoBtn.Width = 188;
        customerDemoBtn.Margin = new Padding(0, 0, 8, 0);
        customerDemoBtn.Click += (_, _) => OpenCustomerPortalEntry();

        var registerDemoBtn = Theme.OutlineButton("Đăng ký Khách hàng (Demo)", Theme.BorderStrong, IconKind.Plus);
        registerDemoBtn.Height = 32;
        registerDemoBtn.Width = 198;
        registerDemoBtn.Margin = new Padding(0);
        registerDemoBtn.Click += (_, _) => OpenCustomerRegistration();

        demoBtns.Controls.Add(customerDemoBtn);
        demoBtns.Controls.Add(registerDemoBtn);
        demoLauncherCard.Controls.Add(demoBtns);
        demoLauncherCard.Controls.Add(demoLbl);

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Phím tắt: Enter để xác thực nhanh  •  Điều phối tự động Admin / Nhân viên / Nhiếp ảnh gia / Khách hàng",
            TextAlign = ContentAlignment.BottomLeft,
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            Font = Theme.Font(7.9f)
        };

        form.Controls.Add(emblemCanvas, 0, 0);
        form.Controls.Add(title, 0, 1);
        form.Controls.Add(desc, 0, 2);
        form.Controls.Add(AuthChromeFactory.CreateVectorInputField("TÊN ĐĂNG NHẬP HOẶC EMAIL", _username, IconKind.Users), 0, 3);
        form.Controls.Add(AuthChromeFactory.CreateVectorInputField("MẬT KHẨU BẢO MẬT", _password, IconKind.Lock, allowReveal: true), 0, 4);
        form.Controls.Add(options, 0, 5);
        form.Controls.Add(_login, 0, 6);
        form.Controls.Add(_errorPanel, 0, 7);
        form.Controls.Add(demoLauncherCard, 0, 8);
        form.Controls.Add(footer, 0, 9);

        vault.Controls.Add(form);
        return vault;
    }

    private void OpenCustomerPortalEntry()
    {
        ClearError();
        Hide();
        try
        {
            using var customerLogin = new CustomerLoginForm(_app);
            customerLogin.ShowDialog(this);
        }
        finally
        {
            RestoreLoginWindow();
        }
    }

    private void OpenCustomerRegistration()
    {
        ClearError();
        using var reg = new CustomerRegistrationForm(_app);
        if (reg.ShowDialog(this) == DialogResult.OK && reg.CreatedDemoSession is { } session)
        {
            Hide();
            try
            {
                using var portal = new CustomerMainForm(_app, session);
                portal.ShowDialog(this);
            }
            finally
            {
                RestoreLoginWindow();
            }
        }
        // Không tự động nạp định danh khách hàng vào ô đăng nhập Quản trị / Nhân viên.
    }

    private void ConfigureLoginButton()
    {
        _login.Text = "Mở không gian làm việc";
        _login.Icon = IconKind.ArrowRight;
        _login.Dock = DockStyle.Fill;
        _login.Height = 44;
        _login.Font = Theme.Font(10.0f, FontStyle.Bold);
        _login.Cursor = Cursors.Hand;
        _login.Margin = new Padding(0, 2, 0, 2);
        _login.Click += async (_, _) => await LoginAsync();
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
        _errorPanel.Visible = true;
        _errorPanel.BringToFront();
        _errorPanel.Invalidate();
    }

    private void ClearError()
    {
        _errorLabel.Text = "";
        _errorLabel.Visible = false;
        _errorPanel.Visible = false;
    }

    private void RestoreLoginWindow()
    {
        if (IsDisposed) return;
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private async Task LoginAsync()
    {
        if (_isAuthenticating) return;

        var username = _username.Text.Trim();
        var password = _password.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Vui lòng nhập tên đăng nhập.");
            _username.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Vui lòng nhập mật khẩu.");
            _password.Focus();
            return;
        }

        _isAuthenticating = true;
        ClearError();
        _login.Enabled = false;
        _login.Text = "Đang xác thực...";
        _login.Refresh();


        try
        {
            var result = await _app.Auth.LoginAsync(username, password);
            if (!result.Success || result.Data is null)
            {
                ShowError(string.IsNullOrWhiteSpace(result.Message) ? "Thông tin đăng nhập không hợp lệ." : result.Message);
                _password.SelectAll();
                _password.Focus();
                return;
            }


            var secRole = result.Data.VaiTro == VaiTro.QuanTriVien
                ? UserSecurityRole.Admin
                : UserSecurityRole.Staff;
            var route = UnifiedAuthRouter.ResolveRoute(new AuthenticatedPrincipal(
                result.Data.TenDangNhap,
                result.Data.HoTen,
                secRole,
                IsPhotographerPosition: false,
                MustChangePassword: result.Data.PhaiDoiMatKhau,
                IsDemoSession: false));
            if (!route.IsAllowed)
            {
                _app.Auth.SignOut();
                ShowError(route.BlockReason ?? "Tài khoản chưa được phép truy cập.");
                return;
            }

            if (result.Data.PhaiDoiMatKhau)
            {
                using var change = new ChangePasswordForm(_app, result.Data);
                if (change.ShowDialog(this) != DialogResult.OK)
                {
                    _app.Auth.SignOut();
                    _password.Clear();
                    ShowError("Bạn cần hoàn tất đổi mật khẩu bảo mật trước khi truy cập hệ thống.");
                    _password.Focus();
                    return;
                }
                if (_app.Session is null || _app.Session.PhaiDoiMatKhau)
                    throw new UnauthorizedAccessException();
            }

            Hide();

            using var initialization = new ApplicationInitializationForm(_app);
            if (initialization.ShowDialog() != DialogResult.OK || initialization.DashboardData is null)
            {
                _app.Auth.SignOut();
                _password.Clear();
                RestoreLoginWindow();
                ShowError(!string.IsNullOrWhiteSpace(initialization.LastErrorMessage)
                    ? initialization.LastErrorMessage
                    : "Khởi tạo không gian làm việc không thành công hoặc đã bị hủy.");
                _password.Focus();
                return;
            }

            using var main = new MainForm(_app, initialization.DashboardData);

            main.ShowDialog();

            _app.Auth.SignOut();
            _password.Clear();
            ClearError();
            RestoreLoginWindow();
            _username.Focus();
        }
        catch (Exception ex)
        {
            _app.Auth.SignOut();
            RestoreLoginWindow();
            var errorMsg = Ui.ReportError("LoginForm.LoginAsync", ex);

            ShowError(errorMsg);
            _password.Focus();
        }
        finally
        {
            _app.Auth.SignOut();
            _isAuthenticating = false;
            if (!IsDisposed)
            {
                if (!Visible) RestoreLoginWindow();
                _login.Enabled = true;
                _login.Text = "Mở không gian làm việc";
                _login.Refresh();
            }
        }
    }
}
