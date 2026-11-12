-- REVIEW ONLY. One guarded batch; do not add GO or SQLCMD includes.
-- Fresh test schema only; not an upgrade of an existing database.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @OwnerApprovedExecution bit=0;
IF @OwnerApprovedExecution<>1 THROW 51010, 'Execution not approved.', 1;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'
 OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'
 OR COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\SQLEXPRESS'
 THROW 51010, 'Refused: exact MSI/SQLEXPRESS server identity is required.', 1;
IF COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e' THROW 51010, 'Wrong test database.', 1;
IF CURRENT_USER<>N'dbo' THROW 51010, 'Use the reviewed provisioning identity; not the test runner.', 1;
IF EXISTS(SELECT 1 FROM sys.objects WHERE is_ms_shipped=0) THROW 51010, 'Fresh empty schema required. No reset is performed.', 1;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
BEGIN TRANSACTION;
-- BEGIN BASELINE TABLE PAYLOAD (02_CreateSchema.sql without database switching)
CREATE TABLE NhanVien(
 NhanVienId int IDENTITY PRIMARY KEY, MaNhanVien varchar(20) NOT NULL UNIQUE, HoTen nvarchar(100) NOT NULL,
 SoDienThoai varchar(15) NOT NULL, ChucVu varchar(30) NOT NULL CHECK(ChucVu IN('TIEP_NHAN','NHIEP_ANH_GIA','KHAC')),
 TrangThai varchar(20) NOT NULL DEFAULT 'DANG_LAM' CHECK(TrangThai IN('DANG_LAM','NGUNG_LAM')), GhiChu nvarchar(500) NULL,
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE INDEX IX_NhanVien_HoTen ON NhanVien(HoTen);

CREATE TABLE TaiKhoan(
 TaiKhoanId int IDENTITY PRIMARY KEY, NhanVienId int NULL REFERENCES NhanVien(NhanVienId), TenDangNhap varchar(50) NOT NULL UNIQUE,
 MatKhauHash nvarchar(255) NOT NULL, VaiTro varchar(20) NOT NULL CHECK(VaiTro IN('QUAN_TRI_VIEN','NHAN_VIEN')),
 TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG' CHECK(TrangThai IN('HOAT_DONG','BI_KHOA')), PhaiDoiMatKhau bit NOT NULL DEFAULT 1,
 LanDangNhapCuoi datetime2(0) NULL, CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE UNIQUE INDEX UX_TaiKhoan_NhanVien ON TaiKhoan(NhanVienId) WHERE NhanVienId IS NOT NULL;

CREATE TABLE KhachHang(
 KhachHangId bigint IDENTITY PRIMARY KEY, MaKhachHang varchar(20) NOT NULL UNIQUE, HoTen nvarchar(100) NOT NULL,
 SoDienThoai varchar(15) NOT NULL, DiaChi nvarchar(255) NULL, Email varchar(254) NULL, GhiChu nvarchar(500) NULL,
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE INDEX IX_KhachHang_HoTen ON KhachHang(HoTen); CREATE INDEX IX_KhachHang_SDT ON KhachHang(SoDienThoai);

CREATE TABLE GoiChup(
 GoiChupId int IDENTITY PRIMARY KEY, MaGoiChup varchar(20) NOT NULL UNIQUE, TenGoiChup nvarchar(150) NOT NULL,
 MoTa nvarchar(1000) NULL, GiaGoi decimal(18,2) NOT NULL CHECK(GiaGoi>=0), ThoiLuongPhut int NOT NULL CHECK(ThoiLuongPhut>0),
 TrangThai varchar(20) NOT NULL DEFAULT 'DANG_AP_DUNG' CHECK(TrangThai IN('DANG_AP_DUNG','NGUNG_AP_DUNG')),
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE DichVu(
 DichVuId int IDENTITY PRIMARY KEY, MaDichVu varchar(20) NOT NULL UNIQUE, TenDichVu nvarchar(150) NOT NULL,
 DonViTinh nvarchar(30) NOT NULL, DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0), MoTa nvarchar(1000) NULL,
 TrangThai varchar(20) NOT NULL DEFAULT 'DANG_CUNG_CAP' CHECK(TrangThai IN('DANG_CUNG_CAP','NGUNG_CUNG_CAP')),
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE PhongChup(
 PhongChupId int IDENTITY PRIMARY KEY, MaPhong varchar(20) NOT NULL UNIQUE, TenPhong nvarchar(100) NOT NULL, MoTa nvarchar(500) NULL,
 TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG' CHECK(TrangThai IN('HOAT_DONG','NGUNG_SU_DUNG')),
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE TaiNguyen(
 TaiNguyenId int IDENTITY PRIMARY KEY, MaTaiNguyen varchar(20) NOT NULL UNIQUE, TenTaiNguyen nvarchar(150) NOT NULL,
 LoaiTaiNguyen varchar(20) NOT NULL CHECK(LoaiTaiNguyen IN('THIET_BI','TRANG_PHUC')), TongSoLuong int NOT NULL CHECK(TongSoLuong>0),
 DichVuThueId int NULL REFERENCES DichVu(DichVuId), TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG' CHECK(TrangThai IN('HOAT_DONG','NGUNG_SU_DUNG')),
 GhiChu nvarchar(500) NULL, CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());

CREATE TABLE LichChup(
 LichChupId bigint IDENTITY PRIMARY KEY, MaLichChup varchar(25) NOT NULL UNIQUE, KhachHangId bigint NOT NULL REFERENCES KhachHang(KhachHangId),
 GoiChupId int NOT NULL REFERENCES GoiChup(GoiChupId), TenGoiChot nvarchar(150) NOT NULL, GiaGoiChot decimal(18,2) NOT NULL CHECK(GiaGoiChot>=0),
 BatDau datetime2(0) NOT NULL, KetThuc datetime2(0) NOT NULL, NhiepAnhGiaId int NOT NULL REFERENCES NhanVien(NhanVienId),
 PhongChupId int NOT NULL REFERENCES PhongChup(PhongChupId), TrangThai varchar(30) NOT NULL CHECK(TrangThai IN('DA_DAT_LICH','DA_CHUP','DANG_CHINH_SUA','CHO_GIAO_ANH','DA_GIAO_ANH','HOAN_THANH','DA_HUY')),
 TienGiam decimal(18,2) NOT NULL DEFAULT 0 CHECK(TienGiam>=0), LyDoGiam nvarchar(500) NULL, GhiChu nvarchar(1000) NULL,
 LyDoHuy nvarchar(500) NULL, HuyBoiTaiKhoanId int NULL REFERENCES TaiKhoan(TaiKhoanId), HuyLuc datetime2(0) NULL, HoanThanhLuc datetime2(0) NULL,
 CreatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT CK_LichChup_ThoiGian CHECK(KetThuc>BatDau), CONSTRAINT CK_LichChup_LyDoGiam CHECK(TienGiam=0 OR LyDoGiam IS NOT NULL));
CREATE INDEX IX_LichChup_ThoiGian ON LichChup(BatDau,KetThuc); CREATE INDEX IX_LichChup_AnhGia ON LichChup(NhiepAnhGiaId,BatDau);
CREATE INDEX IX_LichChup_Phong ON LichChup(PhongChupId,BatDau); CREATE INDEX IX_LichChup_TrangThai ON LichChup(TrangThai,BatDau);

CREATE TABLE LichChupDichVu(
 LichChupDichVuId bigint IDENTITY PRIMARY KEY, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId), DichVuId int NOT NULL REFERENCES DichVu(DichVuId),
 TenDichVuChot nvarchar(150) NOT NULL, DonViTinhChot nvarchar(30) NOT NULL, DonGiaChot decimal(18,2) NOT NULL CHECK(DonGiaChot>=0), SoLuong decimal(10,2) NOT NULL CHECK(SoLuong>0),
 CreatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT UX_LichChupDichVu UNIQUE(LichChupId,DichVuId));
CREATE TABLE PhanCongTaiNguyen(
 PhanCongId bigint IDENTITY PRIMARY KEY, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId), TaiNguyenId int NOT NULL REFERENCES TaiNguyen(TaiNguyenId), SoLuong int NOT NULL CHECK(SoLuong>0),
 BatDauSuDung datetime2(0) NOT NULL, KetThucSuDung datetime2(0) NOT NULL, TrangThai varchar(20) NOT NULL CHECK(TrangThai IN('DA_PHAN_CONG','DA_TRA','DA_HUY')),
 CreatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), CHECK(KetThucSuDung>BatDauSuDung));
CREATE INDEX IX_PhanCong_XungDot ON PhanCongTaiNguyen(TaiNguyenId,BatDauSuDung,KetThucSuDung,TrangThai);
CREATE TABLE ThanhToan(
 ThanhToanId bigint IDENTITY PRIMARY KEY, MaThanhToan varchar(25) NOT NULL UNIQUE, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId),
 LoaiThu varchar(20) NOT NULL CHECK(LoaiThu IN('DAT_COC','BO_SUNG','CON_LAI')), SoTien decimal(18,2) NOT NULL CHECK(SoTien>0), NgayGiaoDich datetime2(0) NOT NULL,
 TaiKhoanThucHienId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), GhiChu nvarchar(500) NULL);
CREATE INDEX IX_ThanhToan_LichNgay ON ThanhToan(LichChupId,NgayGiaoDich);
CREATE TABLE HoanTien(
 HoanTienId bigint IDENTITY PRIMARY KEY, MaHoanTien varchar(25) NOT NULL UNIQUE, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId),
 SoTien decimal(18,2) NOT NULL CHECK(SoTien>0), NgayGiaoDich datetime2(0) NOT NULL, TaiKhoanThucHienId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), LyDo nvarchar(500) NOT NULL, GhiChu nvarchar(500) NULL);
CREATE INDEX IX_HoanTien_LichNgay ON HoanTien(LichChupId,NgayGiaoDich);
CREATE TABLE NhatKyHeThong(
 NhatKyId bigint IDENTITY PRIMARY KEY, TaiKhoanId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), HanhDong varchar(50) NOT NULL,
 LoaiDoiTuong varchar(50) NOT NULL, DoiTuongId varchar(50) NOT NULL, GiaTriCu nvarchar(max) NULL, GiaTriMoi nvarchar(max) NULL, LyDo nvarchar(500) NULL, ThoiDiem datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE INDEX IX_NhatKy_TaiKhoan ON NhatKyHeThong(TaiKhoanId,ThoiDiem); CREATE INDEX IX_NhatKy_DoiTuong ON NhatKyHeThong(LoaiDoiTuong,DoiTuongId,ThoiDiem);
CREATE TABLE NhatKySaoLuu(
 NhatKySaoLuuId bigint IDENTITY PRIMARY KEY, LoaiThaoTac varchar(20) NOT NULL CHECK(LoaiThaoTac IN('SAO_LUU','PHUC_HOI')),
 DuongDanTep nvarchar(1000) NOT NULL, TrangThai varchar(20) NOT NULL CHECK(TrangThai IN('THANH_CONG','THAT_BAI')), ThongBao nvarchar(1000) NULL,
 TaiKhoanId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), ThoiDiem datetime2(0) NOT NULL DEFAULT SYSDATETIME());
-- END BASELINE TABLE PAYLOAD
-- Fresh-schema indexes from 05; no legacy-data backfill is required.
CREATE INDEX IX_LichChup_HoanThanhLuc ON LichChup(HoanThanhLuc) WHERE HoanThanhLuc IS NOT NULL;
CREATE INDEX IX_ThanhToan_NgayGiaoDich ON ThanhToan(NgayGiaoDich);
CREATE INDEX IX_HoanTien_NgayGiaoDich ON HoanTien(NgayGiaoDich);
EXEC sys.sp_addextendedproperty @name=N'Phoenix.DisposableTestRunId',@value=N'7d70a08ac15349acbd207f82c8349f6e';
EXEC sys.sp_addextendedproperty @name=N'Phoenix.SchemaVersion',@value=N'baseline-c2c3c23-spec2.1';
EXEC sys.sp_addextendedproperty @name=N'Phoenix.SchemaScriptsSha256',@value=N'B8A556B123DDB93B742FDA93643AC70CB4D11E8F62E9AE30BAF37758900B0EF3';
DECLARE @MetadataFingerprint varchar(64)=(
SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(CAST(N'' AS nvarchar(max)),
 CONVERT(nvarchar(max),(SELECT s.name AS [schema],t.name AS [table],c.column_id,c.name,ty.name AS [type],
 c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.collation_name,dc.definition AS [default],cc.definition AS [computed]
 FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id
 JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
 LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
 WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name,c.column_id FOR XML RAW('column'),TYPE)),
 CONVERT(nvarchar(max),(SELECT OBJECT_SCHEMA_NAME(i.object_id) AS [schema],OBJECT_NAME(i.object_id) AS [table],i.name,
 i.type_desc,i.is_unique,i.is_primary_key,i.is_unique_constraint,i.is_disabled,i.filter_definition,
 c.name AS [column],ic.key_ordinal,ic.is_included_column,ic.is_descending_key
 FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id
 JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
 JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
 WHERE t.is_ms_shipped=0 ORDER BY [schema],[table],i.name,ic.index_column_id FOR XML RAW('index'),TYPE)),
 CONVERT(nvarchar(max),(SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) AS [schema],OBJECT_NAME(f.parent_object_id) AS [table],f.name,
 pc.name AS [parentColumn],OBJECT_SCHEMA_NAME(f.referenced_object_id) AS [referencedSchema],OBJECT_NAME(f.referenced_object_id) AS [referencedTable],
 rc.name AS [referencedColumn],f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted
 FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
 JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id
 JOIN sys.columns rc ON rc.object_id=fc.referenced_object_id AND rc.column_id=fc.referenced_column_id
 ORDER BY [schema],[table],f.name,fc.constraint_column_id FOR XML RAW('foreignKey'),TYPE)),
 CONVERT(nvarchar(max),(SELECT OBJECT_SCHEMA_NAME(parent_object_id) AS [schema],OBJECT_NAME(parent_object_id) AS [table],name,definition,is_disabled,is_not_trusted
 FROM sys.check_constraints ORDER BY [schema],[table],name FOR XML RAW('check'),TYPE))
)),2)
);
EXEC sys.sp_addextendedproperty @name=N'Phoenix.SchemaMetadataSha256',@value=@MetadataFingerprint;
COMMIT;
END TRY
BEGIN CATCH
IF XACT_STATE()<>0 ROLLBACK;
THROW 51010, 'Test schema setup failed; inspect the isolated database. No cleanup was attempted.', 1;
END CATCH;
