using System.Drawing.Drawing2D;
using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

/// <summary>
/// STUDIOMANAGER v2.2 — PHASE 01 FRONTEND FOUNDATION (UI LAYOUT HOTFIX)
/// Màn hình tiếp nhận Yêu cầu Đặt lịch Trực tuyến (Tab 2.2 Hub Lịch Chụp).
/// Chế độ: Presentation-only, Zero SQL Server writes, chuẩn nhận diện Atelier Lumière.
/// Đã tối ưu hóa bố cục: Responsive 1366x768 - 1680x896, loại bỏ triệt để hiện tượng xén chữ.
/// </summary>
public sealed class BookingRequestsPlaceholderPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _searchInput = new("Tìm theo mã yêu cầu, tên khách hàng, số điện thoại hoặc gói chụp...")
    {
        Dock = DockStyle.Fill,
        Height = 38
    };

    // Right-hand Live Dossier Controls
    private readonly Label _selectedCodeLabel = new()
    {
        Dock = DockStyle.Left,
        Width = 190,
        Font = Theme.Font(13.5f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _selectedStatusBadge = new()
    {
        Dock = DockStyle.Right,
        Width = 150,
        Font = Theme.MonoFont(8.0f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly Label _tableSummaryLabel = new()
    {
        Dock = DockStyle.Right,
        Width = 240,
        Font = Theme.MonoFont(8.0f, FontStyle.Bold),
        ForeColor = Theme.Primary,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "0 yêu cầu hiển thị"
    };

    // Dossier dynamic value labels
    private readonly Label _valCustomer = CreateValueLabel(isEmphasized: true);
    private readonly Label _valPhone = CreateValueLabel(isEmphasized: true, color: Theme.Primary);
    private readonly Label _valEmail = CreateValueLabel();
    private readonly Label _valChannel = CreateValueLabel();
    private readonly Label _valPackage = CreateValueLabel(isEmphasized: true, fontSize: 10.5f);
    private readonly Label _valDate = CreateValueLabel(isEmphasized: true);
    private readonly Label _valChamber = CreateValueLabel();
    private readonly Label _valDeposit = CreateValueLabel(isEmphasized: true, color: Theme.Success, fontSize: 11.5f);
    private readonly Label _valStyleNotes = CreateMultiLineValueLabel();
    private readonly Label _valOperationalNotes = CreateMultiLineValueLabel();

    // Telemetry KPI StatCards
    private StatCard _cardTotal = null!;
    private StatCard _cardPending = null!;
    private StatCard _cardReview = null!;
    private StatCard _cardConfirmed = null!;

    private sealed record RequestItem(
        string Code,
        string StatusCode,
        string StatusText,
        Color StatusColor,
        string Customer,
        string Phone,
        string Email,
        string Package,
        string PreferredDate,
        string Deposit,
        string SuggestedChamber,
        string StyleNotes,
        string OperationalNotes
    );

    // 5 approved booking-request states: CHO_XU_LY, CAN_BO_SUNG, DA_XAC_NHAN, DA_TU_CHOI, KHACH_HUY
    private readonly List<RequestItem> _allRequests =
    [
        new(
            "REQ-202611-001",
            "CHO_XU_LY",
            "Chờ xử lý",
            Theme.Warning,
            "Nguyễn Mai Anh",
            "0903 123 456",
            "maianh.nguyen@email.com",
            "Heritage Bridal Editorial",
            "20/11/2026 14:00",
            "2.000.000 đ",
            "Studio Chamber A (Dự kiến)",
            "Tone màu ấm Atelier, áo dài lụa truyền thống kết hợp ánh sáng nghệ thuật cổ điển.",
            "Yêu cầu mới gửi qua Customer Portal. Đặt lịch trực tuyến chưa giữ chỗ phòng chụp thực tế. Cần liên hệ tư vấn xác nhận concept."
        ),
        new(
            "REQ-202611-002",
            "CAN_BO_SUNG",
            "Cần bổ sung",
            Theme.Primitives.Teal600,
            "Trần Minh Quân",
            "0912 345 678",
            "quan.tm@fashionbrand.vn",
            "Lookbook Minimalist FW26",
            "22/11/2026 09:00",
            "1.500.000 đ",
            "Studio Chamber B (Dự kiến)",
            "Ánh sáng High-key hiện đại, 8 bộ trang phục thời trang thương hiệu cao cấp.",
            "Nhân viên studio đã liên hệ sơ bộ; chờ khách bổ sung số lượng người mẫu và danh sách trang phục chi tiết."
        ),
        new(
            "REQ-202611-003",
            "DA_XAC_NHAN",
            "Đã xác nhận",
            Theme.Success,
            "Lê Hoàng Yến",
            "0988 777 666",
            "yen.le@innovatecorp.com",
            "Chân dung Doanh nhân Signature",
            "25/11/2026 10:30",
            "1.000.000 đ",
            "Studio Chamber C (Dự kiến)",
            "Phông nền Dark Obsidian cao cấp, ánh sáng tạo khối tôn vinh phong thái lãnh đạo.",
            "Đã thống nhất concept và khung giờ; hướng dẫn khách quét mã VietQR cọc để chuyển đổi lịch chụp chính thức."
        ),
        new(
            "REQ-202611-004",
            "DA_TU_CHOI",
            "Đã từ chối",
            Theme.Danger,
            "Phạm Đăng Khoa",
            "0977 888 999",
            "khoa.pham@gmail.com",
            "Bộ ảnh Gia đình Festive",
            "28/11/2026 15:00",
            "2.500.000 đ",
            "Studio Chamber A (Dự kiến)",
            "Concept Noel ấm áp phong cách gia đình 4 người.",
            "Studio đã kín phòng Chamber A và toàn bộ ekip nhiếp ảnh gia khung giờ cuối tuần; đã liên hệ từ chối lịch sự và hẹn dịp khác."
        ),
        new(
            "REQ-202611-005",
            "KHACH_HUY",
            "Khách hủy",
            Theme.Muted,
            "Đỗ Thu Thảo",
            "0934 567 890",
            "thuthao.do@outlook.com",
            "Nghệ thuật Mỹ cảm Ánh sáng",
            "30/11/2026 13:00",
            "1.800.000 đ",
            "Studio Chamber B (Dự kiến)",
            "Phong cách Cinematic Dramatic Portrait kết hợp hiệu ứng khói và ánh sáng ven.",
            "Khách có lịch công tác đột xuất ở nước ngoài, chủ động hủy yêu cầu qua tài khoản Customer Portal."
        )
    ];

    private string _activeFilter = "ALL";

    public BookingRequestsPlaceholderPage(AppFacade app)
    {
        _app = app;
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Font = Theme.Font();
        Padding = new Padding(20, 14, 20, 16);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));   // Banner rộng rãi, không cấn chữ
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126f));  // 4 thẻ Telemetry KPI phân bổ đều
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Split Table (62%) / Dossier (38%)

        mainLayout.Controls.Add(BuildSimulationBanner(), 0, 0);
        mainLayout.Controls.Add(BuildTelemetryGrid(), 0, 1);
        mainLayout.Controls.Add(BuildWorkspaceSplit(), 0, 2);

        Controls.Add(mainLayout);
        SetupGridColumns();
        ApplyFilter();
    }

    private Control BuildSimulationBanner()
    {
        var banner = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(16, 8, 16, 8),
            CornerRadius = 8,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.Warning,
            AccentStripeColor = Theme.Warning,
            AccentStripeThickness = 3
        };

        var bannerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190f));
        bannerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var badgePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 8, 4)
        };
        var badgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "MÔ PHỎNG UI\nCHƯA KẾT NỐI BACKEND",
            Font = Theme.MonoFont(8.0f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Ochre700,
            TextAlign = ContentAlignment.MiddleLeft
        };
        badgePanel.Controls.Add(badgeLabel);

        var textPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 4, 2)
        };
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Hộp thư Tiếp nhận Yêu cầu Đặt lịch Trực tuyến — v2.2 Milestone (Customer Booking Hub)",
            Font = Theme.Font(9.6f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var descLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Giao diện minh họa quy trình thẩm tra & tiếp nhận yêu cầu từ Web Customer Portal (Phase 02/03). Yêu cầu trực tuyến chưa giữ chỗ phòng chụp thực tế. 0 thao tác ghi vào cơ sở dữ liệu sản xuất.",
            Font = Theme.Font(8.3f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        textPanel.Controls.Add(descLabel);
        textPanel.Controls.Add(titleLabel);

        var phaseLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "KIẾN TRÚC GIAI ĐOẠN 01\nĐIỀU HƯỚNG VẬN HÀNH",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleRight
        };

        bannerLayout.Controls.Add(badgePanel, 0, 0);
        bannerLayout.Controls.Add(textPanel, 1, 0);
        bannerLayout.Controls.Add(phaseLabel, 2, 0);

        banner.Controls.Add(bannerLayout);
        return banner;
    }

    private Control BuildTelemetryGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10),
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _cardTotal = new StatCard("Tổng yêu cầu", Theme.Primary, IconKind.Layers)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 6, 0),
            Value = "5",
            Trend = "Đồng bộ Portal trực tuyến"
        };

        _cardPending = new StatCard("Chờ xử lý (Mới)", Theme.Warning, IconKind.Clock)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 6, 0),
            Value = "1",
            Trend = "Cần studio thẩm tra"
        };

        _cardReview = new StatCard("Cần bổ sung", Theme.Primitives.Teal600, IconKind.Users)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 6, 0),
            Value = "1",
            Trend = "Đang liên hệ khách"
        };

        _cardConfirmed = new StatCard("Đã xác nhận", Theme.Success, IconKind.Check)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
            Value = "1",
            Trend = "Sẵn sàng lên lịch chụp"
        };

        grid.Controls.Add(_cardTotal, 0, 0);
        grid.Controls.Add(_cardPending, 1, 0);
        grid.Controls.Add(_cardReview, 2, 0);
        grid.Controls.Add(_cardConfirmed, 3, 0);
        return grid;
    }

    private Control BuildWorkspaceSplit()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        split.Controls.Add(BuildLeftTableSection(), 0, 0);
        split.Controls.Add(BuildRightDossierSection(), 1, 0);
        return split;
    }

    private Control BuildLeftTableSection()
    {
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.Transparent
        };

        var toolbar = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(12, 8, 12, 8),
            CornerRadius = 8,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };

        var toolLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        toolLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        toolLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        toolLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106f));
        toolLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _searchInput.TextChanged += (_, _) => ApplyFilter();

        var filterFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(6, 2, 0, 0)
        };

        var btnAll = Theme.OutlineButton("Tất cả (5)", Theme.BorderStrong);
        var btnPending = Theme.OutlineButton("Chờ xử lý (1)", Theme.Warning);
        var btnReview = Theme.OutlineButton("Cần bổ sung (1)", Theme.Primitives.Teal600);
        var btnConfirmed = Theme.OutlineButton("Đã xác nhận (1)", Theme.Success);
        var btnClosed = Theme.OutlineButton("Đã đóng (2)", Theme.BorderStrong);

        btnAll.Height = 32; btnPending.Height = 32; btnReview.Height = 32; btnConfirmed.Height = 32; btnClosed.Height = 32;
        btnAll.Click += (_, _) => { _activeFilter = "ALL"; ApplyFilter(); };
        btnPending.Click += (_, _) => { _activeFilter = "CHO_XU_LY"; ApplyFilter(); };
        btnReview.Click += (_, _) => { _activeFilter = "CAN_BO_SUNG"; ApplyFilter(); };
        btnConfirmed.Click += (_, _) => { _activeFilter = "DA_XAC_NHAN"; ApplyFilter(); };
        btnClosed.Click += (_, _) => { _activeFilter = "CLOSED"; ApplyFilter(); };

        filterFlow.Controls.Add(btnAll);
        filterFlow.Controls.Add(btnPending);
        filterFlow.Controls.Add(btnReview);
        filterFlow.Controls.Add(btnConfirmed);
        filterFlow.Controls.Add(btnClosed);

        var refreshBtn = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refreshBtn.Dock = DockStyle.Right;
        refreshBtn.Height = 32;
        refreshBtn.Width = 100;
        refreshBtn.Click += (_, _) => ApplyFilter();

        toolLayout.Controls.Add(_searchInput, 0, 0);
        toolLayout.Controls.Add(filterFlow, 1, 0);
        toolLayout.Controls.Add(refreshBtn, 2, 0);
        toolbar.Controls.Add(toolLayout);

        var tableCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };

        var tableHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 4, 4)
        };
        var tableTitle = new Label
        {
            Dock = DockStyle.Left,
            Width = 320,
            Text = "HÀNG ĐỢI YÊU CẦU ĐẶT LỊCH TRỰC TUYẾN",
            Font = Theme.MonoFont(8.0f, FontStyle.Bold),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        tableHeader.Controls.Add(tableTitle);
        tableHeader.Controls.Add(_tableSummaryLabel);

        tableCard.Controls.Add(_grid);
        tableCard.Controls.Add(tableHeader);

        container.Controls.Add(tableCard);
        container.Controls.Add(toolbar);
        return container;
    }

    private Control BuildRightDossierSection()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 14, 16, 14),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 3
        };

        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "DOSSIER CHI TIẾT  //  QUY TRÌNH THẨM TRA",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var headerRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 4)
        };
        headerRow.Controls.Add(_selectedStatusBadge);
        headerRow.Controls.Add(_selectedCodeLabel);

        // Scrollable content holding structured dossier sections and pipeline guide
        var scrollContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 4, 8)
        };

        var contentStack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        contentStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        contentStack.Controls.Add(BuildSectionCard("HỒ SƠ KHÁCH HÀNG & LIÊN HỆ", [
            ("Họ tên khách hàng", _valCustomer),
            ("Số điện thoại liên hệ", _valPhone),
            ("Hộp thư điện tử (Email)", _valEmail),
            ("Kênh tiếp nhận", _valChannel)
        ]), 0, 0);

        contentStack.Controls.Add(BuildSectionCard("DỰ KIẾN THỜI GIAN & KHÔNG GIAN", [
            ("Gói chụp quan tâm", _valPackage),
            ("Thời gian mong muốn", _valDate),
            ("Phòng đề xuất (Dự kiến)", _valChamber),
            ("Tiền cọc ước tính (50%)", _valDeposit)
        ]), 0, 1);

        contentStack.Controls.Add(BuildSectionCard("Ý TƯỞNG & PHONG CÁCH NGHỆ THUẬT (CONCEPT)", [
            ("Phong cách & Moodboard đề xuất", _valStyleNotes)
        ]), 0, 2);

        contentStack.Controls.Add(BuildSectionCard("GHI CHÚ THẨM TRA VẬN HÀNH STUDIO", [
            ("Tiến độ & Ghi chú tư vấn", _valOperationalNotes)
        ]), 0, 3);

        contentStack.Controls.Add(BuildPipelineGuideCard(), 0, 4);

        scrollContainer.Controls.Add(contentStack);

        card.Controls.Add(scrollContainer);
        card.Controls.Add(headerRow);
        card.Controls.Add(overline);

        return card;
    }

    private Control BuildSectionCard(string title, (string Label, Label ValueControl)[] fields)
    {
        var panel = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(12, 10, 12, 10),
            CornerRadius = 6,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.BorderSubtle
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = fields.Length * 2 + 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var sectionHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = title,
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(sectionHeader);

        for (var i = 0; i < fields.Length; i++)
        {
            var (lblText, valCtrl) = fields[i];

            var caption = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Text = lblText.ToUpperInvariant(),
                Font = Theme.Font(7.6f, FontStyle.Regular),
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, i == 0 ? 0 : 4, 0, 1)
            };

            layout.Controls.Add(caption);
            layout.Controls.Add(valCtrl);
        }

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildPipelineGuideCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(12, 10, 12, 10),
            CornerRadius = 6,
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 2
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = "Lộ trình Thẩm tra & Tiếp nhận (CTR-07..CTR-08):",
            Font = Theme.Font(8.6f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var guide = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Text = "1. Yêu cầu nộp trực tuyến: Khách chọn gói chụp & concept qua Customer Portal (Trạng thái: Chờ xử lý). Lưu ý: Chưa giữ chỗ phòng chụp thực tế.\n" +
                   "2. Thẩm tra & Tư vấn: Nhân viên studio gọi điện xác nhận concept hoặc yêu cầu bổ sung thông tin (Cần bổ sung).\n" +
                   "3. Thống nhất & Cọc: Chốt concept và hướng dẫn khách chuyển cọc qua VietQR (Đã xác nhận).\n" +
                   "4. Lập lịch chính thức: Thao tác 1-Click 'Chuyển thành Lịch chụp' tự động kiểm tra xung đột phòng máy và tạo bản ghi LichChup.",
            Font = Theme.Font(8.2f),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.TopLeft,
            Margin = new Padding(0, 4, 0, 6)
        };

        var warningBanner = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "QUY TẮC AN TOÀN: Yêu cầu đặt lịch không được tự động chiếm dụng phòng chụp.",
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Terracotta700,
            TextAlign = ContentAlignment.MiddleLeft
        };

        layout.Controls.Add(title);
        layout.Controls.Add(guide);
        layout.Controls.Add(warningBanner);

        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateValueLabel(bool isEmphasized = false, Color? color = null, float fontSize = 9.4f) => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Font = Theme.Font(fontSize, isEmphasized ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 0, 0, 5)
    };

    private static Label CreateMultiLineValueLabel() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Font = Theme.Font(9.0f, FontStyle.Regular),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.TopLeft,
        Margin = new Padding(0, 0, 0, 6)
    };

    private void SetupGridColumns()
    {
        _grid.Columns.Clear();
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ScrollBars = ScrollBars.Both;

        var colCode = new DataGridViewTextBoxColumn
        {
            HeaderText = "Mã yêu cầu",
            FillWeight = 14,
            MinimumWidth = 120,
            ReadOnly = true,
            DefaultCellStyle = { Font = Theme.MonoFont(8.8f, FontStyle.Bold) }
        };

        var colCustomer = new DataGridViewTextBoxColumn
        {
            HeaderText = "Khách hàng & SĐT",
            FillWeight = 22,
            MinimumWidth = 170,
            ReadOnly = true
        };

        var colPackage = new DataGridViewTextBoxColumn
        {
            HeaderText = "Gói chụp & Concept",
            FillWeight = 26,
            MinimumWidth = 190,
            ReadOnly = true
        };

        var colDate = new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày mong muốn",
            FillWeight = 14,
            MinimumWidth = 130,
            ReadOnly = true
        };

        var colDeposit = new DataGridViewTextBoxColumn
        {
            HeaderText = "Cọc dự kiến",
            FillWeight = 11,
            MinimumWidth = 105,
            ReadOnly = true,
            DefaultCellStyle =
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Font = Theme.Font(9.2f, FontStyle.Bold),
                ForeColor = Theme.Success
            }
        };

        var colStatus = new DataGridViewTextBoxColumn
        {
            HeaderText = "Trạng thái",
            FillWeight = 13,
            MinimumWidth = 120,
            ReadOnly = true,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        _grid.Columns.AddRange(colCode, colCustomer, colPackage, colDate, colDeposit, colStatus);
        _grid.SelectionChanged += (_, _) => UpdateDossierFromSelection();
    }

    private void ApplyFilter()
    {
        var q = _searchInput.Text.Trim();
        var filtered = _allRequests.Where(r =>
        {
            var matchQuery = string.IsNullOrWhiteSpace(q) ||
                             r.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             r.Customer.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             r.Phone.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             r.Package.Contains(q, StringComparison.OrdinalIgnoreCase);

            var matchFilter = _activeFilter switch
            {
                "CHO_XU_LY" => r.StatusCode == "CHO_XU_LY",
                "CAN_BO_SUNG" => r.StatusCode == "CAN_BO_SUNG",
                "DA_XAC_NHAN" => r.StatusCode == "DA_XAC_NHAN",
                "CLOSED" => r.StatusCode is "DA_TU_CHOI" or "KHACH_HUY",
                _ => true
            };

            return matchQuery && matchFilter;
        }).ToList();

        _grid.Rows.Clear();
        foreach (var item in filtered)
        {
            var rowIdx = _grid.Rows.Add(
                item.Code,
                $"{item.Customer} ({item.Phone})",
                item.Package,
                item.PreferredDate,
                item.Deposit,
                item.StatusText
            );
            _grid.Rows[rowIdx].Tag = item;

            // Highlight status cell with appropriate theme color
            _grid.Rows[rowIdx].Cells[5].Style.ForeColor = item.StatusColor;
            _grid.Rows[rowIdx].Cells[5].Style.Font = Theme.Font(8.8f, FontStyle.Bold);
        }

        _tableSummaryLabel.Text = $"{filtered.Count} yêu cầu ({_allRequests.Count} tổng cộng)";
        if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
            UpdateDossierFromSelection();
        }
        else
        {
            ClearDossier();
        }
    }

    private void ClearDossier()
    {
        _selectedCodeLabel.Text = "Chưa chọn yêu cầu";
        _selectedStatusBadge.Text = "";
        _selectedStatusBadge.BackColor = Color.Transparent;

        _valCustomer.Text = "—";
        _valPhone.Text = "—";
        _valEmail.Text = "—";
        _valChannel.Text = "—";
        _valPackage.Text = "—";
        _valDate.Text = "—";
        _valChamber.Text = "—";
        _valDeposit.Text = "—";
        _valStyleNotes.Text = "—";
        _valOperationalNotes.Text = "—";
    }

    private void UpdateDossierFromSelection()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not RequestItem req)
        {
            ClearDossier();
            return;
        }

        _selectedCodeLabel.Text = req.Code;
        _selectedStatusBadge.Text = req.StatusText.ToUpperInvariant();
        _selectedStatusBadge.BackColor = Color.FromArgb(35, req.StatusColor);
        _selectedStatusBadge.ForeColor = req.StatusColor;

        _valCustomer.Text = req.Customer;
        _valPhone.Text = req.Phone;
        _valEmail.Text = req.Email;
        _valChannel.Text = "Web Customer Portal v2.2 (Trực tuyến)";

        _valPackage.Text = req.Package;
        _valDate.Text = req.PreferredDate;
        _valChamber.Text = req.SuggestedChamber;
        _valDeposit.Text = req.Deposit;

        _valStyleNotes.Text = req.StyleNotes;
        _valOperationalNotes.Text = req.OperationalNotes;
    }
}
