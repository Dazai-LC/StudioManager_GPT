using System.ComponentModel;
using System.Globalization;
using System.Text;
using StudioManager.CustomerApi;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;

namespace StudioManager.CustomerDesktop;

public enum CustomerWorkspaceSection { Catalog, Compose, Requests, Payments }

/// <summary>Shared native P04 UI. Calls only the reviewed service protocol; no Domain/SQL dependency.</summary>
public sealed class BookingWorkspaceControl : UserControl
{
    private readonly bool _staff;
    private readonly Func<IdentityRequest, Task<IdentityResponse>> _send;
    private readonly Action<IdentityResponse> _report;
    private readonly Func<Func<Task>, Task> _execute;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _catalog = Grid(), _requests = Grid(), _selectedServices = Grid();
    private readonly ComboBox _kind = Combo(), _state = Combo(), _rooms = Combo(), _photographers = Combo();
    private readonly NumericUpDown _quantity = new() { Minimum = 0.01m, Maximum = 1000, Value = 1, DecimalPlaces = 2, Width = 90 };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 190,
        Value = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date.AddDays(1).AddHours(9) };
    private readonly TextBox _notes = new() { Multiline = true, Height = 65, MaxLength = 1000, Width = 600 };
    private readonly TextBox _reason = new() { MaxLength = 500, Width = 600 };
    private readonly TextBox _detailText = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Height = 260, Width = 700 };
    private readonly Label _selection = new() { AutoSize = true, MaximumSize = new Size(740, 0), Text = "Chưa chọn gói / phòng ưu tiên." };
    private readonly Label _catalogSelection = AtelierUi.Copy("Chưa chọn gói chụp.");
    private readonly BindingList<SelectedService> _addons = [];
    private readonly Stack<int?> _catalogHistory = new();
    private readonly Stack<long?> _requestHistory = new();
    private int? _catalogAfter, _catalogNext, _roomAfter, _photographerAfter;
    private long? _beforeRequest;
    private BookingCatalogItemDto? _package, _room;
    private BookingDetailDto? _detail;
    private IReadOnlyList<BookingRequestRowDto> _requestRows = [];
    private long _viewGeneration;
    private bool _closed;
    public Func<long, Task>? OpenInternalBooking { get; set; }
    public event EventHandler<CustomerWorkspaceSection>? CustomerSectionChanged;

    public BookingWorkspaceControl(bool staff, Func<IdentityRequest, Task<IdentityResponse>> send,
        Action<IdentityResponse> report, Func<Func<Task>, Task> execute)
    {
        _staff = staff; _send = send; _report = report; _execute = execute;
        Dock = DockStyle.Fill; Font = new Font("Segoe UI", 10);
        Controls.Add(_tabs);
        if (!staff) BuildCatalog();
        BuildRequests();
        if (!staff)
        {
            Font = Theme.Font(); BackColor = Theme.Background; AtelierUi.HideTabHeaders(_tabs);
            foreach (var grid in new[] { _catalog, _requests, _selectedServices }) AtelierUi.StyleGrid(grid);
            _detailText.Font = Theme.Font(9.3f); _detailText.ForeColor = Theme.TextSecondary;
            _detailText.BackColor = Theme.SurfaceWarm; _detailText.BorderStyle = BorderStyle.FixedSingle;
            _notes.Font = _reason.Font = Theme.Font(10);
            foreach (var combo in new[] { _kind, _state }) { combo.Font = Theme.Font(9.5f); combo.BackColor = Theme.Surface; combo.ForeColor = Theme.Text; }
        }
        _requests.CellDoubleClick += async (_, _) => await _execute(async () => { await LoadDetail(); });
    }

    public async Task InitializeAsync()
    {
        if (_staff) await LoadStaffChoices(true);
        else await LoadCatalog();
        await LoadRequests();
    }

    public async Task NavigateCustomerAsync(CustomerWorkspaceSection section, bool refresh = true)
    {
        if (_staff) throw new InvalidOperationException("Customer presentation is unavailable in the staff workspace.");
        ShowCustomerPane(section);
        if (section is CustomerWorkspaceSection.Requests or CustomerWorkspaceSection.Payments)
        {
            _beforeRequest = null; _requestHistory.Clear();
            _state.SelectedIndex = section == CustomerWorkspaceSection.Payments ? 2 : 0;
            await LoadRequests();
        }
        else if (refresh && section == CustomerWorkspaceSection.Catalog) await LoadCatalog();
    }

    private void ShowCustomerPane(CustomerWorkspaceSection section)
    {
        _tabs.SelectedIndex = section == CustomerWorkspaceSection.Catalog ? 0 : section == CustomerWorkspaceSection.Compose ? 1 : 2;
        CustomerSectionChanged?.Invoke(this, section);
    }

    public void ResetSession()
    {
        Interlocked.Increment(ref _viewGeneration);
        if (_closed || IsDisposed || Disposing) return;
        _package = null; _room = null; _detail = null; _addons.Clear();
        _requestRows = []; _requests.DataSource = null; _catalog.DataSource = null;
        _notes.Clear(); _reason.Clear(); _detailText.Clear(); _rooms.Items.Clear(); _photographers.Items.Clear();
        _catalogAfter = _catalogNext = _roomAfter = _photographerAfter = null;
        _beforeRequest = null; _catalogHistory.Clear(); _requestHistory.Clear(); UpdateSelection();
        if (!_staff)
        {
            _kind.SelectedIndex = 0; _state.SelectedIndex = 0; _quantity.Value = 1;
            _start.Value = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date.AddDays(1).AddHours(9);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _closed = true; Interlocked.Increment(ref _viewGeneration); }
        base.Dispose(disposing);
    }

    private void RequireCurrentView(long generation)
    {
        if (_closed || IsDisposed || Disposing || generation != Volatile.Read(ref _viewGeneration))
            throw new OperationCanceledException("The view session ended.");
    }

    private void BuildCatalog()
    {
        var page = new TabPage("Gói chụp & dịch vụ"); _tabs.TabPages.Add(page);
        var panel = Stack(page);
        panel.Controls.Add(AtelierUi.Copy("Khám phá gói chụp của studio", true));
        panel.Controls.Add(Label("Chọn danh mục để xem gói, dịch vụ bổ sung hoặc phòng. Giá được xác nhận lại khi xem trước."));
        _kind.DisplayMember = nameof(CatalogChoice.Text);
        _kind.Items.AddRange([new CatalogChoice(BookingCatalogType.Packages, "Gói chụp"), new CatalogChoice(BookingCatalogType.Services, "Dịch vụ bổ sung"), new CatalogChoice(BookingCatalogType.Rooms, "Phòng chụp")]);
        _kind.SelectedIndex = 0; panel.Controls.Add(_kind);
        _kind.SelectedIndexChanged += (_, _) => { _catalog.DataSource = null; _catalogAfter = _catalogNext = null; _catalogHistory.Clear(); };
        Buttons(panel, ("Tải danh mục", async () => { _catalogAfter = null; _catalogHistory.Clear(); await LoadCatalog(); }),
            ("Trang trước", async () => { if (_catalogHistory.TryPop(out var cursor)) { _catalogAfter = cursor; await LoadCatalog(); } }),
            ("Trang tiếp", async () => { if (_catalogNext is { } next) { _catalogHistory.Push(_catalogAfter); _catalogAfter = next; await LoadCatalog(); } }));
        panel.Controls.Add(_catalog);
        panel.Controls.Add(Label("Số lượng khi chọn dịch vụ bổ sung"));
        panel.Controls.Add(_quantity);
        Buttons(panel, ("Chọn mục đang xem", SelectCatalogItem), ("Bỏ phòng ưu tiên", () => { _room = null; UpdateSelection(); return Task.CompletedTask; }));
        panel.Controls.Add(_catalogSelection);
        Buttons(panel, ("Tiếp tục đặt lịch", () => { ShowCustomerPane(CustomerWorkspaceSection.Compose); return Task.CompletedTask; }));

        var compose = new TabPage("Đặt lịch & yêu cầu"); _tabs.TabPages.Add(compose); panel = Stack(compose);
        panel.Controls.Add(AtelierUi.Copy("Yêu cầu một buổi chụp", true));
        panel.Controls.Add(_selection);
        _selectedServices.DataSource = _addons; panel.Controls.Add(_selectedServices);
        Buttons(panel, ("Bỏ dịch vụ đang chọn", () =>
        {
            if (_selectedServices.CurrentRow?.DataBoundItem is SelectedService service) _addons.Remove(service);
            return Task.CompletedTask;
        }));
        panel.Controls.Add(Label("Giờ bắt đầu mong muốn (giờ Việt Nam). Thời lượng theo gói. Phòng là ưu tiên; nhân viên sẽ xác nhận khi duyệt."));
        panel.Controls.Add(_start); panel.Controls.Add(Label("Yêu cầu / ghi chú")); panel.Controls.Add(_notes);
        Buttons(panel, ("Xem trước & xác nhận gửi", PreviewAndSubmit));
        Buttons(panel, ("Quay lại danh mục", () => { ShowCustomerPane(CustomerWorkspaceSection.Catalog); return Task.CompletedTask; }));
    }

    private async Task LoadCatalog()
    {
        if (_kind.SelectedItem is not CatalogChoice choice) return;
        var response = await Send(new(IdentityCommand.GetBookingCatalog) { BookingCatalogQuery = new(choice.Kind, _catalogAfter) });
        if (!response.Success || IsDisposed) return;
        _catalog.DataSource = response.BookingCatalog!.Items.ToList();
        _catalogNext = response.BookingCatalog.NextAfterId;
    }
    private Task SelectCatalogItem()
    {
        if (_catalog.CurrentRow?.DataBoundItem is not BookingCatalogItemDto item || _kind.SelectedItem is not CatalogChoice choice)
            return Task.CompletedTask;
        // Selection is local preview state only. All IDs, availability and prices are revalidated by the service.
        if (choice.Kind == BookingCatalogType.Packages) _package = item;
        else if (choice.Kind == BookingCatalogType.Rooms) _room = item;
        else if (_addons.All(a => a.ServiceId != item.Id))
        {
            if (_addons.Count >= 20) { Info("Tối đa 20 dịch vụ."); return Task.CompletedTask; }
            _addons.Add(new(item.Id, item.Name, item.Unit, item.Price, _quantity.Value));
        }
        UpdateSelection(); return Task.CompletedTask;
    }
    private void UpdateSelection()
    {
        _selection.Text = _package is null ? "Chưa chọn gói chụp. Mở danh mục, chọn gói rồi tiếp tục đặt lịch."
            : $"Gói: {_package.Name} — {_package.DurationMinutes} phút — {Money(_package.Price)}. Phòng ưu tiên: {_room?.Name ?? "nhân viên chọn khi duyệt"}.";
        _catalogSelection.Text = _selection.Text;
    }

    private async Task PreviewAndSubmit()
    {
        if (_package is null) { Info("Chọn gói chụp trước."); return; }
        var local = DateTime.SpecifyKind(_start.Value, DateTimeKind.Unspecified);
        var start = new DateTimeOffset(local, TimeSpan.FromHours(7)).ToUniversalTime();
        start = new DateTimeOffset(start.Ticks - start.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
        var draft = new BookingDraftDto(Guid.NewGuid(), _package.Id, start, _room?.Id, _notes.Text,
            _addons.Select(a => new BookingAddonSelectionDto(a.ServiceId, a.Quantity)).ToArray());
        var preview = await Send(new(IdentityCommand.PreviewBookingRequest) { BookingDraft = draft });
        if (!preview.Success || IsDisposed) return;
        if (!AtelierUi.Confirm(this, "Xác nhận yêu cầu", QuoteText(preview.BookingQuote!) + "\n\nĐây là yêu cầu chờ duyệt, chưa giữ chỗ. Xác nhận gửi?")) return;
        var submitted = await Send(new(IdentityCommand.SubmitBookingRequest) { BookingSubmissionKey = draft.SubmissionKey });
        if (!submitted.Success || IsDisposed) return;
        _beforeRequest = null; _requestHistory.Clear();
        ShowCustomerPane(CustomerWorkspaceSection.Requests); await LoadRequests();
        if (submitted.CreatedRequestId is { } id) await LoadDetail(id);
    }

    private void BuildRequests()
    {
        var page = new TabPage(_staff ? "Duyệt & lịch sử xử lý" : "Yêu cầu của tôi"); _tabs.TabPages.Add(page);
        var panel = Stack(page);
        if (!_staff) panel.Controls.Add(AtelierUi.Copy("Theo dõi yêu cầu và lịch chính thức", true));
        _state.Items.AddRange(["Tất cả", "PENDING", "APPROVED", "REJECTED", "CANCELLED"]); _state.SelectedIndex = _staff ? 1 : 0;
        panel.Controls.Add(_state);
        Buttons(panel, ("Tải danh sách", async () => { _beforeRequest = null; _requestHistory.Clear(); await LoadRequests(); }),
            ("Trang trước", async () => { if (_requestHistory.TryPop(out var cursor)) { _beforeRequest = cursor; await LoadRequests(); } }),
            ("Trang tiếp", async () => { if (_requestRows.Count == 25) { _requestHistory.Push(_beforeRequest); _beforeRequest = _requestRows[^1].Id; await LoadRequests(); } }));
        panel.Controls.Add(_requests);
        Buttons(panel, ("Xem chi tiết / lịch sử", async () => { await LoadDetail(); }), ("Mở lịch chính thức", OpenOfficial));
        if(!_staff)Buttons(panel,("Số dư / thanh toán / biên nhận",OpenPayments));
        panel.Controls.Add(Label("PENDING là chờ duyệt lịch, chưa phải đã thanh toán. Chỉ nhân viên ghi nhận khoản thu; khách hàng chỉ xem dữ liệu đã lưu."));
        panel.Controls.Add(_detailText);
        if (_staff)
        {
            panel.Controls.Add(Label("Chọn rõ phòng và nhiếp ảnh gia đang hoạt động. Khi duyệt, giá dự kiến đã lưu sẽ trở thành giá chốt; giá danh mục mới không ghi đè báo giá này."));
            panel.Controls.Add(_rooms); panel.Controls.Add(_photographers);
            Buttons(panel, ("Tải lại phòng & nhân sự", () => LoadStaffChoices(true)),
                ("Thêm trang phòng", async () => { if (_roomAfter.HasValue) await LoadChoicePage(BookingCatalogType.Rooms); }),
                ("Thêm trang nhân sự", async () => { if (_photographerAfter.HasValue) await LoadChoicePage(BookingCatalogType.Photographers); }));
        }
        panel.Controls.Add(Label(_staff ? "Lý do xử lý (5–500 ký tự, khách hàng nhìn thấy)" : "Lý do rút yêu cầu (chỉ áp dụng khi PENDING)"));
        panel.Controls.Add(_reason);
        if (_staff) Buttons(panel, ("Kiểm tra khả dụng", Check), ("Duyệt & tạo lịch", Approve), ("Từ chối", Reject));
        else Buttons(panel, ("Rút yêu cầu PENDING", Cancel));
    }

    private async Task LoadRequests()
    {
        var state = _state.SelectedIndex <= 0 ? null : _state.SelectedItem?.ToString();
        var result = await Send(new(_staff ? IdentityCommand.GetBookingReviewQueue : IdentityCommand.GetMyBookingRequests) { BookingFilter = new(state, _beforeRequest) });
        if (!result.Success || IsDisposed) return;
        _requestRows = result.BookingRequests ?? [];
        _requests.DataSource = _requestRows.Select(r => new RequestView(r.Id, r.State, r.CustomerName, r.PackageName,
            Local(r.StartUtc), Local(r.SubmittedAtUtc), Money(r.Estimate), r.BookingCode ?? "")).ToList();
        _detail = null; _detailText.Clear();
        if (!_staff && _requestRows.Count == 0)
            _detailText.Text = "Chưa có yêu cầu phù hợp. Bạn có thể đổi bộ lọc hoặc chọn gói chụp để gửi yêu cầu mới. Thanh toán và biên nhận chỉ có sau khi lịch được duyệt và nhân viên ghi nhận khoản thu.";
    }
    private async Task<BookingDetailDto?> LoadDetail(long? requestId = null)
    {
        var id = requestId ?? (_requests.CurrentRow?.DataBoundItem as RequestView)?.Id;
        if (id is null) { Info("Chọn một yêu cầu."); return null; }
        var result = await Send(new(_staff ? IdentityCommand.GetBookingReview : IdentityCommand.GetMyBookingRequest) { BookingRequestId = id });
        if (!result.Success || IsDisposed) return null;
        _detail = result.BookingDetail!;
        var history = string.Join(Environment.NewLine, _detail.History.Select(h => $"{Local(h.AtUtc)} — {h.State} — {h.Actor}: {h.Reason}"));
        _detailText.Text = $"Yêu cầu #{_detail.Id}: {_detail.State}\r\nGửi lúc: {Local(_detail.SubmittedAtUtc)}\r\n" +
            QuoteText(_detail.Quote) + $"\r\nPhản hồi: {_detail.Feedback ?? "Chưa xử lý"}\r\n" +
            $"Lịch chính thức: {_detail.OfficialBookingCode ?? "Chưa có"} — Giá chốt khi duyệt: {(_detail.ConfirmedTotal is { } total ? Money(total) : "Chưa chốt")}\r\n\r\n{history}";
        if (!_staff && _detailText.Parent is ScrollableControl viewport) viewport.ScrollControlIntoView(_detailText);
        return _detail;
    }
    private async Task LoadStaffChoices(bool reset)
    {
        if (reset) { _rooms.Items.Clear(); _photographers.Items.Clear(); _roomAfter = _photographerAfter = null; }
        await LoadChoicePage(BookingCatalogType.Rooms); await LoadChoicePage(BookingCatalogType.Photographers);
    }
    private async Task LoadChoicePage(BookingCatalogType kind)
    {
        var isRoom = kind == BookingCatalogType.Rooms;
        var result = await Send(new(IdentityCommand.GetBookingStaffCatalog) { BookingCatalogQuery = new(kind, isRoom ? _roomAfter : _photographerAfter) });
        if (!result.Success || IsDisposed) return;
        var combo = isRoom ? _rooms : _photographers;
        combo.DisplayMember = nameof(Choice.Text);
        foreach (var item in result.BookingCatalog!.Items)
            if (!combo.Items.Cast<Choice>().Any(c => c.Id == item.Id)) combo.Items.Add(new Choice(item.Id, item.Code + " — " + item.Name));
        if (isRoom) _roomAfter = result.BookingCatalog.NextAfterId; else _photographerAfter = result.BookingCatalog.NextAfterId;
    }
    private BookingApprovalDto? Approval(BookingDetailDto detail)
    {
        if (_rooms.SelectedItem is not Choice room || _photographers.SelectedItem is not Choice photographer)
        { Info("Chọn phòng và nhiếp ảnh gia."); return null; }
        return new(photographer.Id, room.Id, detail.Quote.Estimate);
    }
    private async Task Check()
    {
        var detail = await LoadDetail(); if (detail is null) return;
        var approval = Approval(detail); if (approval is null) return;
        await Send(new(IdentityCommand.CheckBookingAvailability) { BookingRequestId = detail.Id, BookingApproval = approval });
    }
    private async Task Approve()
    {
        var detail = await LoadDetail(); if (detail is null) return;
        var approval = Approval(detail); if (approval is null) return;
        if (MessageBox.Show(this, $"Duyệt yêu cầu #{detail.Id}?\nGiá chốt: {Money(detail.Quote.Estimate)}\nGiờ Việt Nam: {Local(detail.Quote.StartUtc)}\nPhòng: {_rooms.Text}\nNhiếp ảnh gia: {_photographers.Text}\nLý do gửi khách: {_reason.Text}",
            "Xác nhận tạo lịch chính thức", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var result = await Send(new(IdentityCommand.ApproveBookingRequest) { BookingRequestId = detail.Id, BookingApproval = approval, Reason = _reason.Text });
        if (result.Success) { await LoadRequests(); await LoadDetail(detail.Id); }
    }
    private async Task Reject()
    {
        var detail = await LoadDetail(); if (detail is null) return;
        if (MessageBox.Show(this, $"Từ chối yêu cầu #{detail.Id}?\nLý do gửi khách: {_reason.Text}", "Xác nhận từ chối",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var result = await Send(new(IdentityCommand.RejectBookingRequest) { BookingRequestId = detail.Id, Reason = _reason.Text });
        if (result.Success) { await LoadRequests(); await LoadDetail(detail.Id); }
    }
    private async Task Cancel()
    {
        var detail = await LoadDetail(); if (detail is null) return;
        if (!AtelierUi.Confirm(this, "Xác nhận rút yêu cầu", $"Rút yêu cầu #{detail.Id} đang chờ?\nLý do gửi khách: {_reason.Text}", "Rút yêu cầu")) return;
        var result = await Send(new(IdentityCommand.CancelBookingRequest) { BookingRequestId = detail.Id, Reason = _reason.Text });
        if (result.Success) { await LoadRequests(); await LoadDetail(detail.Id); }
    }
    private async Task OpenOfficial()
    {
        var detail = await LoadDetail(); if (detail is null) return;
        var result = await Send(new(_staff ? IdentityCommand.GetReviewedRequestBooking : IdentityCommand.GetMyRequestBooking) { BookingRequestId = detail.Id });
        if (!result.Success || IsDisposed || result.OfficialBooking is not { } booking) return;
        if (_staff && OpenInternalBooking is { } open) { await open(booking.Id); return; }
        if (!_staff)
        {
            using var official = new OfficialBookingForm(booking, detail.State); official.ShowDialog(this); return;
        }
        using var dialog = new Form { Text = "Lịch chính thức — " + booking.Code, ClientSize = new Size(650, 380), StartPosition = FormStartPosition.CenterParent };
        dialog.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Text = $"Mã lịch: {booking.Code}\r\nTrạng thái lịch (riêng với yêu cầu): {booking.Status}\r\n{Local(booking.StartUtc)} → {Local(booking.EndUtc)}\r\n" +
                $"Gói: {booking.PackageName} — {Money(booking.PackagePrice)}\r\nDịch vụ: {Money(booking.ServicesTotal)}\r\nTổng hiện tại: {Money(booking.Total)}\r\nPhòng: {booking.Room}\r\nNhiếp ảnh gia: {booking.Photographer}" });
        dialog.ShowDialog(this);
    }
    private async Task<IdentityResponse> Send(IdentityRequest request)
    {
        var generation = Volatile.Read(ref _viewGeneration);
        RequireCurrentView(generation);
        var response = await _send(request);
        RequireCurrentView(generation);
        _report(response);
        // Reporting an expired/invalid session can synchronously clear this view.
        RequireCurrentView(generation);
        return response;
    }
    private async Task OpenPayments()
    {
        var detail=await LoadDetail();if(detail is null)return;
        if(detail.State!="APPROVED"){Info("Yêu cầu chưa được duyệt thành lịch chính thức nên chưa có số dư để xem.");return;}
        using var form=new CustomerPaymentsForm(detail.Id,Send);form.ShowDialog(this);
    }
    private static string QuoteText(BookingQuoteDto q)
    {
        var text = new StringBuilder($"{q.PackageCode} — {q.PackageName}\r\nGói: {Money(q.PackagePrice)}; {q.DurationMinutes} phút\r\n" +
            $"Giờ Việt Nam: {Local(q.StartUtc)}\r\nPhòng ưu tiên: {q.PreferredRoomName ?? "Nhân viên sẽ chọn"}\r\n" +
            $"Liên hệ: {q.Contact.FullName} — {q.Contact.Phone} — {q.Contact.Email}\r\nĐịa chỉ: {q.Contact.Address}\r\n");
        foreach (var addon in q.Addons) text.AppendLine($"{addon.Name}: {addon.Quantity} {addon.Unit} × {Money(addon.UnitPrice)}");
        return text.AppendLine($"Tổng dự kiến: {Money(q.Estimate)}\r\nGhi chú: {q.Notes}").ToString();
    }
    private void Info(string message)
    {
        if (_staff) MessageBox.Show(this, message, "Đặt lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else AtelierUi.Info(this, "Đặt lịch & yêu cầu", message);
    }
    private static string Money(decimal value) => value.ToString("#,0.####", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
    private static string Local(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss");
    private static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 480 };
    private static Label Label(string value) => new() { Text = value, AutoSize = true, MaximumSize = new Size(740, 0), Margin = new Padding(3, 10, 3, 5) };
    private static DataGridView Grid() => new() { Width = 740, Height = 190, ReadOnly = true, AutoGenerateColumns = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private FlowLayoutPanel Stack(Control owner)
    {
        if (!_staff) return AtelierUi.Stack(owner);
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10) };
        owner.Controls.Add(panel); return panel;
    }
    private void Buttons(Control parent, params (string Text, Func<Task> Action)[] commands)
    {
        var row = new FlowLayoutPanel { AutoSize = true, Width = 740, WrapContents = true };
        foreach (var (text, action) in commands)
        {
            Control button = _staff ? new Button { Text = text, AutoSize = true, Height = 34, Margin = new Padding(3) }
                : AtelierUi.Action(text, text == "Xem trước & xác nhận gửi");
            button.Click += async (_, _) => await _execute(action); row.Controls.Add(button);
        }
        parent.Controls.Add(row);
    }
    private sealed record CatalogChoice(BookingCatalogType Kind, string Text);
    private sealed record Choice(int Id, string Text);
    private sealed record SelectedService([property: Browsable(false)] int ServiceId, string Name, string Unit, decimal UnitPrice, decimal Quantity);
    private sealed record RequestView(long Id, string State, string Customer, string Package, string VietnamTime, string SubmittedVietnam, string Estimate, string OfficialBooking);
}

internal sealed class OfficialBookingForm : Form
{
    internal OfficialBookingForm(OfficialBookingDto booking, string requestState)
    {
        Text = "Lịch chụp chính thức · Atelier Lumière"; ClientSize = new Size(860, 620); MinimumSize = new Size(740, 520);
        StartPosition = FormStartPosition.CenterParent;
        var layout = PaymentUi.Layout(this, "Lịch chụp " + booking.Code, "Thông tin đã được studio xác nhận · Chỉ xem");
        var summary = new Label { Text = $"Yêu cầu: {requestState}  ·  Trạng thái lịch: {ReceiptText.BookingStatus(booking.Status)}" };
        layout.Controls.Add(PaymentUi.Summary(summary), 0, 1);
        var text = new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None,
            Font = Theme.Font(10), BackColor = Theme.Surface, ForeColor = Theme.Text, Padding = new Padding(16),
            Text = $"Mã lịch: {booking.Code}\r\nGiờ Việt Nam: {booking.StartUtc.ToOffset(TimeSpan.FromHours(7)):dd/MM/yyyy HH:mm} → {booking.EndUtc.ToOffset(TimeSpan.FromHours(7)):dd/MM/yyyy HH:mm}\r\n\r\n" +
                $"Gói: {booking.PackageName}\r\nGiá gói đã chốt: {booking.PackagePrice.ToString("#,0.####", CultureInfo.GetCultureInfo("vi-VN"))} đ\r\nDịch vụ: {booking.ServicesTotal.ToString("#,0.####", CultureInfo.GetCultureInfo("vi-VN"))} đ\r\nTổng hiện tại: {booking.Total.ToString("#,0.####", CultureInfo.GetCultureInfo("vi-VN"))} đ\r\n\r\nPhòng: {booking.Room}\r\nNhiếp ảnh gia: {booking.Photographer}" };
        layout.Controls.Add(PaymentUi.Data(text), 0, 2);
        var actions = PaymentUi.Actions(); var close = PaymentUi.Action(actions, "Đóng", IconKind.Close); close.Click += (_, _) => Close();
        layout.Controls.Add(actions, 0, 3); CancelButton = close;
    }
}

internal sealed class CustomerPaymentsForm : Form
{
    private readonly long _request;
    private readonly Func<IdentityRequest,Task<IdentityResponse>> _send;
    private readonly Label _summary=new(){Text="Đang tải thông tin thanh toán của lịch…"};
    private readonly DataGridView _grid=Theme.Grid();
    private readonly FlowLayoutPanel _actions=PaymentUi.Actions();
    private readonly ModernButton _older,_receipt;
    private IReadOnlyList<PaymentRowDto> _rows=[];
    private long? _next;
    private bool _busy;
    public CustomerPaymentsForm(long request,Func<IdentityRequest,Task<IdentityResponse>> send)
    {
        _request=request;_send=send;Text="Thanh toán đã ghi nhận — chỉ xem • StudioManager";ClientSize=new Size(1000,640);MinimumSize=new Size(800,540);StartPosition=FormStartPosition.CenterParent;
        var layout=PaymentUi.Layout(this,"Thanh toán của tôi","Theo dõi khoản thu đã xác nhận · Chỉ xem các lịch thuộc quyền của bạn");
        _grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells;
        ModernButton Add(string text,IconKind icon,Func<Task> action,bool primary=false)
        {var b=PaymentUi.Action(_actions,text,icon,primary);b.Click+=async(_,_)=>await Run(action);return b;}
        Add("Tải lại mới nhất",IconKind.Refresh,()=>LoadData(null));_older=Add("Giao dịch cũ hơn",IconKind.History,()=>LoadData(_next));_receipt=Add("Xem / in biên nhận",IconKind.Printer,Receipt,true);_receipt.Enabled=false;
        layout.Controls.Add(PaymentUi.Summary(_summary),0,1);layout.Controls.Add(PaymentUi.Data(_grid),0,2);layout.Controls.Add(_actions,0,3);
        Shown+=async(_,_)=>await Run(()=>LoadData(null));
        _grid.SelectionChanged+=(_,_)=>UpdateReceiptAction();
    }
    private async Task Run(Func<Task> work)
    {
        if(_busy || IsDisposed)return;_busy=true;_actions.Enabled=false;_grid.Enabled=false;
        try{await work();}
        catch(OperationCanceledException){Close();}
        catch{if(!IsDisposed){_rows=[];_grid.DataSource=null;_summary.Text="Không tải được dữ liệu; đăng nhập lại và tải mới. Không có thao tác thu tiền từ màn hình này.";}}
        finally{_busy=false;if(!IsDisposed){_actions.Enabled=true;_grid.Enabled=true;_older.Enabled=_next.HasValue;UpdateReceiptAction();}}
    }
    private async Task LoadData(long? before)
    {
        var response=await _send(new(IdentityCommand.GetMyBookingPayments){BookingRequestId=_request,BeforePaymentId=before});
        if(IsDisposed)return;
        if(!response.Success || response.Payments is not { } value){_rows=[];_grid.DataSource=null;_summary.Text=response.Message;_next=null;return;}
        _next=value.NextBeforeId;_rows=value.Items;
        _summary.Text=$"Lịch {value.BookingCode} · {ReceiptText.BookingStatus(value.Status)}\r\nTổng: {ReceiptText.Money(value.Total)} | Đã thu mô phỏng: {ReceiptText.Money(value.Collected)} | Đã hoàn: {ReceiptText.Money(value.Refunded)}\r\nCòn lại: {ReceiptText.Money(value.Remaining)}\r\nChỉ xem các khoản thu nhân viên đã xác nhận; khách không thể tự đánh dấu đã thanh toán.";
        _grid.DataSource=_rows.Select(p=>new{p.Id,Ma=p.Code,MucDich=ReceiptText.Purpose(p.Purpose),SoTien=ReceiptText.Money(p.Amount),PhuongThuc=ReceiptText.Method(p.Method),ThoiDiem=p.PaidAt.ToString("dd/MM/yyyy HH:mm:ss"),BienNhan=p.CanViewReceipt?"Có":"Lịch sử không đủ thông tin sở hữu"}).ToList();
        if(_grid.Columns.Contains("Id"))_grid.Columns["Id"].Visible=false;
        foreach(var(key,title) in new[]{("Ma","Mã giao dịch"),("MucDich","Mục đích thu"),("SoTien","Số tiền"),("PhuongThuc","Phương thức"),("ThoiDiem","Thời điểm thu"),("BienNhan","Biên nhận")})
            if(_grid.Columns.Contains(key))_grid.Columns[key].HeaderText=title;
    }
    private async Task Receipt()
    {
        if(_grid.CurrentRow?.Cells["Id"].Value is not long id)return;
        if(!_rows.Any(p=>p.Id==id && p.CanViewReceipt)){PaymentUi.Warn(this,"Biên nhận cũ không đủ thông tin để cấp quyền khách hàng. Liên hệ nhân viên.");return;}
        var result=await _send(new(IdentityCommand.GetMyBookingReceipt){BookingRequestId=_request,PaymentId=id});
        if(IsDisposed)return;
        if(!result.Success || result.Receipt is not { } receipt){_rows=[];_grid.DataSource=null;_next=null;_summary.Text=result.Message;PaymentUi.Warn(this,result.Message);return;}
        using var preview=new PersistedReceiptForm(receipt);preview.ShowDialog(this);
    }
    private void UpdateReceiptAction()=>_receipt.Enabled=!_busy && _grid.CurrentRow?.Cells["Id"].Value is long id && _rows.Any(p=>p.Id==id && p.CanViewReceipt);
}
