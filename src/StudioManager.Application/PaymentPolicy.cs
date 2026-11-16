using System.Globalization;
using System.Text.RegularExpressions;
using StudioManager.Domain;

namespace StudioManager.Application;

public static class PaymentPolicy
{
    public static Result Validate(PaymentInput input, bool receiptRequired)
    {
        if(input.LichChupId<=0) return Result.Fail("INVALID_BOOKING","Chọn lịch chụp đã được xác nhận.");
        if(input.SoTien<=0) return Result.Fail("INVALID_AMOUNT","Số tiền thu phải lớn hơn 0.");
        if(!BookingMoneyPolicy.FitsLedger(input.SoTien)) return Result.Fail("INVALID_PRECISION","Số tiền phải chính xác đến 0,01 đ; không tự làm tròn.");
        if(input.LoaiThu is not ("DAT_COC" or "BO_SUNG" or "CON_LAI")) return Result.Fail("INVALID_TYPE","Loại thu không hợp lệ.");
        if(input.GhiChu?.Length>500) return Result.Fail("INVALID_NOTE","Ghi chú tối đa 500 ký tự.");
        if(receiptRequired && (!PaymentMethods.IsSupported(input.PhuongThucThu) || input.YeuCauThuId==Guid.Empty))
            return Result.Fail("INVALID_METHOD","Chọn tiền mặt hoặc chuyển khoản DEMO và xác nhận một giao dịch mới.");
        return Result.Ok();
    }
    // No grouping separators/exponents/implicit rounding. Both decimal separators are explicit UX choices.
    public static bool TryReadAmount(string text,out decimal amount)
    {
        amount=0;
        return Regex.IsMatch(text.Trim(),@"\A[0-9]+(?:[.,][0-9]{1,2})?\z") &&
            decimal.TryParse(text.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out amount) &&
            amount>0 && BookingMoneyPolicy.FitsLedger(amount);
    }
    public static bool ValidSnapshot(PaymentReceiptSnapshot s)
    {
        try
        {
            return s.CustomerId>0 && !string.IsNullOrWhiteSpace(s.BookingCode) && s.Services is not null &&
                BookingMoneyPolicy.FitsLedger(s.PackagePrice) && BookingMoneyPolicy.FitsLedger(s.Discount) &&
                BookingMoneyPolicy.FitsLedger(s.BookingTotal) && BookingMoneyPolicy.FitsLedger(s.BalanceAfter) && s.BalanceAfter<=s.BookingTotal &&
                s.Services.All(x=>x.Quantity>0 && BookingMoneyPolicy.FitsLedger(x.UnitPrice) && BookingMoneyPolicy.FitsLedger(x.UnitPrice*x.Quantity)) &&
                s.PackagePrice+s.Services.Sum(x=>x.UnitPrice*x.Quantity)-s.Discount==s.BookingTotal;
        }
        catch(OverflowException){return false;}
    }
}
