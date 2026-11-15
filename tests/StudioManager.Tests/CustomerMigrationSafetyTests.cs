using System.Text.RegularExpressions;
using Xunit;

namespace StudioManager.Tests;

public sealed class CustomerMigrationSafetyTests
{
    [Fact]
    public void P03Migration_IsDisabledAdditiveAndDoesNotSeedOrSwitchBusinessDatabase()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "StudioManager.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var sql = File.ReadAllText(Path.Combine(root.FullName, "database/06_P03_CustomerIdentity_REVIEW_ONLY.sql"));
        Assert.Contains("DECLARE @OwnerApprovedExecution bit=0;", sql);
        Assert.Contains("DECLARE @ExpectedDatabaseName sysname=NULL;", sql);
        Assert.Equal(new[] { "TaiKhoanKhachHang", "SoHuuKhachHang" },
            Regex.Matches(sql, @"CREATE TABLE dbo\.(\w+)").Select(match => match.Groups[1].Value));
        Assert.DoesNotMatch(new Regex(@"^\s*(USE|GO|INSERT|UPDATE|DELETE|MERGE|DROP|ALTER TABLE)\b", RegexOptions.Multiline | RegexOptions.IgnoreCase), sql);
        Assert.Contains("FK_SHKH_Customer REFERENCES dbo.KhachHang(KhachHangId)", sql);
        Assert.Contains("UX_SHKH_ApprovedCustomer", sql);
    }
}
