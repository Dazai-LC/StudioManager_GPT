using System.Drawing.Drawing2D;
using StudioManager.Application.Services;

namespace StudioManager.WinForms.Forms;

public sealed class LoginForm : Form
{
    private readonly AppFacade _app;
    private readonly TextBox _username = LoginTextBox("Nhập tên đăng nhập hoặc email");
    private readonly TextBox _password = LoginTextBox("Nhập mật khẩu", true);
    private readonly Button _login = new();
    private readonly Label _error = new() { AutoSize = false, Height = 28, ForeColor = Theme.Danger, TextAlign = ContentAlignment.MiddleLeft };

    public LoginForm(AppFacade app)
    {
        _app = app; Text = "Đăng nhập • StudioManager"; StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1360, 780); MinimumSize = new Size(1060, 680); BackColor = Theme.Background;
        Font = Theme.Font(); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(24), BackColor = Theme.Background };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
        shell.Controls.Add(BuildHero(), 0, 0); shell.Controls.Add(BuildLoginArea(), 1, 0); Controls.Add(shell); AcceptButton = _login;
    }

    private Control BuildHero()
    {
        var hero = new LoginHeroPanel { Dock = DockStyle.Fill, Padding = new Padding(56, 46, 54, 44), Margin = Padding.Empty };
        var brand = new Label { Dock = DockStyle.Top, Height = 76, Text = "▣  StudioManager\n     Manage · Create · Grow", ForeColor = Color.White, Font = Theme.Font(16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        var slogan = new Label { Dock = DockStyle.Top, Height = 142, Text = "Quản lý studio\ndễ dàng hơn, hiệu quả hơn", ForeColor = Color.White, Font = Theme.Font(25, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
        var intro = new Label { Dock = DockStyle.Top, Height = 70, Text = "Từ lịch chụp, khách hàng đến tài chính,\ntất cả đều nằm trong một hệ thống.", ForeColor = Color.FromArgb(211, 224, 241), Font = Theme.Font(11), TextAlign = ContentAlignment.MiddleLeft };
        var features = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 258, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };
        features.Controls.Add(Feature("▣", "Quản lý lịch chụp / quay", "Không bỏ lỡ bất kỳ lịch hẹn nào"));
        features.Controls.Add(Feature("♟", "Theo dõi khách hàng", "Chăm sóc khách hàng tốt hơn"));
        features.Controls.Add(Feature("$", "Kiểm soát tài chính", "Minh bạch – Chính xác – Hiệu quả"));
        features.Controls.Add(Feature("▥", "Báo cáo trực quan", "Nắm bắt tình hình kinh doanh nhanh chóng"));
        var quote = new Label { Dock = DockStyle.Bottom, Height = 75, Text = "Better Studio\nBigger Dreams", ForeColor = Color.FromArgb(205, 220, 242), Font = new Font("Segoe Script", 16, FontStyle.Italic), TextAlign = ContentAlignment.MiddleLeft };
        hero.Controls.Add(quote); hero.Controls.Add(features); hero.Controls.Add(intro); hero.Controls.Add(slogan); hero.Controls.Add(brand); return hero;
    }

    private Control BuildLoginArea()
    {
        var area = new LoginSurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(70, 42, 70, 36), Margin = Padding.Empty };
        var help = new Label { Dock = DockStyle.Top, Height = 34, Text = "Chưa có tài khoản?   Liên hệ quản trị viên  ↗", TextAlign = ContentAlignment.TopRight, ForeColor = Theme.Primary, Font = Theme.Font(9) };
        var content = new Panel { Width = 430, Height = 585, BackColor = Color.Transparent, Anchor = AnchorStyles.None };
        area.Controls.Add(content); area.Controls.Add(help);
        area.Resize += (_, _) => content.Location = new Point(Math.Max(20, (area.ClientSize.Width - content.Width) / 2), Math.Max(70, (area.ClientSize.Height - content.Height) / 2 + 12));
        var logo = new Label { Dock = DockStyle.Top, Height = 82, Text = "▣", Font = Theme.Font(35, FontStyle.Bold), ForeColor = Theme.Primary, TextAlign = ContentAlignment.MiddleCenter };
        var title = new Label { Dock = DockStyle.Top, Height = 48, Text = "StudioManager", Font = Theme.Font(24, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleCenter };
        var desc = new Label { Dock = DockStyle.Top, Height = 46, Text = "Đăng nhập để tiếp tục", Font = Theme.Font(11), ForeColor = Theme.Muted, TextAlign = ContentAlignment.TopCenter };
        var fields = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 184, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        fields.Controls.Add(InputField("Tài khoản", _username)); fields.Controls.Add(InputField("Mật khẩu", _password));
        var options = new Panel { Dock = DockStyle.Top, Height = 42 };
        var remember = new CheckBox { Dock = DockStyle.Left, AutoSize = true, Text = "Ghi nhớ đăng nhập", Checked = true, ForeColor = Theme.Text, Font = Theme.Font(9.3f) };
        var forgot = new LinkLabel { Dock = DockStyle.Right, Width = 125, Text = "Quên mật khẩu?", TextAlign = ContentAlignment.MiddleRight, LinkColor = Theme.Primary, ActiveLinkColor = Theme.Primary };
        forgot.LinkClicked += (_, _) => MessageBox.Show(this, "Vui lòng liên hệ quản trị viên để đặt lại mật khẩu.", "Khôi phục mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        options.Controls.Add(forgot); options.Controls.Add(remember); ConfigureLoginButton();
        var divider = new Label { Dock = DockStyle.Top, Height = 45, Text = "────────────    Hoặc    ────────────", TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Border };
        var windows = new Button { Dock = DockStyle.Top, Height = 44, Text = "▦  Đăng nhập bằng Windows", FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Text, Font = Theme.Font(9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        windows.FlatAppearance.BorderColor = Color.FromArgb(211, 221, 236); windows.FlatAppearance.BorderSize = 1;
        windows.Click += (_, _) => MessageBox.Show(this, "Tính năng đăng nhập Windows sẽ được cấu hình bởi quản trị viên.", "StudioManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        var footer = new Label { Dock = DockStyle.Bottom, Height = 42, Text = "StudioManager   v1.0.0\n© 2026 StudioManager. All rights reserved.", TextAlign = ContentAlignment.BottomCenter, ForeColor = Theme.Muted, Font = Theme.Font(8) };
        content.Controls.Add(footer); content.Controls.Add(windows); content.Controls.Add(divider); content.Controls.Add(_error); _error.Dock = DockStyle.Top;
        content.Controls.Add(_login); _login.Dock = DockStyle.Top; content.Controls.Add(options); content.Controls.Add(fields); content.Controls.Add(desc); content.Controls.Add(title); content.Controls.Add(logo); return area;
    }

    private void ConfigureLoginButton()
    {
        _login.Height = 46; _login.Text = "⇥  Đăng nhập"; _login.FlatStyle = FlatStyle.Flat; _login.BackColor = Theme.Primary; _login.ForeColor = Color.White;
        _login.Font = Theme.Font(10, FontStyle.Bold); _login.Cursor = Cursors.Hand; _login.Margin = new Padding(0, 4, 0, 0); _login.FlatAppearance.BorderSize = 0;
        _login.Resize += (_, _) => Theme.Round(_login, 8); _login.Click += async (_, _) => await LoginAsync();
    }

    private static Panel Feature(string icon, string title, string subtitle)
    {
        var row = new Panel { Width = 480, Height = 58, BackColor = Color.Transparent, Margin = new Padding(0, 3, 0, 3) };
        var badge = new Label { Dock = DockStyle.Left, Width = 48, Text = icon, BackColor = Color.FromArgb(60, 104, 158), ForeColor = Color.FromArgb(126, 168, 255), Font = Theme.Font(16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        badge.Resize += (_, _) => Theme.Round(badge, 10);
        row.Controls.Add(new Label { Dock = DockStyle.Fill, Padding = new Padding(16, 4, 0, 0), Text = title + "\n" + subtitle, ForeColor = Color.White, Font = Theme.Font(9.4f), TextAlign = ContentAlignment.MiddleLeft }); row.Controls.Add(badge); return row;
    }

    private static Panel InputField(string label, TextBox input)
    {
        var p = new Panel { Width = 430, Height = 88, Margin = Padding.Empty };
        var caption = new Label { Dock = DockStyle.Top, Height = 30, Text = label, ForeColor = Theme.Text, Font = Theme.Font(9.5f, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
        var border = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(14, 11, 12, 7), BackColor = Color.White };
        border.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(207, 219, 236)); e.Graphics.DrawRectangle(pen, 0, 0, border.Width - 1, border.Height - 1); };
        border.Resize += (_, _) => Theme.Round(border, 8); border.Controls.Add(input); p.Controls.Add(border); p.Controls.Add(caption); return p;
    }

    private static TextBox LoginTextBox(string placeholder, bool password = false) => new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, PlaceholderText = placeholder, UseSystemPasswordChar = password, Font = Theme.Font(10.5f), ForeColor = Theme.Text };

    private async Task LoginAsync()
    {
        _error.Text = ""; _login.Enabled = false; _login.Text = "Đang đăng nhập...";
        try { var result = await _app.Auth.LoginAsync(_username.Text.Trim(), _password.Text); if (!result.Success || result.Data is null) { _error.Text = result.Message; return; } _app.Session = result.Data; Hide(); using var main = new MainForm(_app); main.ShowDialog(); _app.Session = null; _password.Clear(); Show(); }
        catch (Exception ex) { _error.Text = "Không thể kết nối SQL Server. " + ex.Message; }
        finally { _login.Enabled = true; _login.Text = "⇥  Đăng nhập"; }
    }
}

internal sealed class LoginHeroPanel : Panel
{
    public LoginHeroPanel() { DoubleBuffered = true; }
    protected override void OnPaintBackground(PaintEventArgs e) { using var gradient = new LinearGradientBrush(ClientRectangle, Color.FromArgb(21, 50, 82), Color.FromArgb(10, 29, 50), 35f); e.Graphics.FillRectangle(gradient, ClientRectangle); using var glow = new SolidBrush(Color.FromArgb(20, 95, 151, 215)); e.Graphics.FillEllipse(glow, Width / 2, -80, Width, Height); }
}

internal sealed class LoginSurfacePanel : Panel
{
    public LoginSurfacePanel() { DoubleBuffered = true; BackColor = Color.White; }
    protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(Color.White); using var b = new SolidBrush(Color.FromArgb(25, 79, 124, 246)); e.Graphics.FillEllipse(b, Width - 100, 70, 190, 190); e.Graphics.FillEllipse(b, -100, Height - 130, 220, 220); }
}
