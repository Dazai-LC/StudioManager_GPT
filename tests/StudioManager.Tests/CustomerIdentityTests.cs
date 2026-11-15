using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using StudioManager.IdentityService;
using StudioManager.Infrastructure;
using Xunit;

namespace StudioManager.Tests;

public sealed class CustomerIdentityTests
{
    private const string Password = "CustomerPass1";
    private const string NewPassword = "ChangedPass2";
    private static readonly CustomerContact Contact = new("Khách kiểm thử", "0900000000", "contact@example.test", "Địa chỉ thử");

    [Fact]
    public async Task RegisterLoginProfileLogout_UsesSeparateAccountAndNeverInfersLegacyOwnership()
    {
        var fixture = new Fixture(); var customer = fixture.Connection();
        Assert.True((await customer.RegisterAsync("  A@Example.test ", Password, Password, Contact)).Success);
        Assert.True((await customer.LoginAsync("a@example.test", Password)).Success);
        Assert.Equal("a@example.test", (await customer.GetMyProfileAsync()).Data!.LoginEmail);
        Assert.Equal("NOT_LINKED", (await customer.GetMyOwnedCustomerAsync()).Code);
        Assert.Empty(fixture.Store.Links);
        Assert.Equal(2, fixture.Store.BusinessCustomers.Count);
        Assert.False((await customer.RegisterAsync("a@EXAMPLE.test", Password, Password, Contact)).Success);
        customer.Logout();
        Assert.False((await customer.GetMyProfileAsync()).Success);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("locked")]
    public async Task WrongPasswordOrLockedCustomer_DoesNotKeepAnEarlierSession(string condition)
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a");
        var customer = fixture.Connection(); Assert.True((await customer.LoginAsync("a@example.test", Password)).Success);
        if (condition == "locked") fixture.Store.Accounts[1] = fixture.Store.Accounts[1] with { Active = false };
        Assert.False((await customer.LoginAsync("a@example.test", condition == "wrong" ? "WrongPassword1" : Password)).Success);
        Assert.False((await customer.GetMyProfileAsync()).Success);
    }

    [Fact]
    public async Task PasswordChange_RequiresCurrentPassword_RevokesAllOldConnections_AndKeepsNewLoginWorking()
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a");
        var first = fixture.Connection(); var second = fixture.Connection();
        await first.LoginAsync("a@example.test", Password); await second.LoginAsync("a@example.test", Password);
        Assert.Equal("INVALID_PASSWORD", (await first.ChangePasswordAsync("WrongPassword1", NewPassword, NewPassword)).Code);
        Assert.True((await first.GetMyProfileAsync()).Success);
        Assert.True((await first.ChangePasswordAsync(Password, NewPassword, NewPassword)).Success);
        Assert.False((await first.GetMyProfileAsync()).Success);
        Assert.False((await second.GetMyProfileAsync()).Success);
        Assert.False((await first.LoginAsync("a@example.test", Password)).Success);
        Assert.True((await first.LoginAsync("a@example.test", NewPassword)).Success);
    }

    [Fact]
    public async Task CustomerACannotReadOrUpdateB_EvenWithATargetIdInTheWireRequest()
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a"); await fixture.AddCustomer("b");
        var first = new IdentityDispatcher(fixture.Connection()); var second = fixture.Connection();
        await first.DispatchAsync(new(IdentityCommand.CustomerLogin) { Identifier = "a@example.test", Password = Password });
        await second.LoginAsync("b@example.test", Password);
        var profile = await first.DispatchAsync(new(IdentityCommand.GetMyProfile) { CustomerId = 2, LinkId = 2 });
        Assert.Equal("a@example.test", profile.Profile!.LoginEmail);
        Assert.True((await first.DispatchAsync(new(IdentityCommand.UpdateMyProfile)
        { CustomerId = 2, Contact = new("A updated", Contact.Phone, Contact.Email, Contact.Address) })).Success);
        Assert.Equal("A updated", fixture.Store.Accounts[1].Contact.FullName);
        Assert.Equal(Contact.FullName, (await second.GetMyProfileAsync()).Data!.Contact.FullName);
    }

    [Theory]
    [InlineData("guest")]
    [InlineData("customer")]
    public async Task LinkingStaffCommands_AreRejectedBeforeRepositoryAccess(string actor)
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a");
        var service = fixture.Connection();
        if (actor == "customer") await service.LoginAsync("a@example.test", Password);
        Assert.False((await service.ListLinkReviewsAsync()).Success);
        foreach (var decision in Enum.GetValues<CustomerLinkDecision>())
            Assert.False((await service.ReviewLinkAsync(1, decision, 41, "Không có quyền")).Success);
        Assert.Equal(0, fixture.Store.ReviewCalls);
        Assert.Equal(0, fixture.Store.ListCalls);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("staff")]
    public async Task StaffApprovalAndRevocation_ControlsAccess_WithoutMatchingContacts(string reviewerName)
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a"); await fixture.AddCustomer("b");
        var first = fixture.Connection(); var second = fixture.Connection(); var reviewer = fixture.Connection();
        await first.LoginAsync("a@example.test", Password); await second.LoginAsync("b@example.test", Password);
        Assert.True((await first.RequestLinkAsync("KH-41", "Đối chiếu tại quầy")).Success);
        Assert.Equal("NOT_LINKED", (await first.GetMyOwnedCustomerAsync()).Code);
        Assert.True((await reviewer.StaffLoginAsync(reviewerName, Password)).Success);
        var pending = Assert.Single((await reviewer.ListLinkReviewsAsync()).Data!);
        Assert.True((await reviewer.ReviewLinkAsync(pending.Id, CustomerLinkDecision.Approve, 41, "Đã đối chiếu giấy tờ tại quầy")).Success);
        Assert.Equal("KH-41", (await first.GetMyOwnedCustomerAsync()).Data!.Code);
        Assert.Equal("NOT_LINKED", (await second.GetMyOwnedCustomerAsync()).Code);
        Assert.True((await reviewer.ReviewLinkAsync(pending.Id, CustomerLinkDecision.Revoke, null, "Khách yêu cầu thu hồi")).Success);
        Assert.Equal("NOT_LINKED", (await first.GetMyOwnedCustomerAsync()).Code);
        Assert.Equal(CustomerLinkState.Revoked, (await first.GetMyLinkAsync()).Data!.State);
    }

    [Fact]
    public async Task RejectionAndDuplicateOwner_DoNotCreateOwnership()
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a"); await fixture.AddCustomer("b");
        var first = fixture.Connection(); var second = fixture.Connection(); var staff = fixture.Connection();
        await first.LoginAsync("a@example.test", Password); await second.LoginAsync("b@example.test", Password);
        await staff.StaffLoginAsync("staff", Password);
        await first.RequestLinkAsync("KH-41", "Đề nghị đối chiếu");
        Assert.True((await staff.ReviewLinkAsync(1, CustomerLinkDecision.Reject, null, "Chưa đủ bằng chứng")).Success);
        Assert.Equal("NOT_LINKED", (await first.GetMyOwnedCustomerAsync()).Code);
        Assert.True((await first.RequestLinkAsync("KH-41", "Đã bổ sung bằng chứng")).Success);
        Assert.True((await staff.ReviewLinkAsync(2, CustomerLinkDecision.Approve, 41, "Đã xác minh")).Success);
        await second.RequestLinkAsync("KH-41", "Cùng số điện thoại");
        Assert.False((await staff.ReviewLinkAsync(3, CustomerLinkDecision.Approve, 41, "Đề nghị trùng")).Success);
        Assert.Equal("NOT_LINKED", (await second.GetMyOwnedCustomerAsync()).Code);
    }

    [Fact]
    public async Task LockedCustomerAndRevokedStaffAreRevalidatedOnNextCommand()
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a");
        var customer = fixture.Connection(); var staff = fixture.Connection();
        await customer.LoginAsync("a@example.test", Password); await staff.StaffLoginAsync("staff", Password);
        await customer.RequestLinkAsync("KH-41", "Đối chiếu");
        fixture.Directory.Accounts[2] = fixture.Directory.Accounts[2] with { HoatDong = false };
        Assert.False((await staff.ReviewLinkAsync(1, CustomerLinkDecision.Approve, 41, "Không còn quyền")).Success);
        fixture.Store.Accounts[1] = fixture.Store.Accounts[1] with { Active = false };
        Assert.False((await customer.GetMyProfileAsync()).Success);
        Assert.False((await customer.UpdateMyProfileAsync(Contact)).Success);
        Assert.Equal(0, fixture.Store.ReviewCalls);
    }

    [Fact]
    public async Task CustomerCredentialsCannotBecomeStaff_AndStaffCannotUseCustomerEndpoints()
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a"); var service = fixture.Connection();
        Assert.False((await service.StaffLoginAsync("a@example.test", Password)).Success);
        Assert.True((await service.StaffLoginAsync("admin", Password)).Success);
        Assert.False((await service.GetMyProfileAsync()).Success);
        fixture.Directory.Accounts[1] = fixture.Directory.Accounts[1] with { PhaiDoiMatKhau = true };
        Assert.False((await service.StaffLoginAsync("admin", Password)).Success);
    }

    [Fact]
    public async Task SessionExpiry_RequiresFreshAuthentication()
    {
        var fixture = new Fixture(); await fixture.AddCustomer("a"); var service = fixture.Connection();
        await service.LoginAsync("a@example.test", Password);
        fixture.Clock.Now = fixture.Clock.Now.AddHours(9);
        Assert.False((await service.GetMyProfileAsync()).Success);
    }

    [Theory]
    [InlineData("weak", "weak")]
    [InlineData("CustomerPass1", "DifferentPass2")]
    public async Task InvalidRegistration_DoesNotPersist(string password, string confirmation)
    {
        var fixture = new Fixture();
        Assert.False((await fixture.Connection().RegisterAsync("a@example.test", password, confirmation, Contact)).Success);
        Assert.Empty(fixture.Store.Accounts);
    }

    internal sealed class Fixture
    {
        internal readonly Pbkdf2PasswordHasher Hasher = new();
        internal readonly Clock Clock = new();
        internal readonly Directory Directory = new();
        internal readonly Store Store;
        internal Fixture()
        {
            Directory.Accounts[1] = new(1, null, "admin", Hasher.Hash(Password), VaiTro.QuanTriVien, true, false, "Admin");
            Directory.Accounts[2] = new(2, null, "staff", Hasher.Hash(Password), VaiTro.NhanVien, true, false, "Staff");
            Store = new(Directory);
        }
        internal CustomerIdentityService Connection() => new(Store, Directory, Hasher, Clock);
        internal Task<Result> AddCustomer(string name) => Connection().RegisterAsync(name + "@example.test", Password, Password, Contact);
    }
    internal sealed class Clock : IClock { public DateTime Now { get; set; } = new(2026, 11, 13, 1, 0, 0, DateTimeKind.Utc); }
    internal sealed class Directory : ICustomerStaffDirectory
    {
        internal Dictionary<int, TaiKhoan> Accounts { get; } = [];
        public Task<TaiKhoan?> FindAsync(string username, CancellationToken ct = default) => Task.FromResult(Accounts.Values.FirstOrDefault(a => a.TenDangNhap == username));
        public Task<TaiKhoan?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(Accounts.GetValueOrDefault(id));
    }
    internal sealed record LinkRecord(long Id, long Account, string Reference, string Evidence, CustomerLinkState State, long? Customer);
    internal sealed class Store(Directory staff) : ICustomerIdentityRepository
    {
        internal Dictionary<long, CustomerAccount> Accounts { get; } = [];
        internal Dictionary<long, LinkRecord> Links { get; } = [];
        internal Dictionary<long, OwnedCustomerContact> BusinessCustomers { get; } = new()
        { [41] = new("KH-41", "Hồ sơ A", Contact.Phone, Contact.Email, Contact.Address), [42] = new("KH-42", "Hồ sơ B", Contact.Phone, Contact.Email, Contact.Address) };
        internal int ReviewCalls, ListCalls;
        internal Exception? RequestLinkFailure;
        internal TimeSpan RequestLinkDelay;
        public Task<CustomerAccount?> FindByLoginAsync(string email, CancellationToken ct = default) => Task.FromResult(Accounts.Values.FirstOrDefault(a => a.LoginEmail == email));
        public Task<CustomerAccount?> GetAccountAsync(long id, CancellationToken ct = default) => Task.FromResult(Accounts.GetValueOrDefault(id));
        public Task<Result> RegisterAsync(string email, string hash, CustomerContact contact, CancellationToken ct = default)
        {
            if (Accounts.Values.Any(a => a.LoginEmail == email)) return Task.FromResult(Result.Fail("REGISTRATION_UNAVAILABLE", ""));
            var id = Accounts.Count + 1L; Accounts[id] = new(id, email, hash, Guid.NewGuid(), true, contact); return Ok();
        }
        private bool Allowed(CustomerCredential actor) => Accounts.TryGetValue(actor.AccountId, out var a) && a.Active &&
            a.SecurityVersion == actor.SecurityVersion && a.PasswordHash == actor.PasswordHash;
        private bool Allowed(CustomerReviewer actor) => staff.Accounts.TryGetValue(actor.AccountId, out var a) && a.HoatDong &&
            !a.PhaiDoiMatKhau && a.VaiTro == actor.Role && a.MatKhauHash == actor.PasswordHash;
        public Task<Result> UpdateContactAsync(CustomerCredential actor, CustomerContact contact, CancellationToken ct = default)
        {
            if (!Allowed(actor)) return Deny(); Accounts[actor.AccountId] = Accounts[actor.AccountId] with { Contact = contact }; return Ok();
        }
        public Task<Result> ChangePasswordAsync(CustomerCredential actor, string hash, CancellationToken ct = default)
        {
            if (!Allowed(actor)) return Deny();
            Accounts[actor.AccountId] = Accounts[actor.AccountId] with { PasswordHash = hash, SecurityVersion = Guid.NewGuid() }; return Ok();
        }
        public Task<CustomerLink?> GetMyLinkAsync(CustomerCredential actor, CancellationToken ct = default)
        {
            var link = Allowed(actor) ? Links.Values.LastOrDefault(l => l.Account == actor.AccountId) : null;
            return Task.FromResult(link is null ? null : new CustomerLink(link.Id, link.State, link.Reference, DateTime.UtcNow,
                link.State == CustomerLinkState.Approved ? BusinessCustomers[link.Customer!.Value].Code : null));
        }
        public async Task<Result> RequestLinkAsync(CustomerCredential actor, string reference, string evidence, CancellationToken ct = default)
        {
            if (RequestLinkDelay > TimeSpan.Zero) await Task.Delay(RequestLinkDelay, ct);
            if (RequestLinkFailure is { } failure) throw failure;
            if (!Allowed(actor)) return Result.Fail("UNAUTHORIZED", "");
            if (Links.Values.Any(l => l.Account == actor.AccountId && l.State is CustomerLinkState.Pending or CustomerLinkState.Approved))
                return Result.Fail("LINK_EXISTS", "");
            var id = Links.Count + 1L; Links[id] = new(id, actor.AccountId, reference, evidence, CustomerLinkState.Pending, null); return Result.Ok();
        }
        public Task<OwnedCustomerContact?> GetOwnedCustomerAsync(CustomerCredential actor, CancellationToken ct = default)
        {
            var link = Allowed(actor) ? Links.Values.FirstOrDefault(l => l.Account == actor.AccountId && l.State == CustomerLinkState.Approved) : null;
            return Task.FromResult(link is null ? null : BusinessCustomers.GetValueOrDefault(link.Customer!.Value));
        }
        public Task<IReadOnlyList<CustomerLinkReview>> ListLinksAsync(CustomerReviewer actor, long? beforeLinkId = null, CancellationToken ct = default)
        {
            ListCalls++;
            IReadOnlyList<CustomerLinkReview> result = !Allowed(actor) ? [] :
                Links.Values.Where(l => l.State is CustomerLinkState.Pending or CustomerLinkState.Approved && (beforeLinkId is null || l.Id < beforeLinkId))
                    .OrderByDescending(l => l.Id).Take(25).Select(l => new CustomerLinkReview(l.Id, l.Account, Accounts[l.Account].LoginEmail,
                        Accounts[l.Account].Contact.FullName, l.Reference, l.Evidence, l.State, l.Customer)).ToArray();
            return Task.FromResult(result);
        }
        public Task<Result> ReviewLinkAsync(CustomerReviewer actor, long id, CustomerLinkDecision decision, long? customerId, string reason, CancellationToken ct = default)
        {
            ReviewCalls++;
            if (!Allowed(actor)) return Deny();
            if (!Links.TryGetValue(id, out var link) || (decision == CustomerLinkDecision.Approve && !Accounts[link.Account].Active))
                return Task.FromResult(Result.Fail("NOT_FOUND", ""));
            if ((decision == CustomerLinkDecision.Revoke && link.State != CustomerLinkState.Approved) ||
                (decision != CustomerLinkDecision.Revoke && link.State != CustomerLinkState.Pending))
                return Task.FromResult(Result.Fail("LINK_STATE_CHANGED", ""));
            if (decision == CustomerLinkDecision.Approve && (customerId is null || !BusinessCustomers.ContainsKey(customerId.Value)))
                return Task.FromResult(Result.Fail("NOT_FOUND", ""));
            if (decision == CustomerLinkDecision.Approve && Links.Values.Any(l => l.State == CustomerLinkState.Approved && (l.Customer == customerId || l.Account == link.Account)))
                return Task.FromResult(Result.Fail("OWNERSHIP_CONFLICT", ""));
            Links[id] = link with { State = decision switch { CustomerLinkDecision.Approve => CustomerLinkState.Approved,
                CustomerLinkDecision.Reject => CustomerLinkState.Rejected, _ => CustomerLinkState.Revoked }, Customer = decision == CustomerLinkDecision.Approve ? customerId : link.Customer };
            return Ok();
        }
        private static Task<Result> Ok() => Task.FromResult(Result.Ok());
        private static Task<Result> Deny() => Task.FromResult(Result.Fail("UNAUTHORIZED", ""));
    }
}
