-- REVIEW ONLY / NOT AUTHORIZED / NOT EXECUTED. No automatic migration or grants.
-- Owner reviews an execution COPY against one disposable P04/demo database only.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @OwnerApprovedExecution bit=0;
DECLARE @ExpectedServerName sysname=NULL;
DECLARE @ExpectedInstanceName sysname=NULL;
DECLARE @ExpectedDatabaseName sysname=NULL;
DECLARE @ExpectedDatabaseId int=NULL;
DECLARE @ExpectedDatabaseCreatedAt datetime2(7)=NULL;
DECLARE @RunId uniqueidentifier=NULL;
IF COALESCE(@OwnerApprovedExecution,0)<>1 OR @RunId IS NULL OR @RunId='00000000-0000-0000-0000-000000000000'
 OR @ExpectedServerName IS NULL OR @ExpectedInstanceName IS NULL OR @ExpectedDatabaseName IS NULL
 OR @ExpectedDatabaseId IS NULL OR @ExpectedDatabaseId<=4 OR @ExpectedDatabaseCreatedAt IS NULL OR @@TRANCOUNT<>0
 THROW 51080,'Demo receipt migration has not been approved.',1;
DECLARE @RunText nvarchar(32)=LOWER(REPLACE(CONVERT(nvarchar(36),@RunId),N'-',N''));
IF SERVERPROPERTY('ServerName') IS NULL OR SERVERPROPERTY('InstanceName') IS NULL OR @@SERVERNAME IS NULL
 OR DATALENGTH(CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')))<>DATALENGTH(@ExpectedServerName)
 OR DATALENGTH(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')))<>DATALENGTH(@ExpectedInstanceName)
 OR DATALENGTH(CONVERT(nvarchar(128),@@SERVERNAME))<>DATALENGTH(@ExpectedServerName)
 THROW 51080,'SQL identity is missing or differs from the approval record.',1;
IF @ExpectedDatabaseName COLLATE Latin1_General_100_BIN2 NOT IN
 (N'StudioManager_P04_Test_'+@RunText,N'StudioManager_Demo_Test_'+@RunText)
 OR DB_NAME() COLLATE Latin1_General_100_BIN2<>@ExpectedDatabaseName COLLATE Latin1_General_100_BIN2
 OR DATALENGTH(DB_NAME())<>DATALENGTH(@ExpectedDatabaseName) OR DB_ID()<>@ExpectedDatabaseId
 OR CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) COLLATE Latin1_General_100_BIN2<>@ExpectedServerName COLLATE Latin1_General_100_BIN2
 OR CONVERT(nvarchar(128),@@SERVERNAME) COLLATE Latin1_General_100_BIN2<>@ExpectedServerName COLLATE Latin1_General_100_BIN2
 OR SERVERPROPERTY('InstanceName') IS NULL
 OR CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) COLLATE Latin1_General_100_BIN2<>@ExpectedInstanceName COLLATE Latin1_General_100_BIN2
 OR NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id=DB_ID() AND CONVERT(datetime2(7),create_date)=@ExpectedDatabaseCreatedAt
   AND state=0 AND is_read_only=0 AND source_database_id IS NULL AND group_database_id IS NULL)
 THROW 51080,'Wrong or recreated disposable target; protected databases are never eligible.',1;
IF OBJECT_ID(N'dbo.ThanhToan',N'U') IS NULL OR OBJECT_ID(N'dbo.LichChup',N'U') IS NULL
 OR OBJECT_ID(N'dbo.YeuCauDatLich',N'U') IS NULL
 OR COL_LENGTH(N'dbo.ThanhToan',N'PhuongThucThu') IS NOT NULL
 OR COL_LENGTH(N'dbo.ThanhToan',N'YeuCauThuId') IS NOT NULL OR COL_LENGTH(N'dbo.ThanhToan',N'BienNhanJson') IS NOT NULL
 THROW 51080,'Reviewed P03/P04 baseline required; inspect existing/partial receipt schema instead of replaying.',1;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
 BEGIN TRANSACTION;
 ALTER TABLE dbo.ThanhToan ADD PhuongThucThu varchar(24) NULL,YeuCauThuId uniqueidentifier NULL,BienNhanJson nvarchar(max) NULL;
 -- A separate batch is required so new-column references bind after ALTER TABLE.
 EXEC sys.sp_executesql N'ALTER TABLE dbo.ThanhToan WITH CHECK ADD CONSTRAINT CK_ThanhToan_DemoReceipt CHECK(
  (PhuongThucThu IS NULL AND YeuCauThuId IS NULL AND BienNhanJson IS NULL) OR
  (PhuongThucThu IS NOT NULL AND PhuongThucThu COLLATE Latin1_General_100_BIN2 IN(''CASH'',''BANK_TRANSFER_DEMO'')
   AND YeuCauThuId IS NOT NULL AND BienNhanJson IS NOT NULL AND ISJSON(BienNhanJson)=1));
 CREATE UNIQUE INDEX UX_ThanhToan_YeuCauThu ON dbo.ThanhToan(YeuCauThuId) WHERE YeuCauThuId IS NOT NULL;';
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
-- Historical rows keep all three NULL. No UPDATE/backfill, money rewrite, FK change, new ledger or permissions.
