using System.Buffers.Binary;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using StudioManager.IdentityService;
using Xunit;

namespace StudioManager.Tests;

public sealed class BookingRequestPipeTests
{
    private static BookingDraftDto Draft(BookingRequestFixture f) => new(Guid.NewGuid(), 1, f.Draft().StartUtc, 1, "Yêu cầu thử qua pipe", [new(1, 1.25m)]);
    private static IdentityRequest CustomerLogin() => new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = "CustomerPass1" };
    private static IdentityRequest StaffLogin() => new(IdentityCommand.StaffLogin) { Identifier = "staff", Password = "CustomerPass1" };

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task RealPipe_CustomerCatalogPreviewSubmit_StaffApprove_CustomerReadsOfficialAndHistory()
    {
        var f = await BookingRequestFixture.Create();
        await using var c = new LocalIdentityPipe(f.Identity, bookingRepository: f);
        await using var staff = new LocalIdentityPipe(f.Identity, bookingRepository: f);
        Assert.True((await c.SendAsync(CustomerLogin())).Success);
        var catalog = await c.SendAsync(new(IdentityCommand.GetBookingCatalog) { BookingCatalogQuery = new(BookingCatalogType.Packages) });
        Assert.Single(catalog.BookingCatalog!.Items);
        var draft = Draft(f);
        var preview = await c.SendAsync(new(IdentityCommand.PreviewBookingRequest) { BookingDraft = draft, CustomerId = 2 });
        Assert.True(preview.Success);
        Assert.Equal(1_000_154.30m, preview.BookingQuote!.Estimate);
        var submitted = await c.SendAsync(new(IdentityCommand.SubmitBookingRequest) { BookingSubmissionKey = draft.SubmissionKey, CustomerId = 2 });
        Assert.True(submitted.Success);
        var id = submitted.CreatedRequestId!.Value;
        Assert.Equal(1, f.Requests[id].AccountId);
        Assert.Empty(f.Bookings);
        Assert.True((await staff.SendAsync(StaffLogin())).Success);
        Assert.Single((await staff.SendAsync(new(IdentityCommand.GetBookingReviewQueue) { BookingFilter = new("PENDING") })).BookingRequests!);
        var detail = (await staff.SendAsync(new(IdentityCommand.GetBookingReview) { BookingRequestId = id })).BookingDetail!;
        var approval = new BookingApprovalDto(1, 1, detail.Quote.Estimate);
        Assert.True((await staff.SendAsync(new(IdentityCommand.CheckBookingAvailability) { BookingRequestId = id, BookingApproval = approval })).Success);
        Assert.True((await staff.SendAsync(new(IdentityCommand.ApproveBookingRequest) { BookingRequestId = id, BookingApproval = approval, Reason = "Đối chiếu và duyệt qua pipe" })).Success);
        var own = (await c.SendAsync(new(IdentityCommand.GetMyBookingRequest) { BookingRequestId = id })).BookingDetail!;
        Assert.Equal("APPROVED", own.State); Assert.Equal(2, own.History.Count);
        var official = await c.SendAsync(new(IdentityCommand.GetMyRequestBooking) { BookingRequestId = id });
        Assert.Equal(own.OfficialBookingCode, official.OfficialBooking!.Code);
        Assert.Equal("DA_DAT_LICH", official.OfficialBooking.Status);
        Assert.Equal(TimeSpan.Zero, official.OfficialBooking.StartUtc.Offset);
        Assert.Equal("STATE_CHANGED", (await c.SendAsync(new(IdentityCommand.CancelBookingRequest) { BookingRequestId = id, Reason = "Không rút yêu cầu đã duyệt" })).Code);
        Assert.Single(f.Bookings);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task ApprovalReplyLost_IsOutcomeUnknown_AndNoWriteIsReplayed()
    {
        var f = await BookingRequestFixture.Create(); var customer = f.Connection();
        await customer.Identity.LoginAsync("a@example.test", "CustomerPass1");
        var draft = f.Draft(); await customer.Service.PreviewAsync(draft); var id = (await customer.Service.SubmitAsync(draft.SubmissionKey)).Data;
        using var current = WindowsIdentity.GetCurrent();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var name = "StudioManager.P04.LostReply." + Guid.NewGuid().ToString("N");
        await using var server = IdentityPipeServer.CreatePipe(name, current.User!, true);
        var accept = server.WaitForConnectionAsync(timeout.Token);
        await using var stream = await CustomerPipeClient.ConnectLocalPipeAsync(name, timeout.Token); await accept;
        await using var client = new CustomerPipeClient(); LocalIdentityPipe.Attach(client, stream);
        var authority = f.Identity.Connection();
        var dispatcher = new IdentityDispatcher(authority, new BookingRequestService(authority, f, f.Identity.Clock));
        var work = Task.Run(async () =>
        {
            var login = await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
            await IdentityProtocol.WriteAsync(server, await dispatcher.DispatchAsync(login, timeout.Token), timeout.Token);
            var mutation = await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
            Assert.True((await dispatcher.DispatchAsync(mutation, timeout.Token)).Success);
            await server.DisposeAsync(); dispatcher.Disconnect();
        }, timeout.Token);
        Assert.True((await client.SendAsync(StaffLogin(), timeout.Token)).Success);
        var approve = new IdentityRequest(IdentityCommand.ApproveBookingRequest)
        { BookingRequestId = id, BookingApproval = new(1, 1, f.Requests[id].Quote.Estimate), Reason = "Duyệt và mô phỏng mất phản hồi" };
        var result = await client.SendAsync(approve, timeout.Token); await work;
        Assert.Equal("OUTCOME_UNKNOWN", result.Code);
        Assert.Contains("BOOKING_MUTATION.RESPONSE_READ", result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-provider", result.Message, StringComparison.Ordinal);
        Assert.Single(f.Bookings); Assert.Equal(BookingRequestState.Approved, f.Requests[id].State);
        Assert.Equal("SESSION_EXPIRED", (await client.SendAsync(approve, timeout.Token)).Code);
        Assert.Single(f.Bookings);
    }

    [Fact]
    public async Task P03WireShapes_DoNotGainUnknownP04Properties()
    {
        await using var request = new MemoryStream();
        await IdentityProtocol.WriteAsync(request, new IdentityRequest(IdentityCommand.GetMyProfile));
        request.Position = 4; using var rq = await JsonDocument.ParseAsync(request);
        Assert.DoesNotContain(rq.RootElement.EnumerateObject(), p => p.Name.StartsWith("Booking", StringComparison.Ordinal));
        await using var response = new MemoryStream();
        await IdentityProtocol.WriteAsync(response, new IdentityResponse(true, "OK", "Thử"));
        response.Position = 4; using var rs = await JsonDocument.ParseAsync(response);
        Assert.DoesNotContain(rs.RootElement.EnumerateObject(), p => p.Name.StartsWith("Booking", StringComparison.Ordinal) || p.Name is "CreatedRequestId" or "OfficialBooking");
    }

    [Fact]
    public async Task NestedCallerSuppliedAccountIsRejectedBeforeApplicationDispatch()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"Command":"PreviewBookingRequest","BookingDraft":{"SubmissionKey":"11111111-1111-1111-1111-111111111111","PackageId":1,"StartUtc":"2026-12-01T02:00:00+00:00","PreferredRoomId":1,"Notes":"","Addons":[],"AccountId":2}}""");
        await using var frame = new MemoryStream(); var length = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
        await frame.WriteAsync(length); await frame.WriteAsync(bytes); frame.Position = 0;
        await Assert.ThrowsAsync<JsonException>(() => IdentityProtocol.ReadAsync<IdentityRequest>(frame));
    }
}
