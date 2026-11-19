using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Pages;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StudioManager.WinForms.Tests;

// Real STA controls and real Application services over a synthetic, in-memory repository only.
// No Program.Main, Local Demo bootstrap, CustomerPipeClient, SQL connection or Windows Service is used.
internal static class BookingUi
{
    internal static void Run(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            try { test(); }
            catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
            finally
            {
                foreach (var form in System.Windows.Forms.Application.OpenForms.Cast<Form>().ToArray()) form.Dispose();
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(25)), "Offline booking UI test timed out.");
        failure?.Throw();
    }

    internal static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    internal static object? Invoke(object target, string name, params object?[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
    internal static Task InvokeTask(object target, string name, params object?[] args) => (Task)Invoke(target, name, args)!;
    internal static void Await(Task task)
    {
        var timer = Stopwatch.StartNew();
        while (!task.IsCompleted)
        {
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), "Offline UI operation timed out.");
            System.Windows.Forms.Application.DoEvents();
            Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
        System.Windows.Forms.Application.DoEvents();
    }

    internal static Form PaymentForm(AppFacade app, long id) => (Form)Activator.CreateInstance(
        typeof(BookingsPage).Assembly.GetType("StudioManager.WinForms.Pages.PaymentWorkspaceForm")!, app, id)!;
}

internal sealed class BookingUiFixture : IDisposable
{
    internal const long A = 9_000_000_001, B = 9_000_000_002;
    internal BookingRepository Repository { get; }
    internal AppFacade App { get; }
    internal BookingsPage Page { get; }
    internal DataGridView Grid => BookingUi.Field<DataGridView>(Page, "_grid");
    internal List<AtelierDialogRequest> Notifications { get; } = [];
    internal Func<AtelierDialogRequest, DialogResult>? Respond { get; set; }
    private readonly Form _host = new() { Opacity = 0, ShowInTaskbar = false, ClientSize = new Size(1280, 850) };
    private readonly PropertyInfo _interceptor = typeof(AtelierNotificationService).GetProperty(
        "TestDialogInterceptor", BindingFlags.Static | BindingFlags.NonPublic)!;
    private ExceptionDispatchInfo? _assertion;

    internal BookingUiFixture()
    {
        var proxy = DispatchProxy.Create<IStudioRepository, BookingRepository>();
        Repository = (BookingRepository)(object)proxy;
        App = new AppFacade(proxy, new FakeHasher(), new FakeClock());
        BookingUi.Await(App.Auth.LoginAsync("fixture", "Fixture1"));
        _interceptor.SetValue(null, new Func<AtelierDialogRequest, DialogResult?>(request =>
        {
            Notifications.Add(request);
            try { return Respond?.Invoke(request) ?? (request.Kind == AtelierNotificationKind.Confirmation ? DialogResult.Cancel : DialogResult.OK); }
            catch (Exception error) { _assertion = ExceptionDispatchInfo.Capture(error); return DialogResult.Cancel; }
        }));
        Page = new BookingsPage(App) { Dock = DockStyle.Fill };
        _host.Controls.Add(Page);
        _host.Show();
        System.Windows.Forms.Application.DoEvents();
        Assert.Equal(2, Grid.Rows.Count);
    }

    internal LichChup? Selected => (LichChup?)BookingUi.Invoke(Page, "Selected");
    internal void Reload() => BookingUi.Await(BookingUi.InvokeTask(Page, "LoadAsync", true));
    internal void Click(long id)
    {
        var row = Grid.Rows.Cast<DataGridViewRow>().Single(row => (long)row.Cells["Id"].Value == id);
        var column = Grid.Columns.Cast<DataGridViewColumn>().First(column => column.Visible);
        Grid.CurrentCell = row.Cells[column.Index];
        BookingUi.Invoke(Grid, "OnCellClick", new DataGridViewCellEventArgs(column.Index, row.Index));
    }
    internal void AssertNoTarget()
    {
        Assert.Null(Selected);
        Assert.Empty(Grid.SelectedRows.Cast<DataGridViewRow>());
        Assert.Equal("Chưa chọn lịch chụp", BookingUi.Field<Label>(Page, "_inspCode").Text);
        foreach (var name in new[] { "_cancel", "_refund", "_pay", "_edit", "_progress", "_detail" })
            Assert.False(BookingUi.Field<ModernButton>(Page, name).Enabled, name + " must require a booking selection.");
    }

    internal static void AssertTarget(AtelierDialogRequest request, LichChup booking)
    {
        Assert.Contains(booking.KhachHang, request.Message);
        Assert.Contains(booking.Ma, request.Message);
        Assert.Contains(booking.BatDau.ToString("dd/MM/yyyy HH:mm"), request.Message);
        Assert.Contains(booking.KetThuc.ToString("dd/MM/yyyy HH:mm"), request.Message);
        Assert.Contains(booking.TrangThai.HienThi(), request.Message);
    }

    public void Dispose()
    {
        _host.Dispose();
        _interceptor.SetValue(null, null);
        Assert.Empty(Repository.UnexpectedCalls);
        _assertion?.Throw();
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string value) => value;
        public bool Verify(string value, string hash) => value == hash;
    }
    private sealed class FakeClock : IClock { public DateTime Now => DateTime.Now; }
}

public class BookingRepository : DispatchProxy
{
    public readonly Dictionary<long, LichChup> Bookings = new()
    {
        [BookingUiFixture.A] = Booking(BookingUiFixture.A, "TEST-A", "Khách A thử nghiệm"),
        [BookingUiFixture.B] = Booking(BookingUiFixture.B, "TEST-B", "Khách B thử nghiệm")
    };
    public IReadOnlyList<LichChup> Rows => Order.Where(Bookings.ContainsKey).Select(id => Bookings[id]).ToArray();
    public long[] Order = [BookingUiFixture.A, BookingUiFixture.B];
    public Func<BookingSearchFilter, Task<IReadOnlyList<LichChup>>>? Search;
    public Func<long, Task<LichChup?>>? Read;
    public Func<long, BookingPaymentSummary?>? Balance;
    public readonly List<(long Id, string Reason)> Cancellations = [];
    public readonly List<RefundInput> Refunds = [];
    public readonly List<PaymentInput> Payments = [];
    public readonly List<(long Id, TrangThaiLich Next)> Advances = [];
    public readonly List<string> UnexpectedCalls = [];
    public int BookingReads;
    private readonly TaiKhoan _account = new(1, null, "fixture", "Fixture1", VaiTro.QuanTriVien, true, false, "Offline fixture");

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        switch (method!.Name)
        {
            case "FindAccountAsync": case "GetAccountByIdAsync": return Task.FromResult<TaiKhoan?>(_account);
            case "UpdateLastLoginAsync": return Task.CompletedTask;
            case "GetLookupsAsync": return Task.FromResult<IReadOnlyList<LookupItem>>([]);
            case "SearchBookingsAsync": return Search?.Invoke((BookingSearchFilter)args![0]!) ?? Task.FromResult(Rows);
            case "GetBookingAsync":
                BookingReads++;
                return Read?.Invoke((long)args![0]!) ?? Task.FromResult(Bookings.GetValueOrDefault((long)args![0]!));
            case "CancelBookingAsync":
                Cancellations.Add(((long)args![0]!, (string)args[1]!));
                return Task.FromResult(Result.Fail("OFFLINE", "Synthetic cancellation target recorded; no persistence."));
            case "AddRefundAsync":
                Refunds.Add((RefundInput)args![0]!);
                return Task.FromResult(Result.Fail("OFFLINE", "Synthetic refund target recorded; no persistence."));
            case "UpdateBookingStatusAsync":
                Advances.Add(((long)args![0]!, (TrangThaiLich)args[1]!));
                return Task.FromResult(Result.Fail("OFFLINE", "Synthetic status target recorded; no persistence."));
            case "PostPaymentAsync":
                Payments.Add((PaymentInput)args![0]!);
                return Task.FromResult(Result<PaymentReceipt>.Fail("OFFLINE", "Synthetic collection target recorded; no persistence."));
            case "GetPaymentSummaryAsync":
                var id = (long)args![0]!;
                return Task.FromResult(Balance is not null ? Balance(id) : Bookings.TryGetValue(id, out var booking)
                    ? new BookingPaymentSummary(id, booking.Ma, booking.TrangThai.ToString(), booking.TongThanhToan,
                        booking.DaThu, booking.DaHoan, booking.ConLai, [], null) : null);
            default: UnexpectedCalls.Add(method.Name); throw new InvalidOperationException("Unexpected fake repository call: " + method.Name);
        }
    }

    private static LichChup Booking(long id, string code, string customer) => new(id, code, id + 100, customer, 1,
        "Gói chụp thử nghiệm", 100, new DateTime(2030, 5, 6, 9, 30, 0), new DateTime(2030, 5, 6, 11, 0, 0),
        1, "Nhiếp ảnh gia thử nghiệm", 1, "Phòng thử nghiệm", TrangThaiLich.DaDatLich, 0, null, null);
}

internal sealed class BookingDialogDriver : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 10 };
    private readonly HashSet<Form> _handled = [];
    private ExceptionDispatchInfo? _failure;
    internal int Handled => _handled.Count;
    internal BookingDialogDriver(string typeName, Action<Form> respond)
    {
        var elapsed = Stopwatch.StartNew();
        _timer.Tick += (_, _) =>
        {
            var dialog = System.Windows.Forms.Application.OpenForms.Cast<Form>().FirstOrDefault(form => form.GetType().Name == typeName);
            if (dialog is null) return;
            if (elapsed.Elapsed > TimeSpan.FromSeconds(8)) { dialog.Close(); _failure ??= ExceptionDispatchInfo.Capture(new TimeoutException("Offline dialog timed out.")); return; }
            if (!_handled.Add(dialog)) return;
            dialog.Opacity = 0;
            try { respond(dialog); }
            catch (Exception error) { _failure = ExceptionDispatchInfo.Capture(error); dialog.Close(); }
        };
        _timer.Start();
    }
    public void Dispose() { _timer.Stop(); _timer.Dispose(); _failure?.Throw(); }
}
