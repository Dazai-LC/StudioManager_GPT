using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using StudioManager.Domain;

namespace StudioManager.Application;

/// <summary>
/// Trusted in-process authority for existing internal accounts. This is not a transport
/// security boundary: an untrusted desktop must not receive direct repository/SQL access.
/// </summary>
public sealed class InternalAuthorization(IStudioRepository repository, IClock clock)
{
    private sealed record IssuedSession(UserSession Principal, DateTime ExpiresAt);
    private readonly ConcurrentDictionary<Guid, IssuedSession> _sessions = new();
    private readonly object _sync = new();
    private long _generation;
    public UserSession? CurrentSession { get; private set; }

    internal long BeginSignIn()
    {
        lock (_sync) { SignOut(); return _generation; }
    }

    internal UserSession? TryIssue(TaiKhoan account, long expectedGeneration)
    {
        lock (_sync)
        {
            if (_generation != expectedGeneration) return null;
            if (CurrentSession is { } previous) _sessions.TryRemove(previous.SessionToken, out _);
            var principal = new UserSession(account.Id, account.TenDangNhap, account.HoTen, account.VaiTro, account.PhaiDoiMatKhau)
            {
                SessionToken = Guid.NewGuid(),
                CredentialStamp = Stamp(account.MatKhauHash)
            };
            _sessions[principal.SessionToken] = new(principal, clock.Now.AddHours(8));
            CurrentSession = principal;
            return principal;
        }
    }

    public async Task<Result<UserSession>> RequireAsync(UserSession? presented, CancellationToken ct = default,
        bool adminOnly = false, bool allowPasswordChange = false)
    {
        if (presented is null || presented.SessionToken == Guid.Empty ||
            !_sessions.TryGetValue(presented.SessionToken, out var issued) || issued.Principal != presented ||
            issued.ExpiresAt <= clock.Now)
            return Result<UserSession>.Fail("SESSION_INVALID", "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");

        var current = await repository.GetAccountByIdAsync(issued.Principal.TaiKhoanId, ct);
        // Logout/revocation/expiry may occur while the database read is in flight.
        if (!_sessions.TryGetValue(presented.SessionToken, out var stillIssued) || !ReferenceEquals(issued, stillIssued) || issued.ExpiresAt <= clock.Now)
            return Result<UserSession>.Fail("SESSION_INVALID", "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
        if (current is null || !current.HoatDong || current.VaiTro != issued.Principal.VaiTro ||
            current.PhaiDoiMatKhau != issued.Principal.PhaiDoiMatKhau ||
            !string.Equals(Stamp(current.MatKhauHash), issued.Principal.CredentialStamp, StringComparison.Ordinal))
        {
            RevokeAccount(issued.Principal.TaiKhoanId);
            return Result<UserSession>.Fail("SESSION_INVALID", "Tài khoản hoặc quyền truy cập đã thay đổi. Vui lòng đăng nhập lại.");
        }
        if (current.PhaiDoiMatKhau && !allowPasswordChange)
            return Result<UserSession>.Fail("PASSWORD_CHANGE_REQUIRED", "Bạn cần đổi mật khẩu trước khi sử dụng chức năng này.");
        if (adminOnly && current.VaiTro != VaiTro.QuanTriVien)
            return Result<UserSession>.Fail("FORBIDDEN", "Chỉ Quản trị viên được thực hiện thao tác này.");
        return Result<UserSession>.Ok(issued.Principal);
    }

    public async Task DemandCurrentAsync(CancellationToken ct = default, bool adminOnly = false)
    {
        var access = await RequireAsync(CurrentSession, ct, adminOnly);
        if (!access.Success) throw new UnauthorizedAccessException(access.Message);
    }

    public void SignOut()
    {
        lock (_sync)
        {
            _generation++;
            if (CurrentSession is { } principal) _sessions.TryRemove(principal.SessionToken, out _);
            CurrentSession = null;
        }
    }

    internal long RevokeAccount(int accountId)
    {
        lock (_sync)
        {
            foreach (var entry in _sessions.Where(x => x.Value.Principal.TaiKhoanId == accountId))
                _sessions.TryRemove(entry.Key, out _);
            if (CurrentSession?.TaiKhoanId == accountId) { CurrentSession = null; _generation++; }
            return _generation;
        }
    }

    internal long? RevokeForPasswordChange(UserSession expected)
    {
        lock (_sync)
        {
            var canRotate = CurrentSession == expected && _sessions.ContainsKey(expected.SessionToken);
            RevokeAccount(expected.TaiKhoanId);
            return canRotate ? _generation : null;
        }
    }

    public static string Stamp(string encodedPasswordHash)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(encodedPasswordHash)));
}
