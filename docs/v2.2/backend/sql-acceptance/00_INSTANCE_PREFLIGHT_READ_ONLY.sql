-- REVIEW ONLY: bounded instance metadata; no SQL mutation or account creation.
SET NOCOUNT ON;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'
 OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'
 OR COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\SQLEXPRESS'
 THROW 51010, 'Refused: exact MSI/SQLEXPRESS server identity is required.', 1;
IF COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'master' THROW 51010, 'Connect to master on SQLEXPRESS only.', 1;
IF COALESCE(IS_SRVROLEMEMBER('sysadmin'),0)<>1 THROW 51010, 'Provisioner metadata visibility required.', 1;
SELECT CONVERT(nvarchar(128),@@SERVERNAME) ServerName,
 CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')) MachineName,
 CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) InstanceName,
 CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) ProductVersion,
 CONVERT(nvarchar(128),SERVERPROPERTY('Edition')) Edition,
 IS_SRVROLEMEMBER('sysadmin') IsProvisionerSysadmin,
 HAS_PERMS_BY_NAME(NULL,'SERVER','CREATE ANY DATABASE') CanCreateAnyDatabase;
SELECT name,state_desc FROM sys.databases WHERE database_id>4 ORDER BY name;
IF EXISTS(SELECT 1 FROM sys.databases WHERE database_id>4)
 THROW 51010, 'Expected no user databases before provisioning; stop for review.', 1;
