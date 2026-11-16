using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StudioManager.WinForms.Controls;

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
        Ui.LogError("AtelierNotifications.ShowException", ex);
        var details = StudioManager.Application.SafeDiagnostics.Capture(ex, "AtelierNotifications.ShowException").DiagnosticJson;
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

