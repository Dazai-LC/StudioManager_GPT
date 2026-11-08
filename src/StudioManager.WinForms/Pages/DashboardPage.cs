using StudioManager.Application.Services;
using StudioManager.Application;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class DashboardPage : UserControl
{
    private readonly AppFacade _app;
    private DashboardData? _initialData;

    private readonly StatCard _orders = new("TỔNG ĐƠN (THÁNG NÀY)", Theme.Primary, "📅");
    private readonly StatCard _revenueCard = new("DOANH THU (THÁNG NÀY)", Theme.Success, "₫");
    private readonly StatCard _projects = new("ĐANG THỰC HIỆN", Theme.Purple, "📷");
    private readonly StatCard _debt = new("CÔNG NỢ CẦN THU", Theme.Danger, "⚠");

    private readonly RevenueBarChart _revenue = new() { Dock = DockStyle.Fill };
    private readonly StatusDonutChart _status = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _grid = Theme.Grid();
    private readonly FlowLayoutPanel _schedule = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = Theme.Surface
    };
    private readonly FlowLayoutPanel _activity = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = Theme.Surface
    };

    private readonly SkeletonLoader _skeleton = new() { Dock = DockStyle.Fill };
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 8000, InitialDelay = 300, ReshowDelay = 150 };

    public event EventHandler? ViewBookingsRequested;

    public DashboardPage(AppFacade app, DashboardData? initialData = null)
    {
        _app = app;
        _initialData = initialData;
        BackColor = Theme.Background;
        AutoScroll = true;

        var scrollContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 840,
            Padding = new Padding(0, 0, 8, 20),
            BackColor = Color.Transparent
        };

        var header = BuildGreeting();

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 138,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 10),
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        AddCard(cards, _orders, 0);
        AddCard(cards, _revenueCard, 1);
        AddCard(cards, _projects, 2);
        AddCard(cards, _debt, 3);

        var analytics = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 320,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 0, 0, 12),
            BackColor = Color.Transparent
        };
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        analytics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));

        analytics.Controls.Add(CreateSectionCard("Doanh thu thực thu", _revenue, "Năm " + DateTime.Today.Year), 0, 0);
        analytics.Controls.Add(CreateSectionCard("Lịch hôm nay", _schedule, "Xem tất cả  →", OpenBookings), 1, 0);
        analytics.Controls.Add(CreateSectionCard("Phân bổ trạng thái", _status), 2, 0);

        var lower = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        lower.Controls.Add(CreateSectionCard("Đơn hàng gần đây", _grid, "Xem tất cả  →", OpenBookings), 0, 0);
        lower.Controls.Add(CreateSectionCard("Nhật ký hoạt động", _activity, "Xem tất cả  →", OpenBookings), 1, 0);

        scrollContainer.Controls.Add(lower);
        scrollContainer.Controls.Add(analytics);
        scrollContainer.Controls.Add(cards);
        scrollContainer.Controls.Add(header);

        Controls.Add(scrollContainer);
        Controls.Add(_skeleton);
        _skeleton.BringToFront();

        _schedule.ClientSizeChanged += (_, _) => ResizeRows(_schedule);
        _activity.ClientSizeChanged += (_, _) => ResizeRows(_activity);
        _grid.CellMouseEnter += GridCellMouseEnter;

        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildGreeting()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 8)
        };

        var dateCard = new CardPanel
        {
            Dock = DockStyle.Right,
            Width = 240,
            Height = 44,
            Padding = new Padding(12, 8, 12, 8),
            CornerRadius = 10,
            BorderColor = Theme.Border
        };

        var dateLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "📅   " + DateTime.Today.ToString("dddd, dd/MM/yyyy"),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(9.0f, FontStyle.Bold),
            BackColor = Color.Transparent
        };
        dateCard.Controls.Add(dateLabel);

        var copy = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Chào buổi {DayPart()}, {_app.Session!.HoTen} 👋\nTổng quan hoạt động và tiến độ lịch chụp trong hệ thống.",
            ForeColor = Theme.Text,
            Font = Theme.Font(10.2f),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        header.Controls.Add(copy);
        header.Controls.Add(dateCard);
        return header;
    }

    private static string DayPart() => DateTime.Now.Hour < 12 ? "sáng" : DateTime.Now.Hour < 18 ? "chiều" : "tối";

    private static void AddCard(TableLayoutPanel host, Control card, int column)
    {
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(column == 0 ? 0 : 6, 2, column == 3 ? 0 : 6, 6);
        host.Controls.Add(card, column, 0);
    }

    private static Control CreateSectionCard(string title, Control content, string action = "", Action? onAction = null)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 14),
            Margin = new Padding(0, 0, 10, 0),
            CornerRadius = 12,
            BorderColor = Theme.Border
        };

        var head = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 6)
        };

        head.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        });

        if (action.Length > 0 && onAction is null)
        {
            head.Controls.Add(new Label
            {
                Dock = DockStyle.Right,
                Width = 120,
                Text = action,
                Font = Theme.Font(8.5f),
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            });
        }
        else if (action.Length > 0 && onAction is not null)
        {
            var link = new LinkLabel
            {
                Dock = DockStyle.Right,
                Width = 120,
                Text = action,
                Font = Theme.Font(8.8f, FontStyle.Bold),
                LinkColor = Theme.Primary,
                ActiveLinkColor = Theme.PrimaryHover,
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand,
                TabStop = true,
                BackColor = Color.Transparent
            };
            link.LinkClicked += (_, _) => onAction();
            head.Controls.Add(link);
        }

        card.Controls.Add(content);
        card.Controls.Add(head);
        return card;
    }

    private async Task LoadAsync()
    {
        if (_initialData is not null)
        {
            var initial = _initialData;
            _initialData = null;
            Bind(initial);
            _skeleton.Stop();
            return;
        }

        _skeleton.Start();
        try
        {
            var d = await _app.Dashboard.LoadAsync();
            Bind(d);
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
        finally
        {
            _skeleton.Stop();
        }
    }

    private void Bind(DashboardData d)
    {
        var monthlyRevenue = d.DoanhThu6Thang.LastOrDefault().ThucThu;

        _orders.Value = (d.LichHomNay + d.LichSapToi + d.DangXuLy).ToString();
        _orders.Trend = "↑ Cập nhật trực tiếp";

        _revenueCard.Value = $"{monthlyRevenue:N0} đ";
        _revenueCard.Trend = "↑ Thực thu tháng này";

        _projects.Value = (d.DangXuLy + d.ChoGiao).ToString();
        _projects.Trend = $"↑ {d.ChoGiao} lịch chờ giao";

        _debt.Value = $"{d.CongNo:N0} đ";
        _debt.Trend = "Cần theo dõi thu nợ";

        _revenue.Data = d.DoanhThu6Thang.Select(x => (x.Thang, x.ThucThu)).ToList();
        _status.Data = d.TheoTrangThai;

        var upcoming = d.LichGanNhat.OrderBy(x => x.BatDau).Take(5).ToList();

        _schedule.Controls.Clear();
        foreach (var x in upcoming)
            _schedule.Controls.Add(ScheduleRow(x));

        _activity.Controls.Clear();
        foreach (var x in upcoming.Take(5))
            _activity.Controls.Add(ActivityRow(x));

        ResizeRows(_schedule);
        ResizeRows(_activity);

        _grid.DataSource = upcoming.Select(x => new
        {
            Mã_đơn = x.Ma,
            Khách_hàng = x.KhachHang,
            Dịch_vụ = x.TenGoiChot,
            Ngày_chụp = x.BatDau.ToString("dd/MM/yyyy"),
            Trạng_thái = x.TrangThai.HienThi(),
            Tổng_tiền = $"{x.TongThanhToan:N0} đ"
        }).ToList();

        // Custom grid column formatting
        if (_grid.Columns.Contains("Mã_đơn")) _grid.Columns["Mã_đơn"].HeaderText = "Mã đơn";
        if (_grid.Columns.Contains("Khách_hàng")) _grid.Columns["Khách_hàng"].HeaderText = "Khách hàng";
        if (_grid.Columns.Contains("Dịch_vụ")) _grid.Columns["Dịch_vụ"].HeaderText = "Gói chụp";
        if (_grid.Columns.Contains("Ngày_chụp")) _grid.Columns["Ngày_chụp"].HeaderText = "Ngày chụp";
        if (_grid.Columns.Contains("Trạng_thái")) _grid.Columns["Trạng_thái"].HeaderText = "Trạng thái";
        if (_grid.Columns.Contains("Tổng_tiền"))
        {
            _grid.Columns["Tổng_tiền"].HeaderText = "Tổng tiền";
            _grid.Columns["Tổng_tiền"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }

    private Control ScheduleRow(LichChup x)
    {
        var row = new TableLayoutPanel
        {
            Width = 320,
            Height = 54,
            Margin = new Padding(0, 2, 0, 4),
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

        var timeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = x.BatDau.ToString("HH:mm"),
            Font = Theme.Font(9.0f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var detail = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Text = $"{x.KhachHang}\n{x.TenGoiChot}",
            Font = Theme.Font(8.5f),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 4, 0),
            BackColor = Color.Transparent
        };
        _toolTip.SetToolTip(detail, $"{x.KhachHang} — {x.TenGoiChot}\nThời gian: {x.BatDau:HH:mm} - {x.KetThuc:HH:mm}");

        var colors = Theme.GetStatusColors(x.TrangThai.HienThi());
        var badge = new Label
        {
            Dock = DockStyle.Fill,
            Text = x.TrangThai.HienThi(),
            AutoEllipsis = false,
            Font = Theme.Font(7.5f, FontStyle.Bold),
            ForeColor = colors.Text,
            BackColor = colors.Background,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(4, 8, 8, 8)
        };
        badge.Resize += (_, _) => Theme.Round(badge, 6);

        row.Controls.Add(timeLabel, 0, 0);
        row.Controls.Add(detail, 1, 0);
        row.Controls.Add(badge, 2, 0);
        row.Resize += (_, _) => Theme.Round(row, 8);

        return row;
    }

    private Control ActivityRow(LichChup x)
    {
        var row = new TableLayoutPanel
        {
            Width = 420,
            Height = 58,
            Margin = new Padding(0, 2, 0, 4),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var icon = new Label
        {
            Dock = DockStyle.Fill,
            Text = "✓",
            BackColor = Color.FromArgb(236, 253, 245),
            ForeColor = Theme.Success,
            Font = Theme.Font(12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(6, 8, 4, 8)
        };
        icon.Resize += (_, _) => Theme.Round(icon, 8);

        var text = $"Lịch #{x.Ma}  •  {x.TrangThai.HienThi()}\nKhách: {x.KhachHang}  —  {x.BatDau:dd/MM HH:mm}";
        var detail = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 2, 4, 0),
            Text = text,
            AutoEllipsis = true,
            Font = Theme.Font(8.4f),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _toolTip.SetToolTip(detail, text);

        row.Controls.Add(icon, 0, 0);
        row.Controls.Add(detail, 1, 0);
        row.Resize += (_, _) => Theme.Round(row, 8);

        return row;
    }

    private void OpenBookings() => ViewBookingsRequested?.Invoke(this, EventArgs.Empty);

    private static void ResizeRows(FlowLayoutPanel panel)
    {
        var width = Math.Max(180, panel.ClientSize.Width - (panel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - 6);
        foreach (Control row in panel.Controls) row.Width = width;
    }

    private void GridCellMouseEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.ToolTipText = cell.FormattedValue?.ToString() ?? "";
    }
}
