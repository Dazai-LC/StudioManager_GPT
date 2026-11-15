using System.Buffers.Binary;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Data.SqlClient;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using StudioManager.IdentityService;
using Xunit;

namespace StudioManager.Tests;

public sealed class CustomerPipeBoundaryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(IdentityProtocol.MaxMessageBytes + 1)]
    public async Task InvalidFrameSize_IsRefusedBeforePayloadRead(int length)
    {
        var bytes = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(bytes, length);
        await Assert.ThrowsAsync<InvalidDataException>(() => IdentityProtocol.ReadAsync<IdentityRequest>(new MemoryStream(bytes)));
    }

    [Fact]
    public async Task ClientSuppliedActorOrRoleFields_AreRejectedByWireContract()
    {
        const string payload = """{"Command":"GetMyProfile","AccountId":2,"Role":"Admin"}""";
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => IdentityProtocol.ReadAsync<IdentityRequest>(Frame(payload)));
    }

    [Fact]
    public void CredentialRequests_DoNotRenderPasswordsOrContactPayload()
    {
        var request = new IdentityRequest(IdentityCommand.CustomerLogin) { Identifier = "private@example.test", Password = "private-password" };
        Assert.DoesNotContain("private-password", request.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("private@example.test", request.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void PipeAcl_DeniesNetwork_AndNeverGrantsClientsServerCreationRights()
    {
        using var current = WindowsIdentity.GetCurrent();
        var clients = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        var security = IdentityPipeServer.CreateSecurity(current.User!, clients);
        var rules = security.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<PipeAccessRule>().ToArray();
        Assert.Contains(rules, r => r.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.NetworkSid, null)) && r.AccessControlType == AccessControlType.Deny);
        var clientRule = Assert.Single(rules, r => r.IdentityReference.Equals(clients) && r.AccessControlType == AccessControlType.Allow);
        Assert.Equal((PipeAccessRights)0, clientRule.PipeAccessRights & (PipeAccessRights.CreateNewInstance | PipeAccessRights.ChangePermissions | PipeAccessRights.TakeOwnership));
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task ActualLocalPipe_AuthenticatesScopesIdentityAndRevokesStateOnDisconnect()
    {
        var fixture = new CustomerIdentityTests.Fixture();
        await fixture.AddCustomer("a"); await fixture.AddCustomer("b");
        using var current = WindowsIdentity.GetCurrent();
        var name = "StudioManager.P03.Test." + Guid.NewGuid().ToString("N");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var server = IdentityPipeServer.CreatePipe(name, current.User!, true);
        var dispatcher = new IdentityDispatcher(fixture.Connection());
        var accept = server.WaitForConnectionAsync(timeout.Token);
        await using var client = await CustomerPipeClient.ConnectLocalPipeAsync(name, timeout.Token);
        await accept;
        var serverWork = Task.Run(async () =>
        {
            for (var count = 0; count < 4; count++)
            {
                var request = await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
                await IdentityProtocol.WriteAsync(server, await dispatcher.DispatchAsync(request, timeout.Token), timeout.Token);
            }
            dispatcher.Disconnect();
        }, timeout.Token);
        async Task<IdentityResponse> Send(IdentityRequest request)
        {
            await IdentityProtocol.WriteAsync(client, request, timeout.Token);
            return await IdentityProtocol.ReadAsync<IdentityResponse>(client, timeout.Token);
        }
        Assert.True((await Send(new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = "CustomerPass1" })).Success);
        var profile = await Send(new(IdentityCommand.GetMyProfile) { CustomerId = 2 });
        Assert.Equal("a@example.test", profile.Profile!.LoginEmail);
        Assert.False((await Send(new(IdentityCommand.ApproveLink) { LinkId = 2, CustomerId = 42, Reason = "Giả quyền" })).Success);
        Assert.True((await Send(new(IdentityCommand.Logout))).Success);
        await serverWork;
        Assert.False((await dispatcher.DispatchAsync(new(IdentityCommand.GetMyProfile))).Success);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task OwnershipRequest_TraversesRealConnectionHandler_AfterPasswordChangeAndRelogin()
    {
        var fixture = new CustomerIdentityTests.Fixture();
        await fixture.AddCustomer("a"); await fixture.AddCustomer("b");
        await WithConnection(fixture, async send =>
        {
            Assert.True((await send(new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = "CustomerPass1" })).Success);
            Assert.Empty(fixture.Store.Links);
            Assert.True((await send(new(IdentityCommand.UpdateMyProfile)
            { Contact = new("A updated", "0900000000", "new-contact@example.test", "Địa chỉ mới") })).Success);
            Assert.True((await send(new(IdentityCommand.ChangePassword)
            { Password = "CustomerPass1", NewPassword = "ChangedPass2", Confirmation = "ChangedPass2" })).Success);
            Assert.Equal("SESSION_EXPIRED", (await send(new(IdentityCommand.GetMyProfile))).Code);
            Assert.Equal("INVALID_LOGIN", (await send(new(IdentityCommand.CustomerLogin)
            { Identifier = "a@example.test", Password = "CustomerPass1" })).Code);
            Assert.True((await send(new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = "ChangedPass2" })).Success);
            Assert.Equal("A updated", (await send(new(IdentityCommand.GetMyProfile))).Profile!.Contact.FullName);
            Assert.Equal("INVALID_INPUT", (await send(new(IdentityCommand.RequestLink) { Reference = "KH900001" })).Code);

            const string evidence = "Yêu cầu liên kết hồ sơ khách hàng cũ KH900001 - kiểm thử P03";
            Assert.True((await send(new(IdentityCommand.RequestLink)
            { Reference = "KH900001", Evidence = evidence, CustomerId = 2 })).Success);
            var stored = Assert.Single(fixture.Store.Links).Value;
            Assert.Equal(1, stored.Account);
            Assert.Equal(CustomerLinkState.Pending, stored.State);
            Assert.Null(stored.Customer);
            Assert.Equal(evidence, stored.Evidence);
            Assert.Equal("LINK_EXISTS", (await send(new(IdentityCommand.RequestLink)
            { Reference = "KH900001", Evidence = evidence })).Code);
            var status = (await send(new(IdentityCommand.GetMyLink))).Link!;
            Assert.Equal("Pending", status.State);
            Assert.Equal("KH900001", status.Reference);
            Assert.Null(status.CustomerCode);
            Assert.Equal("NOT_LINKED", (await send(new(IdentityCommand.GetMyLinkedCustomer))).Code);
            Assert.Equal("UNAUTHORIZED", (await send(new(IdentityCommand.ApproveLink)
            { LinkId = stored.Id, CustomerId = 41, Reason = "Không được tự duyệt" })).Code);
            Assert.Equal("SESSION_EXPIRED", (await send(new(IdentityCommand.RevokeLink)
            { LinkId = stored.Id, Reason = "Không được tự thu hồi" })).Code);
            Assert.Equal(0, fixture.Store.ReviewCalls);
            Assert.True((await send(new(IdentityCommand.Logout))).Success);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SupportedOSPlatform("windows")]
    public async Task OwnershipRepositoryFailure_ReturnsSanitizedResponse_AndKeepsPipeUsableForRelogin(bool sqlPermissionFailure)
    {
        var fixture = new CustomerIdentityTests.Fixture(); await fixture.AddCustomer("a");
        const string secret = "Server=private;Password=private-secret; C:\\private\\customer.sql; SELECT private";
        await WithConnection(fixture, async send =>
        {
            Assert.True((await send(new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = "CustomerPass1" })).Success);
            fixture.Store.RequestLinkFailure = sqlPermissionFailure ? SqlPermissionFailure(secret) : new InvalidOperationException(secret);
            var error = await send(new(IdentityCommand.RequestLink) { Reference = "KH900001", Evidence = "Đối chiếu" });
            Assert.Equal("SERVICE_ERROR", error.Code);
            Assert.Contains(sqlPermissionFailure ? "OWNERSHIP_SQL_PERMISSION" : "OWNERSHIP_REQUEST_HANDLER", error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("private", error.Message, StringComparison.Ordinal);
            Assert.Empty(fixture.Store.Links);
            Assert.Equal("SESSION_EXPIRED", (await send(new(IdentityCommand.GetMyProfile))).Code);
            fixture.Store.RequestLinkFailure = null;
            Assert.True((await send(new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = "CustomerPass1" })).Success);
            Assert.True((await send(new(IdentityCommand.RequestLink) { Reference = "KH900001", Evidence = "Đối chiếu" })).Success);
            Assert.Equal("Pending", (await send(new(IdentityCommand.GetMyLink))).Link!.State);
        });
    }

    [Theory]
    [InlineData("closed-write", IdentityCommand.RequestLink, "OWNERSHIP_REQUEST.REQUEST_WRITE", "PIPE_CLOSED")]
    [InlineData("closed-response", IdentityCommand.RequestLink, "OWNERSHIP_REQUEST.RESPONSE_READ", "PIPE_CLOSED")]
    [InlineData("invalid-response", IdentityCommand.GetMyLink, "OWNERSHIP_STATUS.RESPONSE_READ", "INVALID_MESSAGE")]
    [SupportedOSPlatform("windows")]
    public async Task ClientTransportFailures_ReportFixedStage_WithoutExposingResponseData(string failure, IdentityCommand command, string expectedStage, string expectedReason)
    {
        using var current = WindowsIdentity.GetCurrent();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var name = "StudioManager.P03.Response." + Guid.NewGuid().ToString("N");
        await using var server = IdentityPipeServer.CreatePipe(name, current.User!, true);
        var accept = server.WaitForConnectionAsync(timeout.Token);
        await using var client = await CustomerPipeClient.ConnectLocalPipeAsync(name, timeout.Token);
        await accept;
        await using var rpc = AttachVerifiedTestPipe(client);
        var serverWork = Task.Run(async () =>
        {
            if (failure == "closed-write") { client.Dispose(); return; }
            await IdentityProtocol.ReadAsync<IdentityRequest>(server, timeout.Token);
            if (failure == "closed-response") { await server.DisposeAsync(); return; }
            using var invalid = Frame("""{"Success":false,"Code":"PRIVATE_SQL","Message":"Password=private-secret; C:\\private\\sql","Unexpected":"private-customer"}""");
            await server.WriteAsync(invalid.ToArray(), timeout.Token);
            await server.FlushAsync(timeout.Token);
        }, timeout.Token);
        if (failure == "closed-write") await serverWork;
        var response = await rpc.SendAsync(new(command) { Reference = "KH900001", Evidence = "Đối chiếu" }, timeout.Token);
        await serverWork;
        Assert.False(response.Success);
        Assert.Equal(command == IdentityCommand.RequestLink ? "OUTCOME_UNKNOWN" : "CONNECTION_LOST", response.Code);
        Assert.EndsWith($"Mã bước: {expectedStage}; {expectedReason}.", response.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("private", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRIVATE_SQL", response.Message, StringComparison.Ordinal);
    }

    // Construct a provider error in memory: no SQL connection, grant or database command is executed.
    private static SqlException SqlPermissionFailure(string message)
    {
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(c => c.GetParameters() is var p && p.Length == 9 && p[7].ParameterType == typeof(uint));
        var error = (SqlError)constructor.Invoke([229, (byte)1, (byte)14, "private-server", message, "private-procedure", 1, (uint)0, null]);
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(errors, [error]);
        var create = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
            null, [typeof(SqlErrorCollection), typeof(string)], null)!;
        return (SqlException)create.Invoke(null, [errors, "16.0"])!;
    }

    [SupportedOSPlatform("windows")]
    private static CustomerPipeClient AttachVerifiedTestPipe(NamedPipeClientStream pipe)
    {
        // The unique pipe is created/connected by this test; production still requires SCM verification.
        var rpc = new CustomerPipeClient();
        typeof(CustomerPipeClient).GetField("_pipe", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(rpc, pipe);
        return rpc;
    }

    [SupportedOSPlatform("windows")]
    private static async Task WithConnection(CustomerIdentityTests.Fixture fixture, Func<Func<IdentityRequest, Task<IdentityResponse>>, Task> action)
    {
        await using var connection = new LocalIdentityPipe(fixture);
        await action(connection.SendAsync);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task UnregisteredServerProcess_IsRefusedBeforeCredentialExchange()
    {
        using var current = WindowsIdentity.GetCurrent();
        var name = "StudioManager.P03.Untrusted." + Guid.NewGuid().ToString("N");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var server = IdentityPipeServer.CreatePipe(name, current.User!, true);
        var accept = server.WaitForConnectionAsync(timeout.Token);
        await using var client = await CustomerPipeClient.ConnectLocalPipeAsync(name, timeout.Token);
        await accept;
        Assert.Throws<IOException>(() => CustomerPipeClient.VerifyServiceProcess(client.SafePipeHandle));
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task ExistingPipeName_CannotBeSilentlyAdoptedByAuthority()
    {
        using var current = WindowsIdentity.GetCurrent();
        var name = "StudioManager.P03.First." + Guid.NewGuid().ToString("N");
        await using var first = IdentityPipeServer.CreatePipe(name, current.User!, true);
        Assert.Throws<UnauthorizedAccessException>(() => IdentityPipeServer.CreatePipe(name, current.User!, true));
    }

    private static MemoryStream Frame(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var stream = new MemoryStream(); var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        stream.Write(header); stream.Write(bytes); stream.Position = 0; return stream;
    }
}
