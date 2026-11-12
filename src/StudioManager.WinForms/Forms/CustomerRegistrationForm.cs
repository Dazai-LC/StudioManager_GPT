using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Forms;

/// <summary>
/// STUDIOMANAGER v2.2 — DESKTOP CUSTOMER REGISTRATION
/// Atelier Lumière Three-Layer Design System.
/// Strict Desktop-only implementation (DEC-01-R2 / Desktop Migration).
/// Safe demonstration mode: validates rules for CUSTOMER role without touching DB.
/// </summary>
public sealed class CustomerRegistrationForm : Form
{
    private readonly AppFacade _app;

    // Form inputs
    private readonly TextBox _fullName = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Nhập họ và tên đầy đủ (vd: Nguyễn Văn An)"
    };

    private readonly TextBox _phone = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Nhập số điện thoại (vd: 0901234567 — 10 chữ số)"
    };

    private readonly TextBox _email = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Nhập địa chỉ email (vd: name@example.com)"
    };

    private readonly TextBox _password = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Tối thiểu 8 ký tự, gồm cả chữ cái và số",
        UseSystemPasswordChar = true
    };

    private readonly TextBox _confirmPassword = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.0f),
        PlaceholderText = "Nhập lại mật khẩu để xác nhận",
        UseSystemPasswordChar = true
    };

    private readonly CheckBox _acceptTerms = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = false,
        AutoEllipsis = true,
        Text = "Tôi đồng ý với Điều khoản Dịch vụ và Quy định Bảo mật của Atelier Lumière",
        Checked = false,
        ForeColor = Theme.TextSecondary,
        Font = Theme.Font(8.8f),
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = Padding.Empty,
        Cursor = Cursors.Hand
    };

    // Password strength visual indicator
    private readonly PasswordStrengthMeterPanel _strengthMeter = new()
    {
        Dock = DockStyle.Fill,
        Height = 36,
        Margin = new Padding(0, 2, 0, 4)
    };

    // Feedback Panels
    private readonly CardPanel _errorPanel = new()
    {
        Dock = DockStyle.Fill,
        Height = 48,
        Margin = new Padding(0, 4, 0, 4),
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

    private readonly CardPanel _successPanel = new()
    {
        Dock = DockStyle.Fill,
        Height = 126,
        Margin = new Padding(0, 6, 0, 6),
        Visible = false,
        BackColor = Theme.SuccessLight,
        BorderColor = Theme.SuccessBorder,
        AccentStripeColor = Theme.Success,
        AccentStripeLeft = true,
        AccentStripeThickness = 4,
        CornerRadius = 8,
        Padding = new Padding(14, 8, 14, 8)
    };

    private readonly Label _successLabel = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.Success,
        BackColor = Color.Transparent,
        Font = Theme.Font(8.5f),
        TextAlign = ContentAlignment.TopLeft,
        Visible = false
    };

    // Action buttons
    private readonly ModernButton _registerBtn = Theme.Button("Kiểm tra & Gửi đăng ký (Demo)", Theme.Primary, IconKind.Check);
    private readonly ModernButton _backBtn = Theme.Button("Quay lại Đăng nhập", Theme.Secondary, IconKind.ArrowRight);

    // Exported registered identity and created Demo session
    public string RegisteredIdentifier { get; private set; } = string.Empty;
    public CustomerSessionContext? CreatedDemoSession { get; private set; }

    public CustomerRegistrationForm(AppFacade app)
    {
        _app = app;
        Text = "Atelier Lumière — Đăng ký thông tin khách hàng (Demo UI)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1180, 740);
        MinimumSize = new Size(1000, 640);
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

        // Keyboard navigation and submission shortcuts
        AcceptButton = _registerBtn;
        CancelButton = _backBtn;

        void OnInputChanged()
        {
            if (_errorPanel.Visible) ClearError();
            if (!_registerBtn.Enabled)
            {
                _registerBtn.Enabled = true;
                _registerBtn.Text = "Kiểm tra & Gửi đăng ký (Demo)";
                _successPanel.Visible = false;
                _successLabel.Visible = false;
            }
        }

        void BindEnterKey(TextBox box)
        {
            box.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    await ValidateAndRegisterAsync();
                }
            };
        }

        BindEnterKey(_fullName);
        BindEnterKey(_phone);
        BindEnterKey(_email);
        BindEnterKey(_password);
        BindEnterKey(_confirmPassword);

        // Bind interactive events
        _password.TextChanged += (_, _) =>
        {
            _strengthMeter.UpdatePassword(_password.Text);
            OnInputChanged();
        };

        _fullName.TextChanged += (_, _) => OnInputChanged();
        _phone.TextChanged += (_, _) => OnInputChanged();
        _email.TextChanged += (_, _) => OnInputChanged();
        _confirmPassword.TextChanged += (_, _) => OnInputChanged();
        _acceptTerms.CheckedChanged += (_, _) => OnInputChanged();

        FormClosing += (_, _) =>
        {
            // Security requirement: Never retain plaintext passwords in memory
            _password.Clear();
            _confirmPassword.Clear();
        };
    }

    private static Control BuildTopArchitecturalRibbon() => AuthChromeFactory.BuildTopRibbon(
        "ATELIER LUMIÈRE  //  ĐĂNG KÝ THÀNH VIÊN KHÁCH HÀNG",
        "BẢN XEM TRƯỚC GIAO DIỆN  •  DÀNH CHO KHÁCH HÀNG",
        Theme.Primitives.Ochre600);

    private Control BuildMainSplitCanvas()
    {
        var canvas = new FullCanvasAuthSurface(AuthVisualAssets.CustomerRegistrationBackgroundAssetId, StudioBackdropMood.WarmEditorial)
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 20, 26, 20)
        };

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        split.Controls.Add(BuildArtworkShowcasePanel(), 0, 0);
        split.Controls.Add(BuildRegistrationVaultPanel(), 1, 0);
        canvas.Controls.Add(split);
        return canvas;
    }

    private static Control BuildArtworkShowcasePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(12, 16, 18, 12),
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));  // Overline
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Headline
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Lead text
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 3 Benefit cards
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));  // Footer signature

        var overline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "ATELIER LUMIÈRE  •  THÀNH VIÊN KHÁCH HÀNG",
            ForeColor = Theme.Primitives.Terracotta200,
            Font = Theme.MonoFont(8.0f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var headline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Khởi đầu trải nghiệm\nnhiếp ảnh nghệ thuật.",
            ForeColor = Theme.Primitives.Alabaster50,
            Font = Theme.Font(18.0f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lead = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Bản xem trước giao diện thành viên. Hệ thống Cổng khách hàng trực tuyến sẽ cho phép quản lý lịch chụp và xem album ảnh trong các bản cập nhật tiếp theo.",
            ForeColor = Theme.Primitives.Stone300,
            Font = Theme.Font(8.8f),
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 4, 0, 0)
        };

        layout.Resize += (_, _) =>
        {
            var w = Math.Max(180, layout.ClientSize.Width - 6);
            var hSize = TextRenderer.MeasureText(headline.Text, headline.Font, new Size(w, 200), TextFormatFlags.WordBreak);
            var lSize = TextRenderer.MeasureText(lead.Text, lead.Font, new Size(w, 180), TextFormatFlags.WordBreak);
            layout.RowStyles[1].Height = Math.Max(70, hSize.Height + 10);
            layout.RowStyles[2].Height = Math.Max(44, lSize.Height + 8);
        };

        var benefits = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };
        benefits.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        benefits.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        benefits.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        benefits.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));

        benefits.Controls.Add(CreateBenefitTile(IconKind.Calendar, "Theo dõi Tiến độ (Dự kiến)", "Xem các giai đoạn thực hiện buổi chụp từ đặt lịch, chụp ảnh đến hoàn thiện sản phẩm."), 0, 0);
        benefits.Controls.Add(CreateBenefitTile(IconKind.Camera, "Thư viện Ảnh cá nhân (Dự kiến)", "Không gian duyệt trước bộ ảnh sau khi chụp và chọn ảnh ưng ý cho album hoàn thiện."), 0, 1);
        benefits.Controls.Add(CreateBenefitTile(IconKind.Shield, "Bảo mật Thông tin Riêng tư", "Dữ liệu đăng ký được quản lý riêng biệt, bảo vệ an toàn thông tin và lịch sử dịch vụ."), 0, 2);

        var signature = new Label
        {
            Dock = DockStyle.Fill,
            Text = "ATELIER LUMIÈRE  •  KHÔNG GIAN TRẢI NGHIỆM KHÁCH HÀNG",
            ForeColor = Theme.Primitives.Stone400,
            Font = Theme.Font(7.8f),
            TextAlign = ContentAlignment.BottomLeft,
            AutoEllipsis = true
        };

        layout.Controls.Add(overline, 0, 0);
        layout.Controls.Add(headline, 0, 1);
        layout.Controls.Add(lead, 0, 2);
        layout.Controls.Add(benefits, 0, 3);
        layout.Controls.Add(signature, 0, 4);

        panel.Controls.Add(layout);
        return panel;
    }

    private static Control CreateBenefitTile(IconKind icon, string title, string subtitle) =>
        AuthChromeFactory.CreateTranslucentPillarTile(
            icon,
            title,
            subtitle,
            Theme.Primitives.Sage200,
            Color.FromArgb(62, Theme.Primitives.Sage600));

    private Control BuildRegistrationVaultPanel()
    {
        var vault = new ElevatedFrostedAuthCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10, 2, 0, 2),
            Padding = new Padding(34, 18, 34, 16),
            AccentStripeColor = Theme.Primitives.Sage600,
            AutoScroll = true
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 12,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // 0: Badge & Category
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 1: Title
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // 2: Subtitle
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 68)); // 3: Full Name
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 68)); // 4: Phone & Email split
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 68)); // 5: Password
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 6: Password Strength Meter
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 68)); // 7: Confirm Password
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 8: Terms checkbox
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // 9: Action Buttons (Register & Cancel)
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 10: Inline Feedback Banners
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 11: Security disclaimer footer

        // Row 0: Badge
        var customerVisual = RoleVisualResolver.ForCustomer();
        var badgeCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        badgeCanvas.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new RectangleF(0, (badgeCanvas.Height - 28) / 2f, 28, 28);
            using var path = Theme.RoundedRectangle(box, 6f);
            using var fill = new SolidBrush(customerVisual.SoftBackground);
            using var pen = new Pen(customerVisual.BorderColor, 1f);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            VectorIcons.Draw(g, customerVisual.Icon, new RectangleF(box.Left + 6, box.Top + 6, 16, 16), customerVisual.Accent, 1.8f);

            using var badgeFont = Theme.MonoFont(7.8f, FontStyle.Bold);
            var badgeRect = new Rectangle(36, 0, Math.Max(100, badgeCanvas.Width - 40), badgeCanvas.Height);
            TextRenderer.DrawText(g, "ĐĂNG KÝ TÀI KHOẢN KHÁCH HÀNG (BẢN XEM TRƯỚC)", badgeFont, badgeRect, customerVisual.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        };

        // Row 1: Title
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đăng ký thông tin khách hàng",
            Font = Theme.Font(16.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // Row 2: Subtitle
        var desc = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Bản xem trước giao diện (Demo UI) — Nhập thông tin để kiểm tra biểu mẫu đăng ký.",
            Font = Theme.Font(8.8f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Row 3: Full Name Input
        var nameInputPanel = CreateInputField("HỌ VÀ TÊN KHÁCH HÀNG *", _fullName, IconKind.Users);

        // Row 4: Phone & Email Side-by-Side Grid
        var contactGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        contactGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        contactGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        contactGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var phoneInputPanel = CreateInputField("SỐ ĐIỆN THOẠI (10 SỐ) *", _phone, IconKind.Command, rightMargin: 6);
        var emailInputPanel = CreateInputField("ĐỊA CHỈ EMAIL *", _email, IconKind.Info, leftMargin: 6);
        contactGrid.Controls.Add(phoneInputPanel, 0, 0);
        contactGrid.Controls.Add(emailInputPanel, 1, 0);

        // Row 5: Password
        var passInputPanel = CreateInputField("MẬT KHẨU BẢO MẬT * (Tối thiểu 8 ký tự, gồm chữ & số)", _password, IconKind.Lock, allowReveal: true);

        // Row 7: Confirm Password
        var confirmInputPanel = CreateInputField("XÁC NHẬN MẬT KHẨU *", _confirmPassword, IconKind.Shield, allowReveal: true);

        // Row 9: Buttons
        var buttonsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        buttonsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        buttonsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
        buttonsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _registerBtn.Height = 44;
        _registerBtn.Dock = DockStyle.Fill;
        _registerBtn.Font = Theme.Font(9.5f, FontStyle.Bold);
        _registerBtn.Cursor = Cursors.Hand;
        _registerBtn.Margin = new Padding(0, 2, 6, 2);
        _registerBtn.Click += async (_, _) => await ValidateAndRegisterAsync();

        _backBtn.Height = 44;
        _backBtn.Dock = DockStyle.Fill;
        _backBtn.Font = Theme.Font(9.0f, FontStyle.Regular);
        _backBtn.Cursor = Cursors.Hand;
        _backBtn.Margin = new Padding(6, 2, 0, 2);
        _backBtn.Click += (_, _) =>
        {
            DialogResult = !string.IsNullOrEmpty(RegisteredIdentifier) ? DialogResult.OK : DialogResult.Cancel;
            Close();
        };

        buttonsRow.Controls.Add(_registerBtn, 0, 0);
        buttonsRow.Controls.Add(_backBtn, 1, 0);

        // Row 10: Feedback container
        var feedbackContainer = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        _errorPanel.Controls.Add(_errorLabel);
        _successPanel.Controls.Add(_successLabel);
        feedbackContainer.Controls.Add(_errorPanel);
        feedbackContainer.Controls.Add(_successPanel);

        // Row 11: Security disclaimer footer
        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Bản xem trước giao diện StudioManager v2.2  •  Chưa kết nối dịch vụ lưu trữ khách hàng",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.TextLight,
            Font = Theme.Font(7.6f),
            BackColor = Color.Transparent
        };

        // Assemble Form rows
        form.Controls.Add(badgeCanvas, 0, 0);
        form.Controls.Add(title, 0, 1);
        form.Controls.Add(desc, 0, 2);
        form.Controls.Add(nameInputPanel, 0, 3);
        form.Controls.Add(contactGrid, 0, 4);
        form.Controls.Add(passInputPanel, 0, 5);
        form.Controls.Add(_strengthMeter, 0, 6);
        form.Controls.Add(confirmInputPanel, 0, 7);
        form.Controls.Add(_acceptTerms, 0, 8);
        form.Controls.Add(buttonsRow, 0, 9);
        form.Controls.Add(feedbackContainer, 0, 10);
        form.Controls.Add(footer, 0, 11);

        vault.Controls.Add(form);
        return vault;
    }

    private static Panel CreateInputField(string label, TextBox input, IconKind icon, bool allowReveal = false, int leftMargin = 0, int rightMargin = 0)
    {
        var p = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(leftMargin, 0, rightMargin, 0),
            BackColor = Color.Transparent
        };

        var caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = label,
            UseMnemonic = false,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(7.8f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        var border = new ModernInputContainer
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Theme.Primitives.Alabaster50,
            LeadingIcon = icon
        };

        input.BackColor = Theme.Primitives.Alabaster50;
        input.GotFocus += (_, _) => border.Invalidate();
        input.LostFocus += (_, _) => border.Invalidate();
        border.Click += (_, _) => input.Focus();
        border.Controls.Add(input);

        Panel? revealBtn = null;
        if (allowReveal)
        {
            revealBtn = new Panel
            {
                Dock = DockStyle.Right,
                Width = 36,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            revealBtn.Paint += (_, e) =>
            {
                var k = input.UseSystemPasswordChar ? IconKind.Eye : IconKind.EyeOff;
                VectorIcons.Draw(e.Graphics, k, new RectangleF(10, (revealBtn.Height - 16) / 2f, 16, 16), Theme.Primary, 1.75f);
            };
            revealBtn.Click += (_, _) =>
            {
                input.UseSystemPasswordChar = !input.UseSystemPasswordChar;
                revealBtn.Invalidate();
                input.Focus();
            };
            border.Controls.Add(revealBtn);
        }

        void LayoutInput()
        {
            var rightGutter = revealBtn is not null ? 40 : 12;
            var th = Math.Max(20, input.PreferredHeight);
            input.SetBounds(38, Math.Max(4, (border.ClientSize.Height - th) / 2),
                Math.Max(30, border.ClientSize.Width - 38 - rightGutter), th);
        }

        border.Resize += (_, _) => LayoutInput();
        input.FontChanged += (_, _) => LayoutInput();
        LayoutInput();

        p.Controls.Add(border);
        p.Controls.Add(caption);
        return p;
    }

    private void ShowError(string message)
    {
        _successPanel.Visible = false;
        _successLabel.Visible = false;
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

    private void ShowSuccess(string fullName, string identifier, string email)
    {
        ClearError();
        RegisteredIdentifier = identifier;
        CreatedDemoSession = DesktopDemoSessionStore.RegisterDemoCustomer(new CustomerRegistrationRequestDto(
            fullName, identifier, email, _password.Text, _confirmPassword.Text, null, _acceptTerms.Checked));

        _successLabel.Text =
            $"✓ ĐÃ KHỞI TẠO HỒ SƠ KHÁCH HÀNG (PHIÊN DEMO)\n" +
            $"• Họ và tên: {fullName}  •  Liên hệ: {identifier} • {email}\n" +
            $"• Trạng thái liên kết lịch sử: Chưa liên kết hồ sơ cũ (bảo mật thông tin)\n\n" +
            $"Nhấn 'Mở Không gian Khách hàng (Demo)' để trải nghiệm đặt lịch ngay hoặc 'Quay lại'.";

        _successLabel.Visible = true;
        _successPanel.Visible = true;
        _successPanel.BringToFront();
        _successPanel.Invalidate();

        _registerBtn.Enabled = false;
        _registerBtn.Text = "Đã xác nhận dữ liệu (Demo)";
        _backBtn.Text = "Mở Không gian Khách hàng (Demo)";
    }

    private async Task ValidateAndRegisterAsync()
    {
        ClearError();

        var fullName = _fullName.Text.Trim();
        var phone = _phone.Text.Trim();
        var email = _email.Text.Trim();
        var password = _password.Text;
        var confirm = _confirmPassword.Text;

        // 1. Full name validation
        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowError("Vui lòng nhập họ và tên khách hàng.");
            _fullName.Focus();
            return;
        }

        if (fullName.Length < 2 || fullName.Length > 80)
        {
            ShowError("Họ và tên cần có độ dài từ 2 đến 80 ký tự.");
            _fullName.Focus();
            return;
        }

        // 2. Phone validation (10 digits starting with 0)
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowError("Vui lòng nhập số điện thoại liên lạc.");
            _phone.Focus();
            return;
        }

        if (!Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            ShowError("Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại Việt Nam gồm 10 chữ số (bắt đầu bằng số 0).");
            _phone.Focus();
            return;
        }

        // 3. Email validation
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Vui lòng nhập địa chỉ email.");
            _email.Focus();
            return;
        }

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ShowError("Địa chỉ email chưa đúng định dạng chuẩn (vd: name@example.com).");
            _email.Focus();
            return;
        }

        // 4. Password validation (min 8 chars, letters and digits)
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập mật khẩu bảo mật.");
            _password.Focus();
            return;
        }

        if (password.Length < 8)
        {
            ShowError("Mật khẩu phải có độ dài tối thiểu 8 ký tự.");
            _password.Focus();
            return;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        if (!hasLetter || !hasDigit)
        {
            ShowError("Mật khẩu phải chứa ít nhất một chữ cái và một chữ số.");
            _password.Focus();
            return;
        }

        // 5. Confirm password
        if (string.IsNullOrEmpty(confirm))
        {
            ShowError("Vui lòng nhập lại mật khẩu xác nhận.");
            _confirmPassword.Focus();
            return;
        }

        if (password != confirm)
        {
            ShowError("Mật khẩu xác nhận không khớp với mật khẩu đã nhập.");
            _confirmPassword.Focus();
            return;
        }

        // 6. Terms acceptance
        if (!_acceptTerms.Checked)
        {
            ShowError("Bạn cần đánh dấu đồng ý với Điều khoản Dịch vụ và Quy định Bảo mật để tiếp tục.");
            _acceptTerms.Focus();
            return;
        }

        // Simulation latency for realistic feel
        _registerBtn.Enabled = false;
        _registerBtn.Text = "Đang kiểm tra quy tắc...";
        _registerBtn.Refresh();

        await Task.Delay(350);

        // Success demonstration (Safe, pure frontend, demo mode)
        ShowSuccess(fullName, phone, email);
    }
}

/// <summary>
/// Password strength meter panel for WinForms.
/// Evaluates length, numbers, letters, case mixing and special symbols.
/// </summary>
internal sealed class PasswordStrengthMeterPanel : Panel
{
    private int _score;
    private string _strengthText = "Chưa nhập mật khẩu";
    private Color _barColor = Color.FromArgb(200, Theme.Primitives.Stone400);

    public PasswordStrengthMeterPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    public void UpdatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            _score = 0;
            _strengthText = "Chưa nhập mật khẩu";
            _barColor = Color.FromArgb(200, Theme.Primitives.Stone400);
            Invalidate();
            return;
        }

        var hasLength = password.Length >= 8;
        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        var hasMixedCase = password.Any(char.IsUpper) && password.Any(char.IsLower);
        var hasSymbol = password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

        var pts = 0;
        if (hasLength) pts++;
        if (hasLetter && hasDigit) pts++;
        if (hasMixedCase) pts++;
        if (hasSymbol) pts++;
        if (password.Length >= 12) pts++;

        if (!hasLength)
        {
            _score = 1;
            _strengthText = "Mật khẩu yếu (cần tối thiểu 8 ký tự)";
            _barColor = Theme.Danger;
        }
        else
        {
            switch (pts)
            {
                case <= 2:
                    _score = 2;
                    _strengthText = "Mật khẩu trung bình (nên thêm chữ hoa hoặc ký tự đặc biệt)";
                    _barColor = Theme.Warning;
                    break;
                case 3:
                case 4:
                    _score = 3;
                    _strengthText = "Mật khẩu khá an toàn";
                    _barColor = Color.FromArgb(180, Theme.Primitives.Sage600);
                    break;
                default:
                    _score = 4;
                    _strengthText = "Mật khẩu rất mạnh (tuyệt vời)";
                    _barColor = Theme.Primitives.Sage600;
                    break;
            }
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var w = Width;
        var h = Height;
        if (w < 40 || h < 14) return;

        // Top text row
        using var font = Theme.Font(7.6f);
        using var textBrush = new SolidBrush(Theme.TextSecondary);
        var textRect = new Rectangle(0, 0, w, 16);
        TextRenderer.DrawText(g, $"Độ an toàn: {_strengthText}", font, textRect, _score <= 1 ? Theme.Danger : (_score == 2 ? Theme.Warning : Theme.Success),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        // Background track bar (4 rounded segments)
        var barY = 20;
        var barH = 5;
        var gap = 4;
        var segW = Math.Max(10, (w - gap * 3) / 4);

        for (var i = 0; i < 4; i++)
        {
            var segRect = new RectangleF(i * (segW + gap), barY, segW, barH);
            using var segPath = Theme.RoundedRectangle(segRect, 2.5f);

            var isFilled = (i + 1) <= _score;
            using var brush = new SolidBrush(isFilled ? _barColor : Color.FromArgb(40, Theme.Primitives.Stone400));
            g.FillPath(brush, segPath);
        }
    }
}
