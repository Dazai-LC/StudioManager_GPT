using System.ComponentModel;
using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class CrudPage : UserControl
{
    private readonly AppFacade _app;private readonly string _entity;private readonly bool _readOnly;private readonly DataGridView _grid=Theme.Grid();private readonly TextBox _search=Ui.SearchBox();
    public CrudPage(AppFacade app,string title,string entity,bool readOnly=false)
    {
        _app=app;_entity=entity;_readOnly=readOnly;BackColor=Theme.Background;var header=new PageHeader(title,"Quản lý, tìm kiếm và theo dõi dữ liệu tập trung.");var add=Theme.Button(entity=="KhachHang"?"+ Thêm khách hàng":"+ Thêm mới");add.Width=155;add.Visible=!readOnly;add.Click+=async(_,_)=>await EditAsync(null);header.Actions.Controls.Add(add);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Padding=new Padding(0,7,0,7)};toolbar.Controls.Add(_search);var find=Theme.Button("Tìm kiếm",Color.FromArgb(71,85,105));find.Width=100;find.Click+=async(_,_)=>await LoadAsync();toolbar.Controls.Add(find);var refresh=Theme.Button("↻ Làm mới",Theme.Cyan);refresh.Width=105;refresh.Click+=async(_,_)=>{_search.Clear();await LoadAsync();};toolbar.Controls.Add(refresh);var details=Theme.Button("Xem chi tiết",Theme.Purple);details.Width=120;details.Click+=(_,_)=>ShowDetails();toolbar.Controls.Add(details);var export=Theme.Button("Xuất CSV",Theme.Success);export.Width=105;export.Click+=(_,_)=>Ui.ExportGrid(_grid,this,_entity);toolbar.Controls.Add(export);var edit=Theme.Button("Chỉnh sửa",Theme.Primary);edit.Visible=!readOnly;edit.Click+=async(_,_)=>await EditAsync(SelectedId());toolbar.Controls.Add(edit);var stop=Theme.Button(entity=="KhachHang"?"Xóa":"Ngừng dùng",Theme.Danger);stop.Visible=!readOnly;stop.Click+=async(_,_)=>await DeactivateAsync();toolbar.Controls.Add(stop);
        var card=new CardPanel{Dock=DockStyle.Fill};card.Controls.Add(_grid);Controls.Add(card);Controls.Add(toolbar);Controls.Add(header);_search.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter)await LoadAsync();};_grid.CellDoubleClick+=(_,_)=>ShowDetails();Load+=async(_,_)=>await LoadAsync();
    }
    private long? SelectedId()=>_grid.CurrentRow?.Cells["Id"].Value is object v?Convert.ToInt64(v):null;
    private async Task LoadAsync(){try{var rows=await _app.Administration.QueryGridAsync(_entity,string.IsNullOrWhiteSpace(_search.Text)?null:_search.Text.Trim(),_app.Session!);var table=new BindingList<RowView>(rows.Select(x=>new RowView(x)).ToList());_grid.DataSource=table;if(_grid.Columns.Contains("Data"))_grid.Columns["Data"].Visible=false;BuildColumns(rows);}catch(Exception ex){Ui.Error(this,ex.Message);}}
    private void BuildColumns(IReadOnlyList<IDictionary<string,object?>> rows){if(rows.Count==0)return;_grid.DataSource=null;var dt=new System.Data.DataTable();foreach(var key in rows[0].Keys)dt.Columns.Add(key,typeof(object));foreach(var r in rows){var row=dt.NewRow();foreach(var p in r)row[p.Key]=p.Value??DBNull.Value;dt.Rows.Add(row);}_grid.DataSource=dt;if(_grid.Columns.Contains("Id"))_grid.Columns["Id"].Visible=false;}
    private async Task EditAsync(long? id){if(_readOnly)return;using var dialog=new SimpleEditDialog(_entity,id,_grid.CurrentRow);if(dialog.ShowDialog(this)!=DialogResult.OK)return;var result=await _app.Administration.SaveAsync(_entity,id,dialog.Values,_app.Session!);if(!result.Success)Ui.Error(this,result.Message);else{Ui.Info(this,result.Message);await LoadAsync();}}
    private async Task DeactivateAsync(){var id=SelectedId();if(id is null)return;var message=_entity=="KhachHang"?"Xóa khách hàng đã chọn? Chỉ khách hàng chưa có lịch mới được xóa.":"Ngừng sử dụng mục đã chọn? Dữ liệu lịch sử vẫn được giữ nguyên.";if(MessageBox.Show(message,"Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;var r=await _app.Administration.DeactivateAsync(_entity,id.Value,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await LoadAsync();}
    private void ShowDetails(){if(_grid.CurrentRow is null){Ui.Error(this,"Vui lòng chọn một dòng dữ liệu.");return;}var lines=_grid.Columns.Cast<DataGridViewColumn>().Where(x=>x.Visible).OrderBy(x=>x.DisplayIndex).Select(x=>$"{x.HeaderText}: {_grid.CurrentRow.Cells[x.Index].FormattedValue}");MessageBox.Show(string.Join(Environment.NewLine+Environment.NewLine,lines),"Chi tiết dữ liệu",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    private sealed class RowView(IDictionary<string,object?> data){[Browsable(false)]public IDictionary<string,object?> Data{get;}=data;}
}

internal sealed class SimpleEditDialog : Form
{
    private readonly string _entity;private readonly Dictionary<string,Control> _inputs=new();public IReadOnlyDictionary<string,object?> Values=>_inputs.ToDictionary(x=>x.Key,x=>Read(x.Value));
    public SimpleEditDialog(string entity,long? id,DataGridViewRow? row)
    {
        _entity=entity;Text=(id is null?"Thêm ":"Cập nhật ")+entity;StartPosition=FormStartPosition.CenterParent;ClientSize=new(620,520);BackColor=Theme.Background;Font=Theme.Font();FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;var body=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(24),FlowDirection=FlowDirection.LeftToRight};foreach(var f in Fields(entity)){var input=CreateInput(f);_inputs[f.Name]=input;body.Controls.Add(Ui.Field(f.Label,input,260));}var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=68,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12)};var save=Theme.Button("Lưu");save.Click+=(_,_)=>{if(ValidateFields()){DialogResult=DialogResult.OK;Close();}};var cancel=Theme.Button("Hủy",Color.FromArgb(100,116,139));cancel.Click+=(_,_)=>Close();footer.Controls.Add(save);footer.Controls.Add(cancel);Controls.Add(body);Controls.Add(footer);if(row!=null)Fill(row);
    }
    private static IEnumerable<FieldDef> Fields(string e)=>e switch
    {
        "KhachHang"=>[new("HoTen","Họ và tên *"),new("SoDienThoai","Số điện thoại *"),new("Email","Email"),new("DiaChi","Địa chỉ"),new("GhiChu","Ghi chú")],
        "NhanVien"=>[new("HoTen","Họ và tên *"),new("SoDienThoai","Số điện thoại *"),new("ChucVu","Chức vụ",["TIEP_NHAN","NHIEP_ANH_GIA","KHAC"]),new("TrangThai","Trạng thái",["DANG_LAM","NGUNG_LAM"]),new("GhiChu","Ghi chú")],
        "GoiChup"=>[new("TenGoiChup","Tên gói *"),new("GiaGoi","Giá gói *",null,true),new("ThoiLuongPhut","Thời lượng phút *",null,true),new("MoTa","Mô tả"),new("TrangThai","Trạng thái",["DANG_AP_DUNG","NGUNG_AP_DUNG"])],
        "DichVu"=>[new("TenDichVu","Tên dịch vụ *"),new("DonViTinh","Đơn vị tính *"),new("DonGia","Đơn giá *",null,true),new("MoTa","Mô tả"),new("TrangThai","Trạng thái",["DANG_CUNG_CAP","NGUNG_CUNG_CAP"])],
        "PhongChup"=>[new("TenPhong","Tên phòng *"),new("MoTa","Mô tả"),new("TrangThai","Trạng thái",["HOAT_DONG","NGUNG_SU_DUNG"])],
        "TaiNguyen"=>[new("TenTaiNguyen","Tên tài nguyên *"),new("LoaiTaiNguyen","Loại",["THIET_BI","TRANG_PHUC"]),new("TongSoLuong","Tổng số lượng *",null,true),new("GhiChu","Ghi chú"),new("TrangThai","Trạng thái",["HOAT_DONG","NGUNG_SU_DUNG"])],_=>[]
    };
    private static Control CreateInput(FieldDef f){if(f.Options is not null){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};c.Items.AddRange(f.Options);if(c.Items.Count>0)c.SelectedIndex=0;return c;}if(f.Number)return new NumericUpDown{Maximum=1_000_000_000,ThousandsSeparator=true};return new TextBox();}
    private void Fill(DataGridViewRow row){var grid=row.DataGridView;if(grid is null)return;foreach(var p in _inputs){var col=grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c=>Normalize(c.HeaderText).Contains(Normalize(LabelFor(p.Key)))||Normalize(c.Name).Contains(Normalize(LabelFor(p.Key))));if(col is null)continue;var value=row.Cells[col.Index].Value;if(p.Value is ComboBox cb)cb.SelectedItem=value?.ToString();else if(p.Value is NumericUpDown n&&decimal.TryParse(value?.ToString(),out var d))n.Value=Math.Min(n.Maximum,d);else p.Value.Text=value?.ToString()??"";}}
    private bool ValidateFields(){foreach(var p in _inputs.Where(x=>x.Key is "HoTen" or "SoDienThoai" or "TenGoiChup" or "TenDichVu" or "DonViTinh" or "TenPhong" or "TenTaiNguyen")){if(string.IsNullOrWhiteSpace(p.Value.Text)){MessageBox.Show("Vui lòng nhập đầy đủ các trường bắt buộc.");p.Value.Focus();return false;}}return true;}
    private static object? Read(Control c)=>c switch{NumericUpDown n=>n.Value,ComboBox cb=>cb.SelectedItem?.ToString(),_=>string.IsNullOrWhiteSpace(c.Text)?null:c.Text.Trim()};
    private static string Normalize(string s)=>new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();private static string LabelFor(string k)=>k switch{"HoTen"=>"Họ tên","SoDienThoai"=>"Điện thoại","TenGoiChup"=>"Tên gói","GiaGoi"=>"Giá","ThoiLuongPhut"=>"Thời lượng","TenDichVu"=>"Tên dịch vụ","DonViTinh"=>"Đơn vị","DonGia"=>"Đơn giá","TenPhong"=>"Tên phòng","TenTaiNguyen"=>"Tên tài nguyên","LoaiTaiNguyen"=>"Loại","TongSoLuong"=>"Số lượng","GhiChu"=>"Ghi chú","MoTa"=>"Mô tả","TrangThai"=>"Trạng thái",_=>k};
    private sealed record FieldDef(string Name,string Label,string[]? Options=null,bool Number=false);
}
