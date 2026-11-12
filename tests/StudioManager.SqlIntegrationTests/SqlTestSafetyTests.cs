using Xunit;

namespace StudioManager.SqlIntegrationTests;

public sealed class SqlTestSafetyTests
{
    private const string RunId = "7d70a08ac15349acbd207f82c8349f6e";
    private const string Valid = @"Server=localhost\SQLEXPRESS;Database=StudioManager_P02_Test_" + RunId + ";Integrated Security=true;Encrypt=true";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("WRITE")]
    public void MissingOrUnrecognizedApproval_RejectsWithoutOpeningSql(string? mode)
        => Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(Valid, RunId, mode));

    [Theory]
    [InlineData("StudioManager")]
    [InlineData("master")]
    [InlineData("StudioManager_P02_Test_00000000000000000000000000000000")]
    [InlineData("StudioManager_P02_Test_")]
    public void WrongDatabaseOrRunIdentity_IsRejected(string database)
        => Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(
            $"Server={SqlTestSettings.ApprovedDataSource};Database={database};Integrated Security=true", RunId, SqlTestSettings.ReadMode));

    [Theory]
    [InlineData("Server=customer-machine;Integrated Security=true")]
    [InlineData("Server=localhost.example.com;Integrated Security=true")]
    [InlineData("Server=localhost\\SQLEXPRESS;User Id=test;Password=not-a-real-secret")]
    [InlineData("Server=localhost\\SQLEXPRESS;Integrated Security=true;AttachDBFilename=C:\\test.mdf")]
    [InlineData("Server=localhost\\SQLEXPRESS;Integrated Security=true;Failover Partner=other")]
    public void UnsafeConnectionCapabilities_AreRejected(string prefix)
        => Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(
            prefix + ";Database=StudioManager_P02_Test_" + RunId, RunId, SqlTestSettings.WriteMode));

    [Fact]
    public void ApprovedLocalReadTarget_ParsesButDoesNotOpenConnection()
    {
        var settings = SqlTestSettings.Parse(Valid, RunId, SqlTestSettings.ReadMode);
        Assert.Equal(SqlTestMode.ReadOnly, settings.Mode);
        Assert.Equal("StudioManager_P02_Test_" + RunId, settings.DatabaseName);
    }

    [Fact]
    public void InvalidConnectionConfiguration_DoesNotEchoItsContents()
    {
        const string privateValue = "do-not-echo-connection-material";
        var error = Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(privateValue, RunId, SqlTestSettings.ReadMode));
        Assert.DoesNotContain(privateValue, error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void VersionedSchemaManifest_HasStableSha256Shape()
        => Assert.Matches("^[A-F0-9]{64}$", SqlSchemaManifest.ExpectedChecksum());

    [Theory]
    [InlineData("localhost")]
    [InlineData(@"localhost\PHOENIXP02")]
    [InlineData(@"localhost\MSSQLSERVER")]
    [InlineData(@"MSI\SQLEXPRESS")]
    [InlineData(@"localhost\OtherInstance")]
    [InlineData(@"(localdb)\MSSQLLocalDB")]
    [InlineData(@"127.0.0.1\SQLEXPRESS")]
    public void UnapprovedInstanceOrAlias_IsRejectedBeforeConnecting(string server)
        => Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(
            $"Server={server};Database=StudioManager_P02_Test_{RunId};Integrated Security=true", RunId, SqlTestSettings.WriteMode));

    [Theory]
    [InlineData("database")]
    [InlineData("machine")]
    [InlineData("missing-machine")]
    [InlineData("instance")]
    [InlineData("default-instance")]
    [InlineData("missing-instance")]
    [InlineData("server-name")]
    [InlineData("missing-server-name")]
    [InlineData("run")]
    [InlineData("missing-run")]
    [InlineData("version")]
    [InlineData("checksum")]
    public void ObservedIdentityOrMarkersMismatch_FailsClosed(string mismatch)
    {
        var settings = SqlTestSettings.Parse(Valid, RunId, SqlTestSettings.WriteMode);
        Assert.Throws<SqlTestDiagnosticException>(() => settings.ValidateObservedTarget(
            mismatch == "database" ? "StudioManager" : settings.DatabaseName,
            mismatch == "machine" ? "some-other-host" : mismatch == "missing-machine" ? null : SqlTestSettings.ApprovedMachine,
            mismatch == "instance" ? "PHOENIXP02" : mismatch == "default-instance" ? "MSSQLSERVER" : mismatch == "missing-instance" ? null : SqlTestSettings.ApprovedInstance,
            mismatch == "server-name" ? "MSI" : mismatch == "missing-server-name" ? null : SqlTestSettings.ApprovedServerName,
            mismatch == "run" ? Guid.NewGuid().ToString("N") : mismatch == "missing-run" ? null : RunId,
            mismatch == "version" ? "unknown" : SqlTestSettings.SchemaVersion,
            mismatch == "checksum" ? "unreviewed" : SqlSchemaManifest.ExpectedChecksum()));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(null, true)]
    [InlineData(true, null)]
    public void ElevatedMissingOrUnknownPermissions_AreRejected(bool? leastPrivilege, bool? requiredAccess)
        => Assert.Throws<InvalidOperationException>(() => SqlTestSettings.ValidateObservedPermissions(false, false, leastPrivilege, requiredAccess));

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(null, false)]
    [InlineData(false, null)]
    public void SysadminDatabaseOwnerOrUnknownRole_RejectsEvenIfOtherChecksPass(bool? sysadmin, bool? databaseOwner)
        => Assert.Throws<InvalidOperationException>(() => SqlTestSettings.ValidateObservedPermissions(sysadmin, databaseOwner, true, true));

    [Fact]
    public void DifferentRunWithMatchingDatabaseName_StillRequiresAnotherReview()
    {
        const string otherRun = "4aa91e5c2c1745929af52db934aab96f";
        Assert.Throws<SqlTestDiagnosticException>(() => SqlTestSettings.Parse(
            $"Server={SqlTestSettings.ApprovedDataSource};Database=StudioManager_P02_Test_{otherRun};Integrated Security=true",
            otherRun, SqlTestSettings.WriteMode));
    }

    [Fact]
    public void ExpectedObservedIdentityAndLeastPrivilege_AreAcceptedOffline()
    {
        var settings = SqlTestSettings.Parse(Valid, RunId, SqlTestSettings.WriteMode);
        settings.ValidateObservedTarget(settings.DatabaseName, SqlTestSettings.ApprovedMachine, SqlTestSettings.ApprovedInstance, SqlTestSettings.ApprovedServerName,
            RunId, SqlTestSettings.SchemaVersion, SqlSchemaManifest.ExpectedChecksum());
        SqlTestSettings.ValidateObservedPermissions(false, false, true, true);
    }
}
