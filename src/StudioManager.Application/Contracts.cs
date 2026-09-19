using StudioManager.Domain;

namespace StudioManager.Application;

public sealed record DashboardData(int LichHomNay, int LichSapToi, int DangXuLy, int ChoGiao, decimal CongNo,
    IReadOnlyList<LichChup> LichGanNhat, IReadOnlyDictionary<string, int> TheoTrangThai, IReadOnlyList<(string Thang, decimal ThucThu)> DoanhThu6Thang);
public sealed record BookingInput(long KhachHangId, int GoiChupId, DateTime BatDau, DateTime KetThuc, int NhiepAnhGiaId, int PhongChupId, string? GhiChu);
public sealed record PaymentInput(long LichChupId, string LoaiThu, decimal SoTien, string? GhiChu);
public sealed record RefundInput(long LichChupId, decimal SoTien, string LyDo, string? GhiChu);
public sealed record LookupItem(long Id, string Code, string Name, string? Extra = null);
public sealed record ReportData(decimal DoanhThu, decimal TongThu, decimal TongHoan, decimal ThucThu, decimal CongNo,
    int TongLich, int HoanThanh, int DaHuy, IReadOnlyList<(string Name, decimal Value)> ByPackage);

public interface IStudioRepository
{
    Task<TaiKhoan?> FindAccountAsync(string username, CancellationToken ct = default);
    Task UpdateLastLoginAsync(int id, DateTime time, CancellationToken ct = default);
    Task<IReadOnlyList<LichChup>> SearchBookingsAsync(string? keyword, DateTime? from, DateTime? to, string? status, CancellationToken ct = default);
    Task<LichChup?> GetBookingAsync(long id, CancellationToken ct = default);
    Task<bool> HasConflictAsync(long? excludeId, int photographerId, int roomId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<long> CreateBookingAsync(BookingInput input, UserSession user, CancellationToken ct = default);
    Task<Result> UpdateBookingAsync(long id, BookingInput input, string? reason, UserSession user, CancellationToken ct = default);
    Task<Result> UpdateBookingStatusAsync(long id, TrangThaiLich next, string? reason, UserSession user, CancellationToken ct = default);
    Task<Result> CancelBookingAsync(long id, string reason, UserSession user, CancellationToken ct = default);
    Task<Result> AddPaymentAsync(PaymentInput input, UserSession user, CancellationToken ct = default);
    Task<Result> AddRefundAsync(RefundInput input, UserSession user, CancellationToken ct = default);
    Task<DashboardData> GetDashboardAsync(CancellationToken ct = default);
    Task<ReportData> GetReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<LookupItem>> GetLookupsAsync(string type, bool activeOnly = true, CancellationToken ct = default);
    Task<IReadOnlyList<IDictionary<string, object?>>> QueryGridAsync(string entity, string? keyword, CancellationToken ct = default);
    Task<Result> SaveSimpleAsync(string entity, long? id, IReadOnlyDictionary<string, object?> values, UserSession user, CancellationToken ct = default);
    Task<Result> DeactivateAsync(string entity, long id, UserSession user, CancellationToken ct = default);
    Task<Result> CreateAccountAsync(string username, string passwordHash, int? employeeId, VaiTro role, UserSession user, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(int accountId, string passwordHash, UserSession user, CancellationToken ct = default);
    Task<IReadOnlyList<IDictionary<string, object?>>> GetBookingChildrenAsync(long bookingId, string type, CancellationToken ct = default);
    Task<Result> AddBookingServiceAsync(long bookingId, int serviceId, decimal quantity, UserSession user, CancellationToken ct = default);
    Task<Result> SetDiscountAsync(long bookingId, decimal amount, string? reason, UserSession user, CancellationToken ct = default);
    Task<Result> AssignResourceAsync(long bookingId, int resourceId, int quantity, UserSession user, CancellationToken ct = default);
    Task<Result> BackupAsync(string path, UserSession user, CancellationToken ct = default);
    Task<Result> RestoreAsync(string path, UserSession user, CancellationToken ct = default);
}
