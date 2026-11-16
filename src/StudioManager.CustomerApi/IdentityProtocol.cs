using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudioManager.CustomerApi;

public enum IdentityCommand
{
    Register, CustomerLogin, StaffLogin, Logout, GetMyProfile, UpdateMyProfile, ChangePassword,
    RequestLink, GetMyLink, GetMyLinkedCustomer, ListLinkReviews, ApproveLink, RejectLink, RevokeLink,
    GetBookingCatalog, GetBookingStaffCatalog, PreviewBookingRequest, SubmitBookingRequest, CancelBookingRequest,
    GetMyBookingRequests, GetMyBookingRequest, GetBookingReviewQueue, GetBookingReview, CheckBookingAvailability,
    ApproveBookingRequest, RejectBookingRequest, GetMyRequestBooking, GetReviewedRequestBooking,
    GetMyBookingPayments, GetMyBookingReceipt
}
public sealed record ContactDto(string FullName, string Phone, string Email, string Address);
public sealed record ProfileDto(string LoginEmail, ContactDto Contact);
public sealed record LinkDto(long Id, string State, string Reference, DateTime RequestedAtUtc, string? CustomerCode);
public sealed record LinkReviewDto(long Id, long AccountId, string LoginEmail, string FullName, string Reference, string Evidence, string State, long? CustomerId);
public sealed record LinkedCustomerDto(string Code, string FullName, string Phone, string? Email, string? Address);

public sealed record IdentityRequest(IdentityCommand Command)
{
    public string? Identifier { get; init; }
    public string? Password { get; init; }
    public string? NewPassword { get; init; }
    public string? Confirmation { get; init; }
    public ContactDto? Contact { get; init; }
    public string? Reference { get; init; }
    public string? Evidence { get; init; }
    public long LinkId { get; init; }
    public long? BeforeLinkId { get; init; }
    public long? CustomerId { get; init; } // A staff selection only; never a customer actor/owner.
    public string? Reason { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingCatalogQueryDto? BookingCatalogQuery { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingDraftDto? BookingDraft { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingApprovalDto? BookingApproval { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingFilterDto? BookingFilter { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? BookingRequestId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? BookingSubmissionKey { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? PaymentId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? BeforePaymentId { get; init; }
    public override string ToString() => $"IdentityRequest {{ Command = {Command}, Payload = [private] }}";
}
public sealed record IdentityResponse(bool Success, string Code, string Message)
{
    public ProfileDto? Profile { get; init; }
    public LinkDto? Link { get; init; }
    public LinkedCustomerDto? LinkedCustomer { get; init; }
    public IReadOnlyList<LinkReviewDto>? Reviews { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingCatalogPageDto? BookingCatalog { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingQuoteDto? BookingQuote { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<BookingRequestRowDto>? BookingRequests { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingDetailDto? BookingDetail { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OfficialBookingDto? OfficialBooking { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CreatedRequestId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BookingPaymentsDto? Payments { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaymentReceiptDto? Receipt { get; init; }
    public override string ToString() => $"IdentityResponse {{ Success = {Success}, Code = {Code} }}";
}

public static class IdentityProtocol
{
    public const string ServiceName = "StudioManagerIdentity";
    public const string PipeName = "StudioManager.Identity.v1";
    public const int MaxMessageBytes = 256 * 1024;
    public static readonly TimeSpan ConnectionIdleTimeout = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ClientIdleTimeout = TimeSpan.FromSeconds(110);
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public static bool RequiresSignIn(IdentityResponse response) => response.Code is
        "UNAUTHORIZED" or "SESSION_EXPIRED" or "CONNECTION_LOST" or "OUTCOME_UNKNOWN" or
        "SERVICE_UNAVAILABLE" or "SERVICE_ERROR" or "RATE_LIMITED" or "INVALID_REQUEST" or "CLIENT_CLOSED";

    private static readonly JsonSerializerOptions Json = new()
    {
        MaxDepth = 12, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken ct = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        if (payload.Length is <= 0 or > MaxMessageBytes) throw new InvalidDataException("Message size refused.");
        var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, ct); await stream.WriteAsync(payload, ct); await stream.FlushAsync(ct);
    }

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken ct = default)
    {
        var header = new byte[4]; await stream.ReadExactlyAsync(header, ct);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is <= 0 or > MaxMessageBytes) throw new InvalidDataException("Message size refused.");
        var bytes = new byte[length]; await stream.ReadExactlyAsync(bytes, ct);
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new InvalidDataException("Message refused.");
    }
}
