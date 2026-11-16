using System.Collections.Concurrent;
using StudioManager.Domain;
namespace StudioManager.Application.Services;

public sealed partial class FinanceService
{
    private int _posting;
    private readonly ConcurrentDictionary<long,byte> _needsReload = new();

    public async Task<Result<PaymentReceipt>> CollectAsync(PaymentInput input,UserSession user,CancellationToken ct=default)
    {
        var valid=PaymentPolicy.Validate(input,true);
        if(!valid.Success)return Result<PaymentReceipt>.Fail(valid.Code,valid.Message);
        if(Interlocked.CompareExchange(ref _posting,1,0)!=0)
            return Result<PaymentReceipt>.Fail("BUSY","Đang ghi nhận giao dịch; không bấm gửi lại.");
        try
        {
            var access=await Access.RequireAsync(user,ct);
            if(!access.Success)return Result<PaymentReceipt>.Fail(access.Code,access.Message);
            if(_needsReload.ContainsKey(input.LichChupId))
                return Result<PaymentReceipt>.Fail("RELOAD_REQUIRED","Tải lại số dư và lịch sử từ máy chủ trước khi thu tiếp; kiểm tra giao dịch trước đó.");
            Result<PaymentReceipt> result;
            try { result=await repo.PostPaymentAsync(input,access.Data!,ct); }
            catch { result=Result<PaymentReceipt>.Fail("OUTCOME_UNKNOWN","Chưa xác định kết quả ghi nhận. Tải lại số dư/lịch sử; không gửi lại giao dịch."); }
            if(result.Code=="OUTCOME_UNKNOWN")_needsReload[input.LichChupId]=0;
            return result;
        }
        finally { Volatile.Write(ref _posting,0); }
    }
    public async Task<Result<BookingPaymentSummary>> PaymentsAsync(long bookingId,long? beforeId=null,CancellationToken ct=default)
    {
        var access=await Access.RequireAsync(Access.CurrentSession,ct);
        if(!access.Success)return Result<BookingPaymentSummary>.Fail(access.Code,access.Message);
        if(bookingId<=0 || beforeId is <=0)return Result<BookingPaymentSummary>.Fail("INVALID_BOOKING","Lịch không hợp lệ.");
        var summary=await repo.GetPaymentSummaryAsync(bookingId,beforeId,access.Data!,ct);
        if(summary is null)return Result<BookingPaymentSummary>.Fail("NOT_FOUND","Không tìm thấy lịch chụp.");
        if(beforeId is null && Volatile.Read(ref _posting)==0)_needsReload.TryRemove(bookingId,out _);
        return Result<BookingPaymentSummary>.Ok(summary);
    }
    public async Task<Result<PaymentReceipt>> ReceiptAsync(long bookingId,long paymentId,CancellationToken ct=default)
    {
        var access=await Access.RequireAsync(Access.CurrentSession,ct);
        if(!access.Success)return Result<PaymentReceipt>.Fail(access.Code,access.Message);
        if(bookingId<=0 || paymentId<=0)return Result<PaymentReceipt>.Fail("INVALID_BOOKING","Giao dịch không hợp lệ.");
        var receipt=await repo.GetPaymentReceiptAsync(bookingId,paymentId,access.Data!,ct);
        return receipt is null?Result<PaymentReceipt>.Fail("NOT_FOUND","Không tìm thấy giao dịch của lịch chụp này."):Result<PaymentReceipt>.Ok(receipt);
    }
}
