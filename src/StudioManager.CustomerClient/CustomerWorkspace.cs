using System.Globalization;
using StudioManager.CustomerApi;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;

namespace StudioManager.CustomerDesktop;

public enum CustomerPage { Home, Packages, Booking, Requests, Payments }

/// <summary>
/// Authenticated customer experience: dashboard, package gallery, booking wizard, request tracking and payments.
/// All data comes from the reviewed CustomerApi protocol via the supplied send delegate; nothing is simulated.
/// </summary>
internal sealed class CustomerWorkspace : UserControl
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    private readonly Func<IdentityRequest, Task<IdentityResponse>> _send;
    private readonly Action<IdentityResponse> _report;
    private readonly Func<Func<Task>, Task> _execute;
    private readonly Dictionary<CustomerPage, PageFlow> _pages = new();
    private readonly Dictionary<string, Image?> _images = new();
    private long _generation;
    private int _navigationTicket;
    private bool _closed, _catalogLoaded, _submitting;
    private string _name = "";
    private List<BookingCatalogItemDto> _packages = [], _services = [], _rooms = [];
    private BookingCatalogItemDto? _package, _room;
    private readonly SortedDictionary<int, decimal> _addons = new();
    private int _step;
    private DateTime _start = Tomorrow();
    private string _notes = "";
    private BookingDraftDto? _draft;
    private BookingQuoteDto? _quote;
    private BookingDetailDto? _result;
    private List<BookingRequestRowDto> _rows = [];
    private string? _filter;
    private long? _openRequest;
    private BookingDetailDto? _detail;
    private bool _hasMoreRequests;

    public event EventHandler<CustomerPage>? PageChanged;
    public CustomerPage Current { get; private set; } = CustomerPage.Home;

    public CustomerWorkspace(Func<IdentityRequest, Task<IdentityResponse>> send, Action<IdentityResponse> report, Func<Func<Task>, Task> execute)
    {
        _send = send; _report = report; _execute = execute;
        Dock = DockStyle.Fill; BackColor = Theme.Background; Font = Theme.Font();
        foreach (var page in Enum.GetValues<CustomerPage>())
        {
            _pages[page] = new PageFlow();
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    // ---- lifecycle -----------------------------------------------------------------------------------------------
    public void SetCustomerName(string name) => _name = name ?? "";

    public void ResetSession()
    {
        Interlocked.Increment(ref _generation);
        Interlocked.Increment(ref _navigationTicket);
        if (_closed || IsDisposed || Disposing) return;
        _name = ""; _package = _room = null; _addons.Clear(); _step = 0; _start = Tomorrow(); _notes = "";
        _draft = null; _quote = null; _result = null; _rows = []; _filter = null; _openRequest = null; _detail = null;
        _packages = []; _services = []; _rooms = []; _catalogLoaded = false; _submitting = false; _hasMoreRequests = false;
        foreach (var page in _pages.Values) page.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closed = true; Interlocked.Increment(ref _generation);
            Interlocked.Increment(ref _navigationTicket);
            foreach (var image in _images.Values) image?.Dispose();
            _images.Clear();
        }
        base.Dispose(disposing);
    }

    public Task InitializeAsync() => NavigateAsync(CustomerPage.Home);

    public async Task NavigateAsync(CustomerPage page, bool refresh = false)
    {
        var ticket = Interlocked.Increment(ref _navigationTicket);
        Show(page);
        var flow = _pages[page];
        if (refresh || flow.Controls.Count == 0) await LoadAsync(page, ticket);
    }

    private void Show(CustomerPage page)
    {
        if (InvokeRequired) { BeginInvoke(() => Show(page)); return; }
        Current = page;
        SuspendLayout();
        try
        {
            var activeFlow = _pages[page];
            for (var i = Controls.Count - 1; i >= 0; i--)
            {
                var c = Controls[i];
                if (c is PageFlow pf && pf != activeFlow)
                {
                    Controls.RemoveAt(i);
                    pf.Visible = false;
                }
            }
            if (!Controls.Contains(activeFlow))
            {
                Controls.Add(activeFlow);
            }
            activeFlow.Visible = true;
            activeFlow.BringToFront();
            activeFlow.Focus();
        }
        finally
        {
            ResumeLayout(true);
        }
        PageChanged?.Invoke(this, page);
    }

    private async Task LoadAsync(CustomerPage page, int ticket)
    {
        var flow = _pages[page];
        if (flow.Controls.Count == 0) { flow.Clear(); flow.Controls.Add(CustomerUi.Text("Đang tải dữ liệu từ studio…", 10, false, Theme.Muted, 8, 8)); }
        switch (page)
        {
            case CustomerPage.Home: await LoadRequests(null, false); break;
            case CustomerPage.Packages: await EnsureCatalog(false); break;
            case CustomerPage.Booking: await EnsureCatalog(false); break;
            case CustomerPage.Requests: _openRequest = null; _detail = null; await LoadRequests(_filter, false); break;
            case CustomerPage.Payments: await LoadRequests("APPROVED", false); break;
        }
        if (ticket != Volatile.Read(ref _navigationTicket) || _closed || IsDisposed) return;
        Render(page);
    }

    private void Render(CustomerPage page)
    {
        if (_closed || IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => Render(page)); return; }
        var flow = _pages[page]; flow.SuspendLayout(); flow.Clear();
        try
        {
            switch (page)
            {
                case CustomerPage.Home: RenderHome(flow); break;
                case CustomerPage.Packages: RenderPackages(flow); break;
                case CustomerPage.Booking: RenderBooking(flow); break;
                case CustomerPage.Requests: RenderRequests(flow); break;
                case CustomerPage.Payments: RenderPayments(flow); break;
            }
        }
        finally { flow.ResumeLayout(true); }
    }

    private async Task<IdentityResponse> Send(IdentityRequest request)
    {
        var generation = Volatile.Read(ref _generation);
        void Require() { if (_closed || IsDisposed || Disposing || generation != Volatile.Read(ref _generation)) throw new OperationCanceledException(); }
        Require();
        var response = await _send(request); Require();
        _report(response); Require();
        return response;
    }

    // ---- data ----------------------------------------------------------------------------------------------------
    private async Task EnsureCatalog(bool force)
    {
        if (_catalogLoaded && !force) return;
        var packages = await LoadAllCatalog(BookingCatalogType.Packages); if (packages is null) return;
        var services = await LoadAllCatalog(BookingCatalogType.Services); if (services is null) return;
        var rooms = await LoadAllCatalog(BookingCatalogType.Rooms); if (rooms is null) return;
        _packages = packages; _services = services; _rooms = rooms; _catalogLoaded = true;
        if (_package is not null) _package = _packages.FirstOrDefault(p => p.Id == _package.Id);
        if (_room is not null) _room = _rooms.FirstOrDefault(r => r.Id == _room.Id);
        foreach (var id in _addons.Keys.Where(id => _services.All(s => s.Id != id)).ToArray()) _addons.Remove(id);
    }

    private async Task<List<BookingCatalogItemDto>?> LoadAllCatalog(BookingCatalogType kind)
    {
        var items = new List<BookingCatalogItemDto>(); int? after = null;
        for (var page = 0; page < 20; page++)
        {
            var response = await Send(new(IdentityCommand.GetBookingCatalog) { BookingCatalogQuery = new(kind, after) });
            if (!response.Success || response.BookingCatalog is null) return null;
            items.AddRange(response.BookingCatalog.Items);
            if (response.BookingCatalog.NextAfterId is not { } next) break;
            after = next;
        }
        return items;
    }

    private async Task LoadRequests(string? state, bool append)
    {
        long? before = append && _rows.Count > 0 ? _rows[^1].Id : null;
        var response = await Send(new(IdentityCommand.GetMyBookingRequests) { BookingFilter = new(state, before) });
        if (!response.Success || IsDisposed) return;
        var page = response.BookingRequests ?? [];
        _rows = append ? [.. _rows, .. page] : [.. page]; _hasMoreRequests = page.Count == 25;
    }

    private async Task OpenRequestAsync(long id)
    {
        var response = await Send(new(IdentityCommand.GetMyBookingRequest) { BookingRequestId = id });
        if (!response.Success || response.BookingDetail is null) return;
        _openRequest = id; _detail = response.BookingDetail; Show(CustomerPage.Requests); Render(CustomerPage.Requests);
    }

    private Image? Cover(string code)
    {
        var key = (code ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(key)) return null;
        if (_images.TryGetValue(key, out var cached)) return cached;
        Image? result = null;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "MediaDefaults", "packages", $"pkg_{key}_cover_ai.jpg");
            if (key.All(char.IsLetterOrDigit) && File.Exists(path))
            {
                using var source = Image.FromFile(path);
                var width = Math.Min(720, source.Width); var height = Math.Max(1, source.Height * width / source.Width);
                var thumb = new Bitmap(width, height);
                using (var g = Graphics.FromImage(thumb)) { g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; g.DrawImage(source, 0, 0, width, height); }
                result = thumb;
            }
        }
        catch { result = null; }
        _images[key] = result; return result;
    }

    // ---- shared builders -----------------------------------------------------------------------------------------
    private Task Run(Func<Task> action) => _execute(action);

    private ModernButton Button(string text, Func<Task> action, bool primary = false, IconKind icon = IconKind.ArrowRight)
    {
        var button = AtelierUi.Action(text, primary, icon); button.Click += async (_, _) => await Run(action); return button;
    }

    private ModernButton Local(string text, Action action, bool primary = false, IconKind icon = IconKind.ArrowRight)
    {
        var button = AtelierUi.Action(text, primary, icon); button.Click += (_, _) => action(); return button;
    }

    private static Label Title(string text, string subtitle, PageFlow flow)
    {
        flow.Controls.Add(CustomerUi.Text(text, 18, true, Theme.Text, 0, 2));
        var sub = CustomerUi.Text(subtitle, 10, false, Theme.Muted, 0, 14); flow.Controls.Add(sub); return sub;
    }

    private Card EmptyState(string title, string message, params ModernButton[] actions)
    {
        var card = new Card { Padding = new Padding(28) };
        card.Add(CustomerUi.Text(title, 13, true, Theme.Text, 0, 6));
        card.Add(CustomerUi.Text(message, 10, false, Theme.TextSecondary, 0, 10));
        var row = CustomerUi.Row(); foreach (var action in actions) row.Controls.Add(action); card.Add(row); return card;
    }

    private static string Money(decimal value) => value.ToString("#,0.####", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
    private static string At(DateTimeOffset value) => value.ToOffset(Vn).ToString("dd/MM/yyyy HH:mm");
    private static DateTime Tomorrow() => DateTimeOffset.UtcNow.ToOffset(Vn).Date.AddDays(1).AddHours(9);

    private Card RequestCard(BookingRequestRowDto row, string action, Func<Task> open)
    {
        var card = new Card();
        var head = CustomerUi.Row(); head.Controls.Add(CustomerUi.Text($"Yêu cầu #{row.Id}", 11.5f, true, Theme.Text, 4, 0));
        head.Controls.Add(CustomerUi.StateChip(row.State)); card.Add(head);
        card.Add(CustomerUi.Text(row.PackageName, 12, true, Theme.Text, 2, 2));
        card.Add(CustomerUi.Text($"Giờ chụp mong muốn: {At(row.StartUtc)}  ·  Gửi lúc {At(row.SubmittedAtUtc)}", 9.5f));
        card.Add(CustomerUi.Text($"Giá dự kiến: {Money(row.Estimate)}" + (string.IsNullOrEmpty(row.BookingCode) ? "" : $"  ·  Lịch chính thức: {row.BookingCode}"), 9.5f, true, Theme.TextSecondary));
        var buttons = CustomerUi.Row(); buttons.Controls.Add(Button(action, open, true)); card.Add(buttons); return card;
    }

    private Control StatusStrip()
    {
        var card = new Card { BackColor = Theme.PrimaryLight, BorderColor = Theme.PrimaryLightBorder };
        card.Add(CustomerUi.Text("Hành trình của một lịch chụp", 10.5f, true, Theme.PrimaryPressed, 0, 4));
        card.Add(CustomerUi.Text("1. Bạn gửi yêu cầu  →  2. Nhân viên studio xem xét  →  3. Được duyệt thành lịch chính thức. " +
            "Yêu cầu mới gửi chỉ ở trạng thái chờ duyệt, chưa giữ chỗ và chưa phải lịch đã xác nhận.", 9.5f, false, Theme.TextSecondary));
        return card;
    }

    // ---- home ----------------------------------------------------------------------------------------------------
    private void RenderHome(PageFlow flow)
    {
        var hero = new Card(true) { Padding = new Padding(30), Margin = new Padding(0, 0, 0, 16) };
        hero.Add(CustomerUi.Text("ATELIER LUMIÈRE", 8.5f, true, Theme.Primitives.Terracotta200, 0, 6));
        hero.Add(CustomerUi.Text(string.IsNullOrWhiteSpace(_name) ? "Xin chào bạn" : $"Xin chào, {_name}", 22, true, Theme.Primitives.Alabaster50, 0, 6));
        hero.Add(CustomerUi.Text("Chọn một gói chụp, đặt khung giờ mong muốn và theo dõi toàn bộ yêu cầu của bạn ngay tại đây.", 10.5f, false, Theme.Primitives.Alabaster200, 0, 14));
        var row = CustomerUi.Row();
        row.Controls.Add(Button("Đặt lịch chụp", () => NavigateAsync(CustomerPage.Booking), true, IconKind.Calendar));
        row.Controls.Add(Button("Khám phá gói chụp", () => NavigateAsync(CustomerPage.Packages), false, IconKind.Camera));
        hero.Add(row); flow.Controls.Add(hero);

        flow.Controls.Add(CustomerUi.Text("Yêu cầu gần đây", 13, true, Theme.Text, 4, 8));
        if (_rows.Count == 0)
            flow.Controls.Add(EmptyState("Bạn chưa có yêu cầu nào", "Hãy xem các gói chụp của studio và gửi yêu cầu đầu tiên. Nhân viên sẽ xem xét và xác nhận lịch cho bạn.",
                Button("Xem gói chụp", () => NavigateAsync(CustomerPage.Packages), true, IconKind.Camera)));
        else
        {
            foreach (var request in _rows.Take(3)) { var id = request.Id; flow.Controls.Add(RequestCard(request, "Xem chi tiết", () => OpenRequestAsync(id))); }
            var more = CustomerUi.Row(); more.Controls.Add(Button("Tất cả yêu cầu của tôi", () => NavigateAsync(CustomerPage.Requests), false, IconKind.History)); flow.Controls.Add(more);
        }
        flow.Controls.Add(StatusStrip());
    }

    // ---- packages ------------------------------------------------------------------------------------------------
    private void RenderPackages(PageFlow flow)
    {
        Title("Gói chụp & dịch vụ", "Giá hiển thị là giá hiện hành của studio; báo giá cuối cùng được hệ thống xác nhận ở bước xem lại.", flow);
        if (!_catalogLoaded) { flow.Controls.Add(EmptyState("Chưa tải được danh mục", "Không thể lấy danh sách gói chụp lúc này. Vui lòng thử lại.", Button("Tải lại", () => NavigateAsync(CustomerPage.Packages), true, IconKind.Refresh))); return; }
        if (_packages.Count == 0) { flow.Controls.Add(EmptyState("Studio chưa mở gói chụp nào", "Hiện chưa có gói chụp đang hoạt động. Vui lòng quay lại sau.")); return; }
        var grid = new FlowLayoutPanel { WrapContents = true, AutoSize = false, BackColor = Color.Empty, Margin = new Padding(0, 0, 0, 8) };
        foreach (var package in _packages) grid.Controls.Add(PackageCard(package));
        flow.Controls.Add(grid);
        flow.Controls.Add(CustomerUi.Text("Dịch vụ bổ sung", 13, true, Theme.Text, 10, 6));
        if (_services.Count == 0) flow.Controls.Add(CustomerUi.Text("Chưa có dịch vụ bổ sung. Bạn vẫn có thể đặt lịch chỉ với gói chụp.", 10));
        foreach (var service in _services)
        {
            var card = new Card { Padding = new Padding(16, 12, 16, 12), Margin = new Padding(0, 0, 0, 10) };
            var head = CustomerUi.Row(); head.Controls.Add(CustomerUi.Text(service.Name, 11, true, Theme.Text, 2, 0));
            head.Controls.Add(new Chip($"{Money(service.Price)} / {service.Unit}", Theme.PrimaryPressed, Theme.PrimaryLight)); card.Add(head);
            if (!string.IsNullOrWhiteSpace(service.Description)) card.Add(CustomerUi.Text(service.Description, 9.5f));
            flow.Controls.Add(card);
        }
        flow.Controls.Add(CustomerUi.Text("Phòng chụp ưu tiên (không bắt buộc)", 13, true, Theme.Text, 10, 4));
        flow.Controls.Add(CustomerUi.Text("Bạn có thể chọn trước phòng chụp mong muốn hoặc để nhân viên studio sắp xếp khi duyệt yêu cầu.", 9.5f, false, Theme.Muted, 0, 8));
        if (_rooms.Count == 0)
            flow.Controls.Add(CustomerUi.Text("Danh sách phòng sẽ được nhân viên studio sắp xếp khi duyệt lịch.", 10));
        else
        {
            foreach (var roomItem in _rooms)
            {
                var r = roomItem;
                var isSelectedRoom = _room?.Id == r.Id;
                var roomCard = new Card { Padding = new Padding(16, 12, 16, 12), Margin = new Padding(0, 0, 0, 10), BorderColor = isSelectedRoom ? Theme.Primary : Theme.Border };
                var head = CustomerUi.Row();
                head.Controls.Add(CustomerUi.Text($"{r.Code} — {r.Name}", 11, true, Theme.Text, 4, 0));
                if (isSelectedRoom) head.Controls.Add(new Chip("Phòng ưu tiên đã chọn", Theme.Success, Theme.SuccessLight));
                roomCard.Add(head);
                if (!string.IsNullOrWhiteSpace(r.Description)) roomCard.Add(CustomerUi.Text(r.Description, 9.5f));
                var roomActions = CustomerUi.Row();
                roomActions.Controls.Add(Local(isSelectedRoom ? "Bỏ phòng ưu tiên" : "Chọn làm phòng ưu tiên", () =>
                {
                    _room = isSelectedRoom ? null : r; _quote = null; _draft = null; Render(CustomerPage.Packages);
                }, !isSelectedRoom, isSelectedRoom ? IconKind.Close : IconKind.Check));
                roomCard.Add(roomActions);
                flow.Controls.Add(roomCard);
            }
        }
        var summaryText = _package is null
            ? $"Chưa chọn gói chụp. Phòng ưu tiên: {_room?.Name ?? "nhân viên chọn khi duyệt"}."
            : $"Đang chọn gói: {_package.Name} ({Money(_package.Price)} · {_package.DurationMinutes} phút). Phòng ưu tiên: {_room?.Name ?? "nhân viên chọn khi duyệt"}.";
        flow.Controls.Add(CustomerUi.Text(summaryText, 10, true, Theme.TextSecondary, 6, 6));
        var go = CustomerUi.Row();
        if (_room is not null) go.Controls.Add(Local("Bỏ phòng ưu tiên", () => { _room = null; _quote = null; _draft = null; Render(CustomerPage.Packages); }, false, IconKind.Close));
        go.Controls.Add(Button("Tiếp tục đặt lịch", () => NavigateAsync(CustomerPage.Booking, false), true, IconKind.Calendar));
        flow.Controls.Add(go);
    }

    private Control PackageCard(BookingCatalogItemDto package)
    {
        var selected = _package?.Id == package.Id;
        var width = CustomerUi.S(this, 320);
        var card = new Card { Width = width, Margin = new Padding(0, 0, 16, 16), BorderColor = selected ? Theme.Primary : Theme.Border };
        var cover = new CoverPanel(Cover(package.Code), package.Name) { Height = CustomerUi.S(this, 170) }; card.Add(cover);
        var title = CustomerUi.Text(package.Name, 12.5f, true, Theme.Text, 0, 2); title.AutoSize = false; title.AutoEllipsis = true; title.Height = title.Font.Height + 6; card.Add(title);
        var price = CustomerUi.Row(false); price.Controls.Add(CustomerUi.Text(Money(package.Price), 12, true, Theme.Primary, 2, 0));
        price.Controls.Add(CustomerUi.Text($"  ·  {package.DurationMinutes} phút", 9.5f, false, Theme.Muted, 5, 0)); card.Add(price);
        var description = CustomerUi.Text(string.IsNullOrWhiteSpace(package.Description) ? "Studio sẽ tư vấn chi tiết khi xác nhận lịch." : package.Description, 9.5f, false, Theme.TextSecondary, 6, 8);
        description.AutoSize = false; description.AutoEllipsis = true; description.Height = description.Font.Height * 3 + 6; card.Add(description);
        var row = CustomerUi.Row();
        row.Controls.Add(Local("Chi tiết", () => AtelierUi.Info(FindForm()!, package.Name, $"{Money(package.Price)} · {package.DurationMinutes} phút\n\n{package.Description}"), false, IconKind.None));
        row.Controls.Add(Local(selected ? "Đã chọn · Tiếp tục" : "Chọn gói này", () => { _package = package; _quote = null; _draft = null; _step = 0; _ = Run(() => NavigateAsync(CustomerPage.Booking, false)); }, !selected, selected ? IconKind.ArrowRight : IconKind.Plus));
        card.Add(row); return card;
    }

    // ---- booking wizard -------------------------------------------------------------------------------------------
    private static readonly string[] Steps = ["Gói chụp", "Dịch vụ thêm", "Lịch & ghi chú", "Xem lại & gửi"];

    private void RenderBooking(PageFlow flow)
    {
        Title("Đặt lịch chụp", "Gửi yêu cầu theo từng bước. Nhân viên studio sẽ duyệt trước khi lịch trở thành chính thức.", flow);
        if (_step == 4 && _result is not null) { RenderResult(flow); return; }
        flow.Controls.Add(new StepBar(Steps) { Step = _step });
        var card = new Card();
        switch (_step)
        {
            case 0: StepPackage(card); break;
            case 1: StepAddons(card); break;
            case 2: StepSchedule(card); break;
            default: StepReview(card); break;
        }
        flow.Controls.Add(card);
        var nav = CustomerUi.Row();
        if (_step > 0) nav.Controls.Add(Local("Quay lại", () => { _step--; Render(CustomerPage.Booking); }, false, IconKind.None));
        if (_step < 3) nav.Controls.Add(Button("Tiếp tục", AdvanceAsync, true));
        else
        {
            var submit = Button(_submitting ? "Đang gửi…" : "Gửi yêu cầu đặt lịch", SubmitAsync, true, IconKind.Calendar);
            submit.Enabled = _quote is not null && !_submitting; nav.Controls.Add(submit);
        }
        flow.Controls.Add(nav);
    }

    private async Task AdvanceAsync()
    {
        if (_step == 0 && _package is null) { AtelierUi.Info(FindForm()!, "Đặt lịch chụp", "Vui lòng chọn một gói chụp trước."); return; }
        if (_step == 2)
        {
            if (_start < DateTimeOffset.UtcNow.ToOffset(Vn).DateTime) { AtelierUi.Info(FindForm()!, "Đặt lịch chụp", "Giờ chụp mong muốn phải ở tương lai."); return; }
            if (!await PreviewAsync()) return;
        }
        _step++; Render(CustomerPage.Booking);
    }

    private void StepPackage(Card card)
    {
        card.Add(CustomerUi.Text("Bước 1 · Chọn gói chụp", 12.5f, true, Theme.Text, 0, 6));
        if (_package is null)
        {
            card.Add(CustomerUi.Text("Bạn chưa chọn gói chụp. Hãy xem các gói của studio để chọn phong cách phù hợp.", 10, false, Theme.TextSecondary, 0, 8));
            var pick = CustomerUi.Row(); pick.Controls.Add(Button("Xem gói chụp", () => NavigateAsync(CustomerPage.Packages, false), true, IconKind.Camera));
            card.Add(pick); return;
        }
        card.Add(new CoverPanel(Cover(_package.Code), _package.Name) { Height = CustomerUi.S(this, 150) });
        card.Add(CustomerUi.Text(_package.Name, 13, true, Theme.Text, 0, 2));
        card.Add(CustomerUi.Text($"{Money(_package.Price)}  ·  {_package.DurationMinutes} phút", 10.5f, true, Theme.Primary, 0, 6));
        if (!string.IsNullOrWhiteSpace(_package.Description)) card.Add(CustomerUi.Text(_package.Description, 9.5f));
        var change = CustomerUi.Row(); change.Controls.Add(Button("Đổi gói khác", () => NavigateAsync(CustomerPage.Packages, false), false, IconKind.Camera)); card.Add(change);
    }

    private void StepAddons(Card card)
    {
        card.Add(CustomerUi.Text("Bước 2 · Dịch vụ bổ sung (tuỳ chọn)", 12.5f, true, Theme.Text, 0, 4));
        card.Add(CustomerUi.Text("Đặt số lượng lớn hơn 0 cho dịch vụ bạn muốn. Tối đa 20 dịch vụ. Tổng tiền do hệ thống tính ở bước xem lại.", 9.5f, false, Theme.Muted, 0, 8));
        if (_services.Count == 0) { card.Add(CustomerUi.Text("Studio chưa có dịch vụ bổ sung. Bạn có thể tiếp tục.", 10)); return; }
        foreach (var service in _services)
        {
            var item = service; var row = CustomerUi.Row(false); row.Margin = new Padding(0, 6, 0, 6);
            var text = CustomerUi.Text($"{item.Name} · {Money(item.Price)} / {item.Unit}", 10, false, Theme.Text, 8, 0); text.Margin = new Padding(0, 8, 14, 0);
            var quantity = new NumericUpDown { Minimum = 0, Maximum = 1000, DecimalPlaces = 2, Width = CustomerUi.S(this, 90), Font = Theme.Font(10), Margin = new Padding(0, 4, 0, 0),
                Value = _addons.TryGetValue(item.Id, out var current) ? current : 0 };
            quantity.ValueChanged += (_, _) =>
            {
                if (quantity.Value == 0) _addons.Remove(item.Id);
                else if (_addons.Count >= 20 && !_addons.ContainsKey(item.Id)) { quantity.Value = 0; AtelierUi.Info(FindForm()!, "Dịch vụ bổ sung", "Tối đa 20 dịch vụ."); }
                else _addons[item.Id] = quantity.Value;
                _quote = null; _draft = null;
            };
            row.Controls.Add(text); row.Controls.Add(quantity); card.Add(row);
        }
    }

    private void StepSchedule(Card card)
    {
        card.Add(CustomerUi.Text("Bước 3 · Lịch mong muốn, phòng ưu tiên & ghi chú", 12.5f, true, Theme.Text, 0, 4));
        card.Add(CustomerUi.Text("Giờ theo múi giờ Việt Nam (+07:00). Đây là mong muốn của bạn; nhân viên sẽ xác nhận phòng chụp và nhiếp ảnh gia khi duyệt.", 9.5f, false, Theme.Muted, 0, 8));
        card.Add(CustomerUi.Text("Ngày giờ chụp mong muốn", 9.5f, true, Theme.Text, 4, 3));
        var floor = DateTimeOffset.UtcNow.ToOffset(Vn).Date;
        var picker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy  HH:mm", Width = CustomerUi.S(this, 220), Font = Theme.Font(10.5f), MinDate = floor,
            Value = _start < floor ? floor.AddHours(9) : _start };
        picker.ValueChanged += (_, _) => { _start = picker.Value; _quote = null; _draft = null; };
        card.Add(picker);

        card.Add(CustomerUi.Text("Phòng chụp ưu tiên (không bắt buộc)", 9.5f, true, Theme.Text, 12, 3));
        card.Add(CustomerUi.Text("Chọn phòng chụp bạn ưu tiên hoặc để nhân viên studio sắp xếp khi duyệt yêu cầu.", 9f, false, Theme.Muted, 0, 4));
        var roomCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = CustomerUi.S(this, 440),
            Font = Theme.Font(10),
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            DisplayMember = nameof(RoomChoice.Label)
        };
        roomCombo.Items.Add(new RoomChoice(null, "Để nhân viên studio chọn khi duyệt (mặc định)"));
        var selectedIdx = 0;
        for (var i = 0; i < _rooms.Count; i++)
        {
            var r = _rooms[i];
            var text = string.IsNullOrWhiteSpace(r.Description) ? $"{r.Code} — {r.Name}" : $"{r.Code} — {r.Name} ({r.Description})";
            roomCombo.Items.Add(new RoomChoice(r, text));
            if (_room?.Id == r.Id) selectedIdx = i + 1;
        }
        roomCombo.SelectedIndex = selectedIdx;
        var roomNote = CustomerUi.Text(_room is null ? "Hiện tại: Nhân viên studio sẽ chọn phòng phù hợp khi duyệt." : $"Hiện tại: Ưu tiên phòng {_room.Code} — {_room.Name}.", 9.2f, false, Theme.TextSecondary, 4, 4);
        var clearRoomBtn = Local("Bỏ phòng ưu tiên", () => { roomCombo.SelectedIndex = 0; }, false, IconKind.Close);
        clearRoomBtn.Enabled = _room is not null;
        roomCombo.SelectedIndexChanged += (_, _) =>
        {
            _room = (roomCombo.SelectedItem as RoomChoice)?.Room;
            _quote = null; _draft = null;
            clearRoomBtn.Enabled = _room is not null;
            roomNote.Text = _room is null ? "Hiện tại: Nhân viên studio sẽ chọn phòng phù hợp khi duyệt." : $"Hiện tại: Ưu tiên phòng {_room.Code} — {_room.Name}.";
        };
        card.Add(roomCombo);
        card.Add(roomNote);
        var roomBtnRow = CustomerUi.Row(); roomBtnRow.Controls.Add(clearRoomBtn); card.Add(roomBtnRow);

        card.Add(CustomerUi.Text("Ghi chú cho studio (không bắt buộc)", 9.5f, true, Theme.Text, 12, 3));
        var notes = new TextBox { Multiline = true, Height = CustomerUi.S(this, 90), MaxLength = 1000, Font = Theme.Font(10), Text = _notes, ScrollBars = ScrollBars.Vertical, Tag = "fill", BorderStyle = BorderStyle.FixedSingle };
        notes.TextChanged += (_, _) => { _notes = notes.Text; _quote = null; _draft = null; };
        card.Add(notes);
    }

    private sealed record RoomChoice(BookingCatalogItemDto? Room, string Label);

    private async Task<bool> PreviewAsync()
    {
        if (_package is null) return false;
        var local = DateTime.SpecifyKind(_start, DateTimeKind.Unspecified);
        var start = new DateTimeOffset(local, Vn).ToUniversalTime();
        start = new DateTimeOffset(start.Ticks - start.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
        var draft = new BookingDraftDto(Guid.NewGuid(), _package.Id, start, _room?.Id, _notes, _addons.Select(a => new BookingAddonSelectionDto(a.Key, a.Value)).ToArray());
        var preview = await Send(new(IdentityCommand.PreviewBookingRequest) { BookingDraft = draft });
        if (!preview.Success || preview.BookingQuote is null) return false;
        _draft = draft; _quote = preview.BookingQuote; return true;
    }

    private void StepReview(Card card)
    {
        card.Add(CustomerUi.Text("Bước 4 · Xem lại báo giá", 12.5f, true, Theme.Text, 0, 6));
        if (_quote is not { } q) { card.Add(CustomerUi.Text("Chưa có báo giá. Quay lại bước trước và tiếp tục để lấy báo giá từ studio.", 10)); return; }
        card.Add(CustomerUi.Text($"{q.PackageCode} — {q.PackageName}", 13, true, Theme.Text, 0, 2));
        card.Add(CustomerUi.Text($"Gói: {Money(q.PackagePrice)}  ·  {q.DurationMinutes} phút", 10));
        card.Add(CustomerUi.Text($"Giờ chụp mong muốn: {At(q.StartUtc)} (giờ Việt Nam)", 10));
        card.Add(CustomerUi.Text("Phòng ưu tiên: " + (q.PreferredRoomName ?? "Nhân viên sẽ chọn khi duyệt"), 10, true, Theme.TextSecondary));
        foreach (var addon in q.Addons) card.Add(CustomerUi.Text($"+ {addon.Name}: {addon.Quantity:0.##} {addon.Unit} × {Money(addon.UnitPrice)}", 10));
        if (!string.IsNullOrWhiteSpace(q.Notes)) card.Add(CustomerUi.Text("Ghi chú: " + q.Notes, 10, false, Theme.TextSecondary, 4, 4));
        card.Add(CustomerUi.Text($"Liên hệ: {q.Contact.FullName} · {q.Contact.Phone} · {q.Contact.Email}", 10, false, Theme.TextSecondary, 4, 4));
        if (!string.IsNullOrWhiteSpace(q.Contact.Address)) card.Add(CustomerUi.Text("Địa chỉ: " + q.Contact.Address, 10, false, Theme.TextSecondary, 0, 4));
        card.Add(CustomerUi.Text("Tổng dự kiến: " + Money(q.Estimate), 14, true, Theme.Primary, 10, 8));
        card.Add(CustomerUi.Text("Đây là yêu cầu chờ duyệt, chưa giữ chỗ. Giá chốt sẽ là giá dự kiến đã lưu khi nhân viên duyệt.", 9.5f, false, Theme.Muted));
    }

    private async Task SubmitAsync()
    {
        if (_submitting || _draft is null || _quote is null) return;
        var roomText = _quote.PreferredRoomName ?? "Nhân viên sẽ chọn khi duyệt";
        if (!AtelierUi.Confirm(FindForm()!, "Xác nhận gửi yêu cầu", $"{_quote.PackageName}\nGiờ chụp: {At(_quote.StartUtc)}\nPhòng ưu tiên: {roomText}\nTổng dự kiến: {Money(_quote.Estimate)}\n\nYêu cầu sẽ chờ nhân viên duyệt, chưa phải lịch chính thức.")) return;
        _submitting = true; Render(CustomerPage.Booking);
        try
        {
            var submitted = await Send(new(IdentityCommand.SubmitBookingRequest) { BookingSubmissionKey = _draft.SubmissionKey });
            if (!submitted.Success || submitted.CreatedRequestId is not { } id) return;
            var detail = await Send(new(IdentityCommand.GetMyBookingRequest) { BookingRequestId = id });
            _result = detail.BookingDetail;
            if (_result is null) { _step = 0; _package = null; _room = null; _addons.Clear(); _quote = null; _draft = null; AtelierUi.Info(FindForm()!, "Đã gửi", $"Yêu cầu #{id} đã được ghi nhận và đang chờ duyệt."); return; }
            _step = 4; _package = null; _room = null; _addons.Clear(); _notes = ""; _quote = null; _draft = null; _start = Tomorrow();
        }
        finally { _submitting = false; if (!_closed && !IsDisposed) Render(CustomerPage.Booking); }
    }

    private void RenderResult(PageFlow flow)
    {
        var detail = _result!; var card = new Card { BorderColor = Theme.SuccessBorder };
        card.Add(CustomerUi.Text("Đã gửi yêu cầu thành công", 15, true, Theme.Success, 0, 6));
        var head = CustomerUi.Row(); head.Controls.Add(CustomerUi.Text($"Mã yêu cầu #{detail.Id}", 12, true, Theme.Text, 4, 0)); head.Controls.Add(CustomerUi.StateChip(detail.State)); card.Add(head);
        card.Add(CustomerUi.Text($"{detail.Quote.PackageName} · {At(detail.Quote.StartUtc)} · dự kiến {Money(detail.Quote.Estimate)}", 10, false, Theme.TextSecondary, 4, 2));
        card.Add(CustomerUi.Text($"Phòng ưu tiên: {detail.Quote.PreferredRoomName ?? "Nhân viên sẽ chọn khi duyệt"}", 10, false, Theme.TextSecondary, 0, 8));
        card.Add(CustomerUi.Text("Yêu cầu của bạn đang chờ nhân viên studio xem xét. Đây CHƯA phải lịch chính thức; bạn sẽ thấy lịch chính thức trong mục Lịch hẹn khi được duyệt.", 10, false, Theme.Text, 0, 8));
        var row = CustomerUi.Row();
        var id = detail.Id;
        row.Controls.Add(Button("Theo dõi yêu cầu", async () => { _result = null; _step = 0; await OpenRequestAsync(id); }, true, IconKind.History));
        row.Controls.Add(Local("Đặt thêm một lịch", () => { _result = null; _step = 0; Render(CustomerPage.Booking); }, false, IconKind.Plus));
        card.Add(row); flow.Controls.Add(card);
    }

    // ---- requests ------------------------------------------------------------------------------------------------
    private void RenderRequests(PageFlow flow)
    {
        if (_detail is { } detail) { RenderDetail(flow, detail); return; }
        Title("Lịch hẹn của tôi", "Theo dõi yêu cầu đặt lịch và lịch chụp chính thức của riêng bạn.", flow);
        var filters = CustomerUi.Row();
        foreach (var (key, text) in new (string? Key, string Text)[] { (null, "Tất cả"), ("PENDING", "Chờ duyệt"), ("APPROVED", "Đã duyệt"), ("REJECTED", "Từ chối"), ("CANCELLED", "Đã rút") })
        {
            var button = AtelierUi.Action(text, _filter == key, IconKind.None); var state = key;
            button.Click += async (_, _) => await Run(async () => { _filter = state; await LoadRequests(state, false); Render(CustomerPage.Requests); });
            filters.Controls.Add(button);
        }
        flow.Controls.Add(filters);
        if (_rows.Count == 0)
        {
            flow.Controls.Add(EmptyState("Chưa có yêu cầu phù hợp", _filter is null ? "Bạn chưa gửi yêu cầu nào. Hãy chọn gói chụp để bắt đầu." : "Không có yêu cầu nào ở trạng thái này.",
                Button("Đặt lịch chụp", () => NavigateAsync(CustomerPage.Booking), true, IconKind.Calendar))); return;
        }
        foreach (var row in _rows) { var id = row.Id; flow.Controls.Add(RequestCard(row, "Xem chi tiết & tiến trình", () => OpenRequestAsync(id))); }
        if (_hasMoreRequests)
        {
            var more = CustomerUi.Row(); more.Controls.Add(Button("Xem thêm yêu cầu cũ hơn", async () => { await LoadRequests(_filter, true); Render(CustomerPage.Requests); }, false, IconKind.History)); flow.Controls.Add(more);
        }
    }

    private void RenderDetail(PageFlow flow, BookingDetailDto d)
    {
        var back = CustomerUi.Row(); back.Controls.Add(Button("← Danh sách yêu cầu", async () => { _detail = null; _openRequest = null; await LoadRequests(_filter, false); Render(CustomerPage.Requests); }, false, IconKind.None)); flow.Controls.Add(back);
        var head = new Card();
        var title = CustomerUi.Row(); title.Controls.Add(CustomerUi.Text($"Yêu cầu #{d.Id}", 16, true, Theme.Text, 2, 0)); title.Controls.Add(CustomerUi.StateChip(d.State)); head.Add(title);
        head.Add(CustomerUi.Text(d.State switch
        {
            "PENDING" => "Đang chờ nhân viên studio duyệt. Chưa phải lịch chính thức.",
            "APPROVED" => $"Đã được duyệt. Lịch chính thức: {d.OfficialBookingCode ?? "đang cập nhật"}.",
            "REJECTED" => "Yêu cầu không được duyệt.",
            "CANCELLED" => "Bạn đã rút yêu cầu này.",
            _ => d.State
        }, 10.5f, false, Theme.TextSecondary, 4, 6));
        if (!string.IsNullOrWhiteSpace(d.Feedback)) head.Add(CustomerUi.Text((d.State == "REJECTED" ? "Lý do từ studio: " : "Phản hồi: ") + d.Feedback, 10.5f, true, d.State == "REJECTED" ? Theme.Danger : Theme.Text, 2, 4));
        flow.Controls.Add(head);

        var quote = new Card();
        quote.Add(CustomerUi.Text($"{d.Quote.PackageCode} — {d.Quote.PackageName}", 13, true, Theme.Text, 0, 4));
        quote.Add(CustomerUi.Text($"Giờ chụp: {At(d.Quote.StartUtc)} (giờ Việt Nam) · {d.Quote.DurationMinutes} phút", 10));
        quote.Add(CustomerUi.Text($"Gói: {Money(d.Quote.PackagePrice)}", 10));
        quote.Add(CustomerUi.Text("Phòng ưu tiên: " + (d.Quote.PreferredRoomName ?? "Nhân viên sẽ chọn khi duyệt"), 10));
        foreach (var a in d.Quote.Addons) quote.Add(CustomerUi.Text($"+ {a.Name}: {a.Quantity:0.##} {a.Unit} × {Money(a.UnitPrice)}", 10));
        if (!string.IsNullOrWhiteSpace(d.Quote.Notes)) quote.Add(CustomerUi.Text("Ghi chú: " + d.Quote.Notes, 10, false, Theme.TextSecondary, 4, 2));
        quote.Add(CustomerUi.Text("Giá dự kiến: " + Money(d.Quote.Estimate), 12, true, Theme.Primary, 6, 2));
        if (d.ConfirmedTotal is { } confirmed) quote.Add(CustomerUi.Text("Giá chốt khi duyệt: " + Money(confirmed), 12, true, Theme.Success, 0, 2));
        flow.Controls.Add(quote);

        var timeline = new Card();
        timeline.Add(CustomerUi.Text("Tiến trình", 12.5f, true, Theme.Text, 0, 6));
        foreach (var ev in d.History.OrderBy(h => h.AtUtc))
        {
            var (text, fore, _) = CustomerUi.StateStyle(ev.State);
            var line = CustomerUi.Text($"●  {text}  ·  {At(ev.AtUtc)}", 10, true, fore, 2, 0); timeline.Add(line);
            if (!string.IsNullOrWhiteSpace(ev.Reason)) timeline.Add(CustomerUi.Text("      " + ev.Reason, 9.5f, false, Theme.TextSecondary, 0, 4));
        }
        flow.Controls.Add(timeline);

        var actions = new Card();
        var buttons = CustomerUi.Row();
        TextBox? reason = null;
        if (d.State == "PENDING")
        {
            actions.Add(CustomerUi.Text("Rút yêu cầu (chỉ áp dụng khi đang chờ duyệt · lý do 5–500 ký tự)", 11, true, Theme.Text, 0, 4));
            reason = new TextBox { MaxLength = 500, Font = Theme.Font(10), PlaceholderText = "Nhập lý do rút yêu cầu (từ 5 đến 500 ký tự)", Tag = "fill", BorderStyle = BorderStyle.FixedSingle }; actions.Add(reason);
        }
        if (d.OfficialBookingId is not null) buttons.Controls.Add(Button("Xem lịch chính thức", () => OpenOfficialAsync(d), true, IconKind.Calendar));
        if (d.State == "APPROVED") buttons.Controls.Add(Button("Thanh toán & biên nhận", () => OpenPaymentsAsync(d.Id), false, IconKind.Wallet));
        if (d.State == "PENDING") buttons.Controls.Add(Button("Rút yêu cầu", () => CancelAsync(d, reason!.Text), false, IconKind.Close));
        buttons.Controls.Add(Button("Tải lại", () => OpenRequestAsync(d.Id), false, IconKind.Refresh));
        actions.Add(buttons); flow.Controls.Add(actions);
    }

    private async Task CancelAsync(BookingDetailDto detail, string reason)
    {
        if (!AtelierUi.Confirm(FindForm()!, "Rút yêu cầu", $"Rút yêu cầu #{detail.Id} đang chờ duyệt?", "Rút yêu cầu")) return;
        var result = await Send(new(IdentityCommand.CancelBookingRequest) { BookingRequestId = detail.Id, Reason = reason });
        if (result.Success) await LoadRequests(_filter, false);
        await OpenRequestAsync(detail.Id);
    }

    private async Task OpenOfficialAsync(BookingDetailDto detail)
    {
        var result = await Send(new(IdentityCommand.GetMyRequestBooking) { BookingRequestId = detail.Id });
        if (!result.Success || result.OfficialBooking is not { } booking) return;
        using var form = new OfficialBookingForm(booking, detail.State); form.ShowDialog(FindForm());
    }

    private Task OpenPaymentsAsync(long requestId)
    {
        using var form = new CustomerPaymentsForm(requestId, Send); form.ShowDialog(FindForm()); return Task.CompletedTask;
    }

    // ---- payments ------------------------------------------------------------------------------------------------
    private void RenderPayments(PageFlow flow)
    {
        Title("Thanh toán & biên lai", "Chỉ xem các khoản thu đã được nhân viên studio ghi nhận cho lịch đã duyệt của bạn.", flow);
        var note = new Card { BackColor = Theme.WarningLight, BorderColor = Theme.WarningBorder };
        note.Add(CustomerUi.Text("DEMO · Thanh toán mô phỏng", 10.5f, true, Theme.Warning, 0, 3));
        note.Add(CustomerUi.Text("Phiên bản này không có cổng thanh toán thật. Tiền mặt và chuyển khoản được nhân viên ghi nhận; bạn không thể tự đánh dấu đã thanh toán.", 9.5f));
        flow.Controls.Add(note);
        if (_rows.Count == 0)
        {
            flow.Controls.Add(EmptyState("Chưa có lịch nào để thanh toán", "Thanh toán và biên lai xuất hiện sau khi yêu cầu của bạn được duyệt thành lịch chính thức.",
                Button("Xem lịch hẹn của tôi", () => NavigateAsync(CustomerPage.Requests), true, IconKind.History))); return;
        }
        foreach (var row in _rows.Where(r => r.State == "APPROVED")) { var id = row.Id; flow.Controls.Add(RequestCard(row, "Số dư, giao dịch & biên lai", () => OpenPaymentsAsync(id))); }
    }
}
