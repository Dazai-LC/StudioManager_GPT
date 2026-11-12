using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Pages;

public sealed class BookingsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _search = Ui.SearchBox("Tìm mã lịch, tên khách hàng, số điện thoại...");
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Theme.Font(9.2f) };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Theme.Font(9.2f) };
    private readonly ComboBox _photographer = FilterBox();
    private readonly ComboBox _room = FilterBox();
    private readonly ComboBox _package = FilterBox();
    private readonly ComboBox _status = FilterBox();
    private readonly SegmentedTabBar _statusRibbon = new() { Dock = DockStyle.Fill };

    // Right-hand Live Inspector Controls
    private readonly Label _inspCode = new()
    {
        Dock = DockStyle.Fill,
        Text = "Chưa chọn lịch chụp",
        Font = Theme.Font(13.0f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _inspCustomer = new()
    {
        Dock = DockStyle.Bottom,
        Height = 20,
        Text = "Chọn một dòng bên trái để kiểm tra hồ sơ & điều phối",
        Font = Theme.Font(8.8f),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly BookingStageTrackControl _stageTrack = new() { Dock = DockStyle.Top, Height = 54 };
    private readonly VisualMediaPictureBox _bookingPackagePreview = new(VisualAssetService.GetCoverEntry("GC000001")) { Dock = DockStyle.Top, Height = 112, Margin = new Padding(0, 0, 0, 8) };
    private readonly DossierFieldList _dossierFields = new() { Dock = DockStyle.Top, Height = 212 };
    private readonly BookingFinancialGaugeControl _finGauge = new() { Dock = DockStyle.Top, Height = 104 };
    private readonly RoleBadgePill _inspCustomerPill = new() { Height = 24, AutoWidth = false, Width = 178 };
    private readonly RoleBadgePill _inspPhotographerPill = new() { Height = 24, AutoWidth = false, Width = 178 };
    private readonly ToolTip _toolTip = new();

    private ModernButton _edit = null!;
    private ModernButton _detail = null!;
    private ModernButton _progress = null!;
    private ModernButton _pay = null!;
    private ModernButton _refund = null!;
    private ModernButton _cancel = null!;

    private IReadOnlyList<LichChup> _items = [];
    private bool _suppressStatusSync;
    private BookingsTopHeaderPanel _topContainer = null!;

    public BookingsPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var advancedFilters = BuildAdvancedFilters();
        var topToolbar = BuildCommandToolbar(advancedFilters);
        var ribbonHost = BuildStatusPipelineRibbon();
        var workbench = BuildMasterDetailWorkbench();

        _topContainer = new BookingsTopHeaderPanel(topToolbar, ribbonHost, advancedFilters);
        Controls.Add(workbench);
        Controls.Add(_topContainer);
        _topContainer.SendToBack();
        workbench.BringToFront();

        ConfigureGridPainting();
        _grid.CellDoubleClick += async (_, _) => await DetailAsync();
        _grid.SelectionChanged += (_, _) => UpdateInspectorAndActions();
        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoadAsync();
            }
        };

        Load += async (_, _) =>
        {
            await LoadFiltersAsync();
            await LoadAsync();
        };
    }

    private Control BuildCommandToolbar(Control advancedFilters)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 8),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong
        };

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = Theme.Surface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156));

        _search.Dock = DockStyle.Fill;
        _search.Margin = Padding.Empty;

        var find = Theme.OutlineButton("Tìm kiếm", Theme.BorderStrong, IconKind.Search);
        find.Dock = DockStyle.Fill;
        find.Margin = new Padding(8, 0, 0, 0);
        find.Click += async (_, _) => await LoadAsync();

        var filterBtn = Theme.OutlineButton("Bộ lọc sâu", Theme.BorderStrong, IconKind.Filter);
        filterBtn.Dock = DockStyle.Fill;
        filterBtn.Margin = new Padding(8, 0, 0, 0);
        filterBtn.Click += (_, _) =>
        {
            advancedFilters.Visible = !advancedFilters.Visible;
            filterBtn.Text = advancedFilters.Visible ? "Ẩn bộ lọc" : "Bộ lọc sâu";
            _topContainer.UpdateLayout();
        };

        var refresh = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(8, 0, 0, 0);
        refresh.Click += async (_, _) =>
        {
            _search.Clear();
            _from.Checked = false;
            _to.Checked = false;
            SelectAll(_photographer);
            SelectAll(_room);
            SelectAll(_package);
            SelectAll(_status);
            _suppressStatusSync = true;
            _statusRibbon.SelectByKey("");
            _suppressStatusSync = false;
            await LoadAsync();
        };

        var export = Theme.Button("Xuất Excel", Theme.Excel, IconKind.Download);
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(8, 0, 0, 0);
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, "lich-chup");

        var create = Theme.Button("Tạo lịch mới", Theme.Primary, IconKind.Plus);
        create.Dock = DockStyle.Fill;
        create.Margin = new Padding(8, 0, 0, 0);
        create.Click += async (_, _) => await CreateAsync();

        row.Controls.Add(_search, 0, 0);
        row.Controls.Add(find, 1, 0);
        row.Controls.Add(filterBtn, 2, 0);
        row.Controls.Add(refresh, 3, 0);
        row.Controls.Add(export, 4, 0);
        row.Controls.Add(create, 5, 0);

        card.Controls.Add(row);
        return card;
    }

    private Control BuildStatusPipelineRibbon()
    {
        var host = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = Color.Transparent
        };

        _statusRibbon.SetItems([
            new("", "Tất cả lịch"),
            new("DA_DAT_LICH", "Đã đặt lịch"),
            new("DA_CHUP", "Đã chụp"),
            new("DANG_CHINH_SUA", "Đang chỉnh sửa"),
            new("CHO_GIAO_ANH", "Chờ giao ảnh"),
            new("DA_GIAO_ANH", "Đã giao ảnh"),
            new("HOAN_THANH", "Hoàn thành"),
            new("DA_HUY", "Đã hủy")
        ], "");

        _statusRibbon.SegmentSelected += async (_, seg) =>
        {
            if (_suppressStatusSync) return;
            _suppressStatusSync = true;
            for (var i = 0; i < _status.Items.Count; i++)
            {
                if (_status.Items[i] is FilterOption opt && string.Equals(opt.Code, seg.Key, StringComparison.OrdinalIgnoreCase))
                {
                    _status.SelectedIndex = i;
                    break;
                }
            }
            _suppressStatusSync = false;
            await LoadAsync();
        };

        host.Controls.Add(_statusRibbon);
        return host;
    }

    private Control BuildAdvancedFilters()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 84,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(0, 2, 0, 6),
            Visible = false,
            CornerRadius = 10,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.BorderStrong
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };

        flow.Controls.Add(Ui.Field("TỪ NGÀY", _from, 142));
        flow.Controls.Add(Ui.Field("ĐẾN NGÀY", _to, 142));
        RoleVisualResolver.AttachRoleIconComboBox(
            _photographer,
            item => item is FilterOption opt && opt.Id > 0
                ? RoleVisualResolver.ForPhotographer()
                : RoleVisualResolver.Unknown("Tất cả"),
            item => item is FilterOption opt ? opt.Name : item?.ToString() ?? string.Empty);
        flow.Controls.Add(Ui.Field("NHIẾP ẢNH GIA", _photographer, 185));
        flow.Controls.Add(Ui.Field("PHÒNG CHỤP", _room, 160));
        flow.Controls.Add(Ui.Field("GÓI CHỤP", _package, 180));
        flow.Controls.Add(Ui.Field("TRẠNG THÁI", _status, 165));

        _status.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressStatusSync) return;
            if (_status.SelectedItem is FilterOption opt)
            {
                _suppressStatusSync = true;
                _statusRibbon.SelectByKey(opt.Code);
                _suppressStatusSync = false;
            }
        };

        card.Controls.Add(flow);
        return card;
    }

    private Control BuildMasterDetailWorkbench()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var gridFrame = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(1),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10
        };
        _grid.Margin = Padding.Empty;
        gridFrame.Controls.Add(_grid);

        var inspector = BuildRightBookingInspector();

        split.Controls.Add(gridFrame, 0, 0);
        split.Controls.Add(inspector, 1, 0);
        return split;
    }

    private Control BuildRightBookingInspector()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            Padding = new Padding(18, 16, 18, 14),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 106,
            BackColor = Color.Transparent
        };
        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "HỒ SƠ LỊCH CHỤP & TÀI CHÍNH TRỰC TIẾP",
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var roleStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 3, 0, 2),
            BackColor = Color.Transparent
        };
        _inspCustomerPill.Margin = new Padding(0, 0, 6, 0);
        roleStrip.Controls.Add(_inspCustomerPill);
        roleStrip.Controls.Add(_inspPhotographerPill);

        header.Controls.Add(_inspCode);
        header.Controls.Add(_inspCustomer);
        header.Controls.Add(roleStrip);
        header.Controls.Add(overline);

        var scrollBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 6)
        };
        scrollBody.Controls.Add(_finGauge);
        scrollBody.Controls.Add(_dossierFields);
        scrollBody.Controls.Add(_bookingPackagePreview);
        scrollBody.Controls.Add(_stageTrack);

        var isAdmin = _app.Session!.VaiTro == VaiTro.QuanTriVien;

        // Action Console at bottom of Inspector (Reflow: Row 0 has 3 buttons, Row 1 has 3 for Admin or 2 for Staff)
        var actionConsole = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 96,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };

        var row0 = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        row0.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
        row0.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        row0.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            ColumnCount = isAdmin ? 3 : 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        if (isAdmin)
        {
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        }
        else
        {
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        }

        _progress = Theme.Button("Bước tiếp", Theme.Primary, IconKind.Check);
        _progress.Dock = DockStyle.Fill;
        _progress.Margin = new Padding(0, 2, 4, 2);
        _progress.Click += async (_, _) => await AdvanceAsync();

        _detail = Theme.OutlineButton("Hồ sơ & DV", Theme.BorderStrong, IconKind.Layers);
        _detail.Dock = DockStyle.Fill;
        _detail.Margin = new Padding(2, 2, 2, 2);
        _detail.Click += async (_, _) => await DetailAsync();

        _edit = Theme.OutlineButton("Sửa lịch", Theme.BorderStrong, IconKind.Edit);
        _edit.Dock = DockStyle.Fill;
        _edit.Margin = new Padding(4, 2, 0, 2);
        _edit.Click += async (_, _) => await EditAsync();

        _pay = Theme.Button("Thu tiền", Theme.Success, IconKind.Wallet);
        _pay.Dock = DockStyle.Fill;
        _pay.Margin = new Padding(0, 2, 4, 2);
        _pay.Click += async (_, _) => await PaymentAsync(false);

        _refund = Theme.Button("Hoàn tiền", Theme.Warning, IconKind.Wallet);
        _refund.Dock = DockStyle.Fill;
        _refund.Margin = new Padding(2, 2, 2, 2);
        _refund.Visible = isAdmin;
        _refund.Click += async (_, _) => await PaymentAsync(true);

        _cancel = Theme.Button("Hủy lịch", Theme.Danger, IconKind.Trash);
        _cancel.Dock = DockStyle.Fill;
        _cancel.Margin = new Padding(4, 2, 0, 2);
        _cancel.Click += async (_, _) => await CancelAsync();

        row0.Controls.Add(_progress, 0, 0);
        row0.Controls.Add(_detail, 1, 0);
        row0.Controls.Add(_edit, 2, 0);

        if (isAdmin)
        {
            row1.Controls.Add(_pay, 0, 0);
            row1.Controls.Add(_refund, 1, 0);
            row1.Controls.Add(_cancel, 2, 0);
        }
        else
        {
            row1.Controls.Add(_pay, 0, 0);
            row1.Controls.Add(_cancel, 1, 0);
        }

        actionConsole.Controls.Add(row1);
        actionConsole.Controls.Add(row0);

        card.Controls.Add(scrollBody);
        card.Controls.Add(header);
        card.Controls.Add(actionConsole);
        return card;
    }

    private void ConfigureGridPainting()
    {
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is null) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName == "Còn_lại" && e.Value is decimal remaining && remaining > 0)
            {
                e.CellStyle.ForeColor = Theme.Danger;
                e.CellStyle.SelectionForeColor = Theme.Danger;
                e.CellStyle.Font = Theme.Font(9.0f, FontStyle.Bold);
            }
            else if (colName == "Ma")
            {
                e.CellStyle.Font = Theme.MonoFont(8.8f, FontStyle.Bold);
            }
        };

        _grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "Trạng_thái") return;

            e.PaintBackground(e.ClipBounds, true);
            var statusStr = e.FormattedValue?.ToString() ?? "";
            if (!string.IsNullOrEmpty(statusStr) && e.Graphics is { } g)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var (fg, bg, border) = Theme.GetStatusColors(statusStr);
                using var font = Theme.Font(7.9f, FontStyle.Bold);
                var sz = TextRenderer.MeasureText(g, statusStr, font);
                var pillW = Math.Min(e.CellBounds.Width - 10, sz.Width + 16);
                var pillH = 23;
                var rect = new RectangleF(
                    e.CellBounds.Left + 6,
                    e.CellBounds.Top + (e.CellBounds.Height - pillH) / 2f,
                    pillW,
                    pillH);

                using var path = Theme.RoundedRectangle(rect, 11f);
                using var fill = new SolidBrush(bg);
                using var pen = new Pen(border, 1f);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);

                TextRenderer.DrawText(g, statusStr, font, Rectangle.Round(rect), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            e.Handled = true;
        };
    }

    private LichChup? Selected() =>
        _grid.CurrentRow?.Cells["Id"].Value is object v ? _items.FirstOrDefault(x => x.Id == Convert.ToInt64(v)) : null;

    private async Task LoadAsync()
    {
        try
        {
            var filter = new BookingSearchFilter(
                string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(),
                _from.Checked ? _from.Value.Date : null,
                _to.Checked ? _to.Value.Date : null,
                SelectedId(_photographer),
                SelectedId(_room),
                SelectedId(_package),
                SelectedStatus());

            _items = await _app.Bookings.SearchAsync(filter);

            _grid.DataSource = _items.Select(x => new
            {
                x.Id,
                Ma = x.Ma,
                Khách_hàng = x.KhachHang,
                Bắt_đầu = x.BatDau.ToString("dd/MM/yyyy HH:mm"),
                Kết_thúc = x.KetThuc.ToString("HH:mm"),
                Nhiếp_ảnh_gia = x.NhiepAnhGia,
                Phòng = x.Phong,
                Trạng_thái = x.TrangThai.HienThi(),
                Tổng_tiền = x.TongThanhToan,
                Còn_lại = x.ConLai
            }).ToList();

            if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;

            var captions = new Dictionary<string, string>
            {
                { "Ma", "Mã lịch" },
                { "Khách_hàng", "Khách hàng" },
                { "Bắt_đầu", "Bắt đầu" },
                { "Kết_thúc", "Kết thúc" },
                { "Nhiếp_ảnh_gia", "Nhiếp ảnh gia" },
                { "Phòng", "Phòng" },
                { "Trạng_thái", "Trạng thái" },
                { "Tổng_tiền", "Tổng tiền" },
                { "Còn_lại", "Còn nợ" }
            };

            foreach (var caption in captions)
                if (_grid.Columns.Contains(caption.Key)) _grid.Columns[caption.Key].HeaderText = caption.Value;

            if (_grid.Columns.Contains("Ma")) _grid.Columns["Ma"].FillWeight = 76;
            if (_grid.Columns.Contains("Kết_thúc")) _grid.Columns["Kết_thúc"].FillWeight = 58;
            if (_grid.Columns.Contains("Trạng_thái")) _grid.Columns["Trạng_thái"].FillWeight = 115;

            foreach (var c in new[] { "Tổng_tiền", "Còn_lại" })
            {
                if (_grid.Columns.Contains(c))
                {
                    _grid.Columns[c].DefaultCellStyle.Format = "N0";
                    _grid.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            if (_grid.Rows.Count > 0)
            {
                _grid.Rows[0].Selected = true;
                var firstVisibleCol = _grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
                if (firstVisibleCol is not null)
                    _grid.CurrentCell = _grid.Rows[0].Cells[firstVisibleCol.Index];
            }
            UpdateInspectorAndActions();
        }
        catch (Exception ex)
        {
            Ui.Error(this, Ui.ReportError("BookingsPage", ex));
        }
    }

    private void UpdateInspectorAndActions()
    {
        var b = Selected();
        var hasSelection = b is not null;

        var isAdmin = _app.Session!.VaiTro == VaiTro.QuanTriVien;
        _detail.Enabled = hasSelection;
        _edit.Enabled = b is not null && (isAdmin
            ? b.TrangThai is not TrangThaiLich.HoanThanh and not TrangThaiLich.DaHuy
            : b.TrangThai == TrangThaiLich.DaDatLich);
        _progress.Enabled = b is not null && b.TrangThai.BuocTiepTheo() is not null;
        _pay.Enabled = b is not null && b.TrangThai != TrangThaiLich.DaHuy && b.ConLai > 0;
        _refund.Enabled = _refund.Visible && b is not null && b.TrangThai == TrangThaiLich.DaHuy && b.DaThu - b.DaHoan > 0;
        _cancel.Enabled = b is not null && BusinessRules.DuocHuy(b.TrangThai, _app.Session!.VaiTro, b.BatDau, DateTime.Now);

        if (b is null)
        {
            _inspCode.Text = "Chưa chọn lịch chụp";
            _inspCustomer.Text = "Chọn một lịch ở bảng bên trái để xem hồ sơ chi tiết";
            _inspCustomerPill.SetRole(RoleVisualResolver.ForCustomer(), _toolTip, "Khách hàng: —");
            _inspPhotographerPill.SetRole(RoleVisualResolver.ForPhotographer(), _toolTip, "Nhiếp ảnh: —");
            _stageTrack.SetBooking(null);
            _dossierFields.SetRows([
                ("Khách hàng", "—", null, false),
                ("Gói chụp", "—", null, false),
                ("Khung giờ", "—", null, false),
                ("Nhiếp ảnh gia", "—", null, false),
                ("Phòng chụp", "—", null, false),
                ("Ghi chú", "—", null, false)
            ]);
            _finGauge.SetBooking(null);
            _bookingPackagePreview.BindAsset(VisualAssetService.GetCoverEntry("GC000001"));
            return;
        }

        _inspCode.Text = $"#{b.Ma}  •  {b.TrangThai.HienThi()}";
        _inspCustomer.Text = $"Khách hàng: {b.KhachHang}";
        _inspCustomerPill.SetRole(RoleVisualResolver.ForCustomer(tooltipDetail: $"Khách hàng đặt lịch: {b.KhachHang}"), _toolTip, b.KhachHang);
        _inspPhotographerPill.SetRole(RoleVisualResolver.ForPhotographer(tooltipDetail: $"Nhiếp ảnh gia phụ trách: {b.NhiepAnhGia}"), _toolTip, b.NhiepAnhGia);
        _stageTrack.SetBooking(b);

        var nextStep = b.TrangThai.BuocTiepTheo();
        _progress.Text = nextStep is not null ? $"→ {nextStep.Value.HienThi()}" : "Đã kết thúc";

        _dossierFields.SetRows([
            ("Gói chụp chốt", $"{b.TenGoiChot} ({b.GiaGoiChot:N0} đ)", Theme.Primary, true),
            ("Thời gian chụp", $"{b.BatDau:dd/MM/yyyy HH:mm} - {b.KetThuc:HH:mm}", null, false),
            ("Nhiếp ảnh gia", b.NhiepAnhGia, RoleVisualResolver.ForPhotographer().Accent, true),
            ("Phòng chụp", b.Phong, null, false),
            ("Dịch vụ bổ sung", $"{b.TienDichVu:N0} đ", null, false),
            ("Giảm giá", b.TienGiam > 0 ? $"-{b.TienGiam:N0} đ ({b.LyDoGiam})" : "0 đ", b.TienGiam > 0 ? Theme.Warning : Theme.Muted, false),
            ("Ghi chú", string.IsNullOrWhiteSpace(b.GhiChu) ? "Không có ghi chú" : b.GhiChu, Theme.TextSecondary, false)
        ]);

        _finGauge.SetBooking(b);

        var pkgCode = SeedGroundedCatalogRepository.GetPackages()
            .FirstOrDefault(p => b.TenGoiChot.Contains(p.TenGoi, StringComparison.OrdinalIgnoreCase) ||
                                 p.TenGoi.Contains(b.TenGoiChot, StringComparison.OrdinalIgnoreCase))
            ?.MaGoiChup ?? "GC000001";
        _bookingPackagePreview.BindAsset(VisualAssetService.GetCoverEntry(pkgCode), VisualAssetService.GetGallery(pkgCode));
    }

    private async Task LoadFiltersAsync()
    {
        BindFilter(_photographer, await _app.BookingSupport.GetLookupsAsync("NhiepAnhGia", false));
        BindFilter(_room, await _app.BookingSupport.GetLookupsAsync("PhongChup", false));
        BindFilter(_package, await _app.BookingSupport.GetLookupsAsync("GoiChup", false));

        _status.DataSource = new[]
        {
            new FilterOption(0, "", "Tất cả trạng thái"),
            new FilterOption(0, "DA_DAT_LICH", "Đã đặt lịch"),
            new FilterOption(0, "DA_CHUP", "Đã chụp"),
            new FilterOption(0, "DANG_CHINH_SUA", "Đang chỉnh sửa ảnh"),
            new FilterOption(0, "CHO_GIAO_ANH", "Chờ giao ảnh"),
            new FilterOption(0, "DA_GIAO_ANH", "Đã giao ảnh"),
            new FilterOption(0, "HOAN_THANH", "Hoàn thành"),
            new FilterOption(0, "DA_HUY", "Đã hủy")
        };
        _status.DisplayMember = "Name";
    }

    private static ComboBox FilterBox() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.2f) };

    private static void BindFilter(ComboBox box, IReadOnlyList<LookupItem> list)
    {
        box.DataSource = (new[] { new FilterOption(0, "", "Tất cả") })
            .Concat(list.Select(x => new FilterOption((int)x.Id, "", $"{x.Code} • {x.Name}"))).ToList();
        box.DisplayMember = "Name";
    }

    private static void SelectAll(ComboBox box) { if (box.Items.Count > 0) box.SelectedIndex = 0; }
    private static int? SelectedId(ComboBox box) => box.SelectedItem is FilterOption option && option.Id > 0 ? option.Id : null;
    private string? SelectedStatus() => _status.SelectedItem is FilterOption option && !string.IsNullOrWhiteSpace(option.Code) ? option.Code : null;
    private sealed record FilterOption(int Id, string Code, string Name);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();

        base.Dispose(disposing);
    }

    private async Task CreateAsync()
    {
        using var f = new BookingDialog(_app);
        if (f.ShowDialog(this) != DialogResult.OK || f.Input is null) return;
        var r = await _app.Bookings.CreateAsync(f.Input, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await LoadAsync(); }
    }

    private async Task EditAsync()
    {
        var b = Selected();
        if (b is null) return;
        if (b.TrangThai is TrangThaiLich.HoanThanh or TrangThaiLich.DaHuy)
        {
            Ui.Error(this, "Không thể sửa lịch đã hoàn thành hoặc đã hủy.");
            return;
        }

        string? reason = null;
        if (b.TrangThai != TrangThaiLich.DaDatLich)
        {
            if (_app.Session!.VaiTro != VaiTro.QuanTriVien)
            {
                Ui.Error(this, "Chỉ được sửa thông tin cốt lõi khi lịch đang ở trạng thái 'Đã đặt lịch'.");
                return;
            }
            using var reasonDialog = new TextPrompt("Lý do hiệu chỉnh ngoại lệ", "Nhập lý do hiệu chỉnh ngoại lệ:");
            if (reasonDialog.ShowDialog(this) != DialogResult.OK) return;
            reason = reasonDialog.Value;
        }

        using var f = new BookingDialog(_app, b);
        if (f.ShowDialog(this) != DialogResult.OK || f.Input is null) return;
        var r = await _app.Bookings.UpdateAsync(b.Id, f.Input, reason, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await LoadAsync(); }
    }

    private async Task DetailAsync()
    {
        var b = Selected();
        if (b is null) return;
        using var d = new BookingDetailDialog(_app, b);
        d.ShowDialog(this);
        await LoadAsync();
    }

    private async Task AdvanceAsync()
    {
        var b = Selected();
        if (b is null) return;
        var next = b.TrangThai.BuocTiepTheo();
        if (next is null)
        {
            Ui.Error(this, "Lịch đã ở trạng thái kết thúc.");
            return;
        }

        if (!AtelierNotificationService.Confirm(
                this,
                "Cập nhật tiến độ lịch chụp",
                $"Xác nhận chuyển lịch #{b.Ma} sang trạng thái “{next.Value.HienThi()}”?",
                confirmText: "Chuyển trạng thái",
                cancelText: "Giữ nguyên"))
            return;

        var r = await _app.Bookings.AdvanceAsync(b.Id, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else await LoadAsync();
    }

    private async Task CancelAsync()
    {
        var b = Selected();
        if (b is null) return;
        using var d = new TextPrompt("Hủy lịch chụp", "Nhập lý do hủy lịch chụp:");
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var r = await _app.Bookings.CancelAsync(b.Id, d.Value, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await LoadAsync(); }
    }

    private async Task PaymentAsync(bool refund)
    {
        var b = Selected();
        if (b is null) return;
        var maximum = refund ? Math.Max(0, b.DaThu - b.DaHoan) : b.ConLai;
        if (maximum <= 0)
        {
            Ui.Error(this, refund ? "Không còn số tiền có thể hoàn cho lịch này." : "Lịch này đã thanh toán đủ, không thể thu thêm.");
            return;
        }

        using var d = new MoneyDialog(refund ? "Hoàn tiền lịch chụp" : "Ghi nhận thanh toán", maximum, refund);
        if (d.ShowDialog(this) != DialogResult.OK) return;

        Result r = refund
            ? await _app.Finance.RefundAsync(new(b.Id, d.Amount, d.Note, d.Note), _app.Session!)
            : await _app.Finance.ReceiveAsync(new(b.Id, d.PaymentType, d.Amount, d.Note), _app.Session!);

        if (!r.Success) Ui.Error(this, r.Message);
        else
        {
            var updated = await _app.Bookings.GetAsync(b.Id) ?? b;
            await LoadAsync();
            using var receipt = new ReceiptPreviewForm(updated, d.Amount, refund, _app.Session!.HoTen, d.Note);
            receipt.ShowDialog(this);
        }
    }
}

/// <summary>
/// 6-Stage Horizontal Workflow Progress Track for the Live Booking Inspector.
/// </summary>
internal sealed class BookingStageTrackControl : Control
{
    private static readonly (TrangThaiLich Stage, string ShortName)[] Stages =
    [
        (TrangThaiLich.DaDatLich, "Đặt lịch"),
        (TrangThaiLich.DaChup, "Đã chụp"),
        (TrangThaiLich.DangChinhSuaAnh, "Hậu kỳ"),
        (TrangThaiLich.ChoGiaoAnh, "Chờ giao"),
        (TrangThaiLich.DaGiaoAnh, "Đã giao"),
        (TrangThaiLich.HoanThanh, "Hoàn tất")
    ];

    private LichChup? _booking;

    public BookingStageTrackControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    public void SetBooking(LichChup? booking)
    {
        _booking = booking;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (_booking?.TrangThai == TrangThaiLich.DaHuy)
        {
            var cancelBox = new RectangleF(2, 8, Width - 4, 32);
            using var cPath = Theme.RoundedRectangle(cancelBox, 8f);
            using var cFill = new SolidBrush(Theme.DangerLight);
            using var cBorder = new Pen(Theme.DangerBorder, 1f);
            g.FillPath(cFill, cPath);
            g.DrawPath(cBorder, cPath);
            using var cFont = Theme.Font(8.5f, FontStyle.Bold);
            TextRenderer.DrawText(g, "LỊCH CHỤP ĐÃ HỦY — KHÔNG CÒN TRONG ĐƯỜNG ỐNG VẬN HÀNH", cFont,
                Rectangle.Round(cancelBox), Theme.Danger,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var activeIndex = _booking is null
            ? -1
            : Array.FindIndex(Stages, x => x.Stage == _booking.TrangThai);

        var count = Stages.Length;
        var slotW = Width / (float)count;
        const float nodeY = 14f;
        const float r = 8f;

        for (var i = 0; i < count - 1; i++)
        {
            var x1 = slotW * i + slotW / 2f + r;
            var x2 = slotW * (i + 1) + slotW / 2f - r;
            var passed = activeIndex > i;
            using var pen = new Pen(passed ? Theme.Success : Theme.Primitives.Alabaster200, 2.5f);
            g.DrawLine(pen, x1, nodeY, x2, nodeY);
        }

        using var font = Theme.Font(7.2f, FontStyle.Bold);
        for (var i = 0; i < count; i++)
        {
            var cx = slotW * i + slotW / 2f;
            var isDone = activeIndex > i || (_booking?.TrangThai == TrangThaiLich.HoanThanh && i == count - 1);
            var isCurrent = activeIndex == i && !isDone;

            var fill = isDone ? Theme.Success : (isCurrent ? Theme.Primary : Theme.Primitives.Alabaster200);
            using (var b = new SolidBrush(fill))
            {
                g.FillEllipse(b, cx - r, nodeY - r, r * 2, r * 2);
            }

            if (isCurrent)
            {
                using var ring = new Pen(Color.FromArgb(90, Theme.Primary), 3f);
                g.DrawEllipse(ring, cx - r - 2, nodeY - r - 2, (r + 2) * 2, (r + 2) * 2);
            }

            var lblColor = isCurrent ? Theme.Primary : (isDone ? Theme.Success : Theme.Muted);
            var textRect = new Rectangle((int)(cx - slotW / 2f), 26, (int)slotW, 18);
            TextRenderer.DrawText(g, Stages[i].ShortName, font, textRect, lblColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}

/// <summary>
/// Financial Ledger & Payment Progress Gauge for the Live Booking Inspector.
/// </summary>
internal sealed class BookingFinancialGaugeControl : Control
{
    private LichChup? _booking;

    public BookingFinancialGaugeControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    public void SetBooking(LichChup? booking)
    {
        _booking = booking;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var box = new RectangleF(0.5f, 4f, Math.Max(10, Width - 1), Math.Max(10, Height - 8));
        using var path = Theme.RoundedRectangle(box, 9f);
        using var bg = new SolidBrush(Theme.SurfaceWarm);
        using var border = new Pen(Theme.BorderStrong, 1f);
        g.FillPath(bg, path);
        g.DrawPath(border, path);

        using var headFont = Theme.MonoFont(7.5f, FontStyle.Bold);
        using var totalFont = Theme.Font(11.8f, FontStyle.Bold);
        using var subFont = Theme.Font(8.2f, FontStyle.Bold);

        var total = _booking?.TongThanhToan ?? 0m;
        var netPaid = _booking is null ? 0m : Math.Max(0m, _booking.DaThu - _booking.DaHoan);
        var remain = _booking?.ConLai ?? 0m;
        var ratio = total > 0 ? (float)Math.Clamp(netPaid / total, 0m, 1m) : 0f;

        var headH = Math.Max(16, headFont.Height + 2);
        var midH = Math.Max(24, totalFont.Height + 2);
        var botH = Math.Max(18, subFont.Height + 2);

        TextRenderer.DrawText(g, "TIẾN ĐỘ THANH TOÁN", headFont, new Rectangle(12, 8, Width - 24, headH),
            Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var midY = 8 + headH + 1;
        var halfW = Math.Max(60, (Width - 28) / 2);

        TextRenderer.DrawText(g, $"Thực thu: {netPaid:N0} đ", subFont, new Rectangle(12, midY, halfW, midH),
            Theme.Success, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(g, $"Tổng: {total:N0} đ", totalFont, new Rectangle(12 + halfW, midY, Width - 24 - halfW, midH),
            Theme.Text, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        // Progress Bar
        var barY = midY + midH + 4;
        var barW = Math.Max(40f, Width - 24f);
        var trackRect = new RectangleF(12, barY, barW, 8f);
        using (var tPath = Theme.RoundedRectangle(trackRect, 4f))
        using (var tBrush = new SolidBrush(Theme.Primitives.Alabaster200))
        {
            g.FillPath(tBrush, tPath);
        }

        if (ratio > 0)
        {
            var fillW = Math.Max(8f, barW * ratio);
            using var fPath = Theme.RoundedRectangle(new RectangleF(12, barY, fillW, 8f), 4f);
            using var fBrush = new SolidBrush(remain <= 0 ? Theme.Success : Theme.Primary);
            g.FillPath(fBrush, fPath);
        }

        var remainStr = remain > 0 ? $"Còn phải thu: {remain:N0} đ" : "Đã thanh toán hoàn tất (0 đ)";
        TextRenderer.DrawText(g, remainStr, subFont, new Rectangle(12, barY + 11, Width - 24, botH),
            remain > 0 ? Theme.Danger : Theme.Success,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
}

internal sealed class BookingDetailDialog : Form
{
    private readonly AppFacade _app;
    private LichChup _booking;
    private readonly DataGridView _services = Theme.Grid();
    private readonly DataGridView _resources = Theme.Grid();
    private readonly DataGridView _payments = Theme.Grid();

    private readonly DossierFieldList _leftSummary = new() { Dock = DockStyle.Fill };
    private readonly DossierFieldList _rightSummary = new() { Dock = DockStyle.Fill };
    private readonly SegmentedTabBar _sectionTabs = new() { Dock = DockStyle.Top, Height = 42 };
    private readonly Panel _tabBodyHost = new() { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };

    private Control _servicesPanel = null!;
    private Control _resourcesPanel = null!;
    private Control _paymentsPanel = null!;
    private ModernButton _reprintReceiptBtn = null!;

    public BookingDetailDialog(AppFacade app, LichChup booking)
    {
        _app = app;
        _booking = booking;
        Text = $"Hồ sơ điều phối lịch chụp #{booking.Ma} • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1060, 740);
        MinimumSize = new Size(940, 680);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();

        var summaryCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 12, 18, 12),
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4,
            AutoScroll = true
        };

        var summarySplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Surface
        };
        summarySplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        summarySplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        summarySplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        summarySplit.Controls.Add(_leftSummary, 0, 0);
        summarySplit.Controls.Add(_rightSummary, 1, 0);
        summaryCard.Controls.Add(summarySplit);

        _servicesPanel = BuildServicesPanel();
        _resourcesPanel = BuildResourcesPanel();
        _paymentsPanel = BuildPaymentsPanel();

        _sectionTabs.SetItems([
            new("services", "Dịch vụ bổ sung & Giảm giá", IconKind.Sparkles),
            new("resources", "Phân công Thiết bị & Tài nguyên", IconKind.Layers),
            new("payments", "Lịch sử Giao dịch Tài chính", IconKind.Wallet)
        ], "services");

        _sectionTabs.SegmentSelected += (_, seg) => SwitchDetailSection(seg.Key);

        var bottomWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        bottomWrap.Controls.Add(_tabBodyHost);
        bottomWrap.Controls.Add(_sectionTabs);
        _sectionTabs.SendToBack();
        _tabBodyHost.BringToFront();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 226));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(summaryCard, 0, 0);
        layout.Controls.Add(bottomWrap, 0, 1);
        Controls.Add(layout);

        _payments.SelectionChanged += (_, _) => UpdateReprintState();
        _payments.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) OpenHistoricalReceipt();
        };

        SwitchDetailSection("services");
        UpdateSummary();
        Load += async (_, _) => await ReloadAsync();
    }

    private void SwitchDetailSection(string key)
    {
        _tabBodyHost.SuspendLayout();
        _tabBodyHost.Controls.Clear();
        var target = key switch
        {
            "resources" => _resourcesPanel,
            "payments" => _paymentsPanel,
            _ => _servicesPanel
        };
        target.Dock = DockStyle.Fill;
        _tabBodyHost.Controls.Add(target);
        _tabBodyHost.ResumeLayout(true);
    }

    private Control BuildServicesPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var bar = DetailToolbar();
        var add = Theme.Button("Thêm dịch vụ", Theme.Primary, IconKind.Plus);
        add.Click += async (_, _) => await AddServiceAsync();
        var remove = Theme.Button("Xóa dịch vụ", Theme.Danger, IconKind.Trash);
        remove.Click += async (_, _) => await RemoveServiceAsync();
        var discount = Theme.Button("Nhập giảm giá", Theme.Warning, IconKind.Wallet);
        discount.Click += async (_, _) => await DiscountAsync();

        bar.Controls.Add(add);
        bar.Controls.Add(remove);
        bar.Controls.Add(discount);

        var dataHost = Ui.DataHost(_services);
        p.Controls.Add(dataHost);
        p.Controls.Add(bar);
        bar.SendToBack();
        dataHost.BringToFront();
        return p;
    }

    private Control BuildResourcesPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var bar = DetailToolbar();
        var add = Theme.Button("Phân công tài nguyên", Theme.Primary, IconKind.Plus);
        add.Click += async (_, _) => await AssignResourceAsync();
        var returned = Theme.Button("Đánh dấu đã trả", Theme.Success, IconKind.Check);
        returned.Click += async (_, _) => await UpdateResourceAsync(TrangThaiPhanCong.DaTra);
        var cancel = Theme.Button("Hủy phân công", Theme.Danger, IconKind.Trash);
        cancel.Click += async (_, _) => await UpdateResourceAsync(TrangThaiPhanCong.DaHuy);

        bar.Controls.Add(add);
        bar.Controls.Add(returned);
        bar.Controls.Add(cancel);

        var dataHost = Ui.DataHost(_resources);
        p.Controls.Add(dataHost);
        p.Controls.Add(bar);
        bar.SendToBack();
        dataHost.BringToFront();
        return p;
    }

    private Control BuildPaymentsPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var bar = DetailToolbar();

        _reprintReceiptBtn = Theme.OutlineButton("Xem / In lại biên nhận", Theme.BorderStrong, IconKind.Printer);
        _reprintReceiptBtn.Enabled = false;
        _reprintReceiptBtn.Click += (_, _) => OpenHistoricalReceipt();

        bar.Controls.Add(_reprintReceiptBtn);

        var dataHost = Ui.DataHost(_payments);
        p.Controls.Add(dataHost);
        p.Controls.Add(bar);
        bar.SendToBack();
        dataHost.BringToFront();
        return p;
    }

    private static FlowLayoutPanel DetailToolbar() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true,
        Padding = new Padding(0, 0, 0, 6),
        BackColor = Color.Transparent
    };

    private async Task ReloadAsync()
    {
        _booking = await _app.Bookings.GetAsync(_booking.Id) ?? _booking;
        UpdateSummary();
        Bind(_services, await _app.BookingSupport.GetChildrenAsync(_booking.Id, "DichVu"));
        Bind(_resources, await _app.BookingSupport.GetChildrenAsync(_booking.Id, "TaiNguyen"));
        var thu = await _app.BookingSupport.GetChildrenAsync(_booking.Id, "ThanhToan");
        var hoan = await _app.BookingSupport.GetChildrenAsync(_booking.Id, "HoanTien");
        Bind(_payments, thu.Select(x => TransactionRow(x, false)).Concat(hoan.Select(x => TransactionRow(x, true))).Cast<IDictionary<string, object?>>().ToList());
        UpdateReprintState();
    }

    private static Dictionary<string, object?> TransactionRow(IDictionary<string, object?> row, bool refund)
    {
        var rawType = row.TryGetValue("Loại", out var raw) && raw is not null ? raw.ToString() : null;
        var type = refund ? "Hoàn tiền" : rawType switch
        {
            "DAT_COC" => "Đặt cọc",
            "BO_SUNG" => "Thu bổ sung",
            "CON_LAI" => "Thanh toán còn lại",
            _ => rawType ?? "Thanh toán"
        };
        var note = refund && row.TryGetValue("Lý do", out var reason) ? reason : row.TryGetValue("Ghi chú", out var remark) ? remark : null;
        return new()
        {
            ["Id"] = row.TryGetValue("Id", out var id) ? id : null,
            ["Mã"] = row.TryGetValue("Mã", out var code) ? code : null,
            ["Loại"] = type,
            ["Số tiền"] = row.TryGetValue("Số tiền", out var amount) ? amount : null,
            ["Ngày"] = row.TryGetValue("Ngày", out var date) ? date : null,
            ["Ghi chú"] = note,
            ["IsRefund"] = refund
        };
    }

    private static void Bind(DataGridView grid, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        var dt = new System.Data.DataTable();
        if (rows.Count > 0)
        {
            foreach (var k in rows.SelectMany(x => x.Keys).Distinct()) dt.Columns.Add(k, typeof(object));
            foreach (var r in rows)
            {
                var x = dt.NewRow();
                foreach (var a in r) x[a.Key] = a.Value ?? DBNull.Value;
                dt.Rows.Add(x);
            }
        }
        grid.DataSource = dt;
        if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
        if (grid.Columns.Contains("IsRefund")) grid.Columns["IsRefund"].Visible = false;
        if (grid.Columns.Contains("Số tiền"))
        {
            grid.Columns["Số tiền"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["Số tiền"].DefaultCellStyle.Format = "N0";
        }
        if (grid.Columns.Contains("Ngày"))
        {
            grid.Columns["Ngày"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
    }

    private void UpdateReprintState()
    {
        if (_reprintReceiptBtn is null) return;
        var row = _payments.CurrentRow;
        if (row is null || row.Index < 0)
        {
            _reprintReceiptBtn.Enabled = false;
            return;
        }

        var isRefund = IsRefundRow(row);
        if (isRefund && _app.Session!.VaiTro != VaiTro.QuanTriVien)
        {
            _reprintReceiptBtn.Enabled = false;
            return;
        }

        _reprintReceiptBtn.Enabled = true;
    }

    private static bool IsRefundRow(DataGridViewRow row)
    {
        if (row.Cells["IsRefund"].Value is bool b) return b;
        var type = row.Cells["Loại"].Value?.ToString() ?? "";
        return string.Equals(type, "Hoàn tiền", StringComparison.OrdinalIgnoreCase);
    }

    private void OpenHistoricalReceipt()
    {
        var row = _payments.CurrentRow;
        if (row is null || row.Index < 0)
        {
            Ui.Error(this, "Vui lòng chọn một dòng giao dịch trong bảng lịch sử.");
            return;
        }

        var isRefund = IsRefundRow(row);
        if (isRefund && _app.Session!.VaiTro != VaiTro.QuanTriVien)
        {
            Ui.Error(this, "Chỉ Quản trị viên mới có quyền xem hoặc in lại phiếu hoàn tiền.");
            return;
        }

        var code = row.Cells["Mã"].Value?.ToString() ?? "[Chưa cấp mã]";
        var type = row.Cells["Loại"].Value?.ToString() ?? (isRefund ? "Hoàn tiền" : "Thanh toán");
        var amountVal = row.Cells["Số tiền"].Value;
        var amount = 0m;
        if (amountVal is decimal d) amount = d;
        else if (amountVal is not null) decimal.TryParse(amountVal.ToString(), out amount);

        DateTime? date = null;
        var dateVal = row.Cells["Ngày"].Value;
        if (dateVal is DateTime dt) date = dt;
        else if (dateVal is not null && DateTime.TryParse(dateVal.ToString(), out var parsedDate)) date = parsedDate;

        var note = row.Cells["Ghi chú"].Value?.ToString() ?? "";

        using var receipt = new ReceiptPreviewForm(
            _booking,
            amount,
            isRefund,
            "[Không có trong dữ liệu lịch sử]",
            note,
            transactionCode: code,
            transactionType: type,
            transactionDate: date,
            isReprint: true);

        receipt.ShowDialog(this);
    }

    private async Task AddServiceAsync()
    {
        var list = await _app.BookingSupport.GetLookupsAsync("DichVu");
        using var d = new SelectQuantityDialog("Thêm dịch vụ bổ sung", list, false);
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var r = await _app.BookingSupport.AddServiceAsync(_booking.Id, (int)d.SelectedId, d.Quantity, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else await ReloadAsync();
    }

    private async Task RemoveServiceAsync()
    {
        var id = SelectedChildId(_services);
        if (id is null) return;
        if (!AtelierNotificationService.Confirm(
                this,
                "Xác nhận xóa dịch vụ",
                "Xóa dịch vụ đã chọn? Hệ thống sẽ từ chối nếu làm tổng phải thu thấp hơn số đã thu.",
                confirmText: "Xóa dịch vụ",
                cancelText: "Quay lại",
                isDestructive: true))
            return;
        var r = await _app.BookingSupport.RemoveServiceAsync(_booking.Id, id.Value, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else await ReloadAsync();
    }

    private async Task AssignResourceAsync()
    {
        var list = await _app.BookingSupport.GetLookupsAsync("TaiNguyen");
        using var d = new SelectQuantityDialog("Phân công tài nguyên", list, true);
        if (d.ShowDialog(this) != DialogResult.OK) return;

        var selected = list.FirstOrDefault(x => x.Id == d.SelectedId);
        var addRental = false;
        if (selected?.Extra?.Contains("Dịch vụ thuê:", StringComparison.OrdinalIgnoreCase) == true)
        {
            addRental = AtelierNotificationService.Confirm(
                this,
                "Xác nhận dịch vụ thuê",
                $"{selected.Name} có {selected.Extra.Split("Dịch vụ thuê:", StringSplitOptions.None).Last().Trim()}.\n\nThêm dịch vụ thuê tương ứng vào lịch với số lượng {d.Quantity:0}?",
                confirmText: "Thêm dịch vụ thuê",
                cancelText: "Chỉ phân công tài nguyên");
        }

        var r = await _app.BookingSupport.AssignResourceAsync(_booking.Id, (int)d.SelectedId, (int)d.Quantity, addRental, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await ReloadAsync(); }
    }

    private async Task DiscountAsync()
    {
        using var d = new DiscountDialog(_booking.TienGiam);
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var r = await _app.BookingSupport.SetDiscountAsync(_booking.Id, d.Amount, d.Reason, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await ReloadAsync(); }
    }

    private async Task UpdateResourceAsync(TrangThaiPhanCong next)
    {
        var id = SelectedChildId(_resources);
        if (id is null) return;
        var action = next == TrangThaiPhanCong.DaTra ? "đánh dấu tài nguyên đã trả" : "hủy phân công tài nguyên";
        if (!AtelierNotificationService.Confirm(
                this,
                "Xác nhận cập nhật tài nguyên",
                $"Xác nhận {action}?",
                confirmText: "Xác nhận",
                cancelText: "Hủy",
                isDestructive: next != TrangThaiPhanCong.DaTra))
            return;
        var r = await _app.BookingSupport.UpdateResourceAssignmentAsync(id.Value, next, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else await ReloadAsync();
    }

    private static long? SelectedChildId(DataGridView grid) =>
        grid.CurrentRow?.Cells["Id"].Value is object value ? Convert.ToInt64(value) : null;

    private void UpdateSummary()
    {
        _leftSummary.SetRows([
            ("Mã lịch & Trạng thái", $"#{_booking.Ma}  •  {_booking.TrangThai.HienThi()}", Theme.Primary, true),
            ("Khách hàng", _booking.KhachHang, null, false),
            ("Gói chụp chốt", _booking.TenGoiChot, null, false),
            ("Khung giờ chụp", $"{_booking.BatDau:dd/MM/yyyy HH:mm} → {_booking.KetThuc:dd/MM/yyyy HH:mm}", null, false),
            ("Nhiếp ảnh gia & Phòng", $"{_booking.NhiepAnhGia}  •  {_booking.Phong}", null, false)
        ]);

        _rightSummary.SetRows([
            ("Tạm tính (Gói + Dịch vụ)", $"{_booking.TamTinh:N0} đ", null, false),
            ("Chiết khấu / Giảm giá", $"{_booking.TienGiam:N0} đ", _booking.TienGiam > 0 ? Theme.Warning : null, false),
            ("Tổng phải thanh toán", $"{_booking.TongThanhToan:N0} đ", Theme.Primary, true),
            ("Đã thu / Đã hoàn", $"{_booking.DaThu:N0} đ  /  {_booking.DaHoan:N0} đ", Theme.Success, false),
            ("Công nợ còn lại", $"{_booking.ConLai:N0} đ", _booking.ConLai > 0 ? Theme.Danger : Theme.Success, false)
        ]);
    }
}

internal sealed class SelectQuantityDialog : Form
{
    private readonly ComboBox _item = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
    private readonly NumericUpDown _quantity = new() { Minimum = 1, Maximum = 100, DecimalPlaces = 0, Font = Theme.Font(9.5f) };
    public long SelectedId => (long)(_item.SelectedValue ?? 0L);
    public decimal Quantity => _quantity.Value;

    public SelectQuantityDialog(string title, IReadOnlyList<LookupItem> list, bool integer)
    {
        Text = title + " • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 320);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _quantity.DecimalPlaces = integer ? 0 : 2;
        _item.DisplayMember = "Display";
        _item.ValueMember = "Id";
        _item.DataSource = list.Select(x => new Option(x.Id, $"{x.Code} • {x.Name} ({x.Extra})")).ToList();

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 3
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        body.Controls.Add(Ui.Field("HẠNG MỤC LỰA CHỌN", _item, 396));
        body.Controls.Add(Ui.Field("SỐ LƯỢNG ÁP DỤNG", _quantity, 396));

        var save = Theme.Button("Xác nhận áp dụng", Theme.Primary, IconKind.Check);
        save.Width = 396;
        save.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        body.Controls.Add(save);

        card.Controls.Add(body);
        Controls.Add(card);
        AcceptButton = save;
    }

    private sealed record Option(long Id, string Display);
}

internal sealed class DiscountDialog : Form
{
    private readonly NumericUpDown _amount = new() { Maximum = 10_000_000_000, ThousandsSeparator = true, Font = Theme.Font(10) };
    private readonly TextBox _reason = new() { Font = Theme.Font(9.5f) };
    public decimal Amount => _amount.Value;
    public string? Reason => string.IsNullOrWhiteSpace(_reason.Text) ? null : _reason.Text.Trim();

    public DiscountDialog(decimal current)
    {
        Text = "Thiết lập chiết khấu & giảm giá • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 325);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _amount.Value = current;

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = Theme.Warning,
            AccentStripeThickness = 3
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        body.Controls.Add(Ui.Field("SỐ TIỀN GIẢM GIÁ (VNĐ)", _amount, 396));
        body.Controls.Add(Ui.Field("LÝ DO GIẢM GIÁ (bắt buộc khi > 0)", _reason, 396));

        var save = Theme.Button("Lưu giảm giá", Theme.Primary, IconKind.Check);
        save.Width = 396;
        save.Click += (_, _) =>
        {
            if (Amount > 0 && Reason is null)
            {
                Ui.Error(this, "Vui lòng nhập lý do giảm giá.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        body.Controls.Add(save);

        card.Controls.Add(body);
        Controls.Add(card);
        AcceptButton = save;
    }
}

internal sealed class BookingDialog : Form
{
    private readonly AppFacade _app;
    private readonly LichChup? _existing;
    private readonly Dictionary<long, BookingPackageSnapshot> _packageQuotes = [];
    private readonly ComboBox _customer = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
    private readonly ComboBox _package = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
    private readonly ComboBox _photographer = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
    private readonly ComboBox _room = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true, Font = Theme.Font(9.5f) };
    private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true, Font = Theme.Font(9.5f) };
    private readonly TextBox _note = new() { Font = Theme.Font(9.5f) };
    private ModernButton _addCustomer = null!;

    public BookingInput? Input { get; private set; }

    public BookingDialog(AppFacade app, LichChup? existing = null)
    {
        _app = app;
        _existing = existing;
        Text = existing is null ? "Khởi tạo Lịch chụp mới • StudioManager" : $"Hiệu chỉnh Lịch chụp #{existing.Ma} • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(720, 520);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Padding = new Padding(16);

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 16),
            BackColor = Theme.Surface,
            CornerRadius = 12,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            Text = existing is null ? "Thiết lập thông tin ca chụp & phân bổ phòng máy" : $"Cập nhật thông tin lịch chụp #{existing.Ma}",
            Font = Theme.Font(12.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));

        var customerHost = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        customerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        customerHost.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 134f));
        customerHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _customer.Dock = DockStyle.Fill;
        _customer.Margin = new Padding(0, 2, 6, 0);
        RoleVisualResolver.AttachRoleIconComboBox(
            _customer,
            _ => RoleVisualResolver.ForCustomer(),
            item => item is LookupView lv ? lv.Display : item?.ToString() ?? string.Empty);
        RoleVisualResolver.AttachRoleIconComboBox(
            _photographer,
            _ => RoleVisualResolver.ForPhotographer(),
            item => item is LookupView lv ? lv.Display : item?.ToString() ?? string.Empty);

        _addCustomer = Theme.OutlineButton("Thêm khách hàng", Theme.Primary, IconKind.Plus);
        _addCustomer.Dock = DockStyle.Fill;
        _addCustomer.Margin = Padding.Empty;
        _addCustomer.Font = Theme.Font(8.4f, FontStyle.Bold);
        _addCustomer.Click += async (_, _) => await QuickCreateCustomerAsync();

        customerHost.Controls.Add(_customer, 0, 0);
        customerHost.Controls.Add(_addCustomer, 1, 0);

        var customerField = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 8),
            BackColor = Color.Transparent
        };
        customerField.Controls.Add(customerHost);
        customerField.Controls.Add(Ui.FieldLabel("KHÁCH HÀNG *"));

        static Panel CellField(string label, Control input, bool isLeft)
        {
            var f = Ui.Field(label, input, 320);
            f.Dock = DockStyle.Fill;
            f.Margin = isLeft ? new Padding(0, 0, 8, 8) : new Padding(8, 0, 0, 8);
            return f;
        }

        var noteField = Ui.Field("GHI CHÚ CONCEPT / YÊU CẦU ĐẶC BIỆT", _note, 660);
        noteField.Dock = DockStyle.Fill;
        noteField.Margin = new Padding(0, 0, 0, 8);

        body.Controls.Add(customerField, 0, 0);
        body.Controls.Add(CellField("GÓI CHỤP STUDIO *", _package, false), 1, 0);
        body.Controls.Add(CellField("THỜI GIAN BẮT ĐẦU *", _start, true), 0, 1);
        body.Controls.Add(CellField("THỜI GIAN KẾT THÚC *", _end, false), 1, 1);
        body.Controls.Add(CellField("NHIẾP ẢNH GIA PHỤ TRÁCH *", _photographer, true), 0, 2);
        body.Controls.Add(CellField("KHÔNG GIAN PHÒNG CHỤP *", _room, false), 1, 2);
        body.Controls.Add(noteField, 0, 3);
        body.SetColumnSpan(noteField, 2);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };

        var save = Theme.Button(existing is null ? "Tạo lịch chụp" : "Lưu thay đổi", Theme.Primary, IconKind.Check);
        save.Click += (_, _) => Save();

        var cancel = Theme.OutlineButton("Hủy bỏ", Theme.BorderStrong, IconKind.Close);
        cancel.Click += (_, _) => Close();

        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        card.Controls.Add(body);
        card.Controls.Add(title);
        card.Controls.Add(footer);
        title.SendToBack();
        footer.SendToBack();
        body.BringToFront();
        Controls.Add(card);

        _start.Value = existing?.BatDau ?? DateTime.Today.AddDays(1).AddHours(8);
        _end.Value = existing?.KetThuc ?? _start.Value.AddHours(2);
        _note.Text = existing?.GhiChu ?? "";

        Load += async (_, _) => await LoadLookupsAsync();
        AcceptButton = save;
    }

    private async Task QuickCreateCustomerAsync()
    {
        if (_addCustomer.Enabled == false) return;

        var previousIds = (_customer.DataSource as IEnumerable<LookupView>)?.Select(x => x.Id).ToHashSet()
            ?? new HashSet<long>();

        using var dialog = new SimpleEditDialog("KhachHang", null, null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _addCustomer.Enabled = false;
            var result = await _app.Administration.SaveAsync("KhachHang", null, dialog.Values, _app.Session!);
            if (!result.Success)
            {
                Ui.Error(this, result.Message);
                return;
            }

            Ui.Info(this, "Đã khởi tạo hồ sơ khách hàng thành công.");

            var updatedLookups = await _app.BookingSupport.GetLookupsAsync("KhachHang");
            Bind(_customer, updatedLookups);

            var newlyAdded = updatedLookups.FirstOrDefault(x => !previousIds.Contains(x.Id));
            if (newlyAdded is not null)
            {
                _customer.SelectedValue = newlyAdded.Id;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(this, Ui.ReportError("BookingsPage", ex));
        }
        finally
        {
            _addCustomer.Enabled = true;
        }
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            Bind(_customer, await _app.BookingSupport.GetLookupsAsync("KhachHang"));
            Bind(_package, await _app.BookingSupport.GetLookupsAsync("GoiChup", _existing is null));
            Bind(_photographer, await _app.BookingSupport.GetLookupsAsync("NhiepAnhGia", _existing is null));
            Bind(_room, await _app.BookingSupport.GetLookupsAsync("PhongChup", _existing is null));

            if (_existing is not null)
            {
                _customer.SelectedValue = _existing.KhachHangId;
                _package.SelectedValue = (long)_existing.GoiChupId;
                _photographer.SelectedValue = (long)_existing.NhiepAnhGiaId;
                _room.SelectedValue = (long)_existing.PhongChupId;
                // Use typed SQL values, never the culture-formatted price in a lookup label.
                var rows = await _app.Administration.QueryGridAsync("GoiChup", null, _app.Session!);
                foreach (var row in rows)
                {
                    if (row.TryGetValue("Trạng thái", out var state) && Equals(state, "DANG_AP_DUNG"))
                    {
                        var packageId = Convert.ToInt32(row["Id"]);
                        _packageQuotes[packageId] = new(packageId, Convert.ToString(row["Tên gói"]) ?? "", Convert.ToDecimal(row["Giá"]));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Ui.Error(this, Ui.ReportError("BookingsPage", ex));
        }
    }

    private static void Bind(ComboBox c, IReadOnlyList<LookupItem> list)
    {
        c.DisplayMember = "Display";
        c.ValueMember = "Id";
        c.DataSource = list.Select(x => new LookupView(x.Id, $"{x.Code} • {x.Name}")).ToList();
    }

    private void Save()
    {
        if (_customer.SelectedItem is not LookupView k ||
            _package.SelectedItem is not LookupView g ||
            _photographer.SelectedItem is not LookupView n ||
            _room.SelectedItem is not LookupView p)
        {
            Ui.Error(this, "Vui lòng chọn đầy đủ các trường thông tin bắt buộc (*).");
            return;
        }

        var input = new BookingInput(k.Id, (int)g.Id, _start.Value, _end.Value, (int)n.Id, (int)p.Id, string.IsNullOrWhiteSpace(_note.Text) ? null : _note.Text.Trim());
        if (_existing is not null && g.Id != _existing.GoiChupId)
        {
            if (!_packageQuotes.TryGetValue(g.Id, out var quote))
            {
                Ui.Error(this, "Gói được chọn không còn áp dụng hoặc chưa tải được báo giá. Vui lòng mở lại lịch và chọn gói khác.");
                return;
            }
            if (!AtelierNotificationService.Confirm(this, "Xác nhận đổi gói và báo giá",
                    $"Gói đã chốt: {_existing.TenGoiChot} — {_existing.GiaGoiChot:N0} đ.\n" +
                    $"Gói mới: {quote.Name} — {quote.Price:N0} đ.\n\n" +
                    "Xác nhận áp dụng báo giá mới? Các dịch vụ bổ sung và giảm giá vẫn được kiểm tra theo số tiền đã thu.",
                    confirmText: "Chấp nhận báo giá", cancelText: "Giữ lịch hiện tại")) return;
            using var quoteReason = new TextPrompt("Lý do đổi gói chụp", "Nhập lý do đổi gói và chấp nhận báo giá mới:");
            if (quoteReason.ShowDialog(this) != DialogResult.OK) return;
            if (quoteReason.Value.Length > 500)
            {
                Ui.Error(this, "Lý do đổi gói dài tối đa 500 ký tự.");
                return;
            }
            input = input with { PackageQuote = new(_existing.GoiChupId, _existing.GiaGoiChot, quote.Price, quoteReason.Value) };
        }
        Input = input;
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record LookupView(long Id, string Display);
}

internal sealed class TextPrompt : Form
{
    private readonly TextBox _text = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Font = Theme.Font(9.5f) };
    public string Value => _text.Text.Trim();

    public TextPrompt(string title, string label)
    {
        Text = title + " • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 270);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 3
        };

        card.Controls.Add(_text);
        _text.Dock = DockStyle.Fill;

        card.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 30,
            Font = Theme.Font(9.5f, FontStyle.Bold),
            ForeColor = Theme.Text
        });

        var ok = Theme.Button("Xác nhận", Theme.Primary, IconKind.Check);
        ok.Dock = DockStyle.Bottom;
        ok.Height = 40;
        ok.Margin = new Padding(0, 10, 0, 0);
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                Ui.Error(this, "Vui lòng nhập nội dung yêu cầu.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };

        card.Controls.Add(ok);
        Controls.Add(card);
        AcceptButton = ok;
    }
}

internal sealed class MoneyDialog : Form
{
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 0, ThousandsSeparator = true, Maximum = 10_000_000_000, Font = Theme.Font(10.5f, FontStyle.Bold) };
    private readonly TextBox _note = new() { Font = Theme.Font(9.5f) };
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };

    public decimal Amount => _amount.Value;
    public string Note => _note.Text.Trim();
    public string PaymentType => _type.SelectedItem?.ToString() ?? "BO_SUNG";

    public MoneyDialog(string title, decimal max, bool refund)
    {
        Text = title + " • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 425);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _amount.Maximum = Math.Max(1, max);
        _amount.Value = Math.Max(1, max);
        _type.Items.AddRange(["DAT_COC", "BO_SUNG", "CON_LAI"]);
        _type.SelectedIndex = refund ? 1 : 0;

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = refund ? Theme.Warning : Theme.Success,
            AccentStripeThickness = 4
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var banner = new CardPanel
        {
            Width = 416,
            Height = 44,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 8,
            BackColor = refund ? Theme.WarningLight : Theme.SuccessLight,
            BorderColor = refund ? Theme.WarningBorder : Theme.SuccessBorder
        };
        banner.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Hạn mức giao dịch hợp lệ tối đa: {max:N0} đ",
            Font = Theme.Font(9.0f, FontStyle.Bold),
            ForeColor = refund ? Theme.Warning : Theme.Success,
            TextAlign = ContentAlignment.MiddleLeft
        });

        body.Controls.Add(banner);
        body.Controls.Add(Ui.Field("SỐ TIỀN GIAO DỊCH (VNĐ) *", _amount, 416));
        if (!refund) body.Controls.Add(Ui.Field("PHÂN LOẠI KHOẢN THU", _type, 416));
        body.Controls.Add(Ui.Field(refund ? "LÝ DO HOÀN TIỀN *" : "GHI CHÚ GIAO DỊCH", _note, 416));

        var save = Theme.Button("Xác nhận giao dịch", refund ? Theme.Warning : Theme.Success, IconKind.Wallet);
        save.Width = 416;
        save.Click += (_, _) =>
        {
            if (refund && string.IsNullOrWhiteSpace(Note))
            {
                Ui.Error(this, "Vui lòng nhập lý do hoàn tiền.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        body.Controls.Add(save);

        card.Controls.Add(body);
        Controls.Add(card);
        AcceptButton = save;
    }
}

internal sealed class ReceiptPreviewForm : Form
{
    private readonly RichTextBox _preview = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.MonoFont(10.2f)
    };
    private readonly System.Drawing.Printing.PrintDocument _document = new();

    public ReceiptPreviewForm(
        LichChup booking,
        decimal amount,
        bool refund,
        string employee,
        string note,
        string? transactionCode = null,
        string? transactionType = null,
        DateTime? transactionDate = null,
        bool isReprint = false)
    {
        var titleType = refund ? "Phiếu hoàn tiền" : "Biên nhận thanh toán";
        Text = isReprint ? $"{titleType} (Bản in lại) • StudioManager" : $"{titleType} • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(680, 680);
        BackColor = Theme.Background;
        Padding = new Padding(20);
        Font = Theme.Font();

        var typeHeader = refund ? "PHIẾU HOÀN TIỀN KHÁCH HÀNG" : "BIÊN NHẬN THANH TOÁN DỊCH VỤ";
        if (isReprint) typeHeader += " (BẢN IN LẠI)";
        var separator = new string('═', 54);
        var subSep = new string('─', 54);

        var timeDisplay = (transactionDate ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss");
        var codeDisplay = !string.IsNullOrWhiteSpace(transactionCode) ? transactionCode : "[Chưa cấp mã]";
        var transTypeDisplay = !string.IsNullOrWhiteSpace(transactionType) ? transactionType : (refund ? "Hoàn tiền" : "Thanh toán");
        var noteDisplay = !string.IsNullOrWhiteSpace(note) ? note : "—";
        var staffDisplay = !string.IsNullOrWhiteSpace(employee) ? employee : "[Không có trong dữ liệu lịch sử]";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(separator);
        sb.AppendLine("           ATELIER LUMIÈRE • STUDIO MANAGER");
        sb.AppendLine($"           {typeHeader}");
        sb.AppendLine(separator);
        sb.AppendLine();
        if (isReprint || !string.IsNullOrWhiteSpace(transactionCode))
        {
            sb.AppendLine($"Mã giao dịch   : {codeDisplay}");
        }
        sb.AppendLine($"Mã lịch chụp   : {booking.Ma}");
        sb.AppendLine($"Khách hàng     : {booking.KhachHang}");
        sb.AppendLine($"Gói dịch vụ    : {booking.TenGoiChot}");
        sb.AppendLine($"Phân loại GD   : {transTypeDisplay}");
        sb.AppendLine($"Thời điểm GD   : {timeDisplay}");
        sb.AppendLine(subSep);
        sb.AppendLine($"SỐ TIỀN GD     : {amount:N0} VNĐ");
        sb.AppendLine($"Tổng dịch vụ   : {booking.TongThanhToan:N0} VNĐ");
        if (!isReprint)
        {
            sb.AppendLine($"Đã thu lũy kế  : {booking.DaThu:N0} VNĐ");
            sb.AppendLine($"Còn lại        : {booking.ConLai:N0} VNĐ");
        }
        sb.AppendLine(subSep);
        sb.AppendLine($"{(refund ? "Lý do hoàn" : "Ghi chú")}        : {noteDisplay}");
        sb.AppendLine($"Nhân viên lập  : {staffDisplay}");
        sb.AppendLine();
        sb.AppendLine(separator);
        if (isReprint)
        {
            sb.AppendLine("       Trích xuất từ dữ liệu lịch sử hệ thống");
        }
        else
        {
            sb.AppendLine("       Trân trọng cảm ơn quý khách đã đồng hành!");
        }
        sb.AppendLine(separator);

        _preview.Text = sb.ToString();

        var previewCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 12),
            CornerRadius = 10,
            BackColor = Theme.Primitives.Alabaster50,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };
        previewCard.Controls.Add(_preview);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };

        var print = Theme.Button("In phiếu biên nhận", Theme.Primary, IconKind.Printer);
        print.Width = 164;
        print.Click += (_, _) =>
        {
            using var p = new PrintPreviewDialog { Document = _document, Width = 900, Height = 700 };
            p.ShowDialog(this);
        };

        var close = Theme.OutlineButton("Đóng", Theme.BorderStrong, IconKind.Close);
        close.Width = 100;
        close.Click += (_, _) => Close();

        var multiFmt = Theme.OutlineButton("Xem đa khổ (A4 / K80 / K58)", Theme.Primary, IconKind.Eye);
        multiFmt.Width = 216;
        multiFmt.Click += (_, _) =>
        {
            var services = booking.TienDichVu > 0
                ? new[] { new BookingRequestServiceLineDto("DV_BO_SUNG", "Dịch vụ bổ sung theo lịch chụp", "Gói", booking.TienDichVu, 1) }
                : Array.Empty<BookingRequestServiceLineDto>();

            var doc = DesktopDemoSessionStore.BuildReceiptPreview(
                ReceiptFormatKind.A4Invoice,
                booking.Ma,
                booking.KhachHang,
                "Khách hàng studio",
                booking.TenGoiChot,
                booking.Phong,
                services,
                booking.GiaGoiChot,
                refund ? Math.Max(0, booking.DaThu) : amount,
                string.IsNullOrWhiteSpace(transTypeDisplay) ? "Thanh toán tại studio" : transTypeDisplay,
                staffDisplay);
            using var ext = new ExtendedReceiptPreviewForm(doc);
            ext.ShowDialog(this);
        };

        actions.Controls.Add(print);
        actions.Controls.Add(multiFmt);
        actions.Controls.Add(close);

        _document.PrintPage += (_, e) =>
        {
            if (e.Graphics is { } graphics)
                graphics.DrawString(_preview.Text, Theme.MonoFont(10.5f), Brushes.Black, new RectangleF(60, 60, e.MarginBounds.Width, e.MarginBounds.Height));
        };

        Controls.Add(previewCard);
        Controls.Add(actions);
    }
}

internal sealed class BookingsTopHeaderPanel : Panel
{
    private readonly Control _toolbar;
    private readonly Control _ribbon;
    private readonly Control _advancedFilters;

    public BookingsTopHeaderPanel(Control toolbar, Control ribbon, Control advancedFilters)
    {
        _toolbar = toolbar;
        _ribbon = ribbon;
        _advancedFilters = advancedFilters;
        Dock = DockStyle.Top;
        BackColor = Color.Transparent;

        Controls.Add(_advancedFilters);
        Controls.Add(_ribbon);
        Controls.Add(_toolbar);
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        int w = ClientSize.Width;
        int y = 0;

        _toolbar.SetBounds(0, y, w, _toolbar.Height);
        y += _toolbar.Height + 8;

        _ribbon.SetBounds(0, y, w, _ribbon.Height);
        y += _ribbon.Height + 8;

        if (_advancedFilters.Visible)
        {
            _advancedFilters.SetBounds(0, y, w, _advancedFilters.Height);
            y += _advancedFilters.Height + 8;
        }

        Height = y;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        UpdateLayout();
    }
}
