using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

internal enum SqlTestMode { ReadOnly, IsolatedWrites }

/// <summary>Never reads the application settings and never provisions, resets, or drops a database.</summary>
internal sealed record SqlTestSettings(string ConnectionString, string DatabaseName, string RunId, SqlTestMode Mode)
{
    public override string ToString() => $"SqlTestSettings {{ Mode = {Mode}, Target = [isolated] }}";
    internal const string ModeVariable = "PHOENIX_SQL_TEST_MODE";
    internal const string ConnectionVariable = "PHOENIX_SQL_TEST_CONNECTION_STRING";
    internal const string RunIdVariable = "PHOENIX_SQL_TEST_RUN_ID";
    internal const string ReadMode = "READ_ONLY";
    internal const string WriteMode = "APPROVED_ISOLATED_WRITES";
    internal const string SchemaVersion = "baseline-c2c3c23-spec2.1";
    // Owner-selected existing instance. A different host, run or database requires another review.
    internal const string ApprovedMachine = "MSI";
    internal const string ApprovedInstance = "SQLEXPRESS";
    internal const string ApprovedServerName = @"MSI\SQLEXPRESS";
    internal const string ApprovedDataSource = @"localhost\SQLEXPRESS";
    internal const string ApprovedRunId = "7d70a08ac15349acbd207f82c8349f6e";
    internal const string ApprovedDatabase = "StudioManager_P02_Test_" + ApprovedRunId;

    internal static SqlTestSettings FromEnvironment() => Parse(
        Environment.GetEnvironmentVariable(ConnectionVariable),
        Environment.GetEnvironmentVariable(RunIdVariable),
        Environment.GetEnvironmentVariable(ModeVariable));

    internal static SqlTestSettings Parse(string? connectionString, string? runId, string? mode)
    {
        if (mode is not (ReadMode or WriteMode)) throw Refused(SqlTargetIdentityReason.CONFIG_MODE_INVALID);
        if (!Guid.TryParseExact(runId, "N", out var parsedRunId)) throw Refused(SqlTargetIdentityReason.CONFIG_RUN_ID_INVALID);
        if (runId != ApprovedRunId) throw Refused(SqlTargetIdentityReason.CONFIG_RUN_ID_INVALID);
        if (string.IsNullOrWhiteSpace(connectionString)) throw Refused(SqlTargetIdentityReason.CONFIG_CONNECTION_MISSING);
        SqlConnectionStringBuilder builder;
        try { builder = new SqlConnectionStringBuilder(connectionString); }
        catch { throw Refused(SqlTargetIdentityReason.CONFIG_CONNECTION_INVALID); }

        var normalizedRunId = parsedRunId.ToString("N");
        var requiredDatabase = ApprovedDatabase;
        // A prefix alone is insufficient. Database identity must match the independently supplied run ID.
        if (!string.Equals(builder.InitialCatalog, requiredDatabase, StringComparison.Ordinal))
            throw Refused(SqlTargetIdentityReason.CONFIG_DATABASE_MISMATCH);
        if (!builder.IntegratedSecurity || !string.IsNullOrEmpty(builder.UserID) || !string.IsNullOrEmpty(builder.Password))
            throw Refused(SqlTargetIdentityReason.CONFIG_AUTHENTICATION_REFUSED);
        if (!string.IsNullOrEmpty(builder.AttachDBFilename) || builder.UserInstance ||
            !string.IsNullOrEmpty(builder.FailoverPartner) || builder.ApplicationIntent != ApplicationIntent.ReadWrite)
            throw Refused(SqlTargetIdentityReason.CONFIG_CONNECTION_CAPABILITY_REFUSED);
        if (!string.Equals(builder.DataSource, ApprovedDataSource, StringComparison.OrdinalIgnoreCase))
            throw Refused(SqlTargetIdentityReason.CONFIG_DATA_SOURCE_MISMATCH);

        builder.PersistSecurityInfo = false;
        builder.Pooling = false;
        builder.ConnectTimeout = 5;
        builder.ApplicationName = "Phoenix.P02.IsolatedSqlTests";
        return new(builder.ConnectionString, requiredDatabase, normalizedRunId,
            mode == WriteMode ? SqlTestMode.IsolatedWrites : SqlTestMode.ReadOnly);
    }

    internal void ValidateObservedTarget(string? database, string? machine, string? instance, string? serverName,
        string? runId, string? version, string? checksum)
    {
        if (database != ApprovedDatabase || database != DatabaseName) throw Refused(SqlTargetIdentityReason.DATABASE_MISMATCH);
        if (machine != ApprovedMachine) throw Refused(SqlTargetIdentityReason.MACHINE_MISMATCH);
        var localMachine = SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.LOCAL_MACHINE_READ_FAILED, () => Environment.MachineName);
        if (!string.Equals(machine, localMachine, StringComparison.OrdinalIgnoreCase)) throw Refused(SqlTargetIdentityReason.LOCAL_MACHINE_MISMATCH);
        if (instance != ApprovedInstance) throw Refused(SqlTargetIdentityReason.INSTANCE_MISMATCH);
        if (serverName != ApprovedServerName) throw Refused(SqlTargetIdentityReason.SERVER_MISMATCH);
        if (runId != RunId) throw Refused(SqlTargetIdentityReason.RUN_ID_MISMATCH);
        if (version != SchemaVersion) throw Refused(SqlTargetIdentityReason.VERSION_MISMATCH);
        if (checksum != SqlSchemaManifest.ExpectedChecksum()) throw Refused(SqlTargetIdentityReason.CHECKSUM_MISMATCH);
    }

    internal static void ValidateObservedPermissions(bool? isSysadmin, bool? isDatabaseOwner,
        bool? leastPrivilege, bool? requiredObjectAccess)
    {
        if (isSysadmin != false || isDatabaseOwner != false ||
            leastPrivilege != true || requiredObjectAccess != true) throw Refused();
    }

    internal static InvalidOperationException Refused() => new(
        "SQL test target refused. Supply an explicitly approved local disposable database and matching test markers. No application configuration is used.");

    internal static SqlTestDiagnosticException Refused(SqlTargetIdentityReason reason)
        => new(SqlTestDiagnosticStage.TARGET_IDENTITY, reason);
}

internal static class SqlSchemaManifest
{
    internal static string RepositoryRoot() => FindRepositoryRoot(AppContext.BaseDirectory, File.Exists);

    internal static string FindRepositoryRoot(string startingDirectory, Func<string, bool> fileExists)
        => SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.MANIFEST_ROOT_LOOKUP_FAILED, () =>
    {
        var directory = new DirectoryInfo(startingDirectory);
        while (directory is not null)
        {
            if (fileExists(Path.Combine(directory.FullName, "StudioManager.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        // File.Exists also returns false when inaccessible; do not misreport that as a SQL mismatch.
        throw SqlTestSettings.Refused(SqlTargetIdentityReason.MANIFEST_ROOT_UNAVAILABLE);
    });

    internal static string ExpectedChecksum()
    {
        // Normalize line endings so a Windows Git checkout does not change the schema identity.
        var root = RepositoryRoot();
        return CalculateChecksum(name => File.ReadAllText(Path.Combine(root, "database", name)));
    }

    internal static string CalculateChecksum(Func<string, string> readScript)
        => SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.MANIFEST_HASH_FAILED, () =>
    {
        var scripts = new[] { "02_CreateSchema.sql", "05_UpgradeToSpec2_1.sql" };
        var manifest = string.Join("\n", scripts.Select(name =>
            name + "\n" + ReadScript(() => readScript(name)).Replace("\r\n", "\n")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));
    });

    private static string ReadScript(Func<string> read)
    {
        try { return read(); }
        catch (UnauthorizedAccessException) { throw SqlTestSettings.Refused(SqlTargetIdentityReason.MANIFEST_ACCESS_DENIED); }
        catch (FileNotFoundException) { throw SqlTestSettings.Refused(SqlTargetIdentityReason.MANIFEST_SCRIPT_NOT_FOUND); }
        catch (DirectoryNotFoundException) { throw SqlTestSettings.Refused(SqlTargetIdentityReason.MANIFEST_SCRIPT_NOT_FOUND); }
        catch { throw SqlTestSettings.Refused(SqlTargetIdentityReason.MANIFEST_SCRIPT_READ_FAILED); }
    }

    internal static string MetadataFingerprintSql() => File.ReadAllText(Path.Combine(RepositoryRoot(),
        "docs/v2.2/backend/sql-acceptance/03_SCHEMA_FINGERPRINT_SELECT.sql")).Trim();
}

/// <summary>Skips before fixture construction; setting only a connection string never enables a SQL connection.</summary>
public sealed class SqlServerReadFactAttribute : FactAttribute
{
    public SqlServerReadFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(SqlTestSettings.ModeVariable) is not (SqlTestSettings.ReadMode or SqlTestSettings.WriteMode))
            Skip = "BLOCKED: no explicitly approved isolated SQL Server test target. No SQL connection or write was attempted.";
    }
}

public sealed class SqlServerWriteFactAttribute : FactAttribute
{
    public SqlServerWriteFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(SqlTestSettings.ModeVariable) != SqlTestSettings.WriteMode)
            Skip = "BLOCKED: isolated SQL write tests require a separately approved disposable database. No SQL connection or write was attempted.";
    }
}
