using System.Data;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Domain;

namespace StudioManager.Infrastructure;

public sealed partial class SqlBookingRequestRepository(string connectionString) : IBookingRequestRepository
{
    private async Task<T> Transaction<T>(Func<SqlConnection, SqlTransaction, Task<T>> work, CancellationToken ct)
    {
        await using var cn = new SqlConnection(connectionString); await cn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try { var result = await work(cn, tx); await tx.CommitAsync(ct); return result; }
        catch { try { await tx.RollbackAsync(CancellationToken.None); } catch { } throw; }
    }
    private static async Task<Result<T>> Guard<T>(Func<Task<Result<T>>> work)
    {
        try { return await work(); }
        catch (BookingRequestRuleException e) { return BookingRequestErrors.Fail<T>(e.Code); }
    }
    private static async Task<Result> Guard(Func<Task<Result>> work)
    {
        try { return await work(); }
        catch (BookingRequestRuleException e) { return BookingRequestErrors.Fail(e.Code); }
    }
    private static SqlCommand Command(SqlConnection cn, SqlTransaction tx, string sql, params (string, object?)[] args)
    {
        var command = new SqlCommand(sql, cn, tx);
        foreach (var (name, value) in args)
        {
            if (value is null)
            {
                var type = name switch { "@Owner" or "@Before" or "@Booking" or "@Customer" => SqlDbType.BigInt,
                    "@Room" or "@Staff" => SqlDbType.Int, "@Confirmed" => SqlDbType.Decimal, _ => SqlDbType.NVarChar };
                var parameter = command.Parameters.Add(name, type);
                if (type == SqlDbType.NVarChar) parameter.Size = 500;
                if (type == SqlDbType.Decimal) { parameter.Precision = 18; parameter.Scale = 2; }
                parameter.Value = DBNull.Value;
            }
            else
            {
                var parameter = command.Parameters.AddWithValue(name, value);
                if (name is "@Estimate" or "@Confirmed") { parameter.SqlDbType = SqlDbType.Decimal; parameter.Precision = 18; parameter.Scale = 2; }
            }
        }
        return command;
    }
    private static UserSession StaffSession(StaffBookingActor actor) => new(actor.Credential.AccountId, "", "", actor.Credential.Role)
    { SessionToken = Guid.NewGuid(), CredentialStamp = InternalAuthorization.Stamp(actor.Credential.PasswordHash) };
    private static long? Owner(BookingActor actor) => actor is CustomerBookingActor c ? c.Credential.AccountId : null;
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string Db(BookingRequestState state) => state.ToString().ToUpperInvariant();
    private static BookingRequestState State(string value) => Enum.Parse<BookingRequestState>(value, true);

    private static async Task<CustomerContact?> RequireActor(SqlConnection cn, SqlTransaction tx, BookingActor actor, CancellationToken ct)
    {
        if (actor is StaffBookingActor staff)
        {
            await SqlStudioRepository.RequireActorAsync(cn, tx, StaffSession(staff), false, ct);
            return null;
        }
        if (actor is not CustomerBookingActor customer) throw new UnauthorizedAccessException();
        var credential = customer.Credential;
        await using var command = Command(cn, tx, """
            SELECT HoTen,SoDienThoai,EmailLienHe,DiaChi FROM dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK)
            WHERE TaiKhoanKhachHangId=@Account AND HoatDong=1 AND SecurityVersion=@Version
              AND MatKhauHash COLLATE Latin1_General_100_BIN2=@Hash COLLATE Latin1_General_100_BIN2
            """, ("@Account", credential.AccountId), ("@Version", credential.SecurityVersion), ("@Hash", credential.PasswordHash));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new UnauthorizedAccessException();
        return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    }

    private static string CatalogSelect(BookingCatalogKind kind) => kind switch
    {
        BookingCatalogKind.Packages => "SELECT GoiChupId,MaGoiChup,TenGoiChup,COALESCE(MoTa,N''),GiaGoi,N'',ThoiLuongPhut FROM dbo.GoiChup WITH(HOLDLOCK) WHERE TrangThai='DANG_AP_DUNG'",
        BookingCatalogKind.Services => """
            SELECT d.DichVuId,d.MaDichVu,d.TenDichVu,COALESCE(d.MoTa,N''),d.DonGia,d.DonViTinh,0 FROM dbo.DichVu d WITH(HOLDLOCK)
            WHERE d.TrangThai='DANG_CUNG_CAP' AND NOT EXISTS(SELECT 1 FROM dbo.TaiNguyen t WITH(HOLDLOCK) WHERE t.DichVuThueId=d.DichVuId)
            """,
        BookingCatalogKind.Rooms => "SELECT PhongChupId,MaPhong,TenPhong,COALESCE(MoTa,N''),CAST(0 AS decimal(18,2)),N'',0 FROM dbo.PhongChup WITH(HOLDLOCK) WHERE TrangThai='HOAT_DONG'",
        BookingCatalogKind.Photographers => "SELECT NhanVienId,MaNhanVien,HoTen,N'',CAST(0 AS decimal(18,2)),N'',0 FROM dbo.NhanVien WITH(HOLDLOCK) WHERE TrangThai='DANG_LAM' AND ChucVu='NHIEP_ANH_GIA'",
        _ => throw new BookingRequestRuleException("INVALID_INPUT")
    };
    private static string CatalogId(BookingCatalogKind kind) => kind switch
    {
        BookingCatalogKind.Packages => "GoiChupId", BookingCatalogKind.Services => "d.DichVuId",
        BookingCatalogKind.Rooms => "PhongChupId", BookingCatalogKind.Photographers => "NhanVienId",
        _ => throw new BookingRequestRuleException("INVALID_INPUT")
    };
    private static BookingCatalogItem ReadCatalog(SqlDataReader r)
        => new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetDecimal(4), r.GetString(5), r.GetInt32(6));
    private static async Task<BookingCatalogItem?> CatalogItem(SqlConnection cn, SqlTransaction tx, BookingCatalogKind kind, int id, CancellationToken ct)
    {
        await using var command = Command(cn, tx, CatalogSelect(kind) + " AND " + CatalogId(kind) + "=@Id", ("@Id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadCatalog(reader) : null;
    }

    public Task<Result<BookingCatalogPage>> CatalogAsync(BookingActor actor, BookingCatalogKind kind, int? afterId, CancellationToken ct = default)
        => Guard(() => Transaction(async (cn, tx) =>
        {
            await RequireActor(cn, tx, actor, ct);
            if (actor is not StaffBookingActor && kind == BookingCatalogKind.Photographers) throw new UnauthorizedAccessException();
            var sql = "SELECT TOP(26) " + CatalogSelect(kind)[7..];
            await using var command = Command(cn, tx, sql + " AND " + CatalogId(kind) + ">@After ORDER BY " + CatalogId(kind), ("@After", afterId ?? 0));
            var list = new List<BookingCatalogItem>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) list.Add(ReadCatalog(reader));
            var page = list.Take(25).ToArray();
            return Result<BookingCatalogPage>.Ok(new(page, list.Count > 25 ? page[^1].Id : null));
        }, ct));

    private static async Task<Result<BookingQuote>> QuoteCore(SqlConnection cn, SqlTransaction tx, BookingRequestDraft draft,
        CustomerContact contact, DateTimeOffset now, CancellationToken ct)
    {
        var package = await CatalogItem(cn, tx, BookingCatalogKind.Packages, draft.PackageId, ct);
        var services = new List<BookingCatalogItem>();
        foreach (var addon in draft.Addons)
            if (await CatalogItem(cn, tx, BookingCatalogKind.Services, addon.ServiceId, ct) is { } item) services.Add(item);
        var room = draft.PreferredRoomId is { } id ? await CatalogItem(cn, tx, BookingCatalogKind.Rooms, id, ct) : null;
        return BookingRequestPolicy.BuildQuote(draft, package, services, room, contact, now);
    }
    public Task<Result<BookingQuote>> QuoteAsync(CustomerBookingActor actor, BookingRequestDraft draft, DateTimeOffset now, CancellationToken ct = default)
        => Guard(() => Transaction(async (cn, tx) =>
        {
            var contact = await RequireActor(cn, tx, actor, ct);
            return await QuoteCore(cn, tx, draft, contact!, now, ct);
        }, ct));

    public Task<Result<long>> SubmitAsync(CustomerBookingActor actor, BookingQuote quote, DateTimeOffset now, CancellationToken ct = default)
        => Guard(() => Transaction(async (cn, tx) =>
        {
            var contact = await RequireActor(cn, tx, actor, ct);
            await using (var previous = Command(cn, tx, """
                SELECT YeuCauDatLichId,QuoteFingerprint FROM dbo.YeuCauDatLich WITH(UPDLOCK,HOLDLOCK)
                WHERE TaiKhoanKhachHangId=@Account AND SubmissionKey=@Key
                """, ("@Account", actor.Credential.AccountId), ("@Key", quote.Draft.SubmissionKey)))
            {
                await using var r = await previous.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    if (r.GetString(1) != quote.Fingerprint) throw new BookingRequestRuleException("SUBMISSION_KEY_REUSED");
                    return Result<long>.Ok(r.GetInt64(0), "Yêu cầu đã tồn tại. Vui lòng xem trạng thái.");
                }
            }
            var current = await QuoteCore(cn, tx, quote.Draft, contact!, now, ct);
            if (!current.Success) throw new BookingRequestRuleException(current.Code);
            if (current.Data!.Fingerprint != quote.Fingerprint) throw new BookingRequestRuleException("STALE_QUOTE");
            await using var insert = Command(cn, tx, """
                INSERT dbo.YeuCauDatLich(TaiKhoanKhachHangId,SubmissionKey,QuoteFingerprint,GoiChupId,MaGoiChot,TenGoiChot,GiaGoiChot,
                  ThoiLuongPhut,BatDauUtc,PhongChupUuTienId,TenPhongUuTien,HoTen,SoDienThoai,Email,DiaChi,GhiChu,TongDuKien)
                VALUES(@Account,@Key,@Fingerprint,@Package,@Code,@Name,@Price,@Duration,@Start,@Room,@RoomName,@FullName,@Phone,@Email,@Address,@Notes,@Estimate);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);
                """, ("@Account", actor.Credential.AccountId), ("@Key", quote.Draft.SubmissionKey), ("@Fingerprint", quote.Fingerprint),
                ("@Package", quote.Package.Id), ("@Code", quote.Package.Code), ("@Name", quote.Package.Name), ("@Price", quote.Package.Price),
                ("@Duration", quote.Package.DurationMinutes), ("@Start", quote.Draft.StartUtc.UtcDateTime), ("@Room", quote.Draft.PreferredRoomId),
                ("@RoomName", quote.RoomName), ("@FullName", quote.Contact.FullName), ("@Phone", quote.Contact.Phone), ("@Email", quote.Contact.Email),
                ("@Address", quote.Contact.Address), ("@Notes", quote.Draft.Notes), ("@Estimate", quote.Estimate));
            var id = Convert.ToInt64(await insert.ExecuteScalarAsync(ct));
            foreach (var addon in quote.Addons)
            {
                await using var add = Command(cn, tx, """
                    INSERT dbo.YeuCauDatLichDichVu(YeuCauDatLichId,DichVuId,TenDichVuChot,DonViTinhChot,DonGiaChot,SoLuong)
                    VALUES(@Id,@Service,@Name,@Unit,@Price,@Qty)
                    """, ("@Id", id), ("@Service", addon.ServiceId), ("@Name", addon.Name), ("@Unit", addon.Unit), ("@Price", addon.UnitPrice), ("@Qty", addon.Quantity));
                await add.ExecuteNonQueryAsync(ct);
            }
            await History(cn, tx, id, BookingRequestState.Pending, null, "Khách gửi yêu cầu đặt lịch.", ct);
            return Result<long>.Ok(id, "Đã gửi yêu cầu PENDING. Chưa giữ chỗ hoặc tạo lịch chính thức.");
        }, ct));

    public Task<IReadOnlyList<BookingRequestSummary>> ListAsync(BookingActor actor, BookingRequestState? state, long? beforeId, CancellationToken ct = default)
        => Transaction<IReadOnlyList<BookingRequestSummary>>(async (cn, tx) =>
        {
            await RequireActor(cn, tx, actor, ct);
            await using var command = Command(cn, tx, """
                SELECT TOP(25) r.YeuCauDatLichId,r.TrangThai,r.HoTen,r.TenGoiChot,r.BatDauUtc,r.SubmittedAtUtc,r.TongDuKien,l.MaLichChup
                FROM dbo.YeuCauDatLich r LEFT JOIN dbo.LichChup l ON l.LichChupId=r.LichChupId
                WHERE (@Owner IS NULL OR r.TaiKhoanKhachHangId=@Owner) AND (@Before IS NULL OR r.YeuCauDatLichId<@Before)
                  AND (@State IS NULL OR r.TrangThai=@State) ORDER BY r.YeuCauDatLichId DESC
                """, ("@Owner", Owner(actor)), ("@Before", beforeId), ("@State", state is { } s ? Db(s) : null));
            var result = new List<BookingRequestSummary>();
            await using var r = await command.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) result.Add(new(r.GetInt64(0), State(r.GetString(1)), r.GetString(2), r.GetString(3),
                Utc(r.GetDateTime(4)), Utc(r.GetDateTime(5)), r.GetDecimal(6), r.IsDBNull(7) ? null : r.GetString(7)));
            return result;
        }, ct);

    private sealed record Stored(BookingRequestDetails Details, long? CustomerId, bool CreatedCustomer);

    private static async Task<Stored> LockStaffRequest(SqlConnection cn, SqlTransaction tx, StaffBookingActor actor,
        long id, bool forUpdate, CancellationToken ct)
    {
        // Locate only. READCOMMITTED releases the locator's shared request lock at statement completion.
        // This lookup is never authorization/state authority; both are rechecked after account -> request locking.
        long account;
        await using (var locate = Command(cn, tx,
            "SELECT TaiKhoanKhachHangId FROM dbo.YeuCauDatLich WITH(READCOMMITTED) WHERE YeuCauDatLichId=@Id", ("@Id", id)))
        {
            var value = await locate.ExecuteScalarAsync(ct);
            if (value is null or DBNull) throw new BookingRequestRuleException("NOT_FOUND");
            account = (long)value;
        }
        await using (var customer = Command(cn, tx,
            "SELECT TaiKhoanKhachHangId FROM dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK) WHERE TaiKhoanKhachHangId=@Account", ("@Account", account)))
            if (await customer.ExecuteScalarAsync(ct) is null or DBNull) throw new BookingRequestRuleException("NOT_FOUND");
        var stored = await ReadRequest(cn, tx, actor, id, forUpdate, ct) ?? throw new BookingRequestRuleException("NOT_FOUND");
        if (stored.Details.AccountId != account) throw new BookingRequestRuleException("STATE_CHANGED");
        return stored;
    }

    private static async Task<Stored?> ReadRequest(SqlConnection cn, SqlTransaction tx, BookingActor actor, long id, bool forUpdate, CancellationToken ct)
    {
        var hint = forUpdate ? " WITH(UPDLOCK,HOLDLOCK)" : " WITH(HOLDLOCK)";
        await using var command = Command(cn, tx, """
            SELECT r.YeuCauDatLichId,r.TaiKhoanKhachHangId,r.SubmissionKey,r.QuoteFingerprint,r.TrangThai,r.GoiChupId,r.MaGoiChot,r.TenGoiChot,
              r.GiaGoiChot,r.ThoiLuongPhut,r.BatDauUtc,r.PhongChupUuTienId,r.TenPhongUuTien,r.HoTen,r.SoDienThoai,r.Email,r.DiaChi,r.GhiChu,
              r.TongDuKien,r.SubmittedAtUtc,r.PhanHoi,r.ReviewedAtUtc,r.CancelledAtUtc,r.LichChupId,l.MaLichChup,r.TongDaChot,r.KhachHangId,r.TaoKhachMoi
            FROM dbo.YeuCauDatLich r
            """ + hint + """
             LEFT JOIN dbo.LichChup l ON l.LichChupId=r.LichChupId WHERE r.YeuCauDatLichId=@Id AND (@Owner IS NULL OR r.TaiKhoanKhachHangId=@Owner)
            """, ("@Id", id), ("@Owner", Owner(actor)));
        await using var r = await command.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var account = r.GetInt64(1); var key = r.GetGuid(2); var fingerprint = r.GetString(3); var state = State(r.GetString(4));
        var package = new BookingCatalogItem(r.GetInt32(5), r.GetString(6), r.GetString(7), "", r.GetDecimal(8), "", r.GetInt32(9));
        var start = Utc(r.GetDateTime(10)); int? room = r.IsDBNull(11) ? null : r.GetInt32(11);
        var roomName = r.IsDBNull(12) ? null : r.GetString(12);
        var contact = new CustomerContact(r.GetString(13), r.GetString(14), r.GetString(15), r.GetString(16));
        var notes = r.GetString(17); var estimate = r.GetDecimal(18); var submitted = Utc(r.GetDateTime(19));
        var feedback = r.IsDBNull(20) ? null : r.GetString(20);
        DateTimeOffset? reviewed = r.IsDBNull(21) ? null : Utc(r.GetDateTime(21));
        DateTimeOffset? cancelled = r.IsDBNull(22) ? null : Utc(r.GetDateTime(22));
        long? bookingId = r.IsDBNull(23) ? null : r.GetInt64(23); var bookingCode = r.IsDBNull(24) ? null : r.GetString(24);
        decimal? confirmed = r.IsDBNull(25) ? null : r.GetDecimal(25);
        long? customerId = r.IsDBNull(26) ? null : r.GetInt64(26); var created = r.GetBoolean(27);
        await r.CloseAsync();
        var addons = new List<BookingAddonSnapshot>();
        await using (var add = Command(cn, tx, "SELECT DichVuId,TenDichVuChot,DonViTinhChot,DonGiaChot,SoLuong FROM dbo.YeuCauDatLichDichVu WHERE YeuCauDatLichId=@Id ORDER BY DichVuId", ("@Id", id)))
        {
            await using var ar = await add.ExecuteReaderAsync(ct);
            while (await ar.ReadAsync(ct)) addons.Add(new(ar.GetInt32(0), ar.GetString(1), ar.GetString(2), ar.GetDecimal(3), ar.GetDecimal(4)));
        }
        var events = new List<BookingRequestEvent>();
        await using (var history = Command(cn, tx, "SELECT TrangThai,ThoiDiemUtc,TaiKhoanNhanSuId,LyDo FROM dbo.YeuCauDatLichLichSu WHERE YeuCauDatLichId=@Id ORDER BY LichSuId", ("@Id", id)))
        {
            await using var hr = await history.ExecuteReaderAsync(ct);
            while (await hr.ReadAsync(ct)) events.Add(new(State(hr.GetString(0)), Utc(hr.GetDateTime(1)), hr.IsDBNull(2) ? "Khách hàng" : "Nhân sự", hr.GetString(3)));
        }
        var draft = new BookingRequestDraft(key, package.Id, start, room, notes, addons.Select(a => new BookingAddonSelection(a.ServiceId, a.Quantity)).ToArray());
        var quote = new BookingQuote(draft, package, roomName, contact, addons, estimate, fingerprint);
        return new(new(id, account, state, quote, submitted, feedback, reviewed, cancelled, bookingId, bookingCode, confirmed, events), customerId, created);
    }

    public Task<BookingRequestDetails?> GetAsync(BookingActor actor, long requestId, CancellationToken ct = default)
        => Transaction(async (cn, tx) => { await RequireActor(cn, tx, actor, ct); return (await ReadRequest(cn, tx, actor, requestId, false, ct))?.Details; }, ct);

    public Task<Result> CancelAsync(CustomerBookingActor actor, long requestId, string reason, CancellationToken ct = default)
        => Guard(() => Transaction(async (cn, tx) =>
        {
            await RequireActor(cn, tx, actor, ct);
            var stored = await ReadRequest(cn, tx, actor, requestId, true, ct) ?? throw new BookingRequestRuleException("NOT_FOUND");
            if (!BookingRequestRules.CanTransition(stored.Details.State, BookingRequestState.Cancelled, false)) throw new BookingRequestRuleException("STATE_CHANGED");
            await using var update = Command(cn, tx, "UPDATE dbo.YeuCauDatLich SET TrangThai='CANCELLED',CancelledAtUtc=SYSUTCDATETIME(),PhanHoi=@Reason WHERE YeuCauDatLichId=@Id AND TrangThai='PENDING' AND TaiKhoanKhachHangId=@Owner",
                ("@Id", requestId), ("@Owner", actor.Credential.AccountId), ("@Reason", reason));
            if (await update.ExecuteNonQueryAsync(ct) != 1) throw new BookingRequestRuleException("STATE_CHANGED");
            await History(cn, tx, requestId, BookingRequestState.Cancelled, null, reason, ct);
            return Result.Ok("Đã rút yêu cầu đang chờ.");
        }, ct));

    private static BookingInput Input(Stored stored, BookingApproval approval, long customerId)
        => new(customerId, stored.Details.Quote.Package.Id, StudioBookingTime.ToLegacyLocal(stored.Details.Quote.Draft.StartUtc),
            StudioBookingTime.ToLegacyLocal(stored.Details.Quote.Draft.StartUtc.AddMinutes(stored.Details.Quote.Package.DurationMinutes)),
            approval.PhotographerId, approval.RoomId, stored.Details.Quote.Draft.Notes);
    private static async Task ValidateApproval(SqlConnection cn, SqlTransaction tx, Stored stored, BookingApproval input, DateTimeOffset now, CancellationToken ct)
    {
        if (stored.Details.State != BookingRequestState.Pending) throw new BookingRequestRuleException("STATE_CHANGED");
        if (input.ExpectedEstimate != stored.Details.Quote.Estimate) throw new BookingRequestRuleException("STALE_REQUEST");
        var settlement = BookingMoneyPolicy.QuoteTotal(stored.Details.Quote.Package.Price, stored.Details.Quote.Addons);
        if (!settlement.Success) throw new BookingRequestRuleException(settlement.Code);
        if (settlement.Data != stored.Details.Quote.Estimate) throw new BookingRequestRuleException("STALE_REQUEST");
        var valid = BookingRequestPolicy.ValidateDraft(stored.Details.Quote.Draft, now);
        if (!valid.Success) throw new BookingRequestRuleException(valid.Code);
        // Same allocation-before-catalog order as the existing staff booking writer.
        await SqlStudioRepository.ValidateBookingAllocationAsync(cn, tx, Input(stored, input, 0), ct);
        if (await CatalogItem(cn, tx, BookingCatalogKind.Packages, stored.Details.Quote.Package.Id, ct) is null)
            throw new BookingRequestRuleException("INACTIVE_PACKAGE");
        foreach (var addon in stored.Details.Quote.Addons)
            if (await CatalogItem(cn, tx, BookingCatalogKind.Services, addon.ServiceId, ct) is null) throw new BookingRequestRuleException("INACTIVE_SERVICE");
    }
    public Task<Result> CheckAvailabilityAsync(StaffBookingActor actor, long requestId, BookingApproval input, DateTimeOffset now, CancellationToken ct = default)
        => Guard(() => Transaction(async (cn, tx) =>
        {
            await RequireActor(cn, tx, actor, ct);
            var stored = await LockStaffRequest(cn, tx, actor, requestId, false, ct);
            await ValidateApproval(cn, tx, stored, input, now, ct);
            return Result.Ok("Hiện khả dụng. Đây chưa phải giữ chỗ; sẽ kiểm tra lại trong transaction duyệt.");
        }, ct));

    public Task<Result> ReviewAsync(StaffBookingActor actor, long requestId, bool approve, BookingApproval? input, string reason, DateTimeOffset now, CancellationToken ct = default)
        => Guard(() => Transaction(async (cn, tx) =>
        {
            await RequireActor(cn, tx, actor, ct);
            var stored = await LockStaffRequest(cn, tx, actor, requestId, true, ct);
            var next = approve ? BookingRequestState.Approved : BookingRequestState.Rejected;
            if (!BookingRequestRules.CanTransition(stored.Details.State, next, true)) throw new BookingRequestRuleException("STATE_CHANGED");
            long? bookingId = null; long? customerId = null; var created = false;
            if (approve)
            {
                await ValidateApproval(cn, tx, stored, input!, now, ct);
                (customerId, created) = await ResolveCustomer(cn, tx, stored, actor.Credential.AccountId, ct);
                bookingId = await SqlStudioRepository.CreateBookingWithinTransactionAsync(cn, tx, Input(stored, input!, customerId.Value),
                    StaffSession(actor), stored.Details.Quote, ct);
            }
            await using var update = Command(cn, tx, """
                UPDATE dbo.YeuCauDatLich SET TrangThai=@State,ReviewedBy=@Staff,ReviewedAtUtc=SYSUTCDATETIME(),PhanHoi=@Reason,
                  LichChupId=@Booking,KhachHangId=@Customer,TaoKhachMoi=@Created,TongDaChot=@Confirmed
                WHERE YeuCauDatLichId=@Id AND TrangThai='PENDING'
                """, ("@State", Db(next)), ("@Staff", actor.Credential.AccountId), ("@Reason", reason), ("@Booking", bookingId),
                ("@Customer", customerId), ("@Created", created), ("@Confirmed", approve ? stored.Details.Quote.Estimate : null), ("@Id", requestId));
            if (await update.ExecuteNonQueryAsync(ct) != 1) throw new BookingRequestRuleException("STATE_CHANGED");
            await History(cn, tx, requestId, next, actor.Credential.AccountId, reason, ct);
            await Audit(cn, tx, actor.Credential.AccountId, approve ? "DUYET_YEU_CAU_DAT_LICH" : "TU_CHOI_YEU_CAU_DAT_LICH", requestId,
                approve ? "APPROVED;LichChupId=" + bookingId : "REJECTED", reason, ct);
            return Result.Ok(approve ? "Đã duyệt và tạo đúng một lịch chính thức." : "Đã từ chối; không tạo lịch.");
        }, ct));

    private static async Task<(long, bool)> ResolveCustomer(SqlConnection cn, SqlTransaction tx, Stored stored, int staffId, CancellationToken ct)
    {
        var account = stored.Details.AccountId;
        await using (var active = Command(cn, tx, "SELECT COUNT(*) FROM dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK) WHERE TaiKhoanKhachHangId=@A AND HoatDong=1", ("@A", account)))
            if (Convert.ToInt32(await active.ExecuteScalarAsync(ct)) != 1) throw new BookingRequestRuleException("STATE_CHANGED");
        await using (var link = Command(cn, tx, """
            SELECT s.KhachHangId FROM dbo.SoHuuKhachHang s WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.KhachHang k WITH(HOLDLOCK) ON k.KhachHangId=s.KhachHangId
            WHERE s.TaiKhoanKhachHangId=@A AND s.TrangThai='APPROVED'
            """, ("@A", account)))
        {
            var value = await link.ExecuteScalarAsync(ct);
            if (value is not null && value != DBNull.Value) return (Convert.ToInt64(value), false);
        }
        await using (var previous = Command(cn, tx, """
            SELECT TOP(1) r.KhachHangId FROM dbo.YeuCauDatLich r WITH(HOLDLOCK)
            JOIN dbo.KhachHang k WITH(HOLDLOCK) ON k.KhachHangId=r.KhachHangId
            WHERE r.TaiKhoanKhachHangId=@A AND r.TrangThai='APPROVED' AND r.TaoKhachMoi=1
              AND NOT EXISTS(SELECT 1 FROM dbo.SoHuuKhachHang s WITH(HOLDLOCK)
                WHERE s.KhachHangId=r.KhachHangId AND s.TrangThai='APPROVED' AND s.TaiKhoanKhachHangId<>@A)
            ORDER BY r.YeuCauDatLichId
            """, ("@A", account)))
        {
            var value = await previous.ExecuteScalarAsync(ct);
            if (value is not null && value != DBNull.Value) return (Convert.ToInt64(value), false);
        }
        var contact = stored.Details.Quote.Contact;
        await using var create = Command(cn, tx, """
            INSERT dbo.KhachHang(MaKhachHang,HoTen,SoDienThoai,Email,DiaChi,GhiChu,CreatedAt,UpdatedAt)
            VALUES(@Code,@Name,@Phone,@Email,@Address,N'Tạo từ yêu cầu P04',GETDATE(),GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """, ("@Code", "KH" + Guid.NewGuid().ToString("N")[..18]), ("@Name", contact.FullName),
            ("@Phone", contact.Phone), ("@Email", contact.Email), ("@Address", contact.Address));
        var id = Convert.ToInt64(await create.ExecuteScalarAsync(ct));
        await Audit(cn, tx, staffId, "TAO_KHACH_TU_YEU_CAU", stored.Details.Id, "KhachHangId=" + id, "Hồ sơ mới; không suy từ email hoặc số điện thoại.", ct);
        return (id, true);
    }

    private static async Task History(SqlConnection cn, SqlTransaction tx, long id, BookingRequestState state, int? staff, string reason, CancellationToken ct)
    {
        await using var command = Command(cn, tx, "INSERT dbo.YeuCauDatLichLichSu(YeuCauDatLichId,TrangThai,TaiKhoanNhanSuId,LyDo) VALUES(@Id,@State,@Staff,@Reason)",
            ("@Id", id), ("@State", Db(state)), ("@Staff", staff), ("@Reason", reason));
        await command.ExecuteNonQueryAsync(ct);
    }
    private static async Task Audit(SqlConnection cn, SqlTransaction tx, int staff, string action, long request, string value, string reason, CancellationToken ct)
    {
        await using var command = Command(cn, tx, """
            INSERT dbo.NhatKyHeThong(TaiKhoanId,HanhDong,LoaiDoiTuong,DoiTuongId,GiaTriCu,GiaTriMoi,LyDo)
            VALUES(@Staff,@Action,'YeuCauDatLich',@Id,'PENDING',@Value,@Reason)
            """, ("@Staff", staff), ("@Action", action), ("@Id", request.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("@Value", value), ("@Reason", reason));
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task<OfficialRequestBooking?> GetOfficialAsync(BookingActor actor, long requestId, CancellationToken ct = default)
        => Transaction(async (cn, tx) =>
        {
            await RequireActor(cn, tx, actor, ct);
            await using var command = Command(cn, tx, """
                SELECT l.LichChupId,l.MaLichChup,l.TrangThai,l.BatDau,l.KetThuc,l.TenGoiChot,l.GiaGoiChot,
                  COALESCE(dv.Total,0),l.GiaGoiChot+COALESCE(dv.Total,0)-l.TienGiam,p.TenPhong,n.HoTen
                FROM dbo.YeuCauDatLich r JOIN dbo.LichChup l ON l.LichChupId=r.LichChupId
                JOIN dbo.PhongChup p ON p.PhongChupId=l.PhongChupId JOIN dbo.NhanVien n ON n.NhanVienId=l.NhiepAnhGiaId
                OUTER APPLY(SELECT CAST(SUM(SoLuong*DonGiaChot) AS decimal(28,4)) Total FROM dbo.LichChupDichVu WHERE LichChupId=l.LichChupId) dv
                WHERE r.YeuCauDatLichId=@Id AND r.TrangThai='APPROVED'
                  AND (@Owner IS NULL OR (r.TaiKhoanKhachHangId=@Owner AND r.KhachHangId=l.KhachHangId))
                """, ("@Id", requestId), ("@Owner", Owner(actor)));
            await using var r = await command.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct) ? new OfficialRequestBooking(r.GetInt64(0), r.GetString(1), r.GetString(2),
                StudioBookingTime.FromLegacyLocal(r.GetDateTime(3)), StudioBookingTime.FromLegacyLocal(r.GetDateTime(4)),
                r.GetString(5), r.GetDecimal(6), r.GetDecimal(7), r.GetDecimal(8), r.GetString(9), r.GetString(10)) : null;
        }, ct);
}
