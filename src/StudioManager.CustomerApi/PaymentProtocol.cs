using System.Globalization;
using System.Text;
namespace StudioManager.CustomerApi;

public sealed record PaymentLineDto(string Name,string Unit,decimal UnitPrice,decimal Quantity);
public sealed record PaymentRowDto(long Id,string Code,string Purpose,decimal Amount,string? Method,DateTime PaidAt,bool CanViewReceipt);
public sealed record BookingPaymentsDto(string BookingCode,string Status,decimal Total,decimal Collected,decimal Refunded,
    decimal Remaining,IReadOnlyList<PaymentRowDto> Items,long? NextBeforeId);
public sealed record PaymentReceiptDto(long Id,string Code,string BookingCode,string CustomerName,string Phone,string PackageName,
    decimal? PackagePrice,IReadOnlyList<PaymentLineDto> Services,decimal? Discount,decimal? BookingTotal,decimal Amount,string? Method,
    string Purpose,DateTime PaidAt,string StaffName,decimal? BalanceAfter,bool HasSnapshot,string? Note);

public static class ReceiptText
{
    private static string OneLine(string value)=>string.Concat(value.Select(c=>char.IsControl(c)?' ':c));
    public static string Money(decimal value)=>value.ToString("#,0.00##",CultureInfo.GetCultureInfo("vi-VN"))+" đ";
    public static string BookingStatus(string value)=>value switch
    {"DA_DAT_LICH"=>"Đã xác nhận lịch","DA_CHUP"=>"Đã chụp","DANG_CHINH_SUA"=>"Đang chỉnh sửa ảnh","CHO_GIAO_ANH"=>"Chờ giao ảnh","DA_GIAO_ANH"=>"Đã giao ảnh","HOAN_THANH"=>"Hoàn thành","DA_HUY"=>"Đã hủy",_=>"Chưa xác định trạng thái"};
    public static string Method(string? value)=>value switch {"CASH"=>"Tiền mặt (mô phỏng)","BANK_TRANSFER_DEMO"=>"Chuyển khoản DEMO",_=>"Chưa lưu phương thức (giao dịch cũ)"};
    public static string Purpose(string value)=>value switch {"DAT_COC"=>"Đặt cọc","BO_SUNG"=>"Thu bổ sung","CON_LAI"=>"Thu phần còn lại",_=>value};
    public static string Format(PaymentReceiptDto r)
    {
        if(r.Id<=0 || string.IsNullOrWhiteSpace(r.Code))throw new ArgumentException("Persisted transaction identity required.");
        var text=new StringBuilder("ATELIER LUMIÈRE · STUDIO MANAGER\r\nBIÊN NHẬN THU TIỀN — DEMO ĐỒ ÁN\r\nKhông phải hóa đơn thuế điện tử.\r\n\r\n");
        text.AppendLine("Mã giao dịch: "+OneLine(r.Code)).AppendLine("Mã lịch: "+OneLine(r.BookingCode));
        text.AppendLine("Thời điểm thu: "+r.PaidAt.ToString("dd/MM/yyyy HH:mm:ss"));
        text.AppendLine("Khách hàng: "+OneLine(r.CustomerName)+" · "+OneLine(r.Phone)).AppendLine("Gói chụp: "+OneLine(r.PackageName));
        if(r.HasSnapshot)
        {
            text.AppendLine("Giá gói: "+Money(r.PackagePrice!.Value));
            foreach(var line in r.Services)text.AppendLine($"• {OneLine(line.Name)}: {line.Quantity:0.##} {OneLine(line.Unit)} × {Money(line.UnitPrice)} = {Money(line.UnitPrice*line.Quantity)}");
            text.AppendLine("Giảm giá: "+Money(r.Discount!.Value)).AppendLine("Tổng lịch tại lúc thu: "+Money(r.BookingTotal!.Value));
        }
        else text.AppendLine("Giao dịch lịch sử: thông tin hồ sơ/gói hiện tại; tổng, giảm giá và dư nợ lúc thu chưa được lưu.");
        text.AppendLine().AppendLine("SỐ TIỀN GIAO DỊCH: "+Money(r.Amount));
        text.AppendLine("Phương thức: "+Method(r.Method)).AppendLine("Mục đích: "+Purpose(r.Purpose));
        if(r.BalanceAfter is { } balance)text.AppendLine("Còn lại ngay sau lần thu này: "+Money(balance));
        text.AppendLine("Nhân viên thu: "+OneLine(r.StaffName)).AppendLine("Ghi chú: "+OneLine(r.Note??"—"));
        return text.AppendLine().AppendLine("Biên nhận lấy từ giao dịch đã lưu; in lại giữ nguyên mã.")
            .AppendLine("Chuyển khoản DEMO do nhân viên xác nhận thủ công; không xác minh ngân hàng.").ToString();
    }
}
