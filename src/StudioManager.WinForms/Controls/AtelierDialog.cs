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
