using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using StudioManager.CustomerApi;
using StudioManager.WinForms;
using Xunit;

namespace StudioManager.Tests;

/// <summary>Temporary files, OS identity reads and launch plans only. No live demo, SQL, service or GUI is started.</summary>
[SupportedOSPlatform("windows")]
public sealed class LocalDemoDevelopmentTests
{
    [Theory]
    [InlineData(LocalDemoComponent.Staff)]
    [InlineData(LocalDemoComponent.CustomerClient)]
    public void DevelopmentSelection_UsesSourceDebugClientsAndExistingPackagedHost(LocalDemoComponent component)
    {
        using var workspace = new Workspace();
        var profile = workspace.Select(component);
        Assert.True(profile.IsDevelopment);
        Assert.Equal(workspace.Runtime, profile.RuntimeFile);
        Assert.Equal(workspace.Executable(component), profile.Executable(component));
        Assert.Equal(Path.Combine(workspace.Demo, "IdentityService", "StudioManager.IdentityService.exe"), profile.HostExecutable);
        Assert.Equal(Path.Combine(workspace.Demo, "Staff"), profile.ConfigurationDirectory(LocalDemoComponent.Staff));
        Assert.Equal([LocalDemoDevelopment.Option], profile.LaunchArguments);
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess([], workspace.Executable(component), component, requireLocalDemo: true));
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess(
            ["--local-demo", "--runtime", workspace.Runtime], workspace.Executable(component), component));
    }

    [Theory]
    [InlineData("settings")]
    [InlineData("marker")]
    [InlineData("profile")]
    [InlineData("unknown-property")]
    public void MissingOrIncorrectDevelopmentSelection_FailsClosed(string invalid)
    {
        using var workspace = new Workspace();
        if (invalid == "settings") File.Delete(workspace.Settings);
        else if (invalid == "marker") File.Delete(Path.Combine(workspace.Demo, LocalDemoProfile.MarkerFileName));
        else if (invalid == "profile") workspace.WriteSettings("artifacts/demo/local-demo-host.json", "foreign-profile");
        else File.WriteAllText(workspace.Settings, "{\"Profile\":\"StudioManager.LocalDemo.20261116A.v1\",\"RuntimeFile\":\"artifacts/demo/local-demo-host.json\",\"Fallback\":true}");
        Assert.Throws<InvalidOperationException>(() => workspace.Select(LocalDemoComponent.Staff));
        Assert.False(File.Exists(workspace.Runtime));
    }

    [Theory]
    [InlineData("../outside/local-demo-host.json")]
    [InlineData("artifacts/../outside/local-demo-host.json")]
    [InlineData("artifacts/demo/service.json")]
    [InlineData(@"C:\other\local-demo-host.json")]
    [InlineData(@"\\MSI\share\local-demo-host.json")]
    public void DevelopmentRuntime_CannotEscapeRepositoryArtifactsOrChangeLeaseName(string runtime)
    {
        using var workspace = new Workspace();
        workspace.WriteSettings(runtime);
        Assert.Throws<InvalidOperationException>(() => workspace.Select(LocalDemoComponent.Staff));
    }

    [Fact]
    public void DevelopmentFlag_RejectsExtraArgumentsReleaseOutputsAndHostSelection()
    {
        using var workspace = new Workspace();
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess(
            [LocalDemoDevelopment.Option, "extra"], workspace.Executable(LocalDemoComponent.Staff), LocalDemoComponent.Staff));
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess(
            [LocalDemoDevelopment.Option], workspace.Executable(LocalDemoComponent.Staff).Replace("Debug", "Release"), LocalDemoComponent.Staff));
        Assert.Throws<InvalidOperationException>(() => LocalDemoProfile.SelectForProcess(
            [LocalDemoDevelopment.Option], workspace.Executable(LocalDemoComponent.Staff), LocalDemoComponent.IdentityService));
        Assert.Throws<InvalidOperationException>(() => LocalDemoHostStartup.EnsureRunning(workspace.PackagedProfile));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealCustomerLaunchPlan_SupportsDebugAndPackagedLayoutsAndRegistration(bool registration)
    {
        using var workspace = new Workspace();
        foreach (var profile in new[] { workspace.Select(LocalDemoComponent.Staff), workspace.PackagedProfile })
        {
            workspace.CopyCustomerOutput(profile.Executable(LocalDemoComponent.CustomerClient));
            var start = CustomerPortalLauncher.Prepare(profile.Executable(LocalDemoComponent.Staff), profile, registration);
            Assert.Equal(profile.Executable(LocalDemoComponent.CustomerClient), start.FileName);
            Assert.Equal(Path.GetDirectoryName(start.FileName), start.WorkingDirectory);
            Assert.False(start.UseShellExecute);
            var expected = profile.LaunchArguments.Concat(registration ? ["--register"] : Array.Empty<string>());
            Assert.Equal(expected, start.ArgumentList);
        }
    }

    [Fact]
    public void CustomerLaunch_RejectsMissingPortalAndRuntimeChangesInsteadOfOpeningLegacyUi()
    {
        using var workspace = new Workspace();
        var profile = workspace.Select(LocalDemoComponent.Staff);
        Assert.ThrowsAny<Exception>(() => CustomerPortalLauncher.Prepare(profile.Executable(LocalDemoComponent.Staff), profile, false));
        workspace.CopyCustomerOutput(profile.Executable(LocalDemoComponent.CustomerClient));
        workspace.WriteSettings("artifacts/other/local-demo-host.json");
        Directory.CreateDirectory(Path.Combine(workspace.Root, "artifacts", "other"));
        File.WriteAllText(Path.Combine(workspace.Root, "artifacts", "other", LocalDemoProfile.MarkerFileName), LocalDemoProfile.ProfileName);
        Assert.Throws<InvalidOperationException>(() => CustomerPortalLauncher.Prepare(profile.Executable(LocalDemoComponent.Staff), profile, true));
    }

    [Fact]
    public void DeadPidLease_IsArchivedIntactWithoutDeletingAnyEvidence()
    {
        using var workspace = new Workspace();
        var lease = workspace.Host();
        using (workspace.WriteLease(lease)) { }
        var bytes = File.ReadAllBytes(workspace.Runtime);
        Assert.False(LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile, _ => null));
        Assert.False(File.Exists(workspace.Runtime));
        var archive = Assert.Single(Directory.GetFiles(workspace.Demo, "local-demo-host.json.stale-*"));
        Assert.Equal(bytes, File.ReadAllBytes(archive));
    }

    [Fact]
    public void VerifiedLiveHost_IsReusedWithoutChangingItsLease()
    {
        using var workspace = new Workspace();
        var lease = workspace.Host();
        using var liveLease = workspace.WriteLease(lease);
        Assert.True(LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile, _ => lease));
        Assert.Empty(Directory.GetFiles(workspace.Demo, "*.stale-*"));
    }

    [Theory]
    [InlineData("pid-reuse")]
    [InlineData("path")]
    [InlineData("user")]
    [InlineData("session")]
    [InlineData("access-denied")]
    public void UnverifiableOrForeignLivePid_IsNeverRecovered(string mismatch)
    {
        using var workspace = new Workspace();
        var lease = workspace.Host();
        using (workspace.WriteLease(lease)) { }
        var observed = mismatch switch
        {
            "pid-reuse" => lease with { StartedUtcTicks = lease.StartedUtcTicks + 1 },
            "path" => lease with { Executable = Environment.ProcessPath! },
            "user" => lease with { UserSid = "S-1-5-18" },
            "session" => lease with { SessionId = lease.SessionId + 1 },
            _ => lease
        };
        Assert.Throws<InvalidOperationException>(() => LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile,
            _ => mismatch == "access-denied" ? throw new UnauthorizedAccessException() : observed));
        Assert.True(File.Exists(workspace.Runtime));
        Assert.Empty(Directory.GetFiles(workspace.Demo, "*.stale-*"));
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("pipe")]
    [InlineData("path")]
    [InlineData("pid")]
    [InlineData("ticks")]
    [InlineData("session")]
    public void InvalidLeaseData_IsRetainedBeforeAnyProcessLookup(string invalid)
    {
        using var workspace = new Workspace();
        var lease = workspace.Host();
        lease = invalid switch
        {
            "profile" => lease with { Profile = "foreign" },
            "pipe" => lease with { Pipe = IdentityProtocol.PipeName },
            "path" => lease with { Executable = Environment.ProcessPath! },
            "pid" => lease with { ProcessId = 0 },
            "ticks" => lease with { StartedUtcTicks = long.MaxValue },
            _ => lease with { SessionId = -1 }
        };
        using (workspace.WriteLease(lease)) { }
        var lookedUp = false;
        Assert.Throws<InvalidOperationException>(() => LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile, _ => { lookedUp = true; return null; }));
        Assert.False(lookedUp);
        Assert.True(File.Exists(workspace.Runtime));
    }

    [Fact]
    public void LockedLease_AndPidBecomingLiveDuringRecovery_AreRetained()
    {
        using var workspace = new Workspace();
        var lease = workspace.Host();
        using (workspace.WriteLease(lease))
            Assert.Throws<InvalidOperationException>(() => LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile, _ => null));
        var calls = 0;
        Assert.Throws<InvalidOperationException>(() => LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile, _ => ++calls == 1 ? null : lease));
        Assert.True(File.Exists(workspace.Runtime));
        Assert.Empty(Directory.GetFiles(workspace.Demo, "*.stale-*"));
    }

    [Theory]
    [InlineData("{invalid")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Profile\":\"x\",\"Profile\":\"x\",\"Pipe\":\"x\",\"ProcessId\":1,\"StartedUtcTicks\":1,\"Executable\":\"x\",\"UserSid\":\"x\"}")]
    public void MalformedLease_IsRetained(string json)
    {
        using var workspace = new Workspace();
        using (workspace.WriteLease(workspace.Host())) { }
        File.WriteAllText(workspace.Runtime, json);
        Assert.Throws<InvalidOperationException>(() => LocalDemoHostStartup.ReuseOrArchiveLease(workspace.PackagedProfile, _ => throw new Exception("Must not inspect process")));
        Assert.Equal(json, File.ReadAllText(workspace.Runtime));
    }

    [Fact]
    public void ProcessObservation_ReadsCurrentIdentityWithoutChangingTheProcess()
    {
        var observed = LocalDemoHostStartup.ObserveIfRunning(Environment.ProcessId);
        Assert.NotNull(observed);
        Assert.Equal(Environment.ProcessPath, observed.Executable, ignoreCase: true);
        using var current = Process.GetCurrentProcess();
        Assert.False(current.HasExited);
    }

    private sealed class Workspace : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "StudioManager.Startup.Offline." + Guid.NewGuid().ToString("N"));
        internal string Demo => Path.Combine(Root, "artifacts", "demo");
        internal string Runtime => Path.Combine(Demo, LocalDemoProfile.RuntimeFileName);
        internal string Settings => Path.Combine(Root, LocalDemoDevelopment.SettingsFileName);
        internal LocalDemoProfile PackagedProfile => LocalDemoProfile.Parse(["--local-demo", "--runtime", Runtime])!;

        internal Workspace()
        {
            Directory.CreateDirectory(Demo);
            File.WriteAllText(Path.Combine(Root, "StudioManager.sln"), "offline fixture");
            File.WriteAllText(Path.Combine(Demo, LocalDemoProfile.MarkerFileName), LocalDemoProfile.ProfileName);
            foreach (var component in new[] { LocalDemoComponent.Staff, LocalDemoComponent.CustomerClient })
            {
                var executable = Executable(component);
                Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
                var project = component == LocalDemoComponent.Staff ? "StudioManager.WinForms" : "StudioManager.CustomerClient";
                File.WriteAllText(Path.Combine(Root, "src", project, project + ".csproj"), "<Project />");
            }
            WriteSettings("artifacts/demo/local-demo-host.json");
        }

        internal string Executable(LocalDemoComponent component) => LocalDemoDevelopment.Executable(Root, component);
        internal LocalDemoProfile Select(LocalDemoComponent component) => LocalDemoProfile.SelectForProcess([LocalDemoDevelopment.Option], Executable(component), component)!;
        internal void WriteSettings(string runtime, string profile = LocalDemoProfile.ProfileName) =>
            File.WriteAllText(Settings, JsonSerializer.Serialize(new { Profile = profile, RuntimeFile = runtime }));

        internal LocalDemoHostIdentity.Host Host()
        {
            using var user = WindowsIdentity.GetCurrent();
            using var current = Process.GetCurrentProcess();
            return new(LocalDemoProfile.ProfileName, LocalDemoProfile.PipeName, current.Id,
                current.StartTime.ToUniversalTime().Ticks, PackagedProfile.HostExecutable, user.User!.Value, current.SessionId);
        }

        internal FileStream WriteLease(LocalDemoHostIdentity.Host lease)
        {
            using var user = WindowsIdentity.GetCurrent();
            var security = new FileSecurity();
            security.SetOwner(user.User!); security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(user.User!, FileSystemRights.FullControl, AccessControlType.Allow));
            var stream = new FileInfo(Runtime).Create(FileMode.CreateNew, FileSystemRights.Read | FileSystemRights.Write,
                FileShare.Read, 4096, FileOptions.None, security);
            JsonSerializer.Serialize(stream, lease); stream.Flush(true);
            return stream;
        }

        internal void CopyCustomerOutput(string targetExecutable)
        {
            var repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(repository.FullName, "StudioManager.sln"))) repository = repository.Parent ?? throw new InvalidOperationException("Build/test from repository.");
            var source = Path.GetDirectoryName(LocalDemoDevelopment.Executable(repository.FullName, LocalDemoComponent.CustomerClient))!;
            var target = Path.GetDirectoryName(targetExecutable)!;
            Directory.CreateDirectory(target);
            foreach (var file in new[] { "StudioManager.CustomerClient.exe", "StudioManager.CustomerClient.dll",
                "StudioManager.CustomerClient.deps.json", "StudioManager.CustomerClient.runtimeconfig.json", "StudioManager.CustomerApi.dll" })
                File.Copy(Path.Combine(source, file), Path.Combine(target, file));
        }

        public void Dispose() => Directory.Delete(Root, true); // Only this test's unique temporary fixture.
    }
}
