using Microsoft.Data.SqlClient;

namespace StudioManager.SqlIntegrationTests;

internal sealed class VerifiedSqlTestDatabase
{
    private readonly SqlTestSettings _settings;
    private (int Id, DateTime CreatedAt)? _observedDatabase;
    private VerifiedSqlTestDatabase(SqlTestSettings settings) => _settings = settings;

    internal string ConnectionString => _settings.ConnectionString;
    internal string RunId => _settings.RunId;

    internal static async Task<VerifiedSqlTestDatabase> OpenAsync(bool requireWrites = false)
    {
        var settings = SqlTestDiagnostics.TargetIdentity(() =>
        {
            var configured = SqlTestSettings.FromEnvironment();
            if (requireWrites && configured.Mode != SqlTestMode.IsolatedWrites)
                throw SqlTestSettings.Refused(SqlTargetIdentityReason.WRITE_MODE_REQUIRED);
            return configured;
        });
        var database = new VerifiedSqlTestDatabase(settings);
        await database.RevalidateAsync(requireWrites);
        return database;
    }

    internal async Task RevalidateAsync(bool requireWrites = false)
    {
        if (requireWrites && _settings.Mode != SqlTestMode.IsolatedWrites)
            throw SqlTestSettings.Refused(SqlTargetIdentityReason.WRITE_MODE_REQUIRED);
        await using var connection = await OpenConnectionAsync();
    }

    private async Task<string?> ValidateTargetIdentityAsync(SqlConnection connection)
    {
        var settings = _settings;
        // All SQL here is bounded metadata SELECTs. Resource failures have their own safe reasons.
        var observed = await SqlTestDiagnostics.TargetResourceAsync(
            SqlTargetIdentityReason.QUERY_EXECUTION_FAILED, SqlTargetIdentityReason.COMMAND_DISPOSE_FAILED,
            () => Task.FromResult(new SqlCommand(SqlTargetIdentityReader.Query, connection) { CommandTimeout = 10 }),
            command => SqlTestDiagnostics.TargetResourceAsync(
                SqlTargetIdentityReason.QUERY_EXECUTION_FAILED, SqlTargetIdentityReason.READER_DISPOSE_FAILED,
                () => command.ExecuteReaderAsync(), reader => SqlTargetIdentityReader.ReadAsync(reader)));

        observed.RequireWriteability(settings.Mode == SqlTestMode.IsolatedWrites);
        settings.ValidateObservedTarget(observed.Database, observed.Machine, observed.Instance, observed.Server,
            observed.RunId, observed.Version, observed.Checksum);
        _observedDatabase = observed.ValidateContinuity(_observedDatabase);
        return observed.MetadataFingerprint;
    }

    private static async Task ValidatePermissionsAsync(SqlConnection connection, bool requireWrites)
    {
        const string permissions = """
            SELECT
              CASE WHEN NOT EXISTS(SELECT 1 FROM sys.fn_my_permissions(NULL,'SERVER')
                    WHERE permission_name NOT IN('CONNECT SQL','VIEW ANY DATABASE'))
                AND EXISTS(SELECT 1 FROM sys.fn_my_permissions(NULL,'SERVER') WHERE permission_name='CONNECT SQL')
                AND EXISTS(SELECT 1 FROM sys.fn_my_permissions(NULL,'SERVER') WHERE permission_name='VIEW ANY DATABASE')
                AND NOT EXISTS(SELECT 1 FROM sys.fn_my_permissions(NULL,'DATABASE')
                    WHERE permission_name NOT IN('CONNECT','VIEW DEFINITION','SELECT','INSERT','UPDATE','DELETE',
                      'VIEW SECURITY DEFINITION','VIEW PERFORMANCE DEFINITION',
                      'VIEW ANY COLUMN ENCRYPTION KEY DEFINITION','VIEW ANY COLUMN MASTER KEY DEFINITION'))
                AND COALESCE(IS_SRVROLEMEMBER('sysadmin'),1)=0
                AND COALESCE(IS_MEMBER('db_owner'),1)=0
                AND COALESCE(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL'),1)=0
                AND COALESCE(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER'),1)=0
                AND EXISTS(SELECT 1 FROM sys.databases WHERE database_id=DB_ID() AND is_trustworthy_on=0 AND is_db_chaining_on=0)
                AND COALESCE(HAS_DBACCESS(N'StudioManager'),0)=0
                AND NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id>4 AND database_id<>DB_ID() AND HAS_DBACCESS(name)=1)
              THEN 1 ELSE 0 END,
              CASE WHEN HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')=1
                AND (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)=14
                AND NOT EXISTS(SELECT 1 FROM sys.tables t WHERE t.is_ms_shipped=0 AND
                    (COALESCE(HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id))+'.'+QUOTENAME(t.name),'OBJECT','SELECT'),0)=0 OR
                     (@Writes=1 AND (COALESCE(HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id))+'.'+QUOTENAME(t.name),'OBJECT','INSERT'),0)=0 OR
                       COALESCE(HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id))+'.'+QUOTENAME(t.name),'OBJECT','UPDATE'),0)=0 OR
                       COALESCE(HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(t.schema_id))+'.'+QUOTENAME(t.name),'OBJECT','DELETE'),0)=0))))
              THEN 1 ELSE 0 END,
              IS_SRVROLEMEMBER('sysadmin'), IS_MEMBER('db_owner');
            """;
        await using (var privilege = new SqlCommand(permissions, connection) { CommandTimeout = 10 })
        {
            privilege.Parameters.AddWithValue("@Writes", requireWrites);
            await using var access = await privilege.ExecuteReaderAsync();
            if (!await access.ReadAsync()) throw SqlTestSettings.Refused();
            SqlTestSettings.ValidateObservedPermissions(
                access.IsDBNull(2) ? null : access.GetInt32(2) != 0,
                access.IsDBNull(3) ? null : access.GetInt32(3) != 0,
                access.GetInt32(0) == 1, access.GetInt32(1) == 1);
        }
    }

    private static async Task ValidateSchemaFingerprintAsync(SqlConnection connection, string? reviewedMetadataFingerprint)
    {
        if (string.IsNullOrEmpty(reviewedMetadataFingerprint)) throw SqlTestSettings.Refused();
        await using (var fingerprint = new SqlCommand(SqlSchemaManifest.MetadataFingerprintSql(), connection) { CommandTimeout = 10 })
        {
            var actual = await fingerprint.ExecuteScalarAsync() as string;
            if (!string.Equals(actual, reviewedMetadataFingerprint, StringComparison.Ordinal)) throw SqlTestSettings.Refused();
        }
    }

    private static async Task ValidateSchemaShapeAsync(SqlConnection connection)
    {
        // Do not let an independently scheduled metadata Fact be the fixture's safety gate.
        const string shape = """
            SELECT CASE WHEN
              (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)=14
              AND (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN
                ('NhanVien','TaiKhoan','KhachHang','GoiChup','DichVu','PhongChup','TaiNguyen','LichChup',
                 'LichChupDichVu','PhanCongTaiNguyen','ThanhToan','HoanTien','NhatKyHeThong','NhatKySaoLuu'))=14
              AND COL_LENGTH('dbo.LichChup','HoanThanhLuc') IS NOT NULL
              AND COL_LENGTH('dbo.LichChupDichVu','UpdatedBy') IS NOT NULL
              AND COL_LENGTH('dbo.PhanCongTaiNguyen','UpdatedAt') IS NOT NULL
              AND NOT EXISTS(SELECT 1 FROM sys.triggers WHERE is_ms_shipped=0 AND is_disabled=0)
              AND NOT EXISTS(SELECT 1 FROM sys.synonyms)
              AND NOT EXISTS(SELECT 1 FROM sys.sql_modules)
              AND NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
              AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
            THEN 1 ELSE 0 END;
            """;
        await using var schema = new SqlCommand(shape, connection) { CommandTimeout = 10 };
        if (Convert.ToInt32(await schema.ExecuteScalarAsync()) != 1) throw SqlTestSettings.Refused();
    }

    internal async Task<SqlConnection> OpenConnectionAsync()
    {
        SqlConnection? connection = null;
        string? reviewedMetadataFingerprint = null;
        var requireWrites = _settings.Mode == SqlTestMode.IsolatedWrites;
        await SqlTestDiagnostics.VerifyConnectionAsync(
            open: async () =>
            {
                connection = new SqlConnection(_settings.ConnectionString);
                await connection.OpenAsync();
            },
            targetIdentity: async () => reviewedMetadataFingerprint = await ValidateTargetIdentityAsync(connection!),
            permissions: () => ValidatePermissionsAsync(connection!, requireWrites),
            fingerprint: () => ValidateSchemaFingerprintAsync(connection!, reviewedMetadataFingerprint),
            writeSchemaShape: requireWrites ? () => ValidateSchemaShapeAsync(connection!) : null,
            disposeOnFailure: () => connection?.DisposeAsync() ?? ValueTask.CompletedTask);
        return connection!;
    }
}
