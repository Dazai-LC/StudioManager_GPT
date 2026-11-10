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
        PlaceholderText = "Nhập mật khẩu mới (tối thiểu 8 ký tự)",
        UseSystemPasswordChar = true,
        Font = Theme.Font(10.0f),
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text
    };

    private readonly TextBox _confirmation = new()
    {
        PlaceholderText = "Nhập lại mật khẩu mới để xác nhận",
        UseSystemPasswordChar = true,
        Font = Theme.Font(10.0f),
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text
    };

    private readonly ModernButton _save = Theme.Button("Xác nhận đổi mật khẩu", Theme.Primary, IconKind.Key);

    private readonly CardPanel _errorPanel = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.DangerLight,
        BorderColor = Theme.DangerBorder,
        AccentStripeColor = Theme.Danger,
        AccentStripeLeft = true,
        AccentStripeThickness = 4,
        CornerRadius = 8,
        Padding = new Padding(12, 4, 10, 4),
        Visible = false
    };

    private readonly Label _error = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.Danger,
        Font = Theme.Font(8.6f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Visible = false
    };

    public ChangePasswordForm(AppFacade app, UserSession session)
    {
        _app = app;
        _session = session;
        Text = "Thiết lập mật khẩu bảo mật • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(540, 530);
        MinimumSize = new Size(500, 500);
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Theme.Background;
        Font = Theme.Font();

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(34, 24, 34, 20),
            Margin = new Padding(16),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 12,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 7,
            ColumnCount = 1,
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); // 0: Header Badge + Title
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 1: Intro
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); // 2: Password
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); // 3: Confirm
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // 4: Policy Card
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 5: Error Banner
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // 6: Save Button

        var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        headerPanel.Paint += (_, e) =>
        {
            var g = e.Graphics;
            var box = new RectangleF(0, 6, 40, 40);
            using var path = Theme.RoundedRectangle(box, 10f);
            using var fill = new SolidBrush(Theme.PrimaryLight);
            g.FillPath(fill, path);
            VectorIcons.Draw(g, IconKind.Key, new RectangleF(10, 16, 20, 20), Theme.Primary, 1.9f);
        };
        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(52, 0, 0, 0),
            Text = "Thiết lập mật khẩu bảo mật",
            ForeColor = Theme.Text,
            Font = Theme.Font(15.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        headerPanel.Controls.Add(titleLabel);

        var introLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Xin chào {_session.HoTen}. Vui lòng thiết lập mật khẩu cá nhân mới để bảo vệ tài khoản làm việc của bạn.",
            ForeColor = Theme.Muted,
            Font = Theme.Font(9.0f),
            TextAlign = ContentAlignment.TopLeft
        };

        var policyCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.Border,
            CornerRadius = 8,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 2, 0, 4)
        };
        policyCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Yêu cầu bảo mật: Tối thiểu 8 ký tự, bao gồm chữ cái viết hoa, chữ thường và chữ số.",
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.3f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });

        _errorPanel.Controls.Add(_error);

        _save.Dock = DockStyle.Fill;
        _save.Margin = new Padding(0, 4, 0, 0);
        _save.Click += async (_, _) => await SaveAsync();

        layout.Controls.Add(headerPanel, 0, 0);
        layout.Controls.Add(introLabel, 0, 1);
        layout.Controls.Add(PasswordField("MẬT KHẨU MỚI", _password), 0, 2);
        layout.Controls.Add(PasswordField("XÁC NHẬN MẬT KHẨU MỚI", _confirmation), 0, 3);
        layout.Controls.Add(policyCard, 0, 4);
        layout.Controls.Add(_errorPanel, 0, 5);
        layout.Controls.Add(_save, 0, 6);

        card.Controls.Add(layout);
        Controls.Add(card);
        AcceptButton = _save;
    }

    private void ShowError(string msg)
    {
        _error.Text = msg;
        _error.Visible = true;
        _errorPanel.Visible = true;
    }

    private async Task SaveAsync()
    {
        _error.Visible = false;
        _errorPanel.Visible = false;
        _save.Enabled = false;
        _save.Text = "Đang lưu mật khẩu...";

        try
        {
            var result = await _app.Auth.ChangePasswordAsync(_session.TaiKhoanId, _password.Text, _confirmation.Text);
            if (!result.Success)
            {
                ShowError(result.Message);
                return;
            }

            MessageBox.Show(this, "Mật khẩu đã được cập nhật thành công.",
                "StudioManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError("Không thể đổi mật khẩu: " + ex.Message);
        }
        finally
        {
            _save.Enabled = true;
            _save.Text = "Xác nhận đổi mật khẩu";
        }
    }

    private static Panel PasswordField(string label, TextBox input)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 6), BackColor = Color.Transparent };

        var caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = label,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.0f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var border = new ModernInputContainer
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Theme.Primitives.Alabaster50,
            LeadingIcon = IconKind.Lock
        };

        var revealBtn = new Panel
        {
            Dock = DockStyle.Right,
            Width = 38,
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

        input.GotFocus += (_, _) => border.Invalidate();
        input.LostFocus += (_, _) => border.Invalidate();
        border.Controls.Add(input);
        border.Controls.Add(revealBtn);

        void LayoutInput()
        {
            var th = Math.Max(20, input.PreferredHeight);
            input.SetBounds(40, Math.Max(4, (border.ClientSize.Height - th) / 2),
                Math.Max(40, border.ClientSize.Width - 40 - 42), th);
        }
        border.Resize += (_, _) => LayoutInput();
        input.FontChanged += (_, _) => LayoutInput();
        LayoutInput();

        panel.Controls.Add(border);
        panel.Controls.Add(caption);
        return panel;
    }
}
