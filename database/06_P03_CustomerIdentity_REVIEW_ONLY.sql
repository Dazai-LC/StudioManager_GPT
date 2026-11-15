-- REVIEW ONLY. Copy, review the exact database and explicitly enable execution after Owner approval.
-- Additive migration: no business data backfill, inferred ownership, login/grant or database switching.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @OwnerApprovedExecution bit=0;
DECLARE @ExpectedDatabaseName sysname=NULL;
IF @OwnerApprovedExecution<>1 OR @ExpectedDatabaseName IS NULL
 THROW 51030, 'P03 migration execution has not been approved.', 1;
IF DB_ID()<=4 OR DB_NAME() COLLATE Latin1_General_100_BIN2<>@ExpectedDatabaseName COLLATE Latin1_General_100_BIN2
 THROW 51030, 'Wrong migration database.', 1;
IF OBJECT_ID(N'dbo.KhachHang',N'U') IS NULL OR OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NULL OR OBJECT_ID(N'dbo.NhatKyHeThong',N'U') IS NULL
 THROW 51030, 'Reviewed business baseline required.', 1;
IF OBJECT_ID(N'dbo.TaiKhoanKhachHang',N'U') IS NOT NULL OR OBJECT_ID(N'dbo.SoHuuKhachHang',N'U') IS NOT NULL
 THROW 51030, 'P03 objects already exist; inspect instead of overwriting.', 1;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
 BEGIN TRANSACTION;
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
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
