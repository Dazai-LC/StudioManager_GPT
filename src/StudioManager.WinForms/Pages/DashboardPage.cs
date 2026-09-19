using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class DashboardPage : UserControl
{
    private readonly AppFacade _app;
    private readonly StatCard _today = new("LỊCH HÔM NAY", Theme.Primary, "◷"), _upcoming = new("LỊCH SẮP TỚI", Theme.Warning, "↗"), _processing = new("ĐANG XỬ LÝ", Theme.Success, "◌"), _debt = new("CÔNG NỢ", Theme.Danger, "₫");
    private readonly DataGridView _grid = Theme.Grid(); private readonly RevenueBarChart _revenue = new() { Dock = DockStyle.Fill }; private readonly StatusDonutChart _status = new() { Dock = DockStyle.Fill }; private readonly SkeletonLoader _skeleton = new() { Dock = DockStyle.Fill };
    private readonly Label _updated = new() { AutoSize = true, Font = Theme.Font(8.5f), ForeColor = Theme.Muted, Margin = new Padding(8, 15, 5, 0) };

    public DashboardPage(AppFacade app)
    {
        _app = app; BackColor = Theme.Background;
        var header = new PageHeader("Tổng quan", $"Xin chào, {_app.Session!.HoTen}. Theo dõi hoạt động studio theo thời gian thực.");
        var refresh = Theme.Button("↻  Làm mới", Theme.Primary); refresh.Click += async (_, _) => await LoadAsync(); header.Actions.Controls.Add(refresh); header.Actions.Controls.Add(_updated);

        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 148, ColumnCount = 4, Padding = new Padding(0, 4, 0, 8) };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        AddCard(cards, _today, 0); AddCard(cards, _upcoming, 1); AddCard(cards, _processing, 2); AddCard(cards, _debt, 3);

        var analytics = new TableLayoutPanel { Dock = DockStyle.Top, Height = 286, ColumnCount = 2, Padding = new Padding(0, 0, 0, 10) };
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64)); analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        analytics.Controls.Add(ChartCard("Dòng tiền 6 tháng", "Tổng tiền thu theo tháng", _revenue), 0, 0); analytics.Controls.Add(ChartCard("Trạng thái lịch", "Phân bổ lịch chụp hiện tại", _status), 1, 0);

        var upcomingCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(18) };
        upcomingCard.Controls.Add(_grid); upcomingCard.Controls.Add(new Label { Text = "Lịch chụp sắp tới", Dock = DockStyle.Top, Height = 40, Font = Theme.Font(13, FontStyle.Bold), ForeColor = Theme.Text });

        Controls.Add(upcomingCard); Controls.Add(analytics); Controls.Add(cards); Controls.Add(header); Controls.Add(_skeleton); _skeleton.BringToFront(); Load += async (_, _) => await LoadAsync();
    }

    private static void AddCard(TableLayoutPanel host, Control card, int column) { card.Dock = DockStyle.Fill; card.Margin = new Padding(column == 0 ? 0 : 7, 4, column == 3 ? 0 : 7, 6); host.Controls.Add(card, column, 0); }
    private static Control ChartCard(string title, string subtitle, Control chart)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(18), Margin = new Padding(0, 0, 8, 0) };
        card.Controls.Add(chart); card.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Top, Height = 24, Font = Theme.Font(8.8f), ForeColor = Theme.Muted }); card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 30, Font = Theme.Font(12, FontStyle.Bold), ForeColor = Theme.Text }); return card;
    }

    private async Task LoadAsync()
    {
        _skeleton.Start();
        try
        {
            var dataTask = _app.Repository.GetDashboardAsync(); await Task.WhenAll(dataTask, Task.Delay(520)); var d = await dataTask;
            _today.Value = d.LichHomNay.ToString(); _today.Trend = "Trong ngày hôm nay"; _upcoming.Value = d.LichSapToi.ToString(); _upcoming.Trend = "Đang chờ thực hiện"; _processing.Value = d.DangXuLy.ToString(); _processing.Trend = $"{d.ChoGiao} lịch chờ giao ảnh"; _debt.Value = $"{d.CongNo:N0} đ"; _debt.Trend = "Cần theo dõi thanh toán";
            _revenue.Data = d.DoanhThu6Thang.Select(x => (x.Thang, x.ThucThu)).ToList(); _status.Data = d.TheoTrangThai;
            _grid.DataSource = d.LichGanNhat.Select(x => new { x.Ma, x.KhachHang, Bắt_đầu = x.BatDau.ToString("dd/MM/yyyy HH:mm"), x.NhiepAnhGia, Phòng = x.Phong, Trạng_thái = x.TrangThai.HienThi() }).ToList();
            _updated.Text = "Cập nhật " + DateTime.Now.ToString("HH:mm");
        }
        catch (Exception ex) { Ui.Error(this, ex.Message); }
        finally { _skeleton.Stop(); }
    }
}
