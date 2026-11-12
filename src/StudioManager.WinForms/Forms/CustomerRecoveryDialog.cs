using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Forms;

/// <summary>
/// Customer account recovery and safe existing-booking ownership verification explanation modal.
/// Frontend Demo mode only: does not send real emails/OTPs or automatically merge profiles by phone.
/// </summary>
public sealed class CustomerRecoveryDialog : Form
{
    private readonly TextBox _contactInput = new() { Dock = DockStyle.Fill, Font = Theme.Font(9.8f), PlaceholderText = "Email hoặc SĐT đã đăng ký" };
    private readonly TextBox _fullNameInput = new() { Dock = DockStyle.Fill, Font = Theme.Font(9.8f), PlaceholderText = "Họ và tên dùng trên lịch chụp (nếu có)" };
    private readonly TextBox _bookingRefInput = new() { Dock = DockStyle.Fill, Font = Theme.Font(9.8f), PlaceholderText = "Ví dụ: LC000001 (không bắt buộc)" };
    private readonly Label _feedback = new() { Dock = DockStyle.Fill, Font = Theme.Font(8.7f), ForeColor = Theme.TextSecondary };

    public CustomerRecoveryDialog()
    {
        Text = "Khôi phục truy cập & Xác minh liên kết hồ sơ (Demo)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 440);
        MinimumSize = new Size(500, 410);
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var intro = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Để bảo vệ dữ liệu cá nhân, hệ thống KHÔNG tự động gộp lịch sử chỉ qua số điện thoại.\nNhập thông tin để xem quy trình xác minh khôi phục trong phiên bản Desktop:",
            Font = Theme.Font(8.9f),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Control MakeRow(string caption, TextBox box)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 4) };
            var lbl = new Label { Dock = DockStyle.Top, Height = 22, Text = caption, Font = Theme.Font(8.2f, FontStyle.Bold), ForeColor = Theme.TextSecondary };
            var wrap = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(8, 5, 8, 5), BackColor = Theme.Primitives.Alabaster50 };
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.Primitives.Alabaster50;
            wrap.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.BorderStrong, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, wrap.Width - 1, wrap.Height - 1);
            };
            wrap.Controls.Add(box);
            p.Controls.Add(wrap);
            p.Controls.Add(lbl);
            return p;
        }

        var infoCard = new Controls.CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            AccentStripeColor = Theme.Primitives.Sage600,
            AccentStripeLeft = true,
            AccentStripeThickness = 4,
            Padding = new Padding(14, 10, 12, 10),
            Margin = new Padding(0, 6, 0, 8)
        };
        _feedback.Text = "Chế độ Demo: Sau khi đối chiếu tên + SĐT + mã lịch chụp cũ tại backend, trạng thái liên kết sẽ chuyển sang 'Đang chờ xác minh' hoặc do nhân sự Tiếp nhận duyệt thủ công.";
        infoCard.Controls.Add(_feedback);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var closeBtn = Theme.OutlineButton("Đóng", null, IconKind.Close);
        var submitBtn = Theme.Button("Mô phỏng gửi yêu cầu khôi phục", Theme.Primitives.Sage600, IconKind.Check);
        submitBtn.Width = 250;

        submitBtn.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_contactInput.Text))
            {
                _feedback.ForeColor = Theme.Danger;
                _feedback.Text = "Vui lòng nhập email hoặc số điện thoại để mô phỏng kiểm tra yêu cầu khôi phục.";
                _contactInput.Focus();
                return;
            }
            var eval = DesktopDemoSessionStore.PreviewIdentityLinkState(
                CustomerIdentityLinkState.PendingVerification,
                _bookingRefInput.Text);
            if (DesktopDemoSessionStore.CurrentCustomerSession is { } activeSession)
            {
                DesktopDemoSessionStore.SubmitCustomerIdentityVerificationRequest(
                    activeSession.Profile.DemoAccountId,
                    new CustomerRecoveryRequestDto(
                        _contactInput.Text.Trim(),
                        _fullNameInput.Text.Trim(),
                        _bookingRefInput.Text.Trim(),
                        "Gửi từ hộp thoại khôi phục & đối chiếu hồ sơ"));
            }
            _feedback.ForeColor = Theme.Success;
            _feedback.Text = $"✓ Đã tiếp nhận yêu cầu đối chiếu (Demo): {eval.SummaryTitle}. {eval.NextRecommendedAction}";
        };
        closeBtn.Click += (_, _) => Close();

        actions.Controls.Add(closeBtn);
        actions.Controls.Add(submitBtn);

        root.Controls.Add(intro, 0, 0);
        root.Controls.Add(MakeRow("EMAIL HOẶC SỐ ĐIỆN THOẠI *", _contactInput), 0, 1);
        root.Controls.Add(MakeRow("HỌ VÀ TÊN XÁC MINH", _fullNameInput), 0, 2);
        root.Controls.Add(MakeRow("MÃ LỊCH CHỤP CŨ THAM CHIẾU (NẾU CÓ)", _bookingRefInput), 0, 3);
        root.Controls.Add(infoCard, 0, 4);
        root.Controls.Add(actions, 0, 5);

        Controls.Add(root);
        AcceptButton = submitBtn;
        CancelButton = closeBtn;
    }
}

