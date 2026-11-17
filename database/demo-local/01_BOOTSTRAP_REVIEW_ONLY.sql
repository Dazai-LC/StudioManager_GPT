-- LOCAL DEMO ONLY / REVIEW ONLY / NOT EXECUTED.
-- This separate fresh-bootstrap copy does not replace or modify migrations 01-08.
-- Default refusal: Invoke-LocalDemo.ps1 enables this one bit IN MEMORY only after explicit Owner switches.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @OwnerApprovedLocalDemo bit=0;
IF COALESCE(@OwnerApprovedLocalDemo,0)<>1
 THROW 51160,'Local demo bootstrap has not been approved.',1;
IF @@TRANCOUNT<>0 THROW 51160,'Use a fresh connection without an ambient transaction.',1;
DECLARE @ExpectedDatabase sysname=N'StudioManager_P04_Test_20261116A';
DECLARE @Identity TABLE(Expected nvarchar(128),Observed nvarchar(128));
INSERT @Identity VALUES
 (N'MSI',CONVERT(nvarchar(128),SERVERPROPERTY('MachineName'))),
 (N'MSI',CONVERT(nvarchar(128),SERVERPROPERTY('ComputerNamePhysicalNetBIOS'))),
 (N'SQLEXPRESS',CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName'))),
 (N'MSI\SQLEXPRESS',CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))),
 (N'MSI\SQLEXPRESS',CONVERT(nvarchar(128),@@SERVERNAME)),
 (@ExpectedDatabase,DB_NAME());
IF EXISTS(SELECT 1 FROM @Identity WHERE Observed IS NULL OR DATALENGTH(Expected)<>DATALENGTH(Observed)
 OR Expected COLLATE Latin1_General_100_BIN2<>Observed COLLATE Latin1_General_100_BIN2)
 OR DB_ID()<=4 OR COALESCE(SCHEMA_NAME(),N'')<>N'dbo'
 OR NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id=DB_ID() AND state=0 AND is_read_only=0
   AND source_database_id IS NULL AND group_database_id IS NULL)
 OR COALESCE(TRY_CONVERT(int,SERVERPROPERTY('ProductMajorVersion')),0)<16
 THROW 51160,'Wrong SQL instance/database/context; only the exact approved empty local demo target is eligible.',1;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
 BEGIN TRANSACTION;
 -- Serialize this bootstrap with itself; other user objects still cause refusal.
 DECLARE @LockResult int;
 EXEC @LockResult=sys.sp_getapplock @Resource=N'StudioManager.LocalDemo.Bootstrap',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0;
 IF @LockResult<0 THROW 51160,'Another bootstrap is active; no retry or overwrite.',1;
 IF EXISTS(SELECT 1 FROM sys.objects WHERE is_ms_shipped=0)
   OR EXISTS(SELECT 1 FROM sys.types WHERE is_user_defined=1)
  THROW 51160,'The approved database is not empty. Nothing will be reset or overwritten.',1;

-- BEGIN BUSINESS SCHEMA: original 02 payload, USE/GO removed; dbo context enforced above.
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
-- END BUSINESS SCHEMA
-- Three fresh-schema indexes from 05; none of its upgrade/backfill statements are replayed.
CREATE INDEX IX_LichChup_HoanThanhLuc ON LichChup(HoanThanhLuc) WHERE HoanThanhLuc IS NOT NULL;
CREATE INDEX IX_ThanhToan_NgayGiaoDich ON ThanhToan(NgayGiaoDich);
CREATE INDEX IX_HoanTien_NgayGiaoDich ON HoanTien(NgayGiaoDich);
-- BEGIN P03 IDENTITY DDL (06, unchanged payload)
CREATE TABLE dbo.TaiKhoanKhachHang(
  TaiKhoanKhachHangId bigint IDENTITY CONSTRAINT PK_TaiKhoanKhachHang PRIMARY KEY,
  LoginEmail varchar(254) COLLATE Latin1_General_100_BIN2 NOT NULL,
  MatKhauHash nvarchar(255) NOT NULL,
  SecurityVersion uniqueidentifier NOT NULL CONSTRAINT DF_TKKH_Version DEFAULT NEWID(),
  HoatDong bit NOT NULL CONSTRAINT DF_TKKH_Active DEFAULT 1,
  HoTen nvarchar(100) NOT NULL, SoDienThoai varchar(20) NOT NULL,
  EmailLienHe varchar(254) NOT NULL, DiaChi nvarchar(255) NOT NULL,
  CreatedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_TKKH_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_TKKH_Updated DEFAULT SYSUTCDATETIME(),
  PasswordChangedAtUtc datetime2(7) NULL,
  CONSTRAINT UQ_TKKH_LoginEmail UNIQUE(LoginEmail),
  CONSTRAINT CK_TKKH_Profile CHECK(LEN(LoginEmail)>3 AND LEN(HoTen)>0 AND LEN(SoDienThoai)>0 AND LEN(EmailLienHe)>3)
 );
 CREATE TABLE dbo.SoHuuKhachHang(
  SoHuuKhachHangId bigint IDENTITY CONSTRAINT PK_SoHuuKhachHang PRIMARY KEY,
  TaiKhoanKhachHangId bigint NOT NULL CONSTRAINT FK_SHKH_Account REFERENCES dbo.TaiKhoanKhachHang(TaiKhoanKhachHangId),
  KhachHangId bigint NULL CONSTRAINT FK_SHKH_Customer REFERENCES dbo.KhachHang(KhachHangId),
  TrangThai varchar(10) NOT NULL CONSTRAINT DF_SHKH_State DEFAULT 'PENDING',
  ThamChieu nvarchar(100) NOT NULL, BangChungKhachGui nvarchar(500) NOT NULL,
  RequestedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_SHKH_Requested DEFAULT SYSUTCDATETIME(),
  ReviewedBy int NULL CONSTRAINT FK_SHKH_Reviewer REFERENCES dbo.TaiKhoan(TaiKhoanId),
  ReviewedAtUtc datetime2(7) NULL, ReviewReason nvarchar(500) NULL,
  RevokedBy int NULL CONSTRAINT FK_SHKH_Revoker REFERENCES dbo.TaiKhoan(TaiKhoanId),
  RevokedAtUtc datetime2(7) NULL, RevokeReason nvarchar(500) NULL,
  CONSTRAINT CK_SHKH_State CHECK(TrangThai IN('PENDING','APPROVED','REJECTED','REVOKED')),
  CONSTRAINT CK_SHKH_Target CHECK(
   (TrangThai IN('PENDING','REJECTED') AND KhachHangId IS NULL) OR
   (TrangThai IN('APPROVED','REVOKED') AND KhachHangId IS NOT NULL)),
  CONSTRAINT CK_SHKH_Review CHECK(
   (TrangThai='PENDING' AND ReviewedBy IS NULL AND ReviewedAtUtc IS NULL AND ReviewReason IS NULL) OR
   (TrangThai<>'PENDING' AND ReviewedBy IS NOT NULL AND ReviewedAtUtc IS NOT NULL AND ReviewReason IS NOT NULL AND LEN(ReviewReason)>0)),
  CONSTRAINT CK_SHKH_Revoke CHECK(
   (TrangThai='REVOKED' AND RevokedBy IS NOT NULL AND RevokedAtUtc IS NOT NULL AND RevokeReason IS NOT NULL AND LEN(RevokeReason)>0) OR
   (TrangThai<>'REVOKED' AND RevokedBy IS NULL AND RevokedAtUtc IS NULL AND RevokeReason IS NULL))
 );
 CREATE UNIQUE INDEX UX_SHKH_PendingAccount ON dbo.SoHuuKhachHang(TaiKhoanKhachHangId) WHERE TrangThai='PENDING';
 CREATE UNIQUE INDEX UX_SHKH_ApprovedAccount ON dbo.SoHuuKhachHang(TaiKhoanKhachHangId) WHERE TrangThai='APPROVED';
 CREATE UNIQUE INDEX UX_SHKH_ApprovedCustomer ON dbo.SoHuuKhachHang(KhachHangId) WHERE TrangThai='APPROVED';
 CREATE INDEX IX_SHKH_AccountHistory ON dbo.SoHuuKhachHang(TaiKhoanKhachHangId,SoHuuKhachHangId);
-- END P03 IDENTITY DDL
-- BEGIN P04 BOOKING DDL (07, unchanged payload)
CREATE TABLE dbo.YeuCauDatLich(
  YeuCauDatLichId bigint IDENTITY CONSTRAINT PK_YCDL PRIMARY KEY,
  TaiKhoanKhachHangId bigint NOT NULL CONSTRAINT FK_YCDL_Account REFERENCES dbo.TaiKhoanKhachHang(TaiKhoanKhachHangId),
  SubmissionKey uniqueidentifier NOT NULL, QuoteFingerprint char(64) NOT NULL,
  TrangThai varchar(10) NOT NULL CONSTRAINT DF_YCDL_State DEFAULT 'PENDING',
  GoiChupId int NOT NULL CONSTRAINT FK_YCDL_Package REFERENCES dbo.GoiChup(GoiChupId),
  MaGoiChot varchar(20) NOT NULL, TenGoiChot nvarchar(150) NOT NULL, GiaGoiChot decimal(18,2) NOT NULL,
  ThoiLuongPhut int NOT NULL, BatDauUtc datetime2(0) NOT NULL,
  PhongChupUuTienId int NULL CONSTRAINT FK_YCDL_Room REFERENCES dbo.PhongChup(PhongChupId),
  TenPhongUuTien nvarchar(100) NULL,
  HoTen nvarchar(100) NOT NULL, SoDienThoai varchar(15) NOT NULL, Email varchar(254) NOT NULL, DiaChi nvarchar(255) NOT NULL,
  GhiChu nvarchar(1000) NOT NULL, TongDuKien decimal(18,2) NOT NULL,
  SubmittedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_YCDL_Submitted DEFAULT SYSUTCDATETIME(),
  ReviewedBy int NULL CONSTRAINT FK_YCDL_Reviewer REFERENCES dbo.TaiKhoan(TaiKhoanId),
  ReviewedAtUtc datetime2(7) NULL, PhanHoi nvarchar(500) NULL, CancelledAtUtc datetime2(7) NULL,
  LichChupId bigint NULL CONSTRAINT FK_YCDL_Booking REFERENCES dbo.LichChup(LichChupId),
  KhachHangId bigint NULL CONSTRAINT FK_YCDL_Customer REFERENCES dbo.KhachHang(KhachHangId),
  TaoKhachMoi bit NOT NULL CONSTRAINT DF_YCDL_NewCustomer DEFAULT 0, TongDaChot decimal(18,2) NULL,
  CONSTRAINT UQ_YCDL_Submission UNIQUE(TaiKhoanKhachHangId,SubmissionKey),
  CONSTRAINT CK_YCDL_Values CHECK(GiaGoiChot>=0 AND TongDuKien>=GiaGoiChot AND ThoiLuongPhut BETWEEN 1 AND 1440
   AND LEN(HoTen)>0 AND LEN(SoDienThoai)>0 AND LEN(Email)>3),
  CONSTRAINT CK_YCDL_PreferredRoom CHECK((PhongChupUuTienId IS NULL AND TenPhongUuTien IS NULL)
   OR (PhongChupUuTienId IS NOT NULL AND TenPhongUuTien IS NOT NULL)),
  CONSTRAINT CK_YCDL_Lifecycle CHECK(
   (TrangThai='PENDING' AND ReviewedBy IS NULL AND ReviewedAtUtc IS NULL AND PhanHoi IS NULL AND CancelledAtUtc IS NULL
    AND LichChupId IS NULL AND KhachHangId IS NULL AND TaoKhachMoi=0 AND TongDaChot IS NULL)
   OR (TrangThai='APPROVED' AND ReviewedBy IS NOT NULL AND ReviewedAtUtc IS NOT NULL AND PhanHoi IS NOT NULL AND LEN(PhanHoi)>=5
    AND CancelledAtUtc IS NULL AND LichChupId IS NOT NULL AND KhachHangId IS NOT NULL AND TongDaChot IS NOT NULL AND TongDaChot=TongDuKien)
   OR (TrangThai='REJECTED' AND ReviewedBy IS NOT NULL AND ReviewedAtUtc IS NOT NULL AND PhanHoi IS NOT NULL AND LEN(PhanHoi)>=5
    AND CancelledAtUtc IS NULL AND LichChupId IS NULL AND KhachHangId IS NULL AND TaoKhachMoi=0 AND TongDaChot IS NULL)
   OR (TrangThai='CANCELLED' AND ReviewedBy IS NULL AND ReviewedAtUtc IS NULL AND PhanHoi IS NOT NULL AND LEN(PhanHoi)>=5
    AND CancelledAtUtc IS NOT NULL AND LichChupId IS NULL AND KhachHangId IS NULL AND TaoKhachMoi=0 AND TongDaChot IS NULL))
 );
 CREATE UNIQUE INDEX UX_YCDL_OfficialBooking ON dbo.YeuCauDatLich(LichChupId) WHERE LichChupId IS NOT NULL;
 CREATE INDEX IX_YCDL_MyHistory ON dbo.YeuCauDatLich(TaiKhoanKhachHangId,YeuCauDatLichId DESC);
 CREATE INDEX IX_YCDL_StaffQueue ON dbo.YeuCauDatLich(TrangThai,YeuCauDatLichId DESC);
 CREATE TABLE dbo.YeuCauDatLichDichVu(
  YeuCauDatLichId bigint NOT NULL CONSTRAINT FK_YCDLDV_Request REFERENCES dbo.YeuCauDatLich(YeuCauDatLichId),
  DichVuId int NOT NULL CONSTRAINT FK_YCDLDV_Service REFERENCES dbo.DichVu(DichVuId),
  TenDichVuChot nvarchar(150) NOT NULL, DonViTinhChot nvarchar(30) NOT NULL,
  DonGiaChot decimal(18,2) NOT NULL CHECK(DonGiaChot>=0),
  SoLuong decimal(10,2) NOT NULL CHECK(SoLuong>0 AND SoLuong<=1000),
  CONSTRAINT CK_YCDLDV_ExactMoney CHECK(DonGiaChot*SoLuong=ROUND(DonGiaChot*SoLuong,2)
   AND DonGiaChot*SoLuong<=9999999999999999.99),
  CONSTRAINT PK_YCDLDV PRIMARY KEY(YeuCauDatLichId,DichVuId)
 );
 CREATE TABLE dbo.YeuCauDatLichLichSu(
  LichSuId bigint IDENTITY CONSTRAINT PK_YCDLLS PRIMARY KEY,
  YeuCauDatLichId bigint NOT NULL CONSTRAINT FK_YCDLLS_Request REFERENCES dbo.YeuCauDatLich(YeuCauDatLichId),
  TrangThai varchar(10) NOT NULL CHECK(TrangThai IN('PENDING','APPROVED','REJECTED','CANCELLED')),
  ThoiDiemUtc datetime2(7) NOT NULL CONSTRAINT DF_YCDLLS_Time DEFAULT SYSUTCDATETIME(),
  TaiKhoanNhanSuId int NULL CONSTRAINT FK_YCDLLS_Staff REFERENCES dbo.TaiKhoan(TaiKhoanId),
  LyDo nvarchar(500) NOT NULL,
  CONSTRAINT UQ_YCDLLS_State UNIQUE(YeuCauDatLichId,TrangThai),
  CONSTRAINT CK_YCDLLS_Actor CHECK(
   (TrangThai IN('APPROVED','REJECTED') AND TaiKhoanNhanSuId IS NOT NULL)
   OR (TrangThai IN('PENDING','CANCELLED') AND TaiKhoanNhanSuId IS NULL))
 );
-- END P04 BOOKING DDL
-- BEGIN RECEIPT DDL (08, unchanged payload)
ALTER TABLE dbo.ThanhToan ADD PhuongThucThu varchar(24) NULL,YeuCauThuId uniqueidentifier NULL,BienNhanJson nvarchar(max) NULL;
 -- A separate batch is required so new-column references bind after ALTER TABLE.
 EXEC sys.sp_executesql N'ALTER TABLE dbo.ThanhToan WITH CHECK ADD CONSTRAINT CK_ThanhToan_DemoReceipt CHECK(
  (PhuongThucThu IS NULL AND YeuCauThuId IS NULL AND BienNhanJson IS NULL) OR
  (PhuongThucThu IS NOT NULL AND PhuongThucThu COLLATE Latin1_General_100_BIN2 IN(''CASH'',''BANK_TRANSFER_DEMO'')
   AND YeuCauThuId IS NOT NULL AND BienNhanJson IS NOT NULL AND ISJSON(BienNhanJson)=1));
 CREATE UNIQUE INDEX UX_ThanhToan_YeuCauThu ON dbo.ThanhToan(YeuCauThuId) WHERE YeuCauThuId IS NOT NULL;';
-- END RECEIPT DDL
-- BEGIN SYNTHETIC CATALOG/STAFF SEED (03 subset; code-based FK lookups)
SET NOCOUNT ON;

INSERT NhanVien(MaNhanVien,HoTen,SoDienThoai,ChucVu,TrangThai) VALUES
('NV000001',N'Nguyễn Minh Anh','0901000001','TIEP_NHAN','DANG_LAM'),('NV000002',N'Trần Thu Hà','0901000002','TIEP_NHAN','DANG_LAM'),
('NV000003',N'Lê Hoàng Nam','0901000003','TIEP_NHAN','DANG_LAM'),('NV000004',N'Phạm Gia Huy','0901000004','NHIEP_ANH_GIA','DANG_LAM'),
('NV000005',N'Võ Khánh Linh','0901000005','NHIEP_ANH_GIA','DANG_LAM'),('NV000006',N'Đỗ Quang Minh','0901000006','NHIEP_ANH_GIA','DANG_LAM'),
('NV000007',N'Bùi Ngọc Mai','0901000007','NHIEP_ANH_GIA','DANG_LAM'),('NV000008',N'Ngô Đức Long','0901000008','NHIEP_ANH_GIA','NGUNG_LAM');

-- Mật khẩu demo cho cả ba tài khoản: Admin@123 (bắt buộc đổi khi dùng thật)
DECLARE @Hash nvarchar(255)=N'PBKDF2-SHA256$210000$384+g6Fb4W74qN4ljb5MzA==$jM2+11okSJfLHLnG27IkEdW8bM/kJ6+PWoAggBn7Mr8=';
INSERT TaiKhoan(NhanVienId,TenDangNhap,MatKhauHash,VaiTro,TrangThai,PhaiDoiMatKhau) VALUES
((SELECT NhanVienId FROM dbo.NhanVien WHERE MaNhanVien='NV000001'),'admin',@Hash,'QUAN_TRI_VIEN','HOAT_DONG',1),((SELECT NhanVienId FROM dbo.NhanVien WHERE MaNhanVien='NV000002'),'nhanvien1',@Hash,'NHAN_VIEN','HOAT_DONG',1),((SELECT NhanVienId FROM dbo.NhanVien WHERE MaNhanVien='NV000003'),'nhanvien2',@Hash,'NHAN_VIEN','HOAT_DONG',1);

INSERT GoiChup(MaGoiChup,TenGoiChup,MoTa,GiaGoi,ThoiLuongPhut) VALUES
('GC000001',N'Chân dung cá nhân',N'01 concept, 10 ảnh chỉnh sửa',1500000,120),('GC000002',N'Gia đình',N'Tối đa 6 thành viên',2800000,180),
('GC000003',N'Em bé',N'Phông nền và phụ kiện cơ bản',2200000,150),('GC000004',N'Cặp đôi',N'02 concept studio',2500000,180),('GC000005',N'Kỷ yếu',N'Gói nhóm ngoại cảnh',6500000,300);
INSERT DichVu(MaDichVu,TenDichVu,DonViTinh,DonGia,MoTa) VALUES
('DV000001',N'Trang điểm',N'Người',450000,N'Trang điểm studio'),('DV000002',N'Thuê trang phục',N'Bộ',300000,N'Trang phục có sẵn'),
('DV000003',N'In ảnh 15x21',N'Tấm',30000,NULL),('DV000004',N'In album',N'Cuốn',850000,NULL),('DV000005',N'Chụp thêm giờ',N'Giờ',500000,NULL),
('DV000006',N'Chỉnh sửa ảnh bổ sung',N'Ảnh',50000,NULL),('DV000007',N'Phông nền cao cấp',N'Bộ',250000,NULL),('DV000008',N'Giao ảnh nhanh',N'Gói',400000,NULL);
INSERT PhongChup(MaPhong,TenPhong,MoTa) VALUES ('P0001',N'Phòng Ánh Sáng',N'Phong cách tối giản'),('P0002',N'Phòng Cổ Điển',N'Nội thất vintage'),('P0003',N'Phòng Em Bé',N'An toàn, nhiều đạo cụ');
INSERT TaiNguyen(MaTaiNguyen,TenTaiNguyen,LoaiTaiNguyen,TongSoLuong,DichVuThueId) VALUES
('TN000001',N'Canon R6 Mark II','THIET_BI',2,NULL),('TN000002',N'Sony A7 IV','THIET_BI',2,NULL),('TN000003',N'Đèn Godox AD600','THIET_BI',4,NULL),
('TN000004',N'Đèn LED RGB','THIET_BI',3,NULL),('TN000005',N'Phông giấy trắng','THIET_BI',2,NULL),('TN000006',N'Váy cưới trắng','TRANG_PHUC',3,(SELECT DichVuId FROM dbo.DichVu WHERE MaDichVu='DV000002')),
('TN000007',N'Áo dài đỏ','TRANG_PHUC',4,(SELECT DichVuId FROM dbo.DichVu WHERE MaDichVu='DV000002')),('TN000008',N'Vest nam','TRANG_PHUC',4,(SELECT DichVuId FROM dbo.DichVu WHERE MaDichVu='DV000002')),('TN000009',N'Váy công chúa bé','TRANG_PHUC',5,(SELECT DichVuId FROM dbo.DichVu WHERE MaDichVu='DV000002')),('TN000010',N'Đạo cụ sinh nhật','THIET_BI',3,NULL);
-- END SYNTHETIC SEED
 IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>19
  THROW 51160,'Unexpected final table count; rollback required.',1;
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
SELECT DB_NAME() AS ApprovedDatabase,
 (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS TablesCreated,
 (SELECT COUNT(*) FROM dbo.TaiKhoan) AS StaffAccounts,
 (SELECT COUNT(*) FROM dbo.GoiChup) AS Packages,
 (SELECT COUNT(*) FROM dbo.DichVu) AS Services,
 (SELECT COUNT(*) FROM dbo.YeuCauDatLich) AS CustomerRequests;