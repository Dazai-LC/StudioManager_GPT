using System.Data.Common;
using System.Globalization;

namespace StudioManager.SqlIntegrationTests;

internal sealed record SqlTargetIdentityObservation(string? Database, string? Machine, string? Updateability,
    string? RunId, string? Version, string? Checksum, string? Instance, int DatabaseId, DateTime CreatedAt,
    string? MetadataFingerprint, string? Server)
{
    public override string ToString() => "SqlTargetIdentityObservation { Target = [isolated] }";

    internal void RequireWriteability(bool requireWrites)
    {
        if (requireWrites && Updateability != "READ_WRITE")
            throw SqlTestSettings.Refused(SqlTargetIdentityReason.DATABASE_NOT_WRITABLE);
    }

    internal (int Id, DateTime CreatedAt) ValidateContinuity((int Id, DateTime CreatedAt)? previous)
    {
        if (previous is { } existing)
        {
            if (existing.Id != DatabaseId) throw SqlTestSettings.Refused(SqlTargetIdentityReason.DATABASE_ID_CHANGED);
            if (existing.CreatedAt != CreatedAt) throw SqlTestSettings.Refused(SqlTargetIdentityReason.DATABASE_CREATED_AT_CHANGED);
        }
        return (DatabaseId, CreatedAt);
    }
}

internal static class SqlTargetIdentityReader
{
    // Eleven fixed ordinals; DatabaseId is projected as text to avoid provider-specific integer metadata.
    internal const string Query = """
        SELECT DB_NAME() AS DatabaseName, CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')) AS MachineName,
               CONVERT(nvarchar(128),DATABASEPROPERTYEX(DB_NAME(),'Updateability')) AS Updateability,
               CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.DisposableTestRunId')) AS RunId,
               CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.SchemaVersion')) AS SchemaVersion,
               CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.SchemaScriptsSha256')) AS SourceChecksum,
               CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) AS InstanceName, CONVERT(nvarchar(11), DB_ID()) AS DatabaseId,
               (SELECT create_date FROM sys.databases WHERE database_id=DB_ID()) AS CreatedAt,
               CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.SchemaMetadataSha256')) AS MetadataFingerprint,
               CONVERT(nvarchar(128),@@SERVERNAME) AS ServerName;
        """;

    internal static async Task<SqlTargetIdentityObservation> ReadAsync(DbDataReader reader)
    {
        if (!await SqlTestDiagnostics.TargetIdentityAsync(SqlTargetIdentityReason.READER_READ_FAILED, () => reader.ReadAsync()))
            throw SqlTestSettings.Refused(SqlTargetIdentityReason.READER_EMPTY);

        SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.READER_LAYOUT_INVALID, () =>
        {
            string[] names = ["DatabaseName", "MachineName", "Updateability", "RunId", "SchemaVersion", "SourceChecksum",
                "InstanceName", "DatabaseId", "CreatedAt", "MetadataFingerprint", "ServerName"];
            if (reader.FieldCount != names.Length) throw SqlTestSettings.Refused(SqlTargetIdentityReason.READER_LAYOUT_INVALID);
            for (var index = 0; index < names.Length; index++)
                if (reader.GetName(index) != names[index]) throw SqlTestSettings.Refused(SqlTargetIdentityReason.READER_LAYOUT_INVALID);
            return true;
        });

        string? Text(int ordinal, SqlTargetIdentityReason reason) => SqlTestDiagnostics.TargetIdentity(reason,
            () => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal));

        return new(Text(0, SqlTargetIdentityReason.READ_DATABASE_FAILED), Text(1, SqlTargetIdentityReason.READ_MACHINE_FAILED),
            Text(2, SqlTargetIdentityReason.READ_UPDATEABILITY_FAILED), Text(3, SqlTargetIdentityReason.READ_RUN_ID_FAILED),
            Text(4, SqlTargetIdentityReason.READ_VERSION_FAILED), Text(5, SqlTargetIdentityReason.READ_CHECKSUM_FAILED),
            Text(6, SqlTargetIdentityReason.READ_INSTANCE_FAILED),
            ReadDatabaseId(reader),
            SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.READ_CREATED_AT_FAILED, () => reader.GetDateTime(8)),
            Text(9, SqlTargetIdentityReason.READ_METADATA_FINGERPRINT_FAILED), Text(10, SqlTargetIdentityReason.READ_SERVER_FAILED));
    }

    internal static int ReadDatabaseId(DbDataReader reader)
    {
        const int ordinal = 7;
        var invalid = SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.READ_DATABASE_ID_STATE_CHECK_FAILED,
            () => reader.IsClosed || reader.FieldCount <= ordinal);
        if (invalid) throw SqlTestSettings.Refused(SqlTargetIdentityReason.READ_DATABASE_ID_READER_INVALID);

        var isNull = SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.READ_DATABASE_ID_NULL_CHECK_FAILED,
            () => reader.IsDBNull(ordinal));
        if (isNull) throw SqlTestSettings.Refused(SqlTargetIdentityReason.READ_DATABASE_ID_SQL_NULL);

        var text = SqlTestDiagnostics.TargetIdentity(SqlTargetIdentityReason.READ_DATABASE_ID_FAILED,
            () => reader.GetString(ordinal));
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var databaseId) || databaseId <= 0 ||
            text!.Any(character => character is < '0' or > '9'))
            throw SqlTestSettings.Refused(SqlTargetIdentityReason.READ_DATABASE_ID_FAILED);
        return databaseId;
    }
}
