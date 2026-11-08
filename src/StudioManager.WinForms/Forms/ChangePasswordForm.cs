using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

public sealed class ChangePasswordForm : Form
{
    private readonly AppFacade _app;
    private readonly UserSession _session;
    private readonly TextBox _password = new()
    {
        PlaceholderText = "Nhập mật khẩu mới",
        UseSystemPasswordChar = true,
        Font = Theme.Font(10),
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = Theme.Text
    };
    private readonly TextBox _confirmation = new()
    {
        PlaceholderText = "Nhập lại mật khẩu mới",
        UseSystemPasswordChar = true,
        Font = Theme.Font(10),
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = Theme.Text
    };
    private readonly ModernButton _save = Theme.Button("Cập nhật mật khẩu");
    private readonly Label _error = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.Danger,
        Font = Theme.Font(8.5f),
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Visible = false
    };

    public ChangePasswordForm(AppFacade app, UserSession session)
    {
        _app = app;
        _session = session;
        Text = "Đổi mật khẩu bảo mật • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 490);
        MinimumSize = new Size(460, 460);
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Theme.Background;
        Font = Theme.Font();

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(36, 28, 36, 24),
            Margin = new Padding(16),
            BackColor = Theme.Surface,
            CornerRadius = 12
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 8,
            ColumnCount = 1,
            BackColor = Theme.Surface
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Title
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Subtitle
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); // Password
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); // Confirm
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // Notice
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // Error
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Button
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "🔒   Đổi mật khẩu",
            ForeColor = Theme.Text,
            Font = Theme.Font(18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var introLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Xin chào {_session.HoTen}.\nĐây là lần đăng nhập đầu tiên hoặc mật khẩu đã được quản trị viên đặt lại.",
            ForeColor = Theme.Muted,
            Font = Theme.Font(9.0f),
            TextAlign = ContentAlignment.TopCenter
        };

        var ruleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "• Tối thiểu 8 ký tự, bao gồm chữ hoa, chữ thường và chữ số.",
            ForeColor = Theme.Muted,
            Font = Theme.Font(8.3f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _save.Dock = DockStyle.Fill;
        _save.Margin = new Padding(0, 2, 0, 2);
        _save.Click += async (_, _) => await SaveAsync();

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(introLabel, 0, 1);
        layout.Controls.Add(PasswordField("MẬT KHẨU MỚI", _password), 0, 2);
        layout.Controls.Add(PasswordField("XÁC NHẬN MẬT KHẨU", _confirmation), 0, 3);
        layout.Controls.Add(ruleLabel, 0, 4);
        layout.Controls.Add(_error, 0, 5);
        layout.Controls.Add(_save, 0, 6);

        card.Controls.Add(layout);
        Controls.Add(card);
        AcceptButton = _save;
    }

    private async Task SaveAsync()
    {
        _error.Visible = false;
        _error.Text = "";
        _save.Enabled = false;
        _save.Text = "Đang cập nhật...";

        try
        {
            var result = await _app.Auth.ChangePasswordAsync(_session.TaiKhoanId, _password.Text, _confirmation.Text);
            if (!result.Success)
            {
                _error.Text = "⚠  " + result.Message;
                _error.Visible = true;
                return;
            }

            MessageBox.Show(this, "Mật khẩu đã được đổi thành công. Bạn có thể tiếp tục sử dụng hệ thống.",
                "StudioManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _error.Text = "⚠  Không thể đổi mật khẩu: " + ex.Message;
            _error.Visible = true;
        }
        finally
        {
            _save.Enabled = true;
            _save.Text = "Cập nhật mật khẩu";
        }
    }

    private static Panel PasswordField(string label, TextBox input)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 6), BackColor = Color.Transparent };

        var caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = label,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var border = new ModernInputContainer
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Color.White,
            Padding = new Padding(12, 11, 8, 10)
        };

        var reveal = new Label
        {
            Dock = DockStyle.Right,
            Width = 44,
            Text = "Hiện",
            ForeColor = Theme.Primary,
            Font = Theme.Font(8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        reveal.Click += (_, _) =>
        {
            input.UseSystemPasswordChar = !input.UseSystemPasswordChar;
            reveal.Text = input.UseSystemPasswordChar ? "Hiện" : "Ẩn";
        };

        input.Dock = DockStyle.Fill;
        border.Controls.Add(input);
        border.Controls.Add(reveal);

        panel.Controls.Add(border);
        panel.Controls.Add(caption);
        return panel;
    }
}
