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
        return Result<UserSession>.Ok(new(account.Id, account.TenDangNhap, account.HoTen, account.VaiTro));
    }
}

public sealed class BookingService(IStudioRepository repo, IClock clock)
{
    public Task<IReadOnlyList<LichChup>> SearchAsync(string? q, DateTime? from, DateTime? to, string? status, CancellationToken ct = default)
        => repo.SearchBookingsAsync(q, from, to, status, ct);

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
