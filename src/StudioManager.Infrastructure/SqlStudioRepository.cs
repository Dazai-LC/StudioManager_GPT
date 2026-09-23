using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Domain;

namespace StudioManager.Infrastructure;

public sealed class SqlStudioRepository(string connectionString) : IStudioRepository
{
    private SqlConnection Connection() => new(connectionString);
    private static string DbStatus(TrangThaiLich s) => s switch
    {
        TrangThaiLich.DaDatLich => "DA_DAT_LICH", TrangThaiLich.DaChup => "DA_CHUP",
        TrangThaiLich.DangChinhSuaAnh => "DANG_CHINH_SUA", TrangThaiLich.ChoGiaoAnh => "CHO_GIAO_ANH",
        TrangThaiLich.DaGiaoAnh => "DA_GIAO_ANH", TrangThaiLich.HoanThanh => "HOAN_THANH", _ => "DA_HUY"
    };
    private static TrangThaiLich DomainStatus(string s) => s switch
    {
        "DA_DAT_LICH" => TrangThaiLich.DaDatLich, "DA_CHUP" => TrangThaiLich.DaChup,
        "DANG_CHINH_SUA" => TrangThaiLich.DangChinhSuaAnh, "CHO_GIAO_ANH" => TrangThaiLich.ChoGiaoAnh,
        "DA_GIAO_ANH" => TrangThaiLich.DaGiaoAnh, "HOAN_THANH" => TrangThaiLich.HoanThanh, _ => TrangThaiLich.DaHuy
    };

    public async Task<TaiKhoan?> FindAccountAsync(string username, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT tk.TaiKhoanId,tk.NhanVienId,tk.TenDangNhap,tk.MatKhauHash,tk.VaiTro,tk.TrangThai,tk.PhaiDoiMatKhau,
                   COALESCE(nv.HoTen,tk.TenDangNhap) HoTen
            FROM TaiKhoan tk LEFT JOIN NhanVien nv ON nv.NhanVienId=tk.NhanVienId
            WHERE tk.TenDangNhap=@Username
            ";
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn); cmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username;
        await using var r = await cmd.ExecuteReaderAsync(ct); if (!await r.ReadAsync(ct)) return null;
        return new(r.GetInt32(0), r.IsDBNull(1) ? null : r.GetInt32(1), r.GetString(2), r.GetString(3),
            r.GetString(4) == "QUAN_TRI_VIEN" ? VaiTro.QuanTriVien : VaiTro.NhanVien, r.GetString(5) == "HOAT_DONG", r.GetBoolean(6), r.GetString(7));
    }

    public async Task UpdateLastLoginAsync(int id, DateTime time, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("UPDATE TaiKhoan SET LanDangNhapCuoi=@Now,UpdatedAt=@Now WHERE TaiKhoanId=@Id", cn);
        cmd.Parameters.AddWithValue("@Now", time); cmd.Parameters.AddWithValue("@Id", id); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<Result> ChangeOwnPasswordAsync(int accountId, string passwordHash, CancellationToken ct = default)
    {
        try
        {
            await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);
            await using var cmd = new SqlCommand("UPDATE TaiKhoan SET MatKhauHash=@Hash,PhaiDoiMatKhau=0,UpdatedAt=GETDATE() WHERE TaiKhoanId=@Id AND TrangThai='HOAT_DONG'", cn,tx);
            cmd.Parameters.Add("@Hash", SqlDbType.VarChar, 500).Value = passwordHash; cmd.Parameters.AddWithValue("@Id", accountId);
            if(await cmd.ExecuteNonQueryAsync(ct)!=1)return Result.Fail("NOT_FOUND", "Tài khoản không tồn tại hoặc đã bị khóa.");
            await AuditAsync(cn,tx,new UserSession(accountId,"", "",VaiTro.NhanVien),"DOI_MAT_KHAU","TaiKhoan",accountId.ToString(),null,"PhaiDoiMatKhau=0",null,ct);
            await tx.CommitAsync(ct);return Result.Ok("Đổi mật khẩu thành công.");
        }
        catch (Exception ex) { return Result.Fail("DB_ERROR", Friendly(ex)); }
    }

    public async Task<IReadOnlyList<LichChup>> SearchBookingsAsync(string? keyword, DateTime? from, DateTime? to, string? status, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT l.LichChupId,l.MaLichChup,l.KhachHangId,kh.HoTen,l.GoiChupId,l.TenGoiChot,l.GiaGoiChot,
              l.BatDau,l.KetThuc,l.NhiepAnhGiaId,nv.HoTen,l.PhongChupId,p.TenPhong,l.TrangThai,l.TienGiam,l.LyDoGiam,l.GhiChu,
              COALESCE(dv.TienDV,0),COALESCE(tt.DaThu,0),COALESCE(ht.DaHoan,0)
            FROM LichChup l JOIN KhachHang kh ON kh.KhachHangId=l.KhachHangId JOIN NhanVien nv ON nv.NhanVienId=l.NhiepAnhGiaId
              JOIN PhongChup p ON p.PhongChupId=l.PhongChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoLuong*DonGiaChot) TienDV FROM LichChupDichVu GROUP BY LichChupId) dv ON dv.LichChupId=l.LichChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoTien) DaThu FROM ThanhToan GROUP BY LichChupId) tt ON tt.LichChupId=l.LichChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoTien) DaHoan FROM HoanTien GROUP BY LichChupId) ht ON ht.LichChupId=l.LichChupId
            WHERE (@Q IS NULL OR l.MaLichChup LIKE '%'+@Q+'%' OR kh.HoTen LIKE N'%'+@Q+'%' OR kh.SoDienThoai LIKE '%'+@Q+'%')
              AND (@From IS NULL OR l.BatDau>=@From) AND (@To IS NULL OR l.BatDau<DATEADD(day,1,@To)) AND (@Status IS NULL OR l.TrangThai=@Status)
            ORDER BY CASE WHEN l.BatDau>=GETDATE() THEN 0 ELSE 1 END,l.BatDau
            ";
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var cmd = new SqlCommand(sql, cn);
        AddNullable(cmd,"@Q",keyword); AddNullable(cmd,"@From",from); AddNullable(cmd,"@To",to); AddNullable(cmd,"@Status",status);
        var list = new List<LichChup>(); await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(MapBooking(r)); return list;
    }

    public async Task<LichChup?> GetBookingAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT l.LichChupId,l.MaLichChup,l.KhachHangId,kh.HoTen,l.GoiChupId,l.TenGoiChot,l.GiaGoiChot,
              l.BatDau,l.KetThuc,l.NhiepAnhGiaId,nv.HoTen,l.PhongChupId,p.TenPhong,l.TrangThai,l.TienGiam,l.LyDoGiam,l.GhiChu,
              COALESCE(dv.TienDV,0),COALESCE(tt.DaThu,0),COALESCE(ht.DaHoan,0)
            FROM LichChup l JOIN KhachHang kh ON kh.KhachHangId=l.KhachHangId
              JOIN NhanVien nv ON nv.NhanVienId=l.NhiepAnhGiaId JOIN PhongChup p ON p.PhongChupId=l.PhongChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoLuong*DonGiaChot) TienDV FROM LichChupDichVu GROUP BY LichChupId) dv ON dv.LichChupId=l.LichChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoTien) DaThu FROM ThanhToan GROUP BY LichChupId) tt ON tt.LichChupId=l.LichChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoTien) DaHoan FROM HoanTien GROUP BY LichChupId) ht ON ht.LichChupId=l.LichChupId
            WHERE l.LichChupId=@Id";
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id); await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapBooking(reader) : null;
    }

    public async Task<bool> HasConflictAsync(long? excludeId, int photographerId, int roomId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(1) FROM LichChup WITH(UPDLOCK,HOLDLOCK)
            WHERE TrangThai<>'DA_HUY' AND (@Id IS NULL OR LichChupId<>@Id) AND BatDau<@End AND KetThuc>@Start
              AND (NhiepAnhGiaId=@Photographer OR PhongChupId=@Room)
            ";
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var cmd=new SqlCommand(sql,cn);
        AddNullable(cmd,"@Id",excludeId); cmd.Parameters.AddWithValue("@Photographer",photographerId); cmd.Parameters.AddWithValue("@Room",roomId);
        cmd.Parameters.AddWithValue("@Start",start); cmd.Parameters.AddWithValue("@End",end);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0;
    }

    public async Task<long> CreateBookingAsync(BookingInput input, UserSession user, CancellationToken ct = default)
    {
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string conflict="SELECT COUNT(1) FROM LichChup WITH(UPDLOCK,HOLDLOCK) WHERE TrangThai<>'DA_HUY' AND BatDau<@End AND KetThuc>@Start AND (NhiepAnhGiaId=@N OR PhongChupId=@P)";
            await using(var c=new SqlCommand(conflict,cn,tx)){c.Parameters.AddWithValue("@End",input.KetThuc);c.Parameters.AddWithValue("@Start",input.BatDau);c.Parameters.AddWithValue("@N",input.NhiepAnhGiaId);c.Parameters.AddWithValue("@P",input.PhongChupId);if(Convert.ToInt32(await c.ExecuteScalarAsync(ct))>0)throw new InvalidOperationException("Lịch bị trùng.");}
            var code=$"LC{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            const string insert=@"
              INSERT LichChup(MaLichChup,KhachHangId,GoiChupId,TenGoiChot,GiaGoiChot,BatDau,KetThuc,NhiepAnhGiaId,PhongChupId,TrangThai,TienGiam,GhiChu,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt)
              SELECT @Code,@K,g.GoiChupId,g.TenGoiChup,g.GiaGoi,@Start,@End,@N,@P,'DA_DAT_LICH',0,@Note,@User,GETDATE(),@User,GETDATE() FROM GoiChup g WHERE g.GoiChupId=@G AND g.TrangThai='DANG_AP_DUNG';
              SELECT CAST(SCOPE_IDENTITY() AS bigint);
              ";
            long id; await using(var c=new SqlCommand(insert,cn,tx)){c.Parameters.AddWithValue("@Code",code);c.Parameters.AddWithValue("@K",input.KhachHangId);c.Parameters.AddWithValue("@G",input.GoiChupId);c.Parameters.AddWithValue("@Start",input.BatDau);c.Parameters.AddWithValue("@End",input.KetThuc);c.Parameters.AddWithValue("@N",input.NhiepAnhGiaId);c.Parameters.AddWithValue("@P",input.PhongChupId);c.Parameters.AddWithValue("@Note",(object?)input.GhiChu??DBNull.Value);c.Parameters.AddWithValue("@User",user.TaiKhoanId);id=Convert.ToInt64(await c.ExecuteScalarAsync(ct));}
            await AuditAsync(cn,tx,user,"TAO_LICH","LichChup",id.ToString(),null,JsonSerializer.Serialize(input),null,ct); await tx.CommitAsync(ct); return id;
        } catch { await tx.RollbackAsync(ct); throw; }
    }

    public async Task<Result> UpdateBookingAsync(long id,BookingInput input,string? reason,UserSession user,CancellationToken ct=default)
    {
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string oldSql="SELECT CONCAT(KhachHangId,'|',GoiChupId,'|',CONVERT(varchar(19),BatDau,126),'|',CONVERT(varchar(19),KetThuc,126),'|',NhiepAnhGiaId,'|',PhongChupId) FROM LichChup WITH(UPDLOCK,HOLDLOCK) WHERE LichChupId=@Id";
            var old=await ScalarAsync<string>(cn,tx,oldSql,id,ct);if(old is null)return Result.Fail("NOT_FOUND","Không tìm thấy lịch.");
            const string conflict="SELECT COUNT(1) FROM LichChup WITH(UPDLOCK,HOLDLOCK) WHERE LichChupId<>@Id AND TrangThai<>'DA_HUY' AND BatDau<@End AND KetThuc>@Start AND (NhiepAnhGiaId=@N OR PhongChupId=@P)";
            await using(var c=new SqlCommand(conflict,cn,tx)){c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@End",input.KetThuc);c.Parameters.AddWithValue("@Start",input.BatDau);c.Parameters.AddWithValue("@N",input.NhiepAnhGiaId);c.Parameters.AddWithValue("@P",input.PhongChupId);if(Convert.ToInt32(await c.ExecuteScalarAsync(ct))>0)return Result.Fail("CONFLICT","Nhiếp ảnh gia hoặc phòng chụp đã có lịch.");}
            const string update=@"UPDATE l SET KhachHangId=@K,GoiChupId=g.GoiChupId,TenGoiChot=g.TenGoiChup,GiaGoiChot=g.GiaGoi,BatDau=@Start,KetThuc=@End,NhiepAnhGiaId=@N,PhongChupId=@P,GhiChu=@Note,UpdatedBy=@User,UpdatedAt=GETDATE() FROM LichChup l JOIN GoiChup g ON g.GoiChupId=@G WHERE l.LichChupId=@Id AND g.TrangThai='DANG_AP_DUNG' AND g.GiaGoi+COALESCE((SELECT SUM(SoLuong*DonGiaChot) FROM LichChupDichVu WHERE LichChupId=l.LichChupId),0)-l.TienGiam>=COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE LichChupId=l.LichChupId),0)";
            await using(var c=new SqlCommand(update,cn,tx)){c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@K",input.KhachHangId);c.Parameters.AddWithValue("@G",input.GoiChupId);c.Parameters.AddWithValue("@Start",input.BatDau);c.Parameters.AddWithValue("@End",input.KetThuc);c.Parameters.AddWithValue("@N",input.NhiepAnhGiaId);c.Parameters.AddWithValue("@P",input.PhongChupId);c.Parameters.AddWithValue("@Note",(object?)input.GhiChu??DBNull.Value);c.Parameters.AddWithValue("@User",user.TaiKhoanId);if(await c.ExecuteNonQueryAsync(ct)!=1)return Result.Fail("LIMIT","Gói không khả dụng hoặc tổng mới thấp hơn tiền đã thu.");}
            await AuditAsync(cn,tx,user,"CAP_NHAT_LICH","LichChup",id.ToString(),old,JsonSerializer.Serialize(input),reason,ct);await tx.CommitAsync(ct);return Result.Ok("Đã cập nhật lịch chụp.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> UpdateBookingStatusAsync(long id, TrangThaiLich next, string? reason, UserSession user, CancellationToken ct = default)
    {
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);
        try
        {
            var current=await ScalarAsync<string>(cn,tx,"SELECT TrangThai FROM LichChup WITH(UPDLOCK) WHERE LichChupId=@Id",id,ct);
            if(current is null)return Result.Fail("NOT_FOUND","Không tìm thấy lịch chụp.");
            var from=DomainStatus(current); if(!BusinessRules.DuocChuyen(from,next))return Result.Fail("INVALID_STATE","Không thể bỏ qua hoặc quay lại bước tiến độ.");
            await ExecuteAsync(cn,tx,"UPDATE LichChup SET TrangThai=@Value,HoanThanhLuc=CASE WHEN @Value='HOAN_THANH' THEN GETDATE() ELSE HoanThanhLuc END,UpdatedBy=@User,UpdatedAt=GETDATE() WHERE LichChupId=@Id",id,user.TaiKhoanId,DbStatus(next),ct);
            await AuditAsync(cn,tx,user,"DOI_TRANG_THAI","LichChup",id.ToString(),current,DbStatus(next),reason,ct); await tx.CommitAsync(ct); return Result.Ok("Đã cập nhật tiến độ.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",ex.Message);}
    }

    public async Task<Result> CancelBookingAsync(long id, string reason, UserSession user, CancellationToken ct = default)
    {
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);
        try
        {
            var current=await ScalarAsync<string>(cn,tx,"SELECT TrangThai FROM LichChup WITH(UPDLOCK) WHERE LichChupId=@Id",id,ct); if(current is null)return Result.Fail("NOT_FOUND","Không tìm thấy lịch.");
            await ExecuteAsync(cn,tx,"UPDATE LichChup SET TrangThai='DA_HUY',LyDoHuy=@Value,HuyBoiTaiKhoanId=@User,HuyLuc=GETDATE(),UpdatedBy=@User,UpdatedAt=GETDATE() WHERE LichChupId=@Id; UPDATE PhanCongTaiNguyen SET TrangThai='DA_HUY',UpdatedBy=@User,UpdatedAt=GETDATE() WHERE LichChupId=@Id AND TrangThai='DA_PHAN_CONG'",id,user.TaiKhoanId,reason,ct);
            await AuditAsync(cn,tx,user,"HUY_LICH","LichChup",id.ToString(),current,"DA_HUY",reason,ct); await tx.CommitAsync(ct);return Result.Ok("Đã hủy lịch chụp.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",ex.Message);}
    }

    public async Task<Result> AddPaymentAsync(PaymentInput input, UserSession user, CancellationToken ct = default)
    {
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string q=@"SELECT l.TrangThai,l.GiaGoiChot+COALESCE((SELECT SUM(SoLuong*DonGiaChot) FROM LichChupDichVu WHERE LichChupId=l.LichChupId),0)-l.TienGiam-COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE LichChupId=l.LichChupId),0) ConLai FROM LichChup l WITH(UPDLOCK,HOLDLOCK) WHERE l.LichChupId=@Id";
            await using var c=new SqlCommand(q,cn,tx);c.Parameters.AddWithValue("@Id",input.LichChupId);await using var r=await c.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return Result.Fail("NOT_FOUND","Không tìm thấy lịch.");var state=r.GetString(0);var remain=r.GetDecimal(1);await r.CloseAsync();
            if(state=="DA_HUY")return Result.Fail("CANCELLED","Lịch đã hủy không được thu thêm."); if(input.SoTien>remain)return Result.Fail("LIMIT",$"Số tiền tối đa có thể thu là {remain:N0} đ.");
            var code=$"TT{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            const string ins="INSERT ThanhToan(MaThanhToan,LichChupId,LoaiThu,SoTien,NgayGiaoDich,TaiKhoanThucHienId,GhiChu) VALUES(@Code,@Id,@Type,@Amount,GETDATE(),@User,@Note)";
            await using(var x=new SqlCommand(ins,cn,tx)){x.Parameters.AddWithValue("@Code",code);x.Parameters.AddWithValue("@Id",input.LichChupId);x.Parameters.AddWithValue("@Type",input.LoaiThu);x.Parameters.AddWithValue("@Amount",input.SoTien);x.Parameters.AddWithValue("@User",user.TaiKhoanId);x.Parameters.AddWithValue("@Note",(object?)input.GhiChu??DBNull.Value);await x.ExecuteNonQueryAsync(ct);}
            await AuditAsync(cn,tx,user,"THU_TIEN","LichChup",input.LichChupId.ToString(),null,$"{input.SoTien:0.##}",null,ct);await tx.CommitAsync(ct);return Result.Ok($"Đã ghi nhận {input.SoTien:N0} đ.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",ex.Message);}
    }

    public async Task<Result> AddRefundAsync(RefundInput input, UserSession user, CancellationToken ct = default)
    {
        if(user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Không đủ quyền.");
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string q="SELECT l.TrangThai,COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE LichChupId=l.LichChupId),0)-COALESCE((SELECT SUM(SoTien) FROM HoanTien WHERE LichChupId=l.LichChupId),0) FROM LichChup l WITH(UPDLOCK,HOLDLOCK) WHERE l.LichChupId=@Id";
            await using var c=new SqlCommand(q,cn,tx);c.Parameters.AddWithValue("@Id",input.LichChupId);await using var r=await c.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return Result.Fail("NOT_FOUND","Không tìm thấy lịch.");var state=r.GetString(0);var max=r.GetDecimal(1);await r.CloseAsync();
            if(state!="DA_HUY")return Result.Fail("INVALID_STATE","Chỉ hoàn tiền cho lịch đã hủy.");if(input.SoTien>max)return Result.Fail("LIMIT",$"Số tiền tối đa có thể hoàn là {max:N0} đ.");
            var code=$"HT{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            const string ins="INSERT HoanTien(MaHoanTien,LichChupId,SoTien,NgayGiaoDich,TaiKhoanThucHienId,LyDo,GhiChu) VALUES(@Code,@Id,@Amount,GETDATE(),@User,@Reason,@Note)";
            await using(var x=new SqlCommand(ins,cn,tx)){x.Parameters.AddWithValue("@Code",code);x.Parameters.AddWithValue("@Id",input.LichChupId);x.Parameters.AddWithValue("@Amount",input.SoTien);x.Parameters.AddWithValue("@User",user.TaiKhoanId);x.Parameters.AddWithValue("@Reason",input.LyDo);x.Parameters.AddWithValue("@Note",(object?)input.GhiChu??DBNull.Value);await x.ExecuteNonQueryAsync(ct);}
            await AuditAsync(cn,tx,user,"HOAN_TIEN","LichChup",input.LichChupId.ToString(),null,$"{input.SoTien:0.##}",input.LyDo,ct);await tx.CommitAsync(ct);return Result.Ok("Đã ghi nhận hoàn tiền.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",ex.Message);}
    }

    public async Task<DashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        var bookings=await SearchBookingsAsync(null,DateTime.Today.AddMonths(-6),DateTime.Today.AddMonths(2),null,ct);
        var today=bookings.Count(x=>x.BatDau.Date==DateTime.Today&&x.TrangThai!=TrangThaiLich.DaHuy);
        var upcoming=bookings.Count(x=>x.BatDau>DateTime.Now&&x.TrangThai==TrangThaiLich.DaDatLich);
        var processing=bookings.Count(x=>x.TrangThai is TrangThaiLich.DaChup or TrangThaiLich.DangChinhSuaAnh);
        var waiting=bookings.Count(x=>x.TrangThai==TrangThaiLich.ChoGiaoAnh);
        var debt=bookings.Where(x=>x.TrangThai!=TrangThaiLich.DaHuy).Sum(x=>x.ConLai);
        var statuses=bookings.GroupBy(x=>x.TrangThai.HienThi()).ToDictionary(x=>x.Key,x=>x.Count());
        await using var cn=Connection();await cn.OpenAsync(ct);var revenue=new List<(string,decimal)>();
        const string sql="SELECT FORMAT(NgayGiaoDich,'MM/yyyy'),SUM(SoTien) FROM ThanhToan WHERE NgayGiaoDich>=DATEADD(month,-5,DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1)) GROUP BY FORMAT(NgayGiaoDich,'MM/yyyy'),YEAR(NgayGiaoDich),MONTH(NgayGiaoDich) ORDER BY YEAR(NgayGiaoDich),MONTH(NgayGiaoDich)";
        await using(var c=new SqlCommand(sql,cn))await using(var r=await c.ExecuteReaderAsync(ct))while(await r.ReadAsync(ct))revenue.Add((r.GetString(0),r.GetDecimal(1)));
        return new(today,upcoming,processing,waiting,debt,bookings.Where(x=>x.BatDau>=DateTime.Today).Take(8).ToList(),statuses,revenue);
    }

    public async Task<ReportData> GetReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COALESCE(SUM(l.GiaGoiChot + COALESCE(dv.TienDichVu, 0) - l.TienGiam), 0)
            FROM LichChup l LEFT JOIN (SELECT LichChupId, SUM(SoLuong * DonGiaChot) TienDichVu FROM LichChupDichVu GROUP BY LichChupId) dv ON dv.LichChupId=l.LichChupId
            WHERE l.TrangThai='HOAN_THANH' AND l.HoanThanhLuc>=@F AND l.HoanThanhLuc<DATEADD(day,1,@T);
            SELECT COALESCE(SUM(SoTien),0) FROM ThanhToan WHERE NgayGiaoDich>=@F AND NgayGiaoDich<DATEADD(day,1,@T);
            SELECT COALESCE(SUM(SoTien),0) FROM HoanTien WHERE NgayGiaoDich>=@F AND NgayGiaoDich<DATEADD(day,1,@T);
            SELECT COALESCE(SUM(l.GiaGoiChot + COALESCE(dv.TienDichVu,0) - l.TienGiam - COALESCE(tt.DaThu,0)),0)
            FROM LichChup l LEFT JOIN (SELECT LichChupId,SUM(SoLuong*DonGiaChot) TienDichVu FROM LichChupDichVu GROUP BY LichChupId) dv ON dv.LichChupId=l.LichChupId
              LEFT JOIN (SELECT LichChupId,SUM(SoTien) DaThu FROM ThanhToan GROUP BY LichChupId) tt ON tt.LichChupId=l.LichChupId WHERE l.TrangThai<>'DA_HUY';
            SELECT COUNT(1) FROM LichChup WHERE BatDau>=@F AND BatDau<DATEADD(day,1,@T);
            SELECT COUNT(1) FROM LichChup WHERE TrangThai='HOAN_THANH' AND HoanThanhLuc>=@F AND HoanThanhLuc<DATEADD(day,1,@T);
            SELECT COUNT(1) FROM LichChup WHERE TrangThai='DA_HUY' AND HuyLuc>=@F AND HuyLuc<DATEADD(day,1,@T);
            SELECT TenGoiChot, COUNT(1) FROM LichChup WHERE BatDau>=@F AND BatDau<DATEADD(day,1,@T) GROUP BY TenGoiChot ORDER BY COUNT(1) DESC;";
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@F", from.Date); cmd.Parameters.AddWithValue("@T", to.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        async Task<decimal> DecimalAsync() { await reader.ReadAsync(ct); var value = reader.GetDecimal(0); await reader.NextResultAsync(ct); return value; }
        async Task<int> CountAsync() { await reader.ReadAsync(ct); var value = reader.GetInt32(0); await reader.NextResultAsync(ct); return value; }
        var revenue = await DecimalAsync(); var collected = await DecimalAsync(); var refunded = await DecimalAsync(); var debt = await DecimalAsync();
        var totalBookings = await CountAsync(); var completed = await CountAsync(); var cancelled = await CountAsync();
        var byPackage = new List<(string Name, decimal Value)>();
        while (await reader.ReadAsync(ct)) byPackage.Add((reader.GetString(0), reader.GetInt32(1)));
        return new(revenue, collected, refunded, collected-refunded, debt, totalBookings, completed, cancelled, byPackage);
    }

    public async Task<IReadOnlyList<LookupItem>> GetLookupsAsync(string type, bool activeOnly=true, CancellationToken ct=default)
    {
        var (sql,state)=type switch
        {
            "KhachHang"=>("SELECT KhachHangId,MaKhachHang,HoTen,SoDienThoai FROM KhachHang ORDER BY HoTen",(string?)null),
            "GoiChup"=>("SELECT GoiChupId,MaGoiChup,TenGoiChup,FORMAT(GiaGoi,'N0') FROM GoiChup WHERE (@Active=0 OR TrangThai='DANG_AP_DUNG') ORDER BY TenGoiChup","active"),
            "NhiepAnhGia"=>("SELECT NhanVienId,MaNhanVien,HoTen,SoDienThoai FROM NhanVien WHERE ChucVu='NHIEP_ANH_GIA' AND (@Active=0 OR TrangThai='DANG_LAM') ORDER BY HoTen","active"),
            "PhongChup"=>("SELECT PhongChupId,MaPhong,TenPhong,MoTa FROM PhongChup WHERE (@Active=0 OR TrangThai='HOAT_DONG') ORDER BY TenPhong","active"),
            "DichVu"=>("SELECT DichVuId,MaDichVu,TenDichVu,FORMAT(DonGia,'N0') FROM DichVu WHERE (@Active=0 OR TrangThai='DANG_CUNG_CAP') ORDER BY TenDichVu","active"),
            "TaiNguyen"=>("SELECT TaiNguyenId,MaTaiNguyen,TenTaiNguyen,CONVERT(varchar(20),TongSoLuong) FROM TaiNguyen WHERE (@Active=0 OR TrangThai='HOAT_DONG') ORDER BY TenTaiNguyen","active"),
            "NhanVien"=>("SELECT NhanVienId,MaNhanVien,HoTen,SoDienThoai FROM NhanVien WHERE (@Active=0 OR TrangThai='DANG_LAM') ORDER BY HoTen","active"),
            _=>throw new ArgumentOutOfRangeException(nameof(type))
        };
        await using var cn=Connection();await cn.OpenAsync(ct);await using var cmd=new SqlCommand(sql,cn);if(state!=null)cmd.Parameters.AddWithValue("@Active",activeOnly);var list=new List<LookupItem>();await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))list.Add(new(Convert.ToInt64(r.GetValue(0)),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetValue(3).ToString()));return list;
    }

    public async Task<IReadOnlyList<IDictionary<string,object?>>> QueryGridAsync(string entity,string? keyword,CancellationToken ct=default)
    {
        var sql=entity switch
        {
            "KhachHang"=>"SELECT KhachHangId AS Id,MaKhachHang AS [Mã],HoTen AS [Họ tên],SoDienThoai AS [Điện thoại],Email,DiaChi AS [Địa chỉ],GhiChu AS [Ghi chú] FROM KhachHang WHERE @Q IS NULL OR MaKhachHang LIKE '%'+@Q+'%' OR HoTen LIKE N'%'+@Q+'%' OR SoDienThoai LIKE '%'+@Q+'%' ORDER BY KhachHangId DESC",
            "NhanVien"=>"SELECT NhanVienId AS Id,MaNhanVien AS [Mã],HoTen AS [Họ tên],SoDienThoai AS [Điện thoại],ChucVu AS [Chức vụ],TrangThai AS [Trạng thái],GhiChu AS [Ghi chú] FROM NhanVien WHERE @Q IS NULL OR MaNhanVien LIKE '%'+@Q+'%' OR HoTen LIKE N'%'+@Q+'%' ORDER BY NhanVienId DESC",
            "GoiChup"=>"SELECT GoiChupId AS Id,MaGoiChup AS [Mã],TenGoiChup AS [Tên gói],GiaGoi AS [Giá],ThoiLuongPhut AS [Thời lượng],TrangThai AS [Trạng thái],MoTa AS [Mô tả] FROM GoiChup WHERE @Q IS NULL OR MaGoiChup LIKE '%'+@Q+'%' OR TenGoiChup LIKE N'%'+@Q+'%' ORDER BY GoiChupId DESC",
            "DichVu"=>"SELECT DichVuId AS Id,MaDichVu AS [Mã],TenDichVu AS [Tên dịch vụ],DonViTinh AS [Đơn vị],DonGia AS [Đơn giá],TrangThai AS [Trạng thái],MoTa AS [Mô tả] FROM DichVu WHERE @Q IS NULL OR MaDichVu LIKE '%'+@Q+'%' OR TenDichVu LIKE N'%'+@Q+'%' ORDER BY DichVuId DESC",
            "PhongChup"=>"SELECT PhongChupId AS Id,MaPhong AS [Mã],TenPhong AS [Tên phòng],TrangThai AS [Trạng thái],MoTa AS [Mô tả] FROM PhongChup WHERE @Q IS NULL OR MaPhong LIKE '%'+@Q+'%' OR TenPhong LIKE N'%'+@Q+'%' ORDER BY PhongChupId DESC",
            "TaiNguyen"=>"SELECT TaiNguyenId AS Id,MaTaiNguyen AS [Mã],TenTaiNguyen AS [Tên tài nguyên],LoaiTaiNguyen AS [Loại],TongSoLuong AS [Số lượng],TrangThai AS [Trạng thái],GhiChu AS [Ghi chú] FROM TaiNguyen WHERE @Q IS NULL OR MaTaiNguyen LIKE '%'+@Q+'%' OR TenTaiNguyen LIKE N'%'+@Q+'%' ORDER BY TaiNguyenId DESC",
            "TaiKhoan"=>"SELECT TaiKhoanId AS Id,TenDangNhap AS [Tên đăng nhập],VaiTro AS [Vai trò],TrangThai AS [Trạng thái],PhaiDoiMatKhau AS [Đổi mật khẩu],LanDangNhapCuoi AS [Đăng nhập cuối] FROM TaiKhoan WHERE @Q IS NULL OR TenDangNhap LIKE '%'+@Q+'%' ORDER BY TaiKhoanId DESC",
            "NhatKy"=>"SELECT TOP 500 NhatKyId AS Id,ThoiDiem AS [Thời điểm],tk.TenDangNhap AS [Tài khoản],HanhDong AS [Hành động],LoaiDoiTuong AS [Đối tượng],DoiTuongId AS [Mã đối tượng],LyDo AS [Lý do] FROM NhatKyHeThong n JOIN TaiKhoan tk ON tk.TaiKhoanId=n.TaiKhoanId WHERE @Q IS NULL OR HanhDong LIKE '%'+@Q+'%' OR DoiTuongId LIKE '%'+@Q+'%' ORDER BY NhatKyId DESC",
            _=>throw new ArgumentOutOfRangeException(nameof(entity))
        };
        await using var cn=Connection();await cn.OpenAsync(ct);await using var cmd=new SqlCommand(sql,cn);AddNullable(cmd,"@Q",keyword);var rows=new List<IDictionary<string,object?>>();await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct)){var d=new Dictionary<string,object?>();for(int i=0;i<r.FieldCount;i++)d[r.GetName(i)]=r.IsDBNull(i)?null:r.GetValue(i);rows.Add(d);}return rows;
    }

    public async Task<Result> SaveSimpleAsync(string entity,long? id,IReadOnlyDictionary<string,object?> v,UserSession user,CancellationToken ct=default)
    {
        try
        {
            if(user.VaiTro!=VaiTro.QuanTriVien&&entity!="KhachHang")return Result.Fail("FORBIDDEN","Bạn không có quyền thay đổi danh mục này.");
            var spec=SimpleEntitySpec.For(entity);await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);
            var columns=spec.Columns.Where(c=>v.ContainsKey(c)).ToList();string sql;
            if(id is null){var code=spec.Prefix+Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();sql=$"INSERT {entity}({spec.CodeColumn},{string.Join(',',columns)},CreatedAt,UpdatedAt) VALUES(@Code,{string.Join(',',columns.Select(x=>"@"+x))},GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint)";}
            else sql=$"UPDATE {entity} SET {string.Join(',',columns.Select(x=>$"{x}=@{x}"))},UpdatedAt=GETDATE() WHERE {spec.IdColumn}=@Id; SELECT CAST(@Id AS bigint)";
            await using var cmd=new SqlCommand(sql,cn,tx);if(id is null)cmd.Parameters.AddWithValue("@Code",spec.Prefix+Guid.NewGuid().ToString("N")[..6].ToUpperInvariant());else cmd.Parameters.AddWithValue("@Id",id.Value);foreach(var col in columns)cmd.Parameters.AddWithValue("@"+col,v[col]??DBNull.Value);var saved=Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));await AuditAsync(cn,tx,user,id is null?"THEM_DANH_MUC":"SUA_DANH_MUC",entity,saved.ToString(),null,JsonSerializer.Serialize(v),null,ct);await tx.CommitAsync(ct);return Result.Ok("Đã lưu dữ liệu.");
        }catch(Exception ex){return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> DeactivateAsync(string entity,long id,UserSession user,CancellationToken ct=default)
    {
        if(user.VaiTro!=VaiTro.QuanTriVien&&entity!="KhachHang")return Result.Fail("FORBIDDEN","Không đủ quyền.");
        if(entity=="KhachHang")
        {
            try{await using var customerCn=Connection();await customerCn.OpenAsync(ct);await using var customerCmd=new SqlCommand("DELETE FROM KhachHang WHERE KhachHangId=@Id AND NOT EXISTS(SELECT 1 FROM LichChup WHERE KhachHangId=@Id)",customerCn);customerCmd.Parameters.AddWithValue("@Id",id);return await customerCmd.ExecuteNonQueryAsync(ct)==1?Result.Ok("Đã xóa khách hàng."):Result.Fail("HAS_HISTORY","Khách hàng đã có lịch sử nên không thể xóa.");}catch(Exception ex){return Result.Fail("DB_ERROR",Friendly(ex));}
        }
        if(entity=="TaiKhoan")
        {
            try{await using var accountCn=Connection();await accountCn.OpenAsync(ct);await using var accountCmd=new SqlCommand("UPDATE TaiKhoan SET TrangThai=CASE WHEN TrangThai='HOAT_DONG' THEN 'BI_KHOA' ELSE 'HOAT_DONG' END,UpdatedAt=GETDATE() WHERE TaiKhoanId=@Id",accountCn);accountCmd.Parameters.AddWithValue("@Id",id);await accountCmd.ExecuteNonQueryAsync(ct);return Result.Ok("Đã thay đổi trạng thái tài khoản.");}catch(Exception ex){return Result.Fail("DB_ERROR",Friendly(ex));}
        }
        var map=entity switch{"NhanVien"=>("NhanVienId","TrangThai","NGUNG_LAM"),"GoiChup"=>("GoiChupId","TrangThai","NGUNG_AP_DUNG"),"DichVu"=>("DichVuId","TrangThai","NGUNG_CUNG_CAP"),"PhongChup"=>("PhongChupId","TrangThai","NGUNG_SU_DUNG"),"TaiNguyen"=>("TaiNguyenId","TrangThai","NGUNG_SU_DUNG"),_=>throw new ArgumentOutOfRangeException(nameof(entity))};
        try{await using var cn=Connection();await cn.OpenAsync(ct);await using var cmd=new SqlCommand($"UPDATE {entity} SET {map.Item2}=@S,UpdatedAt=GETDATE() WHERE {map.Item1}=@Id",cn);cmd.Parameters.AddWithValue("@S",map.Item3);cmd.Parameters.AddWithValue("@Id",id);await cmd.ExecuteNonQueryAsync(ct);return Result.Ok("Đã ngừng sử dụng dữ liệu.");}catch(Exception ex){return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> CreateAccountAsync(string username,string passwordHash,int? employeeId,VaiTro role,UserSession user,CancellationToken ct=default)
    {
        if(user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Không đủ quyền.");
        try{await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);await using var cmd=new SqlCommand("INSERT TaiKhoan(NhanVienId,TenDangNhap,MatKhauHash,VaiTro,TrangThai,PhaiDoiMatKhau,CreatedAt,UpdatedAt) VALUES(@N,@U,@H,@R,'HOAT_DONG',1,GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS int)",cn,tx);AddNullable(cmd,"@N",employeeId);cmd.Parameters.AddWithValue("@U",username);cmd.Parameters.AddWithValue("@H",passwordHash);cmd.Parameters.AddWithValue("@R",role==VaiTro.QuanTriVien?"QUAN_TRI_VIEN":"NHAN_VIEN");var accountId=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));await AuditAsync(cn,tx,user,"TAO_TAI_KHOAN","TaiKhoan",accountId.ToString(),null,JsonSerializer.Serialize(new{username,employeeId,role}),null,ct);await tx.CommitAsync(ct);return Result.Ok("Đã tạo tài khoản.");}catch(Exception ex){return Result.Fail("DB_ERROR",Friendly(ex));}
    }
    public async Task<Result> ResetPasswordAsync(int accountId,string passwordHash,UserSession user,CancellationToken ct=default)
    {
        if(user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Không đủ quyền.");
        try{await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);await using var cmd=new SqlCommand("UPDATE TaiKhoan SET MatKhauHash=@H,PhaiDoiMatKhau=1,UpdatedAt=GETDATE() WHERE TaiKhoanId=@Id",cn,tx);cmd.Parameters.AddWithValue("@H",passwordHash);cmd.Parameters.AddWithValue("@Id",accountId);if(await cmd.ExecuteNonQueryAsync(ct)!=1)return Result.Fail("NOT_FOUND","Không tìm thấy tài khoản.");await AuditAsync(cn,tx,user,"RESET_MAT_KHAU","TaiKhoan",accountId.ToString(),null,"PhaiDoiMatKhau=1",null,ct);await tx.CommitAsync(ct);return Result.Ok("Đã đặt lại mật khẩu.");}catch(Exception ex){return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<IReadOnlyList<IDictionary<string,object?>>> GetBookingChildrenAsync(long bookingId,string type,CancellationToken ct=default)
    {
        var sql=type switch
        {
            "DichVu"=>"SELECT l.LichChupDichVuId AS Id,l.TenDichVuChot AS [Dịch vụ],l.SoLuong AS [Số lượng],l.DonViTinhChot AS [Đơn vị],l.DonGiaChot AS [Đơn giá],l.SoLuong*l.DonGiaChot AS [Thành tiền] FROM LichChupDichVu l WHERE l.LichChupId=@Id ORDER BY l.LichChupDichVuId",
            "TaiNguyen"=>"SELECT p.PhanCongId AS Id,t.TenTaiNguyen AS [Tài nguyên],p.SoLuong AS [Số lượng],p.BatDauSuDung AS [Bắt đầu],p.KetThucSuDung AS [Kết thúc],p.TrangThai AS [Trạng thái] FROM PhanCongTaiNguyen p JOIN TaiNguyen t ON t.TaiNguyenId=p.TaiNguyenId WHERE p.LichChupId=@Id ORDER BY p.PhanCongId",
            "ThanhToan"=>"SELECT ThanhToanId AS Id,MaThanhToan AS [Mã],LoaiThu AS [Loại],SoTien AS [Số tiền],NgayGiaoDich AS [Ngày],GhiChu AS [Ghi chú] FROM ThanhToan WHERE LichChupId=@Id ORDER BY NgayGiaoDich",
            "HoanTien"=>"SELECT HoanTienId AS Id,MaHoanTien AS [Mã],SoTien AS [Số tiền],NgayGiaoDich AS [Ngày],LyDo AS [Lý do] FROM HoanTien WHERE LichChupId=@Id ORDER BY NgayGiaoDich",
            _=>throw new ArgumentOutOfRangeException(nameof(type))
        };
        await using var cn=Connection();await cn.OpenAsync(ct);await using var cmd=new SqlCommand(sql,cn);cmd.Parameters.AddWithValue("@Id",bookingId);var list=new List<IDictionary<string,object?>>();await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct)){var d=new Dictionary<string,object?>();for(var i=0;i<r.FieldCount;i++)d[r.GetName(i)]=r.IsDBNull(i)?null:r.GetValue(i);list.Add(d);}return list;
    }

    public async Task<Result> AddBookingServiceAsync(long bookingId,int serviceId,decimal quantity,UserSession user,CancellationToken ct=default)
    {
        if(quantity<=0)return Result.Fail("INVALID_QUANTITY","Số lượng phải lớn hơn 0.");
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string precheck=@"SELECT l.TrangThai,l.GiaGoiChot+COALESCE((SELECT SUM(x.SoLuong*x.DonGiaChot) FROM LichChupDichVu x WHERE x.LichChupId=l.LichChupId AND x.DichVuId<>@D),0)+@Q*COALESCE((SELECT x.DonGiaChot FROM LichChupDichVu x WHERE x.LichChupId=l.LichChupId AND x.DichVuId=@D),d.DonGia)-l.TienGiam,COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE LichChupId=l.LichChupId),0),d.TrangThai,CASE WHEN EXISTS(SELECT 1 FROM LichChupDichVu x WHERE x.LichChupId=l.LichChupId AND x.DichVuId=@D) THEN 1 ELSE 0 END FROM LichChup l WITH(UPDLOCK,HOLDLOCK) CROSS JOIN DichVu d WHERE l.LichChupId=@L AND d.DichVuId=@D";
            await using(var pre=new SqlCommand(precheck,cn,tx)){pre.Parameters.AddWithValue("@L",bookingId);pre.Parameters.AddWithValue("@D",serviceId);pre.Parameters.AddWithValue("@Q",quantity);await using var reader=await pre.ExecuteReaderAsync(ct);if(!await reader.ReadAsync(ct))return Result.Fail("NOT_FOUND","Không tìm thấy lịch hoặc dịch vụ.");var state=reader.GetString(0);var proposed=reader.GetDecimal(1);var paid=reader.GetDecimal(2);var active=reader.GetString(3)=="DANG_CUNG_CAP";var exists=reader.GetInt32(4)==1;if(state is "DA_HUY" or "HOAN_THANH")return Result.Fail("INVALID_STATE","Không thể thêm dịch vụ cho lịch đã kết thúc.");if(!exists&&!active)return Result.Fail("INACTIVE_SERVICE","Dịch vụ đã ngừng cung cấp, không thể thêm mới.");if(proposed<paid)return Result.Fail("LIMIT","Thao tác làm tổng thanh toán thấp hơn số tiền đã thu.");}
            const string sql=@"IF EXISTS(SELECT 1 FROM LichChupDichVu WHERE LichChupId=@L AND DichVuId=@D)
              UPDATE LichChupDichVu SET SoLuong=@Q,UpdatedAt=GETDATE() WHERE LichChupId=@L AND DichVuId=@D;
              ELSE INSERT LichChupDichVu(LichChupId,DichVuId,TenDichVuChot,DonViTinhChot,DonGiaChot,SoLuong,CreatedBy,CreatedAt,UpdatedAt)
              SELECT @L,d.DichVuId,d.TenDichVu,d.DonViTinh,d.DonGia,@Q,@U,GETDATE(),GETDATE() FROM DichVu d WHERE d.DichVuId=@D AND d.TrangThai='DANG_CUNG_CAP';";
            await using(var c=new SqlCommand(sql,cn,tx)){c.Parameters.AddWithValue("@L",bookingId);c.Parameters.AddWithValue("@D",serviceId);c.Parameters.AddWithValue("@Q",quantity);c.Parameters.AddWithValue("@U",user.TaiKhoanId);if(await c.ExecuteNonQueryAsync(ct)==0)return Result.Fail("INACTIVE_SERVICE","Dịch vụ đã ngừng cung cấp, không thể thêm mới.");}await AuditAsync(cn,tx,user,"CAP_NHAT_DICH_VU","LichChup",bookingId.ToString(),null,$"DV={serviceId};SL={quantity}",null,ct);await tx.CommitAsync(ct);return Result.Ok("Đã cập nhật dịch vụ phát sinh.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> RemoveBookingServiceAsync(long bookingId,long bookingServiceId,UserSession user,CancellationToken ct=default)
    {
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string check=@"SELECT l.TrangThai,l.GiaGoiChot+COALESCE((SELECT SUM(x.SoLuong*x.DonGiaChot) FROM LichChupDichVu x WHERE x.LichChupId=l.LichChupId AND x.LichChupDichVuId<>@Line),0)-l.TienGiam,COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE LichChupId=l.LichChupId),0),CONCAT(s.DichVuId,'|',s.SoLuong,'|',s.DonGiaChot) FROM LichChup l JOIN LichChupDichVu s WITH(UPDLOCK,HOLDLOCK) ON s.LichChupId=l.LichChupId WHERE l.LichChupId=@Booking AND s.LichChupDichVuId=@Line";
            await using var cmd=new SqlCommand(check,cn,tx);cmd.Parameters.AddWithValue("@Booking",bookingId);cmd.Parameters.AddWithValue("@Line",bookingServiceId);await using var reader=await cmd.ExecuteReaderAsync(ct);
            if(!await reader.ReadAsync(ct))return Result.Fail("NOT_FOUND","Không tìm thấy dòng dịch vụ.");var state=reader.GetString(0);var proposed=reader.GetDecimal(1);var paid=reader.GetDecimal(2);var old=reader.GetString(3);await reader.CloseAsync();
            if(state is "DA_HUY" or "HOAN_THANH")return Result.Fail("INVALID_STATE","Không thể sửa dịch vụ của lịch đã kết thúc.");
            if(proposed<paid)return Result.Fail("LIMIT","Không thể xóa vì tổng thanh toán mới thấp hơn số tiền đã thu.");
            await using var delete=new SqlCommand("DELETE FROM LichChupDichVu WHERE LichChupId=@Booking AND LichChupDichVuId=@Line",cn,tx);delete.Parameters.AddWithValue("@Booking",bookingId);delete.Parameters.AddWithValue("@Line",bookingServiceId);if(await delete.ExecuteNonQueryAsync(ct)!=1)return Result.Fail("NOT_FOUND","Không tìm thấy dòng dịch vụ.");
            await AuditAsync(cn,tx,user,"XOA_DICH_VU","LichChup",bookingId.ToString(),old,null,null,ct);await tx.CommitAsync(ct);return Result.Ok("Đã xóa dịch vụ phát sinh.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> SetDiscountAsync(long bookingId,decimal amount,string? reason,UserSession user,CancellationToken ct=default)
    {
        if(amount<0||amount>0&&string.IsNullOrWhiteSpace(reason))return Result.Fail("INVALID","Giảm giá phải hợp lệ và có lý do.");
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try{var old=await ScalarAsync<string>(cn,tx,"SELECT CONCAT(TienGiam,'|',COALESCE(LyDoGiam,'')) FROM LichChup WITH(UPDLOCK,HOLDLOCK) WHERE LichChupId=@Id",bookingId,ct);if(old is null)return Result.Fail("NOT_FOUND","Không tìm thấy lịch.");const string sql=@"UPDATE l SET TienGiam=@A,LyDoGiam=@R,UpdatedBy=@U,UpdatedAt=GETDATE() FROM LichChup l CROSS APPLY(SELECT l.GiaGoiChot+COALESCE((SELECT SUM(SoLuong*DonGiaChot) FROM LichChupDichVu WHERE LichChupId=l.LichChupId),0) TamTinh) x CROSS APPLY(SELECT COALESCE((SELECT SUM(SoTien) FROM ThanhToan WHERE LichChupId=l.LichChupId),0) DaThu) y WHERE l.LichChupId=@Id AND @A BETWEEN 0 AND x.TamTinh AND x.TamTinh-@A>=y.DaThu AND l.TrangThai NOT IN('DA_HUY','HOAN_THANH')";await using var cmd=new SqlCommand(sql,cn,tx);cmd.Parameters.AddWithValue("@A",amount);AddNullable(cmd,"@R",reason);cmd.Parameters.AddWithValue("@U",user.TaiKhoanId);cmd.Parameters.AddWithValue("@Id",bookingId);var n=await cmd.ExecuteNonQueryAsync(ct);if(n!=1)return Result.Fail("LIMIT","Giảm giá không hợp lệ, làm tổng tiền thấp hơn đã thu hoặc lịch đã kết thúc.");await AuditAsync(cn,tx,user,"CAP_NHAT_GIAM_GIA","LichChup",bookingId.ToString(),old,$"{amount}|{reason}",reason,ct);await tx.CommitAsync(ct);return Result.Ok("Đã cập nhật giảm giá.");}catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> AssignResourceAsync(long bookingId,int resourceId,int quantity,UserSession user,CancellationToken ct=default)
    {
        if(quantity<=0)return Result.Fail("INVALID_QUANTITY","Số lượng phải lớn hơn 0.");await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            const string q=@"SELECT l.BatDau,l.KetThuc,t.TongSoLuong-COALESCE((SELECT SUM(p.SoLuong) FROM PhanCongTaiNguyen p WHERE p.TaiNguyenId=t.TaiNguyenId AND p.TrangThai='DA_PHAN_CONG' AND p.BatDauSuDung<l.KetThuc AND p.KetThucSuDung>l.BatDau),0) ConLai FROM LichChup l CROSS JOIN TaiNguyen t WITH(UPDLOCK,HOLDLOCK) WHERE l.LichChupId=@L AND t.TaiNguyenId=@T AND t.TrangThai='HOAT_DONG' AND l.TrangThai NOT IN('DA_HUY','HOAN_THANH')";
            DateTime start,end;int available;await using(var c=new SqlCommand(q,cn,tx)){c.Parameters.AddWithValue("@L",bookingId);c.Parameters.AddWithValue("@T",resourceId);await using var r=await c.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))return Result.Fail("NOT_FOUND","Lịch hoặc tài nguyên không khả dụng.");start=r.GetDateTime(0);end=r.GetDateTime(1);available=r.GetInt32(2);}if(quantity>available)return Result.Fail("RESOURCE_LIMIT",$"Chỉ còn {available} tài nguyên khả dụng.");
            const string ins="INSERT PhanCongTaiNguyen(LichChupId,TaiNguyenId,SoLuong,BatDauSuDung,KetThucSuDung,TrangThai,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt) VALUES(@L,@T,@Q,@S,@E,'DA_PHAN_CONG',@U,GETDATE(),@U,GETDATE())";await using(var c=new SqlCommand(ins,cn,tx)){c.Parameters.AddWithValue("@L",bookingId);c.Parameters.AddWithValue("@T",resourceId);c.Parameters.AddWithValue("@Q",quantity);c.Parameters.AddWithValue("@S",start);c.Parameters.AddWithValue("@E",end);c.Parameters.AddWithValue("@U",user.TaiKhoanId);await c.ExecuteNonQueryAsync(ct);}await AuditAsync(cn,tx,user,"PHAN_CONG_TAI_NGUYEN","LichChup",bookingId.ToString(),null,$"TN={resourceId};SL={quantity}",null,ct);await tx.CommitAsync(ct);return Result.Ok("Đã phân công tài nguyên.");
        }catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> UpdateResourceAssignmentAsync(long assignmentId,TrangThaiPhanCong next,UserSession user,CancellationToken ct=default)
    {
        if(next==TrangThaiPhanCong.DaPhanCong)return Result.Fail("INVALID_STATE","Chỉ có thể trả hoặc hủy phân công.");
        var dbNext=next==TrangThaiPhanCong.DaTra?"DA_TRA":"DA_HUY";
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);
        try{const string q="SELECT LichChupId,TrangThai FROM PhanCongTaiNguyen WITH(UPDLOCK,HOLDLOCK) WHERE PhanCongId=@Id";await using var read=new SqlCommand(q,cn,tx);read.Parameters.AddWithValue("@Id",assignmentId);await using var reader=await read.ExecuteReaderAsync(ct);if(!await reader.ReadAsync(ct))return Result.Fail("NOT_FOUND","Không tìm thấy phân công tài nguyên.");var bookingId=reader.GetInt64(0);var current=reader.GetString(1);await reader.CloseAsync();if(current!="DA_PHAN_CONG")return Result.Fail("INVALID_STATE","Phân công này đã được xử lý.");await ExecuteAsync(cn,tx,"UPDATE PhanCongTaiNguyen SET TrangThai=@Value,UpdatedBy=@User,UpdatedAt=GETDATE() WHERE PhanCongId=@Id",assignmentId,user.TaiKhoanId,dbNext,ct);await AuditAsync(cn,tx,user,dbNext=="DA_TRA"?"TRA_TAI_NGUYEN":"HUY_PHAN_CONG","PhanCongTaiNguyen",assignmentId.ToString(),current,dbNext,null,ct);await tx.CommitAsync(ct);return Result.Ok(dbNext=="DA_TRA"?"Đã trả tài nguyên.":"Đã hủy phân công tài nguyên.");}catch(Exception ex){await tx.RollbackAsync(ct);return Result.Fail("DB_ERROR",Friendly(ex));}
    }

    public async Task<Result> BackupAsync(string path,UserSession user,CancellationToken ct=default)
    {
        if(user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Chỉ Quản trị viên được sao lưu dữ liệu.");
        var db=new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if(string.IsNullOrWhiteSpace(db))return Result.Fail("CONFIG","Chuỗi kết nối chưa có tên cơ sở dữ liệu.");
        var safeDb=db.Replace("]","]]",StringComparison.Ordinal);
        var result=await AdminDatabaseAsync($"BACKUP DATABASE [{safeDb}] TO DISK=@Path WITH INIT,COMPRESSION",path,ct);
        await LogBackupAsync("SAO_LUU",path,result,user,ct);
        return result;
    }
    public async Task<Result> RestoreAsync(string path,UserSession user,CancellationToken ct=default)
    {
        if(user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Chỉ Quản trị viên được phục hồi dữ liệu.");
        var builder=new SqlConnectionStringBuilder(connectionString);var db=builder.InitialCatalog;if(string.IsNullOrWhiteSpace(db))return Result.Fail("CONFIG","Chuỗi kết nối chưa có tên cơ sở dữ liệu.");builder.InitialCatalog="master";var safeDb=db.Replace("]","]]",StringComparison.Ordinal);
        try
        {
            SqlConnection.ClearAllPools();await using var cn=new SqlConnection(builder.ConnectionString);await cn.OpenAsync(ct);
            var sql=$"ALTER DATABASE [{safeDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [{safeDb}] FROM DISK=@Path WITH REPLACE,RECOVERY; ALTER DATABASE [{safeDb}] SET MULTI_USER;";
            await using var cmd=new SqlCommand(sql,cn){CommandTimeout=600};cmd.Parameters.AddWithValue("@Path",path);await cmd.ExecuteNonQueryAsync(ct);SqlConnection.ClearAllPools();var result=Result.Ok("Phục hồi thành công. Ứng dụng cần được khởi động lại.");await LogBackupAsync("PHUC_HOI",path,result,user,ct);return result;
        }
        catch(Exception ex)
        {
            try{await using var cn=new SqlConnection(builder.ConnectionString);await cn.OpenAsync(ct);await using var recover=new SqlCommand($"IF DB_ID(@Db) IS NOT NULL ALTER DATABASE [{safeDb}] SET MULTI_USER",cn);recover.Parameters.AddWithValue("@Db",db);await recover.ExecuteNonQueryAsync(ct);}catch{ }
            var result=Result.Fail("RESTORE_FAILED",Friendly(ex));await LogBackupAsync("PHUC_HOI",path,result,user,ct);return result;
        }
    }

    private async Task<Result> AdminDatabaseAsync(string sql,string path,CancellationToken ct)
    {try{await using var cn=Connection();await cn.OpenAsync(ct);await using var cmd=new SqlCommand(sql,cn){CommandTimeout=300};cmd.Parameters.AddWithValue("@Path",path);await cmd.ExecuteNonQueryAsync(ct);return Result.Ok("Thao tác cơ sở dữ liệu đã hoàn tất.");}catch(Exception ex){return Result.Fail("DB_ADMIN",Friendly(ex));}}
    private async Task LogBackupAsync(string action,string path,Result result,UserSession user,CancellationToken ct)
    {
        try{await using var cn=Connection();await cn.OpenAsync(ct);await using var cmd=new SqlCommand("INSERT NhatKySaoLuu(LoaiThaoTac,DuongDanTep,TrangThai,ThongBao,TaiKhoanId,ThoiDiem) VALUES(@A,@P,@S,@M,@U,GETDATE())",cn);cmd.Parameters.AddWithValue("@A",action);cmd.Parameters.AddWithValue("@P",path);cmd.Parameters.AddWithValue("@S",result.Success?"THANH_CONG":"THAT_BAI");cmd.Parameters.AddWithValue("@M",result.Message);cmd.Parameters.AddWithValue("@U",user.TaiKhoanId);await cmd.ExecuteNonQueryAsync(ct);}catch{ /* A failed log must not turn a completed SQL backup into a false failure. */ }
    }
    private static LichChup MapBooking(SqlDataReader r)=>new(r.GetInt64(0),r.GetString(1),r.GetInt64(2),r.GetString(3),r.GetInt32(4),r.GetString(5),r.GetDecimal(6),r.GetDateTime(7),r.GetDateTime(8),r.GetInt32(9),r.GetString(10),r.GetInt32(11),r.GetString(12),DomainStatus(r.GetString(13)),r.GetDecimal(14),r.IsDBNull(15)?null:r.GetString(15),r.IsDBNull(16)?null:r.GetString(16),r.GetDecimal(17),r.GetDecimal(18),r.GetDecimal(19));
    private static void AddNullable(SqlCommand c,string n,object? value)=>c.Parameters.AddWithValue(n,value??DBNull.Value);
    private static async Task AuditAsync(SqlConnection cn,SqlTransaction tx,UserSession u,string action,string type,string id,string? oldValue,string? newValue,string? reason,CancellationToken ct){await using var c=new SqlCommand("INSERT NhatKyHeThong(TaiKhoanId,HanhDong,LoaiDoiTuong,DoiTuongId,GiaTriCu,GiaTriMoi,LyDo,ThoiDiem) VALUES(@U,@A,@T,@I,@O,@N,@R,GETDATE())",cn,tx);c.Parameters.AddWithValue("@U",u.TaiKhoanId);c.Parameters.AddWithValue("@A",action);c.Parameters.AddWithValue("@T",type);c.Parameters.AddWithValue("@I",id);c.Parameters.AddWithValue("@O",(object?)oldValue??DBNull.Value);c.Parameters.AddWithValue("@N",(object?)newValue??DBNull.Value);c.Parameters.AddWithValue("@R",(object?)reason??DBNull.Value);await c.ExecuteNonQueryAsync(ct);}
    private static async Task<T?> ScalarAsync<T>(SqlConnection cn,SqlTransaction tx,string sql,long id,CancellationToken ct){await using var c=new SqlCommand(sql,cn,tx);c.Parameters.AddWithValue("@Id",id);var v=await c.ExecuteScalarAsync(ct);return v is null or DBNull?default:(T)v;}
    private static async Task ExecuteAsync(SqlConnection cn,SqlTransaction tx,string sql,long id,int user,string value,CancellationToken ct){await using var c=new SqlCommand(sql,cn,tx);c.Parameters.AddWithValue("@Id",id);c.Parameters.AddWithValue("@User",user);c.Parameters.AddWithValue("@Value",value);await c.ExecuteNonQueryAsync(ct);}
    private static string Friendly(Exception ex)=>ex is SqlException s&&s.Number is 2601 or 2627?"Mã hoặc thông tin duy nhất đã tồn tại.":"Không thể lưu dữ liệu. "+ex.Message;

    private sealed record SimpleEntitySpec(string IdColumn,string CodeColumn,string Prefix,string[] Columns)
    {
        public static SimpleEntitySpec For(string e)=>e switch
        {
            "KhachHang"=>new("KhachHangId","MaKhachHang","KH",["HoTen","SoDienThoai","Email","DiaChi","GhiChu"]),
            "NhanVien"=>new("NhanVienId","MaNhanVien","NV",["HoTen","SoDienThoai","ChucVu","TrangThai","GhiChu"]),
            "GoiChup"=>new("GoiChupId","MaGoiChup","GC",["TenGoiChup","GiaGoi","ThoiLuongPhut","MoTa","TrangThai"]),
            "DichVu"=>new("DichVuId","MaDichVu","DV",["TenDichVu","DonViTinh","DonGia","MoTa","TrangThai"]),
            "PhongChup"=>new("PhongChupId","MaPhong","P",["TenPhong","MoTa","TrangThai"]),
            "TaiNguyen"=>new("TaiNguyenId","MaTaiNguyen","TN",["TenTaiNguyen","LoaiTaiNguyen","TongSoLuong","DichVuThueId","TrangThai","GhiChu"]),
            _=>throw new ArgumentOutOfRangeException(nameof(e))
        };
    }
}
