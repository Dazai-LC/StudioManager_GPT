using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.WinForms.Contracts;

namespace StudioManager.WinForms.Controls;

/// <summary>
/// Pure-GDI+ NON-SCANNABLE illustrative VietQR &amp; manual transfer instruction card.
/// Intentionally avoids ISO QR finder patterns so mobile banking cameras cannot scan it as a real bank account.
/// Enforces syntax ("PHOENIX [Ma]") and strictly excludes automated payment webhooks/SePay.
/// </summary>
public sealed class VietQrCardControl : CardPanel
{
    private VietQrInstructionDto _dto;
    private readonly Label _bankLabel = new() { AutoSize = true, MaximumSize = new Size(340, 0), Font = Theme.Font(8.8f, FontStyle.Bold), ForeColor = Theme.Text };
    private readonly Label _acctLabel = new() { AutoSize = true, Font = Theme.MonoFont(8.9f, FontStyle.Bold), ForeColor = Theme.TextSecondary };
    private readonly Label _amountLabel = new() { AutoSize = true, Font = Theme.Font(11.0f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly Label _syntaxLabel = new() { AutoSize = true, Font = Theme.MonoFont(9.5f, FontStyle.Bold), ForeColor = Theme.Primitives.Sage700 };
    private readonly Label _disclaimerLabel = new() { Dock = DockStyle.Fill, Font = Theme.Font(8.0f, FontStyle.Bold), ForeColor = Theme.WarningHover };
    private readonly Panel _qrCanvas = new() { Size = new Size(156, 156), BackColor = Color.White };

    public VietQrCardControl(VietQrInstructionDto dto)
    {
        _dto = dto;
        BackColor = Theme.Surface;
        BorderColor = Theme.BorderStrong;
        AccentStripeColor = Theme.Primitives.Sage600;
        AccentStripeLeft = true;
        AccentStripeThickness = 4;
        Padding = new Padding(14, 12, 14, 10);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        _qrCanvas.Paint += (_, e) => DrawNonScannableDemoPlate(e.Graphics, _dto.TransferSyntax, _dto.SuggestedAmount);

        var details = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 0, 0, 0)
        };
        var tag = new Label
        {
            AutoSize = true,
            Text = "THÔNG TIN CHUYỂN KHOẢN MINH HỌA (DEMO)",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Sage600,
            Margin = new Padding(0, 0, 0, 4)
        };
        var copyBtn = Theme.OutlineButton("Sao chép nội dung CK", Theme.Primitives.Sage600, IconKind.Check);
        copyBtn.Height = 30;
        copyBtn.Width = 186;
        copyBtn.Margin = new Padding(0, 6, 0, 0);
        copyBtn.Click += (_, _) =>
        {
            try { Clipboard.SetText(_dto.TransferSyntax); } catch { /* clipboard optional in headless/STA */ }
            copyBtn.Text = "Đã chép: " + _dto.TransferSyntax;
            AtelierNotificationService.ShowToast(
                FindForm(),
                AtelierNotificationKind.Success,
                "Đã sao chép nội dung chuyển khoản",
                $"Nội dung '{_dto.TransferSyntax}' đã sẵn sàng để dán (chế độ minh họa).");
        };

        details.Controls.Add(tag);
        details.Controls.Add(_bankLabel);
        details.Controls.Add(_acctLabel);
        details.Controls.Add(_amountLabel);
        details.Controls.Add(_syntaxLabel);
        details.Controls.Add(copyBtn);

        var warnPanel = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.WarningLight,
            BorderColor = Theme.WarningBorder,
            CornerRadius = 6,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 6, 0, 0)
        };
        grid.SetColumnSpan(warnPanel, 2);
        warnPanel.Controls.Add(_disclaimerLabel);

        grid.Controls.Add(_qrCanvas, 0, 0);
        grid.Controls.Add(details, 1, 0);
        grid.Controls.Add(warnPanel, 0, 1);
        Controls.Add(grid);

        Bind(_dto);
    }

    public void Bind(VietQrInstructionDto dto)
    {
        _dto = dto;
        _bankLabel.Text = $"{dto.BankDisplayName} • {dto.AccountHolder}";
        _acctLabel.Text = $"Số tài khoản (Minh họa): {dto.MaskedAccountNumber}";
        _amountLabel.Text = $"Số tiền đề xuất: {dto.SuggestedAmount:N0} đ";
        _syntaxLabel.Text = $"Nội dung bắt buộc: {dto.TransferSyntax}";
        _disclaimerLabel.Text = dto.DisclaimerText;
        _qrCanvas.Invalidate();
    }

    /// <summary>
    /// Draws a clearly labeled non-scannable UI mockup card instead of 7x7 ISO QR finder squares.
    /// Guarantees banking apps will never accidentally treat Demo art as a real transfer destination.
    /// </summary>
    private static void DrawNonScannableDemoPlate(Graphics g, string syntax, decimal amount)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Theme.Primitives.Alabaster50);

        using var outerPen = new Pen(Theme.BorderStrong, 1.25f) { DashStyle = DashStyle.Dash };
        g.DrawRectangle(outerPen, 2, 2, 151, 151);

        // Soft decorative wireframe blocks (NO 7x7 finder corners)
        using var softTile = new SolidBrush(Color.FromArgb(228, 222, 212));
        for (var r = 0; r < 5; r++)
        {
            for (var c = 0; c < 5; c++)
            {
                if ((r + c) % 2 == 0 && !(r == 2 && c == 2))
                {
                    g.FillRectangle(softTile, 18 + c * 25, 16 + r * 20, 16, 12);
                }
            }
        }

        // Center vector badge
        using var pillBg = new SolidBrush(Theme.Surface);
        using var pillBorder = new Pen(Theme.Primary, 1.4f);
        var emblemRect = new RectangleF(24, 20, 108, 52);
        using var path = new GraphicsPath();
        var radius = 10f;
        path.AddArc(emblemRect.X, emblemRect.Y, radius, radius, 180, 90);
        path.AddArc(emblemRect.Right - radius, emblemRect.Y, radius, radius, 270, 90);
        path.AddArc(emblemRect.Right - radius, emblemRect.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(emblemRect.X, emblemRect.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        g.FillPath(pillBg, path);
        g.DrawPath(pillBorder, path);

        VectorIcons.Draw(g, IconKind.Wallet, new RectangleF(66, 25, 22, 22), Theme.Primary, 1.8f);
        using var fTitle = Theme.Font(7.2f, FontStyle.Bold);
        using var brPrimary = new SolidBrush(Theme.Primary);
        using var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("QR MINH HỌA UI", fTitle, brPrimary, new RectangleF(26, 49, 104, 18), sfCenter);

        // Clear non-scannable stamp at bottom with generous line spacing
        using var warnBg = new SolidBrush(Theme.WarningLight);
        using var warnBorder = new Pen(Theme.WarningBorder, 1f);
        g.FillRectangle(warnBg, 8, 82, 140, 64);
        g.DrawRectangle(warnBorder, 8, 82, 140, 64);

        using var fWarn = Theme.Font(6.7f, FontStyle.Bold);
        using var fAmt = Theme.MonoFont(7.0f, FontStyle.Bold);
        using var brWarn = new SolidBrush(Theme.WarningHover);
        using var brInk = new SolidBrush(Theme.TextSecondary);
        g.DrawString("KHÔNG QUÉT ĐỂ\nCHUYỂN TIỀN", fWarn, brWarn, new RectangleF(10, 86, 136, 32), sfCenter);
        using var sepPen = new Pen(Theme.WarningBorder, 0.8f);
        g.DrawLine(sepPen, 18, 121, 138, 121);
        g.DrawString($"{amount:N0} đ • Demo", fAmt, brInk, new RectangleF(10, 125, 136, 18), sfCenter);
    }
}
