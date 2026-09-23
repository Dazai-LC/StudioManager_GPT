using StudioManager.Application.Services;
using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

public sealed class MasterDataValidationTests
{
    private static readonly UserSession Admin = new(1, "admin", "Admin", VaiTro.QuanTriVien);
    private static readonly UserSession Employee = new(2, "staff", "Nhân viên", VaiTro.NhanVien);

    [Fact]
    public async Task PackageWithNegativePrice_IsRejectedBeforePersistence()
    {
        var result = await Service().SaveAsync("GoiChup", null, new Dictionary<string, object?>
        {
            ["TenGoiChup"] = "Gói thử", ["GiaGoi"] = -1m, ["ThoiLuongPhut"] = 60
        }, Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID", result.Code);
    }

    [Fact]
    public async Task PackageWithZeroDuration_IsRejectedBeforePersistence()
    {
        var result = await Service().SaveAsync("GoiChup", null, new Dictionary<string, object?>
        {
            ["TenGoiChup"] = "Gói thử", ["GiaGoi"] = 0m, ["ThoiLuongPhut"] = 0
        }, Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID", result.Code);
    }

    [Fact]
    public async Task ResourceWithZeroQuantity_IsRejectedBeforePersistence()
    {
        var result = await Service().SaveAsync("TaiNguyen", null, new Dictionary<string, object?>
        {
            ["TenTaiNguyen"] = "Đèn", ["LoaiTaiNguyen"] = "THIET_BI", ["TongSoLuong"] = 0
        }, Admin);

        Assert.False(result.Success);
        Assert.Equal("INVALID", result.Code);
    }

    [Fact]
    public async Task CustomerWithInvalidEmail_IsRejectedBeforePersistence()
    {
        var result = await Service().SaveAsync("KhachHang", null, new Dictionary<string, object?>
        {
            ["HoTen"] = "Khách thử", ["SoDienThoai"] = "0900000000", ["Email"] = "khong-hop-le"
        }, Employee);

        Assert.False(result.Success);
        Assert.Equal("INVALID", result.Code);
    }

    [Fact]
    public async Task EmployeeCannotSaveMasterData()
    {
        var result = await Service().SaveAsync("DichVu", null, new Dictionary<string, object?>(), Employee);

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.Code);
    }

    private static AdministrationService Service() => new(null!, null!);
}
