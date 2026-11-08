using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class AccountsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _search = Ui.SearchBox();
    private readonly ModernButton _lockButton = Theme.Button("Khóa tài khoản", Theme.Danger);
    private ModernButton? _resetButton, _detailButton;

    public AccountsPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var header = new PageHeader("Tài khoản người dùng", "Quản lý tài khoản đăng nhập, phân quyền, khóa và đặt lại mật khẩu.");
        var add = Theme.Button("+ Tạo tài khoản");
        add.Width = 150;
        add.Click += async (_, _) => await CreateAsync();
        header.Actions.Controls.Add(add);

        var bar = BuildSearchBar();
        var card = Ui.DataHost(_grid);
        var actions = BuildSelectionActions();

        Controls.Add(card);
        Controls.Add(actions);
        Controls.Add(bar);
        Controls.Add(header);

        _grid.CellFormatting += GridCellFormatting;
        _grid.CellDoubleClick += (_, _) => ShowDetails();
        _grid.SelectionChanged += (_, _) => UpdateLockButton();

        UpdateLockButton();
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
        _search.PlaceholderText = "Tìm theo tên đăng nhập hoặc họ tên nhân viên...";

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
        export.Click += (_, _) => Ui.ExportGrid(_grid, this, "tai-khoan");

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

        _lockButton.Width = 144;
        _lockButton.Click += async (_, _) => await LockAsync();

        _resetButton = Theme.Button("Đặt lại mật khẩu", Theme.Warning);
        _resetButton.Width = 155;
        _resetButton.Click += async (_, _) => await ResetAsync();

        _detailButton = Theme.Button("Xem chi tiết", Theme.Secondary);
        _detailButton.Width = 120;
        _detailButton.Click += (_, _) => ShowDetails();

        group.Controls.Add(_lockButton);
        group.Controls.Add(_resetButton);
        group.Controls.Add(_detailButton);

        return Ui.SelectionActionBar("Chọn một tài khoản để xem chi tiết, đặt lại mật khẩu hoặc đổi trạng thái khóa/mở khóa.", group);
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
            UpdateLockButton();
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
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
        else if (colName == "Vai trò" && e.Value is string role)
        {
            e.CellStyle.ForeColor = role.Contains("QuanTri", StringComparison.OrdinalIgnoreCase) ? Theme.Primary : Theme.TextSecondary;
            e.CellStyle.Font = Theme.Font(9.0f, FontStyle.Bold);
        }
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
        else Ui.Info(this, r.Message);
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
        _grid.CurrentRow is not null && _grid.Columns.Contains("Trạng thái") && _grid.CurrentRow.Cells["Trạng thái"].Value?.ToString() == "HOAT_DONG";

    private void UpdateLockButton()
    {
        var enabled = Id() is not null;
        var active = IsActiveSelection();
        _lockButton.Text = active ? "Khóa tài khoản" : "Mở khóa tài khoản";
        _lockButton.BackColor = active ? Theme.Danger : Theme.Success;
        _lockButton.Enabled = enabled;
        if (_resetButton is not null) _resetButton.Enabled = enabled;
        if (_detailButton is not null) _detailButton.Enabled = enabled;
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
        Text = "Đặt lại mật khẩu tài khoản • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 220);
        BackColor = Theme.Background;
        Padding = new Padding(26);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        body.Controls.Add(Ui.Field("MẬT KHẨU TẠM MỚI (TỐI THIỂU 8 KÝ TỰ)", _value, 380));

        var ok = Theme.Button("Xác nhận đặt lại");
        ok.Width = 380;
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

        Controls.Add(body);
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
        Text = "Tạo tài khoản người dùng mới • StudioManager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 420);
        BackColor = Theme.Background;
        Padding = new Padding(26);
        Font = Theme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _employee.DisplayMember = "Name";
        _employee.ValueMember = "Id";
        _employee.DataSource = employees.Select(x => new Employee((int)x.Id, x.Name)).ToList();

        _role.Items.AddRange(["Nhân viên vận hành", "Quản trị viên hệ thống"]);
        _role.SelectedIndex = 0;

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        body.Controls.Add(Ui.Field("NHÂN VIÊN LIÊN KẾT", _employee, 410));
        body.Controls.Add(Ui.Field("TÊN ĐĂNG NHẬP *", _username, 410));
        body.Controls.Add(Ui.Field("MẬT KHẨU TẠM *", _password, 410));
        body.Controls.Add(Ui.Field("VAI TRÒ TRUY CẬP", _role, 410));

        var save = Theme.Button("Tạo tài khoản");
        save.Width = 410;
        save.Click += (_, _) =>
        {
            if (Username.Length < 3 || Password.Length < 8)
            {
                Ui.Error(this, "Tên đăng nhập tối thiểu 3 ký tự, mật khẩu tối thiểu 8 ký tự.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        body.Controls.Add(save);

        Controls.Add(body);
        AcceptButton = save;
    }

    private sealed record Employee(int Id, string Name);
}
