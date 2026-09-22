using StudioManager.Application.Services;
using StudioManager.Application;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class DashboardPage : UserControl
{
    private readonly AppFacade _app;
    private DashboardData? _initialData;
    private readonly StatCard _orders = new("TỔNG ĐƠN (THÁNG NÀY)", Theme.Primary, "▣");
    private readonly StatCard _revenueCard = new("DOANH THU (THÁNG NÀY)", Theme.Success, "$ ");
    private readonly StatCard _projects = new("DỰ ÁN ĐANG THỰC HIỆN", Theme.Purple, "▥");
    private readonly StatCard _debt = new("CÔNG NỢ", Theme.Danger, "!");
    private readonly RevenueBarChart _revenue = new() { Dock = DockStyle.Fill };
    private readonly StatusDonutChart _status = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _grid = Theme.Grid();
    private readonly FlowLayoutPanel _schedule = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White };
    private readonly FlowLayoutPanel _activity = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White };
    private readonly SkeletonLoader _skeleton = new() { Dock = DockStyle.Fill };
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 8000, InitialDelay = 350, ReshowDelay = 150 };
    public event EventHandler? ViewBookingsRequested;

    public DashboardPage(AppFacade app, DashboardData? initialData = null)
    {
        _app = app; _initialData = initialData; BackColor = Theme.Background; AutoScroll = true;
        var body = new Panel { Dock = DockStyle.Top, Height = 790 };
        var header = BuildGreeting();
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 140, ColumnCount = 4, Padding = new Padding(0, 4, 0, 8) };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        AddCard(cards, _orders, 0); AddCard(cards, _revenueCard, 1); AddCard(cards, _projects, 2); AddCard(cards, _debt, 3);

        var analytics = new TableLayoutPanel { Dock = DockStyle.Top, Height = 310, ColumnCount = 3, Padding = new Padding(0, 0, 0, 10) };
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29)); analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        analytics.Controls.Add(Card("Doanh thu theo tháng", _revenue, "Năm " + DateTime.Today.Year), 0, 0);
        analytics.Controls.Add(Card("Lịch hôm nay", _schedule, "Xem tất cả  →", OpenBookings), 1, 0);
        analytics.Controls.Add(Card("Phân bổ lịch theo trạng thái", _status), 2, 0);

        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62)); lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        lower.Controls.Add(Card("Đơn hàng gần đây", _grid, "Xem tất cả  →", OpenBookings), 0, 0);
        lower.Controls.Add(Card("Hoạt động gần đây", _activity, "Xem tất cả  →", OpenBookings), 1, 0);
        body.Controls.Add(lower); body.Controls.Add(analytics); body.Controls.Add(cards); body.Controls.Add(header);
        Controls.Add(body); Controls.Add(_skeleton); _skeleton.BringToFront();
        _schedule.ClientSizeChanged += (_, _) => ResizeRows(_schedule); _activity.ClientSizeChanged += (_, _) => ResizeRows(_activity);
        _grid.CellMouseEnter += GridCellMouseEnter;
        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildGreeting()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 80 };
        var date = new Label { Dock = DockStyle.Right, Width = 230, Text = "▣   " + DateTime.Today.ToString("dddd, dd/MM/yyyy"), TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.Muted, Font = Theme.Font(9.5f) };
        var copy = new Label { Dock = DockStyle.Fill, Text = $"Chào buổi {DayPart()}, {_app.Session!.HoTen} 👋\nChúc bạn có một ngày làm việc hiệu quả!", ForeColor = Theme.Text, Font = Theme.Font(10), TextAlign = ContentAlignment.MiddleLeft };
        header.Controls.Add(copy); header.Controls.Add(date); return header;
    }

    private static string DayPart() => DateTime.Now.Hour < 12 ? "sáng" : DateTime.Now.Hour < 18 ? "chiều" : "tối";
    private static void AddCard(TableLayoutPanel host, Control card, int column) { card.Dock = DockStyle.Fill; card.Margin = new Padding(column == 0 ? 0 : 7, 4, column == 3 ? 0 : 7, 6); host.Controls.Add(card, column, 0); }
    private static Control Card(string title, Control content, string action = "", Action? onAction = null)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16), Margin = new Padding(0, 0, 10, 0) };
        var head = new Panel { Dock = DockStyle.Top, Height = 36 };
        head.Controls.Add(new Label { Dock = DockStyle.Fill, Text = title, Font = Theme.Font(11, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft });
        if (action.Length > 0 && onAction is null) head.Controls.Add(new Label { Dock = DockStyle.Right, Width = 120, Text = action, Font = Theme.Font(8.5f), ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleRight });
        if (action.Length > 0 && onAction is not null)
        {
            var link = new LinkLabel { Dock = DockStyle.Right, Width = 120, Text = action, Font = Theme.Font(8.5f), LinkColor = Theme.Primary, ActiveLinkColor = Theme.Primary, TextAlign = ContentAlignment.MiddleRight, Cursor = Cursors.Hand, TabStop = true };
            link.LinkClicked += (_, _) => onAction(); head.Controls.Add(link);
        }
        card.Controls.Add(content); card.Controls.Add(head); return card;
    }

    private async Task LoadAsync()
    {
        if (_initialData is not null) { var initial = _initialData; _initialData = null; Bind(initial); _skeleton.Stop(); return; }
        _skeleton.Start();
        try
        {
            var task = _app.Repository.GetDashboardAsync(); await Task.WhenAll(task, Task.Delay(520)); var d = await task;
            Bind(d);
        }
        catch (Exception ex) { Ui.Error(this, ex.Message); }
        finally { _skeleton.Stop(); }
    }

    private void Bind(DashboardData d)
    {
        var monthlyRevenue = d.DoanhThu6Thang.LastOrDefault().ThucThu;
        _orders.Value = (d.LichHomNay + d.LichSapToi + d.DangXuLy).ToString(); _orders.Trend = "↑ Dữ liệu cập nhật trực tiếp";
        _revenueCard.Value = $"{monthlyRevenue:N0} đ"; _revenueCard.Trend = "↑ Thực thu trong tháng";
        _projects.Value = (d.DangXuLy + d.ChoGiao).ToString(); _projects.Trend = $"↑ {d.ChoGiao} lịch chờ giao";
        _debt.Value = $"{d.CongNo:N0} đ"; _debt.Trend = "Cần theo dõi thanh toán";
        _revenue.Data = d.DoanhThu6Thang.Select(x => (x.Thang, x.ThucThu)).ToList(); _status.Data = d.TheoTrangThai;
        var upcoming = d.LichGanNhat.OrderBy(x => x.BatDau).Take(5).ToList();
        _schedule.Controls.Clear(); foreach (var x in upcoming) _schedule.Controls.Add(ScheduleRow(x));
        _activity.Controls.Clear(); foreach (var x in upcoming.Take(5)) _activity.Controls.Add(ActivityRow(x));
        ResizeRows(_schedule); ResizeRows(_activity);
        _grid.DataSource = upcoming.Select(x => new { Mã_đơn = x.Ma, Khách_hàng = x.KhachHang, Dịch_vụ = x.TenGoiChot, Ngày_chụp = x.BatDau.ToString("dd/MM/yyyy"), Trạng_thái = x.TrangThai.HienThi(), Tổng_tiền = $"{x.TongThanhToan:N0} đ" }).ToList();
    }

    private Control ScheduleRow(LichChup x)
    {
        var row = new TableLayoutPanel { Width = 320, Height = 54, Margin = new Padding(0, 1, 0, 3), ColumnCount = 3, RowCount = 1 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        var detail = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, Text = x.TenGoiChot + "\n" + x.KhachHang, Font = Theme.Font(8.4f), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(3, 0, 4, 0) };
        _toolTip.SetToolTip(detail, x.TenGoiChot + Environment.NewLine + x.KhachHang);
        row.Controls.Add(new Label { Dock = DockStyle.Fill, Text = x.BatDau.ToString("HH:mm"), Font = Theme.Font(8.7f, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        row.Controls.Add(detail, 1, 0); row.Controls.Add(new Label { Dock = DockStyle.Fill, Text = x.TrangThai.HienThi(), AutoEllipsis = true, Font = Theme.Font(7.6f), ForeColor = Theme.Primary, BackColor = Theme.PrimaryLight, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(4, 5, 0, 5) }, 2, 0); return row;
    }

    private Control ActivityRow(LichChup x)
    {
        var row = new TableLayoutPanel { Width = 420, Height = 58, Margin = new Padding(0, 1, 0, 3), ColumnCount = 2, RowCount = 1 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var text = $"Lịch {x.Ma} · {x.TrangThai.HienThi()}\nKhách hàng: {x.KhachHang}  •  {x.BatDau:dd/MM HH:mm}";
        var detail = new Label { Dock = DockStyle.Fill, Padding = new Padding(10, 2, 2, 0), Text = text, AutoEllipsis = true, Font = Theme.Font(8.3f), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft };
        _toolTip.SetToolTip(detail, text); row.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "✓", BackColor = Color.FromArgb(228, 248, 240), ForeColor = Theme.Success, Font = Theme.Font(13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0, 5, 0, 5) }, 0, 0); row.Controls.Add(detail, 1, 0); return row;
    }

    private void OpenBookings() => ViewBookingsRequested?.Invoke(this, EventArgs.Empty);
    private static void ResizeRows(FlowLayoutPanel panel) { var width = Math.Max(180, panel.ClientSize.Width - (panel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - 4); foreach (Control row in panel.Controls) row.Width = width; }
    private void GridCellMouseEnter(object? sender, DataGridViewCellEventArgs e) { if (e.RowIndex < 0 || e.ColumnIndex < 0) return; var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex]; cell.ToolTipText = cell.FormattedValue?.ToString() ?? ""; }
}
