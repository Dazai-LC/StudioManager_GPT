using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class ReportsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DateTimePicker _from = new()
    {
        Format = DateTimePickerFormat.Short,
        Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        Font = Theme.Font(9.2f)
    };
    private readonly DateTimePicker _to = new()
    {
        Format = DateTimePickerFormat.Short,
        Value = DateTime.Today,
        Font = Theme.Font(9.2f)
    };

    private readonly SegmentedTabBar _presetTabs = new() { Width = 360, Height = 38 };
    private readonly FinancialProportionBanner _proportionBanner = new() { Dock = DockStyle.Fill };
    private readonly PackageHorizontalBarChart _packageChart = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _grid = Theme.Grid();
    private readonly DossierFieldList _ledgerBreakdown = new() { Dock = DockStyle.Bottom, Height = 148 };

    public ReportsPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var topBar = BuildTopFilterConsole();
        var bannerHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 128,
            Padding = new Padding(0, 0, 0, 10),
            BackColor = Color.Transparent
        };
        bannerHost.Controls.Add(_proportionBanner);

        var splitAnalytics = BuildBottomAnalyticalSplit();

        Controls.Add(splitAnalytics);
        Controls.Add(bannerHost);
        Controls.Add(topBar);

        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildTopFilterConsole()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 68,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong
        };

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Theme.Surface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 368));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));

        _presetTabs.SetItems([
            new("month", "Tháng này", IconKind.Calendar),
            new("quarter", "Quý này"),
            new("year", "Năm nay"),
            new("all", "Toàn thời gian")
        ], "month");

        _presetTabs.SegmentSelected += async (_, seg) =>
        {
            var today = DateTime.Today;
            switch (seg.Key)
            {
                case "month":
                    _from.Value = new DateTime(today.Year, today.Month, 1);
                    _to.Value = today;
                    break;
                case "quarter":
                    var qMonth = ((today.Month - 1) / 3) * 3 + 1;
                    _from.Value = new DateTime(today.Year, qMonth, 1);
                    _to.Value = today;
                    break;
                case "year":
                    _from.Value = new DateTime(today.Year, 1, 1);
                    _to.Value = today;
                    break;
                case "all":
                    _from.Value = new DateTime(today.Year - 3, 1, 1);
                    _to.Value = today.AddMonths(3);
                    break;
            }
            await LoadAsync();
        };

        var presetHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 7, 0, 0) };
        presetHost.Controls.Add(_presetTabs);

        var dateRangeFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(8, 2, 0, 0),
            BackColor = Color.Transparent
        };
        dateRangeFlow.Controls.Add(CompactDateSlot("TỪ NGÀY", _from));
        dateRangeFlow.Controls.Add(CompactDateSlot("ĐẾN NGÀY", _to));

        var runBtn = Theme.Button("Phân tích", Theme.Primary, IconKind.ChartBar);
        runBtn.Dock = DockStyle.Fill;
        runBtn.Margin = new Padding(6, 7, 0, 7);
        runBtn.Click += async (_, _) => await LoadAsync();

        var exportBtn = Theme.Button("Xuất Excel", Theme.Excel, IconKind.Download);
        exportBtn.Dock = DockStyle.Fill;
        exportBtn.Margin = new Padding(8, 7, 0, 7);
        exportBtn.Click += (_, _) => Ui.ExportGrid(_grid, this, "bao-cao-goi-chup");

        row.Controls.Add(presetHost, 0, 0);
        row.Controls.Add(dateRangeFlow, 1, 0);
        row.Controls.Add(runBtn, 2, 0);
        row.Controls.Add(exportBtn, 3, 0);

        card.Controls.Add(row);
        return card;
    }

    private static Control CompactDateSlot(string label, DateTimePicker picker)
    {
        var panel = new Panel { Width = 148, Height = 46, Margin = new Padding(0, 0, 10, 0), BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 16,
            Text = label,
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Muted
        };
        picker.Dock = DockStyle.Bottom;
        panel.Controls.Add(picker);
        panel.Controls.Add(lbl);
        return panel;
    }

    private Control BuildBottomAnalyticalSplit()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Left: Custom GDI+ Ranked Horizontal Bar Chart
        var leftCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(18, 14, 18, 14),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };
        var leftTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "Xếp hạng Tỷ trọng Khai thác theo Gói chụp",
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        leftCard.Controls.Add(_packageChart);
        leftCard.Controls.Add(leftTitle);

        // Right: Package Breakdown Table + Financial Ledger Summary
        var rightCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(18, 14, 18, 14),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primitives.Espresso900,
            AccentStripeThickness = 4
        };
        var rightTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "Bảng số liệu Gói chụp & Đối soát Sổ cái",
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text
        };

        _grid.Margin = Padding.Empty;
        rightCard.Controls.Add(_grid);
        rightCard.Controls.Add(_ledgerBreakdown);
        rightCard.Controls.Add(rightTitle);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        return split;
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await _app.Reports.LoadAsync(_from.Value.Date, _to.Value.Date, _app.Session!);
            if (!result.Success || result.Data is null)
            {
                Ui.Error(this, result.Message);
                return;
            }

            var r = result.Data;
            _proportionBanner.SetReport(r);
            _packageChart.Data = r.ByPackage;

            _ledgerBreakdown.SetRows([
                ("Tổng thu tiền mặt / chuyển khoản", $"{r.TongThu:N0} đ", Theme.Success, false),
                ("Tổng chi hoàn tiền khách hàng", $"{r.TongHoan:N0} đ", r.TongHoan > 0 ? Theme.Warning : Theme.Muted, false),
                ("Thực thu ròng trong kỳ", $"{r.ThucThu:N0} đ", Theme.Primary, true),
                ("Tiến độ hoàn thành / Đã hủy", $"{r.HoanThanh} hoàn thành  •  {r.DaHuy} đã hủy / {r.TongLich} tổng lịch", null, false)
            ]);

            var totalCount = Math.Max(1m, r.ByPackage.Sum(x => x.Value));
            _grid.DataSource = r.ByPackage.Select(x => new
            {
                Gói_chụp = x.Name,
                Số_lần = (int)x.Value,
                Tỷ_trọng = $"{(x.Value * 100m / totalCount):0.#}%"
            }).ToList();

            if (_grid.Columns.Contains("Gói_chụp")) _grid.Columns["Gói_chụp"].HeaderText = "Tên gói chụp";
            if (_grid.Columns.Contains("Số_lần"))
            {
                _grid.Columns["Số_lần"].HeaderText = "Số lượt phục vụ";
                _grid.Columns["Số_lần"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (_grid.Columns.Contains("Tỷ_trọng"))
            {
                _grid.Columns["Tỷ_trọng"].HeaderText = "Tỷ trọng";
                _grid.Columns["Tỷ_trọng"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }
}

/// <summary>
/// Custom GDI+ Financial Equation & Stacked Proportion Banner for ReportsPage.
/// </summary>
internal sealed class FinancialProportionBanner : CardPanel
{
    private ReportData? _data;

    public FinancialProportionBanner()
    {
        CornerRadius = 10;
        BackColor = Theme.Surface;
        BorderColor = Theme.BorderStrong;
        AccentStripeColor = Theme.Primary;
        AccentStripeThickness = 3;
        Padding = new Padding(16, 12, 16, 12);
    }

    public void SetReport(ReportData data)
    {
        _data = data;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var d = _data;
        var rev = d?.DoanhThu ?? 0m;
        var cash = d?.ThucThu ?? 0m;
        var debt = d?.CongNo ?? 0m;
        var count = d?.TongLich ?? 0;
        var done = d?.HoanThanh ?? 0;

        var tiles = new (string Label, string Value, string Sub, Color Accent, IconKind Icon)[]
        {
            ("TỔNG GIÁ TRỊ DỊCH VỤ", $"{rev:N0} đ", "Doanh thu hợp đồng", Theme.Primary, IconKind.ChartBar),
            ("THỰC THU RÒNG (NET CASH)", $"{cash:N0} đ", $"Đã trừ hoàn {d?.TongHoan ?? 0:N0} đ", Theme.Success, IconKind.Wallet),
            ("CÔNG NỢ CÒN PHẢI THU", $"{debt:N0} đ", "Cần đối soát thu hồi", Theme.Danger, IconKind.AlertTriangle),
            ("TỔNG QUY MÔ LỊCH CHỤP", $"{count} lịch", $"{done} lịch đã hoàn thành", Theme.Cyan, IconKind.Calendar)
        };

        var slotW = (Width - 32f) / 4f;
        using var divPen = new Pen(Theme.Border, 1f);
        using var lblFont = Theme.MonoFont(7.5f, FontStyle.Bold);
        using var valFont = Theme.Font(15.0f, FontStyle.Bold);
        using var subFont = Theme.Font(8.0f, FontStyle.Bold);

        for (var i = 0; i < tiles.Length; i++)
        {
            var t = tiles[i];
            var x = 16f + i * slotW;
            if (i > 0) g.DrawLine(divPen, x, 14, x, 74);

            VectorIcons.Draw(g, t.Icon, new RectangleF(x + 10, 16, 20, 20), t.Accent, 1.85f);

            var tx = (int)(x + 38);
            var tw = Math.Max(40, (int)slotW - 46);
            TextRenderer.DrawText(g, t.Label, lblFont, new Rectangle(tx, 12, tw, 16), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, t.Value, valFont, new Rectangle(tx, 28, tw, 26), Theme.Text, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, t.Sub, subFont, new Rectangle(tx, 54, tw, 18), t.Accent, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        // Bottom Stacked Financial Proportion Bar (Cash vs Debt)
        var barY = Height - 22f;
        var barW = Math.Max(60f, Width - 32f);
        var trackRect = new RectangleF(16f, barY, barW, 8f);
        using (var tPath = Theme.RoundedRectangle(trackRect, 4f))
        using (var tFill = new SolidBrush(Theme.Primitives.Alabaster200))
        {
            g.FillPath(tFill, tPath);
        }

        var denom = Math.Max(1m, cash + debt);
        var cashRatio = (float)Math.Clamp(cash / denom, 0m, 1m);
        if (cash > 0)
        {
            var cw = Math.Max(8f, barW * cashRatio);
            using var cPath = Theme.RoundedRectangle(new RectangleF(16f, barY, cw, 8f), 4f);
            using var cBrush = new SolidBrush(Theme.Success);
            g.FillPath(cBrush, cPath);
        }
    }
}
