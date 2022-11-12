CREATE TABLE [dbo].[MusicFilesAgain]
(
	[MusicFileId] INT NOT NULL PRIMARY KEY Identity(1,1),
	[Name] nvarchar(300) not null,
	Artist nvarchar(300) not null,
	Album nvarchar(300) not null,
	Location nvarchar(500) not null

)
