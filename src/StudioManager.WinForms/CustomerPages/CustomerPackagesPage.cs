using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerPackagesPage : UserControl
{
    private readonly AppFacade _app;
    private readonly ICustomerNavigator _nav;
    private List<CatalogPackageViewDto> _packages = [];
    private CatalogPackageViewDto _selected;

    private readonly FlowLayoutPanel _cardList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = false,
        FlowDirection = FlowDirection.TopDown,
        Padding = new Padding(0, 6, 4, 6),
        BackColor = Color.Transparent
    };

    private VisualMediaPictureBox _heroPhoto = null!;
    private readonly Label _detailCode = new() { Dock = DockStyle.Top, Height = 20, Font = Theme.MonoFont(8.2f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly Label _detailTitle = new() { Dock = DockStyle.Top, Height = 28, Font = Theme.Font(13.8f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
    private readonly Label _detailPrice = new() { Dock = DockStyle.Top, Height = 24, Font = Theme.Font(11.6f, FontStyle.Bold), ForeColor = Theme.Primitives.Sage700 };
    private readonly Label _valDesc = new() { AutoSize = true, Font = Theme.Font(8.8f), ForeColor = Theme.Text };
    private readonly Label _valHighlights = new() { AutoSize = true, Font = Theme.Font(8.8f), ForeColor = Theme.Text };
    private readonly Label _valRoom = new() { AutoSize = true, Font = Theme.Font(8.8f), ForeColor = Theme.TextSecondary };
    private readonly List<(string Code, CardPanel Card)> _packageCards = [];
    private readonly DataGridView _compareGrid = Theme.Grid();
    private bool _syncingSelection;

    public CustomerPackagesPage(AppFacade app, ICustomerNavigator nav, string? preselectedPackageCode = null)
    {
        _app = app;
        _nav = nav;
        _packages = SeedGroundedCatalogRepository.GetPackages().ToList();
        _selected = _packages.FirstOrDefault(p => p.MaGoiChup == preselectedPackageCode) ?? _packages[0];

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Bảng gói chụp & Trải nghiệm concept",
            $"Danh mục {_packages.Count:00} gói chụp tiêu chuẩn tại Atelier Lumière • Nhấn ảnh để mở trình xem toàn màn hình");
        var wizardBtn = Theme.Button("Đặt lịch với gói đang chọn", Theme.Primary, IconKind.Calendar);
        wizardBtn.Width = 224;
        wizardBtn.Click += (_, _) => _nav.NavigateTo("wizard", _selected.MaGoiChup);
        header.Actions.Controls.Add(wizardBtn);

        var mainSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43f));
        mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57f));
        mainSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 64f));
        mainSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 36f));

        var listCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 8), Padding = new Padding(12) };
        var listLbl = new Label { Dock = DockStyle.Top, Height = 24, Text = "CHỌN GÓI ĐỂ XEM CHI TIẾT", Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Muted };
        listCard.Controls.Add(_cardList);
        listCard.Controls.Add(listLbl);

        var detailCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 8), Padding = new Padding(12) };
        var detailGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        detailGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
        detailGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
        detailGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _heroPhoto = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(_selected.MaGoiChup), _selected.Gallery)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6),
            MaxThumbWidth = 900
        };

        var detailCopy = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var kvPanel = BuildPackageKvPanel();
        detailCopy.Controls.Add(kvPanel);
        detailCopy.Controls.Add(_detailPrice);
        detailCopy.Controls.Add(_detailTitle);
        detailCopy.Controls.Add(_detailCode);

        var detailActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        var bookThisBtn = Theme.Button("Chọn gói này để đặt lịch", Theme.Primitives.Sage600, IconKind.ArrowRight);
        bookThisBtn.Width = 220;
        bookThisBtn.Click += (_, _) => _nav.NavigateTo("wizard", _selected.MaGoiChup);
        var openViewerBtn = Theme.OutlineButton("Mở trình xem ảnh", Theme.Primary, IconKind.Eye);
        openViewerBtn.Width = 172;
        openViewerBtn.Click += (_, _) => _heroPhoto.OpenLightbox();
        detailActions.Controls.Add(bookThisBtn);
        detailActions.Controls.Add(openViewerBtn);

        detailGrid.Controls.Add(_heroPhoto, 0, 0);
        detailGrid.Controls.Add(detailCopy, 0, 1);
        detailGrid.Controls.Add(detailActions, 0, 2);
        detailCard.Controls.Add(detailGrid);

        // Bottom Comparison Strip
        var compareCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14) };
        mainSplit.SetColumnSpan(compareCard, 2);
        var compareHdr = new Label { Dock = DockStyle.Top, Height = 24, Text = "BẢNG SO SÁNH NHANH CÁC GÓI CHỤP", Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Muted };
        ConfigureComparisonTable();
        compareCard.Controls.Add(_compareGrid);
        compareCard.Controls.Add(compareHdr);

        mainSplit.Controls.Add(listCard, 0, 0);
        mainSplit.Controls.Add(detailCard, 1, 0);
        mainSplit.Controls.Add(compareCard, 0, 1);

        Controls.Add(mainSplit);
        Controls.Add(header);

        _cardList.SizeChanged += (_, _) => UpdateCardListWidths();

        RenderPackageCards();
        BindSelected(_selected);
        _ = TryRefreshFromSqlAsync();
    }

    private Control BuildPackageKvPanel()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 4, 4, 2),
            BackColor = Color.Transparent
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        void AddRow(int rowIdx, string key, Label valueLabel)
        {
            var k = new Label
            {
                Dock = DockStyle.Fill,
                Text = key,
                Font = Theme.Font(8.4f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 2, 8, 2)
            };
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Margin = new Padding(0, 2, 0, 2);
            table.Controls.Add(k, 0, rowIdx);
            table.Controls.Add(valueLabel, 1, rowIdx);
        }

        AddRow(0, "Mô tả gói:", _valDesc);
        AddRow(1, "Điểm nổi bật:", _valHighlights);
        AddRow(2, "Phòng gợi ý:", _valRoom);
        table.SizeChanged += (_, _) =>
        {
            var maxW = Math.Max(180, table.ClientSize.Width - 134);
            _valDesc.MaximumSize = new Size(maxW, 0);
            _valHighlights.MaximumSize = new Size(maxW, 0);
            _valRoom.MaximumSize = new Size(maxW, 0);
        };
        return table;
    }

    private async Task TryRefreshFromSqlAsync()
    {
        try
        {
            var live = await SeedGroundedCatalogRepository.TryLoadLivePackagesReadOnlyAsync(_app);
            if (!IsDisposed && live.Count > 0 && live[0].FromLiveSql)
            {
                _packages = live.ToList();
                var cur = _packages.FirstOrDefault(x => x.MaGoiChup == _selected.MaGoiChup) ?? _packages[0];
                RenderPackageCards();
                PopulateComparisonTable();
                BindSelected(cur);
            }
        }
        catch
        {
            // Keep grounded seed baseline cleanly
        }
    }

    private void RenderPackageCards()
    {
        _cardList.SuspendLayout();
        _cardList.Controls.Clear();
        _packageCards.Clear();
        var targetWidth = Math.Max(260, _cardList.ClientSize.Width - 22);

        foreach (var pkg in _packages)
        {
            var isSel = pkg.MaGoiChup == _selected.MaGoiChup;
            var card = new CardPanel
            {
                Size = new Size(targetWidth, 102),
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(10),
                BackColor = isSel ? Theme.PrimaryLight : Theme.Primitives.Alabaster50,
                BorderColor = isSel ? Theme.Primary : Theme.Border,
                AccentStripeColor = isSel ? Theme.Primary : Color.Transparent,
                AccentStripeLeft = true,
                AccentStripeThickness = 4,
                Cursor = Cursors.Hand
            };

            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var thumb = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(pkg.MaGoiChup), pkg.Gallery)
            {
                Dock = DockStyle.Fill,
                ShowBadge = false,
                MaxThumbWidth = 260,
                Cursor = Cursors.Hand
            };

            var copy = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 2, 0, 0) };
            var nameLbl = new Label { Dock = DockStyle.Top, Height = 22, Text = $"{pkg.TenGoi} ({pkg.MaGoiChup})", Font = Theme.Font(9.5f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
            var priceLbl = new Label { Dock = DockStyle.Top, Height = 20, Text = $"{pkg.GiaCoBan:N0} đ  •  {pkg.ThoiLuongPhut} phút", Font = Theme.Font(8.8f, FontStyle.Bold), ForeColor = Theme.Primary };
            var descLbl = new Label { Dock = DockStyle.Fill, Text = pkg.MoTa, Font = Theme.Font(8.2f), ForeColor = Theme.Muted, AutoEllipsis = true };
            copy.Controls.Add(descLbl);
            copy.Controls.Add(priceLbl);
            copy.Controls.Add(nameLbl);

            row.Controls.Add(thumb, 0, 0);
            row.Controls.Add(copy, 1, 0);
            card.Controls.Add(row);

            void SelectMe(object? _, EventArgs __) => BindSelected(pkg);
            card.Click += SelectMe;
            thumb.Click += SelectMe;
            copy.Click += SelectMe;
            nameLbl.Click += SelectMe;
            priceLbl.Click += SelectMe;
            descLbl.Click += SelectMe;

            _packageCards.Add((pkg.MaGoiChup, card));
            _cardList.Controls.Add(card);
        }
        _cardList.ResumeLayout();
    }

    private void UpdateCardListWidths()
    {
        var w = Math.Max(260, _cardList.ClientSize.Width - 22);
        foreach (var (_, card) in _packageCards)
        {
            if (card.Width != w) card.Width = w;
        }
    }

    private void UpdatePackageCardSelectionVisuals()
    {
        foreach (var (code, card) in _packageCards)
        {
            var isSel = string.Equals(code, _selected.MaGoiChup, StringComparison.OrdinalIgnoreCase);
            card.BackColor = isSel ? Theme.PrimaryLight : Theme.Primitives.Alabaster50;
            card.BorderColor = isSel ? Theme.Primary : Theme.Border;
            card.AccentStripeColor = isSel ? Theme.Primary : Color.Transparent;
            card.Invalidate();
        }
    }

    private void BindSelected(CatalogPackageViewDto pkg)
    {
        _selected = pkg;
        var cover = VisualAssetService.GetCoverEntry(pkg.MaGoiChup);
        _heroPhoto.BindAsset(cover, pkg.Gallery);
        _detailCode.Text = $"MÃ GÓI: {pkg.MaGoiChup}  •  {(pkg.FromLiveSql ? "ĐỌC CHỈ XEM TỪ SQL" : "DỮ LIỆU DANH MỤC MẪU")}  •  {cover.SourceBadgeLabel}";
        _detailTitle.Text = pkg.TenGoi;
        _detailPrice.Text = $"{pkg.GiaCoBan:N0} đ   •   Thời lượng dự kiến: {pkg.ThoiLuongPhut} phút";
        _valDesc.Text = pkg.MoTa;
        _valHighlights.Text = pkg.Highlights;
        _valRoom.Text = $"{pkg.SuggestedRoomCode} — Nhân sự studio kiểm tra lịch trống thực tế trước khi chốt.";

        UpdatePackageCardSelectionVisuals();
        if (!_syncingSelection)
        {
            _syncingSelection = true;
            try
            {
                var idx = _packages.FindIndex(p => p.MaGoiChup == pkg.MaGoiChup);
                if (idx >= 0 && idx < _compareGrid.Rows.Count)
                {
                    _compareGrid.ClearSelection();
                    _compareGrid.Rows[idx].Selected = true;
                }
            }
            finally { _syncingSelection = false; }
        }
    }

    private void ConfigureComparisonTable()
    {
        _compareGrid.Dock = DockStyle.Fill;
        _compareGrid.ReadOnly = true;
        _compareGrid.AllowUserToAddRows = false;
        _compareGrid.AllowUserToDeleteRows = false;
        _compareGrid.MultiSelect = false;
        _compareGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _compareGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _compareGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "Mã", FillWeight = 12 });
        _compareGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên gói chụp", FillWeight = 22 });
        _compareGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrice", HeaderText = "Giá cơ bản", FillWeight = 16 });
        _compareGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDur", HeaderText = "Thời lượng", FillWeight = 13 });
        _compareGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColRoom", HeaderText = "Phòng gợi ý", FillWeight = 13 });
        _compareGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDesc", HeaderText = "Mô tả theo danh mục", FillWeight = 34 });
        PopulateComparisonTable();
        _compareGrid.SelectionChanged += (_, _) =>
        {
            if (_syncingSelection || _compareGrid.SelectedRows.Count == 0) return;
            var idx = _compareGrid.SelectedRows[0].Index;
            if (idx >= 0 && idx < _packages.Count)
            {
                _syncingSelection = true;
                try { BindSelected(_packages[idx]); }
                finally { _syncingSelection = false; }
            }
        };
    }

    private void PopulateComparisonTable()
    {
        _syncingSelection = true;
        try
        {
            _compareGrid.Rows.Clear();
            foreach (var p in _packages)
            {
                _compareGrid.Rows.Add(
                    p.MaGoiChup,
                    p.TenGoi,
                    $"{p.GiaCoBan:N0} đ",
                    $"{p.ThoiLuongPhut} phút",
                    p.SuggestedRoomCode,
                    p.MoTa);
            }
        }
        finally { _syncingSelection = false; }
    }
}
