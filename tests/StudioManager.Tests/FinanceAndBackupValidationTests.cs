using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

public sealed class FinanceAndBackupValidationTests
{
    private static readonly UserSession Admin = new(1, "admin", "Admin", VaiTro.QuanTriVien);
    private static readonly UserSession Employee = new(2, "staff", "Nhân viên", VaiTro.NhanVien);

    [Fact]
    public async Task Receive_RejectsZeroAmountBeforePersistence()
    {
        var service = new FinanceService(null!);

        var result = await service.ReceiveAsync(new PaymentInput(1, "DAT_COC", 0, null), Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID_AMOUNT", result.Code);
    }

    [Fact]
    public async Task Receive_RejectsUnknownPaymentTypeBeforePersistence()
    {
        var service = new FinanceService(null!);

        var result = await service.ReceiveAsync(new PaymentInput(1, "KHONG_HOP_LE", 100_000, null), Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID_TYPE", result.Code);
    }

    [Fact]
    public async Task Refund_RequiresAdministratorBeforePersistence()
    {
        var service = new FinanceService(null!);

        var result = await service.RefundAsync(new RefundInput(1, 100_000, "Khách đổi lịch", null), Employee);

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.Code);
    }

    [Fact]
    public async Task Backup_RejectsEmployeeAndInvalidExtensionBeforePersistence()
    {
        var service = new BackupRestoreService(null!);

        var employeeResult = await service.BackupAsync("C:/backup/studio.bak", Employee);
        var extensionResult = await service.BackupAsync("C:/backup/studio.zip", Admin);

        Assert.False(employeeResult.Success);
        Assert.Equal("FORBIDDEN", employeeResult.Code);
        Assert.False(extensionResult.Success);
        Assert.Equal("INVALID_PATH", extensionResult.Code);
    }

    [Fact]
    public async Task Restore_RejectsMissingBackupFileBeforePersistence()
    {
        var service = new BackupRestoreService(null!);

        var result = await service.RestoreAsync(Path.Combine(Path.GetTempPath(), "studio-manager-missing-backup.bak"), Admin);

        Assert.False(result.Success);
        Assert.Equal("FILE_NOT_FOUND", result.Code);
    }

    [Fact]
    public async Task BookingSupport_RejectsNonPositiveServiceQuantityBeforePersistence()
    {
        var service = new BookingSupportService(null!);

        var result = await service.AddServiceAsync(1, 1, 0, Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID_QUANTITY", result.Code);
    }

    [Fact]
    public async Task BookingSupport_RejectsNonPositiveResourceQuantityBeforePersistence()
    {
        var service = new BookingSupportService(null!);

        var result = await service.AssignResourceAsync(1, 1, 0, false, Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID_QUANTITY", result.Code);
    }
}
