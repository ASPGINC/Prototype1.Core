IF NOT EXISTS (SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].[VersionInfo]') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
	CREATE TABLE [dbo].[VersionInfo](
		[Version] [bigint] NOT NULL
	) ON [PRIMARY]
END