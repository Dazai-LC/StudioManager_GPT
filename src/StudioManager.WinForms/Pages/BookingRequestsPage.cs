using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.CustomerDesktop;

namespace StudioManager.WinForms.Pages;

/// <summary>The internal desktop uses the same P03 service login and P04 UI, with no transport principal fabrication.</summary>
public sealed class BookingRequestsPage : UserControl
{
    private readonly CustomerPipeClient _client = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Func<IdentityRequest, CancellationToken, Task<IdentityResponse>> _send;
    private readonly TextBox _username = new() { Width = 180 }, _password = new() { Width = 160, UseSystemPasswordChar = true, MaxLength = 128 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 84 };
    private readonly BookingWorkspaceControl _workspace;
    private readonly System.Windows.Forms.Timer _idle = new() { Interval = 1000 };
    private bool _busy, _signedIn, _closed;
    private long _viewGeneration;
    private Task _shutdown = Task.CompletedTask;

    public BookingRequestsPage(AppFacade app) : this(app, null) { }

    internal BookingRequestsPage(AppFacade app, Func<IdentityRequest, CancellationToken, Task<IdentityResponse>>? send)
    {
        _send = send ?? _client.SendAsync;
        Dock = DockStyle.Fill; BackColor = Theme.Background; Font = Theme.Font();
        _username.Text = app.Session?.TenDangNhap ?? "";
        _workspace = new(true, Send, Report, Run) { Enabled = false };
        _workspace.OpenInternalBooking = async id =>
        {
            var generation = Volatile.Read(ref _viewGeneration);
            var booking = await app.Bookings.GetAsync(id);
            if (booking is null || !Current(generation) || !_signedIn) return;
            using var dialog = new BookingDetailDialog(app, booking);
            dialog.ShowDialog(this);
        };
        var login = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(8), WrapContents = true };
        login.Controls.Add(new Label { Text = "Đăng nhập dịch vụ để duyệt đặt lịch", AutoSize = true });
        login.Controls.Add(_username); login.Controls.Add(_password);
        var signIn = new Button { Text = "Đăng nhập nhân sự", AutoSize = true };
        signIn.Click += async (_, _) => await Run(async () =>
        {
            var request = new IdentityRequest(IdentityCommand.StaffLogin) { Identifier = _username.Text, Password = _password.Text };
            SignedOut(); // Failed re-login must never retain old authenticated controls/data.
            var result = await Send(request);
            _password.Clear(); Report(result);
            if (!result.Success) return;
            _signedIn = true; _workspace.Enabled = true; _idle.Start();
            await _workspace.InitializeAsync();
        });
        var logout = new Button { Text = "Đăng xuất dịch vụ", AutoSize = true };
        logout.Click += async (_, _) => await Run(async () => { SignedOut(); Report(await Send(new(IdentityCommand.Logout))); });
        login.Controls.Add(signIn); login.Controls.Add(logout);
        Controls.Add(_workspace); Controls.Add(login); Controls.Add(_status);
        _idle.Tick += async (_, _) =>
        {
            if (_busy || !_signedIn || _closed || IsDisposed || Disposing) return;
            _busy = true;
            var generation = Volatile.Read(ref _viewGeneration);
            try { if (await _client.ExpireIdleConnectionAsync() && Current(generation)) { _status.Text = "Phiên đã hết hạn chờ. Vui lòng đăng nhập lại."; SignedOut(); } }
            catch { if (Current(generation)) { _status.Text = "Kết nối đã kết thúc. Vui lòng đăng nhập lại."; SignedOut(); } }
            finally { _busy = false; }
        };
    }
    private bool Current(long generation) => !_closed && !IsDisposed && !Disposing && generation == Volatile.Read(ref _viewGeneration);

    private async Task<IdentityResponse> Send(IdentityRequest request)
    {
        var generation = Volatile.Read(ref _viewGeneration);
        if (!Current(generation)) throw new OperationCanceledException();
        var response = await _send(request, _lifetime.Token);
        if (!Current(generation)) throw new OperationCanceledException();
        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_closed)
        {
            _closed = true; Interlocked.Increment(ref _viewGeneration);
            _idle.Stop(); _idle.Dispose(); _lifetime.Cancel();
            _shutdown = _client.DisposeAsync().AsTask();
            _ = _shutdown.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }

    private void SignedOut()
    {
        Interlocked.Increment(ref _viewGeneration);
        if (_closed || IsDisposed || Disposing) return;
        _signedIn = false; _idle.Stop(); _workspace.Enabled = false; _workspace.ResetSession(); _password.Clear();
    }
    private void Report(IdentityResponse response)
    {
        if (_closed || IsDisposed || Disposing) return;
        _status.Text = response.Message;
        if (IdentityProtocol.RequiresSignIn(response)) SignedOut();
    }
    private async Task Run(Func<Task> work)
    {
        if (_busy || _closed || IsDisposed || Disposing) return;
        _busy = true; Enabled = false;
        try { await work(); }
        catch (OperationCanceledException) { /* Unmounted/reset view: ignore its stale completion. */ }
        catch { if (!_closed && !IsDisposed && !Disposing) { _status.Text = "Không thể hoàn tất. Vui lòng đăng nhập lại và tải trạng thái trước khi thao tác tiếp."; SignedOut(); } }
        finally { _busy = false; if (!_closed && !IsDisposed && !Disposing) Enabled = true; }
    }
}
