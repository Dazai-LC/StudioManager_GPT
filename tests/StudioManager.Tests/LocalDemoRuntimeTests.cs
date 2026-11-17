using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using StudioManager.CustomerApi;
using StudioManager.IdentityService;
using StudioManager.Infrastructure;
using Xunit;

namespace StudioManager.Tests;

/// <summary>No SQL, installed service, UI or production configuration is opened by these tests.</summary>
[SupportedOSPlatform("windows")]
public sealed class LocalDemoRuntimeTests
{
    private const string Connection = @"Server=MSI\SQLEXPRESS;Database=StudioManager_P04_Test_20261116A;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=StudioManager.LocalDemo.Staff";

    [Fact]
    public async Task NoArguments_PreserveProductionClientAndServerDefaults()
    {
        Assert.Null(LocalDemoProfile.Parse([]));
        Assert.Equal("StudioManager.Identity.v1", IdentityProtocol.PipeName);
        Assert.Equal("StudioManagerIdentity", IdentityProtocol.ServiceName);
        using var user = WindowsIdentity.GetCurrent();
        var server = new IdentityPipeServer(user.User!, () => throw new InvalidOperationException());
        await using var client = new CustomerPipeClient();
        Assert.Equal(IdentityProtocol.PipeName, server.EndpointName);
        Assert.Equal(IdentityProtocol.PipeName, client.EndpointName);
        Assert.DoesNotContain(typeof(CustomerPipeClient).Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name!.Contains("SqlClient") || assembly.Name.Contains("Infrastructure"));
    }

    [Theory]
    [InlineData("--local-demo")]
    [InlineData("--console")]
    [InlineData("--pipe")]
    public void IncompleteOrInventedOptions_FailClosed(string option) =>
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.Parse([option]));

    [Theory]
    [InlineData("local-demo-host.json")]
    [InlineData(@"\\MSI\share\local-demo-host.json")]
    [InlineData(@"C:\demo\service.json")]
    [InlineData(@"C:\demo\local-demo-host.json:stream")]
    [InlineData("")]
    public void InvalidRuntimePath_FailsClosed(string path) =>
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.Parse(["--local-demo", "--runtime", path]));

    [Fact]
    public void DuplicateOrWrongProfileOptions_FailClosed()
    {
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.Parse(["--local-demo", "--pipe", "anything"]));
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.Parse(["--local-demo", "--runtime", @"C:\demo\local-demo-host.json", "--local-demo"]));
    }

    [Theory]
    [InlineData(LocalDemoComponent.IdentityService, "IdentityService", "StudioManager.IdentityService.exe")]
    [InlineData(LocalDemoComponent.CustomerClient, "CustomerClient", "StudioManager.CustomerClient.exe")]
    [InlineData(LocalDemoComponent.Staff, "Staff", "StudioManager.WinForms.exe")]
    public void MatchingRuntimeLayout_IsRequiredForEveryComponent(LocalDemoComponent component, string directory, string exe)
    {
        using var folder = new DemoFolder();
        var expected = Path.Combine(folder.Path, directory, exe);
        folder.Profile.ValidateExecutable(expected, component);
        Assert.Throws<InvalidOperationException>(() => folder.Profile.ValidateExecutable(
            Path.Combine(folder.Path, "Unrelated", exe), component));
        Assert.Throws<InvalidOperationException>(() => folder.Profile.ValidateExecutable(null, component));
    }

    [Theory]
    [InlineData(LocalDemoComponent.IdentityService)]
    [InlineData(LocalDemoComponent.CustomerClient)]
    [InlineData(LocalDemoComponent.Staff)]
    public void MarkedDemoOutputs_RequireExplicitSelection_AndCannotFallBackToProduction(LocalDemoComponent component)
    {
        using var folder = new DemoFolder();
        var exe = folder.Profile.Executable(component);
        string[] args = ["--local-demo", "--runtime", folder.Profile.RuntimeFile];
        Assert.Equal(folder.Profile.RuntimeFile, LocalDemoProfile.SelectForProcess(args, exe, component)!.RuntimeFile);
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess([], exe, component));
        File.WriteAllText(Path.Combine(folder.Path, LocalDemoProfile.MarkerFileName), "incorrect-profile");
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess(args, exe, component));
        File.Delete(Path.Combine(folder.Path, LocalDemoProfile.MarkerFileName));
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess(args, exe, component));
        Assert.Null(LocalDemoProfile.SelectForProcess([], exe, component)); // An ordinary, unmarked output keeps its default.
    }

    [Fact]
    public async Task CustomerAndStaffApi_UseSameDemoPipe_WithoutProductionFallback()
    {
        using var folder = new DemoFolder();
        await using var customer = new CustomerPipeClient(TimeProvider.System, folder.Profile);
        await using var staff = new CustomerPipeClient(TimeProvider.System, folder.Profile);
        Assert.Equal(LocalDemoProfile.PipeName, customer.EndpointName);
        Assert.Equal(customer.EndpointName, staff.EndpointName);
        Assert.NotEqual(IdentityProtocol.PipeName, customer.EndpointName);
        using var user = WindowsIdentity.GetCurrent();
        Assert.Equal(customer.EndpointName, IdentityPipeServer.ForLocalDemo(user.User!, () => throw new InvalidOperationException()).EndpointName);
        Assert.Throws<InvalidOperationException>(() => IdentityPipeServer.ForLocalDemo(
            new SecurityIdentifier(WellKnownSidType.WorldSid, null), () => throw new InvalidOperationException()));
    }

    [Fact]
    public void DemoPipeAcl_GrantsOnlyTheCurrentUser_AndRetainsNetworkDenial()
    {
        using var current = WindowsIdentity.GetCurrent();
        var acl = IdentityPipeServer.CreateSecurity(current.User!, current.User!);
        Assert.True(acl.AreAccessRulesProtected);
        var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<System.IO.Pipes.PipeAccessRule>().ToArray();
        Assert.All(rules.Where(rule => rule.AccessControlType == AccessControlType.Allow),
            rule => Assert.Equal(current.User, rule.IdentityReference));
        Assert.Contains(rules, rule => rule.AccessControlType == AccessControlType.Deny &&
            rule.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.NetworkSid, null)));
    }

    [Fact]
    public void DemoSqlConfiguration_AcceptsOnlyApprovedDatabaseWithoutConnecting()
    {
        var connection = new SqlConnectionStringBuilder(LocalDemoSqlSettings.ValidateJson(Json(Connection)));
        Assert.Equal(@"MSI\SQLEXPRESS", connection.DataSource);
        Assert.Equal("StudioManager_P04_Test_20261116A", connection.InitialCatalog);
        Assert.True(connection.IntegratedSecurity && connection.TrustServerCertificate);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, connection.Encrypt);
    }

    [Theory]
    [InlineData("Data Source", @"MSI\MSSQLSERVER")]
    [InlineData("Data Source", @"localhost\SQLEXPRESS")]
    [InlineData("Initial Catalog", "StudioManager")]
    [InlineData("Initial Catalog", "StudioManager_P03_Test")]
    [InlineData("Initial Catalog", "master")]
    [InlineData("Integrated Security", "False")]
    [InlineData("Encrypt", "False")]
    [InlineData("Trust Server Certificate", "False")]
    [InlineData("User ID", "fake-user")]
    [InlineData("Password", "fake-test-secret")]
    [InlineData("AttachDBFilename", @"C:\fake\demo.mdf")]
    [InlineData("Failover Partner", "unexpected")]
    [InlineData("Host Name In Certificate", "unexpected")]
    [InlineData("Application Intent", "ReadOnly")]
    [InlineData("Server SPN", "unexpected")]
    public void UnexpectedSqlConfiguration_IsRejectedAndRedacted(string key, string value)
    {
        var connection = new SqlConnectionStringBuilder(Connection) { [key] = value };
        var error = Assert.Throws<InvalidOperationException>(() => LocalDemoSqlSettings.ValidateJson(Json(connection.ConnectionString)));
        Assert.DoesNotContain("fake-test-secret", error.ToString());
        Assert.DoesNotContain(@"C:\fake", error.ToString());
        Assert.Null(error.InnerException);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{invalid")]
    [InlineData("{\"ConnectionString\":\"\"}")]
    [InlineData("{\"ConnectionString\":\"Password=fake-test-secret\",\"Other\":true}")]
    public void MissingOrMalformedSqlConfiguration_HasNoDefault(string json) =>
        Assert.Throws<InvalidOperationException>(() => LocalDemoSqlSettings.ValidateJson(json));

    [Fact]
    public void MissingRuntimeFiles_FailBeforeAnySqlOrCredentialExchange()
    {
        using var folder = new DemoFolder();
        Assert.Throws<InvalidOperationException>(() => LocalDemoSqlSettings.Load(folder.Path));
        Assert.Throws<InvalidOperationException>(() => LocalDemoHostIdentity.Read(folder.Profile));
        Assert.False(File.Exists(folder.Profile.RuntimeFile));
    }

    [Fact]
    public void ExpectedHost_MatchesPidStartExeUserSessionAndProfile()
    {
        using var folder = new DemoFolder();
        var host = Expected(folder.Profile);
        LocalDemoHostIdentity.Validate(folder.Profile, host, host, host.UserSid);
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("pipe")]
    [InlineData("pid")]
    [InlineData("start")]
    [InlineData("exe")]
    [InlineData("sid")]
    [InlineData("session")]
    public void UnexpectedHost_IsRejected(string mismatch)
    {
        using var folder = new DemoFolder();
        var expected = Expected(folder.Profile);
        var observed = mismatch switch
        {
            "profile" => expected with { Profile = "production" },
            "pipe" => expected with { Pipe = IdentityProtocol.PipeName },
            "pid" => expected with { ProcessId = expected.ProcessId + 1 },
            "start" => expected with { StartedUtcTicks = expected.StartedUtcTicks + 1 },
            "exe" => expected with { Executable = @"C:\unexpected\StudioManager.IdentityService.exe" },
            "sid" => expected with { UserSid = "S-1-5-18" },
            _ => expected with { SessionId = expected.SessionId + 1 }
        };
        // Profile/pipe assertions are properties of the descriptor; the remaining checks compare OS observations.
        if (mismatch is "profile" or "pipe")
            Assert.Throws<IOException>(() => LocalDemoHostIdentity.Validate(folder.Profile, observed, expected, expected.UserSid));
        else Assert.Throws<IOException>(() => LocalDemoHostIdentity.Validate(folder.Profile, expected, observed, expected.UserSid));
    }

    [Fact]
    public async Task NativePipePidCheck_RejectsAnUnexpectedProcess()
    {
        using var user = WindowsIdentity.GetCurrent();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var name = "StudioManager.Demo.Offline." + Guid.NewGuid().ToString("N");
        await using var server = IdentityPipeServer.CreatePipe(name, user.User!, true);
        var accept = server.WaitForConnectionAsync(stop.Token);
        await using var client = await CustomerPipeClient.ConnectLocalPipeAsync(name, stop.Token);
        await accept;
        LocalDemoHostIdentity.ValidatePipeProcess(client.SafePipeHandle, Environment.ProcessId);
        Assert.Throws<IOException>(() => LocalDemoHostIdentity.ValidatePipeProcess(client.SafePipeHandle, Environment.ProcessId + 1));
        var observed = LocalDemoHostIdentity.Observe(Environment.ProcessId);
        Assert.Equal(user.User!.Value, observed.UserSid);
        Assert.Equal(Environment.ProcessPath, observed.Executable, ignoreCase: true);
        Assert.Equal(Environment.ProcessId, observed.ProcessId);
    }

    [Fact]
    public void HostLease_IsCurrentUserOnly_AndDoesNotOverwriteOrLeakConfiguration()
    {
        using var folder = new DemoFolder();
        var host = Expected(folder.Profile);
        using (LocalDemoHostIdentity.PublishCore(folder.Profile, host))
        {
            var info = new FileInfo(folder.Profile.RuntimeFile);
            var acl = info.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
            Assert.True(acl.AreAccessRulesProtected);
            Assert.Equal(host.UserSid, acl.GetOwner(typeof(SecurityIdentifier))!.Value);
            Assert.All(acl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>(),
                rule => Assert.Equal(host.UserSid, rule.IdentityReference.Value));
            using var reader = new StreamReader(new FileStream(folder.Profile.RuntimeFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            var text = reader.ReadToEnd();
            Assert.DoesNotContain("ConnectionString", text);
            Assert.DoesNotContain("Password", text);
            Assert.Throws<IOException>(() => LocalDemoHostIdentity.PublishCore(folder.Profile, host));
        }
        Assert.False(File.Exists(folder.Profile.RuntimeFile));
    }

    [Fact]
    public async Task SquattedDemoPipe_CannotSolicitCredentials_AndDoesNotTriggerProductionFallback()
    {
        using var folder = new DemoFolder();
        using var user = WindowsIdentity.GetCurrent();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        // A protected descriptor that claims the correct path is insufficient: OS executable observation must match.
        using var lease = LocalDemoHostIdentity.PublishCore(folder.Profile, Expected(folder.Profile));
        await using var server = IdentityPipeServer.CreatePipe(LocalDemoProfile.PipeName, user.User!, true);
        var accept = server.WaitForConnectionAsync(stop.Token);
        await using var client = new CustomerPipeClient(TimeProvider.System, folder.Profile);
        var response = await client.SendAsync(new(IdentityCommand.CustomerLogin)
            { Identifier = "synthetic@example.test", Password = "FakeTestPassword1" }, stop.Token);
        await accept;
        Assert.False(response.Success);
        Assert.Equal("SERVICE_UNAVAILABLE", response.Code);
        Assert.Contains("SERVICE_IDENTITY", response.Message);
        Assert.DoesNotContain("FakeTestPassword1", response.ToString());
        Assert.Equal(LocalDemoProfile.PipeName, client.EndpointName);
        Assert.Equal(0, await server.ReadAsync(new byte[1], stop.Token));
    }

    [Fact]
    public async Task ConsoleLifetime_UsesRealDispatcher_ClosesActiveSessionsAndRemovesLease()
    {
        using var folder = new DemoFolder();
        using var user = WindowsIdentity.GetCurrent();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var fixture = new CustomerIdentityTests.Fixture();
        await fixture.AddCustomer("a");
        IdentityDispatcher? dispatcher = null;
        var server = IdentityPipeServer.ForLocalDemo(user.User!, () => dispatcher = new(fixture.Connection()));
        var worker = LocalDemoConsoleHost.RunLifetimeAsync(server,
            () => LocalDemoHostIdentity.PublishCore(folder.Profile, Expected(folder.Profile)), stop.Token);
        try
        {
            Assert.True(File.Exists(folder.Profile.RuntimeFile));
            await using var client = await CustomerPipeClient.ConnectLocalPipeAsync(LocalDemoProfile.PipeName, stop.Token);
            await IdentityProtocol.WriteAsync(client, new IdentityRequest(IdentityCommand.CustomerLogin)
                { Identifier = "a@example.test", Password = "CustomerPass1" }, stop.Token);
            Assert.True((await IdentityProtocol.ReadAsync<IdentityResponse>(client, stop.Token)).Success);
            await IdentityProtocol.WriteAsync(client, new IdentityRequest(IdentityCommand.GetMyProfile), stop.Token);
            Assert.Equal("a@example.test", (await IdentityProtocol.ReadAsync<IdentityResponse>(client, stop.Token)).Profile!.LoginEmail);
            stop.Cancel();
            await worker.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(File.Exists(folder.Profile.RuntimeFile));
            Assert.False((await dispatcher!.DispatchAsync(new(IdentityCommand.GetMyProfile))).Success);
            Assert.Equal(0, await client.ReadAsync(new byte[1]).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally { stop.Cancel(); await worker.WaitAsync(TimeSpan.FromSeconds(5)); }
    }

    [Fact]
    public async Task OccupiedLease_StopsNewConsoleListener_WithoutReplacingTheFile()
    {
        using var folder = new DemoFolder();
        using var user = WindowsIdentity.GetCurrent();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        File.WriteAllText(folder.Profile.RuntimeFile, "already occupied");
        var fixture = new CustomerIdentityTests.Fixture();
        var server = IdentityPipeServer.ForLocalDemo(user.User!, () => new(fixture.Connection()));
        await Assert.ThrowsAsync<IOException>(() => LocalDemoConsoleHost.RunLifetimeAsync(server,
            () => LocalDemoHostIdentity.PublishCore(folder.Profile, Expected(folder.Profile)), stop.Token));
        Assert.Equal("already occupied", File.ReadAllText(folder.Profile.RuntimeFile));
        // A new first-instance listener proves the failed host released its pipe handle.
        await using var replacement = IdentityPipeServer.CreatePipe(LocalDemoProfile.PipeName, user.User!, true);
    }

    private static string Json(string connection) => JsonSerializer.Serialize(new { ConnectionString = connection });
    private static LocalDemoHostIdentity.Host Expected(LocalDemoProfile profile)
    {
        using var current = WindowsIdentity.GetCurrent();
        using var process = Process.GetCurrentProcess();
        return new(LocalDemoProfile.ProfileName, LocalDemoProfile.PipeName, process.Id,
            process.StartTime.ToUniversalTime().Ticks, profile.HostExecutable, current.User!.Value, process.SessionId);
    }

    private sealed class DemoFolder : IDisposable
    {
        internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "StudioManager.Demo.Offline." + Guid.NewGuid().ToString("N"));
        internal LocalDemoProfile Profile { get; }
        internal DemoFolder()
        {
            Directory.CreateDirectory(Path);
            File.WriteAllText(System.IO.Path.Combine(Path, LocalDemoProfile.MarkerFileName), LocalDemoProfile.ProfileName);
            Profile = LocalDemoProfile.Parse(["--local-demo", "--runtime", System.IO.Path.Combine(Path, LocalDemoProfile.RuntimeFileName)])!;
        }
        public void Dispose() => Directory.Delete(Path, true);
    }
}
