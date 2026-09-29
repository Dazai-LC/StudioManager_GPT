/*
  Upgrade an existing StudioManager database without deleting business data.
  Run after 01_CreateDatabase.sql and 02_CreateSchema.sql only when upgrading an
  already-created database. A fresh installation still uses scripts 01-04.
*/
USE StudioManager;
GO

/* Required by SQL Server when creating a filtered index. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF COL_LENGTH('LichChup', 'HoanThanhLuc') IS NULL
    ALTER TABLE LichChup ADD HoanThanhLuc datetime2(0) NULL;
GO

/* Older databases were created before update attribution was added to these
   dependent records. Keep this migration idempotent and preserve all data. */
IF COL_LENGTH('PhanCongTaiNguyen', 'UpdatedBy') IS NULL
    EXEC(N'ALTER TABLE PhanCongTaiNguyen ADD UpdatedBy int NULL;');
GO
IF COL_LENGTH('PhanCongTaiNguyen', 'UpdatedBy') IS NOT NULL
BEGIN
    EXEC(N'UPDATE PhanCongTaiNguyen SET UpdatedBy = CreatedBy WHERE UpdatedBy IS NULL;');
    EXEC(N'ALTER TABLE PhanCongTaiNguyen ALTER COLUMN UpdatedBy int NOT NULL;');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_PhanCongTaiNguyen_UpdatedBy')
        EXEC(N'ALTER TABLE PhanCongTaiNguyen ADD CONSTRAINT FK_PhanCongTaiNguyen_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES TaiKhoan(TaiKhoanId);');
END
GO
IF COL_LENGTH('PhanCongTaiNguyen', 'UpdatedAt') IS NULL
    EXEC(N'ALTER TABLE PhanCongTaiNguyen ADD UpdatedAt datetime2(0) NULL;');
GO
IF COL_LENGTH('PhanCongTaiNguyen', 'UpdatedAt') IS NOT NULL
BEGIN
    EXEC(N'UPDATE PhanCongTaiNguyen SET UpdatedAt = CreatedAt WHERE UpdatedAt IS NULL;');
    EXEC(N'ALTER TABLE PhanCongTaiNguyen ALTER COLUMN UpdatedAt datetime2(0) NOT NULL;');
END
GO
IF COL_LENGTH('LichChupDichVu', 'UpdatedBy') IS NULL
    EXEC(N'ALTER TABLE LichChupDichVu ADD UpdatedBy int NULL;');
GO
IF COL_LENGTH('LichChupDichVu', 'UpdatedBy') IS NOT NULL
BEGIN
    EXEC(N'UPDATE LichChupDichVu SET UpdatedBy = CreatedBy WHERE UpdatedBy IS NULL;');
    EXEC(N'ALTER TABLE LichChupDichVu ALTER COLUMN UpdatedBy int NOT NULL;');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LichChupDichVu_UpdatedBy')
        EXEC(N'ALTER TABLE LichChupDichVu ADD CONSTRAINT FK_LichChupDichVu_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES TaiKhoan(TaiKhoanId);');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LichChup_HoanThanhLuc' AND object_id=OBJECT_ID('LichChup'))
    CREATE INDEX IX_LichChup_HoanThanhLuc ON LichChup(HoanThanhLuc) WHERE HoanThanhLuc IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ThanhToan_NgayGiaoDich' AND object_id=OBJECT_ID('ThanhToan'))
    CREATE INDEX IX_ThanhToan_NgayGiaoDich ON ThanhToan(NgayGiaoDich);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HoanTien_NgayGiaoDich' AND object_id=OBJECT_ID('HoanTien'))
    CREATE INDEX IX_HoanTien_NgayGiaoDich ON HoanTien(NgayGiaoDich);
GO

/*
  Financial totals and resource capacity are cross-row invariants. They are
  deliberately enforced by serializable Application/Repository transactions,
  not by a SQL CHECK constraint (which cannot safely aggregate other rows).
*/
