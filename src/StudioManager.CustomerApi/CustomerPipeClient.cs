using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace StudioManager.CustomerApi;

/// <summary>No SQL reference/configuration. Authentication is scoped to this live pipe connection.</summary>
[SupportedOSPlatform("windows")]
public sealed class CustomerPipeClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _time;
    private NamedPipeClientStream? _pipe;
    private long _leaseStarted;
    private bool _leaseActive, _sessionExpired;
    private volatile bool _disposed;

    public CustomerPipeClient() : this(TimeProvider.System) { }
    internal CustomerPipeClient(TimeProvider time) => _time = time;

    public async Task<IdentityResponse> SendAsync(IdentityRequest request, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        var stage = "CONNECTION_OPEN";
        try
        {
            if (_disposed) return new(false, "CLIENT_CLOSED", "Ứng dụng đã đóng kết nối.");
            await ExpireIdleCoreAsync();
            if (request.Command == IdentityCommand.Logout)
            {
                await ClosePipeAsync();
                return new(true, "OK", "Đã đăng xuất.");
            }
            var startsSession = request.Command is IdentityCommand.CustomerLogin or IdentityCommand.StaffLogin;
            if (_sessionExpired && !startsSession && request.Command != IdentityCommand.Register) return SessionExpired();
            if (_pipe is null)
            {
                _pipe = await ConnectLocalPipeAsync(IdentityProtocol.PipeName, ct);
                stage = "SERVICE_IDENTITY";
                VerifyServiceProcess(_pipe.SafePipeHandle); // Before sending any password or profile.
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(IdentityProtocol.RequestTimeout);
            // Starts before the outbound request, conservatively before the server's next idle interval.
            _leaseStarted = _time.GetTimestamp(); _leaseActive = true;
            stage = "REQUEST_WRITE";
            await IdentityProtocol.WriteAsync(_pipe, request, timeout.Token);
            stage = "RESPONSE_READ";
            var response = await IdentityProtocol.ReadAsync<IdentityResponse>(_pipe, timeout.Token);
            if (IdentityProtocol.RequiresSignIn(response) || (request.Command == IdentityCommand.ChangePassword && response.Success))
                await ClosePipeAsync();
            else if (startsSession && response.Success) _sessionExpired = false;
            return response;
        }
        catch (Exception error)
        {
            await ClosePipeAsync();
            var operation = request.Command switch
            {
                IdentityCommand.RequestLink => "OWNERSHIP_REQUEST",
                IdentityCommand.GetMyLink => "OWNERSHIP_STATUS",
                IdentityCommand.ApproveLink => "OWNERSHIP_APPROVE",
                IdentityCommand.RejectLink => "OWNERSHIP_REJECT",
                IdentityCommand.RevokeLink => "OWNERSHIP_REVOKE",
                IdentityCommand.ListLinkReviews => "OWNERSHIP_REVIEW_LIST",
                IdentityCommand.GetMyLinkedCustomer => "OWNERSHIP_READ",
                IdentityCommand.SubmitBookingRequest or IdentityCommand.CancelBookingRequest or
                    IdentityCommand.ApproveBookingRequest or IdentityCommand.RejectBookingRequest => "BOOKING_MUTATION",
                IdentityCommand.PreviewBookingRequest or IdentityCommand.CheckBookingAvailability => "BOOKING_PREVIEW",
                _ => "IDENTITY_COMMAND"
            };
            var reason = error switch
            {
                System.Text.Json.JsonException or InvalidDataException => "INVALID_MESSAGE",
                EndOfStreamException or ObjectDisposedException => "PIPE_CLOSED",
                OperationCanceledException => "TIMEOUT",
                IOException => "PIPE_IO",
                _ => "CLIENT_FAILURE"
            };
            var exchanged = stage is "REQUEST_WRITE" or "RESPONSE_READ";
            var uncertainWrite = exchanged && (request.Command is IdentityCommand.Register or IdentityCommand.UpdateMyProfile or
                IdentityCommand.ChangePassword or IdentityCommand.RequestLink or IdentityCommand.ApproveLink or
                IdentityCommand.RejectLink or IdentityCommand.RevokeLink or IdentityCommand.SubmitBookingRequest or
                IdentityCommand.CancelBookingRequest or IdentityCommand.ApproveBookingRequest or IdentityCommand.RejectBookingRequest);
            var code = uncertainWrite ? "OUTCOME_UNKNOWN" : exchanged ? "CONNECTION_LOST" : "SERVICE_UNAVAILABLE";
            var message = uncertainWrite
                ? "Chưa xác định được kết quả thao tác. Vui lòng đăng nhập lại và tải trạng thái trước khi thực hiện tiếp; ứng dụng không tự gửi lại yêu cầu."
                : exchanged ? "Kết nối phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại."
                : "Không thể liên lạc dịch vụ định danh. Vui lòng đăng nhập lại.";
            return new(false, code, $"{message} Mã bước: {operation}.{stage}; {reason}.");
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true; // Queued/future calls must not reopen a connection while disposal waits for an RPC.
        await _gate.WaitAsync();
        try { await ClosePipeAsync(); }
        finally { _gate.Release(); }
    }

    public async Task<bool> ExpireIdleConnectionAsync()
    {
        await _gate.WaitAsync();
        try { return !_disposed && await ExpireIdleCoreAsync(); }
        finally { _gate.Release(); }
    }

    private async Task<bool> ExpireIdleCoreAsync()
    {
        if (_pipe is null || !_leaseActive || _time.GetElapsedTime(_leaseStarted) < IdentityProtocol.ClientIdleTimeout) return false;
        await ClosePipeAsync();
        return true;
    }

    private async Task ClosePipeAsync()
    {
        var pipe = _pipe; _pipe = null;
        _leaseActive = false; _sessionExpired = true;
        if (pipe is not null) { try { await pipe.DisposeAsync(); } catch { } }
    }

    private static IdentityResponse SessionExpired() => new(false, "SESSION_EXPIRED",
        "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại; yêu cầu chưa được gửi.");

    // SCM is the authority for the registered service PID. A squatted pipe cannot solicit passwords.
    internal static void VerifyServiceProcess(SafePipeHandle pipe)
    {
        if (!GetNamedPipeServerProcessId(pipe, out var pipePid)) throw new IOException("Service identity unavailable.");
        var manager = OpenSCManager(null, null, 1);
        if (manager == IntPtr.Zero) throw new IOException("Service identity unavailable.");
        try
        {
            var service = OpenService(manager, IdentityProtocol.ServiceName, 4);
            if (service == IntPtr.Zero) throw new IOException("Service identity unavailable.");
            try
            {
                if (!QueryServiceStatusEx(service, 0, out var status, Marshal.SizeOf<ServiceStatus>(), out _) ||
                    status.CurrentState != 4 || status.ProcessId == 0 || status.ProcessId != pipePid)
                    throw new IOException("Service identity refused.");
            }
            finally { CloseServiceHandle(service); }
        }
        finally { CloseServiceHandle(manager); }
    }

    internal static async Task<NamedPipeClientStream> ConnectLocalPipeAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(name) || name.Any(c => c is '\\' or '/')) throw new IOException("Local pipe name refused.");
        var until = Environment.TickCount64 + 5000;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            // Individual read/write/attribute/synchronize rights, never GENERIC_WRITE / CREATE_PIPE_INSTANCE.
            // Anonymous SQOS prevents a pipe server from impersonating the client's Windows identity.
            var handle = CreateFile(@"\\.\pipe\" + name, 0x00100083, 0, IntPtr.Zero, 3, 0x40100000, IntPtr.Zero);
            if (!handle.IsInvalid)
            {
                try { return new NamedPipeClientStream(PipeDirection.InOut, true, true, handle); }
                catch { handle.Dispose(); throw; }
            }
            var error = Marshal.GetLastWin32Error(); handle.Dispose();
            if (error is not (2 or 231) || Environment.TickCount64 >= until) throw new IOException("Local identity service unavailable.");
            await Task.Delay(50, ct);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatus
    { public uint ServiceType, CurrentState, ControlsAccepted, Win32ExitCode, ServiceSpecificExitCode, CheckPoint, WaitHint, ProcessId, ServiceFlags; }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafePipeHandle CreateFile(string name, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flags, IntPtr template);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(string? machine, string? database, uint access);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenService(IntPtr manager, string name, uint access);
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryServiceStatusEx(IntPtr service, int infoLevel, out ServiceStatus status, int size, out int needed);
    [DllImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(IntPtr handle);
}
