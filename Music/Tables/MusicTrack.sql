CREATE TABLE [dbo].[MusicTrack]
(
	[MusicFileId] INT NOT NULL PRIMARY KEY Identity(1,1),
	[FileName] nvarchar(255) not null,
	[Title] nvarchar(255) null,
	Artist nvarchar(255) null,
	Album nvarchar(255) null,
	

)
