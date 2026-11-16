namespace StudioManager.Domain;

public enum BookingRequestState { Pending, Approved, Rejected, Cancelled }
public enum BookingCatalogKind { Packages, Services, Rooms, Photographers }
public sealed record BookingCatalogItem(int Id, string Code, string Name, string Description, decimal Price, string Unit, int DurationMinutes);
public sealed record BookingCatalogPage(IReadOnlyList<BookingCatalogItem> Items, int? NextAfterId);
public sealed record BookingAddonSelection(int ServiceId, decimal Quantity);
public sealed record BookingRequestDraft(Guid SubmissionKey, int PackageId, DateTimeOffset StartUtc, int? PreferredRoomId,
    string Notes, IReadOnlyList<BookingAddonSelection> Addons);
public sealed record BookingAddonSnapshot(int ServiceId, string Name, string Unit, decimal UnitPrice, decimal Quantity);
public sealed record BookingQuote(BookingRequestDraft Draft, BookingCatalogItem Package, string? RoomName,
    CustomerContact Contact, IReadOnlyList<BookingAddonSnapshot> Addons, decimal Estimate, string Fingerprint)
{
    public override string ToString() => "BookingQuote { Payload = [private] }";
}
public sealed record BookingRequestEvent(BookingRequestState State, DateTimeOffset AtUtc, string Actor, string Reason);
public sealed record BookingRequestSummary(long Id, BookingRequestState State, string CustomerName, string PackageName,
    DateTimeOffset StartUtc, DateTimeOffset SubmittedAtUtc, decimal Estimate, string? BookingCode);
public sealed record BookingRequestDetails(long Id, long AccountId, BookingRequestState State, BookingQuote Quote,
    DateTimeOffset SubmittedAtUtc, string? Feedback, DateTimeOffset? ReviewedAtUtc, DateTimeOffset? CancelledAtUtc,
    long? OfficialBookingId, string? OfficialBookingCode, decimal? ConfirmedTotal, IReadOnlyList<BookingRequestEvent> History);
public sealed record BookingApproval(int PhotographerId, int RoomId, decimal ExpectedEstimate, string Reason);
public sealed record OfficialRequestBooking(long Id, string Code, string Status, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    string PackageName, decimal PackagePrice, decimal ServicesTotal, decimal Total, string Room, string Photographer);

// Backend-only capabilities; never accepted from or serialized to the customer transport.
public abstract record BookingActor;
public sealed record CustomerBookingActor(CustomerCredential Credential) : BookingActor;
public sealed record StaffBookingActor(CustomerReviewer Credential) : BookingActor;

public static class BookingRequestRules
{
    public static bool CanTransition(BookingRequestState from, BookingRequestState to, bool staff)
        => from == BookingRequestState.Pending && (staff
            ? to is BookingRequestState.Approved or BookingRequestState.Rejected
            : to == BookingRequestState.Cancelled);
}

public static class StudioBookingTime
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    public static DateTime ToLegacyLocal(DateTimeOffset utc) => utc.ToOffset(VietnamOffset).DateTime;
    public static DateTimeOffset FromLegacyLocal(DateTime value)
        => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), VietnamOffset).ToUniversalTime();
    public static DateTimeOffset FromVietnamInput(DateTime value) => FromLegacyLocal(value);
}
