using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Domain;
namespace StudioManager.Infrastructure;

public sealed partial class SqlStudioRepository
{
    public Task<Result<PaymentReceipt>> PostPaymentAsync(PaymentInput input,UserSession user,CancellationToken ct=default)
        =>PostPaymentCoreAsync(input,user,true,ct);

    private async Task<Result<PaymentReceipt>> PostPaymentCoreAsync(PaymentInput input,UserSession user,bool receiptRequired,CancellationToken ct)
    {
        Result<PaymentReceipt> Fail(string code,string message)=>Result<PaymentReceipt>.Fail(code,message);
        var valid=PaymentPolicy.Validate(input,receiptRequired);
        if(!valid.Success)return Fail(valid.Code,valid.Message);
        await using var cn=Connection();SqlTransaction? tx=null;
        try
        {
            await cn.OpenAsync(ct);tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            user=await RequireActorAsync(cn,tx,user,false,ct);
            if(receiptRequired && !await HasReceiptSchema(cn,tx,ct))
                return Fail("MIGRATION_REQUIRED","Chức năng biên nhận chưa sẵn sàng. Liên hệ quản trị để cập nhật dữ liệu; chưa ghi khoản thu.");
            var booking=await ReadPaymentBalance(cn,tx,input.LichChupId,true,ct);
            if(booking is null)return Fail("NOT_FOUND","Không tìm thấy lịch chụp.");
            if(receiptRequired)
            {
                await using var previous=new SqlCommand("""
                    SELECT ThanhToanId,LichChupId,LoaiThu,SoTien,PhuongThucThu,TaiKhoanThucHienId,GhiChu
                    FROM dbo.ThanhToan WITH(UPDLOCK,HOLDLOCK) WHERE YeuCauThuId=@Key
                    """,cn,tx);
                previous.Parameters.Add("@Key",SqlDbType.UniqueIdentifier).Value=input.YeuCauThuId;
                long? existing=null;
                await using(var r=await previous.ExecuteReaderAsync(ct))
                    if(await r.ReadAsync(ct))
                    {
                        if(r.GetInt64(1)!=input.LichChupId || r.GetString(2)!=input.LoaiThu || r.GetDecimal(3)!=input.SoTien ||
                            r.GetString(4)!=input.PhuongThucThu || r.GetInt32(5)!=user.TaiKhoanId ||
                            (r.IsDBNull(6)?null:r.GetString(6))!=input.GhiChu)
                            return Fail("POSTING_KEY_REUSED","Mã xác nhận đã dùng cho giao dịch khác. Tải lại lịch sử.");
                        existing=r.GetInt64(0);
                    }
                if(existing is { } id)
                {
                    var saved=await ReadPaymentReceipt(cn,tx,input.LichChupId,id,false,ct);
                    if(saved is null)throw new InvalidDataException();
                    await tx.CommitAsync(ct);
                    return Result<PaymentReceipt>.Ok(saved,"Giao dịch đã tồn tại; không ghi thêm khoản thu.");
                }
            }
            if(booking.Status=="DA_HUY")return Fail("CANCELLED","Lịch đã hủy không được thu thêm.");
            var collection=BookingMoneyPolicy.ValidateCollection(input.SoTien,booking.Total-booking.Collected);
            if(!collection.Success)return Fail(collection.Code,collection.Message);
            if(receiptRequired && (!BookingMoneyPolicy.FitsLedger(booking.Total) || !BookingMoneyPolicy.FitsLedger(booking.Total-booking.Collected)))
                return Fail("HISTORICAL_PRECISION","Số dư lịch cũ có phần lẻ không thể biểu diễn đến 0,01 đ; cần kiểm tra, không tự làm tròn.");
            PaymentReceiptSnapshot? snapshot=null;
            if(receiptRequired)
            {
                await using var details=new SqlCommand("""
                    SELECT l.KhachHangId,k.HoTen,k.SoDienThoai,l.TenGoiChot,l.GiaGoiChot,l.TienGiam,
                      COALESCE(n.HoTen,t.TenDangNhap)
                    FROM dbo.LichChup l JOIN dbo.KhachHang k ON k.KhachHangId=l.KhachHangId
                    JOIN dbo.TaiKhoan t ON t.TaiKhoanId=@User LEFT JOIN dbo.NhanVien n ON n.NhanVienId=t.NhanVienId
                    WHERE l.LichChupId=@Id
                    """,cn,tx);
                details.Parameters.Add("@Id",SqlDbType.BigInt).Value=input.LichChupId;
                details.Parameters.Add("@User",SqlDbType.Int).Value=user.TaiKhoanId;
                await using(var r=await details.ExecuteReaderAsync(ct))
                {
                    if(!await r.ReadAsync(ct))throw new InvalidDataException();
                    snapshot=new(r.GetInt64(0),r.GetString(1),r.GetString(2),booking.BookingCode,r.GetString(3),r.GetDecimal(4),[],
                        r.GetDecimal(5),booking.Total,booking.Total-booking.Collected-input.SoTien,r.GetString(6));
                }
                snapshot=snapshot with {Services=await ReceiptServices(cn,tx,input.LichChupId,ct)};
                if(!PaymentPolicy.ValidSnapshot(snapshot))return Fail("HISTORICAL_PRECISION","Các dòng tiền lịch cũ không tạo được biên nhận chính xác; cần kiểm tra, không tự làm tròn.");
            }
            var code=$"TT{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
            var sql=receiptRequired?"""
                INSERT dbo.ThanhToan(MaThanhToan,LichChupId,LoaiThu,SoTien,NgayGiaoDich,TaiKhoanThucHienId,GhiChu,PhuongThucThu,YeuCauThuId,BienNhanJson)
                VALUES(@Code,@Id,@Type,@Amount,GETDATE(),@User,@Note,@Method,@Key,@Snapshot);
                SELECT CONVERT(bigint,SCOPE_IDENTITY());
                """:"""
                INSERT dbo.ThanhToan(MaThanhToan,LichChupId,LoaiThu,SoTien,NgayGiaoDich,TaiKhoanThucHienId,GhiChu)
                VALUES(@Code,@Id,@Type,@Amount,GETDATE(),@User,@Note);
                SELECT CONVERT(bigint,SCOPE_IDENTITY());
                """;
            long paymentId;
            await using(var insert=new SqlCommand(sql,cn,tx))
            {
                insert.Parameters.Add("@Code",SqlDbType.VarChar,25).Value=code;
                insert.Parameters.Add("@Id",SqlDbType.BigInt).Value=input.LichChupId;
                insert.Parameters.Add("@Type",SqlDbType.VarChar,20).Value=input.LoaiThu;
                var amount=insert.Parameters.Add("@Amount",SqlDbType.Decimal);amount.Precision=18;amount.Scale=2;amount.Value=input.SoTien;
                insert.Parameters.Add("@User",SqlDbType.Int).Value=user.TaiKhoanId;
                insert.Parameters.Add("@Note",SqlDbType.NVarChar,500).Value=(object?)input.GhiChu??DBNull.Value;
                if(receiptRequired)
                {
                    insert.Parameters.Add("@Method",SqlDbType.VarChar,24).Value=input.PhuongThucThu!;
                    insert.Parameters.Add("@Key",SqlDbType.UniqueIdentifier).Value=input.YeuCauThuId;
                    insert.Parameters.Add("@Snapshot",SqlDbType.NVarChar,-1).Value=JsonSerializer.Serialize(snapshot);
                }
                paymentId=Convert.ToInt64(await insert.ExecuteScalarAsync(ct));
            }
            await AuditAsync(cn,tx,user,"THU_TIEN","LichChup",input.LichChupId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                null,JsonSerializer.Serialize(new{PaymentId=paymentId,input.SoTien,input.PhuongThucThu}),null,ct);
            var receipt=receiptRequired?await ReadPaymentReceipt(cn,tx,input.LichChupId,paymentId,false,ct):null;
            if(receiptRequired && receipt is null)throw new InvalidDataException();
            await tx.CommitAsync(ct);
            return new(true,"OK",receiptRequired?"Đã lưu khoản thu mô phỏng và biên nhận.":"Đã ghi nhận khoản thu.",receipt);
        }
        catch(UnauthorizedAccessException){return Fail("SESSION_INVALID","Phiên nhân sự đã thay đổi. Vui lòng đăng nhập lại.");}
        catch { return Fail("OUTCOME_UNKNOWN","Chưa xác định được kết quả ghi nhận. Tải lại số dư/lịch sử trước khi thao tác; không tự gửi lại."); }
        finally
        {
            if(tx is not null){try{if(tx.Connection is not null)await tx.RollbackAsync(CancellationToken.None);}catch{}await tx.DisposeAsync();}
        }
    }

    public async Task<BookingPaymentSummary?> GetPaymentSummaryAsync(long bookingId,long? beforeId,UserSession user,CancellationToken ct=default)
    {
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await RequireActorAsync(cn,tx,user,false,ct);
        var result=await ReadPaymentSummary(cn,tx,bookingId,beforeId,false,ct);await tx.CommitAsync(ct);return result;
    }
    public async Task<PaymentReceipt?> GetPaymentReceiptAsync(long bookingId,long paymentId,UserSession user,CancellationToken ct=default)
    {
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await RequireActorAsync(cn,tx,user,false,ct);
        var result=await ReadPaymentReceipt(cn,tx,bookingId,paymentId,false,ct);await tx.CommitAsync(ct);return result;
    }
    private static async Task<bool> HasReceiptSchema(SqlConnection cn,SqlTransaction tx,CancellationToken ct)
    {
        await using var q=new SqlCommand("SELECT CASE WHEN COL_LENGTH('dbo.ThanhToan','PhuongThucThu') IS NOT NULL AND COL_LENGTH('dbo.ThanhToan','YeuCauThuId') IS NOT NULL AND COL_LENGTH('dbo.ThanhToan','BienNhanJson') IS NOT NULL THEN 1 ELSE 0 END",cn,tx);
        return Convert.ToInt32(await q.ExecuteScalarAsync(ct))==1;
    }
    private static async Task<BookingPaymentSummary?> ReadPaymentBalance(SqlConnection cn,SqlTransaction tx,long bookingId,bool update,CancellationToken ct)
    {
        var sql="""
            SELECT l.MaLichChup,l.TrangThai,l.GiaGoiChot+COALESCE((SELECT CAST(SUM(SoLuong*DonGiaChot) AS decimal(32,4)) FROM dbo.LichChupDichVu WHERE LichChupId=l.LichChupId),0)-l.TienGiam,
              COALESCE((SELECT CAST(SUM(SoTien) AS decimal(28,2)) FROM dbo.ThanhToan WHERE LichChupId=l.LichChupId),0),
              COALESCE((SELECT CAST(SUM(SoTien) AS decimal(28,2)) FROM dbo.HoanTien WHERE LichChupId=l.LichChupId),0)
            FROM dbo.LichChup l
            """+(update?" WITH(UPDLOCK,HOLDLOCK) ":" WITH(HOLDLOCK) ")+" WHERE l.LichChupId=@Id";
        await using var q=new SqlCommand(sql,cn,tx);q.Parameters.Add("@Id",SqlDbType.BigInt).Value=bookingId;
        await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        var state=r.GetString(1);var total=r.GetDecimal(2);var collected=r.GetDecimal(3);
        return new(bookingId,r.GetString(0),state,total,collected,r.GetDecimal(4),state=="DA_HUY"?0:Math.Max(0,total-collected),[],null);
    }
    internal static async Task<BookingPaymentSummary?> ReadPaymentSummary(SqlConnection cn,SqlTransaction tx,long bookingId,long? beforeId,bool customer,CancellationToken ct)
    {
        var balance=await ReadPaymentBalance(cn,tx,bookingId,false,ct);if(balance is null)return null;
        var extended=await HasReceiptSchema(cn,tx,ct);
        var method=extended?"t.PhuongThucThu":"CAST(NULL AS varchar(24))";
        var canView=customer?(extended?"CASE WHEN TRY_CONVERT(bigint,JSON_VALUE(t.BienNhanJson,'$.CustomerId'))=l.KhachHangId THEN 1 ELSE 0 END":"0"):"1";
        var sql="SELECT TOP(51) t.ThanhToanId,t.MaThanhToan,t.LoaiThu,t.SoTien,"+method+",t.NgayGiaoDich,"+canView+
            " FROM dbo.ThanhToan t JOIN dbo.LichChup l ON l.LichChupId=t.LichChupId WHERE t.LichChupId=@Id AND (@Before IS NULL OR t.ThanhToanId<@Before) ORDER BY t.ThanhToanId DESC";
        await using var q=new SqlCommand(sql,cn,tx);q.Parameters.Add("@Id",SqlDbType.BigInt).Value=bookingId;
        q.Parameters.Add("@Before",SqlDbType.BigInt).Value=(object?)beforeId??DBNull.Value;
        var rows=new List<PaymentTransaction>();await using var r=await q.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))rows.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetDecimal(3),r.IsDBNull(4)?null:r.GetString(4),r.GetDateTime(5),r.GetInt32(6)==1));
        return balance with{Payments=rows.Take(50).ToArray(),NextBeforeId=rows.Count>50?rows[49].Id:null};
    }
    private static async Task<IReadOnlyList<ReceiptServiceLine>> ReceiptServices(SqlConnection cn,SqlTransaction tx,long bookingId,CancellationToken ct)
    {
        await using var q=new SqlCommand("SELECT TenDichVuChot,DonViTinhChot,DonGiaChot,SoLuong FROM dbo.LichChupDichVu WHERE LichChupId=@Id ORDER BY LichChupDichVuId",cn,tx);
        q.Parameters.Add("@Id",SqlDbType.BigInt).Value=bookingId;var rows=new List<ReceiptServiceLine>();
        await using var r=await q.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))rows.Add(new(r.GetString(0),r.GetString(1),r.GetDecimal(2),r.GetDecimal(3)));return rows;
    }
    internal static async Task<PaymentReceipt?> ReadPaymentReceipt(SqlConnection cn,SqlTransaction tx,long bookingId,long paymentId,bool customer,CancellationToken ct)
    {
        var extended=await HasReceiptSchema(cn,tx,ct);
        var extra=extended?"t.PhuongThucThu,t.BienNhanJson":"CAST(NULL AS varchar(24)),CAST(NULL AS nvarchar(max))";
        var sql="SELECT t.ThanhToanId,t.MaThanhToan,t.LichChupId,l.MaLichChup,k.HoTen,k.SoDienThoai,l.TenGoiChot,t.SoTien,t.LoaiThu,t.NgayGiaoDich,COALESCE(n.HoTen,a.TenDangNhap),t.GhiChu,"+extra+",l.KhachHangId FROM dbo.ThanhToan t JOIN dbo.LichChup l ON l.LichChupId=t.LichChupId JOIN dbo.KhachHang k ON k.KhachHangId=l.KhachHangId JOIN dbo.TaiKhoan a ON a.TaiKhoanId=t.TaiKhoanThucHienId LEFT JOIN dbo.NhanVien n ON n.NhanVienId=a.NhanVienId WHERE t.ThanhToanId=@Payment AND t.LichChupId=@Id";
        await using var q=new SqlCommand(sql,cn,tx);q.Parameters.Add("@Id",SqlDbType.BigInt).Value=bookingId;q.Parameters.Add("@Payment",SqlDbType.BigInt).Value=paymentId;
        await using var r=await q.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return null;
        var snapshot=r.IsDBNull(13)?null:JsonSerializer.Deserialize<PaymentReceiptSnapshot>(r.GetString(13));
        if(snapshot is not null && !PaymentPolicy.ValidSnapshot(snapshot))throw new InvalidDataException("Stored receipt data is inconsistent.");
        if(customer && (snapshot is null || snapshot.CustomerId!=r.GetInt64(14)))return null;
        return new(r.GetInt64(0),r.GetString(1),r.GetInt64(2),snapshot?.BookingCode??r.GetString(3),snapshot?.CustomerName??r.GetString(4),
            snapshot?.Phone??r.GetString(5),snapshot?.PackageName??r.GetString(6),snapshot?.PackagePrice,snapshot?.Services??[],snapshot?.Discount,snapshot?.BookingTotal,
            r.GetDecimal(7),r.IsDBNull(12)?null:r.GetString(12),r.GetString(8),r.GetDateTime(9),snapshot?.StaffName??r.GetString(10),snapshot?.BalanceAfter,snapshot is not null,r.IsDBNull(11)?null:r.GetString(11));
    }
}
