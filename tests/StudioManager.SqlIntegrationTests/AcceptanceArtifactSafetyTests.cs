using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

public sealed class AcceptanceArtifactSafetyTests
{
    [Fact]
    public void PreparedSchema_CannotSwitchDatabaseAndKeepsTheReviewedTablePayload()
    {
        var root = SqlSchemaManifest.RepositoryRoot();
        var artifact = File.ReadAllText(Path.Combine(root, "docs/v2.2/backend/sql-acceptance/01_SCHEMA_REVIEW_ONLY.sql"));
        Assert.DoesNotMatch(new Regex(@"^\s*(USE\b|GO\b|:)", RegexOptions.Multiline | RegexOptions.IgnoreCase), artifact);
        Assert.DoesNotContain("$(", artifact, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"\b(CREATE\s+DATABASE|BACKUP|RESTORE|INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.IgnoreCase), artifact);
        Assert.Contains("DECLARE @OwnerApprovedExecution bit=0;", artifact, StringComparison.Ordinal);
        Assert.True(artifact.IndexOf("IF @OwnerApprovedExecution<>1", StringComparison.Ordinal) < artifact.IndexOf("CREATE TABLE", StringComparison.Ordinal));
        var expected = File.ReadAllText(Path.Combine(root, "database/02_CreateSchema.sql")).Replace("\r\n", "\n");
        expected = Regex.Replace(expected, @"^USE StudioManager;\s*\n", "", RegexOptions.Multiline);
        expected = Regex.Replace(expected, @"^GO\s*$", "", RegexOptions.Multiline).Trim();
        var begin = artifact.IndexOf("-- BEGIN BASELINE TABLE PAYLOAD", StringComparison.Ordinal);
        begin = artifact.IndexOf('\n', begin) + 1;
        var end = artifact.IndexOf("-- END BASELINE TABLE PAYLOAD", StringComparison.Ordinal);
        Assert.Equal(expected, artifact[begin..end].Replace("\r\n", "\n").Trim());
        Assert.Contains(SqlSchemaManifest.MetadataFingerprintSql().Replace("\r\n", "\n"), artifact.Replace("\r\n", "\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void PreparedTargetAndCreateArtifact_AgreeAndDoNotReuseAnExistingDatabase()
    {
        var folder = Path.Combine(SqlSchemaManifest.RepositoryRoot(), "docs/v2.2/backend/sql-acceptance");
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "P02_SQL_TARGET.json")));
        var target = json.RootElement;
        var database = target.GetProperty("database").GetString();
        var run = target.GetProperty("runId").GetString();
        Assert.Equal("StudioManager_P02_Test_" + run, database);
        Assert.Equal(SqlSchemaManifest.ExpectedChecksum(), target.GetProperty("schemaScriptsSha256").GetString());
        Assert.Equal(SqlTestSettings.ApprovedDataSource, target.GetProperty("dataSource").GetString());
        Assert.Equal(SqlTestSettings.ApprovedMachine, target.GetProperty("machine").GetString());
        Assert.Equal(SqlTestSettings.ApprovedServerName, target.GetProperty("serverName").GetString());
        Assert.Equal(SqlTestSettings.ApprovedInstance, target.GetProperty("instance").GetString());
        Assert.Equal(SqlTestSettings.ApprovedRunId, run);
        Assert.Equal(SqlTestSettings.ApprovedDatabase, database);
        var create = File.ReadAllText(Path.Combine(folder, "00_CREATE_DATABASE_REVIEW_ONLY.sql"));
        Assert.Contains("DECLARE @OwnerApprovedExecution bit=0;", create, StringComparison.Ordinal);
        Assert.Contains($"IF DB_ID(N'{database}') IS NOT NULL THROW", create, StringComparison.Ordinal);
        var creates = Regex.Matches(create, @"CREATE DATABASE \[([^\]]+)\]");
        Assert.Single(creates);
        Assert.Equal(database, creates[0].Groups[1].Value);
        Assert.DoesNotMatch(new Regex(@"^\s*(USE\b|GO\b|:)", RegexOptions.Multiline | RegexOptions.IgnoreCase), create);
    }

    [Theory]
    [InlineData("00_INSTANCE_PREFLIGHT_READ_ONLY.sql", "master", false)]
    [InlineData("00_CREATE_DATABASE_REVIEW_ONLY.sql", "master", true)]
    [InlineData("01_SCHEMA_REVIEW_ONLY.sql", SqlTestSettings.ApprovedDatabase, true)]
    [InlineData("02_METADATA_READ_ONLY.sql", SqlTestSettings.ApprovedDatabase, false)]
    [InlineData("04_CREATE_TEST_LOGIN_REVIEW_ONLY.sql", "master", true)]
    [InlineData("05_CREATE_TEST_USER_REVIEW_ONLY.sql", SqlTestSettings.ApprovedDatabase, true)]
    public void EverySqlEntryPoint_RequiresExactIdentityAndContextWithoutBatchSwitching(string name, string context, bool mutates)
    {
        var script = File.ReadAllText(Path.Combine(SqlSchemaManifest.RepositoryRoot(), "docs/v2.2/backend/sql-acceptance", name));
        Assert.Contains("COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'", script);
        Assert.Contains("COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'", script);
        Assert.Contains("COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\\SQLEXPRESS'", script);
        Assert.Contains($"COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'{context}'", script);
        Assert.DoesNotContain("$(", script);
        Assert.DoesNotMatch(new Regex(@"^\s*(USE\b|GO\b|:)", RegexOptions.Multiline | RegexOptions.IgnoreCase), script);
        if (mutates)
        {
            Assert.Contains("DECLARE @OwnerApprovedExecution bit=0;", script);
            Assert.Contains("IF @OwnerApprovedExecution<>1 THROW", script);
        }
        else
        {
            var sql = Regex.Replace(script, @"--[^\r\n]*", "");
            // Permission names such as 'CREATE ANY DATABASE' are SELECT arguments, not statements.
            // EXEC remains forbidden, so removing string literals cannot hide executable dynamic SQL.
            sql = Regex.Replace(sql, @"N?'(?:''|[^'])*'", "''", RegexOptions.IgnoreCase);
            Assert.DoesNotMatch(new Regex(@"\b(CREATE|ALTER|DROP|INSERT|UPDATE|DELETE|MERGE|GRANT|DENY|REVOKE|EXEC|EXECUTE|BACKUP|RESTORE)\b", RegexOptions.IgnoreCase), sql);
        }
    }

    [Fact]
    public void RunnerProvisioning_GrantsOnlyReviewedDatabasePermissions()
    {
        var folder = Path.Combine(SqlSchemaManifest.RepositoryRoot(), "docs/v2.2/backend/sql-acceptance");
        var login = File.ReadAllText(Path.Combine(folder, "04_CREATE_TEST_LOGIN_REVIEW_ONLY.sql"));
        var user = File.ReadAllText(Path.Combine(folder, "05_CREATE_TEST_USER_REVIEW_ONLY.sql"));
        var sql = Regex.Replace(login + "\n" + user, @"--[^\r\n]*", "");
        Assert.Contains("IF SUSER_ID(N'MSI\\PhoenixP02Tests') IS NOT NULL THROW", login);
        Assert.Contains("IF USER_ID(N'MSI\\PhoenixP02Tests') IS NOT NULL THROW", user);
        Assert.Contains("CREATE LOGIN [MSI\\PhoenixP02Tests] FROM WINDOWS", login);
        Assert.Contains($"WITH DEFAULT_DATABASE=[{SqlTestSettings.ApprovedDatabase}]", login);
        Assert.DoesNotMatch(new Regex(@"\b(ALTER\s+SERVER\s+ROLE|ALTER\s+ROLE|sp_addsrvrolemember|sp_addrolemember|WITH\s+GRANT\s+OPTION)\b", RegexOptions.IgnoreCase), sql);
        var grants = Regex.Matches(sql, @"\bGRANT\s+[^;]+;", RegexOptions.IgnoreCase).Select(m => m.Value).ToArray();
        Assert.Equal(new[] {
            "GRANT CONNECT, VIEW DEFINITION TO [MSI\\PhoenixP02Tests];",
            "GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [MSI\\PhoenixP02Tests];"
        }, grants);
    }
}
