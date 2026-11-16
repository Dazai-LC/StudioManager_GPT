using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Principal;
using StudioManager.CustomerApi;
using StudioManager.IdentityService;
using StudioManager.Application;
using StudioManager.Application.Services;

namespace StudioManager.Tests;

/// <summary>Unique real local pipes, production handler/client, in-memory repositories. Never uses SQL or SCM services.</summary>
[SupportedOSPlatform("windows")]
internal sealed class LocalIdentityPipe(CustomerIdentityTests.Fixture fixture, TimeProvider? time = null, TimeSpan? idleTimeout = null,
    IBookingRequestRepository? bookingRepository = null) : IAsyncDisposable
{
    private static readonly FieldInfo PipeField = typeof(CustomerPipeClient).GetField("_pipe", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly CancellationTokenSource _stop = new(TimeSpan.FromSeconds(30));
    private readonly List<Task> _handlers = [];
    internal CustomerPipeClient Client { get; } = new(time ?? TimeProvider.System);
    internal int Connections => _handlers.Count;
    internal Task LastHandler => _handlers[^1];
    internal NamedPipeClientStream? LastStream { get; private set; }

    internal async Task<IdentityResponse> SendAsync(IdentityRequest request)
    {
        if (PipeField.GetValue(Client) is null && request.Command is IdentityCommand.CustomerLogin or IdentityCommand.StaffLogin or IdentityCommand.Register)
            await OpenAsync();
        return await Client.SendAsync(request, _stop.Token);
    }

    private async Task OpenAsync()
    {
        using var current = WindowsIdentity.GetCurrent();
        var name = "StudioManager.P03.Lifecycle." + Guid.NewGuid().ToString("N");
        var pipe = IdentityPipeServer.CreatePipe(name, current.User!, true);
        try
        {
            var accept = pipe.WaitForConnectionAsync(_stop.Token);
            var stream = await CustomerPipeClient.ConnectLocalPipeAsync(name, _stop.Token);
            await accept;
            Attach(Client, stream); LastStream = stream;
            var authority = fixture.Connection();
            var dispatcher = new IdentityDispatcher(authority, bookingRepository is null ? null : new BookingRequestService(authority, bookingRepository, fixture.Clock));
            var server = new IdentityPipeServer(current.User!, () => new IdentityDispatcher(fixture.Connection()));
            _handlers.Add(server.HandleAsync(pipe, dispatcher, _stop.Token, idleTimeout));
        }
        catch { await pipe.DisposeAsync(); throw; }
    }

    internal static void Attach(CustomerPipeClient client, NamedPipeClientStream stream)
    {
        // Only a test-owned, already connected pipe. The public constructor's SCM verification is unchanged;
        // rejection of an unregistered process and pipe ACLs are covered by the existing boundary tests.
        PipeField.SetValue(client, stream);
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        await Client.DisposeAsync();
        await Task.WhenAll(_handlers).WaitAsync(TimeSpan.FromSeconds(5));
        _stop.Dispose();
    }
}

internal sealed class PipeTestTime : TimeProvider
{
    private long _timestamp = 1;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);
    internal void Advance(TimeSpan elapsed) => Interlocked.Add(ref _timestamp, elapsed.Ticks);
}
