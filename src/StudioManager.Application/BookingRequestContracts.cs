using StudioManager.Domain;

namespace StudioManager.Application;

public interface IBookingRequestRepository
{
    Task<Result<BookingCatalogPage>> CatalogAsync(BookingActor actor, BookingCatalogKind kind, int? afterId, CancellationToken ct = default);
    Task<Result<BookingQuote>> QuoteAsync(CustomerBookingActor actor, BookingRequestDraft draft, DateTimeOffset now, CancellationToken ct = default);
    Task<Result<long>> SubmitAsync(CustomerBookingActor actor, BookingQuote quote, DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<BookingRequestSummary>> ListAsync(BookingActor actor, BookingRequestState? state, long? beforeId, CancellationToken ct = default);
    Task<BookingRequestDetails?> GetAsync(BookingActor actor, long requestId, CancellationToken ct = default);
    Task<Result> CancelAsync(CustomerBookingActor actor, long requestId, string reason, CancellationToken ct = default);
    Task<Result> CheckAvailabilityAsync(StaffBookingActor actor, long requestId, BookingApproval input, DateTimeOffset now, CancellationToken ct = default);
    Task<Result> ReviewAsync(StaffBookingActor actor, long requestId, bool approve, BookingApproval? input, string reason, DateTimeOffset now, CancellationToken ct = default);
    Task<OfficialRequestBooking?> GetOfficialAsync(BookingActor actor, long requestId, CancellationToken ct = default);
    Task<BookingPaymentSummary?> GetPaymentsAsync(CustomerBookingActor actor, long requestId, long? beforeId, CancellationToken ct = default);
    Task<PaymentReceipt?> GetReceiptAsync(CustomerBookingActor actor, long requestId, long paymentId, CancellationToken ct = default);
}

public sealed class BookingRequestRuleException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public static class BookingRequestErrors
{
    public static string Message(string code) => code switch
    {
        "UNAUTHORIZED" => "Phiên đăng nhập hoặc quyền truy cập đã thay đổi. Vui lòng đăng nhập lại.",
        "NOT_FOUND" => "Không tìm thấy yêu cầu hoặc lịch thuộc quyền truy cập của bạn.",
        "INVALID_INPUT" => "Thông tin yêu cầu không hợp lệ.",
        "INVALID_TIME" => "Chọn giờ Việt Nam trong tương lai (ít nhất 5 phút), tối đa 365 ngày; thời lượng từ 1 đến 1440 phút.",
        "INVALID_QUANTITY" => "Mỗi dịch vụ chỉ chọn một lần; số lượng 0,01–1000, tối đa hai chữ số thập phân, tối đa 20 dịch vụ.",
        "INACTIVE_PACKAGE" => "Gói chụp không còn áp dụng hoặc không hợp lệ.",
        "INACTIVE_SERVICE" => "Dịch vụ không còn cung cấp hoặc cần quy trình phân công tài nguyên riêng.",
        "ALLOCATION_UNAVAILABLE" => "Phòng hoặc nhiếp ảnh gia không còn hoạt động/không hợp lệ.",
        "BOOKING_CONTACT_REQUIRED" => "Cập nhật hồ sơ liên hệ trước khi đặt lịch: họ tên, email, địa chỉ và số điện thoại tối đa 15 ký tự.",
        "PREVIEW_REQUIRED" => "Vui lòng xem trước và xác nhận lại yêu cầu trong phiên đăng nhập hiện tại.",
        "STALE_QUOTE" => "Danh mục hoặc thông tin liên hệ vừa thay đổi. Vui lòng xem trước báo giá mới.",
        "STALE_REQUEST" => "Giá trị đang duyệt khác yêu cầu đã lưu. Vui lòng tải lại chi tiết.",
        "STATE_CHANGED" => "Yêu cầu đã được xử lý hoặc rút. Vui lòng tải lại trạng thái.",
        "CONFLICT" => "Phòng hoặc nhiếp ảnh gia đã có lịch trong khoảng giờ này. Yêu cầu vẫn chưa được duyệt.",
        "REASON_REQUIRED" => "Nhập lý do rõ ràng từ 5 đến 500 ký tự (nội dung này được hiển thị cho khách).",
        "SUBMISSION_KEY_REUSED" => "Mã gửi đã được dùng cho nội dung khác. Vui lòng tải lịch sử trước khi tạo yêu cầu mới.",
        "PRICE_LIMIT" => "Giá trị báo giá vượt giới hạn lưu trữ.",
        "PRICE_PRECISION" => "Tiền từng dòng phải chính xác đến 0,01 đ. Vui lòng đổi số lượng; ứng dụng không tự làm tròn.",
        _ => "Không thể hoàn tất yêu cầu đặt lịch. Vui lòng tải lại trạng thái."
    };
    public static Result Fail(string code) => Result.Fail(code, Message(code));
    public static Result<T> Fail<T>(string code) => Result<T>.Fail(code, Message(code));
}
