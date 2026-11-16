using System.Globalization;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

public sealed class BookingRequestTests
{
    private static async Task<BookingRequestService> Customer(BookingRequestFixture f, string name = "a")
    {
        var c = f.Connection(); Assert.True((await c.Identity.LoginAsync(name + "@example.test", "CustomerPass1")).Success); return c.Service;
    }
    private static async Task<BookingRequestService> Staff(BookingRequestFixture f, string name = "staff")
    {
        var c = f.Connection(); Assert.True((await c.Identity.StaffLoginAsync(name, "CustomerPass1")).Success); return c.Service;
    }
    private static async Task<long> Submit(BookingRequestFixture f, BookingRequestService c, BookingRequestDraft? draft = null)
    {
        draft ??= f.Draft();
        var quote = await c.PreviewAsync(draft); Assert.True(quote.Success, quote.Code);
        var result = await c.SubmitAsync(draft.SubmissionKey); Assert.True(result.Success, result.Code); return result.Data;
    }
    private static BookingApproval Approval(BookingRequestFixture f, long id, int room = 1, int photographer = 1)
        => new(photographer, room, f.Requests[id].Quote.Estimate, "Đã kiểm tra yêu cầu");

    [Fact]
    public async Task RegisteredCustomerWithoutLegacyLink_PreviewsAndSubmitsPending_WithoutOfficialBooking()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        Assert.Equal("PREVIEW_REQUIRED", (await c.SubmitAsync(Guid.NewGuid())).Code);
        var id = await Submit(f, c);
        var request = (await c.GetAsync(false, id)).Data!;
        Assert.Equal(BookingRequestState.Pending, request.State);
        Assert.Equal(1_000_154.30m, request.Quote.Estimate);
        Assert.Equal(TimeSpan.Zero, request.SubmittedAtUtc.Offset);
        Assert.Null(request.OfficialBookingId); Assert.Null(request.ConfirmedTotal);
        Assert.Empty(f.Bookings); Assert.Empty(f.CreatedCustomers); Assert.Empty(f.Identity.Store.Links);
        Assert.Single(request.History);
    }

    [Theory]
    [InlineData("package", "INACTIVE_PACKAGE")]
    [InlineData("service", "INACTIVE_SERVICE")]
    [InlineData("room", "ALLOCATION_UNAVAILABLE")]
    public async Task InactiveCatalogSelection_IsRejected(string type, string code)
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        if (type == "package") f.Packages.Remove(1);
        if (type == "service") f.Services.Remove(1);
        if (type == "room") f.Rooms.Remove(1);
        Assert.Equal(code, (await c.PreviewAsync(f.Draft())).Code);
        Assert.Empty(f.Requests);
    }

    [Theory]
    [InlineData("past")]
    [InlineData("too-soon")]
    [InlineData("too-far")]
    [InlineData("non-utc")]
    [InlineData("fractional-second")]
    public async Task InvalidRequestedTime_IsRejected(string kind)
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        var start = kind switch { "past" => f.Now.AddDays(-1), "too-soon" => f.Now.AddMinutes(1),
            "too-far" => f.Now.AddDays(366), "non-utc" => f.Now.AddDays(2).ToOffset(TimeSpan.FromHours(7)), _ => f.Now.AddDays(2).AddTicks(1) };
        Assert.Equal("INVALID_TIME", (await c.PreviewAsync(f.Draft() with { StartUtc = start })).Code);
        Assert.Empty(f.Requests);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1001")]
    [InlineData("1.001")]
    public async Task InvalidQuantity_IsRejected(string value)
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        Assert.Equal("INVALID_QUANTITY", (await c.PreviewAsync(f.Draft() with
        { Addons = [new(1, decimal.Parse(value, CultureInfo.InvariantCulture))] })).Code);
    }

    [Fact]
    public async Task InvalidPackageAndDuplicateOrTooManyAddons_AreRejected()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        Assert.Equal("INVALID_INPUT", (await c.PreviewAsync(f.Draft() with { PackageId = 0 })).Code);
        Assert.Equal("INACTIVE_PACKAGE", (await c.PreviewAsync(f.Draft() with { PackageId = 999 })).Code);
        Assert.Equal("INVALID_QUANTITY", (await c.PreviewAsync(f.Draft() with { Addons = [new(1, 1), new(1, 2)] })).Code);
        Assert.Equal("INVALID_QUANTITY", (await c.PreviewAsync(f.Draft() with { Addons = Enumerable.Range(1, 21).Select(i => new BookingAddonSelection(i, 1)).ToArray() })).Code);
        Assert.Empty(f.Requests);
    }

    [Fact]
    public async Task CustomerIsolation_AppliesToListDetailsCancellationAndOfficialReference()
    {
        var f = await BookingRequestFixture.Create(); var a = await Customer(f); var b = await Customer(f, "b");
        var id = await Submit(f, a);
        Assert.Empty((await b.ListAsync(false, null, null)).Data!);
        Assert.Equal("NOT_FOUND", (await b.GetAsync(false, id)).Code);
        Assert.Equal("NOT_FOUND", (await b.CancelAsync(id, "Không có quyền")).Code);
        Assert.Equal("NOT_FOUND", (await b.OfficialAsync(false, id)).Code);
        Assert.Equal("UNAUTHORIZED", (await b.ReviewAsync(id, true, Approval(f, id), "Giả quyền nhân sự")).Code);
        Assert.Equal(BookingRequestState.Pending, f.Requests[id].State); Assert.Empty(f.Bookings);
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("admin")]
    public async Task AuthorizedApproval_CreatesOneBookingAndDistinctCustomer_ThenOwnerCanRead(string role)
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var staff = await Staff(f, role);
        var id = await Submit(f, c);
        Assert.True((await staff.CheckAsync(id, Approval(f, id))).Success);
        Assert.Empty(f.Bookings); // Availability does not reserve or create.
        Assert.True((await staff.ReviewAsync(id, true, Approval(f, id), "Đồng ý giá dự kiến")).Success);
        var detail = (await c.GetAsync(false, id)).Data!;
        Assert.Equal(BookingRequestState.Approved, detail.State);
        Assert.Equal(detail.Quote.Estimate, detail.ConfirmedTotal);
        Assert.NotNull(detail.OfficialBookingId); Assert.NotNull(detail.OfficialBookingCode);
        Assert.Single(f.Bookings); Assert.Single(f.CreatedCustomers); Assert.Empty(f.Identity.Store.Links);
        Assert.NotEqual(41, f.Bookings[detail.OfficialBookingId.Value].Customer);
        Assert.NotEqual(42, f.Bookings[detail.OfficialBookingId.Value].Customer);
        Assert.Equal(detail.OfficialBookingCode, (await c.OfficialAsync(false, id)).Data!.Code);
        Assert.Equal(2, detail.History.Count); Assert.True(f.AuditRows >= 2);
        Assert.Equal("STATE_CHANGED", (await staff.ReviewAsync(id, false, null, "Không được từ chối lại")).Code);
        Assert.Equal("STATE_CHANGED", (await c.CancelAsync(id, "Không được rút đã duyệt")).Code);
    }

    [Fact]
    public async Task ApprovedP03Ownership_IsUsed_WithoutCreatingOrClaimingAnotherCustomer()
    {
        var f = await BookingRequestFixture.Create();
        var oldCustomer = f.Identity.Connection(); await oldCustomer.LoginAsync("a@example.test", "CustomerPass1");
        await oldCustomer.RequestLinkAsync("KH-41", "Đối chiếu hồ sơ cũ");
        var reviewer = f.Identity.Connection(); await reviewer.StaffLoginAsync("staff", "CustomerPass1");
        Assert.True((await reviewer.ReviewLinkAsync(1, CustomerLinkDecision.Approve, 41, "Đã xác minh tại quầy")).Success);
        var c = await Customer(f); var staff = await Staff(f);
        var id = await Submit(f, c); await staff.ReviewAsync(id, true, Approval(f, id), "Dùng hồ sơ đã liên kết");
        Assert.Equal(41, Assert.Single(f.Bookings).Value.Customer);
        Assert.Empty(f.CreatedCustomers); Assert.Single(f.Identity.Store.Links);
    }

    [Fact]
    public async Task SnapshotPricesAndNamesSurviveCatalogChanges_AndStaffMustAcceptExactEstimate()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var staff = await Staff(f);
        var id = await Submit(f, c); var saved = f.Requests[id].Quote;
        f.Packages[1] = f.Packages[1] with { Name = "Tên mới", Price = 9_000_000 };
        f.Services[1] = f.Services[1] with { Name = "Dịch vụ mới", Price = 999 };
        Assert.Equal("STALE_REQUEST", (await staff.ReviewAsync(id, true, Approval(f, id) with { ExpectedEstimate = 1 }, "Chấp nhận sai giá")).Code);
        Assert.True((await staff.ReviewAsync(id, true, Approval(f, id), "Chấp nhận báo giá đã lưu")).Success);
        var booking = Assert.Single(f.Bookings).Value;
        Assert.Equal(saved.Package.Name, booking.Quote.Package.Name); Assert.Equal(saved.Estimate, booking.Quote.Estimate);
        Assert.Equal(saved.Addons[0].UnitPrice, booking.Quote.Addons[0].UnitPrice);
    }

    [Fact]
    public async Task CatalogChangeBetweenPreviewAndSubmit_RequiresNewPreview()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var draft = f.Draft();
        Assert.True((await c.PreviewAsync(draft)).Success); f.Packages[1] = f.Packages[1] with { Price = 2_000_000 };
        Assert.Equal("STALE_QUOTE", (await c.SubmitAsync(draft.SubmissionKey)).Code);
        Assert.Equal("PREVIEW_REQUIRED", (await c.SubmitAsync(draft.SubmissionKey)).Code);
        Assert.Empty(f.Requests);
    }

    [Fact]
    public async Task RejectRequiresReason_AndCancellationOnlyChangesOwnPendingRequest()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var staff = await Staff(f);
        var id = await Submit(f, c);
        Assert.Equal("REASON_REQUIRED", (await staff.ReviewAsync(id, false, null, "")).Code);
        Assert.True((await staff.ReviewAsync(id, false, null, "Không đủ điều kiện duyệt")).Success);
        Assert.Equal("Không đủ điều kiện duyệt", (await c.GetAsync(false, id)).Data!.Feedback);
        Assert.Equal("STATE_CHANGED", (await c.CancelAsync(id, "Không được rút đã xử lý")).Code);
        var second = await Submit(f, c);
        Assert.True((await c.CancelAsync(second, "Khách thay đổi kế hoạch")).Success);
        Assert.Equal("STATE_CHANGED", (await staff.ReviewAsync(second, true, Approval(f, second), "Không được duyệt đã rút")).Code);
        Assert.Equal(BookingRequestState.Cancelled, f.Requests[second].State);
        Assert.Equal(2, f.Requests[second].History.Count); Assert.Equal(2, (await c.ListAsync(false, null, null)).Data!.Count);
        Assert.Empty(f.Bookings); Assert.Empty(f.CreatedCustomers);
    }

    [Fact]
    public async Task ConcurrentApprovalOfOneRequest_CommitsExactlyOnce()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        var first = await Staff(f); var second = await Staff(f, "admin"); var id = await Submit(f, c);
        var results = await Task.WhenAll(first.ReviewAsync(id, true, Approval(f, id), "Nhân sự duyệt thứ nhất"),
            second.ReviewAsync(id, true, Approval(f, id), "Nhân sự duyệt thứ hai"));
        Assert.Single(results, r => r.Success); Assert.Single(results, r => r.Code == "STATE_CHANGED");
        Assert.Single(f.Bookings); Assert.Equal(2, f.Requests[id].History.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentCompetingRequests_RejectRoomOrPhotographerConflict(bool roomConflict)
    {
        var f = await BookingRequestFixture.Create(); var a = await Customer(f); var b = await Customer(f, "b");
        var s1 = await Staff(f); var s2 = await Staff(f, "admin");
        var id1 = await Submit(f, a); var id2 = await Submit(f, b);
        var second = roomConflict ? Approval(f, id2, 1, 2) : Approval(f, id2, 2, 1);
        var result = await Task.WhenAll(s1.ReviewAsync(id1, true, Approval(f, id1), "Duyệt yêu cầu đầu"),
            s2.ReviewAsync(id2, true, second, "Duyệt yêu cầu cạnh tranh"));
        Assert.Single(result, r => r.Success); Assert.Single(result, r => r.Code == "CONFLICT");
        Assert.Single(f.Bookings);
        Assert.Single(f.Requests.Values, r => r.State == BookingRequestState.Pending);
    }

    [Fact]
    public async Task AdjacentTimesAllowed_AndOwnCreatedCustomerReusedWithoutP03Grant()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var s = await Staff(f);
        var first = await Submit(f, c); await s.ReviewAsync(first, true, Approval(f, first), "Duyệt lịch thứ nhất");
        var second = await Submit(f, c, f.Draft() with { StartUtc = f.Draft().StartUtc.AddMinutes(60) });
        Assert.True((await s.ReviewAsync(second, true, Approval(f, second), "Duyệt lịch kế tiếp")).Success);
        Assert.Equal(f.Bookings[1].Customer, f.Bookings[2].Customer);
        Assert.Single(f.CreatedCustomers); Assert.Empty(f.Identity.Store.Links);
    }

    [Theory]
    [InlineData("package", "INACTIVE_PACKAGE")]
    [InlineData("service", "INACTIVE_SERVICE")]
    [InlineData("room", "ALLOCATION_UNAVAILABLE")]
    [InlineData("photographer", "ALLOCATION_UNAVAILABLE")]
    public async Task ApprovalRevalidatesCurrentCatalogAndAllocation(string changed, string code)
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var staff = await Staff(f);
        var id = await Submit(f, c);
        if (changed == "package") f.Packages.Remove(1);
        if (changed == "service") f.Services.Remove(1);
        if (changed == "room") f.Rooms.Remove(1);
        if (changed == "photographer") f.Photographers.Remove(1);
        Assert.Equal(code, (await staff.ReviewAsync(id, true, Approval(f, id), "Kiểm tra lại khi duyệt")).Code);
        Assert.Empty(f.Bookings); Assert.Empty(f.CreatedCustomers);
        Assert.Equal(BookingRequestState.Pending, f.Requests[id].State);
    }

    [Fact]
    public async Task PreviouslyCreatedProfileNowOwnedByAnotherAccount_IsNeverImplicitlyReclaimed()
    {
        var f = await BookingRequestFixture.Create(); var a = await Customer(f); var staff = await Staff(f);
        var first = await Submit(f, a);
        await staff.ReviewAsync(first, true, Approval(f, first), "Tạo hồ sơ mới đúng tài khoản");
        var oldCustomer = f.Bookings[1].Customer;
        f.Identity.Store.BusinessCustomers[oldCustomer] = new("KH-TEST", "Khách thử", "0900000000", "contact@example.test", "Địa chỉ thử");
        var b = f.Identity.Connection(); await b.LoginAsync("b@example.test", "CustomerPass1");
        await b.RequestLinkAsync("KH-TEST", "Nhân viên đối chiếu thực tế");
        var reviewer = f.Identity.Connection(); await reviewer.StaffLoginAsync("staff", "CustomerPass1");
        Assert.True((await reviewer.ReviewLinkAsync(1, CustomerLinkDecision.Approve, oldCustomer, "Đã xác minh chuyển quyền")).Success);
        var second = await Submit(f, a, f.Draft() with { StartUtc = f.Draft().StartUtc.AddMinutes(60) });
        Assert.True((await staff.ReviewAsync(second, true, Approval(f, second), "Không dùng hồ sơ của người khác")).Success);
        Assert.NotEqual(oldCustomer, f.Bookings[2].Customer);
        Assert.Equal(oldCustomer, f.Identity.Store.Links[1].Customer);
    }

    [Fact]
    public async Task ApplicationRollbackContract_LeavesNoOrphanBookingCustomerAuditOrApproval()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var s = await Staff(f); var id = await Submit(f, c);
        f.FailBeforeApprovalCommit = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.ReviewAsync(id, true, Approval(f, id), "Mô phỏng lỗi trước commit"));
        Assert.Empty(f.Bookings); Assert.Empty(f.CreatedCustomers); Assert.Equal(0, f.AuditRows);
        Assert.Equal(BookingRequestState.Pending, f.Requests[id].State); Assert.Single(f.Requests[id].History);
        f.FailBeforeApprovalCommit = false;
        Assert.True((await s.ReviewAsync(id, true, Approval(f, id), "Duyệt sau khi tải lại")).Success);
        Assert.Single(f.Bookings);
    }

    [Fact]
    public async Task SubmissionKeyPreventsDuplicates_AndRejectsDifferentContent()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var draft = f.Draft();
        var first = await Submit(f, c, draft); Assert.Equal(first, await Submit(f, c, draft)); Assert.Single(f.Requests);
        Assert.True((await c.PreviewAsync(draft with { Notes = "Nội dung khác cùng mã" })).Success);
        Assert.Equal("SUBMISSION_KEY_REUSED", (await c.SubmitAsync(draft.SubmissionKey)).Code); Assert.Single(f.Requests);
    }

    [Fact]
    public async Task QuoteCannotCrossAccountsOrPasswordRotation_AndExpiredStaffCannotApprove()
    {
        var f = await BookingRequestFixture.Create(); var connection = f.Connection();
        await connection.Identity.LoginAsync("a@example.test", "CustomerPass1");
        var draft = f.Draft(); await connection.Service.PreviewAsync(draft);
        await connection.Identity.LoginAsync("b@example.test", "CustomerPass1");
        Assert.Equal("PREVIEW_REQUIRED", (await connection.Service.SubmitAsync(draft.SubmissionKey)).Code);
        await connection.Service.PreviewAsync(draft);
        await connection.Identity.ChangePasswordAsync("CustomerPass1", "ChangedPass2", "ChangedPass2");
        await connection.Identity.LoginAsync("b@example.test", "ChangedPass2");
        Assert.Equal("PREVIEW_REQUIRED", (await connection.Service.SubmitAsync(draft.SubmissionKey)).Code);
        var c = await Customer(f); var id = await Submit(f, c); var s = await Staff(f);
        f.Identity.Clock.Now = f.Identity.Clock.Now.AddHours(9);
        Assert.Equal("UNAUTHORIZED", (await s.ReviewAsync(id, true, Approval(f, id), "Hết phiên không được duyệt")).Code);
        Assert.Empty(f.Bookings);
    }

    [Fact]
    public async Task AccountLockAndStaffRevocation_AreRechecked()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f); var draft = f.Draft();
        await c.PreviewAsync(draft); f.Identity.Store.Accounts[1] = f.Identity.Store.Accounts[1] with { Active = false };
        Assert.Equal("UNAUTHORIZED", (await c.SubmitAsync(draft.SubmissionKey)).Code);
        f.Identity.Store.Accounts[1] = f.Identity.Store.Accounts[1] with { Active = true };
        c = await Customer(f); var id = await Submit(f, c); var s = await Staff(f);
        f.Identity.Directory.Accounts[2] = f.Identity.Directory.Accounts[2] with { HoatDong = false };
        Assert.Equal("UNAUTHORIZED", (await s.ReviewAsync(id, true, Approval(f, id), "Tài khoản đã bị khóa")).Code);
        Assert.Empty(f.Bookings);
    }

    [Fact]
    public async Task CatalogPaginationAndLegacyContactLimit_AreExplicit()
    {
        var f = await BookingRequestFixture.Create(); var c = await Customer(f);
        for (var id = 2; id <= 30; id++) f.Packages[id] = f.Packages[1] with { Id = id };
        var first = (await c.CatalogAsync(false, BookingCatalogKind.Packages, null)).Data!;
        Assert.Equal(25, first.Items.Count); Assert.Equal(25, first.NextAfterId);
        Assert.Equal(5, (await c.CatalogAsync(false, BookingCatalogKind.Packages, first.NextAfterId)).Data!.Items.Count);
        Assert.Equal("INVALID_INPUT", (await c.CatalogAsync(false, BookingCatalogKind.Photographers, null)).Code);
        f.Identity.Store.Accounts[1] = f.Identity.Store.Accounts[1] with { Contact = f.Identity.Store.Accounts[1].Contact with { Phone = "0123456789012345" } };
        Assert.Equal("BOOKING_CONTACT_REQUIRED", (await c.PreviewAsync(f.Draft())).Code);
    }

    [Fact]
    public void VietnamTimeRoundTripsLegacyWallClock_AndRequestStateMachineIsSeparate()
    {
        var utc = new DateTimeOffset(2026, 11, 20, 2, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTime(2026, 11, 20, 9, 0, 0), StudioBookingTime.ToLegacyLocal(utc));
        Assert.Equal(utc, StudioBookingTime.FromLegacyLocal(StudioBookingTime.ToLegacyLocal(utc)));
        foreach (var terminal in new[] { BookingRequestState.Approved, BookingRequestState.Rejected, BookingRequestState.Cancelled })
            foreach (var state in Enum.GetValues<BookingRequestState>()) Assert.False(BookingRequestRules.CanTransition(terminal, state, true));
        Assert.False(BookingRequestRules.CanTransition(BookingRequestState.Pending, BookingRequestState.Approved, false));
        Assert.True(BookingRequestRules.CanTransition(BookingRequestState.Pending, BookingRequestState.Cancelled, false));
    }
}
