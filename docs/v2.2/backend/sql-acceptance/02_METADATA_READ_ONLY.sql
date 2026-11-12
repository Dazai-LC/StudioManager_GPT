-- READ-ONLY METADATA. No business rows, credentials, connection strings or hash values.
SET NOCOUNT ON;
IF COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),N'') COLLATE Latin1_General_100_BIN2<>N'MSI'
 OR COALESCE(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'') COLLATE Latin1_General_100_BIN2<>N'SQLEXPRESS'
 OR COALESCE(CONVERT(nvarchar(128),@@SERVERNAME),N'') COLLATE Latin1_General_100_BIN2<>N'MSI\SQLEXPRESS'
 THROW 51010, 'Refused: exact MSI/SQLEXPRESS server identity is required.', 1;
IF COALESCE(DB_NAME(),N'') COLLATE Latin1_General_100_BIN2<>N'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e' THROW 51010, 'Wrong test database.', 1;
SELECT CONVERT(nvarchar(128),@@SERVERNAME) ServerName,CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')) MachineName,
 CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) InstanceName,
 CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) ProductVersion,
 DB_NAME() DatabaseName,d.compatibility_level,d.is_read_committed_snapshot_on,d.is_trustworthy_on,d.is_db_chaining_on
FROM sys.databases d WHERE d.database_id=DB_ID();
SELECT name,CONVERT(nvarchar(128),value) MarkerValue FROM sys.extended_properties
 WHERE class=0 AND name IN(N'Phoenix.DisposableTestRunId',N'Phoenix.SchemaVersion',N'Phoenix.SchemaScriptsSha256',N'Phoenix.SchemaMetadataSha256');
SELECT s.name SchemaName,t.name TableName,c.column_id,c.name ColumnName,ty.name TypeName,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,dc.definition DefaultDefinition
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id
JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name,c.column_id;
SELECT OBJECT_SCHEMA_NAME(parent_object_id) SchemaName,OBJECT_NAME(parent_object_id) TableName,name,type_desc
FROM sys.key_constraints ORDER BY SchemaName,TableName,name;
SELECT OBJECT_NAME(parent_object_id) TableName,name,definition,is_disabled,is_not_trusted FROM sys.check_constraints ORDER BY TableName,name;
SELECT OBJECT_NAME(f.parent_object_id) TableName,f.name,pc.name ParentColumn,OBJECT_NAME(f.referenced_object_id) ReferencedTable,rc.name ReferencedColumn,
 f.delete_referential_action_desc,f.update_referential_action_desc,f.is_disabled,f.is_not_trusted
FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id
JOIN sys.columns rc ON rc.object_id=fc.referenced_object_id AND rc.column_id=fc.referenced_column_id
ORDER BY TableName,f.name,fc.constraint_column_id;
SELECT OBJECT_NAME(i.object_id) TableName,i.name,i.is_unique,i.is_primary_key,i.is_disabled,i.filter_definition,
 c.name ColumnName,ic.key_ordinal,ic.is_included_column,ic.is_descending_key
FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.is_ms_shipped=0
ORDER BY TableName,i.name,ic.key_ordinal,ic.index_column_id;
-- Names/types only: never dump arbitrary module definitions containing potential secrets.
SELECT s.name SchemaName,o.name,o.type_desc FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE o.is_ms_shipped=0 AND o.type IN('P','V','TR','FN','IF','TF','SN') ORDER BY s.name,o.name;
SELECT permission_name FROM sys.fn_my_permissions(NULL,'SERVER') ORDER BY permission_name;
SELECT permission_name FROM sys.fn_my_permissions(NULL,'DATABASE') ORDER BY permission_name;
SELECT COALESCE(IS_SRVROLEMEMBER('sysadmin'),1) IsSysadmin,COALESCE(IS_MEMBER('db_owner'),1) IsDatabaseOwner,
 COALESCE(HAS_PERMS_BY_NAME(NULL,'SERVER','CREATE ANY DATABASE'),1) CanCreateAnyDatabase,
 COALESCE(HAS_DBACCESS(N'StudioManager'),0) CanAccessStudioDatabase;
SELECT t.name TableName,HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','SELECT') CanSelect,
 HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','INSERT') CanInsert,
 HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','UPDATE') CanUpdate,
 HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','DELETE') CanDelete
FROM sys.tables t WHERE t.schema_id=SCHEMA_ID('dbo') ORDER BY t.name;
