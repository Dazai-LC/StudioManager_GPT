using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StudioManager.WinForms.Controls;

public enum AtelierNotificationKind
{
    Info,
    Success,
    Warning,
    Error,
    Confirmation
}

public sealed record AtelierDialogRequest(
    AtelierNotificationKind Kind,
    string Title,
    string Message,
    string PrimaryButtonText,
    string? SecondaryButtonText,
    bool IsDestructive,
    string? TechnicalDetails);

/// <summary>
/// Studio-branded modal dialog replacing native MessageBox.Show across StudioManager Desktop.
/// Supports Vietnamese copy, multi-line wrapping, safe destructive defaults, and optional
/// collapsible technical details without exposing internal jargon by default.
/// </summary>
public sealed class AtelierDialog : Form
{
    private readonly AtelierNotificationKind _kind;
    private readonly bool _isDestructive;
    private Panel? _detailsContainer;
    private TextBox? _detailsBox;
    private ModernButton? _toggleDetailsBtn;
    private bool _detailsExpanded;

    public AtelierDialog(
        AtelierNotificationKind kind,
        string title,
        string message,
        string primaryButtonText = "Đã hiểu",
        string? secondaryButtonText = null,
        bool isDestructive = false,
        string? technicalDetails = null)
    {
        _kind = kind;
        _isDestructive = isDestructive;

        Text = string.IsNullOrWhiteSpace(title) ? "Atelier Lumière — Thông báo" : title.Trim();
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = Theme.Font(9.5f);
        DoubleBuffered = true;
        KeyPreview = true;

        var (accent, lightBg, borderCol, icon) = ResolveVisualTokens(kind, isDestructive);

        var dialogWidth = !string.IsNullOrWhiteSpace(technicalDetails) ? 540 : 480;
        Width = dialogWidth;

        // Top semantic stripe
        var stripe = new Panel
        {
            Dock = DockStyle.Top,
            Height = 5,
            BackColor = accent
        };

        // Main content area
        var bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(22, 20, 22, 16)
        };

        var iconSize = 44;
        var iconHost = new Panel
        {
            Location = new Point(22, 20),
            Size = new Size(iconSize, iconSize),
            BackColor = Color.Transparent
        };
        iconHost.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var bgBrush = new SolidBrush(lightBg);
            using var pen = new Pen(borderCol, 1.25f);
            var rect = new RectangleF(1, 1, iconSize - 3, iconSize - 3);
            e.Graphics.FillEllipse(bgBrush, rect);
            e.Graphics.DrawEllipse(pen, rect);
            VectorIcons.Draw(e.Graphics, icon, new RectangleF(11, 11, iconSize - 22, iconSize - 22), accent, 1.9f);
        };

        var textLeft = 22 + iconSize + 16;
        var textWidth = dialogWidth - textLeft - 28;

        var titleLbl = new Label
        {
            Text = string.IsNullOrWhiteSpace(title) ? DefaultTitle(kind) : title.Trim(),
            Location = new Point(textLeft, 18),
            MaximumSize = new Size(textWidth, 0),
            AutoSize = true,
            Font = Theme.Font(11.2f, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        var measuredTitle = TextRenderer.MeasureText(titleLbl.Text, titleLbl.Font, new Size(textWidth, 0), TextFormatFlags.WordBreak);

        var msgText = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        var msgTop = 18 + Math.Max(24, measuredTitle.Height) + 8;
        var msgFont = Theme.Font(9.5f);
        var measuredMsg = TextRenderer.MeasureText(msgText, msgFont, new Size(textWidth, 0), TextFormatFlags.WordBreak);
        var msgHeight = Math.Clamp(measuredMsg.Height + 6, 28, 220);

        Control msgControl;
        if (measuredMsg.Height > 220)
        {
            msgControl = new TextBox
            {
                Text = msgText,
                Location = new Point(textLeft, msgTop),
                Size = new Size(textWidth, 220),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary,
                Font = msgFont,
                ScrollBars = ScrollBars.Vertical,
                TabStop = false
            };
        }
        else
        {
            msgControl = new Label
            {
                Text = msgText,
                Location = new Point(textLeft, msgTop),
                Size = new Size(textWidth, msgHeight),
                Font = msgFont,
                ForeColor = Theme.TextSecondary
            };
        }

        bodyHost.Controls.Add(iconHost);
        bodyHost.Controls.Add(titleLbl);
        bodyHost.Controls.Add(msgControl);

        var contentBottom = Math.Max(20 + iconSize + 6, msgTop + msgHeight + 8);

        if (!string.IsNullOrWhiteSpace(technicalDetails))
        {
            _toggleDetailsBtn = Theme.GhostButton("Xem chi tiết kỹ thuật", IconKind.Info);
            _toggleDetailsBtn.Location = new Point(textLeft - 6, contentBottom);
            _toggleDetailsBtn.Height = 30;
            _toggleDetailsBtn.Width = 210;
            _toggleDetailsBtn.Click += (_, _) => ToggleDetails();
            bodyHost.Controls.Add(_toggleDetailsBtn);

            _detailsContainer = new Panel
            {
                Location = new Point(textLeft, contentBottom + 34),
                Size = new Size(textWidth, 126),
                Visible = false,
                BackColor = Theme.SurfaceWarm,
                Padding = new Padding(8)
            };
            _detailsBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.SurfaceWarm,
                ForeColor = Theme.Muted,
                Font = new Font("Consolas", 8.5f),
                ScrollBars = ScrollBars.Vertical,
                Text = technicalDetails.Trim()
            };
            _detailsContainer.Controls.Add(_detailsBox);
            bodyHost.Controls.Add(_detailsContainer);
            contentBottom += 36;
        }

        // Bottom footer action bar
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = Theme.SurfaceWarm,
            Padding = new Padding(20, 12, 20, 12)
        };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var primaryBtn = Theme.Button(
            primaryButtonText,
            isDestructive ? Theme.Danger : accent,
            isDestructive ? IconKind.Trash : icon);
        primaryBtn.Height = 36;
        primaryBtn.Width = Math.Max(118, primaryBtn.RequiredWidth + 8);
        primaryBtn.DialogResult = string.IsNullOrWhiteSpace(secondaryButtonText) ? DialogResult.OK : DialogResult.Yes;

        ModernButton? secondaryBtn = null;
        if (!string.IsNullOrWhiteSpace(secondaryButtonText))
        {
            secondaryBtn = Theme.OutlineButton(secondaryButtonText);
            secondaryBtn.Height = 36;
            secondaryBtn.Width = Math.Max(112, secondaryBtn.RequiredWidth + 8);
            secondaryBtn.DialogResult = DialogResult.Cancel;
        }

        // Layout footer right-aligned
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        primaryBtn.Margin = new Padding(8, 0, 0, 0);
        actions.Controls.Add(primaryBtn);
        if (secondaryBtn is not null)
        {
            secondaryBtn.Margin = new Padding(0);
            actions.Controls.Add(secondaryBtn);
        }
        footer.Controls.Add(actions);

        Controls.Add(bodyHost);
        Controls.Add(footer);
        Controls.Add(stripe);

        if (secondaryBtn is not null)
        {
            CancelButton = secondaryBtn;
            if (isDestructive)
            {
                // Safe non-destructive default: accidental Enter preserves data
                AcceptButton = secondaryBtn;
                Shown += (_, _) => secondaryBtn.Focus();
            }
            else
            {
                AcceptButton = primaryBtn;
                Shown += (_, _) => primaryBtn.Focus();
            }
        }
        else
        {
            AcceptButton = primaryBtn;
            CancelButton = primaryBtn;
            Shown += (_, _) => primaryBtn.Focus();
        }

        var chromeHeight = Height - ClientSize.Height;
        ClientSize = new Size(dialogWidth, contentBottom + footer.Height + stripe.Height + 12);
        MaximumSize = new Size(640, 720);
        MinimumSize = new Size(440, 180 + chromeHeight);
    }

    private void ToggleDetails()
    {
        if (_detailsContainer is null || _toggleDetailsBtn is null) return;
        _detailsExpanded = !_detailsExpanded;
        _detailsContainer.Visible = _detailsExpanded;
        _toggleDetailsBtn.Text = _detailsExpanded ? "Ẩn chi tiết kỹ thuật" : "Xem chi tiết kỹ thuật";
        var delta = _detailsExpanded ? 132 : -132;
        ClientSize = new Size(ClientSize.Width, Math.Max(210, ClientSize.Height + delta));
    }

    private static (Color Accent, Color LightBg, Color BorderCol, IconKind Icon) ResolveVisualTokens(
        AtelierNotificationKind kind,
        bool isDestructive)
    {
        if (isDestructive)
        {
            return (Theme.Danger, Theme.DangerLight, Theme.DangerBorder, IconKind.AlertTriangle);
        }

        return kind switch
        {
            AtelierNotificationKind.Success => (Theme.Success, Theme.SuccessLight, Theme.SuccessBorder, IconKind.Check),
            AtelierNotificationKind.Warning => (Theme.Warning, Theme.WarningLight, Theme.WarningBorder, IconKind.AlertTriangle),
            AtelierNotificationKind.Error => (Theme.Danger, Theme.DangerLight, Theme.DangerBorder, IconKind.AlertTriangle),
            AtelierNotificationKind.Confirmation => (Theme.Primary, Theme.PrimaryLight, Theme.PrimaryLightBorder, IconKind.Info),
            _ => (Theme.Primary, Theme.PrimaryLight, Theme.PrimaryLightBorder, IconKind.Info)
        };
    }

    private static string DefaultTitle(AtelierNotificationKind kind) => kind switch
    {
        AtelierNotificationKind.Success => "Thao tác thành công",
        AtelierNotificationKind.Warning => "Cần lưu ý",
        AtelierNotificationKind.Error => "Không thể thực hiện",
        AtelierNotificationKind.Confirmation => "Xác nhận thao tác",
        _ => "Thông báo từ Atelier Lumière"
    };
}

/// <summary>
/// Lightweight, non-intrusive toast banner for short positive or informational feedback.
/// Auto-dismisses safely without stealing keyboard focus.
/// </summary>
public sealed class AtelierToast : Form
{
    private readonly System.Windows.Forms.Timer _lifeTimer;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            const int WS_EX_NOACTIVATE = 0x08000000;
            cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public AtelierToast(AtelierNotificationKind kind, string title, string message, int durationMs = 3400)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        Size = new Size(376, 86);
        BackColor = Theme.Surface;
        DoubleBuffered = true;

        var (accent, lightBg, icon) = kind switch
        {
            AtelierNotificationKind.Success => (Theme.Success, Theme.SuccessLight, IconKind.Check),
            AtelierNotificationKind.Warning => (Theme.Warning, Theme.WarningLight, IconKind.AlertTriangle),
            AtelierNotificationKind.Error => (Theme.Danger, Theme.DangerLight, IconKind.AlertTriangle),
            _ => (Theme.Primary, Theme.PrimaryLight, IconKind.Info)
        };

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10,
            AccentStripeLeft = true,
            AccentStripeColor = accent,
            AccentStripeThickness = 4,
            Padding = new Padding(14, 10, 12, 10)
        };

        var iconBox = new Panel
        {
            Location = new Point(14, 18),
            Size = new Size(30, 30),
            BackColor = Color.Transparent
        };
        iconBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(lightBg);
            e.Graphics.FillEllipse(b, 0, 0, 29, 29);
            VectorIcons.Draw(e.Graphics, icon, new RectangleF(7, 7, 16, 16), accent, 1.8f);
        };

        var titleLbl = new Label
        {
            Text = string.IsNullOrWhiteSpace(title) ? "Atelier Lumière" : title.Trim(),
            Location = new Point(52, 10),
            Size = new Size(280, 20),
            Font = Theme.Font(9.4f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoEllipsis = true
        };

        var msgLbl = new Label
        {
            Text = message.Trim(),
            Location = new Point(52, 32),
            Size = new Size(286, 42),
            Font = Theme.Font(8.5f),
            ForeColor = Theme.TextSecondary,
            AutoEllipsis = true
        };

        var closeBtn = new Label
        {
            Text = "✕",
            Location = new Point(342, 8),
            Size = new Size(22, 22),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Font(8.5f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            Cursor = Cursors.Hand
        };
        closeBtn.Click += (_, _) => SafeClose();
        card.Click += (_, _) => SafeClose();

        card.Controls.AddRange([iconBox, titleLbl, msgLbl, closeBtn]);
        Controls.Add(card);

        _lifeTimer = new System.Windows.Forms.Timer { Interval = Math.Clamp(durationMs, 1500, 10000) };
        _lifeTimer.Tick += (_, _) => SafeClose();
        Shown += (_, _) => _lifeTimer.Start();
    }

    private void SafeClose()
    {
        if (IsDisposed) return;
        _lifeTimer.Stop();
        Close();
        Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifeTimer.Stop();
            _lifeTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Inline rounded validation or informational alert banner embedded inside pages and wizards.
/// </summary>
public sealed class AtelierInlineAlert : CardPanel
{
    private readonly Panel _iconBox;
    private readonly Label _titleLbl;
    private readonly Label _bodyLbl;
    private AtelierNotificationKind _kind = AtelierNotificationKind.Info;
    private Color _accent = Theme.Primary;
    private Color _iconBg = Theme.PrimaryLight;
    private IconKind _icon = IconKind.Info;

    public AtelierInlineAlert()
    {
        CornerRadius = 10;
        AccentStripeLeft = true;
        AccentStripeThickness = 4;
        Padding = new Padding(14, 10, 14, 10);
        MinimumSize = new Size(220, 52);
        Visible = false;

        _iconBox = new Panel
        {
            Dock = DockStyle.Left,
            Width = 34,
            BackColor = Color.Transparent
        };
        _iconBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(_iconBg);
            e.Graphics.FillEllipse(b, 2, 4, 26, 26);
            VectorIcons.Draw(e.Graphics, _icon, new RectangleF(8, 10, 14, 14), _accent, 1.8f);
        };

        var textHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(6, 0, 0, 0)
        };
        _titleLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Font(9.0f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoEllipsis = true
        };
        _bodyLbl = new Label
        {
            Dock = DockStyle.Fill,
            Font = Theme.Font(8.5f),
            ForeColor = Theme.TextSecondary
        };
        textHost.Controls.Add(_bodyLbl);
        textHost.Controls.Add(_titleLbl);

        Controls.Add(textHost);
        Controls.Add(_iconBox);
    }

    public void ShowMessage(AtelierNotificationKind kind, string title, string message)
    {
        _kind = kind;
        (_accent, BackColor, BorderColor, _iconBg, _icon) = kind switch
        {
            AtelierNotificationKind.Success => (Theme.Success, Theme.SuccessLight, Theme.SuccessBorder, Theme.Surface, IconKind.Check),
            AtelierNotificationKind.Warning => (Theme.Warning, Theme.WarningLight, Theme.WarningBorder, Theme.Surface, IconKind.AlertTriangle),
            AtelierNotificationKind.Error => (Theme.Danger, Theme.DangerLight, Theme.DangerBorder, Theme.Surface, IconKind.AlertTriangle),
            _ => (Theme.Primary, Theme.PrimaryLight, Theme.PrimaryLightBorder, Theme.Surface, IconKind.Info)
        };
        AccentStripeColor = _accent;
        _titleLbl.ForeColor = _accent == Theme.Primary ? Theme.Primitives.Terracotta800 : _accent;
        _titleLbl.Text = title;
        _bodyLbl.Text = message;
        Visible = true;
        _iconBox.Invalidate();
        Invalidate();
    }

    public void Clear()
    {
        Visible = false;
    }
}

public static class AtelierNotificationService
{
    /// <summary>
    /// Optional automated-test interceptor allowing STA verification scripts to inspect dialog
    /// invocations without blocking on modal loops. Null in normal desktop execution.
    /// </summary>
    internal static Func<AtelierDialogRequest, DialogResult?>? TestDialogInterceptor { get; set; }

    public static void ShowInfo(IWin32Window? owner, string message, string? title = null, string? technicalDetails = null) =>
        ShowDialogCore(owner, AtelierNotificationKind.Info, title ?? "Atelier Lumière — Thông tin", message, "Đã hiểu", null, false, technicalDetails);

    public static void ShowSuccess(IWin32Window? owner, string message, string? title = null) =>
        ShowDialogCore(owner, AtelierNotificationKind.Success, title ?? "Hoàn tất thao tác", message, "Tiếp tục", null, false, null);

    public static void ShowWarning(IWin32Window? owner, string message, string? title = null, string? technicalDetails = null) =>
        ShowDialogCore(owner, AtelierNotificationKind.Warning, title ?? "Cần lưu ý", message, "Đã hiểu", null, false, technicalDetails);

    public static void ShowError(IWin32Window? owner, string message, string? title = null, string? technicalDetails = null) =>
        ShowDialogCore(owner, AtelierNotificationKind.Error, title ?? "Không thể thực hiện", message, "Đóng", null, false, technicalDetails);

    public static void ShowException(IWin32Window? owner, string stage, Exception ex, string? userFriendlyMessage = null)
    {
        var summary = string.IsNullOrWhiteSpace(userFriendlyMessage)
            ? Ui.FormatRuntimeError(stage, ex)
            : userFriendlyMessage;
        var details = $"Bước xử lý: {stage}{Environment.NewLine}{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
        ShowDialogCore(owner, AtelierNotificationKind.Error, "Đã xảy ra sự cố xử lý", summary, "Đóng", null, false, details);
    }

    public static bool Confirm(
        IWin32Window? owner,
        string title,
        string message,
        string confirmText = "Xác nhận",
        string cancelText = "Quay lại",
        bool isDestructive = false)
    {
        var dr = ShowDialogCore(
            owner,
            AtelierNotificationKind.Confirmation,
            title,
            message,
            confirmText,
            cancelText,
            isDestructive,
            null);
        return dr is DialogResult.Yes or DialogResult.OK;
    }

    public static void ShowToast(
        IWin32Window? owner,
        AtelierNotificationKind kind,
        string title,
        string message,
        int durationMs = 3400)
    {
        if (TestDialogInterceptor is not null)
        {
            TestDialogInterceptor(new AtelierDialogRequest(kind, title, message, "OK", null, false, null));
            return;
        }

        var parentForm = ResolveOwnerForm(owner);
        var toast = new AtelierToast(kind, title, message, durationMs);

        var workArea = parentForm is not null
            ? Screen.FromControl(parentForm).WorkingArea
            : Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);

        var targetX = parentForm is not null && parentForm.WindowState != FormWindowState.Minimized
            ? Math.Min(workArea.Right - toast.Width - 16, parentForm.Right - toast.Width - 24)
            : workArea.Right - toast.Width - 20;
        var targetY = parentForm is not null && parentForm.WindowState != FormWindowState.Minimized
            ? Math.Min(workArea.Bottom - toast.Height - 16, parentForm.Bottom - toast.Height - 24)
            : workArea.Bottom - toast.Height - 20;

        toast.Location = new Point(Math.Max(workArea.Left + 12, targetX), Math.Max(workArea.Top + 12, targetY));

        if (parentForm is not null && !parentForm.IsDisposed)
        {
            void OnParentClosed(object? _, FormClosedEventArgs __)
            {
                if (!toast.IsDisposed) toast.Close();
            }
            parentForm.FormClosed += OnParentClosed;
            toast.FormClosed += (_, _) =>
            {
                if (!parentForm.IsDisposed) parentForm.FormClosed -= OnParentClosed;
            };
            toast.Show(parentForm);
        }
        else
        {
            toast.Show();
        }
    }

    private static DialogResult ShowDialogCore(
        IWin32Window? owner,
        AtelierNotificationKind kind,
        string title,
        string message,
        string primaryButtonText,
        string? secondaryButtonText,
        bool isDestructive,
        string? technicalDetails)
    {
        var request = new AtelierDialogRequest(
            kind,
            title,
            message,
            primaryButtonText,
            secondaryButtonText,
            isDestructive,
            technicalDetails);

        var intercepted = TestDialogInterceptor?.Invoke(request);
        if (intercepted.HasValue)
        {
            return intercepted.Value;
        }

        using var dlg = new AtelierDialog(
            kind,
            title,
            message,
            primaryButtonText,
            secondaryButtonText,
            isDestructive,
            technicalDetails);

        var resolvedOwner = ResolveOwnerWindow(owner);
        if (resolvedOwner is null)
        {
            dlg.StartPosition = FormStartPosition.CenterScreen;
            return dlg.ShowDialog();
        }

        return dlg.ShowDialog(resolvedOwner);
    }

    private static IWin32Window? ResolveOwnerWindow(IWin32Window? owner)
    {
        if (owner is Control c && (c.IsDisposed || !c.IsHandleCreated))
        {
            return Form.ActiveForm;
        }
        return owner ?? Form.ActiveForm;
    }

    private static Form? ResolveOwnerForm(IWin32Window? owner) => owner switch
    {
        Form f => f,
        Control c => c.FindForm(),
        _ => Form.ActiveForm
    };
}

