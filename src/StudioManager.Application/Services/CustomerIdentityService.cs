using System.Net.Mail;
using StudioManager.Domain;

namespace StudioManager.Application.Services;

/// <summary>One instance per pipe connection; all actor identity remains inside the service.</summary>
public sealed class CustomerIdentityService(ICustomerIdentityRepository repo, ICustomerStaffDirectory staff,
    IPasswordHasher hasher, IClock clock)
{
    private CustomerCredential? _customer;
    private CustomerReviewer? _reviewer;
    private DateTime _expires;

    public bool IsSessionExpired => (_customer is not null || _reviewer is not null) && clock.Now >= _expires;

    public void Logout() { _customer = null; _reviewer = null; _expires = default; }

    internal async Task<CustomerCredential?> BookingCustomerAsync(CancellationToken ct)
        => await CustomerAsync(ct) is null ? null : _customer;
    internal async Task<CustomerReviewer?> BookingReviewerAsync(CancellationToken ct)
        => await ReviewerAsync(ct) ? _reviewer : null;

    public async Task<Result> RegisterAsync(string email, string password, string confirmation, CustomerContact contact, CancellationToken ct = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null || !ValidContact(contact)) return InvalidInput();
        var validation = PasswordPolicy(password, confirmation);
        if (!validation.Success) return validation;
        // Registration never queries KhachHang or infers ownership from contact details.
        return await repo.RegisterAsync(normalized, hasher.Hash(password), Clean(contact), ct);
    }

    public async Task<Result<CustomerProfile>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        Logout();
        var normalized = NormalizeEmail(email);
        if (normalized is null || string.IsNullOrEmpty(password) || password.Length > 128) return LoginFailed<CustomerProfile>();
        var account = await repo.FindByLoginAsync(normalized, ct);
        if (account is null || !hasher.Verify(password, account.PasswordHash) || !account.Active) return LoginFailed<CustomerProfile>();
        var current = await repo.GetAccountAsync(account.Id, ct);
        if (current is null || !current.Active || current.SecurityVersion != account.SecurityVersion ||
            current.PasswordHash != account.PasswordHash) return LoginFailed<CustomerProfile>();
        _customer = new(current.Id, current.SecurityVersion, current.PasswordHash);
        _expires = clock.Now.AddHours(8);
        return Result<CustomerProfile>.Ok(new(current.LoginEmail, current.Contact));
    }

    public async Task<Result> StaffLoginAsync(string username, string password, CancellationToken ct = default)
    {
        Logout();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50 || string.IsNullOrEmpty(password) || password.Length > 128)
            return LoginFailed();
        var account = await staff.FindAsync(username.Trim(), ct);
        if (account is null || !hasher.Verify(password, account.MatKhauHash) || !account.HoatDong || account.PhaiDoiMatKhau)
            return LoginFailed();
        var current = await staff.GetAsync(account.Id, ct);
        if (current is null || !current.HoatDong || current.PhaiDoiMatKhau || current.MatKhauHash != account.MatKhauHash ||
            current.VaiTro != account.VaiTro) return LoginFailed();
        _reviewer = new(current.Id, current.VaiTro, current.MatKhauHash);
        _expires = clock.Now.AddHours(8);
        return Result.Ok();
    }

    public async Task<Result<CustomerProfile>> GetMyProfileAsync(CancellationToken ct = default)
    {
        var account = await CustomerAsync(ct);
        return account is null ? Denied<CustomerProfile>() : Result<CustomerProfile>.Ok(new(account.LoginEmail, account.Contact));
    }

    public async Task<Result> UpdateMyProfileAsync(CustomerContact contact, CancellationToken ct = default)
    {
        if (!ValidContact(contact)) return InvalidInput();
        if (await CustomerAsync(ct) is null) return Denied();
        return await repo.UpdateContactAsync(_customer!, Clean(contact), ct);
    }

    public async Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, string confirmation, CancellationToken ct = default)
    {
        var validation = PasswordPolicy(newPassword, confirmation);
        if (!validation.Success) return validation;
        if (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 128) return InvalidInput();
        var account = await CustomerAsync(ct);
        if (account is null) return Denied();
        if (!hasher.Verify(currentPassword, account.PasswordHash)) return Result.Fail("INVALID_PASSWORD", "Mật khẩu hiện tại không đúng.");
        var result = await repo.ChangePasswordAsync(_customer!, hasher.Hash(newPassword), ct);
        Logout(); // Successful rotation revokes every old version, including this connection.
        return result;
    }

    public async Task<Result<CustomerLink?>> GetMyLinkAsync(CancellationToken ct = default)
    {
        if (await CustomerAsync(ct) is null) return Denied<CustomerLink?>();
        return Result<CustomerLink?>.Ok(await repo.GetMyLinkAsync(_customer!, ct));
    }

    public async Task<Result> RequestLinkAsync(string reference, string evidence, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100 || string.IsNullOrWhiteSpace(evidence) || evidence.Length > 500)
            return InvalidInput();
        if (await CustomerAsync(ct) is null) return Denied();
        return await repo.RequestLinkAsync(_customer!, reference.Trim(), evidence.Trim(), ct);
    }

    public async Task<Result<OwnedCustomerContact>> GetMyOwnedCustomerAsync(CancellationToken ct = default)
    {
        if (await CustomerAsync(ct) is null) return Denied<OwnedCustomerContact>();
        var owned = await repo.GetOwnedCustomerAsync(_customer!, ct);
        return owned is null ? Result<OwnedCustomerContact>.Fail("NOT_LINKED", "Chưa có liên kết được duyệt còn hiệu lực.")
            : Result<OwnedCustomerContact>.Ok(owned);
    }

    public async Task<Result<IReadOnlyList<CustomerLinkReview>>> ListLinkReviewsAsync(long? beforeLinkId = null, CancellationToken ct = default)
    {
        if (!await ReviewerAsync(ct)) return Denied<IReadOnlyList<CustomerLinkReview>>();
        if (beforeLinkId is <= 0) return Result<IReadOnlyList<CustomerLinkReview>>.Fail("INVALID_INPUT", "Thông tin không hợp lệ.");
        return Result<IReadOnlyList<CustomerLinkReview>>.Ok(await repo.ListLinksAsync(_reviewer!, beforeLinkId, ct));
    }

    public async Task<Result> ReviewLinkAsync(long linkId, CustomerLinkDecision decision, long? khachHangId, string reason, CancellationToken ct = default)
    {
        if (!await ReviewerAsync(ct)) return Denied();
        if (linkId <= 0 || !Enum.IsDefined(decision) || string.IsNullOrWhiteSpace(reason) || reason.Length > 500 ||
            (decision == CustomerLinkDecision.Approve && khachHangId is not > 0)) return InvalidInput();
        return await repo.ReviewLinkAsync(_reviewer!, linkId, decision, khachHangId, reason.Trim(), ct);
    }

    private async Task<CustomerAccount?> CustomerAsync(CancellationToken ct)
    {
        if (_customer is null || clock.Now >= _expires) { Logout(); return null; }
        var current = await repo.GetAccountAsync(_customer.AccountId, ct);
        if (current is null || !current.Active || current.SecurityVersion != _customer.SecurityVersion || current.PasswordHash != _customer.PasswordHash)
        { Logout(); return null; }
        return current;
    }

    private async Task<bool> ReviewerAsync(CancellationToken ct)
    {
        if (_reviewer is null || clock.Now >= _expires) { Logout(); return false; }
        var current = await staff.GetAsync(_reviewer.AccountId, ct);
        if (current is null || !current.HoatDong || current.PhaiDoiMatKhau || current.MatKhauHash != _reviewer.PasswordHash ||
            current.VaiTro != _reviewer.Role || current.VaiTro is not (VaiTro.QuanTriVien or VaiTro.NhanVien))
        { Logout(); return false; }
        return true;
    }

    private static string? NormalizeEmail(string? email)
    {
        var value = email?.Trim().ToLowerInvariant();
        return value is { Length: > 3 and <= 254 } && value.All(c => c is >= '!' and <= '~') &&
            MailAddress.TryCreate(value, out var address) && address.Address == value ? value : null;
    }

    private static bool ValidContact(CustomerContact? contact) => contact is not null &&
        !string.IsNullOrWhiteSpace(contact.FullName) && contact.FullName.Length <= 100 &&
        !string.IsNullOrWhiteSpace(contact.Phone) && contact.Phone.Length <= 20 &&
        contact.Phone.All(c => char.IsAsciiDigit(c) || c is '+' or ' ' or '-' or '(' or ')') &&
        NormalizeEmail(contact.Email) is not null && contact.Address is not null && contact.Address.Length <= 255;
    private static CustomerContact Clean(CustomerContact contact) => contact with
    { FullName = contact.FullName.Trim(), Phone = contact.Phone.Trim(), Email = contact.Email.Trim(), Address = contact.Address.Trim() };
    private static Result PasswordPolicy(string? password, string? confirmation) =>
        password != confirmation ? Result.Fail("PASSWORD_MISMATCH", "Xác nhận mật khẩu không khớp.") :
        password is not { Length: >= 8 and <= 128 } || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)
            ? Result.Fail("WEAK_PASSWORD", "Mật khẩu 8–128 ký tự, có chữ hoa, chữ thường và số.") : Result.Ok();
    private static Result InvalidInput() => Result.Fail("INVALID_INPUT", "Thông tin không hợp lệ.");
    private static Result Denied() => Result.Fail("UNAUTHORIZED", "Vui lòng đăng nhập đúng tài khoản.");
    private static Result<T> Denied<T>() => Result<T>.Fail("UNAUTHORIZED", "Vui lòng đăng nhập đúng tài khoản.");
    private static Result LoginFailed() => Result.Fail("INVALID_LOGIN", "Thông tin đăng nhập không hợp lệ hoặc tài khoản không khả dụng.");
    private static Result<T> LoginFailed<T>() => Result<T>.Fail("INVALID_LOGIN", "Thông tin đăng nhập không hợp lệ hoặc tài khoản không khả dụng.");
}
