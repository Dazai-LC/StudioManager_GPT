using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Forms;

/// <summary>
/// Full-canvas photographic Customer Demo authentication entry form.
/// Isolates Customer Demo UX completely from internal Staff/Admin SQL Server authentication.
/// </summary>
public sealed class CustomerLoginForm : Form
{
    private readonly AppFacade _app;

    private readonly TextBox _identifier = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Email hoặc SĐT (vd: khachhang.demo@atelier.local)"
    };

    private readonly TextBox _password = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Nhập mật khẩu (Demo: bất kỳ từ 4 ký tự)",
        UseSystemPasswordChar = true
    };

    private readonly ModernButton _signInBtn = Theme.Button("Vào Không gian Khách hàng (Demo)", Theme.Primitives.Sage600, IconKind.ArrowRight);
    private readonly Label _errorBanner = new()
    {
        Dock = DockStyle.Fill,
        Font = Theme.Font(8.8f, FontStyle.Bold),
        ForeColor = Theme.Danger,
        BackColor = Color.Transparent,
        Visible = false
    };

    public CustomerLoginForm(AppFacade app)
    {
        _app = app;
        DesktopDemoSessionStore.EnsureSeeded();

        Text = "Cổng Khách hàng (Demo) — Atelier Lumière";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1220, 740);
        MinimumSize = new Size(1040, 660);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Primitives.Espresso900;
        Font = Theme.Font();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        root.Controls.Add(AuthChromeFactory.BuildTopRibbon(
            "ATELIER LUMIÈRE  //  CUSTOMER DESKTOP PORTAL (DEMO)",
            "CỔNG KHÁCH HÀNG  •  CHẾ ĐỘ MÔ PHỎNG",
            Theme.Primitives.Sage600), 0, 0);

        var canvas = new FullCanvasAuthSurface(AuthVisualAssets.CustomerRegistrationBackgroundAssetId, StudioBackdropMood.WarmEditorial)
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 22)
        };

        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        columns.Controls.Add(BuildLeftShowcaseCopy(), 0, 0);
        columns.Controls.Add(BuildFrostedCustomerCard(), 1, 0);

        canvas.Controls.Add(columns);
        root.Controls.Add(canvas, 0, 1);
        Controls.Add(root);

        AcceptButton = _signInBtn;
        _identifier.Text = "khachhang.demo@atelier.local";
        _password.Text = "demo1234";
    }

    private static Control BuildLeftShowcaseCopy()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12, 14, 24, 14) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var overline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "ATELIER LUMIÈRE  •  TRẢI NGHIỆM KHÁCH HÀNG",
            Font = Theme.MonoFont(8.1f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Sage200,
            BackColor = Color.Transparent
        };
        var headline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Không gian đặt lịch &\nlưu giữ khoảnh khắc.",
            Font = Theme.Font(19.5f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Alabaster50,
            BackColor = Color.Transparent
        };
        var lead = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Khám phá trực quan 05 gói chụp, không gian phòng ánh sáng, gửi yêu cầu đặt lịch và theo dõi tiến độ bàn giao album trong môi trường Demo cục bộ.",
            Font = Theme.Font(9.1f),
            ForeColor = Theme.Primitives.Alabaster200,
            BackColor = Color.Transparent
        };

        var pillars = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
        for (var i = 0; i < 3; i++) pillars.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        pillars.Controls.Add(AuthChromeFactory.CreateTranslucentPillarTile(
            IconKind.Sparkles, "Gói chụp & Phòng chụp trực quan",
            "Xem hình ảnh studio, bảng giá minh bạch và phóng to chi tiết trang phục, thiết bị.",
            Theme.Primitives.Sage200, Color.FromArgb(65, Theme.Primitives.Sage600)), 0, 0);
        pillars.Controls.Add(AuthChromeFactory.CreateTranslucentPillarTile(
            IconKind.Calendar, "Yêu cầu đặt lịch & VietQR (Demo)",
            "Đề xuất ngày giờ mong muốn và thanh toán cọc VietQR có nhân viên đối soát thủ công.",
            Theme.Primitives.Terracotta200, Color.FromArgb(65, Theme.Primary)), 0, 1);
        pillars.Controls.Add(AuthChromeFactory.CreateTranslucentPillarTile(
            IconKind.Shield, "Tách biệt quyền riêng tư hồ sơ",
            "Không tự động gộp lịch sử cũ qua SĐT khi chưa xác minh quyền sở hữu.",
            Theme.Primitives.Ochre200, Color.FromArgb(65, Theme.Primitives.Ochre600)), 0, 2);

        var sig = new Label
        {
            Dock = DockStyle.Fill,
            Text = "GIAO DIỆN DEMO DÀNH RIÊNG CHO KHÁCH HÀNG  •  KHÔNG TRUY CẬP BÀN QUẢN TRỊ",
            Font = Theme.MonoFont(7.6f),
            ForeColor = Theme.Primitives.Stone300,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft
        };

        layout.Controls.Add(overline, 0, 0);
        layout.Controls.Add(headline, 0, 1);
        layout.Controls.Add(lead, 0, 2);
        layout.Controls.Add(pillars, 0, 3);
        layout.Controls.Add(sig, 0, 4);
        host.Controls.Add(layout);
        return host;
    }

    private Control BuildFrostedCustomerCard()
    {
        var card = new ElevatedFrostedAuthCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 2, 0, 2),
            Padding = new Padding(38, 24, 38, 18),
            AccentStripeColor = Theme.Primitives.Sage600
        };

        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 10, BackColor = Color.Transparent };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // 0: Access Mode Switcher
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // 1: Customer Badge Pill
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // 2: Headline
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // 3: Persona Quick-Fill Strip
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); // 4: Identifier
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); // 5: Password
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 6: Primary CTA
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // 7: Recovery + Register
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 8: Error Banner
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // 9: Footer notice

        var switcher = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var backBtn = Theme.OutlineButton("← Quay lại Đăng nhập hợp nhất", Theme.BorderStrong);
        backBtn.Dock = DockStyle.Left;
        backBtn.Width = 248;
        backBtn.Height = 32;
        backBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        var demoTag = new Label
        {
            Dock = DockStyle.Fill,
            Text = "CHẾ ĐỘ XEM TRƯỚC GIAO DIỆN KHÁCH HÀNG",
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Sage600,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true
        };
        switcher.Controls.Add(demoTag);
        switcher.Controls.Add(backBtn);

        var roleHost = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
        var rolePill = new RoleBadgePill();
        rolePill.SetRole(RoleVisualResolver.ForCustomer("Khách hàng (Demo)"));
        roleHost.Controls.Add(rolePill);
        roleHost.Controls.Add(new Label { AutoSize = true, Text = "Phiên mô phỏng an toàn", Font = Theme.Font(8.5f), ForeColor = Theme.Muted, Padding = new Padding(6, 5, 0, 0) });

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đăng nhập Tài khoản Khách hàng",
            Font = Theme.Font(17.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var personaRow = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, WrapContents = true };
        var verifiedSample = Theme.OutlineButton("Mẫu đã xác minh (Thảo Vy)", Theme.Primitives.Sage600);
        verifiedSample.Height = 30;
        verifiedSample.Width = 192;
        verifiedSample.Margin = new Padding(0, 4, 8, 2);
        verifiedSample.Click += (_, _) => { _identifier.Text = "khachhang.demo@atelier.local"; _password.Text = "demo1234"; };
        var unlinkedSample = Theme.OutlineButton("Mẫu khách mới chưa liên kết", Theme.BorderStrong);
        unlinkedSample.Height = 30;
        unlinkedSample.Width = 192;
        unlinkedSample.Margin = new Padding(0, 4, 0, 2);
        unlinkedSample.Click += (_, _) => { _identifier.Text = "khachmoi.demo@atelier.local"; _password.Text = "demo1234"; };
        personaRow.Controls.Add(verifiedSample);
        personaRow.Controls.Add(unlinkedSample);

        _signInBtn.Dock = DockStyle.Fill;
        _signInBtn.Height = 44;
        _signInBtn.Click += (_, _) => EnterCustomerPortal();

        var links = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0), WrapContents = true };
        var forgotBtn = Theme.OutlineButton("Khôi phục & Liên kết hồ sơ", null, IconKind.Shield);
        forgotBtn.Height = 34;
        forgotBtn.Width = 194;
        forgotBtn.Click += (_, _) => { using var d = new CustomerRecoveryDialog(); d.ShowDialog(this); };

        var regBtn = Theme.OutlineButton("Đăng ký thành viên (Demo)", Theme.Primary, IconKind.RoleCustomer);
        regBtn.Height = 34;
        regBtn.Width = 194;
        regBtn.Margin = new Padding(0, 0, 8, 0);
        regBtn.Click += (_, _) => OpenRegistration();

        links.Controls.Add(regBtn);
        links.Controls.Add(forgotBtn);

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Phiên làm việc này chỉ mô phỏng trải nghiệm khách hàng tại Desktop, tách biệt hoàn toàn với tài khoản Nhân viên / Quản trị SQL.",
            Font = Theme.Font(8.2f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft
        };

        form.Controls.Add(switcher, 0, 0);
        form.Controls.Add(roleHost, 0, 1);
        form.Controls.Add(title, 0, 2);
        form.Controls.Add(personaRow, 0, 3);
        form.Controls.Add(AuthChromeFactory.CreateVectorInputField("EMAIL HOẶC SỐ ĐIỆN THOẠI", _identifier, IconKind.Users), 0, 4);
        form.Controls.Add(AuthChromeFactory.CreateVectorInputField("MẬT KHẨU", _password, IconKind.Lock, allowReveal: true), 0, 5);
        form.Controls.Add(_signInBtn, 0, 6);
        form.Controls.Add(links, 0, 7);
        form.Controls.Add(_errorBanner, 0, 8);
        form.Controls.Add(footer, 0, 9);

        card.Controls.Add(form);
        return card;
    }

    private void EnterCustomerPortal()
    {
        _errorBanner.Visible = false;
        if (string.IsNullOrWhiteSpace(_identifier.Text))
        {
            _errorBanner.Text = "Vui lòng nhập email hoặc số điện thoại (có thể nhấn nút Nạp mẫu bên trên).";
            _errorBanner.Visible = true;
            return;
        }
        if (_password.Text.Length < 4)
        {
            _errorBanner.Text = "Mật khẩu Demo cần tối thiểu 4 ký tự.";
            _errorBanner.Visible = true;
            return;
        }

        var session = DesktopDemoSessionStore.SignInDemoCustomer(_identifier.Text);
        LaunchCustomerMain(session);
    }

    private void OpenRegistration()
    {
        using var reg = new CustomerRegistrationForm(_app);
        if (reg.ShowDialog(this) == DialogResult.OK && reg.CreatedDemoSession is { } session)
            LaunchCustomerMain(session);
    }

    private void LaunchCustomerMain(CustomerSessionContext session)
    {
        Hide();
        try
        {
            using var portal = new CustomerMainForm(_app, session);
            portal.ShowDialog(this);
        }
        finally
        {
            if (!IsDisposed)
            {
                Show();
                Activate();
            }
        }
    }
}
