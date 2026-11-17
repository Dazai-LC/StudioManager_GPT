using System.Runtime.Versioning;
using System.Security.Principal;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Infrastructure;

namespace StudioManager.IdentityService;

[SupportedOSPlatform("windows")]
internal static class LocalDemoConsoleHost
{
    internal static async Task RunAsync(LocalDemoProfile profile, CancellationToken stop)
    {
        if (!Environment.UserInteractive || !string.Equals(Environment.MachineName, "MSI", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Local demo requires an interactive Windows session on MSI.");
        profile.ValidateExecutable(Environment.ProcessPath, LocalDemoComponent.IdentityService);
        var connection = LocalDemoSqlSettings.Load(AppContext.BaseDirectory);
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User ?? throw new InvalidOperationException("A local Windows user is required.");
        var repository = new SqlCustomerIdentityRepository(connection);
        var staff = new SqlCustomerStaffDirectory(new SqlStudioRepository(connection));
        var bookings = new SqlBookingRequestRepository(connection);
        var hasher = new Pbkdf2PasswordHasher();
        var clock = new UtcClock();
        var server = IdentityPipeServer.ForLocalDemo(user, () =>
        {
            var authority = new CustomerIdentityService(repository, staff, hasher, clock);
            return new IdentityDispatcher(authority, new BookingRequestService(authority, bookings, clock));
        });
        await RunLifetimeAsync(server, () => LocalDemoHostIdentity.Publish(profile), stop);
    }

    // The same lifetime/pipe server is exercised offline with in-memory application repositories.
    internal static async Task RunLifetimeAsync(IdentityPipeServer server, Func<IDisposable> publish, CancellationToken stop)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var worker = server.RunAsync(lifetime.Token);
        try
        {
            // RunAsync creates the first protected pipe before yielding; never publish a failed listener.
            if (worker.IsCompleted) await worker;
            using var lease = publish();
            Console.WriteLine("LOCAL DEMO host ready; use the matching --local-demo --runtime profile. Ctrl+C to stop.");
            try { await worker; }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }
        finally
        {
            lifetime.Cancel();
            try { await worker; } catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private sealed class UtcClock : IClock { public DateTime Now => DateTime.UtcNow; }
}
