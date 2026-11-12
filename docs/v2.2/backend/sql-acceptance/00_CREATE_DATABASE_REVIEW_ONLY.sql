-- REVIEW ONLY. No database was created by preparing this artifact.
-- Requires separate Owner approval; no existing database is ever reused/dropped.
SET NOCOUNT ON;
DECLARE @OwnerApprovedExecution bit=0;
IF @OwnerApprovedExecution<>1 THROW 51010, 'Execution not approved: review the exact test target first.', 1;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'
 OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'
 OR COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\SQLEXPRESS'
 THROW 51010, 'Refused: exact MSI/SQLEXPRESS server identity is required.', 1;
IF COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'master' THROW 51010, 'Connect to master on SQLEXPRESS only.', 1;
IF DB_ID(N'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e') IS NOT NULL THROW 51010, 'Target already exists; inspect it instead of overwriting.', 1;
IF EXISTS(SELECT 1 FROM sys.databases WHERE database_id>4) THROW 51010, 'Unexpected user database on SQLEXPRESS; stop for review.', 1;
IF COALESCE(IS_SRVROLEMEMBER('sysadmin'),0)<>1 THROW 51010, 'Reviewed provisioning identity required; never use the test runner.', 1;
EXEC(N'CREATE DATABASE [StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e]');
EXEC(N'ALTER DATABASE [StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e] SET READ_COMMITTED_SNAPSHOT ON');
EXEC(N'ALTER DATABASE [StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e] SET TRUSTWORTHY OFF');
EXEC(N'ALTER DATABASE [StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e] SET DB_CHAINING OFF');
-- Failure recovery: leave the database for investigation. No automatic DROP or restore.
