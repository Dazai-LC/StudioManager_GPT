using System.Runtime.Versioning;
using System.Security.Principal;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using StudioManager.IdentityService;
using Xunit;

namespace StudioManager.Tests;

[SupportedOSPlatform("windows")]
public sealed class CustomerPipeLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdleAfterStaffList_ExpiresBeforeApproval_ThenExplicitLoginApprovesOnce(bool serverClosedFirst)
    {
        var fixture = await PendingRequest();
        var time = new PipeTestTime();
        await using var pipe = new LocalIdentityPipe(fixture, time, serverClosedFirst ? TimeSpan.FromMilliseconds(500) : null);
        Assert.True((await pipe.SendAsync(StaffLogin())).Success);
        Assert.Single((await pipe.SendAsync(new(IdentityCommand.ListLinkReviews))).Reviews!);
        if (serverClosedFirst)
        {
            await pipe.LastHandler.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(pipe.LastStream!.IsConnected);
            time.Advance(TimeSpan.FromSeconds(121));
        }
        else
        {
            time.Advance(IdentityProtocol.ClientIdleTimeout);
            Assert.True(await pipe.Client.ExpireIdleConnectionAsync());
            Assert.False(await pipe.Client.ExpireIdleConnectionAsync());
            await pipe.LastHandler.WaitAsync(TimeSpan.FromSeconds(5));
        }
        var approval = await pipe.SendAsync(Approve(1, 41));
        Assert.Equal("SESSION_EXPIRED", approval.Code);
        Assert.Equal(1, pipe.Connections);
        Assert.Equal(CustomerLinkState.Pending, fixture.Store.Links[1].State);
        Assert.Equal(0, fixture.Store.ReviewCalls);
        Assert.True((await pipe.SendAsync(StaffLogin())).Success);
        Assert.Equal(2, pipe.Connections);
        Assert.True((await pipe.SendAsync(Approve(1, 41))).Success);
        Assert.Equal(CustomerLinkState.Approved, fixture.Store.Links[1].State);
        Assert.Equal(1, fixture.Store.ReviewCalls);
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("admin")]
    public async Task ConsecutiveOwnershipCommands_ApproveReadRevokeRejectAndCompetingOwner(string reviewer)
    {
        var fixture = new CustomerIdentityTests.Fixture();
        await fixture.AddCustomer("a"); await fixture.AddCustomer("b");
        await using var a = new LocalIdentityPipe(fixture);
        await using var b = new LocalIdentityPipe(fixture);
        await using var staff = new LocalIdentityPipe(fixture);
        Assert.True((await a.SendAsync(CustomerLogin("a"))).Success);
        Assert.True((await b.SendAsync(CustomerLogin("b"))).Success);
        Assert.True((await staff.SendAsync(StaffLogin(reviewer))).Success);
        Assert.Equal("NOT_LINKED", (await a.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).Code);
        Assert.True((await a.SendAsync(new(IdentityCommand.RequestLink) { Reference = "KH900001", Evidence = "Synthetic evidence", CustomerId = 2 })).Success);
        var pending = (await a.SendAsync(new(IdentityCommand.GetMyLink))).Link!;
        Assert.Equal("Pending", pending.State); Assert.Null(pending.CustomerCode);
        Assert.Equal(1, fixture.Store.Links[pending.Id].Account);
        Assert.Equal("NOT_LINKED", (await a.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).Code);
        var review = Assert.Single((await staff.SendAsync(new(IdentityCommand.ListLinkReviews))).Reviews!);
        Assert.Equal("NOT_FOUND", (await staff.SendAsync(Approve(review.Id, 999))).Code);
        Assert.True((await staff.SendAsync(Approve(review.Id, 41))).Success);
        Assert.Equal("Approved", (await a.SendAsync(new(IdentityCommand.GetMyLink))).Link!.State);
        Assert.Equal("KH-41", (await a.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).LinkedCustomer!.Code);
        Assert.Equal("NOT_LINKED", (await b.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).Code);
        Assert.True((await b.SendAsync(new(IdentityCommand.RequestLink) { Reference = "KH900001", Evidence = "Competing evidence" })).Success);
        var competing = (await b.SendAsync(new(IdentityCommand.GetMyLink))).Link!;
        Assert.Equal("OWNERSHIP_CONFLICT", (await staff.SendAsync(Approve(competing.Id, 41))).Code);
        Assert.Equal("KH-41", (await a.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).LinkedCustomer!.Code);
        Assert.True((await staff.SendAsync(new(IdentityCommand.RevokeLink) { LinkId = pending.Id, Reason = "Synthetic revocation" })).Success);
        Assert.Equal("NOT_LINKED", (await a.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).Code);
        Assert.Equal("Revoked", (await a.SendAsync(new(IdentityCommand.GetMyLink))).Link!.State);
        Assert.True((await staff.SendAsync(new(IdentityCommand.RejectLink) { LinkId = competing.Id, Reason = "Synthetic rejection" })).Success);
        Assert.Equal("Rejected", (await b.SendAsync(new(IdentityCommand.GetMyLink))).Link!.State);
        Assert.True((await b.SendAsync(new(IdentityCommand.RequestLink) { Reference = "KH900002", Evidence = "New evidence" })).Success);
        Assert.Equal(CustomerLinkState.Revoked, fixture.Store.Links[pending.Id].State);
        Assert.Equal(CustomerLinkState.Rejected, fixture.Store.Links[competing.Id].State);
        Assert.True((await staff.SendAsync(Approve((await b.SendAsync(new(IdentityCommand.GetMyLink))).Link!.Id, 42))).Success);
        Assert.Equal("KH-42", (await b.SendAsync(new(IdentityCommand.GetMyLinkedCustomer))).LinkedCustomer!.Code);
        Assert.Equal(1, a.Connections); Assert.Equal(1, b.Connections); Assert.Equal(1, staff.Connections);
        Assert.Equal("UNAUTHORIZED", (await a.SendAsync(Approve(competing.Id, 41))).Code);
    }

    [Fact]
    public async Task AcceptedCommand_GetsRequestDeadline_InsteadOfRemainingIdleDeadline()
    {
        var fixture = new CustomerIdentityTests.Fixture(); await fixture.AddCustomer("a");
        fixture.Store.RequestLinkDelay = TimeSpan.FromSeconds(1);
        await using var pipe = new LocalIdentityPipe(fixture, idleTimeout: TimeSpan.FromMilliseconds(500));
        Assert.True((await pipe.SendAsync(CustomerLogin("a"))).Success);
        Assert.True((await pipe.SendAsync(new(IdentityCommand.RequestLink) { Reference = "KH900001", Evidence = "Synthetic slow operation" })).Success);
        Assert.Equal(CustomerLinkState.Pending, Assert.Single(fixture.Store.Links).Value.State);
        Assert.Equal(1, pipe.Connections);
    }

    [Fact]
    public async Task AbsoluteStaffSessionExpiry_IsStructured_AndApprovalNeverReachesRepository()
    {
        var fixture = await PendingRequest();
        await using var pipe = new LocalIdentityPipe(fixture);
        Assert.True((await pipe.SendAsync(StaffLogin())).Success);
        fixture.Clock.Now = fixture.Clock.Now.AddHours(9);
        Assert.Equal("SESSION_EXPIRED", (await pipe.SendAsync(Approve(1, 41))).Code);
        Assert.Equal(0, fixture.Store.ReviewCalls);
        Assert.Equal(CustomerLinkState.Pending, fixture.Store.Links[1].State);
    }

    [Fact]
    public async Task ApprovalCommittedButReplyLost_IsUnknown_AndNeverAutomaticallyReplayed()
    {
        var fixture = await PendingRequest();
        using var current = WindowsIdentity.GetCurrent();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var name = "StudioManager.P03.LostReply." + Guid.NewGuid().ToString("N");
        await using var server = IdentityPipeServer.CreatePipe(name, current.User!, true);
        var accept = server.WaitForConnectionAsync(timeout.Token);
        await using var stream = await CustomerPipeClient.ConnectLocalPipeAsync(name, timeout.Token);
        await accept;
        await using var client = new CustomerPipeClient();
        LocalIdentityPipe.Attach(client, stream);
        var dispatcher = new IdentityDispatcher(fixture.Connection());
        var serverTask = Task.Run(async () =>
        {
            var login = await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
            await IdentityProtocol.WriteAsync(server, await dispatcher.DispatchAsync(login, timeout.Token), timeout.Token);
            var approve = await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
            Assert.True((await dispatcher.DispatchAsync(approve, timeout.Token)).Success);
            await server.DisposeAsync();
            dispatcher.Disconnect();
        }, timeout.Token);
        Assert.True((await client.SendAsync(StaffLogin(), timeout.Token)).Success);
        var result = await client.SendAsync(Approve(1, 41), timeout.Token);
        await serverTask;
        Assert.Equal("OUTCOME_UNKNOWN", result.Code);
        Assert.Contains("OWNERSHIP_APPROVE.RESPONSE_READ", result.Message, StringComparison.Ordinal);
        Assert.Equal(CustomerLinkState.Approved, fixture.Store.Links[1].State);
        Assert.Equal(1, fixture.Store.ReviewCalls);
        Assert.Equal("SESSION_EXPIRED", (await client.SendAsync(Approve(1, 41), timeout.Token)).Code);
        Assert.Equal(1, fixture.Store.ReviewCalls);
        await using var recovery = new LocalIdentityPipe(fixture);
        Assert.True((await recovery.SendAsync(StaffLogin())).Success);
        Assert.Equal("Approved", Assert.Single((await recovery.SendAsync(new(IdentityCommand.ListLinkReviews))).Reviews!).State);
        Assert.Equal(1, fixture.Store.ReviewCalls);
    }

    [Fact]
    public async Task DisposeDuringRpc_RejectsQueuedLogin_AndNeverReopensConnection()
    {
        using var current = WindowsIdentity.GetCurrent();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var name = "StudioManager.P03.Dispose." + Guid.NewGuid().ToString("N");
        await using var server = IdentityPipeServer.CreatePipe(name, current.User!, true);
        var accept = server.WaitForConnectionAsync(timeout.Token);
        await using var stream = await CustomerPipeClient.ConnectLocalPipeAsync(name, timeout.Token);
        await accept;
        await using var client = new CustomerPipeClient();
        LocalIdentityPipe.Attach(client, stream);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Task.Run(async () =>
        {
            await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
            received.SetResult();
            await reply.Task.WaitAsync(timeout.Token);
            await IdentityProtocol.WriteAsync(server, new IdentityResponse(false, "INVALID_INPUT", "Synthetic validation"), timeout.Token);
        }, timeout.Token);
        var active = client.SendAsync(new(IdentityCommand.GetMyProfile), timeout.Token);
        await received.Task.WaitAsync(timeout.Token);
        var dispose = client.DisposeAsync().AsTask();
        var queued = client.SendAsync(StaffLogin(), timeout.Token);
        reply.SetResult();
        Assert.Equal("INVALID_INPUT", (await active).Code);
        await dispose;
        Assert.Equal("CLIENT_CLOSED", (await queued).Code);
        await serverTask;
    }

    private static IdentityRequest StaffLogin(string name = "staff") => new(IdentityCommand.StaffLogin) { Identifier = name, Password = "CustomerPass1" };
    private static IdentityRequest CustomerLogin(string name) => new(IdentityCommand.CustomerLogin) { Identifier = name + "@example.test", Password = "CustomerPass1" };
    private static IdentityRequest Approve(long link, long customer) => new(IdentityCommand.ApproveLink) { LinkId = link, CustomerId = customer, Reason = "Synthetic review" };
    private static async Task<CustomerIdentityTests.Fixture> PendingRequest()
    {
        var fixture = new CustomerIdentityTests.Fixture(); await fixture.AddCustomer("a");
        var customer = fixture.Connection(); await customer.LoginAsync("a@example.test", "CustomerPass1");
        await customer.RequestLinkAsync("KH900001", "Synthetic evidence");
        return fixture;
    }
}
