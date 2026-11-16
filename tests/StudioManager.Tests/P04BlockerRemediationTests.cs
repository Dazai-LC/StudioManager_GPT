using System.Globalization;
using System.Reflection;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using StudioManager.IdentityService;
using StudioManager.Infrastructure;
using Xunit;

namespace StudioManager.Tests;

public sealed class P04BlockerRemediationTests
{
    public static IEnumerable<object[]> RejectedWrites()
    {
        foreach (var entity in new[] { "GoiChup", "DichVu" })
        foreach (var update in new[] { false, true })
        foreach (var price in new[] { 123.456m, 0.001m, 10_000_000_000_000_000m, decimal.MaxValue })
            yield return [entity, update, price];
    }

    [Theory, MemberData(nameof(RejectedWrites))]
    public async Task CatalogCreateAndUpdate_RejectPrecisionOrOverflowBeforeAuthorizationAndSql(string entity, bool update, decimal price)
    {
        var values = Values(entity, price); var actor = new UserSession(1, "fixture", "Fixture", VaiTro.QuanTriVien);
        var application = await new AdministrationService(null!, null!).SaveAsync(entity, update ? 1 : null, values, actor);
        Assert.Equal("INVALID_PRICE", application.Code);
        // Empty configuration cannot open a live endpoint; old unguarded code returns DB_ERROR here.
        var repository = await new SqlStudioRepository("").SaveSimpleAsync(entity, update ? 1 : null, values, actor);
        Assert.Equal("INVALID_PRICE", repository.Code);
        Assert.Equal(price, values[CatalogPricePolicy.PriceColumn(entity)!]);
    }

    public static IEnumerable<object[]> AcceptedWrites()
    {
        foreach (var entity in new[] { "GoiChup", "DichVu" })
        foreach (var update in new[] { false, true })
        foreach (var price in new[] { 0m, 100.00m, 123.45m, BookingMoneyPolicy.LedgerMaximum })
            yield return [entity, update, price];
    }

    [Theory, MemberData(nameof(AcceptedWrites))]
    public async Task ValidCatalogPrices_ReachAuthorizedSaveUnchanged(string entity, bool update, decimal price)
    {
        var repository = DispatchProxy.Create<IStudioRepository, CatalogStore>(); var store = (CatalogStore)(object)repository;
        store.Accounts[1] = new(1, null, "fixture", InternalSecurityRegressionTests.TestHasher.Encode("Fixture1"), VaiTro.QuanTriVien, true, false, "Fixture");
        var app = new AppFacade(repository, new InternalSecurityRegressionTests.TestHasher(), new InternalSecurityRegressionTests.MutableClock());
        Assert.True((await app.Auth.LoginAsync("fixture", "Fixture1")).Success);
        Assert.True((await app.Administration.SaveAsync(entity, update ? 1 : null, Values(entity, price), app.Session!)).Success);
        Assert.Equal(1, store.BusinessWrites); Assert.Equal(price, store.Price);
    }

    [Theory]
    [InlineData("en-US", "123.450000000000000000000000000000000000", true)]
    [InlineData("en-US", "123.450000000000000000000000000000000001", false)]
    [InlineData("en-US", "123.456", false)]
    [InlineData("en-US", "0.001", false)]
    [InlineData("en-US", "1e2", false)]
    [InlineData("en-US", "1,234.50", false)]
    [InlineData("vi-VN", "123,45", true)]
    [InlineData("vi-VN", "123,456", false)]
    [InlineData("vi-VN", "0,001", false)]
    public void RawText_IsCheckedBeforeDecimalParsingCanRound(string culture, string text, bool accepted)
    {
        var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); Assert.Equal(accepted, CatalogPricePolicy.Read(text).Success); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void BinaryFloatsAndMissingOrMalformedPrices_AreNotNormalizedToMoney()
    {
        foreach (var value in new object?[] { null, "", "invalid", 123.45d, 123.45f, -0.01m })
            Assert.False(CatalogPricePolicy.Read(value).Success);
        Assert.True(CatalogPricePolicy.Read(100).Success);
        Assert.True(CatalogPricePolicy.Validate("GoiChup", new Dictionary<string, object?> { ["TenGoiChup"] = "Rename only" }).Success);
    }

    [Theory]
    [InlineData("GoiChup", "GiaGoi")]
    [InlineData("DichVu", "DonGia")]
    public void ActualSqlParameterBinder_UsesExactValidatedDecimal18_2(string entity, string column)
    {
        using var command = new SqlCommand();
        var binder = typeof(SqlStudioRepository).GetMethod("AddCatalogValue", BindingFlags.Static | BindingFlags.NonPublic)!;
        binder.Invoke(null, [command, entity, column, 123.45m]);
        var parameter = command.Parameters["@" + column];
        Assert.Equal(System.Data.SqlDbType.Decimal, parameter.SqlDbType);
        Assert.Equal((byte)18, parameter.Precision); Assert.Equal((byte)2, parameter.Scale); Assert.Equal(123.45m, parameter.Value);
        using var invalid = new SqlCommand();
        Assert.Throws<TargetInvocationException>(() => binder.Invoke(null, [invalid, entity, column, 123.456m]));
        Assert.Empty(invalid.Parameters.Cast<SqlParameter>());
    }

    [Fact]
    public async Task RawSharedSaveInputs_CannotBypassPriceValidationWithoutTheUi()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var actor = new UserSession(1, "fixture", "Fixture", VaiTro.QuanTriVien);
            foreach (var entity in new[] { "GoiChup", "DichVu" })
            foreach (var id in new long?[] { null, 1 })
            foreach (var raw in new object?[] { null, "123.456", "123.450000000000000000000000000000000001", "invalid", 123.45d, 123.45f })
            {
                var values = Values(entity, raw);
                Assert.Equal("INVALID_PRICE", (await new AdministrationService(null!, null!).SaveAsync(entity, id, values, actor)).Code);
                Assert.Equal("INVALID_PRICE", (await new SqlStudioRepository("").SaveSimpleAsync(entity, id, values, actor)).Code);
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnexpectedApprovalFailure_RequiresReconciliation_BeforeAndAfterFakeCommit(bool committed)
    {
        var f = await BookingRequestFixture.Create(); var c = f.Connection();
        await c.Identity.LoginAsync("a@example.test", "CustomerPass1");
        var draft = f.Draft(); await c.Service.PreviewAsync(draft); var id = (await c.Service.SubmitAsync(draft.SubmissionKey)).Data;
        var authority = f.Identity.Connection(); var dispatcher = new IdentityDispatcher(authority, new BookingRequestService(authority, f, f.Identity.Clock));
        var login = new IdentityRequest(IdentityCommand.StaffLogin) { Identifier = "staff", Password = "CustomerPass1" };
        Assert.True((await dispatcher.DispatchAsync(login)).Success);
        f.FailBeforeApprovalCommit = !committed; f.FailAfterApprovalCommit = committed;
        var approve = new IdentityRequest(IdentityCommand.ApproveBookingRequest)
        { BookingRequestId = id, BookingApproval = new(1, 1, f.Requests[id].Quote.Estimate), Reason = "Duyệt thử ngoại lệ transaction" };
        var failure = await dispatcher.DispatchAsync(approve);
        Assert.Equal("OUTCOME_UNKNOWN", failure.Code); Assert.True(IdentityProtocol.RequiresSignIn(failure));
        Assert.Contains("tải trạng thái", failure.Message); Assert.DoesNotContain("synthetic", failure.Message);
        Assert.Equal(committed ? 1 : 0, f.Bookings.Count);
        var mutations = f.Mutations;
        Assert.False((await dispatcher.DispatchAsync(approve)).Success); Assert.Equal(mutations, f.Mutations);
        Assert.True((await dispatcher.DispatchAsync(login)).Success);
        var detail = await dispatcher.DispatchAsync(new(IdentityCommand.GetBookingReview) { BookingRequestId = id });
        Assert.Equal(committed ? "APPROVED" : "PENDING", detail.BookingDetail!.State);
        Assert.Equal(mutations, f.Mutations); // Reload never retries the mutation.
    }

    [Fact]
    public void Migration_RemainsDisabled_AndPinsEngineIdentityAndWholeBaselineBeforeDdl()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "StudioManager.sln"))) directory = directory.Parent;
        var sql = File.ReadAllText(Path.Combine(directory!.FullName, "database", "07_P04_BookingRequests_REVIEW_ONLY.sql"));
        foreach (var token in new[] { "@OwnerApprovedExecution bit=0", "@ExternalPreflightApproved bit=0", "@ExpectedDatabaseName sysname=NULL",
            "@ExpectedBaselineFingerprint varbinary(32)=NULL", "SERVERPROPERTY('InstanceName')", "SERVERPROPERTY('MachineName')",
            "SERVERPROPERTY('ComputerNamePhysicalNetBIOS')", "@ObservedInstanceName IS NULL", "DATALENGTH(Expected)<>DATALENGTH(Observed)",
            "sys.foreign_key_columns", "sys.check_constraints", "@RequiredIndexes", "@ObservedBaselineFingerprint<>@ExpectedBaselineFingerprint" })
            Assert.Contains(token, sql, StringComparison.Ordinal);
        Assert.True(sql.IndexOf("@ObservedBaselineFingerprint<>", StringComparison.Ordinal) < sql.IndexOf("CREATE TABLE dbo.YeuCauDatLich(", StringComparison.Ordinal));
    }

    private static Dictionary<string, object?> Values(string entity, object? price) => entity == "GoiChup"
        ? new() { ["TenGoiChup"] = "Fixture", ["GiaGoi"] = price, ["ThoiLuongPhut"] = 60 }
        : new() { ["TenDichVu"] = "Fixture", ["DonViTinh"] = "unit", ["DonGia"] = price };

    public class CatalogStore : InternalSecurityRegressionTests.RepositoryDouble
    {
        internal decimal Price;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IStudioRepository.SaveSimpleAsync))
            {
                var values = (IReadOnlyDictionary<string, object?>)args![2]!;
                Price = (decimal)values[CatalogPricePolicy.PriceColumn((string)args[0]!)!]!;
            }
            return base.Invoke(method, args);
        }
    }
}
