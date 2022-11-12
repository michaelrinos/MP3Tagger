CREATE PROCEDURE [dbo].[MusicFile_Create]
	@Name nvarchar(300) ,
	@Artist nvarchar(300) ,
	@Album nvarchar(300) ,
	@Location nvarchar(500) 
AS
BEGIN

insert into dbo.MusicFilesAgain
select @Name, @Artist, @Album, @Location

END
	
