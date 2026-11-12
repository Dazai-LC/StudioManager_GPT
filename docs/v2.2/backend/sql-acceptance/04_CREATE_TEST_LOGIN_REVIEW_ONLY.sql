-- REVIEW ONLY. Windows group must already exist and have reviewed non-admin membership.
-- Current sysadmin is the provisioner ONLY. No server-wide write permission is granted to the runner.
SET NOCOUNT ON;
DECLARE @OwnerApprovedExecution bit=0;
IF @OwnerApprovedExecution<>1 THROW 51010, 'Execution not approved.', 1;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'
 OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'
 OR COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\SQLEXPRESS'
 THROW 51010, 'Refused: exact MSI/SQLEXPRESS server identity is required.', 1;
IF COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'master' THROW 51010, 'Connect to master on SQLEXPRESS only.', 1;
IF COALESCE(IS_SRVROLEMEMBER('sysadmin'),0)<>1 THROW 51010, 'Reviewed provisioning identity required.', 1;
IF DB_ID(N'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e') IS NULL THROW 51010, 'Provision and review the exact test database first.', 1;
IF EXISTS(SELECT 1 FROM sys.databases WHERE database_id>4 AND name COLLATE Latin1_General_100_BIN2<>N'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e')
 THROW 51010, 'Unexpected user database on SQLEXPRESS; stop for review.', 1;
IF SUSER_ID(N'MSI\PhoenixP02Tests') IS NOT NULL THROW 51010, 'Login already exists; stop instead of changing an existing principal.', 1;
CREATE LOGIN [MSI\PhoenixP02Tests] FROM WINDOWS
 WITH DEFAULT_DATABASE=[StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e];
-- New login receives only public defaults. Do not add server roles or server permission grants.
-- Actual runner token (including inherited Windows groups) must pass the harness before any DML.
