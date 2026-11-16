-- REVIEW ONLY. One-time additive migration; never execute this repository template automatically.
-- All NULL/0 inputs below must come from the Owner-approved disposable target record.
-- External preflight MUST verify client endpoint/TLS, approved reference baseline fingerprint,
-- restricted runtime/verifier principals, exclusive provisioning and the isolated Windows topology.
-- Do not copy the fingerprint from an unreviewed candidate and call it an approved baseline.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @OwnerApprovedExecution bit=0;
DECLARE @ExternalPreflightApproved bit=0;
DECLARE @ExpectedDatabaseName sysname=NULL;
DECLARE @ExpectedMachineName sysname=NULL;
DECLARE @ExpectedPhysicalMachineName sysname=NULL;
DECLARE @ExpectedInstanceName sysname=NULL;
DECLARE @ExpectedServerName sysname=NULL;
DECLARE @ExpectedConfiguredServerName sysname=NULL;
DECLARE @ExpectedDatabaseId int=NULL;
DECLARE @ExpectedDatabaseCreatedAt datetime2(7)=NULL;
DECLARE @ExpectedDatabaseCollation sysname=NULL;
DECLARE @ExpectedRunId uniqueidentifier=NULL;
DECLARE @ExpectedBaselineFingerprint varbinary(32)=NULL;

IF @OwnerApprovedExecution IS NULL OR @OwnerApprovedExecution<>1 OR @ExternalPreflightApproved IS NULL
 OR @ExternalPreflightApproved<>1 OR @ExpectedDatabaseName IS NULL OR @ExpectedRunId IS NULL
 OR @ExpectedRunId='00000000-0000-0000-0000-000000000000'
 OR @ExpectedDatabaseId IS NULL OR @ExpectedDatabaseId<=4 OR @ExpectedDatabaseCreatedAt IS NULL
 OR @ExpectedBaselineFingerprint IS NULL OR DATALENGTH(@ExpectedBaselineFingerprint)<>32
 THROW 51040, 'P04 migration execution and external preflight have not been approved.', 1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0
 THROW 51040, 'Use a fresh dedicated migration connection without an ambient transaction.', 1;

-- Only an explicitly reviewed standalone named SQL Server instance is supported here.
-- InstanceName is NULL on a default instance; @@SERVERNAME is configurable, not sole authority.
DECLARE @ObservedMachineName nvarchar(128)=CONVERT(nvarchar(128),SERVERPROPERTY('MachineName'));
DECLARE @ObservedPhysicalMachineName nvarchar(128)=CONVERT(nvarchar(128),SERVERPROPERTY('ComputerNamePhysicalNetBIOS'));
DECLARE @ObservedInstanceName nvarchar(128)=CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName'));
DECLARE @ObservedServerName nvarchar(128)=CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'));
DECLARE @ObservedConfiguredServerName nvarchar(128)=CONVERT(nvarchar(128),@@SERVERNAME);
IF COALESCE(TRY_CONVERT(int,SERVERPROPERTY('IsClustered')),1)<>0
 OR COALESCE(TRY_CONVERT(int,SERVERPROPERTY('IsLocalDB')),1)<>0
 OR COALESCE(TRY_CONVERT(int,SERVERPROPERTY('ProductMajorVersion')),0)<16
 OR COALESCE(TRY_CONVERT(int,SERVERPROPERTY('EngineEdition')),0) NOT IN(2,3,4)
 OR @ObservedInstanceName IS NULL OR UPPER(@ObservedInstanceName)=N'MSSQLSERVER'
 OR @ObservedMachineName IS NULL OR @ObservedPhysicalMachineName IS NULL OR @ObservedServerName IS NULL
 OR @ObservedConfiguredServerName IS NULL
 OR UPPER(@ObservedMachineName) COLLATE Latin1_General_100_BIN2<>UPPER(@ObservedPhysicalMachineName) COLLATE Latin1_General_100_BIN2
 OR UPPER(@ObservedServerName) COLLATE Latin1_General_100_BIN2<>UPPER(@ObservedMachineName+N'\'+@ObservedInstanceName) COLLATE Latin1_General_100_BIN2
 OR UPPER(@ObservedConfiguredServerName) COLLATE Latin1_General_100_BIN2<>UPPER(@ObservedMachineName+N'\'+@ObservedInstanceName) COLLATE Latin1_General_100_BIN2
 THROW 51040, 'Unsupported or inconsistent SQL instance identity; external review required.', 1;

DECLARE @ExpectedDisposableName nvarchar(128)=N'StudioManager_P04_Test_'+LOWER(REPLACE(CONVERT(nvarchar(36),@ExpectedRunId),N'-',N''));
DECLARE @Identity TABLE(Expected nvarchar(128),Observed nvarchar(128));
INSERT @Identity VALUES
 (@ExpectedMachineName,@ObservedMachineName),(@ExpectedPhysicalMachineName,@ObservedPhysicalMachineName),
 (@ExpectedInstanceName,@ObservedInstanceName),(@ExpectedServerName,@ObservedServerName),
 (@ExpectedConfiguredServerName,@ObservedConfiguredServerName),(@ExpectedDatabaseName,DB_NAME()),
 (@ExpectedDatabaseName,@ExpectedDisposableName),
 (@ExpectedDatabaseCollation,CONVERT(nvarchar(128),DATABASEPROPERTYEX(DB_NAME(),'Collation')));
IF EXISTS(SELECT 1 FROM @Identity WHERE Expected IS NULL OR Observed IS NULL OR LEN(Expected)=0
 OR DATALENGTH(Expected)<>DATALENGTH(LTRIM(RTRIM(Expected)))
 OR DATALENGTH(Expected)<>DATALENGTH(Observed)
 OR Expected COLLATE Latin1_General_100_BIN2<>Observed COLLATE Latin1_General_100_BIN2)
 THROW 51040, 'SQL target identity does not match the approved disposable target.', 1;
IF DB_ID()<=4 OR DB_NAME() COLLATE Latin1_General_100_BIN2<>@ExpectedDatabaseName COLLATE Latin1_General_100_BIN2
 OR DB_ID()<>@ExpectedDatabaseId
 OR NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id=DB_ID() AND state=0 AND is_read_only=0
  AND source_database_id IS NULL AND group_database_id IS NULL
  AND CONVERT(datetime2(7),create_date)=@ExpectedDatabaseCreatedAt AND compatibility_level>=160)
 THROW 51040, 'Wrong, recreated, read-only or unsupported migration database.', 1;
IF COALESCE(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),0)<>1
 THROW 51040, 'Complete baseline metadata visibility is required for migration review.', 1;

-- P02 business shape + P03 identity shape; P02 acceptance-run markers/rights are not reused.
-- Sizes are bytes (nvarchar = two bytes/character); NULL means not applicable.
DECLARE @RequiredColumns TABLE(TableName sysname,ColumnName sysname,TypeName sysname,
 MaxLength smallint NULL,PrecisionValue tinyint NULL,ScaleValue tinyint NULL,Nullable bit,IsIdentity bit,
 PRIMARY KEY(TableName,ColumnName));
INSERT @RequiredColumns VALUES
 (N'NhanVien',N'NhanVienId',N'int',NULL,NULL,NULL,0,1),
 (N'NhanVien',N'MaNhanVien',N'varchar',20,NULL,NULL,0,0),
 (N'NhanVien',N'HoTen',N'nvarchar',200,NULL,NULL,0,0),
 (N'NhanVien',N'SoDienThoai',N'varchar',15,NULL,NULL,0,0),
 (N'NhanVien',N'ChucVu',N'varchar',30,NULL,NULL,0,0),
 (N'NhanVien',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'NhanVien',N'GhiChu',N'nvarchar',1000,NULL,NULL,1,0),
 (N'NhanVien',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'NhanVien',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'TaiKhoan',N'TaiKhoanId',N'int',NULL,NULL,NULL,0,1),
 (N'TaiKhoan',N'NhanVienId',N'int',NULL,NULL,NULL,1,0),
 (N'TaiKhoan',N'TenDangNhap',N'varchar',50,NULL,NULL,0,0),
 (N'TaiKhoan',N'MatKhauHash',N'nvarchar',510,NULL,NULL,0,0),
 (N'TaiKhoan',N'VaiTro',N'varchar',20,NULL,NULL,0,0),
 (N'TaiKhoan',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'TaiKhoan',N'PhaiDoiMatKhau',N'bit',NULL,NULL,NULL,0,0),
 (N'TaiKhoan',N'LanDangNhapCuoi',N'datetime2',NULL,NULL,0,1,0),
 (N'TaiKhoan',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'TaiKhoan',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'KhachHang',N'KhachHangId',N'bigint',NULL,NULL,NULL,0,1),
 (N'KhachHang',N'MaKhachHang',N'varchar',20,NULL,NULL,0,0),
 (N'KhachHang',N'HoTen',N'nvarchar',200,NULL,NULL,0,0),
 (N'KhachHang',N'SoDienThoai',N'varchar',15,NULL,NULL,0,0),
 (N'KhachHang',N'DiaChi',N'nvarchar',510,NULL,NULL,1,0),
 (N'KhachHang',N'Email',N'varchar',254,NULL,NULL,1,0),
 (N'KhachHang',N'GhiChu',N'nvarchar',1000,NULL,NULL,1,0),
 (N'KhachHang',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'KhachHang',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'GoiChup',N'GoiChupId',N'int',NULL,NULL,NULL,0,1),
 (N'GoiChup',N'MaGoiChup',N'varchar',20,NULL,NULL,0,0),
 (N'GoiChup',N'TenGoiChup',N'nvarchar',300,NULL,NULL,0,0),
 (N'GoiChup',N'MoTa',N'nvarchar',2000,NULL,NULL,1,0),
 (N'GoiChup',N'GiaGoi',N'decimal',NULL,18,2,0,0),
 (N'GoiChup',N'ThoiLuongPhut',N'int',NULL,NULL,NULL,0,0),
 (N'GoiChup',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'GoiChup',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'GoiChup',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'DichVu',N'DichVuId',N'int',NULL,NULL,NULL,0,1),
 (N'DichVu',N'MaDichVu',N'varchar',20,NULL,NULL,0,0),
 (N'DichVu',N'TenDichVu',N'nvarchar',300,NULL,NULL,0,0),
 (N'DichVu',N'DonViTinh',N'nvarchar',60,NULL,NULL,0,0),
 (N'DichVu',N'DonGia',N'decimal',NULL,18,2,0,0),
 (N'DichVu',N'MoTa',N'nvarchar',2000,NULL,NULL,1,0),
 (N'DichVu',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'DichVu',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'DichVu',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'PhongChup',N'PhongChupId',N'int',NULL,NULL,NULL,0,1),
 (N'PhongChup',N'MaPhong',N'varchar',20,NULL,NULL,0,0),
 (N'PhongChup',N'TenPhong',N'nvarchar',200,NULL,NULL,0,0),
 (N'PhongChup',N'MoTa',N'nvarchar',1000,NULL,NULL,1,0),
 (N'PhongChup',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'PhongChup',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'PhongChup',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'TaiNguyen',N'TaiNguyenId',N'int',NULL,NULL,NULL,0,1),
 (N'TaiNguyen',N'MaTaiNguyen',N'varchar',20,NULL,NULL,0,0),
 (N'TaiNguyen',N'TenTaiNguyen',N'nvarchar',300,NULL,NULL,0,0),
 (N'TaiNguyen',N'LoaiTaiNguyen',N'varchar',20,NULL,NULL,0,0),
 (N'TaiNguyen',N'TongSoLuong',N'int',NULL,NULL,NULL,0,0),
 (N'TaiNguyen',N'DichVuThueId',N'int',NULL,NULL,NULL,1,0),
 (N'TaiNguyen',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'TaiNguyen',N'GhiChu',N'nvarchar',1000,NULL,NULL,1,0),
 (N'TaiNguyen',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'TaiNguyen',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'LichChup',N'LichChupId',N'bigint',NULL,NULL,NULL,0,1),
 (N'LichChup',N'MaLichChup',N'varchar',25,NULL,NULL,0,0),
 (N'LichChup',N'KhachHangId',N'bigint',NULL,NULL,NULL,0,0),
 (N'LichChup',N'GoiChupId',N'int',NULL,NULL,NULL,0,0),
 (N'LichChup',N'TenGoiChot',N'nvarchar',300,NULL,NULL,0,0),
 (N'LichChup',N'GiaGoiChot',N'decimal',NULL,18,2,0,0),
 (N'LichChup',N'BatDau',N'datetime2',NULL,NULL,0,0,0),
 (N'LichChup',N'KetThuc',N'datetime2',NULL,NULL,0,0,0),
 (N'LichChup',N'NhiepAnhGiaId',N'int',NULL,NULL,NULL,0,0),
 (N'LichChup',N'PhongChupId',N'int',NULL,NULL,NULL,0,0),
 (N'LichChup',N'TrangThai',N'varchar',30,NULL,NULL,0,0),
 (N'LichChup',N'TienGiam',N'decimal',NULL,18,2,0,0),
 (N'LichChup',N'LyDoGiam',N'nvarchar',1000,NULL,NULL,1,0),
 (N'LichChup',N'GhiChu',N'nvarchar',2000,NULL,NULL,1,0),
 (N'LichChup',N'LyDoHuy',N'nvarchar',1000,NULL,NULL,1,0),
 (N'LichChup',N'HuyBoiTaiKhoanId',N'int',NULL,NULL,NULL,1,0),
 (N'LichChup',N'HuyLuc',N'datetime2',NULL,NULL,0,1,0),
 (N'LichChup',N'HoanThanhLuc',N'datetime2',NULL,NULL,0,1,0),
 (N'LichChup',N'CreatedBy',N'int',NULL,NULL,NULL,0,0),
 (N'LichChup',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'LichChup',N'UpdatedBy',N'int',NULL,NULL,NULL,0,0),
 (N'LichChup',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'LichChupDichVu',N'LichChupDichVuId',N'bigint',NULL,NULL,NULL,0,1),
 (N'LichChupDichVu',N'LichChupId',N'bigint',NULL,NULL,NULL,0,0),
 (N'LichChupDichVu',N'DichVuId',N'int',NULL,NULL,NULL,0,0),
 (N'LichChupDichVu',N'TenDichVuChot',N'nvarchar',300,NULL,NULL,0,0),
 (N'LichChupDichVu',N'DonViTinhChot',N'nvarchar',60,NULL,NULL,0,0),
 (N'LichChupDichVu',N'DonGiaChot',N'decimal',NULL,18,2,0,0),
 (N'LichChupDichVu',N'SoLuong',N'decimal',NULL,10,2,0,0),
 (N'LichChupDichVu',N'CreatedBy',N'int',NULL,NULL,NULL,0,0),
 (N'LichChupDichVu',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'LichChupDichVu',N'UpdatedBy',N'int',NULL,NULL,NULL,0,0),
 (N'LichChupDichVu',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'PhanCongTaiNguyen',N'PhanCongId',N'bigint',NULL,NULL,NULL,0,1),
 (N'PhanCongTaiNguyen',N'LichChupId',N'bigint',NULL,NULL,NULL,0,0),
 (N'PhanCongTaiNguyen',N'TaiNguyenId',N'int',NULL,NULL,NULL,0,0),
 (N'PhanCongTaiNguyen',N'SoLuong',N'int',NULL,NULL,NULL,0,0),
 (N'PhanCongTaiNguyen',N'BatDauSuDung',N'datetime2',NULL,NULL,0,0,0),
 (N'PhanCongTaiNguyen',N'KetThucSuDung',N'datetime2',NULL,NULL,0,0,0),
 (N'PhanCongTaiNguyen',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'PhanCongTaiNguyen',N'CreatedBy',N'int',NULL,NULL,NULL,0,0),
 (N'PhanCongTaiNguyen',N'CreatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'PhanCongTaiNguyen',N'UpdatedBy',N'int',NULL,NULL,NULL,0,0),
 (N'PhanCongTaiNguyen',N'UpdatedAt',N'datetime2',NULL,NULL,0,0,0),
 (N'ThanhToan',N'ThanhToanId',N'bigint',NULL,NULL,NULL,0,1),
 (N'ThanhToan',N'MaThanhToan',N'varchar',25,NULL,NULL,0,0),
 (N'ThanhToan',N'LichChupId',N'bigint',NULL,NULL,NULL,0,0),
 (N'ThanhToan',N'LoaiThu',N'varchar',20,NULL,NULL,0,0),
 (N'ThanhToan',N'SoTien',N'decimal',NULL,18,2,0,0),
 (N'ThanhToan',N'NgayGiaoDich',N'datetime2',NULL,NULL,0,0,0),
 (N'ThanhToan',N'TaiKhoanThucHienId',N'int',NULL,NULL,NULL,0,0),
 (N'ThanhToan',N'GhiChu',N'nvarchar',1000,NULL,NULL,1,0),
 (N'HoanTien',N'HoanTienId',N'bigint',NULL,NULL,NULL,0,1),
 (N'HoanTien',N'MaHoanTien',N'varchar',25,NULL,NULL,0,0),
 (N'HoanTien',N'LichChupId',N'bigint',NULL,NULL,NULL,0,0),
 (N'HoanTien',N'SoTien',N'decimal',NULL,18,2,0,0),
 (N'HoanTien',N'NgayGiaoDich',N'datetime2',NULL,NULL,0,0,0),
 (N'HoanTien',N'TaiKhoanThucHienId',N'int',NULL,NULL,NULL,0,0),
 (N'HoanTien',N'LyDo',N'nvarchar',1000,NULL,NULL,0,0),
 (N'HoanTien',N'GhiChu',N'nvarchar',1000,NULL,NULL,1,0),
 (N'NhatKyHeThong',N'NhatKyId',N'bigint',NULL,NULL,NULL,0,1),
 (N'NhatKyHeThong',N'TaiKhoanId',N'int',NULL,NULL,NULL,0,0),
 (N'NhatKyHeThong',N'HanhDong',N'varchar',50,NULL,NULL,0,0),
 (N'NhatKyHeThong',N'LoaiDoiTuong',N'varchar',50,NULL,NULL,0,0),
 (N'NhatKyHeThong',N'DoiTuongId',N'varchar',50,NULL,NULL,0,0),
 (N'NhatKyHeThong',N'GiaTriCu',N'nvarchar',-1,NULL,NULL,1,0),
 (N'NhatKyHeThong',N'GiaTriMoi',N'nvarchar',-1,NULL,NULL,1,0),
 (N'NhatKyHeThong',N'LyDo',N'nvarchar',1000,NULL,NULL,1,0),
 (N'NhatKyHeThong',N'ThoiDiem',N'datetime2',NULL,NULL,0,0,0),
 (N'NhatKySaoLuu',N'NhatKySaoLuuId',N'bigint',NULL,NULL,NULL,0,1),
 (N'NhatKySaoLuu',N'LoaiThaoTac',N'varchar',20,NULL,NULL,0,0),
 (N'NhatKySaoLuu',N'DuongDanTep',N'nvarchar',2000,NULL,NULL,0,0),
 (N'NhatKySaoLuu',N'TrangThai',N'varchar',20,NULL,NULL,0,0),
 (N'NhatKySaoLuu',N'ThongBao',N'nvarchar',2000,NULL,NULL,1,0),
 (N'NhatKySaoLuu',N'TaiKhoanId',N'int',NULL,NULL,NULL,0,0),
 (N'NhatKySaoLuu',N'ThoiDiem',N'datetime2',NULL,NULL,0,0,0),
 (N'TaiKhoanKhachHang',N'TaiKhoanKhachHangId',N'bigint',NULL,NULL,NULL,0,1),
 (N'TaiKhoanKhachHang',N'LoginEmail',N'varchar',254,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'MatKhauHash',N'nvarchar',510,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'SecurityVersion',N'uniqueidentifier',NULL,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'HoatDong',N'bit',NULL,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'HoTen',N'nvarchar',200,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'SoDienThoai',N'varchar',20,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'EmailLienHe',N'varchar',254,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'DiaChi',N'nvarchar',510,NULL,NULL,0,0),
 (N'TaiKhoanKhachHang',N'CreatedAtUtc',N'datetime2',NULL,NULL,7,0,0),
 (N'TaiKhoanKhachHang',N'UpdatedAtUtc',N'datetime2',NULL,NULL,7,0,0),
 (N'TaiKhoanKhachHang',N'PasswordChangedAtUtc',N'datetime2',NULL,NULL,7,1,0),
 (N'SoHuuKhachHang',N'SoHuuKhachHangId',N'bigint',NULL,NULL,NULL,0,1),
 (N'SoHuuKhachHang',N'TaiKhoanKhachHangId',N'bigint',NULL,NULL,NULL,0,0),
 (N'SoHuuKhachHang',N'KhachHangId',N'bigint',NULL,NULL,NULL,1,0),
 (N'SoHuuKhachHang',N'TrangThai',N'varchar',10,NULL,NULL,0,0),
 (N'SoHuuKhachHang',N'ThamChieu',N'nvarchar',200,NULL,NULL,0,0),
 (N'SoHuuKhachHang',N'BangChungKhachGui',N'nvarchar',1000,NULL,NULL,0,0),
 (N'SoHuuKhachHang',N'RequestedAtUtc',N'datetime2',NULL,NULL,7,0,0),
 (N'SoHuuKhachHang',N'ReviewedBy',N'int',NULL,NULL,NULL,1,0),
 (N'SoHuuKhachHang',N'ReviewedAtUtc',N'datetime2',NULL,NULL,7,1,0),
 (N'SoHuuKhachHang',N'ReviewReason',N'nvarchar',1000,NULL,NULL,1,0),
 (N'SoHuuKhachHang',N'RevokedBy',N'int',NULL,NULL,NULL,1,0),
 (N'SoHuuKhachHang',N'RevokedAtUtc',N'datetime2',NULL,NULL,7,1,0),
 (N'SoHuuKhachHang',N'RevokeReason',N'nvarchar',1000,NULL,NULL,1,0);

DECLARE @RequiredForeignKeys TABLE(TableName sysname,ColumnName sysname,ParentTable sysname,ParentColumn sysname);
INSERT @RequiredForeignKeys VALUES
 (N'TaiKhoan',N'NhanVienId',N'NhanVien',N'NhanVienId'),
 (N'TaiNguyen',N'DichVuThueId',N'DichVu',N'DichVuId'),
 (N'LichChup',N'KhachHangId',N'KhachHang',N'KhachHangId'),
 (N'LichChup',N'GoiChupId',N'GoiChup',N'GoiChupId'),
 (N'LichChup',N'NhiepAnhGiaId',N'NhanVien',N'NhanVienId'),
 (N'LichChup',N'PhongChupId',N'PhongChup',N'PhongChupId'),
 (N'LichChup',N'HuyBoiTaiKhoanId',N'TaiKhoan',N'TaiKhoanId'),
 (N'LichChup',N'CreatedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'LichChup',N'UpdatedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'LichChupDichVu',N'LichChupId',N'LichChup',N'LichChupId'),
 (N'LichChupDichVu',N'DichVuId',N'DichVu',N'DichVuId'),
 (N'LichChupDichVu',N'CreatedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'LichChupDichVu',N'UpdatedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'PhanCongTaiNguyen',N'LichChupId',N'LichChup',N'LichChupId'),
 (N'PhanCongTaiNguyen',N'TaiNguyenId',N'TaiNguyen',N'TaiNguyenId'),
 (N'PhanCongTaiNguyen',N'CreatedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'PhanCongTaiNguyen',N'UpdatedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'ThanhToan',N'LichChupId',N'LichChup',N'LichChupId'),
 (N'ThanhToan',N'TaiKhoanThucHienId',N'TaiKhoan',N'TaiKhoanId'),
 (N'HoanTien',N'LichChupId',N'LichChup',N'LichChupId'),
 (N'HoanTien',N'TaiKhoanThucHienId',N'TaiKhoan',N'TaiKhoanId'),
 (N'NhatKyHeThong',N'TaiKhoanId',N'TaiKhoan',N'TaiKhoanId'),
 (N'NhatKySaoLuu',N'TaiKhoanId',N'TaiKhoan',N'TaiKhoanId'),
 (N'SoHuuKhachHang',N'TaiKhoanKhachHangId',N'TaiKhoanKhachHang',N'TaiKhoanKhachHangId'),
 (N'SoHuuKhachHang',N'KhachHangId',N'KhachHang',N'KhachHangId'),
 (N'SoHuuKhachHang',N'ReviewedBy',N'TaiKhoan',N'TaiKhoanId'),
 (N'SoHuuKhachHang',N'RevokedBy',N'TaiKhoan',N'TaiKhoanId');

DECLARE @RequiredIndexes TABLE(TableName sysname,IndexName sysname NULL,KeyColumns nvarchar(500),
 IsUnique bit,IsPrimary bit,SimpleFilter nvarchar(500) NULL);
INSERT @RequiredIndexes VALUES
 (N'NhanVien',NULL,N'NhanVienId',1,1,NULL),
 (N'NhanVien',NULL,N'MaNhanVien',1,0,NULL),
 (N'NhanVien',N'IX_NhanVien_HoTen',N'HoTen',0,0,NULL),
 (N'TaiKhoan',NULL,N'TaiKhoanId',1,1,NULL),
 (N'TaiKhoan',NULL,N'TenDangNhap',1,0,NULL),
 (N'TaiKhoan',N'UX_TaiKhoan_NhanVien',N'NhanVienId',1,0,N'NhanVienIdISNOTNULL'),
 (N'KhachHang',NULL,N'KhachHangId',1,1,NULL),
 (N'KhachHang',NULL,N'MaKhachHang',1,0,NULL),
 (N'KhachHang',N'IX_KhachHang_HoTen',N'HoTen',0,0,NULL),
 (N'KhachHang',N'IX_KhachHang_SDT',N'SoDienThoai',0,0,NULL),
 (N'GoiChup',NULL,N'GoiChupId',1,1,NULL),
 (N'GoiChup',NULL,N'MaGoiChup',1,0,NULL),
 (N'DichVu',NULL,N'DichVuId',1,1,NULL),
 (N'DichVu',NULL,N'MaDichVu',1,0,NULL),
 (N'PhongChup',NULL,N'PhongChupId',1,1,NULL),
 (N'PhongChup',NULL,N'MaPhong',1,0,NULL),
 (N'TaiNguyen',NULL,N'TaiNguyenId',1,1,NULL),
 (N'TaiNguyen',NULL,N'MaTaiNguyen',1,0,NULL),
 (N'LichChup',NULL,N'LichChupId',1,1,NULL),
 (N'LichChup',NULL,N'MaLichChup',1,0,NULL),
 (N'LichChup',N'IX_LichChup_ThoiGian',N'BatDau|KetThuc',0,0,NULL),
 (N'LichChup',N'IX_LichChup_AnhGia',N'NhiepAnhGiaId|BatDau',0,0,NULL),
 (N'LichChup',N'IX_LichChup_Phong',N'PhongChupId|BatDau',0,0,NULL),
 (N'LichChup',N'IX_LichChup_TrangThai',N'TrangThai|BatDau',0,0,NULL),
 (N'LichChupDichVu',NULL,N'LichChupDichVuId',1,1,NULL),
 (N'LichChupDichVu',NULL,N'LichChupId|DichVuId',1,0,NULL),
 (N'PhanCongTaiNguyen',NULL,N'PhanCongId',1,1,NULL),
 (N'PhanCongTaiNguyen',N'IX_PhanCong_XungDot',N'TaiNguyenId|BatDauSuDung|KetThucSuDung|TrangThai',0,0,NULL),
 (N'ThanhToan',NULL,N'ThanhToanId',1,1,NULL),
 (N'ThanhToan',NULL,N'MaThanhToan',1,0,NULL),
 (N'ThanhToan',N'IX_ThanhToan_LichNgay',N'LichChupId|NgayGiaoDich',0,0,NULL),
 (N'HoanTien',NULL,N'HoanTienId',1,1,NULL),
 (N'HoanTien',NULL,N'MaHoanTien',1,0,NULL),
 (N'HoanTien',N'IX_HoanTien_LichNgay',N'LichChupId|NgayGiaoDich',0,0,NULL),
 (N'NhatKyHeThong',NULL,N'NhatKyId',1,1,NULL),
 (N'NhatKyHeThong',N'IX_NhatKy_TaiKhoan',N'TaiKhoanId|ThoiDiem',0,0,NULL),
 (N'NhatKyHeThong',N'IX_NhatKy_DoiTuong',N'LoaiDoiTuong|DoiTuongId|ThoiDiem',0,0,NULL),
 (N'NhatKySaoLuu',NULL,N'NhatKySaoLuuId',1,1,NULL),
 (N'LichChup',N'IX_LichChup_HoanThanhLuc',N'HoanThanhLuc',0,0,N'HoanThanhLucISNOTNULL'),
 (N'ThanhToan',N'IX_ThanhToan_NgayGiaoDich',N'NgayGiaoDich',0,0,NULL),
 (N'HoanTien',N'IX_HoanTien_NgayGiaoDich',N'NgayGiaoDich',0,0,NULL),
 (N'TaiKhoanKhachHang',NULL,N'TaiKhoanKhachHangId',1,1,NULL),
 (N'TaiKhoanKhachHang',NULL,N'LoginEmail',1,0,NULL),
 (N'SoHuuKhachHang',NULL,N'SoHuuKhachHangId',1,1,NULL),
 (N'SoHuuKhachHang',N'UX_SHKH_PendingAccount',N'TaiKhoanKhachHangId',1,0,N'TrangThai=''PENDING'''),
 (N'SoHuuKhachHang',N'UX_SHKH_ApprovedAccount',N'TaiKhoanKhachHangId',1,0,N'TrangThai=''APPROVED'''),
 (N'SoHuuKhachHang',N'UX_SHKH_ApprovedCustomer',N'KhachHangId',1,0,N'TrangThai=''APPROVED'''),
 (N'SoHuuKhachHang',N'IX_SHKH_AccountHistory',N'TaiKhoanKhachHangId|SoHuuKhachHangId',0,0,NULL);

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
 BEGIN TRANSACTION;
 -- Refuse all object kinds/schemas, including a partial P04 installation.
 IF EXISTS(SELECT 1 FROM sys.objects WHERE is_ms_shipped=0 AND
  (name IN(N'YeuCauDatLich',N'YeuCauDatLichDichVu',N'YeuCauDatLichLichSu')
   OR name LIKE N'PK[_]YCDL%' OR name LIKE N'FK[_]YCDL%' OR name LIKE N'CK[_]YCDL%'
   OR name LIKE N'UQ[_]YCDL%' OR name LIKE N'DF[_]YCDL%'))
  THROW 51040, 'P04 objects already exist; inspect instead of rerunning.', 1;
 IF EXISTS(SELECT 1 FROM sys.objects o WHERE o.is_ms_shipped=0 AND o.type IN('U','V','SN','P','FN','IF','TF','TR')
  AND (o.type<>'U' OR SCHEMA_NAME(o.schema_id)<>N'dbo'
   OR NOT EXISTS(SELECT 1 FROM @RequiredColumns e WHERE e.TableName=o.name)))
  THROW 51040, 'Unexpected user objects or triggers in the disposable baseline.', 1;
 IF EXISTS(SELECT 1 FROM @RequiredColumns e
  LEFT JOIN sys.tables t ON t.name COLLATE Latin1_General_100_BIN2=e.TableName COLLATE Latin1_General_100_BIN2 AND t.schema_id=SCHEMA_ID(N'dbo')
  LEFT JOIN sys.columns c ON c.object_id=t.object_id AND c.name COLLATE Latin1_General_100_BIN2=e.ColumnName COLLATE Latin1_General_100_BIN2
  LEFT JOIN sys.types ty ON ty.user_type_id=c.user_type_id
  LEFT JOIN sys.identity_columns idc ON idc.object_id=c.object_id AND idc.column_id=c.column_id
  WHERE c.column_id IS NULL OR ty.name COLLATE Latin1_General_100_BIN2<>e.TypeName COLLATE Latin1_General_100_BIN2
   OR c.user_type_id<>c.system_type_id OR c.is_nullable<>e.Nullable OR c.is_identity<>e.IsIdentity OR c.is_computed<>0
   OR (e.MaxLength IS NOT NULL AND c.max_length<>e.MaxLength)
   OR (e.PrecisionValue IS NOT NULL AND c.precision<>e.PrecisionValue)
   OR (e.ScaleValue IS NOT NULL AND c.scale<>e.ScaleValue)
   OR (e.IsIdentity=1 AND (CONVERT(decimal(38,0),idc.seed_value)<>1 OR CONVERT(decimal(38,0),idc.increment_value)<>1))
   OR (c.collation_name IS NOT NULL AND c.collation_name COLLATE Latin1_General_100_BIN2<>
    (CASE WHEN e.TableName=N'TaiKhoanKhachHang' AND e.ColumnName=N'LoginEmail' THEN N'Latin1_General_100_BIN2' ELSE @ExpectedDatabaseCollation END) COLLATE Latin1_General_100_BIN2))
  OR EXISTS(SELECT 1 FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
   WHERE t.schema_id=SCHEMA_ID(N'dbo') AND NOT EXISTS(SELECT 1 FROM @RequiredColumns e
    WHERE e.TableName=t.name AND e.ColumnName=c.name))
  THROW 51040, 'Required business/P03 column, type, nullability or identity contract mismatch.', 1;
 IF EXISTS(SELECT 1 FROM @RequiredForeignKeys e WHERE NOT EXISTS(
  SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
  JOIN sys.tables t ON t.object_id=fk.parent_object_id JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=fkc.parent_column_id
  JOIN sys.tables pt ON pt.object_id=fk.referenced_object_id JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fkc.referenced_column_id
  WHERE t.schema_id=SCHEMA_ID(N'dbo') AND pt.schema_id=SCHEMA_ID(N'dbo') AND t.name=e.TableName AND c.name=e.ColumnName
   AND pt.name=e.ParentTable AND pc.name=e.ParentColumn AND fk.is_disabled=0 AND fk.is_not_trusted=0
   AND fk.delete_referential_action=0 AND fk.update_referential_action=0
   AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id)=1))
  OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1 OR definition IS NULL)
  THROW 51040, 'Required trusted business/P03 foreign-key or CHECK constraint mismatch.', 1;
 IF EXISTS(SELECT 1 FROM @RequiredIndexes e WHERE NOT EXISTS(
  SELECT 1 FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id
  CROSS APPLY(SELECT STRING_AGG(CONVERT(nvarchar(max),c.name),N'|') WITHIN GROUP(ORDER BY ic.key_ordinal) AS KeyColumns
   FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
   WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0) k
  WHERE t.schema_id=SCHEMA_ID(N'dbo') AND t.name=e.TableName AND (e.IndexName IS NULL OR i.name=e.IndexName)
   AND i.is_unique=e.IsUnique AND i.is_primary_key=e.IsPrimary AND i.is_disabled=0 AND i.is_hypothetical=0
   AND k.KeyColumns=e.KeyColumns AND NOT EXISTS(SELECT 1 FROM sys.index_columns d
    WHERE d.object_id=i.object_id AND d.index_id=i.index_id AND d.key_ordinal>0 AND d.is_descending_key=1)
   AND ((e.SimpleFilter IS NULL AND i.has_filter=0) OR (e.SimpleFilter IS NOT NULL AND i.has_filter=1
    AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(i.filter_definition,N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'') COLLATE Latin1_General_100_BIN2=e.SimpleFilter COLLATE Latin1_General_100_BIN2))))
  THROW 51040, 'Required business/P03 primary, unique, filtered or scheduling index mismatch.', 1;

 -- Pin full definitions too. Expected SHA256 comes from an independently approved reference baseline
 -- using THIS canonical inventory, never from an unreviewed candidate (no trust-on-first-use).
 -- Includes all CHECK/default/index/FK/column definitions; omits generated constraint names/object IDs.
 DECLARE @BaselineInventory nvarchar(max)=CONVERT(nvarchar(max),(
  SELECT
   (SELECT t.name AS [table],c.column_id,c.name,ty.name AS [type],c.max_length,c.precision,c.scale,
     c.is_nullable,c.is_identity,c.is_computed,c.collation_name,c.generated_always_type,c.encryption_type,
     CONVERT(nvarchar(100),idc.seed_value) AS seed,CONVERT(nvarchar(100),idc.increment_value) AS increment,
     dc.definition AS [default],cc.definition AS computed
    FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
    LEFT JOIN sys.identity_columns idc ON idc.object_id=c.object_id AND idc.column_id=c.column_id
    LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
    LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
    WHERE t.schema_id=SCHEMA_ID(N'dbo') ORDER BY t.name COLLATE Latin1_General_100_BIN2,c.column_id FOR XML PATH('column'),TYPE) AS [columns],
   (SELECT t.name AS [table],ck.parent_column_id,ck.definition,ck.is_disabled,ck.is_not_trusted,ck.is_not_for_replication
    FROM sys.check_constraints ck JOIN sys.tables t ON t.object_id=ck.parent_object_id
    WHERE t.schema_id=SCHEMA_ID(N'dbo') ORDER BY t.name COLLATE Latin1_General_100_BIN2,ck.parent_column_id,ck.definition COLLATE Latin1_General_100_BIN2 FOR XML PATH('check'),TYPE) AS checks,
   (SELECT t.name AS [table],c.name AS [column],pt.name AS [parent],pc.name AS [parentColumn],
     fk.is_disabled,fk.is_not_trusted,fk.is_not_for_replication,fk.delete_referential_action,fk.update_referential_action
    FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
    JOIN sys.tables t ON t.object_id=fk.parent_object_id JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=fkc.parent_column_id
    JOIN sys.tables pt ON pt.object_id=fk.referenced_object_id JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fkc.referenced_column_id
    WHERE t.schema_id=SCHEMA_ID(N'dbo')
    ORDER BY t.name COLLATE Latin1_General_100_BIN2,c.name COLLATE Latin1_General_100_BIN2,pt.name COLLATE Latin1_General_100_BIN2,pc.name COLLATE Latin1_General_100_BIN2 FOR XML PATH('fk'),TYPE) AS foreignKeys,
   (SELECT t.name AS [table],i.type,i.is_unique,i.is_primary_key,i.is_unique_constraint,i.has_filter,i.filter_definition,i.is_disabled,i.ignore_dup_key,
     (SELECT c.name,ic.key_ordinal,ic.is_descending_key,ic.is_included_column
      FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id ORDER BY ic.index_column_id FOR XML PATH('key'),TYPE) AS [keys]
    FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id
    WHERE t.schema_id=SCHEMA_ID(N'dbo') AND i.index_id>0
    ORDER BY t.name COLLATE Latin1_General_100_BIN2,i.is_primary_key DESC,i.is_unique DESC,i.name COLLATE Latin1_General_100_BIN2 FOR XML PATH('index'),TYPE) AS indexes
  FOR XML PATH('baseline'),TYPE));
 DECLARE @ObservedBaselineFingerprint varbinary(32)=HASHBYTES('SHA2_256',@BaselineInventory);
 IF @ObservedBaselineFingerprint IS NULL OR @ObservedBaselineFingerprint<>@ExpectedBaselineFingerprint
  THROW 51040, 'Approved complete baseline fingerprint mismatch; external review required.', 1;

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
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
