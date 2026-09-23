/*
  Upgrade an existing StudioManager database without deleting business data.
  Run after 01_CreateDatabase.sql and 02_CreateSchema.sql only when upgrading an
  already-created database. A fresh installation still uses scripts 01-04.
*/
USE StudioManager;
GO

IF COL_LENGTH('LichChup', 'HoanThanhLuc') IS NULL
    ALTER TABLE LichChup ADD HoanThanhLuc datetime2(0) NULL;
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
