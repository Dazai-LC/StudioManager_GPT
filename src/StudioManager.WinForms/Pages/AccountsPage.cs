using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class AccountsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid=Theme.Grid();
    private readonly TextBox _search=Ui.SearchBox();

    public AccountsPage(AppFacade app)
    {
        _app=app;
        BackColor=Theme.Background;

        var header=new PageHeader("Tài khoản","Tạo, khóa/mở khóa và đặt lại mật khẩu.");
        var add=Theme.Button("+ Tạo tài khoản");
        add.Width=150;
        add.Click+=async(_,_)=>await CreateAsync();
        header.Actions.Controls.Add(add);

        var bar=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(0,8,0,8),WrapContents=true};
        bar.Controls.Add(_search);
        var find=Theme.Button("Tìm kiếm",Color.FromArgb(71,85,105));
        find.Click+=async(_,_)=>await LoadAsync();
        bar.Controls.Add(find);
        var refresh=Theme.Button("Làm mới",Color.FromArgb(71,85,105));
        refresh.Click+=async(_,_)=>{_search.Clear();await LoadAsync();};
        bar.Controls.Add(refresh);
        var detail=Theme.Button("Chi tiết",Color.FromArgb(71,85,105));
        detail.Click+=(_,_)=>ShowDetails();
        bar.Controls.Add(detail);
        var export=Theme.Button("Xuất CSV",Theme.Purple);
        export.Click+=(_,_)=>Ui.ExportGrid(_grid,this,"tai-khoan");
        bar.Controls.Add(export);
        var reset=Theme.Button("Đặt lại mật khẩu",Theme.Warning);
        reset.Width=155;
        reset.Click+=async(_,_)=>await ResetAsync();
        bar.Controls.Add(reset);
        var lockButton=Theme.Button("Khóa / mở",Theme.Danger);
        lockButton.Width=130;
        lockButton.Click+=async(_,_)=>await LockAsync();
        bar.Controls.Add(lockButton);

        var card=new CardPanel{Dock=DockStyle.Fill};
        card.Controls.Add(_grid);
        Controls.Add(card);
        Controls.Add(bar);
        Controls.Add(header);
        _grid.CellDoubleClick+=(_,_)=>ShowDetails();
        Load+=async(_,_)=>await LoadAsync();
    }
    private int? Id()=>_grid.CurrentRow?.Cells["Id"].Value is object x?Convert.ToInt32(x):null;
    private async Task LoadAsync(){var rows=await _app.Administration.QueryGridAsync("TaiKhoan",string.IsNullOrWhiteSpace(_search.Text)?null:_search.Text.Trim(),_app.Session!);var dt=new System.Data.DataTable();if(rows.Count>0){foreach(var k in rows[0].Keys)dt.Columns.Add(k,typeof(object));foreach(var r in rows){var x=dt.NewRow();foreach(var p in r)x[p.Key]=p.Value??DBNull.Value;dt.Rows.Add(x);}}_grid.DataSource=dt;if(_grid.Columns.Contains("Id"))_grid.Columns["Id"].Visible=false;}
    private async Task CreateAsync(){using var d=new AccountDialog(await _app.Administration.GetLookupsAsync("NhanVien"));if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Administration.CreateAccountAsync(d.Username,d.Password,d.EmployeeId,d.Role,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task ResetAsync(){var id=Id();if(id is null)return;using var d=new PasswordPrompt();if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Administration.ResetPasswordAsync(id.Value,d.Value,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else Ui.Info(this,r.Message);}
    private async Task LockAsync(){var id=Id();if(id is null)return;if(id==_app.Session!.TaiKhoanId){Ui.Error(this,"Không thể khóa tài khoản đang đăng nhập.");return;}var r=await _app.Administration.DeactivateAsync("TaiKhoan",id.Value,_app.Session);if(!r.Success)Ui.Error(this,r.Message);else await LoadAsync();}
    private void ShowDetails()
    {
        if(_grid.CurrentRow is null){Ui.Error(this,"Vui lòng chọn một tài khoản.");return;}
        var lines=_grid.Columns.Cast<DataGridViewColumn>()
            .Where(c=>c.Visible)
            .Select(c=>$"{c.HeaderText}: {_grid.CurrentRow.Cells[c.Index].FormattedValue}");
        MessageBox.Show(string.Join(Environment.NewLine,lines),"Chi tiết tài khoản",MessageBoxButtons.OK,MessageBoxIcon.Information);
    }
}

internal sealed class PasswordPrompt : Form
{
    private readonly TextBox _value=new(){UseSystemPasswordChar=true};public string Value=>_value.Text;
    public PasswordPrompt(){Text="Đặt lại mật khẩu";StartPosition=FormStartPosition.CenterParent;ClientSize=new(420,210);BackColor=Theme.Background;Padding=new Padding(28);var body=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};body.Controls.Add(Ui.Field("MẬT KHẨU TẠM MỚI",_value,330));var ok=Theme.Button("Xác nhận");ok.Width=330;ok.Click+=(_,_)=>{if(Value.Length<8){Ui.Error(this,"Mật khẩu phải có ít nhất 8 ký tự.");return;}DialogResult=DialogResult.OK;Close();};body.Controls.Add(ok);Controls.Add(body);}
}

internal sealed class AccountDialog : Form
{
    private readonly TextBox _username=new(),_password=new(){UseSystemPasswordChar=true};private readonly ComboBox _employee=new(){DropDownStyle=ComboBoxStyle.DropDownList},_role=new(){DropDownStyle=ComboBoxStyle.DropDownList};public string Username=>_username.Text.Trim();public string Password=>_password.Text;public int? EmployeeId=>_employee.SelectedValue is int i?i:null;public VaiTro Role=>_role.SelectedIndex==0?VaiTro.NhanVien:VaiTro.QuanTriVien;
    public AccountDialog(IReadOnlyList<StudioManager.Application.LookupItem> employees){Text="Tạo tài khoản";StartPosition=FormStartPosition.CenterParent;ClientSize=new(450,410);BackColor=Theme.Background;Padding=new Padding(28);_employee.DisplayMember="Name";_employee.ValueMember="Id";_employee.DataSource=employees.Select(x=>new Employee((int)x.Id,x.Name)).ToList();_role.Items.AddRange(["Nhân viên","Quản trị viên"]);_role.SelectedIndex=0;var body=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};body.Controls.Add(Ui.Field("NHÂN VIÊN",_employee,360));body.Controls.Add(Ui.Field("TÊN ĐĂNG NHẬP *",_username,360));body.Controls.Add(Ui.Field("MẬT KHẨU TẠM *",_password,360));body.Controls.Add(Ui.Field("VAI TRÒ",_role,360));var save=Theme.Button("Tạo tài khoản");save.Width=360;save.Click+=(_,_)=>{if(Username.Length<3||Password.Length<8){Ui.Error(this,"Tên đăng nhập tối thiểu 3 ký tự, mật khẩu tối thiểu 8 ký tự.");return;}DialogResult=DialogResult.OK;Close();};body.Controls.Add(save);Controls.Add(body);}
    private sealed record Employee(int Id,string Name);
}
