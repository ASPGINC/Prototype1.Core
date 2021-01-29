--declare @dbName varchar(128) = DB_Name()
    
--declare @sqlSingleUser nvarchar(500) = 'ALTER DATABASE [' + @dbName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE'
--EXECUTE sp_executesql @sqlSingleUser;