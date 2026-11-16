using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.CustomerDesktop;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class PaymentWorkspacePage : UserControl
{
    public PaymentWorkspacePage(AppFacade app)
    {
        Dock=DockStyle.Fill;
        BackColor=Theme.Background;Font=Theme.Font();
        Controls.Add(new BookingsPage(app){Dock=DockStyle.Fill});
        Controls.Add(new Label{Dock=DockStyle.Top,Height=46,Font=Theme.Font(9f,FontStyle.Bold),ForeColor=Theme.TextSecondary,
            BackColor=Theme.SurfaceWarm,Padding=new Padding(12,8,12,8),
            Text="THU TIỀN MÔ PHỎNG & BIÊN NHẬN · Chọn lịch đã xác nhận → Thu tiền.",AutoEllipsis=true});
    }
}

internal static class PaymentPresentation
{
    internal static PaymentReceiptDto Receipt(PaymentReceipt r)=>new(r.Id,r.Code,r.BookingCode,r.CustomerName,r.Phone,r.PackageName,r.PackagePrice,
        r.Services.Select(s=>new PaymentLineDto(s.Name,s.Unit,s.UnitPrice,s.Quantity)).ToArray(),r.Discount,r.BookingTotal,r.Amount,r.Method,
        r.Purpose,r.PaidAt,r.StaffName,r.BalanceAfter,r.HasSnapshot,r.Note);
}

internal sealed class PaymentWorkspaceForm : Form
{
    private readonly AppFacade _app;
    private readonly long _bookingId;
    private readonly Label _summary=new(){Text="Đang tải số dư và lịch sử giao dịch…"};
    private readonly DataGridView _history=Theme.Grid();
    private readonly FlowLayoutPanel _actions=PaymentUi.Actions();
    private readonly ModernButton _collect,_older,_receipt;
    private BookingPaymentSummary? _balance;
    private IReadOnlyList<PaymentTransaction> _rows=[];
    private bool _busy,_reloadRequired;
    public PaymentWorkspaceForm(AppFacade app,long bookingId)
    {
        _app=app;_bookingId=bookingId;Text="Thu tiền mô phỏng & biên nhận • StudioManager";ClientSize=new Size(1040,680);
        StartPosition=FormStartPosition.CenterParent;MinimumSize=new Size(800,560);
        var layout=PaymentUi.Layout(this,"Thu tiền & biên nhận","Số dư từ lịch đã xác nhận · Giao dịch mô phỏng do nhân viên xác nhận");
        _history.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells;
        ModernButton Add(string name,IconKind icon,Func<Task> work,bool primary=false)
        {var b=PaymentUi.Action(_actions,name,icon,primary);b.Click+=async(_,_)=>await Run(work);return b;}
        Add("Tải lại số dư / lịch sử",IconKind.Refresh,()=>LoadData(null));
        _collect=Add("Thu tiền mô phỏng",IconKind.Wallet,Collect,true);
        _receipt=Add("Xem / in biên nhận",IconKind.Printer,Receipt);_receipt.Enabled=false;
        _older=Add("Giao dịch cũ hơn",IconKind.History,()=>LoadData(_balance?.NextBeforeId));
        layout.Controls.Add(PaymentUi.Summary(_summary),0,1);layout.Controls.Add(PaymentUi.Data(_history),0,2);layout.Controls.Add(_actions,0,3);
        _history.CellDoubleClick+=async(_,_)=>await Run(Receipt);
        _history.SelectionChanged+=(_,_)=>_receipt.Enabled=!_busy && _history.CurrentRow is not null;
        Shown+=async(_,_)=>await Run(()=>LoadData(null));
    }
    private async Task Run(Func<Task> work)
    {
        if(_busy || IsDisposed)return;_busy=true;_actions.Enabled=false;_history.Enabled=false;
        try{await work();}
        catch
        {
            _reloadRequired=true;_summary.Text="Không tải được trạng thái. Tải lại trước khi thực hiện tiếp; không gửi lại khoản thu.";
            _balance=null;_rows=[];_history.DataSource=null;
        }
        finally
        {
            _busy=false;
            if(!IsDisposed){_actions.Enabled=true;_history.Enabled=true;_collect.Enabled=!_reloadRequired && _balance?.Remaining>0;_older.Enabled=_balance?.NextBeforeId is not null;_receipt.Enabled=_history.CurrentRow is not null;}
        }
    }
    private async Task LoadData(long? before)
    {
        var actor=_app.Session;
        var result=await _app.Finance.PaymentsAsync(_bookingId,before);
        if(!Current(actor))return;
        if(!result.Success){_balance=null;_rows=[];_history.DataSource=null;_summary.Text=result.Message;return;}
        _balance=result.Data!;_rows=_balance.Payments;_reloadRequired=false;
        _summary.Text=$"Lịch {_balance.BookingCode} · {ReceiptText.BookingStatus(_balance.Status)}\r\nTổng: {ReceiptText.Money(_balance.Total)} | Đã thu: {ReceiptText.Money(_balance.Collected)} | Đã hoàn: {ReceiptText.Money(_balance.Refunded)}\r\n"+
            $"Còn lại: {ReceiptText.Money(_balance.Remaining)}\r\nChỉ ghi nhận mô phỏng. Chuyển khoản DEMO do nhân viên xác nhận; không có xác minh ngân hàng.";
        _history.DataSource=_rows.Select(p=>new{p.Id,Ma=p.Code,MucDich=ReceiptText.Purpose(p.Purpose),SoTien=ReceiptText.Money(p.Amount),PhuongThuc=ReceiptText.Method(p.Method),ThoiDiem=p.PaidAt.ToString("dd/MM/yyyy HH:mm:ss")}).ToList();
        if(_history.Columns.Contains("Id"))_history.Columns["Id"].Visible=false;
        foreach(var (key,title) in new[]{("Ma","Mã giao dịch"),("MucDich","Mục đích thu"),("SoTien","Số tiền"),("PhuongThuc","Phương thức"),("ThoiDiem","Thời điểm thu")})
            if(_history.Columns.Contains(key))_history.Columns[key].HeaderText=title;
    }
    private async Task Collect()
    {
        if(_balance is null || _reloadRequired)return;
        // Explicit reload before presenting the amount; SQL validates again under the booking lock.
        await LoadData(null);if(_balance is null || _balance.Remaining<=0)return;
        using var dialog=new CollectionDialog(_balance);
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        var request=new PaymentInput(_bookingId,dialog.Purpose,dialog.Amount,dialog.Note)
        {PhuongThucThu=dialog.Method,YeuCauThuId=Guid.NewGuid()};
        var actor=_app.Session;if(actor is null){_balance=null;return;}
        var result=await _app.Finance.CollectAsync(request,actor);
        if(!Current(actor))return;
        if(!result.Success)
        {
            _reloadRequired=result.Code is "OUTCOME_UNKNOWN" or "RELOAD_REQUIRED";
            AtelierNotificationService.ShowWarning(this,result.Message,"Ghi nhận khoản thu");
            if(!_reloadRequired)await LoadData(null);return;
        }
        var saved=result.Data!;
        // Show the receipt returned for this committed identity, never the last row or dialog amount.
        using(var preview=new PersistedReceiptForm(PaymentPresentation.Receipt(saved)))preview.ShowDialog(this);
        await LoadData(null);
    }
    private async Task Receipt()
    {
        if(_history.CurrentRow is not {Index:>=0} selected || selected.Index>=_rows.Count)return;
        if(selected.Cells["Id"].Value is not long id)return;
        var actor=_app.Session;
        var result=await _app.Finance.ReceiptAsync(_bookingId,id);
        if(!Current(actor))return;
        if(!result.Success){Ui.Error(this,result.Message);return;}
        using var preview=new PersistedReceiptForm(PaymentPresentation.Receipt(result.Data!));preview.ShowDialog(this);
    }
    private bool Current(UserSession? actor)
    {
        if(IsDisposed)return false;
        if(_app.Session==actor)return true;
        _balance=null;_rows=[];_history.DataSource=null;_reloadRequired=true;
        _summary.Text="Phiên đăng nhập đã thay đổi. Tải lại dữ liệu trước khi thao tác.";return false;
    }
}

internal sealed class CollectionDialog : Form
{
    private readonly TextBox _amount=new(){Font=Theme.Font(10f),BackColor=Theme.SurfaceWarm,ForeColor=Theme.Text},
        _note=new(){Font=Theme.Font(),BackColor=Theme.SurfaceWarm,ForeColor=Theme.Text,MaxLength=500};
    private readonly ComboBox _method=new(){DropDownStyle=ComboBoxStyle.DropDownList,Font=Theme.Font(),BackColor=Theme.Surface,ForeColor=Theme.Text},
        _purpose=new(){DropDownStyle=ComboBoxStyle.DropDownList,Font=Theme.Font(),BackColor=Theme.Surface,ForeColor=Theme.Text};
    public decimal Amount {get;private set;}
    public string Method=>(string)_method.SelectedValue!;
    public string Purpose=>(string)_purpose.SelectedValue!;
    public string Note=>_note.Text.Trim();
    public CollectionDialog(BookingPaymentSummary summary)
    {
        Text="Xác nhận khoản thu mô phỏng — "+summary.BookingCode;StartPosition=FormStartPosition.CenterParent;
        ClientSize=new Size(600,700);MinimumSize=new Size(560,620);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
        var layout=PaymentUi.Layout(this,"Xác nhận khoản thu","Chọn phương thức, mục đích thu và kiểm tra số tiền trước khi ghi nhận");
        layout.Controls.Add(PaymentUi.Summary(new Label{Text=$"Lịch {summary.BookingCode}\r\nTổng {ReceiptText.Money(summary.Total)} · Đã thu {ReceiptText.Money(summary.Collected)}\r\nCòn lại {ReceiptText.Money(summary.Remaining)}"}),0,1);
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.Transparent,Margin=Padding.Empty};
        var card=new CardPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(20,14,20,14)};
        var body=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty};
        card.Controls.Add(body);scroll.Controls.Add(card);layout.Controls.Add(scroll,0,2);
        _method.BindingContext=new BindingContext();_purpose.BindingContext=new BindingContext();
        _method.DisplayMember="Text";_method.ValueMember="Code";_method.DataSource=new[]{new Choice(PaymentMethods.Cash,"Tiền mặt — mô phỏng"),new Choice(PaymentMethods.BankTransferDemo,"Chuyển khoản ngân hàng — DEMO")};
        _purpose.DisplayMember="Text";_purpose.ValueMember="Code";_purpose.DataSource=new[]{new Choice("DAT_COC","Đặt cọc"),new Choice("BO_SUNG","Thu bổ sung"),new Choice("CON_LAI","Thu phần còn lại")};
        var fields=new[]{Ui.Field("PHƯƠNG THỨC THU",_method,480),Ui.Field("MỤC ĐÍCH THU",_purpose,480),
            Ui.Field("SỐ TIỀN (KHÔNG TÁCH HÀNG NGHÌN, TỐI ĐA 2 SỐ LẺ)",_amount,480),Ui.Field("GHI CHÚ",_note,480)};
        foreach(var field in fields){field.Margin=new Padding(0,0,0,8);body.Controls.Add(field);}
        _amount.Text=summary.Remaining.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
        var instructions=new Label{Text="DEMO: không chuyển tiền thật. Với chuyển khoản, nhân viên xác nhận thủ công; hướng dẫn này không chứng minh đã nhận tiền.",
            AutoSize=true,Font=Theme.Font(9f),ForeColor=Theme.Muted,Margin=new Padding(0,8,0,8),UseMnemonic=false};body.Controls.Add(instructions);
        var sizing=false;
        void SizeFields()
        {
            if(sizing)return;sizing=true;
            try
            {
                var width=Math.Max(240,card.ClientSize.Width-card.Padding.Horizontal);
                body.MinimumSize=new Size(width,0);body.MaximumSize=new Size(width,0);
                foreach(var field in fields)field.Width=width;
                instructions.MaximumSize=new Size(width,0);
            }
            finally{sizing=false;}
        }
        card.SizeChanged+=(_,_)=>SizeFields();SizeFields();
        var actions=PaymentUi.Actions();layout.Controls.Add(actions,0,3);
        var save=PaymentUi.Action(actions,"Xác nhận ghi vào sổ thu",IconKind.Check,true);AcceptButton=save;
        var cancel=PaymentUi.Action(actions,"Quay lại",IconKind.Close);cancel.Click+=(_,_)=>Close();CancelButton=cancel;
        save.Click+=(_,_)=>
        {
            if(!PaymentPolicy.TryReadAmount(_amount.Text,out var amount) || amount>summary.Remaining)
            {Ui.Error(this,"Nhập số tiền dương chính xác đến 0,01 đ, không vượt số dư; không tự làm tròn.");return;}
            if(!AtelierNotificationService.Confirm(this,"Xác nhận thủ công",$"Ghi nhận {ReceiptText.Money(amount)} cho {summary.BookingCode}?\r\n{_method.Text} · {_purpose.Text}\r\nĐây là giao dịch mô phỏng."))return;
            Amount=amount;DialogResult=DialogResult.OK;Close();
        };
    }
    private sealed record Choice(string Code,string Text);
}
