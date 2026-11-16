using StudioManager.CustomerApi;
using StudioManager.CustomerDesktop;

namespace StudioManager.CustomerClient;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new IdentityForm());
    }
}

internal sealed class IdentityForm : Form
{
    private readonly CustomerPipeClient _client = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Func<IdentityRequest, CancellationToken, Task<IdentityResponse>> _send;
    private long _viewGeneration;
    private bool _closed;
    private Task _shutdown = Task.CompletedTask;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage _login = new("Đăng nhập / Đăng ký");
    private readonly TabPage _profile = new("Hồ sơ của tôi") { Enabled = false };
    private readonly TabPage _reviews = new("Duyệt liên kết") { Enabled = false };
    private readonly TabPage _booking = new("Đặt lịch & yêu cầu") { Enabled = false };
    private readonly TabPage _bookingReviews = new("Duyệt đặt lịch") { Enabled = false };
    private readonly BookingWorkspaceControl _customerBooking;
    private readonly BookingWorkspaceControl _staffBooking;
    private bool _customerBookingLoaded, _staffBookingLoaded;
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 84, AutoEllipsis = true };
    private readonly TextBox _identifier = new(), _password = Password(), _name = new(), _phone = new(), _email = new(), _address = new();
    private readonly TextBox _current = Password(), _next = Password(), _confirm = Password();
    private readonly TextBox _reference = new(), _evidence = new(), _customerId = new(), _reason = new();
    private readonly CheckBox _staff = new() { Text = "Đăng nhập tài khoản nhân sự để duyệt liên kết", AutoSize = true };
    private readonly Label _loginEmail = new() { AutoSize = true };
    private readonly DataGridView _grid = new()
    { Dock = DockStyle.Top, Height = 280, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private readonly TextBox _regName = new(), _regPhone = new(), _regEmail = new(), _regAddress = new(), _regPassword = Password(), _regConfirm = Password();
    private long? _beforeLink;
    private readonly Stack<long?> _reviewHistory = new();
    private readonly System.Windows.Forms.Timer _idleTimer = new() { Interval = 1000 };
    private bool _busy;

    internal IdentityForm() : this(null) { }

    // Test-only composition uses an internal constructor; production always uses the SCM-verified client.
    internal IdentityForm(Func<IdentityRequest, CancellationToken, Task<IdentityResponse>>? send)
    {
        _send = send ?? _client.SendAsync;
        Text = "StudioManager — Tài khoản khách hàng";
        ClientSize = new Size(900, 770); MinimumSize = new Size(740, 650);
        Font = new Font("Segoe UI", 10);
        _customerBooking = new(false, Send, Show, Execute);
        _staffBooking = new(true, Send, Show, Execute);
        _booking.Controls.Add(_customerBooking); _bookingReviews.Controls.Add(_staffBooking);
        _tabs.TabPages.AddRange([_login, _profile, _reviews, _booking, _bookingReviews]); Controls.Add(_tabs); Controls.Add(_status);
        BuildLogin(); BuildProfile(); BuildReviews();
        _tabs.Selected += async (_, _) =>
        {
            if (_tabs.SelectedTab == _booking && _booking.Enabled && !_customerBookingLoaded)
                await Execute(async () => { _customerBookingLoaded = true; await _customerBooking.InitializeAsync(); });
            if (_tabs.SelectedTab == _bookingReviews && _bookingReviews.Enabled && !_staffBookingLoaded)
                await Execute(async () => { _staffBookingLoaded = true; await _staffBooking.InitializeAsync(); });
        };
        var logout = new Button { Text = "Đăng xuất", Dock = DockStyle.Bottom, Height = 36 };
        logout.Click += async (_, _) => await Execute(async () => { SignedOut(); Show(await Send(new(IdentityCommand.Logout))); });
        Controls.Add(logout);
        _idleTimer.Tick += async (_, _) =>
        {
            if (_busy || _closed || IsDisposed || Disposing) return;
            _busy = true;
            var generation = Volatile.Read(ref _viewGeneration);
            try
            {
                if (await _client.ExpireIdleConnectionAsync() && Current(generation))
                {
                    _status.Text = "Phiên đăng nhập đã hết hạn chờ. Vui lòng đăng nhập lại.";
                    SignedOut();
                }
            }
            catch { if (Current(generation)) { _status.Text = "Kết nối phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại."; SignedOut(); } }
            finally { _busy = false; }
        };
    }

    private bool Current(long generation) => !_closed && !IsDisposed && !Disposing && generation == Volatile.Read(ref _viewGeneration);

    private async Task<IdentityResponse> Send(IdentityRequest request)
    {
        var generation = Volatile.Read(ref _viewGeneration);
        if (!Current(generation)) throw new OperationCanceledException();
        var result = await _send(request, _lifetime.Token);
        if (!Current(generation)) throw new OperationCanceledException();
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_closed)
        {
            _closed = true; Interlocked.Increment(ref _viewGeneration);
            _idleTimer.Stop(); _idleTimer.Dispose(); _lifetime.Cancel();
            _shutdown = _client.DisposeAsync().AsTask();
            _ = _shutdown.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BuildLogin()
    {
        var form = Stack(_login);
        form.Controls.Add(new Label { AutoSize = true, Text = "Khách đăng nhập bằng email. Nhân sự dùng tên đăng nhập nội bộ." });
        Field(form, "Email / Tên đăng nhập", _identifier); Field(form, "Mật khẩu", _password);
        form.Controls.Add(_staff);
        Button(form, "Đăng nhập", async () =>
        {
            var staff = _staff.Checked;
            var request = new IdentityRequest(staff ? IdentityCommand.StaffLogin : IdentityCommand.CustomerLogin)
            { Identifier = _identifier.Text, Password = _password.Text };
            SignedOut(); // Clear every old principal's data before any authentication await.
            _password.Clear();
            var response = await Send(request);
            if (IsDisposed || Disposing) return;
            _password.Clear(); Show(response);
            if (!response.Success) { SignedOut(); return; }
            _profile.Enabled = !staff; _reviews.Enabled = staff;
            _booking.Enabled = !staff; _bookingReviews.Enabled = staff;
            _idleTimer.Start();
            if (staff) { _tabs.SelectedTab = _reviews; await LoadReviews(); }
            else { _tabs.SelectedTab = _profile; SetProfile(response.Profile!); await RefreshLink(); }
        });
        form.Controls.Add(new Label { AutoSize = true, Text = "Đăng ký khách hàng — email đăng nhập không tự liên kết hồ sơ cũ", Padding = new Padding(0, 24, 0, 8) });
        Field(form, "Họ tên", _regName); Field(form, "Số điện thoại", _regPhone);
        Field(form, "Email đăng nhập", _regEmail); Field(form, "Địa chỉ", _regAddress);
        Field(form, "Mật khẩu mới", _regPassword); Field(form, "Xác nhận mật khẩu", _regConfirm);
        Button(form, "Đăng ký", async () =>
        {
            var response = await Send(new(IdentityCommand.Register)
            { Identifier = _regEmail.Text, Password = _regPassword.Text, Confirmation = _regConfirm.Text,
                Contact = new(_regName.Text, _regPhone.Text, _regEmail.Text, _regAddress.Text) });
            if (IsDisposed || Disposing) return;
            _regPassword.Clear(); _regConfirm.Clear(); Show(response);
            if (response.Success) _identifier.Text = _regEmail.Text;
        });
    }

    private void BuildProfile()
    {
        var form = Stack(_profile); form.Controls.Add(_loginEmail);
        Field(form, "Họ tên", _name); Field(form, "Số điện thoại", _phone); Field(form, "Email liên hệ", _email); Field(form, "Địa chỉ", _address);
        Button(form, "Lưu thông tin liên hệ", async () => Show(await Send(new(IdentityCommand.UpdateMyProfile)
            { Contact = new(_name.Text, _phone.Text, _email.Text, _address.Text) })));
        Button(form, "Tải lại hồ sơ của tôi", async () =>
        {
            var response = await Send(new(IdentityCommand.GetMyProfile)); Show(response);
            if (response.Success) SetProfile(response.Profile!);
        });
        Field(form, "Mật khẩu hiện tại", _current); Field(form, "Mật khẩu mới", _next); Field(form, "Xác nhận", _confirm);
        Button(form, "Đổi mật khẩu và đăng xuất", async () =>
        {
            var response = await Send(new(IdentityCommand.ChangePassword)
                { Password = _current.Text, NewPassword = _next.Text, Confirmation = _confirm.Text });
            if (IsDisposed || Disposing) return;
            _current.Clear(); _next.Clear(); _confirm.Clear(); Show(response);
            if (response.Success) SignedOut();
        });
        Field(form, "Mã hồ sơ / lịch chụp cũ", _reference); Field(form, "Thông tin để nhân viên đối chiếu", _evidence);
        Button(form, "Gửi yêu cầu liên kết hồ sơ cũ", async () =>
        {
            var response = await Send(new(IdentityCommand.RequestLink) { Reference = _reference.Text, Evidence = _evidence.Text });
            Show(response); if (response.Success) await RefreshLink();
        });
        Button(form, "Xem trạng thái liên kết", RefreshLink);
        Button(form, "Xem hồ sơ studio được phép truy cập", async () =>
        {
            var response = await Send(new(IdentityCommand.GetMyLinkedCustomer)); Show(response);
            if (IsDisposed || Disposing) return;
            if (response.LinkedCustomer is { } customer)
                MessageBox.Show(this, $"{customer.Code}\n{customer.FullName}\n{customer.Phone}\n{customer.Email}\n{customer.Address}", "Hồ sơ được liên kết");
        });
    }

    private void BuildReviews()
    {
        var form = Stack(_reviews); form.Controls.Add(_grid);
        form.Controls.Add(new Label { AutoSize = true, Text = "Đối chiếu trực tiếp hồ sơ ở desktop nội bộ trước khi duyệt. Không suy từ email/SĐT." });
        Field(form, "KhachHangId được nhân viên chọn", _customerId); Field(form, "Lý do / bằng chứng đã đối chiếu", _reason);
        Button(form, "Tải yêu cầu mới nhất", async () => { _beforeLink = null; _reviewHistory.Clear(); await LoadReviews(); });
        Button(form, "Trang trước", async () =>
        {
            if (_reviewHistory.TryPop(out var cursor)) { _beforeLink = cursor; await LoadReviews(); }
        });
        Button(form, "Trang tiếp", async () =>
        {
            if (_grid.Rows.Count == 0 || _grid.Rows[_grid.Rows.Count - 1].DataBoundItem is not LinkReviewDto last) return;
            _reviewHistory.Push(_beforeLink); _beforeLink = last.Id; await LoadReviews();
        });
        Button(form, "Duyệt", () => Review(IdentityCommand.ApproveLink));
        Button(form, "Từ chối", () => Review(IdentityCommand.RejectLink));
        Button(form, "Thu hồi", () => Review(IdentityCommand.RevokeLink));
    }

    private async Task Review(IdentityCommand command)
    {
        if (_grid.CurrentRow?.DataBoundItem is not LinkReviewDto selected) { _status.Text = "Chọn một yêu cầu."; return; }
        long? customer = long.TryParse(_customerId.Text, out var id) ? id : null;
        var response = await Send(new(command) { LinkId = selected.Id, CustomerId = customer, Reason = _reason.Text });
        if (IsDisposed || Disposing) return;
        Show(response); if (response.Success) await LoadReviews();
    }
    private async Task LoadReviews()
    {
        var response = await Send(new(IdentityCommand.ListLinkReviews) { BeforeLinkId = _beforeLink });
        if (IsDisposed || Disposing) return;
        Show(response);
        _grid.DataSource = response.Reviews?.ToList();
    }
    private async Task RefreshLink()
    {
        var response = await Send(new(IdentityCommand.GetMyLink));
        if (IsDisposed || Disposing) return;
        Show(response);
        if (response.Link is { } link) _status.Text = $"Liên kết: {link.State} — {link.Reference} — {link.CustomerCode}";
        else if (response.Success) _status.Text = "Chưa có yêu cầu liên kết.";
    }
    private void SetProfile(ProfileDto profile)
    {
        if (IsDisposed || Disposing) return;
        _loginEmail.Text = "Email đăng nhập: " + profile.LoginEmail;
        _name.Text = profile.Contact.FullName; _phone.Text = profile.Contact.Phone; _email.Text = profile.Contact.Email; _address.Text = profile.Contact.Address;
    }
    private void SignedOut()
    {
        Interlocked.Increment(ref _viewGeneration);
        if (_closed || IsDisposed || Disposing) return;
        _idleTimer.Stop();
        _booking.Enabled = false; _bookingReviews.Enabled = false;
        _customerBooking.ResetSession(); _staffBooking.ResetSession();
        _customerBookingLoaded = false; _staffBookingLoaded = false;
        _profile.Enabled = false; _reviews.Enabled = false; _tabs.SelectedTab = _login; _grid.DataSource = null;
        foreach (var box in new[] { _name, _phone, _email, _address, _reference, _evidence, _reason, _customerId, _current, _next, _confirm }) box.Clear();
        _loginEmail.Text = "";
        _beforeLink = null; _reviewHistory.Clear();
    }
    private void Show(IdentityResponse response)
    {
        if (_closed || IsDisposed || Disposing) return;
        _status.Text = response.Message;
        if (IdentityProtocol.RequiresSignIn(response)) SignedOut();
    }
    private async Task Execute(Func<Task> action)
    {
        if (_busy || _closed || IsDisposed || Disposing) return;
        _busy = true;
        Enabled = false;
        try { await action(); }
        catch (OperationCanceledException) { /* A reset/disposed view must not render a stale callback. */ }
        catch { if (!_closed && !IsDisposed && !Disposing) { _status.Text = "Không thể hoàn tất thao tác. Vui lòng đăng nhập lại."; SignedOut(); } }
        finally { _busy = false; if (!_closed && !IsDisposed && !Disposing) Enabled = true; }
    }
    private void Button(FlowLayoutPanel host, string caption, Func<Task> action)
    {
        var button = new Button { Text = caption, Width = 600, Height = 34, Margin = new Padding(4, 6, 4, 6) };
        button.Click += async (_, _) => await Execute(action); host.Controls.Add(button);
    }
    private static TextBox Password() => new() { UseSystemPasswordChar = true, MaxLength = 128 };
    private static FlowLayoutPanel Stack(Control parent)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16) };
        parent.Controls.Add(panel); return panel;
    }
    private static void Field(FlowLayoutPanel host, string label, TextBox value)
    {
        host.Controls.Add(new Label { Text = label, AutoSize = true }); value.Width = 600; host.Controls.Add(value);
    }
}
