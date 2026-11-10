using System.ComponentModel;
using System.Drawing.Drawing2D;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class CrudPage : UserControl
{
    private readonly AppFacade _app;
    private readonly string _entity;
    private readonly bool _readOnly;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _search = Ui.SearchBox();

    private readonly Label _countBadge = new()
    {
        Dock = DockStyle.Right,
        Width = 130,
        Font = Theme.MonoFont(8.2f, FontStyle.Bold),
        ForeColor = Theme.Primary,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "0 bản ghi"
    };

    // Live Right-Hand Dossier Inspector Controls
    private readonly Label _dossierTitle = new()
    {
        Dock = DockStyle.Fill,
        Text = "Chưa chọn bản ghi",
        Font = Theme.Font(12.5f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
    private readonly Label _dossierSubtitle = new()
    {
        Dock = DockStyle.Bottom,
        Height = 20,
        Text = "Chọn một dòng ở bảng bên trái để xem hồ sơ chi tiết",
        Font = Theme.Font(8.6f),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
    private readonly DossierFieldList _dossierFields = new() { Dock = DockStyle.Top, Height = 240 };

    // Audit Diff Live Boxes (only visible when _entity == "NhatKy")
    private readonly RichTextBox _auditOldBox = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.TextSecondary,
        Font = Theme.MonoFont(8.6f),
        ScrollBars = RichTextBoxScrollBars.Vertical,
        WordWrap = true
    };
    private readonly RichTextBox _auditNewBox = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Primitives.Alabaster50,
        ForeColor = Theme.Text,
        Font = Theme.MonoFont(8.6f),
        ScrollBars = RichTextBoxScrollBars.Vertical,
        WordWrap = true
    };

    // Filter controls
    private readonly ComboBox _statusFilter = FilterBox();
    private readonly DateTimePicker _auditFrom = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Theme.Font(9.0f) };
    private readonly DateTimePicker _auditTo = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Theme.Font(9.0f) };
    private readonly ComboBox _actionFilter = FilterBox();
    private readonly ComboBox _targetFilter = FilterBox();

    private IReadOnlyList<IDictionary<string, object?>> _allRows = [];

    private ModernButton? _detailsButton, _historyButton, _editButton, _stopButton;

    private static readonly Dictionary<string, (string Code, string Name)[]> StatusOptionsByEntity = new()
    {
        ["NhanVien"] = [("", "Tất cả trạng thái"), ("DANG_LAM", "Đang làm việc"), ("NGUNG_LAM", "Ngừng làm việc")],
        ["GoiChup"] = [("", "Tất cả trạng thái"), ("DANG_AP_DUNG", "Đang áp dụng"), ("NGUNG_AP_DUNG", "Ngừng áp dụng")],
        ["DichVu"] = [("", "Tất cả trạng thái"), ("DANG_CUNG_CAP", "Đang cung cấp"), ("NGUNG_CUNG_CAP", "Ngừng cung cấp")],
        ["PhongChup"] = [("", "Tất cả trạng thái"), ("HOAT_DONG", "Đang hoạt động"), ("NGUNG_SU_DUNG", "Ngừng sử dụng")],
        ["TaiNguyen"] = [("", "Tất cả trạng thái"), ("HOAT_DONG", "Đang hoạt động"), ("NGUNG_SU_DUNG", "Ngừng sử dụng")]
    };

    private static bool SupportsStatusFilter(string entity) => StatusOptionsByEntity.ContainsKey(entity);

    public CrudPage(AppFacade app, string title, string entity, bool readOnly = false)
    {
        _app = app;
        _entity = entity;
        _readOnly = readOnly;
        BackColor = Theme.Background;

        if (SupportsStatusFilter(_entity))
        {
            var opts = StatusOptionsByEntity[_entity];
            _statusFilter.DataSource = opts.Select(x => new FilterOption(0, x.Code, x.Name)).ToList();
            _statusFilter.DisplayMember = "Name";
            _statusFilter.SelectedIndexChanged += (_, _) => ApplyFiltersAndBind();
        }

        if (_entity == "NhatKy")
        {
            _actionFilter.DataSource = new[]
            {
                new FilterOption(0, "", "Tất cả hành động"),
                new FilterOption(0, "TAO_LICH", "Tạo lịch chụp"),
                new FilterOption(0, "SUA_LICH", "Sửa lịch chụp"),
                new FilterOption(0, "CHUYEN_TIEN_DO", "Chuyển tiến độ"),
                new FilterOption(0, "HUY_LICH", "Hủy lịch"),
                new FilterOption(0, "THU_TIEN", "Thu tiền"),
                new FilterOption(0, "HOAN_TIEN", "Hoàn tiền"),
                new FilterOption(0, "CAP_NHAT_DICH_VU", "Dịch vụ phát sinh"),
                new FilterOption(0, "PHAN_CONG", "Phân công tài nguyên"),
                new FilterOption(0, "DANH_MUC", "Thao tác danh mục"),
                new FilterOption(0, "TAI_KHOAN", "Quản trị tài khoản"),
                new FilterOption(0, "SAO_LUU", "Sao lưu / Phục hồi")
            };
            _actionFilter.DisplayMember = "Name";

            _targetFilter.DataSource = new[]
            {
                new FilterOption(0, "", "Tất cả đối tượng"),
                new FilterOption(0, "LichChup", "Lịch chụp"),
                new FilterOption(0, "KhachHang", "Khách hàng"),
                new FilterOption(0, "NhanVien", "Nhân sự"),
                new FilterOption(0, "GoiChup", "Gói chụp"),
                new FilterOption(0, "DichVu", "Dịch vụ"),
                new FilterOption(0, "PhongChup", "Phòng chụp"),
                new FilterOption(0, "TaiNguyen", "Tài nguyên"),
                new FilterOption(0, "TaiKhoan", "Tài khoản"),
                new FilterOption(0, "Database", "Cơ sở dữ liệu")
            };
            _targetFilter.DisplayMember = "Name";

            _auditFrom.ValueChanged += (_, _) => ApplyFiltersAndBind();
            _auditTo.ValueChanged += (_, _) => ApplyFiltersAndBind();
            _actionFilter.SelectedIndexChanged += (_, _) => ApplyFiltersAndBind();
            _targetFilter.SelectedIndexChanged += (_, _) => ApplyFiltersAndBind();
        }

        var toolbar = BuildCommandToolbar();
        var splitWorkbench = BuildMasterDetailDirectory();

        Controls.Add(splitWorkbench);
        Controls.Add(toolbar);
        toolbar.SendToBack();
        splitWorkbench.BringToFront();

        _search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoadAsync();
            }
        };

        ConfigureGridPainting();
        _grid.SelectionChanged += (_, _) => UpdateInspectorAndActionState();
        _grid.CellDoubleClick += (_, _) => ShowDetails();
        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildCommandToolbar()
    {
        if (_entity == "NhatKy")
        {
            return BuildAuditCommandToolbar();
        }

        var hasStatus = SupportsStatusFilter(_entity);
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong
        };

        var colCount = (_readOnly ? 4 : 5) + (hasStatus ? 1 : 0);
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = colCount,
            RowCount = 1,
            BackColor = Theme.Surface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Search
        if (hasStatus)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176)); // Status Filter
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114)); // Find
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114)); // Refresh
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124)); // Export
        if (!_readOnly)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178)); // Add

        _search.Dock = DockStyle.Fill;
        _search.Margin = Padding.Empty;
        _search.PlaceholderText = $"Tìm kiếm nhanh trong {EntityDisplayName(_entity).ToLowerInvariant()}...";

        var find = Theme.OutlineButton("Tìm kiếm", Theme.BorderStrong, IconKind.Search);
        find.Dock = DockStyle.Fill;
        find.Margin = new Padding(8, 0, 0, 0);
        find.Click += async (_, _) => await LoadAsync();

        var refresh = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(8, 0, 0, 0);
        refresh.Click += async (_, _) =>
        {
            _search.Clear();
            if (hasStatus && _statusFilter.Items.Count > 0) _statusFilter.SelectedIndex = 0;
            await LoadAsync();
        };

        var export = Theme.Button("Xuất Excel", Theme.Excel, IconKind.Download);
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(8, 0, 0, 0);
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, _entity);

        var colIdx = 0;
        row.Controls.Add(_search, colIdx++, 0);
        if (hasStatus)
        {
            _statusFilter.Dock = DockStyle.Fill;
            _statusFilter.Margin = new Padding(8, 2, 0, 0);
            row.Controls.Add(_statusFilter, colIdx++, 0);
        }
        row.Controls.Add(find, colIdx++, 0);
        row.Controls.Add(refresh, colIdx++, 0);
        row.Controls.Add(export, colIdx++, 0);

        if (!_readOnly)
        {
            var addText = _entity == "KhachHang" ? "Thêm khách hàng" : $"Thêm {EntityDisplayName(_entity).ToLowerInvariant()}";
            var add = Theme.Button(addText, Theme.Primary, IconKind.Plus);
            add.Dock = DockStyle.Fill;
            add.Margin = new Padding(8, 0, 0, 0);
            add.Click += async (_, _) => await EditAsync(null);
            row.Controls.Add(add, colIdx, 0);
        }

        card.Controls.Add(row);
        return card;
    }

    private Control BuildAuditCommandToolbar()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 128,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong
        };

        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        // Row 0: Search + Actions
        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));

        _search.Dock = DockStyle.Fill;
        _search.Margin = Padding.Empty;
        _search.PlaceholderText = "Tìm theo hành động, tài khoản, mã đối tượng hoặc lý do...";

        var find = Theme.OutlineButton("Tìm kiếm", Theme.BorderStrong, IconKind.Search);
        find.Dock = DockStyle.Fill;
        find.Margin = new Padding(8, 0, 0, 0);
        find.Click += async (_, _) => await LoadAsync();

        var refresh = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(8, 0, 0, 0);
        refresh.Click += async (_, _) =>
        {
            _search.Clear();
            _auditFrom.Checked = false;
            _auditTo.Checked = false;
            if (_actionFilter.Items.Count > 0) _actionFilter.SelectedIndex = 0;
            if (_targetFilter.Items.Count > 0) _targetFilter.SelectedIndex = 0;
            await LoadAsync();
        };

        var export = Theme.Button("Xuất Excel", Theme.Excel, IconKind.Download);
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(8, 0, 0, 0);
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, _entity);

        topRow.Controls.Add(_search, 0, 0);
        topRow.Controls.Add(find, 1, 0);
        topRow.Controls.Add(refresh, 2, 0);
        topRow.Controls.Add(export, 3, 0);

        // Row 1: Filter bar
        var filterRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = Color.Transparent
        };

        filterRow.Controls.Add(CompactFilterSlot("TỪ NGÀY", _auditFrom, 142));
        filterRow.Controls.Add(CompactFilterSlot("ĐẾN NGÀY", _auditTo, 142));
        filterRow.Controls.Add(CompactFilterSlot("HÀNH ĐỘNG HỆ THỐNG", _actionFilter, 196));
        filterRow.Controls.Add(CompactFilterSlot("LOẠI ĐỐI TƯỢNG", _targetFilter, 176));

        var clearBtn = Theme.OutlineButton("Xóa lọc", Theme.BorderStrong, IconKind.Refresh);
        clearBtn.Height = 34;
        clearBtn.Width = 98;
        clearBtn.Margin = new Padding(4, 18, 0, 0);
        clearBtn.Font = Theme.Font(8.4f, FontStyle.Bold);
        clearBtn.Click += (_, _) =>
        {
            _auditFrom.Checked = false;
            _auditTo.Checked = false;
            if (_actionFilter.Items.Count > 0) _actionFilter.SelectedIndex = 0;
            if (_targetFilter.Items.Count > 0) _targetFilter.SelectedIndex = 0;
            ApplyFiltersAndBind();
        };
        filterRow.Controls.Add(clearBtn);

        container.Controls.Add(topRow, 0, 0);
        container.Controls.Add(filterRow, 0, 1);
        card.Controls.Add(container);
        return card;
    }

    private static Panel CompactFilterSlot(string label, Control input, int width = 150)
    {
        var p = new Panel { Width = width, Height = 52, Margin = new Padding(0, 0, 10, 0), BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = label,
            Font = Theme.MonoFont(7.2f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        input.Dock = DockStyle.Bottom;
        input.Height = 32;
        p.Controls.Add(input);
        p.Controls.Add(lbl);
        return p;
    }

    private static ComboBox FilterBox() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.0f) };
    private sealed record FilterOption(int Id, string Code, string Name);

    private Control BuildMasterDetailDirectory()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Left Directory Table Card
        var leftCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(14, 10, 14, 12),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10
        };

        var leftHead = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 6)
        };
        leftHead.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Danh sách {EntityDisplayName(_entity)}",
            Font = Theme.Font(10.0f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        });
        leftHead.Controls.Add(_countBadge);
        _countBadge.BringToFront();

        _grid.Margin = Padding.Empty;
        leftCard.Controls.Add(_grid);
        leftCard.Controls.Add(leftHead);

        // Right Dossier / Audit Diff Inspector Card
        var rightInspector = BuildRightDossierInspector();

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightInspector, 1, 0);
        return split;
    }

    private Control BuildRightDossierInspector()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 0, 0),
            Padding = new Padding(18, 16, 18, 14),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10,
            AccentStripeColor = _entity == "NhatKy" ? Theme.Primitives.Espresso900 : Theme.Primary,
            AccentStripeThickness = 4
        };

        var head = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.Transparent
        };
        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = _entity == "NhatKy" ? "TRUY VẾT DỮ LIỆU TRƯỚC & SAU THAO TÁC" : $"HỒ SƠ CHI TIẾT  •  {EntityDisplayName(_entity).ToUpperInvariant()}",
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        head.Controls.Add(_dossierTitle);
        head.Controls.Add(_dossierSubtitle);
        head.Controls.Add(overline);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 6)
        };

        if (_entity == "NhatKy")
        {
            var diffSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 240,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            diffSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            diffSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            diffSplit.Controls.Add(CreateDiffBox("GIÁ TRỊ CŨ (TRƯỚC KHI SỬA)", _auditOldBox, Theme.Warning), 0, 0);
            diffSplit.Controls.Add(CreateDiffBox("GIÁ TRỊ MỚI (SAU KHI SỬA)", _auditNewBox, Theme.Success), 0, 1);

            _dossierFields.Height = 144;
            body.Controls.Add(diffSplit);
            body.Controls.Add(_dossierFields);
        }
        else
        {
            body.Controls.Add(_dossierFields);
        }

        // Guard Note if applicable
        var guard = BuildGuardNote();
        var actionBar = BuildInspectorActionButtons();

        card.Controls.Add(body);
        card.Controls.Add(head);
        if (guard is not null) card.Controls.Add(guard);
        card.Controls.Add(actionBar);
        return card;
    }

    private static Control CreateDiffBox(string label, RichTextBox rtb, Color accent)
    {
        var box = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(10, 6, 10, 8),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 8,
            AccentStripeColor = accent,
            AccentStripeLeft = true,
            AccentStripeThickness = 3
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = label,
            Font = Theme.MonoFont(7.5f, FontStyle.Bold),
            ForeColor = accent
        };
        box.Controls.Add(rtb);
        box.Controls.Add(title);
        return box;
    }

    private Control BuildInspectorActionButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };

        if (!_readOnly)
        {
            _editButton = Theme.Button("Chỉnh sửa", Theme.Primary, IconKind.Edit);
            _editButton.Click += async (_, _) => await EditAsync(SelectedId());
            panel.Controls.Add(_editButton);
        }

        if (_entity is "KhachHang" or "NhanVien" or "PhongChup")
        {
            _historyButton = Theme.OutlineButton("Lịch sử chụp", Theme.BorderStrong, IconKind.History);
            _historyButton.Click += async (_, _) => await ShowHistoryAsync();
            panel.Controls.Add(_historyButton);
        }

        _detailsButton = Theme.OutlineButton(_entity == "NhatKy" ? "Phóng to Diff" : "Bản in hồ sơ", Theme.BorderStrong, IconKind.Eye);
        _detailsButton.Click += (_, _) => ShowDetails();
        panel.Controls.Add(_detailsButton);

        if (!_readOnly)
        {
            _stopButton = Theme.Button(_entity == "KhachHang" ? "Xóa hồ sơ" : "Ngừng dùng", Theme.Danger, IconKind.Trash);
            _stopButton.Click += async (_, _) => await DeactivateAsync();
            panel.Controls.Add(_stopButton);
        }

        return panel;
    }

    private Control? BuildGuardNote()
    {
        if (_entity != "KhachHang") return null;

        var panel = new CardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 0, 4),
            CornerRadius = 8,
            BackColor = Theme.PrimaryLight,
            BorderColor = Theme.PrimaryLightBorder,
            AccentStripeColor = Theme.Primary,
            AccentStripeLeft = true,
            AccentStripeThickness = 3
        };

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Quy tắc bảo toàn: Khách hàng đã phát sinh lịch chụp được bảo vệ khỏi thao tác xóa.",
            ForeColor = Theme.Primitives.Terracotta800,
            Font = Theme.Font(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        });

        return panel;
    }

    private static string EntityDisplayName(string e) => e switch
    {
        "KhachHang" => "Khách hàng",
        "NhanVien" => "Nhân sự",
        "GoiChup" => "Gói chụp",
        "DichVu" => "Dịch vụ bổ sung",
        "PhongChup" => "Phòng chụp",
        "TaiNguyen" => "Tài nguyên",
        "NhatKy" => "Nhật ký kiểm toán",
        _ => e
    };

    private long? SelectedId() => _grid.CurrentRow?.Cells["Id"].Value is object v ? Convert.ToInt64(v) : null;

    private void UpdateInspectorAndActionState()
    {
        var row = _grid.CurrentRow;
        var enabled = row is not null && SelectedId() is not null;
        foreach (var button in new[] { _detailsButton, _historyButton, _editButton, _stopButton })
            if (button is not null) button.Enabled = enabled;

        if (!enabled || row is null)
        {
            _dossierTitle.Text = "Chưa chọn bản ghi";
            _dossierSubtitle.Text = "Chọn một dòng ở bảng bên trái để xem chi tiết";
            _dossierFields.SetRows([("Trạng thái", "Chưa có mục nào được chọn", Theme.Muted, false)]);
            _auditOldBox.Text = "";
            _auditNewBox.Text = "";
            return;
        }

        var visibleCols = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();

        var primaryText = visibleCols.Count > 1
            ? $"{row.Cells[visibleCols[0].Index].FormattedValue}  •  {row.Cells[visibleCols[1].Index].FormattedValue}"
            : visibleCols.FirstOrDefault() is { } c0 ? row.Cells[c0.Index].FormattedValue?.ToString() ?? "" : "";

        _dossierTitle.Text = primaryText;
        _dossierSubtitle.Text = $"Mã định danh hệ thống: #{SelectedId()}";

        var fieldRows = new List<(string Label, string Value, Color? ValueColor, bool Highlight)>();
        foreach (var col in visibleCols)
        {
            var val = row.Cells[col.Index].FormattedValue?.ToString() ?? "—";
            Color? valColor = null;
            var highlight = false;
            if (col.HeaderText.Contains("Trạng thái", StringComparison.OrdinalIgnoreCase))
            {
                valColor = Theme.GetStatusColors(val).Text;
                highlight = true;
            }
            else if (col.HeaderText.Contains("Giá", StringComparison.OrdinalIgnoreCase))
            {
                valColor = Theme.Primary;
                highlight = true;
            }
            fieldRows.Add((col.HeaderText, val, valColor, highlight));
        }

        _dossierFields.SetRows(fieldRows);

        if (_entity == "NhatKy")
        {
            _auditOldBox.Text = _grid.Columns.Contains("Giá trị cũ")
                ? (row.Cells["Giá trị cũ"].FormattedValue?.ToString() ?? "(Trống)")
                : "(Trống)";
            _auditNewBox.Text = _grid.Columns.Contains("Giá trị mới")
                ? (row.Cells["Giá trị mới"].FormattedValue?.ToString() ?? "(Trống)")
                : "(Trống)";
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _allRows = await _app.Administration.QueryGridAsync(_entity, string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(), _app.Session!);
            ApplyFiltersAndBind();
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }

    private void ApplyFiltersAndBind()
    {
        IEnumerable<IDictionary<string, object?>> filtered = _allRows;

        // 1. Catalog status filter
        if (SupportsStatusFilter(_entity) && _statusFilter.SelectedItem is FilterOption sOpt && !string.IsNullOrEmpty(sOpt.Code))
        {
            filtered = filtered.Where(r =>
            {
                if (!r.TryGetValue("Trạng thái", out var v) || v is null) return false;
                var str = v.ToString()!;
                return string.Equals(str, sOpt.Code, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(str, sOpt.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        // 2. Audit Log filters (when _entity == "NhatKy")
        if (_entity == "NhatKy")
        {
            if (_auditFrom.Checked)
            {
                var fromDate = _auditFrom.Value.Date;
                filtered = filtered.Where(r =>
                {
                    if (!r.TryGetValue("Thời điểm", out var v) || v is null) return false;
                    return (v is DateTime dt ? dt : DateTime.TryParse(v.ToString(), out var p) ? p : DateTime.MinValue) >= fromDate;
                });
            }

            if (_auditTo.Checked)
            {
                var toDate = _auditTo.Value.Date.AddDays(1);
                filtered = filtered.Where(r =>
                {
                    if (!r.TryGetValue("Thời điểm", out var v) || v is null) return false;
                    return (v is DateTime dt ? dt : DateTime.TryParse(v.ToString(), out var p) ? p : DateTime.MaxValue) < toDate;
                });
            }

            if (_actionFilter.SelectedItem is FilterOption aOpt && !string.IsNullOrEmpty(aOpt.Code))
            {
                filtered = filtered.Where(r =>
                {
                    if (!r.TryGetValue("Hành động", out var v) || v is null) return false;
                    return v.ToString()!.Contains(aOpt.Code, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (_targetFilter.SelectedItem is FilterOption tOpt && !string.IsNullOrEmpty(tOpt.Code))
            {
                filtered = filtered.Where(r =>
                {
                    if (!r.TryGetValue("Đối tượng", out var v) || v is null) return false;
                    return string.Equals(v.ToString(), tOpt.Code, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        var list = filtered.ToList();
        _countBadge.Text = list.Count == _allRows.Count
            ? $"{list.Count} bản ghi"
            : $"{list.Count} / {_allRows.Count} bản ghi";

        BuildColumns(list);

        if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
            var firstCol = _grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
            if (firstCol is not null) _grid.CurrentCell = _grid.Rows[0].Cells[firstCol.Index];
        }
        UpdateInspectorAndActionState();
    }

    private void BuildColumns(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        _grid.DataSource = null;
        if (rows.Count == 0)
        {
            if (_allRows.Count > 0)
            {
                var emptyDt = new System.Data.DataTable();
                foreach (var key in _allRows[0].Keys) emptyDt.Columns.Add(key, typeof(object));
                _grid.DataSource = emptyDt;
                if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
                if (_entity == "TaiNguyen" && _grid.Columns.Contains("Dịch vụ thuê ID")) _grid.Columns["Dịch vụ thuê ID"].Visible = false;
                if (_entity == "NhatKy")
                {
                    if (_grid.Columns.Contains("Giá trị cũ")) _grid.Columns["Giá trị cũ"].Visible = false;
                    if (_grid.Columns.Contains("Giá trị mới")) _grid.Columns["Giá trị mới"].Visible = false;
                }
            }
            return;
        }

        var dt = new System.Data.DataTable();
        foreach (var key in rows[0].Keys) dt.Columns.Add(key, typeof(object));
        foreach (var r in rows)
        {
            var row = dt.NewRow();
            foreach (var p in r) row[p.Key] = p.Value ?? DBNull.Value;
            dt.Rows.Add(row);
        }
        _grid.DataSource = dt;
        if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
        if (_entity == "TaiNguyen" && _grid.Columns.Contains("Dịch vụ thuê ID")) _grid.Columns["Dịch vụ thuê ID"].Visible = false;
        if (_entity == "NhatKy")
        {
            if (_grid.Columns.Contains("Giá trị cũ")) _grid.Columns["Giá trị cũ"].Visible = false;
            if (_grid.Columns.Contains("Giá trị mới")) _grid.Columns["Giá trị mới"].Visible = false;
        }

        foreach (var col in new[] { "Giá", "Đơn giá", "Tổng tiền", "Thực thu" })
        {
            if (_grid.Columns.Contains(col))
            {
                _grid.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                _grid.Columns[col].DefaultCellStyle.Format = "N0";
            }
        }
    }

    private void ConfigureGridPainting()
    {
        _grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName != "Trạng thái") return;

            e.PaintBackground(e.ClipBounds, true);
            var status = e.FormattedValue?.ToString() ?? "";
            if (!string.IsNullOrEmpty(status) && e.Graphics is { } g)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var (fg, bg, border) = Theme.GetStatusColors(status);
                using var font = Theme.Font(7.9f, FontStyle.Bold);
                var sz = TextRenderer.MeasureText(g, status, font);
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

                TextRenderer.DrawText(g, status, font, Rectangle.Round(rect), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            e.Handled = true;
        };
    }

    private async Task EditAsync(long? id)
    {
        if (_readOnly) return;
        IReadOnlyList<LookupItem>? rentalServices = null;
        if (_entity == "TaiNguyen") rentalServices = await _app.Administration.GetLookupsAsync("DichVu", id is null);

        using var dialog = new SimpleEditDialog(_entity, id, id is null ? null : _grid.CurrentRow, rentalServices);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var result = await _app.Administration.SaveAsync(_entity, id, dialog.Values, _app.Session!);
        if (!result.Success) Ui.Error(this, result.Message);
        else
        {
            Ui.Info(this, result.Message);
            await LoadAsync();
        }
    }

    private async Task DeactivateAsync()
    {
        var id = SelectedId();
        if (id is null) return;
        var message = _entity == "KhachHang"
            ? "Xóa khách hàng đã chọn? Lưu ý chỉ khách hàng chưa phát sinh lịch mới được phép xóa."
            : "Ngừng sử dụng mục đã chọn? Dữ liệu lịch sử giao dịch vẫn được lưu trữ nguyên vẹn.";

        if (MessageBox.Show(message, "Xác nhận thao tác", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var r = await _app.Administration.DeactivateAsync(_entity, id.Value, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else await LoadAsync();
    }

    private async Task ShowHistoryAsync()
    {
        var id = SelectedId();
        if (id is null)
        {
            Ui.Error(this, "Vui lòng chọn một dòng dữ liệu.");
            return;
        }

        try
        {
            var rows = await _app.Administration.GetBookingHistoryAsync(_entity, id.Value, _app.Session!);
            using var dialog = new BookingHistoryDialog(_entity, rows);
            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }

    private void ShowDetails()
    {
        if (_grid.CurrentRow is null)
        {
            Ui.Error(this, "Vui lòng chọn một dòng dữ liệu.");
            return;
        }

        if (_entity == "NhatKy")
        {
            using var audit = new AuditDetailDialog(_grid.CurrentRow);
            audit.ShowDialog(this);
            return;
        }

        var lines = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(x => x.Visible)
            .OrderBy(x => x.DisplayIndex)
            .Select(x => $"{x.HeaderText}: {_grid.CurrentRow.Cells[x.Index].FormattedValue}");

        MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, lines),
            $"Hồ sơ {EntityDisplayName(_entity)}", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed class RowView(IDictionary<string, object?> data)
    {
        [Browsable(false)] public IDictionary<string, object?> Data { get; } = data;
    }
}

internal sealed class SimpleEditDialog : Form
{
    private readonly Dictionary<string, Control> _inputs = new();
    public IReadOnlyDictionary<string, object?> Values => _inputs.ToDictionary(x => x.Key, x => Read(x.Value));

    public SimpleEditDialog(string entity, long? id, DataGridViewRow? row, IReadOnlyList<LookupItem>? rentalServices = null)
    {
        Text = (id is null ? "Thêm mới " : "Cập nhật ") + EntityDisplayName(entity) + " • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 520);
        MinimumSize = new Size(660, 480);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
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
            Text = (id is null ? "Khởi tạo hồ sơ " : "Hiệu chỉnh hồ sơ ") + EntityDisplayName(entity),
            Font = Theme.Font(12.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var fieldDefs = Fields(entity).ToList();
        var rowCount = Math.Max(1, (fieldDefs.Count + 1) / 2);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rowCount,
            AutoScroll = true,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        for (var r = 0; r < rowCount; r++)
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));

        for (var i = 0; i < fieldDefs.Count; i++)
        {
            var f = fieldDefs[i];
            var input = CreateInput(f, rentalServices);
            _inputs[f.Name] = input;
            var isLastOdd = (i == fieldDefs.Count - 1) && (fieldDefs.Count % 2 == 1);
            var isLeft = (i % 2 == 0);
            var fieldPanel = Ui.Field(f.Label, input, 300);
            fieldPanel.Dock = DockStyle.Fill;
            fieldPanel.Margin = isLastOdd
                ? new Padding(0, 0, 0, 8)
                : (isLeft ? new Padding(0, 0, 8, 8) : new Padding(8, 0, 0, 8));

            body.Controls.Add(fieldPanel, isLeft ? 0 : 1, i / 2);
            if (isLastOdd)
                body.SetColumnSpan(fieldPanel, 2);
        }

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = Color.Transparent
        };

        var save = Theme.Button("Lưu thông tin", Theme.Primary, IconKind.Check);
        save.Click += (_, _) =>
        {
            if (ValidateFields())
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

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

        if (row != null) Fill(row);
        AcceptButton = save;
    }

    private static string EntityDisplayName(string e) => e switch
    {
        "KhachHang" => "Khách hàng",
        "NhanVien" => "Nhân sự",
        "GoiChup" => "Gói chụp",
        "DichVu" => "Dịch vụ bổ sung",
        "PhongChup" => "Phòng chụp",
        "TaiNguyen" => "Tài nguyên",
        _ => e
    };

    private static IEnumerable<FieldDef> Fields(string e) => e switch
    {
        "KhachHang" => [new("HoTen", "HỌ VÀ TÊN *"), new("SoDienThoai", "SỐ ĐIỆN THOẠI *"), new("Email", "ĐỊA CHỈ EMAIL"), new("DiaChi", "ĐỊA CHỈ LIÊN HỆ"), new("GhiChu", "GHI CHÚ KHÁCH HÀNG")],
        "NhanVien" => [new("HoTen", "HỌ VÀ TÊN *"), new("SoDienThoai", "SỐ ĐIỆN THOẠI *"), new("ChucVu", "CHỨC VỤ NGHIỆP VỤ", ["TIEP_NHAN", "NHIEP_ANH_GIA", "KHAC"]), new("TrangThai", "TRẠNG THÁI CÔNG VIỆC", ["DANG_LAM", "NGUNG_LAM"]), new("GhiChu", "GHI CHÚ NHÂN SỰ")],
        "GoiChup" => [new("TenGoiChup", "TÊN GÓI CHỤP *"), new("GiaGoi", "GIÁ GÓI (VNĐ) *", null, true), new("ThoiLuongPhut", "THỜI LƯỢNG (PHÚT) *", null, true), new("MoTa", "MÔ TẢ GÓI"), new("TrangThai", "TRẠNG THÁI", ["DANG_AP_DUNG", "NGUNG_AP_DUNG"])],
        "DichVu" => [new("TenDichVu", "TÊN DỊCH VỤ *"), new("DonViTinh", "ĐƠN VỊ TÍNH *"), new("DonGia", "ĐƠN GIÁ (VNĐ) *", null, true), new("MoTa", "MÔ TẢ DỊCH VỤ"), new("TrangThai", "TRẠNG THÁI", ["DANG_CUNG_CAP", "NGUNG_CUNG_CAP"])],
        "PhongChup" => [new("TenPhong", "TÊN PHÒNG CHỤP *"), new("MoTa", "MÔ TẢ BỐI CẢNH"), new("TrangThai", "TRẠNG THÁI", ["HOAT_DONG", "NGUNG_SU_DUNG"])],
        "TaiNguyen" => [new("TenTaiNguyen", "TÊN TÀI NGUYÊN *"), new("LoaiTaiNguyen", "PHÂN LOẠI", ["THIET_BI", "TRANG_PHUC"]), new("TongSoLuong", "TỔNG SỐ LƯỢNG *", null, true), new("DichVuThueId", "DỊCH VỤ THUÊ LIÊN KẾT"), new("GhiChu", "GHI CHÚ"), new("TrangThai", "TRẠNG THÁI", ["HOAT_DONG", "NGUNG_SU_DUNG"])],
        _ => []
    };

    private static Control CreateInput(FieldDef f, IReadOnlyList<LookupItem>? rentalServices)
    {
        if (f.Name == "DichVuThueId")
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id", Font = Theme.Font(9.5f) };
            var items = new List<LookupItem> { new(0, "", "Không liên kết") };
            if (rentalServices is not null) items.AddRange(rentalServices);
            c.DataSource = items;
            return c;
        }

        if (f.Options is not null)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
            c.Items.AddRange(f.Options);
            if (c.Items.Count > 0) c.SelectedIndex = 0;
            return c;
        }

        if (f.Number)
            return new NumericUpDown { Maximum = 1_000_000_000, ThousandsSeparator = true, Font = Theme.Font(9.5f) };

        return new TextBox { Font = Theme.Font(9.5f) };
    }

    private void Fill(DataGridViewRow row)
    {
        var grid = row.DataGridView;
        if (grid is null) return;
        foreach (var p in _inputs)
        {
            var col = grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c =>
                Normalize(c.HeaderText).Contains(Normalize(LabelFor(p.Key))) ||
                Normalize(c.Name).Contains(Normalize(LabelFor(p.Key))));
            if (col is null) continue;
            var value = row.Cells[col.Index].Value;
            if (p.Key == "DichVuThueId" && p.Value is ComboBox rental && long.TryParse(value?.ToString(), out var rentalId))
            {
                rental.SelectedValue = rentalId;
                continue;
            }
            if (p.Value is ComboBox cb) cb.SelectedItem = value?.ToString();
            else if (p.Value is NumericUpDown n && decimal.TryParse(value?.ToString(), out var d)) n.Value = Math.Min(n.Maximum, d);
            else p.Value.Text = value?.ToString() ?? "";
        }
    }

    private bool ValidateFields()
    {
        foreach (var p in _inputs.Where(x => x.Key is "HoTen" or "SoDienThoai" or "TenGoiChup" or "TenDichVu" or "DonViTinh" or "TenPhong" or "TenTaiNguyen"))
        {
            if (string.IsNullOrWhiteSpace(p.Value.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ các trường thông tin bắt buộc (*).", "StudioManager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                p.Value.Focus();
                return false;
            }
        }
        return true;
    }

    private static object? Read(Control c) => c switch
    {
        NumericUpDown n => n.Value,
        ComboBox { SelectedItem: LookupItem item } => item.Id == 0 ? null : item.Id,
        ComboBox cb => cb.SelectedItem?.ToString(),
        _ => string.IsNullOrWhiteSpace(c.Text) ? null : c.Text.Trim()
    };

    private static string Normalize(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string LabelFor(string k) => k switch
    {
        "HoTen" => "Họ tên",
        "SoDienThoai" => "Điện thoại",
        "TenGoiChup" => "Tên gói",
        "GiaGoi" => "Giá",
        "ThoiLuongPhut" => "Thời lượng",
        "TenDichVu" => "Tên dịch vụ",
        "DonViTinh" => "Đơn vị",
        "DonGia" => "Đơn giá",
        "TenPhong" => "Tên phòng",
        "TenTaiNguyen" => "Tên tài nguyên",
        "LoaiTaiNguyen" => "Loại",
        "TongSoLuong" => "Số lượng",
        "DichVuThueId" => "Dịch vụ thuê ID",
        "GhiChu" => "Ghi chú",
        "MoTa" => "Mô tả",
        "TrangThai" => "Trạng thái",
        _ => k
    };

    private sealed record FieldDef(string Name, string Label, string[]? Options = null, bool Number = false);
}

internal sealed class BookingHistoryDialog : Form
{
    public BookingHistoryDialog(string entity, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        Text = $"Lịch sử chụp liên quan • {entity} • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 560);
        MinimumSize = new Size(800, 440);
        BackColor = Theme.Background;
        Font = Theme.Font();

        var grid = Theme.Grid();
        var table = new System.Data.DataTable();
        if (rows.Count > 0)
        {
            foreach (var key in rows[0].Keys) table.Columns.Add(key, typeof(object));
            foreach (var source in rows)
            {
                var row = table.NewRow();
                foreach (var value in source) row[value.Key] = value.Value ?? DBNull.Value;
                table.Rows.Add(row);
            }
        }
        grid.DataSource = table;
        if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;

        var banner = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(16, 16, 16, 8),
            CornerRadius = 8,
            AccentStripeColor = Theme.Primary,
            AccentStripeLeft = true,
            AccentStripeThickness = 4
        };
        banner.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = rows.Count == 0 ? "Chưa có lịch chụp nào phát sinh với hồ sơ này." : $"Tìm thấy {rows.Count} lịch chụp liên quan trong hồ sơ.",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Text,
            Font = Theme.Font(9.2f, FontStyle.Bold)
        });

        var host = Ui.DataHost(grid);
        var layout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 16) };
        layout.Controls.Add(host);

        Controls.Add(layout);
        Controls.Add(banner);
    }
}

internal sealed class AuditDetailDialog : Form
{
    public AuditDetailDialog(DataGridViewRow row)
    {
        Text = "So sánh dữ liệu kiểm toán chi tiết • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(960, 640);
        MinimumSize = new Size(820, 520);
        BackColor = Theme.Background;
        Font = Theme.Font();

        string Read(string name) => row.DataGridView?.Columns.Contains(name) == true
            ? row.Cells[name].FormattedValue?.ToString() ?? ""
            : string.Empty;

        var headCard = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 180,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(16, 16, 16, 8),
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4,
            AutoScroll = true
        };

        var dossier = new DossierFieldList { Dock = DockStyle.Fill };
        dossier.SetRows([
            ("Mã nhật ký & Thời điểm", $"#{Read("Id")}  •  {Read("Thời điểm")}", Theme.Primary, true),
            ("Tài khoản thực hiện", Read("Tài khoản"), null, false),
            ("Hành động & Đối tượng", $"{Read("Hành động")}  →  {Read("Đối tượng")} ({Read("Mã đối tượng")})", null, false),
            ("Lý do ghi nhận", Read("Lý do"), null, false)
        ]);
        headCard.Controls.Add(dossier);

        var valuesCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Margin = new Padding(16, 8, 16, 16),
            CornerRadius = 10
        };

        var values = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        values.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        values.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        values.Controls.Add(ValueBox("GIÁ TRỊ CŨ (TRƯỚC THAO TÁC)", Read("Giá trị cũ"), Theme.Warning), 0, 0);
        values.Controls.Add(ValueBox("GIÁ TRỊ MỚI (SAU THAO TÁC)", Read("Giá trị mới"), Theme.Success), 1, 0);

        valuesCard.Controls.Add(values);

        Controls.Add(valuesCard);
        Controls.Add(headCard);
    }

    private static Control ValueBox(string title, string value, Color accent)
    {
        var box = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(12),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 8,
            AccentStripeColor = accent,
            AccentStripeThickness = 3
        };

        var rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Primitives.Alabaster50,
            ForeColor = Theme.Text,
            Font = Theme.MonoFont(9.4f),
            Text = string.IsNullOrWhiteSpace(value) ? "(Trống)" : value,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = title,
            ForeColor = accent,
            Font = Theme.MonoFont(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        box.Controls.Add(rtb);
        box.Controls.Add(lbl);
        return box;
    }
}
