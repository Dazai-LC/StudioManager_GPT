using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

/// <summary>
/// STUDIOMANAGER v2.2 — PHASE 01 FRONTEND FOUNDATION
/// Màn hình mô phỏng Trung tâm Đối soát & Thanh toán (Tab 2.3 Hub Lịch Chụp).
/// Chế độ: Presentation-only, Zero SQL Server writes, chuẩn nhận diện Atelier Lumière.
/// </summary>
public sealed class PaymentWorkspacePlaceholderPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _searchInput = new("Tìm theo mã giao dịch, mã lịch, tên khách hàng hoặc cú pháp...")
    {
        Dock = DockStyle.Fill,
        Height = 38
    };
    private readonly DossierFieldList _dossier = new();
    private readonly Label _selectedTxnLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 28,
        Font = Theme.Font(11.5f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _selectedStatusBadge = new()
    {
        Height = 24,
        Width = 160,
        Font = Theme.MonoFont(7.8f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly Label _tableSummaryLabel = new()
    {
        Dock = DockStyle.Right,
        Width = 220,
        Font = Theme.MonoFont(8.0f, FontStyle.Bold),
        ForeColor = Theme.Primitives.Teal600,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "0 giao dịch hiển thị"
    };

    private sealed record TransactionItem(
        string TxnCode,
        string ReferenceCode,
        string Customer,
        string Amount,
        string Channel,
        string TransferContent,
        string Status,
        Color StatusColor,
        string BankName,
        string Timestamp,
        string LedgerNote
    );

    private readonly List<TransactionItem> _allTransactions =
    [
        new("TXN-202611-042", "BK-202611-042", "Nguyễn Mai Anh", "2.000.000 đ", "VietQR (MBBank)", "PHOENIX BK042", "Đã khớp đối soát", Theme.Success, "MBBank (Atelier Lumière)", "10:45:12 11/11/2026", "Nhân viên đã kiểm tra sao kê & tạo phiếu thu cọc đợt 1"),
        new("TXN-202611-043", "BK-202611-043", "Trần Hoàng Nam", "1.500.000 đ", "Chuyển khoản Vietcombank", "PHOENIX BK043", "Đã khớp đối soát", Theme.Success, "Vietcombank CN Ba Đình", "09:30:45 11/11/2026", "Đã xác nhận cọc & cập nhật trạng thái DaXacNhan"),
        new("TXN-202611-044", "REQ-202611-008", "Lê Thu Trang", "500.000 đ", "VietQR (Techcombank)", "PHOENIX REQ008", "Đã xác nhận giữ cọc", Theme.Primitives.Teal600, "Techcombank Hoàn Kiếm", "14:15:00 10/11/2026", "Nhân viên ghi nhận giữ slot concept Lookbook"),
        new("TXN-202611-045", "BK-202611-039", "Phạm Quốc Bảo", "3.200.000 đ", "Tiền mặt tại quầy", "THU TIEN DOT 2 BK039", "Đã thu đủ (Tiền mặt)", Theme.Success, "Quầy Thu ngân Studio", "16:20:18 10/11/2026", "Thu tiền mặt đợt 2 sau hoàn thiện hậu kỳ"),
        new("TXN-202611-046", "CHƯA XÁC ĐỊNH", "Nguyễn Văn X", "1.000.000 đ", "Chuyển khoản liên ngân hàng", "CK DAT COC CHUP ANH", "Chờ nhân viên đối soát", Theme.Warning, "VietinBank", "08:12:30 11/11/2026", "Cú pháp thiếu tiền tố PHOENIX, nhân viên đang tra soát tài khoản")
    ];

    private string _activeFilter = "ALL";

    public PaymentWorkspacePlaceholderPage(AppFacade app)
    {
        _app = app;
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Font = Theme.Font();
        Padding = new Padding(24, 16, 24, 20);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74f));   // Simulation Banner
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126f));  // Telemetry KPI Cards
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Workspace Split

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
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(16, 10, 16, 10),
            CornerRadius = 8,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.Primitives.Teal600,
            AccentStripeColor = Theme.Primitives.Teal600,
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
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
        bannerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var badgeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "MÔ PHỎNG ĐỐI SOÁT\nVIETQR & THANH TOÁN STUDIO",
            Font = Theme.MonoFont(7.8f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Teal600,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var textPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = "Trung tâm Đối soát Giao dịch & Thanh toán — v2.2 Milestone (Payment Hub)",
            Font = Theme.Font(9.4f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var descLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Mô phỏng luồng đối soát chuyển khoản VietQR, đối soát nhân viên và thanh toán tiền mặt. Bảo toàn 100% nguyên tắc dòng tiền bất biến và sổ cái đối soát an toàn.",
            Font = Theme.Font(8.3f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        textPanel.Controls.Add(descLabel);
        textPanel.Controls.Add(titleLabel);

        var phaseLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "KIẾN TRÚC GIAI ĐOẠN 01\nHOÀN TẤT ĐIỀU HƯỚNG",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Teal600,
            TextAlign = ContentAlignment.MiddleRight
        };

        bannerLayout.Controls.Add(badgeLabel, 0, 0);
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
            Margin = new Padding(0, 0, 0, 12),
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var card1 = new StatCard("Thu chuyển khoản VietQR", Theme.Primitives.Teal600, IconKind.Wallet);
        card1.Margin = new Padding(0, 0, 6, 0);
        card1.Value = "68.5M";
        card1.Trend = "Chuyển khoản & Quét mã";

        var card2 = new StatCard("Đã đối soát khớp", Theme.Success, IconKind.Check);
        card2.Margin = new Padding(6, 0, 6, 0);
        card2.Value = "42 GD";
        card2.Trend = "Nhân viên đã xác nhận";

        var card3 = new StatCard("Chờ nhân viên đối soát", Theme.Warning, IconKind.AlertTriangle);
        card3.Margin = new Padding(6, 0, 6, 0);
        card3.Value = "1 GD";
        card3.Trend = "Lệch cú pháp CK";

        var card4 = new StatCard("Hoàn tiền & Điều chỉnh", Theme.Primary, IconKind.History);
        card4.Margin = new Padding(6, 0, 0, 0);
        card4.Value = "0 đ";
        card4.Trend = "Không có tranh chấp";

        grid.Controls.Add(card1, 0, 0);
        grid.Controls.Add(card2, 1, 0);
        grid.Controls.Add(card3, 2, 0);
        grid.Controls.Add(card4, 3, 0);
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
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
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
        toolLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        toolLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        toolLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        toolLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _searchInput.TextChanged += (_, _) => ApplyFilter();

        var filterFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(8, 2, 0, 0)
        };

        var btnAll = Theme.OutlineButton("Tất cả", Theme.BorderStrong);
        var btnAuto = Theme.OutlineButton("Đã khớp (4)", Theme.BorderStrong);
        var btnManual = Theme.OutlineButton("Chờ duyệt (1)", Theme.BorderStrong);

        btnAll.Height = 32; btnAuto.Height = 32; btnManual.Height = 32;
        btnAll.Click += (_, _) => { _activeFilter = "ALL"; ApplyFilter(); };
        btnAuto.Click += (_, _) => { _activeFilter = "AUTO"; ApplyFilter(); };
        btnManual.Click += (_, _) => { _activeFilter = "MANUAL"; ApplyFilter(); };

        filterFlow.Controls.Add(btnAll);
        filterFlow.Controls.Add(btnAuto);
        filterFlow.Controls.Add(btnManual);

        var refreshBtn = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refreshBtn.Dock = DockStyle.Right;
        refreshBtn.Height = 32;
        refreshBtn.Width = 96;
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
            Width = 340,
            Text = "LUỒNG GIAO DỊCH VÀ ĐỐI SOÁT THANH TOÁN MÔ PHỎNG",
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
            Padding = new Padding(18, 16, 18, 16),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            AccentStripeColor = Theme.Primitives.Teal600,
            AccentStripeThickness = 3
        };

        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "CHI TIẾT GIAO DỊCH  //  QUY TẮC ĐỐI SOÁT",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Teal600,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };
        statusPanel.Controls.Add(_selectedStatusBadge);

        var protocolNoteCard = new CardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 160,
            Padding = new Padding(14),
            CornerRadius = 8,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.Border
        };

        var noteTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = "Quy tắc Đối soát An toàn CTR-10..CTR-12:",
            Font = Theme.Font(8.6f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var noteBody = new Label
        {
            Dock = DockStyle.Fill,
            Text = "1. Cú pháp chuẩn nhận diện: 'PHOENIX [MãLịch]' hoặc 'PHOENIX [MãYêuCầu]'.\n" +
                   "2. Ghi nhận phiếu thu bất biến: Không ghi đè lịch sử tài chính, chỉ tạo thêm bản ghi thu cọc mới.\n" +
                   "3. Cập nhật trạng thái: Nhân viên xác nhận cọc để chuyển lịch chụp sang DaXacNhan.\n" +
                   "4. Cách ly xử lý sai lệch: Giao dịch sai cú pháp được đưa vào hàng đợi đối soát thủ công của nhân viên.",
            Font = Theme.Font(8.0f),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.TopLeft
        };
        protocolNoteCard.Controls.Add(noteBody);
        protocolNoteCard.Controls.Add(noteTitle);

        var scrollContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 6)
        };
        scrollContainer.Controls.Add(_dossier);

        card.Controls.Add(scrollContainer);
        card.Controls.Add(protocolNoteCard);
        card.Controls.Add(statusPanel);
        card.Controls.Add(_selectedTxnLabel);
        card.Controls.Add(overline);

        return card;
    }

    private void SetupGridColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã giao dịch", Width = 140, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã tham chiếu", Width = 120, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Khách hàng", Width = 140, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số tiền (VNĐ)", Width = 120, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hình thức", Width = 150, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cú pháp chuyển khoản", Width = 150, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", Width = 150, ReadOnly = true });

        _grid.SelectionChanged += (_, _) => UpdateDossierFromSelection();
    }

    private void ApplyFilter()
    {
        var q = _searchInput.Text.Trim();
        var filtered = _allTransactions.Where(t =>
        {
            var matchQuery = string.IsNullOrWhiteSpace(q) ||
                             t.TxnCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             t.ReferenceCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             t.Customer.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                             t.TransferContent.Contains(q, StringComparison.OrdinalIgnoreCase);

            var matchFilter = _activeFilter switch
            {
                "AUTO" => t.Status.Contains("khớp") || t.Status.Contains("xác nhận") || t.Status.Contains("thu đủ"),
                "MANUAL" => t.Status.Contains("đối soát") || t.Status.Contains("Chờ"),
                _ => true
            };

            return matchQuery && matchFilter;
        }).ToList();

        _grid.Rows.Clear();
        foreach (var item in filtered)
        {
            var rowIdx = _grid.Rows.Add(item.TxnCode, item.ReferenceCode, item.Customer, item.Amount, item.Channel, item.TransferContent, item.Status);
            _grid.Rows[rowIdx].Tag = item;
        }

        _tableSummaryLabel.Text = $"{filtered.Count} giao dịch ({_allTransactions.Count} tổng cộng)";
        if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
            UpdateDossierFromSelection();
        }
    }

    private void UpdateDossierFromSelection()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not TransactionItem txn)
        {
            _selectedTxnLabel.Text = "Chưa chọn giao dịch";
            _selectedStatusBadge.Text = "";
            _dossier.SetRows([]);
            return;
        }

        _selectedTxnLabel.Text = txn.TxnCode;
        _selectedStatusBadge.Text = txn.Status.ToUpperInvariant();
        _selectedStatusBadge.BackColor = Color.FromArgb(40, txn.StatusColor);
        _selectedStatusBadge.ForeColor = txn.StatusColor;

        _dossier.SetRows([
            ("Mã giao dịch", txn.TxnCode, Theme.Primitives.Teal600, true),
            ("Mã tham chiếu", txn.ReferenceCode, Theme.Primary, false),
            ("Khách hàng", txn.Customer, null, false),
            ("Số tiền giao dịch", txn.Amount, Theme.Success, true),
            ("Hình thức thanh toán", txn.Channel, null, false),
            ("Nội dung chuyển khoản", txn.TransferContent, Theme.TextSecondary, true),
            ("Tài khoản / Quầy nhận", txn.BankName, null, false),
            ("Thời gian ghi nhận", txn.Timestamp, null, false),
            ("Ghi chú sổ cái", txn.LedgerNote, Theme.TextSecondary, false)
        ]);
    }
}

