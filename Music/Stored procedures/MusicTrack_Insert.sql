CREATE PROCEDURE [dbo].[MusicTrack_Insert]
    @Filename NVARCHAR(255), 
    @Title NVARCHAR(255) = NULL,
    @Artist NVARCHAR(255) = NULL,
    @Album NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE INTO [MusicTrack] AS target
    USING (SELECT @Filename AS Filename, @Title AS Title, @Artist AS Artist, @Album AS Album) AS source
    ON 
        (target.Filename = source.Filename AND
         ISNULL(target.Title, '') = ISNULL(source.Title, '') AND
         ISNULL(target.Artist, '') = ISNULL(source.Artist, '') AND
         ISNULL(target.Album, '') = ISNULL(source.Album, ''))
    WHEN MATCHED THEN
        UPDATE SET
            Title = source.Title,
            Artist = source.Artist,
            Album = source.Album
    WHEN NOT MATCHED THEN
        INSERT (Filename, Title, Artist, Album)
        VALUES (source.Filename, source.Title, source.Artist, source.Album);
END;