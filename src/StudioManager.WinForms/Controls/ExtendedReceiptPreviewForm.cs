using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using StudioManager.WinForms.Contracts;

namespace StudioManager.WinForms.Controls;

/// <summary>
/// Multi-format desktop Receipt Preview &amp; Print Workspace supporting A4, K80 thermal,
/// K58 thermal, and PDF export preview layout without relying on WebView/HTML rendering.
/// Handles multi-line wrapping cleanly across narrow thermal widths and draws Demo watermark.
/// </summary>
public sealed class ExtendedReceiptPreviewForm : Form
{
    private readonly ReceiptDocumentDto _baseDoc;
    private ReceiptDocumentDto _activeDoc;
    private readonly Panel _paperHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Primitives.Alabaster200 };
    private readonly Panel _sheet = new() { BackColor = Color.White };
    private readonly Dictionary<ReceiptFormatKind, ModernButton> _fmtButtons = new();

    public ExtendedReceiptPreviewForm(ReceiptDocumentDto doc)
    {
        _baseDoc = doc;
        _activeDoc = doc;

        Text = $"Xem trước biên nhận ({doc.ReceiptNumber}) — Atelier Lumière";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 760);
        MinimumSize = new Size(760, 560);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        BackColor = Theme.Background;
        Font = Theme.Font();

        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(_sheet, true);

        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Theme.Surface,
            Padding = new Padding(16, 10, 16, 10)
        };
        var formatBar = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 580, WrapContents = false };
        var actionBar = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 280, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };

        void AddFmtBtn(string text, ReceiptFormatKind kind)
        {
            var btn = Theme.OutlineButton(text);
            btn.Height = 34;
            btn.Width = Math.Max(104, btn.RequiredWidth);
            btn.Click += (_, _) => SwitchFormat(kind);
            _fmtButtons[kind] = btn;
            formatBar.Controls.Add(btn);
        }
        AddFmtBtn("A4 Đầy đủ", ReceiptFormatKind.A4Invoice);
        AddFmtBtn("Nhiệt K80", ReceiptFormatKind.ThermalK80);
        AddFmtBtn("Nhiệt K58", ReceiptFormatKind.ThermalK58);
        AddFmtBtn("Bố cục PDF", ReceiptFormatKind.PdfLayoutPreview);

        var closeBtn = Theme.OutlineButton("Đóng");
        closeBtn.Height = 34;
        closeBtn.Click += (_, _) => Close();
        var printBtn = Theme.Button("In bản xem trước", Theme.Primary, IconKind.Printer);
        printBtn.Width = 166;
        printBtn.Height = 34;
        printBtn.Click += (_, _) => TriggerPrintPreview();

        actionBar.Controls.Add(closeBtn);
        actionBar.Controls.Add(printBtn);
        topBar.Controls.Add(formatBar);
        topBar.Controls.Add(actionBar);

        _sheet.Paint += (_, e) => PaintReceipt(e.Graphics, _sheet.ClientRectangle);
        _paperHost.Controls.Add(_sheet);
        _paperHost.Resize += (_, _) => CenterSheet();

        Controls.Add(_paperHost);
        Controls.Add(topBar);
        CancelButton = closeBtn;

        SwitchFormat(doc.Format);
    }

    private void SwitchFormat(ReceiptFormatKind fmt)
    {
        _activeDoc = _baseDoc with { Format = fmt };
        foreach (var kv in _fmtButtons)
        {
            var active = kv.Key == fmt;
            kv.Value.BorderColor = active ? Theme.Primary : Theme.BorderStrong;
            kv.Value.ForeColor = active ? Theme.Primary : Theme.TextSecondary;
            kv.Value.BackColor = active ? Theme.Primitives.Terracotta50 : Theme.Surface;
        }

        var width = fmt switch
        {
            ReceiptFormatKind.ThermalK80 => 360,
            ReceiptFormatKind.ThermalK58 => 282,
            ReceiptFormatKind.PdfLayoutPreview => 690,
            _ => 670
        };
        var requiredHeight = MeasureRequiredHeight(width);
        _sheet.Size = new Size(width, Math.Max(fmt is ReceiptFormatKind.ThermalK80 or ReceiptFormatKind.ThermalK58 ? 640 : 780, requiredHeight));
        CenterSheet();
        _sheet.Invalidate();
    }

    private int MeasureRequiredHeight(int sheetWidth)
    {
        var isThermal = _activeDoc.Format is ReceiptFormatKind.ThermalK80 or ReceiptFormatKind.ThermalK58;
        var pad = isThermal ? 14 : 36;
        var contentW = Math.Max(120, sheetWidth - pad * 2);
        var estimatedLines = 18 + _activeDoc.LineItems.Count * 2 + (_activeDoc.PaymentMethodLabel.Length / Math.Max(18, contentW / 9));
        return pad * 2 + estimatedLines * 24 + 120;
    }

    private void CenterSheet()
    {
        var x = Math.Max(20, (_paperHost.ClientSize.Width - _sheet.Width) / 2);
        _sheet.Location = new Point(x, 22);
    }

    private void TriggerPrintPreview()
    {
        using var doc = new PrintDocument { DocumentName = _activeDoc.ReceiptNumber };
        doc.PrintPage += (_, e) => PaintReceipt(e.Graphics!, e.MarginBounds);
        using var dlg = new PrintPreviewDialog { Document = doc, Width = 860, Height = 660 };
        dlg.ShowDialog(this);
    }

    private void PaintReceipt(Graphics g, Rectangle bounds)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.White);

        var isThermal = _activeDoc.Format is ReceiptFormatKind.ThermalK80 or ReceiptFormatKind.ThermalK58;
        var pad = isThermal ? 14 : 36;
        var y = bounds.Top + pad;
        var left = bounds.Left + pad;
        var right = bounds.Right - pad;
        var contentW = Math.Max(120, right - left);

        using var borderPen = new Pen(Theme.BorderStrong, 1f);
        g.DrawRectangle(borderPen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);

        // Diagonal subtle Demo watermark
        var wmState = g.Save();
        using var fWm = Theme.Font(isThermal ? 18f : 32f, FontStyle.Bold);
        using var brWm = new SolidBrush(Color.FromArgb(16, Theme.Primary.R, Theme.Primary.G, Theme.Primary.B));
        g.TranslateTransform(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        g.RotateTransform(-28f);
        using var sfWm = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("BẢN XEM TRƯỚC DEMO", fWm, brWm, 0, 0, sfWm);
        g.Restore(wmState);

        using var fBrand = Theme.Font(isThermal ? 11f : 15f, FontStyle.Bold);
        using var fSub = Theme.MonoFont(isThermal ? 7.4f : 8.4f, FontStyle.Bold);
        using var fBody = Theme.Font(isThermal ? 7.9f : 9.1f);
        using var fBold = Theme.Font(isThermal ? 8.1f : 9.3f, FontStyle.Bold);
        using var brText = new SolidBrush(Theme.Text);
        using var brAccent = new SolidBrush(Theme.Primary);
        using var brMuted = new SolidBrush(Theme.Muted);
        using var brSuccess = new SolidBrush(Theme.Success);

        g.DrawString("ATELIER LUMIÈRE STUDIO", fBrand, brAccent, left, y);
        y += isThermal ? 22 : 28;
        g.DrawString($"BIÊN NHẬN XEM TRƯỚC ({_activeDoc.Format})  •  DEMO", fSub, brMuted, left, y);
        y += 22;
        using var divPen = new Pen(Theme.BorderStrong, 1f);
        g.DrawLine(divPen, left, y, right, y);
        y += 12;

        void DrawKv(string k, string v, Brush? valueBrush = null)
        {
            var vb = valueBrush ?? brText;
            var kSize = g.MeasureString(k, fBody);
            var vSize = g.MeasureString(v, fBold);

            if (kSize.Width + vSize.Width + 14 <= contentW)
            {
                g.DrawString(k, fBody, brMuted, left, y);
                using var sfR = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(v, fBold, vb, new RectangleF(left, y, contentW, Math.Max(20, (int)Math.Ceiling(vSize.Height))), sfR);
                y += Math.Max(22, (int)Math.Ceiling(Math.Max(kSize.Height, vSize.Height)) + 4);
            }
            else
            {
                g.DrawString(k, fBody, brMuted, left, y);
                y += (int)Math.Ceiling(kSize.Height) + 2;
                var wrapped = g.MeasureString(v, fBold, contentW);
                g.DrawString(v, fBold, vb, new RectangleF(left + 4, y, contentW - 4, wrapped.Height + 6));
                y += (int)Math.Ceiling(wrapped.Height) + 6;
            }
        }

        DrawKv("Số biên nhận:", _activeDoc.ReceiptNumber);
        DrawKv("Ngày phát hành:", _activeDoc.IssuedAtLabel);
        DrawKv("Mã tham chiếu:", _activeDoc.ReferenceCode);
        DrawKv("Khách hàng:", _activeDoc.CustomerName);
        DrawKv("Điện thoại:", _activeDoc.CustomerPhone);
        DrawKv("Gói dịch vụ:", _activeDoc.PackageName);
        DrawKv("Phòng chụp:", _activeDoc.RoomName);
        DrawKv("Phương thức:", _activeDoc.PaymentMethodLabel);
        y += 6;
        g.DrawLine(divPen, left, y, right, y);
        y += 10;

        var priceColW = isThermal ? 86f : 124f;
        var itemDescW = Math.Max(80f, contentW - priceColW - 8f);
        foreach (var item in _activeDoc.LineItems)
        {
            var desc = $"{item.Description} ({item.Quantity} {item.UnitLabel})";
            var measuredDesc = g.MeasureString(desc, fBody, (int)itemDescW);
            g.DrawString(desc, fBody, brText, new RectangleF(left, y, itemDescW, measuredDesc.Height + 4));
            using var sfR = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString($"{item.LineTotal:N0} đ", fBold, brText, new RectangleF(left + itemDescW + 4, y, priceColW + 4, 22), sfR);
            y += Math.Max(24, (int)Math.Ceiling(measuredDesc.Height) + 6);
        }

        y += 8;
        g.DrawLine(divPen, left, y, right, y);
        y += 10;
        DrawKv("Tổng giá trị đơn:", $"{_activeDoc.Subtotal:N0} đ");
        DrawKv("Đã ghi nhận thanh toán:", $"{_activeDoc.PaidAmount:N0} đ", _activeDoc.PaidAmount > 0 ? brSuccess : brText);
        DrawKv("Số tiền còn lại:", $"{_activeDoc.RemainingAmount:N0} đ", _activeDoc.RemainingAmount > 0 ? brAccent : brSuccess);
        DrawKv("Người lập (Demo):", _activeDoc.StaffIssuerName);

        y += 14;
        var footerSize = g.MeasureString(_activeDoc.FooterDisclaimer, fBody, contentW);
        g.DrawString(_activeDoc.FooterDisclaimer, fBody, brMuted, new RectangleF(left, y, contentW, footerSize.Height + 10));
    }
}
