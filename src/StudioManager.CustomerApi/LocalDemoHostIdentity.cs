using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32.SafeHandles;

namespace StudioManager.CustomerApi;

/// <summary>Small current-user host lease; never contains SQL settings or customer credentials.</summary>
[SupportedOSPlatform("windows")]
public static class LocalDemoHostIdentity
{
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    internal sealed record Host(string Profile, string Pipe, int ProcessId, long StartedUtcTicks, string Executable, string UserSid, int SessionId);

    public static IDisposable Publish(LocalDemoProfile profile)
    {
        profile.ValidateExecutable(Environment.ProcessPath, LocalDemoComponent.IdentityService);
        return PublishCore(profile, Observe(Environment.ProcessId));
    }

    internal static IDisposable PublishCore(LocalDemoProfile profile, Host host)
    {
        LocalDemoProfile.RejectReparsePoints(profile.RuntimeFile);
        using var current = WindowsIdentity.GetCurrent();
        var sid = current.User ?? throw new InvalidOperationException("A local Windows user is required.");
        var security = new FileSecurity();
        security.SetOwner(sid); security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
        // CreateNew refuses a stale/occupied lease. Share.Read prevents modification/deletion while the host runs.
        var file = new FileInfo(profile.RuntimeFile).Create(FileMode.CreateNew, FileSystemRights.Read | FileSystemRights.Write,
            FileShare.Read, 4096, FileOptions.None, security);
        try
        {
            JsonSerializer.Serialize(file, host, Json); file.Flush(true);
            return new Lease(file, profile.RuntimeFile);
        }
        catch { file.Dispose(); File.Delete(profile.RuntimeFile); throw; }
    }

    internal static Host Read(LocalDemoProfile profile)
    {
        try
        {
            var host = ReadDescriptor(profile);
            using var current = WindowsIdentity.GetCurrent();
            Validate(profile, host, Observe(host.ProcessId), current.User!.Value);
            return host;
        }
        catch { throw new InvalidOperationException("Local demo host identity is unavailable or invalid; start the matching console host first."); }
    }

    internal static Host ReadDescriptor(LocalDemoProfile profile)
    {
        LocalDemoProfile.RejectReparsePoints(profile.RuntimeFile);
        var info = new FileInfo(profile.RuntimeFile);
        if (!info.Exists || info.Length is <= 0 or > 4096) throw new InvalidOperationException("Missing or invalid Local Demo lease.");
        using var current = WindowsIdentity.GetCurrent();
        var sid = current.User ?? throw new InvalidOperationException("A local Windows user is required.");
        var acl = info.GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
        if (!sid.Equals(acl.GetOwner(typeof(SecurityIdentifier))) || !acl.AreAccessRulesProtected)
            throw new InvalidOperationException("The Local Demo lease must be protected and owned by the current Windows user.");
        foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            if (rule.AccessControlType == AccessControlType.Allow && !sid.Equals(rule.IdentityReference))
                throw new InvalidOperationException("The Local Demo lease grants access to another Windows user.");
        using var stream = new FileStream(profile.RuntimeFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var host = ReadDescriptor(stream);
        ValidateDescriptor(profile, host, sid.Value);
        return host;
    }

    internal static Host ReadDescriptor(Stream stream)
    {
        if (stream.Length is <= 0 or > 4096) throw new InvalidOperationException("Invalid Local Demo lease size.");
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Invalid Local Demo lease.");
        var names = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != 7 || names.Distinct(StringComparer.Ordinal).Count() != 7)
            throw new InvalidOperationException("Incomplete or ambiguous Local Demo lease.");
        return document.RootElement.Deserialize<Host>(Json) ?? throw new InvalidOperationException("Invalid Local Demo lease.");
    }

    internal static void ValidateDescriptor(LocalDemoProfile profile, Host lease, string currentSid)
    {
        if (lease.Profile != LocalDemoProfile.ProfileName || lease.Pipe != LocalDemoProfile.PipeName ||
            lease.ProcessId <= 0 || lease.StartedUtcTicks <= 0 || lease.StartedUtcTicks > DateTime.UtcNow.Ticks ||
            lease.UserSid != currentSid || lease.SessionId < 0 ||
            !string.Equals(lease.Executable, profile.HostExecutable, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Local demo host identity refused.");
    }

    public static void VerifyPeer(LocalDemoProfile profile, SafePipeHandle pipe)
    {
        var host = Read(profile);
        ValidatePipeProcess(pipe, host.ProcessId);
        using var current = WindowsIdentity.GetCurrent();
        Validate(profile, host, Observe(host.ProcessId), current.User!.Value);
    }

    internal static void ValidatePipeProcess(SafePipeHandle pipe, int expectedPid)
    {
        if (expectedPid <= 0 || !GetNamedPipeServerProcessId(pipe, out var pid) || pid != expectedPid)
            throw new IOException("Local demo host identity refused.");
    }

    internal static void Validate(LocalDemoProfile profile, Host lease, Host observed, string currentSid)
    {
        ValidateDescriptor(profile, lease, currentSid);
        if (lease.ProcessId != observed.ProcessId || lease.StartedUtcTicks != observed.StartedUtcTicks ||
            lease.UserSid != observed.UserSid || lease.SessionId != observed.SessionId ||
            !string.Equals(observed.Executable, profile.HostExecutable, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Local demo host identity refused.");
    }

    internal static Host Observe(int pid)
    {
        using var process = Process.GetProcessById(pid);
        using var current = Process.GetCurrentProcess();
        if (process.SessionId != current.SessionId || !OpenProcessToken(process.SafeHandle, 8, out var token))
            throw new IOException("Local demo host identity unavailable.");
        using (token)
        using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
            return new(LocalDemoProfile.ProfileName, LocalDemoProfile.PipeName, pid,
                process.StartTime.ToUniversalTime().Ticks, process.MainModule!.FileName,
                identity.User!.Value, process.SessionId);
    }

    private sealed class Lease(FileStream file, string path) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; file.Dispose(); File.Delete(path);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
}
