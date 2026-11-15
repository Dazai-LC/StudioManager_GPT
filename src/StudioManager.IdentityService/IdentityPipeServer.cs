using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using StudioManager.Application;
using StudioManager.CustomerApi;

namespace StudioManager.IdentityService;

[SupportedOSPlatform("windows")]
public sealed class IdentityPipeServer(SecurityIdentifier clientGroup, Func<IdentityDispatcher> createDispatcher)
{
    private readonly LoginBudget _budget = new();

    public async Task RunAsync(CancellationToken stop)
    {
        using var slots = new SemaphoreSlim(16, 16);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var tasks = new List<Task>();
        var listener = CreatePipe(IdentityProtocol.PipeName, clientGroup, true);
        try
        {
            while (!stop.IsCancellationRequested)
            {
                await slots.WaitAsync(stop);
                try { await listener.WaitForConnectionAsync(stop); }
                catch { slots.Release(); throw; }
                var connected = listener;
                try { listener = CreatePipe(IdentityProtocol.PipeName, clientGroup, false); }
                catch { connected.Dispose(); slots.Release(); throw; }
                tasks.RemoveAll(task => task.IsCompleted);
                tasks.Add(RunConnectionAsync(connected, slots, lifetime.Token));
            }
        }
        finally
        {
            lifetime.Cancel();
            listener.Dispose();
            try { await Task.WhenAll(tasks); } catch { }
        }
    }

    private async Task RunConnectionAsync(NamedPipeServerStream stream, SemaphoreSlim slots, CancellationToken ct)
    {
        try { await HandleAsync(stream, createDispatcher(), ct); }
        catch (Exception error)
        {
            try { await stream.DisposeAsync(); } catch { }
            SafeDiagnostics.Capture(error, "P03.ConnectionStartup");
        }
        finally { slots.Release(); }
    }

    public static PipeSecurity CreateSecurity(SecurityIdentifier service, SecurityIdentifier clients)
    {
        var security = new PipeSecurity();
        security.SetAccessRuleProtection(true, false);
        security.SetOwner(service);
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
            PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new PipeAccessRule(service, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(clients, PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize, AccessControlType.Allow));
        return security;
    }

    internal static NamedPipeServerStream CreatePipe(string name, SecurityIdentifier clients, bool first)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var security = CreateSecurity(identity.User!, clients);
        return NamedPipeServerStreamAcl.Create(name, PipeDirection.InOut, 17, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | (first ? PipeOptions.FirstPipeInstance : PipeOptions.None), 4096, 4096, security);
    }

    internal async Task HandleAsync(NamedPipeServerStream stream, IdentityDispatcher dispatcher, CancellationToken stop,
        TimeSpan? idleTimeout = null)
    {
        // Tests may shorten the wait; production always uses the finite shared policy.
        var wait = idleTimeout ?? IdentityProtocol.ConnectionIdleTimeout;
        if (wait <= TimeSpan.Zero || wait > IdentityProtocol.ConnectionIdleTimeout) throw new ArgumentOutOfRangeException(nameof(idleTimeout));
        await using var owned = stream;
        try
        {
            while (!stop.IsCancellationRequested && stream.IsConnected)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
                timeout.CancelAfter(wait);
                IdentityRequest request;
                try { request = await IdentityProtocol.ReadAsync<IdentityRequest>(stream, timeout.Token); }
                catch (System.Text.Json.JsonException)
                {
                    await IdentityProtocol.WriteAsync(stream, new IdentityResponse(false, "INVALID_REQUEST", "Yêu cầu không hợp lệ."), timeout.Token);
                    break;
                }
                timeout.Token.ThrowIfCancellationRequested();
                // A complete request gets its own bounded deadline, not the remainder of the idle wait.
                timeout.CancelAfter(IdentityProtocol.RequestTimeout);
                IdentityResponse response;
                if (_budget.Allow(request)) response = await dispatcher.DispatchAsync(request, timeout.Token);
                else
                {
                    dispatcher.Disconnect();
                    response = new(false, "RATE_LIMITED", "Có quá nhiều yêu cầu. Vui lòng thử lại sau.");
                }
                await IdentityProtocol.WriteAsync(stream, response, timeout.Token);
                if (IdentityProtocol.RequiresSignIn(response) || request.Command == IdentityCommand.Logout ||
                    (request.Command == IdentityCommand.ChangePassword && response.Success)) break;
            }
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or ObjectDisposedException) { }
        catch (Exception error) { SafeDiagnostics.Capture(error, "P03.PipeConnection"); }
        finally { dispatcher.Disconnect(); }
    }

    private sealed class LoginBudget
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, (DateTime Until, int Attempts)> _entries = new(StringComparer.Ordinal);
        internal bool Allow(IdentityRequest request)
        {
            if (request.Command is not (IdentityCommand.CustomerLogin or IdentityCommand.StaffLogin or IdentityCommand.Register)) return true;
            var now = DateTime.UtcNow;
            var identifier = (request.Identifier ?? "").Trim().ToLowerInvariant();
            var key = request.Command == IdentityCommand.Register ? "register" :
                request.Command + ":" + identifier[..Math.Min(identifier.Length, 254)];
            lock (_sync)
            {
                foreach (var expired in _entries.Where(pair => pair.Value.Until <= now).Select(pair => pair.Key).ToArray()) _entries.Remove(expired);
                if (!_entries.TryGetValue(key, out var state))
                {
                    if (_entries.Count >= 1024) return false;
                    state = (now.AddMinutes(5), 0);
                }
                _entries[key] = (state.Until, state.Attempts + 1);
                return state.Attempts < (request.Command == IdentityCommand.Register ? 20 : 10);
            }
        }
    }
}
