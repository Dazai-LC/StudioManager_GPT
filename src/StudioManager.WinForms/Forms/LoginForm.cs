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
        ClientSize = new Size(1160, 690);
        MinimumSize = new Size(1040, 650);
        BackColor = Theme.Background;
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
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

    private static Control BuildTopArchitecturalRibbon()
    {
        var ribbon = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Sidebar,
            Padding = new Padding(24, 0, 24, 0)
        };

        ribbon.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Bottom terracotta hairline
            using var linePen = new Pen(Color.FromArgb(90, Theme.Primary), 1f);
            g.DrawLine(linePen, 0, ribbon.Height - 1, ribbon.Width, ribbon.Height - 1);

            // Brand Aperture Icon
            var iconRect = new RectangleF(24, (ribbon.Height - 22) / 2f, 22, 22);
            VectorIcons.Draw(g, IconKind.Aperture, iconRect, Theme.Primary, 1.9f);

            using var titleFont = Theme.Font(9.5f, FontStyle.Bold);
            using var subFont = Theme.MonoFont(8.2f, FontStyle.Regular);
            using var lightBrush = new SolidBrush(Theme.Primitives.Alabaster100);
            using var mutedBrush = new SolidBrush(Theme.SidebarMuted);

            g.DrawString("STUDIOMANAGER", titleFont, lightBrush, 54, (ribbon.Height - 18) / 2f);
            g.DrawString("ATELIER LUMIÈRE  //  DESKTOP COMMAND WORKBENCH", subFont, mutedBrush, 182, (ribbon.Height - 16) / 2f);

            // Right telemetry badge
            var badgeText = "SQL SERVER ENGINE  •  LOCAL WORKSPACE";
            var sz = g.MeasureString(badgeText, subFont);
            var bx = ribbon.Width - sz.Width - 28;
            using var dotBrush = new SolidBrush(Theme.Primitives.Sage600);
            g.FillEllipse(dotBrush, bx - 14, (ribbon.Height - 7) / 2f, 7, 7);
            g.DrawString(badgeText, subFont, mutedBrush, bx, (ribbon.Height - sz.Height) / 2f);
        };

        return ribbon;
    }

    private Control BuildMainSplitCanvas()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(24, 22, 24, 22),
            BackColor = Theme.Background
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        split.Controls.Add(BuildOpticalShowcasePanel(), 0, 0);
        split.Controls.Add(BuildCredentialVaultPanel(), 1, 0);
        return split;
    }

    private static Control BuildOpticalShowcasePanel()
    {
        var hero = new AtelierShowcasePanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(36, 34, 36, 30)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // Tagline pill
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 106)); // Editorial Headline
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));  // Editorial Lead
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 3 Architectural Pillar Cards
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // Footer signature

        var overline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "PRO PHOTOGRAPHY OPERATIONS SYSTEM",
            ForeColor = Theme.Primitives.Terracotta200,
            Font = Theme.MonoFont(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var headline = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Nghệ thuật điều phối\nkhông gian nhiếp ảnh.",
            ForeColor = Theme.Primitives.Alabaster50,
            Font = Theme.Font(21.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lead = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Kiểm soát trọn vẹn quy trình từ đặt lịch phòng máy, điều phối nhiếp ảnh gia, hậu kỳ hình ảnh đến quyết toán tài chính.",
            ForeColor = Theme.Primitives.Stone300,
            Font = Theme.Font(9.3f),
            TextAlign = ContentAlignment.TopLeft
        };

        var pillars = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };
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
            TextAlign = ContentAlignment.BottomLeft
        };

        layout.Controls.Add(overline, 0, 0);
        layout.Controls.Add(headline, 0, 1);
        layout.Controls.Add(lead, 0, 2);
        layout.Controls.Add(pillars, 0, 3);
        layout.Controls.Add(signature, 0, 4);

        hero.Controls.Add(layout);
        return hero;
    }

    private static Control CreatePillarTile(IconKind icon, string title, string subtitle)
    {
        var tile = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 5),
            BackColor = Color.Transparent
        };

        tile.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new RectangleF(0.5f, 0.5f, tile.Width - 1, tile.Height - 1);
            using var path = Theme.RoundedRectangle(bounds, 10f);
            using var fill = new SolidBrush(Color.FromArgb(150, Theme.Primitives.Espresso800));
            using var border = new Pen(Color.FromArgb(45, Theme.Primitives.Stone300), 1f);
            g.FillPath(fill, path);
            g.DrawPath(border, path);

            var iconBox = new RectangleF(14, (tile.Height - 36) / 2f, 36, 36);
            using var iconPath = Theme.RoundedRectangle(iconBox, 8f);
            using var iconFill = new SolidBrush(Color.FromArgb(45, Theme.Primary));
            g.FillPath(iconFill, iconPath);
            VectorIcons.Draw(g, icon, new RectangleF(iconBox.Left + 9, iconBox.Top + 9, 18, 18), Theme.Primitives.Terracotta200, 1.8f);

            using var tFont = Theme.Font(9.4f, FontStyle.Bold);
            using var sFont = Theme.Font(8.3f);
            using var tBrush = new SolidBrush(Theme.Primitives.Alabaster50);
            using var sBrush = new SolidBrush(Theme.Primitives.Stone300);

            g.DrawString(title, tFont, tBrush, 62, tile.Height / 2f - 17);
            g.DrawString(subtitle, sFont, sBrush, new RectangleF(62, tile.Height / 2f + 2, tile.Width - 74, 24));
        };

        return tile;
    }

    private Control BuildCredentialVaultPanel()
    {
        var vault = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12, 0, 0, 0),
            Padding = new Padding(52, 34, 52, 26),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 12,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };

        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); // 0: Header badge
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // 1: Title
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // 2: Subtitle
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // 3: Username
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // 4: Password
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 5: Options row
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 6: Primary CTA Button
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // 7: Inline Error Banner
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 8: Footer

        var emblemCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        emblemCanvas.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new RectangleF(0, 4, 42, 42);
            using var path = Theme.RoundedRectangle(box, 10f);
            using var fill = new SolidBrush(Theme.PrimaryLight);
            using var pen = new Pen(Theme.PrimaryLightBorder, 1f);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            VectorIcons.Draw(g, IconKind.Shield, new RectangleF(10, 14, 22, 22), Theme.Primary, 1.9f);

            using var badgeFont = Theme.MonoFont(8.0f, FontStyle.Bold);
            using var badgeBrush = new SolidBrush(Theme.Primary);
            g.DrawString("XÁC THỰC PHIÊN LÀM VIỆC", badgeFont, badgeBrush, 54, 18);
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Đăng nhập bàn điều khiển",
            Font = Theme.Font(19.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var desc = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Sử dụng tài khoản Quản trị viên hoặc Nhân viên được cấp phép để truy cập.",
            Font = Theme.Font(9.2f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.TopLeft
        };

        var options = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Transparent };
        var remember = new CheckBox
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            Text = "Duy trì trạng thái làm việc trên thiết bị này",
            Checked = true,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.9f)
        };

        var forgot = new LinkLabel
        {
            Dock = DockStyle.Right,
            Width = 148,
            Text = "Cần cấp lại mật khẩu?",
            TextAlign = ContentAlignment.MiddleRight,
            LinkColor = Theme.Primary,
            ActiveLinkColor = Theme.PrimaryHover,
            Cursor = Cursors.Hand,
            Font = Theme.Font(8.9f, FontStyle.Bold)
        };
        forgot.LinkClicked += (_, _) => MessageBox.Show(this,
            "Để bảo đảm an toàn dữ liệu, việc đặt lại mật khẩu do Quản trị viên thực hiện.\n\nHướng dẫn cho Quản trị viên:\nTrung tâm Hệ thống → Tài khoản & Phân quyền → Đặt lại mật khẩu.",
            "Chính sách bảo mật tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);

        options.Controls.Add(forgot);
        options.Controls.Add(remember);

        ConfigureLoginButton();
        _errorPanel.Controls.Add(_errorLabel);

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Bàn phím tắt: Nhấn Enter để xác thực nhanh  •  Bảo mật PBKDF2-SHA256",
            TextAlign = ContentAlignment.BottomLeft,
            ForeColor = Theme.TextLight,
            Font = Theme.Font(8.0f),
            BackColor = Color.Transparent
        };

        form.Controls.Add(emblemCanvas, 0, 0);
        form.Controls.Add(title, 0, 1);
        form.Controls.Add(desc, 0, 2);
        form.Controls.Add(CreateVectorInputField("TÊN ĐĂNG NHẬP", _username, IconKind.Users), 0, 3);
        form.Controls.Add(CreateVectorInputField("MẬT KHẨU BẢO MẬT", _password, IconKind.Lock, allowReveal: true), 0, 4);
        form.Controls.Add(options, 0, 5);
        form.Controls.Add(_login, 0, 6);
        form.Controls.Add(_errorPanel, 0, 7);
        form.Controls.Add(footer, 0, 8);

        vault.Controls.Add(form);
        return vault;
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

    private static Panel CreateVectorInputField(string label, TextBox input, IconKind icon, bool allowReveal = false)
    {
        var p = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Transparent };

        var caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = label,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.0f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        };

        var border = new ModernInputContainer
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Padding = new Padding(40, 12, allowReveal ? 44 : 12, 10),
            BackColor = Theme.Primitives.Alabaster50,
            LeadingIcon = icon
        };

        input.BackColor = Theme.Primitives.Alabaster50;
        input.Dock = DockStyle.Fill;
        input.GotFocus += (_, _) => border.Invalidate();
        input.LostFocus += (_, _) => border.Invalidate();
        border.Click += (_, _) => input.Focus();
        border.Controls.Add(input);

        if (allowReveal)
        {
            var revealBtn = new Panel
            {
                Dock = DockStyle.Right,
                Width = 36,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            revealBtn.Paint += (_, e) =>
            {
                var k = input.UseSystemPasswordChar ? IconKind.Eye : IconKind.EyeOff;
                VectorIcons.Draw(e.Graphics, k, new RectangleF(9, (revealBtn.Height - 16) / 2f, 16, 16), Theme.Primary, 1.75f);
            };
            revealBtn.Click += (_, _) =>
            {
                input.UseSystemPasswordChar = !input.UseSystemPasswordChar;
                revealBtn.Invalidate();
                input.Focus();
            };
            border.Controls.Add(revealBtn);
        }

        p.Controls.Add(border);
        p.Controls.Add(caption);
        return p;
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

        var stage = "Xác thực đăng nhập";

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

            _app.Session = result.Data;

            if (result.Data.PhaiDoiMatKhau)
            {
                stage = "Đổi mật khẩu bảo mật lần đầu";
                using var change = new ChangePasswordForm(_app, result.Data);
                if (change.ShowDialog(this) != DialogResult.OK)
                {
                    _app.Session = null;
                    _password.Clear();
                    ShowError("Bạn cần hoàn tất đổi mật khẩu bảo mật trước khi truy cập hệ thống.");
                    _password.Focus();
                    return;
                }
                _app.Session = result.Data with { PhaiDoiMatKhau = false };
            }

            Hide();

            stage = "Khởi tạo không gian làm việc";
            using var initialization = new ApplicationInitializationForm(_app);
            if (initialization.ShowDialog() != DialogResult.OK || initialization.DashboardData is null)
            {
                _app.Session = null;
                _password.Clear();
                RestoreLoginWindow();
                ShowError(!string.IsNullOrWhiteSpace(initialization.LastErrorMessage)
                    ? initialization.LastErrorMessage
                    : "Khởi tạo không gian làm việc không thành công hoặc đã bị hủy.");
                _password.Focus();
                return;
            }

            stage = "Khởi tạo giao diện chính (MainForm)";
            using var main = new MainForm(_app, initialization.DashboardData);

            stage = "Hiển thị bàn làm việc chính";
            main.ShowDialog();

            _app.Session = null;
            _password.Clear();
            ClearError();
            RestoreLoginWindow();
            _username.Focus();
        }
        catch (Exception ex)
        {
            _app.Session = null;
            RestoreLoginWindow();
            Ui.LogError($"LoginForm.LoginAsync [{stage}]", ex);

            var errorMsg = Ui.IsDatabaseException(ex)
                ? "Không thể kết nối cơ sở dữ liệu SQL Server. Vui lòng kiểm tra dịch vụ SQL và thử lại."
                : Ui.FormatRuntimeError(stage, ex);

            ShowError(errorMsg);
            _password.Focus();
        }
        finally
        {
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

internal sealed class AtelierShowcasePanel : Panel
{
    public AtelierShowcasePanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, 12f);

        using (var bg = new LinearGradientBrush(ClientRectangle,
            Theme.Primitives.Espresso900,
            Theme.Primitives.Espresso800,
            55f))
        {
            g.FillPath(bg, path);
        }

        var state = g.Save();
        g.SetClip(path);

        // Warm Terracotta Optical Glow
        using (var glow = new SolidBrush(Color.FromArgb(26, Theme.Primary)))
        {
            g.FillEllipse(glow, Width - 220, -80, 320, 320);
        }

        // Precision Optical Lens Rings (Architectural background motif)
        var cx = Width - 60f;
        var cy = 90f;
        using (var ringPen = new Pen(Color.FromArgb(22, Theme.Primitives.Alabaster200), 1f))
        using (var dashedPen = new Pen(Color.FromArgb(30, Theme.Primary), 1f) { DashStyle = DashStyle.Dot })
        {
            g.DrawEllipse(ringPen, cx - 70, cy - 70, 140, 140);
            g.DrawEllipse(dashedPen, cx - 115, cy - 115, 230, 230);
            g.DrawEllipse(ringPen, cx - 165, cy - 165, 330, 330);
            g.DrawLine(ringPen, cx - 190, cy, cx + 190, cy);
            g.DrawLine(ringPen, cx, cy - 190, cx, cy + 190);
        }

        g.Restore(state);

        using var borderPen = new Pen(Color.FromArgb(60, Theme.Primitives.Stone400), 1f);
        g.DrawPath(borderPen, path);
    }
}

internal sealed class ModernInputContainer : Panel
{
    public IconKind LeadingIcon { get; set; } = IconKind.None;

    public ModernInputContainer()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var hasFocus = ContainsFocus;
        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, 8f);

        using (var fillBrush = new SolidBrush(BackColor))
        {
            g.FillPath(fillBrush, path);
        }

        using (var borderPen = new Pen(hasFocus ? Theme.Primary : Theme.BorderStrong, hasFocus ? 1.6f : 1f))
        {
            g.DrawPath(borderPen, path);
        }

        if (LeadingIcon != IconKind.None)
        {
            var iconColor = hasFocus ? Theme.Primary : Theme.Muted;
            VectorIcons.Draw(g, LeadingIcon, new RectangleF(13, (Height - 16) / 2f, 16, 16), iconColor, 1.75f);
        }
    }
}
