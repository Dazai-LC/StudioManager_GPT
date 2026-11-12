SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(CAST(N'' AS nvarchar(max)),
 CONVERT(nvarchar(max),(SELECT s.name AS [schema],t.name AS [table],c.column_id,c.name,ty.name AS [type],
 c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.collation_name,dc.definition AS [default],cc.definition AS [computed]
 FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id
 JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
 LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
 WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name,c.column_id FOR XML RAW('column'),TYPE)),
 CONVERT(nvarchar(max),(SELECT OBJECT_SCHEMA_NAME(i.object_id) AS [schema],OBJECT_NAME(i.object_id) AS [table],i.name,
 i.type_desc,i.is_unique,i.is_primary_key,i.is_unique_constraint,i.is_disabled,i.filter_definition,
 c.name AS [column],ic.key_ordinal,ic.is_included_column,ic.is_descending_key
 FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id
 JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
 JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
 WHERE t.is_ms_shipped=0 ORDER BY [schema],[table],i.name,ic.index_column_id FOR XML RAW('index'),TYPE)),
 CONVERT(nvarchar(max),(SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) AS [schema],OBJECT_NAME(f.parent_object_id) AS [table],f.name,
 pc.name AS [parentColumn],OBJECT_SCHEMA_NAME(f.referenced_object_id) AS [referencedSchema],OBJECT_NAME(f.referenced_object_id) AS [referencedTable],
 rc.name AS [referencedColumn],f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted
 FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
 JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id
 JOIN sys.columns rc ON rc.object_id=fc.referenced_object_id AND rc.column_id=fc.referenced_column_id
 ORDER BY [schema],[table],f.name,fc.constraint_column_id FOR XML RAW('foreignKey'),TYPE)),
 CONVERT(nvarchar(max),(SELECT OBJECT_SCHEMA_NAME(parent_object_id) AS [schema],OBJECT_NAME(parent_object_id) AS [table],name,definition,is_disabled,is_not_trusted
 FROM sys.check_constraints ORDER BY [schema],[table],name FOR XML RAW('check'),TYPE))
)),2)
