using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

public sealed class LoginForm : Form
{
    private readonly AppFacade _app; private readonly TextBox _username = new() { PlaceholderText = "Tên đăng nhập" }; private readonly TextBox _password = new() { PlaceholderText = "Mật khẩu", UseSystemPasswordChar = true }; private readonly Button _login = Theme.Button("Đăng nhập"); private readonly Label _error = new() { ForeColor = Theme.Danger, AutoSize = false, Height = 32, TextAlign = ContentAlignment.MiddleLeft };
    public LoginForm(AppFacade app)
    {
        _app = app; Text = "Đăng nhập • Studio Manager"; StartPosition = FormStartPosition.CenterScreen; ClientSize = new(1040, 640); MinimumSize = new(900, 560); BackColor = Theme.Background; Font = Theme.Font(); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        var hero = new Panel { Dock = DockStyle.Left, Width = 520, BackColor = Theme.Sidebar, Padding = new Padding(58) };
        hero.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Quản lý studio\nnhẹ nhàng hơn mỗi ngày.\n\nLịch chụp • Khách hàng\nTiến độ • Doanh thu", ForeColor = Color.White, Font = Theme.Font(24, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(90, 100, 90, 70), BackColor = Theme.Surface };
        var card = new Panel { Dock = DockStyle.Fill };
        var title = new Label { Text = "Chào mừng trở lại", Dock = DockStyle.Top, Height = 54, Font = Theme.Font(24, FontStyle.Bold), ForeColor = Theme.Text };
        var desc = new Label { Text = "Đăng nhập để tiếp tục quản lý studio", Dock = DockStyle.Top, Height = 42, Font = Theme.Font(10), ForeColor = Theme.Muted };
        var fields = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 175, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _username.Width = _password.Width = 330; _username.Height = _password.Height = 42; _username.Font = _password.Font = Theme.Font(11); fields.Controls.Add(Ui.Field("TÊN ĐĂNG NHẬP", _username, 340)); fields.Controls.Add(Ui.Field("MẬT KHẨU", _password, 340));
        _login.Width = 340; _login.Height = 44; _login.Click += async (_, _) => await LoginAsync();
        card.Controls.Add(new Label { Text = "Tài khoản demo: admin / Admin@123", Dock = DockStyle.Bottom, Height = 30, Font = Theme.Font(9), ForeColor = Theme.Muted }); card.Controls.Add(_error); _error.Dock = DockStyle.Bottom; card.Controls.Add(_login); _login.Dock = DockStyle.Bottom; card.Controls.Add(fields); card.Controls.Add(desc); card.Controls.Add(title); right.Controls.Add(card); Controls.Add(right); Controls.Add(hero); AcceptButton = _login;
    }
    private async Task LoginAsync()
    {
        _error.Text = ""; _login.Enabled = false; _login.Text = "Đang đăng nhập...";
        try
        {
            var result = await _app.Auth.LoginAsync(_username.Text, _password.Text);
            if (!result.Success || result.Data is null) { _error.Text = result.Message; return; }
            _app.Session = result.Data; Hide(); using var main = new MainForm(_app); main.ShowDialog(); _app.Session = null; _password.Clear(); Show();
        }
        catch (Exception ex) { _error.Text = "Không thể kết nối SQL Server. " + ex.Message; }
        finally { _login.Enabled = true; _login.Text = "Đăng nhập"; }
    }
}
