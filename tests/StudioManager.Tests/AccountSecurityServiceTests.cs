using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

public sealed class AccountSecurityServiceTests
{
    private static readonly UserSession Admin = new(7, "admin", "Admin", VaiTro.QuanTriVien);
    private static readonly UserSession Employee = new(8, "staff", "Nhân viên", VaiTro.NhanVien);

    [Theory]
    [InlineData("Abcdefgh", "WEAK_PASSWORD")]
    [InlineData("abcdefgh1", "WEAK_PASSWORD")]
    [InlineData("ABCDEFGH1", "WEAK_PASSWORD")]
    public async Task ChangePassword_RejectsPasswordsThatDoNotMeetPolicy(string password, string expectedCode)
    {
        var service = new AuthService(null!, new TestHasher(), new FixedClock());

        var result = await service.ChangePasswordAsync(1, password, password);

        Assert.False(result.Success);
        Assert.Equal(expectedCode, result.Code);
    }

    [Fact]
    public async Task ChangePassword_RejectsMismatchedConfirmationBeforePersistence()
    {
        var service = new AuthService(null!, new TestHasher(), new FixedClock());

        var result = await service.ChangePasswordAsync(1, "Password1", "Password2");

        Assert.False(result.Success);
        Assert.Equal("PASSWORD_MISMATCH", result.Code);
    }

    [Fact]
    public async Task CreateAccount_RejectsNonAdministratorBeforePersistence()
    {
        var service = new AdministrationService(null!, new TestHasher());

        var result = await service.CreateAccountAsync("newuser", "Password1", null, VaiTro.NhanVien, Employee);

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.Code);
    }

    [Fact]
    public async Task ResetPassword_RejectsSelfReset()
    {
        var service = new AdministrationService(null!, new TestHasher());

        var result = await service.ResetPasswordAsync(Admin.TaiKhoanId, "Password1", Admin);

        Assert.False(result.Success);
        Assert.Equal("SELF_RESET", result.Code);
    }

    [Fact]
    public async Task ToggleAccountLock_RejectsSelfLock()
    {
        var service = new AdministrationService(null!, new TestHasher());

        var result = await service.ToggleAccountLockAsync(Admin.TaiKhoanId, Admin);

        Assert.False(result.Success);
        Assert.Equal("SELF_LOCK", result.Code);
    }

    private sealed class TestHasher : IPasswordHasher
    {
        public string Hash(string password) => password;
        public bool Verify(string password, string encoded) => password == encoded;
    }

    private sealed class FixedClock : IClock { public DateTime Now => new(2026, 9, 23); }
}
