using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace StudioManager.CustomerApi;

/// <summary>Development-only start/reuse. No SQL commands, SCM access, process termination or config writes.</summary>
[SupportedOSPlatform("windows")]
public static class LocalDemoHostStartup
{
    public static void EnsureRunning(LocalDemoProfile profile)
    {
        if (!profile.IsDevelopment || !Environment.UserInteractive ||
            !string.Equals(Environment.MachineName, "MSI", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Automatic Local Demo startup requires the explicit development profile in an interactive MSI session.");
        profile.ValidateMarker();
        // Serialize concurrent Staff/Customer F5 launches, including recovery and readiness verification.
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profile.RuntimeFile.ToUpperInvariant())));
        using var gate = new Mutex(false, @"Local\StudioManager.LocalDemo.Start." + key);
        var acquired = false;
        try
        {
            try { acquired = gate.WaitOne(TimeSpan.FromSeconds(25)); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new InvalidOperationException("Another Local Demo startup is still running. Wait and press F5 again.");
            if (ReuseOrArchiveLease(profile, ObserveIfRunning))
            {
                VerifyReady(profile);
                return;
            }

            var executable = profile.HostExecutable; // Always the existing, verified packaged host, never the Windows Service.
            var folder = profile.ConfigurationDirectory(LocalDemoComponent.IdentityService);
            foreach (var file in new[] { executable, Path.Combine(folder, "appsettings.json"),
                Path.Combine(folder, "StudioManager.IdentityService.runtimeconfig.json"),
                Path.Combine(folder, "StudioManager.IdentityService.deps.json"), Path.Combine(folder, "StudioManager.CustomerApi.dll") })
            {
                LocalDemoProfile.RejectReparsePoints(file);
                if (!File.Exists(file)) throw new InvalidOperationException("The existing packaged Local Demo host/configuration is incomplete. Restore the approved package; no default configuration will be created.");
            }
            if (AssemblyName.GetAssemblyName(Path.Combine(folder, "StudioManager.IdentityService.dll")).Name != "StudioManager.IdentityService")
                throw new InvalidOperationException("Unexpected Local Demo host assembly.");
            var start = new ProcessStartInfo(executable)
                { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = folder };
            start.ArgumentList.Add("--local-demo"); start.ArgumentList.Add("--runtime"); start.ArgumentList.Add(profile.RuntimeFile);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the Local Demo console host.");
            var until = Environment.TickCount64 + 15000;
            while (!File.Exists(profile.RuntimeFile))
            {
                if (process.HasExited || Environment.TickCount64 >= until)
                    throw new InvalidOperationException("The packaged Local Demo host did not become ready. Check its approved appsettings.json and Local Demo diagnostics; no process was terminated or production fallback attempted.");
                Thread.Sleep(100);
            }
            // Publish may still be flushing its small descriptor. Retry only for this new process, within the deadline.
            while (true)
            {
                try
                {
                    var lease = LocalDemoHostIdentity.Read(profile);
                    if (lease.ProcessId != process.Id) throw new InvalidOperationException("Another host claimed the Local Demo lease. No process was terminated.");
                    break;
                }
                catch (InvalidOperationException) when (!process.HasExited && Environment.TickCount64 < until) { Thread.Sleep(100); }
            }
            VerifyReady(profile);
        }
        finally { if (acquired) gate.ReleaseMutex(); }
    }

    internal static bool ReuseOrArchiveLease(LocalDemoProfile profile, Func<int, LocalDemoHostIdentity.Host?> observe)
    {
        profile.ValidateMarker();
        if (!File.Exists(profile.RuntimeFile)) return false;
        LocalDemoHostIdentity.Host lease;
        try
        {
            lease = LocalDemoHostIdentity.ReadDescriptor(profile);
        }
        catch { throw Retained("The lease data/ACL is invalid or inaccessible. Use the same Windows user as the demo host and inspect the lease."); }
        LocalDemoHostIdentity.Host? observed;
        try { observed = observe(lease.ProcessId); }
        catch { throw Retained($"PID {lease.ProcessId} could not be positively identified as a dead process or the matching host."); }
        if (observed is not null)
        {
            try
            {
                using var current = WindowsIdentity.GetCurrent();
                LocalDemoHostIdentity.Validate(profile, lease, observed, current.User!.Value);
            }
            catch { throw Retained($"PID {lease.ProcessId} is live but its executable, start time, Windows user or session differs from the lease. Do not kill it or remove the lease."); }
            return true;
        }
        try
        {
            // Denies concurrent readers/writers. A running host holds Share.Read and prevents this open/rename.
            using var guard = new FileStream(profile.RuntimeFile, FileMode.Open, FileAccess.Read, FileShare.Delete);
            if (LocalDemoHostIdentity.ReadDescriptor(guard) != lease || observe(lease.ProcessId) is not null)
                throw new IOException("Local Demo lease/process changed during recovery.");
            // Preserve the crashed host's descriptor as evidence; never overwrite or delete artifacts.
            var archive = profile.RuntimeFile + ".stale-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
            File.Move(profile.RuntimeFile, archive);
            return false;
        }
        catch
        {
            throw Retained("The lease is locked or its data/process changed during recovery. Wait for any concurrent startup and retry.");
        }
    }

    private static InvalidOperationException Retained(string reason) => new(
        "Local Demo startup refused: " + reason + " The lease was retained. No process or Windows Service was stopped.");

    internal static LocalDemoHostIdentity.Host? ObserveIfRunning(int pid)
    {
        Process process;
        try { process = Process.GetProcessById(pid); }
        catch (ArgumentException) { return null; } // Only a positively absent PID is stale.
        using (process)
        {
            if (process.HasExited) return null;
            return LocalDemoHostIdentity.Observe(pid); // Access denied, other session, PID reuse etc. fail closed.
        }
    }

    private static void VerifyReady(LocalDemoProfile profile)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var pipe = CustomerPipeClient.ConnectLocalPipeAsync(LocalDemoProfile.PipeName, timeout.Token).GetAwaiter().GetResult();
            LocalDemoHostIdentity.VerifyPeer(profile, pipe.SafePipeHandle);
            // No protocol request or credentials are sent by this readiness probe.
        }
        catch
        {
            throw new InvalidOperationException("The recorded Local Demo host is not ready or its pipe identity could not be verified. It was left running; no production fallback or automatic restart was attempted.");
        }
    }
}
