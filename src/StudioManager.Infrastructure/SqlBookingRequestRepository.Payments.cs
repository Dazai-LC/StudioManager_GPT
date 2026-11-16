using Microsoft.Data.SqlClient;
using StudioManager.Domain;
namespace StudioManager.Infrastructure;

public sealed partial class SqlBookingRequestRepository
{
    private static async Task<long?> PaymentBooking(SqlConnection cn,SqlTransaction tx,CustomerBookingActor actor,long requestId,CancellationToken ct)
    {
        await RequireActor(cn,tx,actor,ct);
        // Same request-owner + current business-customer continuity rule as GetOfficialAsync.
        await using var q=Command(cn,tx,"""
            SELECT l.LichChupId FROM dbo.YeuCauDatLich r WITH(HOLDLOCK)
            JOIN dbo.LichChup l WITH(HOLDLOCK) ON l.LichChupId=r.LichChupId
            WHERE r.YeuCauDatLichId=@Id AND r.TrangThai='APPROVED'
              AND r.TaiKhoanKhachHangId=@Owner AND r.KhachHangId=l.KhachHangId
            """,("@Id",requestId),("@Owner",actor.Credential.AccountId));
        var id=await q.ExecuteScalarAsync(ct);return id is null or DBNull?null:Convert.ToInt64(id);
    }
    public Task<BookingPaymentSummary?> GetPaymentsAsync(CustomerBookingActor actor,long requestId,long? beforeId,CancellationToken ct=default)
        =>Transaction(async(cn,tx)=>await PaymentBooking(cn,tx,actor,requestId,ct) is { } booking
            ?await SqlStudioRepository.ReadPaymentSummary(cn,tx,booking,beforeId,true,ct):null,ct);
    public Task<PaymentReceipt?> GetReceiptAsync(CustomerBookingActor actor,long requestId,long paymentId,CancellationToken ct=default)
        =>Transaction(async(cn,tx)=>await PaymentBooking(cn,tx,actor,requestId,ct) is { } booking
            ?await SqlStudioRepository.ReadPaymentReceipt(cn,tx,booking,paymentId,true,ct):null,ct);
}
