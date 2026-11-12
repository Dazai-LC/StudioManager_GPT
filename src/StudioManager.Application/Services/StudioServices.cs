using StudioManager.Domain;

namespace StudioManager.Application.Services;

public sealed class AuthService(IStudioRepository repo, IPasswordHasher hasher, IClock clock, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, clock);
    public UserSession? CurrentSession => Access.CurrentSession;
    public void SignOut() => Access.SignOut();

    public async Task<Result<UserSession>> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var attempt = Access.BeginSignIn();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Result<UserSession>.Fail("REQUIRED", "Vui lòng nhập tên đăng nhập và mật khẩu.");
        var account = await repo.FindAccountAsync(username.Trim(), ct);
        if (account is null || !hasher.Verify(password, account.MatKhauHash))
            return Result<UserSession>.Fail("INVALID_LOGIN", "Tên đăng nhập hoặc mật khẩu không chính xác.");
        if (!account.HoatDong) return Result<UserSession>.Fail("LOCKED", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        await repo.UpdateLastLoginAsync(account.Id, clock.Now, ct);
        // Re-read after the login write: do not issue a session for an account revoked mid-login.
        var current = await repo.GetAccountByIdAsync(account.Id, ct);
        if (current is null || !current.HoatDong || current.MatKhauHash != account.MatKhauHash || current.VaiTro != account.VaiTro)
            return Result<UserSession>.Fail("SESSION_INVALID", "Tài khoản đã thay đổi. Vui lòng đăng nhập lại.");
        var principal = Access.TryIssue(current, attempt);
        return principal is null
            ? Result<UserSession>.Fail("SESSION_INVALID", "Yêu cầu đăng nhập đã bị hủy. Vui lòng thử lại.")
            : Result<UserSession>.Ok(principal);
    }

    public Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, string confirmation, CancellationToken ct = default)
        => ChangePasswordCoreAsync(currentPassword, newPassword, confirmation, false, ct);

    public Task<Result> CompleteRequiredPasswordChangeAsync(string newPassword, string confirmation, CancellationToken ct = default)
        => ChangePasswordCoreAsync(null, newPassword, confirmation, true, ct);

    private async Task<Result> ChangePasswordCoreAsync(string? currentPassword, string newPassword, string confirmation, bool requiredChange, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmation))
            return Result.Fail("REQUIRED", "Vui lòng nhập đầy đủ mật khẩu mới và xác nhận mật khẩu.");
        if (newPassword.Length < 8)
            return Result.Fail("WEAK_PASSWORD", "Mật khẩu mới phải có ít nhất 8 ký tự.");
        if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower) || !newPassword.Any(char.IsDigit))
            return Result.Fail("WEAK_PASSWORD", "Mật khẩu phải có chữ hoa, chữ thường và chữ số.");
        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
            return Result.Fail("PASSWORD_MISMATCH", "Mật khẩu xác nhận không khớp.");
        var access = await Access.RequireAsync(CurrentSession, ct, allowPasswordChange: true);
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        var actor = access.Data!;
        if (requiredChange != actor.PhaiDoiMatKhau)
            return Result.Fail("INVALID_AUTH_STATE", "Trạng thái xác thực không phù hợp với thao tác đổi mật khẩu.");
        var account = await repo.GetAccountByIdAsync(actor.TaiKhoanId, ct);
        if (account is null || !account.HoatDong || account.VaiTro != actor.VaiTro ||
            account.PhaiDoiMatKhau != requiredChange || InternalAuthorization.Stamp(account.MatKhauHash) != actor.CredentialStamp)
            return Result.Fail("SESSION_INVALID", "Tài khoản đã thay đổi. Vui lòng đăng nhập lại.");
        if (!requiredChange && (string.IsNullOrWhiteSpace(currentPassword) || !hasher.Verify(currentPassword, account.MatKhauHash)))
            return Result.Fail("INVALID_PASSWORD", "Mật khẩu hiện tại không chính xác.");
        if (hasher.Verify(newPassword, account.MatKhauHash))
            return Result.Fail("PASSWORD_REUSED", "Mật khẩu mới phải khác mật khẩu hiện tại.");
        var result = await repo.ChangeOwnPasswordAsync(actor.TaiKhoanId, account.MatKhauHash, hasher.Hash(newPassword), ct);
        if (!result.Success) return result;
        var rotation = Access.RevokeForPasswordChange(actor);
        if (rotation is null)
            return Result.Fail("SESSION_INVALID", "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.");
        // Rotate after success; another concurrent reset must not be adopted as a valid session.
        var updated = await repo.GetAccountByIdAsync(actor.TaiKhoanId, ct);
        if (updated is null || !updated.HoatDong || updated.PhaiDoiMatKhau || updated.VaiTro != actor.VaiTro || !hasher.Verify(newPassword, updated.MatKhauHash))
            return Result.Fail("SESSION_INVALID", "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.");
        if (Access.TryIssue(updated, rotation.Value) is null)
            return Result.Fail("SESSION_INVALID", "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.");
        return result;
    }
}

public sealed class BookingService(IStudioRepository repo, IClock clock, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, clock);
    public async Task<LichChup?> GetAsync(long id, CancellationToken ct = default)
    {
        await Access.DemandCurrentAsync(ct);
        return await repo.GetBookingAsync(id, ct);
    }
    public async Task<IReadOnlyList<LichChup>> SearchAsync(BookingSearchFilter filter, CancellationToken ct = default)
    {
        await Access.DemandCurrentAsync(ct);
        return await repo.SearchBookingsAsync(filter, ct);
    }

    public async Task<Result<long>> CreateAsync(BookingInput input, UserSession user, CancellationToken ct = default)
    {
        if (!BusinessRules.KhoangThoiGianHopLe(input.BatDau, input.KetThuc))
            return Result<long>.Fail("INVALID_TIME", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) return Result<long>.Fail(access.Code, access.Message);
        user = access.Data!;
        if (await repo.HasConflictAsync(null, input.NhiepAnhGiaId, input.PhongChupId, input.BatDau, input.KetThuc, ct))
            return Result<long>.Fail("CONFLICT", "Nhiếp ảnh gia hoặc phòng chụp đã có lịch trong khoảng thời gian này.");
        var id = await repo.CreateBookingAsync(input, user, ct);
        return Result<long>.Ok(id, "Đã tạo lịch chụp thành công.");
    }

    public async Task<Result> UpdateAsync(long id, BookingInput input, string? reason, UserSession user, CancellationToken ct = default)
    {
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        var current=await repo.GetBookingAsync(id,ct);if(current is null)return Result.Fail("NOT_FOUND","Không tìm thấy lịch chụp.");
        if(current.TrangThai is TrangThaiLich.HoanThanh or TrangThaiLich.DaHuy)return Result.Fail("FINAL","Không thể sửa lịch đã hoàn thành hoặc đã hủy.");
        if(current.TrangThai!=TrangThaiLich.DaDatLich&&user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Nhân viên chỉ được sửa lịch đang ở trạng thái Đã đặt lịch.");
        if(current.TrangThai!=TrangThaiLich.DaDatLich&&string.IsNullOrWhiteSpace(reason))return Result.Fail("REASON_REQUIRED","Quản trị viên phải nhập lý do hiệu chỉnh ngoại lệ.");
        var quoteIntent = BookingPackageSnapshotPolicy.ValidateIntent(new(current.GoiChupId, current.TenGoiChot, current.GiaGoiChot), input.GoiChupId, input.PackageQuote);
        if (!quoteIntent.Success) return quoteIntent;
        if(!BusinessRules.KhoangThoiGianHopLe(input.BatDau,input.KetThuc))return Result.Fail("INVALID_TIME","Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        if(await repo.HasConflictAsync(id,input.NhiepAnhGiaId,input.PhongChupId,input.BatDau,input.KetThuc,ct))return Result.Fail("CONFLICT","Nhiếp ảnh gia hoặc phòng chụp đã có lịch trong khoảng này.");
        return await repo.UpdateBookingAsync(id,input,reason,user,ct);
    }

    public async Task<Result> AdvanceAsync(long id, UserSession user, CancellationToken ct = default)
    {
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        var booking = await repo.GetBookingAsync(id, ct);
        if (booking is null) return Result.Fail("NOT_FOUND", "Không tìm thấy lịch chụp.");
        var next = booking.TrangThai.BuocTiepTheo();
        if (next is null) return Result.Fail("FINAL", "Lịch đang ở trạng thái kết thúc, không thể cập nhật tiến độ.");
        return await repo.UpdateBookingStatusAsync(id, next.Value, null, user, ct);
    }

    public async Task<Result> CancelAsync(long id, string reason, UserSession user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Fail("REASON_REQUIRED", "Vui lòng nhập lý do hủy.");
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        var booking = await repo.GetBookingAsync(id, ct);
        if (booking is null) return Result.Fail("NOT_FOUND", "Không tìm thấy lịch chụp.");
        if (!BusinessRules.DuocHuy(booking.TrangThai, user.VaiTro, booking.BatDau, clock.Now))
            return Result.Fail("INVALID_STATE", "Bạn không được hủy lịch ở trạng thái hoặc thời điểm hiện tại.");
        return await repo.CancelBookingAsync(id, reason.Trim(), user, ct);
    }
}

public sealed class FinanceService(IStudioRepository repo, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, new SystemClock());
    public async Task<Result> ReceiveAsync(PaymentInput input, UserSession user, CancellationToken ct = default)
    {
        if (input.SoTien <= 0) return Result.Fail("INVALID_AMOUNT", "Số tiền thu phải lớn hơn 0.");
        if (input.LoaiThu is not ("DAT_COC" or "BO_SUNG" or "CON_LAI"))
            return Result.Fail("INVALID_TYPE", "Loại thu không hợp lệ.");
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        return await repo.AddPaymentAsync(input, user, ct);
    }
    public async Task<Result> RefundAsync(RefundInput input, UserSession user, CancellationToken ct = default)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result.Fail("FORBIDDEN", "Chỉ Quản trị viên được hoàn tiền.");
        if (input.SoTien <= 0 || string.IsNullOrWhiteSpace(input.LyDo)) return Result.Fail("INVALID", "Số tiền và lý do hoàn là bắt buộc.");
        var access = await Access.RequireAsync(user, ct, adminOnly: true);
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        return await repo.AddRefundAsync(input, user, ct);
    }
}

public sealed class AdministrationService(IStudioRepository repo, IPasswordHasher hasher, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, new SystemClock());
    public async Task<IReadOnlyList<IDictionary<string, object?>>> QueryGridAsync(string entity, string? keyword, UserSession user, CancellationToken ct = default)
    {
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) throw new UnauthorizedAccessException(access.Message);
        return CanRead(entity, access.Data!) ? await repo.QueryGridAsync(entity, keyword, ct) : [];
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> GetBookingHistoryAsync(string entity, long id, UserSession user, CancellationToken ct = default)
    {
        var access = await Access.RequireAsync(user, ct);
        if (!access.Success) throw new UnauthorizedAccessException(access.Message);
        return CanRead(entity, access.Data!) ? await repo.GetEntityBookingHistoryAsync(entity, id, ct) : [];
    }

    public async Task<Result> SaveAsync(string entity, long? id, IReadOnlyDictionary<string, object?> values, UserSession user, CancellationToken ct = default)
    {
        if (!CanWrite(entity, user)) return Result.Fail("FORBIDDEN", "Bạn không có quyền thay đổi dữ liệu này.");
        if (!ValidateEntity(entity, values, out var message)) return Result.Fail("INVALID", message);
        var access = await Access.RequireAsync(user, ct, adminOnly: entity != "KhachHang");
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        return await repo.SaveSimpleAsync(entity, id, values, user, ct);
    }

    public async Task<Result> DeactivateAsync(string entity, long id, UserSession user, CancellationToken ct = default)
    {
        if (entity == "TaiKhoan") return Result.Fail("USE_ACCOUNT_ACTION", "Hãy dùng thao tác khóa/mở khóa tài khoản.");
        if (!CanWrite(entity, user)) return Result.Fail("FORBIDDEN", "Bạn không có quyền thực hiện thao tác này.");
        var access = await Access.RequireAsync(user, ct, adminOnly: entity != "KhachHang");
        if (!access.Success) return Result.Fail(access.Code, access.Message);
        user = access.Data!;
        return await repo.DeactivateAsync(entity, id, user, ct);
    }

    public async Task<IReadOnlyList<LookupItem>> GetLookupsAsync(string type, bool activeOnly = true, CancellationToken ct = default)
    {
        await Access.DemandCurrentAsync(ct);
        return await repo.GetLookupsAsync(type, activeOnly, ct);
    }

    public async Task<Result> CreateAccountAsync(string username, string password, int? employeeId, VaiTro role, UserSession user, CancellationToken ct = default)
    {
        var access = await RequireAdminAsync(user, ct);
        if (access is not null) return access;
        if (username.Length < 3 || username.Length > 50 || username.Any(char.IsWhiteSpace)) return Result.Fail("INVALID_USERNAME", "Tên đăng nhập dài 3-50 ký tự và không chứa khoảng trắng.");
        var passwordResult = ValidatePassword(password);
        if (!passwordResult.Success) return passwordResult;
        return await repo.CreateAccountAsync(username.Trim(), hasher.Hash(password), employeeId, role, user, ct);
    }

    public async Task<Result> ResetPasswordAsync(int accountId, string password, UserSession user, CancellationToken ct = default)
    {
        if (accountId == user.TaiKhoanId) return Result.Fail("SELF_RESET", "Hãy dùng chức năng đổi mật khẩu cho chính tài khoản đang đăng nhập.");
        var access = await RequireAdminAsync(user, ct);
        if (access is not null) return access;
        var passwordResult = ValidatePassword(password);
        if (!passwordResult.Success) return passwordResult;
        var result = await repo.ResetPasswordAsync(accountId, hasher.Hash(password), user, ct);
        if (result.Success) Access.RevokeAccount(accountId);
        return result;
    }

    public async Task<Result> ToggleAccountLockAsync(int accountId, UserSession user, CancellationToken ct = default)
    {
        if (accountId == user.TaiKhoanId) return Result.Fail("SELF_LOCK", "Không thể khóa tài khoản đang đăng nhập.");
        var access = await RequireAdminAsync(user, ct);
        if (access is not null) return access;
        var result = await repo.ToggleAccountLockAsync(accountId, user, ct);
        if (result.Success) Access.RevokeAccount(accountId);
        return result;
    }

    private async Task<Result?> RequireAdminAsync(UserSession user, CancellationToken ct)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result.Fail("FORBIDDEN", "Chỉ Quản trị viên được thực hiện thao tác này.");
        var access = await Access.RequireAsync(user, ct, adminOnly: true);
        return access.Success ? null : Result.Fail(access.Code, access.Message);
    }

    private static Result ValidatePassword(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return Result.Fail("WEAK_PASSWORD", "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và chữ số.");
        return Result.Ok();
    }

    private static bool CanRead(string entity, UserSession user)
        => entity switch { "NhatKy" or "TaiKhoan" => user.VaiTro == VaiTro.QuanTriVien, _ => true };

    private static bool CanWrite(string entity, UserSession user)
        => entity == "KhachHang" || user.VaiTro == VaiTro.QuanTriVien;

    private static bool ValidateEntity(string entity, IReadOnlyDictionary<string, object?> values, out string message)
    {
        message = "";
        static decimal Number(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null && decimal.TryParse(value.ToString(), out var result) ? result : 0;
        static bool Missing(IReadOnlyDictionary<string, object?> source, string key) => !source.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value?.ToString());
        static bool ValidEmail(string value) { try { _ = new System.Net.Mail.MailAddress(value); return true; } catch { return false; } }
        if (entity == "KhachHang")
        {
            if (Missing(values, "HoTen") || Missing(values, "SoDienThoai")) message = "Họ tên và số điện thoại là bắt buộc.";
            else if (values.TryGetValue("Email", out var email) && email is not null && !string.IsNullOrWhiteSpace(email.ToString()) && !ValidEmail(email.ToString()!)) message = "Email không hợp lệ.";
        }
        if (entity == "NhanVien" && (Missing(values, "HoTen") || Missing(values, "SoDienThoai") || Missing(values, "ChucVu"))) message = "Họ tên, số điện thoại và chức vụ là bắt buộc.";
        if (entity == "GoiChup" && Missing(values, "TenGoiChup")) message = "Tên gói là bắt buộc.";
        if (entity == "DichVu" && (Missing(values, "TenDichVu") || Missing(values, "DonViTinh"))) message = "Tên dịch vụ và đơn vị tính là bắt buộc.";
        if (entity == "PhongChup" && Missing(values, "TenPhong")) message = "Tên phòng là bắt buộc.";
        if (entity == "TaiNguyen" && (Missing(values, "TenTaiNguyen") || Missing(values, "LoaiTaiNguyen"))) message = "Tên và loại tài nguyên là bắt buộc.";
        if (entity == "GoiChup" && (Number(values, "GiaGoi") < 0 || Number(values, "ThoiLuongPhut") <= 0)) message = "Giá gói không âm và thời lượng phải lớn hơn 0.";
        if (entity == "DichVu" && Number(values, "DonGia") < 0) message = "Đơn giá không được âm.";
        if (entity == "TaiNguyen" && Number(values, "TongSoLuong") <= 0) message = "Tổng số lượng phải lớn hơn 0.";
        return string.IsNullOrEmpty(message);
    }
}

public sealed class BookingSupportService(IStudioRepository repo, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, new SystemClock());
    public async Task<IReadOnlyList<LookupItem>> GetLookupsAsync(string type, bool activeOnly = true, CancellationToken ct = default)
    {
        await Access.DemandCurrentAsync(ct);
        return await repo.GetLookupsAsync(type, activeOnly, ct);
    }
    public async Task<IReadOnlyList<IDictionary<string, object?>>> GetChildrenAsync(long bookingId, string type, CancellationToken ct = default)
    {
        await Access.DemandCurrentAsync(ct);
        return await repo.GetBookingChildrenAsync(bookingId, type, ct);
    }
    public Task<Result> AddServiceAsync(long bookingId, int serviceId, decimal quantity, UserSession user, CancellationToken ct = default)
        => quantity <= 0
            ? Task.FromResult(Result.Fail("INVALID_QUANTITY", "Số lượng dịch vụ phải lớn hơn 0."))
            : ExecuteAuthorizedAsync(user, actor => repo.AddBookingServiceAsync(bookingId, serviceId, quantity, actor, ct), ct);
    public Task<Result> RemoveServiceAsync(long bookingId, long bookingServiceId, UserSession user, CancellationToken ct = default)
        => ExecuteAuthorizedAsync(user, actor => repo.RemoveBookingServiceAsync(bookingId, bookingServiceId, actor, ct), ct);
    public Task<Result> SetDiscountAsync(long bookingId, decimal amount, string? reason, UserSession user, CancellationToken ct = default)
        => ExecuteAuthorizedAsync(user, actor => repo.SetDiscountAsync(bookingId, amount, reason, actor, ct), ct);
    public Task<Result> AssignResourceAsync(long bookingId, int resourceId, int quantity, bool addRentalService, UserSession user, CancellationToken ct = default)
        => quantity <= 0
            ? Task.FromResult(Result.Fail("INVALID_QUANTITY", "Số lượng tài nguyên phải lớn hơn 0."))
            : ExecuteAuthorizedAsync(user, actor => repo.AssignResourceAsync(bookingId, resourceId, quantity, addRentalService, actor, ct), ct);
    public Task<Result> UpdateResourceAssignmentAsync(long assignmentId, TrangThaiPhanCong next, UserSession user, CancellationToken ct = default)
        => ExecuteAuthorizedAsync(user, actor => repo.UpdateResourceAssignmentAsync(assignmentId, next, actor, ct), ct);

    private async Task<Result> ExecuteAuthorizedAsync(UserSession user, Func<UserSession, Task<Result>> action, CancellationToken ct)
    {
        var access = await Access.RequireAsync(user, ct);
        return access.Success ? await action(access.Data!) : Result.Fail(access.Code, access.Message);
    }
}

public sealed class DashboardService(IStudioRepository repo, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, new SystemClock());
    public async Task<DashboardData> LoadAsync(CancellationToken ct = default)
    {
        await Access.DemandCurrentAsync(ct);
        return await repo.GetDashboardAsync(ct);
    }
}

public sealed class ReportingService(IStudioRepository repo, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, new SystemClock());
    public async Task<Result<ReportData>> LoadAsync(DateTime from, DateTime to, UserSession user, CancellationToken ct = default)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result<ReportData>.Fail("FORBIDDEN", "Chỉ Quản trị viên được xem báo cáo.");
        if (from.Date > to.Date) return Result<ReportData>.Fail("INVALID_RANGE", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        var access = await Access.RequireAsync(user, ct, adminOnly: true);
        if (!access.Success) return Result<ReportData>.Fail(access.Code, access.Message);
        return Result<ReportData>.Ok(await repo.GetReportAsync(from.Date, to.Date, ct));
    }
}

public sealed class BackupRestoreService(IStudioRepository repo, InternalAuthorization? authorization = null)
{
    private readonly InternalAuthorization Access = authorization ?? new(repo, new SystemClock());
    public Task<Result> BackupAsync(string path, UserSession user, CancellationToken ct = default)
        => ValidateBackupPath(path, user) is { } invalid ? Task.FromResult(invalid) : ExecuteAuthorizedAsync(user, actor => repo.BackupAsync(path, actor, ct), ct);

    public Task<Result> RestoreAsync(string path, UserSession user, CancellationToken ct = default)
        => ValidateRestorePath(path, user) is { } invalid ? Task.FromResult(invalid) : ExecuteAuthorizedAsync(user, actor => repo.RestoreAsync(path, actor, ct), ct);

    public async Task<IReadOnlyList<IDictionary<string, object?>>> GetHistoryAsync(UserSession user, CancellationToken ct = default)
    {
        var access = await Access.RequireAsync(user, ct, adminOnly: true);
        if (!access.Success) throw new UnauthorizedAccessException(access.Message);
        return await repo.GetBackupHistoryAsync(ct);
    }

    public async Task<string?> GetDefaultBackupDirectoryAsync(UserSession user, CancellationToken ct = default)
    {
        var access = await Access.RequireAsync(user, ct, adminOnly: true);
        if (!access.Success) throw new UnauthorizedAccessException(access.Message);
        return await repo.GetDefaultBackupDirectoryAsync(ct);
    }

    private async Task<Result> ExecuteAuthorizedAsync(UserSession user, Func<UserSession, Task<Result>> action, CancellationToken ct)
    {
        var access = await Access.RequireAsync(user, ct, adminOnly: true);
        return access.Success ? await action(access.Data!) : Result.Fail(access.Code, access.Message);
    }

    private static Result? ValidateBackupPath(string path, UserSession user)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result.Fail("FORBIDDEN", "Chỉ Quản trị viên được sao lưu dữ liệu.");
        if (string.IsNullOrWhiteSpace(path) || !string.Equals(Path.GetExtension(path), ".bak", StringComparison.OrdinalIgnoreCase))
            return Result.Fail("INVALID_PATH", "Tệp sao lưu phải có phần mở rộng .bak.");
        return null;
    }

    private static Result? ValidateRestorePath(string path, UserSession user)
    {
        var validation = ValidateBackupPath(path, user);
        if (validation is not null) return validation;
        return File.Exists(path) ? null : Result.Fail("FILE_NOT_FOUND", "Không tìm thấy tệp sao lưu đã chọn.");
    }
}
