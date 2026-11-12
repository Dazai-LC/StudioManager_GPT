using System.Data;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.Infrastructure;

namespace StudioManager.SqlIntegrationTests;

/// <summary>
/// Writes are possible only after explicit isolated-write opt-in, verified target markers, and an empty business schema.
/// Owns every inserted identity and deletes only rows attributable to its newly created test actor. Never creates/drops schema.
/// </summary>
internal sealed class SqlMutationFixture : IAsyncDisposable
{
    private readonly VerifiedSqlTestDatabase _database;
    private bool _ownsActor;
    internal const decimal HistoricalPrice = 100m;
    internal const decimal CurrentCatalogPrice = 200m;
    internal const string HistoricalPackageName = "P02 historical agreed package";
    internal const string InitialPassword = "P02-Fixture-Only-9!";
    internal const string AccountName = "phoenix-p02-fixture";
    internal string InitialHash { get; private set; } = "";
    internal int ActorId { get; private set; }
    internal int PhotographerId { get; private set; }
    internal int PackageId { get; private set; }
    internal int ReplacementPackageId { get; private set; }
    internal int RoomId { get; private set; }
    internal long CustomerId { get; private set; }
    internal long BookingId { get; private set; }
    internal SqlStudioRepository Repository { get; }
    internal AppFacade Application { get; }
    internal UserSession Actor { get; private set; } = null!;

    private SqlMutationFixture(VerifiedSqlTestDatabase database)
    {
        _database = database;
        Repository = new SqlStudioRepository(database.ConnectionString);
        Application = new AppFacade(Repository, new Pbkdf2PasswordHasher(), new SystemClock());
    }

    internal static async Task<SqlMutationFixture> CreateAsync()
    {
        var fixture = new SqlMutationFixture(await VerifiedSqlTestDatabase.OpenAsync(requireWrites: true));
        try
        {
            await fixture.SeedAsync();
            var login = await fixture.Application.Auth.LoginAsync(AccountName, InitialPassword);
            if (!login.Success || login.Data is null) throw new InvalidOperationException("The disposable fixture account could not authenticate.");
            fixture.Actor = login.Data;
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    private async Task SeedAsync()
    {
        InitialHash = new Pbkdf2PasswordHasher().Hash(InitialPassword);
        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        const string ensureEmpty = """
            IF EXISTS(SELECT 1 FROM dbo.NhanVien) OR EXISTS(SELECT 1 FROM dbo.TaiKhoan)
               OR EXISTS(SELECT 1 FROM dbo.KhachHang) OR EXISTS(SELECT 1 FROM dbo.GoiChup)
               OR EXISTS(SELECT 1 FROM dbo.DichVu) OR EXISTS(SELECT 1 FROM dbo.PhongChup)
               OR EXISTS(SELECT 1 FROM dbo.TaiNguyen) OR EXISTS(SELECT 1 FROM dbo.LichChup)
               OR EXISTS(SELECT 1 FROM dbo.LichChupDichVu) OR EXISTS(SELECT 1 FROM dbo.PhanCongTaiNguyen)
               OR EXISTS(SELECT 1 FROM dbo.ThanhToan) OR EXISTS(SELECT 1 FROM dbo.HoanTien)
               OR EXISTS(SELECT 1 FROM dbo.NhatKyHeThong) OR EXISTS(SELECT 1 FROM dbo.NhatKySaoLuu)
                THROW 51000, 'Disposable SQL mutation tests require an empty business schema. No data will be reset.', 1;
            """;
        await using (var empty = new SqlCommand(ensureEmpty, connection, transaction) { CommandTimeout = 10 })
            await empty.ExecuteNonQueryAsync();

        const string seed = """
            INSERT dbo.TaiKhoan(TenDangNhap,MatKhauHash,VaiTro,TrangThai,PhaiDoiMatKhau)
                VALUES(@Username,@Hash,'QUAN_TRI_VIEN','HOAT_DONG',0);
            DECLARE @Actor int=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.NhanVien(MaNhanVien,HoTen,SoDienThoai,ChucVu,TrangThai)
                VALUES('P02-NV',N'P02 test photographer','0900000000','NHIEP_ANH_GIA','DANG_LAM');
            DECLARE @Photographer int=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.KhachHang(MaKhachHang,HoTen,SoDienThoai) VALUES('P02-KH',N'P02 test customer','0900000001');
            DECLARE @Customer bigint=CONVERT(bigint,SCOPE_IDENTITY());
            INSERT dbo.GoiChup(MaGoiChup,TenGoiChup,GiaGoi,ThoiLuongPhut)
                VALUES('P02-GC',N'P02 current catalog package',@CurrentPrice,120);
            DECLARE @Package int=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.GoiChup(MaGoiChup,TenGoiChup,GiaGoi,ThoiLuongPhut)
                VALUES('P02-GC-NEW',N'P02 replacement package',250,120);
            DECLARE @ReplacementPackage int=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.PhongChup(MaPhong,TenPhong) VALUES('P02-P',N'P02 test room');
            DECLARE @Room int=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LichChup(MaLichChup,KhachHangId,GoiChupId,TenGoiChot,GiaGoiChot,BatDau,KetThuc,
                NhiepAnhGiaId,PhongChupId,TrangThai,CreatedBy,UpdatedBy)
                VALUES('P02-LC',@Customer,@Package,@HistoricalName,@HistoricalPrice,'2099-01-01T08:00:00','2099-01-01T10:00:00',
                    @Photographer,@Room,'DA_DAT_LICH',@Actor,@Actor);
            SELECT @Actor,@Photographer,@Customer,@Package,@Room,CONVERT(bigint,SCOPE_IDENTITY()),@ReplacementPackage;
            """;
        await using (var command = new SqlCommand(seed, connection, transaction) { CommandTimeout = 10 })
        {
            command.Parameters.AddWithValue("@Username", AccountName);
            command.Parameters.AddWithValue("@Hash", InitialHash);
            command.Parameters.AddWithValue("@CurrentPrice", CurrentCatalogPrice);
            command.Parameters.AddWithValue("@HistoricalName", HistoricalPackageName);
            command.Parameters.AddWithValue("@HistoricalPrice", HistoricalPrice);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new InvalidOperationException("Disposable fixture setup returned no identities.");
            ActorId = reader.GetInt32(0);
            PhotographerId = reader.GetInt32(1);
            CustomerId = reader.GetInt64(2);
            PackageId = reader.GetInt32(3);
            RoomId = reader.GetInt32(4);
            BookingId = reader.GetInt64(5);
            ReplacementPackageId = reader.GetInt32(6);
        }
        await transaction.CommitAsync();
        _ownsActor = true;
    }

    internal BookingInput BookingInput(DateTime start) => new(CustomerId, PackageId, start, start.AddHours(2), PhotographerId, RoomId, "P02 isolated test");

    internal async Task LockActorAsync()
    {
        await using var connection = await _database.OpenConnectionAsync();
        await using var command = new SqlCommand("UPDATE dbo.TaiKhoan SET TrangThai='BI_KHOA' WHERE TaiKhoanId=@Id AND TenDangNhap=@Name", connection);
        command.Parameters.AddWithValue("@Id", ActorId);
        command.Parameters.AddWithValue("@Name", AccountName);
        if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Disposable test actor could not be locked.");
    }

    internal async Task<long> CountAsync(string kind)
    {
        var sql = kind switch
        {
            "payments" => "SELECT COUNT_BIG(*) FROM dbo.ThanhToan WHERE TaiKhoanThucHienId=@Actor",
            "bookings" => "SELECT COUNT_BIG(*) FROM dbo.LichChup WHERE CreatedBy=@Actor",
            "payment-audits" => "SELECT COUNT_BIG(*) FROM dbo.NhatKyHeThong WHERE TaiKhoanId=@Actor AND HanhDong='THU_TIEN'",
            "create-audits" => "SELECT COUNT_BIG(*) FROM dbo.NhatKyHeThong WHERE TaiKhoanId=@Actor AND HanhDong='TAO_LICH'",
            "package-audits" => "SELECT COUNT_BIG(*) FROM dbo.NhatKyHeThong WHERE TaiKhoanId=@Actor AND HanhDong='DOI_GOI_CHUP'",
            "password-audits" => "SELECT COUNT_BIG(*) FROM dbo.NhatKyHeThong WHERE TaiKhoanId=@Actor AND HanhDong='DOI_MAT_KHAU'",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        await using var connection = await _database.OpenConnectionAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 10 };
        command.Parameters.AddWithValue("@Actor", ActorId);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (!_ownsActor) return;
        // Recheck all markers immediately before cleanup; do not accept a database switched underneath the test.
        await _database.RevalidateAsync(requireWrites: true);
        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        const string cleanup = """
            IF NOT EXISTS(SELECT 1 FROM dbo.TaiKhoan WHERE TaiKhoanId=@Actor AND TenDangNhap=@Name)
                THROW 51001, 'Disposable fixture ownership changed; cleanup refused.', 1;
            DELETE dbo.ThanhToan WHERE TaiKhoanThucHienId=@Actor;
            DELETE dbo.HoanTien WHERE TaiKhoanThucHienId=@Actor;
            DELETE dbo.PhanCongTaiNguyen WHERE CreatedBy=@Actor;
            DELETE dbo.LichChupDichVu WHERE CreatedBy=@Actor;
            DELETE dbo.LichChup WHERE CreatedBy=@Actor;
            DELETE dbo.NhatKyHeThong WHERE TaiKhoanId=@Actor;
            DELETE dbo.NhatKySaoLuu WHERE TaiKhoanId=@Actor;
            DELETE dbo.TaiKhoan WHERE TaiKhoanId=@Actor AND TenDangNhap=@Name;
            DELETE dbo.NhanVien WHERE NhanVienId=@Photographer AND MaNhanVien='P02-NV';
            DELETE dbo.KhachHang WHERE KhachHangId=@Customer AND MaKhachHang='P02-KH';
            DELETE dbo.GoiChup WHERE GoiChupId=@Package AND MaGoiChup='P02-GC';
            DELETE dbo.GoiChup WHERE GoiChupId=@ReplacementPackage AND MaGoiChup='P02-GC-NEW';
            DELETE dbo.PhongChup WHERE PhongChupId=@Room AND MaPhong='P02-P';
            """;
        await using var command = new SqlCommand(cleanup, connection, transaction) { CommandTimeout = 10 };
        command.Parameters.AddWithValue("@Actor", ActorId);
        command.Parameters.AddWithValue("@Name", AccountName);
        command.Parameters.AddWithValue("@Photographer", PhotographerId);
        command.Parameters.AddWithValue("@Customer", CustomerId);
        command.Parameters.AddWithValue("@Package", PackageId);
        command.Parameters.AddWithValue("@ReplacementPackage", ReplacementPackageId);
        command.Parameters.AddWithValue("@Room", RoomId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        _ownsActor = false;
    }
}
