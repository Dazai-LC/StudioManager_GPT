using StudioManager.Domain;

namespace StudioManager.Application.Services;

public sealed class AuthService(IStudioRepository repo, IPasswordHasher hasher, IClock clock)
{
    public async Task<Result<UserSession>> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Result<UserSession>.Fail("REQUIRED", "Vui lòng nhập tên đăng nhập và mật khẩu.");
        var account = await repo.FindAccountAsync(username.Trim(), ct);
        if (account is null || !hasher.Verify(password, account.MatKhauHash))
            return Result<UserSession>.Fail("INVALID_LOGIN", "Tên đăng nhập hoặc mật khẩu không chính xác.");
        if (!account.HoatDong) return Result<UserSession>.Fail("LOCKED", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        await repo.UpdateLastLoginAsync(account.Id, clock.Now, ct);
        return Result<UserSession>.Ok(new(account.Id, account.TenDangNhap, account.HoTen, account.VaiTro, account.PhaiDoiMatKhau));
    }

    public async Task<Result> ChangePasswordAsync(int accountId, string newPassword, string confirmation, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmation))
            return Result.Fail("REQUIRED", "Vui lòng nhập đầy đủ mật khẩu mới và xác nhận mật khẩu.");
        if (newPassword.Length < 8)
            return Result.Fail("WEAK_PASSWORD", "Mật khẩu mới phải có ít nhất 8 ký tự.");
        if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower) || !newPassword.Any(char.IsDigit))
            return Result.Fail("WEAK_PASSWORD", "Mật khẩu phải có chữ hoa, chữ thường và chữ số.");
        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
            return Result.Fail("PASSWORD_MISMATCH", "Mật khẩu xác nhận không khớp.");
        return await repo.ChangeOwnPasswordAsync(accountId, hasher.Hash(newPassword), ct);
    }
}

public sealed class BookingService(IStudioRepository repo, IClock clock)
{
    public Task<LichChup?> GetAsync(long id, CancellationToken ct = default) => repo.GetBookingAsync(id, ct);
    public Task<IReadOnlyList<LichChup>> SearchAsync(BookingSearchFilter filter, CancellationToken ct = default)
        => repo.SearchBookingsAsync(filter, ct);

    public async Task<Result<long>> CreateAsync(BookingInput input, UserSession user, CancellationToken ct = default)
    {
        if (!BusinessRules.KhoangThoiGianHopLe(input.BatDau, input.KetThuc))
            return Result<long>.Fail("INVALID_TIME", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        if (await repo.HasConflictAsync(null, input.NhiepAnhGiaId, input.PhongChupId, input.BatDau, input.KetThuc, ct))
            return Result<long>.Fail("CONFLICT", "Nhiếp ảnh gia hoặc phòng chụp đã có lịch trong khoảng thời gian này.");
        var id = await repo.CreateBookingAsync(input, user, ct);
        return Result<long>.Ok(id, "Đã tạo lịch chụp thành công.");
    }

    public async Task<Result> UpdateAsync(long id, BookingInput input, string? reason, UserSession user, CancellationToken ct = default)
    {
        var current=await repo.GetBookingAsync(id,ct);if(current is null)return Result.Fail("NOT_FOUND","Không tìm thấy lịch chụp.");
        if(current.TrangThai is TrangThaiLich.HoanThanh or TrangThaiLich.DaHuy)return Result.Fail("FINAL","Không thể sửa lịch đã hoàn thành hoặc đã hủy.");
        if(current.TrangThai!=TrangThaiLich.DaDatLich&&user.VaiTro!=VaiTro.QuanTriVien)return Result.Fail("FORBIDDEN","Nhân viên chỉ được sửa lịch đang ở trạng thái Đã đặt lịch.");
        if(current.TrangThai!=TrangThaiLich.DaDatLich&&string.IsNullOrWhiteSpace(reason))return Result.Fail("REASON_REQUIRED","Quản trị viên phải nhập lý do hiệu chỉnh ngoại lệ.");
        if(!BusinessRules.KhoangThoiGianHopLe(input.BatDau,input.KetThuc))return Result.Fail("INVALID_TIME","Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        if(await repo.HasConflictAsync(id,input.NhiepAnhGiaId,input.PhongChupId,input.BatDau,input.KetThuc,ct))return Result.Fail("CONFLICT","Nhiếp ảnh gia hoặc phòng chụp đã có lịch trong khoảng này.");
        return await repo.UpdateBookingAsync(id,input,reason,user,ct);
    }

    public async Task<Result> AdvanceAsync(long id, UserSession user, CancellationToken ct = default)
    {
        var booking = await repo.GetBookingAsync(id, ct);
        if (booking is null) return Result.Fail("NOT_FOUND", "Không tìm thấy lịch chụp.");
        var next = booking.TrangThai.BuocTiepTheo();
        if (next is null) return Result.Fail("FINAL", "Lịch đang ở trạng thái kết thúc, không thể cập nhật tiến độ.");
        return await repo.UpdateBookingStatusAsync(id, next.Value, null, user, ct);
    }

    public async Task<Result> CancelAsync(long id, string reason, UserSession user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Fail("REASON_REQUIRED", "Vui lòng nhập lý do hủy.");
        var booking = await repo.GetBookingAsync(id, ct);
        if (booking is null) return Result.Fail("NOT_FOUND", "Không tìm thấy lịch chụp.");
        if (!BusinessRules.DuocHuy(booking.TrangThai, user.VaiTro, booking.BatDau, clock.Now))
            return Result.Fail("INVALID_STATE", "Bạn không được hủy lịch ở trạng thái hoặc thời điểm hiện tại.");
        return await repo.CancelBookingAsync(id, reason.Trim(), user, ct);
    }
}

public sealed class FinanceService(IStudioRepository repo)
{
    public async Task<Result> ReceiveAsync(PaymentInput input, UserSession user, CancellationToken ct = default)
    {
        if (input.SoTien <= 0) return Result.Fail("INVALID_AMOUNT", "Số tiền thu phải lớn hơn 0.");
        return await repo.AddPaymentAsync(input, user, ct);
    }
    public async Task<Result> RefundAsync(RefundInput input, UserSession user, CancellationToken ct = default)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result.Fail("FORBIDDEN", "Chỉ Quản trị viên được hoàn tiền.");
        if (input.SoTien <= 0 || string.IsNullOrWhiteSpace(input.LyDo)) return Result.Fail("INVALID", "Số tiền và lý do hoàn là bắt buộc.");
        return await repo.AddRefundAsync(input, user, ct);
    }
}

public sealed class AdministrationService(IStudioRepository repo, IPasswordHasher hasher)
{
    public Task<IReadOnlyList<IDictionary<string, object?>>> QueryGridAsync(string entity, string? keyword, UserSession user, CancellationToken ct = default)
        => CanRead(entity, user) ? repo.QueryGridAsync(entity, keyword, ct) : Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>([]);

    public Task<IReadOnlyList<IDictionary<string, object?>>> GetBookingHistoryAsync(string entity, long id, UserSession user, CancellationToken ct = default)
        => CanRead(entity, user) ? repo.GetEntityBookingHistoryAsync(entity, id, ct) : Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>([]);

    public async Task<Result> SaveAsync(string entity, long? id, IReadOnlyDictionary<string, object?> values, UserSession user, CancellationToken ct = default)
    {
        if (!CanWrite(entity, user)) return Result.Fail("FORBIDDEN", "Bạn không có quyền thay đổi dữ liệu này.");
        if (!ValidateEntity(entity, values, out var message)) return Result.Fail("INVALID", message);
        return await repo.SaveSimpleAsync(entity, id, values, user, ct);
    }

    public async Task<Result> DeactivateAsync(string entity, long id, UserSession user, CancellationToken ct = default)
    {
        if (entity == "TaiKhoan") return Result.Fail("USE_ACCOUNT_ACTION", "Hãy dùng thao tác khóa/mở khóa tài khoản.");
        if (!CanWrite(entity, user)) return Result.Fail("FORBIDDEN", "Bạn không có quyền thực hiện thao tác này.");
        return await repo.DeactivateAsync(entity, id, user, ct);
    }

    public Task<IReadOnlyList<LookupItem>> GetLookupsAsync(string type, bool activeOnly = true, CancellationToken ct = default)
        => repo.GetLookupsAsync(type, activeOnly, ct);

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
        return await repo.ResetPasswordAsync(accountId, hasher.Hash(password), user, ct);
    }

    public async Task<Result> ToggleAccountLockAsync(int accountId, UserSession user, CancellationToken ct = default)
    {
        if (accountId == user.TaiKhoanId) return Result.Fail("SELF_LOCK", "Không thể khóa tài khoản đang đăng nhập.");
        var access = await RequireAdminAsync(user, ct);
        if (access is not null) return access;
        return await repo.ToggleAccountLockAsync(accountId, user, ct);
    }

    private async Task<Result?> RequireAdminAsync(UserSession user, CancellationToken ct)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result.Fail("FORBIDDEN", "Chỉ Quản trị viên được thực hiện thao tác này.");
        var current = await repo.GetAccountByIdAsync(user.TaiKhoanId, ct);
        if (current is null || !current.HoatDong) return Result.Fail("SESSION_INVALID", "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
        if (current.VaiTro != VaiTro.QuanTriVien) return Result.Fail("FORBIDDEN", "Tài khoản hiện tại không còn quyền Quản trị viên.");
        return null;
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

public sealed class BookingSupportService(IStudioRepository repo)
{
    public Task<IReadOnlyList<LookupItem>> GetLookupsAsync(string type, bool activeOnly = true, CancellationToken ct = default)
        => repo.GetLookupsAsync(type, activeOnly, ct);
    public Task<IReadOnlyList<IDictionary<string, object?>>> GetChildrenAsync(long bookingId, string type, CancellationToken ct = default)
        => repo.GetBookingChildrenAsync(bookingId, type, ct);
    public Task<Result> AddServiceAsync(long bookingId, int serviceId, decimal quantity, UserSession user, CancellationToken ct = default)
        => repo.AddBookingServiceAsync(bookingId, serviceId, quantity, user, ct);
    public Task<Result> RemoveServiceAsync(long bookingId, long bookingServiceId, UserSession user, CancellationToken ct = default)
        => repo.RemoveBookingServiceAsync(bookingId, bookingServiceId, user, ct);
    public Task<Result> SetDiscountAsync(long bookingId, decimal amount, string? reason, UserSession user, CancellationToken ct = default)
        => repo.SetDiscountAsync(bookingId, amount, reason, user, ct);
    public Task<Result> AssignResourceAsync(long bookingId, int resourceId, int quantity, UserSession user, CancellationToken ct = default)
        => repo.AssignResourceAsync(bookingId, resourceId, quantity, user, ct);
    public Task<Result> UpdateResourceAssignmentAsync(long assignmentId, TrangThaiPhanCong next, UserSession user, CancellationToken ct = default)
        => repo.UpdateResourceAssignmentAsync(assignmentId, next, user, ct);
}

public sealed class DashboardService(IStudioRepository repo)
{
    public Task<DashboardData> LoadAsync(CancellationToken ct = default) => repo.GetDashboardAsync(ct);
}

public sealed class ReportingService(IStudioRepository repo)
{
    public async Task<Result<ReportData>> LoadAsync(DateTime from, DateTime to, UserSession user, CancellationToken ct = default)
    {
        if (user.VaiTro != VaiTro.QuanTriVien) return Result<ReportData>.Fail("FORBIDDEN", "Chỉ Quản trị viên được xem báo cáo.");
        if (from.Date > to.Date) return Result<ReportData>.Fail("INVALID_RANGE", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        return Result<ReportData>.Ok(await repo.GetReportAsync(from.Date, to.Date, ct));
    }
}

public sealed class BackupRestoreService(IStudioRepository repo)
{
    public Task<Result> BackupAsync(string path, UserSession user, CancellationToken ct = default) => repo.BackupAsync(path, user, ct);
    public Task<Result> RestoreAsync(string path, UserSession user, CancellationToken ct = default) => repo.RestoreAsync(path, user, ct);
}
