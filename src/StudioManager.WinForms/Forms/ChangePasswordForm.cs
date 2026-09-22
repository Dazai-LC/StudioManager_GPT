using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

public sealed class ChangePasswordForm : Form
{
    private readonly AppFacade _app;
    private readonly UserSession _session;
    private readonly TextBox _password = PasswordBox("Nhập mật khẩu mới");
    private readonly TextBox _confirmation = PasswordBox("Nhập lại mật khẩu mới");
    private readonly Button _save = Theme.Button("Đổi mật khẩu");
    private readonly Label _error = new() { Dock = DockStyle.Fill, ForeColor = Theme.Danger, Font = Theme.Font(8.8f), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };

    public ChangePasswordForm(AppFacade app, UserSession session)
    {
        _app = app; _session = session; Text = "Đổi mật khẩu bắt buộc • StudioManager";
        StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(500, 480); MinimumSize = new Size(480, 460);
        MaximizeBox = false; MinimizeBox = false; FormBorderStyle = FormBorderStyle.FixedDialog; BackColor = Theme.Background; Font = Theme.Font();

        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(42, 30, 42, 28), Margin = new Padding(24) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, ColumnCount = 1, BackColor = Theme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "🔒  Đổi mật khẩu", ForeColor = Theme.Text, Font = Theme.Font(20, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = $"Xin chào {_session.HoTen}. Đây là lần đăng nhập đầu tiên hoặc mật khẩu vừa được đặt lại.", ForeColor = Theme.Muted, Font = Theme.Font(9.2f), TextAlign = ContentAlignment.TopCenter }, 0, 1);
        layout.Controls.Add(Ui.Field("MẬT KHẨU MỚI", _password, 390), 0, 2); layout.Controls.Add(Ui.Field("XÁC NHẬN MẬT KHẨU", _confirmation, 390), 0, 3);
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Tối thiểu 8 ký tự, gồm chữ hoa, chữ thường và chữ số.", ForeColor = Theme.Muted, Font = Theme.Font(8.5f), TextAlign = ContentAlignment.MiddleLeft }, 0, 4);
        layout.Controls.Add(_error, 0, 5); _save.Dock = DockStyle.Fill; _save.Margin = new Padding(0, 3, 0, 3); layout.Controls.Add(_save, 0, 6);
        card.Controls.Add(layout); Controls.Add(card); AcceptButton = _save; _save.Click += async (_, _) => await SaveAsync();
    }

    private async Task SaveAsync()
    {
        _error.Text = ""; _save.Enabled = false; _save.Text = "Đang cập nhật...";
        try
        {
            var result = await _app.Auth.ChangePasswordAsync(_session.TaiKhoanId, _password.Text, _confirmation.Text);
            if (!result.Success) { _error.Text = result.Message; return; }
            MessageBox.Show(this, "Mật khẩu đã được đổi. Bạn có thể tiếp tục sử dụng hệ thống.", "StudioManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { _error.Text = "Không thể đổi mật khẩu. " + ex.Message; }
        finally { _save.Enabled = true; _save.Text = "Đổi mật khẩu"; }
    }

    private static TextBox PasswordBox(string placeholder) => new() { PlaceholderText = placeholder, UseSystemPasswordChar = true, Font = Theme.Font(10.5f), BorderStyle = BorderStyle.FixedSingle };
}
