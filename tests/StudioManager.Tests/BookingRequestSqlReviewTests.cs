using Xunit;

namespace StudioManager.Tests;

public sealed class BookingRequestSqlReviewTests
{
    [Fact]
    public void MigrationIsAdditiveDisabledAndRetainsBookingHistoryConstraints()
    {
        var root = Root();
        var sql = File.ReadAllText(Path.Combine(root, "database", "07_P04_BookingRequests_REVIEW_ONLY.sql"));
        Assert.Contains("@OwnerApprovedExecution bit=0", sql, StringComparison.Ordinal);
        Assert.Contains("@ExpectedDatabaseName sysname=NULL", sql, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"(?im)^\s*(USE|GO|DROP|ALTER|GRANT|UPDATE|DELETE)\b", sql);
        Assert.DoesNotMatch(@"(?im)^\s*INSERT[ \t]+(?![ \t@])", sql); // In-memory baseline/identity declarations only.
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(sql, @"(?i)CREATE TABLE").Count);
        Assert.Contains("UX_YCDL_OfficialBooking", sql, StringComparison.Ordinal);
        Assert.Contains("UQ_YCDL_Submission", sql, StringComparison.Ordinal);
        Assert.Contains("REFERENCES dbo.LichChup(LichChupId)", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CASCADE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApprovalReusesTransactionScopedWriter_WithoutNestedCommit()
    {
        var root = Root();
        var writer = File.ReadAllText(Path.Combine(root, "src", "StudioManager.Infrastructure", "SqlStudioRepository.BookingWriter.cs"));
        var requestRepo = File.ReadAllText(Path.Combine(root, "src", "StudioManager.Infrastructure", "SqlBookingRequestRepository.cs"));
        var internalRepo = File.ReadAllText(Path.Combine(root, "src", "StudioManager.Infrastructure", "SqlStudioRepository.cs"));
        Assert.Contains("CreateBookingWithinTransactionAsync(cn, tx", internalRepo, StringComparison.Ordinal);
        Assert.Contains("CreateBookingWithinTransactionAsync(cn, tx", requestRepo, StringComparison.Ordinal);
        Assert.DoesNotContain("CommitAsync", writer, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", writer, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync(CancellationToken.None)", requestRepo, StringComparison.Ordinal);
    }

    [Fact]
    public void StaffRequestMutations_LockAccountBeforeRequest_AndRecheckLocatedOwner()
    {
        var source = File.ReadAllText(Path.Combine(Root(), "src", "StudioManager.Infrastructure", "SqlBookingRequestRepository.cs"));
        var helper = source[source.IndexOf("private static async Task<Stored> LockStaffRequest", StringComparison.Ordinal)..
            source.IndexOf("private static async Task<Stored?> ReadRequest", StringComparison.Ordinal)];
        Assert.Contains("WITH(READCOMMITTED)", helper, StringComparison.Ordinal);
        Assert.True(helper.IndexOf("dbo.TaiKhoanKhachHang WITH(UPDLOCK,HOLDLOCK)", StringComparison.Ordinal) < helper.IndexOf("await ReadRequest", StringComparison.Ordinal));
        Assert.Contains("stored.Details.AccountId != account", helper, StringComparison.Ordinal);
        Assert.Contains("LockStaffRequest(cn, tx, actor, requestId, true, ct)", source, StringComparison.Ordinal);
        Assert.Contains("LockStaffRequest(cn, tx, actor, requestId, false, ct)", source, StringComparison.Ordinal);
        Assert.Contains("r.TaiKhoanKhachHangId=@Owner AND r.KhachHangId=l.KhachHangId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void P04ExactCentBoundary_IsRepresentedInMigrationAndLegacyBalanceSql()
    {
        var sql = File.ReadAllText(Path.Combine(Root(), "database", "07_P04_BookingRequests_REVIEW_ONLY.sql"));
        Assert.Contains("TongDuKien decimal(18,2)", sql, StringComparison.Ordinal);
        Assert.Contains("TongDaChot decimal(18,2)", sql, StringComparison.Ordinal);
        Assert.Contains("CK_YCDLDV_ExactMoney", sql, StringComparison.Ordinal);
        var legacy = File.ReadAllText(Path.Combine(Root(), "src", "StudioManager.Infrastructure", "SqlStudioRepository.cs"));
        Assert.Contains("CAST(SUM(SoLuong*DonGiaChot) AS decimal(32,4))", legacy, StringComparison.Ordinal);
        Assert.Contains("CAST(SUM(SoTien) AS decimal(28,2))", legacy, StringComparison.Ordinal);
        var payment=File.ReadAllText(Path.Combine(Root(),"src","StudioManager.Infrastructure","SqlStudioRepository.Payments.cs"));
        Assert.Contains("CAST(SUM(SoLuong*DonGiaChot) AS decimal(32,4))",payment,StringComparison.Ordinal);
        Assert.Contains("BookingMoneyPolicy.ValidateCollection(input.SoTien,booking.Total-booking.Collected)",payment,StringComparison.Ordinal);
    }
    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "StudioManager.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
