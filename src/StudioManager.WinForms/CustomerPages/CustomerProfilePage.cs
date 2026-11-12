using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerProfilePage : UserControl
{
    private readonly CustomerSessionContext _session;
    private readonly ICustomerNavigator _nav;

    private readonly TextBox _nameBox = new() { Width = 340, Font = Theme.Font(9.4f) };
    private readonly TextBox _phoneBox = new() { Width = 240, Font = Theme.Font(9.4f) };
    private readonly TextBox _emailBox = new() { Width = 340, Font = Theme.Font(9.4f) };
    private readonly TextBox _addressBox = new() { Width = 380, Font = Theme.Font(9.4f) };

    private readonly TextBox _verifyNameBox = new() { Width = 340, Font = Theme.Font(9.2f) };
    private readonly TextBox _verifyPhoneBox = new() { Width = 240, Font = Theme.Font(9.2f) };
    private readonly TextBox _verifyRefBox = new() { Width = 260, Font = Theme.Font(9.2f), PlaceholderText = "Ví dụ: LC000001 hoặc KH000001" };
    private readonly TextBox _verifyNoteBox = new() { Width = 420, Height = 50, Multiline = true, Font = Theme.Font(8.8f), PlaceholderText = "Ghi chú thêm (ví dụ: đã từng chụp gói Gia đình tháng 09/2026)..." };

    private readonly AtelierInlineAlert _profileAlert = new() { Dock = DockStyle.Top, Height = 58, Margin = new Padding(0, 0, 0, 8) };
    private CardPanel _statusCard = null!;
    private Label _statusPillLbl = null!;
    private Label _statusSummaryLbl = null!;
    private Label _statusExplainLbl = null!;
    private Label _statusNextLbl = null!;

    // Isolated Developer / QA 5-state simulator drawer (collapsed by default)
    private Panel _qaDrawerBody = null!;
    private ModernButton _qaToggleBtn = null!;
    private readonly ComboBox _qaStateCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, Font = Theme.Font(8.9f) };
    private readonly TextBox _qaRefBox = new() { Width = 180, Font = Theme.Font(8.9f), PlaceholderText = "LC000001" };
    private bool _qaExpanded;

    public CustomerProfilePage(CustomerSessionContext session, ICustomerNavigator nav)
    {
        _session = session;
        _nav = nav;

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Hồ sơ cá nhân & Xác minh quyền sở hữu",
            "Quản lý thông tin liên hệ và gửi yêu cầu đối chiếu hồ sơ lịch chụp cũ một cách an toàn");

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));

        // Left: Customer Contact Profile
        var leftCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(18), AutoScroll = true };
        var leftFlow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _nameBox.Text = _session.Profile.FullName;
        _phoneBox.Text = _session.Profile.PhoneNumber;
        _emailBox.Text = _session.Profile.Email;
        _addressBox.Text = _session.Profile.Address;

        _verifyNameBox.Text = _session.Profile.FullName;
        _verifyPhoneBox.Text = _session.Profile.PhoneNumber.Replace(" ", "");

        Control Field(string label, Control c, int h = 58)
        {
            var p = new Panel { Size = new Size(420, h), Margin = new Padding(0, 0, 0, 6) };
            var l = new Label { Dock = DockStyle.Top, Height = 20, Text = label, Font = Theme.Font(8.2f, FontStyle.Bold), ForeColor = Theme.TextSecondary };
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 0) };
            wrap.Controls.Add(c);
            p.Controls.Add(wrap);
            p.Controls.Add(l);
            return p;
        }

        var secTitle = new Label
        {
            Size = new Size(420, 26),
            Text = "Thông tin tài khoản hiện tại",
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 0, 8)
        };

        var saveBtn = Theme.Button("Lưu thông tin hồ sơ (Demo)", Theme.Primary, IconKind.Check);
        saveBtn.Width = 228;
        saveBtn.Click += (_, _) => SaveProfile();

        var pwdBtn = Theme.OutlineButton("Đổi mật khẩu (Demo)");
        pwdBtn.Width = 178;
        pwdBtn.Click += (_, _) => AtelierNotificationService.ShowInfo(
            FindForm(),
            "Chức năng đổi mật khẩu Khách hàng đã có giao diện chuẩn bị cho bước tích hợp Backend.\nKhi kết nối SQL Server, thao tác này sẽ yêu cầu mật khẩu hiện tại trước khi cập nhật mật khẩu mới bằng PBKDF2.",
            "Bảo mật tài khoản khách hàng");

        leftFlow.Controls.Add(secTitle);
        leftFlow.Controls.Add(Field("HỌ VÀ TÊN", _nameBox));
        leftFlow.Controls.Add(Field("SỐ ĐIỆN THOẠI LIÊN HỆ", _phoneBox));
        leftFlow.Controls.Add(Field("EMAIL", _emailBox));
        leftFlow.Controls.Add(Field("ĐỊA CHỈ", _addressBox));
        var btnRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 10, 0, 8) };
        btnRow.Controls.Add(saveBtn);
        btnRow.Controls.Add(pwdBtn);
        leftFlow.Controls.Add(btnRow);
        leftCard.Controls.Add(leftFlow);
        leftCard.Controls.Add(_profileAlert);

        // Right: Read-only Identity Link Status + Controlled Verification Request + Isolated QA Tools
        var rightHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), AutoScroll = true };
        var rightStack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        _statusCard = new CardPanel
        {
            Size = new Size(560, 174),
            Padding = new Padding(16),
            AccentStripeLeft = true,
            AccentStripeThickness = 4,
            Margin = new Padding(0, 0, 0, 12)
        };
        var sTop = new Panel { Dock = DockStyle.Top, Height = 26 };
        _statusPillLbl = new Label { Dock = DockStyle.Right, Width = 230, Font = Theme.Font(8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
        sTop.Controls.Add(_statusPillLbl);
        sTop.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "TRẠNG THÁI LIÊN KẾT HỒ SƠ STUDIO", Font = Theme.MonoFont(7.8f, FontStyle.Bold), ForeColor = Theme.Muted });

        _statusSummaryLbl = new Label { Dock = DockStyle.Top, Height = 24, Font = Theme.Font(10.0f, FontStyle.Bold), ForeColor = Theme.Text };
        _statusExplainLbl = new Label { Dock = DockStyle.Fill, Font = Theme.Font(8.7f), ForeColor = Theme.TextSecondary };
        _statusNextLbl = new Label { Dock = DockStyle.Bottom, Height = 24, Font = Theme.Font(8.3f, FontStyle.Bold), ForeColor = Theme.Primary };
        _statusCard.Controls.Add(_statusExplainLbl);
        _statusCard.Controls.Add(_statusNextLbl);
        _statusCard.Controls.Add(_statusSummaryLbl);
        _statusCard.Controls.Add(sTop);

        // Controlled customer verification request form
        var reqCard = new CardPanel
        {
            Size = new Size(560, 344),
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12)
        };
        var reqFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        reqFlow.Controls.Add(new Label
        {
            Size = new Size(520, 24),
            Text = "Gửi yêu cầu đối chiếu hồ sơ lịch chụp cũ",
            Font = Theme.Font(10.2f, FontStyle.Bold),
            ForeColor = Theme.Text
        });
        reqFlow.Controls.Add(new Label
        {
            Size = new Size(520, 34),
            Text = "Để bảo vệ quyền riêng tư, hệ thống không tự mở lịch sử cũ khi trùng số điện thoại.\nHãy gửi thông tin đối chiếu để nhân viên Lễ tân kiểm tra và phê duyệt liên kết.",
            Font = Theme.Font(8.3f),
            ForeColor = Theme.Muted
        });
        reqFlow.Controls.Add(Field("HỌ TÊN TRÊN HỒ SƠ STUDIO *", _verifyNameBox, 46));
        reqFlow.Controls.Add(Field("SỐ ĐIỆN THOẠI / EMAIL ĐÃ DÙNG *", _verifyPhoneBox, 46));
        reqFlow.Controls.Add(Field("MÃ LỊCH CHỤP HOẶC MÃ KHÁCH HÀNG CŨ *", _verifyRefBox, 46));
        reqFlow.Controls.Add(Field("GHI CHÚ ĐỐI CHIẾU", _verifyNoteBox, 58));

        var sendVerifyBtn = Theme.Button("Gửi yêu cầu đối chiếu hồ sơ cũ", Theme.Primitives.Sage600, IconKind.Shield);
        sendVerifyBtn.Width = 264;
        sendVerifyBtn.Margin = new Padding(0, 4, 0, 0);
        sendVerifyBtn.Click += (_, _) => SubmitVerificationRequest();
        reqFlow.Controls.Add(sendVerifyBtn);
        reqCard.Controls.Add(reqFlow);

        // Isolated QA / Developer Preview Panel (strictly separated and collapsed by default)
        var qaCard = new CardPanel
        {
            Size = new Size(560, 178),
            BackColor = Theme.WarningLight,
            BorderColor = Theme.WarningBorder,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 8)
        };
        _qaToggleBtn = Theme.OutlineButton("Công cụ kiểm thử QA / Developer: Mô phỏng 5 trạng thái ▼", Theme.Warning);
        _qaToggleBtn.Dock = DockStyle.Top;
        _qaToggleBtn.Height = 32;
        _qaToggleBtn.Click += (_, _) => ToggleQaDrawer(qaCard);

        _qaDrawerBody = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(0, 8, 0, 0) };
        foreach (CustomerIdentityLinkState st in Enum.GetValues<CustomerIdentityLinkState>())
            _qaStateCombo.Items.Add(st);
        _qaStateCombo.SelectedItem = _session.Profile.LinkState;
        var qaNote = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "Khu vực dành riêng cho QA/Tester kiểm tra giao diện qua 5 trạng thái liên kết (không xuất hiện ở tài khoản người dùng thật khi vận hành Backend).",
            Font = Theme.Font(7.9f),
            ForeColor = Theme.WarningHover
        };
        var qaControls = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 74, WrapContents = true, Padding = new Padding(0, 4, 0, 0) };
        var qaApplyBtn = Theme.Button("Áp dụng trạng thái QA", Theme.Warning, IconKind.Shield);
        qaApplyBtn.Width = 190;
        qaApplyBtn.Height = 32;
        qaApplyBtn.Click += (_, _) => ApplyQaPreviewState();
        qaControls.Controls.AddRange([_qaStateCombo, _qaRefBox, qaApplyBtn]);
        _qaDrawerBody.Controls.Add(qaControls);
        _qaDrawerBody.Controls.Add(qaNote);

        qaCard.Controls.Add(_qaDrawerBody);
        qaCard.Controls.Add(_qaToggleBtn);
        qaCard.Height = 56;

        rightStack.Controls.Add(_statusCard);
        rightStack.Controls.Add(reqCard);
        rightStack.Controls.Add(qaCard);
        rightHost.Controls.Add(rightStack);
        rightHost.SizeChanged += (_, _) =>
        {
            var w = Math.Max(340, rightHost.ClientSize.Width - 20);
            rightStack.Width = w;
            _statusCard.Width = w;
            reqCard.Width = w;
            qaCard.Width = w;
        };

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightHost, 1, 0);
        Controls.Add(split);
        Controls.Add(header);

        RenderCurrentIdentityStatus();
    }

    private void ToggleQaDrawer(CardPanel qaCard)
    {
        _qaExpanded = !_qaExpanded;
        _qaDrawerBody.Visible = _qaExpanded;
        _qaToggleBtn.Text = _qaExpanded
            ? "Công cụ kiểm thử QA / Developer: Thu gọn ▲"
            : "Công cụ kiểm thử QA / Developer: Mô phỏng 5 trạng thái ▼";
        qaCard.Height = _qaExpanded ? 176 : 56;
    }

    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            _profileAlert.ShowMessage(AtelierNotificationKind.Error, "Thiếu họ tên", "Họ và tên không được để trống.");
            return;
        }
        var updated = _session.Profile with
        {
            FullName = _nameBox.Text.Trim(),
            PhoneNumber = _phoneBox.Text.Trim(),
            Email = _emailBox.Text.Trim(),
            Address = _addressBox.Text.Trim(),
            LastUpdatedLabel = DateTime.Now.ToString("HH:mm dd/MM")
        };
        DesktopDemoSessionStore.UpdateCurrentProfile(updated);
        _profileAlert.ShowMessage(AtelierNotificationKind.Success, "Đã cập nhật hồ sơ (Demo)", "Thông tin liên hệ mới đã được lưu trong phiên hiện tại.");
    }

    private void SubmitVerificationRequest()
    {
        if (string.IsNullOrWhiteSpace(_verifyNameBox.Text) ||
            string.IsNullOrWhiteSpace(_verifyPhoneBox.Text) ||
            string.IsNullOrWhiteSpace(_verifyRefBox.Text))
        {
            AtelierNotificationService.ShowWarning(
                FindForm(),
                "Vui lòng điền đầy đủ Họ tên, Số điện thoại/Email và Mã lịch chụp hoặc Mã khách hàng cũ để nhân viên có căn cứ đối chiếu.",
                "Thiếu thông tin đối chiếu");
            return;
        }

        var dto = new CustomerRecoveryRequestDto(
            _verifyPhoneBox.Text.Trim(),
            _verifyNameBox.Text.Trim(),
            _verifyRefBox.Text.Trim(),
            _verifyNoteBox.Text.Trim());

        var updated = DesktopDemoSessionStore.SubmitCustomerIdentityVerificationRequest(_session.Profile.DemoAccountId, dto);
        _qaStateCombo.SelectedItem = updated.LinkState;
        RenderCurrentIdentityStatus();
        _nav.RefreshBadgeCounts();

        AtelierNotificationService.ShowSuccess(
            FindForm(),
            $"Đã gửi yêu cầu đối chiếu cho mã tham chiếu '{_verifyRefBox.Text.Trim()}'.\nTrạng thái hiện tại: Đang chờ nhân viên kiểm tra — dữ liệu lịch cũ vẫn được khóa an toàn cho đến khi nhân sự xác nhận.",
            "Đã tiếp nhận yêu cầu xác minh");
    }

    private void RenderCurrentIdentityStatus()
    {
        var eval = DesktopDemoSessionStore.PreviewIdentityLinkState(_session.Profile.LinkState, _session.Profile.LinkedCustomerCode);
        var (accent, bg) = _session.Profile.LinkState switch
        {
            CustomerIdentityLinkState.VerifiedOwnership or CustomerIdentityLinkState.AuthorizedManualLink
                => (Theme.Success, Theme.SuccessLight),
            CustomerIdentityLinkState.PendingVerification => (Theme.Warning, Theme.WarningLight),
            CustomerIdentityLinkState.RejectedVerification => (Theme.Danger, Theme.DangerLight),
            _ => (Theme.Primary, Theme.Primitives.Alabaster50)
        };

        _statusCard.AccentStripeColor = accent;
        _statusCard.BackColor = bg;
        _statusPillLbl.Text = eval.StateLabel.ToUpperInvariant();
        _statusPillLbl.ForeColor = accent;
        _statusSummaryLbl.Text = eval.SummaryTitle;
        _statusExplainLbl.Text = eval.Explanation + $"\nCập nhật gần nhất: {_session.Profile.LastUpdatedLabel}";
        _statusNextLbl.Text = eval.CanViewExistingBookings
            ? $"Quyền lịch sử cũ: ĐÃ MỞ ({_session.Profile.LinkedCustomerCode ?? "Demo"})"
            : $"Quyền lịch sử cũ: ĐANG KHÓA AN TOÀN • {eval.NextRecommendedAction}";
        _statusNextLbl.ForeColor = eval.CanViewExistingBookings ? Theme.Success : accent;
        _statusCard.Invalidate();
    }

    private void ApplyQaPreviewState()
    {
        if (_qaStateCombo.SelectedItem is not CustomerIdentityLinkState st) return;
        var eval = DesktopDemoSessionStore.PreviewIdentityLinkState(st, _qaRefBox.Text);
        var updated = _session.Profile with
        {
            LinkState = st,
            LinkedCustomerCode = eval.CandidateCustomerCode,
            LinkStatusHeadline = eval.StateLabel,
            LinkStatusDescription = eval.Explanation,
            LastUpdatedLabel = DateTime.Now.ToString("HH:mm dd/MM")
        };
        DesktopDemoSessionStore.UpdateCurrentProfile(updated);
        RenderCurrentIdentityStatus();
        _nav.RefreshBadgeCounts();
        AtelierNotificationService.ShowToast(
            FindForm(),
            AtelierNotificationKind.Info,
            "Đã chuyển trạng thái kiểm thử QA",
            $"Hồ sơ tạm thời chuyển sang '{eval.StateLabel}'. Hãy kiểm tra tab Lịch chụp & Album.");
    }
}
