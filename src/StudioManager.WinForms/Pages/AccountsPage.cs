using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class AccountsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _search = Ui.SearchBox("Tìm theo tên đăng nhập hoặc họ tên nhân sự...");

    private readonly Label _inspTitle = new()
    {
        Dock = DockStyle.Fill,
        Text = "Chưa chọn tài khoản",
        Font = Theme.Font(12.5f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _inspSubtitle = new()
    {
        Dock = DockStyle.Bottom,
        Height = 20,
        Text = "Chọn một tài khoản bên trái để quản lý quyền & mật khẩu",
        Font = Theme.Font(8.6f),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly DossierFieldList _dossierFields = new() { Dock = DockStyle.Top, Height = 190 };

    private readonly ModernButton _lockButton = Theme.Button("Khóa tài khoản", Theme.Danger, IconKind.Lock);
    private ModernButton? _resetButton, _detailButton;

    public AccountsPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var bar = BuildCommandToolbar();
        var workbench = BuildMasterDetailWorkbench();

        Controls.Add(workbench);
        Controls.Add(bar);
        bar.SendToBack();
        workbench.BringToFront();

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
        _grid.CellDoubleClick += (_, _) => ShowDetails();
        _grid.SelectionChanged += (_, _) => UpdateInspectorAndActions();

        UpdateInspectorAndActions();
        Load += async (_, _) => await LoadAsync();
    }

    private Control BuildCommandToolbar()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 10),
            CornerRadius = 10,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong
        };

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Theme.Surface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

        _search.Dock = DockStyle.Fill;
        _search.Margin = Padding.Empty;

        var find = Theme.OutlineButton("Tìm kiếm", Theme.BorderStrong, IconKind.Search);
        find.Dock = DockStyle.Fill;
        find.Margin = new Padding(8, 0, 0, 0);
        find.Click += async (_, _) => await LoadAsync();

        var refresh = Theme.OutlineButton("Làm mới", Theme.BorderStrong, IconKind.Refresh);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(8, 0, 0, 0);
        refresh.Click += async (_, _) => { _search.Clear(); await LoadAsync(); };

        var export = Theme.Button("Xuất Excel", Theme.Excel, IconKind.Download);
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(8, 0, 0, 0);
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, "tai-khoan");

        var create = Theme.Button("Tạo tài khoản", Theme.Primary, IconKind.Plus);
        create.Dock = DockStyle.Fill;
        create.Margin = new Padding(8, 0, 0, 0);
        create.Click += async (_, _) => await CreateAsync();

        row.Controls.Add(_search, 0, 0);
        row.Controls.Add(find, 1, 0);
        row.Controls.Add(refresh, 2, 0);
        row.Controls.Add(export, 3, 0);
        row.Controls.Add(create, 4, 0);

        card.Controls.Add(row);
        return card;
    }

    private Control BuildMasterDetailWorkbench()
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

        var leftCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(1),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 10
        };
        _grid.Margin = Padding.Empty;
        leftCard.Controls.Add(_grid);

        var rightCard = BuildRightSecurityInspector();

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        return split;
    }

    private Control BuildRightSecurityInspector()
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
            Text = "HỒ SƠ BẢO MẬT & KIỂM SOÁT TRUY CẬP",
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        head.Controls.Add(_inspTitle);
        head.Controls.Add(_inspSubtitle);
        head.Controls.Add(overline);

        var policyCard = new CardPanel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 6, 0, 8),
            CornerRadius = 8,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.Border,
            AccentStripeColor = Theme.Warning,
            AccentStripeLeft = true,
            AccentStripeThickness = 3
        };
        policyCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Chính sách bảo vệ: Quản trị viên không thể tự khóa tài khoản đang đăng nhập. Khi đặt lại mật khẩu, nhân sự sẽ được yêu cầu đổi mật khẩu ở lần đăng nhập kế tiếp.",
            Font = Theme.Font(8.1f),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = Color.Transparent
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _resetButton = Theme.Button("Đặt lại MK", Theme.Warning, IconKind.Key);
        _resetButton.Dock = DockStyle.Fill;
        _resetButton.Margin = new Padding(0, 0, 4, 0);
        _resetButton.Click += async (_, _) => await ResetAsync();

        _lockButton.Dock = DockStyle.Fill;
        _lockButton.Margin = new Padding(2, 0, 2, 0);
        _lockButton.Click += async (_, _) => await LockAsync();

        _detailButton = Theme.OutlineButton("Chi tiết", Theme.BorderStrong, IconKind.Eye);
        _detailButton.Dock = DockStyle.Fill;
        _detailButton.Margin = new Padding(4, 0, 0, 0);
        _detailButton.Click += (_, _) => ShowDetails();

        actions.Controls.Add(_resetButton, 0, 0);
        actions.Controls.Add(_lockButton, 1, 0);
        actions.Controls.Add(_detailButton, 2, 0);

        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 8, 0, 0) };
        body.Controls.Add(_dossierFields);

        card.Controls.Add(body);
        card.Controls.Add(head);
        card.Controls.Add(policyCard);
        card.Controls.Add(actions);
        head.SendToBack();
        actions.SendToBack();
        policyCard.SendToBack();
        body.BringToFront();
        return card;
    }

    private int? Id() => _grid.CurrentRow?.Cells["Id"].Value is object x ? Convert.ToInt32(x) : null;

    private async Task LoadAsync()
    {
        try
        {
            var rows = await _app.Administration.QueryGridAsync("TaiKhoan", string.IsNullOrWhiteSpace(_search.Text) ? null : _search.Text.Trim(), _app.Session!);
            var dt = new System.Data.DataTable();
            if (rows.Count > 0)
            {
                foreach (var k in rows[0].Keys) dt.Columns.Add(k, typeof(object));
                foreach (var r in rows)
                {
                    var x = dt.NewRow();
                    foreach (var p in r) x[p.Key] = p.Value ?? DBNull.Value;
                    dt.Rows.Add(x);
                }
            }
            _grid.DataSource = dt;
            if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;

            if (_grid.Rows.Count > 0)
            {
                _grid.Rows[0].Selected = true;
                var firstCol = _grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
                if (firstCol is not null) _grid.CurrentCell = _grid.Rows[0].Cells[firstCol.Index];
            }
            UpdateInspectorAndActions();
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }

    private void ConfigureGridPainting()
    {
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is null) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName == "Vai trò" && e.Value is string role)
            {
                e.CellStyle.ForeColor = role.Contains("QuanTri", StringComparison.OrdinalIgnoreCase) ? Theme.Primary : Theme.TextSecondary;
                e.CellStyle.Font = Theme.Font(8.9f, FontStyle.Bold);
            }
        };

        _grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName != "Trạng thái") return;

            e.PaintBackground(e.ClipBounds, true);
            var status = e.FormattedValue?.ToString() ?? "";
            if (!string.IsNullOrEmpty(status) && e.Graphics is { } g)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
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

    private async Task CreateAsync()
    {
        using var d = new AccountDialog(await _app.Administration.GetLookupsAsync("NhanVien"));
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var r = await _app.Administration.CreateAccountAsync(d.Username, d.Password, d.EmployeeId, d.Role, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await LoadAsync(); }
    }

    private async Task ResetAsync()
    {
        var id = Id();
        if (id is null) { Ui.Error(this, "Vui lòng chọn một tài khoản."); return; }
        if (id == _app.Session!.TaiKhoanId) { Ui.Error(this, "Hãy dùng chức năng đổi mật khẩu cho chính tài khoản đang đăng nhập."); return; }

        using var d = new PasswordPrompt();
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var r = await _app.Administration.ResetPasswordAsync(id.Value, d.Value, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await LoadAsync(); }
    }

    private async Task LockAsync()
    {
        var id = Id();
        if (id is null) { Ui.Error(this, "Vui lòng chọn một tài khoản."); return; }
        if (id == _app.Session!.TaiKhoanId) { Ui.Error(this, "Không thể tự khóa tài khoản đang đăng nhập."); return; }

        var locking = IsActiveSelection();
        var action = locking ? "khóa" : "mở khóa";
        if (MessageBox.Show($"Xác nhận {action} tài khoản người dùng đã chọn?", "Xác nhận thao tác", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var r = await _app.Administration.ToggleAccountLockAsync(id.Value, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else { Ui.Info(this, r.Message); await LoadAsync(); }
    }

    private bool IsActiveSelection() =>
        _grid.CurrentRow is not null && _grid.Columns.Contains("Trạng thái") &&
        _grid.CurrentRow.Cells["Trạng thái"].Value?.ToString() == "HOAT_DONG";

    private void UpdateInspectorAndActions()
    {
        var id = Id();
        var row = _grid.CurrentRow;
        var enabled = id is not null && row is not null;
        var active = IsActiveSelection();

        _lockButton.Text = active ? "Khóa tài khoản" : "Mở khóa";
        _lockButton.Icon = active ? IconKind.Lock : IconKind.Unlock;
        _lockButton.BackColor = active ? Theme.Danger : Theme.Success;
        _lockButton.Enabled = enabled;
        if (_resetButton is not null) _resetButton.Enabled = enabled;
        if (_detailButton is not null) _detailButton.Enabled = enabled;

        if (!enabled || row is null)
        {
            _inspTitle.Text = "Chưa chọn tài khoản";
            _inspSubtitle.Text = "Chọn một dòng bên trái để kiểm tra bảo mật";
            _dossierFields.SetRows([("Trạng thái", "Chưa có tài khoản được chọn", Theme.Muted, false)]);
            return;
        }

        var visibleCols = _grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
        var username = _grid.Columns.Contains("Tên đăng nhập") ? row.Cells["Tên đăng nhập"].FormattedValue?.ToString() ?? "" : "";
        var role = _grid.Columns.Contains("Vai trò") ? row.Cells["Vai trò"].FormattedValue?.ToString() ?? "" : "";

        _inspTitle.Text = $"{username}  •  {role}";
        _inspSubtitle.Text = id == _app.Session?.TaiKhoanId ? "Tài khoản phiên làm việc hiện tại của bạn" : $"Mã tài khoản hệ thống: #{id}";

        var fieldList = new List<(string Label, string Value, Color? ValueColor, bool Highlight)>();
        foreach (var col in visibleCols)
        {
            var val = row.Cells[col.Index].FormattedValue?.ToString() ?? "—";
            var isStatus = col.HeaderText.Contains("Trạng thái", StringComparison.OrdinalIgnoreCase);
            var isRole = col.HeaderText.Contains("Vai trò", StringComparison.OrdinalIgnoreCase);
            Color? colColor = isStatus ? Theme.GetStatusColors(val).Text : (isRole ? Theme.Primary : null);
            fieldList.Add((col.HeaderText, val, colColor, isStatus || isRole));
        }
        _dossierFields.SetRows(fieldList);
    }

    private void ShowDetails()
    {
        if (_grid.CurrentRow is null) { Ui.Error(this, "Vui lòng chọn một tài khoản."); return; }
        var lines = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .Select(c => $"{c.HeaderText}: {_grid.CurrentRow.Cells[c.Index].FormattedValue}");
        MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, lines), "Chi tiết tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

internal sealed class PasswordPrompt : Form
{
    private readonly TextBox _value = new() { UseSystemPasswordChar = true, Font = Theme.Font(10) };
    public string Value => _value.Text;

    public PasswordPrompt()
    {
        Text = "Đặt lại mật khẩu tạm • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 260);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = Theme.Warning,
            AccentStripeThickness = 4
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        body.Controls.Add(Ui.Field("MẬT KHẨU TẠM MỚI (TỐI THIỂU 8 KÝ TỰ)", _value, 376));

        var ok = Theme.Button("Xác nhận đặt lại", Theme.Primary, IconKind.Key);
        ok.Width = 376;
        ok.Click += (_, _) =>
        {
            if (Value.Length < 8)
            {
                Ui.Error(this, "Mật khẩu tạm phải có ít nhất 8 ký tự.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        body.Controls.Add(ok);

        card.Controls.Add(body);
        Controls.Add(card);
        AcceptButton = ok;
    }
}

internal sealed class AccountDialog : Form
{
    private readonly TextBox _username = new() { Font = Theme.Font(9.5f) };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Font = Theme.Font(9.5f) };
    private readonly ComboBox _employee = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };
    private readonly ComboBox _role = new() { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(9.5f) };

    public string Username => _username.Text.Trim();
    public string Password => _password.Text;
    public int? EmployeeId => _employee.SelectedValue is int i ? i : null;
    public VaiTro Role => _role.SelectedIndex == 0 ? VaiTro.NhanVien : VaiTro.QuanTriVien;

    public AccountDialog(IReadOnlyList<StudioManager.Application.LookupItem> employees)
    {
        Text = "Khởi tạo tài khoản truy cập • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 470);
        BackColor = Theme.Background;
        Padding = new Padding(18);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _employee.DisplayMember = "Name";
        _employee.ValueMember = "Id";
        _employee.DataSource = employees.Select(x => new Employee((int)x.Id, x.Name)).ToList();

        _role.Items.AddRange(["Nhân viên vận hành", "Quản trị viên hệ thống"]);
        _role.SelectedIndex = 0;

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Theme.Surface,
            CornerRadius = 10,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        body.Controls.Add(Ui.Field("NHÂN VIÊN LIÊN KẾT", _employee, 416));
        body.Controls.Add(Ui.Field("TÊN ĐĂNG NHẬP *", _username, 416));
        body.Controls.Add(Ui.Field("MẬT KHẨU KHỞI TẠO *", _password, 416));
        body.Controls.Add(Ui.Field("VAI TRÒ PHÂN QUYỀN", _role, 416));

        var save = Theme.Button("Tạo tài khoản", Theme.Primary, IconKind.Check);
        save.Width = 416;
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(Username) || Password.Length < 8)
            {
                Ui.Error(this, "Vui lòng nhập tên đăng nhập và mật khẩu ít nhất 8 ký tự.");
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

    private sealed record Employee(int Id, string Name);
}
