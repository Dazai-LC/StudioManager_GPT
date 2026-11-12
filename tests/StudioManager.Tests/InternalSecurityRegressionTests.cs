using System.Reflection;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

// These exercise Application behavior with a stateful repository double, not SQL locking.
public sealed class InternalSecurityRegressionTests
{
    private const string Old = "ExistingPassword1";
    private const string New = "ReplacementPassword2";

    private static (RepositoryDouble Store, AppFacade App, MutableClock Clock) Setup(bool forced = false, VaiTro role = VaiTro.QuanTriVien)
    {
        var repository = DispatchProxy.Create<IStudioRepository, RepositoryDouble>();
        var store = (RepositoryDouble)repository;
        store.Accounts[7] = new(7, null, "actor", TestHasher.Encode(Old), role, true, forced, "Actor");
        store.Accounts[8] = new(8, null, "other", TestHasher.Encode(Old), VaiTro.NhanVien, true, false, "Other");
        var clock = new MutableClock();
        return (store, new(repository, new TestHasher(), clock), clock);
    }

    [Theory]
    [InlineData(VaiTro.QuanTriVien)]
    [InlineData(VaiTro.NhanVien)]
    public async Task Login_PreservesLegacyAccountIdentityRoleAndHash(VaiTro role)
    {
        var (store, app, _) = Setup(role: role);
        var before = store.Accounts[7];
        var result = await app.Auth.LoginAsync("actor", Old);
        Assert.True(result.Success);
        Assert.Equal(7, app.Session!.TaiKhoanId);
        Assert.Equal(role, app.Session.VaiTro);
        Assert.NotEqual(Guid.Empty, app.Session.SessionToken);
        Assert.Equal(before, store.Accounts[7]);
        Assert.Equal(1, store.LoginWrites);
    }

    [Theory]
    [InlineData(false, "wrong", "INVALID_LOGIN")]
    [InlineData(true, Old, "LOCKED")]
    public async Task RejectedLogin_DoesNotIssueSession(bool locked, string password, string expected)
    {
        var (store, app, _) = Setup();
        store.Accounts[7] = store.Accounts[7] with { HoatDong = !locked };
        var result = await app.Auth.LoginAsync("actor", password);
        Assert.Equal(expected, result.Code);
        Assert.Null(app.Session);
        Assert.Equal(0, store.LoginWrites);
    }

    [Fact]
    public async Task FailedRelogin_CannotKeepAnEarlierAuthenticatedSession()
    {
        var (_, app, _) = Setup();
        var previous = (await app.Auth.LoginAsync("actor", Old)).Data!;
        Assert.False((await app.Auth.LoginAsync("actor", "incorrect")).Success);
        Assert.Null(app.Session);
        Assert.False((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), previous)).Success);
    }

    [Fact]
    public async Task ForgedOrAlteredPrincipal_CannotAuthorizePayment()
    {
        var (store, app, _) = Setup(role: VaiTro.NhanVien);
        var issued = (await app.Auth.LoginAsync("actor", Old)).Data!;
        var candidates = new[]
        {
            new UserSession(7, "actor", "Actor", VaiTro.QuanTriVien),
            issued with { VaiTro = VaiTro.QuanTriVien },
            issued with { TaiKhoanId = 8 },
            issued with { SessionToken = Guid.NewGuid() }
        };
        foreach (var candidate in candidates)
            Assert.False((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), candidate)).Success);
        Assert.Equal(0, store.BusinessWrites);
    }

    [Theory]
    [InlineData("locked")]
    [InlineData("role")]
    [InlineData("password")]
    [InlineData("forced")]
    [InlineData("missing")]
    public async Task ChangedAccount_RevokesStalePrincipalBeforeWriting(string change)
    {
        var (store, app, _) = Setup();
        var issued = (await app.Auth.LoginAsync("actor", Old)).Data!;
        store.Accounts[7] = change switch
        {
            "locked" => store.Accounts[7] with { HoatDong = false },
            "role" => store.Accounts[7] with { VaiTro = VaiTro.NhanVien },
            "password" => store.Accounts[7] with { MatKhauHash = "changed" },
            "forced" => store.Accounts[7] with { PhaiDoiMatKhau = true },
            _ => store.Accounts[7]
        };
        if (change == "missing") store.Accounts.Remove(7);
        var result = await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), issued);
        Assert.Equal("SESSION_INVALID", result.Code);
        Assert.Null(app.Session);
        Assert.Equal(0, store.BusinessWrites);
    }

    [Fact]
    public async Task OrdinaryChange_VerifiesCurrentPasswordAndCannotSelectAnotherAccount()
    {
        var (store, app, _) = Setup();
        var oldSession = (await app.Auth.LoginAsync("actor", Old)).Data!;
        var other = store.Accounts[8];
        Assert.Equal("INVALID_PASSWORD", (await app.Auth.ChangePasswordAsync("wrong", New, New)).Code);
        Assert.Equal(TestHasher.Encode(Old), store.Accounts[7].MatKhauHash);
        Assert.True((await app.Auth.ChangePasswordAsync(Old, New, New)).Success);
        Assert.Equal(TestHasher.Encode(New), store.Accounts[7].MatKhauHash);
        Assert.Equal(other, store.Accounts[8]);
        Assert.NotEqual(oldSession.SessionToken, app.Session!.SessionToken);
        Assert.False((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), oldSession)).Success);
        Assert.True((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), app.Session)).Success);
        Assert.All(typeof(AuthService).GetMethods().Where(m => m.Name == "ChangePasswordAsync"),
            m => Assert.DoesNotContain(m.GetParameters(), p => p.ParameterType == typeof(int)));
    }

    [Fact]
    public async Task MandatoryChange_IsRestrictedThenRotatesWithoutAnAccountIdArgument()
    {
        var (store, app, _) = Setup(forced: true);
        var restricted = (await app.Auth.LoginAsync("actor", Old)).Data!;
        Assert.Equal("PASSWORD_CHANGE_REQUIRED", (await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), restricted)).Code);
        Assert.Equal("INVALID_AUTH_STATE", (await app.Auth.ChangePasswordAsync(Old, New, New)).Code);
        Assert.True((await app.Auth.CompleteRequiredPasswordChangeAsync(New, New)).Success);
        Assert.False(app.Session!.PhaiDoiMatKhau);
        Assert.False(store.Accounts[7].PhaiDoiMatKhau);
        Assert.NotEqual(restricted.SessionToken, app.Session.SessionToken);
    }

    [Fact]
    public async Task RequiredChangeEndpoint_RejectsAnOrdinaryOrUnauthenticatedSession()
    {
        var (_, app, _) = Setup();
        Assert.Equal("SESSION_INVALID", (await app.Auth.CompleteRequiredPasswordChangeAsync(New, New)).Code);
        await app.Auth.LoginAsync("actor", Old);
        Assert.Equal("INVALID_AUTH_STATE", (await app.Auth.CompleteRequiredPasswordChangeAsync(New, New)).Code);
    }

    [Fact]
    public async Task FailedCompareAndSwap_PreservesCredentialsAndDoesNotRotate()
    {
        var (store, app, _) = Setup();
        await app.Auth.LoginAsync("actor", Old);
        var original = app.Session;
        store.RejectPasswordWrite = true;
        Assert.False((await app.Auth.ChangePasswordAsync(Old, New, New)).Success);
        Assert.Equal(TestHasher.Encode(Old), store.Accounts[7].MatKhauHash);
        Assert.Same(original, app.Session);
    }

    [Fact]
    public async Task AdminReset_IsSeparateAndInvalidatesOtherInstanceOnNextCommand()
    {
        var (store, admin, clock) = Setup();
        var other = new AppFacade((IStudioRepository)(object)store, new TestHasher(), clock);
        await admin.Auth.LoginAsync("actor", Old);
        var stale = (await other.Auth.LoginAsync("other", Old)).Data!;
        Assert.True((await admin.Administration.ResetPasswordAsync(8, New, admin.Session!)).Success);
        Assert.True(store.Accounts[8].PhaiDoiMatKhau);
        Assert.False((await other.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), stale)).Success);
        Assert.Equal(0, store.BusinessWrites);
    }

    [Fact]
    public async Task Staff_CannotResetAccountsOrReadReportsOrBackup()
    {
        var (store, app, _) = Setup(role: VaiTro.NhanVien);
        await app.Auth.LoginAsync("actor", Old);
        Assert.False((await app.Administration.ResetPasswordAsync(8, New, app.Session!)).Success);
        Assert.False((await app.Reports.LoadAsync(DateTime.Today, DateTime.Today, app.Session!)).Success);
        Assert.False((await app.BackupRestore.BackupAsync("test.bak", app.Session!)).Success);
        Assert.Equal(0, store.BusinessWrites);
        Assert.Equal(TestHasher.Encode(Old), store.Accounts[8].MatKhauHash);
    }

    [Fact]
    public async Task LogoutAndExpiry_RejectFurtherCommands()
    {
        var (store, app, clock) = Setup();
        var first = (await app.Auth.LoginAsync("actor", Old)).Data!;
        app.Auth.SignOut();
        Assert.Null(app.Session);
        Assert.False((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), first)).Success);
        var second = (await app.Auth.LoginAsync("actor", Old)).Data!;
        clock.Now = clock.Now.AddHours(8);
        Assert.False((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), second)).Success);
        Assert.Equal(0, store.BusinessWrites);
    }

    [Fact]
    public async Task LogoutDuringAccountRead_RejectsInFlightAuthorization()
    {
        var (store, app, _) = Setup();
        await app.Auth.LoginAsync("actor", Old);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.BeforeRead = async () => { entered.SetResult(); await release.Task; };
        var command = app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), app.Session!);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        app.Auth.SignOut();
        release.SetResult();
        Assert.False((await command).Success);
        Assert.Equal(0, store.BusinessWrites);
    }

    [Fact]
    public async Task LogoutDuringLogin_DoesNotPublishLateSession()
    {
        var (store, app, _) = Setup();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.BeforeRead = async () => { entered.SetResult(); await release.Task; };
        var login = app.Auth.LoginAsync("actor", Old);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        app.Auth.SignOut();
        release.SetResult();
        Assert.False((await login).Success);
        Assert.Null(app.Session);
    }

    [Fact]
    public async Task LogoutDuringPasswordWrite_DoesNotResurrectSession()
    {
        var (store, app, _) = Setup();
        await app.Auth.LoginAsync("actor", Old);
        store.BeforePasswordWrite = () => { app.Auth.SignOut(); return Task.CompletedTask; };
        Assert.Equal("SESSION_INVALID", (await app.Auth.ChangePasswordAsync(Old, New, New)).Code);
        Assert.Null(app.Session);
        Assert.Equal(TestHasher.Encode(New), store.Accounts[7].MatKhauHash);
    }

    [Fact]
    public async Task AnotherLoginDuringPasswordWrite_IsNotReplacedByTheOldActor()
    {
        var (store, app, _) = Setup();
        await app.Auth.LoginAsync("actor", Old);
        store.BeforePasswordWrite = async () => { await app.Auth.LoginAsync("other", Old); };
        Assert.Equal("SESSION_INVALID", (await app.Auth.ChangePasswordAsync(Old, New, New)).Code);
        Assert.Equal(8, app.Session!.TaiKhoanId);
    }

    [Theory]
    [InlineData(VaiTro.QuanTriVien)]
    [InlineData(VaiTro.NhanVien)]
    public async Task IssuedInternalSession_StillReachesExistingBusinessOperations(VaiTro role)
    {
        var (store, app, clock) = Setup(role: role);
        await app.Auth.LoginAsync("actor", Old);
        var actor = app.Session!;
        var input = new BookingInput(1, 1, clock.Now.AddDays(1), clock.Now.AddDays(1).AddHours(2), 1, 1, null);
        Assert.True((await app.Bookings.CreateAsync(input, actor)).Success);
        Assert.True((await app.Bookings.UpdateAsync(1, input, null, actor)).Success);
        Assert.True((await app.Bookings.AdvanceAsync(1, actor)).Success);
        Assert.True((await app.Bookings.CancelAsync(1, "Customer requested cancellation", actor)).Success);
        Assert.True((await app.Administration.SaveAsync("KhachHang", null,
            new Dictionary<string, object?> { ["HoTen"] = "Synthetic Customer", ["SoDienThoai"] = "0900000000" }, actor)).Success);
        Assert.True((await app.BookingSupport.AddServiceAsync(1, 1, 1, actor)).Success);
        Assert.True((await app.BookingSupport.AssignResourceAsync(1, 1, 1, false, actor)).Success);
        Assert.True((await app.Finance.ReceiveAsync(new(1, "DAT_COC", 1, null), actor)).Success);
        await app.Bookings.SearchAsync(new(null, null, null));
        await app.BookingSupport.GetLookupsAsync("DichVu");
        if (role == VaiTro.QuanTriVien)
        {
            Assert.True((await app.Finance.RefundAsync(new(1, 1, "Reason", null), actor)).Success);
            Assert.True((await app.Reports.LoadAsync(clock.Now, clock.Now, actor)).Success);
        }
        Assert.Equal(role == VaiTro.QuanTriVien ? 9 : 8, store.BusinessWrites);
    }

    public class MutableClock : IClock { public DateTime Now { get; set; } = new(2026, 11, 12, 9, 0, 0); }
    public class TestHasher : IPasswordHasher
    {
        public static string Encode(string value) => "test-only:" + value;
        public string Hash(string password) => Encode(password);
        public bool Verify(string password, string encoded) => encoded == Encode(password);
    }

    public class RepositoryDouble : DispatchProxy
    {
        public Dictionary<int, TaiKhoan> Accounts { get; } = [];
        public int BusinessWrites, LoginWrites;
        public bool RejectPasswordWrite;
        public Func<Task>? BeforeRead, BeforePasswordWrite;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var a = args!;
            switch (method!.Name)
            {
                case "FindAccountAsync": return Task.FromResult(Accounts.Values.FirstOrDefault(x => x.TenDangNhap == (string)a[0]!));
                case "GetAccountByIdAsync": return ReadAsync((int)a[0]!);
                case "UpdateLastLoginAsync": LoginWrites++; return Task.CompletedTask;
                case "ChangeOwnPasswordAsync": return ChangeAsync((int)a[0]!, (string)a[1]!, (string)a[2]!);
                case "ResetPasswordAsync":
                    var id = (int)a[0]!;
                    Accounts[id] = Accounts[id] with { MatKhauHash = (string)a[1]!, PhaiDoiMatKhau = true };
                    return Task.FromResult(Result.Ok());
                case "AddPaymentAsync": BusinessWrites++; return Task.FromResult(Result.Ok());
                case "CreateBookingAsync": BusinessWrites++; return Task.FromResult(1L);
                case "HasConflictAsync": return Task.FromResult(false);
                case "GetBookingAsync": return Task.FromResult<LichChup?>(new(1, "LC-TEST", 1, "Customer", 1, "Agreed package", 100,
                    new DateTime(2099, 1, 1, 8, 0, 0), new DateTime(2099, 1, 1, 10, 0, 0), 1, "Photographer", 1, "Room", TrangThaiLich.DaDatLich, 0, null, null));
                case "UpdateBookingAsync": case "UpdateBookingStatusAsync": case "CancelBookingAsync":
                case "SaveSimpleAsync": case "AddBookingServiceAsync": case "AssignResourceAsync": case "AddRefundAsync":
                    BusinessWrites++; return Task.FromResult(Result.Ok());
                case "SearchBookingsAsync": return Task.FromResult<IReadOnlyList<LichChup>>([]);
                case "GetLookupsAsync": return Task.FromResult<IReadOnlyList<LookupItem>>([]);
                case "GetReportAsync": return Task.FromResult(new ReportData(0, 0, 0, 0, 0, 0, 0, 0, []));
                default: throw new NotSupportedException("Unexpected repository call: " + method.Name);
            }
        }
        private async Task<TaiKhoan?> ReadAsync(int id)
        {
            if (BeforeRead is { } hook) await hook();
            return Accounts.GetValueOrDefault(id);
        }
        private async Task<Result> ChangeAsync(int id, string expected, string replacement)
        {
            if (BeforePasswordWrite is { } hook) await hook();
            if (RejectPasswordWrite || Accounts[id].MatKhauHash != expected) return Result.Fail("SESSION_INVALID", "Rejected test write");
            Accounts[id] = Accounts[id] with { MatKhauHash = replacement, PhaiDoiMatKhau = false };
            return Result.Ok();
        }
    }
}
