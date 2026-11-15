using System.Data;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Domain;

namespace StudioManager.Infrastructure;

public sealed class SqlCustomerStaffDirectory(IStudioRepository repository) : ICustomerStaffDirectory
{
    public Task<TaiKhoan?> FindAsync(string username, CancellationToken ct = default) => repository.FindAccountAsync(username, ct);
    public Task<TaiKhoan?> GetAsync(int id, CancellationToken ct = default) => repository.GetAccountByIdAsync(id, ct);
}

public sealed class SqlCustomerIdentityRepository(string connectionString) : ICustomerIdentityRepository
{
    private SqlConnection Connection() => new(connectionString);
    private const string CustomerPredicate = """
        a.TaiKhoanKhachHangId=@Account AND a.HoatDong=1 AND a.SecurityVersion=@Version
        AND a.MatKhauHash COLLATE Latin1_General_100_BIN2=@Hash COLLATE Latin1_General_100_BIN2
        """;
    private const string ReviewerPredicate = """
        t.TaiKhoanId=@Reviewer AND t.TrangThai='HOAT_DONG' AND t.PhaiDoiMatKhau=0 AND t.VaiTro=@Role
        AND t.VaiTro IN('QUAN_TRI_VIEN','NHAN_VIEN')
        AND t.MatKhauHash COLLATE Latin1_General_100_BIN2=@ReviewerHash COLLATE Latin1_General_100_BIN2
        AND NOT EXISTS(SELECT 1 FROM dbo.NhanVien n WHERE n.NhanVienId=t.NhanVienId AND n.TrangThai<>'DANG_LAM')
        """;

    public Task<CustomerAccount?> FindByLoginAsync(string normalizedEmail, CancellationToken ct = default)
        => ReadAccountAsync("LoginEmail=@Value", normalizedEmail, ct);
    public Task<CustomerAccount?> GetAccountAsync(long accountId, CancellationToken ct = default)
        => ReadAccountAsync("TaiKhoanKhachHangId=@Value", accountId, ct);

    private async Task<CustomerAccount?> ReadAccountAsync(string predicate, object value, CancellationToken ct)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            SELECT TaiKhoanKhachHangId,LoginEmail,MatKhauHash,SecurityVersion,HoatDong,HoTen,SoDienThoai,EmailLienHe,DiaChi
            FROM dbo.TaiKhoanKhachHang WHERE
            """ + " " + predicate, cn);
        cmd.Parameters.AddWithValue("@Value", value);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetGuid(3), r.GetBoolean(4),
            new(r.GetString(5), r.GetString(6), r.GetString(7), r.GetString(8))) : null;
    }

    public async Task<Result> RegisterAsync(string normalizedEmail, string hash, CustomerContact contact, CancellationToken ct = default)
    {
        try
        {
            await using var cn = Connection(); await cn.OpenAsync(ct);
            await using var cmd = new SqlCommand("""
                INSERT dbo.TaiKhoanKhachHang(LoginEmail,MatKhauHash,HoTen,SoDienThoai,EmailLienHe,DiaChi)
                VALUES(@Login,@Hash,@Name,@Phone,@Email,@Address)
                """, cn);
            cmd.Parameters.AddWithValue("@Login", normalizedEmail); cmd.Parameters.AddWithValue("@Hash", hash); Contact(cmd, contact);
            await cmd.ExecuteNonQueryAsync(ct); return Result.Ok("Đã tạo tài khoản. Vui lòng đăng nhập.");
        }
        catch (SqlException error) when (error.Number is 2601 or 2627)
        { return Result.Fail("REGISTRATION_UNAVAILABLE", "Không thể đăng ký thông tin này."); }
    }

    public async Task<Result> UpdateContactAsync(CustomerCredential actor, CustomerContact contact, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            UPDATE a SET HoTen=@Name,SoDienThoai=@Phone,EmailLienHe=@Email,DiaChi=@Address,UpdatedAtUtc=SYSUTCDATETIME()
            FROM dbo.TaiKhoanKhachHang a WHERE
            """ + " " + CustomerPredicate, cn);
        Actor(cmd, actor); Contact(cmd, contact);
        return await cmd.ExecuteNonQueryAsync(ct) == 1 ? Result.Ok() : Denied();
    }

    public async Task<Result> ChangePasswordAsync(CustomerCredential actor, string newHash, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            UPDATE a SET MatKhauHash=@NewHash,SecurityVersion=NEWID(),PasswordChangedAtUtc=SYSUTCDATETIME(),UpdatedAtUtc=SYSUTCDATETIME()
            FROM dbo.TaiKhoanKhachHang a WHERE
            """ + " " + CustomerPredicate, cn);
        Actor(cmd, actor); cmd.Parameters.AddWithValue("@NewHash", newHash);
        return await cmd.ExecuteNonQueryAsync(ct) == 1 ? Result.Ok("Đã đổi mật khẩu. Vui lòng đăng nhập lại.") : Denied();
    }

    public async Task<CustomerLink?> GetMyLinkAsync(CustomerCredential actor, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            SELECT TOP(1) l.SoHuuKhachHangId,l.TrangThai,l.ThamChieu,l.RequestedAtUtc,
              CASE WHEN l.TrangThai='APPROVED' THEN k.MaKhachHang ELSE NULL END
            FROM dbo.SoHuuKhachHang l JOIN dbo.TaiKhoanKhachHang a ON a.TaiKhoanKhachHangId=l.TaiKhoanKhachHangId
            LEFT JOIN dbo.KhachHang k ON k.KhachHangId=l.KhachHangId WHERE
            """ + " " + CustomerPredicate + " ORDER BY l.SoHuuKhachHangId DESC", cn);
        Actor(cmd, actor); await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetInt64(0), State(r.GetString(1)), r.GetString(2),
            DateTime.SpecifyKind(r.GetDateTime(3), DateTimeKind.Utc), r.IsDBNull(4) ? null : r.GetString(4)) : null;
    }

    public async Task<Result> RequestLinkAsync(CustomerCredential actor, string reference, string evidence, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await RequireCustomerAsync(cn, tx, actor, ct);
        await using var check = new SqlCommand("SELECT COUNT(*) FROM dbo.SoHuuKhachHang WITH(UPDLOCK,HOLDLOCK) WHERE TaiKhoanKhachHangId=@Account AND TrangThai IN('PENDING','APPROVED')", cn, tx);
        check.Parameters.AddWithValue("@Account", actor.AccountId);
        if (Convert.ToInt32(await check.ExecuteScalarAsync(ct)) != 0) return Result.Fail("LINK_EXISTS", "Đã có yêu cầu đang chờ hoặc liên kết còn hiệu lực.");
        await using var cmd = new SqlCommand("INSERT dbo.SoHuuKhachHang(TaiKhoanKhachHangId,ThamChieu,BangChungKhachGui) VALUES(@Account,@Reference,@Evidence)", cn, tx);
        cmd.Parameters.AddWithValue("@Account", actor.AccountId); cmd.Parameters.AddWithValue("@Reference", reference); cmd.Parameters.AddWithValue("@Evidence", evidence);
        await cmd.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct); return Result.Ok("Đã gửi yêu cầu. Nhân viên sẽ đối chiếu trực tiếp.");
    }

    public async Task<OwnedCustomerContact?> GetOwnedCustomerAsync(CustomerCredential actor, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            SELECT k.MaKhachHang,k.HoTen,k.SoDienThoai,k.Email,k.DiaChi
            FROM dbo.TaiKhoanKhachHang a JOIN dbo.SoHuuKhachHang l ON l.TaiKhoanKhachHangId=a.TaiKhoanKhachHangId AND l.TrangThai='APPROVED'
            JOIN dbo.KhachHang k ON k.KhachHangId=l.KhachHangId WHERE
            """ + " " + CustomerPredicate, cn);
        Actor(cmd, actor); await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetString(0), r.GetString(1), r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4)) : null;
    }

    public async Task<IReadOnlyList<CustomerLinkReview>> ListLinksAsync(CustomerReviewer reviewer, long? beforeLinkId = null, CancellationToken ct = default)
    {
        await using var cn = Connection(); await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand("""
            SELECT TOP(25) l.SoHuuKhachHangId,a.TaiKhoanKhachHangId,a.LoginEmail,a.HoTen,l.ThamChieu,l.BangChungKhachGui,l.TrangThai,l.KhachHangId
            FROM dbo.SoHuuKhachHang l JOIN dbo.TaiKhoanKhachHang a ON a.TaiKhoanKhachHangId=l.TaiKhoanKhachHangId
            WHERE l.TrangThai IN('PENDING','APPROVED') AND (@Before IS NULL OR l.SoHuuKhachHangId<@Before) AND EXISTS(SELECT 1 FROM dbo.TaiKhoan t WHERE
            """ + " " + ReviewerPredicate + ") ORDER BY l.SoHuuKhachHangId DESC", cn);
        Reviewer(cmd, reviewer); cmd.Parameters.AddWithValue("@Before", (object?)beforeLinkId ?? DBNull.Value);
        var result = new List<CustomerLinkReview>(); await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result.Add(new(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.GetString(5), State(r.GetString(6)), r.IsDBNull(7) ? null : r.GetInt64(7)));
        return result;
    }

    public async Task<Result> ReviewLinkAsync(CustomerReviewer reviewer, long linkId, CustomerLinkDecision decision,
        long? khachHangId, string reason, CancellationToken ct = default)
    {
        try
        {
            await using var cn = Connection(); await cn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await using (var auth = new SqlCommand("SELECT COUNT(*) FROM dbo.TaiKhoan t WITH(UPDLOCK,HOLDLOCK) WHERE " + ReviewerPredicate, cn, tx))
            {
                Reviewer(auth, reviewer);
                if (Convert.ToInt32(await auth.ExecuteScalarAsync(ct)) != 1) return Denied();
            }
            await using var current = new SqlCommand("""
                SELECT l.TrangThai,l.TaiKhoanKhachHangId FROM dbo.SoHuuKhachHang l WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.TaiKhoanKhachHang a WITH(UPDLOCK,HOLDLOCK) ON a.TaiKhoanKhachHangId=l.TaiKhoanKhachHangId
                WHERE l.SoHuuKhachHangId=@Link AND (@Approve=0 OR a.HoatDong=1)
                """, cn, tx);
            current.Parameters.AddWithValue("@Link", linkId);
            current.Parameters.AddWithValue("@Approve", decision == CustomerLinkDecision.Approve);
            await using var r = await current.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return Result.Fail("NOT_FOUND", "Không tìm thấy yêu cầu khả dụng.");
            var state = State(r.GetString(0)); await r.CloseAsync();
            if ((decision == CustomerLinkDecision.Revoke && state != CustomerLinkState.Approved) ||
                (decision != CustomerLinkDecision.Revoke && state != CustomerLinkState.Pending))
                return Result.Fail("LINK_STATE_CHANGED", "Yêu cầu đã được xử lý. Vui lòng tải lại.");
            if (decision == CustomerLinkDecision.Approve)
            {
                await using var customer = new SqlCommand("SELECT COUNT(*) FROM dbo.KhachHang WITH(UPDLOCK,HOLDLOCK) WHERE KhachHangId=@Customer", cn, tx);
                customer.Parameters.AddWithValue("@Customer", (object?)khachHangId ?? DBNull.Value);
                if (Convert.ToInt32(await customer.ExecuteScalarAsync(ct)) != 1) return Result.Fail("NOT_FOUND", "Hồ sơ được chọn không còn tồn tại.");
            }
            var sql = decision == CustomerLinkDecision.Revoke
                ? "UPDATE dbo.SoHuuKhachHang SET TrangThai='REVOKED',RevokedBy=@Reviewer,RevokedAtUtc=SYSUTCDATETIME(),RevokeReason=@Reason WHERE SoHuuKhachHangId=@Link"
                : "UPDATE dbo.SoHuuKhachHang SET TrangThai=@State,KhachHangId=@Customer,ReviewedBy=@Reviewer,ReviewedAtUtc=SYSUTCDATETIME(),ReviewReason=@Reason WHERE SoHuuKhachHangId=@Link";
            await using var update = new SqlCommand(sql, cn, tx);
            update.Parameters.AddWithValue("@Link", linkId); update.Parameters.AddWithValue("@Reviewer", reviewer.AccountId);
            update.Parameters.AddWithValue("@Reason", reason);
            if (decision != CustomerLinkDecision.Revoke)
            {
                update.Parameters.AddWithValue("@State", decision == CustomerLinkDecision.Approve ? "APPROVED" : "REJECTED");
                update.Parameters.AddWithValue("@Customer", decision == CustomerLinkDecision.Approve ? (object?)khachHangId ?? DBNull.Value : DBNull.Value);
            }
            await update.ExecuteNonQueryAsync(ct);
            await using var audit = new SqlCommand("""
                INSERT dbo.NhatKyHeThong(TaiKhoanId,HanhDong,LoaiDoiTuong,DoiTuongId,GiaTriCu,GiaTriMoi,LyDo)
                VALUES(@Reviewer,@Action,'SoHuuKhachHang',@Id,@Old,@New,@Reason)
                """, cn, tx);
            audit.Parameters.AddWithValue("@Reviewer", reviewer.AccountId);
            audit.Parameters.AddWithValue("@Action", decision switch { CustomerLinkDecision.Approve => "DUYET_LIEN_KET_KH", CustomerLinkDecision.Reject => "TU_CHOI_LIEN_KET_KH", _ => "THU_HOI_LIEN_KET_KH" });
            audit.Parameters.AddWithValue("@Id", linkId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            audit.Parameters.AddWithValue("@Old", state.ToString()); audit.Parameters.AddWithValue("@New", decision.ToString()); audit.Parameters.AddWithValue("@Reason", reason);
            await audit.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct); return Result.Ok();
        }
        catch (SqlException error) when (error.Number is 2601 or 2627)
        { return Result.Fail("OWNERSHIP_CONFLICT", "Account hoặc hồ sơ đã có liên kết còn hiệu lực."); }
    }

    private static async Task RequireCustomerAsync(SqlConnection cn, SqlTransaction tx, CustomerCredential actor, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TaiKhoanKhachHang a WITH(UPDLOCK,HOLDLOCK) WHERE " + CustomerPredicate, cn, tx);
        Actor(cmd, actor); if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) != 1) throw new UnauthorizedAccessException();
    }
    private static void Actor(SqlCommand cmd, CustomerCredential actor)
    { cmd.Parameters.AddWithValue("@Account", actor.AccountId); cmd.Parameters.AddWithValue("@Version", actor.SecurityVersion); cmd.Parameters.AddWithValue("@Hash", actor.PasswordHash); }
    private static void Reviewer(SqlCommand cmd, CustomerReviewer reviewer)
    {
        cmd.Parameters.AddWithValue("@Reviewer", reviewer.AccountId); cmd.Parameters.AddWithValue("@ReviewerHash", reviewer.PasswordHash);
        cmd.Parameters.AddWithValue("@Role", reviewer.Role == VaiTro.QuanTriVien ? "QUAN_TRI_VIEN" : reviewer.Role == VaiTro.NhanVien ? "NHAN_VIEN" : "");
    }
    private static void Contact(SqlCommand cmd, CustomerContact contact)
    { cmd.Parameters.AddWithValue("@Name", contact.FullName); cmd.Parameters.AddWithValue("@Phone", contact.Phone); cmd.Parameters.AddWithValue("@Email", contact.Email); cmd.Parameters.AddWithValue("@Address", contact.Address); }
    private static CustomerLinkState State(string state) => state switch
    { "PENDING" => CustomerLinkState.Pending, "APPROVED" => CustomerLinkState.Approved, "REJECTED" => CustomerLinkState.Rejected, "REVOKED" => CustomerLinkState.Revoked, _ => throw new InvalidOperationException("Unknown ownership state.") };
    private static Result Denied() => Result.Fail("UNAUTHORIZED", "Phiên đăng nhập hoặc quyền truy cập đã thay đổi.");
}
