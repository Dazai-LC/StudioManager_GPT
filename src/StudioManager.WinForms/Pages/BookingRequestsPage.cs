using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.CustomerDesktop;

namespace StudioManager.WinForms.Pages;

/// <summary>The internal desktop uses the same P03 service login and P04 UI, with no transport principal fabrication.</summary>
public sealed class BookingRequestsPage : UserControl
{
    private readonly StaffServiceSession _session;
    private readonly TextBox _username = new() { Width = 180 }, _password = new() { Width = 160, UseSystemPasswordChar = true, MaxLength = 128 };
    private readonly Label _sessionLabel = new() { AutoSize = true };
    private readonly Button _signIn = new() { Text = "Đăng nhập nhân sự", AutoSize = true };
    private readonly Button _logout = new() { Text = "Đăng xuất dịch vụ", AutoSize = true };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 84 };
    private readonly BookingWorkspaceControl _workspace;
    private bool _busy, _signedIn, _closed;
    private long _viewGeneration;
    internal BookingRequestsPage(AppFacade app, StaffServiceSession session)
    {
        _session = session;
        Dock = DockStyle.Fill; BackColor = Theme.Background; Font = Theme.Font();
        _username.Text = app.Session?.TenDangNhap ?? "";
        _username.ReadOnly = true; // Service authentication belongs to the signed-in desktop account.
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
        login.Controls.Add(_sessionLabel);
        login.Controls.Add(_username); login.Controls.Add(_password);
        _signIn.Click += async (_, _) => await Run(async () =>
        {
            var pending = _session.LoginAsync(_password.Text);
            _password.Clear(); // No password is retained for navigation or automatic re-login.
            var result = await pending;
            if (_closed || IsDisposed || Disposing) return;
            Report(result); UpdateSessionView();
            if (!result.Success || !_signedIn) return;
            await _workspace.InitializeAsync();
        });
        _logout.Click += async (_, _) => await Run(_session.LogoutAsync);
        login.Controls.Add(_signIn); login.Controls.Add(_logout);
        Controls.Add(_workspace); Controls.Add(login); Controls.Add(_status);
        _session.Changed += SessionChanged;
        UpdateSessionView();
    }

    internal void ActivateSession() => _ = Run(async () =>
    {
        await _session.CheckValidityAsync();
        if (_closed || IsDisposed || Disposing) return;
        UpdateSessionView();
        if (_signedIn) await _workspace.InitializeAsync();
    });
    private bool Current(long generation) => !_closed && !IsDisposed && !Disposing && generation == Volatile.Read(ref _viewGeneration);

    private async Task<IdentityResponse> Send(IdentityRequest request)
    {
        var generation = Volatile.Read(ref _viewGeneration);
        if (!Current(generation)) throw new OperationCanceledException();
        // Unmounting this view discards its callback, but must not cancel a shared authenticated RPC.
        var response = await _session.SendAsync(request);
        if (!Current(generation)) throw new OperationCanceledException();
        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_closed)
        {
            _closed = true; Interlocked.Increment(ref _viewGeneration);
            _session.Changed -= SessionChanged;
            _signedIn = false; _password.Clear(); _workspace.ResetSession();
        }
        base.Dispose(disposing);
    }

    private void SessionChanged(object? sender, EventArgs e)
    {
        Interlocked.Increment(ref _viewGeneration);
        if (_closed || IsDisposed || Disposing) return;
        _workspace.ResetSession(); _password.Clear(); UpdateSessionView();
        if (_signedIn && !_busy && Parent is not null) ActivateSession();
    }
    private void UpdateSessionView()
    {
        _signedIn = _session.IsAuthenticated; _workspace.Enabled = _signedIn;
        _username.Text = _session.AccountName;
        _username.Visible = _password.Visible = _signIn.Visible = !_signedIn;
        _logout.Visible = _signedIn;
        _sessionLabel.Text = _signedIn ? "Đang dùng dịch vụ: " + _session.AccountName : "Đăng nhập dịch vụ để duyệt đặt lịch";
        _status.Text = _session.Message;
    }
    private void Report(IdentityResponse response)
    {
        if (_closed || IsDisposed || Disposing) return;
        _status.Text = response.Message;
    }
    private async Task Run(Func<Task> work)
    {
        if (_busy || _closed || IsDisposed || Disposing) return;
        _busy = true; Enabled = false;
        try { await work(); }
        catch (OperationCanceledException) { /* Unmounted/reset view: ignore its stale completion. */ }
        catch { if (!_closed && !IsDisposed && !Disposing) { _workspace.ResetSession(); UpdateSessionView(); _status.Text = "Không thể tải trạng thái. Vui lòng mở lại trang; không tự gửi lại thao tác."; } }
        finally { _busy = false; if (!_closed && !IsDisposed && !Disposing) Enabled = true; }
    }
}
