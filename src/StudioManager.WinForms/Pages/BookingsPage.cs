using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class BookingsPage : UserControl
{
    private readonly AppFacade _app;private readonly DataGridView _grid=Theme.Grid();private readonly TextBox _search=Ui.SearchBox("Mã lịch, khách hàng, số điện thoại...");private IReadOnlyList<LichChup> _items=[];
    public BookingsPage(AppFacade app)
    {
        _app=app;BackColor=Theme.Background;var header=new PageHeader("Lịch chụp","Theo dõi toàn bộ lịch, tiến độ và tài chính.");var add=Theme.Button("+ Tạo lịch");add.Click+=async(_,_)=>await CreateAsync();header.Actions.Controls.Add(add);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(0,8,0,8),WrapContents=true};bar.Controls.Add(_search);var find=Theme.Button("Tìm",Color.FromArgb(71,85,105));find.Width=70;find.Click+=async(_,_)=>await LoadAsync();bar.Controls.Add(find);var refresh=Theme.Button("Làm mới",Color.FromArgb(71,85,105));refresh.Click+=async(_,_)=>{_search.Clear();await LoadAsync();};bar.Controls.Add(refresh);var export=Theme.Button("Xuất CSV",Theme.Purple);export.Click+=(_,_)=>Ui.ExportGrid(_grid,this,"lich-chup");bar.Controls.Add(export);var edit=Theme.Button("Sửa lịch",Color.FromArgb(71,85,105));edit.Click+=async(_,_)=>await EditAsync();bar.Controls.Add(edit);var detail=Theme.Button("Chi tiết",Color.FromArgb(71,85,105));detail.Click+=async(_,_)=>await DetailAsync();bar.Controls.Add(detail);var progress=Theme.Button("Bước tiếp",Theme.Success);progress.Click+=async(_,_)=>await AdvanceAsync();bar.Controls.Add(progress);var pay=Theme.Button("Thu tiền",Theme.Primary);pay.Click+=async(_,_)=>await PaymentAsync(false);bar.Controls.Add(pay);var refund=Theme.Button("Hoàn tiền",Theme.Warning);refund.Visible=_app.Session!.VaiTro==VaiTro.QuanTriVien;refund.Click+=async(_,_)=>await PaymentAsync(true);bar.Controls.Add(refund);var cancel=Theme.Button("Hủy lịch",Theme.Danger);cancel.Click+=async(_,_)=>await CancelAsync();bar.Controls.Add(cancel);
        var card=new CardPanel{Dock=DockStyle.Fill};card.Controls.Add(_grid);Controls.Add(card);Controls.Add(bar);Controls.Add(header);_grid.CellDoubleClick+=async(_,_)=>await DetailAsync();Load+=async(_,_)=>await LoadAsync();
    }
    private LichChup? Selected()=>_grid.CurrentRow?.Cells["Id"].Value is object v?_items.FirstOrDefault(x=>x.Id==Convert.ToInt64(v)):null;
    private async Task LoadAsync()
    {
        try
        {
            _items=await _app.Bookings.SearchAsync(string.IsNullOrWhiteSpace(_search.Text)?null:_search.Text.Trim(),null,null,null);
            _grid.DataSource=_items.Select(x=>new
            {
                x.Id,
                MaLich=x.Ma,
                KhachHang=x.KhachHang,
                BatDau=x.BatDau.ToString("dd/MM/yyyy HH:mm"),
                KetThuc=x.KetThuc.ToString("dd/MM/yyyy HH:mm"),
                NhiepAnhGia=x.NhiepAnhGia,
                Phong=x.Phong,
                TrangThai=x.TrangThai.HienThi(),
                TongTien=x.TongThanhToan,
                ConLai=x.ConLai
            }).ToList();
            ConfigureGrid();
        }
        catch(Exception ex){Ui.Error(this,ex.Message);}
    }

    private void ConfigureGrid()
    {
        if(_grid.Columns.Contains("Id"))_grid.Columns["Id"].Visible=false;
        SetColumn("MaLich","Mã lịch",115);
        SetColumn("KhachHang","Khách hàng",150);
        SetColumn("BatDau","Bắt đầu",145);
        SetColumn("KetThuc","Kết thúc",145);
        SetColumn("NhiepAnhGia","Nhiếp ảnh gia",145);
        SetColumn("Phong","Phòng",110);
        SetColumn("TrangThai","Trạng thái",125);
        SetColumn("TongTien","Tổng tiền",130,"N0");
        SetColumn("ConLai","Còn lại",125,"N0");
    }

    private void SetColumn(string name,string header,int minimumWidth,string? format=null)
    {
        if(!_grid.Columns.Contains(name))return;
        var column=_grid.Columns[name];
        column.HeaderText=header;
        column.MinimumWidth=minimumWidth;
        if(format is not null)column.DefaultCellStyle.Format=format;
    }
    private async Task CreateAsync(){using var f=new BookingDialog(_app);if(f.ShowDialog(this)!=DialogResult.OK||f.Input is null)return;var r=await _app.Bookings.CreateAsync(f.Input,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task EditAsync(){var b=Selected();if(b is null)return;string? reason=null;if(b.TrangThai!=TrangThaiLich.DaDatLich){if(_app.Session!.VaiTro!=VaiTro.QuanTriVien){Ui.Error(this,"Chỉ được sửa thông tin cốt lõi khi lịch đang Đã đặt lịch.");return;}using var reasonDialog=new TextPrompt("Lý do hiệu chỉnh","Nhập lý do hiệu chỉnh ngoại lệ:");if(reasonDialog.ShowDialog(this)!=DialogResult.OK)return;reason=reasonDialog.Value;}using var f=new BookingDialog(_app,b);if(f.ShowDialog(this)!=DialogResult.OK||f.Input is null)return;var r=await _app.Bookings.UpdateAsync(b.Id,f.Input,reason,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task DetailAsync(){var b=Selected();if(b is null)return;using var d=new BookingDetailDialog(_app,b);d.ShowDialog(this);await LoadAsync();}
    private async Task AdvanceAsync(){var b=Selected();if(b is null)return;var next=b.TrangThai.BuocTiepTheo();if(next is null){Ui.Error(this,"Lịch đã ở trạng thái kết thúc.");return;}if(MessageBox.Show($"Chuyển sang “{next.Value.HienThi()}”?","Cập nhật tiến độ",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;var r=await _app.Bookings.AdvanceAsync(b.Id,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await LoadAsync();}
    private async Task CancelAsync(){var b=Selected();if(b is null)return;using var d=new TextPrompt("Hủy lịch chụp","Nhập lý do hủy:");if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Bookings.CancelAsync(b.Id,d.Value,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task PaymentAsync(bool refund){var b=Selected();if(b is null)return;using var d=new MoneyDialog(refund?"Hoàn tiền":"Ghi nhận thanh toán",refund?Math.Max(0,b.DaThu-b.DaHoan):b.ConLai,refund);if(d.ShowDialog(this)!=DialogResult.OK)return;Result r=refund?await _app.Finance.RefundAsync(new(b.Id,d.Amount,d.Note,d.Note),_app.Session!):await _app.Finance.ReceiveAsync(new(b.Id,d.PaymentType,d.Amount,d.Note),_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{await LoadAsync();using var receipt=new ReceiptPreviewForm(b,d.Amount,refund,_app.Session!.HoTen,d.Note);receipt.ShowDialog(this);}}
}

internal sealed class BookingDetailDialog : Form
{
    private readonly AppFacade _app;private readonly LichChup _booking;private readonly DataGridView _services=Theme.Grid(),_resources=Theme.Grid(),_payments=Theme.Grid();private readonly ToolTip _toolTip=new();
    public BookingDetailDialog(AppFacade app,LichChup booking)
    {
        _app=app;_booking=booking;Text=$"Chi tiết {booking.Ma}";StartPosition=FormStartPosition.CenterParent;ClientSize=new(940,650);MinimumSize=new(850,600);BackColor=Theme.Background;Font=Theme.Font();
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(14)};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,122));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var tabs=new TabControl{Dock=DockStyle.Fill,Font=Theme.Font(10)};tabs.TabPages.Add(BuildServices());tabs.TabPages.Add(BuildResources());tabs.TabPages.Add(BuildPayments());
        layout.Controls.Add(BuildSummary(),0,0);layout.Controls.Add(tabs,0,1);Controls.Add(layout);Load+=async(_,_)=>await ReloadAsync();
    }

    private Control BuildSummary()
    {
        var card=new CardPanel{Dock=DockStyle.Fill,Padding=new Padding(18,12,18,12),Margin=Padding.Empty};
        var lines=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};
        lines.RowStyles.Add(new RowStyle(SizeType.Percent,34));lines.RowStyles.Add(new RowStyle(SizeType.Percent,33));lines.RowStyles.Add(new RowStyle(SizeType.Percent,33));
        var title=$"{_booking.Ma}  •  {_booking.KhachHang}  •  {_booking.TenGoiChot}";
        var schedule=$"Thời gian: {_booking.BatDau:dd/MM/yyyy HH:mm} — {_booking.KetThuc:dd/MM/yyyy HH:mm}  •  {_booking.NhiepAnhGia}  •  {_booking.Phong}";
        var finance=$"Tổng tiền: {_booking.TongThanhToan:N0} đ  |  Đã thu: {_booking.DaThu:N0} đ  |  Còn lại: {_booking.ConLai:N0} đ";
        lines.Controls.Add(SummaryLine(title,Theme.Text,Theme.Font(11,FontStyle.Bold)),0,0);
        lines.Controls.Add(SummaryLine(schedule,Theme.Muted,Theme.Font(9.5f)),0,1);
        lines.Controls.Add(SummaryLine(finance,Theme.Primary,Theme.Font(10,FontStyle.Bold)),0,2);
        card.Controls.Add(lines);return card;
    }

    private Label SummaryLine(string text,Color color,Font font)
    {
        var label=new Label{Dock=DockStyle.Fill,Text=text,AutoEllipsis=true,ForeColor=color,Font=font,TextAlign=ContentAlignment.MiddleLeft};
        _toolTip.SetToolTip(label,text);return label;
    }
    private TabPage BuildServices(){var p=new TabPage("Dịch vụ & giảm giá"){BackColor=Theme.Background,Padding=new Padding(14)};var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=54};var add=Theme.Button("+ Thêm dịch vụ");add.Click+=async(_,_)=>await AddServiceAsync();var discount=Theme.Button("Nhập giảm giá",Theme.Warning);discount.Click+=async(_,_)=>await DiscountAsync();bar.Controls.Add(add);bar.Controls.Add(discount);p.Controls.Add(_services);p.Controls.Add(bar);return p;}
    private TabPage BuildResources(){var p=new TabPage("Tài nguyên"){BackColor=Theme.Background,Padding=new Padding(14)};var add=Theme.Button("+ Phân công");add.Dock=DockStyle.Top;add.Click+=async(_,_)=>await AssignResourceAsync();p.Controls.Add(_resources);p.Controls.Add(add);return p;}
    private TabPage BuildPayments(){var p=new TabPage("Lịch sử tài chính"){BackColor=Theme.Background,Padding=new Padding(14)};p.Controls.Add(_payments);return p;}
    private async Task ReloadAsync(){Bind(_services,await _app.Repository.GetBookingChildrenAsync(_booking.Id,"DichVu"));Bind(_resources,await _app.Repository.GetBookingChildrenAsync(_booking.Id,"TaiNguyen"));var thu=await _app.Repository.GetBookingChildrenAsync(_booking.Id,"ThanhToan");var hoan=await _app.Repository.GetBookingChildrenAsync(_booking.Id,"HoanTien");Bind(_payments,thu.Concat(hoan).ToList());}
    private static void Bind(DataGridView grid,IReadOnlyList<IDictionary<string,object?>> rows){var dt=new System.Data.DataTable();if(rows.Count>0){foreach(var k in rows.SelectMany(x=>x.Keys).Distinct())dt.Columns.Add(k,typeof(object));foreach(var r in rows){var x=dt.NewRow();foreach(var a in r)x[a.Key]=a.Value??DBNull.Value;dt.Rows.Add(x);}}grid.DataSource=dt;if(grid.Columns.Contains("Id"))grid.Columns["Id"].Visible=false;}
    private async Task AddServiceAsync(){var list=await _app.Repository.GetLookupsAsync("DichVu");using var d=new SelectQuantityDialog("Thêm dịch vụ",list,false);if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Repository.AddBookingServiceAsync(_booking.Id,(int)d.SelectedId,d.Quantity,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await ReloadAsync();}
    private async Task AssignResourceAsync(){var list=await _app.Repository.GetLookupsAsync("TaiNguyen");using var d=new SelectQuantityDialog("Phân công tài nguyên",list,true);if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Repository.AssignResourceAsync(_booking.Id,(int)d.SelectedId,(int)d.Quantity,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await ReloadAsync();}
    private async Task DiscountAsync(){using var d=new DiscountDialog(_booking.TienGiam);if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Repository.SetDiscountAsync(_booking.Id,d.Amount,d.Reason,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await ReloadAsync();}}
}

internal sealed class SelectQuantityDialog : Form
{
    private readonly ComboBox _item=new(){DropDownStyle=ComboBoxStyle.DropDownList};private readonly NumericUpDown _quantity=new(){Minimum=1,Maximum=100,DecimalPlaces=0};public long SelectedId=>(long)(_item.SelectedValue??0L);public decimal Quantity=>_quantity.Value;
    public SelectQuantityDialog(string title,IReadOnlyList<LookupItem> list,bool integer){Text=title;StartPosition=FormStartPosition.CenterParent;ClientSize=new(440,270);BackColor=Theme.Background;Padding=new Padding(28);_quantity.DecimalPlaces=integer?0:2;_item.DisplayMember="Display";_item.ValueMember="Id";_item.DataSource=list.Select(x=>new Option(x.Id,$"{x.Code} • {x.Name} ({x.Extra})")).ToList();var body=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};body.Controls.Add(Ui.Field("LỰA CHỌN",_item,350));body.Controls.Add(Ui.Field("SỐ LƯỢNG",_quantity,350));var save=Theme.Button("Xác nhận");save.Width=350;save.Click+=(_,_)=>{DialogResult=DialogResult.OK;Close();};body.Controls.Add(save);Controls.Add(body);}private sealed record Option(long Id,string Display);
}

internal sealed class DiscountDialog : Form
{
    private readonly NumericUpDown _amount=new(){Maximum=10_000_000_000,ThousandsSeparator=true};private readonly TextBox _reason=new();public decimal Amount=>_amount.Value;public string? Reason=>string.IsNullOrWhiteSpace(_reason.Text)?null:_reason.Text.Trim();
    public DiscountDialog(decimal current){Text="Cập nhật giảm giá";StartPosition=FormStartPosition.CenterParent;ClientSize=new(440,280);BackColor=Theme.Background;Padding=new Padding(28);_amount.Value=current;var body=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};body.Controls.Add(Ui.Field("SỐ TIỀN GIẢM",_amount,350));body.Controls.Add(Ui.Field("LÝ DO (bắt buộc khi > 0)",_reason,350));var save=Theme.Button("Lưu giảm giá");save.Width=350;save.Click+=(_,_)=>{if(Amount>0&&Reason is null){Ui.Error(this,"Vui lòng nhập lý do giảm giá.");return;}DialogResult=DialogResult.OK;Close();};body.Controls.Add(save);Controls.Add(body);}
}

internal sealed class BookingDialog : Form
{
    private readonly AppFacade _app;private readonly LichChup? _existing;private readonly ComboBox _customer=new(){DropDownStyle=ComboBoxStyle.DropDownList},_package=new(){DropDownStyle=ComboBoxStyle.DropDownList},_photographer=new(){DropDownStyle=ComboBoxStyle.DropDownList},_room=new(){DropDownStyle=ComboBoxStyle.DropDownList};private readonly DateTimePicker _start=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy HH:mm",ShowUpDown=true},_end=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy HH:mm",ShowUpDown=true};private readonly TextBox _note=new();public BookingInput? Input{get;private set;}
    public BookingDialog(AppFacade app,LichChup? existing=null)
    {
        _app=app;_existing=existing;Text=existing is null?"Tạo lịch chụp mới":"Cập nhật lịch chụp";StartPosition=FormStartPosition.CenterParent;ClientSize=new(650,520);BackColor=Theme.Background;Font=Theme.Font();FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;var body=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28),AutoScroll=true};body.Controls.AddRange([Ui.Field("KHÁCH HÀNG *",_customer),Ui.Field("GÓI CHỤP *",_package),Ui.Field("BẮT ĐẦU *",_start),Ui.Field("KẾT THÚC *",_end),Ui.Field("NHIẾP ẢNH GIA *",_photographer),Ui.Field("PHÒNG CHỤP *",_room),Ui.Field("GHI CHÚ",_note,540)]);var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=68,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12)};var save=Theme.Button(existing is null?"Tạo lịch":"Lưu thay đổi");save.Click+=(_,_)=>Save();var cancel=Theme.Button("Hủy",Color.FromArgb(100,116,139));cancel.Click+=(_,_)=>Close();footer.Controls.Add(save);footer.Controls.Add(cancel);Controls.Add(body);Controls.Add(footer);_start.Value=existing?.BatDau??DateTime.Today.AddDays(1).AddHours(8);_end.Value=existing?.KetThuc??_start.Value.AddHours(2);_note.Text=existing?.GhiChu??"";Load+=async(_,_)=>await LoadLookupsAsync();
    }
    private async Task LoadLookupsAsync(){try{Bind(_customer,await _app.Repository.GetLookupsAsync("KhachHang"));Bind(_package,await _app.Repository.GetLookupsAsync("GoiChup",_existing is null));Bind(_photographer,await _app.Repository.GetLookupsAsync("NhiepAnhGia",_existing is null));Bind(_room,await _app.Repository.GetLookupsAsync("PhongChup",_existing is null));if(_existing is not null){_customer.SelectedValue=_existing.KhachHangId;_package.SelectedValue=(long)_existing.GoiChupId;_photographer.SelectedValue=(long)_existing.NhiepAnhGiaId;_room.SelectedValue=(long)_existing.PhongChupId;}}catch(Exception ex){Ui.Error(this,ex.Message);}}
    private static void Bind(ComboBox c,IReadOnlyList<LookupItem> list){c.DisplayMember="Display";c.ValueMember="Id";c.DataSource=list.Select(x=>new LookupView(x.Id,$"{x.Code} • {x.Name}")).ToList();}
    private void Save(){if(_customer.SelectedItem is not LookupView k||_package.SelectedItem is not LookupView g||_photographer.SelectedItem is not LookupView n||_room.SelectedItem is not LookupView p){Ui.Error(this,"Vui lòng chọn đầy đủ thông tin.");return;}Input=new(k.Id,(int)g.Id,_start.Value,_end.Value,(int)n.Id,(int)p.Id,string.IsNullOrWhiteSpace(_note.Text)?null:_note.Text.Trim());DialogResult=DialogResult.OK;Close();}
    private sealed record LookupView(long Id,string Display);
}

internal sealed class TextPrompt : Form
{
    private readonly TextBox _text=new(){Multiline=true,ScrollBars=ScrollBars.Vertical};public string Value=>_text.Text.Trim();
    public TextPrompt(string title,string label){Text=title;StartPosition=FormStartPosition.CenterParent;ClientSize=new(470,240);BackColor=Theme.Background;Padding=new Padding(24);Controls.Add(_text);_text.Dock=DockStyle.Fill;Controls.Add(new Label{Text=label,Dock=DockStyle.Top,Height=34,Font=Theme.Font(10,FontStyle.Bold)});var ok=Theme.Button("Xác nhận");ok.Dock=DockStyle.Bottom;ok.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(Value)){Ui.Error(this,"Không được để trống lý do.");return;}DialogResult=DialogResult.OK;Close();};Controls.Add(ok);}
}

internal sealed class MoneyDialog : Form
{
    private readonly NumericUpDown _amount=new(){DecimalPlaces=0,ThousandsSeparator=true,Maximum=10_000_000_000};private readonly TextBox _note=new();private readonly ComboBox _type=new(){DropDownStyle=ComboBoxStyle.DropDownList};public decimal Amount=>_amount.Value;public string Note=>_note.Text.Trim();public string PaymentType=>_type.SelectedItem?.ToString()??"BO_SUNG";
    public MoneyDialog(string title,decimal max,bool refund){Text=title;StartPosition=FormStartPosition.CenterParent;ClientSize=new(460,340);BackColor=Theme.Background;Padding=new Padding(28);Font=Theme.Font();_amount.Maximum=Math.Max(1,max);_amount.Value=Math.Max(1,max);_type.Items.AddRange(["DAT_COC","BO_SUNG","CON_LAI"]);_type.SelectedIndex=refund?1:0;var body=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};body.Controls.Add(new Label{Text=$"Tối đa: {max:N0} đ",Height=28,Width=380,ForeColor=Theme.Muted});body.Controls.Add(Ui.Field("SỐ TIỀN *",_amount,380));if(!refund)body.Controls.Add(Ui.Field("LOẠI THU",_type,380));body.Controls.Add(Ui.Field(refund?"LÝ DO HOÀN *":"GHI CHÚ",_note,380));var save=Theme.Button("Xác nhận");save.Width=380;save.Click+=(_,_)=>{if(refund&&string.IsNullOrWhiteSpace(Note)){Ui.Error(this,"Vui lòng nhập lý do hoàn tiền.");return;}DialogResult=DialogResult.OK;Close();};body.Controls.Add(save);Controls.Add(body);}
}

internal sealed class ReceiptPreviewForm : Form
{
    private readonly RichTextBox _preview=new(){Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Color.White,Font=Theme.Font(11)};private readonly System.Drawing.Printing.PrintDocument _document=new();
    public ReceiptPreviewForm(LichChup booking,decimal amount,bool refund,string employee,string note)
    {
        Text=refund?"Phiếu hoàn tiền":"Biên nhận thanh toán";StartPosition=FormStartPosition.CenterParent;ClientSize=new(650,680);BackColor=Theme.Background;Padding=new Padding(24);var type=refund?"PHIẾU HOÀN TIỀN":"BIÊN NHẬN THANH TOÁN";_preview.Text=$"STUDIO MANAGER\n{type}\n\nMã lịch: {booking.Ma}\nKhách hàng: {booking.KhachHang}\nGói chụp: {booking.TenGoiChot}\nNgày giao dịch: {DateTime.Now:dd/MM/yyyy HH:mm}\n\nSỐ TIỀN: {amount:N0} VNĐ\n{(refund?"Lý do":"Ghi chú")}: {note}\n\nNhân viên thực hiện: {employee}\n\nCảm ơn quý khách!";var print=Theme.Button("In phiếu");print.Dock=DockStyle.Bottom;print.Click+=(_,_)=>{using var p=new PrintPreviewDialog{Document=_document,Width=900,Height=700};p.ShowDialog(this);};_document.PrintPage+=(_,e)=>{if(e.Graphics is { } graphics)graphics.DrawString(_preview.Text,Theme.Font(12),Brushes.Black,new RectangleF(60,60,e.MarginBounds.Width,e.MarginBounds.Height));};Controls.Add(_preview);Controls.Add(print);
    }
}
