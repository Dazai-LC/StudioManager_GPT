using System.ComponentModel;
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
    private ModernButton? _detailsButton, _historyButton, _editButton, _stopButton;

    public CrudPage(AppFacade app, string title, string entity, bool readOnly = false)
    {
        _app = app;
        _entity = entity;
        _readOnly = readOnly;
        BackColor = Theme.Background;

        var header = new PageHeader(title, SubtitleFor(entity));
        if (!readOnly)
        {
            var addText = entity == "KhachHang" ? "+ Thêm khách hàng" : "+ Thêm mới";
            var add = Theme.Button(addText);
            add.Width = 160;
            add.Click += async (_, _) => await EditAsync(null);
            header.Actions.Controls.Add(add);
        }

        var toolbar = BuildSearchBar();
        var card = Ui.DataHost(_grid);
        var actions = BuildSelectionActions();
        var guard = BuildGuardNote();

        Controls.Add(card);
        Controls.Add(actions);
        if (guard is not null) Controls.Add(guard);
        Controls.Add(toolbar);
        Controls.Add(header);

        _search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await LoadAsync(); };
        _grid.CellFormatting += GridCellFormatting;
        _grid.SelectionChanged += (_, _) => UpdateActionState();
        _grid.CellDoubleClick += (_, _) => ShowDetails();
        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildSearchBar()
    {
        var card = Ui.SearchToolbarCard();
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Surface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

        _search.Dock = DockStyle.Fill;
        _search.Margin = Padding.Empty;
        _search.PlaceholderText = _entity == "NhatKy"
            ? "Tìm theo hành động, tài khoản hoặc đối tượng..."
            : "Tìm theo mã, tên hoặc thông tin liên quan...";

        var find = Theme.Button("Tìm kiếm");
        find.Dock = DockStyle.Fill;
        find.Margin = new Padding(8, 0, 0, 0);
        find.Click += async (_, _) => await LoadAsync();

        var refresh = Theme.Button("Làm mới", Theme.Secondary);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(8, 0, 0, 0);
        refresh.Click += async (_, _) => { _search.Clear(); await LoadAsync(); };

        var export = Theme.Button("Xuất Excel", Theme.Excel);
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(8, 0, 0, 0);
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, _entity);

        row.Controls.Add(_search, 0, 0);
        row.Controls.Add(find, 1, 0);
        row.Controls.Add(refresh, 2, 0);
        row.Controls.Add(export, 3, 0);

        card.Controls.Add(row);
        return card;
    }

    private Control BuildSelectionActions()
    {
        var group = Ui.ActionButtonGroup();

        if (!_readOnly)
        {
            _stopButton = Theme.Button(_entity == "KhachHang" ? "Xóa" : "Ngừng dùng", Theme.Danger);
            _stopButton.Width = 114;
            _stopButton.Click += async (_, _) => await DeactivateAsync();
            group.Controls.Add(_stopButton);

            _editButton = Theme.Button("Chỉnh sửa", Theme.Primary);
            _editButton.Width = 110;
            _editButton.Click += async (_, _) => await EditAsync(SelectedId());
            group.Controls.Add(_editButton);
        }

        if (_entity is "KhachHang" or "NhanVien" or "PhongChup")
        {
            _historyButton = Theme.Button("Lịch sử", Theme.Secondary);
            _historyButton.Width = 100;
            _historyButton.Click += async (_, _) => await ShowHistoryAsync();
            group.Controls.Add(_historyButton);
        }

        _detailsButton = Theme.Button("Xem chi tiết", Theme.Secondary);
        _detailsButton.Width = 120;
        _detailsButton.Click += (_, _) => ShowDetails();
        group.Controls.Add(_detailsButton);

        return Ui.SelectionActionBar("Chọn một bản ghi để xem chi tiết hoặc thực hiện thao tác quản lý.", group);
    }

    private Control? BuildGuardNote()
    {
        if (_entity != "KhachHang") return null;

        var panel = new CardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 0, 0, 8),
            CornerRadius = 8,
            BackColor = Color.FromArgb(245, 243, 255),
            BorderColor = Color.FromArgb(221, 214, 254)
        };

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "ℹ   Khách hàng đã phát sinh lịch chụp không thể xóa khỏi hệ thống. Chọn “Xem chi tiết” hoặc “Lịch sử” để kiểm tra.",
            ForeColor = Theme.Purple,
            Font = Theme.Font(8.8f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        });

        return panel;
    }

    private static string SubtitleFor(string entity) => entity switch
    {
        "KhachHang" => "Hồ sơ thông tin liên hệ, khách hàng thân thiết và lịch sử đặt lịch.",
        "NhanVien" => "Quản lý danh sách nhân sự, chức vụ nghiệp vụ và trạng thái làm việc.",
        "GoiChup" => "Bảng giá danh mục các gói chụp ảnh cung cấp cho khách hàng.",
        "DichVu" => "Dịch vụ phát sinh bổ sung, cho thuê và đơn giá áp dụng.",
        "PhongChup" => "Danh mục các phòng chụp, bối cảnh và tình trạng sẵn sàng.",
        "TaiNguyen" => "Quản lý thiết bị máy ảnh, ống kính, đèn và trang phục đạo cụ.",
        "NhatKy" => "Theo dõi lịch sử thao tác, sửa đổi và truy cập hệ thống bảo mật.",
        _ => "Quản lý, tìm kiếm và theo dõi dữ liệu nghiệp vụ tập trung."
    };

    private long? SelectedId() => _grid.CurrentRow?.Cells["Id"].Value is object v ? Convert.ToInt64(v) : null;

    private void UpdateActionState()
    {
        var enabled = SelectedId() is not null;
        foreach (var button in new[] { _detailsButton, _historyButton, _editButton, _stopButton })
            if (button is not null) button.Enabled = enabled;
    }

    private async Task LoadAsync()
    {
        try
        {
            var rows = await _app.Administration.QueryGridAsync(_entity, string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(), _app.Session!);
            var table = new BindingList<RowView>(rows.Select(x => new RowView(x)).ToList());
            _grid.DataSource = table;
            if (_grid.Columns.Contains("Data")) _grid.Columns["Data"].Visible = false;
            BuildColumns(rows);
            UpdateActionState();
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }

    private void BuildColumns(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return;
        _grid.DataSource = null;
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

        // Format currency columns
        foreach (var col in new[] { "Giá", "Đơn giá", "Tổng tiền", "Thực thu" })
        {
            if (_grid.Columns.Contains(col))
            {
                _grid.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                _grid.Columns[col].DefaultCellStyle.Format = "N0";
            }
        }
    }

    private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is null) return;
        var colName = _grid.Columns[e.ColumnIndex].Name;

        if (colName == "Trạng thái" && e.Value is string status)
        {
            var colors = Theme.GetStatusColors(status);
            e.CellStyle.ForeColor = colors.Text;
            e.CellStyle.SelectionForeColor = colors.Text;
            e.CellStyle.Font = Theme.Font(8.8f, FontStyle.Bold);
        }
    }

    private async Task EditAsync(long? id)
    {
        if (_readOnly) return;
        IReadOnlyList<LookupItem>? rentalServices = null;
        if (_entity == "TaiNguyen") rentalServices = await _app.Administration.GetLookupsAsync("DichVu", id is null);

        using var dialog = new SimpleEditDialog(_entity, id, _grid.CurrentRow, rentalServices);
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

        MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, lines), "Chi tiết dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed class RowView(IDictionary<string, object?> data)
    {
        [Browsable(false)] public IDictionary<string, object?> Data { get; } = data;
    }
}

internal sealed class SimpleEditDialog : Form
{
    private readonly string _entity;
    private readonly Dictionary<string, Control> _inputs = new();
    public IReadOnlyDictionary<string, object?> Values => _inputs.ToDictionary(x => x.Key, x => Read(x.Value));

    public SimpleEditDialog(string entity, long? id, DataGridViewRow? row, IReadOnlyList<LookupItem>? rentalServices = null)
    {
        _entity = entity;
        Text = (id is null ? "Thêm mới " : "Cập nhật ") + EntityDisplayName(entity) + " • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 530);
        BackColor = Theme.Background;
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(26),
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };

        foreach (var f in Fields(entity))
        {
            var input = CreateInput(f, rentalServices);
            _inputs[f.Name] = input;
            body.Controls.Add(Ui.Field(f.Label, input, 274));
        }

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = Theme.Surface
        };

        var save = Theme.Button("Lưu dữ liệu");
        save.Click += (_, _) =>
        {
            if (ValidateFields())
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        var cancel = Theme.Button("Hủy bỏ", Theme.Secondary);
        cancel.Click += (_, _) => Close();

        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        Controls.Add(body);
        Controls.Add(footer);

        if (row != null) Fill(row);
        AcceptButton = save;
    }

    private static string EntityDisplayName(string e) => e switch
    {
        "KhachHang" => "Khách hàng",
        "NhanVien" => "Nhân viên",
        "GoiChup" => "Gói chụp",
        "DichVu" => "Dịch vụ bổ sung",
        "PhongChup" => "Phòng chụp",
        "TaiNguyen" => "Tài nguyên",
        _ => e
    };

    private static IEnumerable<FieldDef> Fields(string e) => e switch
    {
        "KhachHang" => [new("HoTen", "Họ và tên *"), new("SoDienThoai", "Số điện thoại *"), new("Email", "Địa chỉ Email"), new("DiaChi", "Địa chỉ liên hệ"), new("GhiChu", "Ghi chú khách hàng")],
        "NhanVien" => [new("HoTen", "Họ và tên *"), new("SoDienThoai", "Số điện thoại *"), new("ChucVu", "Chức vụ nghiệp vụ", ["TIEP_NHAN", "NHIEP_ANH_GIA", "KHAC"]), new("TrangThai", "Trạng thái công việc", ["DANG_LAM", "NGUNG_LAM"]), new("GhiChu", "Ghi chú nhân viên")],
        "GoiChup" => [new("TenGoiChup", "Tên gói chụp *"), new("GiaGoi", "Giá gói (VNĐ) *", null, true), new("ThoiLuongPhut", "Thời lượng (Phút) *", null, true), new("MoTa", "Mô tả gói"), new("TrangThai", "Trạng thái", ["DANG_AP_DUNG", "NGUNG_AP_DUNG"])],
        "DichVu" => [new("TenDichVu", "Tên dịch vụ *"), new("DonViTinh", "Đơn vị tính *"), new("DonGia", "Đơn giá (VNĐ) *", null, true), new("MoTa", "Mô tả dịch vụ"), new("TrangThai", "Trạng thái", ["DANG_CUNG_CAP", "NGUNG_CUNG_CAP"])],
        "PhongChup" => [new("TenPhong", "Tên phòng chụp *"), new("MoTa", "Mô tả bối cảnh"), new("TrangThai", "Trạng thái", ["HOAT_DONG", "NGUNG_SU_DUNG"])],
        "TaiNguyen" => [new("TenTaiNguyen", "Tên tài nguyên *"), new("LoaiTaiNguyen", "Loại", ["THIET_BI", "TRANG_PHUC"]), new("TongSoLuong", "Tổng số lượng *", null, true), new("DichVuThueId", "Dịch vụ thuê liên kết"), new("GhiChu", "Ghi chú"), new("TrangThai", "Trạng thái", ["HOAT_DONG", "NGUNG_SU_DUNG"])],
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
            var col = grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => Normalize(c.HeaderText).Contains(Normalize(LabelFor(p.Key))) || Normalize(c.Name).Contains(Normalize(LabelFor(p.Key))));
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

        var empty = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(16, 16, 16, 8),
            CornerRadius = 8
        };
        empty.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = rows.Count == 0 ? "Chưa có lịch chụp liên quan." : $"Tìm thấy {rows.Count} lịch chụp liên quan.",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(9.2f, FontStyle.Bold)
        });

        var host = Ui.DataHost(grid);
        var layout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 16) };
        layout.Controls.Add(host);

        Controls.Add(layout);
        Controls.Add(empty);
    }
}

internal sealed class AuditDetailDialog : Form
{
    public AuditDetailDialog(DataGridViewRow row)
    {
        Text = "Chi tiết nhật ký kiểm toán hệ thống • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(940, 640);
        MinimumSize = new Size(800, 520);
        BackColor = Theme.Background;
        Font = Theme.Font();

        string Read(string name) => row.DataGridView?.Columns.Contains(name) == true
            ? row.Cells[name].FormattedValue?.ToString() ?? ""
            : string.Empty;

        var headCard = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 180,
            Padding = new Padding(16),
            Margin = new Padding(16, 16, 16, 8),
            CornerRadius = 10
        };

        var head = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var i = 0; i < 3; i++) head.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3));

        Add(head, 0, 0, "THỜI ĐIỂM", Read("Thời điểm"));
        Add(head, 1, 0, "TÀI KHOẢN", Read("Tài khoản"));
        Add(head, 0, 1, "HÀNH ĐỘNG", Read("Hành động"));
        Add(head, 1, 1, "ĐỐI TƯỢNG", $"{Read("Đối tượng")} • {Read("Mã đối tượng")}");
        Add(head, 0, 2, "LÝ DO THAO TÁC", Read("Lý do"));
        Add(head, 1, 2, "MÃ NHẬT KÝ (ID)", Read("Id"));

        headCard.Controls.Add(head);

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

        values.Controls.Add(ValueBox("GIÁ TRỊ CŨ (TRƯỚC THAO TÁC)", Read("Giá trị cũ")), 0, 0);
        values.Controls.Add(ValueBox("GIÁ TRỊ MỚI (SAU THAO TÁC)", Read("Giá trị mới")), 1, 0);

        valuesCard.Controls.Add(values);

        Controls.Add(valuesCard);
        Controls.Add(headCard);
    }

    private static void Add(TableLayoutPanel table, int column, int row, string label, string value)
    {
        var field = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 10, 4),
            BackColor = Color.Transparent
        };
        field.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        field.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            ForeColor = Theme.Muted,
            Font = Theme.Font(7.8f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);

        field.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = value,
            AutoEllipsis = true,
            ForeColor = Theme.Text,
            Font = Theme.Font(9.5f),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);

        table.Controls.Add(field, column, row);
    }

    private static Control ValueBox(string title, string value)
    {
        var box = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.Transparent
        };
        box.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        box.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        box.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            ForeColor = Theme.Secondary,
            Font = Theme.Font(8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        box.Controls.Add(new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Theme.Text,
            Font = new Font("Consolas", 9.2f),
            Text = value,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        }, 0, 1);

        return box;
    }
}
