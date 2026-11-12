using Xunit;

namespace StudioManager.SqlIntegrationTests;

public sealed class SqlTestDiagnosticsTests
{
    private const string ConnectionMaterial = "Server=private-server;Database=private-db;User Id=private-user;Password=private-password";
    private const string PrivatePath = @"C:\Private\Studio\sensitive-schema.sql";
    private const string SqlSecret = "private-provider-message-and-sql-secret";
    private const string CleanupSecret = "private-cleanup-connection-material";
    private static readonly SqlTestDiagnosticStage[] OrderedStages =
    [
        SqlTestDiagnosticStage.CONNECTION_OPEN, SqlTestDiagnosticStage.TARGET_IDENTITY,
        SqlTestDiagnosticStage.PERMISSIONS, SqlTestDiagnosticStage.SCHEMA_FINGERPRINT,
        SqlTestDiagnosticStage.SCHEMA_SHAPE
    ];

    [Theory]
    [InlineData("CONNECTION_OPEN", false)]
    [InlineData("TARGET_IDENTITY", false)]
    [InlineData("PERMISSIONS", false)]
    [InlineData("SCHEMA_FINGERPRINT", false)]
    [InlineData("SCHEMA_SHAPE", false)]
    [InlineData("CONNECTION_OPEN", true)]
    [InlineData("TARGET_IDENTITY", true)]
    [InlineData("PERMISSIONS", true)]
    [InlineData("SCHEMA_FINGERPRINT", true)]
    [InlineData("SCHEMA_SHAPE", true)]
    public async Task Failure_ReportsActualStage_RedactsPayload_StopsLaterChecks_AndCleansUp(string stage, bool cleanupFails)
    {
        var failed = Enum.Parse<SqlTestDiagnosticStage>(stage);
        var visited = new List<SqlTestDiagnosticStage>();
        var cleanupCalls = 0;
        Func<Task> Step(SqlTestDiagnosticStage current) => async () =>
        {
            visited.Add(current);
            await Task.Yield();
            if (current == failed) throw PrivateFailure();
        };

        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.VerifyConnectionAsync(
            Step(OrderedStages[0]), Step(OrderedStages[1]), Step(OrderedStages[2]), Step(OrderedStages[3]), Step(OrderedStages[4]),
            () =>
            {
                cleanupCalls++;
                return cleanupFails ? ValueTask.FromException(new IOException(CleanupSecret)) : ValueTask.CompletedTask;
            }));

        Assert.Equal(OrderedStages.Take(Array.IndexOf(OrderedStages, failed) + 1), visited);
        Assert.Equal(1, cleanupCalls);
        AssertSanitized(error, stage);
    }

    [Fact]
    public async Task SuccessfulValidation_RunsAllChecksInOrder_AndLeavesConnectionOwnedByCaller()
    {
        var visited = new List<SqlTestDiagnosticStage>();
        var cleanupCalled = false;
        Func<Task> Step(SqlTestDiagnosticStage stage) => () => { visited.Add(stage); return Task.CompletedTask; };

        await SqlTestDiagnostics.VerifyConnectionAsync(
            Step(OrderedStages[0]), Step(OrderedStages[1]), Step(OrderedStages[2]), Step(OrderedStages[3]), Step(OrderedStages[4]),
            () => { cleanupCalled = true; return ValueTask.CompletedTask; });

        Assert.Equal(OrderedStages, visited);
        Assert.False(cleanupCalled);
    }

    [Fact]
    public async Task ReadOnlyValidation_KeepsIdentityPermissionsAndFingerprint_WithoutWriteShapeStep()
    {
        var visited = new List<SqlTestDiagnosticStage>();
        Func<Task> Step(SqlTestDiagnosticStage stage) => () => { visited.Add(stage); return Task.CompletedTask; };
        await SqlTestDiagnostics.VerifyConnectionAsync(
            Step(OrderedStages[0]), Step(OrderedStages[1]), Step(OrderedStages[2]), Step(OrderedStages[3]), null,
            () => ValueTask.CompletedTask);

        Assert.Equal(OrderedStages.Take(4), visited);
    }

    [Fact]
    public void InvalidConfiguration_IsReportedAsTargetIdentity_BeforeConnectionWork()
    {
        var error = Assert.Throws<SqlTestDiagnosticException>(() => SqlTestDiagnostics.TargetIdentity(
            () => SqlTestSettings.Parse(ConnectionMaterial, SqlTestSettings.ApprovedRunId, SqlTestSettings.ReadMode)));
        AssertSanitized(error, "TARGET_IDENTITY", SqlTargetIdentityReason.CONFIG_DATABASE_MISMATCH);
    }

    [Fact]
    public void ManifestReadFailure_IsRedactedAtTargetIdentity()
    {
        var error = Assert.Throws<SqlTestDiagnosticException>(() => SqlTestDiagnostics.TargetIdentity<SqlTestSettings>(
            () => throw PrivateFailure()));
        AssertSanitized(error, "TARGET_IDENTITY");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(null, false)]
    [InlineData(false, null)]
    public async Task ExistingElevatedOrUnknownRoleGuard_StillFails_WithPermissionsStage(bool? sysadmin, bool? dbOwner)
    {
        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.RunAsync(
            SqlTestDiagnosticStage.PERMISSIONS, () =>
            {
                SqlTestSettings.ValidateObservedPermissions(sysadmin, dbOwner, true, true);
                return Task.CompletedTask;
            }));
        AssertSanitized(error, "PERMISSIONS");
    }

    [Fact]
    public async Task ReadOnlyMetadataAssertionFailure_IsReportedAsSchemaShape()
    {
        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.RunAsync(
            SqlTestDiagnosticStage.SCHEMA_SHAPE, () =>
            {
                Assert.Equal(14, 0);
                return Task.CompletedTask;
            }));
        AssertSanitized(error, "SCHEMA_SHAPE");
    }

    [Fact]
    public async Task NestedExceptionCannotSpoofTheCurrentStage()
    {
        var inner = new SqlTestDiagnosticException(SqlTestDiagnosticStage.CONNECTION_OPEN);
        inner.Data["Private"] = SqlSecret;
        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.RunAsync(
            SqlTestDiagnosticStage.PERMISSIONS, () => Task.FromException(inner)));
        AssertSanitized(error, "PERMISSIONS");
    }

    [Fact]
    public void UnknownStage_CannotBecomeAnUnboundedDiagnosticIdentifier()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new SqlTestDiagnosticException((SqlTestDiagnosticStage)999));
        Assert.Equal("SQL test diagnostic stage refused.", error.Message);
    }

    private static Exception PrivateFailure()
    {
        var error = new InvalidOperationException(ConnectionMaterial,
            new FileNotFoundException(SqlSecret, PrivatePath)) { Source = SqlSecret, HelpLink = PrivatePath };
        error.Data["ConnectionString"] = ConnectionMaterial;
        error.Data["Sql"] = SqlSecret;
        return error;
    }

    private static void AssertSanitized(SqlTestDiagnosticException error, string expectedStage, SqlTargetIdentityReason? expectedReason = null)
    {
        Assert.Equal(expectedStage, error.Stage.ToString());
        var reason = expectedStage == "TARGET_IDENTITY" ? expectedReason ?? SqlTargetIdentityReason.UNEXPECTED_FAILURE : (SqlTargetIdentityReason?)null;
        Assert.Equal(reason, error.Reason);
        var detail = reason is null ? "" : $"; reason {reason}";
        Assert.Equal($"SQL test failed at stage {expectedStage}{detail}. Diagnostic details are intentionally omitted.", error.Message);
        Assert.Null(error.InnerException);
        Assert.Empty(error.Data);
        Assert.Null(error.StackTrace);
        Assert.Null(error.HelpLink);
        var rendered = error.Message + error.ToString() + error.StackTrace + error.Source + error.GetBaseException();
        foreach (var secret in new[] { ConnectionMaterial, PrivatePath, SqlSecret, CleanupSecret, "private-password" })
            Assert.DoesNotContain(secret, rendered, StringComparison.Ordinal);
    }
}
