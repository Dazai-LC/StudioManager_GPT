using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class ReportsPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DateTimePicker _from=new(){Format=DateTimePickerFormat.Short,Value=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1)},_to=new(){Format=DateTimePickerFormat.Short,Value=DateTime.Today};
    private readonly StatCard _revenue=new("DOANH THU DỊCH VỤ",Theme.Primary,"↗"),_cash=new("THỰC THU",Theme.Success,"$"),_debt=new("CÔNG NỢ HIỆN TẠI",Theme.Warning,"!"),_count=new("TỔNG LỊCH",Theme.Cyan,"#");
    private readonly DataGridView _grid=Theme.Grid();

    public ReportsPage(AppFacade app)
    {
        _app=app;
        BackColor=Theme.Background;
        var header=new PageHeader("Báo cáo & thống kê","Tổng hợp hiệu quả kinh doanh theo khoảng thời gian.");
        var run=Theme.Button("Xem báo cáo");
        run.Click+=async(_,_)=>await LoadAsync();
        var export=Theme.Button("Xuất CSV",Theme.Purple);
        export.Click+=(_,_)=>Ui.ExportGrid(_grid,this,"bao-cao-goi-chup");
        header.Actions.Controls.Add(run);
        header.Actions.Controls.Add(export);

        var filters=new FlowLayoutPanel{Dock=DockStyle.Top,Height=76,Padding=new Padding(0,4,0,8),WrapContents=false};
        filters.Controls.Add(Ui.Field("TỪ NGÀY",_from,190));
        filters.Controls.Add(Ui.Field("ĐẾN NGÀY",_to,190));

        var cards=new TableLayoutPanel{Dock=DockStyle.Top,Height=142,ColumnCount=4,Padding=new Padding(0,0,0,14)};
        for(var i=0;i<4;i++)cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
        cards.Controls.Add(_revenue,0,0);cards.Controls.Add(_cash,1,0);cards.Controls.Add(_debt,2,0);cards.Controls.Add(_count,3,0);
        foreach(Control c in cards.Controls)c.Dock=DockStyle.Fill;

        var card=new CardPanel{Dock=DockStyle.Fill,Padding=new Padding(18)};
        card.Controls.Add(_grid);
        card.Controls.Add(new Label{Text="Mức sử dụng gói chụp",Dock=DockStyle.Top,Height=42,Font=Theme.Font(13,FontStyle.Bold),ForeColor=Theme.Text});
        Controls.Add(card);
        Controls.Add(cards);
        Controls.Add(filters);
        Controls.Add(header);
        Load+=async(_,_)=>await LoadAsync();
    }
    private async Task LoadAsync(){try{var result=await _app.Reports.LoadAsync(_from.Value.Date,_to.Value.Date,_app.Session!);if(!result.Success||result.Data is null){Ui.Error(this,result.Message);return;}var r=result.Data;_revenue.Value=$"{r.DoanhThu:N0} đ";_cash.Value=$"{r.ThucThu:N0} đ";_debt.Value=$"{r.CongNo:N0} đ";_count.Value=r.TongLich.ToString();_grid.DataSource=r.ByPackage.Select(x=>new{Gói_chụp=x.Name,Số_lần=(int)x.Value}).ToList();}catch(Exception ex){Ui.Error(this,ex.Message);}}
}
