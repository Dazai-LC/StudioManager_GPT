using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class BookingsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid=Theme.Grid();
    private readonly TextBox _search=Ui.SearchBox("Mã lịch, khách hàng, số điện thoại...");
    private readonly DateTimePicker _from=new(){Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false},_to=new(){Format=DateTimePickerFormat.Short,ShowCheckBox=true,Checked=false};
    private readonly ComboBox _photographer=FilterBox(),_room=FilterBox(),_package=FilterBox(),_status=FilterBox();
    private readonly Button _edit,_detail,_progress,_pay,_refund,_cancel;
    private IReadOnlyList<LichChup> _items=[];

    public BookingsPage(AppFacade app)
    {
        _app=app;BackColor=Theme.Background;
        var header=new PageHeader("Lịch chụp","Theo dõi toàn bộ lịch, tiến độ và tài chính.");
        var add=Theme.Button("+ Tạo lịch");add.Click+=async(_,_)=>await CreateAsync();header.Actions.Controls.Add(add);

        var filters=Toolbar();
        filters.Controls.Add(Ui.Field("Tìm kiếm",_search,280));
        filters.Controls.Add(Ui.Field("Từ ngày",_from,150));
        filters.Controls.Add(Ui.Field("Đến ngày",_to,150));
        filters.Controls.Add(Ui.Field("Nhiếp ảnh gia",_photographer,190));
        filters.Controls.Add(Ui.Field("Phòng",_room,160));
        filters.Controls.Add(Ui.Field("Gói chụp",_package,180));
        filters.Controls.Add(Ui.Field("Trạng thái",_status,170));
        var find=Theme.Button("Tìm kiếm");find.Click+=async(_,_)=>await LoadAsync();
        var refresh=Theme.Button("Làm mới",Color.FromArgb(71,85,105));refresh.Click+=async(_,_)=>{_search.Clear();_from.Checked=false;_to.Checked=false;SelectAll(_photographer);SelectAll(_room);SelectAll(_package);SelectAll(_status);await LoadAsync();};
        filters.Controls.Add(Ui.ToolbarButtonGroup(find,refresh));

        var actions=Toolbar();
        actions.Controls.Add(Ui.ToolbarCaption("THAO TÁC LỊCH ĐÃ CHỌN"));
        _detail=Theme.Button("Chi tiết",Color.FromArgb(71,85,105));_detail.Click+=async(_,_)=>await DetailAsync();actions.Controls.Add(Ui.ToolbarButton(_detail));
        _edit=Theme.Button("Sửa lịch",Color.FromArgb(71,85,105));_edit.Click+=async(_,_)=>await EditAsync();actions.Controls.Add(Ui.ToolbarButton(_edit));
        _progress=Theme.Button("Bước tiếp");_progress.Click+=async(_,_)=>await AdvanceAsync();actions.Controls.Add(Ui.ToolbarButton(_progress));
        _pay=Theme.Button("Thu tiền");_pay.Click+=async(_,_)=>await PaymentAsync(false);actions.Controls.Add(Ui.ToolbarButton(_pay));
        _refund=Theme.Button("Hoàn tiền");_refund.Visible=_app.Session!.VaiTro==VaiTro.QuanTriVien;_refund.Click+=async(_,_)=>await PaymentAsync(true);actions.Controls.Add(Ui.ToolbarButton(_refund));
        _cancel=Theme.Button("Hủy lịch",Theme.Danger);_cancel.Click+=async(_,_)=>await CancelAsync();actions.Controls.Add(Ui.ToolbarButton(_cancel));
        var export=Theme.Button("Xuất CSV",Color.FromArgb(71,85,105));export.Click+=(_,_)=>Ui.ExportGrid(_grid,this,"lich-chup");actions.Controls.Add(Ui.ToolbarButton(export));

        var card=new CardPanel{Dock=DockStyle.Fill};card.Controls.Add(_grid);
        Controls.Add(card);Controls.Add(actions);Controls.Add(filters);Controls.Add(header);
        _grid.CellDoubleClick+=async(_,_)=>await DetailAsync();
        _grid.SelectionChanged+=(_,_)=>UpdateActionState();
        Load+=async(_,_)=>{await LoadFiltersAsync();await LoadAsync();};
    }
    private LichChup? Selected()=>_grid.CurrentRow?.Cells["Id"].Value is object v?_items.FirstOrDefault(x=>x.Id==Convert.ToInt64(v)):null;
    private async Task LoadAsync(){try{var filter=new BookingSearchFilter(string.IsNullOrWhiteSpace(_search.Text)?null:_search.Text.Trim(),_from.Checked?_from.Value.Date:null,_to.Checked?_to.Value.Date:null,SelectedId(_photographer),SelectedId(_room),SelectedId(_package),SelectedStatus());_items=await _app.Bookings.SearchAsync(filter);_grid.DataSource=_items.Select(x=>new{x.Id,Ma=x.Ma,Khách_hàng=x.KhachHang,Bắt_đầu=x.BatDau.ToString("dd/MM/yyyy HH:mm"),Kết_thúc=x.KetThuc.ToString("dd/MM/yyyy HH:mm"),Nhiếp_ảnh_gia=x.NhiepAnhGia,Phòng=x.Phong,Trạng_thái=x.TrangThai.HienThi(),Tổng_tiền=x.TongThanhToan,Còn_lại=x.ConLai}).ToList();if(_grid.Columns.Contains("Id"))_grid.Columns["Id"].Visible=false;foreach(var c in new[]{"Tổng_tiền","Còn_lại"})if(_grid.Columns.Contains(c))_grid.Columns[c].DefaultCellStyle.Format="N0";_grid.ClearSelection();_grid.CurrentCell=null;UpdateActionState();}catch(Exception ex){Ui.Error(this,ex.Message);}}
    private static FlowLayoutPanel Toolbar()=>new(){Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(0,6,0,6),WrapContents=true};
    private void UpdateActionState()
    {
        var booking=Selected();
        var hasSelection=booking is not null;
        _detail.Enabled=hasSelection;
        _edit.Enabled=booking is not null && booking.TrangThai is not TrangThaiLich.HoanThanh and not TrangThaiLich.DaHuy;
        _progress.Enabled=booking is not null && booking.TrangThai.BuocTiepTheo() is not null;
        _pay.Enabled=booking is not null && booking.TrangThai!=TrangThaiLich.DaHuy && booking.ConLai>0;
        _refund.Enabled=_refund.Visible && booking is not null && booking.TrangThai==TrangThaiLich.DaHuy && booking.DaThu-booking.DaHoan>0;
        _cancel.Enabled=booking is not null && BusinessRules.DuocHuy(booking.TrangThai,_app.Session!.VaiTro,booking.BatDau,DateTime.Now);
    }
    private async Task LoadFiltersAsync(){BindFilter(_photographer,await _app.BookingSupport.GetLookupsAsync("NhiepAnhGia",false));BindFilter(_room,await _app.BookingSupport.GetLookupsAsync("PhongChup",false));BindFilter(_package,await _app.BookingSupport.GetLookupsAsync("GoiChup",false));_status.DataSource=new[]{new FilterOption(0,"","Tất cả trạng thái"),new FilterOption(0,"DA_DAT_LICH","Đã đặt lịch"),new FilterOption(0,"DA_CHUP","Đã chụp"),new FilterOption(0,"DANG_CHINH_SUA","Đang chỉnh sửa ảnh"),new FilterOption(0,"CHO_GIAO_ANH","Chờ giao ảnh"),new FilterOption(0,"DA_GIAO_ANH","Đã giao ảnh"),new FilterOption(0,"HOAN_THANH","Hoàn thành"),new FilterOption(0,"DA_HUY","Đã hủy")};_status.DisplayMember="Name";}
    private static ComboBox FilterBox()=>new(){DropDownStyle=ComboBoxStyle.DropDownList};private static void BindFilter(ComboBox box,IReadOnlyList<LookupItem> list){box.DataSource=(new[]{new FilterOption(0,"","Tất cả")}).Concat(list.Select(x=>new FilterOption((int)x.Id,"",$"{x.Code} • {x.Name}"))).ToList();box.DisplayMember="Name";}private static void SelectAll(ComboBox box){if(box.Items.Count>0)box.SelectedIndex=0;}private static int? SelectedId(ComboBox box)=>box.SelectedItem is FilterOption option&&option.Id>0?option.Id:null;private string? SelectedStatus()=>_status.SelectedItem is FilterOption option&&!string.IsNullOrWhiteSpace(option.Code)?option.Code:null;private sealed record FilterOption(int Id,string Code,string Name);
    private async Task CreateAsync(){using var f=new BookingDialog(_app);if(f.ShowDialog(this)!=DialogResult.OK||f.Input is null)return;var r=await _app.Bookings.CreateAsync(f.Input,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task EditAsync(){var b=Selected();if(b is null)return;if(b.TrangThai is TrangThaiLich.HoanThanh or TrangThaiLich.DaHuy){Ui.Error(this,"Không thể sửa lịch đã hoàn thành hoặc đã hủy.");return;}string? reason=null;if(b.TrangThai!=TrangThaiLich.DaDatLich){if(_app.Session!.VaiTro!=VaiTro.QuanTriVien){Ui.Error(this,"Chỉ được sửa thông tin cốt lõi khi lịch đang Đã đặt lịch.");return;}using var reasonDialog=new TextPrompt("Lý do hiệu chỉnh","Nhập lý do hiệu chỉnh ngoại lệ:");if(reasonDialog.ShowDialog(this)!=DialogResult.OK)return;reason=reasonDialog.Value;}using var f=new BookingDialog(_app,b);if(f.ShowDialog(this)!=DialogResult.OK||f.Input is null)return;var r=await _app.Bookings.UpdateAsync(b.Id,f.Input,reason,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task DetailAsync(){var b=Selected();if(b is null)return;using var d=new BookingDetailDialog(_app,b);d.ShowDialog(this);await LoadAsync();}
    private async Task AdvanceAsync(){var b=Selected();if(b is null)return;var next=b.TrangThai.BuocTiepTheo();if(next is null){Ui.Error(this,"Lịch đã ở trạng thái kết thúc.");return;}if(MessageBox.Show($"Chuyển sang “{next.Value.HienThi()}”?","Cập nhật tiến độ",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;var r=await _app.Bookings.AdvanceAsync(b.Id,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await LoadAsync();}
    private async Task CancelAsync(){var b=Selected();if(b is null)return;using var d=new TextPrompt("Hủy lịch chụp","Nhập lý do hủy:");if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.Bookings.CancelAsync(b.Id,d.Value,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await LoadAsync();}}
    private async Task PaymentAsync(bool refund){var b=Selected();if(b is null)return;var maximum=refund?Math.Max(0,b.DaThu-b.DaHoan):b.ConLai;if(maximum<=0){Ui.Error(this,refund?"Không còn số tiền có thể hoàn.":"Lịch này đã thanh toán đủ, không thể ghi nhận thu thêm.");return;}using var d=new MoneyDialog(refund?"Hoàn tiền":"Ghi nhận thanh toán",maximum,refund);if(d.ShowDialog(this)!=DialogResult.OK)return;Result r=refund?await _app.Finance.RefundAsync(new(b.Id,d.Amount,d.Note,d.Note),_app.Session!):await _app.Finance.ReceiveAsync(new(b.Id,d.PaymentType,d.Amount,d.Note),_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{var updated=await _app.Bookings.GetAsync(b.Id)??b;await LoadAsync();using var receipt=new ReceiptPreviewForm(updated,d.Amount,refund,_app.Session!.HoTen,d.Note);receipt.ShowDialog(this);}}
}

internal sealed class BookingDetailDialog : Form
{
    private readonly AppFacade _app;private LichChup _booking;private readonly DataGridView _services=Theme.Grid(),_resources=Theme.Grid(),_payments=Theme.Grid();
    private readonly TableLayoutPanel _summary=new(){Dock=DockStyle.Fill,ColumnCount=4,RowCount=5,CellBorderStyle=TableLayoutPanelCellBorderStyle.Single,BackColor=Theme.Border};
    private readonly Label _customer=SummaryValue(),_package=SummaryValue(),_start=SummaryValue(),_end=SummaryValue(),_photographer=SummaryValue(),_room=SummaryValue(),_subtotal=SummaryValue(),_discount=SummaryValue(),_total=SummaryValue(),_received=SummaryValue();
    private readonly ToolTip _summaryTip=new();
    public BookingDetailDialog(AppFacade app,LichChup booking)
    {
        _app=app;_booking=booking;Text=$"Chi tiết {booking.Ma}";StartPosition=FormStartPosition.CenterParent;ClientSize=new(960,710);MinimumSize=new(880,660);BackColor=Theme.Background;Font=Theme.Font();BuildSummaryTable();var summaryCard=new CardPanel{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,10),Padding=new Padding(14)};summaryCard.Controls.Add(_summary);var tabs=new TabControl{Dock=DockStyle.Fill,Font=Theme.Font(10)};tabs.TabPages.Add(BuildServices());tabs.TabPages.Add(BuildResources());tabs.TabPages.Add(BuildPayments());var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(14)};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,168));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.Controls.Add(summaryCard,0,0);layout.Controls.Add(tabs,0,1);Controls.Add(layout);UpdateSummary();FormClosed+=(_,_)=>_summaryTip.Dispose();Load+=async(_,_)=>await ReloadAsync();
    }
    private static Label SummaryLabel(string text)=>new(){Text=text,Dock=DockStyle.Fill,BackColor=Theme.PrimaryLight,ForeColor=Theme.Muted,Font=Theme.Font(8.5f,FontStyle.Bold),Padding=new Padding(8,0,4,0),TextAlign=ContentAlignment.MiddleLeft};
    private static Label SummaryValue()=>new(){Dock=DockStyle.Fill,BackColor=Theme.Surface,ForeColor=Theme.Text,Font=Theme.Font(9.5f),Padding=new Padding(8,0,4,0),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    private void BuildSummaryTable()
    {
        _summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,116));_summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));_summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));_summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        for(var row=0;row<5;row++)_summary.RowStyles.Add(new RowStyle(SizeType.Percent,20));
        AddSummaryRow(0,"KHÁCH HÀNG",_customer,"GÓI CHỤP",_package);
        AddSummaryRow(1,"BẮT ĐẦU",_start,"KẾT THÚC",_end);
        AddSummaryRow(2,"NHIẾP ẢNH GIA",_photographer,"PHÒNG",_room);
        AddSummaryRow(3,"TẠM TÍNH",_subtotal,"GIẢM GIÁ",_discount);
        AddSummaryRow(4,"TỔNG THANH TOÁN",_total,"ĐÃ THU / CÒN LẠI",_received);
    }
    private void AddSummaryRow(int row,string leftLabel,Label leftValue,string rightLabel,Label rightValue){_summary.Controls.Add(SummaryLabel(leftLabel),0,row);_summary.Controls.Add(leftValue,1,row);_summary.Controls.Add(SummaryLabel(rightLabel),2,row);_summary.Controls.Add(rightValue,3,row);}
    private TabPage BuildServices(){var p=new TabPage("Dịch vụ & giảm giá"){BackColor=Theme.Background,Padding=new Padding(14)};var bar=DetailToolbar();var add=Theme.Button("+ Thêm dịch vụ");add.Click+=async(_,_)=>await AddServiceAsync();var remove=Theme.Button("Xóa dịch vụ",Theme.Danger);remove.Click+=async(_,_)=>await RemoveServiceAsync();var discount=Theme.Button("Nhập giảm giá");discount.Click+=async(_,_)=>await DiscountAsync();bar.Controls.Add(Ui.ToolbarButton(add));bar.Controls.Add(Ui.ToolbarButton(remove));bar.Controls.Add(Ui.ToolbarButton(discount));p.Controls.Add(_services);p.Controls.Add(bar);return p;}
    private TabPage BuildResources(){var p=new TabPage("Tài nguyên"){BackColor=Theme.Background,Padding=new Padding(14)};var bar=DetailToolbar();var add=Theme.Button("+ Phân công");add.Click+=async(_,_)=>await AssignResourceAsync();var returned=Theme.Button("Đánh dấu đã trả");returned.Click+=async(_,_)=>await UpdateResourceAsync(TrangThaiPhanCong.DaTra);var cancel=Theme.Button("Hủy phân công",Theme.Danger);cancel.Click+=async(_,_)=>await UpdateResourceAsync(TrangThaiPhanCong.DaHuy);bar.Controls.Add(Ui.ToolbarButton(add));bar.Controls.Add(Ui.ToolbarButton(returned));bar.Controls.Add(Ui.ToolbarButton(cancel));p.Controls.Add(_resources);p.Controls.Add(bar);return p;}
    private TabPage BuildPayments(){var p=new TabPage("Lịch sử tài chính"){BackColor=Theme.Background,Padding=new Padding(14)};p.Controls.Add(_payments);return p;}
    private static FlowLayoutPanel DetailToolbar()=>new(){Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Padding=new Padding(0,0,0,8)};
    private async Task ReloadAsync(){_booking=await _app.Bookings.GetAsync(_booking.Id)??_booking;UpdateSummary();Bind(_services,await _app.BookingSupport.GetChildrenAsync(_booking.Id,"DichVu"));Bind(_resources,await _app.BookingSupport.GetChildrenAsync(_booking.Id,"TaiNguyen"));var thu=await _app.BookingSupport.GetChildrenAsync(_booking.Id,"ThanhToan");var hoan=await _app.BookingSupport.GetChildrenAsync(_booking.Id,"HoanTien");Bind(_payments,thu.Concat(hoan).ToList());}
    private static void Bind(DataGridView grid,IReadOnlyList<IDictionary<string,object?>> rows){var dt=new System.Data.DataTable();if(rows.Count>0){foreach(var k in rows.SelectMany(x=>x.Keys).Distinct())dt.Columns.Add(k,typeof(object));foreach(var r in rows){var x=dt.NewRow();foreach(var a in r)x[a.Key]=a.Value??DBNull.Value;dt.Rows.Add(x);}}grid.DataSource=dt;if(grid.Columns.Contains("Id"))grid.Columns["Id"].Visible=false;}
    private async Task AddServiceAsync(){var list=await _app.BookingSupport.GetLookupsAsync("DichVu");using var d=new SelectQuantityDialog("Thêm dịch vụ",list,false);if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.BookingSupport.AddServiceAsync(_booking.Id,(int)d.SelectedId,d.Quantity,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await ReloadAsync();}
    private async Task RemoveServiceAsync(){var id=SelectedChildId(_services);if(id is null)return;if(MessageBox.Show("Xóa dịch vụ đã chọn? Hệ thống sẽ từ chối nếu làm tổng phải thu thấp hơn số đã thu.","Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;var r=await _app.BookingSupport.RemoveServiceAsync(_booking.Id,id.Value,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await ReloadAsync();}
    private async Task AssignResourceAsync(){var list=await _app.BookingSupport.GetLookupsAsync("TaiNguyen");using var d=new SelectQuantityDialog("Phân công tài nguyên",list,true);if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.BookingSupport.AssignResourceAsync(_booking.Id,(int)d.SelectedId,(int)d.Quantity,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await ReloadAsync();}
    private async Task DiscountAsync(){using var d=new DiscountDialog(_booking.TienGiam);if(d.ShowDialog(this)!=DialogResult.OK)return;var r=await _app.BookingSupport.SetDiscountAsync(_booking.Id,d.Amount,d.Reason,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else{Ui.Info(this,r.Message);await ReloadAsync();}}
    private async Task UpdateResourceAsync(TrangThaiPhanCong next){var id=SelectedChildId(_resources);if(id is null)return;var action=next==TrangThaiPhanCong.DaTra?"đánh dấu tài nguyên đã trả":"hủy phân công tài nguyên";if(MessageBox.Show($"Xác nhận {action}?","Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;var r=await _app.BookingSupport.UpdateResourceAssignmentAsync(id.Value,next,_app.Session!);if(!r.Success)Ui.Error(this,r.Message);else await ReloadAsync();}
    private static long? SelectedChildId(DataGridView grid)=>grid.CurrentRow?.Cells["Id"].Value is object value?Convert.ToInt64(value):null;
    private void UpdateSummary()
    {
        SetSummary(_customer,_booking.KhachHang);SetSummary(_package,_booking.TenGoiChot);
        SetSummary(_start,_booking.BatDau.ToString("dd/MM/yyyy HH:mm"));SetSummary(_end,_booking.KetThuc.ToString("dd/MM/yyyy HH:mm"));
        SetSummary(_photographer,_booking.NhiepAnhGia);SetSummary(_room,_booking.Phong);
        SetSummary(_subtotal,$"{_booking.TamTinh:N0} đ");SetSummary(_discount,$"{_booking.TienGiam:N0} đ");
        SetSummary(_total,$"{_booking.TongThanhToan:N0} đ");SetSummary(_received,$"{_booking.DaThu:N0} đ / {_booking.ConLai:N0} đ");
    }
    private void SetSummary(Label label,string value){label.Text=value;_summaryTip.SetToolTip(label,value);}
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
    private async Task LoadLookupsAsync(){try{Bind(_customer,await _app.BookingSupport.GetLookupsAsync("KhachHang"));Bind(_package,await _app.BookingSupport.GetLookupsAsync("GoiChup",_existing is null));Bind(_photographer,await _app.BookingSupport.GetLookupsAsync("NhiepAnhGia",_existing is null));Bind(_room,await _app.BookingSupport.GetLookupsAsync("PhongChup",_existing is null));if(_existing is not null){_customer.SelectedValue=_existing.KhachHangId;_package.SelectedValue=(long)_existing.GoiChupId;_photographer.SelectedValue=(long)_existing.NhiepAnhGiaId;_room.SelectedValue=(long)_existing.PhongChupId;}}catch(Exception ex){Ui.Error(this,ex.Message);}}
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
        Text=refund?"Phiếu hoàn tiền":"Biên nhận thanh toán";StartPosition=FormStartPosition.CenterParent;ClientSize=new(650,680);BackColor=Theme.Background;Padding=new Padding(24);var type=refund?"PHIẾU HOÀN TIỀN":"BIÊN NHẬN THANH TOÁN";_preview.Text=$"STUDIO MANAGER\n{type}\n\nMã lịch: {booking.Ma}\nKhách hàng: {booking.KhachHang}\nGói chụp: {booking.TenGoiChot}\nNgày giao dịch: {DateTime.Now:dd/MM/yyyy HH:mm}\n\nSỐ TIỀN LẦN NÀY: {amount:N0} VNĐ\nTổng thanh toán: {booking.TongThanhToan:N0} VNĐ\nTổng đã thu: {booking.DaThu:N0} VNĐ\nCòn phải thu: {booking.ConLai:N0} VNĐ\n{(refund?"Lý do":"Ghi chú")}: {note}\n\nNhân viên thực hiện: {employee}\n\nCảm ơn quý khách!";var print=Theme.Button("In phiếu");print.Dock=DockStyle.Bottom;print.Click+=(_,_)=>{using var p=new PrintPreviewDialog{Document=_document,Width=900,Height=700};p.ShowDialog(this);};_document.PrintPage+=(_,e)=>{if(e.Graphics is { } graphics)graphics.DrawString(_preview.Text,Theme.Font(12),Brushes.Black,new RectangleF(60,60,e.MarginBounds.Width,e.MarginBounds.Height));};Controls.Add(_preview);Controls.Add(print);
    }
}
