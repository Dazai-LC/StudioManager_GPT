using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Pages;

/// <summary>
/// STUDIOMANAGER v2.2 — WORKSTREAM G FRONTEND WORKSPACE
/// Không gian Tiếp nhận & Điều phối Yêu cầu Đặt lịch (Tab 2.2 Hub Lịch Chụp).
/// Đồng bộ dữ liệu Demo với Cổng Khách hàng trong cùng phiên chạy Desktop (Zero SQL writes).
/// </summary>
public sealed class BookingRequestsPlaceholderPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _searchInput = new("Tìm theo mã yêu cầu, tên khách hàng, SĐT hoặc gói chụp...")
    {
        Dock = DockStyle.Fill,
        Height = 38
    };

    private StatCard _cardTotal = null!;
    private StatCard _cardPending = null!;
    private StatCard _cardReview = null!;
    private StatCard _cardConfirmed = null!;

    private VisualMediaPictureBox _packageThumb = null!;
    private readonly Label _dossierHeader = new() { Dock = DockStyle.Top, Height = 42, Font = Theme.Font(11.0f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
    private readonly CardPanel _dossierKvCard = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        BackColor = Theme.Primitives.Alabaster50,
        BorderColor = Theme.Border,
        Padding = new Padding(10, 8, 10, 8),
        Margin = new Padding(0, 2, 0, 8)
    };
    private readonly Label _kvCustomer = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.5f), ForeColor = Theme.Text };
    private readonly Label _kvPackage = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.5f), ForeColor = Theme.Text };
    private readonly Label _kvSchedule = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.5f), ForeColor = Theme.TextSecondary };
    private readonly Label _kvServices = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.5f), ForeColor = Theme.TextSecondary };
    private readonly Label _kvCustomerNote = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.4f), ForeColor = Theme.Muted };
    private readonly Label _kvLinkedBooking = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.MonoFont(8.3f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly ComboBox _roomSelect = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = Theme.Font(9.0f) };
    private readonly ComboBox _photographerSelect = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = Theme.Font(9.0f) };
    private readonly TextBox _customerFeedbackBox = new() { Dock = DockStyle.Top, Height = 52, Multiline = true, Font = Theme.Font(8.9f), PlaceholderText = "Phản hồi hiển thị cho khách hàng..." };
    private readonly TextBox _internalNoteBox = new() { Dock = DockStyle.Top, Height = 44, Multiline = true, Font = Theme.Font(8.9f), PlaceholderText = "Ghi chú điều phối nội bộ..." };
    private readonly Label _conflictStatus = new() { Dock = DockStyle.Top, Height = 36, Font = Theme.Font(8.6f, FontStyle.Bold), ForeColor = Theme.WarningHover };
    private readonly Label _tableSummaryLabel = new() { Dock = DockStyle.Right, Width = 220, Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Primary, TextAlign = ContentAlignment.MiddleRight };

    private string _activeFilter = "ALL";
    private List<CustomerBookingRequestDto> _visible = [];

    public BookingRequestsPlaceholderPage(AppFacade app)
    {
        _app = app;
        DesktopDemoSessionStore.EnsureSeeded();

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Yêu cầu đặt lịch trực tuyến (Frontend Demo)",
            "Tiếp nhận từ Cổng Khách hàng • Kiểm tra trùng lịch đọc-chỉ-xem • Điều phối nhiếp ảnh gia & phòng chụp (Zero SQL writes)");

        var refreshBtn = Theme.OutlineButton("Làm mới danh sách", Theme.BorderStrong, IconKind.Refresh);
        refreshBtn.Click += (_, _) => ReloadRequests();
        header.Actions.Controls.Add(refreshBtn);

        var kpiStrip = BuildKpiStrip();
        var toolbar = BuildFilterToolbar();
        var workspace = BuildWorkspace();

        Controls.Add(workspace);
        Controls.Add(toolbar);
        Controls.Add(kpiStrip);
        Controls.Add(header);

        ReloadRequests();
    }

    private Control BuildKpiStrip()
    {
        var strip = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 112,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 10),
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 4; i++) strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        _cardTotal = new StatCard("TỔNG YÊU CẦU", Theme.Primary, IconKind.Calendar) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        _cardPending = new StatCard("CHỜ XỬ LÝ", Theme.Warning, IconKind.Clock) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        _cardReview = new StatCard("CẦN BỔ SUNG", Theme.Cyan, IconKind.AlertTriangle) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        _cardConfirmed = new StatCard("ĐÃ XÁC NHẬN", Theme.Success, IconKind.Check) { Dock = DockStyle.Fill, Margin = Padding.Empty };

        strip.Controls.Add(_cardTotal, 0, 0);
        strip.Controls.Add(_cardPending, 1, 0);
        strip.Controls.Add(_cardReview, 2, 0);
        strip.Controls.Add(_cardConfirmed, 3, 0);
        return strip;
    }

    private Control BuildFilterToolbar()
    {
        var bar = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 98,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 10)
        };
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        _searchInput.TextChanged += (_, _) => ApplyFilter();
        searchRow.Controls.Add(_searchInput, 0, 0);
        searchRow.Controls.Add(_tableSummaryLabel, 1, 0);

        var chipTabs = new SegmentedTabBar { Dock = DockStyle.Fill };
        chipTabs.SetItems([
            new("ALL", "Tất cả", IconKind.Layers),
            new(nameof(BookingRequestStatus.CHO_XU_LY), "Chờ xử lý", IconKind.Clock),
            new(nameof(BookingRequestStatus.CAN_BO_SUNG), "Cần bổ sung", IconKind.AlertTriangle),
            new(nameof(BookingRequestStatus.DA_XAC_NHAN), "Đã xác nhận", IconKind.Check),
            new("CLOSED", "Từ chối / Khách hủy", IconKind.Close)
        ], "ALL");
        chipTabs.SegmentSelected += (_, seg) => { _activeFilter = seg.Key; ApplyFilter(); };

        rows.Controls.Add(searchRow, 0, 0);
        rows.Controls.Add(chipTabs, 0, 1);
        bar.Controls.Add(rows);
        return bar;
    }

    private Control BuildWorkspace()
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

        // Left Grid
        var gridCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12) };
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => BindSelectedDossier();
        gridCard.Controls.Add(_grid);

        // Right Coordination Dossier
        var dossierCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(14),
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        _packageThumb = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry("GC000001"))
        {
            Dock = DockStyle.Top,
            Height = 112,
            Margin = new Padding(0, 0, 0, 8),
            MaxThumbWidth = 420
        };

        PopulateCoordinationCombos();

        var coordPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 76,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 6, 0, 6)
        };
        coordPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        coordPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        coordPanel.Controls.Add(new Label { Text = "PHÒNG CHỤP PHÂN BỔ", Font = Theme.Font(7.8f, FontStyle.Bold), ForeColor = Theme.Muted, Dock = DockStyle.Fill }, 0, 0);
        coordPanel.Controls.Add(new Label { Text = "NHIẾP ẢNH GIA ĐỀ XUẤT", Font = Theme.Font(7.8f, FontStyle.Bold), ForeColor = Theme.Muted, Dock = DockStyle.Fill }, 1, 0);
        coordPanel.Controls.Add(_roomSelect, 0, 1);
        coordPanel.Controls.Add(_photographerSelect, 1, 1);

        var conflictBtn = Theme.OutlineButton("Kiểm tra trùng lịch (Read-Only / Demo)", Theme.Primitives.Teal600, IconKind.Shield);
        conflictBtn.Dock = DockStyle.Top;
        conflictBtn.Height = 34;
        conflictBtn.Click += async (_, _) => await RunConflictCheckPreviewAsync();

        var feedbackLbl = new Label { Dock = DockStyle.Top, Height = 20, Text = "PHẢN HỒI HIỂN THỊ CHO KHÁCH HÀNG", Font = Theme.Font(7.8f, FontStyle.Bold), ForeColor = Theme.Muted };
        var internalLbl = new Label { Dock = DockStyle.Top, Height = 20, Text = "GHI CHÚ ĐIỀU PHỐI NỘI BỘ", Font = Theme.Font(7.8f, FontStyle.Bold), ForeColor = Theme.Muted };

        var actionsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 92,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        var needMoreBtn = Theme.OutlineButton("Yêu cầu bổ sung", Theme.Cyan, IconKind.AlertTriangle);
        needMoreBtn.Width = 148;
        needMoreBtn.Click += (_, _) => ApplyStaffTransition(BookingRequestStatus.CAN_BO_SUNG);

        var confirmBtn = Theme.Button("Xác nhận yêu cầu", Theme.Success, IconKind.Check);
        confirmBtn.Width = 160;
        confirmBtn.Click += (_, _) => ApplyStaffTransition(BookingRequestStatus.DA_XAC_NHAN);

        var rejectBtn = Theme.OutlineButton("Từ chối kèm lý do", Theme.Danger, IconKind.Close);
        rejectBtn.Width = 158;
        rejectBtn.Click += (_, _) => ApplyStaffTransition(BookingRequestStatus.DA_TU_CHOI);

        var convertBtn = Theme.Button("Xem trước chuyển thành Lịch chụp", Theme.Primary, IconKind.ArrowRight);
        convertBtn.Width = 248;
        convertBtn.Click += (_, _) => ShowConversionPreview();

        actionsFlow.Controls.Add(confirmBtn);
        actionsFlow.Controls.Add(needMoreBtn);
        actionsFlow.Controls.Add(rejectBtn);
        actionsFlow.Controls.Add(convertBtn);

        var kvGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.Transparent
        };
        kvGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114));
        kvGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        void AddKvRow(int rowIdx, string key, Label val)
        {
            var lblK = new Label
            {
                Dock = DockStyle.Fill,
                Text = key,
                UseMnemonic = false,
                Font = Theme.Font(8.2f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 3, 6, 3)
            };
            val.UseMnemonic = false;
            val.Margin = new Padding(0, 3, 0, 3);
            kvGrid.Controls.Add(lblK, 0, rowIdx);
            kvGrid.Controls.Add(val, 1, rowIdx);
        }

        AddKvRow(0, "Khách hàng:", _kvCustomer);
        AddKvRow(1, "Gói & Tạm tính:", _kvPackage);
        AddKvRow(2, "Ngày / Phòng:", _kvSchedule);
        AddKvRow(3, "Dịch vụ thêm:", _kvServices);
        AddKvRow(4, "Ghi chú khách:", _kvCustomerNote);
        AddKvRow(5, "Lịch chuyển đổi:", _kvLinkedBooking);
        _dossierKvCard.Controls.Add(kvGrid);
        scroll.SizeChanged += (_, _) =>
        {
            var maxValW = Math.Max(160, scroll.ClientSize.Width - 156);
            _kvCustomer.MaximumSize = new Size(maxValW, 0);
            _kvPackage.MaximumSize = new Size(maxValW, 0);
            _kvSchedule.MaximumSize = new Size(maxValW, 0);
            _kvServices.MaximumSize = new Size(maxValW, 0);
            _kvCustomerNote.MaximumSize = new Size(maxValW, 0);
            _kvLinkedBooking.MaximumSize = new Size(maxValW, 0);
        };

        // WinForms Dock=Top adds in reverse bottom-to-top z-order
        scroll.Controls.Add(actionsFlow);
        scroll.Controls.Add(_internalNoteBox);
        scroll.Controls.Add(internalLbl);
        scroll.Controls.Add(_customerFeedbackBox);
        scroll.Controls.Add(feedbackLbl);
        scroll.Controls.Add(_conflictStatus);
        scroll.Controls.Add(conflictBtn);
        scroll.Controls.Add(coordPanel);
        scroll.Controls.Add(_dossierKvCard);
        scroll.Controls.Add(_dossierHeader);
        scroll.Controls.Add(_packageThumb);

        dossierCard.Controls.Add(scroll);
        split.Controls.Add(gridCard, 0, 0);
        split.Controls.Add(dossierCard, 1, 0);
        return split;
    }

    private void PopulateCoordinationCombos()
    {
        _roomSelect.Items.Clear();
        foreach (var r in SeedGroundedCatalogRepository.GetRooms())
            _roomSelect.Items.Add($"{r.MaPhong} — {r.TenPhong}");
        if (_roomSelect.Items.Count > 0) _roomSelect.SelectedIndex = 0;

        _photographerSelect.Items.Clear();
        foreach (var p in SeedGroundedCatalogRepository.GetPhotographers().Where(x => x.IsActive))
            _photographerSelect.Items.Add($"{p.MaNhanVien} — {p.HoTen}");
        if (_photographerSelect.Items.Count > 0) _photographerSelect.SelectedIndex = 0;
        RoleVisualResolver.AttachRoleIconComboBox(_photographerSelect, _ => RoleVisualResolver.ForPhotographer());
    }

    private void ReloadRequests()
    {
        var all = DesktopDemoSessionStore.GetAllBookingRequests();
        _cardTotal.Value = all.Count.ToString();
        _cardTotal.Trend = "Đồng bộ từ Cổng Khách hàng (Demo)";
        _cardPending.Value = all.Count(x => x.TrangThai == BookingRequestStatus.CHO_XU_LY).ToString();
        _cardPending.Trend = "Cần tiếp nhận & kiểm tra lịch";
        _cardReview.Value = all.Count(x => x.TrangThai == BookingRequestStatus.CAN_BO_SUNG).ToString();
        _cardReview.Trend = "Đang chờ khách phản hồi";
        _cardConfirmed.Value = all.Count(x => x.TrangThai == BookingRequestStatus.DA_XAC_NHAN).ToString();
        _cardConfirmed.Trend = "Sẵn sàng chuyển Lịch chụp";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = _searchInput.Text.Trim();
        var items = DesktopDemoSessionStore.GetAllBookingRequests().AsEnumerable();
        if (_activeFilter == "CLOSED")
            items = items.Where(x => x.TrangThai is BookingRequestStatus.DA_TU_CHOI or BookingRequestStatus.KHACH_HUY);
        else if (_activeFilter != "ALL")
            items = items.Where(x => x.TrangThai.ToString() == _activeFilter);

        if (!string.IsNullOrWhiteSpace(kw))
        {
            items = items.Where(x =>
                x.MaYeuCau.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.TenKhachHang.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.SoDienThoai.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.TenGoi.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        _visible = items.ToList();
        _tableSummaryLabel.Text = $"{_visible.Count} yêu cầu hiển thị";

        _grid.DataSource = _visible.Select(x => new
        {
            MaYeuCau = x.MaYeuCau,
            KhachHang = x.TenKhachHang,
            GoiChup = x.TenGoi,
            NgayChup = x.NgayChupMongMuon.ToString("dd/MM/yyyy"),
            Gio = x.KhungGioMongMuon,
            TamTinh = $"{x.TongTamTinh:N0} đ",
            TrangThai = DesktopDemoSessionStore.GetStatusVietnameseLabel(x.TrangThai)
        }).ToList();

        if (_grid.Columns.Count >= 7)
        {
            _grid.Columns["MaYeuCau"]!.HeaderText = "Mã yêu cầu";
            _grid.Columns["KhachHang"]!.HeaderText = "Khách hàng";
            _grid.Columns["GoiChup"]!.HeaderText = "Gói chụp";
            _grid.Columns["NgayChup"]!.HeaderText = "Ngày chụp";
            _grid.Columns["Gio"]!.HeaderText = "Khung giờ";
            _grid.Columns["TamTinh"]!.HeaderText = "Tạm tính";
            _grid.Columns["TrangThai"]!.HeaderText = "Trạng thái";
            _grid.Columns["MaYeuCau"]!.FillWeight = 115f;
            _grid.Columns["KhachHang"]!.FillWeight = 125f;
            _grid.Columns["Gio"]!.FillWeight = 95f;
            _grid.Columns["TrangThai"]!.FillWeight = 110f;
        }

        BindSelectedDossier();
    }

    private CustomerBookingRequestDto? SelectedRequest()
    {
        if (_grid.CurrentRow is null) return _visible.FirstOrDefault();
        var code = _grid.CurrentRow.Cells["MaYeuCau"]?.Value?.ToString();
        return _visible.FirstOrDefault(x => x.MaYeuCau == code);
    }

    private void BindSelectedDossier()
    {
        var req = SelectedRequest();
        if (req is null)
        {
            _dossierHeader.Text = "Chưa chọn yêu cầu đặt lịch";
            _kvCustomer.Text = "Chọn một dòng ở bảng bên trái để xem hồ sơ chi tiết.";
            _kvPackage.Text = "-";
            _kvSchedule.Text = "-";
            _kvServices.Text = "-";
            _kvCustomerNote.Text = "-";
            _kvLinkedBooking.Text = "-";
            return;
        }

        _packageThumb.BindAsset(VisualAssetService.GetCoverEntry(req.MaGoiChup), VisualAssetService.GetGallery(req.MaGoiChup));
        _dossierHeader.Text = $"{req.MaYeuCau}  •  {DesktopDemoSessionStore.GetStatusVietnameseLabel(req.TrangThai)}";

        var svcs = req.ExtraServices.Count == 0
            ? "Không có"
            : string.Join(", ", req.ExtraServices.Select(s => $"{s.TenDichVu} ×{s.SoLuong}"));

        _kvCustomer.Text = $"{req.TenKhachHang} • {req.SoDienThoai} • {req.Email}";
        _kvPackage.Text = $"{req.TenGoi} ({req.MaGoiChup}) • {req.ThoiLuongPhut} phút • {req.TongTamTinh:N0} đ";
        _kvSchedule.Text = $"{req.NgayChupMongMuon:dd/MM/yyyy} ({req.KhungGioMongMuon}) • {req.TenPhongUuTien ?? "Chưa chọn"}";
        _kvServices.Text = svcs;
        _kvCustomerNote.Text = string.IsNullOrWhiteSpace(req.GhiChuKhachHang) ? "Không có" : req.GhiChuKhachHang;
        _kvLinkedBooking.Text = req.LinkedBookingCode ?? "Chưa chuyển";

        _conflictStatus.Text = req.ConflictCheckSummary;
        _conflictStatus.ForeColor = req.HasConflictWarning ? Theme.Danger : Theme.Primitives.Sage700;
        _customerFeedbackBox.Text = req.PhanHoiChoKhach;
        _internalNoteBox.Text = req.GhiChuNoiBo;

        var preferRoom = req.AssignedRoomCode ?? req.MaPhongUuTien;
        if (!string.IsNullOrWhiteSpace(preferRoom))
        {
            for (var i = 0; i < _roomSelect.Items.Count; i++)
            {
                if (_roomSelect.Items[i]?.ToString()?.StartsWith(preferRoom, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _roomSelect.SelectedIndex = i;
                    break;
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(req.SuggestedPhotographerCode))
        {
            for (var i = 0; i < _photographerSelect.Items.Count; i++)
            {
                if (_photographerSelect.Items[i]?.ToString()?.StartsWith(req.SuggestedPhotographerCode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _photographerSelect.SelectedIndex = i;
                    break;
                }
            }
        }
        else if (_photographerSelect.Items.Count > 0)
        {
            _photographerSelect.SelectedIndex = 0;
        }
    }

    private async Task RunConflictCheckPreviewAsync()
    {
        var req = SelectedRequest();
        if (req is null) return;
        try
        {
            if (_app.Session is not null)
            {
                var dateBookings = await _app.Bookings.SearchAsync(new Application.BookingSearchFilter(
                    null, req.NgayChupMongMuon.Date, req.NgayChupMongMuon.Date));
                var roomTxt = _roomSelect.SelectedItem?.ToString() ?? "";
                var sameRoomCount = dateBookings.Count(b => roomTxt.Contains(b.Phong, StringComparison.OrdinalIgnoreCase));
                _conflictStatus.ForeColor = sameRoomCount > 0 ? Theme.WarningHover : Theme.Success;
                _conflictStatus.Text = sameRoomCount > 0
                    ? $"Cảnh báo (Đọc từ SQL): Ngày {req.NgayChupMongMuon:dd/MM} đã có {sameRoomCount} lịch dùng phòng này — kiểm tra giờ trước khi chốt!"
                    : $"Đã kiểm tra SQL (Đọc chỉ xem): Chưa thấy lịch trùng ngày {req.NgayChupMongMuon:dd/MM}.";
                return;
            }
        }
        catch
        {
            // Graceful Demo simulation fallback
        }
        _conflictStatus.ForeColor = Theme.Success;
        _conflictStatus.Text = $"Mô phỏng kiểm tra (Demo): Phòng & Nhiếp ảnh gia khả dụng ngày {req.NgayChupMongMuon:dd/MM} ({req.KhungGioMongMuon}).";
    }

    private void ApplyStaffTransition(BookingRequestStatus next)
    {
        var req = SelectedRequest();
        if (req is null) return;

        var fb = _customerFeedbackBox.Text.Trim();
        if (next is BookingRequestStatus.CAN_BO_SUNG or BookingRequestStatus.DA_TU_CHOI && string.IsNullOrWhiteSpace(fb))
        {
            AtelierNotificationService.ShowWarning(
                FindForm(),
                "Vui lòng nhập nội dung phản hồi rõ lý do gửi khách hàng trước khi cập nhật trạng thái.",
                "Thiếu phản hồi cho khách");
            _customerFeedbackBox.Focus();
            return;
        }

        if (next == BookingRequestStatus.DA_TU_CHOI &&
            !AtelierNotificationService.Confirm(
                FindForm(),
                "Xác nhận từ chối yêu cầu đặt lịch",
                $"Bạn chắc chắn muốn từ chối yêu cầu {req.MaYeuCau} của khách hàng {req.TenKhachHang}?",
                "Xác nhận từ chối",
                "Quay lại",
                isDestructive: true))
        {
            return;
        }

        var roomRaw = _roomSelect.SelectedItem?.ToString() ?? "P0001 — Phòng Ánh Sáng";
        var roomParts = roomRaw.Split('—', 2);
        var pgRaw = _photographerSelect.SelectedItem?.ToString() ?? "NV000004 — Phạm Gia Huy";
        var pgParts = pgRaw.Split('—', 2);

        DesktopDemoSessionStore.UpdateRequestFromStaff(
            req.MaYeuCau,
            next,
            _app.Session?.HoTen ?? "Nhân sự Tiếp nhận (Demo)",
            string.IsNullOrWhiteSpace(fb) ? $"Đã cập nhật sang {DesktopDemoSessionStore.GetStatusVietnameseLabel(next)}." : fb,
            _internalNoteBox.Text,
            pgParts[0].Trim(),
            pgParts.Length > 1 ? pgParts[1].Trim() : pgRaw,
            roomParts[0].Trim(),
            roomParts.Length > 1 ? roomParts[1].Trim() : roomRaw,
            _conflictStatus.Text,
            _conflictStatus.ForeColor == Theme.Danger);

        ReloadRequests();
        AtelierNotificationService.ShowToast(
            FindForm(),
            next == BookingRequestStatus.DA_XAC_NHAN ? AtelierNotificationKind.Success : AtelierNotificationKind.Info,
            "Đã cập nhật điều phối yêu cầu",
            next == BookingRequestStatus.DA_XAC_NHAN
                ? $"Đã xác nhận {req.MaYeuCau} và đồng bộ sang danh sách Lịch chụp của khách."
                : $"Đã chuyển {req.MaYeuCau} sang trạng thái {DesktopDemoSessionStore.GetStatusVietnameseLabel(next)}.");
    }

    private void ShowConversionPreview()
    {
        var req = SelectedRequest();
        if (req is null) return;
        AtelierNotificationService.ShowInfo(
            FindForm(),
            $"• Mã yêu cầu gốc: {req.MaYeuCau}\n" +
            $"• Khách hàng: {req.TenKhachHang} ({req.SoDienThoai})\n" +
            $"• Gói chụp: {req.TenGoi} ({req.MaGoiChup}) — {req.TamTinhGoi:N0} đ\n" +
            $"• Phòng dự kiến: {_roomSelect.SelectedItem}\n" +
            $"• Nhiếp ảnh gia: {_photographerSelect.SelectedItem}\n" +
            $"• Tổng tạm tính: {req.TongTamTinh:N0} đ\n" +
            $"• Trạng thái kiểm tra lịch: {_conflictStatus.Text}\n\n" +
            $"Lưu ý: Khi chuyển sang 'Xác nhận lịch', phiên Demo đã tự sinh Lịch chụp giả lập tương ứng. Chức năng ghi bảng LichChup SQL thật sẽ được tích hợp ở Backend.",
            "Xem trước chuyển đổi sang Lịch chụp");
    }
}
