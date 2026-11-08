using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

public sealed class LoginForm : Form
{
    private readonly AppFacade _app;
    private bool _isAuthenticating;

    private readonly TextBox _username = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.5f),
        PlaceholderText = "Nhập tên đăng nhập"
    };

    private readonly TextBox _password = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = Theme.Text,
        Font = Theme.Font(10.5f),
        PlaceholderText = "Nhập mật khẩu",
        UseSystemPasswordChar = true
    };

    private readonly ModernButton _login = Theme.Button("Đăng nhập");

    private readonly CardPanel _errorPanel = new()
    {
        Dock = DockStyle.Fill,
        Height = 44,
        Margin = new Padding(0, 2, 0, 2),
        Visible = false,
        BackColor = Theme.DangerLight,
        BorderColor = Theme.DangerBorder,
        CornerRadius = 8,
        Padding = new Padding(10, 4, 10, 4)
    };

    private readonly Label _errorLabel = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.Danger,
        BackColor = Color.Transparent,
        Font = Theme.Font(8.8f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter,
        AutoEllipsis = true
    };

    public LoginForm(AppFacade app)
    {
        _app = app;
        Text = "Đăng nhập • StudioManager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1180, 700);
        MinimumSize = new Size(1020, 640);
        BackColor = Theme.Background;
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20),
            BackColor = Theme.Background
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(BuildHero(), 0, 0);
        shell.Controls.Add(BuildLoginCard(), 1, 0);
        Controls.Add(shell);
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

    private Control BuildHero()
    {
        var hero = new LoginHeroPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(48, 44, 48, 40),
            Margin = Padding.Empty
        };

        var brand = new Label
        {
            Dock = DockStyle.Top,
            Height = 72,
            Text = "◈  STUDIOMANAGER\n     PROFESSIONAL STUDIO SUITE",
            ForeColor = Color.White,
            Font = Theme.Font(13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        var slogan = new Label
        {
            Dock = DockStyle.Top,
            Height = 110,
            Text = "Quản lý studio\ntinh gọn & chuyên nghiệp",
            ForeColor = Color.White,
            Font = Theme.Font(24, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft,
            BackColor = Color.Transparent
        };

        var intro = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            Text = "Từ lịch hẹn, dịch vụ, tài chính đến khách hàng — tất cả\ntập trung trong một không gian làm việc nhất quán.",
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = Theme.Font(10),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        var features = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 260,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 16, 0, 0)
        };

        features.Controls.Add(CreateFeatureItem("◷", "Lịch chụp & Phòng máy", "Theo dõi lịch hẹn, phòng chụp và phân công nhân sự."));
        features.Controls.Add(CreateFeatureItem("♙", "Hồ sơ khách hàng", "Lưu trữ lịch sử chụp, yêu cầu và liên hệ chi tiết."));
        features.Controls.Add(CreateFeatureItem("$", "Tài chính & Thu chi", "Minh bạch đặt cọc, thanh toán bổ sung và công nợ."));
        features.Controls.Add(CreateFeatureItem("◫", "Báo cáo kinh doanh", "Phân tích doanh thu và công suất dịch vụ trực quan."));

        var quote = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Text = "Dành riêng cho các Studio Nhiếp ảnh Hiện đại",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = Theme.Font(9, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        hero.Controls.Add(quote);
        hero.Controls.Add(features);
        hero.Controls.Add(intro);
        hero.Controls.Add(slogan);
        hero.Controls.Add(brand);

        return hero;
    }

    private Control BuildLoginCard()
    {
        var surface = new LoginCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(54, 40, 54, 30),
            Margin = Padding.Empty
        };

        var center = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6));
        center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 88));
        center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6));

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };

        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // 0: Logo
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // 1: Title
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 2: Subtitle
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 78)); // 3: Username
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 78)); // 4: Password
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // 5: Options
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // 6: Login button
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 7: Error panel container
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 8: Footer

        var logo = new Label
        {
            Dock = DockStyle.Fill,
            Text = "◈",
            Font = Theme.Font(32, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Chào mừng trở lại",
            Font = Theme.Font(21, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var desc = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đăng nhập bằng tài khoản được cấp để tiếp tục",
            Font = Theme.Font(9.8f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.TopCenter,
            BackColor = Color.Transparent
        };

        var options = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Transparent };
        var remember = new CheckBox
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            Text = "Ghi nhớ đăng nhập",
            Checked = true,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(9.2f)
        };

        var forgot = new LinkLabel
        {
            Dock = DockStyle.Right,
            Width = 140,
            Text = "Quên mật khẩu?",
            TextAlign = ContentAlignment.MiddleRight,
            LinkColor = Theme.Primary,
            ActiveLinkColor = Theme.PrimaryHover,
            Cursor = Cursors.Hand,
            Font = Theme.Font(9.2f)
        };
        forgot.LinkClicked += (_, _) => MessageBox.Show(this,
            "Vui lòng liên hệ Quản trị viên để đặt lại mật khẩu.\n\nQuản trị viên có thể thao tác tại: Quản trị hệ thống → Tài khoản → Đặt lại mật khẩu.",
            "Khôi phục mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);

        options.Controls.Add(forgot);
        options.Controls.Add(remember);

        ConfigureLoginButton();

        _errorPanel.Controls.Add(_errorLabel);

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = "StudioManager  •  Phiên bản 1.0 (Build .NET 8)\nBản quyền thuộc về Studio Operations",
            TextAlign = ContentAlignment.BottomCenter,
            ForeColor = Theme.TextLight,
            Font = Theme.Font(8.0f),
            BackColor = Color.Transparent
        };

        form.Controls.Add(logo, 0, 0);
        form.Controls.Add(title, 0, 1);
        form.Controls.Add(desc, 0, 2);
        form.Controls.Add(CreateModernInputField("TÀI KHOẢN", _username, "👤"), 0, 3);
        form.Controls.Add(CreateModernInputField("MẬT KHẨU", _password, "🔒", allowReveal: true), 0, 4);
        form.Controls.Add(options, 0, 5);
        form.Controls.Add(_login, 0, 6);
        form.Controls.Add(_errorPanel, 0, 7);
        form.Controls.Add(footer, 0, 8);

        center.Controls.Add(form, 1, 0);
        surface.Controls.Add(center);
        return surface;
    }

    private void ConfigureLoginButton()
    {
        _login.Text = "Đăng nhập hệ thống  →";
        _login.Dock = DockStyle.Fill;
        _login.Height = 44;
        _login.BackColor = Theme.Primary;
        _login.ForeColor = Color.White;
        _login.Font = Theme.Font(10.2f, FontStyle.Bold);
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

    private static Panel CreateFeatureItem(string icon, string title, string subtitle)
    {
        var row = new Panel
        {
            Width = 460,
            Height = 56,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };

        var badge = new Label
        {
            Dock = DockStyle.Left,
            Width = 42,
            Height = 42,
            Text = icon,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(147, 197, 253),
            Font = Theme.Font(14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        badge.Resize += (_, _) => Theme.Round(badge, 10);

        var copy = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 4, 0, 0),
            Text = $"{title}\n{subtitle}",
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = Theme.Font(9.0f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        row.Controls.Add(copy);
        row.Controls.Add(badge);
        return row;
    }

    private static Panel CreateModernInputField(string label, TextBox input, string icon, bool allowReveal = false)
    {
        var p = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Transparent };

        var caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = label,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        };

        var border = new ModernInputContainer
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Padding = new Padding(12, 11, 10, 11),
            BackColor = Color.White
        };

        var iconLabel = new Label
        {
            Dock = DockStyle.Left,
            Width = 26,
            Text = icon,
            ForeColor = Theme.Muted,
            Font = Theme.Font(11),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        input.Dock = DockStyle.Fill;
        border.Controls.Add(input);
        border.Controls.Add(iconLabel);

        if (allowReveal)
        {
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
            border.Controls.Add(reveal);
        }

        p.Controls.Add(border);
        p.Controls.Add(caption);
        return p;
    }

    private async Task LoginAsync()
    {
        if (_isAuthenticating) return;

        var username = _username.Text.Trim();
        var password = _password.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("⚠  Vui lòng nhập tên đăng nhập.");
            _username.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("⚠  Vui lòng nhập mật khẩu.");
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
                ShowError("⚠  " + (string.IsNullOrWhiteSpace(result.Message) ? "Đăng nhập không thành công." : result.Message));
                _password.SelectAll();
                _password.Focus();
                return;
            }

            _app.Session = result.Data;

            if (result.Data.PhaiDoiMatKhau)
            {
                using var change = new ChangePasswordForm(_app, result.Data);
                if (change.ShowDialog(this) != DialogResult.OK)
                {
                    _app.Session = null;
                    _password.Clear();
                    ShowError("⚠  Bạn phải đổi mật khẩu trước khi sử dụng hệ thống.");
                    return;
                }
                _app.Session = result.Data with { PhaiDoiMatKhau = false };
            }

            Hide();
            using var initialization = new ApplicationInitializationForm(_app);
            if (initialization.ShowDialog() != DialogResult.OK || initialization.DashboardData is null)
            {
                _app.Session = null;
                _password.Clear();
                Show();
                ShowError("⚠  Khởi tạo không gian làm việc thất bại hoặc đã bị hủy.");
                return;
            }

            using var main = new MainForm(_app, initialization.DashboardData);
            main.ShowDialog();

            _app.Session = null;
            _password.Clear();
            ClearError();
            Show();
            _username.Focus();
        }
        catch (Exception ex)
        {
            var isSql = ex is Microsoft.Data.SqlClient.SqlException
                        || ex.Message.Contains("SQL", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("server", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);

            var errorMsg = isSql
                ? "⚠  Không thể kết nối cơ sở dữ liệu SQL Server. Vui lòng kiểm tra dịch vụ và thử lại."
                : "⚠  Đã xảy ra lỗi trong quá trình đăng nhập: " + ex.Message;

            ShowError(errorMsg);
        }
        finally
        {
            _isAuthenticating = false;
            _login.Enabled = true;
            _login.Text = "Đăng nhập hệ thống  →";
            _login.Refresh();
        }
    }
}

internal sealed class LoginHeroPanel : Panel
{
    public LoginHeroPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var gradient = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(15, 23, 42),
            Color.FromArgb(30, 41, 59),
            45f);
        g.FillRectangle(gradient, ClientRectangle);

        using var glow1 = new SolidBrush(Color.FromArgb(18, 37, 99, 235));
        g.FillEllipse(glow1, Width - 140, -60, 280, 280);

        using var glow2 = new SolidBrush(Color.FromArgb(12, 139, 92, 246));
        g.FillEllipse(glow2, -80, Height - 220, 320, 320);

        using var borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f);
        using var path = Theme.RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 16);
        g.DrawPath(borderPen, path);
    }
}

internal sealed class LoginCardPanel : Panel
{
    public LoginCardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Theme.Surface;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var fillBrush = new SolidBrush(Theme.Surface);
        using var path = Theme.RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 16);
        g.FillPath(fillBrush, path);

        using var borderPen = new Pen(Theme.Border, 1f);
        g.DrawPath(borderPen, path);
    }
}

internal sealed class ModernInputContainer : Panel
{
    public ModernInputContainer()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var hasFocus = ContainsFocus;
        var bounds = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using var path = Theme.RoundedRectangle(bounds, 8);

        using var fillBrush = new SolidBrush(Color.White);
        g.FillPath(fillBrush, path);

        using var borderPen = new Pen(hasFocus ? Theme.Primary : Theme.Border, hasFocus ? 1.5f : 1f);
        g.DrawPath(borderPen, path);
    }
}
