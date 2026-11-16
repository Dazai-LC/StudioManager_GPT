using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;

namespace StudioManager.Tests;

/// <summary>Transactional in-memory adapter for application/pipe tests, not evidence of SQL Server execution.</summary>
internal sealed class BookingRequestFixture : IBookingRequestRepository
{
    internal readonly CustomerIdentityTests.Fixture Identity = new();
    internal readonly Dictionary<int, BookingCatalogItem> Packages = new()
    { [1] = new(1, "GC-TEST", "Gói thử", "Mô tả thử", 1_000_000m, "", 60) };
    internal readonly Dictionary<int, BookingCatalogItem> Services = new()
    { [1] = new(1, "DV-TEST", "Dịch vụ thử", "Mô tả", 123.44m, "bộ", 0) };
    internal readonly Dictionary<int, BookingCatalogItem> Rooms = new()
    { [1] = new(1, "P-TEST", "Phòng thử", "", 0, "", 0), [2] = new(2, "P-TEST2", "Phòng 2", "", 0, "", 0) };
    internal readonly Dictionary<int, BookingCatalogItem> Photographers = new()
    { [1] = new(1, "NV-TEST", "Nhiếp ảnh gia thử", "", 0, "", 0), [2] = new(2, "NV-TEST2", "Nhiếp ảnh gia 2", "", 0, "", 0) };
    internal readonly Dictionary<long, BookingRequestDetails> Requests = [];
    internal readonly Dictionary<long, FakeBooking> Bookings = [];
    internal readonly Dictionary<long,List<PaymentReceipt>> Receipts=[];
    internal readonly Dictionary<long, long> CreatedCustomers = [];
    internal readonly Dictionary<long, long> ResolvedCustomers = [];
    internal int AuditRows, Mutations, RepositoryCalls;
    internal bool FailBeforeApprovalCommit;
    internal bool FailAfterApprovalCommit;
    private readonly SemaphoreSlim _transaction = new(1, 1);
    private long _nextCustomer = 1001;
    internal DateTimeOffset Now => new(DateTime.SpecifyKind(Identity.Clock.Now, DateTimeKind.Utc));
    internal BookingRequestDraft Draft(Guid? key = null, int room = 1)
        => new(key ?? Guid.NewGuid(), 1, Now.AddDays(2), room, "Ghi chú thử", [new(1, 1.25m)]);
    internal static async Task<BookingRequestFixture> Create()
    {
        var f = new BookingRequestFixture(); await f.Identity.AddCustomer("a"); await f.Identity.AddCustomer("b"); return f;
    }
    internal (CustomerIdentityService Identity, BookingRequestService Service) Connection()
    {
        var identity = Identity.Connection(); return (identity, new(identity, this, Identity.Clock));
    }
    internal sealed record FakeBooking(long Id, long Account, long Customer, int Room, int Photographer, DateTimeOffset Start,
        DateTimeOffset End, BookingQuote Quote, string State = "DA_DAT_LICH");
    private async Task<T> Locked<T>(BookingActor actor, Func<T> work, CancellationToken ct)
    {
        await _transaction.WaitAsync(ct);
        try { await Task.Yield(); Validate(actor); RepositoryCalls++; return work(); }
        finally { _transaction.Release(); }
    }
    private void Validate(BookingActor actor)
    {
        if (actor is CustomerBookingActor c)
        {
            var a = Identity.Store.Accounts.GetValueOrDefault(c.Credential.AccountId);
            if (a is null || !a.Active || a.SecurityVersion != c.Credential.SecurityVersion || a.PasswordHash != c.Credential.PasswordHash)
                throw new UnauthorizedAccessException();
        }
        else if (actor is StaffBookingActor s)
        {
            var a = Identity.Directory.Accounts.GetValueOrDefault(s.Credential.AccountId);
            if (a is null || !a.HoatDong || a.PhaiDoiMatKhau || a.VaiTro != s.Credential.Role || a.MatKhauHash != s.Credential.PasswordHash)
                throw new UnauthorizedAccessException();
        }
        else throw new UnauthorizedAccessException();
    }
    private bool Visible(BookingActor actor, BookingRequestDetails r)
        => actor is StaffBookingActor || actor is CustomerBookingActor c && r.AccountId == c.Credential.AccountId;
    private Result<BookingQuote> Quote(CustomerBookingActor actor, BookingRequestDraft draft, DateTimeOffset now)
        => BookingRequestPolicy.BuildQuote(draft, Packages.GetValueOrDefault(draft.PackageId), draft.Addons.Select(a => Services.GetValueOrDefault(a.ServiceId))
            .Where(a => a is not null).Cast<BookingCatalogItem>().ToArray(), draft.PreferredRoomId is { } room ? Rooms.GetValueOrDefault(room) : null,
            Identity.Store.Accounts[actor.Credential.AccountId].Contact, now);

    public Task<Result<BookingCatalogPage>> CatalogAsync(BookingActor actor, BookingCatalogKind kind, int? afterId, CancellationToken ct = default)
        => Locked(actor, () =>
        {
            if (kind == BookingCatalogKind.Photographers && actor is not StaffBookingActor) throw new UnauthorizedAccessException();
            var items = (kind switch { BookingCatalogKind.Packages => Packages, BookingCatalogKind.Services => Services,
                BookingCatalogKind.Rooms => Rooms, _ => Photographers }).Values.Where(i => i.Id > (afterId ?? 0)).OrderBy(i => i.Id).Take(26).ToArray();
            return Result<BookingCatalogPage>.Ok(new(items.Take(25).ToArray(), items.Length > 25 ? items[24].Id : null));
        }, ct);
    public Task<Result<BookingQuote>> QuoteAsync(CustomerBookingActor actor, BookingRequestDraft draft, DateTimeOffset now, CancellationToken ct = default)
        => Locked(actor, () => Quote(actor, draft, now), ct);
    public Task<Result<long>> SubmitAsync(CustomerBookingActor actor, BookingQuote quote, DateTimeOffset now, CancellationToken ct = default)
        => Locked(actor, () =>
        {
            var existing = Requests.Values.FirstOrDefault(r => r.AccountId == actor.Credential.AccountId && r.Quote.Draft.SubmissionKey == quote.Draft.SubmissionKey);
            if (existing is not null) return existing.Quote.Fingerprint == quote.Fingerprint
                ? Result<long>.Ok(existing.Id) : BookingRequestErrors.Fail<long>("SUBMISSION_KEY_REUSED");
            var current = Quote(actor, quote.Draft, now);
            if (!current.Success) return Result<long>.Fail(current.Code, current.Message);
            if (current.Data!.Fingerprint != quote.Fingerprint) return BookingRequestErrors.Fail<long>("STALE_QUOTE");
            var id = Requests.Count + 1L;
            Requests[id] = new(id, actor.Credential.AccountId, BookingRequestState.Pending, quote, now, null, null, null, null, null, null,
                [new(BookingRequestState.Pending, now, "Khách hàng", "Đã gửi yêu cầu")]);
            Mutations++; return Result<long>.Ok(id);
        }, ct);
    public Task<IReadOnlyList<BookingRequestSummary>> ListAsync(BookingActor actor, BookingRequestState? state, long? beforeId, CancellationToken ct = default)
        => Locked<IReadOnlyList<BookingRequestSummary>>(actor, () => Requests.Values.Where(r => Visible(actor, r) && (state is null || r.State == state) && (beforeId is null || r.Id < beforeId))
            .OrderByDescending(r => r.Id).Take(25).Select(r => new BookingRequestSummary(r.Id, r.State, r.Quote.Contact.FullName, r.Quote.Package.Name,
                r.Quote.Draft.StartUtc, r.SubmittedAtUtc, r.Quote.Estimate, r.OfficialBookingCode)).ToArray(), ct);
    public Task<BookingRequestDetails?> GetAsync(BookingActor actor, long requestId, CancellationToken ct = default)
        => Locked(actor, () => Requests.TryGetValue(requestId, out var r) && Visible(actor, r) ? r : null, ct);
    public Task<Result> CancelAsync(CustomerBookingActor actor, long requestId, string reason, CancellationToken ct = default)
        => Locked(actor, () =>
        {
            if (!Requests.TryGetValue(requestId, out var r) || !Visible(actor, r)) return BookingRequestErrors.Fail("NOT_FOUND");
            if (!BookingRequestRules.CanTransition(r.State, BookingRequestState.Cancelled, false)) return BookingRequestErrors.Fail("STATE_CHANGED");
            Requests[requestId] = r with { State = BookingRequestState.Cancelled, CancelledAtUtc = Now, Feedback = reason,
                History = r.History.Append(new(BookingRequestState.Cancelled, Now, "Khách hàng", reason)).ToArray() };
            Mutations++; return Result.Ok();
        }, ct);
    private Result Availability(BookingRequestDetails r, BookingApproval input, DateTimeOffset now)
    {
        if (r.State != BookingRequestState.Pending) return BookingRequestErrors.Fail("STATE_CHANGED");
        var valid = BookingRequestPolicy.ValidateDraft(r.Quote.Draft, now); if (!valid.Success) return valid;
        if (input.ExpectedEstimate != r.Quote.Estimate) return BookingRequestErrors.Fail("STALE_REQUEST");
        var settlement = BookingMoneyPolicy.QuoteTotal(r.Quote.Package.Price, r.Quote.Addons);
        if (!settlement.Success) return Result.Fail(settlement.Code, settlement.Message);
        if (settlement.Data != r.Quote.Estimate) return BookingRequestErrors.Fail("STALE_REQUEST");
        if (!Packages.ContainsKey(r.Quote.Package.Id)) return BookingRequestErrors.Fail("INACTIVE_PACKAGE");
        if (r.Quote.Addons.Any(a => !Services.ContainsKey(a.ServiceId))) return BookingRequestErrors.Fail("INACTIVE_SERVICE");
        if (!Rooms.ContainsKey(input.RoomId) || !Photographers.ContainsKey(input.PhotographerId)) return BookingRequestErrors.Fail("ALLOCATION_UNAVAILABLE");
        var start = r.Quote.Draft.StartUtc; var end = start.AddMinutes(r.Quote.Package.DurationMinutes);
        if (Bookings.Values.Any(b => b.State != "DA_HUY" && (b.Room == input.RoomId || b.Photographer == input.PhotographerId) && b.Start < end && b.End > start))
            return BookingRequestErrors.Fail("CONFLICT");
        return Result.Ok();
    }
    public Task<Result> CheckAvailabilityAsync(StaffBookingActor actor, long requestId, BookingApproval input, DateTimeOffset now, CancellationToken ct = default)
        => Locked(actor, () => Requests.TryGetValue(requestId, out var r) ? Availability(r, input, now) : BookingRequestErrors.Fail("NOT_FOUND"), ct);
    public Task<Result> ReviewAsync(StaffBookingActor actor, long requestId, bool approve, BookingApproval? input, string reason, DateTimeOffset now, CancellationToken ct = default)
        => Locked(actor, () =>
        {
            if (!Requests.TryGetValue(requestId, out var r)) return BookingRequestErrors.Fail("NOT_FOUND");
            if (!BookingRequestRules.CanTransition(r.State, approve ? BookingRequestState.Approved : BookingRequestState.Rejected, true))
                return BookingRequestErrors.Fail("STATE_CHANGED");
            long? bookingId = null; long? newCustomer = null; var auditDelta = 1;
            FakeBooking? stagedBooking = null;
            if (approve)
            {
                var valid = Availability(r, input!, now); if (!valid.Success) return valid;
                if (!Identity.Store.Accounts[r.AccountId].Active) return BookingRequestErrors.Fail("STATE_CHANGED");
                var linked = Identity.Store.Links.Values.FirstOrDefault(l => l.Account == r.AccountId && l.State == CustomerLinkState.Approved);
                var customer = linked?.Customer;
                if (customer is null && CreatedCustomers.TryGetValue(r.AccountId, out var previous) &&
                    !Identity.Store.Links.Values.Any(l => l.Customer == previous && l.Account != r.AccountId && l.State == CustomerLinkState.Approved)) customer = previous;
                if (customer is null) { customer = _nextCustomer; newCustomer = customer; auditDelta++; }
                bookingId = Bookings.Count + 1;
                stagedBooking = new(bookingId.Value, r.AccountId, customer.Value, input!.RoomId, input.PhotographerId,
                    r.Quote.Draft.StartUtc, r.Quote.Draft.StartUtc.AddMinutes(r.Quote.Package.DurationMinutes), r.Quote);
                auditDelta++;
                if (FailBeforeApprovalCommit) throw new InvalidOperationException("synthetic-provider-secret-never-visible");
            }
            if (newCustomer is { } created) { CreatedCustomers[r.AccountId] = created; _nextCustomer++; }
            if (stagedBooking is not null) { Bookings[stagedBooking.Id] = stagedBooking; ResolvedCustomers[requestId] = stagedBooking.Customer; }
            var state = approve ? BookingRequestState.Approved : BookingRequestState.Rejected;
            Requests[requestId] = r with { State = state, Feedback = reason, ReviewedAtUtc = now, OfficialBookingId = bookingId,
                OfficialBookingCode = bookingId.HasValue ? "LC-TEST-" + bookingId : null, ConfirmedTotal = approve ? r.Quote.Estimate : null,
                History = r.History.Append(new(state, now, "Nhân sự", reason)).ToArray() };
            AuditRows += auditDelta; Mutations++;
            if (FailAfterApprovalCommit) throw new IOException("synthetic-commit-acknowledgement-secret-never-visible");
            return Result.Ok();
        }, ct);
    public Task<OfficialRequestBooking?> GetOfficialAsync(BookingActor actor, long requestId, CancellationToken ct = default)
        => Locked(actor, () =>
        {
            if (!Requests.TryGetValue(requestId, out var r) || !Visible(actor, r) || r.OfficialBookingId is not { } id) return null;
            var b = Bookings[id];
            if (actor is CustomerBookingActor && ResolvedCustomers.GetValueOrDefault(requestId) != b.Customer) return null;
            var services = b.Quote.Addons.Sum(a => a.Quantity * a.UnitPrice);
            return new OfficialRequestBooking(id, r.OfficialBookingCode!, b.State, b.Start, b.End, b.Quote.Package.Name,
                b.Quote.Package.Price, services, b.Quote.Estimate, Rooms[b.Room].Name, Photographers[b.Photographer].Name);
        }, ct);
    private FakeBooking? PaymentBooking(CustomerBookingActor actor,long requestId)
    {
        if(!Requests.TryGetValue(requestId,out var r) || r.AccountId!=actor.Credential.AccountId || r.State!=BookingRequestState.Approved || r.OfficialBookingId is not { } id)return null;
        var booking=Bookings[id];return ResolvedCustomers.GetValueOrDefault(requestId)==booking.Customer?booking:null;
    }
    public Task<BookingPaymentSummary?> GetPaymentsAsync(CustomerBookingActor actor,long requestId,long? beforeId,CancellationToken ct=default)
        =>Locked<BookingPaymentSummary?>(actor,()=>
        {
            var b=PaymentBooking(actor,requestId);if(b is null)return null;
            var receipts=Receipts.GetValueOrDefault(b.Id)??[];
            var rows=receipts.Where(p=>beforeId is null || p.Id<beforeId).OrderByDescending(p=>p.Id).Take(51).ToArray();
            var collected=receipts.Sum(p=>p.Amount);
            return new(b.Id,"LC-TEST-"+b.Id,b.State,b.Quote.Estimate,collected,0,b.Quote.Estimate-collected,
                rows.Take(50).Select(p=>new PaymentTransaction(p.Id,p.Code,p.Purpose,p.Amount,p.Method,p.PaidAt,p.HasSnapshot)).ToArray(),rows.Length>50?rows[49].Id:null);
        },ct);
    public Task<PaymentReceipt?> GetReceiptAsync(CustomerBookingActor actor,long requestId,long paymentId,CancellationToken ct=default)
        =>Locked<PaymentReceipt?>(actor,()=>PaymentBooking(actor,requestId) is { } b
            ?Receipts.GetValueOrDefault(b.Id)?.SingleOrDefault(p=>p.Id==paymentId && p.HasSnapshot):null,ct);
}
