USE master;
GO
-- THAY đường dẫn dưới đây, sau đó đóng Studio Manager trước khi chạy.
DECLARE @BackupFile nvarchar(1000)=N'C:\Backup\StudioManager.bak';
ALTER DATABASE StudioManager SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE StudioManager FROM DISK=@BackupFile WITH REPLACE, RECOVERY;
ALTER DATABASE StudioManager SET MULTI_USER;
GO
