using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerSpacesResourcesPage : UserControl
{
    private readonly AppFacade? _app;
    private readonly ICustomerNavigator _nav;
    private string _activeFilter = "rooms";
    private string _selectedCode = "P0001";
    private List<CatalogRoomViewDto> _rooms = [];
    private List<CatalogResourceViewDto> _resources = [];
    private readonly List<(string Code, CardPanel Card)> _tiles = [];

    private readonly PageHeader _header;
    private readonly SegmentedTabBar _tabs = new()
    {
        Dock = DockStyle.Top,
        Height = 42,
        Margin = new Padding(0, 0, 0, 8)
    };

    private readonly FlowLayoutPanel _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true,
        Padding = new Padding(0, 6, 4, 6),
        BackColor = Color.Transparent
    };

    private VisualMediaPictureBox _preview = null!;
    private readonly Label _insCode = new() { Dock = DockStyle.Top, Height = 20, UseMnemonic = false, Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Primary, AutoEllipsis = true };
    private readonly Label _insTitle = new() { Dock = DockStyle.Top, Height = 30, UseMnemonic = false, Font = Theme.Font(13f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
    private readonly Label _insKey1 = new() { Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.3f, FontStyle.Bold), ForeColor = Theme.Muted, Margin = new Padding(0, 4, 8, 4) };
    private readonly Label _insVal1 = new() { AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.7f), ForeColor = Theme.Text, Margin = new Padding(0, 4, 0, 4) };
    private readonly Label _insKey2 = new() { Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.3f, FontStyle.Bold), ForeColor = Theme.Muted, Margin = new Padding(0, 4, 8, 4) };
    private readonly Label _insVal2 = new() { AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.7f), ForeColor = Theme.Text, Margin = new Padding(0, 4, 0, 4) };
    private readonly Label _insKey3 = new() { Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.3f, FontStyle.Bold), ForeColor = Theme.Muted, Margin = new Padding(0, 4, 8, 4) };
    private readonly Label _insVal3 = new() { AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.6f), ForeColor = Theme.TextSecondary, Margin = new Padding(0, 4, 0, 4) };
    private readonly Label _insFallbackNotice = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Visible = false,
        Padding = new Padding(8, 6, 8, 6),
        Margin = new Padding(0, 8, 0, 0),
        BackColor = Theme.WarningLight,
        ForeColor = Theme.Primitives.Ochre700,
        Font = Theme.Font(8.1f, FontStyle.Bold),
        UseMnemonic = false
    };

    public CustomerSpacesResourcesPage(ICustomerNavigator nav) : this(null, nav) { }

    public CustomerSpacesResourcesPage(AppFacade? app, ICustomerNavigator nav)
    {
        _app = app;
        _nav = nav;
        _rooms = SeedGroundedCatalogRepository.GetRooms().ToList();
        _resources = SeedGroundedCatalogRepository.GetResources().ToList();

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        _header = new PageHeader(
            "Không gian phòng chụp, Thiết bị & Trang phục",
            BuildSubtitleText());
        var cta = Theme.OutlineButton("Tạo yêu cầu đặt lịch", Theme.Primary, IconKind.Calendar);
        cta.Width = 180;
        cta.Click += (_, _) => _nav.NavigateTo("wizard");
        _header.Actions.Controls.Add(cta);

        UpdateTabCounts();
        _tabs.SegmentSelected += (_, seg) =>
        {
            _activeFilter = seg.Key;
            RenderCards();
        };

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

        var leftCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12) };
        leftCard.Controls.Add(_grid);
        _grid.SizeChanged += (_, _) => UpdateTileWidths();

        var rightCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(16) };
        var rGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        rGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        rGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        rGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _preview = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry("P0001"), VisualAssetService.GetGallery("P0001"))
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };

        var copyBox = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var kvTable = BuildInspectorKvTable();
        copyBox.Controls.Add(_insFallbackNotice);
        copyBox.Controls.Add(kvTable);
        copyBox.Controls.Add(_insTitle);
        copyBox.Controls.Add(_insCode);

        var openViewerBtn = Theme.OutlineButton("Xem ảnh chi tiết", Theme.Primary, IconKind.Eye);
        openViewerBtn.Width = 168;
        openViewerBtn.Click += (_, _) => _preview.OpenLightbox();
        var btnBar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) };
        btnBar.Controls.Add(openViewerBtn);

        rGrid.Controls.Add(_preview, 0, 0);
        rGrid.Controls.Add(copyBox, 0, 1);
        rGrid.Controls.Add(btnBar, 0, 2);
        rightCard.Controls.Add(rGrid);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);

        Controls.Add(split);
        Controls.Add(_tabs);
        Controls.Add(_header);

        RenderCards();
        _ = TryRefreshFromSqlAsync();
    }

    private string BuildSubtitleText()
    {
        var liveBadge = (_rooms.Any(x => x.FromLiveSql) || _resources.Any(x => x.FromLiveSql))
            ? "đọc trực tiếp từ SQL Server"
            : "theo dữ liệu studio";
        return $"Trình bày trực quan {_rooms.Count:00} phòng chụp và {_resources.Count:00} thiết bị / trang phục ({liveBadge})";
    }

    private void UpdateTabCounts()
    {
        var equipCount = _resources.Count(x => !string.Equals(x.LoaiTaiNguyen, "TRANG_PHUC", StringComparison.OrdinalIgnoreCase));
        var costumeCount = _resources.Count(x => string.Equals(x.LoaiTaiNguyen, "TRANG_PHUC", StringComparison.OrdinalIgnoreCase));
        _tabs.SetItems([
            new("rooms", $"Phòng chụp studio ({_rooms.Count})", IconKind.Home),
            new("equip", $"Thiết bị & Đạo cụ ({equipCount})", IconKind.Camera),
            new("costume", $"Trang phục cho thuê ({costumeCount})", IconKind.Sparkles)
        ], _activeFilter);
    }

    private Control BuildInspectorKvTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 4, 4, 4),
            BackColor = Color.Transparent
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.Controls.Add(_insKey1, 0, 0);
        table.Controls.Add(_insVal1, 1, 0);
        table.Controls.Add(_insKey2, 0, 1);
        table.Controls.Add(_insVal2, 1, 1);
        table.Controls.Add(_insKey3, 0, 2);
        table.Controls.Add(_insVal3, 1, 2);
        table.SizeChanged += (_, _) =>
        {
            var maxW = Math.Max(160, table.ClientSize.Width - 134);
            _insVal1.MaximumSize = new Size(maxW, 0);
            _insVal2.MaximumSize = new Size(maxW, 0);
            _insVal3.MaximumSize = new Size(maxW, 0);
            _insFallbackNotice.MaximumSize = new Size(Math.Max(220, table.ClientSize.Width - 12), 0);
        };
        return table;
    }

    private async Task TryRefreshFromSqlAsync()
    {
        if (_app is null) return;
        try
        {
            var liveRooms = await SeedGroundedCatalogRepository.TryLoadLiveRoomsReadOnlyAsync(_app);
            var liveResources = await SeedGroundedCatalogRepository.TryLoadLiveResourcesReadOnlyAsync(_app);
            if (!IsDisposed && (liveRooms.Any(x => x.FromLiveSql) || liveResources.Any(x => x.FromLiveSql)))
            {
                _rooms = liveRooms.ToList();
                _resources = liveResources.ToList();
                _header.HeaderSubtitle = BuildSubtitleText();
                UpdateTabCounts();
                RenderCards();
            }
        }
        catch
        {
            // Keep baseline cleanly
        }
    }

    private void RenderCards()
    {
        _grid.SuspendLayout();
        _grid.Controls.Clear();
        _tiles.Clear();

        if (_activeFilter == "rooms")
        {
            foreach (var r in _rooms)
                _grid.Controls.Add(BuildTile(r.MaPhong, r.TenPhong, r.MoTa, "Không gian studio", () => ShowRoom(r)));
            var target = _rooms.FirstOrDefault(r => r.MaPhong == _selectedCode) ?? _rooms.FirstOrDefault();
            if (target is not null) ShowRoom(target);
        }
        else
        {
            var wantType = _activeFilter == "costume" ? "TRANG_PHUC" : "THIET_BI";
            var filtered = _resources.Where(x => string.Equals(x.LoaiTaiNguyen, wantType, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var res in filtered)
            {
                var sub = $"SL cơ sở: {res.SoLuong}" + (res.MaDichVuThue is not null ? $" • Kèm {res.MaDichVuThue}" : "");
                _grid.Controls.Add(BuildTile(res.MaTaiNguyen, res.TenTaiNguyen, sub, res.LoaiHienThi, () => ShowResource(res)));
            }
            var target = filtered.FirstOrDefault(r => r.MaTaiNguyen == _selectedCode) ?? filtered.FirstOrDefault();
            if (target is not null) ShowResource(target);
        }

        UpdateTileWidths();
        UpdateTileSelectionVisuals();
        _grid.ResumeLayout();
    }

    private Control BuildTile(string code, string name, string subtitle, string pill, Action onSelect)
    {
        var card = new CardPanel
        {
            Size = new Size(256, 212),
            Margin = new Padding(0, 0, 10, 10),
            Padding = new Padding(8),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.Border,
            AccentStripeColor = Color.Transparent,
            AccentStripeLeft = true,
            AccentStripeThickness = 4,
            Cursor = Cursors.Hand
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var pic = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(code), VisualAssetService.GetGallery(code))
        {
            Dock = DockStyle.Fill,
            MaxThumbWidth = 420,
            Cursor = Cursors.Hand
        };
        var title = new Label { Dock = DockStyle.Fill, Text = $"{name} ({code})", Font = Theme.Font(9.2f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
        var sub = new Label { Dock = DockStyle.Fill, Text = $"{pill} • {subtitle}", Font = Theme.Font(8.1f), ForeColor = Theme.Muted, AutoEllipsis = true };

        layout.Controls.Add(pic, 0, 0);
        layout.Controls.Add(title, 0, 1);
        layout.Controls.Add(sub, 0, 2);
        card.Controls.Add(layout);

        void SelectTile(object? _, EventArgs __) => onSelect();
        card.Click += SelectTile;
        pic.Click += SelectTile;
        title.Click += SelectTile;
        sub.Click += SelectTile;
        _tiles.Add((code, card));
        return card;
    }

    private void UpdateTileWidths()
    {
        var avail = Math.Max(220, _grid.ClientSize.Width - 22);
        var cols = avail >= 790 ? 3 : avail >= 470 ? 2 : 1;
        var w = Math.Max(210, (avail - (cols - 1) * 10) / cols);
        foreach (var (_, card) in _tiles)
        {
            if (card.Width != w) card.Width = w;
        }
    }

    private void UpdateTileSelectionVisuals()
    {
        foreach (var (code, card) in _tiles)
        {
            var isSel = string.Equals(code, _selectedCode, StringComparison.OrdinalIgnoreCase);
            card.BackColor = isSel ? Theme.PrimaryLight : Theme.Primitives.Alabaster50;
            card.BorderColor = isSel ? Theme.Primary : Theme.Border;
            card.AccentStripeColor = isSel ? Theme.Primary : Color.Transparent;
            card.Invalidate();
        }
    }

    private void ShowRoom(CatalogRoomViewDto r)
    {
        _selectedCode = r.MaPhong;
        var cover = VisualAssetService.GetCoverEntry(r.MaPhong);
        _preview.BindAsset(cover, r.Gallery);
        _insCode.Text = $"PHÒNG {r.MaPhong}  •  {(r.FromLiveSql ? "SQL" : "STUDIO")}  •  {cover.SourceBadgeLabel}";
        _insTitle.Text = r.TenPhong;
        _insKey1.Text = "Đặc trưng:";
        _insVal1.Text = r.MoTa;
        _insKey2.Text = "Phong cách:";
        _insVal2.Text = r.StyleSummary;
        _insKey3.Text = "Phân bổ lịch:";
        _insVal3.Text = r.Notes;
        UpdateFallbackNotice(cover, "phòng chụp");
        UpdateTileSelectionVisuals();
    }

    private void ShowResource(CatalogResourceViewDto res)
    {
        _selectedCode = res.MaTaiNguyen;
        var cover = VisualAssetService.GetCoverEntry(res.MaTaiNguyen);
        _preview.BindAsset(cover, res.Gallery);
        _insCode.Text = $"{res.MaTaiNguyen}  •  {res.LoaiHienThi.ToUpperInvariant()}  •  {cover.SourceBadgeLabel}";
        _insTitle.Text = res.TenTaiNguyen;
        _insKey1.Text = "Số lượng cơ sở:";
        _insVal1.Text = $"{res.SoLuong} đơn vị quản lý tại studio";
        _insKey2.Text = "Dịch vụ thuê:";
        _insVal2.Text = res.MaDichVuThue ?? "Thuộc trang bị vận hành nội bộ theo lịch chụp";
        _insKey3.Text = "Ghi chú:";
        _insVal3.Text = $"{res.IllustrativeNote} Tình trạng rảnh thực tế được nhân sự kiểm tra khi điều phối lịch.";
        UpdateFallbackNotice(cover, "tài nguyên");
        UpdateTileSelectionVisuals();
    }

    private void UpdateFallbackNotice(VisualAssetEntryDto cover, string kindLabel)
    {
        if (cover.SourceKind == MediaSourceKind.SystemFallback)
        {
            _insFallbackNotice.Text = $"Thông báo ảnh {kindLabel}: Chưa có ảnh chụp riêng cho mã {cover.TargetCode} trong bộ sưu tập cục bộ. Hệ thống hiển thị ảnh thay thế an toàn thay cho ảnh thật.";
            _insFallbackNotice.Visible = true;
        }
        else
        {
            _insFallbackNotice.Visible = false;
        }
    }
}
