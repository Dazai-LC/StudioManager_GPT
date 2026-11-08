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
        Font = Theme.Font(9.5f)
    };
    private readonly DateTimePicker _to = new()
    {
        Format = DateTimePickerFormat.Short,
        Value = DateTime.Today,
        Font = Theme.Font(9.5f)
    };

    private readonly StatCard _revenue = new("DOANH THU DỊCH VỤ", Theme.Primary, "↗");
    private readonly StatCard _cash = new("THỰC THU", Theme.Success, "₫");
    private readonly StatCard _debt = new("CÔNG NỢ HIỆN TẠI", Theme.Warning, "⚠");
    private readonly StatCard _count = new("TỔNG LỊCH CHỤP", Theme.Cyan, "📅");

    private readonly DataGridView _grid = Theme.Grid();
    private readonly Label _summary = new()
    {
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        ForeColor = Theme.TextSecondary,
        Font = Theme.Font(9.0f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent
    };

    public ReportsPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var header = new PageHeader("Báo cáo & Phân tích", "Tổng hợp số liệu kinh doanh, doanh thu thực thu và công suất khai thác dịch vụ.");
        var run = Theme.Button("Xem báo cáo");
        run.Width = 130;
        run.Click += async (_, _) => await LoadAsync();

        var export = Theme.Button("Xuất Excel", Theme.Excel);
        export.Width = 130;
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, "bao-cao-goi-chup");

        header.Actions.Controls.Add(run);
        header.Actions.Controls.Add(export);

        var filtersCard = BuildFilterToolbar();

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 136,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 0, 0, 10),
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(_revenue, 0, 0);
        cards.Controls.Add(_cash, 1, 0);
        cards.Controls.Add(_debt, 2, 0);
        cards.Controls.Add(_count, 3, 0);
        foreach (Control c in cards.Controls) c.Dock = DockStyle.Fill;

        var summaryCard = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(16, 8, 16, 8),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 8,
            BorderColor = Theme.Border
        };
        summaryCard.Controls.Add(_summary);

        var dataCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 14),
            CornerRadius = 12
        };

        var titleLabel = new Label
        {
            Text = "Phân bổ số lượt chụp theo gói dịch vụ",
            Dock = DockStyle.Top,
            Height = 36,
            Font = Theme.Font(11, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };

        dataCard.Controls.Add(Ui.DataHost(_grid));
        dataCard.Controls.Add(titleLabel);

        Controls.Add(dataCard);
        Controls.Add(summaryCard);
        Controls.Add(cards);
        Controls.Add(filtersCard);
        Controls.Add(header);

        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildFilterToolbar()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 10
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        flow.Controls.Add(Ui.Field("TỪ NGÀY", _from, 180));
        flow.Controls.Add(Ui.Field("ĐẾN NGÀY", _to, 180));

        var thisMonth = Theme.OutlineButton("Tháng này");
        thisMonth.Click += async (_, _) =>
        {
            _from.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _to.Value = DateTime.Today;
            await LoadAsync();
        };

        var thisYear = Theme.OutlineButton("Năm nay");
        thisYear.Click += async (_, _) =>
        {
            _from.Value = new DateTime(DateTime.Today.Year, 1, 1);
            _to.Value = DateTime.Today;
            await LoadAsync();
        };

        flow.Controls.Add(Ui.ToolbarButton(thisMonth));
        flow.Controls.Add(Ui.ToolbarButton(thisYear));

        card.Controls.Add(flow);
        return card;
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
            _revenue.Value = $"{r.DoanhThu:N0} đ";
            _cash.Value = $"{r.ThucThu:N0} đ";
            _debt.Value = $"{r.CongNo:N0} đ";
            _count.Value = r.TongLich.ToString();

            _summary.Text = $"📊  Tổng thu: {r.TongThu:N0} đ   •   Tổng hoàn: {r.TongHoan:N0} đ   •   Hoàn thành: {r.HoanThanh} lịch   •   Đã hủy: {r.DaHuy} lịch";

            _grid.DataSource = r.ByPackage.Select(x => new
            {
                Gói_chụp = x.Name,
                Số_lần = (int)x.Value
            }).ToList();

            if (_grid.Columns.Contains("Gói_chụp")) _grid.Columns["Gói_chụp"].HeaderText = "Tên gói chụp";
            if (_grid.Columns.Contains("Số_lần"))
            {
                _grid.Columns["Số_lần"].HeaderText = "Số lượt phục vụ";
                _grid.Columns["Số_lần"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }
}
