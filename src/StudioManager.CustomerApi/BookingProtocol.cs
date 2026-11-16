namespace StudioManager.CustomerApi;

public enum BookingCatalogType { Packages, Services, Rooms, Photographers }
public sealed record BookingCatalogQueryDto(BookingCatalogType Kind, int? AfterId = null);
public sealed record BookingCatalogItemDto(int Id, string Code, string Name, string Description, decimal Price, string Unit, int DurationMinutes);
public sealed record BookingCatalogPageDto(IReadOnlyList<BookingCatalogItemDto> Items, int? NextAfterId);
public sealed record BookingAddonSelectionDto(int ServiceId, decimal Quantity);
public sealed record BookingDraftDto(Guid SubmissionKey, int PackageId, DateTimeOffset StartUtc, int? PreferredRoomId, string Notes,
    IReadOnlyList<BookingAddonSelectionDto> Addons);
public sealed record BookingAddonDto(int ServiceId, string Name, string Unit, decimal UnitPrice, decimal Quantity);
public sealed record BookingApprovalDto(int PhotographerId, int RoomId, decimal ExpectedEstimate);
public sealed record BookingFilterDto(string? State = null, long? BeforeId = null);
public sealed record BookingQuoteDto(Guid SubmissionKey, int PackageId, string PackageCode, string PackageName, decimal PackagePrice,
    int DurationMinutes, DateTimeOffset StartUtc, int? PreferredRoomId, string? PreferredRoomName, ContactDto Contact, string Notes,
    IReadOnlyList<BookingAddonDto> Addons, decimal Estimate);
public sealed record BookingRequestRowDto(long Id, string State, string CustomerName, string PackageName, DateTimeOffset StartUtc,
    DateTimeOffset SubmittedAtUtc, decimal Estimate, string? BookingCode);
public sealed record BookingEventDto(string State, DateTimeOffset AtUtc, string Actor, string Reason);
public sealed record BookingDetailDto(long Id, string State, BookingQuoteDto Quote, DateTimeOffset SubmittedAtUtc, string? Feedback,
    DateTimeOffset? ReviewedAtUtc, DateTimeOffset? CancelledAtUtc, long? OfficialBookingId, string? OfficialBookingCode,
    decimal? ConfirmedTotal, IReadOnlyList<BookingEventDto> History);
public sealed record OfficialBookingDto(long Id, string Code, string Status, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    string PackageName, decimal PackagePrice, decimal ServicesTotal, decimal Total, string Room, string Photographer);
