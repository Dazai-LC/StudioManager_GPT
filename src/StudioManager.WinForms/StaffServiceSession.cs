using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Domain;

namespace StudioManager.WinForms;

/// <summary>One real service connection per MainForm. Navigation never owns or cancels this session.</summary>
internal sealed class StaffServiceSession : IAsyncDisposable
{
    private readonly AppFacade _app;
    private readonly CustomerPipeClient _client = new();
    private readonly Func<IdentityRequest, CancellationToken, Task<IdentityResponse>> _send;
    private readonly Func<Task<bool>> _expire;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly System.Windows.Forms.Timer _idle = new() { Interval = 1000 };
    private UserSession? _owner;
    private bool _authenticated, _polling, _disposed;
    private long _revision;
    private Task? _shutdown;

    internal StaffServiceSession(AppFacade app,
        Func<IdentityRequest, CancellationToken, Task<IdentityResponse>>? send = null, Func<Task<bool>>? expire = null)
    {
        _app = app; _send = send ?? _client.SendAsync; _expire = expire ?? _client.ExpireIdleConnectionAsync;
        _idle.Tick += async (_, _) => await CheckValidityAsync();
        _idle.Start();
    }

    internal event EventHandler? Changed;
    internal string AccountName => _app.Session?.TenDangNhap ?? "";
    internal string Message { get; private set; } = "Đăng nhập dịch vụ một lần để duyệt đặt lịch trong phiên làm việc này.";
    internal bool IsAuthenticated => !_disposed && _authenticated && OwnsCurrentSession();

    private bool OwnsCurrentSession() => _owner is not null && _app.Session is { PhaiDoiMatKhau: false } current &&
        current.SessionToken != Guid.Empty && current.SessionToken == _owner.SessionToken &&
        current.TaiKhoanId == _owner.TaiKhoanId && current.VaiTro == _owner.VaiTro &&
        current.TenDangNhap == _owner.TenDangNhap;

    internal async Task<IdentityResponse> LoginAsync(string password)
    {
        await _gate.WaitAsync();
        try
        {
            if (_disposed) return Ended();
            if (IsAuthenticated) return new(true, "OK", "Đang sử dụng phiên dịch vụ hiện tại.");
            SetSignedOut("Đang xác thực dịch vụ…");
            await CloseConnectionAsync();
            if (_app.Session is not { PhaiDoiMatKhau: false } owner || owner.SessionToken == Guid.Empty)
            {
                SetSignedOut("Phiên ứng dụng đã kết thúc. Vui lòng đăng nhập lại ứng dụng nhân sự.");
                return Ended();
            }
            _owner = owner;
            var response = await _send(new(IdentityCommand.StaffLogin)
                { Identifier = owner.TenDangNhap, Password = password }, _lifetime.Token);
            if (_disposed || !OwnsCurrentSession())
            {
                await CloseConnectionAsync();
                if (!_disposed) SetSignedOut("Tài khoản ứng dụng đã thay đổi. Vui lòng xác thực lại dịch vụ.");
                return Ended();
            }
            _authenticated = response.Success; Message = response.Message;
            if (!response.Success) { _owner = null; await CloseConnectionAsync(); }
            Changed?.Invoke(this, EventArgs.Empty);
            return response;
        }
        finally { _gate.Release(); }
    }

    internal async Task<IdentityResponse> SendAsync(IdentityRequest request)
    {
        var revision = _revision;
        await _gate.WaitAsync();
        try
        {
            if (_disposed || revision != _revision) return Ended();
            if (!IsAuthenticated)
            {
                await CloseConnectionAsync();
                SetSignedOut("Phiên dịch vụ đã kết thúc hoặc tài khoản đã thay đổi. Vui lòng đăng nhập lại.");
                return Ended();
            }
            var response = await _send(request, _lifetime.Token);
            if (_disposed || !OwnsCurrentSession())
            {
                await CloseConnectionAsync();
                if (!_disposed) SetSignedOut("Tài khoản ứng dụng đã thay đổi. Dữ liệu phiên cũ đã được xóa.");
                return Ended();
            }
            if (IdentityProtocol.RequiresSignIn(response))
            {
                await CloseConnectionAsync(); SetSignedOut(response.Message);
            }
            return response;
        }
        finally { _gate.Release(); }
    }

    internal async Task LogoutAsync()
    {
        SetSignedOut("Đã đăng xuất dịch vụ."); // Invalidate queued work before waiting for the current RPC.
        await _gate.WaitAsync();
        try { await CloseConnectionAsync(); }
        finally { _gate.Release(); }
    }

    internal async Task CheckValidityAsync()
    {
        if (_disposed || _polling) return;
        _polling = true;
        await _gate.WaitAsync();
        try
        {
            if (_disposed || !_authenticated) return;
            if (!OwnsCurrentSession())
            {
                await CloseConnectionAsync();
                SetSignedOut("Tài khoản ứng dụng đã thay đổi. Vui lòng đăng nhập lại dịch vụ.");
            }
            else if (await _expire())
            {
                await CloseConnectionAsync();
                SetSignedOut("Phiên dịch vụ đã hết hạn chờ. Vui lòng đăng nhập lại.");
            }
        }
        catch
        {
            await CloseConnectionAsync();
            if (!_disposed) SetSignedOut("Kết nối dịch vụ đã kết thúc. Vui lòng đăng nhập lại.");
        }
        finally { _gate.Release(); _polling = false; }
    }

    private async Task CloseConnectionAsync() => await _client.SendAsync(new(IdentityCommand.Logout));
    private void SetSignedOut(string message)
    {
        _authenticated = false; _owner = null; _revision++; Message = message;
        Changed?.Invoke(this, EventArgs.Empty);
    }
    private static IdentityResponse Ended() => new(false, "SESSION_EXPIRED", "Phiên dịch vụ không còn hiệu lực. Vui lòng đăng nhập lại.");

    public ValueTask DisposeAsync()
    {
        if (_shutdown is not null) return new(_shutdown);
        _disposed = true; _idle.Stop(); _idle.Dispose(); _lifetime.Cancel();
        SetSignedOut("Đã kết thúc phiên làm việc.");
        _shutdown = ShutdownAsync();
        return new(_shutdown);
    }

    private async Task ShutdownAsync()
    {
        try { await _client.DisposeAsync(); }
        finally { _lifetime.Dispose(); }
    }
}
