using StudioManager.Application;
using StudioManager.CustomerApi;
using StudioManager.Domain;

namespace StudioManager.IdentityService;

public sealed partial class IdentityDispatcher
{
    private static bool IsBookingCommand(IdentityCommand command)
        => command >= IdentityCommand.GetBookingCatalog && command <= IdentityCommand.GetMyBookingReceipt;

    private async Task<IdentityResponse> DispatchBookingAsync(IdentityRequest request, CancellationToken ct)
    {
        var id = request.BookingRequestId ?? 0;
        switch (request.Command)
        {
            case IdentityCommand.GetMyBookingPayments:
                var payments=await booking!.MyPaymentsAsync(id,request.BeforePaymentId,ct);
                return new(payments.Success,payments.Code,payments.Message){Payments=payments.Data is { } balance
                    ?new(balance.BookingCode,balance.Status,balance.Total,balance.Collected,balance.Refunded,balance.Remaining,
                        balance.Payments.Select(p=>new PaymentRowDto(p.Id,p.Code,p.Purpose,p.Amount,p.Method,p.PaidAt,p.CanViewReceipt)).ToArray(),balance.NextBeforeId):null};
            case IdentityCommand.GetMyBookingReceipt:
                var receipt=await booking!.MyReceiptAsync(id,request.PaymentId??0,ct);
                return new(receipt.Success,receipt.Code,receipt.Message){Receipt=receipt.Data is { } r?new(r.Id,r.Code,r.BookingCode,r.CustomerName,r.Phone,r.PackageName,r.PackagePrice,
                    r.Services.Select(s=>new PaymentLineDto(s.Name,s.Unit,s.UnitPrice,s.Quantity)).ToArray(),r.Discount,r.BookingTotal,r.Amount,r.Method,r.Purpose,r.PaidAt,r.StaffName,r.BalanceAfter,r.HasSnapshot,r.Note):null};
            case IdentityCommand.GetBookingCatalog:
            case IdentityCommand.GetBookingStaffCatalog:
                if (request.BookingCatalogQuery is not { } catalog) return Response(BookingRequestErrors.Fail("INVALID_INPUT"));
                var page = await booking!.CatalogAsync(request.Command == IdentityCommand.GetBookingStaffCatalog,
                    (BookingCatalogKind)catalog.Kind, catalog.AfterId, ct);
                return new(page.Success, page.Code, page.Message) { BookingCatalog = page.Data is { } p
                    ? new(p.Items.Select(c => new BookingCatalogItemDto(c.Id, c.Code, c.Name, c.Description, c.Price, c.Unit, c.DurationMinutes)).ToArray(), p.NextAfterId) : null };
            case IdentityCommand.PreviewBookingRequest:
                if (request.BookingDraft is not { } draft || draft.Addons is null || draft.Addons.Count > 20 || draft.Addons.Any(a => a is null))
                    return Response(BookingRequestErrors.Fail("INVALID_INPUT"));
                var quote = await booking!.PreviewAsync(new(draft.SubmissionKey, draft.PackageId, draft.StartUtc,
                    draft.PreferredRoomId, draft.Notes, draft.Addons.Select(a => new BookingAddonSelection(a.ServiceId, a.Quantity)).ToArray()), ct);
                return new(quote.Success, quote.Code, quote.Message) { BookingQuote = quote.Data is { } q ? Quote(q) : null };
            case IdentityCommand.SubmitBookingRequest:
                var submitted = await booking!.SubmitAsync(request.BookingSubmissionKey ?? Guid.Empty, ct);
                return new(submitted.Success, submitted.Code, submitted.Message) { CreatedRequestId = submitted.Success ? submitted.Data : null };
            case IdentityCommand.CancelBookingRequest:
                return Response(await booking!.CancelAsync(id, request.Reason!, ct));
            case IdentityCommand.GetMyBookingRequests:
            case IdentityCommand.GetBookingReviewQueue:
                BookingRequestState? state = null;
                if (request.BookingFilter?.State is { } filter)
                {
                    if (filter is not ("PENDING" or "APPROVED" or "REJECTED" or "CANCELLED"))
                        return Response(BookingRequestErrors.Fail("INVALID_INPUT"));
                    state = Enum.Parse<BookingRequestState>(filter, true);
                }
                var list = await booking!.ListAsync(request.Command == IdentityCommand.GetBookingReviewQueue, state, request.BookingFilter?.BeforeId, ct);
                return new(list.Success, list.Code, list.Message) { BookingRequests = list.Data?.Select(r => new BookingRequestRowDto(r.Id,
                    r.State.ToString().ToUpperInvariant(), r.CustomerName, r.PackageName, r.StartUtc, r.SubmittedAtUtc, r.Estimate, r.BookingCode)).ToArray() };
            case IdentityCommand.GetMyBookingRequest:
            case IdentityCommand.GetBookingReview:
                var detail = await booking!.GetAsync(request.Command == IdentityCommand.GetBookingReview, id, ct);
                return new(detail.Success, detail.Code, detail.Message) { BookingDetail = detail.Data is { } d ? new(d.Id, d.State.ToString().ToUpperInvariant(),
                    Quote(d.Quote), d.SubmittedAtUtc, d.Feedback, d.ReviewedAtUtc, d.CancelledAtUtc, d.OfficialBookingId, d.OfficialBookingCode,
                    d.ConfirmedTotal, d.History.Select(h => new BookingEventDto(h.State.ToString().ToUpperInvariant(), h.AtUtc, h.Actor, h.Reason)).ToArray()) : null };
            case IdentityCommand.CheckBookingAvailability:
                return request.BookingApproval is { } check
                    ? Response(await booking!.CheckAsync(id, new(check.PhotographerId, check.RoomId, check.ExpectedEstimate, request.Reason ?? ""), ct))
                    : Response(BookingRequestErrors.Fail("INVALID_INPUT"));
            case IdentityCommand.ApproveBookingRequest:
                return request.BookingApproval is { } approval
                    ? Response(await booking!.ReviewAsync(id, true, new(approval.PhotographerId, approval.RoomId, approval.ExpectedEstimate, request.Reason!), request.Reason!, ct))
                    : Response(BookingRequestErrors.Fail("INVALID_INPUT"));
            case IdentityCommand.RejectBookingRequest:
                return Response(await booking!.ReviewAsync(id, false, null, request.Reason!, ct));
            case IdentityCommand.GetMyRequestBooking:
            case IdentityCommand.GetReviewedRequestBooking:
                var official = await booking!.OfficialAsync(request.Command == IdentityCommand.GetReviewedRequestBooking, id, ct);
                return new(official.Success, official.Code, official.Message) { OfficialBooking = official.Data is { } o
                    ? new(o.Id, o.Code, o.Status, o.StartUtc, o.EndUtc, o.PackageName, o.PackagePrice, o.ServicesTotal, o.Total, o.Room, o.Photographer) : null };
            default: return Invalid();
        }
    }
    private static BookingQuoteDto Quote(BookingQuote q) => new(q.Draft.SubmissionKey, q.Package.Id, q.Package.Code, q.Package.Name, q.Package.Price,
        q.Package.DurationMinutes, q.Draft.StartUtc, q.Draft.PreferredRoomId, q.RoomName,
        new(q.Contact.FullName, q.Contact.Phone, q.Contact.Email, q.Contact.Address), q.Draft.Notes,
        q.Addons.Select(a => new BookingAddonDto(a.ServiceId, a.Name, a.Unit, a.UnitPrice, a.Quantity)).ToArray(), q.Estimate);
}
