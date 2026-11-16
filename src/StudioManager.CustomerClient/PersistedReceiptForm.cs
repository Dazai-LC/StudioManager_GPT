using System.Drawing.Printing;
using StudioManager.CustomerApi;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;
namespace StudioManager.CustomerDesktop;

/// <summary>Prints only a persisted receipt DTO. No demo document generator or database connection.</summary>
public sealed class PersistedReceiptForm : Form
{
    private readonly PrintDocument _document=new();
    private readonly Font _printFont=Theme.Font(10);
    private readonly string _text;
    private int _position;
    public PersistedReceiptForm(PaymentReceiptDto receipt)
    {
        _text=ReceiptText.Format(receipt);
        Text="Biên nhận đã lưu — "+receipt.Code;StartPosition=FormStartPosition.CenterParent;
        ClientSize=new Size(920,760);MinimumSize=new Size(760,560);
        var layout=PaymentUi.Layout(this,"Biên nhận đã lưu",receipt.Code+" · Atelier Lumière");
        layout.Controls.Add(PaymentUi.Summary(new Label{Text="DEMO ĐỒ ÁN · Dữ liệu lấy từ giao dịch đã lưu. In lại giữ nguyên mã và số tiền; không phải hóa đơn thuế điện tử."}),0,1);
        var preview=new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,Font=Theme.MonoFont(10.2f),
            Text=_text,BackColor=Theme.Surface,ForeColor=Theme.Text,ScrollBars=RichTextBoxScrollBars.Vertical,WordWrap=true};
        var paper=PaymentUi.Data(preview);paper.Padding=new Padding(22);layout.Controls.Add(paper,0,2);
        var actions=PaymentUi.Actions();layout.Controls.Add(actions,0,3);
        ModernButton Add(string label,IconKind icon,Action action,bool primary=false)
        {var b=PaymentUi.Action(actions,label,icon,primary);b.Click+=(_,_)=>action();return b;}
        Add("Xem bản in A4",IconKind.Eye,()=>Print(false,false));Add("In biên nhận",IconKind.Printer,()=>Print(true,false),true);
        Add("PDF qua máy in PDF",IconKind.Download,()=>Print(true,true));CancelButton=Add("Đóng",IconKind.Close,Close);
        _document.DocumentName="BienNhan-"+receipt.Code;
        _document.DefaultPageSettings.PaperSize=new PaperSize("A4",827,1169);
        _document.DefaultPageSettings.Margins=new Margins(55,55,55,55);
        _document.BeginPrint+=(_,_)=>_position=0;
        _document.PrintPage+=(_,e)=>
        {
            if(e.Graphics is null)return;
            using var format=new StringFormat(StringFormatFlags.LineLimit);
            var rest=_text[_position..];
            e.Graphics.MeasureString(rest,_printFont,e.MarginBounds.Size,format,out var count,out var lineCount);
            if(count<=0)throw new InvalidOperationException("Không đủ vùng in.");
            e.Graphics.DrawString(rest[..count],_printFont,Brushes.Black,e.MarginBounds,format);
            _position+=count;e.HasMorePages=_position<_text.Length;
        };
    }
    private void Print(bool send,bool pdf)
    {
        try
        {
            if(pdf)
            {
                var printer=PrinterSettings.InstalledPrinters.Cast<string>().FirstOrDefault(p=>p.Equals("Microsoft Print to PDF",StringComparison.OrdinalIgnoreCase));
                if(printer is null){PaymentUi.Warn(this,"Chưa có Microsoft Print to PDF. Có thể chọn máy in PDF khác trong hộp thoại In.");return;}
                _document.PrinterSettings.PrinterName=printer;
            }
            if(!send){using var preview=new PrintPreviewDialog{Document=_document,Width=1050,Height=800};preview.ShowDialog(this);return;}
            using var dialog=new PrintDialog{Document=_document,UseEXDialog=true,AllowSomePages=false};
            if(dialog.ShowDialog(this)==DialogResult.OK)_document.Print();
        }
        catch { PaymentUi.Warn(this,"Không thể hoàn tất in. Kiểm tra máy in rồi thử lại; giao dịch đã lưu không được thu lại."); }
    }
    protected override void Dispose(bool disposing)
    {if(disposing){_document.Dispose();_printFont.Dispose();}base.Dispose(disposing);}
}
