using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

public sealed class SqlTargetIdentityTests
{
    private const string Secret = "private-connection-password-and-provider-text";
    private const string PrivatePath = @"C:\Private\Studio\schema-secret.sql";
    private static readonly DateTime CreatedAt = new(2026, 11, 12, 9, 1, 2, 123, DateTimeKind.Unspecified);
    private static readonly string[] Columns = ["DatabaseName", "MachineName", "Updateability", "RunId", "SchemaVersion",
        "SourceChecksum", "InstanceName", "DatabaseId", "CreatedAt", "MetadataFingerprint", "ServerName"];
    private static readonly int[] IdentityColumns = [0, 1, 6, 10, 3, 4, 5];
    private static readonly string[] IdentityReasons = ["DATABASE_MISMATCH", "MACHINE_MISMATCH", "INSTANCE_MISMATCH",
        "SERVER_MISMATCH", "RUN_ID_MISMATCH", "VERSION_MISMATCH", "CHECKSUM_MISMATCH"];

    public static IEnumerable<object[]> IdentityFailures()
    {
        for (var index = 0; index < IdentityColumns.Length; index++)
            foreach (var variant in new[] { "wrong", "null", "case", "padding" })
                yield return [IdentityColumns[index], IdentityReasons[index], variant];
    }

    [Fact]
    public async Task TextDatabaseIdAndOtherProjectedFields_MapWithoutValueLoss()
    {
        using var table = Table();
        await using var reader = Reader(table);
        var observed = await SqlTargetIdentityReader.ReadAsync(reader);
        Assert.Equal(SqlTestSettings.ApprovedDatabase, observed.Database);
        Assert.Equal(SqlTestSettings.ApprovedMachine, observed.Machine);
        Assert.Equal("READ_WRITE", observed.Updateability);
        Assert.Equal(SqlTestSettings.ApprovedRunId, observed.RunId);
        Assert.Equal(SqlTestSettings.SchemaVersion, observed.Version);
        Assert.Equal(SqlSchemaManifest.ExpectedChecksum(), observed.Checksum);
        Assert.Equal(SqlTestSettings.ApprovedInstance, observed.Instance);
        Assert.Equal(7, observed.DatabaseId);
        Assert.Equal(CreatedAt, observed.CreatedAt);
        Assert.Equal(DateTimeKind.Unspecified, observed.CreatedAt.Kind);
        Assert.Equal(new string('A', 64), observed.MetadataFingerprint);
        Assert.Equal(SqlTestSettings.ApprovedServerName, observed.Server);
        Assert.Contains("CONVERT(nvarchar(11), DB_ID()) AS DatabaseId", SqlTargetIdentityReader.Query);
        Validate(observed);
        Assert.Equal(Columns, Regex.Matches(SqlTargetIdentityReader.Query, @"\bAS (\w+)").Select(match => match.Groups[1].Value));
    }

    [Theory]
    [MemberData(nameof(IdentityFailures))]
    public async Task EachIdentityGuard_RejectsWrongNullCaseAndPadding_WithItsOwnReason(int ordinal, string reason, string variant)
    {
        using var table = Table();
        var expected = (string)table.Rows[0][ordinal];
        table.Rows[0][ordinal] = variant switch
        {
            "null" => DBNull.Value,
            "padding" => expected + " ",
            "case" => expected == expected.ToUpperInvariant() ? expected.ToLowerInvariant() : expected.ToUpperInvariant(),
            _ => Secret
        };
        await using var reader = Reader(table);
        var observed = await SqlTargetIdentityReader.ReadAsync(reader);
        var error = Assert.Throws<SqlTestDiagnosticException>(() => Validate(observed));
        AssertReason(error, reason);
    }

    [Theory]
    [InlineData(0, "READ_DATABASE_FAILED")]
    [InlineData(1, "READ_MACHINE_FAILED")]
    [InlineData(2, "READ_UPDATEABILITY_FAILED")]
    [InlineData(3, "READ_RUN_ID_FAILED")]
    [InlineData(4, "READ_VERSION_FAILED")]
    [InlineData(5, "READ_CHECKSUM_FAILED")]
    [InlineData(6, "READ_INSTANCE_FAILED")]
    [InlineData(7, "READ_DATABASE_ID_FAILED")]
    [InlineData(8, "READ_CREATED_AT_FAILED")]
    [InlineData(9, "READ_METADATA_FINGERPRINT_FAILED")]
    [InlineData(10, "READ_SERVER_FAILED")]
    public async Task WrongClrType_IsNotCoerced_AndNamesTheFailedColumn(int ordinal, string reason)
    {
        using var table = Table(ordinal);
        await using var reader = Reader(table);
        AssertReason(await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadAsync(reader)), reason);
    }

    [Theory]
    [InlineData(7, "READ_DATABASE_ID_SQL_NULL")]
    [InlineData(8, "READ_CREATED_AT_FAILED")]
    public async Task NullDatabaseIdOrCreatedAt_CannotBecomeADefaultValue(int ordinal, string reason)
    {
        using var table = Table();
        table.Rows[0][ordinal] = DBNull.Value;
        await using var reader = Reader(table);
        AssertReason(await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadAsync(reader)), reason);
    }

    [Fact]
    public async Task NoRow_ReportsReaderEmpty()
    {
        using var table = Table();
        table.Rows.Clear();
        await using var reader = Reader(table);
        AssertReason(await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadAsync(reader)), "READER_EMPTY");
    }

    [Fact]
    public async Task ClosedReader_ReportsReaderReadFailure()
    {
        using var table = Table();
        await using var reader = Reader(table);
        reader.Close();
        AssertReason(await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadAsync(reader)), "READER_READ_FAILED");
    }

    [Theory]
    [InlineData("count")]
    [InlineData("name")]
    [InlineData("order")]
    public async Task UnexpectedProjection_CannotSilentlyChangeOrdinalMeaning(string change)
    {
        using var table = Table();
        if (change == "count") table.Columns.Add("Unexpected", typeof(string));
        if (change == "name") table.Columns[0].ColumnName = Secret;
        if (change == "order") table.Columns[1].SetOrdinal(0);
        await using var reader = Reader(table);
        AssertReason(await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadAsync(reader)), "READER_LAYOUT_INVALID");
    }

    [Fact]
    public async Task DatabaseIdAndCreationDate_KeepContinuityAcrossReads()
    {
        using var table = Table();
        await using var reader = Reader(table);
        var observed = await SqlTargetIdentityReader.ReadAsync(reader);
        var first = observed.ValidateContinuity(null);
        Assert.Equal(first, observed.ValidateContinuity(first));
        AssertReason(Assert.Throws<SqlTestDiagnosticException>(() => (observed with { DatabaseId = 8 }).ValidateContinuity(first)),
            "DATABASE_ID_CHANGED");
        AssertReason(Assert.Throws<SqlTestDiagnosticException>(() => (observed with { CreatedAt = CreatedAt.AddTicks(1) }).ValidateContinuity(first)),
            "DATABASE_CREATED_AT_CHANGED");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("READ_ONLY")]
    [InlineData("read_write")]
    public async Task WriteabilityGuard_IsPreserved_WithoutExecutingWrites(string? updateability)
    {
        using var table = Table();
        await using var reader = Reader(table);
        var observed = (await SqlTargetIdentityReader.ReadAsync(reader)) with { Updateability = updateability };
        observed.RequireWriteability(false);
        AssertReason(Assert.Throws<SqlTestDiagnosticException>(() => observed.RequireWriteability(true)), "DATABASE_NOT_WRITABLE");
    }

    [Fact]
    public void SqlVerifiedServerName_DoesNotBypassTheConfiguredClientDataSourceGuard()
    {
        var error = Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(
            $"Server={SqlTestSettings.ApprovedServerName};Database={SqlTestSettings.ApprovedDatabase};Integrated Security=true;Encrypt=true",
            SqlTestSettings.ApprovedRunId, SqlTestSettings.ReadMode));
        AssertReason(error, "CONFIG_DATA_SOURCE_MISMATCH");
    }

    [Theory]
    [InlineData("denied", "MANIFEST_ACCESS_DENIED")]
    [InlineData("missing", "MANIFEST_SCRIPT_NOT_FOUND")]
    [InlineData("directory", "MANIFEST_SCRIPT_NOT_FOUND")]
    [InlineData("io", "MANIFEST_SCRIPT_READ_FAILED")]
    public void ManifestFileFailure_IsNotMisreportedAsChecksumMismatch(string failure, string reason)
    {
        var error = Assert.Throws<SqlTestDiagnosticException>(() => SqlSchemaManifest.CalculateChecksum(_ => throw (failure switch
        {
            "denied" => new UnauthorizedAccessException(PrivatePath),
            "missing" => new FileNotFoundException(Secret, PrivatePath),
            "directory" => new DirectoryNotFoundException(PrivatePath),
            _ => new IOException(Secret)
        })));
        AssertReason(error, reason);
    }

    [Fact]
    public void ManifestRootFailure_IsDistinctFromSqlIdentityFailure()
    {
        AssertReason(Assert.Throws<SqlTestDiagnosticException>(() => SqlSchemaManifest.FindRepositoryRoot(AppContext.BaseDirectory, _ => false)),
            "MANIFEST_ROOT_UNAVAILABLE");
        AssertReason(Assert.Throws<SqlTestDiagnosticException>(() => SqlSchemaManifest.FindRepositoryRoot(AppContext.BaseDirectory,
            _ => throw new IOException(PrivatePath))), "MANIFEST_ROOT_LOOKUP_FAILED");
    }

    [Fact]
    public void ApprovedSourceHashAndLineEndingNormalization_AreUnchanged()
    {
        Assert.Equal("B8A556B123DDB93B742FDA93643AC70CB4D11E8F62E9AE30BAF37758900B0EF3", SqlSchemaManifest.ExpectedChecksum());
        Assert.Equal(SqlSchemaManifest.CalculateChecksum(name => name + "\n"),
            SqlSchemaManifest.CalculateChecksum(name => name + "\r\n"));
    }

    [Theory]
    [InlineData("QUERY_EXECUTION_FAILED")]
    [InlineData("READER_DISPOSE_FAILED")]
    [InlineData("COMMAND_DISPOSE_FAILED")]
    public async Task ResourceFailure_HasPreciseReason_WithoutProviderPayload(string reason)
    {
        var expected = Enum.Parse<SqlTargetIdentityReason>(reason);
        var probe = new ProbeResource { FailDispose = reason != "QUERY_EXECUTION_FAILED" };
        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.RunAsync(
            SqlTestDiagnosticStage.TARGET_IDENTITY, () => SqlTestDiagnostics.TargetResourceAsync(
                SqlTargetIdentityReason.QUERY_EXECUTION_FAILED, expected,
                () => reason == "QUERY_EXECUTION_FAILED" ? Task.FromException<ProbeResource>(new IOException(Secret)) : Task.FromResult(probe),
                _ => Task.FromResult(true))));
        AssertReason(error, reason);
    }

    [Fact]
    public async Task ReaderCloseFailure_CannotReplaceAnEarlierIdentityReason()
    {
        var probe = new ProbeResource { FailDispose = true };
        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.RunAsync(
            SqlTestDiagnosticStage.TARGET_IDENTITY, () => SqlTestDiagnostics.TargetResourceAsync(
                SqlTargetIdentityReason.QUERY_EXECUTION_FAILED, SqlTargetIdentityReason.READER_DISPOSE_FAILED,
                () => Task.FromResult(probe),
                _ => Task.FromException<bool>(SqlTestSettings.Refused(SqlTargetIdentityReason.DATABASE_MISMATCH)))));
        Assert.Equal(1, probe.DisposeCalls);
        AssertReason(error, "DATABASE_MISMATCH");
    }

    [Theory]
    [InlineData("DATABASE_MISMATCH")]
    [InlineData("READ_CREATED_AT_FAILED")]
    [InlineData("MANIFEST_ACCESS_DENIED")]
    public async Task ReasonSurvivesTheProductionStageWrapper_WhilePayloadIsDiscarded(string reason)
    {
        var original = SqlTestSettings.Refused(Enum.Parse<SqlTargetIdentityReason>(reason));
        original.Data["ConnectionString"] = Secret;
        original.Source = PrivatePath;
        original.HelpLink = PrivatePath;
        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.VerifyConnectionAsync(
            () => Task.CompletedTask, () => Task.FromException(original), () => Task.CompletedTask,
            () => Task.CompletedTask, null, () => ValueTask.CompletedTask));
        Assert.NotSame(original, error);
        AssertReason(error, reason);
    }

    [Fact]
    public void UnknownOrCrossStageReason_IsRefused()
    {
        Assert.Equal("SQL test diagnostic reason refused.", Assert.Throws<InvalidOperationException>(() =>
            new SqlTestDiagnosticException(SqlTestDiagnosticStage.TARGET_IDENTITY, (SqlTargetIdentityReason)999)).Message);
        Assert.Equal("SQL test diagnostic reason refused.", Assert.Throws<InvalidOperationException>(() =>
            new SqlTestDiagnosticException(SqlTestDiagnosticStage.PERMISSIONS, SqlTargetIdentityReason.DATABASE_MISMATCH)).Message);
    }

    // Share the reader fixture used to exercise the DatabaseId text projection.
    private static DbDataReader Reader(DataTable table) => new DatabaseIdReaderDiagnosticsTests.ProbeReader(table);

    private static DataTable Table(int? wrongTypeOrdinal = null)
    {
        var values = new object[] { SqlTestSettings.ApprovedDatabase, SqlTestSettings.ApprovedMachine, "READ_WRITE",
            SqlTestSettings.ApprovedRunId, SqlTestSettings.SchemaVersion, SqlSchemaManifest.ExpectedChecksum(),
            SqlTestSettings.ApprovedInstance, "7", CreatedAt, new string('A', 64), SqlTestSettings.ApprovedServerName };
        var table = new DataTable();
        for (var index = 0; index < Columns.Length; index++)
        {
            var type = index == 8 ? typeof(DateTime) : typeof(string);
            if (index == wrongTypeOrdinal)
            {
                type = type == typeof(string) ? typeof(int) : typeof(string);
                values[index] = type == typeof(int) ? 123 : Secret;
            }
            table.Columns.Add(Columns[index], type);
        }
        table.Rows.Add(values);
        return table;
    }

    private static void Validate(SqlTargetIdentityObservation observed) =>
        SqlTestSettings.Parse($"Server={SqlTestSettings.ApprovedDataSource};Database={SqlTestSettings.ApprovedDatabase};Integrated Security=true;Encrypt=true",
            SqlTestSettings.ApprovedRunId, SqlTestSettings.ReadMode).ValidateObservedTarget(
                observed.Database, observed.Machine, observed.Instance, observed.Server, observed.RunId, observed.Version, observed.Checksum);

    private static void AssertReason(SqlTestDiagnosticException error, string reason)
    {
        Assert.Equal(SqlTestDiagnosticStage.TARGET_IDENTITY, error.Stage);
        Assert.Equal(reason, error.Reason.ToString());
        Assert.Equal($"SQL test failed at stage TARGET_IDENTITY; reason {reason}. Diagnostic details are intentionally omitted.", error.Message);
        Assert.Null(error.InnerException);
        Assert.Null(error.StackTrace);
        Assert.Null(error.HelpLink);
        Assert.Empty(error.Data);
        var rendered = error.Message + error.ToString() + error.Source + error.GetBaseException();
        Assert.DoesNotContain(Secret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(PrivatePath, rendered, StringComparison.Ordinal);
    }

    private sealed class ProbeResource : IAsyncDisposable
    {
        internal bool FailDispose { get; init; }
        internal int DisposeCalls { get; private set; }
        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return FailDispose ? ValueTask.FromException(new IOException(PrivatePath)) : ValueTask.CompletedTask;
        }
    }
}
