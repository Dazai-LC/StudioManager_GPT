using System.Security.Cryptography;
using System.Text.Json;
using StudioManager.Domain;

namespace StudioManager.Application;

public static class BookingRequestPolicy
{
    public static Result ValidateDraft(BookingRequestDraft? draft, DateTimeOffset now)
    {
        if (draft is null || draft.SubmissionKey == Guid.Empty || draft.PackageId <= 0 ||
            draft.PreferredRoomId is <= 0 || draft.Notes is null || draft.Notes.Length > 1000)
            return BookingRequestErrors.Fail("INVALID_INPUT");
        if (draft.StartUtc.Offset != TimeSpan.Zero || draft.StartUtc.Ticks % TimeSpan.TicksPerSecond != 0 ||
            draft.StartUtc < now.AddMinutes(5) || draft.StartUtc > now.AddDays(365))
            return BookingRequestErrors.Fail("INVALID_TIME");
        if (draft.Addons is null || draft.Addons.Count > 20 || draft.Addons.Any(a => a is null || a.ServiceId <= 0 ||
            a.Quantity <= 0 || a.Quantity > 1000 || decimal.Round(a.Quantity, 2) != a.Quantity) ||
            draft.Addons.Select(a => a.ServiceId).Distinct().Count() != draft.Addons.Count)
            return BookingRequestErrors.Fail("INVALID_QUANTITY");
        return Result.Ok();
    }

    public static Result<BookingQuote> BuildQuote(BookingRequestDraft draft, BookingCatalogItem? package,
        IReadOnlyList<BookingCatalogItem> services, BookingCatalogItem? room, CustomerContact contact, DateTimeOffset now)
    {
        var valid = ValidateDraft(draft, now);
        if (!valid.Success) return Result<BookingQuote>.Fail(valid.Code, valid.Message);
        if (package is null || package.Id != draft.PackageId) return BookingRequestErrors.Fail<BookingQuote>("INACTIVE_PACKAGE");
        if (package.DurationMinutes is < 1 or > 1440) return BookingRequestErrors.Fail<BookingQuote>("INVALID_TIME");
        if (draft.PreferredRoomId is { } roomId && (room is null || room.Id != roomId))
            return BookingRequestErrors.Fail<BookingQuote>("ALLOCATION_UNAVAILABLE");
        if (string.IsNullOrWhiteSpace(contact.FullName) || contact.FullName.Length > 100 ||
            string.IsNullOrWhiteSpace(contact.Phone) || contact.Phone.Length > 15 || string.IsNullOrWhiteSpace(contact.Email) ||
            contact.Email.Length > 254 || contact.Address is null || contact.Address.Length > 255)
            return BookingRequestErrors.Fail<BookingQuote>("BOOKING_CONTACT_REQUIRED");
        var addons = new List<BookingAddonSnapshot>();
        foreach (var selection in draft.Addons.OrderBy(a => a.ServiceId))
        {
            var item = services.SingleOrDefault(s => s.Id == selection.ServiceId);
            if (item is null) return BookingRequestErrors.Fail<BookingQuote>("INACTIVE_SERVICE");
            addons.Add(new(item.Id, item.Name, item.Unit, item.Price, selection.Quantity));
        }
        try
        {
            var settlement = BookingMoneyPolicy.QuoteTotal(package.Price, addons);
            if (!settlement.Success) return Result<BookingQuote>.Fail(settlement.Code, settlement.Message);
            var estimate = settlement.Data;
            var clean = draft with { Notes = draft.Notes.Trim(), Addons = draft.Addons.OrderBy(a => a.ServiceId).Select(a => a with { }).ToArray() };
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                new { Draft = clean, Package = package, Room = room?.Name, Contact = contact, Addons = addons, Estimate = estimate })));
            return Result<BookingQuote>.Ok(new(clean, package, room?.Name, contact with { }, addons.ToArray(), estimate, fingerprint));
        }
        catch (OverflowException) { return BookingRequestErrors.Fail<BookingQuote>("PRICE_LIMIT"); }
    }

    public static Result ValidateApproval(BookingApproval? input, bool requireReason)
        => input is null || input.PhotographerId <= 0 || input.RoomId <= 0 || input.ExpectedEstimate < 0
            ? BookingRequestErrors.Fail("INVALID_INPUT")
            : requireReason && !ValidReason(input.Reason) ? BookingRequestErrors.Fail("REASON_REQUIRED") : Result.Ok();
    public static bool ValidReason(string? reason) => reason is not null && reason.Trim().Length is >= 5 and <= 500;
}
