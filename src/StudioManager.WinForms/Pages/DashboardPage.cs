using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class DashboardPage : UserControl
{
    private readonly AppFacade _app;
    private DashboardData? _initialData;

    private readonly StudioPulseRibbon _pulseRibbon = new() { Dock = DockStyle.Fill };
    private readonly RevenueBarChart _revenueChart = new() { Dock = DockStyle.Fill };
    private readonly StatusDonutChart _statusChart = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _recentGrid = Theme.Grid();

    private readonly Label _revenueSummaryBadge = new()
    {
        Dock = DockStyle.Right,
        Width = 190,
        Font = Theme.Font(8.8f, FontStyle.Bold),
        ForeColor = Theme.Primary,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "Tháng này: 0 đ"
    };

    private readonly SegmentedTabBar _callSheetTabs = new()
    {
        Dock = DockStyle.Top,
        Height = 38
    };

    private readonly FlowLayoutPanel _callSheetList = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        BackColor = Theme.Surface,
        Padding = new Padding(0, 6, 0, 6)
    };

    private readonly SkeletonLoader _skeleton = new() { Dock = DockStyle.Fill };
    private IReadOnlyList<LichChup> _cachedUpcoming = [];
    private string _activeFeedMode = "callsheet";

    public event EventHandler? ViewBookingsRequested;

    public DashboardPage(AppFacade app, DashboardData? initialData = null)
    {
        _app = app;
        _initialData = initialData;
        BackColor = Theme.Background;

        var rootSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        rootSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67f));
        rootSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        rootSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        rootSplit.Controls.Add(BuildLeftCommandColumn(), 0, 0);
        rootSplit.Controls.Add(BuildRightCallSheetColumn(), 1, 0);

        Controls.Add(rootSplit);
        Controls.Add(_skeleton);
        _skeleton.BringToFront();
        _skeleton.BringToFront();

        ConfigureRecentGridStyling();
        _callSheetList.ClientSizeChanged += (_, _) => FitCallSheetCards();
        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildLeftCommandColumn()
    {
        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0, 0, 12, 0),
            BackColor = Color.Transparent
        };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // 0: Welcome & Quick Actions Bar
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 108)); // 1: 5-Metric Studio Pulse Telemetry Ribbon
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 252)); // 2: Hybrid Revenue Chart + Workflow Funnel
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 3: Recent Bookings Matrix

        left.Controls.Add(BuildTopGreetingBar(), 0, 0);

        var pulseHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 10), BackColor = Color.Transparent };
        pulseHost.Controls.Add(_pulseRibbon);
        left.Controls.Add(pulseHost, 0, 1);

        var middleCharts = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 0, 0, 10),
            BackColor = Color.Transparent
        };
        middleCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        middleCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        middleCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var revCard = CreateHeaderCard("Biểu đồ Thực thu 6 Tháng", _revenueChart, _revenueSummaryBadge);
        revCard.Margin = new Padding(0, 0, 6, 0);

        var statusCard = CreateHeaderCard("Phễu Trạng thái Lịch chụp", _statusChart);
        statusCard.Margin = new Padding(6, 0, 0, 0);

        middleCharts.Controls.Add(revCard, 0, 0);
        middleCharts.Controls.Add(statusCard, 1, 0);
        left.Controls.Add(middleCharts, 0, 2);

        var openAllBtn = Theme.GhostButton("Mở sổ điều phối lịch chụp", IconKind.ArrowRight);
        openAllBtn.Dock = DockStyle.Right;
        openAllBtn.Click += (_, _) => ViewBookingsRequested?.Invoke(this, EventArgs.Empty);

        var gridCard = CreateHeaderCard("Danh sách Lịch chụp & Đơn hàng Gần nhất", _recentGrid, openAllBtn);
        left.Controls.Add(gridCard, 0, 3);

        return left;
    }

    private Control BuildTopGreetingBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(2, 0, 0, 6)
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 340,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        var openBookingsBtn = Theme.Button("Điều phối Lịch chụp", Theme.Primary, IconKind.Calendar);
        openBookingsBtn.Click += (_, _) => ViewBookingsRequested?.Invoke(this, EventArgs.Empty);

        var refreshBtn = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refreshBtn.Click += async (_, _) => await LoadAsync();

        actions.Controls.Add(openBookingsBtn);
        actions.Controls.Add(refreshBtn);

        var greeting = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Xin chào {_app.Session?.HoTen ?? "Studio"}  •  {DateTime.Today:dddd, dd/MM/yyyy}",
            Font = Theme.Font(11.0f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        bar.Controls.Add(greeting);
        bar.Controls.Add(actions);
        return bar;
    }

    private Control BuildRightCallSheetColumn()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 14),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 12,
            AccentStripeColor = Theme.Primitives.Espresso900,
            AccentStripeThickness = 4
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 6)
        };

        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 16,
            Text = "STUDIO CALL SHEET & LIVE STREAM",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Lịch trực phòng máy & Hoạt động",
            Font = Theme.Font(11.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        header.Controls.Add(title);
        header.Controls.Add(overline);

        _callSheetTabs.SetItems([
            new("callsheet", "Call Sheet Ca chụp", IconKind.Camera),
            new("activity", "Nhật ký Vận hành", IconKind.History)
        ], "callsheet");

        _callSheetTabs.SegmentSelected += (_, seg) =>
        {
            _activeFeedMode = seg.Key;
            RenderCallSheetItems();
        };

        var footerBtn = Theme.OutlineButton("Chuyển tới Điều phối Lịch chụp đầy đủ", Theme.BorderStrong, IconKind.ArrowRight);
        footerBtn.Dock = DockStyle.Bottom;
        footerBtn.Height = 38;
        footerBtn.Click += (_, _) => ViewBookingsRequested?.Invoke(this, EventArgs.Empty);

        var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

        card.Controls.Add(_callSheetList);
        card.Controls.Add(spacer);
        card.Controls.Add(_callSheetTabs);
        card.Controls.Add(header);
        card.Controls.Add(footerBtn);

        return card;
    }

    private static CardPanel CreateHeaderCard(string title, Control body, Control? rightHeaderControl = null)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            CornerRadius = 10
        };

        var head = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 6)
        };

        var titleLbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = Theme.Font(10.2f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        head.Controls.Add(titleLbl);
        if (rightHeaderControl is not null)
        {
            head.Controls.Add(rightHeaderControl);
            rightHeaderControl.BringToFront();
        }

        card.Controls.Add(body);
        card.Controls.Add(head);
        return card;
    }

    private void ConfigureRecentGridStyling()
    {
        _recentGrid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var colName = _recentGrid.Columns[e.ColumnIndex].Name;
            if (colName != "Trạng_thái") return;

            e.PaintBackground(e.ClipBounds, true);
            var statusText = e.FormattedValue?.ToString() ?? "";
            if (!string.IsNullOrEmpty(statusText) && e.Graphics is { } g)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var (fg, bg, border) = Theme.GetStatusColors(statusText);
                using var font = Theme.Font(8.0f, FontStyle.Bold);
                var sz = TextRenderer.MeasureText(g, statusText, font);
                var pillW = Math.Min(e.CellBounds.Width - 12, sz.Width + 18);
                var pillH = 24;
                var pillRect = new RectangleF(
                    e.CellBounds.Left + 8,
                    e.CellBounds.Top + (e.CellBounds.Height - pillH) / 2f,
                    pillW,
                    pillH);

                using var path = Theme.RoundedRectangle(pillRect, 12f);
                using var fill = new SolidBrush(bg);
                using var pen = new Pen(border, 1f);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);

                TextRenderer.DrawText(g, statusText, font, Rectangle.Round(pillRect), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            e.Handled = true;
        };
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
        var currentMonthRev = d.DoanhThu6Thang.LastOrDefault().ThucThu;
        _revenueSummaryBadge.Text = $"Tháng này: {currentMonthRev:N0} đ";

        _pulseRibbon.SetMetrics([
            new("CA CHỤP HÔM NAY", d.LichHomNay.ToString(), "Lịch trong ngày", Theme.Primary, IconKind.Camera),
            new("LỊCH SẮP TỚI", d.LichSapToi.ToString(), "Đã đặt trước", Theme.Primitives.Plum600, IconKind.Calendar),
            new("ĐANG HẬU KỲ", d.DangXuLy.ToString(), "Chụp & chỉnh sửa", Theme.Warning, IconKind.Sparkles),
            new("CHỜ GIAO ẢNH", d.ChoGiao.ToString(), "Sẵn sàng bàn giao", Theme.Cyan, IconKind.Check),
            new("CÔNG NỢ CẦN THU", $"{d.CongNo:N0} đ", "Cần theo dõi thu", Theme.Danger, IconKind.Wallet)
        ]);

        _revenueChart.Data = d.DoanhThu6Thang.Select(x => (x.Thang, x.ThucThu)).ToList();
        _statusChart.Data = d.TheoTrangThai;

        _cachedUpcoming = d.LichGanNhat.OrderBy(x => x.BatDau).Take(8).ToList();
        RenderCallSheetItems();

        _recentGrid.DataSource = _cachedUpcoming.Select(x => new
        {
            Mã_đơn = x.Ma,
            Khách_hàng = x.KhachHang,
            Dịch_vụ = x.TenGoiChot,
            Phòng_và_Ekip = $"{x.Phong} • {x.NhiepAnhGia}",
            Ngày_chụp = x.BatDau.ToString("dd/MM/yyyy HH:mm"),
            Trạng_thái = x.TrangThai.HienThi(),
            Tổng_tiền = $"{x.TongThanhToan:N0} đ"
        }).ToList();

        if (_recentGrid.Columns.Contains("Mã_đơn"))
        {
            _recentGrid.Columns["Mã_đơn"].HeaderText = "Mã lịch";
            _recentGrid.Columns["Mã_đơn"].FillWeight = 78;
        }
        if (_recentGrid.Columns.Contains("Khách_hàng")) _recentGrid.Columns["Khách_hàng"].HeaderText = "Khách hàng";
        if (_recentGrid.Columns.Contains("Dịch_vụ")) _recentGrid.Columns["Dịch_vụ"].HeaderText = "Gói chụp";
        if (_recentGrid.Columns.Contains("Phòng_và_Ekip")) _recentGrid.Columns["Phòng_và_Ekip"].HeaderText = "Phòng • Nhiếp ảnh";
        if (_recentGrid.Columns.Contains("Ngày_chụp")) _recentGrid.Columns["Ngày_chụp"].HeaderText = "Khung giờ";
        if (_recentGrid.Columns.Contains("Trạng_thái"))
        {
            _recentGrid.Columns["Trạng_thái"].HeaderText = "Trạng thái";
            _recentGrid.Columns["Trạng_thái"].FillWeight = 115;
        }
        if (_recentGrid.Columns.Contains("Tổng_tiền"))
        {
            _recentGrid.Columns["Tổng_tiền"].HeaderText = "Tổng thanh toán";
            _recentGrid.Columns["Tổng_tiền"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }

    private void RenderCallSheetItems()
    {
        _callSheetList.SuspendLayout();
        _callSheetList.Controls.Clear();

        if (_cachedUpcoming.Count == 0)
        {
            var empty = new Label
            {
                Width = Math.Max(220, _callSheetList.ClientSize.Width - 12),
                Height = 120,
                Text = "Chưa có ca chụp hoặc hoạt động nào trong danh sách gần đây.",
                Font = Theme.Font(9.0f),
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _callSheetList.Controls.Add(empty);
        }
        else
        {
            foreach (var item in _cachedUpcoming.Take(6))
            {
                var card = new CallSheetTicketControl(item, isActivityMode: _activeFeedMode == "activity")
                {
                    Width = Math.Max(220, _callSheetList.ClientSize.Width - 18),
                    Height = 76,
                    Margin = new Padding(0, 0, 0, 8)
                };
                card.Click += (_, _) => ViewBookingsRequested?.Invoke(this, EventArgs.Empty);
                _callSheetList.Controls.Add(card);
            }
        }

        _callSheetList.ResumeLayout(true);
        FitCallSheetCards();
    }

    private void FitCallSheetCards()
    {
        var w = Math.Max(200, _callSheetList.ClientSize.Width - (_callSheetList.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth + 4 : 6));
        foreach (Control c in _callSheetList.Controls) c.Width = w;
    }
}

/// <summary>
/// 5-Metric Horizontal Studio Telemetry Ribbon Bar.
/// </summary>
internal sealed class StudioPulseRibbon : CardPanel
{
    public sealed record PulseItem(string Label, string Value, string Subtitle, Color Accent, IconKind Icon);

    private readonly List<PulseItem> _items = [];

    public StudioPulseRibbon()
    {
        CornerRadius = 10;
        BackColor = Theme.Surface;
        BorderColor = Theme.BorderStrong;
        AccentStripeColor = Theme.Primary;
        AccentStripeThickness = 3;
        Padding = new Padding(8);
    }

    public void SetMetrics(IEnumerable<PulseItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_items.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var slotW = (Width - 16f) / _items.Count;
        using var dividerPen = new Pen(Theme.Border, 1f);
        using var lblFont = Theme.Font(7.6f, FontStyle.Bold);
        using var valFont = Theme.Font(15.5f, FontStyle.Bold);
        using var subFont = Theme.Font(7.8f, FontStyle.Bold);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        using var textBrush = new SolidBrush(Theme.Text);

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var x = 8f + i * slotW;

            if (i > 0)
            {
                g.DrawLine(dividerPen, x, 16, x, Height - 14);
            }

            // Icon badge
            var iconBox = new RectangleF(x + 12, 16, 32, 32);
            using (var p = Theme.RoundedRectangle(iconBox, 8f))
            using (var fill = new SolidBrush(Color.FromArgb(24, item.Accent)))
            {
                g.FillPath(fill, p);
            }
            VectorIcons.Draw(g, item.Icon, new RectangleF(iconBox.Left + 7, iconBox.Top + 7, 18, 18), item.Accent, 1.8f);

            var textX = x + 52;
            var maxTextW = Math.Max(40, (int)slotW - 58);

            TextRenderer.DrawText(g, item.Label, lblFont, new Rectangle((int)textX, 13, maxTextW, 16),
                Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Responsive font scaling to prevent currency / metric value truncation (P2-05 / VD05)
            var currentFontSize = 15.5f;
            while (currentFontSize > 9.0f)
            {
                using var testFont = Theme.Font(currentFontSize, FontStyle.Bold);
                if (TextRenderer.MeasureText(g, item.Value, testFont).Width <= maxTextW)
                    break;
                currentFontSize -= 1.0f;
            }
            using (var itemValFont = Theme.Font(currentFontSize, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, item.Value, itemValFont, new Rectangle((int)textX, 30, maxTextW, 28),
                    Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            TextRenderer.DrawText(g, item.Subtitle, subFont, new Rectangle((int)textX, 60, maxTextW, 16),
                item.Accent, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}

/// <summary>
/// Custom GDI+ Studio Call Sheet Ticket Card.
/// </summary>
internal sealed class CallSheetTicketControl : Control
{
    private readonly LichChup _item;
    private readonly bool _isActivityMode;
    private bool _hovered;

    public CallSheetTicketControl(LichChup item, bool isActivityMode)
    {
        _item = item;
        _isActivityMode = isActivityMode;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        BackColor = Theme.Surface;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var statusText = _item.TrangThai.HienThi();
        var (statusFg, statusBg, statusBorder) = Theme.GetStatusColors(statusText);

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using var path = Theme.RoundedRectangle(bounds, 9f);
        using var fill = new SolidBrush(_hovered ? Theme.PrimaryLight : Theme.SurfaceWarm);
        using var border = new Pen(_hovered ? Theme.Primary : Theme.Border, 1f);
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        // Left vertical status accent stripe
        var state = g.Save();
        g.SetClip(path);
        using (var stripe = new SolidBrush(statusFg))
        {
            g.FillRectangle(stripe, 0, 0, 4, Height);
        }
        g.Restore(state);

        using var timeFont = Theme.MonoFont(8.8f, FontStyle.Bold);
        using var dateFont = Theme.Font(7.6f, FontStyle.Bold);
        using var nameFont = Theme.Font(9.3f, FontStyle.Bold);
        using var metaFont = Theme.Font(8.2f);
        using var pillFont = Theme.Font(7.5f, FontStyle.Bold);

        if (!_isActivityMode)
        {
            // Left Time Box
            var timeBox = new Rectangle(12, 10, 62, Height - 20);
            TextRenderer.DrawText(g, _item.BatDau.ToString("HH:mm"), timeFont, new Rectangle(timeBox.Left, 12, 62, 20),
                Theme.Primary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, _item.BatDau.ToString("dd/MM"), dateFont, new Rectangle(timeBox.Left, 34, 62, 18),
                Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // Customer & Room/Photographer
            var contentX = 76;
            var contentW = Math.Max(60, Width - contentX - 96);
            TextRenderer.DrawText(g, _item.KhachHang, nameFont, new Rectangle(contentX, 11, contentW, 22),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var meta = $"{_item.Phong}  •  {_item.NhiepAnhGia}\n{_item.TenGoiChot}";
            TextRenderer.DrawText(g, meta, metaFont, new Rectangle(contentX, 33, contentW, 34),
                Theme.TextSecondary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
        }
        else
        {
            // Activity Mode: Code + Financial progress
            VectorIcons.Draw(g, IconKind.Clock, new RectangleF(14, 16, 18, 18), statusFg, 1.8f);
            var contentX = 40;
            var contentW = Math.Max(60, Width - contentX - 96);
            TextRenderer.DrawText(g, $"#{_item.Ma} — {_item.KhachHang}", nameFont, new Rectangle(contentX, 11, contentW, 22),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var fin = _item.ConLai > 0
                ? $"Đã thu {_item.DaThu:N0} đ  •  Còn nợ {_item.ConLai:N0} đ"
                : $"Đã tất toán đủ {_item.TongThanhToan:N0} đ";
            TextRenderer.DrawText(g, $"{_item.TenGoiChot}\n{fin}", metaFont, new Rectangle(contentX, 33, contentW, 34),
                _item.ConLai > 0 ? Theme.Warning : Theme.Success,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
        }

        // Right Status Pill
        var pillRect = new RectangleF(Width - 92, 12, 82, 22);
        using var pillPath = Theme.RoundedRectangle(pillRect, 11f);
        using var pillBg = new SolidBrush(statusBg);
        using var pillPen = new Pen(statusBorder, 1f);
        g.FillPath(pillBg, pillPath);
        g.DrawPath(pillPen, pillPath);
        TextRenderer.DrawText(g, statusText, pillFont, Rectangle.Round(pillRect), statusFg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
