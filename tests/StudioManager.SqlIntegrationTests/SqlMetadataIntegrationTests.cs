using Microsoft.Data.SqlClient;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

public sealed class SqlMetadataIntegrationTests
{
    [SqlServerReadFact]
    public async Task ReviewedSchema_HasRequiredTablesColumnsAndConstraintIndexes()
    {
        var database = await VerifiedSqlTestDatabase.OpenAsync();
        await using var connection = await database.OpenConnectionAsync();
        await SqlTestDiagnostics.RunAsync(SqlTestDiagnosticStage.SCHEMA_SHAPE, () => VerifySchemaAsync(connection));
    }

    private static async Task VerifySchemaAsync(SqlConnection connection)
    {
        const string query = """
            SELECT
              (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN
                ('NhanVien','TaiKhoan','KhachHang','GoiChup','DichVu','PhongChup','TaiNguyen','LichChup',
                 'LichChupDichVu','PhanCongTaiNguyen','ThanhToan','HoanTien','NhatKyHeThong','NhatKySaoLuu')),
              COL_LENGTH('dbo.LichChup','HoanThanhLuc'),
              COL_LENGTH('dbo.LichChupDichVu','UpdatedBy'),
              COL_LENGTH('dbo.PhanCongTaiNguyen','UpdatedAt'),
              (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.LichChup') AND name IN
                ('IX_LichChup_AnhGia','IX_LichChup_Phong','IX_LichChup_TrangThai')),
              (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.LichChup') AND name='CK_LichChup_ThoiGian' AND is_disabled=0 AND is_not_trusted=0);
            """;
        await using var command = new SqlCommand(query, connection) { CommandTimeout = 10 };
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(14, reader.GetInt32(0));
        Assert.False(reader.IsDBNull(1));
        Assert.False(reader.IsDBNull(2));
        Assert.False(reader.IsDBNull(3));
        Assert.Equal(3, reader.GetInt32(4));
        Assert.Equal(1, reader.GetInt32(5));
    }
}
