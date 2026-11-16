using StudioManager.Domain;

namespace StudioManager.Application.Services;

/// <summary>Per-connection P04 authority; the P03 session supplies all principals.</summary>
public sealed class BookingRequestService(CustomerIdentityService identity, IBookingRequestRepository repository, IClock clock)
{
    private BookingQuote? _quote;
    private CustomerCredential? _quotedCustomer;
    private DateTimeOffset Now => new(DateTime.SpecifyKind(clock.Now, DateTimeKind.Utc));
    public void ClearPreview() { _quote = null; _quotedCustomer = null; }
    private async Task<CustomerBookingActor?> Customer(CancellationToken ct)
        => await identity.BookingCustomerAsync(ct) is { } customer ? new(customer) : null;
    private async Task<StaffBookingActor?> Staff(CancellationToken ct)
        => await identity.BookingReviewerAsync(ct) is { } reviewer ? new(reviewer) : null;

    public async Task<Result<BookingCatalogPage>> CatalogAsync(bool staff, BookingCatalogKind kind, int? afterId, CancellationToken ct = default)
    {
        BookingActor? actor = staff ? await Staff(ct) : await Customer(ct);
        if (actor is null) return BookingRequestErrors.Fail<BookingCatalogPage>("UNAUTHORIZED");
        if (!Enum.IsDefined(kind) || afterId is <= 0 || (!staff && kind == BookingCatalogKind.Photographers))
            return BookingRequestErrors.Fail<BookingCatalogPage>("INVALID_INPUT");
        return await repository.CatalogAsync(actor, kind, afterId, ct);
    }

    public async Task<Result<BookingQuote>> PreviewAsync(BookingRequestDraft draft, CancellationToken ct = default)
    {
        ClearPreview();
        var actor = await Customer(ct);
        if (actor is null) return BookingRequestErrors.Fail<BookingQuote>("UNAUTHORIZED");
        var valid = BookingRequestPolicy.ValidateDraft(draft, Now);
        if (!valid.Success) return Result<BookingQuote>.Fail(valid.Code, valid.Message);
        var quote = await repository.QuoteAsync(actor, draft, Now, ct);
        if (quote.Success) { _quote = quote.Data; _quotedCustomer = actor.Credential; }
        return quote;
    }

    public async Task<Result<long>> SubmitAsync(Guid submissionKey, CancellationToken ct = default)
    {
        var actor = await Customer(ct);
        if (actor is null) { ClearPreview(); return BookingRequestErrors.Fail<long>("UNAUTHORIZED"); }
        var quote = _quote;
        if (quote is null || actor.Credential != _quotedCustomer || submissionKey != quote.Draft.SubmissionKey)
            return BookingRequestErrors.Fail<long>("PREVIEW_REQUIRED");
        ClearPreview(); // Never replay a mutation from cached state, including after an uncertain response.
        var valid = BookingRequestPolicy.ValidateDraft(quote.Draft, Now);
        return !valid.Success ? Result<long>.Fail(valid.Code, valid.Message) : await repository.SubmitAsync(actor, quote, Now, ct);
    }

    public async Task<Result<IReadOnlyList<BookingRequestSummary>>> ListAsync(bool staff, BookingRequestState? state, long? beforeId, CancellationToken ct = default)
    {
        BookingActor? actor = staff ? await Staff(ct) : await Customer(ct);
        if (actor is null) return BookingRequestErrors.Fail<IReadOnlyList<BookingRequestSummary>>("UNAUTHORIZED");
        if ((state is { } value && !Enum.IsDefined(value)) || beforeId is <= 0)
            return BookingRequestErrors.Fail<IReadOnlyList<BookingRequestSummary>>("INVALID_INPUT");
        return Result<IReadOnlyList<BookingRequestSummary>>.Ok(await repository.ListAsync(actor, state, beforeId, ct));
    }

    public async Task<Result<BookingRequestDetails>> GetAsync(bool staff, long id, CancellationToken ct = default)
    {
        BookingActor? actor = staff ? await Staff(ct) : await Customer(ct);
        if (actor is null) return BookingRequestErrors.Fail<BookingRequestDetails>("UNAUTHORIZED");
        if (id <= 0) return BookingRequestErrors.Fail<BookingRequestDetails>("INVALID_INPUT");
        var detail = await repository.GetAsync(actor, id, ct);
        return detail is null ? BookingRequestErrors.Fail<BookingRequestDetails>("NOT_FOUND") : Result<BookingRequestDetails>.Ok(detail);
    }

    public async Task<Result> CancelAsync(long id, string reason, CancellationToken ct = default)
    {
        var actor = await Customer(ct);
        if (actor is null) return BookingRequestErrors.Fail("UNAUTHORIZED");
        if (id <= 0) return BookingRequestErrors.Fail("INVALID_INPUT");
        if (!BookingRequestPolicy.ValidReason(reason)) return BookingRequestErrors.Fail("REASON_REQUIRED");
        return await repository.CancelAsync(actor, id, reason.Trim(), ct);
    }

    public async Task<Result> CheckAsync(long id, BookingApproval input, CancellationToken ct = default)
    {
        var actor = await Staff(ct);
        if (actor is null) return BookingRequestErrors.Fail("UNAUTHORIZED");
        var valid = BookingRequestPolicy.ValidateApproval(input, false);
        if (id <= 0) return BookingRequestErrors.Fail("INVALID_INPUT");
        return !valid.Success ? valid : await repository.CheckAvailabilityAsync(actor, id, input, Now, ct);
    }

    public async Task<Result> ReviewAsync(long id, bool approve, BookingApproval? input, string reason, CancellationToken ct = default)
    {
        var actor = await Staff(ct);
        if (actor is null) return BookingRequestErrors.Fail("UNAUTHORIZED");
        if (id <= 0) return BookingRequestErrors.Fail("INVALID_INPUT");
        if (!BookingRequestPolicy.ValidReason(reason)) return BookingRequestErrors.Fail("REASON_REQUIRED");
        if (approve)
        {
            var valid = BookingRequestPolicy.ValidateApproval(input, false);
            if (!valid.Success) return valid;
        }
        return await repository.ReviewAsync(actor, id, approve, input, reason.Trim(), Now, ct);
    }

    public async Task<Result<OfficialRequestBooking>> OfficialAsync(bool staff, long id, CancellationToken ct = default)
    {
        BookingActor? actor = staff ? await Staff(ct) : await Customer(ct);
        if (actor is null) return BookingRequestErrors.Fail<OfficialRequestBooking>("UNAUTHORIZED");
        if (id <= 0) return BookingRequestErrors.Fail<OfficialRequestBooking>("INVALID_INPUT");
        var booking = await repository.GetOfficialAsync(actor, id, ct);
        return booking is null ? BookingRequestErrors.Fail<OfficialRequestBooking>("NOT_FOUND") : Result<OfficialRequestBooking>.Ok(booking);
    }
    public async Task<Result<BookingPaymentSummary>> MyPaymentsAsync(long requestId,long? beforeId=null,CancellationToken ct=default)
    {
        var actor=await Customer(ct);
        if(actor is null)return BookingRequestErrors.Fail<BookingPaymentSummary>("UNAUTHORIZED");
        if(requestId<=0 || beforeId is <=0)return BookingRequestErrors.Fail<BookingPaymentSummary>("INVALID_INPUT");
        var result=await repository.GetPaymentsAsync(actor,requestId,beforeId,ct);
        return result is null?BookingRequestErrors.Fail<BookingPaymentSummary>("NOT_FOUND"):Result<BookingPaymentSummary>.Ok(result);
    }
    public async Task<Result<PaymentReceipt>> MyReceiptAsync(long requestId,long paymentId,CancellationToken ct=default)
    {
        var actor=await Customer(ct);
        if(actor is null)return BookingRequestErrors.Fail<PaymentReceipt>("UNAUTHORIZED");
        if(requestId<=0 || paymentId<=0)return BookingRequestErrors.Fail<PaymentReceipt>("INVALID_INPUT");
        var result=await repository.GetReceiptAsync(actor,requestId,paymentId,ct);
        return result is null?BookingRequestErrors.Fail<PaymentReceipt>("NOT_FOUND"):Result<PaymentReceipt>.Ok(result);
    }
}
