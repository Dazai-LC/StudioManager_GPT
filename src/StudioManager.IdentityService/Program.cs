using System.Runtime.Versioning;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Infrastructure;

namespace StudioManager.IdentityService;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static void Main()
    {
        try
        {
            if (Environment.UserInteractive) throw new InvalidOperationException("Run only through the reviewed Windows Service installation.");
            ServiceBase.Run(new IdentityWindowsService());
        }
        catch (Exception error)
        {
            SafeDiagnostics.Capture(error, "P03.ServiceStartup");
            Environment.ExitCode = 1;
        }
    }
}

[SupportedOSPlatform("windows")]
internal sealed class IdentityWindowsService : ServiceBase
{
    private readonly CancellationTokenSource _stop = new();
    private Task? _worker;
    internal IdentityWindowsService() { ServiceName = IdentityProtocol.ServiceName; AutoLog = false; CanStop = true; }

    protected override void OnStart(string[] args)
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("A dedicated non-administrator service identity is required.");
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "StudioManager", "IdentityService", "service.json");
            var settings = JsonSerializer.Deserialize<ServiceSettings>(File.ReadAllText(path)) ?? throw new InvalidOperationException("Service configuration required.");
            var connection = new SqlConnectionStringBuilder(settings.ConnectionString);
            if (!connection.IntegratedSecurity || string.IsNullOrWhiteSpace(connection.DataSource) ||
                string.IsNullOrWhiteSpace(connection.InitialCatalog) || !string.IsNullOrEmpty(connection.UserID) ||
                !string.IsNullOrEmpty(connection.Password) || !string.IsNullOrEmpty(connection.AttachDBFilename) ||
                connection.UserInstance || !string.IsNullOrEmpty(connection.FailoverPartner) ||
                connection.Encrypt == SqlConnectionEncryptOption.Optional)
                throw new InvalidOperationException("Reviewed Windows-authenticated encrypted SQL configuration required.");
            var clients = new SecurityIdentifier(settings.ClientGroupSid);
            if (clients == identity.User || clients.IsWellKnown(WellKnownSidType.WorldSid) ||
                clients.IsWellKnown(WellKnownSidType.AuthenticatedUserSid))
                throw new InvalidOperationException("A dedicated reviewed local client group is required.");
            var repository = new SqlCustomerIdentityRepository(connection.ConnectionString);
            var staff = new SqlCustomerStaffDirectory(new SqlStudioRepository(connection.ConnectionString));
            var hasher = new Pbkdf2PasswordHasher();
            var clock = new UtcClock();
            var bookingRepository = new SqlBookingRequestRepository(connection.ConnectionString);
            var server = new IdentityPipeServer(clients, () =>
            {
                var authority = new CustomerIdentityService(repository, staff, hasher, clock);
                return new IdentityDispatcher(authority, new BookingRequestService(authority, bookingRepository, clock));
            });
            _worker = server.RunAsync(_stop.Token);
            if (_worker.IsCompleted) _worker.GetAwaiter().GetResult();
            _ = _worker.ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception is { } error) SafeDiagnostics.Capture(error, "P03.ServiceLoop");
                if (!_stop.IsCancellationRequested) { ExitCode = 1; Stop(); }
            }, TaskScheduler.Default);
        }
        catch (Exception error)
        {
            SafeDiagnostics.Capture(error, "P03.ServiceStartup");
            throw new InvalidOperationException("Identity service could not start. Review its protected configuration and installation.");
        }
    }

    protected override void OnStop()
    {
        _stop.Cancel();
        try { _worker?.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        catch (Exception error) { SafeDiagnostics.Capture(error, "P03.ServiceStop"); }
    }

    private sealed record ServiceSettings(string ConnectionString, string ClientGroupSid)
    {
        public override string ToString() => "ServiceSettings { Configuration = [private] }";
    }
    private sealed class UtcClock : IClock { public DateTime Now => DateTime.UtcNow; }
}
