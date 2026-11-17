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
            LocalDemoProfile.RejectReparsePoints(profile.RuntimeFile);
            var info = new FileInfo(profile.RuntimeFile);
            if (!info.Exists || info.Length is <= 0 or > 4096) throw new InvalidOperationException();
            using var current = WindowsIdentity.GetCurrent();
            var sid = current.User ?? throw new InvalidOperationException();
            var acl = info.GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
            if (!sid.Equals(acl.GetOwner(typeof(SecurityIdentifier))) || !acl.AreAccessRulesProtected)
                throw new InvalidOperationException();
            foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && !sid.Equals(rule.IdentityReference))
                    throw new InvalidOperationException();
            using var stream = new FileStream(profile.RuntimeFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var host = JsonSerializer.Deserialize<Host>(stream, Json) ?? throw new InvalidOperationException();
            Validate(profile, host, Observe(host.ProcessId), sid.Value);
            return host;
        }
        catch { throw new InvalidOperationException("Local demo host identity is unavailable or invalid; start the matching console host first."); }
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
        if (lease.Profile != LocalDemoProfile.ProfileName || lease.Pipe != LocalDemoProfile.PipeName ||
            lease.ProcessId <= 0 || lease.StartedUtcTicks <= 0 || lease.UserSid != currentSid ||
            lease.ProcessId != observed.ProcessId || lease.StartedUtcTicks != observed.StartedUtcTicks ||
            lease.UserSid != observed.UserSid || lease.SessionId != observed.SessionId ||
            !string.Equals(lease.Executable, profile.HostExecutable, StringComparison.OrdinalIgnoreCase) ||
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
