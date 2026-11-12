-- REVIEW ONLY. Grants are scoped to the one new test database; no db_owner/DDL/backup privileges.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @OwnerApprovedExecution bit=0;
IF @OwnerApprovedExecution<>1 THROW 51010, 'Execution not approved.', 1;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'
 OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'
 OR COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\SQLEXPRESS'
 THROW 51010, 'Refused: exact MSI/SQLEXPRESS server identity is required.', 1;
IF COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e' THROW 51010, 'Wrong test database.', 1;
IF CURRENT_USER<>N'dbo' OR COALESCE(IS_SRVROLEMEMBER('sysadmin'),0)<>1 THROW 51010, 'Reviewed provisioning identity required.', 1;
IF COALESCE(CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.DisposableTestRunId')),N'') COLLATE Latin1_General_100_BIN2<>N'7d70a08ac15349acbd207f82c8349f6e'
 THROW 51010, 'Reviewed run marker required.', 1;
IF COALESCE(CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.SchemaVersion')),N'') COLLATE Latin1_General_100_BIN2<>N'baseline-c2c3c23-spec2.1'
 THROW 51010, 'Reviewed schema version required.', 1;
IF COALESCE(CONVERT(nvarchar(128),(SELECT value FROM sys.extended_properties WHERE class=0 AND name=N'Phoenix.SchemaScriptsSha256')),N'') COLLATE Latin1_General_100_BIN2<>N'B8A556B123DDB93B742FDA93643AC70CB4D11E8F62E9AE30BAF37758900B0EF3'
 THROW 51010, 'Reviewed schema source checksum required.', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>14 THROW 51010, 'Reviewed fresh schema required.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=N'MSI\PhoenixP02Tests' AND type='G') THROW 51010, 'Expected Windows group login required.', 1;
IF USER_ID(N'MSI\PhoenixP02Tests') IS NOT NULL THROW 51010, 'User already exists; stop instead of changing permissions.', 1;
BEGIN TRY
 BEGIN TRANSACTION;
 CREATE USER [MSI\PhoenixP02Tests] FOR LOGIN [MSI\PhoenixP02Tests] WITH DEFAULT_SCHEMA=[dbo];
 GRANT CONNECT, VIEW DEFINITION TO [MSI\PhoenixP02Tests];
 GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [MSI\PhoenixP02Tests];
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW 51010, 'Test user provisioning failed; inspect the isolated target. No automatic reset.', 1;
END CATCH;
