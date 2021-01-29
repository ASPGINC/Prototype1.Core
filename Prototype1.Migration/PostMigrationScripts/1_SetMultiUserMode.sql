--declare @dbName varchar(128) = DB_Name()
--declare @sqlSnapshot nvarchar(500) = 'ALTER DATABASE [' + @dbName + '] SET ALLOW_SNAPSHOT_ISOLATION ON'
--EXECUTE sp_executesql @sqlSnapshot

--declare @sqlMultiUser nvarchar(500) = 'ALTER DATABASE [' + @dbName + ']	SET MULTI_USER'
--EXECUTE sp_executesql @sqlMultiUser