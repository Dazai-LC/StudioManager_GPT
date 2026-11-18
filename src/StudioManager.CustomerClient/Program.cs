using StudioManager.CustomerApi;
using StudioManager.CustomerDesktop;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;

namespace StudioManager.CustomerClient;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        bool registration;
        try
        {
            registration = args.Contains("--register", StringComparer.Ordinal);
            if (args.Count(argument => argument == "--register") > 1) throw new InvalidOperationException();
            LocalDemoClientRuntime.Configure(args.Where(argument => argument != "--register").ToArray(), LocalDemoComponent.CustomerClient);
        }
        catch
        {
            using var error = new StudioManager.WinForms.Controls.AtelierDialog(
                StudioManager.WinForms.Controls.AtelierNotificationKind.Error, "Khởi chạy cổng khách hàng",
                "Không thể khởi chạy cổng khách hàng. Kiểm tra tham số và bản cài. Với local demo, hãy kiểm tra host, --local-demo --runtime và thư mục demo riêng.");
            error.ShowDialog(); Environment.ExitCode = 1; return;
        }
        Application.Run(new IdentityForm(registration));
    }
}

internal sealed partial class IdentityForm : Form
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
    private readonly BookingWorkspaceControl _customerBooking;
    private bool _customerBookingLoaded;
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 84, AutoEllipsis = true };
    private readonly TextBox _identifier = new(), _password = Password(), _name = new(), _phone = new(), _email = new(), _address = new();
    private readonly TextBox _current = Password(), _next = Password(), _confirm = Password();
    private readonly TextBox _reference = new(), _evidence = new(), _customerId = new(), _reason = new();
    private readonly CheckBox _staff = new() { Text = "Nhân sự: chỉ duyệt liên kết hồ sơ (duyệt đặt lịch tại ứng dụng nhân sự)", AutoSize = true };
    private readonly Label _loginEmail = new() { AutoSize = true };
    private readonly DataGridView _grid = new()
    { Dock = DockStyle.Top, Height = 280, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private readonly TextBox _regName = new(), _regPhone = new(), _regEmail = new(), _regAddress = new(), _regPassword = Password(), _regConfirm = Password();
    private long? _beforeLink;
    private readonly Stack<long?> _reviewHistory = new();
    private readonly System.Windows.Forms.Timer _idleTimer = new() { Interval = 1000 };
    private bool _busy;

    internal IdentityForm() : this(null, false) { }
    internal IdentityForm(bool registration) : this(null, registration) { }

    // Test-only composition uses an internal constructor; normal mode uses the SCM-verified client.
    internal IdentityForm(Func<IdentityRequest, CancellationToken, Task<IdentityResponse>>? send) : this(send, false) { }
    private IdentityForm(Func<IdentityRequest, CancellationToken, Task<IdentityResponse>>? send, bool registration)
    {
        _send = send ?? _client.SendAsync;
        Text = "StudioManager — Tài khoản khách hàng";
        ClientSize = new Size(1320, 820); MinimumSize = new Size(1040, 660);
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Font(); BackColor = Theme.Background; StartPosition = FormStartPosition.CenterScreen;
        _customerBooking = new(false, Send, Show, Execute);
        _booking.Controls.Add(_customerBooking);
        BuildShell();
        BuildLogin(); BuildProfile(); BuildReviews();
        SetPages(null); ShowAuth(registration);
        _customerBooking.CustomerSectionChanged += (_, section) => MarkBookingSection(section);
        _tabs.Selected += async (_, _) =>
        {
            if (_tabs.SelectedTab == _booking && _booking.Enabled && !_customerBookingLoaded)
                await Execute(async () => { _customerBookingLoaded = true; await _customerBooking.InitializeAsync(); });
        };
        var logout = Theme.GhostButton("Đăng xuất", IconKind.LogOut);
        logout.Dock = DockStyle.Bottom; logout.Height = 48; logout.ForeColor = Theme.SidebarText;
        logout.Click += async (_, _) => await Execute(async () => { SignedOut(); Show(await Send(new(IdentityCommand.Logout))); });
        _sidebar.Controls.Add(logout); _logout = logout; _logout.Visible = false;
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
            _statusTip.Dispose();
            _shutdown = _client.DisposeAsync().AsTask();
            _ = _shutdown.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            _lifetime.Dispose();
            // Role-specific pages may be detached from the tab control; the form still owns them.
            _profile.Dispose(); _reviews.Dispose(); _booking.Dispose();
        }
        base.Dispose(disposing);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(_identifier.Text) || string.IsNullOrEmpty(_password.Text))
        { Show(new(false, "REQUIRED", "Vui lòng nhập email / tên đăng nhập và mật khẩu.")); return; }
        var staff = _staff.Checked;
        var request = new IdentityRequest(staff ? IdentityCommand.StaffLogin : IdentityCommand.CustomerLogin)
        { Identifier = _identifier.Text.Trim(), Password = _password.Text };
        SignedOut(); // Clear every old principal's data before any authentication await.
        var response = await Send(request);
        if (IsDisposed || Disposing) return;
        _password.Clear(); Show(response);
        if (!response.Success) { SignedOut(); return; }
        SetPages(staff); _idleTimer.Start();
        if (staff) { _accountSummary.Text = request.Identifier + "\nNhân sự · Duyệt liên kết"; _tabs.SelectedTab = _reviews; await LoadReviews(); }
        else { _tabs.SelectedTab = _profile; SetProfile(response.Profile!); await RefreshLink(); }
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(_regName.Text) || string.IsNullOrWhiteSpace(_regPhone.Text) ||
            !System.Net.Mail.MailAddress.TryCreate(_regEmail.Text.Trim(), out var email) ||
            !string.Equals(email.Address, _regEmail.Text.Trim(), StringComparison.OrdinalIgnoreCase))
        { Show(new(false, "INVALID_INPUT", "Vui lòng nhập họ tên, số điện thoại và email hợp lệ.")); return; }
        if (_regPassword.Text != _regConfirm.Text)
        { Show(new(false, "PASSWORD_CONFIRMATION", "Xác nhận mật khẩu chưa khớp.")); return; }
        var identifier = _regEmail.Text.Trim();
        var response = await Send(new(IdentityCommand.Register)
        { Identifier = identifier, Password = _regPassword.Text, Confirmation = _regConfirm.Text,
            Contact = new(_regName.Text, _regPhone.Text, identifier, _regAddress.Text) });
        if (IsDisposed || Disposing) return;
        _regPassword.Clear(); _regConfirm.Clear(); Show(response);
        if (response.Success)
        {
            ShowAuth(false); _identifier.Text = identifier;
            _status.Text = "Đăng ký thành công. Đăng nhập bằng email và mật khẩu vừa tạo để bắt đầu.";
            _status.ForeColor = Theme.Success; _password.Focus();
        }
    }

    private void BuildProfile()
    {
        var form = Stack(_profile); form.Controls.Add(AtelierUi.Copy("Thông tin liên hệ", true)); form.Controls.Add(_loginEmail);
        Field(form, "Họ tên", _name); Field(form, "Số điện thoại", _phone); Field(form, "Email liên hệ", _email); Field(form, "Địa chỉ", _address);
        Button(form, "Lưu thông tin liên hệ", async () => Show(await Send(new(IdentityCommand.UpdateMyProfile)
            { Contact = new(_name.Text, _phone.Text, _email.Text, _address.Text) })));
        Button(form, "Tải lại hồ sơ của tôi", async () =>
        {
            var response = await Send(new(IdentityCommand.GetMyProfile)); Show(response);
            if (response.Success) SetProfile(response.Profile!);
        });
        form.Controls.Add(AtelierUi.Copy("Bảo mật tài khoản", true));
        Field(form, "Mật khẩu hiện tại", _current); Field(form, "Mật khẩu mới", _next); Field(form, "Xác nhận", _confirm);
        Button(form, "Đổi mật khẩu và đăng xuất", async () =>
        {
            var response = await Send(new(IdentityCommand.ChangePassword)
                { Password = _current.Text, NewPassword = _next.Text, Confirmation = _confirm.Text });
            if (IsDisposed || Disposing) return;
            _current.Clear(); _next.Clear(); _confirm.Clear(); Show(response);
            if (response.Success) SignedOut();
        });
        form.Controls.Add(AtelierUi.Copy("Liên kết hồ sơ studio", true));
        form.Controls.Add(AtelierUi.Copy("Hồ sơ cũ chỉ được liên kết khi nhân viên đối chiếu và phê duyệt; hệ thống không tự ghép theo email hay số điện thoại."));
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
                AtelierUi.Info(this, "Hồ sơ được liên kết", $"{customer.Code}\n{customer.FullName}\n{customer.Phone}\n{customer.Email}\n{customer.Address}");
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
        _accountSummary.Text = profile.Contact.FullName + Environment.NewLine + profile.LoginEmail;
        _name.Text = profile.Contact.FullName; _phone.Text = profile.Contact.Phone; _email.Text = profile.Contact.Email; _address.Text = profile.Contact.Address;
    }
    private void SignedOut()
    {
        Interlocked.Increment(ref _viewGeneration);
        if (_closed || IsDisposed || Disposing) return;
        _idleTimer.Stop();
        _customerBooking.ResetSession();
        _customerBookingLoaded = false;
        SetPages(null); _tabs.SelectedTab = _login; _grid.DataSource = null;
        foreach (var box in new[] { _name, _phone, _email, _address, _reference, _evidence, _reason, _customerId, _current, _next, _confirm }) box.Clear();
        foreach (var box in new[] { _password, _regName, _regPhone, _regEmail, _regAddress, _regPassword, _regConfirm }) box.Clear();
        foreach (var box in new[] { _password, _regPassword, _regConfirm, _current, _next, _confirm }) box.UseSystemPasswordChar = true;
        _accountSummary.Text = "Không gian khách hàng";
        _loginEmail.Text = "";
        _beforeLink = null; _reviewHistory.Clear();
    }
    private void SetPages(bool? staff)
    {
        _profile.Enabled = staff == false; _booking.Enabled = staff == false; _reviews.Enabled = staff == true;
        _tabs.SuspendLayout();
        try
        {
            _tabs.TabPages.Clear(); _tabs.TabPages.Add(_login);
            if (staff == true) _tabs.TabPages.Add(_reviews);
            else if (staff == false) _tabs.TabPages.AddRange([_profile, _booking]);
            Text = staff == true ? "StudioManager — Duyệt liên kết hồ sơ (nhân sự)" : "StudioManager — Tài khoản khách hàng";
            RefreshNavigation(staff);
        }
        finally { _tabs.ResumeLayout(); }
    }
    private void Show(IdentityResponse response)
    {
        if (_closed || IsDisposed || Disposing) return;
        _status.Text = response.Message;
        _status.ForeColor = response.Success ? Theme.Success : Theme.Danger;
        if (IdentityProtocol.RequiresSignIn(response)) SignedOut();
    }
    private async Task Execute(Func<Task> action)
    {
        if (_busy || _closed || IsDisposed || Disposing) return;
        _busy = true;
        _tabs.Enabled = false; _navigation.Enabled = false; UseWaitCursor = true;
        _status.ForeColor = Theme.Muted; _status.Text = "Đang xử lý, vui lòng chờ…";
        try { await action(); }
        catch (OperationCanceledException) { /* A reset/disposed view must not render a stale callback. */ }
        catch { if (!_closed && !IsDisposed && !Disposing) { _status.Text = "Không thể hoàn tất thao tác. Vui lòng đăng nhập lại."; SignedOut(); } }
        finally
        {
            _busy = false;
            if (!_closed && !IsDisposed && !Disposing)
            {
                _tabs.Enabled = true; _navigation.Enabled = true; UseWaitCursor = false;
                if (_status.Text == "Đang xử lý, vui lòng chờ…") { _status.Text = "Sẵn sàng."; _status.ForeColor = Theme.TextSecondary; }
            }
        }
    }
    private ModernButton Button(FlowLayoutPanel host, string caption, Func<Task> action, bool primary = false)
    {
        var button = AtelierUi.Action(caption, primary);
        button.Click += async (_, _) => await Execute(action); host.Controls.Add(button);
        return button;
    }
    private static TextBox Password() => new() { UseSystemPasswordChar = true, MaxLength = 128 };
    private static FlowLayoutPanel Stack(Control parent)
    {
        return AtelierUi.Stack(parent);
    }
    private static Panel Field(FlowLayoutPanel host, string label, TextBox value)
    {
        value.Font = Theme.Font(10); value.BorderStyle = BorderStyle.None; value.ForeColor = Theme.Text;
        var field = StudioManager.WinForms.Forms.AtelierInput.CreateVectorInputField(label, value, value.UseSystemPasswordChar ? IconKind.Lock : IconKind.Users,
            value.UseSystemPasswordChar);
        field.Dock = DockStyle.None; field.Height = 76; field.Margin = new Padding(0, 2, 0, 6); host.Controls.Add(field);
        return field;
    }
}
